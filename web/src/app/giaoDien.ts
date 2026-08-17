import type { ThemeConfig } from 'antd';

/**
 * Token giao diện lấy từ bản thiết kế "BlueIdea v2".
 *
 * Gom vào một chỗ để mọi màn hình dùng chung — không rải giá trị màu/khoảng cách vào từng
 * component, vì như vậy sửa thiết kế lần sau sẽ phải dò khắp mã nguồn.
 */
export const mauSac = {
  nen: '#f0f2f5',
  vien: '#f0f0f0',
  vienDam: '#d9d9d9',
  chuChinh: 'rgba(0, 0, 0, 0.88)',
  chuPhu: 'rgba(0, 0, 0, 0.65)',
  chuMo: 'rgba(0, 0, 0, 0.45)',
  chuNhat: 'rgba(0, 0, 0, 0.35)',

  chinh: '#1677ff',
  chinhNhat: '#e6f4ff',
  thanhCong: '#52c41a',
  canhBao: '#faad14',
  loi: '#ff4d4f',
  tim: '#722ed1',
  cam: '#fa8c16',
} as const;

/** Bảng màu thẻ nhãn: [nền, chữ, viền] — dùng cho Tag tự vẽ và badge trạng thái. */
export const mauThe = {
  blue: ['#e6f4ff', '#1677ff', '#91caff'],
  success: ['#f6ffed', '#52c41a', '#b7eb8f'],
  orange: ['#fff7e6', '#fa8c16', '#ffd591'],
  error: ['#fff2f0', '#ff4d4f', '#ffccc7'],
  purple: ['#f9f0ff', '#722ed1', '#d3adf7'],
  def: ['#fafafa', 'rgba(0, 0, 0, 0.65)', '#d9d9d9'],
} as const;

export const kichThuoc = {
  /** Chiều rộng thanh điều hướng bên trái. */
  rongSider: 250,
  /** Chiều cao thanh tiêu đề — bằng chiều cao khối logo để hai bên thẳng hàng. */
  caoHeader: 56,
  /** Khoảng đệm vùng nội dung. */
  demNoiDung: 16,
  /** Khoảng cách giữa các khối trong một trang. */
  khoangKhoi: 12,
} as const;

/**
 * Cấu hình chủ đề Ant Design.
 *
 * Màu chủ đạo vẫn đọc từ cấu hình hệ thống (Mục 9 đặc tả) nên nhận vào tham số,
 * không cố định trong mã nguồn.
 */
export function taoChuDe(mauChuDao: string): ThemeConfig {
  return {
    token: {
      colorPrimary: mauChuDao,
      colorBgLayout: mauSac.nen,
      colorBorderSecondary: mauSac.vien,
      colorText: mauSac.chuChinh,
      colorTextSecondary: mauSac.chuPhu,
      colorTextTertiary: mauSac.chuMo,
      fontFamily:
        "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif",
      fontSize: 14,
      borderRadius: 6,
      borderRadiusLG: 8,
      wireframe: false,
    },
    components: {
      Layout: {
        headerBg: '#fff',
        headerHeight: kichThuoc.caoHeader,
        headerPadding: '0 16px',
        siderBg: '#fff',
        bodyBg: mauSac.nen,
      },

      // Thẻ phẳng: không viền, không đổ bóng — các khối tách nhau bằng khoảng trắng.
      Card: {
        borderRadiusLG: 8,
        headerHeight: 44,
        headerFontSize: 14,
        headerFontSizeSM: 14,
        paddingLG: 16,
        colorBorderSecondary: mauSac.vien,
      },

      Menu: {
        itemBg: 'transparent',
        subMenuItemBg: 'transparent',
        itemBorderRadius: 8,
        itemHeight: 38,
        itemMarginBlock: 2,
        itemMarginInline: 0,
        itemPaddingInline: 12,
        iconSize: 15,
        collapsedIconSize: 16,
        itemSelectedBg: mauSac.chinhNhat,
        itemColor: mauSac.chuPhu,
        activeBarWidth: 0,
        activeBarBorderWidth: 0,
      },

      Table: {
        headerBg: '#fafafa',
        headerColor: mauSac.chuChinh,
        headerSplitColor: 'transparent',
        borderColor: mauSac.vien,
        rowSelectedBg: mauSac.chinhNhat,
        rowSelectedHoverBg: '#bae0ff',
        cellPaddingBlock: 10,
        cellPaddingInline: 12,
      },

      Statistic: { contentFontSize: 24, titleFontSize: 14 },
      Tag: { defaultBg: '#fafafa', defaultColor: mauSac.chuPhu },
      Descriptions: { labelBg: '#fafafa' },
      Segmented: { itemSelectedBg: mauSac.chinhNhat, itemSelectedColor: mauSac.chinh },
      Tabs: { horizontalItemPadding: '10px 0', cardBg: '#fafafa' },
    },
  };
}
