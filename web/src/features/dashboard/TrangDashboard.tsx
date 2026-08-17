import { useMemo } from 'react';
import { Link } from 'react-router-dom';
import { Card, Col, Row, Statistic, Typography } from 'antd';
import {
  CheckCircleOutlined,
  ClockCircleOutlined,
  FileTextOutlined,
  WarningOutlined,
} from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import ReactECharts from 'echarts-for-react';

import { apiBaoCao } from '@/api/endpoints';
import { KhoiDangTai, KhoiLoi } from '@/components/ThanhPhanChung';
import { useAuthStore } from '@/app/store/authStore';

/** Bảng màu dùng chung cho biểu đồ để toàn hệ thống nhất quán. */
const BANG_MAU = ['#1677ff', '#52c41a', '#faad14', '#ff4d4f', '#722ed1', '#13c2c2', '#eb2f96'];

export default function TrangDashboard() {
  const nguoiDung = useAuthStore((s) => s.nguoiDung);

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['bao-cao', 'tong-quan'],
    queryFn: () => apiBaoCao.tongQuan(),
  });

  const bieuDoTrangThai = useMemo(
    () => ({
      tooltip: { trigger: 'item' },
      legend: { bottom: 0, type: 'scroll' },
      series: [
        {
          name: 'Trạng thái',
          type: 'pie',
          radius: ['45%', '70%'],
          avoidLabelOverlap: true,
          itemStyle: { borderRadius: 6, borderColor: '#fff', borderWidth: 2 },
          label: { show: false },
          data: (data?.theoTrangThai ?? []).map((x, i) => ({
            name: x.ten,
            value: x.soLuong,
            itemStyle: { color: BANG_MAU[i % BANG_MAU.length] },
          })),
        },
      ],
    }),
    [data],
  );

  const bieuDoLinhVuc = useMemo(
    () => ({
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
      grid: { left: 8, right: 16, bottom: 8, top: 16, containLabel: true },
      xAxis: { type: 'value', minInterval: 1 },
      yAxis: {
        type: 'category',
        data: (data?.theoLinhVuc ?? []).map((x) => x.ten).reverse(),
        axisLabel: { width: 140, overflow: 'truncate' },
      },
      series: [
        {
          name: 'Số hồ sơ',
          type: 'bar',
          data: (data?.theoLinhVuc ?? []).map((x) => x.soLuong).reverse(),
          itemStyle: { color: '#1677ff', borderRadius: [0, 4, 4, 0] },
          barMaxWidth: 22,
        },
      ],
    }),
    [data],
  );

  const bieuDoXuHuong = useMemo(
    () => ({
      tooltip: { trigger: 'axis' },
      legend: { data: ['Tổng hồ sơ', 'Được công nhận'], bottom: 0 },
      grid: { left: 8, right: 16, bottom: 32, top: 16, containLabel: true },
      xAxis: { type: 'category', data: (data?.xuHuongTheoNam ?? []).map((x) => x.ten) },
      yAxis: { type: 'value', minInterval: 1 },
      series: [
        {
          name: 'Tổng hồ sơ',
          type: 'bar',
          data: (data?.xuHuongTheoNam ?? []).map((x) => x.soLuong),
          itemStyle: { color: '#1677ff', borderRadius: [4, 4, 0, 0] },
          barMaxWidth: 40,
        },
        {
          name: 'Được công nhận',
          type: 'line',
          smooth: true,
          data: (data?.xuHuongTheoNam ?? []).map((x) => x.giaTriPhu ?? 0),
          itemStyle: { color: '#52c41a' },
        },
      ],
    }),
    [data],
  );

  if (isLoading) return <KhoiDangTai />;
  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  return (
    <div>
      <Typography.Title level={4} style={{ marginTop: 0 }}>
        Xin chào, {nguoiDung?.hoTen}
      </Typography.Title>

      <Row gutter={[12, 12]}>
        <Col xs={12} sm={12} md={6}>
          <Card>
            <Statistic
              title="Tổng hồ sơ"
              value={data?.tongHoSo ?? 0}
              prefix={<FileTextOutlined />}
            />
          </Card>
        </Col>
        <Col xs={12} sm={12} md={6}>
          <Card>
            <Statistic
              title="Đang xử lý"
              value={data?.hoSoDangXuLy ?? 0}
              prefix={<ClockCircleOutlined />}
              valueStyle={{ color: '#1677ff' }}
            />
          </Card>
        </Col>
        <Col xs={12} sm={12} md={6}>
          <Card>
            <Statistic
              title="Được công nhận"
              value={data?.hoSoDat ?? 0}
              suffix={`(${data?.tyLeDat ?? 0}%)`}
              prefix={<CheckCircleOutlined />}
              valueStyle={{ color: '#52c41a' }}
            />
          </Card>
        </Col>
        <Col xs={12} sm={12} md={6}>
          <Card>
            <Link to="/xu-ly?chiQuaHan=true">
              <Statistic
                title="Quá hạn xử lý"
                value={data?.hoSoQuaHan ?? 0}
                prefix={<WarningOutlined />}
                valueStyle={{ color: (data?.hoSoQuaHan ?? 0) > 0 ? '#ff4d4f' : undefined }}
              />
            </Link>
          </Card>
        </Col>
      </Row>

      <Row gutter={[12, 12]} style={{ marginTop: 12 }}>
        <Col xs={24} lg={8}>
          <Card title="Hồ sơ theo trạng thái" size="small">
            <ReactECharts option={bieuDoTrangThai} style={{ height: 280 }} />
          </Card>
        </Col>
        <Col xs={24} lg={8}>
          <Card title="Hồ sơ theo lĩnh vực" size="small">
            <ReactECharts option={bieuDoLinhVuc} style={{ height: 280 }} />
          </Card>
        </Col>
        <Col xs={24} lg={8}>
          <Card title="Xu hướng theo năm" size="small">
            <ReactECharts option={bieuDoXuHuong} style={{ height: 280 }} />
          </Card>
        </Col>
      </Row>

      <Row gutter={[12, 12]} style={{ marginTop: 12 }}>
        <Col xs={24} lg={12}>
          <Card title="Top đơn vị có sáng kiến được công nhận" size="small">
            {(data?.topDonVi ?? []).map((x, i) => (
              <div
                key={x.ten}
                style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  padding: '6px 0',
                  borderBottom: i === (data?.topDonVi.length ?? 0) - 1 ? 'none' : '1px solid #f0f0f0',
                }}
              >
                <span>
                  {i + 1}. {x.ten}
                </span>
                <span>
                  <strong style={{ color: '#52c41a' }}>{x.giaTriPhu ?? 0}</strong>
                  <span style={{ color: '#888' }}> / {x.soLuong}</span>
                </span>
              </div>
            ))}
          </Card>
        </Col>
        <Col xs={24} lg={12}>
          <Card title="Cảnh báo" size="small">
            <Statistic
              title="Hồ sơ có tỷ lệ trùng lặp cao (> 40%)"
              value={data?.soCanhBaoTrungLapCao ?? 0}
              valueStyle={{
                color: (data?.soCanhBaoTrungLapCao ?? 0) > 0 ? '#ff4d4f' : '#52c41a',
              }}
            />
            <div style={{ marginTop: 12 }}>
              <Link to="/tra-cuu?trungLapTu=40">Xem danh sách hồ sơ nghi trùng lặp →</Link>
            </div>
          </Card>
        </Col>
      </Row>
    </div>
  );
}
