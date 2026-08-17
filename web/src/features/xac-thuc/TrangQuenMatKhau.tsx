import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { App, Alert, Button, Form, Input, Steps } from 'antd';
import { LockOutlined, SafetyOutlined, UserOutlined } from '@ant-design/icons';

import { guiDuLieu, LoiApi } from '@/api/client';
import { useCauHinhStore } from '@/app/store/cauHinhStore';

interface FormYeuCau {
  dinhDanh: string;
}

interface FormDatLai {
  ma: string;
  matKhauMoi: string;
  xacNhan: string;
}

/**
 * Chức năng 21 — Quên mật khẩu, đặt lại bằng mã OTP gửi qua email.
 *
 * Màn hình không bao giờ nói tài khoản có tồn tại hay không: máy chủ trả cùng một thông báo
 * cho mọi trường hợp, và giao diện hiển thị đúng thông báo đó thay vì tự diễn giải thêm.
 */
export default function TrangQuenMatKhau() {
  const dieuHuong = useNavigate();
  const { message } = App.useApp();
  const { tenHeThong } = useCauHinhStore();

  const [buoc, setBuoc] = useState(0);
  const [tenDangNhap, setTenDangNhap] = useState('');
  const [dangGui, setDangGui] = useState(false);

  async function guiYeuCau(giaTri: FormYeuCau) {
    setDangGui(true);
    try {
      await guiDuLieu('/api/v1/xac-thuc/quen-mat-khau', { dinhDanh: giaTri.dinhDanh.trim() });

      // Người dùng có thể nhập email, nhưng bước 2 cần tên đăng nhập. Nếu họ nhập email,
      // ô tên đăng nhập ở bước sau để trống cho họ tự điền.
      setTenDangNhap(giaTri.dinhDanh.includes('@') ? '' : giaTri.dinhDanh.trim());
      setBuoc(1);
    } catch (loi) {
      message.error(loi instanceof LoiApi ? loi.message : 'Không gửi được yêu cầu.');
    } finally {
      setDangGui(false);
    }
  }

  async function datLai(giaTri: FormDatLai & { tenDangNhap: string }) {
    setDangGui(true);
    try {
      await guiDuLieu('/api/v1/xac-thuc/dat-lai-mat-khau', {
        tenDangNhap: giaTri.tenDangNhap.trim(),
        ma: giaTri.ma.trim(),
        matKhauMoi: giaTri.matKhauMoi,
      });

      message.success('Đã đặt lại mật khẩu. Vui lòng đăng nhập lại.');
      dieuHuong('/dang-nhap', { replace: true });
    } catch (loi) {
      message.error(loi instanceof LoiApi ? loi.message : 'Không đặt lại được mật khẩu.');
    } finally {
      setDangGui(false);
    }
  }

  return (
    <div className="trang-dang-nhap">
      <div className="the-dang-nhap">
        <div style={{ textAlign: 'center', marginBottom: 20 }}>
          <div style={{ fontSize: 18, fontWeight: 700 }}>Quên mật khẩu</div>
          <div style={{ fontSize: 13, color: 'rgba(0,0,0,0.45)', marginTop: 2 }}>{tenHeThong}</div>
        </div>

        <Steps
          size="small"
          current={buoc}
          style={{ marginBottom: 20 }}
          items={[{ title: 'Nhận mã' }, { title: 'Đặt mật khẩu' }]}
        />

        {buoc === 0 ? (
          <Form<FormYeuCau> layout="vertical" onFinish={guiYeuCau} requiredMark={false} size="large">
            <Form.Item
              name="dinhDanh"
              label="Tên đăng nhập hoặc email"
              rules={[{ required: true, message: 'Vui lòng nhập tên đăng nhập hoặc email' }]}
            >
              <Input prefix={<UserOutlined />} autoFocus autoComplete="username" />
            </Form.Item>

            <Button type="primary" htmlType="submit" block loading={dangGui}>
              Gửi mã đặt lại
            </Button>
          </Form>
        ) : (
          <>
            <Alert
              type="info"
              showIcon
              style={{ marginBottom: 16 }}
              message="Nếu thông tin khớp với một tài khoản, mã đã được gửi tới email đăng ký."
              description="Mã có hiệu lực 15 phút và chỉ dùng được một lần."
            />

            <Form<FormDatLai & { tenDangNhap: string }>
              layout="vertical"
              onFinish={datLai}
              requiredMark={false}
              size="large"
              initialValues={{ tenDangNhap }}
            >
              <Form.Item
                name="tenDangNhap"
                label="Tên đăng nhập"
                rules={[{ required: true, message: 'Vui lòng nhập tên đăng nhập' }]}
              >
                <Input prefix={<UserOutlined />} autoComplete="username" />
              </Form.Item>

              <Form.Item
                name="ma"
                label="Mã đặt lại (6 chữ số)"
                rules={[{ required: true, message: 'Vui lòng nhập mã trong email' }]}
              >
                <Input prefix={<SafetyOutlined />} autoComplete="one-time-code" autoFocus />
              </Form.Item>

              <Form.Item
                name="matKhauMoi"
                label="Mật khẩu mới"
                rules={[{ required: true, message: 'Vui lòng nhập mật khẩu mới' }]}
              >
                <Input.Password prefix={<LockOutlined />} autoComplete="new-password" />
              </Form.Item>

              <Form.Item
                name="xacNhan"
                label="Nhập lại mật khẩu mới"
                dependencies={['matKhauMoi']}
                rules={[
                  { required: true, message: 'Vui lòng nhập lại mật khẩu' },
                  ({ getFieldValue }) => ({
                    validator: (_, giaTri) =>
                      !giaTri || getFieldValue('matKhauMoi') === giaTri
                        ? Promise.resolve()
                        : Promise.reject(new Error('Hai mật khẩu không khớp')),
                  }),
                ]}
              >
                <Input.Password prefix={<LockOutlined />} autoComplete="new-password" />
              </Form.Item>

              <Button type="primary" htmlType="submit" block loading={dangGui}>
                Đặt lại mật khẩu
              </Button>

              <Button type="link" block onClick={() => setBuoc(0)} style={{ marginTop: 8 }}>
                Gửi lại mã khác
              </Button>
            </Form>
          </>
        )}

        <div style={{ textAlign: 'center', marginTop: 14 }}>
          <Link to="/dang-nhap" style={{ fontSize: 13 }}>
            Quay lại đăng nhập
          </Link>
        </div>
      </div>
    </div>
  );
}
