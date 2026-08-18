import { useState } from 'react';
import {
  App,
  Alert,
  Button,
  Card,
  DatePicker,
  Form,
  Input,
  Modal,
  Select,
  Space,
  Switch,
  Table,
  Tag,
} from 'antd';
import { DeleteOutlined, EditOutlined, PlusOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';

import { LoiApi } from '@/api/client';
import { apiNgayNghiLe, type NgayNghiLe } from '@/api/endpoints';
import { KhoiLoi, KhoiRong } from '@/components/ThanhPhanChung';
import { DaiTabTrang } from '@/components/DaiTabTrang';
import { DS_TAB_DANH_MUC } from './danhMucTab';

const NAM_HIEN_TAI = dayjs().year();
const DS_NAM = [NAM_HIEN_TAI - 1, NAM_HIEN_TAI, NAM_HIEN_TAI + 1];

/**
 * Chức năng 46 — Ngày nghỉ lễ, dùng để trừ khi tính hạn xử lý hồ sơ.
 *
 * Ngày lễ âm lịch (Tết, Giỗ Tổ Hùng Vương) rơi vào ngày dương khác nhau mỗi năm nên KHÔNG bật lặp
 * hàng năm; chỉ ngày lễ dương lịch cố định (1/1, 30/4, 2/9) mới lặp được.
 */
export default function TrangNgayNghiLe() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [nam, setNam] = useState<number>(NAM_HIEN_TAI);
  const [dangSua, setDangSua] = useState<NgayNghiLe | null>(null);
  const [moForm, setMoForm] = useState(false);
  const [form] = Form.useForm();

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['ngay-nghi-le', nam],
    queryFn: () => apiNgayNghiLe.danhSach(nam),
  });

  function lamMoi() {
    void queryClient.invalidateQueries({ queryKey: ['ngay-nghi-le'] });
  }

  const luu = useMutation({
    mutationFn: async (giaTri: Record<string, unknown>) => {
      const duLieu = {
        ngay: (giaTri.ngay as dayjs.Dayjs).format('YYYY-MM-DD'),
        ten: giaTri.ten as string,
        lapLaiHangNam: !!giaTri.lapLaiHangNam,
        trangThai: (giaTri.trangThai as number) ?? 1,
      };
      return dangSua ? apiNgayNghiLe.sua(dangSua.id, duLieu) : apiNgayNghiLe.them(duLieu);
    },
    onSuccess: () => {
      message.success(dangSua ? 'Đã cập nhật ngày nghỉ' : 'Đã thêm ngày nghỉ');
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
    mutationFn: (id: string) => apiNgayNghiLe.xoa(id),
    onSuccess: () => {
      message.success('Đã xoá ngày nghỉ');
      lamMoi();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không xoá được.'),
  });

  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  return (
    <Card
      title="Danh mục"
      extra={
        <Space>
          <Select
            style={{ width: 140 }}
            value={nam}
            options={DS_NAM.map((x) => ({ value: x, label: `Năm ${x}` }))}
            onChange={setNam}
          />
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => {
              setDangSua(null);
              form.resetFields();
              form.setFieldValue('ngay', dayjs().year(nam).startOf('year'));
              setMoForm(true);
            }}
          >
            Thêm ngày nghỉ
          </Button>
        </Space>
      }
    >
      <DaiTabTrang danhSach={DS_TAB_DANH_MUC} dangChon="ngay-nghi-le" />

      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 12 }}
        message="Ngày trong danh sách này không tính vào thời hạn xử lý hồ sơ."
        description="Ngày lễ âm lịch (Tết, Giỗ Tổ Hùng Vương) đổi ngày dương mỗi năm — khai báo riêng từng năm, không bật lặp hàng năm."
      />

      <Table<NgayNghiLe>
        rowKey="id"
        size="middle"
        loading={isLoading}
        dataSource={data ?? []}
        pagination={false}
        scroll={{ x: 760 }}
        locale={{ emptyText: <KhoiRong moTa={`Chưa khai báo ngày nghỉ nào cho năm ${nam}.`} /> }}
        columns={[
          {
            title: 'Ngày',
            dataIndex: 'ngay',
            width: 160,
            sorter: (a, b) => a.ngay.localeCompare(b.ngay),
            defaultSortOrder: 'ascend',
            render: (v: string) => dayjs(v).format('DD/MM/YYYY'),
          },
          { title: 'Tên ngày nghỉ', dataIndex: 'ten' },
          {
            title: 'Lặp hàng năm',
            dataIndex: 'lapLaiHangNam',
            width: 150,
            render: (v: boolean) => (v ? <Tag color="blue">Có</Tag> : <Tag>Không</Tag>),
          },
          {
            title: 'Trạng thái',
            dataIndex: 'trangThai',
            width: 130,
            render: (v: number) => (v === 1 ? <Tag color="success">Áp dụng</Tag> : <Tag>Ngừng</Tag>),
          },
          {
            title: '',
            key: 'thaoTac',
            width: 110,
            fixed: 'right',
            render: (_v, dong) => (
              <Space>
                <Button
                  type="text"
                  icon={<EditOutlined />}
                  onClick={() => {
                    setDangSua(dong);
                    form.setFieldsValue({ ...dong, ngay: dayjs(dong.ngay) });
                    setMoForm(true);
                  }}
                />
                <Button
                  type="text"
                  danger
                  icon={<DeleteOutlined />}
                  onClick={() =>
                    modal.confirm({
                      title: 'Xoá ngày nghỉ',
                      content: `Xoá "${dong.ten}" (${dayjs(dong.ngay).format(
                        'DD/MM/YYYY',
                      )})? Hạn xử lý hồ sơ tính lại sẽ tính cả ngày này.`,
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
        title={dangSua ? `Sửa: ${dangSua.ten}` : 'Thêm ngày nghỉ lễ'}
        okText="Lưu"
        cancelText="Huỷ"
        confirmLoading={luu.isPending}
        onCancel={() => setMoForm(false)}
        onOk={async () => luu.mutate(await form.validateFields())}
      >
        <Form form={form} layout="vertical" initialValues={{ lapLaiHangNam: false, trangThai: 1 }}>
          <Form.Item name="ngay" label="Ngày" rules={[{ required: true, message: 'Chọn ngày' }]}>
            <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
          </Form.Item>
          <Form.Item
            name="ten"
            label="Tên ngày nghỉ"
            rules={[{ required: true, message: 'Nhập tên' }]}
          >
            <Input placeholder="VD: Quốc khánh 2/9" />
          </Form.Item>
          <Form.Item
            name="lapLaiHangNam"
            label="Lặp hàng năm"
            valuePropName="checked"
            tooltip="Chỉ bật với ngày lễ dương lịch cố định. Ngày lễ âm lịch phải khai báo riêng từng năm."
          >
            <Switch />
          </Form.Item>
          <Form.Item name="trangThai" label="Trạng thái">
            <Select
              options={[
                { value: 1, label: 'Áp dụng' },
                { value: 0, label: 'Ngừng' },
              ]}
            />
          </Form.Item>
        </Form>
      </Modal>
    </Card>
  );
}
