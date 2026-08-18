import { useState } from 'react';
import { Alert, App, Button, Input, Modal, Space, Steps, Typography } from 'antd';
import { useMutation } from '@tanstack/react-query';

import { LoiApi } from '@/api/client';
import { apiKySoUsb, type YeuCauKyUsb } from '@/api/endpoints';

interface Props {
  tepTinId: string;
  doiTuong: 'QUYET_DINH' | 'BIEN_BAN' | 'PHIEU_DANH_GIA';
  doiTuongId: string;
  tenVanBan?: string;
  onDong: () => void;
  onXong?: () => void;
}

/**
 * Ký số bằng USB token.
 *
 * Khoá bí mật nằm trong USB token và không rời khỏi thiết bị — đó là lý do tồn tại của token, và
 * cũng là lý do trình duyệt không tự ký được. Luồng vì vậy có ba nhịp:
 *   1. Máy chủ chốt nội dung và trả về GIÁ TRỊ BĂM.
 *   2. Người dùng đưa giá trị băm đó cho công cụ ký của nhà cung cấp token (plugin/ứng dụng cài
 *      trên máy), nhập mã PIN, nhận lại chữ ký và chứng thư.
 *   3. Dán hai giá trị đó vào đây; máy chủ xác minh rồi mới ghi nhận.
 *
 * Màn hình này cố tình KHÔNG tự gọi công cụ ký: mỗi nhà cung cấp token ở Việt Nam có plugin và
 * giao thức riêng (cổng nội bộ, lược đồ URL, tiện ích trình duyệt). Chốt cứng một hãng sẽ làm
 * các đơn vị dùng hãng khác không ký được. Khi đơn vị đã chọn nhà cung cấp, phần nối tự động
 * thay đúng bước 2 mà không phải sửa gì ở máy chủ.
 */
export function HopThoaiKySoUsb({
  tepTinId,
  doiTuong,
  doiTuongId,
  tenVanBan,
  onDong,
  onXong,
}: Props) {
  const { message } = App.useApp();

  const [yeuCau, setYeuCau] = useState<YeuCauKyUsb | null>(null);
  const [chuKy, setChuKy] = useState('');
  const [chungThu, setChungThu] = useState('');

  const chuanBi = useMutation({
    mutationFn: () => apiKySoUsb.chuanBi({ tepTinId, doiTuong, doiTuongId }),
    onSuccess: setYeuCau,
    onError: (loi) =>
      message.error(loi instanceof LoiApi ? loi.message : 'Không mở được phiên ký.'),
  });

  const hoanTat = useMutation({
    mutationFn: () =>
      apiKySoUsb.hoanTat(yeuCau!.phienId, {
        chuKyBase64: chuKy.trim(),
        chungThuBase64: chungThu.trim(),
      }),
    onSuccess: (kq) => {
      message.success(`Đã ký số bằng chứng thư: ${kq.chuTheChungThu}`);
      onXong?.();
      onDong();
    },
    onError: (loi) =>
      message.error(loi instanceof LoiApi ? loi.message : 'Chữ ký không hợp lệ.'),
  });

  function huy() {
    if (yeuCau) void apiKySoUsb.huy(yeuCau.phienId).catch(() => undefined);
    onDong();
  }

  return (
    <Modal
      open
      width={720}
      title={`Ký số bằng USB token${tenVanBan ? ` — ${tenVanBan}` : ''}`}
      onCancel={huy}
      footer={
        <Space>
          <Button onClick={huy}>Huỷ</Button>
          {!yeuCau ? (
            <Button type="primary" loading={chuanBi.isPending} onClick={() => chuanBi.mutate()}>
              Bắt đầu ký
            </Button>
          ) : (
            <Button
              type="primary"
              disabled={!chuKy.trim() || !chungThu.trim()}
              loading={hoanTat.isPending}
              onClick={() => hoanTat.mutate()}
            >
              Xác nhận chữ ký
            </Button>
          )}
        </Space>
      }
    >
      <Steps
        size="small"
        style={{ marginBottom: 16 }}
        current={yeuCau ? 1 : 0}
        items={[
          { title: 'Chốt nội dung' },
          { title: 'Ký trên máy trạm' },
          { title: 'Xác minh' },
        ]}
      />

      {!yeuCau ? (
        <Alert
          type="info"
          showIcon
          message="Cắm USB token vào máy trước khi bắt đầu."
          description="Hệ thống sẽ chốt nội dung văn bản và tạo giá trị băm để công cụ ký của bạn ký lên đó. Khoá bí mật không rời khỏi token và không gửi lên máy chủ."
        />
      ) : (
        <Space direction="vertical" size={12} style={{ width: '100%' }}>
          <Alert
            type="warning"
            showIcon
            message={`Phiên ký hết hạn lúc ${new Date(yeuCau.hetHan).toLocaleTimeString('vi-VN')}`}
            description="Hết hạn thì bấm Huỷ rồi ký lại — giá trị băm cũ không dùng được nữa."
          />

          <div>
            <Typography.Text strong>1. Giá trị băm cần ký ({yeuCau.thuatToanBam})</Typography.Text>
            <Input.TextArea
              readOnly
              rows={2}
              value={yeuCau.hashBase64}
              onFocus={(e) => e.currentTarget.select()}
            />
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              Sao chép giá trị này, đưa vào công cụ ký của nhà cung cấp token và nhập mã PIN.
            </Typography.Text>
          </div>

          <div>
            <Typography.Text strong>2. Chữ ký nhận được (Base64)</Typography.Text>
            <Input.TextArea
              rows={3}
              value={chuKy}
              placeholder="Dán chữ ký do công cụ ký trả về"
              onChange={(e) => setChuKy(e.target.value)}
            />
          </div>

          <div>
            <Typography.Text strong>3. Chứng thư công khai (Base64, định dạng DER)</Typography.Text>
            <Input.TextArea
              rows={3}
              value={chungThu}
              placeholder="Dán chứng thư đọc từ USB token"
              onChange={(e) => setChungThu(e.target.value)}
            />
          </div>

          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            Máy chủ đối chiếu chữ ký với đúng giá trị băm đã phát ở bước 1 và kiểm tra hiệu lực
            chứng thư trước khi ghi nhận. Sai một trong hai thì chữ ký bị từ chối.
          </Typography.Text>
        </Space>
      )}
    </Modal>
  );
}
