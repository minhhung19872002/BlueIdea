import { Alert, Card, Col, Row, Space, Statistic, Table, Tag, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';

import { apiSaoLuu, type BanSaoLuu } from '@/api/endpoints';
import { KhoiDangTai, KhoiLoi, KhoiRong, ngayGio } from '@/components/ThanhPhanChung';
import { DaiTabTrang } from '@/components/DaiTabTrang';
import { DS_TAB_CAU_HINH } from './cauHinhTab';

const MUC_CANH_BAO: Record<string, 'success' | 'warning' | 'error' | 'info'> = {
  BINH_THUONG: 'success',
  CANH_BAO: 'warning',
  NGUY_HIEM: 'error',
  CHUA_CAU_HINH: 'info',
};

function doDung(byte: number): string {
  if (byte < 1024) return `${byte} B`;
  if (byte < 1024 ** 2) return `${(byte / 1024).toFixed(1)} KB`;
  if (byte < 1024 ** 3) return `${(byte / 1024 ** 2).toFixed(1)} MB`;
  return `${(byte / 1024 ** 3).toFixed(2)} GB`;
}

/**
 * Theo dõi tình trạng sao lưu.
 *
 * Màn hình này CHỈ XEM: tạo bản sao lưu và khôi phục chạy bằng script trên máy chủ. Khôi phục ghi
 * đè toàn bộ cơ sở dữ liệu đang chạy — đưa lên web thì một cú bấm nhầm là mất dữ liệu thật.
 */
export default function TrangSaoLuu() {
  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['sao-luu'],
    queryFn: apiSaoLuu.tinhTrang,
    refetchInterval: 5 * 60 * 1000,
  });

  if (isLoading) return <KhoiDangTai />;
  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;
  if (!data) return <KhoiRong moTa="Không đọc được tình trạng sao lưu." />;

  return (
    <Card title="Cấu hình hệ thống">
      <DaiTabTrang danhSach={DS_TAB_CAU_HINH} dangChon="sao-luu" />

      <Alert
        type={MUC_CANH_BAO[data.mucCanhBao] ?? 'info'}
        showIcon
        style={{ marginBottom: 16 }}
        message={data.thongDiep}
        description={
          <Space direction="vertical" size={2}>
            <span>
              Thư mục sao lưu: <code>{data.thuMuc}</code>
            </span>
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              Tạo bản mới: <code>./sao-luu-blueidea.sh sao-luu</code> — Khôi phục:{' '}
              <code>./sao-luu-blueidea.sh khoi-phuc &lt;thư-mục&gt;</code>. Hai lệnh này chạy trên
              máy chủ, không đưa lên web vì khôi phục ghi đè toàn bộ dữ liệu đang chạy.
            </Typography.Text>
          </Space>
        }
      />

      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        <Col xs={12} md={8}>
          <Card size="small">
            <Statistic title="Số bản sao lưu" value={data.soBan} />
          </Card>
        </Col>
        <Col xs={12} md={8}>
          <Card size="small">
            <Statistic title="Tổng dung lượng" value={doDung(data.tongKichThuocByte)} />
          </Card>
        </Col>
        <Col xs={24} md={8}>
          <Card size="small">
            <Statistic
              title="Bản gần nhất"
              value={
                data.soGioTuLanGanNhat == null
                  ? '—'
                  : data.soGioTuLanGanNhat < 24
                    ? `${Math.round(data.soGioTuLanGanNhat)} giờ trước`
                    : `${Math.round(data.soGioTuLanGanNhat / 24)} ngày trước`
              }
              valueStyle={{
                color: (data.soGioTuLanGanNhat ?? 0) > 48 ? '#cf1322' : '#389e0d',
              }}
            />
          </Card>
        </Col>
      </Row>

      <Table<BanSaoLuu>
        rowKey="ten"
        size="middle"
        dataSource={data.danhSach}
        pagination={{ pageSize: 20, showSizeChanger: true }}
        scroll={{ x: 800 }}
        locale={{ emptyText: <KhoiRong moTa="Chưa có bản sao lưu nào trong thư mục." /> }}
        columns={[
          { title: 'Bản sao lưu', dataIndex: 'ten', width: 220 },
          {
            title: 'Thời điểm',
            dataIndex: 'thoiGian',
            width: 180,
            render: (v: string) => ngayGio(v),
          },
          {
            title: 'Dung lượng',
            dataIndex: 'kichThuocByte',
            width: 140,
            align: 'right',
            render: (v: number) => doDung(v),
          },
          {
            title: 'Cơ sở dữ liệu',
            dataIndex: 'coCsdl',
            width: 150,
            render: (v: boolean) => (v ? <Tag color="success">Có</Tag> : <Tag color="error">Thiếu</Tag>),
          },
          {
            title: 'Tệp đính kèm',
            dataIndex: 'coTepDinhKem',
            width: 150,
            render: (v: boolean) => (v ? <Tag color="success">Có</Tag> : <Tag color="error">Thiếu</Tag>),
          },
          {
            title: 'Khôi phục được',
            dataIndex: 'dayDu',
            width: 160,
            render: (v: boolean) =>
              v ? (
                <Tag color="success">Đầy đủ</Tag>
              ) : (
                <Tag color="warning">Thiếu thành phần</Tag>
              ),
          },
        ]}
      />

      <Typography.Paragraph type="secondary" style={{ marginTop: 12, fontSize: 12 }}>
        Một bản sao lưu chỉ dùng được khi có đủ cả cơ sở dữ liệu và kho tệp đính kèm: hồ sơ trong
        cơ sở dữ liệu trỏ tới tệp, mất tệp thì hồ sơ khôi phục ra rỗng ruột. Bản sao lưu chưa từng
        được khôi phục thử thì chưa thể coi là bản sao lưu.
      </Typography.Paragraph>
    </Card>
  );
}
