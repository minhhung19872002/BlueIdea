import { useState } from 'react';
import {
  App,
  Alert,
  Button,
  Card,
  Col,
  Form,
  Input,
  InputNumber,
  Modal,
  Popconfirm,
  Row,
  Select,
  Space,
  Statistic,
  Switch,
  Table,
  Tag,
  Tooltip,
  Typography,
} from 'antd';
import { DeleteOutlined, EditOutlined, PlusOutlined, SendOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { LoiApi } from '@/api/client';
import { apiCauHinhGuiTin, type CauHinhGuiTin, type LuuCauHinhGuiTin } from '@/api/endpoints';
import { KhoiLoi } from '@/components/ThanhPhanChung';
import { DaiTabTrang } from '@/components/DaiTabTrang';

const TRANG_THAI_HANG_DOI: Record<string, { ten: string; mau: string }> = {
  CHO_GUI: { ten: 'Chờ gửi', mau: '#faad14' },
  DANG_GUI: { ten: 'Đang gửi', mau: '#1677ff' },
  DA_GUI: { ten: 'Đã gửi', mau: '#52c41a' },
  LOI: { ten: 'Lỗi', mau: '#ff4d4f' },
};

/** Các nhánh con của trang — thiết kế gộp thành một mục ở thanh điều hướng. */
const DS_TAB = [
  { ma: 'he-thong', ten: 'Hệ thống', duongDan: '/quan-tri/cau-hinh/he-thong' },
  { ma: 'sang-kien', ten: 'Thông tin sáng kiến', duongDan: '/quan-tri/cau-hinh/sang-kien' },
  { ma: 'email-sms', ten: 'Email & SMS', duongDan: '/quan-tri/gui-tin' },
  { ma: 'chu-ky-so', ten: 'Chữ ký số', duongDan: '/quan-tri/chu-ky-so' },
  { ma: 'tich-hop', ten: 'Tích hợp', duongDan: '/quan-tri/cau-hinh/tich-hop' },
  { ma: 'menu', ten: 'Menu', duongDan: '/quan-tri/cau-hinh/menu' },
];

/** Chức năng 50 — Cấu hình máy chủ email và nhà cung cấp SMS. */
export default function TrangCauHinhGuiTin() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [dangSua, setDangSua] = useState<CauHinhGuiTin | null>(null);
  const [moForm, setMoForm] = useState(false);

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['cau-hinh-gui-tin'],
    queryFn: apiCauHinhGuiTin.danhSach,
  });

  const { data: hangDoi } = useQuery({
    queryKey: ['hang-doi-gui-tin'],
    queryFn: apiCauHinhGuiTin.thongKeHangDoi,
    refetchInterval: 30_000,
  });

  const xoa = useMutation({
    mutationFn: (id: string) => apiCauHinhGuiTin.xoa(id),
    onSuccess: () => {
      message.success('Đã xoá cấu hình');
      void queryClient.invalidateQueries({ queryKey: ['cau-hinh-gui-tin'] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không xoá được.'),
  });

  const guiThu = useMutation({
    mutationFn: ({ id, nguoiNhan }: { id: string; nguoiNhan: string }) =>
      apiCauHinhGuiTin.guiThu(id, nguoiNhan),
    onSuccess: () => message.success('Đã gửi thử thành công — kiểm tra hộp thư của người nhận.'),
    onError: (loi) =>
      modal.error({
        title: 'Gửi thử thất bại',
        width: 560,
        content: (
          <Typography.Paragraph style={{ marginBottom: 0 }}>
            {loi instanceof LoiApi ? loi.message : 'Không gửi được.'}
          </Typography.Paragraph>
        ),
      }),
  });

  function moGuiThu(banGhi: CauHinhGuiTin) {
    let nguoiNhan = '';

    modal.confirm({
      title: `Gửi thử bằng cấu hình ${banGhi.loai}`,
      content: (
        <div>
          <Typography.Paragraph>
            {banGhi.loai === 'SMS'
              ? 'Nhập số điện thoại nhận tin thử:'
              : 'Nhập địa chỉ email nhận tin thử:'}
          </Typography.Paragraph>
          <Input
            autoFocus
            placeholder={banGhi.loai === 'SMS' ? '09xxxxxxxx' : 'ban@donvi.gov.vn'}
            onChange={(e) => {
              nguoiNhan = e.target.value;
            }}
          />
        </div>
      ),
      okText: 'Gửi thử',
      cancelText: 'Huỷ',
      onOk: () => {
        if (!nguoiNhan.trim()) {
          message.warning('Chưa nhập người nhận.');
          return Promise.reject(new Error('thiếu người nhận'));
        }
        return guiThu.mutateAsync({ id: banGhi.id, nguoiNhan: nguoiNhan.trim() });
      },
    });
  }

  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  const chuaCoMacDinh =
    (data ?? []).length > 0 && !(data ?? []).some((x) => x.laMacDinh && x.trangThai === 1);

  return (
    <>
      <Card
        title="Cấu hình hệ thống"
        extra={
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => {
              setDangSua(null);
              setMoForm(true);
            }}
          >
            Thêm cấu hình
          </Button>
        }
      >
        <Row gutter={16} style={{ marginBottom: 16 }}>
          {Object.entries(TRANG_THAI_HANG_DOI).map(([ma, m]) => (
            <Col key={ma} xs={12} sm={6}>
              <Card size="small">
                <Statistic
                  title={m.ten}
                  value={hangDoi?.[ma] ?? 0}
                  valueStyle={{ color: m.mau, fontSize: 20 }}
                />
              </Card>
            </Col>
          ))}
        </Row>

        {chuaCoMacDinh && (
          <Alert
            type="warning"
            showIcon
            style={{ marginBottom: 12 }}
            message="Chưa có cấu hình nào được đánh dấu mặc định và đang hoạt động."
            description="Công việc nền chọn cấu hình mặc định để gửi. Khi chưa có, tin sẽ nằm chờ trong hàng đợi cho tới khi cấu hình xong — không bị mất."
          />
        )}

        <Table<CauHinhGuiTin>
          rowKey="id"
          size="middle"
          loading={isLoading}
          dataSource={data ?? []}
          pagination={false}
          scroll={{ x: 900 }}
          columns={[
            {
              title: 'Loại',
              dataIndex: 'loai',
              width: 110,
              render: (v: string, dong) => (
                <Space direction="vertical" size={0}>
                  <Tag color={v === 'EMAIL' ? 'blue' : 'green'}>{v}</Tag>
                  {dong.laMacDinh && <Tag color="gold">Mặc định</Tag>}
                </Space>
              ),
            },
            { title: 'Nhà cung cấp', dataIndex: 'nhaCungCap', width: 150 },
            {
              title: 'Máy chủ / Endpoint',
              key: 'may',
              render: (_v, dong) =>
                dong.loai === 'EMAIL'
                  ? `${dong.host ?? '—'}:${dong.port ?? '—'}${dong.suDungSsl ? ' (TLS)' : ''}`
                  : (dong.apiEndpoint ?? '—'),
            },
            {
              title: 'Gửi đi từ',
              key: 'guiDi',
              width: 220,
              responsive: ['lg'],
              render: (_v, dong) => dong.emailGuiDi ?? dong.brandname ?? '—',
            },
            {
              title: 'Bí mật',
              key: 'biMat',
              width: 130,
              render: (_v, dong) =>
                dong.daDatMatKhau || dong.daDatApiKey ? (
                  <Tag color="success">Đã đặt</Tag>
                ) : (
                  <Tag>Chưa đặt</Tag>
                ),
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
              width: 130,
              fixed: 'right',
              render: (_v, dong) => (
                <Space size={4}>
                  <Tooltip title="Gửi thử">
                    <Button
                      size="small"
                      icon={<SendOutlined />}
                      loading={guiThu.isPending}
                      onClick={() => moGuiThu(dong)}
                    />
                  </Tooltip>
                  <Tooltip title="Sửa">
                    <Button
                      size="small"
                      icon={<EditOutlined />}
                      onClick={() => {
                        setDangSua(dong);
                        setMoForm(true);
                      }}
                    />
                  </Tooltip>
                  <Popconfirm
                    title="Xoá cấu hình này?"
                    okText="Xoá"
                    cancelText="Huỷ"
                    onConfirm={() => xoa.mutate(dong.id)}
                  >
                    <Button size="small" danger icon={<DeleteOutlined />} />
                  </Popconfirm>
                </Space>
              ),
            },
          ]}
        />

        <Typography.Paragraph type="secondary" style={{ marginTop: 12, fontSize: 12 }}>
          Mật khẩu SMTP và API key được mã hoá AES-256-GCM trước khi lưu và không bao giờ được trả
          về giao diện. Để trống ô mật khẩu khi sửa nghĩa là giữ nguyên giá trị đang có.
        </Typography.Paragraph>
      </Card>

      {moForm && (
        <FormCauHinh
          banGhi={dangSua}
          onDong={() => setMoForm(false)}
          onXong={() => {
            setMoForm(false);
            void queryClient.invalidateQueries({ queryKey: ['cau-hinh-gui-tin'] });
          }}
        />
      )}
    </>
  );
}

// ---------------------------------------------------------------------------

interface GiaTriForm {
  loai: string;
  nhaCungCap?: string;
  host?: string;
  port?: number;
  tenDangNhap?: string;
  matKhau?: string;
  suDungSsl: boolean;
  emailGuiDi?: string;
  tenHienThi?: string;
  apiEndpoint?: string;
  apiKey?: string;
  brandname?: string;
  trangThai: number;
  laMacDinh: boolean;
}

function FormCauHinh({
  banGhi,
  onDong,
  onXong,
}: {
  banGhi: CauHinhGuiTin | null;
  onDong: () => void;
  onXong: () => void;
}) {
  const { message } = App.useApp();
  const [form] = Form.useForm<GiaTriForm>();
  const [loai, setLoai] = useState(banGhi?.loai ?? 'EMAIL');

  const luu = useMutation({
    mutationFn: (giaTri: LuuCauHinhGuiTin) =>
      banGhi ? apiCauHinhGuiTin.sua(banGhi.id, giaTri) : apiCauHinhGuiTin.them(giaTri),
    onSuccess: () => {
      message.success(banGhi ? 'Đã cập nhật cấu hình' : 'Đã thêm cấu hình');
      onXong();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không lưu được.'),
  });

  async function xacNhan() {
    const giaTri = await form.validateFields();
    luu.mutate({ ...giaTri, trangThai: giaTri.trangThai ?? 1 });
  }

  return (
    <Modal
      open
      width={680}
      title={banGhi ? `Sửa cấu hình ${banGhi.loai}` : 'Thêm cấu hình gửi tin'}
      okText={banGhi ? 'Lưu thay đổi' : 'Thêm'}
      cancelText="Huỷ"
      confirmLoading={luu.isPending}
      onOk={xacNhan}
      onCancel={onDong}
      destroyOnClose
    >
      <DaiTabTrang danhSach={DS_TAB} dangChon={'email-sms'} />

      <Form<GiaTriForm>
        form={form}
        layout="vertical"
        initialValues={{
          loai: banGhi?.loai ?? 'EMAIL',
          nhaCungCap: banGhi?.nhaCungCap ?? undefined,
          host: banGhi?.host ?? undefined,
          port: banGhi?.port ?? 587,
          tenDangNhap: banGhi?.tenDangNhap ?? undefined,
          suDungSsl: banGhi?.suDungSsl ?? true,
          emailGuiDi: banGhi?.emailGuiDi ?? undefined,
          tenHienThi: banGhi?.tenHienThi ?? undefined,
          apiEndpoint: banGhi?.apiEndpoint ?? undefined,
          brandname: banGhi?.brandname ?? undefined,
          trangThai: banGhi?.trangThai ?? 1,
          laMacDinh: banGhi?.laMacDinh ?? false,
        }}
      >
        <Row gutter={12}>
          <Col xs={24} md={8}>
            <Form.Item name="loai" label="Loại">
              <Select
                options={[
                  { value: 'EMAIL', label: 'Email (SMTP)' },
                  { value: 'SMS', label: 'SMS (API)' },
                ]}
                onChange={setLoai}
              />
            </Form.Item>
          </Col>
          <Col xs={24} md={10}>
            <Form.Item name="nhaCungCap" label="Nhà cung cấp">
              <Input placeholder={loai === 'EMAIL' ? 'VNPT Mail, Google Workspace…' : 'Viettel, VNPT…'} />
            </Form.Item>
          </Col>
          <Col xs={24} md={6}>
            <Form.Item name="trangThai" label="Trạng thái">
              <Select
                options={[
                  { value: 1, label: 'Hoạt động' },
                  { value: 0, label: 'Ngừng' },
                ]}
              />
            </Form.Item>
          </Col>
        </Row>

        {loai === 'EMAIL' ? (
          <>
            <Row gutter={12}>
              <Col xs={24} md={12}>
                <Form.Item
                  name="host"
                  label="Máy chủ SMTP"
                  rules={[{ required: true, message: 'Nhập máy chủ SMTP' }]}
                >
                  <Input placeholder="smtp.donvi.gov.vn" />
                </Form.Item>
              </Col>
              <Col xs={12} md={6}>
                <Form.Item
                  name="port"
                  label="Cổng"
                  rules={[{ required: true, message: 'Nhập cổng' }]}
                >
                  <InputNumber<number> min={1} max={65535} style={{ width: '100%' }} />
                </Form.Item>
              </Col>
              <Col xs={12} md={6}>
                <Form.Item name="suDungSsl" label="Dùng TLS" valuePropName="checked">
                  <Switch checkedChildren="Có" unCheckedChildren="Không" />
                </Form.Item>
              </Col>
            </Row>

            <Row gutter={12}>
              <Col xs={24} md={12}>
                <Form.Item name="tenDangNhap" label="Tài khoản SMTP">
                  <Input autoComplete="off" />
                </Form.Item>
              </Col>
              <Col xs={24} md={12}>
                <Form.Item
                  name="matKhau"
                  label="Mật khẩu SMTP"
                  tooltip={
                    banGhi?.daDatMatKhau
                      ? 'Đã có mật khẩu. Để trống nếu giữ nguyên.'
                      : 'Mã hoá AES-256-GCM trước khi lưu.'
                  }
                >
                  <Input.Password
                    autoComplete="new-password"
                    placeholder={banGhi?.daDatMatKhau ? '•••••••• (giữ nguyên)' : ''}
                  />
                </Form.Item>
              </Col>
            </Row>

            <Row gutter={12}>
              <Col xs={24} md={12}>
                <Form.Item
                  name="emailGuiDi"
                  label="Địa chỉ gửi đi"
                  rules={[
                    { required: true, message: 'Nhập địa chỉ gửi đi' },
                    { type: 'email', message: 'Email không hợp lệ' },
                  ]}
                >
                  <Input placeholder="khongtraloi@donvi.gov.vn" />
                </Form.Item>
              </Col>
              <Col xs={24} md={12}>
                <Form.Item name="tenHienThi" label="Tên hiển thị">
                  <Input placeholder="Hệ thống Sáng kiến" />
                </Form.Item>
              </Col>
            </Row>
          </>
        ) : (
          <>
            <Form.Item
              name="apiEndpoint"
              label="Endpoint API"
              rules={[{ required: true, message: 'Nhập endpoint API của nhà cung cấp' }]}
            >
              <Input placeholder="https://api.nhacungcap.vn/sms/send" />
            </Form.Item>

            <Row gutter={12}>
              <Col xs={24} md={12}>
                <Form.Item
                  name="apiKey"
                  label="API key"
                  tooltip={
                    banGhi?.daDatApiKey
                      ? 'Đã có API key. Để trống nếu giữ nguyên.'
                      : 'Mã hoá AES-256-GCM trước khi lưu.'
                  }
                >
                  <Input.Password
                    autoComplete="new-password"
                    placeholder={banGhi?.daDatApiKey ? '•••••••• (giữ nguyên)' : ''}
                  />
                </Form.Item>
              </Col>
              <Col xs={24} md={12}>
                <Form.Item name="brandname" label="Brandname">
                  <Input placeholder="SANGKIEN" />
                </Form.Item>
              </Col>
            </Row>
          </>
        )}

        <Form.Item
          name="laMacDinh"
          label="Dùng làm cấu hình mặc định"
          valuePropName="checked"
          tooltip="Công việc nền chọn cấu hình mặc định để gửi. Mỗi loại chỉ có một mặc định."
        >
          <Switch checkedChildren="Có" unCheckedChildren="Không" />
        </Form.Item>
      </Form>
    </Modal>
  );
}
