import { useState } from 'react';
import { useParams } from 'react-router-dom';
import {
  App,
  Button,
  Card,
  Form,
  Input,
  InputNumber,
  Modal,
  Select,
  Space,
  Switch,
  Table,
  Tag,
} from 'antd';
import { DeleteOutlined, EditOutlined, PlusOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { LoiApi } from '@/api/client';
import {
  apiDoiTuong,
  apiDotDeNghi,
  apiLinhVuc,
  apiLoaiTacGia,
  type DanhMucDto,
  type PhanHoiPhanTrang,
  type ThamSoLoc,
} from '@/api/endpoints';
import { KhoiLoi, ngayGio } from '@/components/ThanhPhanChung';
import { DaiTabTrang } from '@/components/DaiTabTrang';

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

/** Các nhánh con của trang — thiết kế gộp thành một mục ở thanh điều hướng. */
const DS_TAB = [
  { ma: 'linh-vuc', ten: 'Lĩnh vực', duongDan: '/quan-tri/danh-muc/linh-vuc' },
  { ma: 'doi-tuong', ten: 'Đối tượng', duongDan: '/quan-tri/danh-muc/doi-tuong' },
  { ma: 'loai-tac-gia', ten: 'Loại tác giả', duongDan: '/quan-tri/danh-muc/loai-tac-gia' },
  { ma: 'dot-de-nghi', ten: 'Đợt đề nghị', duongDan: '/quan-tri/danh-muc/dot-de-nghi' },
];

/** Chức năng 1–4 — Màn hình quản trị danh mục dùng chung. */
export default function TrangDanhMuc() {
  const { ma = 'linh-vuc' } = useParams<{ ma: string }>();
  const cauHinh = CAU_HINH[ma] ?? CAU_HINH['linh-vuc'];

  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [trang, setTrang] = useState(1);
  const [soDong, setSoDong] = useState(20);
  const [tuKhoa, setTuKhoa] = useState('');
  const [dangSua, setDangSua] = useState<DanhMucDto | null>(null);
  const [moForm, setMoForm] = useState(false);
  const [form] = Form.useForm();

  const thamSo = { trang, soDong, tuKhoa };

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['danh-muc', ma, thamSo],
    queryFn: () => cauHinh.api.danhSach(thamSo),
  });

  const luu = useMutation({
    mutationFn: async (giaTri: Record<string, unknown>) =>
      dangSua ? cauHinh.api.sua(dangSua.id, giaTri) : cauHinh.api.them(giaTri),
    onSuccess: () => {
      message.success(dangSua ? 'Đã cập nhật' : 'Đã thêm mới');
      setMoForm(false);
      setDangSua(null);
      form.resetFields();
      void queryClient.invalidateQueries({ queryKey: ['danh-muc', ma] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không lưu được.'),
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

  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

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
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => {
              setDangSua(null);
              form.resetFields();
              setMoForm(true);
            }}
          >
            Thêm mới
          </Button>
        </Space>
      }
    >
      <DaiTabTrang danhSach={DS_TAB} dangChon={ma} />

      <Table<DanhMucDto>
        rowKey="id"
        size="middle"
        loading={isLoading}
        dataSource={data?.duLieu ?? []}
        scroll={{ x: 800 }}
        columns={[
          { title: 'Mã', dataIndex: 'ma', width: 180 },
          { title: 'Tên', dataIndex: 'ten' },
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
            width: 100,
            fixed: 'right',
            render: (_v, dong) => (
              <Space>
                <Button
                  type="text"
                  icon={<EditOutlined />}
                  onClick={async () => {
                    const chiTiet = await cauHinh.api.theoId(dong.id);
                    setDangSua(dong);
                    form.setFieldsValue(chiTiet as unknown as Record<string, unknown>);
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

      <Modal
        open={moForm}
        title={dangSua ? `Sửa: ${dangSua.ten}` : 'Thêm mới'}
        okText="Lưu"
        cancelText="Hủy"
        confirmLoading={luu.isPending}
        onCancel={() => setMoForm(false)}
        onOk={async () => {
          const giaTri = await form.validateFields();
          luu.mutate(giaTri);
        }}
      >
        <Form form={form} layout="vertical" initialValues={{ trangThai: 1, thuTu: 0 }}>
          <Form.Item
            name="ma"
            label="Mã"
            rules={[
              { required: true, message: 'Vui lòng nhập mã' },
              { pattern: /^[A-Z0-9_-]+$/, message: 'Mã chỉ gồm chữ hoa, số, dấu _ và -' },
            ]}
          >
            <Input placeholder="VD: GIAO_DUC" disabled={!!dangSua} />
          </Form.Item>

          <Form.Item name="ten" label="Tên" rules={[{ required: true, message: 'Vui lòng nhập tên' }]}>
            <Input />
          </Form.Item>

          <Form.Item name="moTa" label="Mô tả">
            <Input.TextArea rows={2} />
          </Form.Item>

          {cauHinh.truongThem === 'LOAI_TAC_GIA' && (
            <>
              <Form.Item
                name="choPhepNhieuTacGia"
                label="Cho phép nhiều tác giả"
                valuePropName="checked"
              >
                <Switch />
              </Form.Item>
              <Form.Item name="soTacGiaToiDa" label="Số tác giả tối đa">
                <InputNumber min={1} max={50} style={{ width: '100%' }} />
              </Form.Item>
            </>
          )}

          {cauHinh.truongThem === 'DOT' && (
            <>
              <Form.Item name="nam" label="Năm" rules={[{ required: true, message: 'Nhập năm' }]}>
                <InputNumber min={2000} max={2100} style={{ width: '100%' }} />
              </Form.Item>
              <Form.Item name="capXetDuyet" label="Cấp xét duyệt">
                <Select
                  options={[
                    { value: 'CO_SO', label: 'Cấp cơ sở' },
                    { value: 'THANH_PHO', label: 'Cấp thành phố' },
                    { value: 'TINH', label: 'Cấp tỉnh' },
                  ]}
                />
              </Form.Item>
            </>
          )}

          <Space style={{ width: '100%' }} size="large">
            <Form.Item name="thuTu" label="Thứ tự">
              <InputNumber min={0} />
            </Form.Item>
            <Form.Item name="trangThai" label="Trạng thái">
              <Select
                style={{ width: 160 }}
                options={[
                  { value: 1, label: 'Hoạt động' },
                  { value: 0, label: 'Ngừng hoạt động' },
                ]}
              />
            </Form.Item>
          </Space>
        </Form>
      </Modal>
    </Card>
  );
}
