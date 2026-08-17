import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';

import { layDuLieu, layPhanTrang } from '@/api/client';
import { useCauHinhStore } from '@/app/store/cauHinhStore';

/** Một dòng kết quả trên cổng công khai — đúng 5 trường của bản thiết kế. */
interface SangKienCongKhai {
  id: string;
  maHoSo: string;
  tenSangKien: string;
  tacGiaChinh: string | null;
  nam: number | null;
  soQuyetDinh: string | null;
  tenLinhVuc: string | null;
  tenDonVi: string | null;
}

interface ThongKeCongKhai {
  soSangKien: number;
  soNam: number;
  soQuyetDinh: number;
}

interface LinhVucCongKhai {
  id: string;
  ten: string;
  soLuong: number;
}

/** Lưới 5 cột dùng chung cho hàng tiêu đề và hàng dữ liệu — đúng số đo bản thiết kế. */
const COT = '110px 1fr 150px 90px 130px';

/**
 * Cổng tra cứu sáng kiến công khai — KHÔNG cần đăng nhập.
 *
 * Gọi nhóm endpoint riêng `/api/v1/cong-khai/*` chứ không dùng endpoint hồ sơ nội bộ:
 * endpoint nội bộ yêu cầu xác thực nên người dân chưa đăng nhập sẽ nhận 401 và trang trắng.
 */
