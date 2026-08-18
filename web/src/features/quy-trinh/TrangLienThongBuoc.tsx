import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
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
import { ArrowLeftOutlined, DeleteOutlined, EditOutlined, PlusOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { LoiApi } from '@/api/client';
import {
  apiLienThongBuoc,
  apiQuyTrinh,
  apiTichHop,
  type LienThongBuoc,
} from '@/api/endpoints';
import { KhoiLoi, KhoiRong } from '@/components/ThanhPhanChung';

const SU_KIEN = [
  { value: 'KHI_VAO_BUOC', label: 'Khi hồ sơ vào bước' },
  { value: 'KHI_HOAN_THANH', label: 'Khi hoàn thành bước' },
  { value: 'KHI_PHE_DUYET', label: 'Khi được phê duyệt' },
];

/**
 * Chức năng 16 — Gắn hệ thống liên thông vào từng bước của quy trình.
 *
 * Khác màn hình *Liên thông hệ thống ngoài* (khai báo endpoint, khoá): ở đây quyết định
 * **khi nào** gọi hệ thống nào — hồ sơ chạy tới bước nào thì đẩy dữ liệu đi.
 */
export default function TrangLienThongBuoc() {
  const { id = '' } = useParams<{ id: string }>();
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [dangSua, setDangSua] = useState<LienThongBuoc | null>(null);
  const [moForm, setMoForm] = useState(false);
  const [form] = Form.useForm();

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['lien-thong-buoc', id],
    queryFn: () => apiLienThongBuoc.danhSach(id),
  });

  const { data: soDo } = useQuery({
    queryKey: ['quy-trinh-so-do', id],
    queryFn: () => apiQuyTrinh.soDo(id),
  });

  const { data: heThong } = useQuery({
    queryKey: ['he-thong-tich-hop'],
    queryFn: apiTichHop.danhSach,
  });

  function lamMoi() {
    void queryClient.invalidateQueries({ queryKey: ['lien-thong-buoc', id] });
  }

  const luu = useMutation({
    mutationFn: async (giaTri: Record<string, unknown>) => {
      const duLieu = {
        buocId: (giaTri.buocId as string) || null,
        heThongTichHopId: giaTri.heThongTichHopId as string,
        suKien: giaTri.suKien as string,
        loaiDuLieu: (giaTri.loaiDuLieu as string) || null,
        dongBoHaiChieu: (giaTri.dongBoHaiChieu as boolean) ?? false,
        trangThai: (giaTri.trangThai as number) ?? 1,
      };

      return dangSua
        ? apiLienThongBuoc.sua(dangSua.id, duLieu)
        : apiLienThongBuoc.them(id, duLieu);
    },
    onSuccess: () => {
      message.success(dangSua ? 'Đã cập nhật cấu hình liên thông' : 'Đã thêm cấu hình liên thông');
      setMoForm(false);
      setDangSua(null);
      form.resetFields();
      lamMoi();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không lưu được.'),
  });

  const xoa = useMutation({
    mutationFn: (idXoa: string) => apiLienThongBuoc.xoa(idXoa),
    onSuccess: () => {
      message.success('Đã xoá cấu hình liên thông');
      lamMoi();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không xoá được.'),
  });

  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  const coHeThong = (heThong?.length ?? 0) > 0;

  return (
    <Card
      title="Liên thông theo bước quy trình"
      extra={
        <Space>
          <Link to="/quan-tri/quy-trinh">
            <Button icon={<ArrowLeftOutlined />}>Danh sách quy trình</Button>
          </Link>
          <Link to={`/quan-tri/quy-trinh/${id}/thiet-ke`}>
            <Button>Trình thiết kế</Button>
          </Link>
          <Button
            type="primary"
            icon={<PlusOutlined />}
            disabled={!coHeThong}
            onClick={() => {
              setDangSua(null);
              form.resetFields();
              setMoForm(true);
            }}
          >
            Thêm cấu hình
          </Button>
        </Space>
      }
    >
      <Alert
        type={coHeThong ? 'info' : 'warning'}
        showIcon
        style={{ marginBottom: 12 }}
        message={
          coHeThong
            ? 'Cấu hình ở đây quyết định KHI NÀO đẩy dữ liệu sang hệ thống ngoài.'
            : 'Chưa khai báo hệ thống liên thông nào.'
        }
        description={
          coHeThong ? (
            'Endpoint, khoá và cách xác thực của từng hệ thống khai báo ở màn hình Quản trị → Liên thông hệ thống ngoài.'
          ) : (
            <Link to="/quan-tri/lien-thong">Vào Quản trị → Liên thông hệ thống ngoài để khai báo trước →</Link>
          )
        }
      />

      <Table<LienThongBuoc>
        rowKey="id"
        size="middle"
        loading={isLoading}
        dataSource={data ?? []}
        pagination={false}
        scroll={{ x: 1000 }}
        locale={{ emptyText: <KhoiRong moTa="Quy trình này chưa gắn liên thông ở bước nào." /> }}
        columns={[
          {
            title: 'Bước',
            dataIndex: 'tenBuoc',
            width: 260,
            render: (v: string | null) =>
              v ?? <Typography.Text type="secondary">Toàn quy trình</Typography.Text>,
          },
          {
            title: 'Sự kiện kích hoạt',
            dataIndex: 'suKien',
            width: 200,
            render: (v: string) => SU_KIEN.find((x) => x.value === v)?.label ?? v,
          },
          { title: 'Hệ thống nhận', dataIndex: 'tenHeThong', width: 240 },
          {
            title: 'Loại dữ liệu',
            dataIndex: 'loaiDuLieu',
            width: 200,
            render: (v: string | null) => v ?? '—',
          },
          {
            title: 'Trạng thái',
            dataIndex: 'trangThai',
            width: 120,
            render: (v: number) =>
              v === 1 ? <Tag color="success">Hoạt động</Tag> : <Tag>Ngừng</Tag>,
          },
          {
            title: '',
            key: 'thaoTac',
            width: 100,
            fixed: 'right',
            render: (_v, dong) => (
              <Space>
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
                      title: 'Xoá cấu hình liên thông',
                      content: `Hồ sơ chạy qua bước "${dong.tenBuoc ?? 'toàn quy trình'}" sẽ không còn đẩy dữ liệu sang ${dong.tenHeThong}.`,
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
        width={620}
        title={dangSua ? 'Sửa cấu hình liên thông' : 'Thêm cấu hình liên thông'}
        okText="Lưu"
        cancelText="Huỷ"
        confirmLoading={luu.isPending}
        onCancel={() => setMoForm(false)}
        onOk={async () => luu.mutate(await form.validateFields())}
      >
        <Form
          form={form}
          layout="vertical"
          initialValues={{ suKien: 'KHI_HOAN_THANH', trangThai: 1, dongBoHaiChieu: false }}
        >
          <Form.Item
            name="buocId"
            label="Bước áp dụng"
            tooltip="Để trống = áp dụng cho toàn quy trình."
          >
            <Select
              allowClear
              placeholder="Toàn quy trình"
              options={(soDo?.danhSachBuoc ?? []).map((b) => ({ value: b.id, label: b.ten }))}
            />
          </Form.Item>

          <Form.Item
            name="heThongTichHopId"
            label="Hệ thống nhận dữ liệu"
            rules={[{ required: true, message: 'Chọn hệ thống liên thông' }]}
          >
            <Select
              options={(heThong ?? []).map((x) => ({
                value: x.id,
                label: `${x.ten} (${x.ma})`,
              }))}
            />
          </Form.Item>

          <Form.Item name="suKien" label="Kích hoạt khi">
            <Select options={SU_KIEN} />
          </Form.Item>

          <Form.Item
            name="loaiDuLieu"
            label="Loại dữ liệu đẩy đi"
            tooltip="Nhãn để hệ thống ngoài biết gói dữ liệu thuộc loại nào."
          >
            <Input placeholder="VD: SANG_KIEN_DUOC_CONG_NHAN" />
          </Form.Item>

          <Space size="large" wrap>
            <Form.Item name="trangThai" label="Trạng thái">
              <Select
                style={{ width: 180 }}
                options={[
                  { value: 1, label: 'Hoạt động' },
                  { value: 0, label: 'Ngừng' },
                ]}
              />
            </Form.Item>
            <Form.Item
              name="dongBoHaiChieu"
              label="Đồng bộ hai chiều"
              tooltip="Bật khi hệ thống ngoài cũng đẩy trạng thái ngược lại qua API công khai."
            >
              <Select
                style={{ width: 180 }}
                options={[
                  { value: false, label: 'Một chiều (đẩy đi)' },
                  { value: true, label: 'Hai chiều' },
                ]}
              />
            </Form.Item>
          </Space>
        </Form>
      </Modal>
    </Card>
  );
}
