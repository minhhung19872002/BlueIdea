import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { App, Alert, Button, Divider, Input, Typography } from 'antd';
import {
  LockOutlined,
  LoginOutlined,
  ReloadOutlined,
  SafetyOutlined,
  UserOutlined,
} from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import { z } from 'zod';

import { layDuLieu, LoiApi } from '@/api/client';
import { apiSso } from '@/api/endpoints';
import { useAuthStore } from '@/app/store/authStore';
import { useCauHinhStore } from '@/app/store/cauHinhStore';
import { batDauDangNhapSso } from '@/features/xac-thuc/sso';
import { LogoHeThong } from '@/components/LogoHeThong';
import { BieuMau, Truong, useBieuMau } from '@/components/bieu-mau/BieuMau';
import { batBuoc, tuyChon } from '@/components/bieu-mau/luat';

/**
 * Luật kiểm tra biểu mẫu đăng nhập.
 *
 * Mã xác thực hai lớp và mã trong ảnh chỉ bắt buộc khi máy chủ đã yêu cầu, nên hai điều kiện này
 * đọc qua hàm chứ không khai cứng — cùng một biểu mẫu phục vụ cả ba trạng thái đăng nhập.
 */
function taoLuatDangNhap(canMfa: () => boolean, canCaptcha: () => boolean) {
  return z
    .object({
      tenDangNhap: batBuoc('Tên đăng nhập', 100),
      matKhau: z.string().min(1, 'Vui lòng nhập mật khẩu.'),
      maMfa: tuyChon(50),
      captchaLoiGiai: tuyChon(20),
    })
    .superRefine((giaTri, ctx) => {
      if (canMfa() && !giaTri.maMfa?.trim()) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          path: ['maMfa'],
          message: 'Vui lòng nhập mã xác thực.',
        });
      }

      if (canCaptcha() && !giaTri.captchaLoiGiai?.trim()) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          path: ['captchaLoiGiai'],
          message: 'Vui lòng nhập mã trong ảnh.',
        });
      }
    });
}

type FormDangNhap = z.infer<ReturnType<typeof taoLuatDangNhap>>;

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

  // Hai ràng buộc dưới đây bật/tắt theo phản hồi của máy chủ; đọc qua ref để biểu mẫu chỉ dựng
  // một lần mà luật vẫn thấy trạng thái hiện tại.
  const refCanMfa = useRef(false);
  const refCanCaptcha = useRef(false);
  const form = useBieuMau<FormDangNhap>(
    useMemo(() => taoLuatDangNhap(() => refCanMfa.current, () => refCanCaptcha.current), []),
    { tenDangNhap: '', matKhau: '' },
  );

  refCanMfa.current = canMfa;
  refCanCaptcha.current = !!captcha;

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
          <div style={{ display: 'inline-flex' }}>
            <LogoHeThong kichThuoc={48} />
          </div>
          <div style={{ fontSize: 18, fontWeight: 700, marginTop: 12, lineHeight: 1.4 }}>
            {tenHeThong}
          </div>
          {tenDonVi && (
            <div style={{ fontSize: 13, color: 'rgba(0,0,0,0.45)', marginTop: 2 }}>{tenDonVi}</div>
          )}
        </div>

        <BieuMau form={form} onGui={xuLyGui}>
          <Truong<FormDangNhap> ten="tenDangNhap" label="Tên đăng nhập" required>
            {(o) => (
              <Input
                {...o}
                value={o.value as string}
                prefix={<UserOutlined />}
                autoComplete="username"
                autoFocus
                size="large"
              />
            )}
          </Truong>

          <Truong<FormDangNhap> ten="matKhau" label="Mật khẩu" required>
            {(o) => (
              <Input.Password
                {...o}
                value={o.value as string}
                prefix={<LockOutlined />}
                autoComplete="current-password"
                size="large"
              />
            )}
          </Truong>

          {canMfa && (
            <>
              <Alert
                type="info"
                showIcon
                style={{ marginBottom: 12 }}
                message="Tài khoản đang bật xác thực hai lớp"
                description="Nhập mã 6 chữ số từ ứng dụng xác thực, hoặc một mã khôi phục."
              />
              <Truong<FormDangNhap> ten="maMfa" label="Mã xác thực" required>
                {(o) => (
                  <Input
                    {...o}
                    value={o.value as string}
                    prefix={<SafetyOutlined />}
                    placeholder="123456 hoặc mã khôi phục"
                    autoComplete="one-time-code"
                    autoFocus
                    size="large"
                  />
                )}
              </Truong>
            </>
          )}

          {captcha && (
            <Truong<FormDangNhap> ten="captchaLoiGiai" label="Mã xác nhận trong ảnh" required>
              {(o) => (
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
                    size="large"
                  />
                  {/*
                   * Ô nhập phải nhận trực tiếp value/onChange của trường. Trước đây nó nằm lồng
                   * trong <div> con của Form.Item, mà Ant Design chỉ nối dữ liệu vào PHẦN TỬ CON
                   * TRỰC TIẾP — nên mã người dùng gõ vào đây không bao giờ được gửi đi, và đăng
                   * nhập luôn báo sai CAPTCHA dù gõ đúng.
                   */}
                  <Input
                    {...o}
                    value={o.value as string}
                    placeholder="Nhập mã trong ảnh"
                    style={{ flex: '1 1 140px', minWidth: 0 }}
                    autoComplete="off"
                    size="large"
                  />
                </div>
              )}
            </Truong>
          )}

          {soLanSai >= 3 && !canMfa && (
            <Typography.Paragraph type="warning" style={{ fontSize: 13 }}>
              Bạn đã nhập sai {soLanSai} lần. Tài khoản sẽ bị khóa tạm thời sau 5 lần sai.
            </Typography.Paragraph>
          )}

          <Button
            type="primary"
            htmlType="submit"
            block
            size="large"
            loading={dangTai || form.formState.isSubmitting}
          >
            Đăng nhập
          </Button>
        </BieuMau>

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
