import { useState } from 'react';
import { useParams } from 'react-router-dom';
import {
  App,
  Button,
  Card,
  Input,
  InputNumber,
  Modal,
  Select,
  Space,
  Switch,
  Table,
  Tag,
} from 'antd';
import {
  ArrowDownOutlined,
  ArrowUpOutlined,
  DeleteOutlined,
  EditOutlined,
  ImportOutlined,
  PlusOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { z } from 'zod';

import { LoiApi } from '@/api/client';
import { BieuMau, Truong, useBieuMau } from '@/components/bieu-mau/BieuMau';
import { batBuoc, maDanhMuc, soNguyen, trangThai, tuyChon } from '@/components/bieu-mau/luat';
import {
  apiDoiTuong,
  apiDotDeNghi,
  apiLinhVuc,
  apiLoaiTacGia,
  type DanhMucDto,
  type PhanHoiPhanTrang,
  type ThamSoLoc,
} from '@/api/endpoints';
import { KhoiKhongNhanRa, KhoiLoi, ngayGio } from '@/components/ThanhPhanChung';
import { BocKeoTha, DongKeoTha } from '@/components/DongKeoTha';
import { HopThoaiNhapDanhMuc } from './HopThoaiNhapDanhMuc';
import { DaiTabTrang } from '@/components/DaiTabTrang';
import { DS_TAB_DANH_MUC } from '@/features/quan-tri/danhMucTab';

/**
 * Luật kiểm tra của form danh mục.
 *
 * Đặt cạnh màn hình dùng nó chứ không gom vào một tệp chung: mỗi danh mục có thêm vài trường
 * riêng, gom hết một chỗ sẽ thành một schema khổng lồ mà trường nào cũng phải cho phép vắng mặt.
 */
const luatDanhMuc = z.object({
  ma: maDanhMuc(),
  ten: batBuoc('Tên'),
  moTa: tuyChon(),
  thuTu: soNguyen('Thứ tự', 0, 9999).optional(),
  trangThai: trangThai,

  // Trường riêng của từng danh mục — không áp dụng thì để trống.
  linhVucChaId: z.string().uuid().optional().nullable(),
  choPhepNhieuTacGia: z.boolean().optional(),
  soTacGiaToiDa: soNguyen('Số tác giả tối đa', 1, 50).optional(),
  nam: soNguyen('Năm', 2000, 2100).optional(),
  capXetDuyet: z.string().optional(),
});

type GiaTriDanhMuc = z.infer<typeof luatDanhMuc>;

/** Các danh mục nhập được bằng tệp Excel — phải khớp DichVuNhapDanhMuc.LoaiHoTro ở máy chủ. */
const NHAP_DUOC = ['linh-vuc', 'doi-tuong', 'loai-tac-gia'];

/** Tập hàm tối thiểu mà mọi API danh mục đều có (một số danh mục có thêm hàm riêng). */
interface ApiDanhMucCoBan {
  danhSach: (thamSo?: ThamSoLoc) => Promise<PhanHoiPhanTrang<DanhMucDto>>;
  chon: () => Promise<DanhMucDto[]>;
  theoId: (id: string) => Promise<DanhMucDto>;
  them: (duLieu: Record<string, unknown>) => Promise<DanhMucDto>;
  sua: (id: string, duLieu: Record<string, unknown>) => Promise<DanhMucDto>;
  xoa: (id: string) => Promise<void>;
}

const CAU_HINH: Record<
  string,
  { tieuDe: string; api: ApiDanhMucCoBan; truongThem?: 'LINH_VUC' | 'LOAI_TAC_GIA' | 'DOT' }
> = {
  'linh-vuc': { tieuDe: 'Danh mục lĩnh vực', api: apiLinhVuc, truongThem: 'LINH_VUC' },
  'doi-tuong': { tieuDe: 'Danh mục đối tượng áp dụng', api: apiDoiTuong },
  'loai-tac-gia': { tieuDe: 'Danh mục loại tác giả', api: apiLoaiTacGia, truongThem: 'LOAI_TAC_GIA' },
  'dot-de-nghi': { tieuDe: 'Danh mục đợt đề nghị', api: apiDotDeNghi, truongThem: 'DOT' },
};


/** Chức năng 1–4 — Màn hình quản trị danh mục dùng chung. */
export default function TrangDanhMuc() {
  const { ma = 'linh-vuc' } = useParams<{ ma: string }>();

  /*
   * Mã không nằm trong bảng thì báo hẳn ra ở cuối hàm, KHÔNG lặng lẽ hiện danh mục lĩnh vực.
   *
   * Rơi về mặc định nghĩa là gõ sai địa chỉ — hoặc vào một mục chưa dựng màn hình — lại hiện ra
   * "Danh mục lĩnh vực" trông hoàn toàn bình thường, và người dùng tưởng mình đang xem đúng thứ
   * vừa chọn. Đây cũng là thứ đã che một chức năng thiếu suốt nhiều lần rà soát.
   *
   * Vẫn lấy cấu hình mặc định để các hook bên dưới chạy đủ và đúng thứ tự: thoát sớm ở đây thì
   * lần đổi mã từ hợp lệ sang không hợp lệ sẽ khiến React dựng ít hook hơn lần trước và vỡ.
   */
  const nhanRa = Object.prototype.hasOwnProperty.call(CAU_HINH, ma);
  const cauHinh = CAU_HINH[ma] ?? CAU_HINH['linh-vuc'];

  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [trang, setTrang] = useState(1);
  const [soDong, setSoDong] = useState(20);
  const [tuKhoa, setTuKhoa] = useState('');
  const [dangSua, setDangSua] = useState<DanhMucDto | null>(null);
  const [moForm, setMoForm] = useState(false);
  const [moNhap, setMoNhap] = useState(false);

  const form = useBieuMau(luatDanhMuc, { trangThai: 1, thuTu: 0 } as GiaTriDanhMuc);

  const thamSo = { trang, soDong, tuKhoa };

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['danh-muc', ma, thamSo],
    queryFn: () => cauHinh.api.danhSach(thamSo),
    enabled: nhanRa,
  });

  const luu = useMutation({
    mutationFn: async (giaTri: Record<string, unknown>) =>
      dangSua ? cauHinh.api.sua(dangSua.id, giaTri) : cauHinh.api.them(giaTri),
    onSuccess: () => {
      message.success(dangSua ? 'Đã cập nhật' : 'Đã thêm mới');
      setMoForm(false);
      setDangSua(null);
      form.reset({ trangThai: 1, thuTu: 0 } as GiaTriDanhMuc);
      void queryClient.invalidateQueries({ queryKey: ['danh-muc', ma] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không lưu được.'),
  });

  /**
   * Chức năng 1 — đổi thứ tự lĩnh vực.
   *
   * Có CẢ kéo–thả lẫn nút lên/xuống: kéo–thả nhanh hơn khi sắp lại nhiều dòng, nhưng trong bảng
   * cuộn ngang trên điện thoại thì gần như không dùng nổi, nên nút lên/xuống vẫn phải giữ.
   */
  const sapXep = useMutation({
    mutationFn: (idTheoThuTu: string[]) => apiLinhVuc.sapXep(idTheoThuTu),
    onSuccess: () => {
      message.success('Đã lưu thứ tự mới');
      void queryClient.invalidateQueries({ queryKey: ['danh-muc', ma] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không lưu được thứ tự.'),
  });

  const xoa = useMutation({
    mutationFn: (id: string) => cauHinh.api.xoa(id),
    onSuccess: () => {
      message.success('Đã xóa');
      void queryClient.invalidateQueries({ queryKey: ['danh-muc', ma] });
    },
    onError: (loi) =>
      modal.error({
        title: 'Không thể xóa',
        content: loi instanceof LoiApi ? loi.message : 'Đã xảy ra lỗi.',
      }),
  });

  if (!nhanRa) {
    return (
      <KhoiKhongNhanRa
        ma={ma}
        hopLe={Object.entries(CAU_HINH).map(([k, v]) => ({ ma: k, ten: v.tieuDe }))}
        duongDanGoc="/quan-tri/danh-muc"
      />
    );
  }

  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  const choPhepSapXep = ma === 'linh-vuc';
  const danhSach = data?.duLieu ?? [];

  // Bảng chỉ có Id lĩnh vực cha; tra ngược sang tên để người xem đọc được ngay trên danh sách.
  const tenTheoId = new Map(danhSach.map((x) => [x.id, x.ten]));

  /** Đổi chỗ hai dòng liền kề rồi gửi luôn thứ tự mới của cả trang. */
  function doiCho(chiSo: number, huong: -1 | 1) {
    const dich = chiSo + huong;
    if (dich < 0 || dich >= danhSach.length) return;

    const moi = [...danhSach];
    [moi[chiSo], moi[dich]] = [moi[dich], moi[chiSo]];

    sapXep.mutate(moi.map((x) => x.id));
  }

  return (
    <Card
      title="Danh mục dùng chung"
      extra={
        <Space>
          <Input.Search
            placeholder="Tìm kiếm (không dấu)"
            allowClear
            style={{ width: 260 }}
            onSearch={(v) => {
              setTuKhoa(v);
              setTrang(1);
            }}
          />
          {NHAP_DUOC.includes(ma) && (
            <Button icon={<ImportOutlined />} onClick={() => setMoNhap(true)}>
              Nhập Excel
            </Button>
          )}
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => {
              setDangSua(null);
              form.reset({ trangThai: 1, thuTu: 0 } as GiaTriDanhMuc);
              setMoForm(true);
            }}
          >
            Thêm mới
          </Button>
        </Space>
      }
    >
      <DaiTabTrang danhSach={DS_TAB_DANH_MUC} dangChon={ma} />

      <BocKeoTha
        khoaTheoThuTu={danhSach.map((x) => x.id)}
        keoDuoc={choPhepSapXep && !sapXep.isPending}
        onDoiThuTu={(khoaMoi) => sapXep.mutate(khoaMoi)}
      >
      <Table<DanhMucDto>
        rowKey="id"
        size="middle"
        loading={isLoading}
        dataSource={danhSach}
        scroll={{ x: 800 }}
        components={choPhepSapXep ? { body: { row: DongKeoTha } } : undefined}
        columns={[
          ...(choPhepSapXep
            ? [{ key: 'tay-cam', title: '', width: 44, fixed: 'left' as const }]
            : []),
          { title: 'Mã', dataIndex: 'ma', width: 180 },
          { title: 'Tên', dataIndex: 'ten' },
          ...(cauHinh.truongThem === 'LINH_VUC'
            ? [
                {
                  title: 'Thuộc lĩnh vực',
                  dataIndex: 'danhMucChaId',
                  width: 200,
                  responsive: ['lg' as const],
                  render: (v: string | null) =>
                    v ? (tenTheoId.get(v) ?? '—') : <Tag>Gốc</Tag>,
                },
              ]
            : []),
          { title: 'Mô tả', dataIndex: 'moTa', responsive: ['lg'] },
          { title: 'Thứ tự', dataIndex: 'thuTu', width: 90, align: 'right' },
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
            width: 150,
            responsive: ['xl'],
            render: (v: string) => ngayGio(v, false),
          },
          {
            title: '',
            width: choPhepSapXep ? 160 : 100,
            fixed: 'right',
            render: (_v, dong, chiSo) => (
              <Space size={0}>
                {choPhepSapXep && (
                  <>
                    <Button
                      type="text"
                      icon={<ArrowUpOutlined />}
                      title="Đưa lên trên"
                      disabled={chiSo === 0 || sapXep.isPending}
                      onClick={() => doiCho(chiSo, -1)}
                    />
                    <Button
                      type="text"
                      icon={<ArrowDownOutlined />}
                      title="Đưa xuống dưới"
                      disabled={chiSo === danhSach.length - 1 || sapXep.isPending}
                      onClick={() => doiCho(chiSo, 1)}
                    />
                  </>
                )}
                <Button
                  type="text"
                  icon={<EditOutlined />}
                  onClick={async () => {
                    const chiTiet = await cauHinh.api.theoId(dong.id);
                    setDangSua(dong);
                    form.reset(chiTiet as unknown as GiaTriDanhMuc);
                    setMoForm(true);
                  }}
                />
                <Button
                  type="text"
                  danger
                  icon={<DeleteOutlined />}
                  onClick={() =>
                    modal.confirm({
                      title: 'Xác nhận xóa',
                      content: `Bạn chắc chắn muốn xóa "${dong.ten}"?`,
                      okText: 'Xóa',
                      okButtonProps: { danger: true },
                      cancelText: 'Hủy',
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
          showTotal: (t) => `Tổng ${t} bản ghi`,
          onChange: (t, s) => {
            setTrang(t);
            setSoDong(s);
          },
        }}
      />
      </BocKeoTha>

      {moNhap && <HopThoaiNhapDanhMuc loai={ma} onDong={() => setMoNhap(false)} />}

      <Modal
        open={moForm}
        title={dangSua ? `Sửa: ${dangSua.ten}` : 'Thêm mới'}
        okText="Lưu"
        cancelText="Hủy"
        confirmLoading={luu.isPending || form.formState.isSubmitting}
        onCancel={() => setMoForm(false)}
        okButtonProps={{ htmlType: 'submit', form: 'form-danh-muc' }}
      >
        <BieuMau id="form-danh-muc" form={form} onGui={(giaTri) => luu.mutateAsync(giaTri)}>
          <Truong<GiaTriDanhMuc> ten="ma" label="Mã" required>
            {(o) => (
              <Input {...o} value={o.value as string} placeholder="VD: GIAO_DUC" disabled={!!dangSua} />
            )}
          </Truong>

          <Truong<GiaTriDanhMuc> ten="ten" label="Tên" required>
            {(o) => <Input {...o} value={o.value as string} />}
          </Truong>

          <Truong<GiaTriDanhMuc> ten="moTa" label="Mô tả">
            {(o) => <Input.TextArea {...o} value={o.value as string} rows={2} />}
          </Truong>

          {cauHinh.truongThem === 'LINH_VUC' && (
            <Truong<GiaTriDanhMuc>
              ten="linhVucChaId"
              label="Thuộc lĩnh vực cấp trên"
              tooltip="Để trống nếu đây là lĩnh vực gốc. Lĩnh vực con dùng để nhóm hồ sơ khi thống kê."
            >
              {(o) => (
                <Select
                  {...o}
                  value={o.value as string | undefined}
                  allowClear
                  showSearch
                  optionFilterProp="label"
                  placeholder="(lĩnh vực gốc)"
                  options={(danhSach ?? [])
                    .filter((x) => x.id !== dangSua?.id)
                    .map((x) => ({ value: x.id, label: `${x.ma} — ${x.ten}` }))}
                />
              )}
            </Truong>
          )}

          {cauHinh.truongThem === 'LOAI_TAC_GIA' && (
            <>
              <Truong<GiaTriDanhMuc> ten="choPhepNhieuTacGia" label="Cho phép nhiều tác giả">
                {(o) => <Switch checked={!!o.value} onChange={o.onChange} />}
              </Truong>
              <Truong<GiaTriDanhMuc> ten="soTacGiaToiDa" label="Số tác giả tối đa">
                {(o) => (
                  <InputNumber
                    {...o}
                    value={o.value as number}
                    min={1}
                    max={50}
                    style={{ width: '100%' }}
                  />
                )}
              </Truong>
            </>
          )}

          {cauHinh.truongThem === 'DOT' && (
            <>
              <Truong<GiaTriDanhMuc> ten="nam" label="Năm" required>
                {(o) => (
                  <InputNumber
                    {...o}
                    value={o.value as number}
                    min={2000}
                    max={2100}
                    style={{ width: '100%' }}
                  />
                )}
              </Truong>
              <Truong<GiaTriDanhMuc> ten="capXetDuyet" label="Cấp xét duyệt">
                {(o) => (
                  <Select
                    {...o}
                    value={o.value as string}
                    options={[
                      { value: 'CO_SO', label: 'Cấp cơ sở' },
                      { value: 'THANH_PHO', label: 'Cấp thành phố' },
                      { value: 'TINH', label: 'Cấp tỉnh' },
                    ]}
                  />
                )}
              </Truong>
            </>
          )}

          <Space style={{ width: '100%' }} size="large">
            <Truong<GiaTriDanhMuc> ten="thuTu" label="Thứ tự">
              {(o) => <InputNumber {...o} value={o.value as number} min={0} />}
            </Truong>
            <Truong<GiaTriDanhMuc> ten="trangThai" label="Trạng thái">
              {(o) => (
                <Select
                  {...o}
                  value={o.value as number}
                  style={{ width: 160 }}
                  options={[
                    { value: 1, label: 'Hoạt động' },
                    { value: 0, label: 'Ngừng hoạt động' },
                  ]}
                />
              )}
            </Truong>
          </Space>
        </BieuMau>
      </Modal>
    </Card>
  );
}
