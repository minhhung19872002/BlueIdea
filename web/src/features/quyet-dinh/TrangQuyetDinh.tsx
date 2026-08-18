import { useEffect, useMemo, useState } from 'react';
import {
  App,
  Button,
  Card,
  Col,
  DatePicker,
  Form,
  Input,
  Modal,
  Popconfirm,
  Row,
  Select,
  Space,
  Switch,
  Table,
  Tag,
  Tooltip,
  Typography,
  Upload,
} from 'antd';
import {
  CheckCircleOutlined,
  CloseCircleOutlined,
  DeleteOutlined,
  EditOutlined,
  FilePdfOutlined,
  NotificationOutlined,
  PlusOutlined,
  LinkOutlined,
  SafetyCertificateOutlined,
  UsbOutlined,
  UploadOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';

import { LoiApi, taiTep } from '@/api/client';
import {
  apiDonVi,
  apiDotDeNghi,
  apiQuyetDinh,
  apiTepTin,
  taiTepLen,
  type HoSoDuDieuKien,
  type LuuQuyetDinh,
  type NhatKyKySo,
  type QuyetDinh,
} from '@/api/endpoints';
import { useAuthStore } from '@/app/store/authStore';
import { HopThoaiKySoUsb } from '@/components/HopThoaiKySoUsb';
import { KhoiLoi, KhoiRong, ngayGio } from '@/components/ThanhPhanChung';

const CAP_CONG_NHAN = [
  { value: 'CO_SO', label: 'Cấp cơ sở' },
  { value: 'THANH_PHO', label: 'Cấp thành phố' },
  { value: 'TINH', label: 'Cấp tỉnh' },
];

/** Chức năng 8, 31, 32, 36 — Ban hành quyết định công nhận và công bố kết quả. */
export default function TrangQuyetDinh() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [trang, setTrang] = useState(1);
  const [soDong, setSoDong] = useState(20);
  const [tuKhoa, setTuKhoa] = useState('');
  const [dangSua, setDangSua] = useState<QuyetDinh | null>(null);
  const [moForm, setMoForm] = useState(false);
  const [xemKySo, setXemKySo] = useState<QuyetDinh | null>(null);

  const duocKySo = useAuthStore((st) => st.coQuyen('QUYET_DINH.KY_SO'));

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['quyet-dinh', { trang, soDong, tuKhoa }],
    queryFn: () => apiQuyetDinh.danhSach({ trang, soDong, tuKhoa }),
  });

  const congBo = useMutation({
    mutationFn: ({ id, congKhai }: { id: string; congKhai: boolean }) =>
      apiQuyetDinh.congBo(id, congKhai),
    onSuccess: () => {
      message.success('Đã công bố kết quả và gửi thông báo tới tác giả');
      void queryClient.invalidateQueries({ queryKey: ['quyet-dinh'] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không công bố được.'),
  });

  const [kyUsb, setKyUsb] = useState<QuyetDinh | null>(null);

  /**
   * Liên kết tải xuống có thời hạn.
   *
   * Dùng khi cần gửi văn bản cho người NGOÀI hệ thống (đơn vị phối hợp, người dân): liên kết tự
   * mang chữ ký và hạn dùng nên người nhận không cần tài khoản, mà cũng không trở thành đường
   * tải vĩnh viễn — hết hạn là hỏng.
   */
  const lienKet = useMutation({
    mutationFn: (tepTinId: string) => apiTepTin.lienKetTaiXuong(tepTinId, 15),
    onSuccess: (kq) => {
      const diaChi = `${window.location.origin}${kq.url}`;

      void navigator.clipboard?.writeText(diaChi).catch(() => undefined);

      modal.info({
        title: 'Liên kết tải xuống có thời hạn',
        width: 620,
        content: (
          <div style={{ marginTop: 12 }}>
            <Typography.Paragraph copyable={{ text: diaChi }} style={{ wordBreak: 'break-all' }}>
              {diaChi}
            </Typography.Paragraph>
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              Liên kết hết hạn sau {kq.hetHanSauPhut} phút. Người nhận không cần đăng nhập, nên
              chỉ gửi cho đúng người cần nhận.
            </Typography.Text>
          </div>
        ),
      });
    },
    onError: (loi) =>
      message.error(loi instanceof LoiApi ? loi.message : 'Không tạo được liên kết.'),
  });

  const kySo = useMutation({
    mutationFn: ({ id, tepTinId }: { id: string; tepTinId: string }) =>
      apiQuyetDinh.kySo(id, tepTinId),
    onSuccess: () => {
      message.success('Đã ký số quyết định. Bản gốc được giữ nguyên, chữ ký lưu thành tệp riêng.');
      void queryClient.invalidateQueries({ queryKey: ['quyet-dinh'] });
    },
    onError: (loi) =>
      modal.error({
        title: 'Không ký số được',
        content: loi instanceof LoiApi ? loi.message : 'Đã xảy ra lỗi khi ký số.',
      }),
  });

  const xoa = useMutation({
    mutationFn: (id: string) => apiQuyetDinh.xoa(id),
    onSuccess: () => {
      message.success('Đã xoá quyết định');
      void queryClient.invalidateQueries({ queryKey: ['quyet-dinh'] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không xoá được.'),
  });

  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  function moTaoMoi() {
    setDangSua(null);
    setMoForm(true);
  }

  function moSua(banGhi: QuyetDinh) {
    setDangSua(banGhi);
    setMoForm(true);
  }

  function xacNhanCongBo(banGhi: QuyetDinh) {
    modal.confirm({
      title: `Công bố kết quả quyết định ${banGhi.soQuyetDinh}?`,
      content: (
        <div>
          <p>
            Hệ thống sẽ đánh dấu <strong>{banGhi.soSangKien} sáng kiến</strong> là đã công bố và gửi
            thông báo tới toàn bộ tác giả có tài khoản.
          </p>
          <p style={{ marginBottom: 0 }}>
            Thao tác này không hoàn tác được bằng nút bấm — chỉ sửa lại được ở cấp cơ sở dữ liệu.
          </p>
        </div>
      ),
      okText: 'Công bố và hiển thị công khai',
      cancelText: 'Huỷ',
      onOk: () => congBo.mutateAsync({ id: banGhi.id, congKhai: true }),
    });
  }

  function xacNhanKySo(banGhi: QuyetDinh) {
    if (!banGhi.tepTinId) {
      modal.warning({
        title: 'Chưa có tệp văn bản để ký',
        content:
          'Ký số áp dụng cho tệp văn bản quyết định đã ban hành. Hãy sửa quyết định và tải tệp lên ở mục "Tệp văn bản quyết định" trước.',
      });
      return;
    }

    modal.confirm({
      title: `Ký số quyết định ${banGhi.soQuyetDinh}?`,
      content: (
        <div>
          <p>
            Hệ thống ký tệp văn bản bằng chứng thư số đang cấu hình. Chữ ký lưu thành tệp riêng
            dạng PKCS#7 detached, <strong>bản gốc giữ nguyên</strong>.
          </p>
          <p style={{ marginBottom: 0 }}>
            Quyết định đã ký số sẽ <strong>không sửa hay xoá được</strong> nữa.
          </p>
        </div>
      ),
      okText: 'Ký số',
      cancelText: 'Huỷ',
      onOk: () => kySo.mutateAsync({ id: banGhi.id, tepTinId: banGhi.tepTinId! }),
    });
  }

  return (
    <>
      <Card
        title="Quyết định công nhận sáng kiến"
        extra={
          <Space>
            <Input.Search
              placeholder="Tìm theo số quyết định hoặc trích yếu"
              allowClear
              style={{ width: 280 }}
              onSearch={(v) => {
                setTuKhoa(v);
                setTrang(1);
              }}
            />
            <Button type="primary" icon={<PlusOutlined />} onClick={moTaoMoi}>
              Ban hành quyết định
            </Button>
          </Space>
        }
      >
        <Table<QuyetDinh>
          rowKey="id"
          size="middle"
          loading={isLoading}
          dataSource={data?.duLieu ?? []}
          scroll={{ x: 1100 }}
          columns={[
            {
              title: 'Số quyết định',
              dataIndex: 'soQuyetDinh',
              width: 180,
              render: (v: string, dong) => (
                <div>
                  <Typography.Text strong>{v}</Typography.Text>
                  {dong.daKySo && (
                    <Tag color="green" style={{ marginLeft: 6 }}>
                      Đã ký số
                    </Tag>
                  )}
                </div>
              ),
            },
            {
              title: 'Ngày ban hành',
              dataIndex: 'ngayBanHanh',
              width: 130,
              render: (v: string) => ngayGio(v, false),
            },
            {
              title: 'Cấp',
              dataIndex: 'loai',
              width: 130,
              render: (v: string) => CAP_CONG_NHAN.find((c) => c.value === v)?.label ?? v,
            },
            { title: 'Trích yếu', dataIndex: 'trichYeu', responsive: ['lg'] },
            { title: 'Đợt', dataIndex: 'tenDot', width: 200, responsive: ['xl'] },
            {
              title: 'Sáng kiến',
              key: 'soSangKien',
              width: 140,
              align: 'center',
              render: (_v, dong) => (
                <Tooltip title={`${dong.soDaCongBo}/${dong.soSangKien} đã công bố kết quả`}>
                  <Tag color={dong.soDaCongBo === dong.soSangKien ? 'success' : 'processing'}>
                    {dong.soDaCongBo}/{dong.soSangKien}
                  </Tag>
                </Tooltip>
              ),
            },
            {
              title: '',
              key: 'thaoTac',
              width: 220,
              fixed: 'right',
              render: (_v, dong) => (
                <Space size={4}>
                  <Tooltip title="Xuất PDF">
                    <Button
                      size="small"
                      icon={<FilePdfOutlined />}
                      onClick={() =>
                        taiTep(
                          apiQuyetDinh.duongDanPdf(dong.id),
                          `quyet-dinh-${dong.soQuyetDinh.replace(/\//g, '-')}.pdf`,
                        )
                      }
                    />
                  </Tooltip>
                  <Tooltip title="Công bố kết quả">
                    <Button
                      size="small"
                      type="primary"
                      ghost
                      icon={<NotificationOutlined />}
                      disabled={dong.soDaCongBo === dong.soSangKien}
                      onClick={() => xacNhanCongBo(dong)}
                    />
                  </Tooltip>
                  {duocKySo && !dong.daKySo && (
                    <Tooltip
                      title={
                        dong.tepTinId
                          ? 'Ký số văn bản quyết định'
                          : 'Chưa có tệp văn bản — sửa quyết định để tải tệp lên trước'
                      }
                    >
                      <Button
                        size="small"
                        icon={<SafetyCertificateOutlined />}
                        loading={kySo.isPending}
                        onClick={() => xacNhanKySo(dong)}
                      />
                    </Tooltip>
                  )}
                  {!dong.daKySo && dong.tepTinId && (
                    <Tooltip title="Ký bằng chứng thư cá nhân trên USB token">
                      <Button
                        size="small"
                        icon={<UsbOutlined />}
                        onClick={() => setKyUsb(dong)}
                      />
                    </Tooltip>
                  )}
                  {dong.tepTinId && (
                    <Tooltip title="Tạo liên kết tải xuống có thời hạn để gửi cho người khác">
                      <Button
                        size="small"
                        icon={<LinkOutlined />}
                        loading={lienKet.isPending && lienKet.variables === dong.tepTinId}
                        onClick={() => lienKet.mutate(dong.tepTinId!)}
                      />
                    </Tooltip>
                  )}
                  {dong.daKySo && (
                    <Tooltip title="Xem lịch sử ký số và xác minh chữ ký">
                      <Button
                        size="small"
                        icon={<SafetyCertificateOutlined />}
                        onClick={() => setXemKySo(dong)}
                      />
                    </Tooltip>
                  )}
                  <Tooltip title={dong.daKySo ? 'Đã ký số — không sửa được' : 'Sửa'}>
                    <Button
                      size="small"
                      icon={<EditOutlined />}
                      disabled={dong.daKySo}
                      onClick={() => moSua(dong)}
                    />
                  </Tooltip>
                  <Popconfirm
                    title="Xoá quyết định này?"
                    okText="Xoá"
                    cancelText="Huỷ"
                    onConfirm={() => xoa.mutate(dong.id)}
                  >
                    <Button size="small" danger icon={<DeleteOutlined />} disabled={dong.daKySo} />
                  </Popconfirm>
                </Space>
              ),
            },
          ]}
          pagination={{
            current: trang,
            pageSize: soDong,
            total: data?.tongSo ?? 0,
            showSizeChanger: true,
            showTotal: (t) => `Tổng ${t} quyết định`,
            onChange: (t, s) => {
              setTrang(t);
              setSoDong(s);
            },
          }}
        />
      </Card>

      {xemKySo && (
        <ModalLichSuKySo quyetDinh={xemKySo} onDong={() => setXemKySo(null)} />
      )}

      {kyUsb && (
        <HopThoaiKySoUsb
          tepTinId={kyUsb.tepTinId!}
          doiTuong="QUYET_DINH"
          doiTuongId={kyUsb.id}
          tenVanBan={kyUsb.soQuyetDinh}
          onDong={() => setKyUsb(null)}
          onXong={() => void queryClient.invalidateQueries({ queryKey: ['quyet-dinh'] })}
        />
      )}

      {moForm && (
        <FormQuyetDinh
          banGhi={dangSua}
          onDong={() => setMoForm(false)}
          onXong={() => {
            setMoForm(false);
            void queryClient.invalidateQueries({ queryKey: ['quyet-dinh'] });
          }}
        />
      )}
    </>
  );
}

// ---------------------------------------------------------------------------

interface GiaTriForm {
  soQuyetDinh: string;
  ngayBanHanh: dayjs.Dayjs;
  loai: string;
  trichYeu?: string;
  nguoiKy?: string;
  chucVuNguoiKy?: string;
  donViBanHanhId?: string;
  dotDeNghiId?: string;
}

function FormQuyetDinh({
  banGhi,
  onDong,
  onXong,
}: {
  banGhi: QuyetDinh | null;
  onDong: () => void;
  onXong: () => void;
}) {
  const { message } = App.useApp();
  const [form] = Form.useForm<GiaTriForm>();

  const [dotDeNghiId, setDotDeNghiId] = useState<string | undefined>(
    banGhi?.dotDeNghiId ?? undefined,
  );
  const [daChon, setDaChon] = useState<string[]>([]);
  const [chiHienDaChon, setChiHienDaChon] = useState(false);
  const [tepTinId, setTepTinId] = useState<string | null>(banGhi?.tepTinId ?? null);
  const [tenTep, setTenTep] = useState<string | null>(
    banGhi?.tepTinId ? 'Tệp văn bản đã tải lên trước đó' : null,
  );
  const [dangTaiTep, setDangTaiTep] = useState(false);

  const { data: chiTiet, isLoading: dangTaiChiTiet } = useQuery({
    queryKey: ['quyet-dinh-chi-tiet', banGhi?.id],
    queryFn: () => apiQuyetDinh.chiTiet(banGhi!.id),
    enabled: !!banGhi,
  });

  const { data: duDieuKien, isLoading: dangTaiHoSo } = useQuery({
    queryKey: ['ho-so-du-dieu-kien', dotDeNghiId, banGhi?.id],
    queryFn: () => apiQuyetDinh.hoSoDuDieuKien(dotDeNghiId, banGhi?.id),
  });

  const { data: cacDot } = useQuery({ queryKey: ['dot-chon'], queryFn: apiDotDeNghi.chon });
  const { data: cacDonVi } = useQuery({ queryKey: ['don-vi-chon'], queryFn: apiDonVi.chon });

  // Khi mở form sửa, nạp sẵn danh sách sáng kiến đang thuộc quyết định.
  const idDangThuoc = useMemo(
    () => (chiTiet?.danhSachSangKien ?? []).map((x) => x.id),
    [chiTiet],
  );

  useEffect(() => {
    if (idDangThuoc.length > 0) {
      setDaChon(idDangThuoc);
    }
  }, [idDangThuoc]);

  // Hồ sơ đang thuộc quyết định này không nằm trong danh sách "đủ điều kiện"
  // (vì đã có quyết định), nên phải ghép thêm vào để bảng hiển thị và bỏ chọn được.
  const danhSachHienThi = useMemo(() => {
    const gop = [...(duDieuKien ?? [])];
    const daCo = new Set(gop.map((x) => x.id));

    for (const x of chiTiet?.danhSachSangKien ?? []) {
      if (!daCo.has(x.id)) gop.push(x);
    }

    return chiHienDaChon ? gop.filter((x) => daChon.includes(x.id)) : gop;
  }, [duDieuKien, chiTiet, chiHienDaChon, daChon]);

  const luu = useMutation({
    mutationFn: (giaTri: LuuQuyetDinh) =>
      banGhi ? apiQuyetDinh.sua(banGhi.id, giaTri) : apiQuyetDinh.banHanh(giaTri),
    onSuccess: () => {
      message.success(banGhi ? 'Đã cập nhật quyết định' : 'Đã ban hành quyết định');
      onXong();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không lưu được.'),
  });

  async function xacNhan() {
    const giaTri = await form.validateFields();

    if (daChon.length === 0) {
      message.warning('Phải chọn ít nhất một sáng kiến để đưa vào quyết định.');
      return;
    }

    luu.mutate({
      soQuyetDinh: giaTri.soQuyetDinh,
      ngayBanHanh: giaTri.ngayBanHanh.format('YYYY-MM-DD'),
      loai: giaTri.loai,
      trichYeu: giaTri.trichYeu,
      nguoiKy: giaTri.nguoiKy,
      chucVuNguoiKy: giaTri.chucVuNguoiKy,
      donViBanHanhId: giaTri.donViBanHanhId,
      dotDeNghiId: giaTri.dotDeNghiId,
      tepTinId,
      sangKienIds: daChon,
    });
  }

  return (
    <Modal
      open
      width={1000}
      title={banGhi ? `Sửa quyết định ${banGhi.soQuyetDinh}` : 'Ban hành quyết định công nhận'}
      okText={banGhi ? 'Lưu thay đổi' : 'Ban hành'}
      cancelText="Huỷ"
      confirmLoading={luu.isPending}
      onOk={xacNhan}
      onCancel={onDong}
      destroyOnClose
    >
      <Form<GiaTriForm>
        form={form}
        layout="vertical"
        initialValues={{
          soQuyetDinh: banGhi?.soQuyetDinh ?? '',
          ngayBanHanh: banGhi ? dayjs(banGhi.ngayBanHanh) : dayjs(),
          loai: banGhi?.loai ?? 'CO_SO',
          trichYeu: banGhi?.trichYeu ?? undefined,
          nguoiKy: banGhi?.nguoiKy ?? undefined,
          chucVuNguoiKy: banGhi?.chucVuNguoiKy ?? undefined,
          donViBanHanhId: banGhi?.donViBanHanhId ?? undefined,
          dotDeNghiId: banGhi?.dotDeNghiId ?? undefined,
        }}
      >
        <Row gutter={12}>
          <Col xs={24} md={8}>
            <Form.Item
              name="soQuyetDinh"
              label="Số quyết định"
              rules={[{ required: true, message: 'Nhập số quyết định' }]}
            >
              <Input placeholder="Ví dụ: 125/QĐ-UBND" />
            </Form.Item>
          </Col>
          <Col xs={24} md={8}>
            <Form.Item
              name="ngayBanHanh"
              label="Ngày ban hành"
              rules={[{ required: true, message: 'Chọn ngày ban hành' }]}
            >
              <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
            </Form.Item>
          </Col>
          <Col xs={24} md={8}>
            <Form.Item name="loai" label="Cấp công nhận">
              <Select options={CAP_CONG_NHAN} />
            </Form.Item>
          </Col>
        </Row>

        <Row gutter={12}>
          <Col xs={24} md={12}>
            <Form.Item name="donViBanHanhId" label="Đơn vị ban hành">
              <Select
                allowClear
                showSearch
                optionFilterProp="label"
                placeholder="Chọn đơn vị"
                options={(cacDonVi ?? []).map((x) => ({ value: x.id, label: x.ten }))}
              />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item name="dotDeNghiId" label="Đợt đề nghị">
              <Select
                allowClear
                showSearch
                optionFilterProp="label"
                placeholder="Lọc sáng kiến theo đợt"
                options={(cacDot ?? []).map((x) => ({ value: x.id, label: x.ten }))}
                onChange={(v: string | undefined) => setDotDeNghiId(v)}
              />
            </Form.Item>
          </Col>
        </Row>

        <Form.Item name="trichYeu" label="Trích yếu">
          <Input.TextArea
            rows={2}
            placeholder="Về việc công nhận sáng kiến cấp cơ sở năm 2026"
          />
        </Form.Item>

        <Row gutter={12}>
          <Col xs={24} md={12}>
            <Form.Item name="nguoiKy" label="Người ký">
              <Input placeholder="Họ và tên người ký" />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item name="chucVuNguoiKy" label="Chức vụ người ký">
              <Input placeholder="Ví dụ: Chủ tịch UBND thành phố" />
            </Form.Item>
          </Col>
        </Row>
      </Form>

      <div style={{ marginBottom: 16 }}>
        <Typography.Text strong>Tệp văn bản quyết định</Typography.Text>
        <Typography.Paragraph type="secondary" style={{ fontSize: 12, marginBottom: 8 }}>
          Bản quyết định đã ban hành (PDF hoặc bản scan). Đây chính là tệp sẽ được{' '}
          <strong>ký số</strong> ở màn hình danh sách — chưa có tệp thì nút ký số không dùng được.
        </Typography.Paragraph>

        <Space wrap>
          <Upload
            accept=".pdf,.docx,.doc,.png,.jpg,.jpeg"
            maxCount={1}
            showUploadList={false}
            beforeUpload={(tep) => {
              void (async () => {
                setDangTaiTep(true);
                try {
                  const daTaiLen = await taiTepLen(tep as File);
                  setTepTinId(daTaiLen.id);
                  setTenTep(daTaiLen.tenGoc);
                  message.success('Đã tải tệp văn bản lên');
                } catch (loi) {
                  message.error(loi instanceof LoiApi ? loi.message : 'Không tải được tệp.');
                } finally {
                  setDangTaiTep(false);
                }
              })();

              return false;
            }}
          >
            <Button icon={<UploadOutlined />} loading={dangTaiTep}>
              {tepTinId ? 'Thay tệp khác' : 'Tải tệp văn bản'}
            </Button>
          </Upload>

          {tepTinId && (
            <>
              <Tag color="blue">{tenTep}</Tag>
              <a href={`/api/v1/tep-tin/${tepTinId}/tai-ve`} download>
                Tải về
              </a>
              <Button
                size="small"
                danger
                onClick={() => {
                  setTepTinId(null);
                  setTenTep(null);
                }}
              >
                Gỡ tệp
              </Button>
            </>
          )}
        </Space>
      </div>

      <Space style={{ marginBottom: 8 }}>
        <Typography.Text strong>Sáng kiến được công nhận</Typography.Text>
        <Tag color={daChon.length > 0 ? 'blue' : 'default'}>Đã chọn {daChon.length}</Tag>
        <Switch
          size="small"
          checked={chiHienDaChon}
          onChange={setChiHienDaChon}
          checkedChildren="Chỉ đã chọn"
          unCheckedChildren="Tất cả"
        />
      </Space>

      <Table<HoSoDuDieuKien>
        rowKey="id"
        size="small"
        loading={dangTaiHoSo || dangTaiChiTiet}
        dataSource={danhSachHienThi}
        scroll={{ y: 260 }}
        pagination={false}
        rowSelection={{
          selectedRowKeys: daChon,
          onChange: (khoa) => setDaChon(khoa as string[]),
        }}
        locale={{
          emptyText:
            'Không có sáng kiến nào đủ điều kiện. Sáng kiến phải có kết quả Đạt và chưa nằm trong quyết định khác.',
        }}
        columns={[
          { title: 'Mã hồ sơ', dataIndex: 'maHoSo', width: 130 },
          { title: 'Tên sáng kiến', dataIndex: 'tenSangKien' },
          { title: 'Tác giả chính', dataIndex: 'tenTacGiaChinh', width: 160, responsive: ['lg'] },
          { title: 'Đơn vị', dataIndex: 'tenDonVi', width: 180, responsive: ['xl'] },
          {
            title: 'Điểm',
            dataIndex: 'tongDiem',
            width: 80,
            align: 'right',
            render: (v?: number | null) => v?.toFixed(2) ?? '—',
          },
          { title: 'Mức công nhận', dataIndex: 'tenMucCongNhan', width: 170, responsive: ['lg'] },
        ]}
      />
    </Modal>
  );
}

// ---------------------------------------------------------------------------

/** Chức năng 49 — Lịch sử ký số của một quyết định và xác minh từng chữ ký. */
function ModalLichSuKySo({
  quyetDinh,
  onDong,
}: {
  quyetDinh: QuyetDinh;
  onDong: () => void;
}) {
  const { message } = App.useApp();
  const [ketQuaXacMinh, setKetQuaXacMinh] = useState<Record<string, string>>({});

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['lich-su-ky-so', quyetDinh.id],
    queryFn: () => apiQuyetDinh.lichSuKySo(quyetDinh.id),
  });

  const xacMinh = useMutation({
    mutationFn: (nhatKyId: string) => apiQuyetDinh.xacMinhChuKy(nhatKyId),
    onSuccess: (kq, nhatKyId) => {
      const nhan = !kq.coChuKy
        ? 'Không tìm thấy tệp chữ ký'
        : kq.hopLe
          ? `Chữ ký hợp lệ — ${kq.nguoiKy ?? 'không rõ người ký'}`
          : `Chữ ký KHÔNG hợp lệ: ${kq.thongBaoLoi ?? 'nội dung đã bị thay đổi'}`;

      setKetQuaXacMinh((cu) => ({ ...cu, [nhatKyId]: nhan }));

      if (kq.hopLe) message.success('Chữ ký hợp lệ');
      else message.warning(nhan);
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không xác minh được.'),
  });

  return (
    <Modal
      open
      width={860}
      title={`Lịch sử ký số — ${quyetDinh.soQuyetDinh}`}
      footer={<Button onClick={onDong}>Đóng</Button>}
      onCancel={onDong}
    >
      {error ? (
        <KhoiLoi loi={error} thuLai={refetch} />
      ) : (
        <Table<NhatKyKySo>
          rowKey="id"
          size="small"
          loading={isLoading}
          dataSource={data ?? []}
          pagination={false}
          scroll={{ x: 1000 }}
          locale={{ emptyText: <KhoiRong moTa="Quyết định này chưa có lần ký số nào." /> }}
          columns={[
            {
              title: 'Thời gian ký',
              dataIndex: 'thoiGianKy',
              width: 150,
              render: (v: string) => ngayGio(v),
            },
            {
              title: 'Chứng thư',
              dataIndex: 'serialChungThu',
              width: 300,
              render: (v: string | null, dong) => (
                <Space direction="vertical" size={0}>
                  <Typography.Text code>{v ?? '—'}</Typography.Text>
                  <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                    {dong.nguoiCapChungThu ?? 'Không rõ đơn vị cấp'}
                  </Typography.Text>
                </Space>
              ),
            },
            {
              title: 'Hiệu lực',
              key: 'hieuLuc',
              width: 190,
              responsive: ['lg'],
              render: (_v, dong) =>
                `${ngayGio(dong.hieuLucTu, false)} — ${ngayGio(dong.hieuLucDen, false)}`,
            },
            {
              title: 'Trạng thái',
              dataIndex: 'trangThaiKy',
              width: 120,
              render: (v: string) =>
                v === 'THANH_CONG' ? (
                  <Tag icon={<CheckCircleOutlined />} color="success">
                    Thành công
                  </Tag>
                ) : (
                  <Tag icon={<CloseCircleOutlined />} color="error">
                    Thất bại
                  </Tag>
                ),
            },
            {
              title: '',
              key: 'thaoTac',
              width: 260,
              render: (_v, dong) => (
                <Space direction="vertical" size={4} style={{ width: '100%' }}>
                  <Space size={4} wrap>
                    <Button
                      size="small"
                      loading={xacMinh.isPending && xacMinh.variables === dong.id}
                      disabled={dong.trangThaiKy !== 'THANH_CONG'}
                      onClick={() => xacMinh.mutate(dong.id)}
                    >
                      Xác minh
                    </Button>
                    {dong.tepGocId && (
                      <a href={`/api/v1/tep-tin/${dong.tepGocId}/tai-ve`} download>
                        Bản gốc
                      </a>
                    )}
                    {dong.tepDaKyId && (
                      <a href={`/api/v1/tep-tin/${dong.tepDaKyId}/tai-ve`} download>
                        Tệp chữ ký
                      </a>
                    )}
                  </Space>
                  {ketQuaXacMinh[dong.id] && (
                    <Typography.Text
                      type={ketQuaXacMinh[dong.id].includes('hợp lệ —') ? 'success' : 'danger'}
                      style={{ fontSize: 12 }}
                    >
                      {ketQuaXacMinh[dong.id]}
                    </Typography.Text>
                  )}
                </Space>
              ),
            },
          ]}
        />
      )}
    </Modal>
  );
}
