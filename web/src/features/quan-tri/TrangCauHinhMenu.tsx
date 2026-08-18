import { useMemo, useState } from 'react';
import {
  Alert,
  App,
  Button,
  Card,
  Input,
  InputNumber,
  Modal,
  Select,
  Space,
  Switch,
  Tag,
  Tooltip,
  Tree,
  Typography,
} from 'antd';
import { DeleteOutlined, EditOutlined, PlusOutlined, SaveOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { DataNode } from 'antd/es/tree';

import { z } from 'zod';

import { LoiApi } from '@/api/client';
import { BieuMau, Truong, useBieuMau } from '@/components/bieu-mau/BieuMau';
import { batBuoc, soNguyen, tuyChon } from '@/components/bieu-mau/luat';
import { apiCauHinhMenu, apiHeThong, type MucMenuQuanTri } from '@/api/endpoints';
import { KhoiDangTai, KhoiLoi, KhoiRong } from '@/components/ThanhPhanChung';
import { DaiTabTrang } from '@/components/DaiTabTrang';
import { DS_TAB_CAU_HINH } from './cauHinhTab';

/**
 * Luật kiểm tra mục menu.
 *
 * Đường dẫn để trống nghĩa là mục này chỉ là NHÓM chứa mục con — hợp lệ, nên không bắt buộc.
 * Nhưng nếu có nhập thì phải bắt đầu bằng "/": đường dẫn tương đối sẽ ghép sai khi người dùng
 * đang đứng ở một trang con, và mục menu đó dẫn tới trang 404.
 */
const luatMucMenu = z.object({
  ma: batBuoc('Mã', 50).regex(/^[A-Z0-9_]+$/, 'Mã chỉ gồm chữ hoa, số và dấu _'),
  ten: batBuoc('Tên hiển thị'),
  icon: tuyChon(8),
  duongDan: z
    .string()
    .trim()
    .startsWith('/', 'Đường dẫn phải bắt đầu bằng dấu /')
    .optional()
    .or(z.literal('').transform(() => undefined)),
  menuChaId: z.string().uuid().optional().nullable(),
  quyenMa: z.string().optional().nullable(),
  thuTu: soNguyen('Thứ tự', 0, 999),
  hienThi: z.boolean(),
  moTabMoi: z.boolean(),
});

type GiaTriMucMenu = z.infer<typeof luatMucMenu>;

const MAC_DINH_MENU: GiaTriMucMenu = {
  ma: '',
  ten: '',
  thuTu: 0,
  hienThi: true,
  moTabMoi: false,
};

interface NutSapXep {
  id: string;
  con?: NutSapXep[];
}

/**
 * Chức năng 48 — Cấu hình menu điều hướng.
 *
 * Web và Mobile là hai cây riêng: điện thoại chỉ hiển thị được vài mục, ép dùng chung một cây thì
 * hoặc menu web nghèo đi, hoặc menu mobile dài không dùng nổi.
 */
export default function TrangCauHinhMenu() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [loai, setLoai] = useState<'WEB' | 'MOBILE'>('WEB');
  const [cayNhap, setCayNhap] = useState<DataNode[] | null>(null);
  const [dangSua, setDangSua] = useState<MucMenuQuanTri | null>(null);
  const [moForm, setMoForm] = useState(false);
  const form = useBieuMau(luatMucMenu, MAC_DINH_MENU);

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['cau-hinh-menu', loai],
    queryFn: () => apiCauHinhMenu.danhSach(loai),
  });

  const { data: dsQuyen } = useQuery({
    queryKey: ['vai-tro'],
    queryFn: apiHeThong.vaiTro,
  });

  const theoId = useMemo(() => new Map((data ?? []).map((x) => [x.id, x])), [data]);

  const cayGoc = useMemo<DataNode[]>(() => {
    const dung = (chaId: string | null): DataNode[] =>
      (data ?? [])
        .filter((x) => (x.menuChaId ?? null) === chaId)
        .sort((a, b) => a.thuTu - b.thuTu)
        .map((x) => ({
          key: x.id,
          title: (
            <Space size={6}>
              {x.icon && <span>{x.icon}</span>}
              <span>{x.ten}</span>
              <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                {x.duongDan ?? '(nhóm)'}
              </Typography.Text>
              {!x.hienThi && <Tag>Đang ẩn</Tag>}
              {x.quyenMa && <Tag color="purple">{x.quyenMa}</Tag>}
            </Space>
          ),
          children: dung(x.id),
        }));

    return dung(null);
  }, [data]);

  const cay = cayNhap ?? cayGoc;
  const coThayDoi = cayNhap !== null;

  const luuThuTu = useMutation({
    mutationFn: () => apiCauHinhMenu.sapXep(loai, rutGon(cay)),
    onSuccess: () => {
      message.success('Đã lưu thứ tự menu');
      setCayNhap(null);
      void queryClient.invalidateQueries({ queryKey: ['cau-hinh-menu'] });
      // Thanh điều hướng đang hiển thị menu cũ — nạp lại để người sửa thấy ngay kết quả.
      void queryClient.invalidateQueries({ queryKey: ['menu'] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không lưu được.'),
  });

  const luuMuc = useMutation({
    mutationFn: async (giaTri: GiaTriMucMenu) => {
      const duLieu = {
        ma: giaTri.ma.trim().toUpperCase(),
        ten: giaTri.ten,
        icon: giaTri.icon ?? null,
        duongDan: giaTri.duongDan ?? null,
        menuChaId: giaTri.menuChaId ?? null,
        thuTu: giaTri.thuTu,
        quyenMa: giaTri.quyenMa ?? null,
        loai,
        hienThi: giaTri.hienThi,
        moTabMoi: giaTri.moTabMoi,
      };

      return dangSua ? apiCauHinhMenu.sua(dangSua.id, duLieu) : apiCauHinhMenu.them(duLieu);
    },
    onSuccess: () => {
      message.success(dangSua ? 'Đã cập nhật mục menu' : 'Đã thêm mục menu');
      setMoForm(false);
      setDangSua(null);
      form.reset(MAC_DINH_MENU);
      setCayNhap(null);
      void queryClient.invalidateQueries({ queryKey: ['cau-hinh-menu'] });
      void queryClient.invalidateQueries({ queryKey: ['menu'] });
    },
    onError: (loi) =>
      modal.error({
        title: 'Không lưu được mục menu',
        content: loi instanceof LoiApi ? loi.message : 'Đã xảy ra lỗi.',
      }),
  });

  const xoa = useMutation({
    mutationFn: (id: string) => apiCauHinhMenu.xoa(id),
    onSuccess: () => {
      message.success('Đã xoá mục menu');
      setCayNhap(null);
      void queryClient.invalidateQueries({ queryKey: ['cau-hinh-menu'] });
      void queryClient.invalidateQueries({ queryKey: ['menu'] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không xoá được.'),
  });

  if (isLoading) return <KhoiDangTai />;
  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  return (
    <Card
      title="Cấu hình hệ thống"
      extra={
        <Space>
          <Select<'WEB' | 'MOBILE'>
            style={{ width: 170 }}
            value={loai}
            options={[
              { value: 'WEB', label: 'Menu Web' },
              { value: 'MOBILE', label: 'Menu Mobile' },
            ]}
            onChange={(v) => {
              setLoai(v);
              setCayNhap(null);
            }}
          />
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => {
              setDangSua(null);
              form.reset(MAC_DINH_MENU);
              setMoForm(true);
            }}
          >
            Thêm mục
          </Button>
        </Space>
      }
    >
      <DaiTabTrang danhSach={DS_TAB_CAU_HINH} dangChon="menu" />

      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 12 }}
        message="Kéo thả để đổi thứ tự hoặc chuyển mục vào nhóm khác — thả vào giữa hai mục là cùng cấp, thả lên một mục là làm mục con."
        description="Thay đổi chỉ được ghi khi bấm Lưu thứ tự. Mục ẩn vẫn giữ cấu hình nhưng không hiện trên thanh điều hướng."
      />

      {coThayDoi && (
        <Space style={{ marginBottom: 12 }}>
          <Button
            type="primary"
            icon={<SaveOutlined />}
            loading={luuThuTu.isPending}
            onClick={() => luuThuTu.mutate()}
          >
            Lưu thứ tự
          </Button>
          <Button onClick={() => setCayNhap(null)}>Huỷ thay đổi</Button>
        </Space>
      )}

      {cay.length === 0 ? (
        <KhoiRong moTa={`Chưa cấu hình mục menu nào cho ${loai === 'WEB' ? 'web' : 'mobile'}.`} />
      ) : (
        <Tree
          treeData={cay}
          defaultExpandAll
          showLine
          blockNode
          draggable
          selectable={false}
          titleRender={(nut) => (
            <Space style={{ width: '100%', justifyContent: 'space-between' }}>
              <span>{nut.title as React.ReactNode}</span>
              <Space size={2}>
                <Tooltip title="Sửa">
                  <Button
                    size="small"
                    type="text"
                    icon={<EditOutlined />}
                    onClick={(e) => {
                      e.stopPropagation();
                      const muc = theoId.get(String(nut.key));
                      if (!muc) return;

                      setDangSua(muc);
                      form.reset({
                        ma: muc.ma,
                        ten: muc.ten,
                        icon: muc.icon ?? undefined,
                        duongDan: muc.duongDan ?? undefined,
                        menuChaId: muc.menuChaId ?? undefined,
                        quyenMa: muc.quyenMa ?? undefined,
                        thuTu: muc.thuTu,
                        hienThi: muc.hienThi,
                        moTabMoi: muc.moTabMoi,
                      });
                      setMoForm(true);
                    }}
                  />
                </Tooltip>
                <Tooltip title="Xoá">
                  <Button
                    size="small"
                    type="text"
                    danger
                    icon={<DeleteOutlined />}
                    onClick={(e) => {
                      e.stopPropagation();
                      const muc = theoId.get(String(nut.key));
                      if (!muc) return;

                      modal.confirm({
                        title: 'Xoá mục menu',
                        content: `Xoá "${muc.ten}" khỏi menu ${loai}?`,
                        okText: 'Xoá',
                        okButtonProps: { danger: true },
                        cancelText: 'Huỷ',
                        onOk: () => xoa.mutateAsync(muc.id),
                      });
                    }}
                  />
                </Tooltip>
              </Space>
            </Space>
          )}
          onDrop={(thongTin) => setCayNhap(thaVao(cay, thongTin))}
        />
      )}

      <Modal
        open={moForm}
        width={640}
        title={dangSua ? `Sửa mục menu: ${dangSua.ten}` : `Thêm mục menu ${loai}`}
        okText="Lưu"
        cancelText="Huỷ"
        confirmLoading={luuMuc.isPending || form.formState.isSubmitting}
        onCancel={() => setMoForm(false)}
        okButtonProps={{ htmlType: 'submit', form: 'form-muc-menu' }}
      >
        <BieuMau id="form-muc-menu" form={form} onGui={(giaTri) => luuMuc.mutateAsync(giaTri)}>
          <Space size="large" wrap style={{ display: 'flex' }}>
            <Truong<GiaTriMucMenu> ten="ma" label="Mã" required>
              {(o) => (
                <Input
                  {...o}
                  value={o.value as string}
                  style={{ width: 220 }}
                  placeholder="VD: BC_TAC_GIA"
                />
              )}
            </Truong>
            <Truong<GiaTriMucMenu> ten="ten" label="Tên hiển thị" required>
              {(o) => <Input {...o} value={o.value as string} style={{ width: 300 }} />}
            </Truong>
          </Space>

          <Space size="large" wrap style={{ display: 'flex' }}>
            <Truong<GiaTriMucMenu> ten="icon" label="Icon (emoji)">
              {(o) => (
                <Input
                  {...o}
                  value={o.value as string}
                  style={{ width: 120 }}
                  placeholder="📊"
                  maxLength={4}
                />
              )}
            </Truong>
            <Truong<GiaTriMucMenu>
              ten="duongDan"
              label="Đường dẫn"
              tooltip="Để trống nếu đây là nhóm chỉ chứa mục con."
            >
              {(o) => (
                <Input
                  {...o}
                  value={o.value as string}
                  style={{ width: 380 }}
                  placeholder="/bao-cao/theo-tac-gia"
                />
              )}
            </Truong>
          </Space>

          <Space size="large" wrap style={{ display: 'flex' }}>
            <Truong<GiaTriMucMenu> ten="menuChaId" label="Thuộc nhóm">
              {(o) => (
                <Select
                  {...o}
                  value={o.value as string | undefined}
                  style={{ width: 300 }}
                  allowClear
                  placeholder="(mục gốc)"
                  options={(data ?? [])
                    .filter((x) => x.id !== dangSua?.id)
                    .map((x) => ({ value: x.id, label: x.ten }))}
                />
              )}
            </Truong>
            <Truong<GiaTriMucMenu>
              ten="quyenMa"
              label="Quyền cần có"
              tooltip="Để trống thì mọi người đăng nhập đều thấy mục này."
            >
              {(o) => (
                <Select
                  {...o}
                  value={o.value as string | undefined}
                  style={{ width: 280 }}
                  allowClear
                  showSearch
                  optionFilterProp="label"
                  placeholder="(ai cũng thấy)"
                  options={(
                    (dsQuyen as { quyen?: { ma: string; ten: string }[] } | undefined)?.quyen ?? []
                  ).map((q) => ({ value: q.ma, label: `${q.ma} — ${q.ten}` }))}
                />
              )}
            </Truong>
          </Space>

          <Space size="large" wrap style={{ display: 'flex' }}>
            <Truong<GiaTriMucMenu> ten="thuTu" label="Thứ tự">
              {(o) => (
                <InputNumber
                  {...o}
                  value={o.value as number}
                  min={0}
                  max={999}
                  style={{ width: 120 }}
                />
              )}
            </Truong>
            <Truong<GiaTriMucMenu> ten="hienThi" label="Hiển thị">
              {(o) => <Switch checked={!!o.value} onChange={o.onChange} />}
            </Truong>
            <Truong<GiaTriMucMenu> ten="moTabMoi" label="Mở tab mới">
              {(o) => <Switch checked={!!o.value} onChange={o.onChange} />}
            </Truong>
          </Space>
        </BieuMau>
      </Modal>
    </Card>
  );
}

/** Chuyển cây hiển thị thành cấu trúc gửi lên máy chủ (chỉ cần Id và lồng nhau). */
function rutGon(cay: DataNode[]): NutSapXep[] {
  return cay.map((x) => ({
    id: String(x.key),
    con: x.children?.length ? rutGon(x.children) : undefined,
  }));
}

/** Dựng cây mới sau một lần kéo thả của antd Tree. */
function thaVao(
  cay: DataNode[],
  thongTin: {
    dragNode: DataNode;
    node: DataNode;
    dropToGap: boolean;
    dropPosition: number;
  },
): DataNode[] {
  const khoaKeo = String(thongTin.dragNode.key);
  const khoaDich = String(thongTin.node.key);

  let nutKeo: DataNode | null = null;

  const goRa = (cacNut: DataNode[]): DataNode[] =>
    cacNut
      .filter((x) => {
        if (String(x.key) !== khoaKeo) return true;
        nutKeo = x;
        return false;
      })
      .map((x) => ({ ...x, children: x.children ? goRa(x.children) : undefined }));

  const conLai = goRa(cay);
  if (!nutKeo) return cay;

  const chen = (cacNut: DataNode[]): DataNode[] => {
    const ketQua: DataNode[] = [];

    cacNut.forEach((x) => {
      if (String(x.key) !== khoaDich) {
        ketQua.push({ ...x, children: x.children ? chen(x.children) : undefined });
        return;
      }

      if (!thongTin.dropToGap) {
        // Thả LÊN một mục = đưa vào làm mục con cuối cùng của mục đó.
        ketQua.push({ ...x, children: [...(x.children ?? []), nutKeo!] });
        return;
      }

      // dropPosition < vị trí của nút đích nghĩa là thả phía trên nó.
      if (thongTin.dropPosition < 0) {
        ketQua.push(nutKeo!, { ...x, children: x.children ? chen(x.children) : undefined });
      } else {
        ketQua.push({ ...x, children: x.children ? chen(x.children) : undefined }, nutKeo!);
      }
    });

    return ketQua;
  };

  return chen(conLai);
}
