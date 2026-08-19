import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  App,
  Alert,
  Button,
  Card,
  Col,
  DatePicker,
  Descriptions,
  Input,
  InputNumber,
  Row,
  Select,
  Space,
  Statistic,
  Steps,
  Table,
  Tag,
  Typography,
  Upload,
} from 'antd';
import {
  CheckCircleOutlined,
  CloseCircleOutlined,
  DeleteOutlined,
  ExclamationCircleOutlined,
  PlusOutlined,
  SaveOutlined,
  SendOutlined,
  UploadOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';
import type { Dayjs } from 'dayjs';
import { z } from 'zod';

import { boNhoToken, http, LoiApi, xoaDuLieu } from '@/api/client';
import {
  NGUONG_TAI_THEO_MANH,
  taiTepLenTheoManh,
  apiDoiTuong,
  apiDotDeNghi,
  apiLinhVuc,
  apiLoaiTacGia,
  apiSangKien,
  type NoiDungHoSo,
  type TacGia,
  type ThanhPhanHoSo,
} from '@/api/endpoints';
import { KhoiDangTai } from '@/components/ThanhPhanChung';
import { ONoiDungDai } from '@/components/ONoiDungDai';
import { BieuMau, Truong, useBieuMau } from '@/components/bieu-mau/BieuMau';
import { batBuoc, tuyChon } from '@/components/bieu-mau/luat';

/** Khoảng thời gian tự động lưu nháp (Mục 5 — chức năng 22). */
const CHU_KY_TU_LUU_MS = 30_000;

const BUOC = [
  { title: 'Đợt đề nghị' },
  { title: 'Thông tin chung' },
  { title: 'Tác giả' },
  { title: 'Nội dung' },
  { title: 'Tệp đính kèm' },
  { title: 'Xem lại & Nộp' },
];

/**
 * Luật kiểm tra hồ sơ sáng kiến.
 *
 * Các ô nội dung dài (mô tả giải pháp, tính mới, hiệu quả…) là *thành phần hồ sơ* do quản trị cấu
 * hình: bắt buộc hay không, tối thiểu bao nhiêu ký tự đều đọc từ máy chủ. Vì vậy luật nhận một hàm
 * lấy cấu hình thành phần hiện tại thay vì khai cứng — form giữ nguyên một resolver suốt vòng đời
 * mà vẫn áp đúng cấu hình của đợt đang nộp.
 */
function taoLuatHoSo(layThanhPhan: () => ThanhPhanHoSo[]) {
  return z
    .object({
      dotDeNghiId: z.string().uuid('Vui lòng chọn đợt đề nghị.'),
      tenSangKien: batBuoc('Tên sáng kiến', 1000),
      linhVucId: z.string().uuid('Vui lòng chọn lĩnh vực.'),
      doiTuongId: z.string().uuid().nullish(),
      loaiTacGiaId: z.string().uuid().nullish(),
      moTaGiaiPhap: tuyChon(20000),
      tinhTrangTruocKhiApDung: tuyChon(20000),
      noiDungGiaiPhap: tuyChon(20000),
      tinhMoi: tuyChon(20000),
      khaNangApDung: tuyChon(20000),
      phamViApDung: tuyChon(20000),
      hieuQuaKinhTe: tuyChon(20000),
      hieuQuaXaHoi: tuyChon(20000),
      giaTriLamLoiUocTinh: z
        .number({ invalid_type_error: 'Giá trị làm lợi phải là một số.' })
        .min(0, 'Giá trị làm lợi không được âm.')
        .nullish(),
      thoiGianApDungTu: z.custom<Dayjs>((v) => v == null || dayjs.isDayjs(v)).nullish(),
      thoiGianApDungDen: z.custom<Dayjs>((v) => v == null || dayjs.isDayjs(v)).nullish(),
    })
    .superRefine((giaTri, ctx) => {
      const tu = giaTri.thoiGianApDungTu;
      const den = giaTri.thoiGianApDungDen;

      if (tu && den && den.isBefore(tu, 'day')) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          path: ['thoiGianApDungDen'],
          message: 'Ngày kết thúc áp dụng phải sau ngày bắt đầu.',
        });
      }

      for (const tp of layThanhPhan()) {
        if (!tp.batBuoc || tp.loaiDuLieu === 'TEP') continue;

        const truong = tenTruongTheoMa(tp.ma);
        const noiDung = String(giaTri[truong as keyof typeof giaTri] ?? '').trim();

        if (!noiDung) {
          ctx.addIssue({
            code: z.ZodIssueCode.custom,
            path: [truong],
            message: `Vui lòng nhập ${tp.ten.toLowerCase()}.`,
          });
        } else if (noiDung.length < tp.soKyTuToiThieu) {
          ctx.addIssue({
            code: z.ZodIssueCode.custom,
            path: [truong],
            message: `Cần tối thiểu ${tp.soKyTuToiThieu} ký tự, hiện có ${noiDung.length}.`,
          });
        }
      }
    });
}

