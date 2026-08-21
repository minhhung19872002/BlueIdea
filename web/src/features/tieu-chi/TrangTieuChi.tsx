import { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Alert,
  App,
  Button,
  Card,
  Input,
  InputNumber,
  Modal,
  Select,
  Space,
  Switch,
  Table,
  Tag,
  Tooltip,
} from 'antd';
import { CopyOutlined, PlusOutlined, SettingOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { z } from 'zod';

import { LoiApi } from '@/api/client';
import { BieuMau, Truong, useBieuMau } from '@/components/bieu-mau/BieuMau';
import { batBuoc, maDanhMuc, soNguyen, trangThai } from '@/components/bieu-mau/luat';
import { apiTieuChi, type DanhMucDto } from '@/api/endpoints';
import { KhoiLoi } from '@/components/ThanhPhanChung';

/**
 * Luật kiểm tra bộ tiêu chí.
 *
 * Chặn "điểm đạt tối thiểu > thang điểm" ngay tại đây: cấu hình đó khiến KHÔNG hồ sơ nào đạt
 * được, và lỗi chỉ lộ ra sau khi cả hội đồng đã chấm xong — lúc đó sửa lại là phải chấm lại.
 */
const luatBoTieuChi = z
  .object({
    ma: maDanhMuc(),
    ten: batBuoc('Tên bộ tiêu chí'),
    nam: soNguyen('Năm', 2000, 2100),
    thangDiemToiDa: soNguyen('Thang điểm', 1, 1000),
    diemDatToiThieu: soNguyen('Điểm đạt tối thiểu', 0, 1000),
    cachTinh: z.string(),
    lamTron: soNguyen('Làm tròn', 0, 4),
    tuDongTongHop: z.boolean(),
    loaiBoDiemCaoThap: z.boolean(),
    trangThai: trangThai,
  })
  .refine((v) => v.diemDatToiThieu <= v.thangDiemToiDa, {
    path: ['diemDatToiThieu'],
    message: 'Điểm đạt tối thiểu không được lớn hơn thang điểm.',
  });

type GiaTriBoTieuChi = z.infer<typeof luatBoTieuChi>;

const MAC_DINH_TIEU_CHI: GiaTriBoTieuChi = {
  ma: '',
  ten: '',
  nam: new Date().getFullYear(),
  thangDiemToiDa: 100,
  diemDatToiThieu: 50,
  cachTinh: 'TONG_DIEM',
  lamTron: 2,
  tuDongTongHop: true,
  loaiBoDiemCaoThap: false,
  trangThai: 1,
};

/** Luật cho hộp thoại sao chép — chỉ cần mã, tên và năm áp dụng của bản sao. */
const luatSaoChep = z.object({
  ma: maDanhMuc(),
  ten: batBuoc('Tên bộ mới'),
  nam: soNguyen('Năm áp dụng', 2000, 2100),
});

type GiaTriSaoChep = z.infer<typeof luatSaoChep>;

/** Chức năng 17 — Danh sách bộ tiêu chí chấm điểm. */
export default function TrangTieuChi() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [trang, setTrang] = useState(1);
  const [soDong, setSoDong] = useState(20);
  const [moTao, setMoTao] = useState(false);
  const [saoChep, setSaoChep] = useState<DanhMucDto | null>(null);
  const form = useBieuMau(luatBoTieuChi, MAC_DINH_TIEU_CHI);
  const formSaoChep = useBieuMau(luatSaoChep);

  const taoBanSao = useMutation({
    mutationFn: (giaTri: GiaTriSaoChep) =>
      apiTieuChi.saoChep(saoChep!.id, {
        ma: giaTri.ma.trim().toUpperCase(),
        ten: giaTri.ten,
        nam: giaTri.nam,
      }),
    onSuccess: () => {
      message.success('Đã sao chép bộ tiêu chí');
      setSaoChep(null);
      formSaoChep.reset();
      void queryClient.invalidateQueries({ queryKey: ['bo-tieu-chi'] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không sao chép được.'),
  });

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['bo-tieu-chi', { trang, soDong }],
    queryFn: () => apiTieuChi.danhSach({ trang, soDong }),
  });

  const tao = useMutation({
    mutationFn: (giaTri: GiaTriBoTieuChi) => apiTieuChi.them(giaTri),
    onSuccess: () => {
      message.success('Đã tạo bộ tiêu chí');
      setMoTao(false);
      form.reset(MAC_DINH_TIEU_CHI);
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
        scroll={{ x: 860 }}
        columns={[
          { title: 'Mã', dataIndex: 'ma', width: 200 },
          {
            title: 'Tên bộ tiêu chí',
            dataIndex: 'ten',
            width: 300,
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
                      formSaoChep.reset({
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
        confirmLoading={taoBanSao.isPending || formSaoChep.formState.isSubmitting}
        onCancel={() => setSaoChep(null)}
        okButtonProps={{ htmlType: 'submit', form: 'form-sao-chep-tieu-chi' }}
      >
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 12 }}
          message="Bản sao nhận đủ cây tiêu chí, thang điểm và các mức công nhận của bộ gốc."
          description="Dùng khi sang năm mới: sửa vài tiêu chí trên bản sao thay vì gõ lại từ đầu, và bộ cũ vẫn giữ nguyên để tra cứu hồ sơ các năm trước."
        />
        <BieuMau
          id="form-sao-chep-tieu-chi"
          form={formSaoChep}
          onGui={(giaTri) => taoBanSao.mutateAsync(giaTri)}
        >
          <Truong<GiaTriSaoChep> ten="ma" label="Mã bộ mới" required>
            {(o) => <Input {...o} value={o.value as string} placeholder="VD: BTC-CO-SO-2027" />}
          </Truong>
          <Truong<GiaTriSaoChep> ten="ten" label="Tên bộ mới" required>
            {(o) => <Input {...o} value={o.value as string} />}
          </Truong>
          <Truong<GiaTriSaoChep> ten="nam" label="Năm áp dụng" required>
            {(o) => (
              <InputNumber
                {...o}
                value={o.value as number}
                min={2000}
                max={2100}
                style={{ width: 160 }}
              />
            )}
          </Truong>
        </BieuMau>
      </Modal>

      <Modal
        open={moTao}
        title="Tạo bộ tiêu chí"
        okText="Tạo"
        cancelText="Hủy"
        confirmLoading={tao.isPending || form.formState.isSubmitting}
        onCancel={() => setMoTao(false)}
        okButtonProps={{ htmlType: 'submit', form: 'form-tao-tieu-chi' }}
      >
        <BieuMau id="form-tao-tieu-chi" form={form} onGui={(giaTri) => tao.mutateAsync(giaTri)}>
          <Truong<GiaTriBoTieuChi> ten="ma" label="Mã" required>
            {(o) => <Input {...o} value={o.value as string} placeholder="VD: BTC-CO-SO-2027" />}
          </Truong>
          <Truong<GiaTriBoTieuChi> ten="ten" label="Tên" required>
            {(o) => <Input {...o} value={o.value as string} />}
          </Truong>
          <Space size="large">
            <Truong<GiaTriBoTieuChi> ten="nam" label="Năm">
              {(o) => <InputNumber {...o} value={o.value as number} min={2000} max={2100} />}
            </Truong>
            <Truong<GiaTriBoTieuChi> ten="thangDiemToiDa" label="Thang điểm">
              {(o) => <InputNumber {...o} value={o.value as number} min={1} max={1000} />}
            </Truong>
            <Truong<GiaTriBoTieuChi> ten="diemDatToiThieu" label="Điểm đạt tối thiểu">
              {(o) => <InputNumber {...o} value={o.value as number} min={0} max={1000} />}
            </Truong>
          </Space>
          <Truong<GiaTriBoTieuChi> ten="cachTinh" label="Cách tính điểm">
            {(o) => (
              <Select
                {...o}
                value={o.value as string}
                options={[
                  { value: 'TONG_DIEM', label: 'Tổng điểm các tiêu chí' },
                  { value: 'TRUNG_BINH_CONG', label: 'Trung bình cộng' },
                  { value: 'TRUNG_BINH_TRONG_SO', label: 'Trung bình theo trọng số nhóm' },
                ]}
              />
            )}
          </Truong>

          {/*
            * Hai công tắc này trước đây chỉ đặt được qua API nên không ai đặt: bộ tiêu chí tạo từ
            * màn hình luôn nhận giá trị mặc định của DTO. Đưa lên form để người cấu hình chọn.
            */}
          <Truong<GiaTriBoTieuChi>
            ten="tuDongTongHop"
            label="Tự động tổng hợp điểm"
            tooltip="Người cuối cùng trong danh sách phân công gửi phiếu là hệ thống tổng hợp ngay, thư ký không phải bấm."
          >
            {(o) => (
              <Switch checked={o.value as boolean} onChange={(v) => o.onChange?.(v)} />
            )}
          </Truong>

          <Truong<GiaTriBoTieuChi>
            ten="loaiBoDiemCaoThap"
            label="Loại điểm cao nhất và thấp nhất"
            tooltip="Chỉ áp dụng khi có từ 5 phiếu trở lên."
          >
            {(o) => (
              <Switch checked={o.value as boolean} onChange={(v) => o.onChange?.(v)} />
            )}
          </Truong>
        </BieuMau>
      </Modal>
    </Card>
  );
}
