import { useState } from 'react';
import {
  App,
  Alert,
  Button,
  Card,
  Col,
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

import { z } from 'zod';

import { LoiApi } from '@/api/client';
import { BieuMau, Truong, useBieuMau } from '@/components/bieu-mau/BieuMau';
import { email, soNguyen, trangThai, tuyChon } from '@/components/bieu-mau/luat';
import { apiCauHinhGuiTin, type CauHinhGuiTin, type LuuCauHinhGuiTin } from '@/api/endpoints';
import { KhoiLoi } from '@/components/ThanhPhanChung';
import { DaiTabTrang } from '@/components/DaiTabTrang';
import { DS_TAB_CAU_HINH } from './cauHinhTab';

const TRANG_THAI_HANG_DOI: Record<string, { ten: string; mau: string }> = {
  CHO_GUI: { ten: 'Chờ gửi', mau: '#faad14' },
  DANG_GUI: { ten: 'Đang gửi', mau: '#1677ff' },
  DA_GUI: { ten: 'Đã gửi', mau: '#52c41a' },
  LOI: { ten: 'Lỗi', mau: '#ff4d4f' },
};

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
              width: 220,
              ellipsis: true,
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

/**
 * Luật kiểm tra cấu hình gửi tin.
 *
 * Một form phục vụ hai loại cấu hình khác hẳn nhau (SMTP và API SMS), nên các trường bắt buộc
 * phụ thuộc vào `loai`. Khai `required` cứng cho từng ô sẽ bắt người cấu hình SMS phải điền
 * máy chủ SMTP — thứ họ không có. Vì vậy dùng `superRefine` để yêu cầu đúng nhóm trường của loại
 * đang chọn.
 */
const luatGuiTin = z
  .object({
    loai: z.string(),
    nhaCungCap: tuyChon(200),
    host: tuyChon(200),
    port: soNguyen('Cổng', 1, 65535).optional(),
    tenDangNhap: tuyChon(200),
    matKhau: tuyChon(500),
    suDungSsl: z.boolean(),
    emailGuiDi: email,
    tenHienThi: tuyChon(200),
    apiEndpoint: z
      .string()
      .trim()
      .url('Phải là địa chỉ http/https tuyệt đối.')
      .optional()
      .or(z.literal('').transform(() => undefined)),
    apiKey: tuyChon(500),
    brandname: tuyChon(100),
    trangThai: trangThai,
    laMacDinh: z.boolean(),
  })
  .superRefine((v, ctx) => {
    const thieu = (duong: string, thongBao: string) =>
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: [duong], message: thongBao });

    if (v.loai === 'EMAIL') {
      if (!v.host) thieu('host', 'Cấu hình email cần máy chủ SMTP.');
      if (!v.port) thieu('port', 'Cấu hình email cần cổng SMTP.');
      if (!v.emailGuiDi) thieu('emailGuiDi', 'Cấu hình email cần địa chỉ gửi đi.');
      return;
    }

    if (!v.apiEndpoint) thieu('apiEndpoint', 'Cấu hình SMS cần endpoint API của nhà cung cấp.');
  });

