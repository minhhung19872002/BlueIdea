import { useNavigate } from 'react-router-dom';

export interface MucTab {
  /** Giá trị khớp với tham số route (ví dụ "linh-vuc"). */
  ma: string;
  ten: string;
  /** Đường dẫn điều hướng khi bấm. */
  duongDan: string;
}

/**
 * Dải tab đặt ngay dưới tiêu đề trang.
 *
 * Bản thiết kế gộp các danh mục con (danh mục dùng chung, cấu hình, nhật ký, báo cáo) thành
 * MỘT mục ở thanh điều hướng rồi tách nhánh bằng tab trong trang — nhờ vậy thanh điều hướng
 * không dài quá màn hình. Mỗi tab vẫn là một đường dẫn riêng nên chia sẻ link và nút quay lại
 * của trình duyệt hoạt động bình thường.
 */
export function DaiTabTrang({ danhSach, dangChon }: { danhSach: MucTab[]; dangChon: string }) {
  const dieuHuong = useNavigate();

  return (
    <div
      className="tk-dai-tab"
      style={{
        display: 'flex',
        gap: 2,
        borderBottom: '1px solid #f0f0f0',
        marginBottom: 16,
        overflowX: 'auto',
      }}
      role="tablist"
    >
      {danhSach.map((t) => {
        const chon = t.ma === dangChon;

        return (
          <div
            key={t.ma}
            role="tab"
            tabIndex={0}
            aria-selected={chon}
            onClick={() => dieuHuong(t.duongDan)}
            onKeyDown={(e) => {
              if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                dieuHuong(t.duongDan);
              }
            }}
            style={{
              padding: '8px 16px',
              fontSize: 14,
              whiteSpace: 'nowrap',
              color: chon ? '#1677ff' : 'rgba(0,0,0,0.65)',
              fontWeight: chon ? 600 : 400,
              borderBottom: chon ? '2px solid #1677ff' : '2px solid transparent',
              marginBottom: -1,
              cursor: 'pointer',
            }}
          >
            {t.ten}
          </div>
        );
      })}
    </div>
  );
}
