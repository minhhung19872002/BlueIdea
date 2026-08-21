import { App, Alert, Button, Card, Descriptions, Input, Select } from 'antd';
import dayjs from 'dayjs';
import { z } from 'zod';

import { capNhatDuLieu, LoiApi } from '@/api/client';
import { useAuthStore } from '@/app/store/authStore';
import { BieuMau, Truong, useBieuMau } from '@/components/bieu-mau/BieuMau';
import { DatePicker } from 'antd';

/**
 * Chức năng 21, 43 — Người dùng tự cập nhật thông tin cá nhân.
 *
 * Chỉ những trường chính chủ biết rõ nhất mới sửa được ở đây. Đơn vị, vai trò và trạng thái tài
 * khoản là quyết định của tổ chức nên chỉ hiển thị để đối chiếu, muốn đổi phải qua quản trị viên —
 * cho tự đổi là mở đường leo thang đặc quyền (tự chuyển sang đơn vị khác để xem hồ sơ đơn vị đó).
 */
const luatHoSo = z.object({
  hoTen: z.string().min(1, 'Vui lòng nhập họ và tên.').max(200, 'Họ và tên tối đa 200 ký tự.'),
  email: z
    .string()
    .email('Email không hợp lệ.')
    .max(200, 'Email tối đa 200 ký tự.')
    .optional()
    .or(z.literal('')),
  dienThoai: z
    .string()
    .regex(/^[0-9+()\s.-]{6,20}$/, 'Số điện thoại không hợp lệ.')
    .optional()
    .or(z.literal('')),
  chucVu: z.string().max(200, 'Chức vụ tối đa 200 ký tự.').optional().or(z.literal('')),
  gioiTinh: z.enum(['NAM', 'NU', 'KHAC']).optional().or(z.literal('')),
  ngaySinh: z.string().optional().or(z.literal('')),
});

type FormHoSo = z.infer<typeof luatHoSo>;

export default function TrangHoSoCaNhan() {
  const { message } = App.useApp();
  const nguoiDung = useAuthStore((st) => st.nguoiDung);
  const napLaiThongTin = useAuthStore((st) => st.napLaiThongTin);

  const form = useBieuMau(luatHoSo, {
    hoTen: nguoiDung?.hoTen ?? '',
    email: nguoiDung?.email ?? '',
    dienThoai: nguoiDung?.dienThoai ?? '',
    chucVu: nguoiDung?.chucVu ?? '',
    gioiTinh: (nguoiDung?.gioiTinh as FormHoSo['gioiTinh']) ?? '',
    ngaySinh: nguoiDung?.ngaySinh ?? '',
  });

  async function xuLyGui(giaTri: FormHoSo) {
    try {
      await capNhatDuLieu('/api/v1/xac-thuc/toi', {
        hoTen: giaTri.hoTen,
        email: giaTri.email || null,
        dienThoai: giaTri.dienThoai || null,
        chucVu: giaTri.chucVu || null,
        gioiTinh: giaTri.gioiTinh || null,
        ngaySinh: giaTri.ngaySinh || null,
        anhDaiDienId: nguoiDung?.anhDaiDienId ?? null,
      });

      // Nạp lại từ máy chủ thay vì tự sửa store: tên hiển thị trên thanh trên, chữ ký văn bản và
      // thông tin tác giả điền sẵn khi nộp hồ sơ đều đọc từ đây, lệch một nhịp là sai cả ba chỗ.
      await napLaiThongTin();
      message.success('Đã cập nhật thông tin cá nhân.');
    } catch (loi) {
      message.error(loi instanceof LoiApi ? loi.message : 'Không cập nhật được thông tin.');
    }
  }

  return (
    <Card title="Thông tin cá nhân" style={{ maxWidth: 720, margin: '0 auto' }}>
      <Descriptions size="small" column={1} bordered style={{ marginBottom: 16 }}>
        <Descriptions.Item label="Tên đăng nhập">{nguoiDung?.tenDangNhap}</Descriptions.Item>
        <Descriptions.Item label="Đơn vị">{nguoiDung?.tenDonVi ?? '—'}</Descriptions.Item>
        <Descriptions.Item label="Vai trò">
          {(nguoiDung?.vaiTro ?? []).join(', ') || '—'}
        </Descriptions.Item>
      </Descriptions>

      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 16 }}
        message="Đơn vị và vai trò do quản trị viên gán"
        description="Nếu thông tin đơn vị hoặc vai trò chưa đúng, vui lòng liên hệ quản trị viên đơn vị."
      />

      <BieuMau form={form} onGui={xuLyGui}>
        <Truong<FormHoSo> ten="hoTen" label="Họ và tên" required>
          {(o) => <Input {...o} value={o.value as string} autoComplete="name" />}
        </Truong>

        <Truong<FormHoSo> ten="email" label="Email">
          {(o) => <Input {...o} value={o.value as string} autoComplete="email" />}
        </Truong>

        <Truong<FormHoSo> ten="dienThoai" label="Điện thoại">
          {(o) => <Input {...o} value={o.value as string} autoComplete="tel" />}
        </Truong>

        <Truong<FormHoSo> ten="chucVu" label="Chức vụ">
          {(o) => <Input {...o} value={o.value as string} />}
        </Truong>

        <Truong<FormHoSo> ten="gioiTinh" label="Giới tính">
          {(o) => (
            <Select
              value={(o.value as string) || undefined}
              onChange={o.onChange}
              allowClear
              placeholder="Chọn giới tính"
              options={[
                { value: 'NAM', label: 'Nam' },
                { value: 'NU', label: 'Nữ' },
                { value: 'KHAC', label: 'Khác' },
              ]}
            />
          )}
        </Truong>

        <Truong<FormHoSo> ten="ngaySinh" label="Ngày sinh">
          {(o) => (
            <DatePicker
              format="DD/MM/YYYY"
              style={{ width: '100%' }}
              value={o.value ? dayjs(o.value as string) : null}
              onChange={(ngay) => o.onChange(ngay ? ngay.format('YYYY-MM-DD') : '')}
            />
          )}
        </Truong>

        <Button type="primary" htmlType="submit" loading={form.formState.isSubmitting}>
          Lưu thay đổi
        </Button>
      </BieuMau>
    </Card>
  );
}
