import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  App,
  Alert,
  Badge,
  Button,
  Card,
  Col,
  Descriptions,
  Form,
  Input,
  Modal,
  Progress,
  Row,
  Space,
  Statistic,
  Table,
  Tabs,
  Tag,
  Timeline,
  Tooltip,
  Typography,
} from 'antd';
import {
  DownloadOutlined,
  EditOutlined,
  ReloadOutlined,
  RollbackOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { LoiApi } from '@/api/client';
import {
  apiSangKien,
  apiXuLy,
  type HanhDongKhaDung,
  type SangKienChiTiet,
} from '@/api/endpoints';
import {
  HienThiHan,
  KhoiDangTai,
  KhoiLoi,
  NhanTrangThai,
  NhanTrungLap,
  ngayGio,
} from '@/components/ThanhPhanChung';

export default function TrangChiTietHoSo() {
  const { id = '' } = useParams<{ id: string }>();
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [hanhDongDangChon, setHanhDongDangChon] = useState<HanhDongKhaDung | null>(null);
  const [formXuLy] = Form.useForm<{ yKien: string }>();

  const chiTiet = useQuery({
    queryKey: ['sang-kien', id],
    queryFn: () => apiSangKien.chiTiet(id),
  });

  const hanhDong = useQuery({
    queryKey: ['sang-kien', id, 'hanh-dong'],
    queryFn: () => apiSangKien.hanhDong(id),
  });

  const tienDo = useQuery({
    queryKey: ['sang-kien', id, 'tien-do'],
    queryFn: () => apiSangKien.tienDo(id),
  });

  const lichSu = useQuery({
    queryKey: ['sang-kien', id, 'lich-su'],
    queryFn: () => apiSangKien.lichSu(id),
  });

  const trungLap = useQuery({
    queryKey: ['sang-kien', id, 'trung-lap'],
    queryFn: () => apiSangKien.trungLap(id),
  });

  const thucThi = useMutation({
    mutationFn: (duLieu: { truongHopId: string; yKien?: string }) =>
      apiXuLy.thucThi({
        sangKienId: id,
        truongHopId: duLieu.truongHopId,
        yKien: duLieu.yKien,
        phienBanHoSo: chiTiet.data?.phienBan,
      }),
    onSuccess: (ketQua) => {
      message.success(ketQua.thongBao);
      setHanhDongDangChon(null);
      formXuLy.resetFields();
      void queryClient.invalidateQueries({ queryKey: ['sang-kien', id] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Xử lý thất bại.'),
  });

  const chayLaiTrungLap = useMutation({
    mutationFn: () => apiSangKien.chayLaiTrungLap(id),
    onSuccess: () => {
      message.success('Đã hoàn tất kiểm tra trùng lặp');
      void queryClient.invalidateQueries({ queryKey: ['sang-kien', id] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Kiểm tra thất bại.'),
  });

  const rutHoSo = useMutation({
    mutationFn: (lyDo: string) => apiSangKien.rut(id, lyDo),
    onSuccess: () => {
      message.success('Đã rút hồ sơ');
      void queryClient.invalidateQueries({ queryKey: ['sang-kien', id] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không rút được hồ sơ.'),
  });

  if (chiTiet.isLoading) return <KhoiDangTai />;
  if (chiTiet.error) return <KhoiLoi loi={chiTiet.error} thuLai={chiTiet.refetch} />;

  const hs = chiTiet.data!;

  return (
    <div>
      <Card
        title={
          <Space wrap>
            <Typography.Text strong>{hs.maHoSo}</Typography.Text>
            <NhanTrangThai trangThai={hs.trangThaiTong} />
            {hs.tenBuocHienTai && <Tag color="blue">{hs.tenBuocHienTai}</Tag>}
          </Space>
        }
        extra={
          <Space wrap className="khong-in">
            {hs.choPhepSua && (
              <Link to={`/sang-kien/${id}/sua`}>
                <Button icon={<EditOutlined />}>Sửa hồ sơ</Button>
              </Link>
            )}
            {hs.choPhepRut && (
              <Button
                danger
                icon={<RollbackOutlined />}
                onClick={() =>
                  modal.confirm({
                    title: 'Rút hồ sơ',
                    content: (
                      <Input.TextArea id="ly-do-rut" rows={3} placeholder="Nhập lý do rút hồ sơ" />
                    ),
                    okText: 'Rút hồ sơ',
                    cancelText: 'Hủy',
                    onOk: () => {
                      const o = document.getElementById('ly-do-rut') as HTMLTextAreaElement | null;
                      const lyDo = o?.value?.trim();
                      if (!lyDo) {
                        message.error('Vui lòng nhập lý do rút hồ sơ.');
                        return Promise.reject();
                      }
                      return rutHoSo.mutateAsync(lyDo);
                    },
                  })
                }
              >
                Rút hồ sơ
              </Button>
            )}
          </Space>
        }
      >
        <Typography.Title level={5} style={{ marginTop: 0 }}>
          {hs.tenSangKien}
        </Typography.Title>

        {hs.dangKhoa && (
          <Alert
            type="warning"
            showIcon
            style={{ marginBottom: 12 }}
            message="Hồ sơ đang bị khóa"
            description={hs.lyDoKhoa}
          />
        )}

        <Row gutter={[12, 12]}>
          <Col xs={12} md={6}>
            <Statistic title="Tổng điểm" value={hs.tongDiem ?? 0} precision={2} />
          </Col>
          <Col xs={12} md={6}>
            <Statistic
              title="Mức công nhận"
              value={hs.tenMucCongNhan ?? '—'}
              valueStyle={{ fontSize: 18 }}
            />
          </Col>
          <Col xs={12} md={6}>
            <div>
              <Typography.Text type="secondary">Trùng lặp</Typography.Text>
              <div style={{ marginTop: 4 }}>
                <NhanTrungLap tyLe={hs.tyLeTrungLap} />
              </div>
            </div>
          </Col>
          <Col xs={12} md={6}>
            <div>
              <Typography.Text type="secondary">Hạn xử lý</Typography.Text>
              <div style={{ marginTop: 4 }}>
                <HienThiHan
                  han={hs.hanXuLyHienTai}
                  quaHan={!!hs.hanXuLyHienTai && new Date(hs.hanXuLyHienTai) < new Date()}
                />
              </div>
            </div>
          </Col>
        </Row>

        {/* Nút hành động sinh động theo quy trình — KHÔNG hardcode. */}
        {(hanhDong.data?.length ?? 0) > 0 && (
          <Card size="small" title="Xử lý hồ sơ" style={{ marginTop: 16 }} className="khong-in">
            <Space wrap>
              {hanhDong.data!.map((hd) => (
                <Tooltip key={hd.truongHopId} title={hd.biChan ? hd.lyDoChan : hd.tenBuocTiepTheo}>
                  <Button
                    type={hd.mauNut === 'primary' ? 'primary' : 'default'}
                    danger={hd.mauNut === 'danger'}
                    disabled={hd.biChan}
                    onClick={() => setHanhDongDangChon(hd)}
                  >
                    {hd.ten}
                  </Button>
                </Tooltip>
              ))}
            </Space>
          </Card>
        )}
      </Card>

      <Card style={{ marginTop: 12 }}>
        <Tabs
          defaultActiveKey="noi-dung"
          items={[
            {
              key: 'noi-dung',
              label: 'Nội dung',
              children: <TabNoiDung hs={hs} />,
            },
            {
              key: 'tep',
              label: <Badge count={hs.tepDinhKem.length} size="small" offset={[10, 0]}>Tệp đính kèm</Badge>,
              children: <TabTepDinhKem hs={hs} />,
            },
            {
              key: 'tien-do',
              label: 'Tiến độ xử lý',
              children: <TabTienDo duLieu={tienDo.data ?? []} dangTai={tienDo.isLoading} />,
            },
            {
              key: 'lich-su',
              label: 'Lịch sử chỉnh sửa',
              children: <TabLichSu duLieu={lichSu.data ?? []} dangTai={lichSu.isLoading} />,
            },
            {
              key: 'trung-lap',
              label: 'Kiểm tra trùng lặp',
              children: (
                <TabTrungLap
                  duLieu={trungLap.data ?? null}
                  dangTai={trungLap.isLoading || chayLaiTrungLap.isPending}
                  onChayLai={() => chayLaiTrungLap.mutate()}
                />
              ),
            },
          ]}
        />
      </Card>

      <Modal
        open={!!hanhDongDangChon}
        title={hanhDongDangChon?.ten}
        okText="Xác nhận"
        cancelText="Hủy"
        confirmLoading={thucThi.isPending}
        onCancel={() => setHanhDongDangChon(null)}
        onOk={async () => {
          const giaTri = await formXuLy.validateFields();
          thucThi.mutate({ truongHopId: hanhDongDangChon!.truongHopId, yKien: giaTri.yKien });
        }}
      >
        {hanhDongDangChon?.tenBuocTiepTheo && (
          <Alert
            type="info"
            showIcon
            style={{ marginBottom: 12 }}
            message={`Hồ sơ sẽ chuyển sang bước: ${hanhDongDangChon.tenBuocTiepTheo}`}
          />
        )}

        <Form form={formXuLy} layout="vertical">
          <Form.Item
            name="yKien"
            label="Ý kiến xử lý"
            rules={
              hanhDongDangChon?.batBuocNhapYKien
                ? [{ required: true, message: 'Bước này bắt buộc nhập ý kiến xử lý' }]
                : undefined
            }
          >
            <Input.TextArea rows={4} placeholder="Nhập ý kiến xử lý..." />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}

function TabNoiDung({ hs }: { hs: SangKienChiTiet }) {
  const muc: { nhan: string; giaTri?: string | null }[] = [
    { nhan: 'Tình trạng trước khi áp dụng', giaTri: hs.tinhTrangTruocKhiApDung },
    { nhan: 'Mô tả giải pháp', giaTri: hs.moTaGiaiPhap },
    { nhan: 'Nội dung giải pháp', giaTri: hs.noiDungGiaiPhap },
    { nhan: 'Tính mới', giaTri: hs.tinhMoi },
    { nhan: 'Khả năng áp dụng', giaTri: hs.khaNangApDung },
    { nhan: 'Phạm vi áp dụng', giaTri: hs.phamViApDung },
    { nhan: 'Hiệu quả kinh tế', giaTri: hs.hieuQuaKinhTe },
    { nhan: 'Hiệu quả xã hội', giaTri: hs.hieuQuaXaHoi },
  ];

  return (
    <div>
      <Descriptions bordered size="small" column={{ xs: 1, md: 2 }} style={{ marginBottom: 16 }}>
        <Descriptions.Item label="Đợt đề nghị">{hs.tenDot}</Descriptions.Item>
        <Descriptions.Item label="Lĩnh vực">{hs.tenLinhVuc}</Descriptions.Item>
        <Descriptions.Item label="Đối tượng">{hs.tenDoiTuong ?? '—'}</Descriptions.Item>
        <Descriptions.Item label="Đơn vị">{hs.tenDonVi ?? '—'}</Descriptions.Item>
        <Descriptions.Item label="Ngày nộp">{ngayGio(hs.ngayNop)}</Descriptions.Item>
        <Descriptions.Item label="Ngày công nhận">
          {ngayGio(hs.ngayCongNhan, false)}
        </Descriptions.Item>
        <Descriptions.Item label="Giá trị làm lợi ước tính" span={2}>
          {hs.giaTriLamLoiUocTinh
            ? `${hs.giaTriLamLoiUocTinh.toLocaleString('vi-VN')} đ`
            : '—'}
        </Descriptions.Item>
      </Descriptions>

      <Card size="small" title="Tác giả" style={{ marginBottom: 16 }}>
        <Table
          rowKey={(_, i) => String(i)}
          size="small"
          pagination={false}
          dataSource={hs.danhSachTacGia}
          columns={[
            {
              title: 'Họ và tên',
              dataIndex: 'hoTen',
              render: (giaTri: string, dong) => (
                <Space>
                  {giaTri}
                  {dong.laTacGiaChinh && <Tag color="blue">Tác giả chính</Tag>}
                </Space>
              ),
            },
            { title: 'Chức vụ', dataIndex: 'chucVu' },
            { title: 'Đơn vị công tác', dataIndex: 'donViCongTac' },
            {
              title: 'Tỷ lệ đóng góp',
              dataIndex: 'tyLeDongGop',
              width: 140,
              align: 'right',
              render: (giaTri: number) => `${giaTri}%`,
            },
          ]}
        />
      </Card>

      {muc
        .filter((m) => m.giaTri)
        .map((m) => (
          <Card key={m.nhan} size="small" title={m.nhan} style={{ marginBottom: 12 }}>
            <Typography.Paragraph style={{ whiteSpace: 'pre-wrap', marginBottom: 0 }}>
              {m.giaTri}
            </Typography.Paragraph>
          </Card>
        ))}
    </div>
  );
}

function TabTepDinhKem({ hs }: { hs: SangKienChiTiet }) {
  return (
    <Table
      rowKey="id"
      size="small"
      dataSource={hs.tepDinhKem}
      columns={[
        { title: 'Tên tệp', dataIndex: 'tenGoc' },
        { title: 'Thành phần', dataIndex: 'thanhPhanHoSoMa', width: 200 },
        {
          title: 'Dung lượng',
          dataIndex: 'kichThuoc',
          width: 130,
          align: 'right',
          render: (giaTri: number) => `${(giaTri / 1024).toFixed(1)} KB`,
        },
        {
          title: 'Ngày tải lên',
          dataIndex: 'ngayTaiLen',
          width: 160,
          render: (giaTri: string) => ngayGio(giaTri),
        },
        {
          title: '',
          width: 60,
          render: (_giaTri, dong) => (
            <a href={`/api/v1/tep-tin/${dong.tepTinId}/tai-ve`} download>
              <Button type="text" icon={<DownloadOutlined />} />
            </a>
          ),
        },
      ]}
    />
  );
}

function TabTienDo({
  duLieu,
  dangTai,
}: {
  duLieu: { id: string; tenBuoc: string; tenTruongHop?: string | null; nguoiXuLy?: string | null; yKien?: string | null; thoiGianNhan: string; hanXuLy?: string | null; thoiGianXuLy?: string | null; soNgayXuLy?: number | null; quaHan: boolean }[];
  dangTai: boolean;
}) {
  if (dangTai) return <KhoiDangTai soDong={4} />;

  return (
    <Timeline
      mode="left"
      items={duLieu.map((m) => ({
        color: m.thoiGianXuLy ? (m.quaHan ? 'red' : 'green') : 'blue',
        children: (
          <div>
            <Typography.Text strong>{m.tenBuoc}</Typography.Text>
            {m.tenTruongHop && <Tag style={{ marginLeft: 8 }}>{m.tenTruongHop}</Tag>}
            {m.quaHan && <Tag color="red">Quá hạn</Tag>}
            <div style={{ fontSize: 13, color: '#666', marginTop: 4 }}>
              Nhận: {ngayGio(m.thoiGianNhan)}
              {m.hanXuLy && ` • Hạn: ${ngayGio(m.hanXuLy)}`}
              {m.thoiGianXuLy && ` • Xử lý: ${ngayGio(m.thoiGianXuLy)}`}
              {m.soNgayXuLy !== null && m.soNgayXuLy !== undefined && ` • ${m.soNgayXuLy} ngày`}
            </div>
            {m.nguoiXuLy && (
              <div style={{ fontSize: 13, color: '#666' }}>Người xử lý: {m.nguoiXuLy}</div>
            )}
            {m.yKien && (
              <Typography.Paragraph style={{ marginTop: 4, marginBottom: 0, fontStyle: 'italic' }}>
                “{m.yKien}”
              </Typography.Paragraph>
            )}
          </div>
        ),
      }))}
    />
  );
}

function TabLichSu({
  duLieu,
  dangTai,
}: {
  duLieu: { id: string; hanhDong: string; truongThayDoi: string[]; giaTriTruoc?: Record<string, string | null> | null; giaTriSau?: Record<string, string | null> | null; thoiGian: string; ghiChu?: string | null }[];
  dangTai: boolean;
}) {
  if (dangTai) return <KhoiDangTai soDong={4} />;

  return (
    <Table
      rowKey="id"
      size="small"
      dataSource={duLieu}
      expandable={{
        rowExpandable: (dong) => dong.truongThayDoi.length > 0,
        expandedRowRender: (dong) => (
          <Table
            size="small"
            pagination={false}
            rowKey={(t) => t}
            dataSource={dong.truongThayDoi}
            columns={[
              { title: 'Trường', render: (t: string) => t, width: 220 },
              {
                title: 'Giá trị trước',
                render: (t: string) => (
                  <Typography.Text type="secondary" ellipsis={{ tooltip: dong.giaTriTruoc?.[t] }}>
                    {dong.giaTriTruoc?.[t] ?? '—'}
                  </Typography.Text>
                ),
              },
              {
                title: 'Giá trị sau',
                render: (t: string) => (
                  <Typography.Text ellipsis={{ tooltip: dong.giaTriSau?.[t] }}>
                    {dong.giaTriSau?.[t] ?? '—'}
                  </Typography.Text>
                ),
              },
            ]}
          />
        ),
      }}
      columns={[
        { title: 'Hành động', dataIndex: 'hanhDong', width: 140 },
        {
          title: 'Số trường thay đổi',
          dataIndex: 'truongThayDoi',
          width: 160,
          render: (giaTri: string[]) => giaTri.length,
        },
        { title: 'Ghi chú', dataIndex: 'ghiChu' },
        {
          title: 'Thời gian',
          dataIndex: 'thoiGian',
          width: 170,
          render: (giaTri: string) => ngayGio(giaTri),
        },
      ]}
    />
  );
}

function TabTrungLap({
  duLieu,
  dangTai,
  onChayLai,
}: {
  duLieu: {
    tongSoDoiChieu: number;
    tyLeCaoNhat: number;
    mucCanhBao: string;
    thoiGianXuLyMs: number;
    ngayChay: string;
    chiTiet: {
      sangKienDoiChieuId: string;
      tyLeTuongDong: number;
      tyLeTuVung: number;
      tyLeNguNghia: number;
      soDoanTrung: number;
      cacDoanTrung: { doanNguon: string; doanDich: string; tyLe: number }[];
    }[];
  } | null;
  dangTai: boolean;
  onChayLai: () => void;
}) {
  const [capDangXem, setCapDangXem] = useState<number | null>(null);

  if (dangTai) return <KhoiDangTai soDong={4} />;

  if (!duLieu) {
    return (
      <Alert
        type="info"
        showIcon
        message="Hồ sơ chưa được kiểm tra trùng lặp"
        action={
          <Button icon={<ReloadOutlined />} onClick={onChayLai}>
            Chạy kiểm tra
          </Button>
        }
      />
    );
  }

  const mauTien =
    duLieu.mucCanhBao === 'NGHIEM_TRONG'
      ? '#ff4d4f'
      : duLieu.mucCanhBao === 'CANH_BAO'
        ? '#faad14'
        : '#52c41a';

  return (
    <div>
      <Row gutter={[12, 12]} align="middle" style={{ marginBottom: 16 }}>
        <Col xs={24} md={8}>
          <Progress
            type="dashboard"
            percent={Math.round(duLieu.tyLeCaoNhat)}
            strokeColor={mauTien}
            format={(p) => (
              <div>
                <div style={{ fontSize: 22 }}>{p}%</div>
                <div style={{ fontSize: 12, color: '#888' }}>trùng lặp cao nhất</div>
              </div>
            )}
          />
        </Col>
        <Col xs={24} md={16}>
          <Descriptions size="small" column={1} bordered>
            <Descriptions.Item label="Mức cảnh báo">
              <Tag color={mauTien === '#ff4d4f' ? 'error' : mauTien === '#faad14' ? 'warning' : 'success'}>
                {duLieu.mucCanhBao.replace('_', ' ')}
              </Tag>
            </Descriptions.Item>
            <Descriptions.Item label="Số hồ sơ đối chiếu">{duLieu.tongSoDoiChieu}</Descriptions.Item>
            <Descriptions.Item label="Thời gian xử lý">{duLieu.thoiGianXuLyMs} ms</Descriptions.Item>
            <Descriptions.Item label="Lần chạy gần nhất">{ngayGio(duLieu.ngayChay)}</Descriptions.Item>
          </Descriptions>
          <Button
            icon={<ReloadOutlined />}
            style={{ marginTop: 12 }}
            onClick={onChayLai}
            className="khong-in"
          >
            Chạy lại kiểm tra
          </Button>
        </Col>
      </Row>

      <Table
        rowKey="sangKienDoiChieuId"
        size="small"
        dataSource={duLieu.chiTiet}
        onRow={(_dong, chiSo) => ({ onClick: () => setCapDangXem(chiSo ?? null) })}
        columns={[
          {
            title: 'Hồ sơ đối chiếu',
            dataIndex: 'sangKienDoiChieuId',
            render: (giaTri: string) => <Link to={`/sang-kien/${giaTri}`}>Xem hồ sơ</Link>,
          },
          {
            title: 'Tỷ lệ tổng hợp',
            dataIndex: 'tyLeTuongDong',
            width: 150,
            sorter: (a, b) => a.tyLeTuongDong - b.tyLeTuongDong,
            defaultSortOrder: 'descend',
            render: (giaTri: number) => (
              <Progress
                percent={Math.round(giaTri)}
                size="small"
                strokeColor={giaTri > 40 ? '#ff4d4f' : giaTri >= 20 ? '#faad14' : '#52c41a'}
              />
            ),
          },
          {
            title: 'Từ vựng',
            dataIndex: 'tyLeTuVung',
            width: 100,
            render: (giaTri: number) => `${giaTri.toFixed(1)}%`,
          },
          {
            title: 'Ngữ nghĩa',
            dataIndex: 'tyLeNguNghia',
            width: 100,
            render: (giaTri: number) => `${giaTri.toFixed(1)}%`,
          },
          { title: 'Số đoạn trùng', dataIndex: 'soDoanTrung', width: 130, align: 'right' },
        ]}
      />

      <Modal
        open={capDangXem !== null}
        title="Đối chiếu đoạn trùng lặp"
        width={1000}
        footer={null}
        onCancel={() => setCapDangXem(null)}
      >
        {capDangXem !== null &&
          duLieu.chiTiet[capDangXem]?.cacDoanTrung.map((doan, i) => (
            <Card key={i} size="small" style={{ marginBottom: 12 }}
              title={`Đoạn ${i + 1} — tương đồng ${doan.tyLe.toFixed(1)}%`}
            >
              <div className="doi-chieu-trung-lap">
                <div>
                  <Typography.Text type="secondary">Hồ sơ này</Typography.Text>
                  <Typography.Paragraph className="doan-trung" style={{ whiteSpace: 'pre-wrap' }}>
                    {doan.doanNguon}
                  </Typography.Paragraph>
                </div>
                <div>
                  <Typography.Text type="secondary">Hồ sơ đối chiếu</Typography.Text>
                  <Typography.Paragraph className="doan-trung" style={{ whiteSpace: 'pre-wrap' }}>
                    {doan.doanDich}
                  </Typography.Paragraph>
                </div>
              </div>
            </Card>
          ))}

        {capDangXem !== null && duLieu.chiTiet[capDangXem]?.cacDoanTrung.length === 0 && (
          <Alert
            type="success"
            showIcon
            message="Không có đoạn văn nào trùng nhau vượt ngưỡng"
            description="Tỷ lệ tương đồng đến từ mức độ giống nhau chung của toàn văn, không phải sao chép nguyên văn."
          />
        )}
      </Modal>
    </div>
  );
}
