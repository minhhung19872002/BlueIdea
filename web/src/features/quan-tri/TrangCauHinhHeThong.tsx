import { useEffect, useMemo, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Alert, App, Button, Card, ColorPicker, Input, InputNumber, Space, Switch } from 'antd';
import { SaveOutlined } from '@ant-design/icons';
import { useMutation, useQuery } from '@tanstack/react-query';
import { z } from 'zod';

import { LoiApi } from '@/api/client';
import { apiHeThong, type CauHinhMuc } from '@/api/endpoints';
import { KhoiDangTai, KhoiKhongNhanRa, KhoiLoi } from '@/components/ThanhPhanChung';
import { DaiTabTrang } from '@/components/DaiTabTrang';
import { BieuMau, Truong, useBieuMau } from '@/components/bieu-mau/BieuMau';
import { ONhapAnhCauHinh } from '@/components/ONhapAnhCauHinh';
import { DS_TAB_CAU_HINH } from './cauHinhTab';

const NHOM_THEO_DUONG_DAN: Record<string, { nhom?: string; tieuDe: string }> = {
  'he-thong': { nhom: undefined, tieuDe: 'Cấu hình hệ thống' },
  'sang-kien': { nhom: 'HO_SO', tieuDe: 'Cấu hình thông tin sáng kiến' },
  'email-sms': { nhom: 'EMAIL_SMS', tieuDe: 'Cấu hình email & SMS' },
  'chu-ky-so': { nhom: 'CHU_KY_SO', tieuDe: 'Cấu hình chữ ký số' },
  'tich-hop': { nhom: 'TICH_HOP', tieuDe: 'Cấu hình tích hợp hệ thống' },
  menu: { nhom: 'MENU', tieuDe: 'Cấu hình menu' },
};

/**
 * Luật kiểm tra cấu hình hệ thống.
 *
 * Danh sách trường do máy chủ trả về nên không dựng được schema tĩnh. Thay vào đó nhận một hàm
 * đọc danh mục mục cấu hình hiện tại, để `useForm` giữ nguyên đúng một resolver suốt vòng đời.
 *
 * Các luật ở đây bắt những sai sót mà máy chủ không bắt được và cũng không báo lỗi — chỉ âm thầm
 * cho ra kết quả sai cho tới khi có người phát hiện:
 *   - hai hệ số trùng lặp phải cộng lại bằng 1, vì điểm trùng lặp là tổng có trọng số
 *     `heSoTuVung * tuVung + heSoNguNghia * nguNghia` không hề chuẩn hoá lại. Đặt 0,5 và 0,9 thì
 *     hai hồ sơ giống hệt nhau sẽ báo trùng 140%;
 *   - ngưỡng cảnh báo vàng phải thấp hơn ngưỡng đỏ, nếu không thang cảnh báo bị lộn ngược;
 *   - mẫu mã hồ sơ phải có chỗ chèn số thứ tự, nếu không mọi hồ sơ nhận cùng một mã.
 */
const KHOA = {
  emailHoTro: 'EMAIL_HO_TRO',
  dienThoaiHoTro: 'DIEN_THOAI_HO_TRO',
  mauMaHoSo: 'MAU_MA_HO_SO',
  trungLapVang: 'MUC_CANH_BAO_TRUNG_LAP_VANG',
  trungLapDo: 'MUC_CANH_BAO_TRUNG_LAP_DO',
  heSoTuVung: 'HE_SO_TU_VUNG',
  heSoNguNghia: 'HE_SO_NGU_NGHIA',
} as const;

