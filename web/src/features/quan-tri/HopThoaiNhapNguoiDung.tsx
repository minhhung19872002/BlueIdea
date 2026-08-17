import { useState } from 'react';
import {
  App,
  Alert,
  Button,
  Modal,
  Space,
  Steps,
  Table,
  Tag,
  Typography,
  Upload,
} from 'antd';
import { DownloadOutlined, InboxOutlined } from '@ant-design/icons';
import type { UploadFile } from 'antd';

import { LoiApi, taiTep } from '@/api/client';
import { apiNhapXuat, type KetQuaDongNhap, type KetQuaNhapNguoiDung } from '@/api/endpoints';

/**
 * Chức năng 43 — Nhập người dùng từ Excel.
 *
 * Luôn chạy thử trước rồi mới ghi: quản trị viên thấy trước từng dòng lỗi để sửa tệp,
 * thay vì tạo nửa vời rồi phải dọn tay.
 */
export default function HopThoaiNhapNguoiDung({
  onDong,
  onXong,
}: {
  onDong: () => void;
  onXong: () => void;
}) {
  const { message } = App.useApp();

  const [tep, setTep] = useState<File | null>(null);
  const [ketQua, setKetQua] = useState<KetQuaNhapNguoiDung | null>(null);
  const [dangChay, setDangChay] = useState(false);
  const [daGhi, setDaGhi] = useState(false);

  const buoc = daGhi ? 2 : ketQua ? 1 : 0;

  async function chay(chayThu: boolean) {
    if (!tep) {
      message.warning('Chưa chọn tệp Excel.');
      return;
    }

    setDangChay(true);

    try {
      const phanHoi = await apiNhapXuat.nhapNguoiDung(tep, chayThu);
      setKetQua(phanHoi.duLieu);

      if (!phanHoi.duLieu.chayThu) {
        setDaGhi(true);
        message.success(phanHoi.thongBao);
        onXong();
      } else if (phanHoi.duLieu.soLoi > 0) {
        message.warning(phanHoi.thongBao);
      } else {
        message.success(phanHoi.thongBao);
      }
    } catch (loi) {
      message.error(loi instanceof LoiApi ? loi.message : 'Không đọc được tệp.');
    } finally {
      setDangChay(false);
    }
  }

  const sanSangGhi = ketQua !== null && ketQua.soLoi === 0 && ketQua.soHopLe > 0 && !daGhi;

  return (
    <Modal
      open
      width={860}
      title="Nhập người dùng từ Excel"
      onCancel={onDong}
      footer={
        <Space>
          <Button onClick={onDong}>{daGhi ? 'Đóng' : 'Huỷ'}</Button>
          {!daGhi && (
            <Button loading={dangChay} disabled={!tep} onClick={() => chay(true)}>
              Kiểm tra tệp
            </Button>
          )}
          {!daGhi && (
            <Button type="primary" loading={dangChay} disabled={!sanSangGhi} onClick={() => chay(false)}>
              Tạo {ketQua?.soHopLe ?? 0} tài khoản
            </Button>
          )}
        </Space>
      }
    >
      <Steps
        size="small"
        current={buoc}
        style={{ marginBottom: 16 }}
        items={[
          { title: 'Chọn tệp' },
          { title: 'Kiểm tra' },
          { title: 'Đã tạo' },
        ]}
      />

      {buoc === 0 && (
        <>
          <Button
            type="link"
            icon={<DownloadOutlined />}
            style={{ paddingLeft: 0, marginBottom: 8 }}
            onClick={() =>
              taiTep(apiNhapXuat.duongDanMauNhapNguoiDung(), 'mau-nhap-nguoi-dung.xlsx')
            }
          >
            Tải tệp mẫu (kèm hướng dẫn và dòng ví dụ)
          </Button>

          <Upload.Dragger
            accept=".xlsx"
            maxCount={1}
            beforeUpload={(t) => {
              setTep(t);
              setKetQua(null);
              // Trả false để AntD không tự tải lên — tệp được gửi ở bước bấm nút.
              return false;
            }}
            onRemove={() => {
              setTep(null);
              setKetQua(null);
            }}
            fileList={tep ? ([{ uid: '1', name: tep.name }] as UploadFile[]) : []}
          >
            <p className="ant-upload-drag-icon">
              <InboxOutlined />
            </p>
            <p className="ant-upload-text">Kéo thả hoặc bấm để chọn tệp .xlsx</p>
            <p className="ant-upload-hint">
              Cột theo đúng thứ tự: Tên đăng nhập • Họ và tên • Email • Điện thoại • Chức vụ •
              Mã đơn vị • Mã vai trò
            </p>
          </Upload.Dragger>
        </>
      )}

      {ketQua && (
        <>
          {ketQua.soLoi > 0 ? (
            <Alert
              type="error"
              showIcon
              style={{ marginBottom: 12 }}
              message={`${ketQua.soLoi}/${ketQua.tongDong} dòng có lỗi — chưa tạo tài khoản nào.`}
              description="Hệ thống nhập toàn bộ hoặc không nhập gì. Hãy sửa các dòng lỗi bên dưới rồi tải lại tệp."
            />
          ) : daGhi ? (
            <Alert
              type="success"
              showIcon
              style={{ marginBottom: 12 }}
              message={`Đã tạo ${ketQua.soHopLe} tài khoản.`}
              description="Mật khẩu tạm hiển thị trong bảng dưới đây và chỉ xem được một lần. Hãy sao chép trước khi đóng hộp thoại."
            />
          ) : (
            <Alert
              type="success"
              showIcon
              style={{ marginBottom: 12 }}
              message={`${ketQua.soHopLe}/${ketQua.tongDong} dòng hợp lệ.`}
              description="Bấm “Tạo tài khoản” để ghi vào hệ thống."
            />
          )}

          <Table<KetQuaDongNhap>
            rowKey="soDong"
            size="small"
            dataSource={ketQua.chiTiet}
            pagination={{ pageSize: 10, showSizeChanger: false }}
            scroll={{ y: 300 }}
            columns={[
              { title: 'Dòng', dataIndex: 'soDong', width: 70 },
              { title: 'Tên đăng nhập', dataIndex: 'tenDangNhap', width: 160 },
              { title: 'Họ và tên', dataIndex: 'hoTen', width: 180 },
              {
                title: 'Kết quả',
                dataIndex: 'hopLe',
                width: 110,
                render: (v: boolean) =>
                  v ? <Tag color="success">Hợp lệ</Tag> : <Tag color="error">Lỗi</Tag>,
              },
              {
                title: 'Chi tiết',
                key: 'chiTiet',
                render: (_v, dong) =>
                  dong.loi ? (
                    <Typography.Text type="danger">{dong.loi}</Typography.Text>
                  ) : dong.matKhauTam ? (
                    <Typography.Text copyable code>
                      {dong.matKhauTam}
                    </Typography.Text>
                  ) : (
                    '—'
                  ),
              },
            ]}
          />
        </>
      )}
    </Modal>
  );
}
