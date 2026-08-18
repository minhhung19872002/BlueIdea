import { useState } from 'react';
import {
  App,
  Alert,
  Button,
  Card,
  Form,
  Input,
  InputNumber,
  Modal,
  Select,
  Space,
  Table,
  Tag,
  Typography,
  Upload,
} from 'antd';
import {
  DeleteOutlined,
  EditOutlined,
  FileWordOutlined,
  PlusOutlined,
  UploadOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { LoiApi } from '@/api/client';
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
  const [form] = Form.useForm();

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
    mutationFn: async (giaTri: Record<string, unknown>) => {
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
    form.resetFields();
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
    form.setFieldsValue(chiTiet);
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
        scroll={{ x: 800 }}
        columns={[
          { title: 'Mã', dataIndex: 'ma', width: 200 },
          { title: 'Tên biểu mẫu', dataIndex: 'ten' },
          { title: 'Mô tả', dataIndex: 'moTa', responsive: ['lg'] },
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
            width: 100,
            fixed: 'right',
            render: (_v, dong) => (
              <Space>
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
        confirmLoading={luu.isPending}
        onCancel={dongForm}
        onOk={async () => luu.mutate(await form.validateFields())}
      >
        <Form
          form={form}
          layout="vertical"
          initialValues={{ trangThai: 1, thuTu: 0, loai: 'KHAC', dinhDang: 'DOCX' }}
        >
          <Space size="large" wrap style={{ display: 'flex' }}>
            <Form.Item
              name="ma"
              label="Mã"
              rules={[
                { required: true, message: 'Nhập mã' },
                { pattern: /^[A-Z0-9_-]+$/, message: 'Mã chỉ gồm chữ hoa, số, dấu _ và -' },
              ]}
            >
              <Input style={{ width: 240 }} placeholder="VD: BM_QUYET_DINH" disabled={!!dangSua} />
            </Form.Item>
            <Form.Item name="ten" label="Tên biểu mẫu" rules={[{ required: true, message: 'Nhập tên' }]}>
              <Input style={{ width: 400 }} />
            </Form.Item>
          </Space>

          <Space size="large" wrap style={{ display: 'flex' }}>
            <Form.Item name="loai" label="Loại biểu mẫu">
              <Select style={{ width: 220 }} options={LOAI_BIEU_MAU} />
            </Form.Item>
            <Form.Item name="dinhDang" label="Định dạng xuất">
              <Select
                style={{ width: 160 }}
                options={[
                  { value: 'DOCX', label: 'Word (.docx)' },
                  { value: 'XLSX', label: 'Excel (.xlsx)' },
                  { value: 'PDF', label: 'PDF' },
                ]}
              />
            </Form.Item>
            <Form.Item name="thuTu" label="Thứ tự">
              <InputNumber min={0} style={{ width: 120 }} />
            </Form.Item>
            <Form.Item name="trangThai" label="Trạng thái">
              <Select
                style={{ width: 170 }}
                options={[
                  { value: 1, label: 'Hoạt động' },
                  { value: 0, label: 'Ngừng' },
                ]}
              />
            </Form.Item>
          </Space>

          <Form.Item name="moTa" label="Mô tả">
            <Input.TextArea rows={2} />
          </Form.Item>
        </Form>

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
