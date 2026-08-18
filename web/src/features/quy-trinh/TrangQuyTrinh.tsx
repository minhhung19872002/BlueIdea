import { useState } from 'react';
import { Link } from 'react-router-dom';
import { App, Button, Card, Input, Modal, Space, Table, Tag, Typography } from 'antd';
import {
  ApartmentOutlined,
  CheckCircleOutlined,
  BlockOutlined,
  CopyOutlined,
  PlusOutlined,
  StopOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { z } from 'zod';

import { LoiApi } from '@/api/client';
import { BieuMau, Truong, useBieuMau } from '@/components/bieu-mau/BieuMau';
import { batBuoc, maDanhMuc, trangThai, tuyChon } from '@/components/bieu-mau/luat';
import { apiQuyTrinh, type DanhMucDto } from '@/api/endpoints';
import { KhoiLoi, ngayGio } from '@/components/ThanhPhanChung';

/**
 * Luật kiểm tra khi tạo quy trình mới.
 *
 * Mã quy trình được đưa vào snapshot của từng hồ sơ khi nộp, nên chốt định dạng ngay từ lúc tạo:
 * đổi mã sau khi đã có hồ sơ chạy theo quy trình đó là việc rất tốn công.
 */
const luatQuyTrinh = z.object({
  ma: maDanhMuc(),
  ten: batBuoc('Tên quy trình'),
  moTa: tuyChon(),
  cap: z.string(),
  trangThai: trangThai,
});

type GiaTriQuyTrinh = z.infer<typeof luatQuyTrinh>;

const MAC_DINH_QUY_TRINH: GiaTriQuyTrinh = {
  ma: '',
  ten: '',
  cap: 'CO_SO',
  trangThai: 1,
};

/** Chức năng 9 — Danh sách quy trình xử lý và các thao tác vòng đời. */
export default function TrangQuyTrinh() {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [trang, setTrang] = useState(1);
  const [soDong, setSoDong] = useState(20);
  const [moTao, setMoTao] = useState(false);
  const form = useBieuMau(luatQuyTrinh, MAC_DINH_QUY_TRINH);

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

  /** Sao chép sang một quy trình MỚI — khác "tạo phiên bản mới" của chính quy trình đang có. */
  const saoChep = useMutation({
    mutationFn: ({ id, ma, ten }: { id: string; ma: string; ten: string }) =>
      apiQuyTrinh.saoChep(id, ma, ten),
    onSuccess: () => {
      message.success('Đã sao chép thành quy trình mới');
      void lamMoi();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không sao chép được.'),
  });

  const tao = useMutation({
    mutationFn: (giaTri: GiaTriQuyTrinh) => apiQuyTrinh.them(giaTri),
    onSuccess: () => {
      message.success('Đã tạo quy trình');
      setMoTao(false);
      form.reset(MAC_DINH_QUY_TRINH);
      void lamMoi();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không tạo được.'),
  });

  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  function moSaoChep(dong: DanhMucDto) {
    let ma = `${dong.ma}-COPY`;
    let ten = `${dong.ten} (bản sao)`;

    modal.confirm({
      title: `Sao chép quy trình "${dong.ten}"`,
      content: (
        <Space direction="vertical" style={{ width: '100%', marginTop: 8 }}>
          <Input
            defaultValue={ma}
            addonBefore="Mã"
            onChange={(e) => {
              ma = e.target.value;
            }}
          />
          <Input
            defaultValue={ten}
            addonBefore="Tên"
            onChange={(e) => {
              ten = e.target.value;
            }}
          />
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            Bản sao giữ nguyên bước, trường hợp, tác nhân và thành phần hồ sơ; trạng thái là nháp
            nên phải kiểm tra hợp lệ rồi kích hoạt mới dùng được.
          </Typography.Text>
        </Space>
      ),
      okText: 'Sao chép',
      cancelText: 'Huỷ',
      onOk: () => {
        if (!ma.trim() || !ten.trim()) {
          message.warning('Mã và tên không được để trống.');
          return Promise.reject(new Error('thieu-du-lieu'));
        }

        return saoChep.mutateAsync({ id: dong.id, ma: ma.trim(), ten: ten.trim() });
      },
    });
  }

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
                  icon={<BlockOutlined />}
                  loading={saoChep.isPending}
                  title="Sao chép thành quy trình mới"
                  onClick={() => moSaoChep(dong)}
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
        confirmLoading={tao.isPending || form.formState.isSubmitting}
        onCancel={() => setMoTao(false)}
        okButtonProps={{ htmlType: 'submit', form: 'form-quy-trinh' }}
      >
        <BieuMau id="form-quy-trinh" form={form} onGui={(giaTri) => tao.mutateAsync(giaTri)}>
          <Truong<GiaTriQuyTrinh> ten="ma" label="Mã" required>
            {(o) => <Input {...o} value={o.value as string} placeholder="VD: QT-CO-SO-2027" />}
          </Truong>
          <Truong<GiaTriQuyTrinh> ten="ten" label="Tên" required>
            {(o) => <Input {...o} value={o.value as string} />}
          </Truong>
          <Truong<GiaTriQuyTrinh> ten="moTa" label="Mô tả">
            {(o) => <Input.TextArea {...o} value={o.value as string} rows={2} />}
          </Truong>
        </BieuMau>
      </Modal>
    </Card>
  );
}
