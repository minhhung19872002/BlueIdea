import { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  App,
  Button,
  Card,
  DatePicker,
  Form as FormAntd,
  Input,
  InputNumber,
  Modal,
  Select,
  Space,
  Table,
  Tag,
  Typography,
  Upload,
} from 'antd';
import {
  DeleteOutlined,
  EditOutlined,
  FilePdfOutlined,
  PlusOutlined,
  SettingOutlined,
  UploadOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';
import type { Dayjs } from 'dayjs';
import { z } from 'zod';

import { LoiApi, taiTep } from '@/api/client';
import { BieuMau, Truong, useBieuMau } from '@/components/bieu-mau/BieuMau';
import { batBuoc, maDanhMuc, soNguyen, trangThai, tuyChon } from '@/components/bieu-mau/luat';
import {
  apiDonVi,
  apiDotDeNghi,
  apiHoiDong,
  apiLinhVuc,
  taiTepLen,
  type DanhMucDto,
} from '@/api/endpoints';
import { useAuthStore } from '@/app/store/authStore';
import { KhoiLoi, ngayGio } from '@/components/ThanhPhanChung';

export const CAP_HOI_DONG = [
  { value: 'CO_SO', label: 'Cấp cơ sở' },
  { value: 'THANH_PHO', label: 'Cấp thành phố' },
  { value: 'TINH', label: 'Cấp tỉnh' },
];

/**
 * Luật kiểm tra hội đồng.
 *
 * Chặn "hoạt động đến" sớm hơn "hoạt động từ": khoảng thời gian ngược khiến hội đồng không bao
 * giờ ở trong kỳ hoạt động, và các màn hình lọc theo kỳ sẽ bỏ sót nó mà không báo gì.
 */
const luatHoiDong = z
  .object({
    ma: maDanhMuc(),
    ten: batBuoc('Tên hội đồng'),
    cap: z.string(),
    dotDeNghiId: z.string().uuid().optional().nullable(),
    donViId: z.string().uuid().optional().nullable(),
    linhVucPhuTrach: z.array(z.string()),
    soQuyetDinhThanhLap: tuyChon(100),
    ngayQuyetDinh: z.custom<Dayjs>((v) => v == null || dayjs.isDayjs(v)).nullish(),
    thoiGianHoatDongTu: z.custom<Dayjs>((v) => v == null || dayjs.isDayjs(v)).nullish(),
    thoiGianHoatDongDen: z.custom<Dayjs>((v) => v == null || dayjs.isDayjs(v)).nullish(),
    soThanhVienToiThieu: soNguyen('Số thành viên tối thiểu', 1, 99),
    tyLeThongQua: soNguyen('Tỷ lệ thông qua', 1, 100),
    trangThaiHoatDong: z.string(),
    moTa: tuyChon(),
    thuTu: soNguyen('Thứ tự', 0, 9999),
    trangThai: trangThai,
  })
  .superRefine((v, ctx) => {
    if (
      v.thoiGianHoatDongTu
      && v.thoiGianHoatDongDen
      && v.thoiGianHoatDongDen.isBefore(v.thoiGianHoatDongTu)
    ) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['thoiGianHoatDongDen'],
        message: 'Ngày kết thúc phải sau ngày bắt đầu hoạt động.',
      });
    }
  });

type GiaTriHoiDong = z.infer<typeof luatHoiDong>;

