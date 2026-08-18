import { useState } from 'react';
import {
  App,
  Alert,
  Button,
  Card,
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

import { z } from 'zod';

import { LoiApi } from '@/api/client';
import { BieuMau, Truong, useBieuMau } from '@/components/bieu-mau/BieuMau';
import {
  batBuoc,
  trangThai as trangThaiDanhMuc,
  tuyChon,
} from '@/components/bieu-mau/luat';
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

/**
 * Luật kiểm tra cấu hình chữ ký số.
 *
 * `clientSecret` để trống khi SỬA nghĩa là giữ nguyên bí mật đang lưu, nên không thể bắt buộc —
 * máy chủ cũng không bao giờ trả bí mật cũ về giao diện để điền sẵn.
 */
const luatChuKySo = z.object({
  nhaCungCap: batBuoc('Nhà cung cấp'),
  loaiKy: batBuoc('Hình thức ký'),
  chungThuSo: tuyChon(200),
  endpoint: z
    .string()
    .trim()
    .url('Phải là địa chỉ http/https tuyệt đối.')
    .optional()
    .or(z.literal('').transform(() => undefined)),
  clientId: tuyChon(200),
  clientSecret: tuyChon(500),
  thuatToan: batBuoc('Thuật toán ký'),
  trangThai: trangThaiDanhMuc,
  laMacDinh: z.boolean(),
});

type GiaTriChuKySo = z.infer<typeof luatChuKySo>;

const MAC_DINH_CHU_KY: GiaTriChuKySo = {
  nhaCungCap: 'BAN_CO_YEU_CHINH_PHU',
  loaiKy: 'USB_TOKEN',
  thuatToan: 'SHA256withRSA',
  trangThai: 1,
  laMacDinh: true,
};

/** Chức năng 49 — Cấu hình chữ ký số dùng để ký quyết định và biên bản. */
export default function TrangChuKySo() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [dangSua, setDangSua] = useState<CauHinhChuKySo | null>(null);
  const [moForm, setMoForm] = useState(false);
  const form = useBieuMau(luatChuKySo, MAC_DINH_CHU_KY);

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
    mutationFn: async (giaTri: GiaTriChuKySo) => {
      const duLieu = {
        nhaCungCap: giaTri.nhaCungCap,
        loaiKy: giaTri.loaiKy,
        endpoint: giaTri.endpoint ?? null,
        clientId: giaTri.clientId ?? null,
        clientSecret: giaTri.clientSecret ?? null,
        chungThuSo: giaTri.chungThuSo ?? null,
        thuatToan: giaTri.thuatToan,
        trangThai: giaTri.trangThai,
        laMacDinh: giaTri.laMacDinh,
      };

      return dangSua ? apiChuKySo.sua(dangSua.id, duLieu) : apiChuKySo.them(duLieu);
    },
    onSuccess: () => {
      message.success(dangSua ? 'Đã cập nhật cấu hình' : 'Đã thêm cấu hình chữ ký số');
      setMoForm(false);
      setDangSua(null);
      form.reset(MAC_DINH_CHU_KY);
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
            form.reset(MAC_DINH_CHU_KY);
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
                      form.reset({
                        nhaCungCap: dong.nhaCungCap,
                        loaiKy: dong.loaiKy,
                        chungThuSo: dong.chungThuSo ?? undefined,
                        endpoint: dong.endpoint ?? undefined,
                        clientId: dong.clientId ?? undefined,
                        // Bí mật không bao giờ được trả về giao diện — để trống = giữ nguyên.
                        clientSecret: undefined,
                        thuatToan: dong.thuatToan,
                        trangThai: dong.trangThai as 0 | 1,
                        laMacDinh: dong.laMacDinh,
                      });
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
        confirmLoading={luu.isPending || form.formState.isSubmitting}
        onCancel={() => setMoForm(false)}
        okButtonProps={{ htmlType: 'submit', form: 'form-chu-ky-so' }}
      >
        <BieuMau id="form-chu-ky-so" form={form} onGui={(giaTri) => luu.mutateAsync(giaTri)}>
          <Space size="large" wrap style={{ display: 'flex' }}>
            <Truong<GiaTriChuKySo> ten="nhaCungCap" label="Nhà cung cấp chứng thư">
              {(o) => (
                <Select
                  {...o}
                  value={o.value as string}
                  style={{ width: 260 }}
                  options={NHA_CUNG_CAP}
                />
              )}
            </Truong>
            <Truong<GiaTriChuKySo> ten="loaiKy" label="Hình thức ký">
              {(o) => (
                <Select {...o} value={o.value as string} style={{ width: 260 }} options={LOAI_KY} />
              )}
            </Truong>
          </Space>

          <Truong<GiaTriChuKySo>
            ten="chungThuSo"
            label="Serial chứng thư trên máy chủ"
            tooltip="Dùng khi ký bằng USB token hoặc HSM. Bỏ trống nếu máy chủ đã nạp tệp PFX."
          >
            {(o) => (
              <Input
                {...o}
                value={o.value as string}
                placeholder="VD: 54010102030405060708090A0B0C0D0E"
              />
            )}
          </Truong>

          <Truong<GiaTriChuKySo> ten="endpoint" label="Endpoint dịch vụ ký từ xa">
            {(o) => (
              <Input
                {...o}
                value={o.value as string}
                placeholder="https://ca.example.gov.vn/sign"
              />
            )}
          </Truong>

          <Space size="large" wrap style={{ display: 'flex' }}>
            <Truong<GiaTriChuKySo> ten="clientId" label="Client ID">
              {(o) => <Input {...o} value={o.value as string} style={{ width: 260 }} />}
            </Truong>
            <Truong<GiaTriChuKySo>
              ten="clientSecret"
              label="Client secret"
              tooltip={dangSua ? 'Để trống = giữ nguyên bí mật đang lưu.' : undefined}
            >
              {(o) => (
                <Input.Password
                  {...o}
                  value={o.value as string}
                  style={{ width: 260 }}
                  placeholder={dangSua ? 'Để trống nếu không đổi' : ''}
                />
              )}
            </Truong>
          </Space>

          <Space size="large" wrap>
            <Truong<GiaTriChuKySo> ten="thuatToan" label="Thuật toán ký">
              {(o) => (
                <Select
                  {...o}
                  value={o.value as string}
                  style={{ width: 220 }}
                  options={[
                    { value: 'SHA256withRSA', label: 'SHA256withRSA' },
                    { value: 'SHA384withRSA', label: 'SHA384withRSA' },
                    { value: 'SHA512withRSA', label: 'SHA512withRSA' },
                  ]}
                />
              )}
            </Truong>
            <Truong<GiaTriChuKySo> ten="trangThai" label="Trạng thái">
              {(o) => (
                <Select
                  {...o}
                  value={o.value as number}
                  style={{ width: 180 }}
                  options={[
                    { value: 1, label: 'Hoạt động' },
                    { value: 0, label: 'Ngừng' },
                  ]}
                />
              )}
            </Truong>
            <Truong<GiaTriChuKySo> ten="laMacDinh" label="Dùng làm mặc định">
              {(o) => (
                <Select
                  {...o}
                  value={o.value as boolean}
                  style={{ width: 150 }}
                  options={[
                    { value: true, label: 'Có' },
                    { value: false, label: 'Không' },
                  ]}
                />
              )}
            </Truong>
          </Space>
        </BieuMau>
      </Modal>
    </Card>
  );
}
