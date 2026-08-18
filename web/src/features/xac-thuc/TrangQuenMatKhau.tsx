import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { App, Alert, Button, Input, Steps } from 'antd';
import { LockOutlined, SafetyOutlined, UserOutlined } from '@ant-design/icons';
import { z } from 'zod';

import { guiDuLieu, LoiApi } from '@/api/client';
import { useCauHinhStore } from '@/app/store/cauHinhStore';
import { BieuMau, Truong, useBieuMau } from '@/components/bieu-mau/BieuMau';
import { batBuoc } from '@/components/bieu-mau/luat';

const luatYeuCau = z.object({
  dinhDanh: batBuoc('Tên đăng nhập hoặc email', 200),
});

type FormYeuCau = z.infer<typeof luatYeuCau>;

/**
 * Luật đặt lại mật khẩu.
 *
 * Mã đặt lại là 6 chữ số: bắt định dạng ngay tại đây để người dán nhầm cả dòng chữ trong email
 * biết ngay, thay vì gửi lên rồi nhận về "mã không đúng" và tưởng mã đã hết hạn.
 */
const luatDatLai = z
  .object({
    tenDangNhap: batBuoc('Tên đăng nhập', 100),
    ma: z
      .string()
      .trim()
      .regex(/^\d{6}$/, 'Mã đặt lại gồm đúng 6 chữ số.'),
    matKhauMoi: z.string().min(8, 'Mật khẩu mới phải có ít nhất 8 ký tự.'),
    xacNhan: z.string().min(1, 'Vui lòng nhập lại mật khẩu.'),
  })
  .superRefine((giaTri, ctx) => {
    if (giaTri.matKhauMoi !== giaTri.xacNhan) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['xacNhan'],
        message: 'Hai mật khẩu không khớp.',
      });
    }
  });

type FormDatLai = z.infer<typeof luatDatLai>;

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
  const [dangGui, setDangGui] = useState(false);

  const formYeuCau = useBieuMau(luatYeuCau, { dinhDanh: '' });
  const formDatLai = useBieuMau<FormDatLai>(luatDatLai, {
    tenDangNhap: '',
    ma: '',
    matKhauMoi: '',
    xacNhan: '',
  });

  async function guiYeuCau(giaTri: FormYeuCau) {
    setDangGui(true);
    try {
      await guiDuLieu('/api/v1/xac-thuc/quen-mat-khau', { dinhDanh: giaTri.dinhDanh.trim() });

      // Người dùng có thể nhập email, nhưng bước 2 cần tên đăng nhập. Nếu họ nhập email,
      // ô tên đăng nhập ở bước sau để trống cho họ tự điền.
      const tenSuyRa = giaTri.dinhDanh.includes('@') ? '' : giaTri.dinhDanh.trim();
      formDatLai.reset({ tenDangNhap: tenSuyRa, ma: '', matKhauMoi: '', xacNhan: '' });
      setBuoc(1);
    } catch (loi) {
      message.error(loi instanceof LoiApi ? loi.message : 'Không gửi được yêu cầu.');
    } finally {
      setDangGui(false);
    }
  }

  async function datLai(giaTri: FormDatLai) {
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
          <BieuMau form={formYeuCau} onGui={guiYeuCau}>
            <Truong<FormYeuCau> ten="dinhDanh" label="Tên đăng nhập hoặc email" required>
              {(o) => (
                <Input
                  {...o}
                  value={o.value as string}
                  prefix={<UserOutlined />}
                  autoFocus
                  autoComplete="username"
                  size="large"
                />
              )}
            </Truong>

            <Button type="primary" htmlType="submit" block size="large" loading={dangGui}>
              Gửi mã đặt lại
            </Button>
          </BieuMau>
        ) : (
          <>
            <Alert
              type="info"
              showIcon
              style={{ marginBottom: 16 }}
              message="Nếu thông tin khớp với một tài khoản, mã đã được gửi tới email đăng ký."
              description="Mã có hiệu lực 15 phút và chỉ dùng được một lần."
            />

            <BieuMau form={formDatLai} onGui={datLai}>
              <Truong<FormDatLai> ten="tenDangNhap" label="Tên đăng nhập" required>
                {(o) => (
                  <Input
                    {...o}
                    value={o.value as string}
                    prefix={<UserOutlined />}
                    autoComplete="username"
                    size="large"
                  />
                )}
              </Truong>

              <Truong<FormDatLai> ten="ma" label="Mã đặt lại (6 chữ số)" required>
                {(o) => (
                  <Input
                    {...o}
                    value={o.value as string}
                    prefix={<SafetyOutlined />}
                    autoComplete="one-time-code"
                    autoFocus
                    size="large"
                  />
                )}
              </Truong>

              <Truong<FormDatLai> ten="matKhauMoi" label="Mật khẩu mới" required>
                {(o) => (
                  <Input.Password
                    {...o}
                    value={o.value as string}
                    prefix={<LockOutlined />}
                    autoComplete="new-password"
                    size="large"
                  />
                )}
              </Truong>

              <Truong<FormDatLai> ten="xacNhan" label="Nhập lại mật khẩu mới" required>
                {(o) => (
                  <Input.Password
                    {...o}
                    value={o.value as string}
                    prefix={<LockOutlined />}
                    autoComplete="new-password"
                    size="large"
                  />
                )}
              </Truong>

              <Button type="primary" htmlType="submit" block size="large" loading={dangGui}>
                Đặt lại mật khẩu
              </Button>

              <Button type="link" block onClick={() => setBuoc(0)} style={{ marginTop: 8 }}>
                Gửi lại mã khác
              </Button>
            </BieuMau>
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
