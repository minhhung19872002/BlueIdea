import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  App,
  Alert,
  Button,
  Card,
  Input,
  Modal,
  Select,
  Space,
  Table,
  Tag,
  Typography,
} from 'antd';
import { ArrowLeftOutlined, DeleteOutlined, EditOutlined, PlusOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';

import { LoiApi } from '@/api/client';
import {
  apiLienThongBuoc,
  apiQuyTrinh,
  apiTichHop,
  type LienThongBuoc,
} from '@/api/endpoints';
import { KhoiLoi, KhoiRong } from '@/components/ThanhPhanChung';
import { BieuMau, Truong, useBieuMau } from '@/components/bieu-mau/BieuMau';
import { tuyChon } from '@/components/bieu-mau/luat';

const SU_KIEN = [
  { value: 'KHI_VAO_BUOC', label: 'Khi hồ sơ vào bước' },
  { value: 'KHI_HOAN_THANH', label: 'Khi hoàn thành bước' },
  { value: 'KHI_PHE_DUYET', label: 'Khi được phê duyệt' },
];

/**
 * Luật kiểm tra cấu hình liên thông theo bước.
 *
 * Đồng bộ hai chiều bắt buộc phải khai loại dữ liệu: chiều đẩy đi thì hệ thống ngoài tự hiểu qua
 * ngữ cảnh lời gọi, nhưng chiều nhận về đi qua API công khai dùng chung — không có nhãn loại dữ
 * liệu thì gói tin về không biết thuộc luồng nào và bị bỏ lặng.
 */
const luatLienThong = z
  .object({
    buocId: z.string().uuid().optional().nullable(),
    heThongTichHopId: z.string().uuid('Vui lòng chọn hệ thống liên thông.'),
    suKien: z.string(),
    loaiDuLieu: tuyChon(100),
    trangThai: z.number(),
    dongBoHaiChieu: z.boolean(),
  })
  .superRefine((v, ctx) => {
    if (v.dongBoHaiChieu && !v.loaiDuLieu?.trim()) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['loaiDuLieu'],
        message: 'Đồng bộ hai chiều phải khai loại dữ liệu để định tuyến gói tin nhận về.',
      });
    }
  });

type GiaTriLienThong = z.infer<typeof luatLienThong>;

const MAC_DINH_LIEN_THONG: GiaTriLienThong = {
  heThongTichHopId: '',
  suKien: 'KHI_HOAN_THANH',
  trangThai: 1,
  dongBoHaiChieu: false,
};

/**
 * Chức năng 16 — Gắn hệ thống liên thông vào từng bước của quy trình.
 *
 * Khác màn hình *Liên thông hệ thống ngoài* (khai báo endpoint, khoá): ở đây quyết định
 * **khi nào** gọi hệ thống nào — hồ sơ chạy tới bước nào thì đẩy dữ liệu đi.
 */
