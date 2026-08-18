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
  Tabs,
  Tag,
  Tooltip,
  Typography,
} from 'antd';
import {
  ApiOutlined,
  DeleteOutlined,
  EditOutlined,
  EyeOutlined,
  PlusOutlined,
  SyncOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { LoiApi } from '@/api/client';
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
  const [form] = Form.useForm();

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['he-thong-tich-hop'],
    queryFn: apiTichHop.danhSach,
  });

  const luu = useMutation({
    mutationFn: async (giaTri: Record<string, unknown>) => {
      const duLieu = {
        ma: (giaTri.ma as string).trim().toUpperCase(),
        ten: giaTri.ten as string,
        endpointBase: (giaTri.endpointBase as string) || null,
        loaiXacThuc: giaTri.loaiXacThuc as string,
        clientId: (giaTri.clientId as string) || null,
        clientSecret: (giaTri.clientSecret as string) || null,
        scope: (giaTri.scope as string) || null,
        tanSuatDongBo: giaTri.tanSuatDongBo as string,
        trangThai: giaTri.trangThai as number,
        cauHinhMapping: doiSangBanDo(
          (giaTri.mapping as { khoa: string; giaTri: string }[] | undefined) ?? [],
        ),
      };

      return dangSua ? apiTichHop.sua(dangSua.id, duLieu) : apiTichHop.them(duLieu);
    },
    onSuccess: () => {
      message.success(dangSua ? 'Đã cập nhật hệ thống liên thông' : 'Đã thêm hệ thống liên thông');
      setMoForm(false);
      setDangSua(null);
      form.resetFields();
      void queryClient.invalidateQueries({ queryKey: ['he-thong-tich-hop'] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không lưu được.'),
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
    form.resetFields();
    setMoForm(true);
  }

  function moSua(banGhi: HeThongTichHop) {
    setDangSua(banGhi);
    form.setFieldsValue({
      ...banGhi,
      clientSecret: undefined,
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
                scroll={{ x: 1000 }}
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
                    width: 170,
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
        confirmLoading={luu.isPending}
        onCancel={() => setMoForm(false)}
        onOk={async () => luu.mutate(await form.validateFields())}
      >
        <Form
          form={form}
          layout="vertical"
          initialValues={{ loaiXacThuc: 'API_KEY', tanSuatDongBo: 'THU_CONG', trangThai: 1 }}
        >
          <Space size="large" wrap style={{ display: 'flex' }}>
            <Form.Item
              name="ma"
              label="Mã hệ thống"
              rules={[
                { required: true, message: 'Nhập mã' },
                { pattern: /^[A-Za-z0-9_-]+$/, message: 'Mã chỉ gồm chữ, số, dấu _ và -' },
              ]}
            >
              <Input style={{ width: 220 }} placeholder="VD: IOC, TDKT" disabled={!!dangSua} />
            </Form.Item>
            <Form.Item name="ten" label="Tên hệ thống" rules={[{ required: true, message: 'Nhập tên' }]}>
              <Input style={{ width: 340 }} placeholder="VD: Trung tâm điều hành thông minh IOC" />
            </Form.Item>
          </Space>

          <Form.Item
            name="endpointBase"
            label="Endpoint gốc"
            rules={[{ type: 'url', message: 'Phải là URL http/https tuyệt đối' }]}
          >
            <Input placeholder="https://ioc.example.gov.vn/api" />
          </Form.Item>

          <Space size="large" wrap style={{ display: 'flex' }}>
            <Form.Item name="loaiXacThuc" label="Loại xác thực">
              <Select style={{ width: 240 }} options={LOAI_XAC_THUC} />
            </Form.Item>
            <Form.Item name="tanSuatDongBo" label="Tần suất đồng bộ">
              <Select style={{ width: 180 }} options={TAN_SUAT} />
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

          <Space size="large" wrap style={{ display: 'flex' }}>
            <Form.Item name="clientId" label="Client ID / tên khoá">
              <Input style={{ width: 260 }} />
            </Form.Item>
            <Form.Item
              name="clientSecret"
              label="Client secret / API key"
              tooltip={dangSua ? 'Để trống = giữ nguyên bí mật đang lưu.' : undefined}
            >
              <Input.Password
                style={{ width: 300 }}
                placeholder={dangSua ? 'Để trống nếu không đổi' : ''}
              />
            </Form.Item>
          </Space>

          <Form.Item name="scope" label="Scope (OAuth2)">
            <Input placeholder="VD: sangkien.read sangkien.write" />
          </Form.Item>

          <Typography.Text strong>Ánh xạ tên trường</Typography.Text>
          <Typography.Paragraph type="secondary" style={{ fontSize: 12, marginBottom: 8 }}>
            Đổi tên trường của hệ thống sáng kiến sang tên mà hệ thống ngoài yêu cầu, ví dụ
            <code> maHoSo → ma_sang_kien</code>.
          </Typography.Paragraph>

          <Form.List name="mapping">
            {(danhSach, { add, remove }) => (
              <>
                {danhSach.map((muc) => (
                  <Space key={muc.key} align="baseline" style={{ display: 'flex', marginBottom: 4 }}>
                    <Form.Item
                      name={[muc.name, 'khoa']}
                      rules={[{ required: true, message: 'Nhập tên trường nguồn' }]}
                      style={{ marginBottom: 0 }}
                    >
                      <Input placeholder="Trường nguồn" style={{ width: 220 }} />
                    </Form.Item>
                    <span>→</span>
                    <Form.Item
                      name={[muc.name, 'giaTri']}
                      rules={[{ required: true, message: 'Nhập tên trường đích' }]}
                      style={{ marginBottom: 0 }}
                    >
                      <Input placeholder="Trường đích" style={{ width: 220 }} />
                    </Form.Item>
                    <Button
                      type="text"
                      danger
                      icon={<DeleteOutlined />}
                      onClick={() => remove(muc.name)}
                    />
                  </Space>
                ))}
                <Button type="dashed" onClick={() => add()} icon={<PlusOutlined />}>
                  Thêm ánh xạ
                </Button>
              </>
            )}
          </Form.List>
        </Form>
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

function BangNhatKyDongBo({ heThong }: { heThong: HeThongTichHop[] }) {
  const [heThongId, setHeThongId] = useState<string | undefined>();

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['nhat-ky-dong-bo', heThongId],
    queryFn: () => apiTichHop.nhatKyDongBo(heThongId, 100),
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
        ]}
        pagination={{ pageSize: 20 }}
      />
    </>
  );
}
