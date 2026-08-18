import { useEffect, useMemo, useState } from 'react';
import { useParams, useSearchParams } from 'react-router-dom';
import {
  App,
  Alert,
  Button,
  Card,
  Col,
  Divider,
  Input,
  InputNumber,
  List,
  Progress,
  Radio,
  Row,
  Slider,
  Space,
  Statistic,
  Tag,
  Tooltip,
  Typography,
} from 'antd';
import {
  InfoCircleOutlined,
  SafetyCertificateOutlined,
  SaveOutlined,
  SendOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { LoiApi } from '@/api/client';
import { apiDanhGia, apiSangKien } from '@/api/endpoints';
import { KhoiDangTai, KhoiLoi, ngayGio } from '@/components/ThanhPhanChung';

interface DongCham {
  tieuChiId: string;
  diem: number;
  mucDiemId?: string | null;
  nhanXet?: string | null;
}

/** Chức năng 34 — Màn hình chấm điểm 2 panel: nội dung hồ sơ | phiếu chấm động. */
export default function TrangChamDiem() {
  const { id = '' } = useParams<{ id: string }>();
  const [thamSo] = useSearchParams();
  const hoiDongId = thamSo.get('hoiDongId') ?? '';

  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [cham, setCham] = useState<Record<string, DongCham>>({});
  const [nhanXetChung, setNhanXetChung] = useState('');
  const [uuDiem, setUuDiem] = useState('');
  const [hanChe, setHanChe] = useState('');

  const phieu = useQuery({
    queryKey: ['phieu-cham', id, hoiDongId],
    queryFn: () => apiDanhGia.layPhieu(id, hoiDongId),
    enabled: !!id && !!hoiDongId,
  });

  const hoSo = useQuery({
    queryKey: ['sang-kien', id],
    queryFn: () => apiSangKien.chiTiet(id),
    enabled: !!id,
  });

  useEffect(() => {
    if (!phieu.data) return;

    const bang: Record<string, DongCham> = {};
    phieu.data.chiTiet.forEach((c) => {
      bang[c.tieuChiId] = {
        tieuChiId: c.tieuChiId,
        diem: c.diem,
        mucDiemId: c.mucDiemId,
        nhanXet: c.nhanXet,
      };
    });

    setCham(bang);
    setNhanXetChung(phieu.data.nhanXetChung ?? '');
    setUuDiem(phieu.data.uuDiem ?? '');
    setHanChe(phieu.data.hanChe ?? '');
  }, [phieu.data]);

  const bo = phieu.data?.boTieuChi;

  // Tính tổng điểm realtime ngay trên giao diện theo cách tính của bộ tiêu chí.
  const { tongDiem, diemTheoNhom } = useMemo(() => {
    if (!bo) return { tongDiem: 0, diemTheoNhom: {} as Record<string, number> };

    const theoNhom: Record<string, number> = {};
    bo.danhSachNhom.forEach((n) => {
      theoNhom[n.id] = n.danhSachTieuChi.reduce(
        (tong, t) => tong + (cham[t.id]?.diem ?? 0),
        0,
      );
    });

    const tong =
      bo.cachTinh === 'TRUNG_BINH_TRONG_SO'
        ? bo.danhSachNhom.reduce((s, n) => s + (theoNhom[n.id] * n.trongSo) / 100, 0)
        : Object.values(theoNhom).reduce((s, v) => s + v, 0);

    return { tongDiem: Number(tong.toFixed(bo.lamTron)), diemTheoNhom: theoNhom };
  }, [bo, cham]);

  const luu = useMutation({
    mutationFn: (guiChinhThuc: boolean) => {
      const duLieu = {
        sangKienId: id,
        hoiDongId,
        chiTiet: Object.values(cham),
        nhanXetChung,
        uuDiem,
        hanChe,
      };

      return guiChinhThuc ? apiDanhGia.gui(duLieu) : apiDanhGia.luuNhap(duLieu);
    },
    onSuccess: (_kq, guiChinhThuc) => {
      message.success(guiChinhThuc ? 'Đã gửi phiếu đánh giá' : 'Đã lưu nháp');
      void queryClient.invalidateQueries({ queryKey: ['phieu-cham', id, hoiDongId] });
      void queryClient.invalidateQueries({ queryKey: ['danh-gia'] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không lưu được phiếu.'),
  });

  // Chỉ hỏi lịch sử ký khi phiếu đã gửi — phiếu đang chấm chắc chắn chưa có chữ ký nào.
  const lichSuKy = useQuery({
    queryKey: ['lich-su-ky-phieu', phieu.data?.id],
    queryFn: () => apiDanhGia.lichSuKySoPhieu(phieu.data!.id),
    enabled: !!phieu.data?.id && phieu.data.trangThaiPhieu !== 'DANG_CHAM',
  });

  const kySo = useMutation({
    mutationFn: () => apiDanhGia.kySoPhieu(phieu.data!.id),
    onSuccess: () => {
      message.success('Đã ký số phiếu đánh giá');
      void queryClient.invalidateQueries({ queryKey: ['phieu-cham', id, hoiDongId] });
      void queryClient.invalidateQueries({ queryKey: ['lich-su-ky-phieu'] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không ký số được.'),
  });

  if (phieu.isLoading || hoSo.isLoading) return <KhoiDangTai />;
  if (phieu.error) return <KhoiLoi loi={phieu.error} thuLai={phieu.refetch} />;
  if (!bo) return <Alert type="error" message="Chưa cấu hình bộ tiêu chí cho hội đồng này." />;

  const choPhepSua = phieu.data?.choPhepSua ?? true;
  const dat = tongDiem >= bo.diemDatToiThieu;

  return (
    <Row gutter={12}>
      {/* Panel trái — nội dung hồ sơ */}
      <Col xs={24} lg={11}>
        <Card
          title={`${hoSo.data?.maHoSo} — Nội dung sáng kiến`}
          size="small"
          style={{ maxHeight: 'calc(100vh - 140px)', overflow: 'auto' }}
        >
          <Typography.Title level={5}>{hoSo.data?.tenSangKien}</Typography.Title>

          {hoSo.data?.tyLeTrungLap !== null && hoSo.data?.tyLeTrungLap !== undefined && (
            <Alert
              type={
                hoSo.data.tyLeTrungLap > 40
                  ? 'error'
                  : hoSo.data.tyLeTrungLap >= 20
                    ? 'warning'
                    : 'success'
              }
              showIcon
              style={{ marginBottom: 12 }}
              message={`Tỷ lệ trùng lặp: ${hoSo.data.tyLeTrungLap.toFixed(2)}%`}
            />
          )}

          {[
            ['Tình trạng trước khi áp dụng', hoSo.data?.tinhTrangTruocKhiApDung],
            ['Mô tả giải pháp', hoSo.data?.moTaGiaiPhap],
            ['Nội dung giải pháp', hoSo.data?.noiDungGiaiPhap],
            ['Tính mới', hoSo.data?.tinhMoi],
            ['Khả năng áp dụng', hoSo.data?.khaNangApDung],
            ['Hiệu quả kinh tế', hoSo.data?.hieuQuaKinhTe],
            ['Hiệu quả xã hội', hoSo.data?.hieuQuaXaHoi],
          ]
            .filter(([, giaTri]) => giaTri)
            .map(([nhan, giaTri]) => (
              <div key={nhan as string} style={{ marginBottom: 12 }}>
                <Typography.Text strong>{nhan}</Typography.Text>
                <Typography.Paragraph style={{ whiteSpace: 'pre-wrap', marginBottom: 0 }}>
                  {giaTri}
                </Typography.Paragraph>
              </div>
            ))}

          {(hoSo.data?.tepDinhKem.length ?? 0) > 0 && (
            <>
              <Divider>Tệp đính kèm</Divider>
              {hoSo.data!.tepDinhKem.map((t) => (
                <div key={t.id}>
                  <a href={`/api/v1/tep-tin/${t.tepTinId}/tai-ve`} download>
                    {t.tenGoc}
                  </a>
                </div>
              ))}
            </>
          )}
        </Card>
      </Col>

      {/* Panel phải — phiếu chấm render động từ bộ tiêu chí */}
      <Col xs={24} lg={13}>
        <Card
          title="Phiếu đánh giá"
          size="small"
          extra={
            <Space>
              <Statistic
                value={tongDiem}
                suffix={`/ ${bo.thangDiemToiDa}`}
                valueStyle={{ fontSize: 20, color: dat ? '#52c41a' : '#ff4d4f' }}
              />
              <Tag color={dat ? 'success' : 'error'}>{dat ? 'ĐẠT' : 'CHƯA ĐẠT'}</Tag>
            </Space>
          }
        >
          {!choPhepSua && (
            <Alert
              type="info"
              showIcon
              style={{ marginBottom: 12 }}
              message="Phiếu đã gửi"
              description="Chỉ thư ký hội đồng mới mở lại được phiếu này."
            />
          )}

          <Progress
            percent={Math.round((tongDiem / bo.thangDiemToiDa) * 100)}
            strokeColor={dat ? '#52c41a' : '#faad14'}
            style={{ marginBottom: 16 }}
          />

          {bo.danhSachNhom.map((nhom) => (
            <Card
              key={nhom.id}
              size="small"
              style={{ marginBottom: 12 }}
              title={
                <Space>
                  {nhom.ten}
                  <Tag>
                    {diemTheoNhom[nhom.id]?.toFixed(1) ?? 0} / {nhom.diemToiDa}
                  </Tag>
                  {bo.cachTinh === 'TRUNG_BINH_TRONG_SO' && (
                    <Tag color="blue">trọng số {nhom.trongSo}%</Tag>
                  )}
                </Space>
              }
            >
              {nhom.danhSachTieuChi.map((tc) => (
                <div key={tc.id} style={{ marginBottom: 16 }}>
                  <Space align="start" style={{ width: '100%', justifyContent: 'space-between' }}>
                    <Space>
                      <Typography.Text>{tc.ten}</Typography.Text>
                      {tc.huongDanCham && (
                        <Tooltip title={tc.huongDanCham}>
                          <InfoCircleOutlined style={{ color: '#1677ff' }} />
                        </Tooltip>
                      )}
                      {tc.batBuocNhanXet && <Tag color="orange">bắt buộc nhận xét</Tag>}
                    </Space>
                    <Typography.Text type="secondary">tối đa {tc.diemToiDa}</Typography.Text>
                  </Space>

                  {tc.kieuNhap === 'LUA_CHON' && tc.danhSachMucDiem.length > 0 ? (
                    <Radio.Group
                      disabled={!choPhepSua}
                      value={cham[tc.id]?.mucDiemId}
                      onChange={(e) => {
                        const muc = tc.danhSachMucDiem.find((m) => m.id === e.target.value);
                        datCham(tc.id, { mucDiemId: e.target.value, diem: muc?.diem ?? 0 });
                      }}
                      style={{ marginTop: 8 }}
                    >
                      {tc.danhSachMucDiem.map((m) => (
                        <Radio key={m.id} value={m.id}>
                          {m.ten} ({m.diem})
                        </Radio>
                      ))}
                    </Radio.Group>
                  ) : tc.kieuNhap === 'CO_KHONG' ? (
                    <Radio.Group
                      disabled={!choPhepSua}
                      value={cham[tc.id]?.diem ?? 0}
                      onChange={(e) => datCham(tc.id, { diem: e.target.value })}
                      style={{ marginTop: 8 }}
                    >
                      <Radio value={tc.diemToiDa}>Có ({tc.diemToiDa})</Radio>
                      <Radio value={0}>Không (0)</Radio>
                    </Radio.Group>
                  ) : (
                    <Row gutter={8} align="middle" style={{ marginTop: 8 }}>
                      <Col flex="auto">
                        <Slider
                          disabled={!choPhepSua}
                          min={0}
                          max={tc.diemToiDa}
                          step={tc.buocNhay || 0.5}
                          value={cham[tc.id]?.diem ?? 0}
                          onChange={(v) => datCham(tc.id, { diem: v })}
                        />
                      </Col>
                      <Col flex="90px">
                        <InputNumber
                          disabled={!choPhepSua}
                          min={0}
                          max={tc.diemToiDa}
                          step={tc.buocNhay || 0.5}
                          value={cham[tc.id]?.diem ?? 0}
                          style={{ width: '100%' }}
                          onChange={(v) => datCham(tc.id, { diem: Number(v) || 0 })}
                        />
                      </Col>
                    </Row>
                  )}

                  <Input.TextArea
                    disabled={!choPhepSua}
                    rows={2}
                    placeholder="Nhận xét cho tiêu chí này..."
                    value={cham[tc.id]?.nhanXet ?? ''}
                    onChange={(e) => datCham(tc.id, { nhanXet: e.target.value })}
                    style={{ marginTop: 8 }}
                    status={tc.batBuocNhanXet && !cham[tc.id]?.nhanXet ? 'warning' : undefined}
                  />
                </div>
              ))}
            </Card>
          ))}

          <Card size="small" title="Nhận xét chung" style={{ marginBottom: 12 }}>
            <Input.TextArea
              disabled={!choPhepSua}
              rows={3}
              placeholder="Nhận xét chung về sáng kiến"
              value={nhanXetChung}
              onChange={(e) => setNhanXetChung(e.target.value)}
              style={{ marginBottom: 8 }}
            />
            <Input.TextArea
              disabled={!choPhepSua}
              rows={2}
              placeholder="Ưu điểm"
              value={uuDiem}
              onChange={(e) => setUuDiem(e.target.value)}
              style={{ marginBottom: 8 }}
            />
            <Input.TextArea
              disabled={!choPhepSua}
              rows={2}
              placeholder="Hạn chế"
              value={hanChe}
              onChange={(e) => setHanChe(e.target.value)}
            />
          </Card>

          <Space style={{ width: '100%', justifyContent: 'flex-end' }}>
            <Button
              icon={<SaveOutlined />}
              disabled={!choPhepSua}
              loading={luu.isPending}
              onClick={() => luu.mutate(false)}
            >
              Lưu nháp
            </Button>
            <Button
              type="primary"
              icon={<SendOutlined />}
              disabled={!choPhepSua}
              loading={luu.isPending}
              onClick={() => luu.mutate(true)}
            >
              Gửi phiếu đánh giá
            </Button>

            {/* Chỉ ký được phiếu đã gửi: ký một phiếu còn sửa được thì chữ ký vô nghĩa. */}
            {phieu.data && phieu.data.trangThaiPhieu !== 'DANG_CHAM' && (
              <Button
                icon={<SafetyCertificateOutlined />}
                loading={kySo.isPending}
                onClick={() =>
                  modal.confirm({
                    title: 'Ký số phiếu đánh giá',
                    content:
                      'Hệ thống chốt phiếu thành bản PDF rồi ký số lên bản đó. '
                      + 'Sau khi ký, sửa phiếu sẽ làm chữ ký không còn khớp nội dung.',
                    okText: 'Ký số',
                    cancelText: 'Huỷ',
                    onOk: () => kySo.mutateAsync(),
                  })
                }
              >
                Ký số phiếu
              </Button>
            )}
          </Space>

          {(lichSuKy.data?.length ?? 0) > 0 && (
            <Card size="small" title="Lịch sử ký số phiếu" style={{ marginTop: 12 }}>
              <List
                size="small"
                dataSource={lichSuKy.data ?? []}
                renderItem={(x) => (
                  <List.Item>
                    <Space direction="vertical" size={0} style={{ width: '100%' }}>
                      <Space wrap>
                        <Tag color={x.trangThaiKy === 'THANH_CONG' ? 'success' : 'error'}>
                          {x.trangThaiKy === 'THANH_CONG' ? 'Ký thành công' : 'Ký thất bại'}
                        </Tag>
                        <span>{ngayGio(x.thoiGianKy)}</span>
                      </Space>
                      <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                        Chứng thư: {x.nguoiCapChungThu ?? '—'}
                        {x.serialChungThu ? ` · Serial ${x.serialChungThu}` : ''}
                      </Typography.Text>
                    </Space>
                  </List.Item>
                )}
              />
              <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                Chữ ký gắn với bản PDF chốt tại thời điểm ký. Mở lại phiếu và sửa điểm sẽ làm chữ
                ký cũ không còn khớp nội dung hiện tại.
              </Typography.Text>
            </Card>
          )}
        </Card>
      </Col>
    </Row>
  );

  function datCham(tieuChiId: string, thayDoi: Partial<DongCham>) {
    setCham((cu) => ({
      ...cu,
      [tieuChiId]: { ...{ diem: 0 }, ...cu[tieuChiId], ...thayDoi, tieuChiId },
    }));
  }
}
