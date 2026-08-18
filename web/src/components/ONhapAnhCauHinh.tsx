import { useState } from 'react';
import { App, Button, Space, Typography, Upload } from 'antd';
import { DeleteOutlined, UploadOutlined } from '@ant-design/icons';

import { LoiApi } from '@/api/client';
import { taiTepLen } from '@/api/endpoints';

const DINH_DANG = '.png,.jpg,.jpeg,.gif,.webp,.ico';

/**
 * Ô chọn ảnh cho một khoá cấu hình kiểu TEP (logo, favicon).
 *
 * Giá trị lưu trong cấu hình là **id của tệp đã tải lên**, không phải ảnh nhúng: ảnh nhúng dạng
 * base64 sẽ phình bảng cấu hình và đi kèm mọi lần đọc cấu hình công khai — tức là mỗi lần mở
 * trang đăng nhập.
 *
 * Tải xong là ghi id vào form ngay, nhưng chỉ thực sự lưu khi người dùng bấm Lưu cấu hình như
 * mọi ô khác — không tự ý lưu thay họ.
 */
export function ONhapAnhCauHinh({
  value,
  onChange,
  disabled,
  duongDanXem,
}: {
  value?: string;
  onChange?: (giaTri: string) => void;
  disabled?: boolean;
  /** Địa chỉ xem ảnh đang lưu trên máy chủ; bỏ trống thì chỉ hiện tên tệp. */
  duongDanXem?: string;
}) {
  const { message } = App.useApp();
  const [dangTai, setDangTai] = useState(false);
  const [vuaChon, setVuaChon] = useState<string | null>(null);

  async function taiLen(tep: File) {
    setDangTai(true);
    try {
      const daTaiLen = await taiTepLen(tep);
      onChange?.(daTaiLen.id);
      setVuaChon(daTaiLen.tenGoc);
      message.success('Đã tải ảnh lên. Bấm "Lưu cấu hình" để áp dụng.');
    } catch (loi) {
      message.error(loi instanceof LoiApi ? loi.message : 'Không tải được ảnh.');
    } finally {
      setDangTai(false);
    }
  }

  return (
    <Space direction="vertical" size={6}>
      <Space wrap>
        <Upload
          accept={DINH_DANG}
          showUploadList={false}
          disabled={disabled || dangTai}
          beforeUpload={(tep) => {
            void taiLen(tep as File);
            // Tu goi API nen chan antd tai len bang duong mac dinh.
            return false;
          }}
        >
          <Button icon={<UploadOutlined />} loading={dangTai} disabled={disabled}>
            {value ? 'Đổi ảnh khác' : 'Chọn ảnh'}
          </Button>
        </Upload>

        {value && (
          <Button
            icon={<DeleteOutlined />}
            danger
            type="text"
            disabled={disabled}
            onClick={() => {
              onChange?.('');
              setVuaChon(null);
            }}
          >
            Bỏ ảnh
          </Button>
        )}
      </Space>

      {value && duongDanXem && (
        <img
          src={vuaChon ? `${duongDanXem}?v=${value}` : duongDanXem}
          alt="Ảnh đang dùng"
          style={{ maxWidth: 160, maxHeight: 64, objectFit: 'contain', display: 'block' }}
        />
      )}

      {vuaChon && (
        <Typography.Text type="secondary" style={{ fontSize: 12 }}>
          Tệp mới: {vuaChon} — chưa lưu.
        </Typography.Text>
      )}

      {!value && (
        <Typography.Text type="secondary" style={{ fontSize: 12 }}>
          Chưa đặt ảnh — giao diện dùng chữ viết tắt của tên hệ thống.
        </Typography.Text>
      )}
    </Space>
  );
}
