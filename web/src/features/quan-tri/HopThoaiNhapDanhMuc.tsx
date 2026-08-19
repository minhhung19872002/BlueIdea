import { useState } from 'react';
import { App, Alert, Button, Modal, Space, Statistic, Table, Tag, Typography, Upload } from 'antd';
import { DownloadOutlined, UploadOutlined } from '@ant-design/icons';
import { useMutation, useQueryClient } from '@tanstack/react-query';

import { LoiApi, taiTep } from '@/api/client';
import { apiNhapXuat, type KetQuaNhapDanhMuc } from '@/api/endpoints';

const TEN_LOAI: Record<string, string> = {
  'linh-vuc': 'lĩnh vực',
  'doi-tuong': 'đối tượng áp dụng',
  'loai-tac-gia': 'loại tác giả',
};

/**
 * Nhập danh mục từ Excel.
 *
 * Luôn CHẠY THỬ trước: lần tải lên đầu chỉ kiểm tra và báo lỗi từng dòng, người dùng xem kết quả
 * rồi mới bấm xác nhận ghi. Nhập thẳng thì một tệp sai định dạng đã kịp tạo hàng chục mục rác
 * trong danh mục dùng chung của toàn hệ thống.
 */
export function HopThoaiNhapDanhMuc({
  loai,
  onDong,
}: {
  loai: string;
  onDong: () => void;
}) {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [tep, setTep] = useState<File | null>(null);
  const [ketQua, setKetQua] = useState<KetQuaNhapDanhMuc | null>(null);

  const chay = useMutation({
    mutationFn: ({ tepNhap, chayThu }: { tepNhap: File; chayThu: boolean }) =>
      apiNhapXuat.nhapDanhMuc(loai, tepNhap, chayThu),
    onSuccess: (kq) => {
      setKetQua(kq);

      if (!kq.chayThu) {
        message.success(`Đã thêm ${kq.soThemMoi} và cập nhật ${kq.soCapNhat} mục`);
        void queryClient.invalidateQueries({ queryKey: ['danh-muc'] });
        onDong();
      }
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không đọc được tệp.'),
  });

  const coLoi = (ketQua?.soLoi ?? 0) > 0;

  return (
    <Modal
      open
      width={760}
      title={`Nhập danh mục ${TEN_LOAI[loai] ?? loai} từ Excel`}
      onCancel={onDong}
      footer={[
        <Button key="mau" icon={<DownloadOutlined />} onClick={() => void taiMau()}>
          Tải tệp mẫu
        </Button>,
        <Button key="huy" onClick={onDong}>
          Đóng
        </Button>,
        <Button
          key="ghi"
          type="primary"
          disabled={!tep || !ketQua || ketQua.soHopLe === 0}
          loading={chay.isPending && !chay.variables?.chayThu}
          onClick={() => tep && chay.mutate({ tepNhap: tep, chayThu: false })}
        >
          Xác nhận ghi {ketQua ? `(${ketQua.soHopLe} dòng)` : ''}
        </Button>,
      ]}
    >
      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 12 }}
        message="Mã đã có trong hệ thống sẽ được cập nhật, không tạo bản trùng."
        description="Tệp nhập không bật lại danh mục đang ngừng sử dụng — muốn dùng lại thì bật thủ công trên danh sách."
      />

      <Space direction="vertical" size={12} style={{ width: '100%' }}>
        <Upload
          accept=".xlsx"
          maxCount={1}
          showUploadList={{ showRemoveIcon: false }}
          fileList={tep ? [{ uid: '1', name: tep.name, status: 'done' }] : []}
          beforeUpload={(t) => {
            const tepNhap = t as unknown as File;
            setTep(tepNhap);
            setKetQua(null);
            chay.mutate({ tepNhap, chayThu: true });

            return false;
          }}
        >
          <Button icon={<UploadOutlined />} loading={chay.isPending && chay.variables?.chayThu}>
            Chọn tệp .xlsx
          </Button>
        </Upload>

        {ketQua && (
          <>
            <Space size="large" wrap>
              <Statistic title="Tổng dòng" value={ketQua.tongDong} />
              <Statistic
                title="Hợp lệ"
                value={ketQua.soHopLe}
                valueStyle={{ color: '#389e0d' }}
              />
              <Statistic
                title="Lỗi"
                value={ketQua.soLoi}
                valueStyle={{ color: coLoi ? '#cf1322' : undefined }}
              />
              <Statistic title="Thêm mới" value={ketQua.soThemMoi} />
              <Statistic title="Cập nhật" value={ketQua.soCapNhat} />
            </Space>

            {coLoi && (
              <Alert
                type="warning"
                showIcon
                message={`${ketQua.soLoi} dòng có lỗi và sẽ bị bỏ qua.`}
                description="Các dòng hợp lệ vẫn ghi được — sửa dòng lỗi rồi nhập lại riêng phần đó."
              />
            )}

            <Table
              rowKey="soDong"
              size="small"
              dataSource={ketQua.chiTiet}
              pagination={{ pageSize: 8, showSizeChanger: false }}
              scroll={{ x: 700 }}
              columns={[
                { title: 'Dòng', dataIndex: 'soDong', width: 70 },
                { title: 'Mã', dataIndex: 'ma', width: 150 },
                { title: 'Tên', dataIndex: 'ten', width: 200, ellipsis: true },
                {
                  title: 'Kết quả',
                  dataIndex: 'hopLe',
                  width: 110,
                  render: (v: boolean) =>
                    v ? <Tag color="success">Hợp lệ</Tag> : <Tag color="error">Lỗi</Tag>,
                },
                {
                  title: 'Ghi chú',
                  dataIndex: 'loi',
                  width: 260,
                  render: (v: string | null) =>
                    v ? <Typography.Text type="danger">{v}</Typography.Text> : '—',
                },
              ]}
            />
          </>
        )}
      </Space>
    </Modal>
  );

  async function taiMau() {
    try {
      await taiTep(apiNhapXuat.duongDanMauNhapDanhMuc(loai), `mau-nhap-${loai}.xlsx`);
    } catch (loi) {
      message.error(loi instanceof LoiApi ? loi.message : 'Không tải được tệp mẫu.');
    }
  }
}