type GiaTriHoSo = z.infer<ReturnType<typeof taoLuatHoSo>>;

const MAC_DINH_HO_SO: GiaTriHoSo = {
  dotDeNghiId: '',
  tenSangKien: '',
  linhVucId: '',
};

export default function TrangNopHoSo() {
  const { id } = useParams<{ id: string }>();
  const dieuHuong = useNavigate();
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [buocHienTai, setBuocHienTai] = useState(0);
  const [hoSoId, setHoSoId] = useState<string | undefined>(id);
  /*
   * Cấu hình thành phần hồ sơ về sau lần tải đầu, nên luật đọc qua ref: form dựng một lần với
   * một resolver, ref cập nhật sau vẫn khiến luật thấy đúng cấu hình của đợt đang nộp.
   */
  const refThanhPhan = useRef<ThanhPhanHoSo[]>([]);
  const form = useBieuMau<GiaTriHoSo>(
    useMemo(() => taoLuatHoSo(() => refThanhPhan.current), []),
    MAC_DINH_HO_SO as never,
  );
  const [tacGia, setTacGia] = useState<TacGia[]>([
    { hoTen: '', tyLeDongGop: 100, laTacGiaChinh: true },
  ]);

  const hasThayDoi = useRef(false);

  const { data: cacDot } = useQuery({ queryKey: ['dot-dang-mo'], queryFn: apiDotDeNghi.dangMo });
  const { data: cacLinhVuc } = useQuery({ queryKey: ['linh-vuc-chon'], queryFn: apiLinhVuc.chon });
  const { data: cacDoiTuong } = useQuery({ queryKey: ['doi-tuong-chon'], queryFn: apiDoiTuong.chon });
  const { data: cacLoaiTacGia } = useQuery({
    queryKey: ['loai-tac-gia-chon'],
    queryFn: apiLoaiTacGia.chon,
  });

  const { data: chiTiet, isLoading } = useQuery({
    queryKey: ['sang-kien', hoSoId],
    queryFn: () => apiSangKien.chiTiet(hoSoId!),
    enabled: !!hoSoId,
  });

  refThanhPhan.current = chiTiet?.thanhPhanHoSo ?? [];

  // Nạp dữ liệu vào form khi mở hồ sơ đã có.
  useEffect(() => {
    if (!chiTiet) return;

    form.reset({
      tenSangKien: chiTiet.tenSangKien,
      dotDeNghiId: chiTiet.dotDeNghiId,
      linhVucId: chiTiet.linhVucId,
      doiTuongId: chiTiet.doiTuongId,
      loaiTacGiaId: chiTiet.loaiTacGiaId,
      moTaGiaiPhap: chiTiet.moTaGiaiPhap,
      tinhTrangTruocKhiApDung: chiTiet.tinhTrangTruocKhiApDung,
      noiDungGiaiPhap: chiTiet.noiDungGiaiPhap,
      tinhMoi: chiTiet.tinhMoi,
      khaNangApDung: chiTiet.khaNangApDung,
      phamViApDung: chiTiet.phamViApDung,
      hieuQuaKinhTe: chiTiet.hieuQuaKinhTe,
      giaTriLamLoiUocTinh: chiTiet.giaTriLamLoiUocTinh,
      hieuQuaXaHoi: chiTiet.hieuQuaXaHoi,
      // Trước đây hai mốc thời gian không được nạp lại: mở nháp lên là chúng biến mất, người nộp
      // tưởng mình chưa điền rồi điền lại từ đầu.
      thoiGianApDungTu: chiTiet.thoiGianApDungTu ? dayjs(chiTiet.thoiGianApDungTu) : null,
      thoiGianApDungDen: chiTiet.thoiGianApDungDen ? dayjs(chiTiet.thoiGianApDungDen) : null,
    } as GiaTriHoSo);

    if (chiTiet.danhSachTacGia.length > 0) {
      setTacGia(chiTiet.danhSachTacGia);
    }
  }, [chiTiet, form]);

  const luuNhap = useMutation({
    mutationFn: async () => {
      const { thoiGianApDungTu, thoiGianApDungDen, ...conLai } = form.getValues();
      const duLieu: NoiDungHoSo = {
        ...conLai,
        /*
         * Ô chọn chưa điền giữ chuỗi rỗng ở phía giao diện, nhưng máy chủ khai các trường này là
         * Guid: gửi "" thì mô hình không dựng được và cả yêu cầu bị từ chối 400 trước khi tới
         * nghiệp vụ — bỏ hẳn khoá đi thì lưu nháp dở dang vẫn chạy.
         */
        dotDeNghiId: conLai.dotDeNghiId || (undefined as unknown as string),
        linhVucId: conLai.linhVucId || (undefined as unknown as string),
        doiTuongId: conLai.doiTuongId || null,
        loaiTacGiaId: conLai.loaiTacGiaId || null,
        // Máy chủ nhận chuỗi ISO; giữ nguyên đối tượng Dayjs thì đúng nhờ may mắn của JSON.
        thoiGianApDungTu: thoiGianApDungTu ? thoiGianApDungTu.toISOString() : null,
        thoiGianApDungDen: thoiGianApDungDen ? thoiGianApDungDen.toISOString() : null,
        danhSachTacGia: tacGia,
      };

      if (hoSoId) {
        await apiSangKien.capNhat(hoSoId, duLieu, chiTiet?.phienBan);
        return hoSoId;
      }

      const idMoi = await apiSangKien.tao(duLieu);
      setHoSoId(idMoi);
      return idMoi;
    },
    onSuccess: () => {
      hasThayDoi.current = false;
      void queryClient.invalidateQueries({ queryKey: ['sang-kien'] });
    },
    // Không báo lỗi thì bấm "Tiếp theo" mà nháp lưu hỏng sẽ đứng im không rõ nguyên do.
    onError: (loi) =>
      message.error(loi instanceof LoiApi ? loi.message : 'Không lưu được bản nháp.'),
  });

  const nopHoSo = useMutation({
    mutationFn: () => apiSangKien.nop(hoSoId!),
    onSuccess: (ketQua) => {
      modal.success({
        title: 'Nộp hồ sơ thành công',
        content: (
          <div>
            <p>
              Mã hồ sơ: <strong>{ketQua.maHoSo}</strong>
            </p>
            <p>Hồ sơ đã chuyển sang bước: {ketQua.tenBuocHienTai}</p>
          </div>
        ),
        onOk: () => dieuHuong(`/sang-kien/${hoSoId}`),
      });
    },
    onError: (loi) => {
      message.error(loi instanceof LoiApi ? loi.message : 'Không nộp được hồ sơ.');
    },
  });

  const refLuuNhap = useRef(luuNhap.mutate);
  refLuuNhap.current = luuNhap.mutate;

  useEffect(() => {
    const dinhKy = setInterval(() => {
      if (hasThayDoi.current && duDeLuuNhap()) {
        refLuuNhap.current();
      }
    }, CHU_KY_TU_LUU_MS);

    return () => clearInterval(dinhKy);
  }, []);

  const tongTyLe = useMemo(
    () => tacGia.reduce((tong, t) => tong + (Number(t.tyLeDongGop) || 0), 0),
    [tacGia],
  );

  const loaiTacGiaDaChon = form.watch('loaiTacGiaId');
  const soTacGiaToiDa = useMemo(() => {
    const loai = cacLoaiTacGia?.find((l) => l.id === loaiTacGiaDaChon);
    // Mã danh mục cho biết có cho nhiều tác giả hay không.
    return loai?.ma === 'CA_NHAN' ? 1 : loai?.ma === 'NHOM_TAC_GIA' ? 5 : 10;
  }, [cacLoaiTacGia, loaiTacGiaDaChon]);

  // Đánh dấu có sửa để vòng tự lưu nháp biết mà chạy.
  useEffect(() => {
    const dangKy = form.watch(() => {
      hasThayDoi.current = true;
    });

    return () => dangKy.unsubscribe();
  }, [form]);

  const thanhPhanThieu = (chiTiet?.thanhPhanHoSo ?? []).filter(
    (t) => t.batBuoc && t.trangThai !== 'DU',
  );

  /** Bản nháp chỉ ghi xuống được khi đã có tên sáng kiến và lĩnh vực — máy chủ bắt buộc cả hai. */
  function duDeLuuNhap() {
    const giaTri = form.getValues();
    return !!giaTri.tenSangKien?.trim() && !!giaTri.linhVucId;
  }

  async function sangBuoc(buocMoi: number) {
    // Lưu nháp mỗi khi rời bước để không mất dữ liệu.
    if (buocMoi > buocHienTai) {
      const canKiemTra = truongCuaBuoc(buocHienTai);

      if (canKiemTra.length > 0 && !(await form.trigger(canKiemTra))) return;

      if (buocHienTai === 2 && Math.abs(tongTyLe - 100) > 0.01) {
        message.error(`Tổng tỷ lệ đóng góp phải bằng 100%, hiện tại là ${tongTyLe}%.`);
        return;
      }

      /*
       * Chỉ lưu nháp khi đã có đủ tên sáng kiến và lĩnh vực. Máy chủ bắt buộc hai trường này ngay
       * cả với bản nháp, nhưng người nộp mới điền chúng ở bước 2 — gọi lưu ở bước 1 thì luôn bị
       * 422 và người dùng kẹt lại ngay bước đầu tiên, không cách nào đi tiếp.
       */
      if (duDeLuuNhap()) {
        try {
          await luuNhap.mutateAsync();
        } catch {
          // Thông báo đã hiện ở onError; giữ nguyên bước để người dùng sửa rồi thử lại.
          return;
        }
      }
    }

    setBuocHienTai(buocMoi);
  }

  if (id && isLoading) return <KhoiDangTai />;

  return (
    <Card
      title={hoSoId ? `Chỉnh sửa hồ sơ ${chiTiet?.maHoSo ?? ''}` : 'Nộp sáng kiến mới'}
      extra={
        <Space>
          <Button
            icon={<SaveOutlined />}
            loading={luuNhap.isPending}
            onClick={() => {
              if (!duDeLuuNhap()) {
                message.warning('Cần có tên sáng kiến và lĩnh vực trước khi lưu nháp.');
                return;
              }

              luuNhap.mutate();
            }}
          >
            Lưu nháp
          </Button>
        </Space>
      }
    >
      <Steps
        current={buocHienTai}
        items={BUOC}
        size="small"
        onChange={(b) => void sangBuoc(b)}
        style={{ marginBottom: 24 }}
        responsive
      />

      <BieuMau form={form} onGui={() => luuNhap.mutateAsync()}>
        {/* Bước 1 — Đợt đề nghị */}
        <div style={{ display: buocHienTai === 0 ? 'block' : 'none' }}>
          <Alert
            type="info"
            showIcon
            style={{ marginBottom: 16 }}
            message="Chỉ các đợt đang mở và còn hạn nộp mới hiển thị ở đây."
          />
          <Truong<GiaTriHoSo> ten="dotDeNghiId" label="Đợt đề nghị" required>
            {(o) => (
              <Select
                {...o}
                value={(o.value as string) || undefined}
                placeholder="Chọn đợt đề nghị"
                options={(cacDot ?? []).map((d) => ({ value: d.id, label: d.ten }))}
                showSearch
                optionFilterProp="label"
              />
            )}
          </Truong>
        </div>

        {/* Bước 2 — Thông tin chung */}
        <div style={{ display: buocHienTai === 1 ? 'block' : 'none' }}>
          <Truong<GiaTriHoSo> ten="tenSangKien" label="Tên sáng kiến" required>
            {(o) => (
              <Input.TextArea
                {...o}
                value={o.value as string}
                rows={2}
                showCount
                maxLength={1000}
              />
            )}
          </Truong>

          <Row gutter={12}>
            <Col xs={24} md={8}>
              <Truong<GiaTriHoSo> ten="linhVucId" label="Lĩnh vực" required>
                {(o) => (
                  <Select
                    {...o}
                    value={(o.value as string) || undefined}
                    options={(cacLinhVuc ?? []).map((x) => ({ value: x.id, label: x.ten }))}
                    showSearch
                    optionFilterProp="label"
                  />
                )}
              </Truong>
            </Col>
            <Col xs={24} md={8}>
              <Truong<GiaTriHoSo> ten="doiTuongId" label="Đối tượng áp dụng">
                {(o) => (
                  <Select
                    {...o}
                    value={(o.value as string | null) ?? undefined}
                    allowClear
                    options={(cacDoiTuong ?? []).map((x) => ({ value: x.id, label: x.ten }))}
                  />
                )}
              </Truong>
            </Col>
            <Col xs={24} md={8}>
              <Truong<GiaTriHoSo> ten="loaiTacGiaId" label="Loại tác giả">
                {(o) => (
                  <Select
                    {...o}
                    value={(o.value as string | null) ?? undefined}
                    allowClear
                    options={(cacLoaiTacGia ?? []).map((x) => ({ value: x.id, label: x.ten }))}
                  />
                )}
              </Truong>
            </Col>
          </Row>

          <Row gutter={12}>
            <Col xs={24} md={12}>
              <Truong<GiaTriHoSo> ten="thoiGianApDungTu" label="Áp dụng từ ngày">
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
            </Col>
            <Col xs={24} md={12}>
              <Truong<GiaTriHoSo> ten="thoiGianApDungDen" label="Áp dụng đến ngày">
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
            </Col>
          </Row>
        </div>

        {/* Bước 3 — Tác giả */}
        <div style={{ display: buocHienTai === 2 ? 'block' : 'none' }}>
          <Alert
            type={Math.abs(tongTyLe - 100) < 0.01 ? 'success' : 'warning'}
            showIcon
            style={{ marginBottom: 12 }}
            message={`Tổng tỷ lệ đóng góp: ${tongTyLe}% ${
              Math.abs(tongTyLe - 100) < 0.01 ? '(hợp lệ)' : '— phải bằng 100%'
            }`}
            description={`Số tác giả tối đa cho loại tác giả đã chọn: ${soTacGiaToiDa}.`}
          />

          <Table<TacGia>
            rowKey={(_, i) => String(i)}
            size="small"
            pagination={false}
            dataSource={tacGia}
            columns={[
              {
                title: 'Họ và tên',
                dataIndex: 'hoTen',
                render: (giaTri: string, _dong, i) => (
                  <Input
                    value={giaTri}
                    onChange={(e) => capNhatTacGia(i, { hoTen: e.target.value })}
                    placeholder="Nguyễn Văn A"
                  />
                ),
              },
              {
                title: 'Chức vụ',
                dataIndex: 'chucVu',
                width: 180,
                render: (giaTri: string, _dong, i) => (
                  <Input
                    value={giaTri ?? ''}
                    onChange={(e) => capNhatTacGia(i, { chucVu: e.target.value })}
                  />
                ),
              },
              {
                title: 'Đơn vị công tác',
                dataIndex: 'donViCongTac',
                width: 200,
                render: (giaTri: string, _dong, i) => (
                  <Input
                    value={giaTri ?? ''}
                    onChange={(e) => capNhatTacGia(i, { donViCongTac: e.target.value })}
                  />
                ),
              },
              {
                title: 'Tỷ lệ (%)',
                dataIndex: 'tyLeDongGop',
                width: 110,
                render: (giaTri: number, _dong, i) => (
                  <InputNumber
                    min={0}
                    max={100}
                    value={giaTri}
                    style={{ width: '100%' }}
                    onChange={(v) => capNhatTacGia(i, { tyLeDongGop: Number(v) || 0 })}
                  />
                ),
              },
              {
                title: 'Chính',
                dataIndex: 'laTacGiaChinh',
                width: 80,
                align: 'center',
                render: (giaTri: boolean, _dong, i) => (
                  <Button
                    type={giaTri ? 'primary' : 'default'}
                    size="small"
                    shape="circle"
                    icon={<CheckCircleOutlined />}
                    onClick={() =>
                      setTacGia((cu) =>
                        cu.map((t, j) => ({ ...t, laTacGiaChinh: j === i })),
                      )
                    }
                  />
                ),
              },
              {
                title: '',
                width: 50,
                render: (_giaTri, _dong, i) => (
                  <Button
                    danger
                    type="text"
                    icon={<DeleteOutlined />}
                    disabled={tacGia.length === 1}
                    onClick={() => setTacGia((cu) => cu.filter((_, j) => j !== i))}
                  />
                ),
              },
            ]}
          />

          <Button
            type="dashed"
            block
            icon={<PlusOutlined />}
            style={{ marginTop: 12 }}
            disabled={tacGia.length >= soTacGiaToiDa}
            onClick={() =>
              setTacGia((cu) => [...cu, { hoTen: '', tyLeDongGop: 0, laTacGiaChinh: false }])
            }
          >
            Thêm đồng tác giả
          </Button>
        </div>

        {/* Bước 4 — Nội dung */}
        <div style={{ display: buocHienTai === 3 ? 'block' : 'none' }}>
          {(chiTiet?.thanhPhanHoSo ?? [])
            .filter((t) => t.loaiDuLieu !== 'TEP')
            .map((tp) => (
              <Truong<GiaTriHoSo>
                key={tp.ma}
                ten={tenTruongTheoMa(tp.ma) as never}
                required={tp.batBuoc}
                label={
                  <Space>
                    {tp.ten}
                    {tp.batBuoc && <Tag color="red">Bắt buộc</Tag>}
                    {tp.soKyTuToiThieu > 0 && (
                      <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                        tối thiểu {tp.soKyTuToiThieu} ký tự
                      </Typography.Text>
                    )}
                  </Space>
                }
                extra={tp.moTaHuongDan}
              >
                {(o) => (
                  <ONoiDungDai
                    value={(o.value as string) ?? ''}
                    onChange={o.onChange}
                    rows={6}
                    soKyTuToiThieu={tp.soKyTuToiThieu}
                    placeholder={tp.moTaHuongDan ?? undefined}
                  />
                )}
              </Truong>
            ))}

          <Truong<GiaTriHoSo> ten="giaTriLamLoiUocTinh" label="Giá trị làm lợi ước tính (VNĐ)">
            {(o) => (
              <InputNumber<number>
                {...o}
                value={(o.value as number | null) ?? null}
                style={{ width: '100%' }}
                min={0}
                formatter={(v) => `${v}`.replace(/\B(?=(\d{3})+(?!\d))/g, '.')}
                parser={(v) => Number(v?.replace(/\./g, '') ?? 0)}
              />
            )}
          </Truong>
        </div>

        {/* Bước 5 — Tệp đính kèm */}
        <div style={{ display: buocHienTai === 4 ? 'block' : 'none' }}>
          {!hoSoId ? (
            <Alert type="warning" showIcon message="Vui lòng lưu nháp trước khi tải tệp lên." />
          ) : (
            (chiTiet?.thanhPhanHoSo ?? [])
              .filter((t) => t.loaiDuLieu !== 'VAN_BAN')
              .map((tp) => (
                <Card key={tp.ma} size="small" style={{ marginBottom: 12 }}
                  title={
                    <Space>
                      {tp.ten}
                      {tp.batBuoc && <Tag color="red">Bắt buộc</Tag>}
                      <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                        tối đa {tp.soLuongToiDa} tệp, mỗi tệp ≤ {tp.dungLuongToiDaMb}MB
                      </Typography.Text>
                    </Space>
                  }
                >
                  <Upload
                    action="/api/v1/tep-tin/tai-len"
                    headers={{ Authorization: `Bearer ${boNhoToken.layAccessToken()}` }}
                    data={{ sangKienId: hoSoId, thanhPhanHoSoMa: tp.ma }}
                    name="tep"
                    accept={tp.dinhDangChoPhep.join(',')}
                    /*
                     * Tệp lớn đi đường tải theo mảnh: gửi cả tệp 80MB trong một yêu cầu mà rớt
                     * mạng giữa chừng là mất trắng, người nộp phải chọn lại từ đầu.
                     */
                    customRequest={(tuyChon) => {
                      const tep = tuyChon.file as File;

                      if (tep.size <= NGUONG_TAI_THEO_MANH) {
                        // Tệp nhỏ: để antd gửi một lần như cũ, nhanh hơn chia mảnh.
                        const form = new FormData();
                        form.append('tep', tep);
                        form.append('sangKienId', hoSoId ?? '');
                        form.append('thanhPhanHoSoMa', tp.ma);

                        http
                          .post('/api/v1/tep-tin/tai-len', form, {
                            headers: { 'Content-Type': 'multipart/form-data' },
                            onUploadProgress: (su) =>
                              tuyChon.onProgress?.({
                                percent: su.total ? (su.loaded / su.total) * 100 : 0,
                              }),
                          })
                          .then((kq) => tuyChon.onSuccess?.(kq.data))
                          .catch((loi: unknown) => tuyChon.onError?.(loi as Error));

                        return;
                      }

                      taiTepLenTheoManh(
                        tep,
                        { sangKienId: hoSoId, thanhPhanHoSoMa: tp.ma },
                        (phanTram) => tuyChon.onProgress?.({ percent: phanTram }),
                      )
                        .then((daTaiLen) => tuyChon.onSuccess?.(daTaiLen))
                        .catch((loi: unknown) => tuyChon.onError?.(loi as Error));
                    }}
                    defaultFileList={(chiTiet?.tepDinhKem ?? [])
                      .filter((t) => t.thanhPhanHoSoMa === tp.ma)
                      .map((t) => ({
                        uid: t.id,
                        name: t.tenGoc,
                        status: 'done' as const,
                        url: `/api/v1/tep-tin/${t.tepTinId}/tai-ve`,
                      }))}
                    onChange={(thongTin) => {
                      if (thongTin.file.status === 'done') {
                        message.success(`Đã tải lên ${thongTin.file.name}`);
                        void queryClient.invalidateQueries({ queryKey: ['sang-kien', hoSoId] });
                      } else if (thongTin.file.status === 'error') {
                        const phanHoi = thongTin.file.response as { thongBao?: string } | undefined;
                        message.error(phanHoi?.thongBao ?? `Tải lên ${thongTin.file.name} thất bại`);
                      }
                    }}
                    onRemove={async (tep) => {
                      const dinhKem = (chiTiet?.tepDinhKem ?? []).find((t) => t.id === tep.uid);
                      if (dinhKem) {
                        await xoaDuLieu(`/api/v1/tep-tin/dinh-kem/${dinhKem.id}`);
                        void queryClient.invalidateQueries({ queryKey: ['sang-kien', hoSoId] });
                      }
                      return true;
                    }}
                  >
                    <Button icon={<UploadOutlined />}>Chọn tệp</Button>
                  </Upload>
                </Card>
              ))
          )}
        </div>

        {/* Bước 6 — Xem lại & Nộp */}
        <div style={{ display: buocHienTai === 5 ? 'block' : 'none' }}>
          <Row gutter={12}>
            <Col xs={24} lg={14}>
              <Descriptions bordered size="small" column={1} title="Thông tin hồ sơ">
                <Descriptions.Item label="Tên sáng kiến">
                  {form.watch('tenSangKien')}
                </Descriptions.Item>
                <Descriptions.Item label="Đợt đề nghị">
                  {cacDot?.find((d) => d.id === form.watch('dotDeNghiId'))?.ten}
                </Descriptions.Item>
                <Descriptions.Item label="Lĩnh vực">
                  {cacLinhVuc?.find((d) => d.id === form.watch('linhVucId'))?.ten}
                </Descriptions.Item>
                <Descriptions.Item label="Tác giả">
                  {tacGia.map((t) => `${t.hoTen} (${t.tyLeDongGop}%)`).join(', ')}
                </Descriptions.Item>
                <Descriptions.Item label="Số tệp đính kèm">
                  {chiTiet?.tepDinhKem.length ?? 0}
                </Descriptions.Item>
              </Descriptions>
            </Col>

            <Col xs={24} lg={10}>
              <Card size="small" title="Kiểm tra thành phần hồ sơ">
                {(chiTiet?.thanhPhanHoSo ?? []).map((tp) => (
                  <div key={tp.ma} style={{ padding: '4px 0' }}>
                    {tp.trangThai === 'DU' ? (
                      <CheckCircleOutlined style={{ color: '#52c41a' }} />
                    ) : tp.trangThai === 'KHONG_BAT_BUOC' ? (
                      <ExclamationCircleOutlined style={{ color: '#d9d9d9' }} />
                    ) : (
                      <CloseCircleOutlined style={{ color: '#ff4d4f' }} />
                    )}
                    <span style={{ marginLeft: 8 }}>{tp.ten}</span>
                    {tp.canhBao && (
                      <div style={{ marginLeft: 24, fontSize: 12, color: '#ff4d4f' }}>
                        {tp.canhBao}
                      </div>
                    )}
                  </div>
                ))}

                <Statistic
                  style={{ marginTop: 12 }}
                  title="Thành phần bắt buộc còn thiếu"
                  value={thanhPhanThieu.length}
                  valueStyle={{ color: thanhPhanThieu.length === 0 ? '#52c41a' : '#ff4d4f' }}
                />
              </Card>
            </Col>
          </Row>

          <Button
            type="primary"
            size="large"
            block
            icon={<SendOutlined />}
            style={{ marginTop: 16 }}
            loading={nopHoSo.isPending}
            disabled={!hoSoId || thanhPhanThieu.length > 0}
            onClick={() =>
              modal.confirm({
                title: 'Xác nhận nộp hồ sơ',
                content:
                  'Sau khi nộp, hồ sơ sẽ vào quy trình xử lý và bạn chỉ sửa được khi có yêu cầu bổ sung.',
                okText: 'Nộp hồ sơ',
                cancelText: 'Hủy',
                onOk: () => nopHoSo.mutate(),
              })
            }
          >
            Nộp hồ sơ
          </Button>
        </div>
      </BieuMau>

      <Space style={{ marginTop: 24, width: '100%', justifyContent: 'space-between' }}>
        <Button disabled={buocHienTai === 0} onClick={() => setBuocHienTai((b) => b - 1)}>
          Quay lại
        </Button>
        {buocHienTai < BUOC.length - 1 && (
          <Button type="primary" onClick={() => void sangBuoc(buocHienTai + 1)}>
            Tiếp theo
          </Button>
        )}
      </Space>
    </Card>
  );

  function capNhatTacGia(chiSo: number, thayDoi: Partial<TacGia>) {
    setTacGia((cu) => cu.map((t, i) => (i === chiSo ? { ...t, ...thayDoi } : t)));
    hasThayDoi.current = true;
  }
}

