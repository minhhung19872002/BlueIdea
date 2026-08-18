import { useState } from 'react';
import {
  App,
  Alert,
  Button,
  Card,
  Input,
  InputNumber,
  Modal,
  Select,
  Space,
  Table,
  Tag,
  Tooltip,
  Typography,
} from 'antd';
import {
  ArrowDownOutlined,
  ArrowUpOutlined,
  BarChartOutlined,
  DeleteOutlined,
  EditOutlined,
  PlusOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { z } from 'zod';

import { LoiApi } from '@/api/client';
import { BieuMau, Truong, useBieuMau } from '@/components/bieu-mau/BieuMau';
import { batBuoc, maDanhMuc, soNguyen, trangThai, tuyChon } from '@/components/bieu-mau/luat';
import {
  apiBieuMauThongKe,
  apiNhapXuat,
  type BieuMauThongKeChiTiet,
  type CotBaoCao,
  type DanhMucDto,
} from '@/api/endpoints';
import { KhoiLoi, ngayGio } from '@/components/ThanhPhanChung';
import { DaiTabTrang } from '@/components/DaiTabTrang';
import { DS_TAB_DANH_MUC } from '@/features/quan-tri/danhMucTab';

/** Luật kiểm tra biểu mẫu thống kê. */
const luatBieuMau = z.object({
  ma: maDanhMuc(),
  ten: batBuoc('Tên biểu mẫu'),
  moTa: tuyChon(),
  loaiBaoCao: tuyChon(100),
  thuTu: soNguyen('Thứ tự', 0, 9999),
  trangThai: trangThai,
});

type GiaTriBieuMau = z.infer<typeof luatBieuMau>;

const MAC_DINH_BIEU_MAU: GiaTriBieuMau = {
  ma: '',
  ten: '',
  thuTu: 0,
  trangThai: 1,
};

/**
 * Hàm tổng hợp máy chủ chấp nhận (khớp `HamChoPhep` của DichVuBaoCaoTuyBien).
 *
 * Để trống nghĩa là in thẳng giá trị của từng hồ sơ — đó là lựa chọn thường dùng nhất nên đặt
 * đầu danh sách.
 */
const HAM_TONG_HOP = [
  { value: '', label: 'Không tổng hợp (liệt kê từng hồ sơ)' },
  { value: 'DEM', label: 'Đếm số hồ sơ' },
  { value: 'TONG', label: 'Tổng' },
  { value: 'TRUNG_BINH', label: 'Trung bình' },
  { value: 'LON_NHAT', label: 'Lớn nhất' },
  { value: 'NHO_NHAT', label: 'Nhỏ nhất' },
];

const DINH_DANG_XUAT = [
  { value: 'XLSX', label: 'Excel (.xlsx)' },
  { value: 'PDF', label: 'PDF' },
];

/**
 * Chức năng 7 — Biểu mẫu thống kê cho báo cáo tuỳ biến.
 *
 * Trước đây chỉ có API và màn hình *chạy* báo cáo (`/bao-cao/tuy-bien`), không có chỗ nào tạo hay
 * sửa biểu mẫu: quản trị viên chỉ dùng được đúng mấy mẫu seed tạo sẵn, muốn thêm phải sửa thẳng
 * cơ sở dữ liệu.
 *
 * Cấu hình cột soạn ngay trên bảng thay vì mở thêm hộp thoại con: người dùng cần nhìn thấy toàn
 * bộ danh sách cột khi sắp thứ tự, mà mở hộp thoại từng dòng thì mất đúng cái nhìn đó.
 */
export default function TrangBieuMauThongKe() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [trang, setTrang] = useState(1);
  const [soDong, setSoDong] = useState(20);
  const [tuKhoa, setTuKhoa] = useState('');
  const [moForm, setMoForm] = useState(false);
  const [dangSua, setDangSua] = useState<BieuMauThongKeChiTiet | null>(null);
  const [cot, setCot] = useState<CotBaoCao[]>([]);
  const [dinhDangXuat, setDinhDangXuat] = useState<string[]>(['XLSX', 'PDF']);

  const form = useBieuMau(luatBieuMau, MAC_DINH_BIEU_MAU);

  const thamSo = { trang, soDong, tuKhoa };

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['bieu-mau-thong-ke', thamSo],
    queryFn: () => apiBieuMauThongKe.danhSach(thamSo),
  });

  // Danh sách trường có thể đưa vào báo cáo do máy chủ quyết định — không khai cứng ở đây, vì
  // thêm trường mới ở máy chủ mà quên sửa màn hình thì người dùng không bao giờ chọn được nó.
  const { data: nguonDuLieu } = useQuery({
    queryKey: ['nguon-du-lieu-bao-cao'],
    queryFn: apiNhapXuat.nguonDuLieuBaoCao,
    staleTime: 10 * 60 * 1000,
  });

  const luu = useMutation({
    mutationFn: (giaTri: GiaTriBieuMau) => {
      const duLieu = {
        ...giaTri,
        moTa: giaTri.moTa || null,
        loaiBaoCao: giaTri.loaiBaoCao || null,
        cauHinhCot: cot.map((c, i) => ({ ...c, thuTu: i + 1 })),
        dinhDangXuat,
      };

      return dangSua
        ? apiBieuMauThongKe.sua(dangSua.id, duLieu)
        : apiBieuMauThongKe.them(duLieu);
    },
    onSuccess: () => {
      message.success(dangSua ? 'Đã cập nhật biểu mẫu' : 'Đã thêm biểu mẫu');
      dongForm();
      void queryClient.invalidateQueries({ queryKey: ['bieu-mau-thong-ke'] });
      void queryClient.invalidateQueries({ queryKey: ['bieu-mau-thong-ke-chon'] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không lưu được.'),
  });

  const xoa = useMutation({
    mutationFn: (id: string) => apiBieuMauThongKe.xoa(id),
    onSuccess: () => {
      message.success('Đã xoá biểu mẫu');
      void queryClient.invalidateQueries({ queryKey: ['bieu-mau-thong-ke'] });
      void queryClient.invalidateQueries({ queryKey: ['bieu-mau-thong-ke-chon'] });
    },
    onError: (loi) =>
      modal.error({
        title: 'Không xoá được',
        content: loi instanceof LoiApi ? loi.message : 'Đã xảy ra lỗi.',
      }),
  });

  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  function dongForm() {
    setMoForm(false);
    setDangSua(null);
    setCot([]);
    setDinhDangXuat(['XLSX', 'PDF']);
    form.reset(MAC_DINH_BIEU_MAU);
  }

  function moTaoMoi() {
    dongForm();
    setMoForm(true);
  }

  async function moSua(dong: DanhMucDto) {
    const chiTiet = await apiBieuMauThongKe.theoId(dong.id);

    setDangSua(chiTiet);
    setCot([...(chiTiet.cauHinhCot ?? [])].sort((a, b) => a.thuTu - b.thuTu));
    setDinhDangXuat(chiTiet.dinhDangXuat?.length ? chiTiet.dinhDangXuat : ['XLSX']);
    form.reset({
      ma: chiTiet.ma,
      ten: chiTiet.ten,
      moTa: chiTiet.moTa ?? undefined,
      loaiBaoCao: chiTiet.loaiBaoCao ?? undefined,
      thuTu: chiTiet.thuTu,
      trangThai: chiTiet.trangThai as 0 | 1,
    });
    setMoForm(true);
  }

  function themCot() {
    setCot((cu) => [
      ...cu,
      { ma: '', tieuDe: '', nguon: '', hamTongHop: null, thuTu: cu.length + 1 },
    ]);
  }

  function suaCot(chiSo: number, thayDoi: Partial<CotBaoCao>) {
    setCot((cu) => cu.map((c, i) => (i === chiSo ? { ...c, ...thayDoi } : c)));
  }

  /** Đổi chỗ hai cột liền kề — thứ tự cột chính là thứ tự in ra Excel/PDF. */
  function doiCho(chiSo: number, buoc: -1 | 1) {
    const dich = chiSo + buoc;
    if (dich < 0 || dich >= cot.length) return;

    setCot((cu) => {
      const moi = [...cu];
      [moi[chiSo], moi[dich]] = [moi[dich], moi[chiSo]];
      return moi;
    });
  }

  /**
   * Chọn nguồn dữ liệu thì điền sẵn mã cột và tiêu đề nếu người dùng chưa gõ gì.
   *
   * Ba ô này gần như luôn giống nhau, bắt gõ lại ba lần chỉ tạo cơ hội gõ lệch — mà mã cột lệch
   * thì máy chủ báo trùng mã hoặc báo nguồn không hợp lệ, khó lần ra vì sao.
   */
  function chonNguon(chiSo: number, nguon: string) {
    const goiY = nguonDuLieu?.find((n) => n.ma === nguon);

    setCot((cu) =>
      cu.map((c, i) =>
        i === chiSo
          ? {
              ...c,
              nguon,
              ma: c.ma.trim() ? c.ma : nguon,
              tieuDe: c.tieuDe.trim() ? c.tieuDe : (goiY?.tieuDeGoiY ?? nguon),
            }
          : c,
      ),
    );
  }

  return (
    <Card
      title="Danh mục dùng chung"
      extra={
        <Space>
          <Input.Search
            placeholder="Tìm biểu mẫu (không dấu)"
            allowClear
            style={{ width: 260 }}
            onSearch={(v) => {
              setTuKhoa(v);
              setTrang(1);
            }}
          />
          <Button type="primary" icon={<PlusOutlined />} onClick={moTaoMoi}>
            Thêm biểu mẫu
          </Button>
        </Space>
      }
    >
      <DaiTabTrang danhSach={DS_TAB_DANH_MUC} dangChon="bieu-mau-thong-ke" />

      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 12 }}
        message="Biểu mẫu khai ở đây là các lựa chọn của màn hình Báo cáo tuỳ biến."
        description={
          <span>
            Mỗi biểu mẫu là một bộ cột; người dùng chọn biểu mẫu rồi lọc theo đợt, đơn vị, lĩnh vực
            và xuất ra tệp. Xem kết quả tại <Link to="/bao-cao/tuy-bien">Báo cáo → Tuỳ biến</Link>.
          </span>
        }
      />

      <Table<DanhMucDto>
        rowKey="id"
        size="middle"
        loading={isLoading}
        dataSource={data?.duLieu ?? []}
        scroll={{ x: 1000 }}
        columns={[
          { title: 'Mã', dataIndex: 'ma', width: 200 },
          { title: 'Tên biểu mẫu', dataIndex: 'ten', width: 320 },
          { title: 'Mô tả', dataIndex: 'moTa', width: 260, ellipsis: true, responsive: ['lg'] },
          {
            title: 'Trạng thái',
            dataIndex: 'trangThai',
            width: 130,
            render: (v: number) =>
              v === 1 ? <Tag color="success">Hoạt động</Tag> : <Tag>Ngừng</Tag>,
          },
          {
            title: 'Ngày tạo',
            dataIndex: 'ngayTao',
            width: 130,
            responsive: ['xl'],
            render: (v: string) => ngayGio(v, false),
          },
          {
            title: '',
            width: 130,
            fixed: 'right',
            render: (_v, dong) => (
              <Space>
                <Tooltip title="Chạy thử biểu mẫu này">
                  <Link to={`/bao-cao/tuy-bien?bieuMauId=${dong.id}`}>
                    <Button type="text" icon={<BarChartOutlined />} />
                  </Link>
                </Tooltip>
                <Button type="text" icon={<EditOutlined />} onClick={() => void moSua(dong)} />
                <Button
                  type="text"
                  danger
                  icon={<DeleteOutlined />}
                  onClick={() =>
                    modal.confirm({
                      title: 'Xác nhận xoá',
                      content:
                        `Xoá biểu mẫu "${dong.ten}"? Báo cáo tuỳ biến sẽ không còn lựa chọn này.`,
                      okText: 'Xoá',
                      okButtonProps: { danger: true },
                      cancelText: 'Huỷ',
                      onOk: () => xoa.mutateAsync(dong.id),
                    })
                  }
                />
              </Space>
            ),
          },
        ]}
        pagination={{
          current: trang,
          pageSize: soDong,
          total: data?.tongSo ?? 0,
          showSizeChanger: true,
          showTotal: (t) => `Tổng ${t} biểu mẫu`,
          onChange: (t, s) => {
            setTrang(t);
            setSoDong(s);
          },
        }}
      />

      <Modal
        open={moForm}
        width={900}
        title={dangSua ? `Sửa biểu mẫu: ${dangSua.ten}` : 'Thêm biểu mẫu thống kê'}
        okText="Lưu"
        cancelText="Huỷ"
        confirmLoading={luu.isPending || form.formState.isSubmitting}
        onCancel={dongForm}
        okButtonProps={{ htmlType: 'submit', form: 'form-bieu-mau-thong-ke' }}
      >
        <BieuMau
          id="form-bieu-mau-thong-ke"
          form={form}
          onGui={(giaTri) => luu.mutateAsync(giaTri)}
        >
          <Space size="large" wrap style={{ display: 'flex' }}>
            <Truong<GiaTriBieuMau> ten="ma" label="Mã" required>
              {(o) => (
                <Input
                  {...o}
                  value={o.value as string}
                  style={{ width: 240 }}
                  placeholder="VD: BMTK_THEO_DON_VI"
                  disabled={!!dangSua}
                />
              )}
            </Truong>
            <Truong<GiaTriBieuMau> ten="ten" label="Tên biểu mẫu" required>
              {(o) => <Input {...o} value={o.value as string} style={{ width: 420 }} />}
            </Truong>
          </Space>

          <Truong<GiaTriBieuMau> ten="moTa" label="Mô tả">
            {(o) => (
              <Input.TextArea
                {...o}
                value={o.value as string}
                rows={2}
                placeholder="Biểu mẫu này dùng cho báo cáo nào, gửi cho ai"
              />
            )}
          </Truong>

          <Space size="large" wrap style={{ display: 'flex' }}>
            <Truong<GiaTriBieuMau>
              ten="loaiBaoCao"
              label="Loại báo cáo"
              tooltip="Nhãn để nhóm biểu mẫu, không ảnh hưởng tới dữ liệu chạy ra."
            >
              {(o) => (
                <Input
                  {...o}
                  value={o.value as string}
                  style={{ width: 240 }}
                  placeholder="VD: TONG_HOP_NAM"
                />
              )}
            </Truong>
            <Truong<GiaTriBieuMau> ten="thuTu" label="Thứ tự">
              {(o) => <InputNumber {...o} value={o.value as number} min={0} max={9999} />}
            </Truong>
            <Truong<GiaTriBieuMau> ten="trangThai" label="Trạng thái">
              {(o) => (
                <Select
                  {...o}
                  value={o.value as number}
                  style={{ width: 160 }}
                  options={[
                    { value: 1, label: 'Hoạt động' },
                    { value: 0, label: 'Ngừng' },
                  ]}
                />
              )}
            </Truong>
          </Space>

          <Typography.Text strong style={{ display: 'block', marginBottom: 4 }}>
            Định dạng cho phép xuất
          </Typography.Text>
          <Select
            mode="multiple"
            value={dinhDangXuat}
            onChange={setDinhDangXuat}
            options={DINH_DANG_XUAT}
            style={{ width: '100%', marginBottom: 16 }}
          />

          <Typography.Text strong style={{ display: 'block', marginBottom: 4 }}>
            Các cột của báo cáo
          </Typography.Text>

          {cot.length === 0 && (
            <Alert
              type="warning"
              showIcon
              style={{ marginBottom: 8 }}
              message="Báo cáo phải có ít nhất một cột — máy chủ sẽ từ chối lưu nếu để trống."
            />
          )}

          <Table<CotBaoCao>
            rowKey={(_r, i) => String(i)}
            size="small"
            pagination={false}
            dataSource={cot}
            columns={[
              {
                title: 'Nguồn dữ liệu',
                width: 220,
                render: (_v, _dong, i) => (
                  <Select
                    value={cot[i].nguon || undefined}
                    onChange={(v: string) => chonNguon(i, v)}
                    showSearch
                    optionFilterProp="label"
                    placeholder="Chọn trường"
                    style={{ width: '100%' }}
                    options={(nguonDuLieu ?? []).map((n) => ({
                      value: n.ma,
                      label: n.tieuDeGoiY,
                    }))}
                  />
                ),
              },
              {
                title: 'Mã cột',
                width: 170,
                render: (_v, _dong, i) => (
                  <Input
                    value={cot[i].ma}
                    onChange={(e) => suaCot(i, { ma: e.target.value })}
                    placeholder="maHoSo"
                  />
                ),
              },
              {
                title: 'Tiêu đề in ra',
                width: 200,
                render: (_v, _dong, i) => (
                  <Input
                    value={cot[i].tieuDe}
                    onChange={(e) => suaCot(i, { tieuDe: e.target.value })}
                    placeholder="Mã hồ sơ"
                  />
                ),
              },
              {
                title: 'Tổng hợp',
                width: 200,
                render: (_v, _dong, i) => (
                  <Select
                    value={cot[i].hamTongHop ?? ''}
                    onChange={(v: string) => suaCot(i, { hamTongHop: v || null })}
                    style={{ width: '100%' }}
                    options={HAM_TONG_HOP}
                  />
                ),
              },
              {
                title: '',
                width: 110,
                render: (_v, _dong, i) => (
                  <Space size={0}>
                    <Button
                      type="text"
                      size="small"
                      icon={<ArrowUpOutlined />}
                      disabled={i === 0}
                      onClick={() => doiCho(i, -1)}
                    />
                    <Button
                      type="text"
                      size="small"
                      icon={<ArrowDownOutlined />}
                      disabled={i === cot.length - 1}
                      onClick={() => doiCho(i, 1)}
                    />
                    <Button
                      type="text"
                      size="small"
                      danger
                      icon={<DeleteOutlined />}
                      onClick={() => setCot((cu) => cu.filter((_, j) => j !== i))}
                    />
                  </Space>
                ),
              },
            ]}
          />

          <Button
            type="dashed"
            block
            icon={<PlusOutlined />}
            style={{ marginTop: 8 }}
            onClick={themCot}
          >
            Thêm cột
          </Button>
        </BieuMau>
      </Modal>
    </Card>
  );
}
