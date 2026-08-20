import { useState } from 'react';
import {
  App,
  Alert,
  Button,
  Card,
  Input,
  InputNumber,
  Modal,
  Select,
  Space,
  Table,
  Typography,
} from 'antd';
import { DeleteOutlined, EditOutlined, PlusOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { z } from 'zod';

import { LoiApi } from '@/api/client';
import { BieuMau, Truong, useBieuMau } from '@/components/bieu-mau/BieuMau';
import { soNguyen, tuyChon } from '@/components/bieu-mau/luat';
import {
  apiCapPheDuyet,
  apiDonVi,
  apiDotDeNghi,
  apiLinhVuc,
  type CapPheDuyet,
} from '@/api/endpoints';
import { KhoiLoi, KhoiRong } from '@/components/ThanhPhanChung';
import { DaiTabTrang } from '@/components/DaiTabTrang';
import { DS_TAB_DANH_MUC } from '@/features/quan-tri/danhMucTab';

/**
 * Luật kiểm tra cấp phê duyệt.
 *
 * Đợt và lĩnh vực để trống nghĩa là "áp dụng cho mọi đợt / mọi lĩnh vực", nên hai trường này
 * là tuỳ chọn — không phải quên khai báo bắt buộc.
 */
const luatCapPheDuyet = z.object({
  donViPheDuyetId: z
    .string({ required_error: 'Vui lòng chọn đơn vị phê duyệt.' })
    .uuid('Vui lòng chọn đơn vị phê duyệt.'),
  thuTuCap: soNguyen('Thứ tự cấp', 1, 10),
  dotDeNghiId: z.string().uuid().optional().nullable(),
  linhVucId: z.string().uuid().optional().nullable(),
  ghiChu: tuyChon(),
});

type GiaTriCapPheDuyet = z.infer<typeof luatCapPheDuyet>;

const MAC_DINH_CAP = { thuTuCap: 1 } as GiaTriCapPheDuyet;

/**
 * Chức năng 5 — Cấp phê duyệt theo đợt và lĩnh vực.
 *
 * Một hồ sơ có thể phải qua nhiều cấp (phòng chuyên môn → hội đồng cơ sở → UBND). Bảng này
 * khai báo thứ tự các cấp cho từng phạm vi; để trống đợt hoặc lĩnh vực nghĩa là áp dụng chung.
 */
export default function TrangCapPheDuyet() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [locDot, setLocDot] = useState<string | undefined>();
  const [locLinhVuc, setLocLinhVuc] = useState<string | undefined>();
  const [dangSua, setDangSua] = useState<CapPheDuyet | null>(null);
  const [moForm, setMoForm] = useState(false);
  const form = useBieuMau(luatCapPheDuyet, MAC_DINH_CAP);

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['cap-phe-duyet', locDot, locLinhVuc],
    queryFn: () => apiCapPheDuyet.danhSach({ dotDeNghiId: locDot, linhVucId: locLinhVuc }),
  });

  const { data: cacDot } = useQuery({ queryKey: ['dot-chon'], queryFn: apiDotDeNghi.chon });
  const { data: cacLinhVuc } = useQuery({ queryKey: ['linh-vuc-chon'], queryFn: apiLinhVuc.chon });
  /*
   * Chỉ liệt kê đơn vị đã bật ô "Là đơn vị phê duyệt": máy chủ từ chối gán cấp xét cho đơn vị
   * không bật ô đó, nên bày cả danh sách ra đây chỉ tạo cơ hội chọn nhầm rồi ăn lỗi.
   */
  const { data: cacDonVi } = useQuery({
    queryKey: ['don-vi-phe-duyet-chon'],
    queryFn: apiDonVi.chonDonViPheDuyet,
  });

  function lamMoi() {
    void queryClient.invalidateQueries({ queryKey: ['cap-phe-duyet'] });
  }

  const luu = useMutation({
    mutationFn: async (giaTri: GiaTriCapPheDuyet) => {
      const duLieu = {
        dotDeNghiId: giaTri.dotDeNghiId ?? null,
        linhVucId: giaTri.linhVucId ?? null,
        donViPheDuyetId: giaTri.donViPheDuyetId,
        thuTuCap: giaTri.thuTuCap,
        ghiChu: giaTri.ghiChu ?? null,
      };

      return dangSua ? apiCapPheDuyet.sua(dangSua.id, duLieu) : apiCapPheDuyet.them(duLieu);
    },
    onSuccess: () => {
      message.success(dangSua ? 'Đã cập nhật cấp phê duyệt' : 'Đã thêm cấp phê duyệt');
      setMoForm(false);
      setDangSua(null);
      form.reset(MAC_DINH_CAP);
      lamMoi();
    },
    onError: (loi) =>
      modal.error({
        title: 'Không lưu được',
        content: loi instanceof LoiApi ? loi.message : 'Đã xảy ra lỗi.',
      }),
  });

  const xoa = useMutation({
    mutationFn: (id: string) => apiCapPheDuyet.xoa(id),
    onSuccess: () => {
      message.success('Đã xoá cấp phê duyệt');
      lamMoi();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không xoá được.'),
  });

  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  return (
    <Card
      title="Danh mục dùng chung"
      extra={
        <Button
          type="primary"
          icon={<PlusOutlined />}
          onClick={() => {
            setDangSua(null);
            form.reset(MAC_DINH_CAP);
            setMoForm(true);
          }}
        >
          Thêm cấp phê duyệt
        </Button>
      }
    >
      <DaiTabTrang danhSach={DS_TAB_DANH_MUC} dangChon="cap-phe-duyet" />

      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 12 }}
        message="Thứ tự cấp quyết định hồ sơ phải qua đơn vị nào trước."
        description="Bỏ trống Đợt hoặc Lĩnh vực nghĩa là cấu hình áp dụng chung cho mọi đợt / mọi lĩnh vực. Trong cùng một phạm vi, mỗi thứ tự cấp chỉ được gán cho một đơn vị."
      />

      <Space wrap style={{ marginBottom: 12 }}>
        <Select
          style={{ width: 260 }}
          allowClear
          placeholder="Lọc theo đợt đề nghị"
          value={locDot}
          options={(cacDot ?? []).map((x) => ({ value: x.id, label: x.ten }))}
          onChange={setLocDot}
        />
        <Select
          style={{ width: 240 }}
          allowClear
          placeholder="Lọc theo lĩnh vực"
          value={locLinhVuc}
          options={(cacLinhVuc ?? []).map((x) => ({ value: x.id, label: x.ten }))}
          onChange={setLocLinhVuc}
        />
      </Space>

      <Table<CapPheDuyet>
        rowKey="id"
        size="middle"
        loading={isLoading}
        dataSource={data ?? []}
        pagination={false}
        scroll={{ x: 1000 }}
        locale={{ emptyText: <KhoiRong moTa="Chưa cấu hình cấp phê duyệt nào." /> }}
        columns={[
          {
            title: 'Cấp',
            dataIndex: 'thuTuCap',
            width: 80,
            align: 'center',
            render: (v: number) => <Typography.Text strong>{v}</Typography.Text>,
          },
          { title: 'Đơn vị phê duyệt', dataIndex: 'tenDonViPheDuyet', width: 300 },
          {
            title: 'Đợt áp dụng',
            dataIndex: 'tenDot',
            width: 240,
            render: (v: string | null) =>
              v ?? <Typography.Text type="secondary">Mọi đợt</Typography.Text>,
          },
          {
            title: 'Lĩnh vực áp dụng',
            dataIndex: 'tenLinhVuc',
            width: 220,
            render: (v: string | null) =>
              v ?? <Typography.Text type="secondary">Mọi lĩnh vực</Typography.Text>,
          },
          { title: 'Ghi chú', dataIndex: 'ghiChu', ellipsis: true },
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
                      donViPheDuyetId: dong.donViPheDuyetId,
                      thuTuCap: dong.thuTuCap,
                      dotDeNghiId: dong.dotDeNghiId ?? undefined,
                      linhVucId: dong.linhVucId ?? undefined,
                      ghiChu: dong.ghiChu ?? undefined,
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
                      title: 'Xoá cấp phê duyệt',
                      content: `Xoá cấp ${dong.thuTuCap} — ${dong.tenDonViPheDuyet}?`,
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
        width={600}
        title={dangSua ? 'Sửa cấp phê duyệt' : 'Thêm cấp phê duyệt'}
        okText="Lưu"
        cancelText="Huỷ"
        confirmLoading={luu.isPending || form.formState.isSubmitting}
        onCancel={() => setMoForm(false)}
        okButtonProps={{ htmlType: 'submit', form: 'form-cap-phe-duyet' }}
      >
        <BieuMau id="form-cap-phe-duyet" form={form} onGui={(giaTri) => luu.mutateAsync(giaTri)}>
          <Truong<GiaTriCapPheDuyet> ten="donViPheDuyetId" label="Đơn vị phê duyệt" required>
            {(o) => (
              <Select
                {...o}
                value={o.value as string}
                showSearch
                optionFilterProp="label"
                options={(cacDonVi ?? []).map((x) => ({ value: x.id, label: x.ten }))}
              />
            )}
          </Truong>

          <Truong<GiaTriCapPheDuyet>
            ten="thuTuCap"
            label="Thứ tự cấp"
            tooltip="Cấp 1 xét trước, cấp 2 xét sau."
            required
          >
            {(o) => (
              <InputNumber
                {...o}
                value={o.value as number}
                min={1}
                max={10}
                style={{ width: '100%' }}
              />
            )}
          </Truong>

          <Truong<GiaTriCapPheDuyet> ten="dotDeNghiId" label="Áp dụng cho đợt">
            {(o) => (
              <Select
                {...o}
                value={o.value as string | undefined}
                allowClear
                placeholder="Mọi đợt"
                options={(cacDot ?? []).map((x) => ({ value: x.id, label: x.ten }))}
              />
            )}
          </Truong>

          <Truong<GiaTriCapPheDuyet> ten="linhVucId" label="Áp dụng cho lĩnh vực">
            {(o) => (
              <Select
                {...o}
                value={o.value as string | undefined}
                allowClear
                placeholder="Mọi lĩnh vực"
                options={(cacLinhVuc ?? []).map((x) => ({ value: x.id, label: x.ten }))}
              />
            )}
          </Truong>

          <Truong<GiaTriCapPheDuyet> ten="ghiChu" label="Ghi chú">
            {(o) => <Input.TextArea {...o} value={o.value as string} rows={2} />}
          </Truong>
        </BieuMau>
      </Modal>
    </Card>
  );
}
