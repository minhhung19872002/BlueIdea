import {
  App,
  Alert,
  Button,
  Descriptions,
  Space,
  Table,
  Tag,
  Typography,
} from 'antd';
import { FilePdfOutlined, FileTextOutlined, SafetyCertificateOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { LoiApi, taiTep } from '@/api/client';
import { apiBienBan, type BienBanHop, type DongBienBanHoSo } from '@/api/endpoints';
import { useAuthStore } from '@/app/store/authStore';
import { KhoiDangTai, KhoiLoi, KhoiRong, ngayGio } from '@/components/ThanhPhanChung';

const TRANG_THAI_BIEN_BAN: Record<string, { mau: string; ten: string }> = {
  NHAP: { mau: 'default', ten: 'Nháp' },
  DA_LAP: { mau: 'processing', ten: 'Đã lập' },
  DA_KY: { mau: 'success', ten: 'Đã ký đủ' },
  DA_BAN_HANH: { mau: 'success', ten: 'Đã ban hành' },
};

const TEN_CHUC_DANH: Record<string, string> = {
  CHU_TICH: 'Chủ tịch hội đồng',
  PHO_CHU_TICH: 'Phó chủ tịch hội đồng',
  UY_VIEN_THU_KY: 'Uỷ viên thư ký',
  THU_KY: 'Thư ký',
  UY_VIEN: 'Uỷ viên',
};

/**
 * Nhóm IV — Biên bản phiên họp hội đồng.
 *
 * Biên bản do máy chủ sinh từ chính dữ liệu phiên họp (điểm danh, phiếu bầu, kết luận), nên màn
 * hình này chỉ có ba việc: lập / làm mới, ký nhận, và xuất hoặc ký số bản PDF. Không có ô nào
 * cho gõ tay lại số liệu — biên bản là căn cứ hành chính, số liệu phải trùng khớp dữ liệu.
 */
export function TabBienBan({
  phienHopId,
  phienDaKetThuc,
}: {
  phienHopId: string;
  phienDaKetThuc: boolean;
}) {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const duocHopPhien = useAuthStore((s) => s.coQuyen('HOI_DONG.HOP_PHIEN'));
  const duocKySo = useAuthStore((s) => s.coQuyen('QUYET_DINH.KY_SO'));

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['bien-ban', phienHopId],
    queryFn: () => apiBienBan.theoPhien(phienHopId),
  });

  function lamMoi() {
    void queryClient.invalidateQueries({ queryKey: ['bien-ban', phienHopId] });
  }

  const lap = useMutation({
    mutationFn: () => apiBienBan.lap(phienHopId),
    onSuccess: () => {
      message.success('Đã lập biên bản từ dữ liệu phiên họp');
      lamMoi();
    },
    onError: (loi) =>
      modal.error({
        title: 'Không lập được biên bản',
        content: loi instanceof LoiApi ? loi.message : 'Đã xảy ra lỗi.',
      }),
  });

  const ky = useMutation({
    mutationFn: (id: string) => apiBienBan.ky(id),
    onSuccess: () => {
      message.success('Đã ký nhận biên bản');
      lamMoi();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không ký được.'),
  });

  const kySo = useMutation({
    mutationFn: (id: string) => apiBienBan.kySo(id),
    onSuccess: () => {
      message.success('Đã ký số biên bản bằng chứng thư đang cấu hình');
      lamMoi();
    },
    onError: (loi) =>
      modal.error({
        title: 'Không ký số được',
        content: loi instanceof LoiApi ? loi.message : 'Đã xảy ra lỗi.',
      }),
  });

  if (isLoading) return <KhoiDangTai />;
  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  const bienBan: BienBanHop | null | undefined = data;

  if (!bienBan) {
    return (
      <Space direction="vertical" size={12} style={{ width: '100%' }}>
        <Alert
          type={phienDaKetThuc ? 'info' : 'warning'}
          showIcon
          message={
            phienDaKetThuc
              ? 'Phiên họp đã kết thúc — có thể lập biên bản.'
              : 'Chỉ lập được biên bản sau khi phiên họp đã kết thúc và có kết luận.'
          }
          description="Biên bản sinh tự động từ điểm danh, phiếu bầu và kết luận của phiên họp."
        />

        {duocHopPhien && (
          <Button
            type="primary"
            icon={<FileTextOutlined />}
            loading={lap.isPending}
            disabled={!phienDaKetThuc}
            onClick={() => lap.mutate()}
          >
            Lập biên bản
          </Button>
        )}
      </Space>
    );
  }

  const nhan = TRANG_THAI_BIEN_BAN[bienBan.trangThaiBienBan] ?? {
    mau: 'default',
    ten: bienBan.trangThaiBienBan,
  };

  const soDaKy = bienBan.chuKy.filter((x) => x.daKy).length;

  return (
    <Space direction="vertical" size={12} style={{ width: '100%' }}>
      <Space wrap>
        <Typography.Text strong>Biên bản {bienBan.soBienBan}</Typography.Text>
        <Tag color={nhan.mau}>{nhan.ten}</Tag>
        <Tag>
          {soDaKy}/{bienBan.chuKy.length} chữ ký
        </Tag>
        {bienBan.ngayLap && <Tag>Lập ngày {ngayGio(bienBan.ngayLap, false)}</Tag>}
      </Space>

      <Space wrap>
        <Button
          icon={<FilePdfOutlined />}
          onClick={() =>
            taiTep(
              apiBienBan.duongDanPdf(bienBan.id),
              `bien-ban-${bienBan.soBienBan}.pdf`,
            ).catch((loi: unknown) =>
              message.error(loi instanceof LoiApi ? loi.message : 'Không xuất được biên bản.'),
            )
          }
        >
          Xuất PDF
        </Button>

        <Button loading={ky.isPending} onClick={() => ky.mutate(bienBan.id)}>
          Ký nhận biên bản
        </Button>

        {duocKySo && (
          <Button
            icon={<SafetyCertificateOutlined />}
            loading={kySo.isPending}
            onClick={() =>
              modal.confirm({
                title: 'Ký số biên bản?',
                content:
                  'Hệ thống sinh bản PDF của biên bản hiện hành rồi ký bằng chứng thư đang cấu hình. Lập lại biên bản sau khi ký sẽ khiến chữ ký cũ không còn khớp nội dung.',
                okText: 'Ký số',
                cancelText: 'Huỷ',
                onOk: () => kySo.mutateAsync(bienBan.id),
              })
            }
          >
            Ký số
          </Button>
        )}

        {duocHopPhien && bienBan.trangThaiBienBan !== 'DA_KY' && (
          <Button loading={lap.isPending} onClick={() => lap.mutate()}>
            Lập lại từ dữ liệu phiên
          </Button>
        )}
      </Space>

      <Descriptions bordered size="small" column={{ xs: 1, md: 2 }}>
        <Descriptions.Item label="Thời gian họp">
          {ngayGio(bienBan.thoiGianBatDau)}
        </Descriptions.Item>
        <Descriptions.Item label="Địa điểm">{bienBan.diaDiem ?? '—'}</Descriptions.Item>
        <Descriptions.Item label="Có mặt">
          {bienBan.soCoMat}/{bienBan.soThanhVien} thành viên
        </Descriptions.Item>
        <Descriptions.Item label="Số hồ sơ xét">{bienBan.danhSachHoSo.length}</Descriptions.Item>
        <Descriptions.Item label="Kết luận chung" span={2}>
          {bienBan.ketLuanChung ?? '—'}
        </Descriptions.Item>
      </Descriptions>

      <Table<DongBienBanHoSo>
        rowKey="sangKienId"
        size="small"
        pagination={false}
        dataSource={bienBan.danhSachHoSo}
        scroll={{ x: 900 }}
        locale={{ emptyText: <KhoiRong moTa="Phiên họp không có hồ sơ nào." /> }}
        columns={[
          { title: 'Mã hồ sơ', dataIndex: 'maHoSo', width: 130 },
          { title: 'Tên sáng kiến', dataIndex: 'tenSangKien', width: 300 },
          {
            title: 'Điểm',
            dataIndex: 'tongDiem',
            width: 90,
            align: 'right',
            render: (v?: number | null) => v?.toFixed(2) ?? '—',
          },
          {
            title: 'Phiếu đồng ý',
            key: 'phieu',
            width: 130,
            align: 'center',
            render: (_v, dong) =>
              `${dong.soPhieuDongY}/${
                dong.soPhieuDongY + dong.soPhieuKhongDongY + dong.soPhieuYKienKhac
              }`,
          },
          {
            title: 'Tỷ lệ',
            dataIndex: 'tyLeDongY',
            width: 100,
            align: 'right',
            render: (v: number) => `${v.toFixed(2)}%`,
          },
          {
            title: 'Kết luận',
            dataIndex: 'datNguong',
            width: 160,
            render: (v: boolean) =>
              v ? <Tag color="success">Thông qua</Tag> : <Tag color="error">Không thông qua</Tag>,
          },
        ]}
      />

      <Typography.Text strong>Chữ ký</Typography.Text>

      <Table
        rowKey="thanhVienId"
        size="small"
        pagination={false}
        dataSource={bienBan.chuKy}
        scroll={{ x: 600 }}
        locale={{ emptyText: 'Hội đồng chưa có thành viên nào được cấp quyền ký biên bản.' }}
        columns={[
          { title: 'Họ tên', dataIndex: 'hoTen', width: 200 },
          {
            title: 'Chức danh',
            dataIndex: 'chucDanh',
            width: 220,
            render: (v: string) => TEN_CHUC_DANH[v] ?? v,
          },
          {
            title: 'Trạng thái',
            dataIndex: 'daKy',
            width: 190,
            render: (v: boolean, dong) =>
              v ? (
                <Space direction="vertical" size={0}>
                  <Tag color="success">Đã ký</Tag>
                  <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                    {ngayGio(dong.thoiGianKy)}
                  </Typography.Text>
                </Space>
              ) : (
                <Tag>Chưa ký</Tag>
              ),
          },
        ]}
      />
    </Space>
  );
}
