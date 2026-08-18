import { useMemo, useRef, useState } from 'react';
import {
  App,
  Button,
  Card,
  Col,
  Form,
  Input,
  InputNumber,
  Modal,
  Popconfirm,
  Row,
  Select,
  Space,
  Switch,
  Tag,
  Tooltip,
  Tree,
  Typography,
} from 'antd';
import {
  DeleteOutlined,
  EditOutlined,
  MergeCellsOutlined,
  PictureOutlined,
  PlusOutlined,
  SubnodeOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { DataNode } from 'antd/es/tree';

import { LoiApi, layDuLieu } from '@/api/client';
import { apiDonVi, type NutCay } from '@/api/endpoints';
import { KhoiDangTai, KhoiLoi } from '@/components/ThanhPhanChung';

interface DonVi {
  id: string;
  ma: string;
  ten: string;
  moTa?: string | null;
  thuTu: number;
  trangThai: number;
  tenVietTat?: string | null;
  donViChaId?: string | null;
  cap: number;
  loai: string;
  diaChi?: string | null;
  dienThoai?: string | null;
  email?: string | null;
  nguoiDaiDien?: string | null;
  chucVuNguoiDaiDien?: string | null;
  laDonViPheDuyet: boolean;
  capPheDuyet?: string | null;
  tieuDeVanBan?: string | null;
  nguoiKyMacDinh?: string | null;
  chucVuNguoiKyMacDinh?: string | null;
}

const LOAI_DON_VI = [
  { value: 'DON_VI', label: 'Đơn vị' },
  { value: 'PHONG_BAN', label: 'Phòng ban' },
  { value: 'HOI_DONG', label: 'Hội đồng' },
  { value: 'UBND', label: 'UBND' },
];

const CAP_PHE_DUYET = [
  { value: 'CO_SO', label: 'Cấp cơ sở' },
  { value: 'THANH_PHO', label: 'Cấp thành phố' },
  { value: 'TINH', label: 'Cấp tỉnh' },
];

/** Chức năng 5, 44, 47 — Cây tổ chức đơn vị và cấu hình văn bản của đơn vị. */
export default function TrangDonVi() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [chonId, setChonId] = useState<string | null>(null);
  const [moForm, setMoForm] = useState(false);
  const [suaId, setSuaId] = useState<string | null>(null);
  const [chaMacDinh, setChaMacDinh] = useState<string | null>(null);
  const [gopId, setGopId] = useState<string | null>(null);
  const [gopDichId, setGopDichId] = useState<string | undefined>();
  const [dangXuatAnh, setDangXuatAnh] = useState(false);
  const khungCay = useRef<HTMLDivElement>(null);

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['don-vi', 'cay'],
    queryFn: apiDonVi.cay,
  });

  const { data: dsPhang } = useQuery({ queryKey: ['don-vi-chon'], queryFn: apiDonVi.chon });

  const { data: dangChon } = useQuery({
    queryKey: ['don-vi-chi-tiet', chonId],
    queryFn: () => layDuLieu<DonVi>(`/api/v1/don-vi/${chonId}`),
    enabled: !!chonId,
  });

  const xoa = useMutation({
    mutationFn: (id: string) => apiDonVi.xoa(id),
    onSuccess: () => {
      message.success('Đã xoá đơn vị');
      setChonId(null);
      void queryClient.invalidateQueries({ queryKey: ['don-vi'] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không xoá được.'),
  });

  const cay = useMemo(() => chuyenDoi(data ?? []), [data]);

  /** Bản đồ con -> cha, để biết thả vào khe giữa hai nút thì cha mới là ai. */
  const chaCua = useMemo(() => {
    const bang = new Map<string, string | null>();

    const duyet = (cacNut: NutCay[], chaId: string | null) => {
      cacNut.forEach((n) => {
        bang.set(n.id, chaId);
        duyet(n.con ?? [], n.id);
      });
    };

    duyet(data ?? [], null);
    return bang;
  }, [data]);

  const chuyenCha = useMutation({
    mutationFn: ({ id, chaMoi }: { id: string; chaMoi: string | null }) =>
      apiDonVi.chuyenCha(id, chaMoi),
    onSuccess: () => {
      message.success('Đã chuyển đơn vị');
      void queryClient.invalidateQueries({ queryKey: ['don-vi'] });
    },
    onError: (loi) =>
      message.error(loi instanceof LoiApi ? loi.message : 'Không chuyển được đơn vị.'),
  });

  const gop = useMutation({
    mutationFn: ({ nguonId, dichId }: { nguonId: string; dichId: string }) =>
      apiDonVi.gop(nguonId, dichId),
    onSuccess: (soBanGhi) => {
      message.success(`Đã gộp đơn vị, chuyển ${soBanGhi} bản ghi liên quan`);
      setGopId(null);
      setGopDichId(undefined);
      setChonId(null);
      void queryClient.invalidateQueries({ queryKey: ['don-vi'] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không gộp được.'),
  });

  async function xuatSoDo() {
    if (!khungCay.current) return;

    setDangXuatAnh(true);
    try {
      const { toPng } = await import('html-to-image');
      const anh = await toPng(khungCay.current, { backgroundColor: '#ffffff', pixelRatio: 2 });

      const a = document.createElement('a');
      a.href = anh;
      a.download = 'so-do-to-chuc.png';
      a.click();
    } catch {
      message.error('Không xuất được sơ đồ.');
    } finally {
      setDangXuatAnh(false);
    }
  }

  if (isLoading) return <KhoiDangTai />;
  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  return (
    <>
      <Card
        title="Cơ cấu tổ chức"
        extra={
          <Space>
            <Tooltip title="Lưu sơ đồ tổ chức thành ảnh PNG để dán vào báo cáo">
              <Button icon={<PictureOutlined />} loading={dangXuatAnh} onClick={xuatSoDo}>
                Xuất sơ đồ
              </Button>
            </Tooltip>
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => {
                setSuaId(null);
                setChaMacDinh(null);
                setMoForm(true);
              }}
            >
              Thêm đơn vị gốc
            </Button>
          </Space>
        }
      >
        <Row gutter={16}>
          <Col xs={24} lg={12}>
            <Typography.Paragraph type="secondary">
              Cây đơn vị dùng chung cho phân quyền theo phạm vi dữ liệu và cấp phê duyệt. Chọn một
              đơn vị để xem và sửa chi tiết.
            </Typography.Paragraph>

            <div ref={khungCay}>
              <Tree
                treeData={cay}
                defaultExpandAll
                showLine
                blockNode
                draggable
                selectedKeys={chonId ? [chonId] : []}
                onSelect={(khoa) => setChonId((khoa[0] as string | undefined) ?? null)}
                onDrop={(thongTin) => {
                  const id = String(thongTin.dragNode.key);
                  // Thả GIỮA hai nút = cùng cấp với nút đích; thả LÊN nút = làm con của nút đó.
                  const chaMoi = thongTin.dropToGap
                    ? (chaCua.get(String(thongTin.node.key)) ?? null)
                    : String(thongTin.node.key);

                  chuyenCha.mutate({ id, chaMoi });
                }}
              />
            </div>
          </Col>

          <Col xs={24} lg={12}>
            {!chonId && (
              <Card size="small" type="inner" title="Chi tiết đơn vị">
                <Typography.Text type="secondary">
                  Chọn một đơn vị trong cây bên trái để xem chi tiết.
                </Typography.Text>
              </Card>
            )}

            {chonId && dangChon && (
              <Card
                size="small"
                type="inner"
                title={dangChon.ten}
                extra={
                  <Space size={4}>
                    <Tooltip title="Thêm đơn vị con">
                      <Button
                        size="small"
                        icon={<SubnodeOutlined />}
                        onClick={() => {
                          setSuaId(null);
                          setChaMacDinh(dangChon.id);
                          setMoForm(true);
                        }}
                      />
                    </Tooltip>
                    <Tooltip title="Gộp đơn vị này vào đơn vị khác (khi sáp nhập)">
                      <Button
                        size="small"
                        icon={<MergeCellsOutlined />}
                        onClick={() => setGopId(dangChon.id)}
                      />
                    </Tooltip>
                    <Tooltip title="Sửa">
                      <Button
                        size="small"
                        icon={<EditOutlined />}
                        onClick={() => {
                          setSuaId(dangChon.id);
                          setChaMacDinh(null);
                          setMoForm(true);
                        }}
                      />
                    </Tooltip>
                    <Popconfirm
                      title="Xoá đơn vị này?"
                      description="Không xoá được nếu đơn vị đang được tham chiếu ở nơi khác."
                      okText="Xoá"
                      cancelText="Huỷ"
                      onConfirm={() => xoa.mutate(dangChon.id)}
                    >
                      <Button size="small" danger icon={<DeleteOutlined />} />
                    </Popconfirm>
                  </Space>
                }
              >
                <Dong nhan="Mã" giaTri={dangChon.ma} />
                <Dong nhan="Tên viết tắt" giaTri={dangChon.tenVietTat} />
                <Dong
                  nhan="Loại"
                  giaTri={LOAI_DON_VI.find((l) => l.value === dangChon.loai)?.label ?? dangChon.loai}
                />
                <Dong nhan="Cấp trong cây" giaTri={String(dangChon.cap)} />
                <Dong nhan="Người đại diện" giaTri={dangChon.nguoiDaiDien} />
                <Dong nhan="Chức vụ" giaTri={dangChon.chucVuNguoiDaiDien} />
                <Dong nhan="Địa chỉ" giaTri={dangChon.diaChi} />
                <Dong nhan="Điện thoại" giaTri={dangChon.dienThoai} />
                <Dong nhan="Email" giaTri={dangChon.email} />

                <Typography.Title level={5} style={{ marginTop: 16, fontSize: 13 }}>
                  Cấu hình văn bản (chức năng 47)
                </Typography.Title>
                <Dong nhan="Tiêu đề văn bản" giaTri={dangChon.tieuDeVanBan} />
                <Dong nhan="Người ký mặc định" giaTri={dangChon.nguoiKyMacDinh} />
                <Dong nhan="Chức vụ người ký" giaTri={dangChon.chucVuNguoiKyMacDinh} />

                <div style={{ marginTop: 12 }}>
                  {dangChon.laDonViPheDuyet && (
                    <Tag color="blue">
                      Đơn vị phê duyệt
                      {dangChon.capPheDuyet ? ` — ${dangChon.capPheDuyet}` : ''}
                    </Tag>
                  )}
                  {dangChon.trangThai !== 1 && <Tag>Ngừng hoạt động</Tag>}
                </div>
              </Card>
            )}
          </Col>
        </Row>
      </Card>

      <Modal
        open={!!gopId}
        title="Gộp đơn vị"
        okText="Gộp"
        okButtonProps={{ danger: true, disabled: !gopDichId }}
        cancelText="Huỷ"
        confirmLoading={gop.isPending}
        onCancel={() => {
          setGopId(null);
          setGopDichId(undefined);
        }}
        onOk={() => gop.mutate({ nguonId: gopId!, dichId: gopDichId! })}
      >
        <Typography.Paragraph>
          Hồ sơ, tài khoản và đơn vị con của <strong>{dangChon?.ten}</strong> sẽ được chuyển sang
          đơn vị đích, sau đó đơn vị này được đánh dấu đã xoá.
        </Typography.Paragraph>
        <Typography.Paragraph type="warning">
          Thao tác này không tự hoàn tác được — hãy kiểm tra kỹ đơn vị đích trước khi gộp.
        </Typography.Paragraph>
        <Select
          style={{ width: '100%' }}
          showSearch
          optionFilterProp="label"
          placeholder="Chọn đơn vị đích"
          value={gopDichId}
          options={(dsPhang ?? [])
            .filter((x) => x.id !== gopId)
            .map((x) => ({ value: x.id, label: x.ten }))}
          onChange={setGopDichId}
        />
      </Modal>

      {moForm && (
        <FormDonVi
          id={suaId}
          donViChaMacDinh={chaMacDinh}
          onDong={() => setMoForm(false)}
          onXong={() => {
            setMoForm(false);
            void queryClient.invalidateQueries({ queryKey: ['don-vi'] });
          }}
        />
      )}
    </>
  );
}

function Dong({ nhan, giaTri }: { nhan: string; giaTri?: string | null }) {
  return (
    <div style={{ display: 'flex', gap: 8, marginBottom: 4, fontSize: 13 }}>
      <span style={{ minWidth: 140, color: '#888' }}>{nhan}</span>
      <span>{giaTri || '—'}</span>
    </div>
  );
}

function chuyenDoi(danhSach: NutCay[]): DataNode[] {
  return danhSach.map((n) => ({
    key: n.id,
    title: (
      <span>
        {n.ten} <Tag>{n.ma}</Tag>
        {n.trangThai !== 1 && <Tag color="default">Ngừng</Tag>}
      </span>
    ),
    children: n.con.length > 0 ? chuyenDoi(n.con) : undefined,
  }));
}

// ---------------------------------------------------------------------------

interface GiaTriFormDonVi {
  ma: string;
  ten: string;
  tenVietTat?: string;
  donViChaId?: string;
  loai: string;
  moTa?: string;
  thuTu: number;
  diaChi?: string;
  dienThoai?: string;
  email?: string;
  nguoiDaiDien?: string;
  chucVuNguoiDaiDien?: string;
  laDonViPheDuyet: boolean;
  capPheDuyet?: string;
  tieuDeVanBan?: string;
  nguoiKyMacDinh?: string;
  chucVuNguoiKyMacDinh?: string;
}

function FormDonVi({
  id,
  donViChaMacDinh,
  onDong,
  onXong,
}: {
  id: string | null;
  donViChaMacDinh: string | null;
  onDong: () => void;
  onXong: () => void;
}) {
  const { message } = App.useApp();
  const [form] = Form.useForm<GiaTriFormDonVi>();

  const { data: chiTiet, isLoading } = useQuery({
    queryKey: ['don-vi-chi-tiet', id],
    queryFn: () => layDuLieu<DonVi>(`/api/v1/don-vi/${id}`),
    enabled: !!id,
  });

  const { data: cacDonVi } = useQuery({ queryKey: ['don-vi-chon'], queryFn: apiDonVi.chon });

  const luu = useMutation({
    mutationFn: (giaTri: Record<string, unknown>) =>
      id ? apiDonVi.sua(id, giaTri) : apiDonVi.them(giaTri),
    onSuccess: () => {
      message.success(id ? 'Đã cập nhật đơn vị' : 'Đã thêm đơn vị');
      onXong();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không lưu được.'),
  });

  async function xacNhan() {
    const giaTri = await form.validateFields();

    luu.mutate({
      ...giaTri,
      trangThai: chiTiet?.trangThai ?? 1,
      capPheDuyet: giaTri.laDonViPheDuyet ? giaTri.capPheDuyet : null,
    });
  }

  if (id && isLoading) {
    return (
      <Modal open title="Đang tải..." footer={null} onCancel={onDong}>
        <div style={{ height: 120 }} />
      </Modal>
    );
  }

  // Không cho chọn chính nó làm đơn vị cha — sẽ tạo chu trình trong cây.
  const donViCha = (cacDonVi ?? []).filter((x) => x.id !== id);

  return (
    <Modal
      open
      width={760}
      title={id ? `Sửa đơn vị ${chiTiet?.ma ?? ''}` : 'Thêm đơn vị'}
      okText={id ? 'Lưu thay đổi' : 'Thêm'}
      cancelText="Huỷ"
      confirmLoading={luu.isPending}
      onOk={xacNhan}
      onCancel={onDong}
      destroyOnClose
    >
      <Form<GiaTriFormDonVi>
        form={form}
        layout="vertical"
        initialValues={{
          ma: chiTiet?.ma ?? '',
          ten: chiTiet?.ten ?? '',
          tenVietTat: chiTiet?.tenVietTat ?? undefined,
          donViChaId: chiTiet?.donViChaId ?? donViChaMacDinh ?? undefined,
          loai: chiTiet?.loai ?? 'DON_VI',
          moTa: chiTiet?.moTa ?? undefined,
          thuTu: chiTiet?.thuTu ?? 0,
          diaChi: chiTiet?.diaChi ?? undefined,
          dienThoai: chiTiet?.dienThoai ?? undefined,
          email: chiTiet?.email ?? undefined,
          nguoiDaiDien: chiTiet?.nguoiDaiDien ?? undefined,
          chucVuNguoiDaiDien: chiTiet?.chucVuNguoiDaiDien ?? undefined,
          laDonViPheDuyet: chiTiet?.laDonViPheDuyet ?? false,
          capPheDuyet: chiTiet?.capPheDuyet ?? undefined,
          tieuDeVanBan: chiTiet?.tieuDeVanBan ?? undefined,
          nguoiKyMacDinh: chiTiet?.nguoiKyMacDinh ?? undefined,
          chucVuNguoiKyMacDinh: chiTiet?.chucVuNguoiKyMacDinh ?? undefined,
        }}
      >
        <Row gutter={12}>
          <Col xs={24} md={8}>
            <Form.Item name="ma" label="Mã" rules={[{ required: true, message: 'Nhập mã đơn vị' }]}>
              <Input placeholder="VD: PGD-01" />
            </Form.Item>
          </Col>
          <Col xs={24} md={10}>
            <Form.Item name="ten" label="Tên" rules={[{ required: true, message: 'Nhập tên' }]}>
              <Input placeholder="Phòng Giáo dục và Đào tạo" />
            </Form.Item>
          </Col>
          <Col xs={24} md={6}>
            <Form.Item name="tenVietTat" label="Tên viết tắt">
              <Input placeholder="PGDĐT" />
            </Form.Item>
          </Col>
        </Row>

        <Row gutter={12}>
          <Col xs={24} md={10}>
            <Form.Item name="donViChaId" label="Đơn vị cấp trên">
              <Select
                allowClear
                showSearch
                optionFilterProp="label"
                placeholder="Không có (đơn vị gốc)"
                options={donViCha.map((x) => ({ value: x.id, label: x.ten }))}
              />
            </Form.Item>
          </Col>
          <Col xs={24} md={8}>
            <Form.Item name="loai" label="Loại">
              <Select options={LOAI_DON_VI} />
            </Form.Item>
          </Col>
          <Col xs={24} md={6}>
            <Form.Item name="thuTu" label="Thứ tự">
              <InputNumber<number> min={0} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
        </Row>

        <Row gutter={12}>
          <Col xs={24} md={12}>
            <Form.Item name="nguoiDaiDien" label="Người đại diện">
              <Input />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item name="chucVuNguoiDaiDien" label="Chức vụ người đại diện">
              <Input />
            </Form.Item>
          </Col>
        </Row>

        <Form.Item name="diaChi" label="Địa chỉ">
          <Input />
        </Form.Item>

        <Row gutter={12}>
          <Col xs={24} md={8}>
            <Form.Item name="dienThoai" label="Điện thoại">
              <Input />
            </Form.Item>
          </Col>
          <Col xs={24} md={8}>
            <Form.Item
              name="email"
              label="Email"
              rules={[{ type: 'email', message: 'Email không hợp lệ' }]}
            >
              <Input />
            </Form.Item>
          </Col>
          <Col xs={24} md={8}>
            <Form.Item name="laDonViPheDuyet" label="Đơn vị phê duyệt" valuePropName="checked">
              <Switch checkedChildren="Có" unCheckedChildren="Không" />
            </Form.Item>
          </Col>
        </Row>

        <Form.Item
          noStyle
          shouldUpdate={(truoc, sau) => truoc.laDonViPheDuyet !== sau.laDonViPheDuyet}
        >
          {({ getFieldValue }) =>
            getFieldValue('laDonViPheDuyet') ? (
              <Form.Item name="capPheDuyet" label="Cấp phê duyệt">
                <Select allowClear options={CAP_PHE_DUYET} />
              </Form.Item>
            ) : null
          }
        </Form.Item>

        <Typography.Title level={5} style={{ fontSize: 13, marginTop: 8 }}>
          Cấu hình văn bản của đơn vị (chức năng 47)
        </Typography.Title>

        <Form.Item
          name="tieuDeVanBan"
          label="Tiêu đề văn bản"
          tooltip="Dòng cơ quan chủ quản in ở đầu quyết định và các biểu mẫu xuất ra."
        >
          <Input placeholder="ỦY BAN NHÂN DÂN THÀNH PHỐ ..." />
        </Form.Item>

        <Row gutter={12}>
          <Col xs={24} md={12}>
            <Form.Item name="nguoiKyMacDinh" label="Người ký mặc định">
              <Input />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item name="chucVuNguoiKyMacDinh" label="Chức vụ người ký mặc định">
              <Input />
            </Form.Item>
          </Col>
        </Row>

        <Form.Item name="moTa" label="Ghi chú">
          <Input.TextArea rows={2} />
        </Form.Item>
      </Form>
    </Modal>
  );
}
