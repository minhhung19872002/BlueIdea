import { useState } from 'react';
import { Link } from 'react-router-dom';
import { App, Button, Card, Form, Input, Modal, Space, Table, Tag } from 'antd';
import {
  ApartmentOutlined,
  CheckCircleOutlined,
  CopyOutlined,
  PlusOutlined,
  StopOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { LoiApi } from '@/api/client';
import { apiQuyTrinh, type DanhMucDto } from '@/api/endpoints';
import { KhoiLoi, ngayGio } from '@/components/ThanhPhanChung';

/** Chức năng 9 — Danh sách quy trình xử lý và các thao tác vòng đời. */
export default function TrangQuyTrinh() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [trang, setTrang] = useState(1);
  const [soDong, setSoDong] = useState(20);
  const [moTao, setMoTao] = useState(false);
  const [form] = Form.useForm();

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['quy-trinh', { trang, soDong }],
    queryFn: () => apiQuyTrinh.danhSach({ trang, soDong }),
  });

  const lamMoi = () => queryClient.invalidateQueries({ queryKey: ['quy-trinh'] });

  const kichHoat = useMutation({
    mutationFn: (id: string) => apiQuyTrinh.kichHoat(id),
    onSuccess: (kq) => {
      message.success('Đã kích hoạt quy trình');
      if (kq.danhSachCanhBao.length > 0) {
        modal.warning({
          title: 'Quy trình có cảnh báo',
          content: (
            <ul style={{ paddingLeft: 18 }}>
              {kq.danhSachCanhBao.map((c) => (
                <li key={c.ma + c.thongBao}>{c.thongBao}</li>
              ))}
            </ul>
          ),
        });
      }
      void lamMoi();
    },
    onError: (loi) =>
      modal.error({
        title: 'Không kích hoạt được quy trình',
        content: loi instanceof LoiApi ? loi.message : 'Đã xảy ra lỗi.',
      }),
  });

  const ngungApDung = useMutation({
    mutationFn: (id: string) => apiQuyTrinh.ngungApDung(id),
    onSuccess: () => {
      message.success('Đã ngừng áp dụng');
      void lamMoi();
    },
  });

  const phienBanMoi = useMutation({
    mutationFn: (id: string) => apiQuyTrinh.phienBanMoi(id),
    onSuccess: () => {
      message.success('Đã tạo phiên bản mới');
      void lamMoi();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không tạo được.'),
  });

  const tao = useMutation({
    mutationFn: (giaTri: Record<string, unknown>) => apiQuyTrinh.them(giaTri),
    onSuccess: () => {
      message.success('Đã tạo quy trình');
      setMoTao(false);
      form.resetFields();
      void lamMoi();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không tạo được.'),
  });

  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  return (
    <Card
      title="Quy trình xử lý hồ sơ"
      extra={
        <Button type="primary" icon={<PlusOutlined />} onClick={() => setMoTao(true)}>
          Tạo quy trình
        </Button>
      }
    >
      <Table<DanhMucDto>
        rowKey="id"
        size="middle"
        loading={isLoading}
        dataSource={data?.duLieu ?? []}
        scroll={{ x: 900 }}
        columns={[
          { title: 'Mã', dataIndex: 'ma', width: 180 },
          {
            title: 'Tên quy trình',
            dataIndex: 'ten',
            render: (v: string, dong) => (
              <Link to={`/quan-tri/quy-trinh/${dong.id}/thiet-ke`}>{v}</Link>
            ),
          },
          { title: 'Mô tả', dataIndex: 'moTa', responsive: ['lg'] },
          {
            title: 'Trạng thái',
            dataIndex: 'trangThai',
            width: 130,
            render: (v: number) =>
              v === 1 ? <Tag color="success">Hoạt động</Tag> : <Tag>Ngừng</Tag>,
          },
          {
            title: 'Ngày tạo',
            dataIndex: 'ngayTao',
            width: 140,
            responsive: ['xl'],
            render: (v: string) => ngayGio(v, false),
          },
          {
            title: '',
            width: 220,
            fixed: 'right',
            render: (_v, dong) => (
              <Space>
                <Link to={`/quan-tri/quy-trinh/${dong.id}/thiet-ke`}>
                  <Button size="small" icon={<ApartmentOutlined />}>
                    Thiết kế
                  </Button>
                </Link>
                <Button
                  size="small"
                  type="primary"
                  icon={<CheckCircleOutlined />}
                  loading={kichHoat.isPending}
                  onClick={() => kichHoat.mutate(dong.id)}
                >
                  Kích hoạt
                </Button>
                <Button
                  size="small"
                  icon={<CopyOutlined />}
                  loading={phienBanMoi.isPending}
                  title="Tạo phiên bản mới"
                  onClick={() => phienBanMoi.mutate(dong.id)}
                />
                <Button
                  size="small"
                  danger
                  icon={<StopOutlined />}
                  title="Ngừng áp dụng"
                  onClick={() => ngungApDung.mutate(dong.id)}
                />
              </Space>
            ),
          },
        ]}
        pagination={{
          current: trang,
          pageSize: soDong,
          total: data?.tongSo ?? 0,
          onChange: (t, s) => {
            setTrang(t);
            setSoDong(s);
          },
        }}
      />

      <Modal
        open={moTao}
        title="Tạo quy trình mới"
        okText="Tạo"
        cancelText="Hủy"
        confirmLoading={tao.isPending}
        onCancel={() => setMoTao(false)}
        onOk={async () => tao.mutate(await form.validateFields())}
      >
        <Form form={form} layout="vertical" initialValues={{ cap: 'CO_SO', trangThai: 1 }}>
          <Form.Item name="ma" label="Mã" rules={[{ required: true, message: 'Nhập mã quy trình' }]}>
            <Input placeholder="VD: QT-CO-SO-2027" />
          </Form.Item>
          <Form.Item name="ten" label="Tên" rules={[{ required: true, message: 'Nhập tên' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="moTa" label="Mô tả">
            <Input.TextArea rows={2} />
          </Form.Item>
        </Form>
      </Modal>
    </Card>
  );
}
