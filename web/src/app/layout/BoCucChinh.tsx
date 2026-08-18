import { useMemo, useState } from 'react';
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { Avatar, Badge, Breadcrumb, Button, Drawer, Dropdown, Grid, Layout, Typography } from 'antd';
import {
  BellOutlined,
  KeyOutlined,
  LogoutOutlined,
  MenuOutlined,
  UserOutlined,
} from '@ant-design/icons';
import * as Icons from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';

import { useAuthStore } from '@/app/store/authStore';
import { useCauHinhStore, type MucMenu } from '@/app/store/cauHinhStore';
import { kichThuoc, mauSac } from '@/app/giaoDien';
import { layPhanTrang } from '@/api/client';
import { layDiaChiDangXuatSso } from '@/features/xac-thuc/sso';

const { Header, Sider, Content } = Layout;
const { useBreakpoint } = Grid;

/** Một dòng trong thanh điều hướng: tiêu đề nhóm hoặc mục bấm được. */
interface DongMenu {
  ma: string;
  ten: string;
  icon?: string | null;
  duongDan?: string | null;
  laTieuDeNhom: boolean;
}

/**
 * Icon của mục menu.
 *
 * Cấu hình menu chấp nhận cả hai dạng: emoji (bản thiết kế dùng emoji) hoặc tên component
 * icon của Ant Design. Chuỗi nào không khớp tên component thì hiển thị nguyên văn — nhờ vậy
 * quản trị viên đổi icon trên màn hình cấu hình mà không cần biết tên component.
 */
function LayIcon(ten?: string | null) {
  if (!ten) return null;

  const bang = Icons as unknown as Record<string, React.ComponentType>;
  const Icon = bang[ten];

  return Icon ? <Icon /> : <span>{ten}</span>;
}

/** Chữ viết tắt hiển thị trong avatar khi người dùng chưa có ảnh. */
function vietTat(hoTen?: string | null) {
  if (!hoTen) return '';

  const tu = hoTen.trim().split(/\s+/);
  if (tu.length === 1) return tu[0].slice(0, 2).toUpperCase();

  return `${tu[tu.length - 2][0]}${tu[tu.length - 1][0]}`.toUpperCase();
}

