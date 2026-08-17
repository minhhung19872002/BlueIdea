import { useState } from 'react';
import {
  App,
  Button,
  Card,
  Col,
  DatePicker,
  Form,
  Input,
  Modal,
  Row,
  Select,
  Space,
  Table,
  Tag,
  Tooltip,
  Typography,
} from 'antd';
import {
  EditOutlined,
  ImportOutlined,
  KeyOutlined,
  LockOutlined,
  PlusOutlined,
  UnlockOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';

import { LoiApi } from '@/api/client';
import { apiDonVi, apiHeThong, type LuuNguoiDung } from '@/api/endpoints';
import { KhoiLoi, ngayGio } from '@/components/ThanhPhanChung';
import HopThoaiNhapNguoiDung from './HopThoaiNhapNguoiDung';

interface DongNguoiDung {
  id: string;
  tenDangNhap: string;
  hoTen: string;
  email?: string | null;
  dienThoai?: string | null;
  chucVu?: string | null;
  donViId?: string | null;
  trangThaiTaiKhoan: string;
  lanDangNhapCuoi?: string | null;
  mfaEnabled: boolean;
}

interface VaiTroTomTat {
  id: string;
  ten: string;
}

const TRANG_THAI: Record<string, { mau: string; ten: string }> = {
  HOAT_DONG: { mau: 'success', ten: 'Hoạt động' },
  KHOA: { mau: 'error', ten: 'Đã khóa' },
  CHO_KICH_HOAT: { mau: 'warning', ten: 'Chờ kích hoạt' },
};

/** Chức năng 43 — Quản lý người dùng. */
export default function TrangNguoiDung() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [trang, setTrang] = useState(1);
  const [soDong, setSoDong] = useState(20);
  const [tuKhoa, setTuKhoa] = useState('');
  const [donViId, setDonViId] = useState<string | undefined>();
  const [suaId, setSuaId] = useState<string | null>(null);
  const [moForm, setMoForm] = useState(false);
  const [moNhap, setMoNhap] = useState(false);

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['nguoi-dung', { trang, soDong, tuKhoa, donViId }],
    queryFn: () => apiHeThong.nguoiDung({ trang, soDong, tuKhoa, donViId }),
  });

  const { data: cacDonVi } = useQuery({ queryKey: ['don-vi-chon'], queryFn: apiDonVi.chon });

  const doiTrangThai = useMutation({
    mutationFn: ({ id, trangThai }: { id: string; trangThai: string }) =>
      apiHeThong.doiTrangThaiNguoiDung(id, trangThai),
    onSuccess: () => {
      message.success('Đã cập nhật trạng thái tài khoản');
      void queryClient.invalidateQueries({ queryKey: ['nguoi-dung'] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không cập nhật được.'),
  });

  const datLaiMatKhau = useMutation({
    mutationFn: (id: string) => apiHeThong.datLaiMatKhau(id),
    onSuccess: (ketQua) => hienMatKhauTam(ketQua.matKhauTam),
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không đặt lại được.'),
  });

  function hienMatKhauTam(matKhauTam: string) {
    modal.success({
      title: 'Mật khẩu tạm',
      width: 480,
      content: (
        <div>
          <Typography.Paragraph style={{ marginBottom: 8 }}>
            Bàn giao mật khẩu này cho người dùng. Hệ thống sẽ bắt đổi mật khẩu ngay ở lần đăng nhập
            kế tiếp.
          </Typography.Paragraph>
          <Typography.Paragraph copyable style={{ marginBottom: 8 }}>
            <Typography.Text code style={{ fontSize: 16 }}>
              {matKhauTam}
            </Typography.Text>
          </Typography.Paragraph>
          <Typography.Text type="danger">
            Mật khẩu chỉ lưu dưới dạng băm Argon2id nên không xem lại được sau khi đóng hộp thoại này.
          </Typography.Text>
        </div>
      ),
    });

    void queryClient.invalidateQueries({ queryKey: ['nguoi-dung'] });
  }

  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  const duLieu = (data?.duLieu ?? []) as unknown as DongNguoiDung[];

  return (
    <>
      <Card
        title="Quản lý người dùng"
        extra={
          <Space wrap>
            <Input.Search
              placeholder="Tìm theo họ tên hoặc tài khoản"
              allowClear
              style={{ width: 260 }}
              onSearch={(v) => {
                setTuKhoa(v);
                setTrang(1);
              }}
            />
            <Select
              style={{ width: 220 }}
              placeholder="Tất cả đơn vị"
              allowClear
              showSearch
              optionFilterProp="label"
              value={donViId}
              options={(cacDonVi ?? []).map((x) => ({ value: x.id, label: x.ten }))}
              onChange={setDonViId}
            />
            <Button icon={<ImportOutlined />} onClick={() => setMoNhap(true)}>
              Nhập Excel
            </Button>
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => {
                setSuaId(null);
                setMoForm(true);
              }}
            >
              Thêm tài khoản
            </Button>
          </Space>
        }
      >
        <Table<DongNguoiDung>
          rowKey="id"
          size="middle"
          loading={isLoading}
          dataSource={duLieu}
          scroll={{ x: 1100 }}
          columns={[
            { title: 'Tài khoản', dataIndex: 'tenDangNhap', width: 150 },
            { title: 'Họ và tên', dataIndex: 'hoTen', width: 210 },
            { title: 'Chức vụ', dataIndex: 'chucVu', responsive: ['lg'] },
            { title: 'Email', dataIndex: 'email', width: 220, responsive: ['xl'] },
            {
              title: 'Trạng thái',
              dataIndex: 'trangThaiTaiKhoan',
              width: 140,
              render: (v: string) => {
                const m = TRANG_THAI[v] ?? { mau: 'default', ten: v };
                return <Tag color={m.mau}>{m.ten}</Tag>;
              },
            },
            {
              title: 'Đăng nhập cuối',
              dataIndex: 'lanDangNhapCuoi',
              width: 160,
              responsive: ['lg'],
              render: (v: string | null) => ngayGio(v),
            },
            {
              title: '',
              key: 'thaoTac',
              width: 170,
              fixed: 'right',
              render: (_v, dong) => (
                <Space size={4}>
                  <Tooltip title="Sửa thông tin và vai trò">
                    <Button
                      size="small"
                      icon={<EditOutlined />}
                      onClick={() => {
                        setSuaId(dong.id);
                        setMoForm(true);
                      }}
                    />
                  </Tooltip>
                  <Tooltip title="Đặt lại mật khẩu">
                    <Button
                      size="small"
                      icon={<KeyOutlined />}
                      loading={datLaiMatKhau.isPending}
                      onClick={() =>
                        modal.confirm({
                          title: `Đặt lại mật khẩu cho ${dong.tenDangNhap}?`,
                          content:
                            'Toàn bộ phiên đăng nhập hiện tại của tài khoản này sẽ bị thu hồi ngay lập tức.',
                          okText: 'Đặt lại',
                          cancelText: 'Huỷ',
                          onOk: () => datLaiMatKhau.mutateAsync(dong.id),
                        })
                      }
                    />
                  </Tooltip>
                  {dong.trangThaiTaiKhoan === 'HOAT_DONG' ? (
                    <Tooltip title="Khoá tài khoản">
                      <Button
                        size="small"
                        danger
                        icon={<LockOutlined />}
                        onClick={() => doiTrangThai.mutate({ id: dong.id, trangThai: 'KHOA' })}
                      />
                    </Tooltip>
                  ) : (
                    <Tooltip title="Mở khoá tài khoản">
                      <Button
                        size="small"
                        icon={<UnlockOutlined />}
                        onClick={() => doiTrangThai.mutate({ id: dong.id, trangThai: 'HOAT_DONG' })}
                      />
                    </Tooltip>
                  )}
                </Space>
              ),
            },
          ]}
          pagination={{
            current: trang,
            pageSize: soDong,
            total: data?.tongSo ?? 0,
            showSizeChanger: true,
            showTotal: (t) => `Tổng ${t} người dùng`,
            onChange: (t, s) => {
              setTrang(t);
              setSoDong(s);
            },
          }}
        />
      </Card>

      {moNhap && (
        <HopThoaiNhapNguoiDung
          onDong={() => setMoNhap(false)}
          onXong={() => void queryClient.invalidateQueries({ queryKey: ['nguoi-dung'] })}
        />
      )}

      {moForm && (
        <FormNguoiDung
          id={suaId}
          onDong={() => setMoForm(false)}
          onXong={(matKhauTam) => {
            setMoForm(false);
            void queryClient.invalidateQueries({ queryKey: ['nguoi-dung'] });
            if (matKhauTam) hienMatKhauTam(matKhauTam);
          }}
        />
      )}
    </>
  );
}

// ---------------------------------------------------------------------------

interface GiaTriForm {
  tenDangNhap: string;
  hoTen: string;
  email?: string;
  dienThoai?: string;
  chucVu?: string;
  donViId?: string;
  soCccd?: string;
  ngaySinh?: dayjs.Dayjs;
  gioiTinh?: string;
  trangThaiTaiKhoan: string;
  vaiTroIds: string[];
}

function FormNguoiDung({
  id,
  onDong,
  onXong,
}: {
  id: string | null;
  onDong: () => void;
  onXong: (matKhauTam?: string) => void;
}) {
  const { message } = App.useApp();
  const [form] = Form.useForm<GiaTriForm>();

  const { data: chiTiet, isLoading } = useQuery({
    queryKey: ['nguoi-dung-chi-tiet', id],
    queryFn: () => apiHeThong.chiTietNguoiDung(id!),
    enabled: !!id,
  });

  const { data: cacDonVi } = useQuery({ queryKey: ['don-vi-chon'], queryFn: apiDonVi.chon });
  const { data: duLieuVaiTro } = useQuery({ queryKey: ['vai-tro'], queryFn: apiHeThong.vaiTro });

  const cacVaiTro = (duLieuVaiTro as { vaiTro?: VaiTroTomTat[] } | undefined)?.vaiTro ?? [];

  const luu = useMutation({
    mutationFn: (giaTri: LuuNguoiDung) =>
      id
        ? apiHeThong.suaNguoiDung(id, giaTri).then(() => undefined)
        : apiHeThong.themNguoiDung(giaTri).then((r) => r.matKhauTam),
    onSuccess: (matKhauTam) => {
      message.success(id ? 'Đã cập nhật tài khoản' : 'Đã tạo tài khoản');
      onXong(matKhauTam);
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không lưu được.'),
  });

  async function xacNhan() {
    const giaTri = await form.validateFields();

    luu.mutate({
      tenDangNhap: giaTri.tenDangNhap,
      hoTen: giaTri.hoTen,
      email: giaTri.email,
      dienThoai: giaTri.dienThoai,
      chucVu: giaTri.chucVu,
      donViId: giaTri.donViId,
      soCccd: giaTri.soCccd,
      ngaySinh: giaTri.ngaySinh?.format('YYYY-MM-DD'),
      gioiTinh: giaTri.gioiTinh,
      trangThaiTaiKhoan: giaTri.trangThaiTaiKhoan,
      vaiTroIds: giaTri.vaiTroIds,
    });
  }

  // Chờ nạp xong dữ liệu rồi mới dựng form, để initialValues chỉ áp dụng một lần.
  if (id && isLoading) {
    return (
      <Modal open title="Đang tải..." footer={null} onCancel={onDong}>
        <div style={{ height: 120 }} />
      </Modal>
    );
  }

  return (
    <Modal
      open
      width={760}
      title={id ? `Sửa tài khoản ${chiTiet?.tenDangNhap ?? ''}` : 'Thêm tài khoản'}
      okText={id ? 'Lưu thay đổi' : 'Tạo tài khoản'}
      cancelText="Huỷ"
      confirmLoading={luu.isPending}
      onOk={xacNhan}
      onCancel={onDong}
      destroyOnClose
    >
      <Form<GiaTriForm>
        form={form}
        layout="vertical"
        initialValues={{
          tenDangNhap: chiTiet?.tenDangNhap ?? '',
          hoTen: chiTiet?.hoTen ?? '',
          email: chiTiet?.email ?? undefined,
          dienThoai: chiTiet?.dienThoai ?? undefined,
          chucVu: chiTiet?.chucVu ?? undefined,
          donViId: chiTiet?.donViId ?? undefined,
          ngaySinh: chiTiet?.ngaySinh ? dayjs(chiTiet.ngaySinh) : undefined,
          gioiTinh: chiTiet?.gioiTinh ?? undefined,
          trangThaiTaiKhoan: chiTiet?.trangThaiTaiKhoan ?? 'HOAT_DONG',
          vaiTroIds: chiTiet?.vaiTroIds ?? [],
        }}
      >
        <Row gutter={12}>
          <Col xs={24} md={12}>
            <Form.Item
              name="tenDangNhap"
              label="Tên đăng nhập"
              tooltip={
                id ? 'Tên đăng nhập là định danh dùng trong nhật ký nên không đổi được.' : undefined
              }
              rules={[
                { required: true, message: 'Nhập tên đăng nhập' },
                {
                  pattern: /^[a-z0-9._-]+$/,
                  message: 'Chỉ dùng chữ thường không dấu, số và các ký tự . _ -',
                },
              ]}
            >
              <Input disabled={!!id} placeholder="vd: nguyen.van.a" />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item
              name="hoTen"
              label="Họ và tên"
              rules={[{ required: true, message: 'Nhập họ tên' }]}
            >
              <Input placeholder="Nguyễn Văn A" />
            </Form.Item>
          </Col>
        </Row>

        <Row gutter={12}>
          <Col xs={24} md={12}>
            <Form.Item
              name="email"
              label="Email"
              rules={[{ type: 'email', message: 'Email không hợp lệ' }]}
            >
              <Input placeholder="a.nguyen@donvi.gov.vn" />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item name="dienThoai" label="Điện thoại">
              <Input placeholder="09xxxxxxxx" />
            </Form.Item>
          </Col>
        </Row>

        <Row gutter={12}>
          <Col xs={24} md={12}>
            <Form.Item name="chucVu" label="Chức vụ">
              <Input placeholder="Chuyên viên" />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item name="donViId" label="Đơn vị công tác">
              <Select
                allowClear
                showSearch
                optionFilterProp="label"
                placeholder="Chọn đơn vị"
                options={(cacDonVi ?? []).map((x) => ({ value: x.id, label: x.ten }))}
              />
            </Form.Item>
          </Col>
        </Row>

        <Row gutter={12}>
          <Col xs={24} md={8}>
            <Form.Item name="ngaySinh" label="Ngày sinh">
              <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
            </Form.Item>
          </Col>
          <Col xs={24} md={8}>
            <Form.Item name="gioiTinh" label="Giới tính">
              <Select
                allowClear
                options={[
                  { value: 'NAM', label: 'Nam' },
                  { value: 'NU', label: 'Nữ' },
                  { value: 'KHAC', label: 'Khác' },
                ]}
              />
            </Form.Item>
          </Col>
          <Col xs={24} md={8}>
            <Form.Item name="trangThaiTaiKhoan" label="Trạng thái">
              <Select
                options={Object.entries(TRANG_THAI).map(([value, m]) => ({
                  value,
                  label: m.ten,
                }))}
              />
            </Form.Item>
          </Col>
        </Row>

        <Form.Item
          name="soCccd"
          label="Số CCCD"
          tooltip="Được mã hoá AES-256-GCM trước khi lưu; để trống nếu không muốn thay đổi."
        >
          <Input placeholder={id ? 'Để trống nếu giữ nguyên' : '0xxxxxxxxxxx'} />
        </Form.Item>

        <Form.Item
          name="vaiTroIds"
          label="Vai trò"
          rules={[{ required: true, message: 'Phải gán ít nhất một vai trò' }]}
        >
          <Select
            mode="multiple"
            placeholder="Chọn vai trò"
            optionFilterProp="label"
            options={cacVaiTro.map((v) => ({ value: v.id, label: v.ten }))}
          />
        </Form.Item>

        {!id && (
          <Typography.Text type="secondary">
            Hệ thống sinh mật khẩu tạm và hiển thị đúng một lần sau khi tạo. Người dùng bắt buộc đổi
            mật khẩu ở lần đăng nhập đầu tiên.
          </Typography.Text>
        )}
      </Form>
    </Modal>
  );
}