function taoLuatCauHinh(layDsMuc: () => CauHinhMuc[]) {
  return z.record(z.string(), z.unknown()).superRefine((giaTri, ctx) => {
    const loi = (khoa: string, message: string) =>
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: [khoa], message });

    const co = (khoa: string) => Object.prototype.hasOwnProperty.call(giaTri, khoa);
    const chuoi = (khoa: string) =>
      typeof giaTri[khoa] === 'string' ? (giaTri[khoa] as string).trim() : '';
    const so = (khoa: string) => Number(giaTri[khoa]);

    for (const m of layDsMuc()) {
      if (m.kieuDuLieu !== 'NUMBER' || !co(m.khoa)) continue;

      if (giaTri[m.khoa] === null || giaTri[m.khoa] === '' || Number.isNaN(so(m.khoa))) {
        loi(m.khoa, 'Phải là một số.');
      } else if (so(m.khoa) < 0) {
        loi(m.khoa, 'Không được là số âm.');
      }
    }

    const email = chuoi(KHOA.emailHoTro);
    if (email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      loi(KHOA.emailHoTro, 'Địa chỉ email không hợp lệ.');
    }

    const dienThoai = chuoi(KHOA.dienThoaiHoTro).replace(/[\s.()-]/g, '');
    if (dienThoai && !/^\+?\d{8,15}$/.test(dienThoai)) {
      loi(KHOA.dienThoaiHoTro, 'Số điện thoại phải có 8–15 chữ số.');
    }

    const mauMa = chuoi(KHOA.mauMaHoSo);
    if (mauMa && !mauMa.includes('{STT')) {
      loi(KHOA.mauMaHoSo, 'Mẫu mã phải có {STT} để mỗi hồ sơ nhận một mã khác nhau.');
    }

    if (co(KHOA.trungLapVang) && co(KHOA.trungLapDo)) {
      const vang = so(KHOA.trungLapVang);
      const do_ = so(KHOA.trungLapDo);

      if (!Number.isNaN(vang) && !Number.isNaN(do_) && vang >= do_) {
        loi(KHOA.trungLapVang, 'Ngưỡng cảnh báo vàng phải thấp hơn ngưỡng đỏ.');
      }

      if (do_ > 100) loi(KHOA.trungLapDo, 'Ngưỡng cảnh báo tính theo phần trăm, tối đa 100.');
    }

    if (co(KHOA.heSoTuVung) && co(KHOA.heSoNguNghia)) {
      const tong = so(KHOA.heSoTuVung) + so(KHOA.heSoNguNghia);

      if (!Number.isNaN(tong) && Math.abs(tong - 1) > 0.001) {
        loi(
          KHOA.heSoNguNghia,
          `Hai hệ số phải cộng lại bằng 1 (đang là ${tong.toFixed(2)}), nếu không tỷ lệ trùng lặp sẽ sai thang.`,
        );
      }
    }
  });
}

type GiaTriCauHinh = Record<string, unknown>;

/** Địa chỉ xem ảnh đang lưu, theo khoá cấu hình. */
const DUONG_DAN_ANH: Record<string, string | undefined> = {
  LOGO_ID: `${import.meta.env.VITE_API_URL ?? ''}/api/v1/he-thong/anh-thuong-hieu/logo`,
  FAVICON_ID: `${import.meta.env.VITE_API_URL ?? ''}/api/v1/he-thong/anh-thuong-hieu/favicon`,
};

