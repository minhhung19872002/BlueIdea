import { useMemo, useState } from 'react';
import {
  App,
  Alert,
  Button,
  Card,
  Checkbox,
  Col,
  Form,
  Input,
  InputNumber,
  Modal,
  Popconfirm,
  Row,
  Select,
  Space,
  Table,
  Tag,
  Tooltip,
  Typography,
} from 'antd';
import { DeleteOutlined, EditOutlined, PlusOutlined, SaveOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { LoiApi } from '@/api/client';
import { apiDonVi, apiHeThong, type LuuVaiTro } from '@/api/endpoints';
import { KhoiDangTai, KhoiLoi } from '@/components/ThanhPhanChung';

interface VaiTro {
  id: string;
  ma: string;
  ten: string;
  moTa?: string | null;
  laHeThong: boolean;
  trangThai: number;
  quyenIds: string[];
  phamVi: { loaiPhamVi: string; donViIds: string[] }[];
}

interface Quyen {
  id: string;
  ma: string;
  ten: string;
  nhomChucNang: string;
}

const LOAI_PHAM_VI = [
  { value: 'CA_NHAN', label: 'Chỉ dữ liệu cá nhân' },
  { value: 'DON_VI', label: 'Trong đơn vị' },
  { value: 'DON_VI_VA_CAP_DUOI', label: 'Đơn vị và cấp dưới' },
  { value: 'TOAN_HE_THONG', label: 'Toàn hệ thống' },
  { value: 'TUY_CHINH', label: 'Danh sách đơn vị tuỳ chọn' },
];

/** Chức năng 45 — Vai trò và ma trận phân quyền (sửa trực tiếp trên bảng). */
export default function TrangVaiTro() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [nhomLoc, setNhomLoc] = useState<string | undefined>();
  const [suaId, setSuaId] = useState<string | null>(null);
  const [moForm, setMoForm] = useState(false);

  /** Thay đổi đang chờ lưu: vaiTroId -> tập quyenId. Chỉ ghi xuống khi bấm "Lưu ma trận". */
  const [nhap, setNhap] = useState<Record<string, Set<string>>>({});

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['vai-tro'],
    queryFn: apiHeThong.vaiTro,
  });

  const duLieu = data as { vaiTro: VaiTro[]; quyen: Quyen[] } | undefined;

  const cacNhom = useMemo(
    () => Array.from(new Set((duLieu?.quyen ?? []).map((q) => q.nhomChucNang))),
    [duLieu],
  );

  const quyenHienThi = useMemo(
    () => (duLieu?.quyen ?? []).filter((q) => !nhomLoc || q.nhomChucNang === nhomLoc),
    [duLieu, nhomLoc],
  );

  const luuMaTran = useMutation({
    mutationFn: async (thayDoi: Record<string, Set<string>>) => {
      for (const [vaiTroId, quyenIds] of Object.entries(thayDoi)) {
        const vaiTro = duLieu?.vaiTro.find((v) => v.id === vaiTroId);
        if (!vaiTro) continue;

        await apiHeThong.suaVaiTro(vaiTroId, {
          ma: vaiTro.ma,
          ten: vaiTro.ten,
          moTa: vaiTro.moTa,
          thuTu: 0,
          trangThai: vaiTro.trangThai,
          quyenIds: Array.from(quyenIds),
          loaiPhamVi: vaiTro.phamVi[0]?.loaiPhamVi ?? 'CA_NHAN',
          donViIds: vaiTro.phamVi[0]?.donViIds ?? [],
        });
      }
    },
    onSuccess: () => {
      message.success('Đã lưu ma trận phân quyền');
      setNhap({});
      void queryClient.invalidateQueries({ queryKey: ['vai-tro'] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không lưu được.'),
  });

  const xoa = useMutation({
    mutationFn: (id: string) => apiHeThong.xoaVaiTro(id),
    onSuccess: () => {
      message.success('Đã xoá vai trò');
      void queryClient.invalidateQueries({ queryKey: ['vai-tro'] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không xoá được.'),
  });

  if (isLoading) return <KhoiDangTai />;
  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  /** Tập quyền hiện hành của một vai trò — ưu tiên phần đang sửa chưa lưu. */
  function tapQuyen(vaiTro: VaiTro): Set<string> {
    return nhap[vaiTro.id] ?? new Set(vaiTro.quyenIds);
  }

  function doiQuyen(vaiTro: VaiTro, quyenId: string, bat: boolean) {
    const hienTai = new Set(tapQuyen(vaiTro));

    if (bat) {
      hienTai.add(quyenId);
    } else {
      hienTai.delete(quyenId);
    }

    setNhap((truoc) => ({ ...truoc, [vaiTro.id]: hienTai }));
  }

  /** Bật/tắt toàn bộ quyền đang hiển thị cho một vai trò. */
  function doiCaCot(vaiTro: VaiTro, bat: boolean) {
    const hienTai = new Set(tapQuyen(vaiTro));

    for (const q of quyenHienThi) {
      if (bat) {
        hienTai.add(q.id);
      } else {
        hienTai.delete(q.id);
      }
    }

    setNhap((truoc) => ({ ...truoc, [vaiTro.id]: hienTai }));
  }

  const coThayDoi = Object.keys(nhap).length > 0;

  return (
    <>
      <Card
        title="Vai trò và ma trận phân quyền"
        extra={
          <Space wrap>
            <Select
              style={{ width: 220 }}
              placeholder="Tất cả nhóm chức năng"
              allowClear
              value={nhomLoc}
              options={cacNhom.map((n) => ({ value: n, label: n }))}
              onChange={setNhomLoc}
            />
            <Button
              icon={<PlusOutlined />}
              onClick={() => {
                setSuaId(null);
                setMoForm(true);
              }}
            >
              Thêm vai trò
            </Button>
            <Button
              type="primary"
              icon={<SaveOutlined />}
              disabled={!coThayDoi}
              loading={luuMaTran.isPending}
              onClick={() => luuMaTran.mutate(nhap)}
            >
              Lưu ma trận
            </Button>
          </Space>
        }
      >
        {coThayDoi && (
          <Alert
            type="warning"
            showIcon
            style={{ marginBottom: 12 }}
            message={`Đang có thay đổi chưa lưu ở ${Object.keys(nhap).length} vai trò.`}
            action={
              <Button size="small" onClick={() => setNhap({})}>
                Huỷ thay đổi
              </Button>
            }
          />
        )}

        <Row gutter={[8, 8]} style={{ marginBottom: 16 }}>
          {(duLieu?.vaiTro ?? []).map((v) => (
            <Col key={v.id} xs={24} sm={12} lg={8}>
              <Card
                size="small"
                actions={[
                  <Tooltip key="sua" title="Sửa thông tin và phạm vi dữ liệu">
                    <Button
                      type="text"
                      size="small"
                      icon={<EditOutlined />}
                      onClick={() => {
                        setSuaId(v.id);
                        setMoForm(true);
                      }}
                    />
                  </Tooltip>,
                  <Popconfirm
                    key="xoa"
                    title="Xoá vai trò này?"
                    description="Chỉ xoá được vai trò không phải hệ thống và chưa gán cho tài khoản nào."
                    okText="Xoá"
                    cancelText="Huỷ"
                    onConfirm={() => xoa.mutate(v.id)}
                  >
                    <Button
                      type="text"
                      size="small"
                      danger
                      icon={<DeleteOutlined />}
                      disabled={v.laHeThong}
                    />
                  </Popconfirm>,
                ]}
              >
                <Typography.Text strong>{v.ten}</Typography.Text>
                {v.laHeThong && (
                  <Tag color="blue" style={{ marginLeft: 8 }}>
                    Hệ thống
                  </Tag>
                )}
                <div style={{ fontSize: 12, color: '#888', marginTop: 4 }}>{v.moTa}</div>
                <div style={{ marginTop: 6 }}>
                  <Tag>{tapQuyen(v).size} quyền</Tag>
                  {v.phamVi.map((p) => (
                    <Tag key={p.loaiPhamVi} color="purple">
                      {LOAI_PHAM_VI.find((l) => l.value === p.loaiPhamVi)?.label ??
                        p.loaiPhamVi.replace(/_/g, ' ')}
                    </Tag>
                  ))}
                </div>
              </Card>
            </Col>
          ))}
        </Row>

        <Table<Quyen>
          rowKey="id"
          size="small"
          dataSource={quyenHienThi}
          scroll={{ x: 200 + (duLieu?.vaiTro.length ?? 0) * 130 }}
          pagination={{ pageSize: 30, showSizeChanger: true }}
          columns={[
            {
              title: 'Chức năng',
              dataIndex: 'ten',
              width: 260,
              fixed: 'left',
              render: (v: string, dong) => (
                <div>
                  <div>{v}</div>
                  <Typography.Text type="secondary" style={{ fontSize: 11 }}>
                    {dong.ma}
                  </Typography.Text>
                </div>
              ),
            },
            { title: 'Nhóm', dataIndex: 'nhomChucNang', width: 130, responsive: ['lg'] },
            ...(duLieu?.vaiTro ?? []).map((v) => {
              const dangCo = tapQuyen(v);
              const soCo = quyenHienThi.filter((q) => dangCo.has(q.id)).length;

              return {
                title: (
                  <div style={{ textAlign: 'center' as const }}>
                    <div style={{ fontSize: 12 }}>{v.ten}</div>
                    <Checkbox
                      checked={soCo === quyenHienThi.length && quyenHienThi.length > 0}
                      indeterminate={soCo > 0 && soCo < quyenHienThi.length}
                      onChange={(e) => doiCaCot(v, e.target.checked)}
                    >
                      <span style={{ fontSize: 11 }}>Tất cả</span>
                    </Checkbox>
                  </div>
                ),
                key: v.id,
                width: 130,
                align: 'center' as const,
                render: (_giaTri: unknown, dong: Quyen) => (
                  <Checkbox
                    checked={dangCo.has(dong.id)}
                    onChange={(e) => doiQuyen(v, dong.id, e.target.checked)}
                  />
                ),
              };
            }),
          ]}
        />

        <Typography.Paragraph type="secondary" style={{ marginTop: 12, fontSize: 12 }}>
          Mọi thay đổi phân quyền đều được ghi vào nhật ký hệ thống kèm giá trị trước và sau. Vai
          trò hệ thống không đổi được mã vì mã được mã nguồn tham chiếu trực tiếp.
        </Typography.Paragraph>
      </Card>

      {moForm && (
        <FormVaiTro
          vaiTro={suaId ? (duLieu?.vaiTro.find((v) => v.id === suaId) ?? null) : null}
          onDong={() => setMoForm(false)}
          onXong={() => {
            setMoForm(false);
            void queryClient.invalidateQueries({ queryKey: ['vai-tro'] });
          }}
        />
      )}
    </>
  );
}

// ---------------------------------------------------------------------------

interface GiaTriFormVaiTro {
  ma: string;
  ten: string;
  moTa?: string;
  thuTu: number;
  loaiPhamVi: string;
  donViIds: string[];
}

function FormVaiTro({
  vaiTro,
  onDong,
  onXong,
}: {
  vaiTro: VaiTro | null;
  onDong: () => void;
  onXong: () => void;
}) {
  const { message } = App.useApp();
  const [form] = Form.useForm<GiaTriFormVaiTro>();
  const [loaiPhamVi, setLoaiPhamVi] = useState(vaiTro?.phamVi[0]?.loaiPhamVi ?? 'CA_NHAN');

  const { data: cacDonVi } = useQuery({ queryKey: ['don-vi-chon'], queryFn: apiDonVi.chon });

  const luu = useMutation({
    mutationFn: (giaTri: LuuVaiTro) =>
      vaiTro ? apiHeThong.suaVaiTro(vaiTro.id, giaTri) : apiHeThong.themVaiTro(giaTri),
    onSuccess: () => {
      message.success(vaiTro ? 'Đã cập nhật vai trò' : 'Đã tạo vai trò');
      onXong();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không lưu được.'),
  });

  async function xacNhan() {
    const giaTri = await form.validateFields();

    luu.mutate({
      ma: giaTri.ma,
      ten: giaTri.ten,
      moTa: giaTri.moTa,
      thuTu: giaTri.thuTu ?? 0,
      trangThai: vaiTro?.trangThai ?? 1,

      // Giữ nguyên ma trận quyền hiện có — quyền được sửa trên bảng ở màn hình chính.
      quyenIds: vaiTro?.quyenIds ?? [],
      loaiPhamVi: giaTri.loaiPhamVi,
      donViIds: giaTri.loaiPhamVi === 'TUY_CHINH' ? (giaTri.donViIds ?? []) : [],
    });
  }

  return (
    <Modal
      open
      width={620}
      title={vaiTro ? `Sửa vai trò ${vaiTro.ma}` : 'Thêm vai trò'}
      okText={vaiTro ? 'Lưu thay đổi' : 'Tạo vai trò'}
      cancelText="Huỷ"
      confirmLoading={luu.isPending}
      onOk={xacNhan}
      onCancel={onDong}
      destroyOnClose
    >
      <Form<GiaTriFormVaiTro>
        form={form}
        layout="vertical"
        initialValues={{
          ma: vaiTro?.ma ?? '',
          ten: vaiTro?.ten ?? '',
          moTa: vaiTro?.moTa ?? undefined,
          thuTu: 0,
          loaiPhamVi: vaiTro?.phamVi[0]?.loaiPhamVi ?? 'CA_NHAN',
          donViIds: vaiTro?.phamVi[0]?.donViIds ?? [],
        }}
      >
        <Row gutter={12}>
          <Col xs={24} md={12}>
            <Form.Item
              name="ma"
              label="Mã vai trò"
              tooltip={vaiTro?.laHeThong ? 'Vai trò hệ thống không đổi được mã.' : undefined}
              rules={[
                { required: true, message: 'Nhập mã vai trò' },
                { pattern: /^[A-Z0-9_]+$/, message: 'Chỉ dùng chữ HOA không dấu, số và dấu _' },
              ]}
            >
              <Input disabled={vaiTro?.laHeThong} placeholder="VD: THU_KY_HOI_DONG" />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item
              name="ten"
              label="Tên vai trò"
              rules={[{ required: true, message: 'Nhập tên vai trò' }]}
            >
              <Input placeholder="Thư ký hội đồng" />
            </Form.Item>
          </Col>
        </Row>

        <Form.Item name="moTa" label="Mô tả">
          <Input.TextArea rows={2} />
        </Form.Item>

        <Row gutter={12}>
          <Col xs={24} md={16}>
            <Form.Item
              name="loaiPhamVi"
              label="Phạm vi dữ liệu"
              tooltip="Quyết định vai trò này được xem hồ sơ của những đơn vị nào."
            >
              <Select options={LOAI_PHAM_VI} onChange={setLoaiPhamVi} />
            </Form.Item>
          </Col>
          <Col xs={24} md={8}>
            <Form.Item name="thuTu" label="Thứ tự hiển thị">
              <InputNumber<number> min={0} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
        </Row>

        {loaiPhamVi === 'TUY_CHINH' && (
          <Form.Item
            name="donViIds"
            label="Đơn vị được xem"
            rules={[{ required: true, message: 'Chọn ít nhất một đơn vị' }]}
          >
            <Select
              mode="multiple"
              optionFilterProp="label"
              placeholder="Chọn các đơn vị"
              options={(cacDonVi ?? []).map((x) => ({ value: x.id, label: x.ten }))}
            />
          </Form.Item>
        )}

        {vaiTro && (
          <Typography.Text type="secondary">
            Ma trận quyền của vai trò này được chỉnh trực tiếp trên bảng ở màn hình chính rồi bấm
            “Lưu ma trận”.
          </Typography.Text>
        )}
      </Form>
    </Modal>
  );
}
