import { useState } from 'react';
import { App, Alert, Button, DatePicker, Form, Input, Modal, Popconfirm, Select, Space, Table, Tag } from 'antd';
import { KeyOutlined, PlusOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';

import { capNhatDuLieu, guiDuLieu, layDuLieu, LoiApi, xoaDuLieu } from '@/api/client';
import { ngayGio } from '@/components/ThanhPhanChung';

interface KhoaApi {
  id: string;
  ten: string;
  tienTo: string;
  danhSachIp: string[];
  dangHoatDong: boolean;
  ngayHetHan: string | null;
  lanGoiCuoi: string | null;
  soLanGoi: number;
  ghiChu: string | null;
}

interface FormKhoa {
  ten: string;
  danhSachIp?: string[];
  ngayHetHan?: dayjs.Dayjs | null;
  ghiChu?: string;
}

/**
 * Chức năng 41 — Cấp và quản lý khoá API cho hệ thống ngoài gọi vào BlueIdea.
 *
 * Khoá gốc chỉ hiện đúng một lần ngay sau khi cấp. Bảng danh sách không bao giờ có cột khoá
 * vì máy chủ chỉ lưu bản băm — không có gì để hiện lại kể cả khi muốn.
 */
export default function TrangKhoaApiNgoai() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [moForm, setMoForm] = useState(false);
  const [dangSua, setDangSua] = useState<KhoaApi | null>(null);
  const [khoaMoi, setKhoaMoi] = useState<string | null>(null);
  const [form] = Form.useForm<FormKhoa>();

  const { data, isLoading } = useQuery({
    queryKey: ['khoa-api-ngoai'],
    queryFn: () => layDuLieu<KhoaApi[]>('/api/v1/khoa-api-ngoai'),
  });

  const lamMoi = () => queryClient.invalidateQueries({ queryKey: ['khoa-api-ngoai'] });

  const luu = useMutation({
    mutationFn: async (giaTri: FormKhoa) => {
      const than = {
        ten: giaTri.ten,
        danhSachIp: giaTri.danhSachIp ?? [],
        ngayHetHan: giaTri.ngayHetHan ? giaTri.ngayHetHan.toISOString() : null,
        ghiChu: giaTri.ghiChu ?? null,
      };

      if (dangSua) {
        await capNhatDuLieu(`/api/v1/khoa-api-ngoai/${dangSua.id}`, than);
        return null;
      }

      return guiDuLieu<{ id: string; khoa: string }>('/api/v1/khoa-api-ngoai', than);
    },
    onSuccess: (kq) => {
      setMoForm(false);
      setDangSua(null);
      form.resetFields();
      void lamMoi();

      if (kq) {
        setKhoaMoi(kq.khoa);
      } else {
        message.success('Đã cập nhật khoá API');
      }
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không lưu được.'),
  });

  const doiTrangThai = useMutation({
    mutationFn: ({ id, bat }: { id: string; bat: boolean }) =>
      guiDuLieu(`/api/v1/khoa-api-ngoai/${id}/trang-thai?bat=${bat}`, {}),
    onSuccess: () => void lamMoi(),
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không đổi được.'),
  });

  const thuHoi = useMutation({
    mutationFn: (id: string) => xoaDuLieu(`/api/v1/khoa-api-ngoai/${id}`),
    onSuccess: () => {
      message.success('Đã thu hồi khoá API');
      void lamMoi();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không thu hồi được.'),
  });

  function moTao() {
    setDangSua(null);
    form.resetFields();
    setMoForm(true);
  }

  function moSua(ban: KhoaApi) {
    setDangSua(ban);
    form.setFieldsValue({
      ten: ban.ten,
      danhSachIp: ban.danhSachIp,
      ngayHetHan: ban.ngayHetHan ? dayjs(ban.ngayHetHan) : null,
      ghiChu: ban.ghiChu ?? undefined,
    });
    setMoForm(true);
  }

  return (
    <div className="tk-the tk-the-than">
      <Space style={{ marginBottom: 12, flexWrap: 'wrap' }}>
        <Button type="primary" icon={<PlusOutlined />} onClick={moTao}>
          Cấp khoá mới
        </Button>
      </Space>

      <Table<KhoaApi>
        rowKey="id"
        size="middle"
        loading={isLoading}
        dataSource={data ?? []}
        scroll={{ x: 900 }}
        pagination={false}
        columns={[
          { title: 'Tên hệ thống', dataIndex: 'ten' },
          {
            title: 'Khoá',
            dataIndex: 'tienTo',
            width: 150,
            render: (v: string) => <code>{v}…</code>,
          },
          {
            title: 'IP cho phép',
            dataIndex: 'danhSachIp',
            width: 200,
            render: (v: string[]) =>
              v.length === 0 ? (
                <Tag color="warning">Mọi địa chỉ</Tag>
              ) : (
                v.map((x) => <Tag key={x}>{x}</Tag>)
              ),
          },
          {
            title: 'Trạng thái',
            dataIndex: 'dangHoatDong',
            width: 120,
            render: (v: boolean) =>
              v ? <Tag color="success">Đang bật</Tag> : <Tag>Tạm dừng</Tag>,
          },
          { title: 'Lượt gọi', dataIndex: 'soLanGoi', width: 100 },
          {
            title: 'Gọi gần nhất',
            dataIndex: 'lanGoiCuoi',
            width: 160,
            render: (v: string | null) => (v ? ngayGio(v) : '—'),
          },
          {
            title: '',
            width: 220,
            render: (_, ban) => (
              <Space wrap>
                <Button size="small" onClick={() => moSua(ban)}>
                  Sửa
                </Button>
                <Button
                  size="small"
                  onClick={() =>
                    doiTrangThai.mutate({ id: ban.id, bat: !ban.dangHoatDong })
                  }
                >
                  {ban.dangHoatDong ? 'Tạm dừng' : 'Bật lại'}
                </Button>
                <Popconfirm
                  title="Thu hồi khoá này?"
                  description="Hệ thống đang dùng khoá sẽ bị từ chối ngay lập tức."
                  okText="Thu hồi"
                  cancelText="Huỷ"
                  onConfirm={() => thuHoi.mutate(ban.id)}
                >
                  <Button size="small" danger>
                    Thu hồi
                  </Button>
                </Popconfirm>
              </Space>
            ),
          },
        ]}
      />

      <Modal
        open={moForm}
        title={dangSua ? 'Sửa khoá API' : 'Cấp khoá API mới'}
        onCancel={() => setMoForm(false)}
        onOk={() => form.submit()}
        confirmLoading={luu.isPending}
        okText="Lưu"
        cancelText="Huỷ"
      >
        <Form<FormKhoa> form={form} layout="vertical" onFinish={(v) => luu.mutate(v)}>
          <Form.Item
            name="ten"
            label="Tên hệ thống"
            rules={[{ required: true, message: 'Vui lòng nhập tên hệ thống' }]}
          >
            <Input placeholder="Ví dụ: Hệ thống Thi đua khen thưởng" />
          </Form.Item>

          <Form.Item
            name="danhSachIp"
            label="Địa chỉ IP được phép"
            extra="Để trống nghĩa là cho phép mọi địa chỉ. Nhập IP đơn hoặc dải CIDR, ví dụ 10.0.0.0/8."
          >
            <Select mode="tags" placeholder="203.0.113.7 hoặc 10.0.0.0/8" tokenSeparators={[',']} />
          </Form.Item>

          <Form.Item name="ngayHetHan" label="Ngày hết hạn">
            <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
          </Form.Item>

          <Form.Item name="ghiChu" label="Ghi chú">
            <Input.TextArea rows={2} />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        open={khoaMoi !== null}
        title="Khoá API mới"
        onCancel={() => setKhoaMoi(null)}
        onOk={() => setKhoaMoi(null)}
        okText="Tôi đã sao chép"
        cancelButtonProps={{ style: { display: 'none' } }}
      >
        <Alert
          type="warning"
          showIcon
          style={{ marginBottom: 12 }}
          message="Đây là lần duy nhất khoá được hiển thị"
          description="Hệ thống chỉ lưu bản băm nên không thể hiện lại. Mất khoá thì phải cấp khoá mới."
          icon={<KeyOutlined />}
        />
        <Input.TextArea
          readOnly
          value={khoaMoi ?? ''}
          autoSize
          style={{ fontFamily: 'monospace' }}
          onFocus={(e) => e.target.select()}
        />
        <div style={{ marginTop: 12, fontSize: 13, color: 'rgba(0,0,0,0.65)' }}>
          Hệ thống ngoài gửi khoá này ở header <code>X-Api-Key</code> khi gọi{' '}
          <code>/api/public/v1/sang-kien</code>.
        </div>
      </Modal>
    </div>
  );
}