export default function TrangLienThongBuoc() {
  const { id = '' } = useParams<{ id: string }>();
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [dangSua, setDangSua] = useState<LienThongBuoc | null>(null);
  const [moForm, setMoForm] = useState(false);
  const form = useBieuMau(luatLienThong, MAC_DINH_LIEN_THONG);

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['lien-thong-buoc', id],
    queryFn: () => apiLienThongBuoc.danhSach(id),
  });

  const { data: soDo } = useQuery({
    queryKey: ['quy-trinh-so-do', id],
    queryFn: () => apiQuyTrinh.soDo(id),
  });

  const { data: heThong } = useQuery({
    queryKey: ['he-thong-tich-hop'],
    queryFn: apiTichHop.danhSach,
  });

  function lamMoi() {
    void queryClient.invalidateQueries({ queryKey: ['lien-thong-buoc', id] });
  }

  const luu = useMutation({
    mutationFn: async (giaTri: GiaTriLienThong) => {
      const duLieu = {
        buocId: giaTri.buocId || null,
        heThongTichHopId: giaTri.heThongTichHopId,
        suKien: giaTri.suKien,
        loaiDuLieu: giaTri.loaiDuLieu || null,
        dongBoHaiChieu: giaTri.dongBoHaiChieu,
        trangThai: giaTri.trangThai,
      };

      return dangSua
        ? apiLienThongBuoc.sua(dangSua.id, duLieu)
        : apiLienThongBuoc.them(id, duLieu);
    },
    onSuccess: () => {
      message.success(dangSua ? 'Đã cập nhật cấu hình liên thông' : 'Đã thêm cấu hình liên thông');
      setMoForm(false);
      setDangSua(null);
      form.reset(MAC_DINH_LIEN_THONG);
      lamMoi();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không lưu được.'),
  });

  const xoa = useMutation({
    mutationFn: (idXoa: string) => apiLienThongBuoc.xoa(idXoa),
    onSuccess: () => {
      message.success('Đã xoá cấu hình liên thông');
      lamMoi();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không xoá được.'),
  });

  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  const coHeThong = (heThong?.length ?? 0) > 0;

  return (
    <Card
      title="Liên thông theo bước quy trình"
      extra={
        <Space>
          <Link to="/quan-tri/quy-trinh">
            <Button icon={<ArrowLeftOutlined />}>Danh sách quy trình</Button>
          </Link>
          <Link to={`/quan-tri/quy-trinh/${id}/thiet-ke`}>
            <Button>Trình thiết kế</Button>
          </Link>
          <Button
            type="primary"
            icon={<PlusOutlined />}
            disabled={!coHeThong}
            onClick={() => {
              setDangSua(null);
              form.reset(MAC_DINH_LIEN_THONG);
              setMoForm(true);
            }}
          >
            Thêm cấu hình
          </Button>
        </Space>
      }
    >
      <Alert
        type={coHeThong ? 'info' : 'warning'}
        showIcon
        style={{ marginBottom: 12 }}
        message={
          coHeThong
            ? 'Cấu hình ở đây quyết định KHI NÀO đẩy dữ liệu sang hệ thống ngoài.'
            : 'Chưa khai báo hệ thống liên thông nào.'
        }
        description={
          coHeThong ? (
            'Endpoint, khoá và cách xác thực của từng hệ thống khai báo ở màn hình Quản trị → Liên thông hệ thống ngoài.'
          ) : (
            <Link to="/quan-tri/lien-thong">Vào Quản trị → Liên thông hệ thống ngoài để khai báo trước →</Link>
          )
        }
      />

      <Table<LienThongBuoc>
        rowKey="id"
        size="middle"
        loading={isLoading}
        dataSource={data ?? []}
        pagination={false}
        scroll={{ x: 1000 }}
        locale={{ emptyText: <KhoiRong moTa="Quy trình này chưa gắn liên thông ở bước nào." /> }}
        columns={[
          {
            title: 'Bước',
            dataIndex: 'tenBuoc',
            width: 260,
            render: (v: string | null) =>
              v ?? <Typography.Text type="secondary">Toàn quy trình</Typography.Text>,
          },
          {
            title: 'Sự kiện kích hoạt',
            dataIndex: 'suKien',
            width: 200,
            render: (v: string) => SU_KIEN.find((x) => x.value === v)?.label ?? v,
          },
          { title: 'Hệ thống nhận', dataIndex: 'tenHeThong', width: 240 },
          {
            title: 'Loại dữ liệu',
            dataIndex: 'loaiDuLieu',
            width: 200,
            render: (v: string | null) => v ?? '—',
          },
          {
            title: 'Trạng thái',
            dataIndex: 'trangThai',
            width: 120,
            render: (v: number) =>
              v === 1 ? <Tag color="success">Hoạt động</Tag> : <Tag>Ngừng</Tag>,
          },
          {
            title: '',
            key: 'thaoTac',
            width: 100,
            fixed: 'right',
            render: (_v, dong) => (
              <Space>
                <Button
                  type="text"
                  icon={<EditOutlined />}
                  onClick={() => {
                    setDangSua(dong);
                    form.reset({
                      buocId: dong.buocId ?? null,
                      heThongTichHopId: dong.heThongTichHopId,
                      suKien: dong.suKien,
                      loaiDuLieu: dong.loaiDuLieu ?? undefined,
                      trangThai: dong.trangThai,
                      dongBoHaiChieu: dong.dongBoHaiChieu,
                    });
                    setMoForm(true);
                  }}
                />
                <Button
                  type="text"
                  danger
                  icon={<DeleteOutlined />}
                  onClick={() =>
                    modal.confirm({
                      title: 'Xoá cấu hình liên thông',
                      content: `Hồ sơ chạy qua bước "${dong.tenBuoc ?? 'toàn quy trình'}" sẽ không còn đẩy dữ liệu sang ${dong.tenHeThong}.`,
                      okText: 'Xoá',
                      okButtonProps: { danger: true },
                      cancelText: 'Huỷ',
                      onOk: () => xoa.mutateAsync(dong.id),
                    })
                  }
                />
              </Space>
            ),
          },
        ]}
      />

      <Modal
        open={moForm}
        width={620}
        title={dangSua ? 'Sửa cấu hình liên thông' : 'Thêm cấu hình liên thông'}
        okText="Lưu"
        cancelText="Huỷ"
        confirmLoading={luu.isPending || form.formState.isSubmitting}
        onCancel={() => setMoForm(false)}
        okButtonProps={{ htmlType: 'submit', form: 'form-lien-thong-buoc' }}
      >
        <BieuMau id="form-lien-thong-buoc" form={form} onGui={(giaTri) => luu.mutateAsync(giaTri)}>
          <Truong<GiaTriLienThong>
            ten="buocId"
            label="Bước áp dụng"
            tooltip="Để trống = áp dụng cho toàn quy trình."
          >
            {(o) => (
              <Select
                {...o}
                value={(o.value as string | null) ?? undefined}
                allowClear
                placeholder="Toàn quy trình"
                options={(soDo?.danhSachBuoc ?? []).map((b) => ({ value: b.id, label: b.ten }))}
              />
            )}
          </Truong>

          <Truong<GiaTriLienThong> ten="heThongTichHopId" label="Hệ thống nhận dữ liệu" required>
            {(o) => (
              <Select
                {...o}
                value={(o.value as string) || undefined}
                options={(heThong ?? []).map((x) => ({
                  value: x.id,
                  label: `${x.ten} (${x.ma})`,
                }))}
              />
            )}
          </Truong>

          <Truong<GiaTriLienThong> ten="suKien" label="Kích hoạt khi">
            {(o) => <Select {...o} value={o.value as string} options={SU_KIEN} />}
          </Truong>

          <Truong<GiaTriLienThong>
            ten="loaiDuLieu"
            label="Loại dữ liệu đẩy đi"
            tooltip="Nhãn để hệ thống ngoài biết gói dữ liệu thuộc loại nào."
          >
            {(o) => (
              <Input
                {...o}
                value={o.value as string}
                placeholder="VD: SANG_KIEN_DUOC_CONG_NHAN"
              />
            )}
          </Truong>

          <Space size="large" wrap>
            <Truong<GiaTriLienThong> ten="trangThai" label="Trạng thái">
              {(o) => (
                <Select
                  {...o}
                  value={o.value as number}
                  style={{ width: 180 }}
                  options={[
                    { value: 1, label: 'Hoạt động' },
                    { value: 0, label: 'Ngừng' },
                  ]}
                />
              )}
            </Truong>
            <Truong<GiaTriLienThong>
              ten="dongBoHaiChieu"
              label="Đồng bộ hai chiều"
              tooltip="Bật khi hệ thống ngoài cũng đẩy trạng thái ngược lại qua API công khai."
            >
              {(o) => (
                <Select
                  {...o}
                  value={o.value as boolean}
                  style={{ width: 180 }}
                  options={[
                    { value: false, label: 'Một chiều (đẩy đi)' },
                    { value: true, label: 'Hai chiều' },
                  ]}
                />
              )}
            </Truong>
          </Space>
        </BieuMau>
      </Modal>
    </Card>
  );
}
