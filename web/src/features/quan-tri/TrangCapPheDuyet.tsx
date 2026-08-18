import { useState } from 'react';
import {
  App,
  Alert,
  Button,
  Card,
  Form,
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

import { LoiApi } from '@/api/client';
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
  const [form] = Form.useForm();

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['cap-phe-duyet', locDot, locLinhVuc],
    queryFn: () => apiCapPheDuyet.danhSach({ dotDeNghiId: locDot, linhVucId: locLinhVuc }),
  });

  const { data: cacDot } = useQuery({ queryKey: ['dot-chon'], queryFn: apiDotDeNghi.chon });
  const { data: cacLinhVuc } = useQuery({ queryKey: ['linh-vuc-chon'], queryFn: apiLinhVuc.chon });
  const { data: cacDonVi } = useQuery({ queryKey: ['don-vi-chon'], queryFn: apiDonVi.chon });

  function lamMoi() {
    void queryClient.invalidateQueries({ queryKey: ['cap-phe-duyet'] });
  }

  const luu = useMutation({
    mutationFn: async (giaTri: Record<string, unknown>) => {
      const duLieu = {
        dotDeNghiId: (giaTri.dotDeNghiId as string) || null,
        linhVucId: (giaTri.linhVucId as string) || null,
        donViPheDuyetId: giaTri.donViPheDuyetId as string,
        thuTuCap: (giaTri.thuTuCap as number) ?? 1,
        ghiChu: (giaTri.ghiChu as string) || null,
      };

      return dangSua ? apiCapPheDuyet.sua(dangSua.id, duLieu) : apiCapPheDuyet.them(duLieu);
    },
    onSuccess: () => {
      message.success(dangSua ? 'Đã cập nhật cấp phê duyệt' : 'Đã thêm cấp phê duyệt');
      setMoForm(false);
      setDangSua(null);
      form.resetFields();
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
            form.resetFields();
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
                    form.setFieldsValue(dong);
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
        confirmLoading={luu.isPending}
        onCancel={() => setMoForm(false)}
        onOk={async () => luu.mutate(await form.validateFields())}
      >
        <Form form={form} layout="vertical" initialValues={{ thuTuCap: 1 }}>
          <Form.Item
            name="donViPheDuyetId"
            label="Đơn vị phê duyệt"
            rules={[{ required: true, message: 'Chọn đơn vị phê duyệt' }]}
          >
            <Select
              showSearch
              optionFilterProp="label"
              options={(cacDonVi ?? []).map((x) => ({ value: x.id, label: x.ten }))}
            />
          </Form.Item>

          <Form.Item
            name="thuTuCap"
            label="Thứ tự cấp"
            tooltip="Cấp 1 xét trước, cấp 2 xét sau."
            rules={[{ required: true, message: 'Nhập thứ tự cấp' }]}
          >
            <InputNumber min={1} max={10} style={{ width: '100%' }} />
          </Form.Item>

          <Form.Item name="dotDeNghiId" label="Áp dụng cho đợt">
            <Select
              allowClear
              placeholder="Mọi đợt"
              options={(cacDot ?? []).map((x) => ({ value: x.id, label: x.ten }))}
            />
          </Form.Item>

          <Form.Item name="linhVucId" label="Áp dụng cho lĩnh vực">
            <Select
              allowClear
              placeholder="Mọi lĩnh vực"
              options={(cacLinhVuc ?? []).map((x) => ({ value: x.id, label: x.ten }))}
            />
          </Form.Item>

          <Form.Item name="ghiChu" label="Ghi chú">
            <Input.TextArea rows={2} />
          </Form.Item>
        </Form>
      </Modal>
    </Card>
  );
}
