import { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  App,
  Button,
  Card,
  DatePicker,
  Form,
  Input,
  InputNumber,
  Modal,
  Select,
  Space,
  Table,
  Tag,
} from 'antd';
import { DeleteOutlined, EditOutlined, PlusOutlined, SettingOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';

import { LoiApi } from '@/api/client';
import { apiDonVi, apiDotDeNghi, apiHoiDong, apiLinhVuc, type DanhMucDto } from '@/api/endpoints';
import { useAuthStore } from '@/app/store/authStore';
import { KhoiLoi, ngayGio } from '@/components/ThanhPhanChung';

export const CAP_HOI_DONG = [
  { value: 'CO_SO', label: 'Cấp cơ sở' },
  { value: 'THANH_PHO', label: 'Cấp thành phố' },
  { value: 'TINH', label: 'Cấp tỉnh' },
];

/** Chức năng 19 — Danh sách hội đồng sáng kiến. */
export default function TrangHoiDong() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();
  const duocCauHinh = useAuthStore((s) => s.coQuyen('HOI_DONG.CAU_HINH'));

  const [trang, setTrang] = useState(1);
  const [soDong, setSoDong] = useState(20);
  const [tuKhoa, setTuKhoa] = useState('');
  const [dangSua, setDangSua] = useState<DanhMucDto | null>(null);
  const [moForm, setMoForm] = useState(false);
  const [form] = Form.useForm();

  const thamSo = { trang, soDong, tuKhoa };

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['hoi-dong', thamSo],
    queryFn: () => apiHoiDong.danhSach(thamSo),
  });

  const { data: cacDot } = useQuery({ queryKey: ['dot-chon'], queryFn: apiDotDeNghi.chon });
  const { data: cacDonVi } = useQuery({ queryKey: ['don-vi-chon'], queryFn: apiDonVi.chon });
  const { data: cacLinhVuc } = useQuery({ queryKey: ['linh-vuc-chon'], queryFn: apiLinhVuc.chon });

  const luu = useMutation({
    mutationFn: async (giaTri: Record<string, unknown>) => {
      const duLieu = {
        ...giaTri,
        ngayQuyetDinh: giaTri.ngayQuyetDinh
          ? dayjs(giaTri.ngayQuyetDinh as string).format('YYYY-MM-DD')
          : null,
        thoiGianHoatDongTu: giaTri.thoiGianHoatDongTu
          ? dayjs(giaTri.thoiGianHoatDongTu as string).format('YYYY-MM-DD')
          : null,
        thoiGianHoatDongDen: giaTri.thoiGianHoatDongDen
          ? dayjs(giaTri.thoiGianHoatDongDen as string).format('YYYY-MM-DD')
          : null,
        linhVucPhuTrach: (giaTri.linhVucPhuTrach as string[]) ?? [],
      };

      return dangSua ? apiHoiDong.sua(dangSua.id, duLieu) : apiHoiDong.them(duLieu);
    },
    onSuccess: () => {
      message.success(dangSua ? 'Đã cập nhật hội đồng' : 'Đã tạo hội đồng');
      setMoForm(false);
      setDangSua(null);
      form.resetFields();
      void queryClient.invalidateQueries({ queryKey: ['hoi-dong'] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không lưu được.'),
  });

  const xoa = useMutation({
    mutationFn: (id: string) => apiHoiDong.xoa(id),
    onSuccess: () => {
      message.success('Đã xoá hội đồng');
      void queryClient.invalidateQueries({ queryKey: ['hoi-dong'] });
    },
    onError: (loi) =>
      modal.error({
        title: 'Không xoá được',
        content: loi instanceof LoiApi ? loi.message : 'Đã xảy ra lỗi.',
      }),
  });

  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  async function moSua(dong: DanhMucDto) {
    const chiTiet = await apiHoiDong.theoId(dong.id);
    setDangSua(dong);
    form.setFieldsValue({
      ...chiTiet,
      ngayQuyetDinh: chiTiet.ngayQuyetDinh ? dayjs(chiTiet.ngayQuyetDinh) : undefined,
      thoiGianHoatDongTu: chiTiet.thoiGianHoatDongTu ? dayjs(chiTiet.thoiGianHoatDongTu) : undefined,
      thoiGianHoatDongDen: chiTiet.thoiGianHoatDongDen
        ? dayjs(chiTiet.thoiGianHoatDongDen)
        : undefined,
    });
    setMoForm(true);
  }

  return (
    <Card
      title="Hội đồng sáng kiến"
      extra={
        <Space>
          <Input.Search
            placeholder="Tìm theo tên hội đồng (không dấu)"
            allowClear
            style={{ width: 260 }}
            onSearch={(v) => {
              setTuKhoa(v);
              setTrang(1);
            }}
          />
          {duocCauHinh && (
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => {
                setDangSua(null);
                form.resetFields();
                setMoForm(true);
              }}
            >
              Thành lập hội đồng
            </Button>
          )}
        </Space>
      }
    >
      <Table<DanhMucDto>
        rowKey="id"
        size="middle"
        loading={isLoading}
        dataSource={data?.duLieu ?? []}
        scroll={{ x: 1210 }}
        columns={[
          { title: 'Mã', dataIndex: 'ma', width: 180 },
          {
            title: 'Tên hội đồng',
            dataIndex: 'ten',
            width: 320,
            render: (v: string, dong) => <Link to={`/hoi-dong/${dong.id}`}>{v}</Link>,
          },
          { title: 'Mô tả', dataIndex: 'moTa', width: 260, ellipsis: true, responsive: ['lg'] },
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
            width: 130,
            responsive: ['xl'],
            render: (v: string) => ngayGio(v, false),
          },
          {
            title: '',
            width: 150,
            fixed: 'right',
            render: (_v, dong) => (
              <Space>
                <Link to={`/hoi-dong/${dong.id}`}>
                  <Button size="small" icon={<SettingOutlined />}>
                    Mở
                  </Button>
                </Link>
                {duocCauHinh && (
                  <>
                    <Button type="text" icon={<EditOutlined />} onClick={() => void moSua(dong)} />
                    <Button
                      type="text"
                      danger
                      icon={<DeleteOutlined />}
                      onClick={() =>
                        modal.confirm({
                          title: 'Xác nhận xoá',
                          content: `Xoá hội đồng "${dong.ten}"?`,
                          okText: 'Xoá',
                          okButtonProps: { danger: true },
                          cancelText: 'Huỷ',
                          onOk: () => xoa.mutateAsync(dong.id),
                        })
                      }
                    />
                  </>
                )}
              </Space>
            ),
          },
        ]}
        pagination={{
          current: trang,
          pageSize: soDong,
          total: data?.tongSo ?? 0,
          showSizeChanger: true,
          showTotal: (t) => `Tổng ${t} hội đồng`,
          onChange: (t, s) => {
            setTrang(t);
            setSoDong(s);
          },
        }}
      />

      <Modal
        open={moForm}
        width={640}
        title={dangSua ? `Sửa hội đồng: ${dangSua.ten}` : 'Thành lập hội đồng'}
        okText="Lưu"
        cancelText="Huỷ"
        confirmLoading={luu.isPending}
        onCancel={() => setMoForm(false)}
        onOk={async () => luu.mutate(await form.validateFields())}
      >
        <Form
          form={form}
          layout="vertical"
          initialValues={{
            trangThai: 1,
            thuTu: 0,
            cap: 'CO_SO',
            soThanhVienToiThieu: 5,
            tyLeThongQua: 50,
            trangThaiHoatDong: 'DANG_HOAT_DONG',
          }}
        >
          <Form.Item
            name="ma"
            label="Mã hội đồng"
            rules={[
              { required: true, message: 'Vui lòng nhập mã' },
              { pattern: /^[A-Z0-9_-]+$/, message: 'Mã chỉ gồm chữ hoa, số, dấu _ và -' },
            ]}
          >
            <Input placeholder="VD: HD-CO-SO-2027" disabled={!!dangSua} />
          </Form.Item>

          <Form.Item
            name="ten"
            label="Tên hội đồng"
            rules={[{ required: true, message: 'Nhập tên' }]}
          >
            <Input />
          </Form.Item>

          <Space size="large" wrap>
            <Form.Item name="cap" label="Cấp xét duyệt">
              <Select style={{ width: 180 }} options={CAP_HOI_DONG} />
            </Form.Item>
            <Form.Item name="dotDeNghiId" label="Đợt đề nghị">
              <Select
                style={{ width: 220 }}
                allowClear
                placeholder="Không giới hạn đợt"
                options={(cacDot ?? []).map((x) => ({ value: x.id, label: x.ten }))}
              />
            </Form.Item>
            <Form.Item name="donViId" label="Đơn vị">
              <Select
                style={{ width: 220 }}
                allowClear
                showSearch
                optionFilterProp="label"
                options={(cacDonVi ?? []).map((x) => ({ value: x.id, label: x.ten }))}
              />
            </Form.Item>
          </Space>

          <Form.Item name="linhVucPhuTrach" label="Lĩnh vực phụ trách">
            <Select
              mode="multiple"
              allowClear
              placeholder="Để trống = phụ trách mọi lĩnh vực"
              options={(cacLinhVuc ?? []).map((x) => ({ value: x.id, label: x.ten }))}
            />
          </Form.Item>

          <Space size="large" wrap>
            <Form.Item name="soQuyetDinhThanhLap" label="Số quyết định thành lập">
              <Input style={{ width: 220 }} placeholder="VD: 12/QĐ-UBND" />
            </Form.Item>
            <Form.Item name="ngayQuyetDinh" label="Ngày quyết định">
              <DatePicker format="DD/MM/YYYY" style={{ width: 170 }} />
            </Form.Item>
          </Space>

          <Space size="large" wrap>
            <Form.Item name="thoiGianHoatDongTu" label="Hoạt động từ">
              <DatePicker format="DD/MM/YYYY" style={{ width: 170 }} />
            </Form.Item>
            <Form.Item name="thoiGianHoatDongDen" label="Đến">
              <DatePicker format="DD/MM/YYYY" style={{ width: 170 }} />
            </Form.Item>
          </Space>

          <Space size="large" wrap>
            <Form.Item
              name="soThanhVienToiThieu"
              label="Số thành viên tối thiểu"
              tooltip="Lưu danh sách ít hơn số này sẽ bị máy chủ từ chối."
            >
              <InputNumber min={1} max={99} style={{ width: 180 }} />
            </Form.Item>
            <Form.Item name="tyLeThongQua" label="Tỷ lệ thông qua (%)">
              <InputNumber min={1} max={100} style={{ width: 160 }} />
            </Form.Item>
            <Form.Item name="trangThaiHoatDong" label="Tình trạng">
              <Select
                style={{ width: 180 }}
                options={[
                  { value: 'DANG_HOAT_DONG', label: 'Đang hoạt động' },
                  { value: 'DA_KET_THUC', label: 'Đã kết thúc' },
                ]}
              />
            </Form.Item>
          </Space>

          <Form.Item name="moTa" label="Mô tả">
            <Input.TextArea rows={2} />
          </Form.Item>

          <Space size="large">
            <Form.Item name="thuTu" label="Thứ tự">
              <InputNumber min={0} />
            </Form.Item>
            <Form.Item name="trangThai" label="Trạng thái">
              <Select
                style={{ width: 180 }}
                options={[
                  { value: 1, label: 'Hoạt động' },
                  { value: 0, label: 'Ngừng hoạt động' },
                ]}
              />
            </Form.Item>
          </Space>
        </Form>
      </Modal>
    </Card>
  );
}