/** Chức năng 46, 51 — Cấu hình hệ thống theo nhóm. */
export default function TrangCauHinhHeThong() {
  const { nhom = 'he-thong' } = useParams<{ nhom: string }>();
  /*
   * Nhóm cấu hình lạ thì báo hẳn ra chứ không lặng lẽ hiện nhóm "Cấu hình hệ thống". Ở màn hình
   * cấu hình thì hiện nhầm nhóm còn nguy hiểm hơn nơi khác: người dùng sửa và bấm Lưu, tưởng
   * mình vừa đổi cấu hình email nhưng thực ra vừa ghi đè cấu hình chung.
   *
   * Vẫn lấy nhóm mặc định để các hook bên dưới chạy đủ và đúng thứ tự.
   */
  const nhanRa = Object.prototype.hasOwnProperty.call(NHOM_THEO_DUONG_DAN, nhom);
  const cauHinhTrang = NHOM_THEO_DUONG_DAN[nhom] ?? NHOM_THEO_DUONG_DAN['he-thong'];

  const { message } = App.useApp();
  const [dsMuc, setDsMuc] = useState<CauHinhMuc[]>([]);

  // Luật đọc danh mục qua ref nên resolver không đổi khi máy chủ trả về danh sách trường.
  const refMuc = useRef<CauHinhMuc[]>([]);
  refMuc.current = dsMuc;
  const form = useBieuMau<GiaTriCauHinh>(
    useMemo(() => taoLuatCauHinh(() => refMuc.current), []),
    {},
  );

  const truyVan = useQuery({
    queryKey: ['cau-hinh', cauHinhTrang.nhom],
    queryFn: () => apiHeThong.cauHinh(cauHinhTrang.nhom),
    enabled: nhanRa,
  });

  useEffect(() => {
    if (!truyVan.data) return;

    setDsMuc(truyVan.data);
    const giaTri: Record<string, unknown> = {};

    truyVan.data.forEach((m) => {
      giaTri[m.khoa] =
        m.kieuDuLieu === 'NUMBER'
          ? Number(m.giaTri ?? 0)
          : m.kieuDuLieu === 'BOOLEAN'
            ? m.giaTri === 'true'
            : m.giaTri;
    });

    form.reset(giaTri);
  }, [truyVan.data, form]);

  const luu = useMutation({
    mutationFn: (giaTri: GiaTriCauHinh) =>
      apiHeThong.luuCauHinh(
        dsMuc
          .filter((m) => m.choPhepSua)
          .map((m) => ({
            khoa: m.khoa,
            giaTri:
              giaTri[m.khoa] === undefined || giaTri[m.khoa] === null
                ? null
                : String(
                    typeof giaTri[m.khoa] === 'object' && giaTri[m.khoa] !== null
                      ? (giaTri[m.khoa] as { toHexString?: () => string }).toHexString?.() ??
                          giaTri[m.khoa]
                      : giaTri[m.khoa],
                  ),
          })),
      ),
    onSuccess: () => {
      message.success('Đã lưu cấu hình');
      void truyVan.refetch();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không lưu được.'),
  });

  if (!nhanRa) {
    return (
      <KhoiKhongNhanRa
        ma={nhom}
        hopLe={Object.entries(NHOM_THEO_DUONG_DAN).map(([k, v]) => ({ ma: k, ten: v.tieuDe }))}
        duongDanGoc="/quan-tri/cau-hinh"
      />
    );
  }

  if (truyVan.isLoading) return <KhoiDangTai />;
  if (truyVan.error) return <KhoiLoi loi={truyVan.error} thuLai={truyVan.refetch} />;

  return (
    <Card
      title="Cấu hình hệ thống"
      extra={
        <Button
          type="primary"
          icon={<SaveOutlined />}
          loading={luu.isPending || form.formState.isSubmitting}
          onClick={form.handleSubmit((giaTri) => luu.mutateAsync(giaTri))}
        >
          Lưu cấu hình
        </Button>
      }
    >
      <DaiTabTrang danhSach={DS_TAB_CAU_HINH} dangChon={nhom} />

      {dsMuc.length === 0 && (
        <Alert
          type="info"
          showIcon
          style={{ marginTop: 12 }}
          message="Nhóm này chưa có mục cấu hình nào"
          description="Không phải lỗi — nhóm được khai báo nhưng chưa có khoá nào thuộc về nó. Chọn một tab khác ở trên."
        />
      )}

      <BieuMau
        id="form-cau-hinh"
        form={form}
        onGui={(giaTri) => luu.mutateAsync(giaTri)}
        style={{ maxWidth: 720 }}
      >
        {dsMuc.map((m) => (
          <Truong<GiaTriCauHinh> key={m.khoa} ten={m.khoa} label={m.tenHienThi} extra={m.moTa}>
            {(o) =>
              m.kieuDuLieu === 'BOOLEAN' ? (
                <Switch
                  checked={!!o.value}
                  onChange={o.onChange}
                  disabled={!m.choPhepSua}
                />
              ) : m.kieuDuLieu === 'NUMBER' ? (
                <InputNumber
                  {...o}
                  value={o.value as number}
                  style={{ width: 220 }}
                  disabled={!m.choPhepSua}
                />
              ) : m.kieuDuLieu === 'TEP' ? (
                <ONhapAnhCauHinh
                  value={(o.value as string) ?? ''}
                  onChange={o.onChange}
                  disabled={!m.choPhepSua}
                  duongDanXem={DUONG_DAN_ANH[m.khoa]}
                />
              ) : m.kieuDuLieu === 'COLOR' ? (
                <ColorPicker
                  value={o.value as string}
                  onChange={o.onChange}
                  showText
                  disabled={!m.choPhepSua}
                />
              ) : (
                <Input {...o} value={o.value as string} disabled={!m.choPhepSua} />
              )
            }
          </Truong>
        ))}
      </BieuMau>

      <Space />
    </Card>
  );
}
