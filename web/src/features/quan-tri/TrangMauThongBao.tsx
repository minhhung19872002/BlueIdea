import { useState } from 'react';
import {
  App,
  Alert,
  Button,
  Card,
  Form,
  Input,
  Modal,
  Select,
  Space,
  Table,
  Tag,
  Typography,
} from 'antd';
import { DeleteOutlined, EditOutlined, EyeOutlined, PlusOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { LoiApi } from '@/api/client';
import { apiMauThongBao, type MauThongBao } from '@/api/endpoints';
import { KhoiLoi, KhoiRong } from '@/components/ThanhPhanChung';
import { DaiTabTrang } from '@/components/DaiTabTrang';
import { DS_TAB_CAU_HINH } from './cauHinhTab';

const KENH = [
  { value: 'TAT_CA', label: 'Tất cả kênh' },
  { value: 'EMAIL', label: 'Email' },
  { value: 'SMS', label: 'SMS' },
  { value: 'APP', label: 'Trong ứng dụng' },
];

/**
 * Chức năng 50 — Mẫu email / SMS / thông báo theo sự kiện.
 *
 * Nội dung dùng bộ thay thế placeholder tự viết (không phải template engine đầy đủ) nên mẫu chỉ
 * chèn được biến, không chạy được mã — quản trị viên nhập gì cũng không thành lỗ hổng.
 */
export default function TrangMauThongBao() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [locSuKien, setLocSuKien] = useState<string | undefined>();
  const [dangSua, setDangSua] = useState<MauThongBao | null>(null);
  const [moForm, setMoForm] = useState(false);
  const [form] = Form.useForm();

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['mau-thong-bao', locSuKien],
    queryFn: () => apiMauThongBao.danhSach({ suKien: locSuKien }),
  });

  const { data: dsSuKien } = useQuery({
    queryKey: ['mau-thong-bao-su-kien'],
    queryFn: apiMauThongBao.suKien,
  });

  function lamMoi() {
    void queryClient.invalidateQueries({ queryKey: ['mau-thong-bao'] });
  }

  const luu = useMutation({
    mutationFn: async (giaTri: Record<string, unknown>) => {
      const duLieu = {
        ma: (giaTri.ma as string).trim().toUpperCase(),
        ten: giaTri.ten as string,
        kenh: giaTri.kenh as string,
        suKien: giaTri.suKien as string,
        tieuDe: (giaTri.tieuDe as string) ?? '',
        noiDung: giaTri.noiDung as string,
        danhSachBien: (giaTri.danhSachBien as string[]) ?? [],
        trangThai: (giaTri.trangThai as number) ?? 1,
      };

      return dangSua ? apiMauThongBao.sua(dangSua.id, duLieu) : apiMauThongBao.them(duLieu);
    },
    onSuccess: () => {
      message.success(dangSua ? 'Đã cập nhật mẫu' : 'Đã thêm mẫu thông báo');
      setMoForm(false);
      setDangSua(null);
      form.resetFields();
      lamMoi();
    },
    onError: (loi) =>
      modal.error({
        title: 'Không lưu được mẫu',
        content: loi instanceof LoiApi ? loi.message : 'Đã xảy ra lỗi.',
      }),
  });

  const xoa = useMutation({
    mutationFn: (id: string) => apiMauThongBao.xoa(id),
    onSuccess: () => {
      message.success('Đã xoá mẫu thông báo');
      lamMoi();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không xoá được.'),
  });

  const xemTruoc = useMutation({
    mutationFn: (id: string) => apiMauThongBao.xemTruoc(id),
    onSuccess: (kq) =>
      modal.info({
        title: 'Xem trước nội dung gửi đi',
        width: 640,
        content: (
          <div style={{ marginTop: 12 }}>
            <Typography.Text strong>Tiêu đề</Typography.Text>
            <Typography.Paragraph>{kq.tieuDe || '(không có tiêu đề)'}</Typography.Paragraph>
            <Typography.Text strong>Nội dung</Typography.Text>
            <Typography.Paragraph style={{ whiteSpace: 'pre-wrap' }}>
              {kq.noiDung}
            </Typography.Paragraph>
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              Biến chưa có dữ liệu thật được thay bằng [tên_biến].
            </Typography.Text>
          </div>
        ),
      }),
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không xem trước được.'),
  });

  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  const tenSuKien = new Map((dsSuKien ?? []).map((x) => [x.ma, x.ten]));

  return (
    <Card
      title="Cấu hình hệ thống"
      extra={
        <Space>
          <Select
            style={{ width: 260 }}
            allowClear
            placeholder="Lọc theo sự kiện"
            value={locSuKien}
            options={(dsSuKien ?? []).map((x) => ({ value: x.ma, label: x.ten }))}
            onChange={setLocSuKien}
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
            Thêm mẫu
          </Button>
        </Space>
      }
    >
      <DaiTabTrang danhSach={DS_TAB_CAU_HINH} dangChon="mau-thong-bao" />

      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 12 }}
        message="Mỗi sự kiện có thể có nhiều mẫu theo kênh gửi."
        description={
          <>
            Trong nội dung, chèn biến theo dạng <code>{'{{ ten_bien }}'}</code>. Mẫu đặt trạng thái
            <strong> Ngừng</strong> thì hệ thống bỏ qua khi sự kiện xảy ra — đây cũng là cách tắt
            từng loại thông báo.
          </>
        }
      />

      <Table<MauThongBao>
        rowKey="id"
        size="middle"
        loading={isLoading}
        dataSource={data ?? []}
        pagination={false}
        scroll={{ x: 1100 }}
        locale={{ emptyText: <KhoiRong moTa="Chưa có mẫu thông báo nào." /> }}
        expandable={{
          expandedRowRender: (dong) => (
            <Space direction="vertical" size={4} style={{ width: '100%' }}>
              <Typography.Text strong>Tiêu đề: </Typography.Text>
              <Typography.Text>{dong.tieuDe || '(không có)'}</Typography.Text>
              <Typography.Paragraph style={{ whiteSpace: 'pre-wrap', marginBottom: 0 }}>
                {dong.noiDung}
              </Typography.Paragraph>
            </Space>
          ),
        }}
        columns={[
          { title: 'Mã', dataIndex: 'ma', width: 200 },
          { title: 'Tên mẫu', dataIndex: 'ten', width: 260 },
          {
            title: 'Sự kiện',
            dataIndex: 'suKien',
            width: 220,
            render: (v: string) => tenSuKien.get(v) ?? v,
          },
          {
            title: 'Kênh',
            dataIndex: 'kenh',
            width: 140,
            render: (v: string) => KENH.find((x) => x.value === v)?.label ?? v,
          },
          {
            title: 'Biến',
            dataIndex: 'danhSachBien',
            width: 220,
            render: (v: string[]) => (
              <Space size={[4, 4]} wrap>
                {v.slice(0, 3).map((x) => (
                  <Tag key={x}>{x}</Tag>
                ))}
                {v.length > 3 && <Tag>+{v.length - 3}</Tag>}
              </Space>
            ),
          },
          {
            title: 'Trạng thái',
            dataIndex: 'trangThai',
            width: 120,
            render: (v: number) =>
              v === 1 ? <Tag color="success">Đang dùng</Tag> : <Tag>Ngừng</Tag>,
          },
          {
            title: '',
            key: 'thaoTac',
            width: 140,
            fixed: 'right',
            render: (_v, dong) => (
              <Space>
                <Button
                  type="text"
                  icon={<EyeOutlined />}
                  title="Xem trước"
                  loading={xemTruoc.isPending && xemTruoc.variables === dong.id}
                  onClick={() => xemTruoc.mutate(dong.id)}
                />
                <Button
                  type="text"
                  icon={<EditOutlined />}
                  onClick={() => {
                    setDangSua(dong);
                    form.setFieldsValue(dong);
                    setMoForm(true);
                  }}
                />
                <Button
                  type="text"
                  danger
                  icon={<DeleteOutlined />}
                  onClick={() =>
                    modal.confirm({
                      title: 'Xoá mẫu thông báo',
                      content: `Xoá mẫu "${dong.ten}"? Sự kiện tương ứng sẽ không gửi tin theo kênh này nữa.`,
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
      />

      <Modal
        open={moForm}
        width={720}
        title={dangSua ? `Sửa mẫu: ${dangSua.ten}` : 'Thêm mẫu thông báo'}
        okText="Lưu"
        cancelText="Huỷ"
        confirmLoading={luu.isPending}
        onCancel={() => setMoForm(false)}
        onOk={async () => luu.mutate(await form.validateFields())}
      >
        <Form
          form={form}
          layout="vertical"
          initialValues={{ kenh: 'TAT_CA', trangThai: 1, danhSachBien: [] }}
        >
          <Space size="large" wrap style={{ display: 'flex' }}>
            <Form.Item
              name="ma"
              label="Mã mẫu"
              rules={[
                { required: true, message: 'Nhập mã' },
                { pattern: /^[A-Z0-9_-]+$/, message: 'Mã chỉ gồm chữ hoa, số, dấu _ và -' },
              ]}
            >
              <Input style={{ width: 240 }} placeholder="VD: MAU_TIEP_NHAN_EMAIL" disabled={!!dangSua} />
            </Form.Item>
            <Form.Item name="ten" label="Tên mẫu" rules={[{ required: true, message: 'Nhập tên' }]}>
              <Input style={{ width: 360 }} />
            </Form.Item>
          </Space>

          <Space size="large" wrap style={{ display: 'flex' }}>
            <Form.Item
              name="suKien"
              label="Sự kiện kích hoạt"
              rules={[{ required: true, message: 'Chọn sự kiện' }]}
            >
              <Select
                style={{ width: 300 }}
                options={(dsSuKien ?? []).map((x) => ({ value: x.ma, label: x.ten }))}
              />
            </Form.Item>
            <Form.Item name="kenh" label="Kênh gửi">
              <Select style={{ width: 200 }} options={KENH} />
            </Form.Item>
            <Form.Item name="trangThai" label="Trạng thái">
              <Select
                style={{ width: 160 }}
                options={[
                  { value: 1, label: 'Đang dùng' },
                  { value: 0, label: 'Ngừng' },
                ]}
              />
            </Form.Item>
          </Space>

          <Form.Item name="tieuDe" label="Tiêu đề (email / thông báo)">
            <Input placeholder="VD: Hồ sơ {{ ma_ho_so }} đã được tiếp nhận" />
          </Form.Item>

          <Form.Item
            name="noiDung"
            label="Nội dung"
            rules={[{ required: true, message: 'Nhập nội dung mẫu' }]}
          >
            <Input.TextArea rows={6} placeholder="Kính gửi {{ ho_ten }}, ..." />
          </Form.Item>

          <Form.Item
            name="danhSachBien"
            label="Biến dùng trong mẫu"
            tooltip="Liệt kê tên biến để người sửa mẫu sau này biết dùng được những gì."
          >
            <Select
              mode="tags"
              placeholder="ho_ten, ma_ho_so, ten_sang_kien, han_xu_ly…"
              options={[
                { value: 'ho_ten' },
                { value: 'ma_ho_so' },
                { value: 'ten_sang_kien' },
                { value: 'han_xu_ly' },
                { value: 'ten_don_vi' },
                { value: 'ket_qua' },
              ]}
            />
          </Form.Item>
        </Form>
      </Modal>
    </Card>
  );
}
