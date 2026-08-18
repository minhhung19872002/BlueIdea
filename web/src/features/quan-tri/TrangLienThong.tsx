import { useState } from 'react';
import {
  App,
  Alert,
  Button,
  Card,
  Input,
  InputNumber,
  Modal,
  Select,
  Space,
  Table,
  Tabs,
  Tag,
  Tooltip,
  Typography,
} from 'antd';
import {
  ApiOutlined,
  RedoOutlined,
  DeleteOutlined,
  EditOutlined,
  EyeOutlined,
  PlusOutlined,
  SyncOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useFieldArray } from 'react-hook-form';
import { z } from 'zod';

import { LoiApi } from '@/api/client';
import { BieuMau, Truong, useBieuMau } from '@/components/bieu-mau/BieuMau';
import { batBuoc, trangThai, tuyChon } from '@/components/bieu-mau/luat';
import {
  apiDotDeNghi,
  apiTichHop,
  type BanGhiDongBo,
  type HeThongTichHop,
  type NhatKyDongBo,
} from '@/api/endpoints';
import { useAuthStore } from '@/app/store/authStore';
import { KhoiLoi, KhoiRong, ngayGio } from '@/components/ThanhPhanChung';

const LOAI_XAC_THUC = [
  { value: 'API_KEY', label: 'API Key' },
  { value: 'HMAC', label: 'HMAC (ký nội dung)' },
  { value: 'OAUTH2', label: 'OAuth2 Client Credentials' },
];

const TAN_SUAT = [
  { value: 'THU_CONG', label: 'Thủ công' },
  { value: 'HANG_NGAY', label: 'Hằng ngày' },
  { value: 'HANG_TUAN', label: 'Hằng tuần' },
];

/**
 * Luật kiểm tra hệ thống liên thông.
 *
 * Hai luật phụ thuộc lẫn nhau, nên phải nhìn cả form cùng lúc:
 *   - chọn OAUTH2 thì bắt buộc có Client ID (luồng client_credentials không chạy thiếu nó);
 *   - đồng bộ tự động (khác THU_CONG) thì bắt buộc có endpoint, nếu không job nền sẽ chạy đều
 *     đặn rồi thất bại lặng lẽ mỗi giờ mà không ai để ý.
 */
const luatHeThong = z
  .object({
    ma: batBuoc('Mã hệ thống', 50).regex(
      /^[A-Za-z0-9_-]+$/,
      'Mã chỉ gồm chữ, số, dấu _ và -',
    ),
    ten: batBuoc('Tên hệ thống'),
    endpointBase: z
      .string()
      .trim()
      .url('Phải là địa chỉ http/https tuyệt đối.')
      .optional()
      .or(z.literal('').transform(() => undefined)),
    loaiXacThuc: z.string(),
    clientId: tuyChon(200),
    clientSecret: tuyChon(500),
    scope: tuyChon(300),
    tanSuatDongBo: z.string(),
    trangThai: trangThai,
    mapping: z.array(
      z.object({
        khoa: batBuoc('Trường nguồn', 100),
        giaTri: batBuoc('Trường đích', 100),
      }),
    ),
  })
  .superRefine((v, ctx) => {
    if (v.loaiXacThuc === 'OAUTH2' && !v.clientId) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['clientId'],
        message: 'Xác thực OAuth2 bắt buộc có Client ID.',
      });
    }

    if (v.tanSuatDongBo !== 'THU_CONG' && !v.endpointBase) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['endpointBase'],
        message: 'Đồng bộ tự động cần endpoint, nếu không tác vụ nền sẽ thất bại lặng lẽ.',
      });
    }
  });

type GiaTriHeThong = z.infer<typeof luatHeThong>;

const MAC_DINH_HE_THONG: GiaTriHeThong = {
  ma: '',
  ten: '',
  loaiXacThuc: 'API_KEY',
  tanSuatDongBo: 'THU_CONG',
  trangThai: 1,
  mapping: [],
};

