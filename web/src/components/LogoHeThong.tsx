import { useState } from 'react';

import { DUONG_DAN_LOGO, useCauHinhStore } from '@/app/store/cauHinhStore';

/**
 * Logo hệ thống — ảnh do quản trị viên tải lên, không có thì dùng chữ viết tắt.
 *
 * Trước đây chỗ này là hộp chữ "BI" viết cứng trong mã nguồn, nên một đơn vị dùng nền tảng này
 * không có cách nào đặt nhận diện của mình lên đầu trang, dù đặc tả xếp logo và favicon vào nhóm
 * cấu hình tối thiểu phải có.
 *
 * Vẫn giữ phương án dự phòng bằng chữ viết tắt: chưa cấu hình logo, hoặc tệp lỗi, thì đầu trang
 * phải có gì đó chứ không được để một ô trống.
 */
export function LogoHeThong({ kichThuoc = 28 }: { kichThuoc?: number }) {
  const { tenHeThong, coLogo, mauChuDao } = useCauHinhStore();
  const [loiAnh, setLoiAnh] = useState(false);

  const vietTat = tenHeThong
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((x) => x[0]?.toUpperCase() ?? '')
    .join('') || 'BI';

  if (coLogo && !loiAnh) {
    return (
      <img
        src={DUONG_DAN_LOGO}
        alt={tenHeThong}
        onError={() => setLoiAnh(true)}
        style={{
          width: kichThuoc,
          height: kichThuoc,
          objectFit: 'contain',
          borderRadius: Math.round(kichThuoc / 4),
          flexShrink: 0,
        }}
      />
    );
  }

  return (
    <div
      aria-label={tenHeThong}
      style={{
        width: kichThuoc,
        height: kichThuoc,
        borderRadius: Math.round(kichThuoc / 4),
        background: mauChuDao,
        color: '#fff',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        fontWeight: 800,
        fontSize: Math.round(kichThuoc * 0.43),
        flexShrink: 0,
      }}
    >
      {vietTat}
    </div>
  );
}
