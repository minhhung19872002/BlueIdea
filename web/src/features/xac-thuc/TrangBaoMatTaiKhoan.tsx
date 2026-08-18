import { useState } from 'react';
import { App, Alert, Button, Descriptions, Input, Modal, Space, Steps, Tag } from 'antd';
import { SafetyCertificateOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';

import { guiDuLieu, layDuLieu, LoiApi } from '@/api/client';
import { ngayGio } from '@/components/ThanhPhanChung';
import { useAuthStore } from '@/app/store/authStore';
import { BieuMau, Truong, useBieuMau } from '@/components/bieu-mau/BieuMau';

interface TrangThaiMfa {
  daBat: boolean;
  ngayBat: string | null;
  soMaKhoiPhucConLai: number;
}

interface BatDauGhiDanh {
  biMat: string;
  uriGhiDanh: string;
}

/**
 * Luật xác nhận bật xác thực hai lớp.
 *
 * Mã TOTP luôn là 6 chữ số. Bắt định dạng tại đây để người gõ nhầm khoá bí mật vào ô mã biết ngay,
 * thay vì gửi lên rồi nhận "mã không đúng" và tưởng ứng dụng xác thực lệch giờ.
 */
const luatXacNhanMfa = z.object({
  ma: z
    .string()
    .trim()
    .regex(/^\d{6}$/, 'Mã xác thực gồm đúng 6 chữ số.'),
});

type GiaTriXacNhanMfa = z.infer<typeof luatXacNhanMfa>;

/**
 * Chức năng 21 — Bật/tắt xác thực hai lớp cho chính tài khoản đang đăng nhập.
 *
 * Ghi danh chia hai bước: quét mã rồi phải nhập đúng một mã sinh từ chính bí mật đó thì MFA
 * mới bật. Bật ngay từ bước một thì người quét hỏng sẽ tự khoá mình ra ngoài.
 */
export default function TrangBaoMatTaiKhoan() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const nguoiDung = useAuthStore((s) => s.nguoiDung);
  const formXacNhan = useBieuMau(luatXacNhanMfa, { ma: '' });
  const [ghiDanh, setGhiDanh] = useState<BatDauGhiDanh | null>(null);
  const [maKhoiPhuc, setMaKhoiPhuc] = useState<string[] | null>(null);

  const { data: trangThai, isLoading } = useQuery({
    queryKey: ['mfa', 'trang-thai'],
    queryFn: () => layDuLieu<TrangThaiMfa>('/api/v1/xac-thuc/mfa/trang-thai'),
  });

  const lamMoi = () => queryClient.invalidateQueries({ queryKey: ['mfa'] });

  const batDau = useMutation({
    mutationFn: () => guiDuLieu<BatDauGhiDanh>('/api/v1/xac-thuc/mfa/bat-dau-ghi-danh', {}),
    onSuccess: setGhiDanh,
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không bắt đầu được.'),
  });

  const xacNhan = useMutation({
    mutationFn: (ma: string) =>
      guiDuLieu<{ maKhoiPhuc: string[] }>('/api/v1/xac-thuc/mfa/xac-nhan-ghi-danh', { ma }),
    onSuccess: (kq) => {
      setGhiDanh(null);
      setMaKhoiPhuc(kq.maKhoiPhuc);
      void lamMoi();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Mã không đúng.'),
  });

  const tat = useMutation({
    mutationFn: (giaTri: { matKhau: string; ma: string }) =>
      guiDuLieu('/api/v1/xac-thuc/mfa/tat', giaTri),
    onSuccess: () => {
      message.success('Đã tắt xác thực hai lớp');
      void lamMoi();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không tắt được.'),
  });

  const taoLaiMa = useMutation({
    mutationFn: (matKhau: string) =>
      guiDuLieu<{ maKhoiPhuc: string[] }>('/api/v1/xac-thuc/mfa/tao-lai-ma-khoi-phuc', {
        matKhau,
      }),
    onSuccess: (kq) => {
      setMaKhoiPhuc(kq.maKhoiPhuc);
      void lamMoi();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không sinh lại được.'),
  });

  function hoiMatKhau(nhan: string, khiXong: (matKhau: string, ma: string) => void) {
    let matKhau = '';
    let ma = '';

    modal.confirm({
      title: nhan,
      icon: null,
      content: (
        <Space direction="vertical" style={{ width: '100%', marginTop: 12 }}>
          <Input.Password
            placeholder="Mật khẩu hiện tại"
            autoComplete="current-password"
            onChange={(e) => (matKhau = e.target.value)}
          />
          <Input
            placeholder="Mã xác thực hoặc mã khôi phục"
            autoComplete="one-time-code"
            onChange={(e) => (ma = e.target.value)}
          />
        </Space>
      ),
      okText: 'Xác nhận',
      cancelText: 'Huỷ',
      onOk: () => khiXong(matKhau, ma),
    });
  }

  return (
    <div className="tk-the tk-the-than" style={{ maxWidth: 720 }}>
      {/*
       * Người dùng bị đưa tới đây tự động thì phải nói rõ vì sao — màn hình bảo mật hiện ra
       * không lời giải thích chỉ khiến họ tưởng bấm nhầm rồi bấm quay lại.
       */}
      {nguoiDung?.buocBatMfa && (
        <Alert
          type="warning"
          showIcon
          style={{ marginBottom: 16 }}
          message="Tài khoản quản trị bắt buộc bật xác thực hai lớp"
          description="Đơn vị đã bật yêu cầu này trong cấu hình bảo mật. Hoàn tất các bước bên dưới rồi mới dùng được các chức năng khác."
        />
      )}

      <Descriptions
        title="Xác thực hai lớp (TOTP)"
        column={1}
        size="small"
        bordered
        style={{ marginBottom: 16 }}
      >
        <Descriptions.Item label="Trạng thái">
          {isLoading ? (
            '…'
          ) : trangThai?.daBat ? (
            <Tag color="success">Đang bật</Tag>
          ) : (
            <Tag>Chưa bật</Tag>
          )}
        </Descriptions.Item>
        {trangThai?.daBat && (
          <>
            <Descriptions.Item label="Bật từ">{ngayGio(trangThai.ngayBat)}</Descriptions.Item>
            <Descriptions.Item label="Mã khôi phục còn lại">
              {trangThai.soMaKhoiPhucConLai}
            </Descriptions.Item>
          </>
        )}
      </Descriptions>

      {!trangThai?.daBat && !ghiDanh && (
        <Button
          type="primary"
          icon={<SafetyCertificateOutlined />}
          loading={batDau.isPending}
          onClick={() => batDau.mutate()}
        >
          Bật xác thực hai lớp
        </Button>
      )}

      {trangThai?.daBat && (
        <Space wrap>
          <Button
            danger
            onClick={() =>
              hoiMatKhau('Tắt xác thực hai lớp', (matKhau, ma) => tat.mutate({ matKhau, ma }))
            }
          >
            Tắt xác thực hai lớp
          </Button>
          <Button
            onClick={() =>
              hoiMatKhau('Sinh lại bộ mã khôi phục', (matKhau) => taoLaiMa.mutate(matKhau))
            }
          >
            Sinh lại mã khôi phục
          </Button>
        </Space>
      )}

      {ghiDanh && (
        <div style={{ marginTop: 8 }}>
          <Steps
            size="small"
            current={1}
            style={{ marginBottom: 16 }}
            items={[{ title: 'Tạo mã' }, { title: 'Xác nhận' }]}
          />

          <Alert
            type="warning"
            showIcon
            style={{ marginBottom: 16 }}
            message="Chưa bật xong"
            description="Xác thực hai lớp chỉ có hiệu lực sau khi bạn nhập đúng một mã ở bước dưới."
          />

          <div style={{ marginBottom: 12, fontSize: 13 }}>
            Mở ứng dụng xác thực (Google Authenticator, Microsoft Authenticator…) và thêm tài
            khoản bằng khoá bên dưới:
          </div>

          <Input.TextArea
            readOnly
            value={ghiDanh.biMat}
            autoSize
            style={{ fontFamily: 'monospace', marginBottom: 8 }}
            onFocus={(e) => e.target.select()}
          />

          <Input.TextArea
            readOnly
            value={ghiDanh.uriGhiDanh}
            autoSize={{ minRows: 2 }}
            style={{ fontFamily: 'monospace', fontSize: 12, marginBottom: 16 }}
            onFocus={(e) => e.target.select()}
          />

          <BieuMau
            form={formXacNhan}
            layout="inline"
            onGui={(giaTri) => xacNhan.mutateAsync(giaTri.ma)}
            style={{ gap: 8, flexWrap: 'wrap' }}
          >
            <Truong<GiaTriXacNhanMfa> ten="ma" style={{ marginInlineEnd: 0 }}>
              {(o) => (
                <Input
                  {...o}
                  value={o.value as string}
                  placeholder="Mã 6 chữ số"
                  style={{ width: 160 }}
                  autoComplete="one-time-code"
                />
              )}
            </Truong>
            <Button type="primary" htmlType="submit" loading={xacNhan.isPending}>
              Xác nhận và bật
            </Button>
            <Button onClick={() => setGhiDanh(null)}>Huỷ</Button>
          </BieuMau>
        </div>
      )}

      <Modal
        open={maKhoiPhuc !== null}
        title="Bộ mã khôi phục"
        onCancel={() => setMaKhoiPhuc(null)}
        onOk={() => setMaKhoiPhuc(null)}
        okText="Tôi đã lưu lại"
        cancelButtonProps={{ style: { display: 'none' } }}
      >
        <Alert
          type="warning"
          showIcon
          style={{ marginBottom: 12 }}
          message="Đây là lần duy nhất bộ mã được hiển thị"
          description="Hãy in ra hoặc lưu vào nơi an toàn. Mỗi mã chỉ dùng được một lần, dùng khi bạn mất thiết bị xác thực."
        />
        <div
          style={{
            fontFamily: 'monospace',
            fontSize: 14,
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fill,minmax(120px,1fr))',
            gap: 6,
          }}
        >
          {(maKhoiPhuc ?? []).map((m) => (
            <div key={m}>{m}</div>
          ))}
        </div>
      </Modal>
    </div>
  );
}
