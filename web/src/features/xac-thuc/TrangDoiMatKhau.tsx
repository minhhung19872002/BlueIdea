import { useNavigate } from 'react-router-dom';
import { App, Alert, Button, Card, Form, Input } from 'antd';

import { guiDuLieu, LoiApi } from '@/api/client';
import { useAuthStore } from '@/app/store/authStore';

interface FormDoiMatKhau {
  matKhauCu: string;
  matKhauMoi: string;
  xacNhan: string;
}

export default function TrangDoiMatKhau() {
  const { message } = App.useApp();
  const dieuHuong = useNavigate();
  const { buocDoiMatKhau, dangXuat } = useAuthStore();
  const [form] = Form.useForm<FormDoiMatKhau>();

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

      <Form<FormDoiMatKhau> form={form} layout="vertical" onFinish={xuLyGui} requiredMark={false}>
        <Form.Item
          name="matKhauCu"
          label="Mật khẩu hiện tại"
          rules={[{ required: true, message: 'Vui lòng nhập mật khẩu hiện tại' }]}
        >
          <Input.Password autoComplete="current-password" />
        </Form.Item>

        <Form.Item
          name="matKhauMoi"
          label="Mật khẩu mới"
          extra="Tối thiểu 8 ký tự, gồm chữ hoa, chữ thường, chữ số và ký tự đặc biệt."
          rules={[
            { required: true, message: 'Vui lòng nhập mật khẩu mới' },
            { min: 8, message: 'Mật khẩu phải có ít nhất 8 ký tự' },
            {
              pattern: /(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\w\s])/,
              message: 'Mật khẩu phải có chữ hoa, chữ thường, chữ số và ký tự đặc biệt',
            },
          ]}
        >
          <Input.Password autoComplete="new-password" />
        </Form.Item>

        <Form.Item
          name="xacNhan"
          label="Xác nhận mật khẩu mới"
          dependencies={['matKhauMoi']}
          rules={[
            { required: true, message: 'Vui lòng xác nhận mật khẩu mới' },
            ({ getFieldValue }) => ({
              validator(_, giaTri) {
                if (!giaTri || getFieldValue('matKhauMoi') === giaTri) {
                  return Promise.resolve();
                }
                return Promise.reject(new Error('Mật khẩu xác nhận không khớp'));
              },
            }),
          ]}
        >
          <Input.Password autoComplete="new-password" />
        </Form.Item>

        <Button type="primary" htmlType="submit" block>
          Đổi mật khẩu
        </Button>
      </Form>
    </Card>
  );
}
