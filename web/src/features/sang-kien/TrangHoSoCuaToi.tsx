import { useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { Button, Card, Col, Input, Row, Select, Space } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';

import { apiDotDeNghi, apiSangKien } from '@/api/endpoints';
import { BangSangKien } from './BangSangKien';
import { KhoiLoi, KhoiRong } from '@/components/ThanhPhanChung';

const TRANG_THAI = [
  { value: 'NHAP', label: 'Nháp' },
  { value: 'DA_NOP', label: 'Đã nộp' },
  { value: 'DANG_XU_LY', label: 'Đang xử lý' },
  { value: 'YEU_CAU_BO_SUNG', label: 'Yêu cầu bổ sung' },
  { value: 'DA_PHE_DUYET', label: 'Đã phê duyệt' },
  { value: 'KHONG_DAT', label: 'Không đạt' },
  { value: 'DA_RUT', label: 'Đã rút' },
];

/** Chức năng 23 — Danh sách hồ sơ của tác giả đang đăng nhập. */
export default function TrangHoSoCuaToi() {
  // Ghi nhớ bộ lọc trong URL để chia sẻ/khôi phục được (yêu cầu Mục 9 đặc tả).
  const [thamSoUrl, datThamSoUrl] = useSearchParams();
  const [trang, setTrang] = useState(1);
  const [soDong, setSoDong] = useState(20);

  const tuKhoa = thamSoUrl.get('tuKhoa') ?? '';
  const trangThaiTong = thamSoUrl.get('trangThaiTong') ?? undefined;
  const dotDeNghiId = thamSoUrl.get('dotDeNghiId') ?? undefined;

  const thamSo = { trang, soDong, tuKhoa, trangThaiTong, dotDeNghiId };

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['sang-kien', 'cua-toi', thamSo],
    queryFn: () => apiSangKien.cuaToi(thamSo),
  });

  const { data: cacDot } = useQuery({
    queryKey: ['dot-de-nghi', 'chon'],
    queryFn: () => apiDotDeNghi.chon(),
  });

  function datLoc(khoa: string, giaTri?: string) {
    const moi = new URLSearchParams(thamSoUrl);
    if (giaTri) {
      moi.set(khoa, giaTri);
    } else {
      moi.delete(khoa);
    }
    datThamSoUrl(moi);
    setTrang(1);
  }

  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  return (
    <Card
      title="Hồ sơ sáng kiến của tôi"
      extra={
        <Link to="/sang-kien/nop-moi">
          <Button type="primary" icon={<PlusOutlined />}>
            Nộp sáng kiến mới
          </Button>
        </Link>
      }
    >
      <Row gutter={[8, 8]} style={{ marginBottom: 12 }}>
        <Col xs={24} md={10}>
          <Input.Search
            placeholder="Tìm theo tên hoặc mã hồ sơ (gõ không dấu vẫn ra kết quả)"
            defaultValue={tuKhoa}
            allowClear
            onSearch={(giaTri) => datLoc('tuKhoa', giaTri)}
          />
        </Col>
        <Col xs={12} md={7}>
          <Select
            style={{ width: '100%' }}
            placeholder="Tất cả trạng thái"
            allowClear
            value={trangThaiTong}
            options={TRANG_THAI}
            onChange={(giaTri) => datLoc('trangThaiTong', giaTri)}
          />
        </Col>
        <Col xs={12} md={7}>
          <Select
            style={{ width: '100%' }}
            placeholder="Tất cả đợt"
            allowClear
            value={dotDeNghiId}
            options={(cacDot ?? []).map((d) => ({ value: d.id, label: d.ten }))}
            onChange={(giaTri) => datLoc('dotDeNghiId', giaTri)}
          />
        </Col>
      </Row>

      {!isLoading && (data?.tongSo ?? 0) === 0 ? (
        <KhoiRong
          moTa="Bạn chưa có hồ sơ sáng kiến nào. Hãy bắt đầu bằng cách nộp sáng kiến mới."
          hanhDong={
            <Link to="/sang-kien/nop-moi">
              <Button type="primary" icon={<PlusOutlined />}>
                Nộp sáng kiến mới
              </Button>
            </Link>
          }
        />
      ) : (
        <BangSangKien
          duLieu={data?.duLieu ?? []}
          tongSo={data?.tongSo ?? 0}
          trang={trang}
          soDong={soDong}
          dangTai={isLoading}
          thamSoXuat={thamSo}
          onDoiTrang={(t, s) => {
            setTrang(t);
            setSoDong(s);
          }}
        />
      )}

      <Space style={{ marginTop: 8 }} />
    </Card>
  );
}
