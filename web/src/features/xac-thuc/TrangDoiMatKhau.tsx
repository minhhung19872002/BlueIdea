import { useNavigate } from 'react-router-dom';
import { App, Alert, Button, Card, Input } from 'antd';
import { z } from 'zod';

import { guiDuLieu, LoiApi } from '@/api/client';
import { useAuthStore } from '@/app/store/authStore';
import { BieuMau, Truong, useBieuMau } from '@/components/bieu-mau/BieuMau';

/**
 * Luật đổi mật khẩu.
 *
 * Hai luật cuối chỉ diễn đạt được khi nhìn CẢ FORM cùng lúc, nên đặt ở `superRefine`:
 *   - mật khẩu xác nhận phải khớp mật khẩu mới;
 *   - mật khẩu mới phải khác mật khẩu hiện tại — đặt lại đúng mật khẩu cũ thì thao tác này
 *     không đổi được gì, mà người dùng vẫn tưởng mình đã đổi.
 *
 * Ràng buộc độ mạnh phải khớp chính sách của máy chủ; lệch thì người dùng nhập xong mới bị máy
 * chủ từ chối, và họ không hiểu vì sao giao diện đã báo hợp lệ.
 */
const luatDoiMatKhau = z
  .object({
    matKhauCu: z.string().min(1, 'Vui lòng nhập mật khẩu hiện tại.'),
    matKhauMoi: z
      .string()
      .min(8, 'Mật khẩu phải có ít nhất 8 ký tự.')
      .regex(
        /(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\w\s])/,
        'Mật khẩu phải có chữ hoa, chữ thường, chữ số và ký tự đặc biệt.',
      ),
    xacNhan: z.string().min(1, 'Vui lòng xác nhận mật khẩu mới.'),
  })
  .superRefine((v, ctx) => {
    if (v.matKhauMoi !== v.xacNhan) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['xacNhan'],
        message: 'Mật khẩu xác nhận không khớp.',
      });
    }

    if (v.matKhauCu && v.matKhauMoi && v.matKhauCu === v.matKhauMoi) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['matKhauMoi'],
        message: 'Mật khẩu mới phải khác mật khẩu hiện tại.',
      });
    }
  });

type FormDoiMatKhau = z.infer<typeof luatDoiMatKhau>;

export default function TrangDoiMatKhau() {
  const { message } = App.useApp();
  const dieuHuong = useNavigate();
  const { buocDoiMatKhau, dangXuat } = useAuthStore();

  const form = useBieuMau(luatDoiMatKhau, { matKhauCu: '', matKhauMoi: '', xacNhan: '' });

  async function xuLyGui(giaTri: FormDoiMatKhau) {
    try {
      await guiDuLieu('/api/v1/xac-thuc/doi-mat-khau', {
        matKhauCu: giaTri.matKhauCu,
        matKhauMoi: giaTri.matKhauMoi,
      });

      message.success('Đổi mật khẩu thành công. Vui lòng đăng nhập lại.');
      await dangXuat();
      dieuHuong('/dang-nhap', { replace: true });
    } catch (loi) {
      message.error(loi instanceof LoiApi ? loi.message : 'Không đổi được mật khẩu.');
    }
  }

  return (
    <Card title="Đổi mật khẩu" style={{ maxWidth: 520, margin: '0 auto' }}>
      {buocDoiMatKhau && (
        <Alert
          type="warning"
          showIcon
          style={{ marginBottom: 16 }}
          message="Bắt buộc đổi mật khẩu"
          description="Đây là lần đăng nhập đầu tiên. Bạn cần đổi mật khẩu trước khi sử dụng hệ thống."
        />
      )}

      <BieuMau form={form} onGui={xuLyGui}>
        <Truong<FormDoiMatKhau> ten="matKhauCu" label="Mật khẩu hiện tại" required>
          {(o) => (
            <Input.Password {...o} value={o.value as string} autoComplete="current-password" />
          )}
        </Truong>

        <Truong<FormDoiMatKhau>
          ten="matKhauMoi"
          label="Mật khẩu mới"
          required
          extra="Tối thiểu 8 ký tự, gồm chữ hoa, chữ thường, chữ số và ký tự đặc biệt."
        >
          {(o) => <Input.Password {...o} value={o.value as string} autoComplete="new-password" />}
        </Truong>

        <Truong<FormDoiMatKhau> ten="xacNhan" label="Xác nhận mật khẩu mới" required>
          {(o) => <Input.Password {...o} value={o.value as string} autoComplete="new-password" />}
        </Truong>

        <Button
          type="primary"
          htmlType="submit"
          block
          loading={form.formState.isSubmitting}
        >
          Đổi mật khẩu
        </Button>
      </BieuMau>
    </Card>
  );
}