/** Chức năng 16, 41 — Cấu hình liên thông và đồng bộ sang hệ thống ngoài (IOC, TĐKT). */
export default function TrangLienThong() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const duocCauHinh = useAuthStore((s) => s.coQuyen('TICH_HOP.CAU_HINH'));
  const duocDongBo = useAuthStore((s) => s.coQuyen('TICH_HOP.DONG_BO'));

  const [dangSua, setDangSua] = useState<HeThongTichHop | null>(null);
  const [moForm, setMoForm] = useState(false);
  const [moXemTruoc, setMoXemTruoc] = useState(false);
  const [heThongDongBo, setHeThongDongBo] = useState<HeThongTichHop | null>(null);
  const form = useBieuMau(luatHeThong, MAC_DINH_HE_THONG);

  // Danh sách ánh xạ là mảng động — useFieldArray thay cho Form.List của antd.
  const anhXa = useFieldArray({ control: form.control, name: 'mapping' });

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['he-thong-tich-hop'],
    queryFn: apiTichHop.danhSach,
  });

  const luu = useMutation({
    mutationFn: async (giaTri: GiaTriHeThong) => {
      const duLieu = {
        ma: giaTri.ma.trim().toUpperCase(),
        ten: giaTri.ten,
        endpointBase: giaTri.endpointBase ?? null,
        loaiXacThuc: giaTri.loaiXacThuc,
        clientId: giaTri.clientId ?? null,
        clientSecret: giaTri.clientSecret ?? null,
        scope: giaTri.scope ?? null,
        tanSuatDongBo: giaTri.tanSuatDongBo,
        trangThai: giaTri.trangThai,
        cauHinhMapping: doiSangBanDo(giaTri.mapping),
      };

      return dangSua ? apiTichHop.sua(dangSua.id, duLieu) : apiTichHop.them(duLieu);
    },
    onSuccess: () => {
      message.success(dangSua ? 'Đã cập nhật hệ thống liên thông' : 'Đã thêm hệ thống liên thông');
      setMoForm(false);
      setDangSua(null);
      form.reset(MAC_DINH_HE_THONG);
      void queryClient.invalidateQueries({ queryKey: ['he-thong-tich-hop'] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không lưu được.'),
  });

  /**
   * Thử kết nối gửi payload RỖNG sang hệ thống đích: chỉ cần biết endpoint có sống và thông tin
   * xác thực có đúng không, không được đẩy dữ liệu rác sang hệ thống bên kia.
   */
  const thuKetNoi = useMutation({
    mutationFn: (id: string) => apiTichHop.thuKetNoi(id),
    onSuccess: (kq) =>
      (kq.thanhCong ? modal.success : modal.error)({
        title: kq.thanhCong ? 'Kết nối thành công' : 'Không kết nối được',
        content: (
          <Space direction="vertical" size={2} style={{ marginTop: 8 }}>
            <span>
              <strong>{kq.tenHeThong}</strong>
            </span>
            <Typography.Text type="secondary" style={{ fontSize: 12, wordBreak: 'break-all' }}>
              {kq.endpoint ?? 'Chưa khai báo endpoint'}
            </Typography.Text>
            <span>Thời gian phản hồi: {kq.soMiliGiay} ms</span>
            {kq.thongBao && <span>{kq.thongBao}</span>}
          </Space>
        ),
      }),
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không thử được kết nối.'),
  });

  const xoa = useMutation({
    mutationFn: (id: string) => apiTichHop.xoa(id),
    onSuccess: () => {
      message.success('Đã xoá hệ thống liên thông');
      void queryClient.invalidateQueries({ queryKey: ['he-thong-tich-hop'] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không xoá được.'),
  });

  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  function moTaoMoi() {
    setDangSua(null);
    form.reset(MAC_DINH_HE_THONG);
    setMoForm(true);
  }

  function moSua(banGhi: HeThongTichHop) {
    setDangSua(banGhi);
    form.reset({
      ma: banGhi.ma,
      ten: banGhi.ten,
      endpointBase: banGhi.endpointBase ?? undefined,
      loaiXacThuc: banGhi.loaiXacThuc,
      clientId: banGhi.clientId ?? undefined,
      // Bí mật không bao giờ trả về giao diện — để trống = giữ nguyên.
      clientSecret: undefined,
      scope: banGhi.scope ?? undefined,
      tanSuatDongBo: banGhi.tanSuatDongBo,
      trangThai: banGhi.trangThai as 0 | 1,
      mapping: Object.entries(banGhi.cauHinhMapping ?? {}).map(([khoa, giaTri]) => ({
        khoa,
        giaTri,
      })),
    });
    setMoForm(true);
  }

  return (
    <Card
      title={
        <Space>
          <ApiOutlined />
          <span>Liên thông hệ thống ngoài</span>
        </Space>
      }
      extra={
        <Space>
          {duocDongBo && (
            <Button icon={<EyeOutlined />} onClick={() => setMoXemTruoc(true)}>
              Xem trước dữ liệu
            </Button>
          )}
          {duocCauHinh && (
            <Button type="primary" icon={<PlusOutlined />} onClick={moTaoMoi}>
              Thêm hệ thống
            </Button>
          )}
        </Space>
      }
    >
      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 12 }}
        message="Bí mật (client secret / API key) được mã hoá khi lưu và không bao giờ trả về giao diện."
        description="Khi sửa, để trống ô bí mật nghĩa là giữ nguyên giá trị đang dùng."
      />

      <Tabs
        items={[
          {
            key: 'he-thong',
            label: `Hệ thống liên thông (${data?.length ?? 0})`,
            children: (
              <Table<HeThongTichHop>
                rowKey="id"
                size="middle"
                loading={isLoading}
                dataSource={data ?? []}
                scroll={{ x: 1400 }}
                pagination={false}
                locale={{ emptyText: <KhoiRong moTa="Chưa khai báo hệ thống liên thông nào." /> }}
                columns={[
                  { title: 'Mã', dataIndex: 'ma', width: 130 },
                  { title: 'Tên hệ thống', dataIndex: 'ten', width: 220 },
                  {
                    title: 'Endpoint',
                    dataIndex: 'endpointBase',
                    width: 260,
                    ellipsis: true,
                    responsive: ['lg'],
                    render: (v: string | null) =>
                      v ?? <Typography.Text type="secondary">—</Typography.Text>,
                  },
                  {
                    title: 'Xác thực',
                    dataIndex: 'loaiXacThuc',
                    width: 150,
                    render: (v: string, dong) => (
                      <Space direction="vertical" size={0}>
                        <span>{LOAI_XAC_THUC.find((x) => x.value === v)?.label ?? v}</span>
                        {dong.daDatBiMat ? (
                          <Tag color="success">Đã đặt bí mật</Tag>
                        ) : (
                          <Tag color="warning">Chưa đặt bí mật</Tag>
                        )}
                      </Space>
                    ),
                  },
                  {
                    title: 'Tần suất',
                    dataIndex: 'tanSuatDongBo',
                    width: 120,
                    responsive: ['xl'],
                    render: (v: string) => TAN_SUAT.find((x) => x.value === v)?.label ?? v,
                  },
                  {
                    title: 'Đồng bộ cuối',
                    dataIndex: 'lanDongBoCuoi',
                    width: 150,
                    render: (v: string | null) => ngayGio(v),
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
                    width: 210,
                    fixed: 'right',
                    render: (_v, dong) => (
                      <Space>
                        {duocDongBo && (
                          <Tooltip title="Đẩy danh sách sáng kiến đã công bố sang hệ thống này">
                            <Button
                              size="small"
                              icon={<SyncOutlined />}
                              disabled={dong.trangThai !== 1}
                              onClick={() => setHeThongDongBo(dong)}
                            >
                              Đồng bộ
                            </Button>
                          </Tooltip>
                        )}
                        {duocCauHinh && (
                          <>
                            <Tooltip title="Gọi thử endpoint để kiểm tra địa chỉ và thông tin xác thực">
                              <Button
                                type="text"
                                icon={<ApiOutlined />}
                                loading={thuKetNoi.isPending && thuKetNoi.variables === dong.id}
                                onClick={() => thuKetNoi.mutate(dong.id)}
                              />
                            </Tooltip>
                            <Button
                              type="text"
                              icon={<EditOutlined />}
                              onClick={() => moSua(dong)}
                            />
                            <Button
                              type="text"
                              danger
                              icon={<DeleteOutlined />}
                              onClick={() =>
                                modal.confirm({
                                  title: 'Xác nhận xoá',
                                  content: `Xoá hệ thống liên thông "${dong.ten}"?`,
                                  okText: 'Xoá',
                                  okButtonProps: { danger: true },
                                  cancelText: 'Huỷ',
                                  onOk: () => xoa.mutateAsync(dong.id),
                                })
                              }
                            />
                          </>
                        )}
                      </Space>
                    ),
                  },
                ]}
              />
            ),
          },
          {
            key: 'nhat-ky',
            label: 'Nhật ký đồng bộ',
            children: <BangNhatKyDongBo heThong={data ?? []} />,
          },
        ]}
      />

      <Modal
        open={moForm}
        width={680}
        title={dangSua ? `Sửa hệ thống: ${dangSua.ten}` : 'Thêm hệ thống liên thông'}
        okText="Lưu"
        cancelText="Huỷ"
        confirmLoading={luu.isPending || form.formState.isSubmitting}
        onCancel={() => setMoForm(false)}
        okButtonProps={{ htmlType: 'submit', form: 'form-he-thong' }}
      >
        <BieuMau id="form-he-thong" form={form} onGui={(giaTri) => luu.mutateAsync(giaTri)}>
          <Space size="large" wrap style={{ display: 'flex' }}>
            <Truong<GiaTriHeThong> ten="ma" label="Mã hệ thống" required>
              {(o) => (
                <Input
                  {...o}
                  value={o.value as string}
                  style={{ width: 220 }}
                  placeholder="VD: IOC, TDKT"
                  disabled={!!dangSua}
                />
              )}
            </Truong>
            <Truong<GiaTriHeThong> ten="ten" label="Tên hệ thống" required>
              {(o) => (
                <Input
                  {...o}
                  value={o.value as string}
                  style={{ width: 340 }}
                  placeholder="VD: Trung tâm điều hành thông minh IOC"
                />
              )}
            </Truong>
          </Space>

          <Truong<GiaTriHeThong> ten="endpointBase" label="Endpoint gốc">
            {(o) => (
              <Input
                {...o}
                value={o.value as string}
                placeholder="https://ioc.example.gov.vn/api"
              />
            )}
          </Truong>

          <Space size="large" wrap style={{ display: 'flex' }}>
            <Truong<GiaTriHeThong> ten="loaiXacThuc" label="Loại xác thực">
              {(o) => (
                <Select
                  {...o}
                  value={o.value as string}
                  style={{ width: 240 }}
                  options={LOAI_XAC_THUC}
                />
              )}
            </Truong>
            <Truong<GiaTriHeThong> ten="tanSuatDongBo" label="Tần suất đồng bộ">
              {(o) => (
                <Select
                  {...o}
                  value={o.value as string}
                  style={{ width: 180 }}
                  options={TAN_SUAT}
                />
              )}
            </Truong>
            <Truong<GiaTriHeThong> ten="trangThai" label="Trạng thái">
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

          <Space size="large" wrap style={{ display: 'flex' }}>
            <Truong<GiaTriHeThong> ten="clientId" label="Client ID / tên khoá">
              {(o) => <Input {...o} value={o.value as string} style={{ width: 260 }} />}
            </Truong>
            <Truong<GiaTriHeThong>
              ten="clientSecret"
              label="Client secret / API key"
              tooltip={dangSua ? 'Để trống = giữ nguyên bí mật đang lưu.' : undefined}
            >
              {(o) => (
                <Input.Password
                  {...o}
                  value={o.value as string}
                  style={{ width: 300 }}
                  placeholder={dangSua ? 'Để trống nếu không đổi' : ''}
                />
              )}
            </Truong>
          </Space>

          <Truong<GiaTriHeThong> ten="scope" label="Scope (OAuth2)">
            {(o) => (
              <Input
                {...o}
                value={o.value as string}
                placeholder="VD: sangkien.read sangkien.write"
              />
            )}
          </Truong>

          <Typography.Text strong>Ánh xạ tên trường</Typography.Text>
          <Typography.Paragraph type="secondary" style={{ fontSize: 12, marginBottom: 8 }}>
            Đổi tên trường của hệ thống sáng kiến sang tên mà hệ thống ngoài yêu cầu, ví dụ
            <code> maHoSo → ma_sang_kien</code>.
          </Typography.Paragraph>

          {anhXa.fields.map((muc, i) => (
            <Space key={muc.id} align="baseline" style={{ display: 'flex', marginBottom: 4 }}>
              <Truong<GiaTriHeThong>
                ten={`mapping.${i}.khoa` as `mapping.${number}.khoa`}
                style={{ marginBottom: 0 }}
              >
                {(o) => (
                  <Input
                    {...o}
                    value={o.value as string}
                    placeholder="Trường nguồn"
                    style={{ width: 220 }}
                  />
                )}
              </Truong>
              <span>→</span>
              <Truong<GiaTriHeThong>
                ten={`mapping.${i}.giaTri` as `mapping.${number}.giaTri`}
                style={{ marginBottom: 0 }}
              >
                {(o) => (
                  <Input
                    {...o}
                    value={o.value as string}
                    placeholder="Trường đích"
                    style={{ width: 220 }}
                  />
                )}
              </Truong>
              <Button
                type="text"
                danger
                icon={<DeleteOutlined />}
                onClick={() => anhXa.remove(i)}
              />
            </Space>
          ))}
          <Button
            type="dashed"
            onClick={() => anhXa.append({ khoa: '', giaTri: '' })}
            icon={<PlusOutlined />}
          >
            Thêm ánh xạ
          </Button>
        </BieuMau>
      </Modal>

      <ModalXemTruoc mo={moXemTruoc} onDong={() => setMoXemTruoc(false)} />

      {heThongDongBo && (
        <ModalDongBo heThong={heThongDongBo} onDong={() => setHeThongDongBo(null)} />
      )}
    </Card>
  );
}

function doiSangBanDo(danhSach: { khoa: string; giaTri: string }[]) {
  if (danhSach.length === 0) return null;

  return Object.fromEntries(danhSach.filter((x) => x.khoa).map((x) => [x.khoa, x.giaTri]));
}

// ---------------------------------------------------------------------------

const COT_XEM_TRUOC = [
  { title: 'Mã hồ sơ', dataIndex: 'maHoSo', width: 130 },
  { title: 'Tên sáng kiến', dataIndex: 'tenSangKien' },
  { title: 'Tác giả chính', dataIndex: 'tacGiaChinh', width: 170 },
  { title: 'Đơn vị', dataIndex: 'donVi', width: 180, responsive: ['lg' as const] },
  { title: 'Mức công nhận', dataIndex: 'mucCongNhan', width: 150 },
  { title: 'Số quyết định', dataIndex: 'soQuyetDinh', width: 150, responsive: ['xl' as const] },
];

function ModalXemTruoc({ mo, onDong }: { mo: boolean; onDong: () => void }) {
  const [dotDeNghiId, setDotDeNghiId] = useState<string | undefined>();
  const [nam, setNam] = useState<number | undefined>();

  const { data: cacDot } = useQuery({ queryKey: ['dot-chon'], queryFn: apiDotDeNghi.chon, enabled: mo });

  const { data, isFetching, error, refetch } = useQuery({
    queryKey: ['xem-truoc-dong-bo', dotDeNghiId, nam],
    queryFn: () => apiTichHop.xemTruoc({ dotDeNghiId, nam }),
    enabled: mo,
  });

  return (
    <Modal
      open={mo}
      width={900}
      title="Xem trước dữ liệu sẽ đẩy đi"
      footer={<Button onClick={onDong}>Đóng</Button>}
      onCancel={onDong}
    >
      <Space wrap style={{ marginBottom: 12 }}>
        <Select
          style={{ width: 260 }}
          allowClear
          placeholder="Lọc theo đợt đề nghị"
          value={dotDeNghiId}
          options={(cacDot ?? []).map((x) => ({ value: x.id, label: x.ten }))}
          onChange={setDotDeNghiId}
        />
        <InputNumber
          style={{ width: 140 }}
          min={2000}
          max={2100}
          placeholder="Năm"
          value={nam}
          onChange={(v) => setNam(v ?? undefined)}
        />
      </Space>

      {error ? (
        <KhoiLoi loi={error} thuLai={refetch} />
      ) : (
        <Table<BanGhiDongBo>
          rowKey="maHoSo"
          size="small"
          loading={isFetching}
          dataSource={data ?? []}
          columns={COT_XEM_TRUOC}
          scroll={{ x: 900 }}
          pagination={{ pageSize: 10, showTotal: (t) => `${t} bản ghi sẽ được đẩy đi` }}
        />
      )}
    </Modal>
  );
}

// ---------------------------------------------------------------------------

function ModalDongBo({ heThong, onDong }: { heThong: HeThongTichHop; onDong: () => void }) {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [dotDeNghiId, setDotDeNghiId] = useState<string | undefined>();
  const [nam, setNam] = useState<number | undefined>();

  const { data: cacDot } = useQuery({ queryKey: ['dot-chon'], queryFn: apiDotDeNghi.chon });

  const chay = useMutation({
    mutationFn: () => apiTichHop.dongBo(heThong.id, { dotDeNghiId, nam }),
    onSuccess: (ketQua) => {
      if (ketQua.thatBai > 0) {
        message.warning(
          `Đồng bộ xong: ${ketQua.thanhCong}/${ketQua.tongBanGhi} bản ghi thành công, ${ketQua.thatBai} lỗi.`,
        );
      } else {
        message.success(`Đã đẩy ${ketQua.thanhCong}/${ketQua.tongBanGhi} bản ghi sang ${ketQua.tenHeThong}`);
      }

      void queryClient.invalidateQueries({ queryKey: ['he-thong-tich-hop'] });
      void queryClient.invalidateQueries({ queryKey: ['nhat-ky-dong-bo'] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Đồng bộ thất bại.'),
  });

  return (
    <Modal
      open
      title={`Đồng bộ sang ${heThong.ten}`}
      okText="Chạy đồng bộ"
      cancelText="Đóng"
      confirmLoading={chay.isPending}
      onCancel={onDong}
      onOk={() => chay.mutate()}
    >
      <Alert
        type="warning"
        showIcon
        style={{ marginBottom: 12 }}
        message="Chỉ sáng kiến đã công bố kết quả mới được đẩy đi."
        description="Mỗi lần chạy ghi một dòng vào nhật ký đồng bộ kèm phản hồi của hệ thống ngoài."
      />

      <Space direction="vertical" style={{ width: '100%' }}>
        <Select
          style={{ width: '100%' }}
          allowClear
          placeholder="Đợt đề nghị (để trống = tất cả)"
          value={dotDeNghiId}
          options={(cacDot ?? []).map((x) => ({ value: x.id, label: x.ten }))}
          onChange={setDotDeNghiId}
        />
        <InputNumber
          style={{ width: '100%' }}
          min={2000}
          max={2100}
          placeholder="Năm công nhận (tuỳ chọn)"
          value={nam}
          onChange={(v) => setNam(v ?? undefined)}
        />
      </Space>

      {chay.data && (
        <Alert
          style={{ marginTop: 12 }}
          type={chay.data.thatBai > 0 ? 'warning' : 'success'}
          showIcon
          message={`Kết quả: ${chay.data.thanhCong} thành công / ${chay.data.tongBanGhi} bản ghi`}
          description={chay.data.thongBaoLoi ?? undefined}
        />
      )}
    </Modal>
  );
}

// ---------------------------------------------------------------------------

/** Bảng nhật ký đồng bộ — dùng chung cho tab trong màn hình Liên thông và trang Nhật ký. */
export function BangNhatKyDongBo({ heThong }: { heThong: HeThongTichHop[] }) {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const [heThongId, setHeThongId] = useState<string | undefined>();

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['nhat-ky-dong-bo', heThongId],
    queryFn: () => apiTichHop.nhatKyDongBo(heThongId, 100),
  });

  /** Chỉ gửi lại lần đồng bộ lỗi — gửi lại lần đã thành công là đẩy trùng dữ liệu sang bên kia. */
  const guiLai = useMutation({
    mutationFn: (nhatKyId: string) => apiTichHop.guiLai(nhatKyId),
    onSuccess: (kq) => {
      message.success(
        `Đã gửi lại: ${kq.thanhCong}/${kq.tongBanGhi} bản ghi thành công` +
          (kq.thatBai > 0 ? `, ${kq.thatBai} lỗi` : ''),
      );
      void queryClient.invalidateQueries({ queryKey: ['nhat-ky-dong-bo'] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không gửi lại được.'),
  });

  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  const tenHeThong = new Map(heThong.map((x) => [x.id, x.ten]));

  return (
    <>
      <Select
        style={{ width: 280, marginBottom: 12 }}
        allowClear
        placeholder="Lọc theo hệ thống"
        value={heThongId}
        options={heThong.map((x) => ({ value: x.id, label: x.ten }))}
        onChange={setHeThongId}
      />

      <Table<NhatKyDongBo>
        rowKey="id"
        size="small"
        loading={isLoading}
        dataSource={data ?? []}
        scroll={{ x: 900 }}
        locale={{ emptyText: <KhoiRong moTa="Chưa có lần đồng bộ nào." /> }}
        expandable={{
          expandedRowRender: (dong) => (
            <Typography.Paragraph style={{ marginBottom: 0, whiteSpace: 'pre-wrap' }}>
              {dong.thongBaoLoi ?? 'Không có thông báo lỗi.'}
            </Typography.Paragraph>
          ),
          rowExpandable: (dong) => !!dong.thongBaoLoi,
        }}
        columns={[
          {
            title: 'Hệ thống',
            dataIndex: 'heThongTichHopId',
            width: 200,
            render: (v: string) => tenHeThong.get(v) ?? v,
          },
          { title: 'Chiều', dataIndex: 'chieu', width: 90 },
          { title: 'Tổng', dataIndex: 'tongBanGhi', width: 80, align: 'right' },
          { title: 'Thành công', dataIndex: 'thanhCong', width: 110, align: 'right' },
          { title: 'Thất bại', dataIndex: 'thatBai', width: 100, align: 'right' },
          {
            title: 'Trạng thái',
            dataIndex: 'trangThaiDongBo',
            width: 130,
            render: (v: string) => (
              <Tag color={v === 'THANH_CONG' ? 'success' : v === 'DANG_CHAY' ? 'processing' : 'error'}>
                {v}
              </Tag>
            ),
          },
          {
            title: 'Bắt đầu',
            dataIndex: 'thoiGianBatDau',
            width: 150,
            render: (v: string) => ngayGio(v),
          },
          {
            title: '',
            key: 'thaoTac',
            width: 120,
            fixed: 'right',
            render: (_v, dong) =>
              dong.trangThaiDongBo === 'THANH_CONG' ? (
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  —
                </Typography.Text>
              ) : (
                <Tooltip title="Chạy lại lần đồng bộ này với đúng tham số cũ">
                  <Button
                    size="small"
                    icon={<RedoOutlined />}
                    loading={guiLai.isPending && guiLai.variables === dong.id}
                    onClick={() => guiLai.mutate(dong.id)}
                  >
                    Gửi lại
                  </Button>
                </Tooltip>
              ),
          },
        ]}
        pagination={{ pageSize: 20 }}
      />
    </>
  );
}
