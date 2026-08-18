import { useMemo, useState } from 'react';
import {
  App,
  Alert,
  Button,
  Card,
  Checkbox,
  Col,
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
import { CopyOutlined, DeleteOutlined, EditOutlined, PlusOutlined, SaveOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { z } from 'zod';

import { LoiApi } from '@/api/client';
import { BieuMau, Truong, useBieuMau } from '@/components/bieu-mau/BieuMau';
import { batBuoc, soNguyen, tuyChon } from '@/components/bieu-mau/luat';
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
  const [saoChep, setSaoChep] = useState<VaiTro | null>(null);

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
                  <Tooltip key="sao-chep" title="Tạo vai trò mới với đúng bộ quyền và phạm vi dữ liệu này">
                    <Button
                      type="text"
                      size="small"
                      icon={<CopyOutlined />}
                      onClick={() => setSaoChep(v)}
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

      {saoChep && (
        <HopThoaiSaoChepVaiTro
          nguon={saoChep}
          onDong={() => setSaoChep(null)}
          onXong={() => {
            setSaoChep(null);
            void queryClient.invalidateQueries({ queryKey: ['vai-tro'] });
          }}
        />
      )}

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

/**
 * Luật kiểm tra vai trò.
 *
 * Phạm vi TUY_CHINH bắt buộc chọn ít nhất một đơn vị: để trống thì vai trò đó không xem được hồ
 * sơ nào, người được gán sẽ thấy màn hình trắng mà không hiểu vì sao. Dùng `superRefine` để luật
 * này phụ thuộc vào giá trị của trường khác — thứ mà prop `rules` rời rạc không diễn đạt được.
 */
const luatVaiTro = z
  .object({
    ma: batBuoc('Mã vai trò', 50).regex(
      /^[A-Z0-9_]+$/,
      'Chỉ dùng chữ HOA không dấu, số và dấu _',
    ),
    ten: batBuoc('Tên vai trò'),
    moTa: tuyChon(),
    thuTu: soNguyen('Thứ tự hiển thị', 0, 9999),
    loaiPhamVi: z.string(),
    donViIds: z.array(z.string()),
  })
  .superRefine((v, ctx) => {
    if (v.loaiPhamVi === 'TUY_CHINH' && v.donViIds.length === 0) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['donViIds'],
        message: 'Chọn ít nhất một đơn vị cho phạm vi tuỳ chỉnh.',
      });
    }
  });

