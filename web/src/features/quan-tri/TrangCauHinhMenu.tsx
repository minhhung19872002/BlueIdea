import { useMemo, useState } from 'react';
import {
  Alert,
  App,
  Button,
  Card,
  Form,
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

import { LoiApi } from '@/api/client';
import { apiCauHinhMenu, apiHeThong, type MucMenuQuanTri } from '@/api/endpoints';
import { KhoiDangTai, KhoiLoi, KhoiRong } from '@/components/ThanhPhanChung';
import { DaiTabTrang } from '@/components/DaiTabTrang';
import { DS_TAB_CAU_HINH } from './cauHinhTab';

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
  const [form] = Form.useForm();

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
    mutationFn: async (giaTri: Record<string, unknown>) => {
      const duLieu = {
        ma: (giaTri.ma as string).trim().toUpperCase(),
        ten: giaTri.ten as string,
        icon: (giaTri.icon as string) || null,
        duongDan: (giaTri.duongDan as string) || null,
        menuChaId: (giaTri.menuChaId as string) ?? null,
        thuTu: (giaTri.thuTu as number) ?? 0,
        quyenMa: (giaTri.quyenMa as string) || null,
        loai,
        hienThi: giaTri.hienThi !== false,
        moTabMoi: !!giaTri.moTabMoi,
      };

      return dangSua ? apiCauHinhMenu.sua(dangSua.id, duLieu) : apiCauHinhMenu.them(duLieu);
    },
    onSuccess: () => {
      message.success(dangSua ? 'Đã cập nhật mục menu' : 'Đã thêm mục menu');
      setMoForm(false);
      setDangSua(null);
      form.resetFields();
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
              form.resetFields();
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
                      form.setFieldsValue(muc);
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
        confirmLoading={luuMuc.isPending}
        onCancel={() => setMoForm(false)}
        onOk={async () => luuMuc.mutate(await form.validateFields())}
      >
        <Form form={form} layout="vertical" initialValues={{ hienThi: true, thuTu: 0 }}>
          <Space size="large" wrap style={{ display: 'flex' }}>
            <Form.Item
              name="ma"
              label="Mã"
              rules={[
                { required: true, message: 'Nhập mã' },
                { pattern: /^[A-Z0-9_]+$/, message: 'Mã chỉ gồm chữ hoa, số và dấu _' },
              ]}
            >
              <Input style={{ width: 220 }} placeholder="VD: BC_TAC_GIA" />
            </Form.Item>
            <Form.Item name="ten" label="Tên hiển thị" rules={[{ required: true, message: 'Nhập tên' }]}>
              <Input style={{ width: 300 }} />
            </Form.Item>
          </Space>

          <Space size="large" wrap style={{ display: 'flex' }}>
            <Form.Item name="icon" label="Icon (emoji)">
              <Input style={{ width: 120 }} placeholder="📊" maxLength={4} />
            </Form.Item>
            <Form.Item
              name="duongDan"
              label="Đường dẫn"
              tooltip="Để trống nếu đây là nhóm chỉ chứa mục con."
            >
              <Input style={{ width: 380 }} placeholder="/bao-cao/theo-tac-gia" />
            </Form.Item>
          </Space>

          <Space size="large" wrap style={{ display: 'flex' }}>
            <Form.Item name="menuChaId" label="Thuộc nhóm">
              <Select
                style={{ width: 300 }}
                allowClear
                placeholder="(mục gốc)"
                options={(data ?? [])
                  .filter((x) => x.id !== dangSua?.id)
                  .map((x) => ({ value: x.id, label: x.ten }))}
              />
            </Form.Item>
            <Form.Item
              name="quyenMa"
              label="Quyền cần có"
              tooltip="Để trống thì mọi người đăng nhập đều thấy mục này."
            >
              <Select
                style={{ width: 280 }}
                allowClear
                showSearch
                optionFilterProp="label"
                placeholder="(ai cũng thấy)"
                options={(
                  (dsQuyen as { quyen?: { ma: string; ten: string }[] } | undefined)?.quyen ?? []
                ).map((q) => ({ value: q.ma, label: `${q.ma} — ${q.ten}` }))}
              />
            </Form.Item>
          </Space>

          <Space size="large" wrap style={{ display: 'flex' }}>
            <Form.Item name="thuTu" label="Thứ tự">
              <InputNumber min={0} max={999} style={{ width: 120 }} />
            </Form.Item>
            <Form.Item name="hienThi" label="Hiển thị" valuePropName="checked">
              <Switch />
            </Form.Item>
            <Form.Item name="moTabMoi" label="Mở tab mới" valuePropName="checked">
              <Switch />
            </Form.Item>
          </Space>
        </Form>
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