export default function TrangCongKhai() {
  const dieuHuong = useNavigate();
  const [dangGo, setDangGo] = useState('');
  const [tuKhoa, setTuKhoa] = useState('');
  const [linhVucId, setLinhVucId] = useState<string | null>(null);
  const [trang, setTrang] = useState(1);
  const soDong = 20;

  const { tenDonVi, napCauHinhCongKhai } = useCauHinhStore();

  useEffect(() => {
    void napCauHinhCongKhai();
  }, [napCauHinhCongKhai]);

  const { data: thongKe } = useQuery({
    queryKey: ['cong-khai', 'thong-ke'],
    queryFn: () => layDuLieu<ThongKeCongKhai>('/api/v1/cong-khai/thong-ke'),
  });

  const { data: linhVuc } = useQuery({
    queryKey: ['cong-khai', 'linh-vuc'],
    queryFn: () => layDuLieu<LinhVucCongKhai[]>('/api/v1/cong-khai/linh-vuc'),
  });

  const { data, isLoading } = useQuery({
    queryKey: ['cong-khai', 'sang-kien', { tuKhoa, linhVucId, trang }],
    queryFn: () =>
      layPhanTrang<SangKienCongKhai>('/api/v1/cong-khai/sang-kien', {
        tuKhoa,
        linhVucId,
        trang,
        soDong,
      }),
  });

  function tim() {
    setTuKhoa(dangGo);
    setTrang(1);
  }

  function chonLinhVuc(id: string | null) {
    setLinhVucId(id);
    setTrang(1);
  }

  const dong = data?.duLieu ?? [];
  const tongSo = data?.tongSo ?? 0;
  const soTrang = Math.ceil(tongSo / soDong);

  return (
    <div className="ck-goc">
      <header className="ck-dau">
        <div className="ck-logo">BI</div>
        <span style={{ fontWeight: 600, fontSize: 15 }}>Cổng thông tin sáng kiến</span>
        <span className="ck-don-vi">· {tenDonVi}</span>
        <button type="button" className="ck-nut-dang-nhap" onClick={() => dieuHuong('/dang-nhap')}>
          Đăng nhập cán bộ
        </button>
      </header>

      <div className="ck-banner">
        <div className="ck-banner-tieu-de">Tra cứu sáng kiến đã công nhận</div>
        <div className="ck-banner-mo-ta">
          Công khai — không cần đăng nhập · Cập nhật theo quyết định công nhận mới nhất
        </div>

        <div className="ck-o-tim">
          <input
            className="ck-input"
            placeholder="Nhập tên sáng kiến, tác giả, số quyết định…"
            value={dangGo}
            onChange={(e) => setDangGo(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && tim()}
            aria-label="Từ khoá tra cứu"
          />
          <button type="button" className="ck-nut-tim" onClick={tim}>
            🔍 Tìm kiếm
          </button>
        </div>

        <div className="ck-so-lieu">
          <div>
            <b>{thongKe?.soSangKien ?? 0}</b>sáng kiến công nhận
          </div>
          <div>
            <b>{thongKe?.soNam ?? 0}</b>năm dữ liệu
          </div>
          <div>
            <b>{thongKe?.soQuyetDinh ?? 0}</b>quyết định
          </div>
        </div>
      </div>

      <div className="ck-than">
        <div className="ck-chip-hang">
          <span className="ck-nhan">Lĩnh vực:</span>
          <button
            type="button"
            className={`ck-chip${linhVucId === null ? ' ck-chip-chon' : ''}`}
            onClick={() => chonLinhVuc(null)}
          >
            Tất cả
          </button>
          {(linhVuc ?? []).map((lv) => (
            <button
              key={lv.id}
              type="button"
              className={`ck-chip${linhVucId === lv.id ? ' ck-chip-chon' : ''}`}
              onClick={() => chonLinhVuc(lv.id)}
            >
              {lv.ten} ({lv.soLuong})
            </button>
          ))}
          <span className="ck-dem">{tongSo} kết quả</span>
        </div>

        <div className="ck-bang">
          <div className="ck-hang ck-hang-dau" style={{ gridTemplateColumns: COT }}>
            <div>Mã</div>
            <div>Tên sáng kiến</div>
            <div>Tác giả</div>
            <div>Năm</div>
            <div>Quyết định</div>
          </div>

          {isLoading ? (
            <div className="ck-trong">Đang tải…</div>
          ) : dong.length === 0 ? (
            <div className="ck-trong">
              {tuKhoa || linhVucId
                ? 'Không tìm thấy sáng kiến nào khớp điều kiện tra cứu.'
                : 'Chưa có sáng kiến nào được công bố công khai.'}
            </div>
          ) : (
            dong.map((r) => (
              <div key={r.id} className="ck-hang" style={{ gridTemplateColumns: COT }}>
                <div data-nhan="Mã">
                  <span className="ck-ma">{r.maHoSo}</span>
                </div>
                <div data-nhan="Tên sáng kiến">
                  {/* Bọc trong một khối con để ở chế độ thẻ dọc (dưới 768px) nhãn cột nằm bên
                      trái còn tên và dòng phụ xếp dọc bên phải, thay vì cả ba lọt chung một
                      hàng ngang. */}
                  <div>
                    {r.tenSangKien}
                    {/* Lĩnh vực và đơn vị không có cột riêng trong thiết kế nhưng là thông tin
                        người dân cần để phân biệt các sáng kiến trùng tên. */}
                    {(r.tenLinhVuc || r.tenDonVi) && (
                      <div className="ck-phu">
                        {[r.tenLinhVuc, r.tenDonVi].filter(Boolean).join(' · ')}
                      </div>
                    )}
                  </div>
                </div>
                <div data-nhan="Tác giả" className="ck-mo">
                  {r.tacGiaChinh || '—'}
                </div>
                <div data-nhan="Năm" className="ck-mo">
                  {r.nam ?? '—'}
                </div>
                <div data-nhan="Quyết định">
                  {/* Ô trống không dùng màu link: gạch ngang màu xanh trông như bấm được. */}
                  {r.soQuyetDinh ? (
                    <span className="ck-qd">{r.soQuyetDinh}</span>
                  ) : (
                    <span className="ck-mo">—</span>
                  )}
                </div>
              </div>
            ))
          )}
        </div>

        {soTrang > 1 && (
          <div className="ck-phan-trang">
            <button type="button" disabled={trang <= 1} onClick={() => setTrang(trang - 1)}>
              ← Trước
            </button>
            <span>
              Trang {trang}/{soTrang}
            </span>
            <button type="button" disabled={trang >= soTrang} onClick={() => setTrang(trang + 1)}>
              Sau →
            </button>
          </div>
        )}
      </div>

      <footer className="ck-chan">{tenDonVi} · hotro@tranbien.gov.vn · 0251 3822 xxx</footer>
    </div>
  );
}
