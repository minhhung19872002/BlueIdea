import { useMemo } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';

import { apiBaoCao } from '@/api/endpoints';
import { KhoiDangTai, KhoiLoi } from '@/components/ThanhPhanChung';
import { useAuthStore } from '@/app/store/authStore';

/** Bảng màu vòng tròn trạng thái — lấy đúng thứ tự trong bản thiết kế. */
const BANG_MAU = ['#1677ff', '#52c41a', '#faad14', '#ff4d4f', '#722ed1', '#13c2c2', '#eb2f96'];

export default function TrangDashboard() {
  const nguoiDung = useAuthStore((s) => s.nguoiDung);

  // Trang chủ là nơi MỌI vai trò rơi vào sau khi đăng nhập, nhưng số liệu tổng quan là dữ liệu
  // toàn hệ thống nên máy chủ đòi quyền BAO_CAO.XEM. Thành viên hội đồng hay tác giả không có
  // quyền đó: gọi API rồi hiện lỗi đỏ ngay trang chủ là sai — phải hiện bảng điều hướng rút gọn.
  const duocXemBaoCao = useAuthStore((s) => s.coQuyen('BAO_CAO.XEM'));

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['bao-cao', 'tong-quan'],
    queryFn: () => apiBaoCao.tongQuan(),
    enabled: duocXemBaoCao,
  });

  const trangThai = data?.theoTrangThai ?? [];
  const linhVuc = data?.theoLinhVuc ?? [];
  const xuHuong = data?.xuHuongTheoNam ?? [];
  const topDonVi = data?.topDonVi ?? [];

  /** Vòng tròn vẽ bằng conic-gradient như thiết kế, không cần thư viện biểu đồ. */
  const vongTron = useMemo(() => {
    const tong = trangThai.reduce((s, x) => s + x.soLuong, 0);
    if (tong === 0) return { nen: '#f5f5f5', tong: 0 };

    let goc = 0;
    const doan = trangThai.map((x, i) => {
      const tu = goc;
      goc += (x.soLuong / tong) * 360;
      return `${BANG_MAU[i % BANG_MAU.length]} ${tu}deg ${goc}deg`;
    });

    return { nen: `conic-gradient(${doan.join(',')})`, tong };
  }, [trangThai]);

  const linhVucToiDa = Math.max(1, ...linhVuc.map((x) => x.soLuong));
  const xuHuongToiDa = Math.max(1, ...xuHuong.map((x) => x.soLuong));

  if (!duocXemBaoCao) return <TrangChuRutGon hoTen={nguoiDung?.hoTen} />;
  if (isLoading) return <KhoiDangTai />;
  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  return (
    <div className="tk-cot">
      <div style={{ fontSize: 20, fontWeight: 600 }}>Xin chào, {nguoiDung?.hoTen}</div>

      {/* --- Bốn chỉ số chính --- */}
      <div className="tk-luoi tk-luoi-4">
        <OChiSo nhan="Tổng hồ sơ" icon="📄" giaTri={data?.tongHoSo ?? 0} />
        <OChiSo nhan="Đang xử lý" icon="🕐" giaTri={data?.hoSoDangXuLy ?? 0} mau="#1677ff" />
        <OChiSo
          nhan="Được công nhận"
          icon="✅"
          giaTri={data?.hoSoDat ?? 0}
          phu={`(${data?.tyLeDat ?? 0}%)`}
          mau="#52c41a"
        />
        <Link to="/xu-ly?chiQuaHan=true" style={{ color: 'inherit' }}>
          <OChiSo
            nhan="Quá hạn xử lý"
            icon="⚠️"
            giaTri={data?.hoSoQuaHan ?? 0}
            mau={(data?.hoSoQuaHan ?? 0) > 0 ? '#ff4d4f' : undefined}
          />
        </Link>
      </div>

      {/* --- Ba biểu đồ --- */}
      <div className="tk-luoi tk-luoi-3">
        <div className="tk-the">
          <div className="tk-the-dau">Hồ sơ theo trạng thái</div>
          <div style={{ padding: '20px 16px', display: 'flex', alignItems: 'center', gap: 20 }}>
            <div
              style={{
                width: 130,
                height: 130,
                borderRadius: '50%',
                background: vongTron.nen,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                flexShrink: 0,
              }}
            >
              <div
                style={{
                  width: 76,
                  height: 76,
                  borderRadius: '50%',
                  background: '#fff',
                  display: 'flex',
                  flexDirection: 'column',
                  alignItems: 'center',
                  justifyContent: 'center',
                }}
              >
                <b style={{ fontSize: 18 }}>{vongTron.tong}</b>
                <span style={{ fontSize: 11, color: 'rgba(0,0,0,0.45)' }}>hồ sơ</span>
              </div>
            </div>

            <div
              style={{
                display: 'flex',
                flexDirection: 'column',
                gap: 7,
                fontSize: 12.5,
                minWidth: 0,
              }}
            >
              {trangThai.map((x, i) => (
                <div key={x.ten} style={{ display: 'flex', alignItems: 'center', gap: 7 }}>
                  <span
                    style={{
                      width: 8,
                      height: 8,
                      borderRadius: '50%',
                      background: BANG_MAU[i % BANG_MAU.length],
                      flexShrink: 0,
                    }}
                  />
                  <span
                    style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}
                  >
                    {x.ten}
                  </span>
                  <span>
                    · <b>{x.soLuong}</b>
                  </span>
                </div>
              ))}
            </div>
          </div>
        </div>

        <div className="tk-the">
          <div className="tk-the-dau">Hồ sơ theo lĩnh vực</div>
          <div
            style={{ padding: 16, display: 'flex', flexDirection: 'column', gap: 10 }}
          >
            {linhVuc.map((x) => (
              <div
                key={x.ten}
                style={{
                  display: 'grid',
                  gridTemplateColumns: '98px 1fr 30px',
                  alignItems: 'center',
                  gap: 10,
                }}
              >
                <div
                  style={{
                    fontSize: 12.5,
                    color: 'rgba(0,0,0,0.65)',
                    whiteSpace: 'nowrap',
                    overflow: 'hidden',
                    textOverflow: 'ellipsis',
                  }}
                  title={x.ten}
                >
                  {x.ten}
                </div>
                <div className="tk-thanh-nen">
                  <div
                    className="tk-thanh"
                    style={{ width: `${(x.soLuong / linhVucToiDa) * 100}%` }}
                  />
                </div>
                <div style={{ fontSize: 12.5, fontWeight: 600, textAlign: 'right' }}>
                  {x.soLuong}
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="tk-the">
          <div className="tk-the-dau">Xu hướng theo năm</div>
          <div
            style={{
              padding: '20px 16px 8px',
              display: 'flex',
              alignItems: 'flex-end',
              gap: 18,
              height: 190,
              justifyContent: 'center',
            }}
          >
            {xuHuong.map((x) => (
              <div
                key={x.ten}
                style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 6 }}
              >
                <span style={{ fontSize: 11, color: '#52c41a', fontWeight: 600 }}>
                  {x.giaTriPhu ?? 0}
                </span>
                <div
                  style={{
                    width: 40,
                    height: Math.max(4, (x.soLuong / xuHuongToiDa) * 120),
                    background: '#1677ff',
                    borderRadius: '4px 4px 0 0',
                  }}
                />
                <span style={{ fontSize: 12, color: 'rgba(0,0,0,0.65)' }}>{x.ten}</span>
              </div>
            ))}
          </div>
          <div
            style={{
              padding: '0 16px 12px',
              fontSize: 11.5,
              color: 'rgba(0,0,0,0.45)',
              textAlign: 'center',
            }}
          >
            Cột: tổng hồ sơ · Số xanh: được công nhận
          </div>
        </div>
      </div>

      {/* --- Xếp hạng đơn vị và cảnh báo --- */}
      <div className="tk-luoi tk-luoi-2">
        <div className="tk-the">
          <div className="tk-the-dau">Top đơn vị có sáng kiến được công nhận</div>
          <div style={{ padding: '8px 16px 12px' }}>
            {topDonVi.map((x, i) => (
              <div key={x.ten} className="tk-dong-ds">
                <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                  {i + 1}. {x.ten}
                </span>
                <span style={{ flexShrink: 0 }}>
                  <b style={{ color: '#52c41a', fontWeight: 600 }}>{x.giaTriPhu ?? 0}</b>
                  <span style={{ color: 'rgba(0,0,0,0.45)' }}> / {x.soLuong}</span>
                </span>
              </div>
            ))}
          </div>
        </div>

        <div className="tk-the">
          <div className="tk-the-dau">Cảnh báo</div>
          <div style={{ padding: 16 }}>
            <div style={{ fontSize: 14, color: 'rgba(0,0,0,0.45)' }}>
              Hồ sơ có tỷ lệ trùng lặp cao (&gt; 40%)
            </div>
            <div
              style={{
                fontSize: 24,
                fontWeight: 600,
                marginTop: 4,
                color: (data?.soCanhBaoTrungLapCao ?? 0) > 0 ? '#ff4d4f' : '#52c41a',
              }}
            >
              {data?.soCanhBaoTrungLapCao ?? 0}
            </div>
            <div style={{ marginTop: 12 }}>
              <Link to="/tra-cuu?trungLapTu=40">Xem danh sách hồ sơ nghi trùng lặp →</Link>
            </div>

            {(data?.hoSoQuaHan ?? 0) > 0 && (
              <div
                style={{
                  marginTop: 14,
                  background: '#fffbe6',
                  border: '1px solid #ffe58f',
                  borderRadius: 6,
                  padding: '9px 12px',
                  fontSize: 13,
                  color: 'rgba(0,0,0,0.75)',
                }}
              >
                ⚠ {data?.hoSoQuaHan} hồ sơ đang quá hạn xử lý.{' '}
                <Link to="/xu-ly?chiQuaHan=true">Xem việc cần xử lý</Link>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

/** Ô chỉ số theo đúng số đo thiết kế: đệm 20/24, nhãn 14px mờ, số 24px. */
function OChiSo({
  nhan,
  icon,
  giaTri,
  phu,
  mau,
}: {
  nhan: string;
  icon: string;
  giaTri: number;
  phu?: string;
  mau?: string;
}) {
  return (
    <div className="tk-kpi">
      <div className="tk-kpi-nhan">{nhan}</div>
      <div className="tk-kpi-so" style={{ color: mau }}>
        <span style={{ fontSize: 16 }}>{icon}</span>
        <b>{giaTri}</b>
        {phu && <span className="tk-kpi-phu">{phu}</span>}
      </div>
    </div>
  );
}

/**
 * Trang chủ rút gọn cho vai trò không được xem thống kê toàn hệ thống (thành viên hội đồng,
 * tác giả…): thay vì báo lỗi thiếu quyền, đưa thẳng các lối tắt vào việc họ thực sự làm.
 */
function TrangChuRutGon({ hoTen }: { hoTen?: string | null }) {
  const coQuyen = useAuthStore((s) => s.coQuyen);

  const loiTat = [
    {
      quyen: 'DANH_GIA.XEM',
      duongDan: '/danh-gia',
      icon: '✅',
      ten: 'Việc đánh giá',
      moTa: 'Hồ sơ được phân công cho bạn chấm điểm, kèm hạn hoàn thành.',
    },
    {
      quyen: 'HOI_DONG.XEM',
      duongDan: '/hoi-dong',
      icon: '🧮',
      ten: 'Hội đồng sáng kiến',
      moTa: 'Phiên họp, điểm danh, bỏ phiếu và ma trận điểm của hội đồng.',
    },
    {
      quyen: 'XU_LY.XEM',
      duongDan: '/xu-ly',
      icon: '⚙️',
      ten: 'Việc cần xử lý',
      moTa: 'Hồ sơ đang chờ bạn xử lý ở bước hiện tại.',
    },
    {
      quyen: 'TIEP_NHAN.XEM',
      duongDan: '/tiep-nhan',
      icon: '📥',
      ten: 'Chờ tiếp nhận',
      moTa: 'Hồ sơ vừa nộp, chờ kiểm tra tính hợp lệ.',
    },
    {
      quyen: 'SANG_KIEN.XEM',
      duongDan: '/sang-kien/cua-toi',
      icon: '📁',
      ten: 'Hồ sơ của tôi',
      moTa: 'Sáng kiến bạn đã nộp và tiến độ xử lý từng hồ sơ.',
    },
    {
      quyen: 'SANG_KIEN.THEM',
      duongDan: '/sang-kien/nop-moi',
      icon: '📝',
      ten: 'Nộp hồ sơ mới',
      moTa: 'Đăng ký một sáng kiến mới theo đợt đang mở.',
    },
    {
      quyen: 'SANG_KIEN.XEM',
      duongDan: '/tra-cuu',
      icon: '🔍',
      ten: 'Tra cứu',
      moTa: 'Tìm sáng kiến theo từ khoá hoặc theo ý nghĩa nội dung.',
    },
  ].filter((x) => coQuyen(x.quyen));

  return (
    <div className="tk-cot">
      <div style={{ fontSize: 20, fontWeight: 600 }}>Xin chào, {hoTen}</div>

      <div style={{ fontSize: 14, color: 'rgba(0,0,0,0.45)' }}>
        Tài khoản của bạn không được cấp quyền xem thống kê toàn hệ thống. Dưới đây là các chức năng
        bạn dùng thường xuyên.
      </div>

      <div className="tk-luoi tk-luoi-3">
        {loiTat.map((x) => (
          <Link key={x.duongDan} to={x.duongDan} style={{ color: 'inherit' }}>
            <div className="tk-the" style={{ height: '100%' }}>
              <div style={{ padding: 20 }}>
                <div style={{ fontSize: 22 }}>{x.icon}</div>
                <div style={{ fontSize: 16, fontWeight: 600, marginTop: 6 }}>{x.ten}</div>
                <div style={{ fontSize: 13, color: 'rgba(0,0,0,0.45)', marginTop: 4 }}>{x.moTa}</div>
              </div>
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
}