export function BoCucChinh() {
  const viTri = useLocation();
  const dieuHuong = useNavigate();
  const manHinh = useBreakpoint();
  const laDiDong = !manHinh.lg;

  const [moDrawer, setMoDrawer] = useState(false);

  const { nguoiDung, dangXuat } = useAuthStore();
  const { menu, tenHeThong, tenDonVi } = useCauHinhStore();

  const { data: thongBao } = useQuery({
    queryKey: ['thong-bao-chua-doc'],
    queryFn: () => layPhanTrang('/api/v1/he-thong/thong-bao', { chuaDoc: true, soDong: 1 }),
    refetchInterval: 60_000,
  });

  const duongDanHienTai = viTri.pathname;

  // Thiết kế dùng danh sách PHẲNG có tiêu đề nhóm thay vì submenu bung/gập:
  // toàn bộ chức năng nhìn thấy ngay, không phải bấm hai lần mới tới nơi.
  const dongMenu = useMemo(() => lamPhangMenu(menu), [menu]);

  const maDangChon = useMemo(() => timMucKhop(dongMenu, duongDanHienTai), [dongMenu, duongDanHienTai]);

  const duongDanBreadcrumb = useMemo(
    () => taoBreadcrumb(menu, duongDanHienTai),
    [menu, duongDanHienTai],
  );

  const thanhDieuHuong = (
    <div style={{ display: 'flex', flexDirection: 'column', height: '100%', overflow: 'auto' }}>
      <div
        style={{
          height: kichThuoc.caoHeader,
          flexShrink: 0,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          gap: 10,
        }}
      >
        <div
          style={{
            width: 28,
            height: 28,
            borderRadius: 7,
            background: mauSac.chinh,
            color: '#fff',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            fontWeight: 800,
            fontSize: 12,
          }}
        >
          BI
        </div>
        <span style={{ fontWeight: 600, fontSize: 15, letterSpacing: '0.04em' }}>BLUEIDEA</span>
      </div>

      <nav
        aria-label="Điều hướng chính"
        style={{ display: 'flex', flexDirection: 'column', gap: 2, padding: '4px 8px 16px' }}
      >
        {dongMenu.map((m) =>
          m.laTieuDeNhom ? (
            <div
              key={m.ma}
              style={{
                height: 26,
                display: 'flex',
                alignItems: 'center',
                padding: '0 12px',
                marginTop: 14,
                fontSize: 11.5,
                fontWeight: 700,
                letterSpacing: '0.08em',
                color: mauSac.chuMo,
                flexShrink: 0,
              }}
            >
              {m.ten.toUpperCase()}
            </div>
          ) : (
            <MucDieuHuong
              key={m.ma}
              muc={m}
              dangChon={m.ma === maDangChon}
              onChon={() => {
                if (m.duongDan) dieuHuong(m.duongDan);
                setMoDrawer(false);
              }}
            />
          ),
        )}
      </nav>
    </div>
  );

  return (
    <Layout className="bo-cuc-chinh" style={{ height: '100vh', overflow: 'hidden' }}>
      {!laDiDong && (
        <Sider
          width={kichThuoc.rongSider}
          theme="light"
          style={{ borderRight: `1px solid ${mauSac.vien}`, overflow: 'hidden' }}
        >
          {thanhDieuHuong}
        </Sider>
      )}

      {laDiDong && (
        <Drawer
          placement="left"
          open={moDrawer}
          onClose={() => setMoDrawer(false)}
          styles={{ body: { padding: 0 }, header: { display: 'none' } }}
          width={260}
        >
          {thanhDieuHuong}
        </Drawer>
      )}

      <Layout style={{ minWidth: 0 }}>
        <Header
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 12,
            borderBottom: `1px solid ${mauSac.vien}`,
            flexShrink: 0,
          }}
        >
          {laDiDong && (
            <Button
              type="text"
              icon={<MenuOutlined />}
              onClick={() => setMoDrawer(true)}
              aria-label="Mở menu"
            />
          )}

          {/* Tiêu đề nằm gọn một dòng, cắt bớt khi hẹp — tránh đẩy các nút bên phải ra ngoài. */}
          <div style={{ flex: 1, minWidth: 0, overflow: 'hidden' }}>
            <Typography.Text
              strong
              ellipsis={{ tooltip: `${tenHeThong}${tenDonVi ? ` — ${tenDonVi}` : ''}` }}
              style={{ fontSize: 16, display: 'block', whiteSpace: 'nowrap' }}
            >
              {tenHeThong}
              {tenDonVi && !laDiDong && (
                <span style={{ color: mauSac.chuMo, fontWeight: 400 }}> — {tenDonVi}</span>
              )}
            </Typography.Text>
          </div>

          <Badge count={thongBao?.tongSo ?? 0} size="small" overflowCount={99}>
            <Button type="text" icon={<BellOutlined />} aria-label="Thông báo" />
          </Badge>

          <Dropdown
            menu={{
              items: [
                {
                  key: 'thong-tin',
                  label: (
                    <div style={{ padding: '4px 0' }}>
                      <div style={{ fontWeight: 600 }}>{nguoiDung?.hoTen}</div>
                      <div style={{ fontSize: 12, color: mauSac.chuMo }}>{nguoiDung?.chucVu}</div>
                      <div style={{ fontSize: 12, color: mauSac.chuMo }}>{nguoiDung?.tenDonVi}</div>
                    </div>
                  ),
                  disabled: true,
                },
                { type: 'divider' },
                {
                  key: 'doi-mat-khau',
                  icon: <KeyOutlined />,
                  label: <Link to="/doi-mat-khau">Đổi mật khẩu</Link>,
                },
                {
                  key: 'dang-xuat',
                  icon: <LogoutOutlined />,
                  label: 'Đăng xuất',
                  danger: true,
                  onClick: async () => {
                    // Lấy địa chỉ kết thúc phiên bên nhà cung cấp TRƯỚC khi xoá token —
                    // endpoint đó yêu cầu đăng nhập (chức năng 41, single logout).
                    const diaChiSso = await layDiaChiDangXuatSso();
                    await dangXuat();

                    if (diaChiSso) {
                      window.location.href = diaChiSso;
                      return;
                    }

                    dieuHuong('/dang-nhap');
                  },
                },
              ],
            }}
          >
            <Button type="text" style={{ height: 'auto', padding: '4px 8px' }}>
              {nguoiDung?.hoTen ? (
                <Avatar size={26} style={{ background: mauSac.chinh, fontSize: 11, fontWeight: 600 }}>
                  {vietTat(nguoiDung.hoTen)}
                </Avatar>
              ) : (
                <Avatar size={26} icon={<UserOutlined />} />
              )}
              {!laDiDong && <span style={{ marginLeft: 8 }}>{nguoiDung?.hoTen}</span>}
            </Button>
          </Dropdown>
        </Header>

        <Content className="noi-dung-trang" style={{ overflow: 'auto', minHeight: 0 }}>
          <Breadcrumb
            style={{ marginBottom: kichThuoc.khoangKhoi }}
            items={
              duongDanBreadcrumb.length > 0
                ? [
                    { title: <Link to="/">Trang chủ</Link> },
                    ...duongDanBreadcrumb.map((b) => ({ title: b.ten })),
                  ]
                : [{ title: <Link to="/">Trang chủ</Link> }, { title: 'Tổng quan' }]
            }
          />
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  );
}

