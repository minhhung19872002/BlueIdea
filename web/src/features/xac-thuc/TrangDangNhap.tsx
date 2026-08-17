import { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { App, Button, Form, Input, Typography } from 'antd';
import { LockOutlined, UserOutlined } from '@ant-design/icons';

import { LoiApi } from '@/api/client';
import { useAuthStore } from '@/app/store/authStore';
import { useCauHinhStore } from '@/app/store/cauHinhStore';

interface FormDangNhap {
  tenDangNhap: string;
  matKhau: string;
}

export default function TrangDangNhap() {
  const dieuHuong = useNavigate();
  const [thamSo] = useSearchParams();
  const { message } = App.useApp();

  const { dangNhap, dangTai, nguoiDung } = useAuthStore();
  const { tenHeThong, tenDonVi, emailHoTro, dienThoaiHoTro, napCauHinhCongKhai } =
    useCauHinhStore();

  const [soLanSai, setSoLanSai] = useState(0);

  useEffect(() => {
    void napCauHinhCongKhai();
  }, [napCauHinhCongKhai]);

  useEffect(() => {
    if (nguoiDung) {
      dieuHuong(thamSo.get('tiepTuc') ?? '/', { replace: true });
    }
  }, [nguoiDung, dieuHuong, thamSo]);

  async function xuLyGui(giaTri: FormDangNhap) {
    try {
      await dangNhap(giaTri.tenDangNhap.trim(), giaTri.matKhau);
      message.success('Đăng nhập thành công');
      dieuHuong(thamSo.get('tiepTuc') ?? '/', { replace: true });
    } catch (loi) {
      setSoLanSai((n) => n + 1);
      message.error(loi instanceof LoiApi ? loi.message : 'Không đăng nhập được.');
    }
  }

  return (
    <div className="trang-dang-nhap">
      <div className="the-dang-nhap">
        <div style={{ textAlign: 'center', marginBottom: 24 }}>
          <Typography.Title level={4} style={{ marginBottom: 4 }}>
            {tenHeThong}
          </Typography.Title>
          {tenDonVi && <Typography.Text type="secondary">{tenDonVi}</Typography.Text>}
        </div>

        <Form<FormDangNhap> layout="vertical" onFinish={xuLyGui} requiredMark={false} size="large">
          <Form.Item
            name="tenDangNhap"
            label="Tên đăng nhập"
            rules={[{ required: true, message: 'Vui lòng nhập tên đăng nhập' }]}
          >
            <Input prefix={<UserOutlined />} autoComplete="username" autoFocus />
          </Form.Item>

          <Form.Item
            name="matKhau"
            label="Mật khẩu"
            rules={[{ required: true, message: 'Vui lòng nhập mật khẩu' }]}
          >
            <Input.Password prefix={<LockOutlined />} autoComplete="current-password" />
          </Form.Item>

          {soLanSai >= 3 && (
            <Typography.Paragraph type="warning" style={{ fontSize: 13 }}>
              Bạn đã nhập sai {soLanSai} lần. Tài khoản sẽ bị khóa tạm thời sau 5 lần sai.
            </Typography.Paragraph>
          )}

          <Button type="primary" htmlType="submit" block loading={dangTai}>
            Đăng nhập
          </Button>
        </Form>

        {(emailHoTro || dienThoaiHoTro) && (
          <div style={{ marginTop: 20, textAlign: 'center', fontSize: 12, color: '#888' }}>
            Hỗ trợ: {emailHoTro} {dienThoaiHoTro && `• ${dienThoaiHoTro}`}
          </div>
        )}
      </div>
    </div>
  );
}
