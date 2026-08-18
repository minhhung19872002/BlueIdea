import type { MucTab } from '@/components/DaiTabTrang';

/**
 * Các nhánh con của trang cấu hình — bản thiết kế gộp thành MỘT mục ở thanh điều hướng
 * rồi tách bằng tab trong trang. Khai báo một chỗ để thêm tab mới không phải sửa từng trang.
 */
export const DS_TAB_CAU_HINH: MucTab[] = [
  { ma: 'he-thong', ten: 'Hệ thống', duongDan: '/quan-tri/cau-hinh/he-thong' },
  { ma: 'sang-kien', ten: 'Thông tin sáng kiến', duongDan: '/quan-tri/cau-hinh/sang-kien' },
  { ma: 'email-sms', ten: 'Email & SMS', duongDan: '/quan-tri/gui-tin' },
  { ma: 'mau-thong-bao', ten: 'Mẫu thông báo', duongDan: '/quan-tri/mau-thong-bao' },
  { ma: 'chu-ky-so', ten: 'Chữ ký số', duongDan: '/quan-tri/chu-ky-so' },
  { ma: 'tich-hop', ten: 'Tích hợp', duongDan: '/quan-tri/cau-hinh/tich-hop' },
  { ma: 'menu', ten: 'Menu', duongDan: '/quan-tri/cau-hinh/menu' },
  { ma: 'sao-luu', ten: 'Sao lưu', duongDan: '/quan-tri/sao-luu' },
];
