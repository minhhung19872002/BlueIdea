import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  App,
  Alert,
  Button,
  Card,
  Col,
  DatePicker,
  Descriptions,
  Form,
  Input,
  InputNumber,
  Row,
  Select,
  Space,
  Statistic,
  Steps,
  Table,
  Tag,
  Typography,
  Upload,
} from 'antd';
import {
  CheckCircleOutlined,
  CloseCircleOutlined,
  DeleteOutlined,
  ExclamationCircleOutlined,
  PlusOutlined,
  SaveOutlined,
  SendOutlined,
  UploadOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';

import { boNhoToken, LoiApi, xoaDuLieu } from '@/api/client';
import {
  apiDoiTuong,
  apiDotDeNghi,
  apiLinhVuc,
  apiLoaiTacGia,
  apiSangKien,
  type NoiDungHoSo,
  type TacGia,
} from '@/api/endpoints';
import { KhoiDangTai } from '@/components/ThanhPhanChung';

/** Khoảng thời gian tự động lưu nháp (Mục 5 — chức năng 22). */
const CHU_KY_TU_LUU_MS = 30_000;

const BUOC = [
  { title: 'Đợt đề nghị' },
  { title: 'Thông tin chung' },
  { title: 'Tác giả' },
  { title: 'Nội dung' },
  { title: 'Tệp đính kèm' },
  { title: 'Xem lại & Nộp' },
];

export default function TrangNopHoSo() {
  const { id } = useParams<{ id: string }>();
  const dieuHuong = useNavigate();
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [buocHienTai, setBuocHienTai] = useState(0);
  const [hoSoId, setHoSoId] = useState<string | undefined>(id);
  const [form] = Form.useForm<NoiDungHoSo>();
  const [tacGia, setTacGia] = useState<TacGia[]>([
    { hoTen: '', tyLeDongGop: 100, laTacGiaChinh: true },
  ]);

  const hasThayDoi = useRef(false);

  const { data: cacDot } = useQuery({ queryKey: ['dot-dang-mo'], queryFn: apiDotDeNghi.dangMo });
  const { data: cacLinhVuc } = useQuery({ queryKey: ['linh-vuc-chon'], queryFn: apiLinhVuc.chon });
  const { data: cacDoiTuong } = useQuery({ queryKey: ['doi-tuong-chon'], queryFn: apiDoiTuong.chon });
  const { data: cacLoaiTacGia } = useQuery({
    queryKey: ['loai-tac-gia-chon'],
    queryFn: apiLoaiTacGia.chon,
  });

  const { data: chiTiet, isLoading } = useQuery({
    queryKey: ['sang-kien', hoSoId],
    queryFn: () => apiSangKien.chiTiet(hoSoId!),
    enabled: !!hoSoId,
  });

  // Nạp dữ liệu vào form khi mở hồ sơ đã có.
  useEffect(() => {
    if (!chiTiet) return;

    form.setFieldsValue({
      tenSangKien: chiTiet.tenSangKien,
      dotDeNghiId: chiTiet.dotDeNghiId,
      linhVucId: chiTiet.linhVucId,
      doiTuongId: chiTiet.doiTuongId,
      loaiTacGiaId: chiTiet.loaiTacGiaId,
      moTaGiaiPhap: chiTiet.moTaGiaiPhap,
      tinhTrangTruocKhiApDung: chiTiet.tinhTrangTruocKhiApDung,
      noiDungGiaiPhap: chiTiet.noiDungGiaiPhap,
      tinhMoi: chiTiet.tinhMoi,
      khaNangApDung: chiTiet.khaNangApDung,
      phamViApDung: chiTiet.phamViApDung,
      hieuQuaKinhTe: chiTiet.hieuQuaKinhTe,
      giaTriLamLoiUocTinh: chiTiet.giaTriLamLoiUocTinh,
      hieuQuaXaHoi: chiTiet.hieuQuaXaHoi,
    });

    if (chiTiet.danhSachTacGia.length > 0) {
      setTacGia(chiTiet.danhSachTacGia);
    }
  }, [chiTiet, form]);

  const luuNhap = useMutation({
    mutationFn: async () => {
      const giaTri = form.getFieldsValue();
      const duLieu: NoiDungHoSo = { ...giaTri, danhSachTacGia: tacGia };

      if (hoSoId) {
        await apiSangKien.capNhat(hoSoId, duLieu, chiTiet?.phienBan);
        return hoSoId;
      }

      const idMoi = await apiSangKien.tao(duLieu);
      setHoSoId(idMoi);
      return idMoi;
    },
    onSuccess: () => {
      hasThayDoi.current = false;
      void queryClient.invalidateQueries({ queryKey: ['sang-kien'] });
    },
  });

  const nopHoSo = useMutation({
    mutationFn: () => apiSangKien.nop(hoSoId!),
    onSuccess: (ketQua) => {
      modal.success({
        title: 'Nộp hồ sơ thành công',
        content: (
          <div>
            <p>
              Mã hồ sơ: <strong>{ketQua.maHoSo}</strong>
            </p>
            <p>Hồ sơ đã chuyển sang bước: {ketQua.tenBuocHienTai}</p>
          </div>
        ),
        onOk: () => dieuHuong(`/sang-kien/${hoSoId}`),
      });
    },
    onError: (loi) => {
      message.error(loi instanceof LoiApi ? loi.message : 'Không nộp được hồ sơ.');
    },
  });

  // Tự động lưu nháp định kỳ khi có thay đổi.
  useEffect(() => {
    const dinhKy = setInterval(() => {
      if (hasThayDoi.current && form.getFieldValue('tenSangKien')) {
        luuNhap.mutate();
      }
    }, CHU_KY_TU_LUU_MS);

    return () => clearInterval(dinhKy);
  }, [form, luuNhap]);

  const tongTyLe = useMemo(
    () => tacGia.reduce((tong, t) => tong + (Number(t.tyLeDongGop) || 0), 0),
    [tacGia],
  );

  const loaiTacGiaDaChon = Form.useWatch('loaiTacGiaId', form);
  const soTacGiaToiDa = useMemo(() => {
    const loai = cacLoaiTacGia?.find((l) => l.id === loaiTacGiaDaChon);
    // Mã danh mục cho biết có cho nhiều tác giả hay không.
    return loai?.ma === 'CA_NHAN' ? 1 : loai?.ma === 'NHOM_TAC_GIA' ? 5 : 10;
  }, [cacLoaiTacGia, loaiTacGiaDaChon]);

  const thanhPhanThieu = (chiTiet?.thanhPhanHoSo ?? []).filter(
    (t) => t.batBuoc && t.trangThai !== 'DU',
  );

  async function sangBuoc(buocMoi: number) {
    // Lưu nháp mỗi khi rời bước để không mất dữ liệu.
    if (buocMoi > buocHienTai) {
      try {
        await form.validateFields(truongCuaBuoc(buocHienTai));
      } catch {
        return;
      }

      if (buocHienTai === 2 && Math.abs(tongTyLe - 100) > 0.01) {
        message.error(`Tổng tỷ lệ đóng góp phải bằng 100%, hiện tại là ${tongTyLe}%.`);
        return;
      }

      await luuNhap.mutateAsync();
    }

    setBuocHienTai(buocMoi);
  }

  if (id && isLoading) return <KhoiDangTai />;

  return (
    <Card
      title={hoSoId ? `Chỉnh sửa hồ sơ ${chiTiet?.maHoSo ?? ''}` : 'Nộp sáng kiến mới'}
      extra={
        <Space>
          <Button
            icon={<SaveOutlined />}
            loading={luuNhap.isPending}
            onClick={() => luuNhap.mutate()}
          >
            Lưu nháp
          </Button>
        </Space>
      }
    >
      <Steps
        current={buocHienTai}
        items={BUOC}
        size="small"
        onChange={(b) => void sangBuoc(b)}
        style={{ marginBottom: 24 }}
        responsive
      />

      <Form<NoiDungHoSo>
        form={form}
        layout="vertical"
        requiredMark
        onValuesChange={() => {
          hasThayDoi.current = true;
        }}
      >
        {/* Bước 1 — Đợt đề nghị */}
        <div style={{ display: buocHienTai === 0 ? 'block' : 'none' }}>
          <Alert
            type="info"
            showIcon
            style={{ marginBottom: 16 }}
            message="Chỉ các đợt đang mở và còn hạn nộp mới hiển thị ở đây."
          />
          <Form.Item
            name="dotDeNghiId"
            label="Đợt đề nghị"
            rules={[{ required: true, message: 'Vui lòng chọn đợt đề nghị' }]}
          >
            <Select
              placeholder="Chọn đợt đề nghị"
              options={(cacDot ?? []).map((d) => ({ value: d.id, label: d.ten }))}
              showSearch
              optionFilterProp="label"
            />
          </Form.Item>
        </div>

        {/* Bước 2 — Thông tin chung */}
        <div style={{ display: buocHienTai === 1 ? 'block' : 'none' }}>
          <Form.Item
            name="tenSangKien"
            label="Tên sáng kiến"
            rules={[
              { required: true, message: 'Vui lòng nhập tên sáng kiến' },
              { max: 1000, message: 'Tối đa 1000 ký tự' },
            ]}
          >
            <Input.TextArea rows={2} showCount maxLength={1000} />
          </Form.Item>

          <Row gutter={12}>
            <Col xs={24} md={8}>
              <Form.Item
                name="linhVucId"
                label="Lĩnh vực"
                rules={[{ required: true, message: 'Vui lòng chọn lĩnh vực' }]}
              >
                <Select
                  options={(cacLinhVuc ?? []).map((x) => ({ value: x.id, label: x.ten }))}
                  showSearch
                  optionFilterProp="label"
                />
              </Form.Item>
            </Col>
            <Col xs={24} md={8}>
              <Form.Item name="doiTuongId" label="Đối tượng áp dụng">
                <Select
                  allowClear
                  options={(cacDoiTuong ?? []).map((x) => ({ value: x.id, label: x.ten }))}
                />
              </Form.Item>
            </Col>
            <Col xs={24} md={8}>
              <Form.Item name="loaiTacGiaId" label="Loại tác giả">
                <Select
                  allowClear
                  options={(cacLoaiTacGia ?? []).map((x) => ({ value: x.id, label: x.ten }))}
                />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={12}>
            <Col xs={24} md={12}>
              <Form.Item name="thoiGianApDungTu" label="Áp dụng từ ngày">
                <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
              </Form.Item>
            </Col>
            <Col xs={24} md={12}>
              <Form.Item name="thoiGianApDungDen" label="Áp dụng đến ngày">
                <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
              </Form.Item>
            </Col>
          </Row>
        </div>

        {/* Bước 3 — Tác giả */}
        <div style={{ display: buocHienTai === 2 ? 'block' : 'none' }}>
          <Alert
            type={Math.abs(tongTyLe - 100) < 0.01 ? 'success' : 'warning'}
            showIcon
            style={{ marginBottom: 12 }}
            message={`Tổng tỷ lệ đóng góp: ${tongTyLe}% ${
              Math.abs(tongTyLe - 100) < 0.01 ? '(hợp lệ)' : '— phải bằng 100%'
            }`}
            description={`Số tác giả tối đa cho loại tác giả đã chọn: ${soTacGiaToiDa}.`}
          />

          <Table<TacGia>
            rowKey={(_, i) => String(i)}
            size="small"
            pagination={false}
            dataSource={tacGia}
            columns={[
              {
                title: 'Họ và tên',
                dataIndex: 'hoTen',
                render: (giaTri: string, _dong, i) => (
                  <Input
                    value={giaTri}
                    onChange={(e) => capNhatTacGia(i, { hoTen: e.target.value })}
                    placeholder="Nguyễn Văn A"
                  />
                ),
              },
              {
                title: 'Chức vụ',
                dataIndex: 'chucVu',
                width: 180,
                render: (giaTri: string, _dong, i) => (
                  <Input
                    value={giaTri ?? ''}
                    onChange={(e) => capNhatTacGia(i, { chucVu: e.target.value })}
                  />
                ),
              },
              {
                title: 'Đơn vị công tác',
                dataIndex: 'donViCongTac',
                width: 200,
                render: (giaTri: string, _dong, i) => (
                  <Input
                    value={giaTri ?? ''}
                    onChange={(e) => capNhatTacGia(i, { donViCongTac: e.target.value })}
                  />
                ),
              },
              {
                title: 'Tỷ lệ (%)',
                dataIndex: 'tyLeDongGop',
                width: 110,
                render: (giaTri: number, _dong, i) => (
                  <InputNumber
                    min={0}
                    max={100}
                    value={giaTri}
                    style={{ width: '100%' }}
                    onChange={(v) => capNhatTacGia(i, { tyLeDongGop: Number(v) || 0 })}
                  />
                ),
              },
              {
                title: 'Chính',
                dataIndex: 'laTacGiaChinh',
                width: 80,
                align: 'center',
                render: (giaTri: boolean, _dong, i) => (
                  <Button
                    type={giaTri ? 'primary' : 'default'}
                    size="small"
                    shape="circle"
                    icon={<CheckCircleOutlined />}
                    onClick={() =>
                      setTacGia((cu) =>
                        cu.map((t, j) => ({ ...t, laTacGiaChinh: j === i })),
                      )
                    }
                  />
                ),
              },
              {
                title: '',
                width: 50,
                render: (_giaTri, _dong, i) => (
                  <Button
                    danger
                    type="text"
                    icon={<DeleteOutlined />}
                    disabled={tacGia.length === 1}
                    onClick={() => setTacGia((cu) => cu.filter((_, j) => j !== i))}
                  />
                ),
              },
            ]}
          />

          <Button
            type="dashed"
            block
            icon={<PlusOutlined />}
            style={{ marginTop: 12 }}
            disabled={tacGia.length >= soTacGiaToiDa}
            onClick={() =>
              setTacGia((cu) => [...cu, { hoTen: '', tyLeDongGop: 0, laTacGiaChinh: false }])
            }
          >
            Thêm đồng tác giả
          </Button>
        </div>

        {/* Bước 4 — Nội dung */}
        <div style={{ display: buocHienTai === 3 ? 'block' : 'none' }}>
          {(chiTiet?.thanhPhanHoSo ?? [])
            .filter((t) => t.loaiDuLieu !== 'TEP')
            .map((tp) => (
              <Form.Item
                key={tp.ma}
                name={tenTruongTheoMa(tp.ma)}
                label={
                  <Space>
                    {tp.ten}
                    {tp.batBuoc && <Tag color="red">Bắt buộc</Tag>}
                    {tp.soKyTuToiThieu > 0 && (
                      <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                        tối thiểu {tp.soKyTuToiThieu} ký tự
                      </Typography.Text>
                    )}
                  </Space>
                }
                extra={tp.moTaHuongDan}
                rules={
                  tp.batBuoc
                    ? [
                        { required: true, message: `Vui lòng nhập ${tp.ten.toLowerCase()}` },
                        {
                          min: tp.soKyTuToiThieu,
                          message: `Cần tối thiểu ${tp.soKyTuToiThieu} ký tự`,
                        },
                      ]
                    : undefined
                }
              >
                <Input.TextArea rows={5} showCount />
              </Form.Item>
            ))}

          <Form.Item name="giaTriLamLoiUocTinh" label="Giá trị làm lợi ước tính (VNĐ)">
            <InputNumber<number>
              style={{ width: '100%' }}
              min={0}
              formatter={(v) => `${v}`.replace(/\B(?=(\d{3})+(?!\d))/g, '.')}
              parser={(v) => Number(v?.replace(/\./g, '') ?? 0)}
            />
          </Form.Item>
        </div>

        {/* Bước 5 — Tệp đính kèm */}
        <div style={{ display: buocHienTai === 4 ? 'block' : 'none' }}>
          {!hoSoId ? (
            <Alert type="warning" showIcon message="Vui lòng lưu nháp trước khi tải tệp lên." />
          ) : (
            (chiTiet?.thanhPhanHoSo ?? [])
              .filter((t) => t.loaiDuLieu !== 'VAN_BAN')
              .map((tp) => (
                <Card key={tp.ma} size="small" style={{ marginBottom: 12 }}
                  title={
                    <Space>
                      {tp.ten}
                      {tp.batBuoc && <Tag color="red">Bắt buộc</Tag>}
                      <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                        tối đa {tp.soLuongToiDa} tệp, mỗi tệp ≤ {tp.dungLuongToiDaMb}MB
                      </Typography.Text>
                    </Space>
                  }
                >
                  <Upload
                    action="/api/v1/tep-tin/tai-len"
                    headers={{ Authorization: `Bearer ${boNhoToken.layAccessToken()}` }}
                    data={{ sangKienId: hoSoId, thanhPhanHoSoMa: tp.ma }}
                    name="tep"
                    accept={tp.dinhDangChoPhep.join(',')}
                    defaultFileList={(chiTiet?.tepDinhKem ?? [])
                      .filter((t) => t.thanhPhanHoSoMa === tp.ma)
                      .map((t) => ({
                        uid: t.id,
                        name: t.tenGoc,
                        status: 'done' as const,
                        url: `/api/v1/tep-tin/${t.tepTinId}/tai-ve`,
                      }))}
                    onChange={(thongTin) => {
                      if (thongTin.file.status === 'done') {
                        message.success(`Đã tải lên ${thongTin.file.name}`);
                        void queryClient.invalidateQueries({ queryKey: ['sang-kien', hoSoId] });
                      } else if (thongTin.file.status === 'error') {
                        const phanHoi = thongTin.file.response as { thongBao?: string } | undefined;
                        message.error(phanHoi?.thongBao ?? `Tải lên ${thongTin.file.name} thất bại`);
                      }
                    }}
                    onRemove={async (tep) => {
                      const dinhKem = (chiTiet?.tepDinhKem ?? []).find((t) => t.id === tep.uid);
                      if (dinhKem) {
                        await xoaDuLieu(`/api/v1/tep-tin/dinh-kem/${dinhKem.id}`);
                        void queryClient.invalidateQueries({ queryKey: ['sang-kien', hoSoId] });
                      }
                      return true;
                    }}
                  >
                    <Button icon={<UploadOutlined />}>Chọn tệp</Button>
                  </Upload>
                </Card>
              ))
          )}
        </div>

        {/* Bước 6 — Xem lại & Nộp */}
        <div style={{ display: buocHienTai === 5 ? 'block' : 'none' }}>
          <Row gutter={12}>
            <Col xs={24} lg={14}>
              <Descriptions bordered size="small" column={1} title="Thông tin hồ sơ">
                <Descriptions.Item label="Tên sáng kiến">
                  {form.getFieldValue('tenSangKien')}
                </Descriptions.Item>
                <Descriptions.Item label="Đợt đề nghị">
                  {cacDot?.find((d) => d.id === form.getFieldValue('dotDeNghiId'))?.ten}
                </Descriptions.Item>
                <Descriptions.Item label="Lĩnh vực">
                  {cacLinhVuc?.find((d) => d.id === form.getFieldValue('linhVucId'))?.ten}
                </Descriptions.Item>
                <Descriptions.Item label="Tác giả">
                  {tacGia.map((t) => `${t.hoTen} (${t.tyLeDongGop}%)`).join(', ')}
                </Descriptions.Item>
                <Descriptions.Item label="Số tệp đính kèm">
                  {chiTiet?.tepDinhKem.length ?? 0}
                </Descriptions.Item>
              </Descriptions>
            </Col>

            <Col xs={24} lg={10}>
              <Card size="small" title="Kiểm tra thành phần hồ sơ">
                {(chiTiet?.thanhPhanHoSo ?? []).map((tp) => (
                  <div key={tp.ma} style={{ padding: '4px 0' }}>
                    {tp.trangThai === 'DU' ? (
                      <CheckCircleOutlined style={{ color: '#52c41a' }} />
                    ) : tp.trangThai === 'KHONG_BAT_BUOC' ? (
                      <ExclamationCircleOutlined style={{ color: '#d9d9d9' }} />
                    ) : (
                      <CloseCircleOutlined style={{ color: '#ff4d4f' }} />
                    )}
                    <span style={{ marginLeft: 8 }}>{tp.ten}</span>
                    {tp.canhBao && (
                      <div style={{ marginLeft: 24, fontSize: 12, color: '#ff4d4f' }}>
                        {tp.canhBao}
                      </div>
                    )}
                  </div>
                ))}

                <Statistic
                  style={{ marginTop: 12 }}
                  title="Thành phần bắt buộc còn thiếu"
                  value={thanhPhanThieu.length}
                  valueStyle={{ color: thanhPhanThieu.length === 0 ? '#52c41a' : '#ff4d4f' }}
                />
              </Card>
            </Col>
          </Row>

          <Button
            type="primary"
            size="large"
            block
            icon={<SendOutlined />}
            style={{ marginTop: 16 }}
            loading={nopHoSo.isPending}
            disabled={!hoSoId || thanhPhanThieu.length > 0}
            onClick={() =>
              modal.confirm({
                title: 'Xác nhận nộp hồ sơ',
                content:
                  'Sau khi nộp, hồ sơ sẽ vào quy trình xử lý và bạn chỉ sửa được khi có yêu cầu bổ sung.',
                okText: 'Nộp hồ sơ',
                cancelText: 'Hủy',
                onOk: () => nopHoSo.mutate(),
              })
            }
          >
            Nộp hồ sơ
          </Button>
        </div>
      </Form>

      <Space style={{ marginTop: 24, width: '100%', justifyContent: 'space-between' }}>
        <Button disabled={buocHienTai === 0} onClick={() => setBuocHienTai((b) => b - 1)}>
          Quay lại
        </Button>
        {buocHienTai < BUOC.length - 1 && (
          <Button type="primary" onClick={() => void sangBuoc(buocHienTai + 1)}>
            Tiếp theo
          </Button>
        )}
      </Space>
    </Card>
  );

  function capNhatTacGia(chiSo: number, thayDoi: Partial<TacGia>) {
    setTacGia((cu) => cu.map((t, i) => (i === chiSo ? { ...t, ...thayDoi } : t)));
    hasThayDoi.current = true;
  }
}

/** Ánh xạ mã thành phần hồ sơ sang tên trường trên form. */
function tenTruongTheoMa(ma: string): keyof NoiDungHoSo {
  const bang: Record<string, keyof NoiDungHoSo> = {
    MO_TA_GIAI_PHAP: 'moTaGiaiPhap',
    TINH_TRANG_TRUOC: 'tinhTrangTruocKhiApDung',
    NOI_DUNG_GIAI_PHAP: 'noiDungGiaiPhap',
    TINH_MOI: 'tinhMoi',
    KHA_NANG_AP_DUNG: 'khaNangApDung',
    PHAM_VI_AP_DUNG: 'phamViApDung',
    HIEU_QUA_KINH_TE: 'hieuQuaKinhTe',
    HIEU_QUA_XA_HOI: 'hieuQuaXaHoi',
  };

  return bang[ma] ?? 'moTaGiaiPhap';
}

/** Các trường cần kiểm tra khi rời từng bước. */
function truongCuaBuoc(buoc: number): (keyof NoiDungHoSo)[] {
  switch (buoc) {
    case 0:
      return ['dotDeNghiId'];
    case 1:
      return ['tenSangKien', 'linhVucId'];
    default:
      return [];
  }
}

export { dayjs };
