import { useState } from 'react';
import {
  App,
  Alert,
  Button,
  Card,
  Descriptions,
  Input,
  InputNumber,
  Modal,
  Select,
  Space,
  Table,
  Tag,
  Tooltip,
  Typography,
  Upload,
} from 'antd';
import {
  DeleteOutlined,
  EditOutlined,
  EyeOutlined,
  FileWordOutlined,
  PlusOutlined,
  UploadOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { z } from 'zod';

import { LoiApi, taiTep } from '@/api/client';
import { BieuMau, Truong, useBieuMau } from '@/components/bieu-mau/BieuMau';
import { batBuoc, maDanhMuc, soNguyen, trangThai, tuyChon } from '@/components/bieu-mau/luat';
import {
  apiBieuMauXuat,
  apiNhapXuat,
  taiTepLen,
  type BieuMauXuat,
  type DanhMucDto,
  type TruongBieuMau,
} from '@/api/endpoints';
import { KhoiLoi, ngayGio } from '@/components/ThanhPhanChung';
import { DaiTabTrang } from '@/components/DaiTabTrang';
import { DS_TAB_DANH_MUC } from '@/features/quan-tri/danhMucTab';

/** Luật kiểm tra biểu mẫu xuất. */
const luatBieuMau = z.object({
  ma: maDanhMuc(),
  ten: batBuoc('Tên biểu mẫu'),
  moTa: tuyChon(),
  loai: z.string(),
  dinhDang: z.string(),
  thuTu: soNguyen('Thứ tự', 0, 9999),
  trangThai: trangThai,
});

type GiaTriBieuMau = z.infer<typeof luatBieuMau>;

const MAC_DINH_BIEU_MAU: GiaTriBieuMau = {
  ma: '',
  ten: '',
  loai: 'KHAC',
  dinhDang: 'DOCX',
  thuTu: 0,
  trangThai: 1,
};

const LOAI_BIEU_MAU = [
  { value: 'PHIEU_TIEP_NHAN', label: 'Phiếu tiếp nhận' },
  { value: 'PHIEU_DANH_GIA', label: 'Phiếu đánh giá' },
  { value: 'BIEN_BAN_HOP', label: 'Biên bản họp' },
  { value: 'QUYET_DINH', label: 'Quyết định' },
  { value: 'TONG_HOP', label: 'Tổng hợp' },
  { value: 'KHAC', label: 'Khác' },
];

const KIEU_TRUONG = [
  { value: 'text', label: 'Văn bản' },
  { value: 'number', label: 'Số' },
  { value: 'date', label: 'Ngày' },
  { value: 'table', label: 'Bảng' },
];

/** Chức năng 6 — Biểu mẫu xuất: tệp mẫu .docx và ánh xạ placeholder sang nguồn dữ liệu. */
export default function TrangBieuMauXuat() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [trang, setTrang] = useState(1);
  const [soDong, setSoDong] = useState(20);
  const [tuKhoa, setTuKhoa] = useState('');
  const [dangSua, setDangSua] = useState<BieuMauXuat | null>(null);
  const [moForm, setMoForm] = useState(false);
  const [fileTemplateId, setFileTemplateId] = useState<string | null>(null);
  const [tenTepMau, setTenTepMau] = useState<string | null>(null);
  const [truong, setTruong] = useState<TruongBieuMau[]>([]);
  const [canhBaoQuet, setCanhBaoQuet] = useState<string | null>(null);
  const [dangQuet, setDangQuet] = useState(false);
  const form = useBieuMau(luatBieuMau, MAC_DINH_BIEU_MAU);

  const thamSo = { trang, soDong, tuKhoa };

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['bieu-mau-xuat', thamSo],
    queryFn: () => apiBieuMauXuat.danhSach(thamSo),
  });

  const { data: nguonDuLieu } = useQuery({
    queryKey: ['nguon-du-lieu-bao-cao'],
    queryFn: apiNhapXuat.nguonDuLieuBaoCao,
  });

  const luu = useMutation({
    mutationFn: async (giaTri: GiaTriBieuMau) => {
      const duLieu = {
        ma: giaTri.ma as string,
        ten: giaTri.ten as string,
        moTa: (giaTri.moTa as string) ?? null,
        thuTu: (giaTri.thuTu as number) ?? 0,
        trangThai: (giaTri.trangThai as number) ?? 1,
        loai: giaTri.loai as string,
        dinhDang: giaTri.dinhDang as string,
        fileTemplateId,
        cauHinhTruong: truong,
      };

      return dangSua ? apiBieuMauXuat.sua(dangSua.id, duLieu) : apiBieuMauXuat.them(duLieu);
    },
    onSuccess: () => {
      message.success(dangSua ? 'Đã cập nhật biểu mẫu' : 'Đã thêm biểu mẫu');
      dongForm();
      void queryClient.invalidateQueries({ queryKey: ['bieu-mau-xuat'] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không lưu được.'),
  });

  /**
   * Xem truoc bang du lieu MAU, khong doi hoi phai co ho so that: nguoi cau hinh bieu mau thuong
   * lam truoc khi he thong co du lieu, bat ho di tim mot ho so de thu la vo ly.
   */
  const xemTruoc = useMutation({
    mutationFn: (id: string) => apiBieuMauXuat.xemTruoc(id),
    onSuccess: (kq, id) =>
      modal.info({
        title: `Xem trước: ${kq.tieuDe}`,
        width: 720,
        content: (
          <div style={{ marginTop: 12 }}>
            {kq.truongThieu.length > 0 && (
              <Alert
                type="warning"
                showIcon
                style={{ marginBottom: 12 }}
                message="Có placeholder trỏ tới nguồn dữ liệu không tồn tại"
                description={`Sẽ in ra ô trống: ${kq.truongThieu.join(', ')}`}
              />
            )}
            <Descriptions bordered size="small" column={1}>
              {kq.dongDuLieu.map((d, i) => (
                <Descriptions.Item key={`${d.nhan}-${i}`} label={d.nhan}>
                  {d.giaTri || <Typography.Text type="secondary">(để trống)</Typography.Text>}
                </Descriptions.Item>
              ))}
            </Descriptions>
            <Button
              type="link"
              style={{ paddingLeft: 0, marginTop: 8 }}
              onClick={() =>
                void taiTep(apiBieuMauXuat.duongDanXemTruocPdf(id), 'xem-truoc-bieu-mau.pdf')
              }
            >
              Tải bản PDF xem trước
            </Button>
          </div>
        ),
      }),
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không xem trước được.'),
  });

  const xoa = useMutation({
    mutationFn: (id: string) => apiBieuMauXuat.xoa(id),
    onSuccess: () => {
      message.success('Đã xoá biểu mẫu');
      void queryClient.invalidateQueries({ queryKey: ['bieu-mau-xuat'] });
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
    setFileTemplateId(null);
    setTenTepMau(null);
    setTruong([]);
    setCanhBaoQuet(null);
    form.reset(MAC_DINH_BIEU_MAU);
  }

  function moTaoMoi() {
    dongForm();
    setMoForm(true);
  }

  async function moSua(dong: DanhMucDto) {
    const chiTiet = await apiBieuMauXuat.theoId(dong.id);

    setDangSua(chiTiet);
    setFileTemplateId(chiTiet.fileTemplateId ?? null);
    setTenTepMau(chiTiet.fileTemplateId ? 'Tệp mẫu đã tải lên trước đó' : null);
    setTruong(chiTiet.cauHinhTruong ?? []);
    setCanhBaoQuet(null);
    form.reset({
      ma: chiTiet.ma,
      ten: chiTiet.ten,
      moTa: chiTiet.moTa ?? undefined,
      loai: chiTiet.loai,
      dinhDang: chiTiet.dinhDang,
      thuTu: chiTiet.thuTu,
      trangThai: chiTiet.trangThai as 0 | 1,
    });
    setMoForm(true);
  }

  /**
   * Tải tệp mẫu lên rồi quét placeholder ngay trên cùng một tệp.
   *
   * Quét sinh dòng ánh xạ cho placeholder MỚI và giữ nguyên ánh xạ đã cấu hình — đổi tệp mẫu
   * không được xoá công sức ánh xạ của lần trước.
   */
  async function taiVaQuet(tep: File) {
    setDangQuet(true);
    try {
      const [daTaiLen, ketQua] = await Promise.all([
        taiTepLen(tep),
        apiNhapXuat.quetPlaceholder(tep),
      ]);

      setFileTemplateId(daTaiLen.id);
      setTenTepMau(daTaiLen.tenGoc);
      setCanhBaoQuet(ketQua.canhBao ?? null);

      setTruong((cu) => {
        const daCo = new Map(cu.map((x) => [x.placeholder, x]));

        return ketQua.placeholder.map(
          (p) =>
            daCo.get(p) ?? {
              placeholder: p,
              nguon: ketQua.nguonGoiY.find((n) => n.ma === p)?.ma ?? '',
              kieu: 'text',
              cot: null,
              dinhDangHienThi: null,
            },
        );
      });

      message.success(
        `Đã tải tệp mẫu và tìm thấy ${ketQua.placeholder.length} placeholder trong ${ketQua.soDoanVan} đoạn văn.`,
      );
    } catch (loi) {
      message.error(loi instanceof LoiApi ? loi.message : 'Không đọc được tệp mẫu.');
    } finally {
      setDangQuet(false);
    }
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
      <DaiTabTrang danhSach={DS_TAB_DANH_MUC} dangChon="bieu-mau-xuat" />

      <Table<DanhMucDto>
        rowKey="id"
        size="middle"
        loading={isLoading}
        dataSource={data?.duLieu ?? []}
        scroll={{ x: 1150 }}
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
            width: 140,
            fixed: 'right',
            render: (_v, dong) => (
              <Space>
                <Tooltip title="Xem trước bố cục với dữ liệu mẫu">
                  <Button
                    type="text"
                    icon={<EyeOutlined />}
                    loading={xemTruoc.isPending && xemTruoc.variables === dong.id}
                    onClick={() => xemTruoc.mutate(dong.id)}
                  />
                </Tooltip>
                <Button type="text" icon={<EditOutlined />} onClick={() => void moSua(dong)} />
                <Button
                  type="text"
                  danger
                  icon={<DeleteOutlined />}
                  onClick={() =>
                    modal.confirm({
                      title: 'Xác nhận xoá',
                      content: `Xoá biểu mẫu "${dong.ten}"?`,
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
        width={820}
        title={dangSua ? `Sửa biểu mẫu: ${dangSua.ten}` : 'Thêm biểu mẫu xuất'}
        okText="Lưu"
        cancelText="Huỷ"
        confirmLoading={luu.isPending || form.formState.isSubmitting}
        onCancel={dongForm}
        okButtonProps={{ htmlType: 'submit', form: 'form-bieu-mau' }}
      >
        <BieuMau id="form-bieu-mau" form={form} onGui={(giaTri) => luu.mutateAsync(giaTri)}>
          <Space size="large" wrap style={{ display: 'flex' }}>
            <Truong<GiaTriBieuMau> ten="ma" label="Mã" required>
              {(o) => (
                <Input
                  {...o}
                  value={o.value as string}
                  style={{ width: 240 }}
                  placeholder="VD: BM_QUYET_DINH"
                  disabled={!!dangSua}
                />
              )}
            </Truong>
            <Truong<GiaTriBieuMau> ten="ten" label="Tên biểu mẫu" required>
              {(o) => <Input {...o} value={o.value as string} style={{ width: 400 }} />}
            </Truong>
          </Space>

          <Space size="large" wrap style={{ display: 'flex' }}>
            <Truong<GiaTriBieuMau> ten="loai" label="Loại biểu mẫu">
              {(o) => (
                <Select
                  {...o}
                  value={o.value as string}
                  style={{ width: 220 }}
                  options={LOAI_BIEU_MAU}
                />
              )}
            </Truong>
            <Truong<GiaTriBieuMau> ten="dinhDang" label="Định dạng xuất">
              {(o) => (
                <Select
                  {...o}
                  value={o.value as string}
                  style={{ width: 160 }}
                  options={[
                    { value: 'DOCX', label: 'Word (.docx)' },
                    { value: 'XLSX', label: 'Excel (.xlsx)' },
                    { value: 'PDF', label: 'PDF' },
                  ]}
                />
              )}
            </Truong>
            <Truong<GiaTriBieuMau> ten="thuTu" label="Thứ tự">
              {(o) => (
                <InputNumber {...o} value={o.value as number} min={0} style={{ width: 120 }} />
              )}
            </Truong>
            <Truong<GiaTriBieuMau> ten="trangThai" label="Trạng thái">
              {(o) => (
                <Select
                  {...o}
                  value={o.value as number}
                  style={{ width: 170 }}
                  options={[
                    { value: 1, label: 'Hoạt động' },
                    { value: 0, label: 'Ngừng' },
                  ]}
                />
              )}
            </Truong>
          </Space>

          <Truong<GiaTriBieuMau> ten="moTa" label="Mô tả">
            {(o) => <Input.TextArea {...o} value={o.value as string} rows={2} />}
          </Truong>
        </BieuMau>

        <Typography.Text strong>Tệp mẫu và ánh xạ placeholder</Typography.Text>
        <Typography.Paragraph type="secondary" style={{ fontSize: 12, marginBottom: 8 }}>
          Tải tệp <code>.docx</code> có placeholder dạng <code>{'{{ ten_truong }}'}</code>. Hệ thống
          gộp văn bản cả đoạn nên vẫn bắt được placeholder bị Word cắt thành nhiều đoạn nhỏ.
        </Typography.Paragraph>

        <Space wrap style={{ marginBottom: 12 }}>
          <Upload
            accept=".docx"
            maxCount={1}
            showUploadList={false}
            beforeUpload={(tep) => {
              void taiVaQuet(tep as File);
              return false;
            }}
          >
            <Button icon={<UploadOutlined />} loading={dangQuet}>
              Tải tệp mẫu và quét placeholder
            </Button>
          </Upload>

          {tenTepMau && (
            <Tag icon={<FileWordOutlined />} color="blue">
              {tenTepMau}
            </Tag>
          )}
        </Space>

        {canhBaoQuet && (
          <Alert type="warning" showIcon style={{ marginBottom: 12 }} message={canhBaoQuet} />
        )}

        <Table<TruongBieuMau>
          rowKey="placeholder"
          size="small"
          dataSource={truong}
          pagination={false}
          locale={{ emptyText: 'Chưa có placeholder nào — hãy tải tệp mẫu lên.' }}
          columns={[
            { title: 'Placeholder', dataIndex: 'placeholder', width: 220 },
            {
              title: 'Nguồn dữ liệu',
              dataIndex: 'nguon',
              render: (v: string, dong) => (
                <Select
                  style={{ width: '100%' }}
                  showSearch
                  allowClear
                  placeholder="Chọn trường dữ liệu"
                  value={v || undefined}
                  optionFilterProp="label"
                  options={(nguonDuLieu ?? []).map((n) => ({
                    value: n.ma,
                    label: `${n.ma} — ${n.tieuDeGoiY}`,
                  }))}
                  onChange={(giaTri) =>
                    setTruong((cu) =>
                      cu.map((x) =>
                        x.placeholder === dong.placeholder ? { ...x, nguon: giaTri ?? '' } : x,
                      ),
                    )
                  }
                />
              ),
            },
            {
              title: 'Kiểu',
              dataIndex: 'kieu',
              width: 140,
              render: (v: string, dong) => (
                <Select
                  style={{ width: '100%' }}
                  value={v}
                  options={KIEU_TRUONG}
                  onChange={(giaTri) =>
                    setTruong((cu) =>
                      cu.map((x) =>
                        x.placeholder === dong.placeholder ? { ...x, kieu: giaTri } : x,
                      ),
                    )
                  }
                />
              ),
            },
            {
              title: 'Định dạng hiển thị',
              dataIndex: 'dinhDangHienThi',
              width: 170,
              render: (v: string | null, dong) => (
                <Input
                  placeholder="VD: dd/MM/yyyy"
                  value={v ?? ''}
                  onChange={(e) =>
                    setTruong((cu) =>
                      cu.map((x) =>
                        x.placeholder === dong.placeholder
                          ? { ...x, dinhDangHienThi: e.target.value || null }
                          : x,
                      ),
                    )
                  }
                />
              ),
            },
            {
              title: '',
              width: 50,
              render: (_v, dong) => (
                <Button
                  type="text"
                  danger
                  icon={<DeleteOutlined />}
                  onClick={() =>
                    setTruong((cu) => cu.filter((x) => x.placeholder !== dong.placeholder))
                  }
                />
              ),
            },
          ]}
        />
      </Modal>
    </Card>
  );
}
