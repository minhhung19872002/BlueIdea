import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { App, Badge, Button, Drawer, Empty, List, Space, Tag, Typography } from 'antd';
import { BellOutlined, CheckOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { LoiApi } from '@/api/client';
import { apiHeThong, type ThongBaoTrongUngDung } from '@/api/endpoints';
import { batDauRealtime, layKetNoiRealtime, SU_KIEN_REALTIME } from '@/api/realtime';
import { ngayGio } from '@/components/ThanhPhanChung';

const MAU_MUC_DO: Record<string, string> = {
  KHAN: 'red',
  CAO: 'orange',
  BINH_THUONG: 'blue',
  THAP: 'default',
};

const TEN_MUC_DO: Record<string, string> = {
  KHAN: 'Khẩn',
  CAO: 'Quan trọng',
  BINH_THUONG: 'Thường',
  THAP: 'Thấp',
};

/**
 * Chuông thông báo trên thanh trên cùng: badge đếm số chưa đọc, bấm vào mở ngăn kéo danh sách.
 *
 * Số chưa đọc hỏi lại mỗi phút; danh sách đầy đủ chỉ tải khi người dùng thực sự mở ngăn kéo,
 * để trang nào cũng phải kéo về 20 thông báo là lãng phí.
 */
export function HopThongBao() {
  const { message } = App.useApp();
  const dieuHuong = useNavigate();
  const queryClient = useQueryClient();

  const [mo, setMo] = useState(false);

  const { data: chuaDoc } = useQuery({
    queryKey: ['thong-bao-chua-doc'],
    queryFn: () => apiHeThong.thongBao({ chuaDoc: true, soDong: 1 }),
    // Vẫn giữ nhịp hỏi lại 60 giây làm lưới an toàn: realtime có thể rớt kết nối mà người dùng
    // không hay biết, khi đó chuông vẫn phải cập nhật.
    refetchInterval: 60_000,
  });

  // Realtime: máy chủ chỉ báo "có thông báo mới", client tự gọi lại API để lấy đúng phần dữ
  // liệu mình được xem.
  useEffect(() => {
    let conSong = true;

    void batDauRealtime().then((kn) => {
      if (!kn || !conSong) return;

      kn.on(SU_KIEN_REALTIME.thongBaoMoi, () => {
        void queryClient.invalidateQueries({ queryKey: ['thong-bao-chua-doc'] });
        void queryClient.invalidateQueries({ queryKey: ['thong-bao-danh-sach'] });
      });
    });

    return () => {
      conSong = false;
      layKetNoiRealtime().off(SU_KIEN_REALTIME.thongBaoMoi);
    };
  }, [queryClient]);

  const { data: danhSach, isLoading } = useQuery({
    queryKey: ['thong-bao-danh-sach'],
    queryFn: () => apiHeThong.thongBao({ trang: 1, soDong: 20 }),
    enabled: mo,
  });

  function lamMoi() {
    void queryClient.invalidateQueries({ queryKey: ['thong-bao-chua-doc'] });
    void queryClient.invalidateQueries({ queryKey: ['thong-bao-danh-sach'] });
  }

  const danhDauDaDoc = useMutation({
    mutationFn: (id: string) => apiHeThong.danhDauDaDoc(id),
    onSuccess: lamMoi,
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không cập nhật được.'),
  });

  const docTatCa = useMutation({
    mutationFn: () => apiHeThong.danhDauDocTatCa(),
    onSuccess: () => {
      message.success('Đã đánh dấu đã đọc tất cả');
      lamMoi();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không cập nhật được.'),
  });

  /** Bấm vào một thông báo: đánh dấu đã đọc rồi mở thẳng đối tượng liên quan nếu có. */
  function moThongBao(muc: ThongBaoTrongUngDung) {
    if (!muc.daDoc) danhDauDaDoc.mutate(muc.id);

    if (muc.duongDan) {
      setMo(false);
      dieuHuong(muc.duongDan);
    }
  }

  const soChuaDoc = chuaDoc?.tongSo ?? 0;

  return (
    <>
      <Badge count={soChuaDoc} size="small" overflowCount={99}>
        <Button
          type="text"
          icon={<BellOutlined />}
          aria-label={`Thông báo${soChuaDoc > 0 ? `, ${soChuaDoc} chưa đọc` : ''}`}
          onClick={() => setMo(true)}
        />
      </Badge>

      <Drawer
        open={mo}
        onClose={() => setMo(false)}
        width={420}
        title={
          <Space>
            <span>Thông báo</span>
            {soChuaDoc > 0 && <Tag color="red">{soChuaDoc} chưa đọc</Tag>}
          </Space>
        }
        extra={
          <Button
            size="small"
            icon={<CheckOutlined />}
            disabled={soChuaDoc === 0}
            loading={docTatCa.isPending}
            onClick={() => docTatCa.mutate()}
          >
            Đọc tất cả
          </Button>
        }
      >
        <List<ThongBaoTrongUngDung>
          loading={isLoading}
          dataSource={danhSach?.duLieu ?? []}
          locale={{
            emptyText: <Empty description="Chưa có thông báo nào" />,
          }}
          renderItem={(muc) => (
            <List.Item
              onClick={() => moThongBao(muc)}
              style={{
                cursor: muc.duongDan || !muc.daDoc ? 'pointer' : 'default',
                background: muc.daDoc ? undefined : 'rgba(22,119,255,0.06)',
                padding: '12px 8px',
                alignItems: 'flex-start',
              }}
            >
              <List.Item.Meta
                title={
                  <Space size={6} wrap>
                    <Typography.Text strong={!muc.daDoc}>{muc.tieuDe}</Typography.Text>
                    {muc.mucDo !== 'BINH_THUONG' && (
                      <Tag color={MAU_MUC_DO[muc.mucDo] ?? 'default'}>
                        {TEN_MUC_DO[muc.mucDo] ?? muc.mucDo}
                      </Tag>
                    )}
                  </Space>
                }
                description={
                  <Space direction="vertical" size={2} style={{ width: '100%' }}>
                    <Typography.Text style={{ fontSize: 13 }}>{muc.noiDung}</Typography.Text>
                    <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                      {ngayGio(muc.thoiGian)}
                    </Typography.Text>
                  </Space>
                }
              />
            </List.Item>
          )}
        />

        {(danhSach?.tongSo ?? 0) > (danhSach?.duLieu.length ?? 0) && (
          <Typography.Paragraph
            type="secondary"
            style={{ fontSize: 12, textAlign: 'center', marginTop: 8 }}
          >
            Hiển thị {danhSach?.duLieu.length} thông báo gần nhất trong tổng số {danhSach?.tongSo}.
          </Typography.Paragraph>
        )}
      </Drawer>
    </>
  );
}
