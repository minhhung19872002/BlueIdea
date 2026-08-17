import { useMemo, useState } from 'react';
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';
import {
  Avatar,
  Badge,
  Breadcrumb,
  Button,
  Drawer,
  Dropdown,
  Grid,
  Layout,
  Menu,
  Typography,
} from 'antd';
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
import { layPhanTrang } from '@/api/client';

const { Header, Sider, Content } = Layout;
const { useBreakpoint } = Grid;

/** Lấy component icon của AntD theo tên chuỗi lưu trong cấu hình menu. */
function LayIcon(ten?: string | null) {
  if (!ten) return undefined;
  const bang = Icons as unknown as Record<string, React.ComponentType>;
  const Icon = bang[ten];
  return Icon ? <Icon /> : undefined;
}

export function BoCucChinh() {
  const viTri = useLocation();
  const dieuHuong = useNavigate();
  const manHinh = useBreakpoint();
  const laDiDong = !manHinh.lg;

  const [thuGon, setThuGon] = useState(false);
  const [moDrawer, setMoDrawer] = useState(false);

  const { nguoiDung, dangXuat } = useAuthStore();
  const { menu, tenHeThong, tenDonVi } = useCauHinhStore();

  const { data: thongBao } = useQuery({
    queryKey: ['thong-bao-chua-doc'],
    queryFn: () => layPhanTrang('/api/v1/he-thong/thong-bao', { chuaDoc: true, soDong: 1 }),
    refetchInterval: 60_000,
  });

  const mucMenu = useMemo(() => chuyenDoiMenu(menu), [menu]);

  const duongDanHienTai = viTri.pathname;
  const khoaChon = useMemo(() => timKhoaKhop(menu, duongDanHienTai), [menu, duongDanHienTai]);

  const duongDanBreadcrumb = useMemo(
    () => taoBreadcrumb(menu, duongDanHienTai),
    [menu, duongDanHienTai],
  );

  const noiDungMenu = (
    <>
      <div
        style={{
          height: 56,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          padding: '0 12px',
        }}
      >
        <Typography.Text strong style={{ color: '#fff', fontSize: thuGon ? 14 : 15 }}>
          {thuGon ? 'SK' : 'SÁNG KIẾN'}
        </Typography.Text>
      </div>
      <Menu
        theme="dark"
        mode="inline"
        selectedKeys={khoaChon ? [khoaChon] : []}
        defaultOpenKeys={duongDanBreadcrumb.map((b) => b.ma)}
        items={mucMenu}
        onClick={({ key }) => {
          dieuHuong(key);
          setMoDrawer(false);
        }}
      />
    </>
  );

  return (
    <Layout className="bo-cuc-chinh">
      {!laDiDong && (
        <Sider collapsible collapsed={thuGon} onCollapse={setThuGon} width={250} theme="dark">
          {noiDungMenu}
        </Sider>
      )}

      {laDiDong && (
        <Drawer
          placement="left"
          open={moDrawer}
          onClose={() => setMoDrawer(false)}
          styles={{ body: { padding: 0, background: '#001529' }, header: { display: 'none' } }}
          width={260}
        >
          {noiDungMenu}
        </Drawer>
      )}

      <Layout>
        <Header
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 12,
            padding: '0 16px',
            borderBottom: '1px solid var(--mau-vien)',
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

          {/* Tiêu đề phải nằm gọn trên một dòng, cắt bớt khi hẹp — tránh đè lên breadcrumb. */}
          <div style={{ flex: 1, minWidth: 0, overflow: 'hidden' }}>
            <Typography.Text
              strong
              ellipsis={{ tooltip: `${tenHeThong}${tenDonVi ? ` — ${tenDonVi}` : ''}` }}
              style={{ fontSize: 16, display: 'block', whiteSpace: 'nowrap' }}
            >
              {tenHeThong}
              {tenDonVi && !laDiDong && (
                <span style={{ color: 'rgba(0,0,0,0.45)', fontWeight: 400 }}> — {tenDonVi}</span>
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
                      <div style={{ fontSize: 12, color: '#888' }}>{nguoiDung?.chucVu}</div>
                      <div style={{ fontSize: 12, color: '#888' }}>{nguoiDung?.tenDonVi}</div>
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
                    await dangXuat();
                    dieuHuong('/dang-nhap');
                  },
                },
              ],
            }}
          >
            <Button type="text" style={{ height: 'auto', padding: '4px 8px' }}>
              <Avatar size="small" icon={<UserOutlined />} />
              {!laDiDong && <span style={{ marginLeft: 8 }}>{nguoiDung?.hoTen}</span>}
            </Button>
          </Dropdown>
        </Header>

        <Content className="noi-dung-trang">
          {duongDanBreadcrumb.length > 0 && (
            <Breadcrumb
              style={{ marginBottom: 12 }}
              items={[
                { title: <Link to="/">Trang chủ</Link> },
                ...duongDanBreadcrumb.map((b) => ({ title: b.ten })),
              ]}
            />
          )}
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  );
}

function chuyenDoiMenu(danhSach: MucMenu[]): NonNullable<Parameters<typeof Menu>[0]['items']> {
  return danhSach.map((m) => {
    if (m.menuCon.length > 0) {
      return {
        key: m.duongDan ?? m.ma,
        icon: LayIcon(m.icon),
        label: m.ten,
        children: chuyenDoiMenu(m.menuCon),
      };
    }

    return { key: m.duongDan ?? m.ma, icon: LayIcon(m.icon), label: m.ten };
  });
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
function timKhoaKhop(danhSach: MucMenu[], duongDan: string): string | undefined {
  let khop: string | undefined;

  const duyet = (cac: MucMenu[]) => {
    for (const m of cac) {
      if (m.duongDan && khopDuongDan(m.duongDan, duongDan)) {
        if (!khop || m.duongDan.length > khop.length) {
          khop = m.duongDan;
        }
      }
      duyet(m.menuCon);
    }
  };

  duyet(danhSach);
  return khop;
}

function taoBreadcrumb(danhSach: MucMenu[], duongDan: string): MucMenu[] {
  // Ở trang chủ không cần breadcrumb (header đã có sẵn liên kết "Trang chủ").
  if (duongDan === '/') {
    return [];
  }

  const ketQua: MucMenu[] = [];

  const duyet = (cac: MucMenu[], toTien: MucMenu[]): boolean => {
    for (const m of cac) {
      const duongDi = [...toTien, m];

      if (m.duongDan && khopDuongDan(m.duongDan, duongDan)) {
        ketQua.push(...duongDi);
        return true;
      }

      if (m.menuCon.length > 0 && duyet(m.menuCon, duongDi)) {
        return true;
      }
    }
    return false;
  };

  duyet(danhSach, []);
  return ketQua;
}