type GiaTriForm = z.infer<typeof luatGuiTin>;

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
  const form = useBieuMau(luatGuiTin, {
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
    trangThai: (banGhi?.trangThai ?? 1) as 0 | 1,
    laMacDinh: banGhi?.laMacDinh ?? false,
  } as GiaTriForm);

  // Đọc thẳng từ form thay vì giữ state riêng — hai nguồn dễ lệch khi form được nạp lại.
  const loai = form.watch('loai');

  const luu = useMutation({
    mutationFn: (giaTri: LuuCauHinhGuiTin) =>
      banGhi ? apiCauHinhGuiTin.sua(banGhi.id, giaTri) : apiCauHinhGuiTin.them(giaTri),
    onSuccess: () => {
      message.success(banGhi ? 'Đã cập nhật cấu hình' : 'Đã thêm cấu hình');
      onXong();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không lưu được.'),
  });

  function xacNhan(giaTri: GiaTriForm) {
    return luu.mutateAsync({ ...giaTri, trangThai: giaTri.trangThai });
  }

  return (
    <Modal
      open
      width={680}
      title={banGhi ? `Sửa cấu hình ${banGhi.loai}` : 'Thêm cấu hình gửi tin'}
      okText={banGhi ? 'Lưu thay đổi' : 'Thêm'}
      cancelText="Huỷ"
      confirmLoading={luu.isPending || form.formState.isSubmitting}
      okButtonProps={{ htmlType: 'submit', form: 'form-gui-tin' }}
      onCancel={onDong}
      destroyOnClose
    >
      <DaiTabTrang danhSach={DS_TAB_CAU_HINH} dangChon={'email-sms'} />

      <BieuMau id="form-gui-tin" form={form} onGui={xacNhan}>
        <Row gutter={12}>
          <Col xs={24} md={8}>
            <Truong<GiaTriForm> ten="loai" label="Loại">
              {(o) => (
                <Select
                  {...o}
                  value={o.value as string}
                  options={[
                    { value: 'EMAIL', label: 'Email (SMTP)' },
                    { value: 'SMS', label: 'SMS (API)' },
                  ]}
                />
              )}
            </Truong>
          </Col>
          <Col xs={24} md={10}>
            <Truong<GiaTriForm> ten="nhaCungCap" label="Nhà cung cấp">
              {(o) => (
                <Input
                  {...o}
                  value={o.value as string}
                  placeholder={
                    loai === 'EMAIL' ? 'VNPT Mail, Google Workspace…' : 'Viettel, VNPT…'
                  }
                />
              )}
            </Truong>
          </Col>
          <Col xs={24} md={6}>
            <Truong<GiaTriForm> ten="trangThai" label="Trạng thái">
              {(o) => (
                <Select
                  {...o}
                  value={o.value as number}
                  options={[
                    { value: 1, label: 'Hoạt động' },
                    { value: 0, label: 'Ngừng' },
                  ]}
                />
              )}
            </Truong>
          </Col>
        </Row>

        {loai === 'EMAIL' ? (
          <>
            <Row gutter={12}>
              <Col xs={24} md={12}>
                <Truong<GiaTriForm> ten="host" label="Máy chủ SMTP" required>
                  {(o) => (
                    <Input {...o} value={o.value as string} placeholder="smtp.donvi.gov.vn" />
                  )}
                </Truong>
              </Col>
              <Col xs={12} md={6}>
                <Truong<GiaTriForm> ten="port" label="Cổng" required>
                  {(o) => (
                    <InputNumber<number>
                      {...o}
                      value={o.value as number}
                      min={1}
                      max={65535}
                      style={{ width: '100%' }}
                    />
                  )}
                </Truong>
              </Col>
              <Col xs={12} md={6}>
                <Truong<GiaTriForm> ten="suDungSsl" label="Dùng TLS">
                  {(o) => (
                    <Switch
                      checked={!!o.value}
                      onChange={o.onChange}
                      checkedChildren="Có"
                      unCheckedChildren="Không"
                    />
                  )}
                </Truong>
              </Col>
            </Row>

            <Row gutter={12}>
              <Col xs={24} md={12}>
                <Truong<GiaTriForm> ten="tenDangNhap" label="Tài khoản SMTP">
                  {(o) => <Input {...o} value={o.value as string} autoComplete="off" />}
                </Truong>
              </Col>
              <Col xs={24} md={12}>
                <Truong<GiaTriForm>
                  ten="matKhau"
                  label="Mật khẩu SMTP"
                  tooltip={
                    banGhi?.daDatMatKhau
                      ? 'Đã có mật khẩu. Để trống nếu giữ nguyên.'
                      : 'Mã hoá AES-256-GCM trước khi lưu.'
                  }
                >
                  {(o) => (
                    <Input.Password
                      {...o}
                      value={o.value as string}
                      autoComplete="new-password"
                      placeholder={banGhi?.daDatMatKhau ? '•••••••• (giữ nguyên)' : ''}
                    />
                  )}
                </Truong>
              </Col>
            </Row>

            <Row gutter={12}>
              <Col xs={24} md={12}>
                <Truong<GiaTriForm> ten="emailGuiDi" label="Địa chỉ gửi đi" required>
                  {(o) => (
                    <Input
                      {...o}
                      value={o.value as string}
                      placeholder="khongtraloi@donvi.gov.vn"
                    />
                  )}
                </Truong>
              </Col>
              <Col xs={24} md={12}>
                <Truong<GiaTriForm> ten="tenHienThi" label="Tên hiển thị">
                  {(o) => (
                    <Input {...o} value={o.value as string} placeholder="Hệ thống Sáng kiến" />
                  )}
                </Truong>
              </Col>
            </Row>
          </>
        ) : (
          <>
            <Truong<GiaTriForm> ten="apiEndpoint" label="Endpoint API" required>
              {(o) => (
                <Input
                  {...o}
                  value={o.value as string}
                  placeholder="https://api.nhacungcap.vn/sms/send"
                />
              )}
            </Truong>

            <Row gutter={12}>
              <Col xs={24} md={12}>
                <Truong<GiaTriForm>
                  ten="apiKey"
                  label="API key"
                  tooltip={
                    banGhi?.daDatApiKey
                      ? 'Đã có API key. Để trống nếu giữ nguyên.'
                      : 'Mã hoá AES-256-GCM trước khi lưu.'
                  }
                >
                  {(o) => (
                    <Input.Password
                      {...o}
                      value={o.value as string}
                      autoComplete="new-password"
                      placeholder={banGhi?.daDatApiKey ? '•••••••• (giữ nguyên)' : ''}
                    />
                  )}
                </Truong>
              </Col>
              <Col xs={24} md={12}>
                <Truong<GiaTriForm> ten="brandname" label="Brandname">
                  {(o) => <Input {...o} value={o.value as string} placeholder="SANGKIEN" />}
                </Truong>
              </Col>
            </Row>
          </>
        )}

        <Truong<GiaTriForm>
          ten="laMacDinh"
          label="Dùng làm cấu hình mặc định"
          tooltip="Công việc nền chọn cấu hình mặc định để gửi. Mỗi loại chỉ có một mặc định."
        >
          {(o) => (
            <Switch
              checked={!!o.value}
              onChange={o.onChange}
              checkedChildren="Có"
              unCheckedChildren="Không"
            />
          )}
        </Truong>
      </BieuMau>
    </Modal>
  );
}