const MAC_DINH_HOI_DONG: GiaTriHoiDong = {
  ma: '',
  ten: '',
  cap: 'CO_SO',
  linhVucPhuTrach: [],
  soThanhVienToiThieu: 5,
  tyLeThongQua: 50,
  trangThaiHoatDong: 'DANG_HOAT_DONG',
  thuTu: 0,
  trangThai: 1,
};

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
  const [tepQuyetDinh, setTepQuyetDinh] = useState<{ id: string; ten: string } | null>(null);
  const [dangTaiTep, setDangTaiTep] = useState(false);
  const form = useBieuMau(luatHoiDong, MAC_DINH_HOI_DONG);

  const thamSo = { trang, soDong, tuKhoa };

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['hoi-dong', thamSo],
    queryFn: () => apiHoiDong.danhSach(thamSo),
  });

  const { data: cacDot } = useQuery({ queryKey: ['dot-chon'], queryFn: apiDotDeNghi.chon });
  const { data: cacDonVi } = useQuery({ queryKey: ['don-vi-chon'], queryFn: apiDonVi.chon });
  const { data: cacLinhVuc } = useQuery({ queryKey: ['linh-vuc-chon'], queryFn: apiLinhVuc.chon });

  const luu = useMutation({
    mutationFn: async (giaTri: GiaTriHoiDong) => {
      const duLieu = {
        ...giaTri,
        ngayQuyetDinh: giaTri.ngayQuyetDinh
          ? giaTri.ngayQuyetDinh.format('YYYY-MM-DD')
          : null,
        thoiGianHoatDongTu: giaTri.thoiGianHoatDongTu
          ? giaTri.thoiGianHoatDongTu.format('YYYY-MM-DD')
          : null,
        thoiGianHoatDongDen: giaTri.thoiGianHoatDongDen
          ? giaTri.thoiGianHoatDongDen.format('YYYY-MM-DD')
          : null,
        linhVucPhuTrach: giaTri.linhVucPhuTrach ?? [],
        tepQuyetDinhId: tepQuyetDinh?.id ?? null,
      };

      return dangSua ? apiHoiDong.sua(dangSua.id, duLieu) : apiHoiDong.them(duLieu);
    },
    onSuccess: () => {
      message.success(dangSua ? 'Đã cập nhật hội đồng' : 'Đã tạo hội đồng');
      setMoForm(false);
      setDangSua(null);
      form.reset(MAC_DINH_HOI_DONG);
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
    form.reset({
      ...MAC_DINH_HOI_DONG,
      ...(chiTiet as unknown as GiaTriHoiDong),
      ngayQuyetDinh: chiTiet.ngayQuyetDinh ? dayjs(chiTiet.ngayQuyetDinh) : undefined,
      thoiGianHoatDongTu: chiTiet.thoiGianHoatDongTu ? dayjs(chiTiet.thoiGianHoatDongTu) : undefined,
      thoiGianHoatDongDen: chiTiet.thoiGianHoatDongDen
        ? dayjs(chiTiet.thoiGianHoatDongDen)
        : undefined,
    });
    // Chi luu duoc Id tep, khong luu ten: ten that lay tu Content-Disposition khi bam tai ve.
    setTepQuyetDinh(
      chiTiet.tepQuyetDinhId
        ? { id: chiTiet.tepQuyetDinhId, ten: 'Quyết định thành lập đã đính kèm' }
        : null,
    );
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
                form.reset(MAC_DINH_HOI_DONG);
                setTepQuyetDinh(null);
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
        confirmLoading={luu.isPending || form.formState.isSubmitting}
        onCancel={() => setMoForm(false)}
        okButtonProps={{ htmlType: 'submit', form: 'form-hoi-dong' }}
      >
        <BieuMau id="form-hoi-dong" form={form} onGui={(giaTri) => luu.mutateAsync(giaTri)}>
          <Truong<GiaTriHoiDong> ten="ma" label="Mã hội đồng" required>
            {(o) => (
              <Input
                {...o}
                value={o.value as string}
                placeholder="VD: HD-CO-SO-2027"
                disabled={!!dangSua}
              />
            )}
          </Truong>

          <Truong<GiaTriHoiDong> ten="ten" label="Tên hội đồng" required>
            {(o) => <Input {...o} value={o.value as string} />}
          </Truong>

          <Space size="large" wrap>
            <Truong<GiaTriHoiDong> ten="cap" label="Cấp xét duyệt">
              {(o) => (
                <Select
                  {...o}
                  value={o.value as string}
                  style={{ width: 180 }}
                  options={CAP_HOI_DONG}
                />
              )}
            </Truong>
            <Truong<GiaTriHoiDong> ten="dotDeNghiId" label="Đợt đề nghị">
              {(o) => (
                <Select
                  {...o}
                  value={o.value as string | undefined}
                  style={{ width: 220 }}
                  allowClear
                  placeholder="Không giới hạn đợt"
                  options={(cacDot ?? []).map((x) => ({ value: x.id, label: x.ten }))}
                />
              )}
            </Truong>
            <Truong<GiaTriHoiDong> ten="donViId" label="Đơn vị">
              {(o) => (
                <Select
                  {...o}
                  value={o.value as string | undefined}
                  style={{ width: 220 }}
                  allowClear
                  showSearch
                  optionFilterProp="label"
                  options={(cacDonVi ?? []).map((x) => ({ value: x.id, label: x.ten }))}
                />
              )}
            </Truong>
          </Space>

          <Truong<GiaTriHoiDong> ten="linhVucPhuTrach" label="Lĩnh vực phụ trách">
            {(o) => (
              <Select
                {...o}
                value={o.value as string[]}
                mode="multiple"
                allowClear
                placeholder="Để trống = phụ trách mọi lĩnh vực"
                options={(cacLinhVuc ?? []).map((x) => ({ value: x.id, label: x.ten }))}
              />
            )}
          </Truong>

          <Space size="large" wrap>
            <Truong<GiaTriHoiDong> ten="soQuyetDinhThanhLap" label="Số quyết định thành lập">
              {(o) => (
                <Input
                  {...o}
                  value={o.value as string}
                  style={{ width: 220 }}
                  placeholder="VD: 12/QĐ-UBND"
                />
              )}
            </Truong>
            <Truong<GiaTriHoiDong> ten="ngayQuyetDinh" label="Ngày quyết định">
              {(o) => (
                <DatePicker
                  value={(o.value as Dayjs | null | undefined) ?? null}
                  onChange={o.onChange}
                  onBlur={o.onBlur}
                  status={o.status}
                  format="DD/MM/YYYY"
                  style={{ width: 170 }}
                />
              )}
            </Truong>
          </Space>

          <FormAntd.Item
            label="Tệp quyết định thành lập"
            tooltip="Bản scan quyết định thành lập hội đồng — dùng làm căn cứ khi in biên bản họp."
          >
            <Space wrap>
              <Upload
                accept=".pdf,.doc,.docx,.png,.jpg,.jpeg"
                maxCount={1}
                showUploadList={false}
                beforeUpload={(tep) => {
                  void (async () => {
                    setDangTaiTep(true);
                    try {
                      const daTaiLen = await taiTepLen(tep as File);
                      setTepQuyetDinh({ id: daTaiLen.id, ten: daTaiLen.tenGoc });
                      message.success('Đã tải tệp quyết định lên');
                    } catch (loi) {
                      message.error(
                        loi instanceof LoiApi ? loi.message : 'Không tải được tệp lên.',
                      );
                    } finally {
                      setDangTaiTep(false);
                    }
                  })();

                  return false;
                }}
              >
                <Button icon={<UploadOutlined />} loading={dangTaiTep}>
                  Chọn tệp
                </Button>
              </Upload>

              {tepQuyetDinh ? (
                <Space>
                  <Button
                    type="link"
                    icon={<FilePdfOutlined />}
                    onClick={() =>
                      void taiTep(`/api/v1/tep-tin/${tepQuyetDinh.id}/tai-ve`, tepQuyetDinh.ten)
                    }
                  >
                    {tepQuyetDinh.ten}
                  </Button>
                  <Button type="text" danger size="small" onClick={() => setTepQuyetDinh(null)}>
                    Bỏ tệp
                  </Button>
                </Space>
              ) : (
                <Typography.Text type="secondary">Chưa đính kèm tệp.</Typography.Text>
              )}
            </Space>
          </FormAntd.Item>

          <Space size="large" wrap>
            <Truong<GiaTriHoiDong> ten="thoiGianHoatDongTu" label="Hoạt động từ">
              {(o) => (
                <DatePicker
                  value={(o.value as Dayjs | null | undefined) ?? null}
                  onChange={o.onChange}
                  onBlur={o.onBlur}
                  status={o.status}
                  format="DD/MM/YYYY"
                  style={{ width: 170 }}
                />
              )}
            </Truong>
            <Truong<GiaTriHoiDong> ten="thoiGianHoatDongDen" label="Đến">
              {(o) => (
                <DatePicker
                  value={(o.value as Dayjs | null | undefined) ?? null}
                  onChange={o.onChange}
                  onBlur={o.onBlur}
                  status={o.status}
                  format="DD/MM/YYYY"
                  style={{ width: 170 }}
                />
              )}
            </Truong>
          </Space>

          <Space size="large" wrap>
            <Truong<GiaTriHoiDong>
              ten="soThanhVienToiThieu"
              label="Số thành viên tối thiểu"
              tooltip="Lưu danh sách ít hơn số này sẽ bị máy chủ từ chối."
            >
              {(o) => (
                <InputNumber
                  {...o}
                  value={o.value as number}
                  min={1}
                  max={99}
                  style={{ width: 180 }}
                />
              )}
            </Truong>
            <Truong<GiaTriHoiDong> ten="tyLeThongQua" label="Tỷ lệ thông qua (%)">
              {(o) => (
                <InputNumber
                  {...o}
                  value={o.value as number}
                  min={1}
                  max={100}
                  style={{ width: 160 }}
                />
              )}
            </Truong>
            <Truong<GiaTriHoiDong> ten="trangThaiHoatDong" label="Tình trạng">
              {(o) => (
                <Select
                  {...o}
                  value={o.value as string}
                  style={{ width: 180 }}
                  options={[
                    { value: 'DANG_HOAT_DONG', label: 'Đang hoạt động' },
                    { value: 'DA_KET_THUC', label: 'Đã kết thúc' },
                  ]}
                />
              )}
            </Truong>
          </Space>

          <Truong<GiaTriHoiDong> ten="moTa" label="Mô tả">
            {(o) => <Input.TextArea {...o} value={o.value as string} rows={2} />}
          </Truong>

          <Space size="large">
            <Truong<GiaTriHoiDong> ten="thuTu" label="Thứ tự">
              {(o) => <InputNumber {...o} value={o.value as number} min={0} />}
            </Truong>
            <Truong<GiaTriHoiDong> ten="trangThai" label="Trạng thái">
              {(o) => (
                <Select
                  {...o}
                  value={o.value as number}
                  style={{ width: 180 }}
                  options={[
                    { value: 1, label: 'Hoạt động' },
                    { value: 0, label: 'Ngừng hoạt động' },
                  ]}
                />
              )}
            </Truong>
          </Space>
        </BieuMau>
      </Modal>
    </Card>
  );
}
