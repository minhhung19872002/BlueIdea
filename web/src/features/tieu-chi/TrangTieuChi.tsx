import { useState } from 'react';
import { Link } from 'react-router-dom';
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
  Table,
  Tag,
  Tooltip,
} from 'antd';
import { CopyOutlined, PlusOutlined, SettingOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { LoiApi } from '@/api/client';
import { apiTieuChi, type DanhMucDto } from '@/api/endpoints';
import { KhoiLoi } from '@/components/ThanhPhanChung';

/** Chức năng 17 — Danh sách bộ tiêu chí chấm điểm. */
export default function TrangTieuChi() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [trang, setTrang] = useState(1);
  const [soDong, setSoDong] = useState(20);
  const [moTao, setMoTao] = useState(false);
  const [saoChep, setSaoChep] = useState<DanhMucDto | null>(null);
  const [form] = Form.useForm();
  const [formSaoChep] = Form.useForm();

  const taoBanSao = useMutation({
    mutationFn: (giaTri: { ma: string; ten: string; nam: number }) =>
      apiTieuChi.saoChep(saoChep!.id, {
        ma: giaTri.ma.trim().toUpperCase(),
        ten: giaTri.ten,
        nam: giaTri.nam,
      }),
    onSuccess: () => {
      message.success('Đã sao chép bộ tiêu chí');
      setSaoChep(null);
      formSaoChep.resetFields();
      void queryClient.invalidateQueries({ queryKey: ['bo-tieu-chi'] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không sao chép được.'),
  });

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['bo-tieu-chi', { trang, soDong }],
    queryFn: () => apiTieuChi.danhSach({ trang, soDong }),
  });

  const tao = useMutation({
    mutationFn: (giaTri: Record<string, unknown>) => apiTieuChi.them(giaTri),
    onSuccess: () => {
      message.success('Đã tạo bộ tiêu chí');
      setMoTao(false);
      form.resetFields();
      void queryClient.invalidateQueries({ queryKey: ['bo-tieu-chi'] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không tạo được.'),
  });

  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  return (
    <Card
      title="Bộ tiêu chí chấm điểm"
      extra={
        <Button type="primary" icon={<PlusOutlined />} onClick={() => setMoTao(true)}>
          Tạo bộ tiêu chí
        </Button>
      }
    >
      <Table<DanhMucDto>
        rowKey="id"
        size="middle"
        loading={isLoading}
        dataSource={data?.duLieu ?? []}
        columns={[
          { title: 'Mã', dataIndex: 'ma', width: 200 },
          {
            title: 'Tên bộ tiêu chí',
            dataIndex: 'ten',
            render: (v: string, dong) => <Link to={`/quan-tri/tieu-chi/${dong.id}`}>{v}</Link>,
          },
          {
            title: 'Trạng thái',
            dataIndex: 'trangThai',
            width: 130,
            render: (v: number) => (v === 1 ? <Tag color="success">Hoạt động</Tag> : <Tag>Ngừng</Tag>),
          },
          {
            title: '',
            width: 230,
            render: (_v, dong) => (
              <Space>
                <Link to={`/quan-tri/tieu-chi/${dong.id}`}>
                  <Button size="small" icon={<SettingOutlined />}>
                    Cấu hình
                  </Button>
                </Link>
                <Tooltip title="Tạo bộ tiêu chí năm mới từ bộ này, giữ nguyên cây tiêu chí và mức công nhận">
                  <Button
                    size="small"
                    icon={<CopyOutlined />}
                    onClick={() => {
                      setSaoChep(dong);
                      formSaoChep.setFieldsValue({
                        ma: `${dong.ma}-SAO-CHEP`,
                        ten: `${dong.ten} (bản sao)`,
                        nam: new Date().getFullYear() + 1,
                      });
                    }}
                  >
                    Sao chép
                  </Button>
                </Tooltip>
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
        open={!!saoChep}
        title={`Sao chép bộ tiêu chí: ${saoChep?.ten ?? ''}`}
        okText="Tạo bản sao"
        cancelText="Huỷ"
        confirmLoading={taoBanSao.isPending}
        onCancel={() => setSaoChep(null)}
        onOk={async () => taoBanSao.mutate(await formSaoChep.validateFields())}
      >
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 12 }}
          message="Bản sao nhận đủ cây tiêu chí, thang điểm và các mức công nhận của bộ gốc."
          description="Dùng khi sang năm mới: sửa vài tiêu chí trên bản sao thay vì gõ lại từ đầu, và bộ cũ vẫn giữ nguyên để tra cứu hồ sơ các năm trước."
        />
        <Form form={formSaoChep} layout="vertical">
          <Form.Item name="ma" label="Mã bộ mới" rules={[{ required: true, message: 'Nhập mã' }]}>
            <Input placeholder="VD: BTC-CO-SO-2027" />
          </Form.Item>
          <Form.Item name="ten" label="Tên bộ mới" rules={[{ required: true, message: 'Nhập tên' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="nam" label="Năm áp dụng" rules={[{ required: true, message: 'Nhập năm' }]}>
            <InputNumber min={2000} max={2100} style={{ width: 160 }} />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        open={moTao}
        title="Tạo bộ tiêu chí"
        okText="Tạo"
        cancelText="Hủy"
        confirmLoading={tao.isPending}
        onCancel={() => setMoTao(false)}
        onOk={async () => tao.mutate(await form.validateFields())}
      >
        <Form
          form={form}
          layout="vertical"
          initialValues={{
            nam: new Date().getFullYear(),
            thangDiemToiDa: 100,
            diemDatToiThieu: 50,
            cachTinh: 'TONG_DIEM',
            lamTron: 2,
            trangThai: 1,
          }}
        >
          <Form.Item name="ma" label="Mã" rules={[{ required: true, message: 'Nhập mã' }]}>
            <Input placeholder="VD: BTC-CO-SO-2027" />
          </Form.Item>
          <Form.Item name="ten" label="Tên" rules={[{ required: true, message: 'Nhập tên' }]}>
            <Input />
          </Form.Item>
          <Space size="large">
            <Form.Item name="nam" label="Năm">
              <InputNumber min={2000} max={2100} />
            </Form.Item>
            <Form.Item name="thangDiemToiDa" label="Thang điểm">
              <InputNumber min={1} max={1000} />
            </Form.Item>
            <Form.Item name="diemDatToiThieu" label="Điểm đạt tối thiểu">
              <InputNumber min={0} max={1000} />
            </Form.Item>
          </Space>
          <Form.Item name="cachTinh" label="Cách tính điểm">
            <Select
              options={[
                { value: 'TONG_DIEM', label: 'Tổng điểm các tiêu chí' },
                { value: 'TRUNG_BINH_CONG', label: 'Trung bình cộng' },
                { value: 'TRUNG_BINH_TRONG_SO', label: 'Trung bình theo trọng số nhóm' },
              ]}
            />
          </Form.Item>
        </Form>
      </Modal>
    </Card>
  );
}
