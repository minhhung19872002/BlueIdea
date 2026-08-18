import { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  App,
  Alert,
  Button,
  Card,
  Col,
  DatePicker,
  Input,
  InputNumber,
  Modal,
  Row,
  Select,
  Space,
  Switch,
  Table,
  Tag,
  Tooltip,
  Typography,
} from 'antd';
import dayjs from 'dayjs';
import type { Dayjs } from 'dayjs';
import { z } from 'zod';
import {
  CopyOutlined,
  DeleteOutlined,
  EditOutlined,
  LockOutlined,
  PlusOutlined,
  StopOutlined,
  UnlockOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { LoiApi } from '@/api/client';
import { BieuMau, Truong, useBieuMau } from '@/components/bieu-mau/BieuMau';
import { batBuoc, maDanhMuc, soNguyen, trangThai, tuyChon } from '@/components/bieu-mau/luat';
import {
  apiDonVi,
  apiDotDeNghi,
  apiQuyTrinh,
  apiTieuChi,
  type DotDeNghiQuanLy,
} from '@/api/endpoints';
import { KhoiLoi, ngayGio } from '@/components/ThanhPhanChung';
import { DaiTabTrang } from '@/components/DaiTabTrang';
import { DS_TAB_DANH_MUC } from '@/features/quan-tri/danhMucTab';

const TRANG_THAI_DOT: Record<string, { mau: string; ten: string }> = {
  NHAP: { mau: 'default', ten: 'Nháp' },
  DANG_MO: { mau: 'success', ten: 'Đang mở' },
  DA_DONG: { mau: 'warning', ten: 'Đã đóng' },
  DA_KHOA: { mau: 'error', ten: 'Đã khoá' },
};

const CAP_XET_DUYET: Record<string, string> = {
  CO_SO: 'Cấp cơ sở',
  THANH_PHO: 'Cấp thành phố',
  TINH: 'Cấp tỉnh',
};

/**
 * Luật kiểm tra đợt đề nghị.
 *
 * Chặn "hạn chấm điểm sớm hơn hạn nộp hồ sơ": cấu hình đó khiến hội đồng phải chấm xong trước
 * khi hồ sơ kịp nộp, và lỗi chỉ lộ ra khi đợt đã mở và người dân đã bắt đầu nộp.
 */
const luatDot = z
  .object({
    ma: maDanhMuc(),
    ten: batBuoc('Tên đợt'),
    nam: soNguyen('Năm', 2000, 2100),
    capXetDuyet: z.string(),
    hanNopHoSo: z.custom<Dayjs>((v) => v == null || dayjs.isDayjs(v)).nullish(),
    hanChamDiem: z.custom<Dayjs>((v) => v == null || dayjs.isDayjs(v)).nullish(),
    quyTrinhId: z.string().uuid().optional().nullable(),
    boTieuChiId: z.string().uuid().optional().nullable(),
    donViApDungIds: z.array(z.string()),
    moTa: tuyChon(),
    tuDongKhoa: z.boolean(),
    thuTu: soNguyen('Thứ tự', 0, 9999),
    trangThai: trangThai,
  })
  .superRefine((v, ctx) => {
    if (v.hanNopHoSo && v.hanChamDiem && v.hanChamDiem.isBefore(v.hanNopHoSo)) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['hanChamDiem'],
        message: 'Hạn chấm điểm phải sau hạn nộp hồ sơ.',
      });
    }
  });

type GiaTriDot = z.infer<typeof luatDot>;

const MAC_DINH_DOT: GiaTriDot = {
  ma: '',
  ten: '',
  nam: new Date().getFullYear(),
  capXetDuyet: 'CO_SO',
  tuDongKhoa: true,
  thuTu: 0,
  trangThai: 1,
  donViApDungIds: [],
};

/** Luật cho hộp thoại sao chép đợt. */
const luatSaoChepDot = z.object({
  ma: maDanhMuc(),
  ten: batBuoc('Tên đợt mới'),
  nam: soNguyen('Năm', 2000, 2100),
});