/** Một mục điều hướng bấm được, có trạng thái hover riêng vì AntD Menu không còn dùng ở đây. */
function MucDieuHuong({
  muc,
  dangChon,
  onChon,
}: {
  muc: DongMenu;
  dangChon: boolean;
  onChon: () => void;
}) {
  const [hover, setHover] = useState(false);

  const nen = dangChon ? mauSac.chinhNhat : hover ? 'rgba(0,0,0,0.04)' : 'transparent';

  return (
    <div
      role="link"
      tabIndex={0}
      aria-current={dangChon ? 'page' : undefined}
      onClick={onChon}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          onChon();
        }
      }}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}
      style={{
        height: 38,
        display: 'flex',
        alignItems: 'center',
        gap: 10,
        padding: '0 12px',
        borderRadius: 8,
        fontSize: 14,
        fontWeight: dangChon ? 600 : 400,
        background: nen,
        color: dangChon ? mauSac.chinh : mauSac.chuPhu,
        cursor: 'pointer',
        flexShrink: 0,
        transition: 'background 0.15s',
      }}
    >
      <span style={{ fontSize: 15, width: 16, textAlign: 'center', flexShrink: 0 }}>
        {LayIcon(muc.icon)}
      </span>
      <span style={{ flex: 1, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
        {muc.ten}
      </span>
    </div>
  );
}

/**
 * Trải cây menu thành danh sách phẳng.
 *
 * Mục cha CÓ đường dẫn vẫn giữ nguyên là mục bấm được; chỉ mục cha thuần gom nhóm
 * (không có đường dẫn) mới trở thành tiêu đề nhóm.
 */
function lamPhangMenu(danhSach: MucMenu[]): DongMenu[] {
  const ketQua: DongMenu[] = [];

  for (const m of danhSach) {
    const laNhom = m.menuCon.length > 0 && !m.duongDan;

    ketQua.push({
      ma: m.ma,
      ten: m.ten,
      icon: m.icon,
      duongDan: m.duongDan,
      laTieuDeNhom: laNhom,
    });

    if (m.menuCon.length > 0) {
      ketQua.push(...lamPhangMenu(m.menuCon));
    }
  }

  return ketQua;
}

/**
 * Kiểm tra đường dẫn hiện tại có thuộc một mục menu hay không.
 * Phải so theo đoạn đường dẫn — nếu dùng startsWith thuần thì menu "/" khớp mọi trang.
 */
function khopDuongDan(duongDanMenu: string, duongDanHienTai: string): boolean {
  if (duongDanMenu === '/') {
    return duongDanHienTai === '/';
  }

  return duongDanHienTai === duongDanMenu || duongDanHienTai.startsWith(`${duongDanMenu}/`);
}

/** Tìm mục menu khớp đường dẫn hiện tại (ưu tiên khớp dài nhất). */
function timMucKhop(dongMenu: DongMenu[], duongDan: string): string | undefined {
  let khop: DongMenu | undefined;

  for (const m of dongMenu) {
    if (m.laTieuDeNhom || !m.duongDan) continue;

    if (khopDuongDan(m.duongDan, duongDan)) {
      if (!khop || m.duongDan.length > khop.duongDan!.length) {
        khop = m;
      }
    }
  }

  return khop?.ma;
}

/** Dựng đường dẫn breadcrumb theo cây menu. */
function taoBreadcrumb(danhSach: MucMenu[], duongDan: string): MucMenu[] {
  for (const m of danhSach) {
    if (m.menuCon.length > 0) {
      const duoi = taoBreadcrumb(m.menuCon, duongDan);
      if (duoi.length > 0) {
        return [m, ...duoi];
      }
    }

    if (m.duongDan && khopDuongDan(m.duongDan, duongDan) && m.duongDan !== '/') {
      return [m];
    }
  }

  return [];
}
