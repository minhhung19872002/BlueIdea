import { Link } from 'react-router-dom';
import { Button, Space, Table, Tooltip, Typography } from 'antd';
import { FileExcelOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';

import { taiTep } from '@/api/client';
import type { SangKienTomTat } from '@/api/endpoints';
import { HienThiHan, NhanTrangThai, NhanTrungLap, ngayGio } from '@/components/ThanhPhanChung';

interface Props {
  duLieu: SangKienTomTat[];
  tongSo: number;
  trang: number;
  soDong: number;
  dangTai?: boolean;
  onDoiTrang: (trang: number, soDong: number) => void;
  chonNhieu?: {
    khoaDaChon: string[];
    onDoiChon: (khoa: string[]) => void;
  };
  thamSoXuat?: Record<string, unknown>;
  duongDanXuat?: string;
  tenTepXuat?: string;
}

/** Bảng danh sách hồ sơ dùng chung cho các màn hình: của tôi, tiếp nhận, xử lý, tra cứu. */
export function BangSangKien({
  duLieu,
  tongSo,
  trang,
  soDong,
  dangTai,
  onDoiTrang,
  chonNhieu,
  thamSoXuat,
  duongDanXuat = '/api/v1/sang-kien/xuat-excel',
  tenTepXuat = 'danh-sach-sang-kien.xlsx',
}: Props) {
  const cot: ColumnsType<SangKienTomTat> = [
    {
      title: 'Mã hồ sơ',
      dataIndex: 'maHoSo',
      width: 140,
      fixed: 'left',
      sorter: true,
      render: (giaTri: string, dong) => <Link to={`/sang-kien/${dong.id}`}>{giaTri}</Link>,
    },
    {
      title: 'Tên sáng kiến',
      dataIndex: 'tenSangKien',
      sorter: true,
      render: (giaTri: string, dong) => (
        <div>
          <Link to={`/sang-kien/${dong.id}`}>{giaTri}</Link>
          <div style={{ fontSize: 12, color: '#888' }}>
            {dong.tacGiaChinh}
            {dong.tenDonVi ? ` — ${dong.tenDonVi}` : ''}
          </div>
        </div>
      ),
    },
    { title: 'Lĩnh vực', dataIndex: 'tenLinhVuc', width: 170, responsive: ['lg'] },
    { title: 'Đợt', dataIndex: 'tenDot', width: 200, responsive: ['xl'] },
    {
      title: 'Trạng thái',
      dataIndex: 'trangThaiTong',
      width: 150,
      render: (giaTri: string, dong) => (
        <Space direction="vertical" size={2}>
          <NhanTrangThai trangThai={giaTri} />
          {dong.tenBuocHienTai && (
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              {dong.tenBuocHienTai}
            </Typography.Text>
          )}
        </Space>
      ),
    },
    {
      title: 'Điểm',
      dataIndex: 'tongDiem',
      width: 90,
      align: 'right',
      sorter: true,
      render: (giaTri?: number | null) => giaTri?.toFixed(2) ?? '—',
    },
    {
      title: 'Trùng lặp',
      dataIndex: 'tyLeTrungLap',
      width: 120,
      sorter: true,
      render: (giaTri: number | null) => <NhanTrungLap tyLe={giaTri} />,
    },
    {
      title: 'Hạn xử lý',
      dataIndex: 'hanXuLyHienTai',
      width: 160,
      sorter: true,
      responsive: ['lg'],
      render: (giaTri: string | null, dong) => <HienThiHan han={giaTri} quaHan={dong.quaHan} />,
    },
    {
      title: 'Ngày nộp',
      dataIndex: 'ngayNop',
      width: 120,
      responsive: ['xl'],
      render: (giaTri: string | null) => ngayGio(giaTri, false),
    },
  ];

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 8 }}>
        <Tooltip title="Xuất danh sách theo bộ lọc hiện tại">
          <Button
            icon={<FileExcelOutlined />}
            onClick={() => taiTep(duongDanXuat, tenTepXuat, thamSoXuat)}
          >
            Xuất Excel
          </Button>
        </Tooltip>
      </div>

      <Table<SangKienTomTat>
        rowKey="id"
        size="middle"
        columns={cot}
        dataSource={duLieu}
        loading={dangTai}
        scroll={{ x: 1200 }}
        rowSelection={
          chonNhieu && {
            selectedRowKeys: chonNhieu.khoaDaChon,
            onChange: (khoa) => chonNhieu.onDoiChon(khoa as string[]),
          }
        }
        rowClassName={(dong) =>
          (dong.tyLeTrungLap ?? 0) > 40
            ? 'o-trung-lap-cao'
            : (dong.tyLeTrungLap ?? 0) >= 20
              ? 'o-trung-lap-canh-bao'
              : ''
        }
        pagination={{
          current: trang,
          pageSize: soDong,
          total: tongSo,
          showSizeChanger: true,
          showTotal: (tong, khoang) => `${khoang[0]}–${khoang[1]} trong ${tong} hồ sơ`,
          onChange: onDoiTrang,
        }}
      />
    </>
  );
}
