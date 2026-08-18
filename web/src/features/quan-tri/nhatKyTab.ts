import type { MucTab } from '@/components/DaiTabTrang';

/**
 * Các nhánh con của trang nhật ký — khai báo một chỗ để thêm loại nhật ký mới
 * không phải sửa từng trang.
 */
export const DS_TAB_NHAT_KY: MucTab[] = [
  { ma: 'he-thong', ten: 'Hệ thống', duongDan: '/quan-tri/nhat-ky/he-thong' },
  { ma: 'dang-nhap', ten: 'Đăng nhập', duongDan: '/quan-tri/nhat-ky/dang-nhap' },
  { ma: 'loi', ten: 'Lỗi hệ thống', duongDan: '/quan-tri/nhat-ky-loi' },
  { ma: 'dong-bo', ten: 'Đồng bộ liên thông', duongDan: '/quan-tri/nhat-ky/dong-bo' },
];
