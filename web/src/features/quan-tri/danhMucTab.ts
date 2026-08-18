import type { MucTab } from '@/components/DaiTabTrang';

/**
 * Các nhánh con của trang danh mục — bản thiết kế gộp thành MỘT mục ở thanh điều hướng
 * rồi tách bằng tab trong trang. Khai báo ở đây để mọi trang danh mục dùng chung một dải tab.
 */
export const DS_TAB_DANH_MUC: MucTab[] = [
  { ma: 'linh-vuc', ten: 'Lĩnh vực', duongDan: '/quan-tri/danh-muc/linh-vuc' },
  { ma: 'doi-tuong', ten: 'Đối tượng', duongDan: '/quan-tri/danh-muc/doi-tuong' },
  { ma: 'loai-tac-gia', ten: 'Loại tác giả', duongDan: '/quan-tri/danh-muc/loai-tac-gia' },
  { ma: 'dot-de-nghi', ten: 'Đợt đề nghị', duongDan: '/quan-tri/danh-muc/dot-de-nghi' },
  { ma: 'bieu-mau-xuat', ten: 'Biểu mẫu xuất', duongDan: '/quan-tri/danh-muc/bieu-mau-xuat' },
  { ma: 'cap-phe-duyet', ten: 'Cấp phê duyệt', duongDan: '/quan-tri/danh-muc/cap-phe-duyet' },
];