type GiaTriFormVaiTro = z.infer<typeof luatVaiTro>;

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
  const form = useBieuMau(luatVaiTro, {
    ma: vaiTro?.ma ?? '',
    ten: vaiTro?.ten ?? '',
    moTa: vaiTro?.moTa ?? undefined,
    thuTu: 0,
    loaiPhamVi: vaiTro?.phamVi[0]?.loaiPhamVi ?? 'CA_NHAN',
    donViIds: vaiTro?.phamVi[0]?.donViIds ?? [],
  } as GiaTriFormVaiTro);

  // Theo dõi trực tiếp giá trị trong form thay vì giữ một state song song: hai nguồn dễ lệch nhau
  // khi form được nạp lại.
  const loaiPhamVi = form.watch('loaiPhamVi');

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

  function xacNhan(giaTri: GiaTriFormVaiTro) {
    return luu.mutateAsync({
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
      confirmLoading={luu.isPending || form.formState.isSubmitting}
      okButtonProps={{ htmlType: 'submit', form: 'form-vai-tro' }}
      onCancel={onDong}
      destroyOnClose
    >
      <BieuMau id="form-vai-tro" form={form} onGui={xacNhan}>
        <Row gutter={12}>
          <Col xs={24} md={12}>
            <Truong<GiaTriFormVaiTro>
              ten="ma"
              label="Mã vai trò"
              required
              tooltip={vaiTro?.laHeThong ? 'Vai trò hệ thống không đổi được mã.' : undefined}
            >
              {(o) => (
                <Input
                  {...o}
                  value={o.value as string}
                  disabled={vaiTro?.laHeThong}
                  placeholder="VD: THU_KY_HOI_DONG"
                />
              )}
            </Truong>
          </Col>
          <Col xs={24} md={12}>
            <Truong<GiaTriFormVaiTro> ten="ten" label="Tên vai trò" required>
              {(o) => <Input {...o} value={o.value as string} placeholder="Thư ký hội đồng" />}
            </Truong>
          </Col>
        </Row>

        <Truong<GiaTriFormVaiTro> ten="moTa" label="Mô tả">
          {(o) => <Input.TextArea {...o} value={o.value as string} rows={2} />}
        </Truong>

        <Row gutter={12}>
          <Col xs={24} md={16}>
            <Truong<GiaTriFormVaiTro>
              ten="loaiPhamVi"
              label="Phạm vi dữ liệu"
              tooltip="Quyết định vai trò này được xem hồ sơ của những đơn vị nào."
            >
              {(o) => <Select {...o} value={o.value as string} options={LOAI_PHAM_VI} />}
            </Truong>
          </Col>
          <Col xs={24} md={8}>
            <Truong<GiaTriFormVaiTro> ten="thuTu" label="Thứ tự hiển thị">
              {(o) => (
                <InputNumber<number>
                  {...o}
                  value={o.value as number}
                  min={0}
                  style={{ width: '100%' }}
                />
              )}
            </Truong>
          </Col>
        </Row>

        {loaiPhamVi === 'TUY_CHINH' && (
          <Truong<GiaTriFormVaiTro> ten="donViIds" label="Đơn vị được xem" required>
            {(o) => (
              <Select
                {...o}
                value={o.value as string[]}
                mode="multiple"
                optionFilterProp="label"
                placeholder="Chọn các đơn vị"
                options={(cacDonVi ?? []).map((x) => ({ value: x.id, label: x.ten }))}
              />
            )}
          </Truong>
        )}

        {vaiTro && (
          <Typography.Text type="secondary">
            Ma trận quyền của vai trò này được chỉnh trực tiếp trên bảng ở màn hình chính rồi bấm
            “Lưu ma trận”.
          </Typography.Text>
        )}
      </BieuMau>
    </Modal>
  );
}

const luatSaoChepVaiTro = z.object({
  ma: batBuoc('Mã vai trò', 50).regex(
    /^[A-Za-z0-9_]+$/,
    'Mã chỉ gồm chữ, số và dấu gạch dưới.',
  ),
  ten: batBuoc('Tên vai trò mới'),
});

type GiaTriSaoChepVaiTro = z.infer<typeof luatSaoChepVaiTro>;

/**
 * Sao chép vai trò — nhân bản đúng bộ quyền và phạm vi dữ liệu sang một vai trò mới.
 *
 * Bản sao luôn là vai trò thường (không phải vai trò hệ thống), vì vai trò hệ thống được chặn xoá:
 * sao ra mà giữ nguyên cờ đó thì sinh ra vai trò rác không bao giờ xoá được.
 */
function HopThoaiSaoChepVaiTro({
  nguon,
  onDong,
  onXong,
}: {
  nguon: VaiTro;
  onDong: () => void;
  onXong: () => void;
}) {
  const { message } = App.useApp();
  const form = useBieuMau(luatSaoChepVaiTro, { ten: `${nguon.ten} (bản sao)` } as GiaTriSaoChepVaiTro);

  const luu = useMutation({
    mutationFn: (giaTri: GiaTriSaoChepVaiTro) =>
      apiHeThong.saoChepVaiTro(nguon.id, { ma: giaTri.ma.trim().toUpperCase(), ten: giaTri.ten }),
    onSuccess: () => {
      message.success('Đã tạo vai trò mới từ bản sao');
      onXong();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không sao chép được.'),
  });

  return (
    <Modal
      open
      title={`Sao chép vai trò: ${nguon.ten}`}
      okText="Tạo bản sao"
      cancelText="Huỷ"
      confirmLoading={luu.isPending || form.formState.isSubmitting}
      onCancel={onDong}
      okButtonProps={{ htmlType: 'submit', form: 'form-sao-chep-vai-tro' }}
    >
      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 12 }}
        message={`Bản sao nhận đủ ${nguon.quyenIds.length} quyền và ${nguon.phamVi.length} phạm vi dữ liệu của vai trò gốc.`}
        description="Sau khi tạo, chỉnh lại quyền trên ma trận bên dưới — sửa bản sao không ảnh hưởng vai trò gốc."
      />
      <BieuMau id="form-sao-chep-vai-tro" form={form} onGui={(giaTri) => luu.mutateAsync(giaTri)}>
        <Truong<GiaTriSaoChepVaiTro> ten="ma" label="Mã vai trò mới" required>
          {(o) => <Input {...o} value={o.value as string} placeholder="VD: THU_KY_HOI_DONG_2" />}
        </Truong>
        <Truong<GiaTriSaoChepVaiTro> ten="ten" label="Tên vai trò mới" required>
          {(o) => <Input {...o} value={o.value as string} />}
        </Truong>
      </BieuMau>
    </Modal>
  );
}
