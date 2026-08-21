import { useEffect, useState } from 'react';
import {
  App,
  Button,
  Card,
  Col,
  DatePicker,
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
  DeleteOutlined,
  EditOutlined,
  ImportOutlined,
  KeyOutlined,
  LockOutlined,
  PlusOutlined,
  UnlockOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';
import type { Dayjs } from 'dayjs';
import { z } from 'zod';

import { LoiApi } from '@/api/client';
import { useAuthStore } from '@/app/store/authStore';
import { BieuMau, Truong, useBieuMau } from '@/components/bieu-mau/BieuMau';
import { batBuoc, dienThoai, email, maKyThuat, tuyChon } from '@/components/bieu-mau/luat';
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

  // Ẩn nút khi không có quyền — máy chủ vẫn là nơi quyết định, đây chỉ để đỡ bấm vào rồi ăn 403.
  const duocXoa = useAuthStore((st) => st.coQuyen('NGUOI_DUNG.XOA'));

  const xoaNguoiDung = useMutation({
    mutationFn: (id: string) => apiHeThong.xoaNguoiDung(id),
    onSuccess: () => {
      message.success('Đã xoá tài khoản');
      void queryClient.invalidateQueries({ queryKey: ['nguoi-dung'] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không xoá được.'),
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
          scroll={{ x: 1260 }}
          columns={[
            { title: 'Tài khoản', dataIndex: 'tenDangNhap', width: 150 },
            { title: 'Họ và tên', dataIndex: 'hoTen', width: 210 },
            { title: 'Chức vụ', dataIndex: 'chucVu', width: 160, responsive: ['lg'] },
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
              width: duocXoa ? 208 : 170,
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
                  {duocXoa && (
                    <Tooltip title="Xoá tài khoản">
                      <Button
                        size="small"
                        danger
                        icon={<DeleteOutlined />}
                        loading={xoaNguoiDung.isPending}
                        onClick={() =>
                          modal.confirm({
                            title: `Xoá tài khoản ${dong.tenDangNhap}?`,
                            content:
                              'Tài khoản biến khỏi danh sách và mọi phiên đăng nhập bị thu hồi. '
                              + 'Lịch sử xử lý hồ sơ vẫn giữ nguyên để truy nguyên trách nhiệm. '
                              + 'Chỉ muốn ngăn đăng nhập tạm thời thì dùng khoá tài khoản.',
                            okText: 'Xoá',
                            okButtonProps: { danger: true },
                            cancelText: 'Huỷ',
                            onOk: () => xoaNguoiDung.mutateAsync(dong.id),
                          })
                        }
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

/**
 * Luật kiểm tra tài khoản người dùng.
 *
 * Số CCCD để trống khi SỬA nghĩa là giữ nguyên giá trị đang lưu — máy chủ mã hoá AES-256-GCM và
 * không trả lại giá trị cũ cho giao diện, nên không thể so sánh hay bắt nhập lại.
 */
const luatNguoiDung = z.object({
  tenDangNhap: maKyThuat(100).regex(
    /^[a-z0-9._-]+$/,
    'Chỉ dùng chữ thường không dấu, số và các ký tự . _ -',
  ),
  hoTen: batBuoc('Họ và tên'),
  email: email,
  dienThoai: dienThoai,
  chucVu: tuyChon(200),
  donViId: z.string().uuid().optional(),
  soCccd: z
    .string()
    .trim()
    .regex(/^\d{9}$|^\d{12}$/, 'Số CCCD phải gồm 9 hoặc 12 chữ số.')
    .optional()
    .or(z.literal('').transform(() => undefined)),
  ngaySinh: z.custom<Dayjs>((v) => v == null || dayjs.isDayjs(v)).optional(),
  gioiTinh: z.string().optional(),
  trangThaiTaiKhoan: z.string(),
  vaiTroIds: z.array(z.string()).min(1, 'Phải gán ít nhất một vai trò.'),
});

type GiaTriForm = z.infer<typeof luatNguoiDung>;

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


  const { data: chiTiet, isLoading } = useQuery({
    queryKey: ['nguoi-dung-chi-tiet', id],
    queryFn: () => apiHeThong.chiTietNguoiDung(id!),
    enabled: !!id,
  });

  const form = useBieuMau(luatNguoiDung, {
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
  } as GiaTriForm);

  /**
   * Nạp lại form khi chi tiết tài khoản về.
   *
   * `useForm` chạy ngay ở lần dựng đầu tiên, lúc đó truy vấn chi tiết còn đang chạy nên giá trị
   * mặc định bị chốt ở trạng thái rỗng. Không nạp lại thì mở "Sửa" sẽ ra một form trắng và người
   * dùng tưởng mất dữ liệu.
   */
  useEffect(() => {
    if (!chiTiet) return;

    form.reset({
      tenDangNhap: chiTiet.tenDangNhap,
      hoTen: chiTiet.hoTen,
      email: chiTiet.email ?? undefined,
      dienThoai: chiTiet.dienThoai ?? undefined,
      chucVu: chiTiet.chucVu ?? undefined,
      donViId: chiTiet.donViId ?? undefined,
      ngaySinh: chiTiet.ngaySinh ? dayjs(chiTiet.ngaySinh) : undefined,
      gioiTinh: chiTiet.gioiTinh ?? undefined,
      trangThaiTaiKhoan: chiTiet.trangThaiTaiKhoan,
      vaiTroIds: chiTiet.vaiTroIds ?? [],
    } as GiaTriForm);
  }, [chiTiet, form]);

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

  function xacNhan(giaTri: GiaTriForm) {
    return luu.mutateAsync({
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
      confirmLoading={luu.isPending || form.formState.isSubmitting}
      okButtonProps={{ htmlType: 'submit', form: 'form-nguoi-dung' }}
      onCancel={onDong}
      destroyOnClose
    >
      <BieuMau id="form-nguoi-dung" form={form} onGui={xacNhan}>
        <Row gutter={12}>
          <Col xs={24} md={12}>
            <Truong<GiaTriForm>
              ten="tenDangNhap"
              label="Tên đăng nhập"
              required
              tooltip={
                id ? 'Tên đăng nhập là định danh dùng trong nhật ký nên không đổi được.' : undefined
              }
            >
              {(o) => (
                <Input
                  {...o}
                  value={o.value as string}
                  disabled={!!id}
                  placeholder="vd: nguyen.van.a"
                />
              )}
            </Truong>
          </Col>
          <Col xs={24} md={12}>
            <Truong<GiaTriForm> ten="hoTen" label="Họ và tên" required>
              {(o) => <Input {...o} value={o.value as string} placeholder="Nguyễn Văn A" />}
            </Truong>
          </Col>
        </Row>

        <Row gutter={12}>
          <Col xs={24} md={12}>
            <Truong<GiaTriForm> ten="email" label="Email">
              {(o) => (
                <Input {...o} value={o.value as string} placeholder="a.nguyen@donvi.gov.vn" />
              )}
            </Truong>
          </Col>
          <Col xs={24} md={12}>
            <Truong<GiaTriForm> ten="dienThoai" label="Điện thoại">
              {(o) => <Input {...o} value={o.value as string} placeholder="09xxxxxxxx" />}
            </Truong>
          </Col>
        </Row>

        <Row gutter={12}>
          <Col xs={24} md={12}>
            <Truong<GiaTriForm> ten="chucVu" label="Chức vụ">
              {(o) => <Input {...o} value={o.value as string} placeholder="Chuyên viên" />}
            </Truong>
          </Col>
          <Col xs={24} md={12}>
            <Truong<GiaTriForm> ten="donViId" label="Đơn vị công tác">
              {(o) => (
                <Select
                  {...o}
                  value={o.value as string | undefined}
                  allowClear
                  showSearch
                  optionFilterProp="label"
                  placeholder="Chọn đơn vị"
                  options={(cacDonVi ?? []).map((x) => ({ value: x.id, label: x.ten }))}
                />
              )}
            </Truong>
          </Col>
        </Row>

        <Row gutter={12}>
          <Col xs={24} md={8}>
            <Truong<GiaTriForm> ten="ngaySinh" label="Ngày sinh">
              {(o) => (
                <DatePicker
                  value={(o.value as Dayjs | undefined) ?? null}
                  onChange={o.onChange}
                  onBlur={o.onBlur}
                  status={o.status}
                  style={{ width: '100%' }}
                  format="DD/MM/YYYY"
                />
              )}
            </Truong>
          </Col>
          <Col xs={24} md={8}>
            <Truong<GiaTriForm> ten="gioiTinh" label="Giới tính">
              {(o) => (
                <Select
                  {...o}
                  value={o.value as string | undefined}
                  allowClear
                  options={[
                    { value: 'NAM', label: 'Nam' },
                    { value: 'NU', label: 'Nữ' },
                    { value: 'KHAC', label: 'Khác' },
                  ]}
                />
              )}
            </Truong>
          </Col>
          <Col xs={24} md={8}>
            <Truong<GiaTriForm> ten="trangThaiTaiKhoan" label="Trạng thái">
              {(o) => (
                <Select
                  {...o}
                  value={o.value as string}
                  options={Object.entries(TRANG_THAI).map(([value, m]) => ({
                    value,
                    label: m.ten,
                  }))}
                />
              )}
            </Truong>
          </Col>
        </Row>

        <Truong<GiaTriForm>
          ten="soCccd"
          label="Số CCCD"
          tooltip="Được mã hoá AES-256-GCM trước khi lưu; để trống nếu không muốn thay đổi."
        >
          {(o) => (
            <Input
              {...o}
              value={o.value as string}
              placeholder={id ? 'Để trống nếu giữ nguyên' : '0xxxxxxxxxxx'}
            />
          )}
        </Truong>

        <Truong<GiaTriForm> ten="vaiTroIds" label="Vai trò" required>
          {(o) => (
            <Select
              {...o}
              value={o.value as string[]}
              mode="multiple"
              placeholder="Chọn vai trò"
              optionFilterProp="label"
              options={cacVaiTro.map((v) => ({ value: v.id, label: v.ten }))}
            />
          )}
        </Truong>

        {!id && (
          <Typography.Text type="secondary">
            Hệ thống sinh mật khẩu tạm và hiển thị đúng một lần sau khi tạo. Người dùng bắt buộc đổi
            mật khẩu ở lần đăng nhập đầu tiên.
          </Typography.Text>
        )}
      </BieuMau>
    </Modal>
  );
}
