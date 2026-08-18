import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { Button, Checkbox, Dropdown, Space, Table, Tooltip, Typography } from 'antd';
import { FileExcelOutlined, SettingOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';

import { taiTep } from '@/api/client';
import type { SangKienTomTat } from '@/api/endpoints';
import { HienThiHan, NhanTrangThai, NhanTrungLap, ngayGio } from '@/components/ThanhPhanChung';

/** Các cột người dùng được phép ẩn — mã hồ sơ và tên sáng kiến luôn hiện vì bảng mất chúng thì vô nghĩa. */
const COT_TUY_CHON = [
  { khoa: 'tenLinhVuc', ten: 'Lĩnh vực' },
  { khoa: 'tenDot', ten: 'Đợt' },
  { khoa: 'trangThaiTong', ten: 'Trạng thái' },
  { khoa: 'tongDiem', ten: 'Điểm' },
  { khoa: 'tyLeTrungLap', ten: 'Trùng lặp' },
  { khoa: 'hanXuLyHienTai', ten: 'Hạn xử lý' },
  { khoa: 'ngayNop', ten: 'Ngày nộp' },
];

const KHOA_LUU_COT = 'blueidea.cot-hien-thi';

/** Tô đậm phần khớp từ khoá trong một chuỗi. */
function toKhop(chuoi: string, tuKhoa?: string) {
  if (!tuKhoa || tuKhoa.trim().length < 2) return chuoi;

  // Escape ký tự đặc biệt: người dùng gõ "(" vào ô tìm kiếm không được làm vỡ regex.
  const mau = tuKhoa.trim().replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const phan = chuoi.split(new RegExp(`(${mau})`, 'gi'));

  return phan.map((x, i) =>
    x.toLowerCase() === tuKhoa.trim().toLowerCase() ? (
      <mark key={`${x}-${i}`} style={{ padding: 0, background: '#fff1b8' }}>
        {x}
      </mark>
    ) : (
      x
    ),
  );
}

interface Props {
  duLieu: SangKienTomTat[];
  tongSo: number;
  trang: number;
  soDong: number;
  dangTai?: boolean;
  onDoiTrang: (trang: number, soDong: number) => void;
  /**
   * Người dùng bấm tiêu đề cột để sắp xếp.
   *
   * Các cột khai `sorter: true` nghĩa là sắp xếp Ở MÁY CHỦ — bảng chỉ vẽ mũi tên, còn việc sắp
   * là của truy vấn. Không truyền hàm này thì bấm vào tiêu đề chỉ đổi mũi tên còn danh sách
   * đứng yên, người dùng tưởng hệ thống hỏng.
   */
  onDoiSapXep?: (sapXep?: string, huong?: 'asc' | 'desc') => void;
  chonNhieu?: {
    khoaDaChon: string[];
    onDoiChon: (khoa: string[]) => void;
  };
  thamSoXuat?: Record<string, unknown>;
  duongDanXuat?: string;
  tenTepXuat?: string;
  /** Từ khoá đang tìm — dùng để tô đậm phần khớp trong kết quả. */
  tuKhoaToDam?: string;
  /** Cho phép người dùng chọn cột hiển thị (chức năng 28). */
  chonCot?: boolean;
}

/** Bảng danh sách hồ sơ dùng chung cho các màn hình: của tôi, tiếp nhận, xử lý, tra cứu. */
export function BangSangKien({
  duLieu,
  tongSo,
  trang,
  soDong,
  dangTai,
  onDoiTrang,
  onDoiSapXep,
  chonNhieu,
  thamSoXuat,
  duongDanXuat = '/api/v1/sang-kien/xuat-excel',
  tenTepXuat = 'danh-sach-sang-kien.xlsx',
  tuKhoaToDam,
  chonCot,
}: Props) {
  const [cotHien, setCotHien] = useState<string[]>(() => {
    try {
      const luu = localStorage.getItem(KHOA_LUU_COT);
      return luu ? (JSON.parse(luu) as string[]) : COT_TUY_CHON.map((x) => x.khoa);
    } catch {
      // localStorage hỏng hoặc bị chặn: hiện đủ cột còn hơn hiện bảng trống.
      return COT_TUY_CHON.map((x) => x.khoa);
    }
  });

  useEffect(() => {
    try {
      localStorage.setItem(KHOA_LUU_COT, JSON.stringify(cotHien));
    } catch {
      // Không lưu được thì thôi — lựa chọn vẫn áp dụng cho phiên hiện tại.
    }
  }, [cotHien]);

  const cot: ColumnsType<SangKienTomTat> = [
    {
      title: 'Mã hồ sơ',
      dataIndex: 'maHoSo',
      key: 'maHoSo',
      width: 140,
      fixed: 'left',
      sorter: true,
      render: (giaTri: string, dong) => (
        <Link to={`/sang-kien/${dong.id}`}>{toKhop(giaTri, tuKhoaToDam)}</Link>
      ),
    },
    {
      // Có width: các cột còn lại đều cố định, cột này không đặt width thì phần dư có thể
      // âm và tên sáng kiến bị bóp thành một ký tự mỗi dòng.
      title: 'Tên sáng kiến',
      dataIndex: 'tenSangKien',
      key: 'tenSangKien',
      width: 320,
      sorter: true,
      render: (giaTri: string, dong) => (
        <div>
          <Link to={`/sang-kien/${dong.id}`}>{toKhop(giaTri, tuKhoaToDam)}</Link>
          <div style={{ fontSize: 12, color: '#888' }}>
            {toKhop(dong.tacGiaChinh ?? '', tuKhoaToDam)}
            {dong.tenDonVi ? ` — ${dong.tenDonVi}` : ''}
          </div>
        </div>
      ),
    },
    {
      title: 'Lĩnh vực',
      dataIndex: 'tenLinhVuc',
      key: 'tenLinhVuc',
      width: 170,
      responsive: ['lg'],
    },
    { title: 'Đợt', dataIndex: 'tenDot', key: 'tenDot', width: 200, responsive: ['xl'] },
    {
      title: 'Trạng thái',
      dataIndex: 'trangThaiTong',
      key: 'trangThaiTong',
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
      key: 'tongDiem',
      width: 90,
      align: 'right',
      sorter: true,
      render: (giaTri?: number | null) => giaTri?.toFixed(2) ?? '—',
    },
    {
      title: 'Trùng lặp',
      dataIndex: 'tyLeTrungLap',
      key: 'tyLeTrungLap',
      width: 120,
      sorter: true,
      render: (giaTri: number | null) => <NhanTrungLap tyLe={giaTri} />,
    },
    {
      title: 'Hạn xử lý',
      dataIndex: 'hanXuLyHienTai',
      key: 'hanXuLyHienTai',
      width: 160,
      sorter: true,
      responsive: ['lg'],
      render: (giaTri: string | null, dong) => <HienThiHan han={giaTri} quaHan={dong.quaHan} />,
    },
    {
      title: 'Ngày nộp',
      dataIndex: 'ngayNop',
      key: 'ngayNop',
      width: 120,
      responsive: ['xl'],
      render: (giaTri: string | null) => ngayGio(giaTri, false),
    },
  ];

  const cotHienThi = useMemo(
    () =>
      cot.filter(
        (c) =>
          !chonCot
          || typeof c.key === 'undefined'
          || !COT_TUY_CHON.some((x) => x.khoa === String(c.key))
          || cotHien.includes(String(c.key)),
      ),
    [cot, chonCot, cotHien],
  );

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 8, gap: 8 }}>
        {chonCot && (
          <Dropdown
            trigger={['click']}
            dropdownRender={() => (
              <div
                style={{
                  background: 'var(--nen-the, #fff)',
                  padding: 12,
                  borderRadius: 8,
                  boxShadow: '0 6px 16px rgba(0,0,0,0.12)',
                }}
              >
                <Checkbox.Group
                  value={cotHien}
                  onChange={(v) => setCotHien(v as string[])}
                  options={COT_TUY_CHON.map((x) => ({ value: x.khoa, label: x.ten }))}
                  style={{ display: 'flex', flexDirection: 'column', gap: 6 }}
                />
              </div>
            )}
          >
            <Tooltip title="Chọn cột hiển thị — lựa chọn được nhớ cho lần sau">
              <Button icon={<SettingOutlined />}>Cột hiển thị</Button>
            </Tooltip>
          </Dropdown>
        )}
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
        columns={cotHienThi}
        dataSource={duLieu}
        loading={dangTai}
        scroll={{ x: 1460 }}
        rowSelection={
          chonNhieu && {
            selectedRowKeys: chonNhieu.khoaDaChon,
            onChange: (khoa) => chonNhieu.onDoiChon(khoa as string[]),
          }
        }
        onChange={(_phanTrang, _boLoc, sapXep) => {
          if (!onDoiSapXep) return;

          // Bảng cho phép sắp nhiều cột nên antd có thể trả về mảng; ở đây chỉ sắp một cột.
          const s = Array.isArray(sapXep) ? sapXep[0] : sapXep;
          const cot = s?.columnKey ?? s?.field;

          // Bấm lần thứ ba trên cùng cột = bỏ sắp xếp: antd trả order rỗng.
          if (!cot || !s?.order) {
            onDoiSapXep(undefined, undefined);
            return;
          }

          onDoiSapXep(String(cot), s.order === 'descend' ? 'desc' : 'asc');
        }}
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
