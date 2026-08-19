import { useState } from 'react';
import { App, Alert, Button, DatePicker, Input, Modal, Popconfirm, Select, Space, Table, Tag } from 'antd';
import { KeyOutlined, PlusOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';
import type { Dayjs } from 'dayjs';
import { z } from 'zod';

import { capNhatDuLieu, guiDuLieu, layDuLieu, LoiApi, xoaDuLieu } from '@/api/client';
import { ngayGio } from '@/components/ThanhPhanChung';
import { BieuMau, Truong, useBieuMau } from '@/components/bieu-mau/BieuMau';
import { batBuoc, tuyChon } from '@/components/bieu-mau/luat';

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

/**
 * Luật kiểm tra khoá API.
 *
 * Kiểm tra định dạng IP / CIDR ngay tại giao diện: khai sai một dải là hệ thống ngoài bị chặn
 * im lặng, và người cấu hình rất khó đoán vì lỗi chỉ lộ ra lúc bên kia gọi vào.
 */
const MAU_IP_HOAC_CIDR =
  /^(\d{1,3}\.){3}\d{1,3}(\/([0-9]|[12][0-9]|3[0-2]))?$/;

const luatKhoaApi = z.object({
  ten: batBuoc('Tên hệ thống'),
  danhSachIp: z
    .array(
      z
        .string()
        .trim()
        .refine(
          (v) =>
            MAU_IP_HOAC_CIDR.test(v)
            && v.split('/')[0].split('.').every((o) => Number(o) <= 255),
          'Địa chỉ IP hoặc dải CIDR không hợp lệ.',
        ),
    )
    .optional(),
  ngayHetHan: z.custom<Dayjs>((v) => v == null || dayjs.isDayjs(v)).nullish(),
  ghiChu: tuyChon(),
});

type FormKhoa = z.infer<typeof luatKhoaApi>;

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
  const form = useBieuMau(luatKhoaApi, { ten: '' } as FormKhoa);

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
      form.reset({ ten: '' } as FormKhoa);
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
    form.reset({ ten: '' } as FormKhoa);
    setMoForm(true);
  }

  function moSua(ban: KhoaApi) {
    setDangSua(ban);
    form.reset({
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
          { title: 'Tên hệ thống', dataIndex: 'ten', width: 200 },
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
        okButtonProps={{ htmlType: 'submit', form: 'form-khoa-api' }}
        confirmLoading={luu.isPending || form.formState.isSubmitting}
        okText="Lưu"
        cancelText="Huỷ"
      >
        <BieuMau id="form-khoa-api" form={form} onGui={(v) => luu.mutateAsync(v)}>
          <Truong<FormKhoa> ten="ten" label="Tên hệ thống" required>
            {(o) => (
              <Input
                {...o}
                value={o.value as string}
                placeholder="Ví dụ: Hệ thống Thi đua khen thưởng"
              />
            )}
          </Truong>

          <Truong<FormKhoa>
            ten="danhSachIp"
            label="Địa chỉ IP được phép"
            extra="Để trống nghĩa là cho phép mọi địa chỉ. Nhập IP đơn hoặc dải CIDR, ví dụ 10.0.0.0/8."
          >
            {(o) => (
              <Select
                {...o}
                value={o.value as string[] | undefined}
                mode="tags"
                placeholder="203.0.113.7 hoặc 10.0.0.0/8"
                tokenSeparators={[',']}
              />
            )}
          </Truong>

          <Truong<FormKhoa> ten="ngayHetHan" label="Ngày hết hạn">
            {(o) => (
              <DatePicker
                value={(o.value as Dayjs | null | undefined) ?? null}
                onChange={o.onChange}
                onBlur={o.onBlur}
                status={o.status}
                style={{ width: '100%' }}
                format="DD/MM/YYYY"
              />
            )}
          </Truong>

          <Truong<FormKhoa> ten="ghiChu" label="Ghi chú">
            {(o) => <Input.TextArea {...o} value={o.value as string} rows={2} />}
          </Truong>
        </BieuMau>
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
