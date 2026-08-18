import { useCallback, useEffect, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { App, Alert, Button, Divider, Form, Input, Typography } from 'antd';
import {
  LockOutlined,
  LoginOutlined,
  ReloadOutlined,
  SafetyOutlined,
  UserOutlined,
} from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';

import { layDuLieu, LoiApi } from '@/api/client';
import { apiSso } from '@/api/endpoints';
import { useAuthStore } from '@/app/store/authStore';
import { useCauHinhStore } from '@/app/store/cauHinhStore';
import { batDauDangNhapSso } from '@/features/xac-thuc/sso';

interface FormDangNhap {
  tenDangNhap: string;
  matKhau: string;
  maMfa?: string;
  captchaLoiGiai?: string;
}

interface ThuThachCaptcha {
  id: string;
  anhSvg: string;
}

export default function TrangDangNhap() {
  const dieuHuong = useNavigate();
  const [thamSo] = useSearchParams();
  const { message } = App.useApp();

  const { dangNhap, dangTai, nguoiDung } = useAuthStore();
  const { tenHeThong, tenDonVi, emailHoTro, dienThoaiHoTro, napCauHinhCongKhai } =
    useCauHinhStore();

  const [soLanSai, setSoLanSai] = useState(0);
  const [canMfa, setCanMfa] = useState(false);
  const [captcha, setCaptcha] = useState<ThuThachCaptcha | null>(null);
  const [dangChuyenSso, setDangChuyenSso] = useState(false);

  // Chỉ hiện nút SSO khi máy chủ báo đã cấu hình nhà cung cấp — tránh dẫn người dùng vào
  // một luồng chắc chắn lỗi.
  const { data: trangThaiSso } = useQuery({
    queryKey: ['sso-trang-thai'],
    queryFn: apiSso.trangThai,
    retry: false,
    staleTime: 5 * 60 * 1000,
  });

  useEffect(() => {
    void napCauHinhCongKhai();
  }, [napCauHinhCongKhai]);

  useEffect(() => {
    if (nguoiDung) {
      dieuHuong(thamSo.get('tiepTuc') ?? '/', { replace: true });
    }
  }, [nguoiDung, dieuHuong, thamSo]);

  const napCaptcha = useCallback(async () => {
    try {
      setCaptcha(await layDuLieu<ThuThachCaptcha>('/api/v1/xac-thuc/captcha'));
    } catch {
      // Không chặn đăng nhập nếu không lấy được ảnh: máy chủ vẫn là bên quyết định
      // có bắt buộc CAPTCHA hay không, người dùng bấm "Đổi ảnh" để thử lại.
      setCaptcha(null);
    }
  }, []);

  async function xuLyGui(giaTri: FormDangNhap) {
    try {
      await dangNhap(giaTri.tenDangNhap.trim(), giaTri.matKhau, {
        maMfa: giaTri.maMfa?.trim() || undefined,
        captchaId: captcha?.id,
        captchaLoiGiai: giaTri.captchaLoiGiai?.trim() || undefined,
      });

      message.success('Đăng nhập thành công');
      dieuHuong(thamSo.get('tiepTuc') ?? '/', { replace: true });
    } catch (loi) {
      const maLoi = loi instanceof LoiApi ? loi.maLoi : undefined;

      if (maLoi === 'CAN_XAC_THUC_MFA') {
        // Không tính là nhập sai: mật khẩu đã đúng, người dùng chỉ chưa nhập mã.
        setCanMfa(true);
        message.info('Vui lòng nhập mã từ ứng dụng xác thực.');
        return;
      }

      if (maLoi === 'MA_XAC_THUC_KHONG_DUNG') {
        setCanMfa(true);
      }

      if (maLoi === 'CAN_NHAP_CAPTCHA' || maLoi === 'CAPTCHA_KHONG_DUNG') {
        // Mỗi thử thách chỉ dùng được một lần, kể cả khi đoán sai — phải lấy ảnh mới.
        void napCaptcha();
      }

      setSoLanSai((n) => n + 1);
      message.error(loi instanceof LoiApi ? loi.message : 'Không đăng nhập được.');
    }
  }

  return (
    <div className="trang-dang-nhap">
      <div className="the-dang-nhap">
        <div style={{ textAlign: 'center', marginBottom: 24 }}>
          <div
            style={{
              width: 48,
              height: 48,
              borderRadius: 10,
              background: '#1677ff',
              color: '#fff',
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontWeight: 800,
              fontSize: 16,
            }}
          >
            BI
          </div>
          <div style={{ fontSize: 18, fontWeight: 700, marginTop: 12, lineHeight: 1.4 }}>
            {tenHeThong}
          </div>
          {tenDonVi && (
            <div style={{ fontSize: 13, color: 'rgba(0,0,0,0.45)', marginTop: 2 }}>{tenDonVi}</div>
          )}
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

          {canMfa && (
            <>
              <Alert
                type="info"
                showIcon
                style={{ marginBottom: 12 }}
                message="Tài khoản đang bật xác thực hai lớp"
                description="Nhập mã 6 chữ số từ ứng dụng xác thực, hoặc một mã khôi phục."
              />
              <Form.Item
                name="maMfa"
                label="Mã xác thực"
                rules={[{ required: true, message: 'Vui lòng nhập mã xác thực' }]}
              >
                <Input
                  prefix={<SafetyOutlined />}
                  placeholder="123456 hoặc mã khôi phục"
                  autoComplete="one-time-code"
                  autoFocus
                />
              </Form.Item>
            </>
          )}

          {captcha && (
            <Form.Item
              name="captchaLoiGiai"
              label="Mã xác nhận trong ảnh"
              rules={[{ required: true, message: 'Vui lòng nhập mã trong ảnh' }]}
            >
              <div style={{ display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
                {/* Ảnh do máy chủ tự sinh, nhúng thẳng chứ không tải từ dịch vụ ngoài. */}
                <span
                  aria-hidden
                  style={{ lineHeight: 0, flexShrink: 0 }}
                  dangerouslySetInnerHTML={{ __html: captcha.anhSvg }}
                />
                <Button
                  icon={<ReloadOutlined />}
                  onClick={() => void napCaptcha()}
                  title="Đổi ảnh khác"
                />
                <Input
                  placeholder="Nhập mã trong ảnh"
                  style={{ flex: '1 1 140px', minWidth: 0 }}
                  autoComplete="off"
                />
              </div>
            </Form.Item>
          )}

          {soLanSai >= 3 && !canMfa && (
            <Typography.Paragraph type="warning" style={{ fontSize: 13 }}>
              Bạn đã nhập sai {soLanSai} lần. Tài khoản sẽ bị khóa tạm thời sau 5 lần sai.
            </Typography.Paragraph>
          )}

          <Button type="primary" htmlType="submit" block loading={dangTai}>
            Đăng nhập
          </Button>
        </Form>

        {trangThaiSso?.daCauHinh && (
          <>
            <Divider plain style={{ fontSize: 12, color: 'rgba(0,0,0,0.45)' }}>
              hoặc
            </Divider>
            <Button
              block
              size="large"
              icon={<LoginOutlined />}
              loading={dangChuyenSso}
              onClick={async () => {
                setDangChuyenSso(true);
                try {
                  await batDauDangNhapSso(thamSo.get('tiepTuc') ?? '/');
                } catch (loi) {
                  setDangChuyenSso(false);
                  message.error(
                    loi instanceof LoiApi ? loi.message : 'Không mở được đăng nhập một lần.',
                  );
                }
              }}
            >
              Đăng nhập một lần (SSO)
            </Button>
          </>
        )}

        <div style={{ textAlign: 'center', marginTop: 14 }}>
          <Link to="/quen-mat-khau" style={{ fontSize: 13 }}>
            Quên mật khẩu?
          </Link>
        </div>

        <div
          style={{
            marginTop: 20,
            paddingTop: 14,
            borderTop: '1px solid #f0f0f0',
            fontSize: 12,
            color: 'rgba(0,0,0,0.45)',
            textAlign: 'center',
            lineHeight: 1.6,
          }}
        >
          Khóa tài khoản sau 5 lần sai · Nhật ký truy cập theo quy định ATTT cấp độ 2
          {(emailHoTro || dienThoaiHoTro) && (
            <div style={{ marginTop: 4 }}>
              Hỗ trợ: {emailHoTro} {dienThoaiHoTro && `• ${dienThoaiHoTro}`}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
