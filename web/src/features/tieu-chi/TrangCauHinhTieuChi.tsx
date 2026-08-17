import { useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import {
  App,
  Alert,
  Button,
  Card,
  Col,
  Input,
  InputNumber,
  List,
  Row,
  Space,
  Statistic,
  Tag,
  Typography,
} from 'antd';
import { DeleteOutlined, PlusOutlined, SafetyOutlined, SaveOutlined } from '@ant-design/icons';
import { useMutation, useQuery } from '@tanstack/react-query';

import { LoiApi } from '@/api/client';
import { apiTieuChi } from '@/api/endpoints';
import { KhoiDangTai, KhoiLoi } from '@/components/ThanhPhanChung';

interface TieuChiSua {
  id: string;
  ma: string;
  ten: string;
  diemToiDa: number;
  buocNhay: number;
  batBuocNhanXet: boolean;
  huongDanCham?: string | null;
}

interface NhomSua {
  id: string;
  ma: string;
  ten: string;
  trongSo: number;
  diemToiDa: number;
  danhSachTieuChi: TieuChiSua[];
}

/** Chức năng 17–18 — Cấu hình cây nhóm tiêu chí / tiêu chí (2 cấp). */
export default function TrangCauHinhTieuChi() {
  const { id = '' } = useParams<{ id: string }>();
  const { message, modal } = App.useApp();

  const [nhom, setNhom] = useState<NhomSua[]>([]);

  const truyVan = useQuery({
    queryKey: ['bo-tieu-chi', id],
    queryFn: () => apiTieuChi.chiTiet(id),
  });

  useEffect(() => {
    if (!truyVan.data) return;

    setNhom(
      truyVan.data.danhSachNhom.map((n) => ({
        id: n.id,
        ma: n.ma,
        ten: n.ten,
        trongSo: n.trongSo,
        diemToiDa: n.diemToiDa,
        danhSachTieuChi: n.danhSachTieuChi.map((t) => ({
          id: t.id,
          ma: t.ma,
          ten: t.ten,
          diemToiDa: t.diemToiDa,
          buocNhay: t.buocNhay,
          batBuocNhanXet: t.batBuocNhanXet,
          huongDanCham: t.huongDanCham,
        })),
      })),
    );
  }, [truyVan.data]);

  const luu = useMutation({
    mutationFn: () => apiTieuChi.luuCay(id, nhom),
    onSuccess: () => message.success('Đã lưu bộ tiêu chí'),
    onError: (loi) =>
      modal.error({
        title: 'Không lưu được',
        content: loi instanceof LoiApi ? loi.message : 'Đã xảy ra lỗi.',
      }),
  });

  const kiemTra = useMutation({
    mutationFn: () => apiTieuChi.kiemTra(id),
    onSuccess: (loi) =>
      modal.info({
        title: loi.length === 0 ? 'Bộ tiêu chí hợp lệ' : `Có ${loi.length} vấn đề`,
        content:
          loi.length === 0 ? (
            <Alert type="success" showIcon message="Trọng số, thang điểm và mức công nhận đều hợp lệ." />
          ) : (
            <ul style={{ paddingLeft: 18 }}>
              {loi.map((l) => (
                <li key={l}>{l}</li>
              ))}
            </ul>
          ),
      }),
  });

  const tongTrongSo = useMemo(() => nhom.reduce((s, n) => s + (n.trongSo || 0), 0), [nhom]);
  const tongDiem = useMemo(
    () => nhom.reduce((s, n) => s + n.danhSachTieuChi.reduce((x, t) => x + (t.diemToiDa || 0), 0), 0),
    [nhom],
  );

  if (truyVan.isLoading) return <KhoiDangTai />;
  if (truyVan.error) return <KhoiLoi loi={truyVan.error} thuLai={truyVan.refetch} />;

  const bo = truyVan.data!;
  const trongSoHopLe = bo.cachTinh !== 'TRUNG_BINH_TRONG_SO' || Math.abs(tongTrongSo - 100) < 0.01;
  const diemHopLe = Math.abs(tongDiem - bo.thangDiemToiDa) < 0.01;

  return (
    <Card
      title={`Cấu hình: ${bo.ten}`}
      extra={
        <Space>
          <Button
            icon={<SafetyOutlined />}
            loading={kiemTra.isPending}
            onClick={() => kiemTra.mutate()}
          >
            Kiểm tra
          </Button>
          <Button
            type="primary"
            icon={<SaveOutlined />}
            loading={luu.isPending}
            onClick={() => luu.mutate()}
          >
            Lưu
          </Button>
        </Space>
      }
    >
      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        <Col xs={12} md={6}>
          <Statistic title="Thang điểm" value={bo.thangDiemToiDa} />
        </Col>
        <Col xs={12} md={6}>
          <Statistic title="Điểm đạt tối thiểu" value={bo.diemDatToiThieu} />
        </Col>
        <Col xs={12} md={6}>
          <Statistic
            title="Tổng điểm tối đa cấu hình"
            value={tongDiem}
            valueStyle={{ color: diemHopLe ? '#52c41a' : '#ff4d4f' }}
          />
        </Col>
        <Col xs={12} md={6}>
          <Statistic
            title="Tổng trọng số nhóm"
            value={tongTrongSo}
            suffix="%"
            valueStyle={{ color: trongSoHopLe ? '#52c41a' : '#ff4d4f' }}
          />
        </Col>
      </Row>

      {!diemHopLe && (
        <Alert
          type="warning"
          showIcon
          style={{ marginBottom: 12 }}
          message={`Tổng điểm các tiêu chí (${tongDiem}) khác thang điểm của bộ (${bo.thangDiemToiDa}).`}
        />
      )}

      {!trongSoHopLe && (
        <Alert
          type="error"
          showIcon
          style={{ marginBottom: 12 }}
          message={`Bộ tiêu chí tính theo trọng số nên tổng trọng số phải bằng 100%, hiện là ${tongTrongSo}%.`}
        />
      )}

      {nhom.map((n, iNhom) => (
        <Card
          key={n.id}
          size="small"
          style={{ marginBottom: 12 }}
          title={
            <Space wrap>
              <Input
                value={n.ten}
                style={{ width: 260 }}
                onChange={(e) => datNhom(iNhom, { ten: e.target.value })}
              />
              <Tag>
                {n.danhSachTieuChi.reduce((s, t) => s + t.diemToiDa, 0)} điểm
              </Tag>
            </Space>
          }
          extra={
            <Space>
              <span>Trọng số</span>
              <InputNumber
                min={0}
                max={100}
                value={n.trongSo}
                onChange={(v) => datNhom(iNhom, { trongSo: Number(v) || 0 })}
              />
              <Button
                danger
                type="text"
                icon={<DeleteOutlined />}
                onClick={() => setNhom((cu) => cu.filter((_, i) => i !== iNhom))}
              />
            </Space>
          }
        >
          <List
            size="small"
            dataSource={n.danhSachTieuChi}
            renderItem={(t, iTc) => (
              <List.Item>
                <Space wrap style={{ width: '100%' }}>
                  <Input
                    value={t.ten}
                    style={{ width: 340 }}
                    placeholder="Tên tiêu chí"
                    onChange={(e) => datTieuChi(iNhom, iTc, { ten: e.target.value })}
                  />
                  <InputNumber
                    min={0}
                    value={t.diemToiDa}
                    addonAfter="điểm"
                    onChange={(v) => datTieuChi(iNhom, iTc, { diemToiDa: Number(v) || 0 })}
                  />
                  <Input
                    value={t.huongDanCham ?? ''}
                    style={{ width: 300 }}
                    placeholder="Hướng dẫn chấm"
                    onChange={(e) => datTieuChi(iNhom, iTc, { huongDanCham: e.target.value })}
                  />
                  <Button
                    danger
                    type="text"
                    icon={<DeleteOutlined />}
                    onClick={() =>
                      setNhom((cu) =>
                        cu.map((x, i) =>
                          i === iNhom
                            ? {
                                ...x,
                                danhSachTieuChi: x.danhSachTieuChi.filter((_, j) => j !== iTc),
                              }
                            : x,
                        ),
                      )
                    }
                  />
                </Space>
              </List.Item>
            )}
          />

          <Button
            type="dashed"
            block
            icon={<PlusOutlined />}
            style={{ marginTop: 8 }}
            onClick={() =>
              setNhom((cu) =>
                cu.map((x, i) =>
                  i === iNhom
                    ? {
                        ...x,
                        danhSachTieuChi: [
                          ...x.danhSachTieuChi,
                          {
                            id: '00000000-0000-0000-0000-000000000000',
                            ma: '',
                            ten: '',
                            diemToiDa: 0,
                            buocNhay: 0.5,
                            batBuocNhanXet: false,
                          },
                        ],
                      }
                    : x,
                ),
              )
            }
          >
            Thêm tiêu chí
          </Button>
        </Card>
      ))}

      <Button
        type="dashed"
        block
        icon={<PlusOutlined />}
        onClick={() =>
          setNhom((cu) => [
            ...cu,
            {
              id: '00000000-0000-0000-0000-000000000000',
              ma: '',
              ten: 'Nhóm tiêu chí mới',
              trongSo: 0,
              diemToiDa: 0,
              danhSachTieuChi: [],
            },
          ])
        }
      >
        Thêm nhóm tiêu chí
      </Button>

      <Typography.Paragraph type="secondary" style={{ marginTop: 12, fontSize: 12 }}>
        Lưu ý: bộ tiêu chí đã có phiếu chấm sẽ không sửa được cấu trúc — hãy sao chép sang bộ mới.
      </Typography.Paragraph>
    </Card>
  );

  function datNhom(chiSo: number, thayDoi: Partial<NhomSua>) {
    setNhom((cu) => cu.map((n, i) => (i === chiSo ? { ...n, ...thayDoi } : n)));
  }

  function datTieuChi(iNhom: number, iTc: number, thayDoi: Partial<TieuChiSua>) {
    setNhom((cu) =>
      cu.map((n, i) =>
        i === iNhom
          ? {
              ...n,
              danhSachTieuChi: n.danhSachTieuChi.map((t, j) =>
                j === iTc ? { ...t, ...thayDoi } : t,
              ),
            }
          : n,
      ),
    );
  }
}
