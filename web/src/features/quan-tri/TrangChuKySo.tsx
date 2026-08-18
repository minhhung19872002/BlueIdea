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
  Tooltip,
  Typography,
} from 'antd';
import { DeleteOutlined, EditOutlined, PlusOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { LoiApi } from '@/api/client';
import { apiChuKySo, type CauHinhChuKySo } from '@/api/endpoints';
import { KhoiLoi, KhoiRong } from '@/components/ThanhPhanChung';
import { DaiTabTrang } from '@/components/DaiTabTrang';
import { DS_TAB_CAU_HINH } from './cauHinhTab';

const NHA_CUNG_CAP = [
  { value: 'BAN_CO_YEU_CHINH_PHU', label: 'Ban Cơ yếu Chính phủ' },
  { value: 'VNPT_CA', label: 'VNPT-CA' },
  { value: 'VIETTEL_CA', label: 'Viettel-CA' },
  { value: 'FPT_CA', label: 'FPT-CA' },
  { value: 'MISA', label: 'MISA eSign' },
  { value: 'KHAC', label: 'Khác' },
];

const LOAI_KY = [
  { value: 'USB_TOKEN', label: 'USB Token' },
  { value: 'HSM', label: 'HSM' },
  { value: 'REMOTE_SIGNING', label: 'Ký từ xa (remote signing)' },
  { value: 'SMART_CA', label: 'SmartCA' },
];

/** Chức năng 49 — Cấu hình chữ ký số dùng để ký quyết định và biên bản. */
export default function TrangChuKySo() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [dangSua, setDangSua] = useState<CauHinhChuKySo | null>(null);
  const [moForm, setMoForm] = useState(false);
  const [form] = Form.useForm();

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['cau-hinh-chu-ky-so'],
    queryFn: apiChuKySo.danhSach,
  });

  const { data: trangThai } = useQuery({
    queryKey: ['chu-ky-so-trang-thai'],
    queryFn: apiChuKySo.trangThai,
  });

  function lamMoi() {
    void queryClient.invalidateQueries({ queryKey: ['cau-hinh-chu-ky-so'] });
    void queryClient.invalidateQueries({ queryKey: ['chu-ky-so-trang-thai'] });
  }

  const luu = useMutation({
    mutationFn: async (giaTri: Record<string, unknown>) => {
      const duLieu = {
        nhaCungCap: giaTri.nhaCungCap as string,
        loaiKy: giaTri.loaiKy as string,
        endpoint: (giaTri.endpoint as string) || null,
        clientId: (giaTri.clientId as string) || null,
        clientSecret: (giaTri.clientSecret as string) || null,
        chungThuSo: (giaTri.chungThuSo as string) || null,
        thuatToan: (giaTri.thuatToan as string) ?? 'SHA256withRSA',
        trangThai: (giaTri.trangThai as number) ?? 1,
        laMacDinh: (giaTri.laMacDinh as boolean) ?? false,
      };

      return dangSua ? apiChuKySo.sua(dangSua.id, duLieu) : apiChuKySo.them(duLieu);
    },
    onSuccess: () => {
      message.success(dangSua ? 'Đã cập nhật cấu hình' : 'Đã thêm cấu hình chữ ký số');
      setMoForm(false);
      setDangSua(null);
      form.resetFields();
      lamMoi();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không lưu được.'),
  });

  const xoa = useMutation({
    mutationFn: (id: string) => apiChuKySo.xoa(id),
    onSuccess: () => {
      message.success('Đã xoá cấu hình');
      lamMoi();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không xoá được.'),
  });

  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  return (
    <Card
      title="Cấu hình hệ thống"
      extra={
        <Button
          type="primary"
          icon={<PlusOutlined />}
          onClick={() => {
            setDangSua(null);
            form.resetFields();
            setMoForm(true);
          }}
        >
          Thêm cấu hình
        </Button>
      }
    >
      <DaiTabTrang danhSach={DS_TAB_CAU_HINH} dangChon="chu-ky-so" />

      <Alert
        type={trangThai?.sanSang ? 'success' : 'warning'}
        showIcon
        style={{ marginBottom: 12 }}
        message={
          trangThai?.sanSang
            ? 'Hệ thống sẵn sàng ký số'
            : 'Chưa ký số được — thiếu cấu hình hoặc thiếu chứng thư trên máy chủ'
        }
        description={
          <Space direction="vertical" size={2}>
            <span>
              Cấu hình đang hoạt động:{' '}
              <strong>{trangThai?.coCauHinhHoatDong ? 'có' : 'chưa có'}</strong>
            </span>
            <span>
              Chứng thư trên máy chủ:{' '}
              <strong>{trangThai?.mayChuSanSangKy ? 'đã nạp' : 'chưa nạp'}</strong>
            </span>
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              Khoá bí mật (tệp PFX / USB token) <strong>không</strong> lưu trong cơ sở dữ liệu. Quản
              trị máy chủ gắn tệp vào rồi khai báo đường dẫn ở biến môi trường{' '}
              <code>KYSO_PFX</code> và <code>KYSO_MAT_KHAU_PFX</code>.
            </Typography.Text>
          </Space>
        }
      />

      <Table<CauHinhChuKySo>
        rowKey="id"
        size="middle"
        loading={isLoading}
        dataSource={data ?? []}
        pagination={false}
        scroll={{ x: 1200 }}
        locale={{ emptyText: <KhoiRong moTa="Chưa khai báo cấu hình chữ ký số nào." /> }}
        columns={[
          {
            title: 'Nhà cung cấp',
            dataIndex: 'nhaCungCap',
            width: 220,
            render: (v: string, dong) => (
              <Space direction="vertical" size={0}>
                <span>{NHA_CUNG_CAP.find((x) => x.value === v)?.label ?? v}</span>
                {dong.laMacDinh && <Tag color="blue">Mặc định</Tag>}
              </Space>
            ),
          },
          {
            title: 'Hình thức ký',
            dataIndex: 'loaiKy',
            width: 200,
            render: (v: string) => LOAI_KY.find((x) => x.value === v)?.label ?? v,
          },
          {
            title: 'Chứng thư (serial)',
            dataIndex: 'chungThuSo',
            width: 240,
            render: (v: string | null) =>
              v ? (
                <Typography.Text code>{v}</Typography.Text>
              ) : (
                <Typography.Text type="secondary">Dùng tệp PFX của máy chủ</Typography.Text>
              ),
          },
          {
            title: 'Thuật toán',
            dataIndex: 'thuatToan',
            width: 150,
            responsive: ['lg'],
          },
          {
            title: 'Bí mật',
            dataIndex: 'daDatBiMat',
            width: 130,
            render: (v: boolean) =>
              v ? <Tag color="success">Đã đặt</Tag> : <Tag>Chưa đặt</Tag>,
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
                <Tooltip title="Sửa">
                  <Button
                    type="text"
                    icon={<EditOutlined />}
                    onClick={() => {
                      setDangSua(dong);
                      form.setFieldsValue({ ...dong, clientSecret: undefined });
                      setMoForm(true);
                    }}
                  />
                </Tooltip>
                <Button
                  type="text"
                  danger
                  icon={<DeleteOutlined />}
                  onClick={() =>
                    modal.confirm({
                      title: 'Xoá cấu hình chữ ký số',
                      content:
                        'Văn bản đã ký trước đó vẫn giữ nguyên chữ ký; chỉ những lần ký sau mới bị ảnh hưởng.',
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
        width={640}
        title={dangSua ? 'Sửa cấu hình chữ ký số' : 'Thêm cấu hình chữ ký số'}
        okText="Lưu"
        cancelText="Huỷ"
        confirmLoading={luu.isPending}
        onCancel={() => setMoForm(false)}
        onOk={async () => luu.mutate(await form.validateFields())}
      >
        <Form
          form={form}
          layout="vertical"
          initialValues={{
            nhaCungCap: 'BAN_CO_YEU_CHINH_PHU',
            loaiKy: 'USB_TOKEN',
            thuatToan: 'SHA256withRSA',
            trangThai: 1,
            laMacDinh: true,
          }}
        >
          <Space size="large" wrap style={{ display: 'flex' }}>
            <Form.Item name="nhaCungCap" label="Nhà cung cấp chứng thư">
              <Select style={{ width: 260 }} options={NHA_CUNG_CAP} />
            </Form.Item>
            <Form.Item name="loaiKy" label="Hình thức ký">
              <Select style={{ width: 260 }} options={LOAI_KY} />
            </Form.Item>
          </Space>

          <Form.Item
            name="chungThuSo"
            label="Serial chứng thư trên máy chủ"
            tooltip="Dùng khi ký bằng USB token hoặc HSM. Bỏ trống nếu máy chủ đã nạp tệp PFX."
          >
            <Input placeholder="VD: 54010102030405060708090A0B0C0D0E" />
          </Form.Item>

          <Form.Item
            name="endpoint"
            label="Endpoint dịch vụ ký từ xa"
            rules={[{ type: 'url', message: 'Phải là URL http/https tuyệt đối' }]}
          >
            <Input placeholder="https://ca.example.gov.vn/sign" />
          </Form.Item>

          <Space size="large" wrap style={{ display: 'flex' }}>
            <Form.Item name="clientId" label="Client ID">
              <Input style={{ width: 260 }} />
            </Form.Item>
            <Form.Item
              name="clientSecret"
              label="Client secret"
              tooltip={dangSua ? 'Để trống = giữ nguyên bí mật đang lưu.' : undefined}
            >
              <Input.Password
                style={{ width: 260 }}
                placeholder={dangSua ? 'Để trống nếu không đổi' : ''}
              />
            </Form.Item>
          </Space>

          <Space size="large" wrap>
            <Form.Item name="thuatToan" label="Thuật toán ký">
              <Select
                style={{ width: 220 }}
                options={[
                  { value: 'SHA256withRSA', label: 'SHA256withRSA' },
                  { value: 'SHA384withRSA', label: 'SHA384withRSA' },
                  { value: 'SHA512withRSA', label: 'SHA512withRSA' },
                ]}
              />
            </Form.Item>
            <Form.Item name="trangThai" label="Trạng thái">
              <Select
                style={{ width: 180 }}
                options={[
                  { value: 1, label: 'Hoạt động' },
                  { value: 0, label: 'Ngừng' },
                ]}
              />
            </Form.Item>
            <Form.Item name="laMacDinh" label="Dùng làm mặc định" valuePropName="checked">
              <Select
                style={{ width: 150 }}
                options={[
                  { value: true, label: 'Có' },
                  { value: false, label: 'Không' },
                ]}
              />
            </Form.Item>
          </Space>
        </Form>
      </Modal>
    </Card>
  );
}
