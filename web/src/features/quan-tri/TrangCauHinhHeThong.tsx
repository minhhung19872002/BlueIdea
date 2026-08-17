import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { App, Button, Card, ColorPicker, Form, Input, InputNumber, Space, Switch } from 'antd';
import { SaveOutlined } from '@ant-design/icons';
import { useMutation, useQuery } from '@tanstack/react-query';

import { LoiApi } from '@/api/client';
import { apiHeThong, type CauHinhMuc } from '@/api/endpoints';
import { KhoiDangTai, KhoiLoi } from '@/components/ThanhPhanChung';

const NHOM_THEO_DUONG_DAN: Record<string, { nhom?: string; tieuDe: string }> = {
  'he-thong': { nhom: undefined, tieuDe: 'Cấu hình hệ thống' },
  'sang-kien': { nhom: 'HO_SO', tieuDe: 'Cấu hình thông tin sáng kiến' },
  'email-sms': { nhom: 'EMAIL_SMS', tieuDe: 'Cấu hình email & SMS' },
  'chu-ky-so': { nhom: 'CHU_KY_SO', tieuDe: 'Cấu hình chữ ký số' },
  'tich-hop': { nhom: 'TICH_HOP', tieuDe: 'Cấu hình tích hợp hệ thống' },
  menu: { nhom: 'MENU', tieuDe: 'Cấu hình menu' },
};

/** Chức năng 46, 51 — Cấu hình hệ thống theo nhóm. */
export default function TrangCauHinhHeThong() {
  const { nhom = 'he-thong' } = useParams<{ nhom: string }>();
  const cauHinhTrang = NHOM_THEO_DUONG_DAN[nhom] ?? NHOM_THEO_DUONG_DAN['he-thong'];

  const { message } = App.useApp();
  const [form] = Form.useForm();
  const [dsMuc, setDsMuc] = useState<CauHinhMuc[]>([]);

  const truyVan = useQuery({
    queryKey: ['cau-hinh', cauHinhTrang.nhom],
    queryFn: () => apiHeThong.cauHinh(cauHinhTrang.nhom),
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

    form.setFieldsValue(giaTri);
  }, [truyVan.data, form]);

  const luu = useMutation({
    mutationFn: (giaTri: Record<string, unknown>) =>
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

  if (truyVan.isLoading) return <KhoiDangTai />;
  if (truyVan.error) return <KhoiLoi loi={truyVan.error} thuLai={truyVan.refetch} />;

  return (
    <Card
      title={cauHinhTrang.tieuDe}
      extra={
        <Button
          type="primary"
          icon={<SaveOutlined />}
          loading={luu.isPending}
          onClick={async () => luu.mutate(await form.validateFields())}
        >
          Lưu cấu hình
        </Button>
      }
    >
      <Form form={form} layout="vertical" style={{ maxWidth: 720 }}>
        {dsMuc.map((m) => (
          <Form.Item
            key={m.khoa}
            name={m.khoa}
            label={m.tenHienThi}
            extra={m.moTa}
            valuePropName={m.kieuDuLieu === 'BOOLEAN' ? 'checked' : 'value'}
          >
            {m.kieuDuLieu === 'BOOLEAN' ? (
              <Switch disabled={!m.choPhepSua} />
            ) : m.kieuDuLieu === 'NUMBER' ? (
              <InputNumber style={{ width: 220 }} disabled={!m.choPhepSua} />
            ) : m.kieuDuLieu === 'COLOR' ? (
              <ColorPicker showText disabled={!m.choPhepSua} />
            ) : (
              <Input disabled={!m.choPhepSua} />
            )}
          </Form.Item>
        ))}
      </Form>

      <Space />
    </Card>
  );
}