type GiaTriSaoChepDot = z.infer<typeof luatSaoChepDot>;

/**
 * Chức năng 3 — Quản lý vòng đời đợt đề nghị.
 *
 * Tách khỏi màn hình danh mục dùng chung vì đợt có trạng thái vòng đời riêng
 * (nháp → đang mở → đã đóng → đã khoá) và các thao tác không có ở danh mục khác.
 */
export default function TrangDotDeNghi() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [trang, setTrang] = useState(1);
  const [soDong, setSoDong] = useState(20);
  const [tuKhoa, setTuKhoa] = useState('');
  const [dangSaoChep, setDangSaoChep] = useState<DotDeNghiQuanLy | null>(null);
  const [dangSua, setDangSua] = useState<DotDeNghiQuanLy | null>(null);
  const [moForm, setMoForm] = useState(false);
  const form = useBieuMau(luatSaoChepDot);
  const formDot = useBieuMau(luatDot, MAC_DINH_DOT);

  const thamSo = { trang, soDong, tuKhoa };

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['dot-de-nghi-quan-ly', thamSo],
    queryFn: () => apiDotDeNghi.danhSachQuanLy(thamSo),
  });

  const { data: cacQuyTrinh } = useQuery({
    queryKey: ['quy-trinh-chon'],
    queryFn: apiQuyTrinh.chon,
    enabled: moForm,
  });

  const { data: cacBoTieuChi } = useQuery({
    queryKey: ['bo-tieu-chi-chon'],
    queryFn: apiTieuChi.chon,
    enabled: moForm,
  });

  const { data: cacDonVi } = useQuery({
    queryKey: ['don-vi-chon'],
    queryFn: apiDonVi.chon,
    enabled: moForm,
  });

  function lamMoi() {
    void queryClient.invalidateQueries({ queryKey: ['dot-de-nghi-quan-ly'] });
    void queryClient.invalidateQueries({ queryKey: ['dot-chon'] });
  }

  const doiTrangThai = useMutation({
    mutationFn: ({ id, hanhDong }: { id: string; hanhDong: 'mo' | 'dong' | 'khoa' }) =>
      hanhDong === 'mo'
        ? apiDotDeNghi.moDot(id)
        : hanhDong === 'dong'
          ? apiDotDeNghi.dongDot(id)
          : apiDotDeNghi.khoaDot(id),
    onSuccess: (_kq, bien) => {
      message.success(
        bien.hanhDong === 'mo'
          ? 'Đã mở đợt — hệ thống bắt đầu nhận hồ sơ'
          : bien.hanhDong === 'dong'
            ? 'Đã đóng đợt — không nhận hồ sơ mới, hồ sơ đã nộp vẫn xử lý tiếp'
            : 'Đã khoá đợt — toàn bộ dữ liệu của đợt chuyển sang chỉ đọc',
      );
      lamMoi();
    },
    onError: (loi) =>
      modal.error({
        title: 'Không đổi được trạng thái đợt',
        content: loi instanceof LoiApi ? loi.message : 'Đã xảy ra lỗi.',
      }),
  });

  const luu = useMutation({
    mutationFn: async (giaTri: GiaTriDot) => {
      const duLieu = {
        ...giaTri,
        hanNopHoSo: giaTri.hanNopHoSo ? giaTri.hanNopHoSo.toISOString() : null,
        hanChamDiem: giaTri.hanChamDiem ? giaTri.hanChamDiem.toISOString() : null,
        donViApDungIds: giaTri.donViApDungIds ?? [],
      };

      return dangSua ? apiDotDeNghi.sua(dangSua.id, duLieu) : apiDotDeNghi.them(duLieu);
    },
    onSuccess: () => {
      message.success(dangSua ? 'Đã cập nhật đợt' : 'Đã thêm đợt');
      setMoForm(false);
      setDangSua(null);
      formDot.reset(MAC_DINH_DOT);
      lamMoi();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không lưu được.'),
  });

  const xoa = useMutation({
    mutationFn: (id: string) => apiDotDeNghi.xoa(id),
    onSuccess: () => {
      message.success('Đã xoá đợt');
      lamMoi();
    },
    onError: (loi) =>
      modal.error({
        title: 'Không xoá được',
        content: loi instanceof LoiApi ? loi.message : 'Đợt đang được tham chiếu.',
      }),
  });

  const saoChep = useMutation({
    mutationFn: (giaTri: GiaTriSaoChepDot) => apiDotDeNghi.saoChep(dangSaoChep!.id, giaTri),
    onSuccess: () => {
      message.success('Đã sao chép đợt — đợt mới ở trạng thái Nháp');
      setDangSaoChep(null);
      form.reset();
      lamMoi();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không sao chép được.'),
  });

  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  async function moSua(dong: DotDeNghiQuanLy) {
    const chiTiet = (await apiDotDeNghi.theoId(dong.id)) as unknown as Record<string, unknown>;

    setDangSua(dong);
    formDot.reset({
      ...MAC_DINH_DOT,
      ...(chiTiet as unknown as GiaTriDot),
      hanNopHoSo: chiTiet.hanNopHoSo ? dayjs(chiTiet.hanNopHoSo as string) : undefined,
      hanChamDiem: chiTiet.hanChamDiem ? dayjs(chiTiet.hanChamDiem as string) : undefined,
    });
    setMoForm(true);
  }

  function xacNhanDoiTrangThai(dong: DotDeNghiQuanLy, hanhDong: 'mo' | 'dong' | 'khoa') {
    const noiDung =
      hanhDong === 'mo'
        ? 'Đợt sẽ bắt đầu nhận hồ sơ. Đợt phải được gán quy trình và bộ tiêu chí trước khi mở.'
        : hanhDong === 'dong'
          ? 'Ngừng nhận hồ sơ mới. Hồ sơ đã nộp vẫn được xử lý tiếp bình thường.'
          : 'Khoá đợt là thao tác cuối cùng: toàn bộ hồ sơ và điểm của đợt chuyển sang chỉ đọc.';

    modal.confirm({
      title: `${hanhDong === 'mo' ? 'Mở' : hanhDong === 'dong' ? 'Đóng' : 'Khoá'} đợt "${dong.ten}"?`,
      content: noiDung,
      okText: 'Xác nhận',
      okButtonProps: { danger: hanhDong === 'khoa' },
      cancelText: 'Huỷ',
      onOk: () => doiTrangThai.mutateAsync({ id: dong.id, hanhDong }),
    });
  }

  return (
    <Card
      title="Danh mục dùng chung"
      extra={
        <Space>
          <Input.Search
            placeholder="Tìm đợt đề nghị (không dấu)"
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
              formDot.reset(MAC_DINH_DOT);
              setMoForm(true);
            }}
          >
            Thêm đợt
          </Button>
        </Space>
      }
    >
      <DaiTabTrang danhSach={DS_TAB_DANH_MUC} dangChon="dot-de-nghi" />

      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 12 }}
        message="Vòng đời đợt: Nháp → Đang mở → Đã đóng → Đã khoá"
        description="Thêm, sửa nội dung đợt làm ở tab này; các nút bên phải đổi trạng thái vòng đời. Đợt bật tự động khoá sẽ tự đóng khi quá hạn nộp hồ sơ."
      />

      <Table<DotDeNghiQuanLy>
        rowKey="id"
        size="middle"
        loading={isLoading}
        dataSource={data?.duLieu ?? []}
        scroll={{ x: 1420 }}
        columns={[
          { title: 'Mã', dataIndex: 'ma', width: 170 },
          {
            title: 'Tên đợt',
            dataIndex: 'ten',
            width: 300,
            ellipsis: true,
            render: (v: string, dong) => (
              <Link to={`/quan-tri/danh-muc/dot-de-nghi/${dong.id}`}>{v}</Link>
            ),
          },
          { title: 'Năm', dataIndex: 'nam', width: 80, align: 'right' },
          {
            title: 'Cấp',
            dataIndex: 'capXetDuyet',
            width: 130,
            responsive: ['lg'],
            render: (v: string) => CAP_XET_DUYET[v] ?? v,
          },
          {
            title: 'Hạn nộp',
            dataIndex: 'hanNopHoSo',
            width: 150,
            responsive: ['xl'],
            render: (v: string | null, dong) => (
              <Space direction="vertical" size={0}>
                <span>{ngayGio(v)}</span>
                {dong.tuDongKhoa && (
                  <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                    Tự đóng khi quá hạn
                  </Typography.Text>
                )}
              </Space>
            ),
          },
          {
            title: 'Cấu hình',
            key: 'cauHinh',
            width: 150,
            responsive: ['xl'],
            render: (_v, dong) => (
              <Space direction="vertical" size={2}>
                <Tag color={dong.quyTrinhId ? 'success' : 'error'}>
                  {dong.quyTrinhId ? 'Có quy trình' : 'Thiếu quy trình'}
                </Tag>
                <Tag color={dong.boTieuChiId ? 'success' : 'error'}>
                  {dong.boTieuChiId ? 'Có bộ tiêu chí' : 'Thiếu bộ tiêu chí'}
                </Tag>
              </Space>
            ),
          },
          {
            title: 'Trạng thái',
            dataIndex: 'trangThaiDot',
            width: 130,
            render: (v: string) => {
              const muc = TRANG_THAI_DOT[v] ?? { mau: 'default', ten: v };
              return <Tag color={muc.mau}>{muc.ten}</Tag>;
            },
          },
          {
            title: '',
            key: 'thaoTac',
            width: 270,
            fixed: 'right',
            render: (_v, dong) => (
              <Space size={4}>
                <Tooltip
                  title={
                    dong.quyTrinhId && dong.boTieuChiId
                      ? 'Mở đợt để bắt đầu nhận hồ sơ'
                      : 'Phải gán quy trình và bộ tiêu chí trước khi mở đợt'
                  }
                >
                  <Button
                    size="small"
                    type="primary"
                    ghost
                    icon={<UnlockOutlined />}
                    disabled={dong.trangThaiDot === 'DANG_MO' || dong.trangThaiDot === 'DA_KHOA'}
                    onClick={() => xacNhanDoiTrangThai(dong, 'mo')}
                  />
                </Tooltip>
                <Tooltip title="Đóng đợt — ngừng nhận hồ sơ mới">
                  <Button
                    size="small"
                    icon={<StopOutlined />}
                    disabled={dong.trangThaiDot !== 'DANG_MO'}
                    onClick={() => xacNhanDoiTrangThai(dong, 'dong')}
                  />
                </Tooltip>
                <Tooltip title="Khoá đợt — toàn bộ dữ liệu chỉ đọc">
                  <Button
                    size="small"
                    danger
                    icon={<LockOutlined />}
                    disabled={dong.trangThaiDot === 'DA_KHOA' || dong.trangThaiDot === 'NHAP'}
                    onClick={() => xacNhanDoiTrangThai(dong, 'khoa')}
                  />
                </Tooltip>
                <Tooltip title={dong.trangThaiDot === 'DA_KHOA' ? 'Đợt đã khoá — chỉ đọc' : 'Sửa'}>
                  <Button
                    size="small"
                    icon={<EditOutlined />}
                    disabled={dong.trangThaiDot === 'DA_KHOA'}
                    onClick={() => void moSua(dong)}
                  />
                </Tooltip>
                <Tooltip title="Xoá đợt">
                  <Button
                    size="small"
                    danger
                    icon={<DeleteOutlined />}
                    disabled={dong.trangThaiDot !== 'NHAP'}
                    onClick={() =>
                      modal.confirm({
                        title: 'Xoá đợt đề nghị',
                        content: `Xoá đợt "${dong.ten}"? Chỉ xoá được đợt ở trạng thái Nháp và chưa có hồ sơ.`,
                        okText: 'Xoá',
                        okButtonProps: { danger: true },
                        cancelText: 'Huỷ',
                        onOk: () => xoa.mutateAsync(dong.id),
                      })
                    }
                  />
                </Tooltip>
                <Tooltip title="Sao chép sang đợt năm mới">
                  <Button
                    size="small"
                    icon={<CopyOutlined />}
                    onClick={() => {
                      setDangSaoChep(dong);
                      form.reset({
                        ma: `${dong.ma}-${dong.nam + 1}`,
                        ten: `${dong.ten} (năm ${dong.nam + 1})`,
                        nam: dong.nam + 1,
                      });
                    }}
                  />
                </Tooltip>
              </Space>
            ),
          },
        ]}
        pagination={{
          current: trang,
          pageSize: soDong,
          total: data?.tongSo ?? 0,
          showSizeChanger: true,
          showTotal: (t) => `Tổng ${t} đợt`,
          onChange: (t, s) => {
            setTrang(t);
            setSoDong(s);
          },
        }}
      />

      <Modal
        open={moForm}
        width={720}
        title={dangSua ? `Sửa đợt: ${dangSua.ten}` : 'Thêm đợt đề nghị'}
        okText="Lưu"
        cancelText="Huỷ"
        confirmLoading={luu.isPending || formDot.formState.isSubmitting}
        onCancel={() => setMoForm(false)}
        okButtonProps={{ htmlType: 'submit', form: 'form-dot' }}
      >
        <BieuMau id="form-dot" form={formDot} onGui={(giaTri) => luu.mutateAsync(giaTri)}>
          <Row gutter={12}>
            <Col xs={24} md={10}>
              <Truong<GiaTriDot> ten="ma" label="Mã đợt" required>
                {(o) => (
                  <Input
                    {...o}
                    value={o.value as string}
                    placeholder="VD: DOT-2027"
                    disabled={!!dangSua}
                  />
                )}
              </Truong>
            </Col>
            <Col xs={24} md={14}>
              <Truong<GiaTriDot> ten="ten" label="Tên đợt" required>
                {(o) => (
                  <Input
                    {...o}
                    value={o.value as string}
                    placeholder="VD: Đợt xét công nhận sáng kiến năm 2027"
                  />
                )}
              </Truong>
            </Col>
          </Row>

          <Row gutter={12}>
            <Col xs={12} md={6}>
              <Truong<GiaTriDot> ten="nam" label="Năm" required>
                {(o) => (
                  <InputNumber
                    {...o}
                    value={o.value as number}
                    min={2000}
                    max={2100}
                    style={{ width: '100%' }}
                  />
                )}
              </Truong>
            </Col>
            <Col xs={12} md={6}>
              <Truong<GiaTriDot> ten="capXetDuyet" label="Cấp xét duyệt">
                {(o) => (
                  <Select
                    {...o}
                    value={o.value as string}
                    options={[
                      { value: 'CO_SO', label: 'Cấp cơ sở' },
                      { value: 'THANH_PHO', label: 'Cấp thành phố' },
                      { value: 'TINH', label: 'Cấp tỉnh' },
                    ]}
                  />
                )}
              </Truong>
            </Col>
            <Col xs={12} md={6}>
              <Truong<GiaTriDot> ten="hanNopHoSo" label="Hạn nộp hồ sơ">
                {(o) => (
                  <DatePicker
                    value={(o.value as Dayjs | null | undefined) ?? null}
                    onChange={o.onChange}
                    onBlur={o.onBlur}
                    status={o.status}
                    showTime
                    format="DD/MM/YYYY HH:mm"
                    style={{ width: '100%' }}
                  />
                )}
              </Truong>
            </Col>
            <Col xs={12} md={6}>
              <Truong<GiaTriDot> ten="hanChamDiem" label="Hạn chấm điểm">
                {(o) => (
                  <DatePicker
                    value={(o.value as Dayjs | null | undefined) ?? null}
                    onChange={o.onChange}
                    onBlur={o.onBlur}
                    status={o.status}
                    showTime
                    format="DD/MM/YYYY HH:mm"
                    style={{ width: '100%' }}
                  />
                )}
              </Truong>
            </Col>
          </Row>

          <Row gutter={12}>
            <Col xs={24} md={12}>
              <Truong<GiaTriDot>
                ten="quyTrinhId"
                label="Quy trình áp dụng"
                tooltip="Bắt buộc có trước khi mở đợt."
              >
                {(o) => (
                  <Select
                    {...o}
                    value={o.value as string | undefined}
                    allowClear
                    showSearch
                    optionFilterProp="label"
                    options={(cacQuyTrinh ?? []).map((x) => ({ value: x.id, label: x.ten }))}
                  />
                )}
              </Truong>
            </Col>
            <Col xs={24} md={12}>
              <Truong<GiaTriDot>
                ten="boTieuChiId"
                label="Bộ tiêu chí chấm điểm"
                tooltip="Bắt buộc có trước khi mở đợt."
              >
                {(o) => (
                  <Select
                    {...o}
                    value={o.value as string | undefined}
                    allowClear
                    showSearch
                    optionFilterProp="label"
                    options={(cacBoTieuChi ?? []).map((x) => ({ value: x.id, label: x.ten }))}
                  />
                )}
              </Truong>
            </Col>
          </Row>

          <Truong<GiaTriDot>
            ten="donViApDungIds"
            label="Đơn vị áp dụng"
            tooltip="Để trống = áp dụng cho mọi đơn vị."
          >
            {(o) => (
              <Select
                {...o}
                value={o.value as string[]}
                mode="multiple"
                allowClear
                showSearch
                optionFilterProp="label"
                placeholder="Để trống = tất cả đơn vị"
                options={(cacDonVi ?? []).map((x) => ({ value: x.id, label: x.ten }))}
              />
            )}
          </Truong>

          <Truong<GiaTriDot> ten="moTa" label="Mô tả">
            {(o) => <Input.TextArea {...o} value={o.value as string} rows={2} />}
          </Truong>

          <Space size="large" wrap>
            <Truong<GiaTriDot> ten="tuDongKhoa" label="Tự động đóng khi quá hạn nộp">
              {(o) => <Switch checked={!!o.value} onChange={o.onChange} />}
            </Truong>
            <Truong<GiaTriDot> ten="thuTu" label="Thứ tự">
              {(o) => <InputNumber {...o} value={o.value as number} min={0} />}
            </Truong>
            <Truong<GiaTriDot> ten="trangThai" label="Trạng thái bản ghi">
              {(o) => (
                <Select
                  {...o}
                  value={o.value as number}
                  style={{ width: 180 }}
                  options={[
                    { value: 1, label: 'Hoạt động' },
                    { value: 0, label: 'Ngừng hoạt động' },
                  ]}
                />
              )}
            </Truong>
          </Space>
        </BieuMau>
      </Modal>

      <Modal
        open={!!dangSaoChep}
        title={`Sao chép đợt "${dangSaoChep?.ten ?? ''}"`}
        okText="Sao chép"
        cancelText="Huỷ"
        confirmLoading={saoChep.isPending || form.formState.isSubmitting}
        onCancel={() => setDangSaoChep(null)}
        okButtonProps={{ htmlType: 'submit', form: 'form-sao-chep-dot' }}
      >
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 12 }}
          message="Đợt mới giữ nguyên quy trình, bộ tiêu chí và đơn vị áp dụng của đợt gốc, trạng thái là Nháp."
        />

        <BieuMau id="form-sao-chep-dot" form={form} onGui={(giaTri) => saoChep.mutateAsync(giaTri)}>
          <Truong<GiaTriSaoChepDot> ten="ma" label="Mã đợt mới" required>
            {(o) => <Input {...o} value={o.value as string} />}
          </Truong>
          <Truong<GiaTriSaoChepDot> ten="ten" label="Tên đợt mới" required>
            {(o) => <Input {...o} value={o.value as string} />}
          </Truong>
          <Truong<GiaTriSaoChepDot> ten="nam" label="Năm" required>
            {(o) => (
              <InputNumber
                {...o}
                value={o.value as number}
                min={2000}
                max={2100}
                style={{ width: '100%' }}
              />
            )}
          </Truong>
        </BieuMau>
      </Modal>
    </Card>
  );
}