/** Ánh xạ mã thành phần hồ sơ sang tên trường trên form. */
function tenTruongTheoMa(ma: string): keyof NoiDungHoSo {
  const bang: Record<string, keyof NoiDungHoSo> = {
    MO_TA_GIAI_PHAP: 'moTaGiaiPhap',
    TINH_TRANG_TRUOC: 'tinhTrangTruocKhiApDung',
    NOI_DUNG_GIAI_PHAP: 'noiDungGiaiPhap',
    TINH_MOI: 'tinhMoi',
    KHA_NANG_AP_DUNG: 'khaNangApDung',
    PHAM_VI_AP_DUNG: 'phamViApDung',
    HIEU_QUA_KINH_TE: 'hieuQuaKinhTe',
    HIEU_QUA_XA_HOI: 'hieuQuaXaHoi',
  };

  const truong = bang[ma];
  if (!truong && process.env.NODE_ENV !== 'production') {
    console.warn(`[TrangNopHoSo] Mã thành phần "${ma}" chưa có ánh xạ sang trường form.`);
  }
  return truong ?? 'moTaGiaiPhap';
}

/** Các trường cần kiểm tra khi rời từng bước. */
function truongCuaBuoc(buoc: number): (keyof GiaTriHoSo)[] {
  switch (buoc) {
    case 0:
      return ['dotDeNghiId'];
    case 1:
      return ['tenSangKien', 'linhVucId'];
    default:
      return [];
  }
}

export { dayjs };
