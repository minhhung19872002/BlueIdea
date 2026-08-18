import { useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  App,
  Alert,
  Button,
  Card,
  Checkbox,
  Col,
  DatePicker,
  Descriptions,
  Divider,
  Dropdown,
  Form,
  Input,
  Modal,
  Row,
  Select,
  Space,
  Statistic,
  Table,
  Tabs,
  Tag,
  Typography,
} from 'antd';
import {
  DeleteOutlined,
  FilePdfOutlined,
  PlusOutlined,
  SaveOutlined,
  TeamOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';

import { layPhanTrang, LoiApi, taiTep } from '@/api/client';
import { theoDoiPhienHop } from '@/api/realtime';
import {
  apiDanhGia,
  apiHoiDong,
  apiNhapXuat,
  apiSangKien,
  type DongMaTranDiem,
  type PhienHop,
  type ThanhVienHoiDong,
} from '@/api/endpoints';
import { useAuthStore } from '@/app/store/authStore';
import { TabBienBan } from '@/features/hoi-dong/TabBienBan';
import { CAP_HOI_DONG } from '@/features/hoi-dong/TrangHoiDong';
import { KhoiDangTai, KhoiLoi, KhoiRong, ngayGio } from '@/components/ThanhPhanChung';

const CHUC_DANH = [
  { value: 'CHU_TICH', label: 'Chủ tịch' },
  { value: 'PHO_CHU_TICH', label: 'Phó chủ tịch' },
  { value: 'UY_VIEN_THU_KY', label: 'Uỷ viên thư ký' },
  { value: 'UY_VIEN', label: 'Uỷ viên' },
  { value: 'THU_KY', label: 'Thư ký' },
];

const HINH_THUC_HOP = [
  { value: 'TRUC_TIEP', label: 'Trực tiếp' },
  { value: 'TRUC_TUYEN', label: 'Trực tuyến' },
  { value: 'KET_HOP', label: 'Kết hợp' },
];

const TRANG_THAI_PHIEN: Record<string, { mau: string; ten: string }> = {
  DU_KIEN: { mau: 'default', ten: 'Dự kiến' },
  DANG_DIEN_RA: { mau: 'processing', ten: 'Đang diễn ra' },
  DA_KET_THUC: { mau: 'success', ten: 'Đã kết thúc' },
  DA_HUY: { mau: 'error', ten: 'Đã huỷ' },
};

interface NguoiDungChon {
  id: string;
  hoTen: string;
  tenDangNhap: string;
  chucVu?: string | null;
}

/** Dòng thành viên đang sửa trên giao diện (id rỗng = thành viên mới thêm). */
type DongThanhVien = ThanhVienHoiDong & { khoa: string };

/** Chức năng 19–20 — Chi tiết hội đồng: thành viên, phiên họp, điểm danh, bỏ phiếu. */
export default function TrangChiTietHoiDong() {
  const { id = '' } = useParams<{ id: string }>();
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const duocCauHinh = useAuthStore((s) => s.coQuyen('HOI_DONG.CAU_HINH'));
  const duocHopPhien = useAuthStore((s) => s.coQuyen('HOI_DONG.HOP_PHIEN'));

  const [thanhVien, setThanhVien] = useState<DongThanhVien[]>([]);
  const [moTaoPhien, setMoTaoPhien] = useState(false);
  const [phienDangMo, setPhienDangMo] = useState<string | null>(null);
  const [formPhien] = Form.useForm();

  const truyVan = useQuery({
    queryKey: ['hoi-dong', id],
    queryFn: () => apiHoiDong.theoId(id),
  });

  useEffect(() => {
    if (!truyVan.data) return;

    setThanhVien(
      [...truyVan.data.thanhVien]
        .sort((a, b) => a.thuTu - b.thuTu)
        .map((tv) => ({ ...tv, khoa: tv.id })),
    );
  }, [truyVan.data]);

  const luuThanhVien = useMutation({
    mutationFn: () =>
      apiHoiDong.luuThanhVien(
        id,
        thanhVien.map((tv) => ({
          id: tv.id || '00000000-0000-0000-0000-000000000000',
          nguoiDungId: tv.nguoiDungId ?? null,
          hoTenHienThi: tv.hoTenHienThi,
          chucVuCongTac: tv.chucVuCongTac ?? null,
          donViCongTac: tv.donViCongTac ?? null,
          chucDanh: tv.chucDanh,
          quyenChamDiem: tv.quyenChamDiem,
          quyenNhanXet: tv.quyenNhanXet,
          quyenBoPhieu: tv.quyenBoPhieu,
          quyenKyBienBan: tv.quyenKyBienBan,
          quyenKetLuan: tv.quyenKetLuan,
        })),
      ),
    onSuccess: () => {
      message.success('Đã lưu danh sách thành viên');
      void queryClient.invalidateQueries({ queryKey: ['hoi-dong', id] });
    },
    onError: (loi) =>
      modal.error({
        title: 'Không lưu được thành viên',
        content: loi instanceof LoiApi ? loi.message : 'Đã xảy ra lỗi.',
      }),
  });

  const taoPhien = useMutation({
    mutationFn: async (giaTri: Record<string, unknown>) =>
      apiHoiDong.taoPhienHop({
        hoiDongId: id,
        maPhien: (giaTri.maPhien as string) || null,
        tenPhien: giaTri.tenPhien,
        thoiGianBatDau: dayjs(giaTri.thoiGianBatDau as string).toISOString(),
        thoiGianKetThuc: giaTri.thoiGianKetThuc
          ? dayjs(giaTri.thoiGianKetThuc as string).toISOString()
          : null,
        diaDiem: giaTri.diaDiem ?? null,
        hinhThuc: giaTri.hinhThuc,
        chuTriId: giaTri.chuTriId ?? null,
        thuKyId: giaTri.thuKyId ?? null,
        noiDung: giaTri.noiDung ?? null,
        sangKienIds: (giaTri.sangKienIds as string[]) ?? [],
      }),
    onSuccess: (phien) => {
      message.success('Đã tạo phiên họp');
      setMoTaoPhien(false);
      formPhien.resetFields();
      void queryClient.invalidateQueries({ queryKey: ['hoi-dong', id] });
      setPhienDangMo(phien.id);
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không tạo được phiên.'),
  });

  const soChuTich = useMemo(
    () => thanhVien.filter((x) => x.chucDanh === 'CHU_TICH').length,
    [thanhVien],
  );

  if (truyVan.isLoading) return <KhoiDangTai />;
  if (truyVan.error) return <KhoiLoi loi={truyVan.error} thuLai={truyVan.refetch} />;

  const hoiDong = truyVan.data!;
  const duThanhVien = thanhVien.length >= hoiDong.soThanhVienToiThieu;

  function themDong() {
    setThanhVien((cu) => [
      ...cu,
      {
        khoa: `moi-${cu.length}-${cu.reduce((s, x) => s + x.thuTu, 0)}`,
        id: '',
        nguoiDungId: null,
        hoTenHienThi: '',
        chucVuCongTac: null,
        donViCongTac: null,
        chucDanh: cu.some((x) => x.chucDanh === 'CHU_TICH') ? 'UY_VIEN' : 'CHU_TICH',
        quyenChamDiem: true,
        quyenNhanXet: true,
        quyenBoPhieu: true,
        quyenKyBienBan: false,
        quyenKetLuan: false,
        thuTu: cu.length + 1,
        trangThai: 1,
      },
    ]);
  }

  function suaDong(khoa: string, thayDoi: Partial<DongThanhVien>) {
    setThanhVien((cu) => cu.map((x) => (x.khoa === khoa ? { ...x, ...thayDoi } : x)));
  }

  return (
    <Card
      title={
        <Space wrap>
          <TeamOutlined />
          <span>{hoiDong.ten}</span>
          <Tag>{hoiDong.ma}</Tag>
          {hoiDong.trangThaiHoatDong === 'DA_KET_THUC' ? (
            <Tag>Đã kết thúc</Tag>
          ) : (
            <Tag color="success">Đang hoạt động</Tag>
          )}
        </Space>
      }
      extra={
        <Space>
          <Link to="/hoi-dong">
            <Button>Danh sách hội đồng</Button>
          </Link>
          <Dropdown
            menu={{
              items: [
                { key: 'PDF', label: 'Một tệp PDF (mỗi phiếu một trang)' },
                { key: 'ZIP', label: 'ZIP — mỗi phiếu một tệp riêng' },
              ],
              onClick: ({ key }) =>
                taiTep(
                  apiNhapXuat.duongDanPhieuChamHoiDong(id, key as 'PDF' | 'ZIP'),
                  `phieu-cham-${hoiDong.ma}.${key === 'ZIP' ? 'zip' : 'pdf'}`,
                  hoiDong.dotDeNghiId ? { dotDeNghiId: hoiDong.dotDeNghiId } : undefined,
                ).catch((loi: unknown) =>
                  message.error(
                    loi instanceof LoiApi ? loi.message : 'Không xuất được phiếu chấm.',
                  ),
                ),
            }}
          >
            <Button icon={<FilePdfOutlined />}>Xuất phiếu chấm</Button>
          </Dropdown>
        </Space>
      }
    >
      <Tabs
        items={[
          {
            key: 'thong-tin',
            label: 'Thông tin chung',
            children: (
              <Descriptions bordered size="small" column={{ xs: 1, md: 2 }}>
                <Descriptions.Item label="Cấp xét duyệt">
                  {CAP_HOI_DONG.find((x) => x.value === hoiDong.cap)?.label ?? hoiDong.cap}
                </Descriptions.Item>
                <Descriptions.Item label="Số quyết định thành lập">
                  {hoiDong.soQuyetDinhThanhLap ?? '—'}
                </Descriptions.Item>
                <Descriptions.Item label="Ngày quyết định">
                  {ngayGio(hoiDong.ngayQuyetDinh, false)}
                </Descriptions.Item>
                <Descriptions.Item label="Thời gian hoạt động">
                  {ngayGio(hoiDong.thoiGianHoatDongTu, false)} —{' '}
                  {ngayGio(hoiDong.thoiGianHoatDongDen, false)}
                </Descriptions.Item>
                <Descriptions.Item label="Số thành viên tối thiểu">
                  {hoiDong.soThanhVienToiThieu}
                </Descriptions.Item>
                <Descriptions.Item label="Tỷ lệ thông qua">{hoiDong.tyLeThongQua}%</Descriptions.Item>
                <Descriptions.Item label="Mô tả" span={2}>
                  {hoiDong.moTa ?? '—'}
                </Descriptions.Item>
              </Descriptions>
            ),
          },
          {
            key: 'thanh-vien',
            label: `Thành viên (${thanhVien.length})`,
            children: (
              <BangThanhVien
                thanhVien={thanhVien}
                duocSua={duocCauHinh}
                soChuTich={soChuTich}
                duThanhVien={duThanhVien}
                soToiThieu={hoiDong.soThanhVienToiThieu}
                dangLuu={luuThanhVien.isPending}
                onThem={themDong}
                onSua={suaDong}
                onXoa={(khoa) => setThanhVien((cu) => cu.filter((x) => x.khoa !== khoa))}
                onLuu={() => luuThanhVien.mutate()}
              />
            ),
          },
          {
            key: 'phien-hop',
            label: `Phiên họp (${hoiDong.phienHop.length})`,
            children: (
              <>
                <Space style={{ marginBottom: 12 }}>
                  {duocHopPhien && (
                    <Button
                      type="primary"
                      icon={<PlusOutlined />}
                      disabled={thanhVien.length === 0}
                      onClick={() => setMoTaoPhien(true)}
                    >
                      Tạo phiên họp
                    </Button>
                  )}
                  {thanhVien.length === 0 && (
                    <Typography.Text type="secondary">
                      Cần lưu danh sách thành viên trước khi mở phiên họp.
                    </Typography.Text>
                  )}
                </Space>

                {hoiDong.phienHop.length === 0 ? (
                  <KhoiRong moTa="Hội đồng chưa có phiên họp nào." />
                ) : (
                  <Table<PhienHop>
                    rowKey="id"
                    size="middle"
                    dataSource={[...hoiDong.phienHop].sort((a, b) =>
                      a.thoiGianBatDau < b.thoiGianBatDau ? 1 : -1,
                    )}
                    scroll={{ x: 800 }}
                    columns={[
                      { title: 'Mã phiên', dataIndex: 'maPhien', width: 170 },
                      { title: 'Tên phiên họp', dataIndex: 'tenPhien' },
                      {
                        title: 'Thời gian',
                        dataIndex: 'thoiGianBatDau',
                        width: 160,
                        render: (v: string) => ngayGio(v),
                      },
                      {
                        title: 'Hình thức',
                        dataIndex: 'hinhThuc',
                        width: 120,
                        responsive: ['lg'],
                        render: (v: string) =>
                          HINH_THUC_HOP.find((x) => x.value === v)?.label ?? v,
                      },
                      {
                        title: 'Trạng thái',
                        dataIndex: 'trangThaiPhien',
                        width: 140,
                        render: (v: string) => {
                          const muc = TRANG_THAI_PHIEN[v] ?? { mau: 'default', ten: v };
                          return <Tag color={muc.mau}>{muc.ten}</Tag>;
                        },
                      },
                      {
                        title: '',
                        width: 110,
                        fixed: 'right',
                        render: (_v, dong) => (
                          <Button size="small" onClick={() => setPhienDangMo(dong.id)}>
                            Điều hành
                          </Button>
                        ),
                      },
                    ]}
                    pagination={false}
                  />
                )}
              </>
            ),
          },
          {
            key: 'ma-tran-diem',
            label: 'Ma trận điểm',
            children: <BangMaTranDiem hoiDongId={id} dotDeNghiId={hoiDong.dotDeNghiId} />,
          },
        ]}
      />

      <ModalTaoPhien
        mo={moTaoPhien}
        form={formPhien}
        thanhVien={thanhVien}
        dotDeNghiId={hoiDong.dotDeNghiId}
        dangTao={taoPhien.isPending}
        onHuy={() => setMoTaoPhien(false)}
        onTao={async () => taoPhien.mutate(await formPhien.validateFields())}
      />

      {phienDangMo && (
        <ModalDieuHanhPhien
          phienHopId={phienDangMo}
          thanhVien={thanhVien}
          tyLeThongQua={hoiDong.tyLeThongQua}
          hoiDongId={id}
          onDong={() => setPhienDangMo(null)}
        />
      )}
    </Card>
  );
}

// ---------------------------------------------------------------------------

function BangThanhVien({
  thanhVien,
  duocSua,
  soChuTich,
  duThanhVien,
  soToiThieu,
  dangLuu,
  onThem,
  onSua,
  onXoa,
  onLuu,
}: {
  thanhVien: DongThanhVien[];
  duocSua: boolean;
  soChuTich: number;
  duThanhVien: boolean;
  soToiThieu: number;
  dangLuu: boolean;
  onThem: () => void;
  onSua: (khoa: string, thayDoi: Partial<DongThanhVien>) => void;
  onXoa: (khoa: string) => void;
  onLuu: () => void;
}) {
  const [tuKhoa, setTuKhoa] = useState('');

  const { data: nguoiDung } = useQuery({
    queryKey: ['nguoi-dung-chon', tuKhoa],
    queryFn: () =>
      layPhanTrang<NguoiDungChon>('/api/v1/he-thong/nguoi-dung', {
        trang: 1,
        soDong: 20,
        tuKhoa,
      }),
  });

  const hopLe = soChuTich === 1 && duThanhVien;

  return (
    <>
      {!hopLe && (
        <Alert
          type="warning"
          showIcon
          style={{ marginBottom: 12 }}
          message="Danh sách chưa hợp lệ"
          description={
            <ul style={{ paddingLeft: 18, marginBottom: 0 }}>
              {soChuTich !== 1 && (
                <li>Hội đồng phải có đúng 1 Chủ tịch, hiện đang có {soChuTich}.</li>
              )}
              {!duThanhVien && <li>Cần tối thiểu {soToiThieu} thành viên.</li>}
            </ul>
          }
        />
      )}

      {duocSua && (
        <Space style={{ marginBottom: 12 }}>
          <Button icon={<PlusOutlined />} onClick={onThem}>
            Thêm thành viên
          </Button>
          <Button
            type="primary"
            icon={<SaveOutlined />}
            loading={dangLuu}
            disabled={!hopLe}
            onClick={onLuu}
          >
            Lưu danh sách
          </Button>
        </Space>
      )}

      <Table<DongThanhVien>
        rowKey="khoa"
        size="small"
        dataSource={thanhVien}
        pagination={false}
        scroll={{ x: 1100 }}
        columns={[
          {
            title: 'Tài khoản',
            dataIndex: 'nguoiDungId',
            width: 220,
            render: (v: string | null, dong) =>
              duocSua ? (
                <Select
                  style={{ width: '100%' }}
                  allowClear
                  showSearch
                  filterOption={false}
                  placeholder="Chọn tài khoản (tuỳ chọn)"
                  value={v ?? undefined}
                  onSearch={setTuKhoa}
                  options={(nguoiDung?.duLieu ?? []).map((x) => ({
                    value: x.id,
                    label: `${x.hoTen} (${x.tenDangNhap})`,
                  }))}
                  onChange={(giaTri) => {
                    const chon = (nguoiDung?.duLieu ?? []).find((x) => x.id === giaTri);
                    onSua(dong.khoa, {
                      nguoiDungId: giaTri ?? null,
                      hoTenHienThi: chon?.hoTen ?? dong.hoTenHienThi,
                      chucVuCongTac: chon?.chucVu ?? dong.chucVuCongTac,
                    });
                  }}
                />
              ) : (
                (v ?? '—')
              ),
          },
          {
            title: 'Họ tên hiển thị',
            dataIndex: 'hoTenHienThi',
            width: 200,
            render: (v: string, dong) =>
              duocSua ? (
                <Input
                  value={v}
                  status={v.trim() ? undefined : 'error'}
                  onChange={(e) => onSua(dong.khoa, { hoTenHienThi: e.target.value })}
                />
              ) : (
                v
              ),
          },
          {
            title: 'Chức vụ',
            dataIndex: 'chucVuCongTac',
            width: 160,
            responsive: ['lg'],
            render: (v: string | null, dong) =>
              duocSua ? (
                <Input
                  value={v ?? ''}
                  onChange={(e) => onSua(dong.khoa, { chucVuCongTac: e.target.value })}
                />
              ) : (
                (v ?? '—')
              ),
          },
          {
            title: 'Đơn vị công tác',
            dataIndex: 'donViCongTac',
            width: 180,
            responsive: ['xl'],
            render: (v: string | null, dong) =>
              duocSua ? (
                <Input
                  value={v ?? ''}
                  onChange={(e) => onSua(dong.khoa, { donViCongTac: e.target.value })}
                />
              ) : (
                (v ?? '—')
              ),
          },
          {
            title: 'Chức danh',
            dataIndex: 'chucDanh',
            width: 160,
            render: (v: string, dong) =>
              duocSua ? (
                <Select
                  style={{ width: '100%' }}
                  value={v}
                  options={CHUC_DANH}
                  onChange={(giaTri) => onSua(dong.khoa, { chucDanh: giaTri })}
                />
              ) : (
                (CHUC_DANH.find((x) => x.value === v)?.label ?? v)
              ),
          },
          {
            title: 'Quyền hạn',
            width: 320,
            render: (_v, dong) => (
              <Space wrap size={[12, 4]}>
                {(
                  [
                    ['quyenChamDiem', 'Chấm điểm'],
                    ['quyenNhanXet', 'Nhận xét'],
                    ['quyenBoPhieu', 'Bỏ phiếu'],
                    ['quyenKyBienBan', 'Ký biên bản'],
                    ['quyenKetLuan', 'Kết luận'],
                  ] as const
                ).map(([khoa, nhan]) => (
                  <Checkbox
                    key={khoa}
                    disabled={!duocSua}
                    checked={dong[khoa]}
                    onChange={(e) => onSua(dong.khoa, { [khoa]: e.target.checked })}
                  >
                    {nhan}
                  </Checkbox>
                ))}
              </Space>
            ),
          },
          ...(duocSua
            ? [
                {
                  title: '',
                  width: 60,
                  fixed: 'right' as const,
                  render: (_v: unknown, dong: DongThanhVien) => (
                    <Button
                      type="text"
                      danger
                      icon={<DeleteOutlined />}
                      onClick={() => onXoa(dong.khoa)}
                    />
                  ),
                },
              ]
            : []),
        ]}
      />
    </>
  );
}

// ---------------------------------------------------------------------------

function ModalTaoPhien({
  mo,
  form,
  thanhVien,
  dotDeNghiId,
  dangTao,
  onHuy,
  onTao,
}: {
  mo: boolean;
  form: ReturnType<typeof Form.useForm>[0];
  thanhVien: DongThanhVien[];
  dotDeNghiId?: string | null;
  dangTao: boolean;
  onHuy: () => void;
  onTao: () => void;
}) {
  const { data: hoSo } = useQuery({
    queryKey: ['sang-kien-cho-hop', dotDeNghiId],
    queryFn: () =>
      apiSangKien.danhSach({ trang: 1, soDong: 200, dotDeNghiId: dotDeNghiId ?? undefined }),
    enabled: mo,
  });

  return (
    <Modal
      open={mo}
      width={720}
      title="Tạo phiên họp hội đồng"
      okText="Tạo phiên"
      cancelText="Huỷ"
      confirmLoading={dangTao}
      onCancel={onHuy}
      onOk={onTao}
    >
      <Form
        form={form}
        layout="vertical"
        initialValues={{ hinhThuc: 'TRUC_TIEP', thoiGianBatDau: dayjs() }}
      >
        <Form.Item
          name="tenPhien"
          label="Tên phiên họp"
          rules={[{ required: true, message: 'Nhập tên phiên họp' }]}
        >
          <Input placeholder="VD: Họp xét công nhận sáng kiến đợt 1 năm 2027" />
        </Form.Item>

        <Row gutter={12}>
          <Col xs={24} md={12}>
            <Form.Item name="maPhien" label="Mã phiên" tooltip="Bỏ trống để hệ thống tự sinh.">
              <Input placeholder="Tự sinh nếu bỏ trống" />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item name="hinhThuc" label="Hình thức">
              <Select options={HINH_THUC_HOP} />
            </Form.Item>
          </Col>
        </Row>

        <Row gutter={12}>
          <Col xs={24} md={12}>
            <Form.Item
              name="thoiGianBatDau"
              label="Bắt đầu"
              rules={[{ required: true, message: 'Chọn thời gian bắt đầu' }]}
            >
              <DatePicker showTime format="DD/MM/YYYY HH:mm" style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item name="thoiGianKetThuc" label="Kết thúc (dự kiến)">
              <DatePicker showTime format="DD/MM/YYYY HH:mm" style={{ width: '100%' }} />
            </Form.Item>
          </Col>
        </Row>

        <Form.Item name="diaDiem" label="Địa điểm">
          <Input placeholder="VD: Phòng họp A, trụ sở UBND" />
        </Form.Item>

        <Row gutter={12}>
          <Col xs={24} md={12}>
            <Form.Item name="chuTriId" label="Chủ trì">
              <Select
                allowClear
                options={thanhVien
                  .filter((x) => x.nguoiDungId)
                  .map((x) => ({ value: x.nguoiDungId!, label: x.hoTenHienThi }))}
              />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item name="thuKyId" label="Thư ký">
              <Select
                allowClear
                options={thanhVien
                  .filter((x) => x.nguoiDungId)
                  .map((x) => ({ value: x.nguoiDungId!, label: x.hoTenHienThi }))}
              />
            </Form.Item>
          </Col>
        </Row>

        <Form.Item name="sangKienIds" label="Hồ sơ đưa ra họp">
          <Select
            mode="multiple"
            allowClear
            showSearch
            optionFilterProp="label"
            placeholder="Chọn hồ sơ sẽ xét trong phiên"
            options={(hoSo?.duLieu ?? []).map((x) => ({
              value: x.id,
              label: `${x.maHoSo} — ${x.tenSangKien}`,
            }))}
          />
        </Form.Item>

        <Form.Item name="noiDung" label="Nội dung, chương trình">
          <Input.TextArea rows={3} />
        </Form.Item>
      </Form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------

function ModalDieuHanhPhien({
  phienHopId,
  thanhVien,
  tyLeThongQua,
  hoiDongId,
  onDong,
}: {
  phienHopId: string;
  thanhVien: DongThanhVien[];
  tyLeThongQua: number;
  hoiDongId: string;
  onDong: () => void;
}) {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();
  const duocHopPhien = useAuthStore((s) => s.coQuyen('HOI_DONG.HOP_PHIEN'));
  const duocBoPhieu = useAuthStore((s) => s.coQuyen('HOI_DONG.BO_PHIEU'));
  const duocKetLuan = useAuthStore((s) => s.coQuyen('HOI_DONG.KET_LUAN'));

  const [ketLuan, setKetLuan] = useState('');

  const truyVan = useQuery({
    queryKey: ['phien-hop', phienHopId],
    queryFn: () => apiHoiDong.phienHop(phienHopId),
  });

  // Phòng họp trực tuyến: thư ký điểm danh hay một thành viên bỏ phiếu thì mọi người đang mở
  // phiên thấy ngay, không phải bấm tải lại và đọc số liệu cũ.
  useEffect(() => {
    const huy = theoDoiPhienHop(phienHopId, () => {
      void queryClient.invalidateQueries({ queryKey: ['phien-hop', phienHopId] });
      void queryClient.invalidateQueries({ queryKey: ['ket-qua-bo-phieu', phienHopId] });
    });

    return huy;
  }, [phienHopId, queryClient]);

  const diemDanh = useMutation({
    mutationFn: (duLieu: { thanhVienId: string; coMat: boolean; lyDoVang?: string }) =>
      apiHoiDong.diemDanh(phienHopId, duLieu),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['phien-hop', phienHopId] }),
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không điểm danh được.'),
  });

  const ketThuc = useMutation({
    mutationFn: () => apiHoiDong.ketThucPhienHop(phienHopId, ketLuan || undefined),
    onSuccess: () => {
      message.success('Đã kết thúc phiên họp');
      void queryClient.invalidateQueries({ queryKey: ['phien-hop', phienHopId] });
      void queryClient.invalidateQueries({ queryKey: ['hoi-dong', hoiDongId] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không kết thúc được.'),
  });

  const phien = truyVan.data;
  const daKetThuc = phien?.trangThaiPhien === 'DA_KET_THUC';

  const tenThanhVien = useMemo(
    () => new Map(thanhVien.map((x) => [x.id, x.hoTenHienThi])),
    [thanhVien],
  );

  useEffect(() => {
    if (phien?.ketLuan) setKetLuan(phien.ketLuan);
  }, [phien?.ketLuan]);

  return (
    <Modal
      open
      width={900}
      title={phien ? `Điều hành phiên: ${phien.tenPhien}` : 'Phiên họp'}
      footer={
        <Space>
          <Button onClick={onDong}>Đóng</Button>
          {duocKetLuan && !daKetThuc && (
            <Button
              type="primary"
              loading={ketThuc.isPending}
              onClick={() =>
                modal.confirm({
                  title: 'Kết thúc phiên họp?',
                  content:
                    'Sau khi kết thúc, các thành viên không bỏ phiếu thêm được. Kết luận sẽ được lưu vào phiên.',
                  okText: 'Kết thúc phiên',
                  cancelText: 'Huỷ',
                  onOk: () => ketThuc.mutateAsync(),
                })
              }
            >
              Kết thúc phiên
            </Button>
          )}
        </Space>
      }
      onCancel={onDong}
    >
      {truyVan.isLoading && <KhoiDangTai />}
      {truyVan.error && <KhoiLoi loi={truyVan.error} thuLai={truyVan.refetch} />}

      {phien && (
        <Tabs
          items={[
            {
              key: 'diem-danh',
              label: `Điểm danh (${phien.diemDanh.filter((x) => x.coMat).length}/${phien.diemDanh.length})`,
              children: (
                <Table
                  rowKey="id"
                  size="small"
                  pagination={false}
                  dataSource={phien.diemDanh}
                  columns={[
                    {
                      title: 'Thành viên',
                      dataIndex: 'thanhVienId',
                      render: (v: string) => tenThanhVien.get(v) ?? v,
                    },
                    {
                      title: 'Có mặt',
                      dataIndex: 'coMat',
                      width: 120,
                      render: (v: boolean, dong) => (
                        <Checkbox
                          checked={v}
                          disabled={!duocHopPhien || daKetThuc}
                          onChange={(e) =>
                            diemDanh.mutate({
                              thanhVienId: dong.thanhVienId,
                              coMat: e.target.checked,
                              lyDoVang: e.target.checked ? undefined : (dong.lyDoVang ?? undefined),
                            })
                          }
                        >
                          Có mặt
                        </Checkbox>
                      ),
                    },
                    {
                      title: 'Lý do vắng',
                      dataIndex: 'lyDoVang',
                      render: (v: string | null, dong) =>
                        dong.coMat ? (
                          '—'
                        ) : (
                          <Input
                            defaultValue={v ?? ''}
                            placeholder="Nhập lý do rồi rời khỏi ô để lưu"
                            disabled={!duocHopPhien || daKetThuc}
                            onBlur={(e) =>
                              diemDanh.mutate({
                                thanhVienId: dong.thanhVienId,
                                coMat: false,
                                lyDoVang: e.target.value,
                              })
                            }
                          />
                        ),
                    },
                  ]}
                />
              ),
            },
            {
              key: 'bo-phieu',
              label: `Hồ sơ và bỏ phiếu (${phien.danhSachHoSo.length})`,
              children:
                phien.danhSachHoSo.length === 0 ? (
                  <KhoiRong moTa="Phiên họp chưa gắn hồ sơ nào." />
                ) : (
                  <Space direction="vertical" size={12} style={{ width: '100%' }}>
                    {[...phien.danhSachHoSo]
                      .sort((a, b) => a.thuTu - b.thuTu)
                      .map((hs) => (
                        <TheHoSoBoPhieu
                          key={hs.id}
                          phienHopId={phienHopId}
                          sangKienId={hs.sangKienId}
                          thuTu={hs.thuTu}
                          tyLeThongQua={tyLeThongQua}
                          ketLuanRiengBanDau={hs.ketLuanRieng ?? ''}
                          ketQuaBanDau={hs.ketQua ?? undefined}
                          duocBoPhieu={duocBoPhieu && !daKetThuc}
                          duocKetLuan={duocKetLuan && !daKetThuc}
                        />
                      ))}
                  </Space>
                ),
            },
            {
              key: 'bien-ban',
              label: 'Biên bản',
              children: (
                <TabBienBan phienHopId={phienHopId} phienDaKetThuc={daKetThuc} />
              ),
            },
            {
              key: 'ket-luan',
              label: 'Kết luận',
              children: (
                <>
                  <Alert
                    type={daKetThuc ? 'success' : 'info'}
                    showIcon
                    style={{ marginBottom: 12 }}
                    message={
                      daKetThuc
                        ? 'Phiên họp đã kết thúc — nội dung dưới đây là kết luận đã lưu.'
                        : 'Nhập kết luận rồi bấm "Kết thúc phiên" để lưu.'
                    }
                  />
                  <Input.TextArea
                    rows={6}
                    value={ketLuan}
                    disabled={daKetThuc || !duocKetLuan}
                    placeholder="Kết luận của chủ tịch hội đồng"
                    onChange={(e) => setKetLuan(e.target.value)}
                  />
                </>
              ),
            },
          ]}
        />
      )}
    </Modal>
  );
}

// ---------------------------------------------------------------------------

function TheHoSoBoPhieu({
  phienHopId,
  sangKienId,
  thuTu,
  tyLeThongQua,
  ketLuanRiengBanDau,
  ketQuaBanDau,
  duocBoPhieu,
  duocKetLuan,
}: {
  phienHopId: string;
  sangKienId: string;
  thuTu: number;
  tyLeThongQua: number;
  ketLuanRiengBanDau: string;
  ketQuaBanDau?: string;
  duocBoPhieu: boolean;
  duocKetLuan: boolean;
}) {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const [ghiChu, setGhiChu] = useState('');
  const [phieuKin, setPhieuKin] = useState(false);
  const [ketLuanRieng, setKetLuanRieng] = useState(ketLuanRiengBanDau);
  const [ketQuaHoSo, setKetQuaHoSo] = useState<string | undefined>(ketQuaBanDau);

  const hoSo = useQuery({
    queryKey: ['sang-kien', sangKienId],
    queryFn: () => apiSangKien.chiTiet(sangKienId),
  });

  const ketQua = useQuery({
    queryKey: ['ket-qua-bo-phieu', phienHopId, sangKienId],
    queryFn: () => apiHoiDong.ketQuaBoPhieu(phienHopId, sangKienId),
  });

  const boPhieu = useMutation({
    mutationFn: (yKien: string) =>
      apiHoiDong.boPhieu({ phienHopId, sangKienId, yKien, ghiChu, laPhieuKin: phieuKin }),
    onSuccess: () => {
      message.success('Đã ghi nhận phiếu');
      void queryClient.invalidateQueries({
        queryKey: ['ket-qua-bo-phieu', phienHopId, sangKienId],
      });
      void queryClient.invalidateQueries({ queryKey: ['phien-hop', phienHopId] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không bỏ phiếu được.'),
  });

  /**
   * Kết luận riêng cho từng hồ sơ, tách khỏi kết luận chung của phiên: một phiên xét nhiều hồ sơ,
   * gộp hết vào một ô thì biên bản không tách được kết luận theo từng hồ sơ.
   */
  const ghiYKien = useMutation({
    mutationFn: (duLieu: { ketLuanRieng?: string; ketQua?: string }) =>
      apiHoiDong.ghiYKienHoSo(phienHopId, {
        sangKienId,
        ketLuanRieng: duLieu.ketLuanRieng ?? ketLuanRieng,
        ketQua: duLieu.ketQua ?? ketQuaHoSo ?? null,
      }),
    onSuccess: () => {
      message.success('Đã lưu ý kiến cho hồ sơ');
      void queryClient.invalidateQueries({ queryKey: ['phien-hop', phienHopId] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không lưu được ý kiến.'),
  });

  const kq = ketQua.data;
  const datNguong = (kq?.tyLeDongY ?? 0) >= tyLeThongQua;

  return (
    <Card
      size="small"
      title={
        <Space wrap>
          <Tag>{thuTu}</Tag>
          <Link to={`/sang-kien/${sangKienId}`}>
            {hoSo.data ? `${hoSo.data.maHoSo} — ${hoSo.data.tenSangKien}` : 'Đang tải hồ sơ...'}
          </Link>
        </Space>
      }
    >
      <Row gutter={[12, 12]}>
        <Col xs={24} md={10}>
          <Space size="large">
            <Statistic title="Tổng phiếu" value={kq?.tongPhieu ?? 0} />
            <Statistic title="Đồng ý" value={kq?.dongY ?? 0} valueStyle={{ color: '#389e0d' }} />
            <Statistic
              title="Tỷ lệ đồng ý"
              value={kq?.tyLeDongY ?? 0}
              suffix="%"
              valueStyle={{ color: datNguong ? '#389e0d' : '#cf1322' }}
            />
          </Space>
          <div style={{ marginTop: 4 }}>
            {kq && kq.tongPhieu > 0 && (
              <Tag color={datNguong ? 'success' : 'error'}>
                {datNguong ? 'Đạt' : 'Chưa đạt'} ngưỡng thông qua {tyLeThongQua}%
              </Tag>
            )}
          </div>
        </Col>

        <Col xs={24} md={14}>
          <Input.TextArea
            rows={2}
            placeholder="Ý kiến kèm theo phiếu (tuỳ chọn)"
            value={ghiChu}
            disabled={!duocBoPhieu}
            onChange={(e) => setGhiChu(e.target.value)}
            style={{ marginBottom: 8 }}
          />
          <Space wrap>
            <Checkbox
              checked={phieuKin}
              disabled={!duocBoPhieu}
              onChange={(e) => setPhieuKin(e.target.checked)}
            >
              Phiếu kín
            </Checkbox>
            <Button
              type="primary"
              disabled={!duocBoPhieu}
              loading={boPhieu.isPending}
              onClick={() => boPhieu.mutate('DONG_Y')}
            >
              Đồng ý
            </Button>
            <Button
              danger
              disabled={!duocBoPhieu}
              loading={boPhieu.isPending}
              onClick={() => boPhieu.mutate('KHONG_DONG_Y')}
            >
              Không đồng ý
            </Button>
            <Button
              disabled={!duocBoPhieu}
              loading={boPhieu.isPending}
              onClick={() => boPhieu.mutate('Y_KIEN_KHAC')}
            >
              Ý kiến khác
            </Button>
          </Space>
        </Col>

        <Col span={24}>
          <Divider style={{ margin: '8px 0' }} orientation="left" plain>
            Kết luận của hội đồng cho hồ sơ này
          </Divider>
          <Space direction="vertical" size={8} style={{ width: '100%' }}>
            <Input.TextArea
              rows={2}
              placeholder="Ý kiến / kết luận riêng cho hồ sơ này — nội dung này vào biên bản họp"
              value={ketLuanRieng}
              disabled={!duocKetLuan}
              onChange={(e) => setKetLuanRieng(e.target.value)}
            />
            <Space wrap>
              <Select
                style={{ width: 200 }}
                allowClear
                placeholder="Kết quả xét"
                value={ketQuaHoSo}
                disabled={!duocKetLuan}
                options={[
                  { value: 'DAT', label: 'Đạt' },
                  { value: 'KHONG_DAT', label: 'Không đạt' },
                  { value: 'HOAN', label: 'Hoãn / bổ sung' },
                ]}
                onChange={(v) => setKetQuaHoSo(v)}
              />
              <Button
                disabled={!duocKetLuan}
                loading={ghiYKien.isPending}
                onClick={() => ghiYKien.mutate({})}
              >
                Lưu kết luận hồ sơ
              </Button>
              {kq && kq.tongPhieu > 0 && !ketQuaHoSo && (
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  Gợi ý theo phiếu: {datNguong ? 'Đạt' : 'Không đạt'}
                </Typography.Text>
              )}
            </Space>
          </Space>
        </Col>
      </Row>
    </Card>
  );
}

// ---------------------------------------------------------------------------

const TRANG_THAI_PHIEU: Record<string, { mau: string; ten: string }> = {
  CHUA_CHAM: { mau: 'default', ten: 'Chưa chấm' },
  NHAP: { mau: 'processing', ten: 'Đang chấm' },
  DA_GUI: { mau: 'success', ten: 'Đã gửi' },
  DA_KY: { mau: 'success', ten: 'Đã ký' },
};

/**
 * Chức năng 35 — Bảng ma trận điểm: hàng là hồ sơ, cột là thành viên.
 *
 * Điểm chỉ hiện sau khi phiếu đã gửi (nguyên tắc chấm điểm độc lập). Thư ký mở lại được phiếu
 * đã gửi ngay trên bảng khi thành viên chấm nhầm.
 */
function BangMaTranDiem({
  hoiDongId,
  dotDeNghiId,
}: {
  hoiDongId: string;
  dotDeNghiId?: string | null;
}) {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();
  const duocMoLai = useAuthStore((s) => s.coQuyen('DANH_GIA.MO_LAI_PHIEU'));

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['ma-tran-diem', hoiDongId, dotDeNghiId],
    queryFn: () => apiDanhGia.maTranDiem(hoiDongId, dotDeNghiId ?? undefined),
  });

  const moLai = useMutation({
    mutationFn: (phieuId: string) => apiDanhGia.moLaiPhieu(phieuId),
    onSuccess: () => {
      message.success('Đã mở lại phiếu — thành viên sửa và gửi lại được');
      void queryClient.invalidateQueries({ queryKey: ['ma-tran-diem', hoiDongId] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không mở lại được.'),
  });

  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  const thanhVien = data?.[0]?.diemThanhVien ?? [];

  return (
    <>
      <Typography.Paragraph type="secondary" style={{ fontSize: 12 }}>
        Điểm chỉ hiển thị sau khi thành viên đã <strong>gửi</strong> phiếu — bảo đảm nguyên tắc chấm
        điểm độc lập.
        {duocMoLai && ' Bấm vào ô đã gửi để mở lại phiếu cho thành viên sửa.'}
      </Typography.Paragraph>

      <Table<DongMaTranDiem>
        rowKey="sangKienId"
        size="small"
        loading={isLoading}
        dataSource={data ?? []}
        scroll={{ x: 400 + thanhVien.length * 150 }}
        locale={{
          emptyText: (
            <KhoiRong moTa="Hội đồng chưa được phân công chấm hồ sơ nào (hoặc chưa có hồ sơ thuộc đợt này)." />
          ),
        }}
        columns={[
          {
            title: 'Hồ sơ',
            dataIndex: 'maHoSo',
            width: 260,
            fixed: 'left',
            render: (v: string, dong) => (
              <Space direction="vertical" size={0}>
                <Link to={`/sang-kien/${dong.sangKienId}`}>{v}</Link>
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  {dong.tenSangKien}
                </Typography.Text>
              </Space>
            ),
          },
          ...thanhVien.map((tv, viTri) => ({
            title: tv.tenThanhVien,
            key: tv.thanhVienId,
            width: 150,
            align: 'center' as const,
            render: (_v: unknown, dong: DongMaTranDiem) => {
              const o = dong.diemThanhVien[viTri];
              if (!o) return '—';

              const nhan = TRANG_THAI_PHIEU[o.trangThai] ?? { mau: 'default', ten: o.trangThai };
              const daGui = o.trangThai === 'DA_GUI' || o.trangThai === 'DA_KY';

              return (
                <Space direction="vertical" size={2}>
                  <Typography.Text strong>{o.diem?.toFixed(2) ?? '—'}</Typography.Text>
                  <Tag color={nhan.mau} style={{ marginInlineEnd: 0 }}>
                    {nhan.ten}
                  </Tag>
                  {duocMoLai && daGui && o.phieuId && (
                    <Button
                      size="small"
                      type="link"
                      loading={moLai.isPending && moLai.variables === o.phieuId}
                      onClick={() =>
                        modal.confirm({
                          title: 'Mở lại phiếu đã gửi?',
                          content: `Phiếu của ${o.tenThanhVien} sẽ mở khoá để sửa và gửi lại. Điểm tổng hợp cũ không còn đúng cho tới khi tổng hợp lại.`,
                          okText: 'Mở lại phiếu',
                          cancelText: 'Huỷ',
                          onOk: () => moLai.mutateAsync(o.phieuId!),
                        })
                      }
                    >
                      Mở lại
                    </Button>
                  )}
                </Space>
              );
            },
          })),
          {
            title: 'Trung bình',
            dataIndex: 'diemTrungBinh',
            width: 110,
            align: 'right',
            fixed: 'right',
            render: (v?: number | null, dong?: DongMaTranDiem) => (
              <Space direction="vertical" size={0}>
                <Typography.Text strong>{v?.toFixed(2) ?? '—'}</Typography.Text>
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  {dong?.soPhieuDaCham}/{dong?.soPhieuPhanCong} phiếu
                </Typography.Text>
              </Space>
            ),
          },
        ]}
        pagination={false}
      />
    </>
  );
}
