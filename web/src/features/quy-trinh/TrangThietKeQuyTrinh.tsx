import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  App,
  Alert,
  Button,
  Card,
  Col,
  Drawer,
  Input,
  InputNumber,
  List,
  Row,
  Select,
  Space,
  Switch,
  Tag,
  Tooltip,
  Typography,
} from 'antd';
import {
  CheckCircleOutlined,
  ExpandOutlined,
  FilePdfOutlined,
  PictureOutlined,
  SafetyCertificateOutlined,
  SaveOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { toPng } from 'html-to-image';
import ReactFlow, {
  Background,
  Controls,
  MarkerType,
  MiniMap,
  useEdgesState,
  useNodesState,
  type Edge,
  type Node,
} from 'reactflow';
import 'reactflow/dist/style.css';

import { LoiApi } from '@/api/client';
import { apiQuyTrinh, type BuocQuyTrinh, type SoDoQuyTrinh } from '@/api/endpoints';
import { BieuMau, Truong, useBieuMau } from '@/components/bieu-mau/BieuMau';
import { batBuoc, maKyThuat, soNguyen, tuyChon } from '@/components/bieu-mau/luat';
import { KhoiDangTai, KhoiLoi } from '@/components/ThanhPhanChung';

const LOAI_BUOC = [
  'TIEP_NHAN',
  'THAM_DINH',
  'PHAN_CONG_CHAM',
  'CHAM_DIEM',
  'HOP_HOI_DONG',
  'BO_PHIEU',
  'PHE_DUYET',
  'BAN_HANH_QUYET_DINH',
  'CONG_BO',
  'KET_THUC',
].map((v) => ({ value: v, label: v.replace(/_/g, ' ') }));

const LOAI_TAC_NHAN = [
  'VAI_TRO',
  'NGUOI_DUNG',
  'DON_VI',
  'HOI_DONG',
  'CHUC_DANH_HOI_DONG',
  'NGUOI_TAO_HO_SO',
  'LANH_DAO_DON_VI_TAC_GIA',
].map((v) => ({ value: v, label: v.replace(/_/g, ' ') }));

const QUY_TAC = [
  { value: 'MOT_NGUOI', label: 'Một người xử lý' },
  { value: 'TAT_CA', label: 'Tất cả tác nhân' },
  { value: 'DA_SO', label: 'Đa số' },
  { value: 'CHU_TICH_QUYET_DINH', label: 'Chủ tịch quyết định' },
];

/**
 * Chức năng 12 — các chức năng bổ sung có thể bật/tắt cho từng quy trình.
 *
 * Mã phải khớp với hằng số phía máy chủ; danh sách này chỉ là nhãn hiển thị và mô tả.
 */
const CHUC_NANG_BO_SUNG: { ma: string; ten: string; moTa: string }[] = [
  { ma: 'KIEM_TRA_TRUNG_LAP', ten: 'Kiểm tra trùng lặp', moTa: 'Chạy đối chiếu AI nội bộ khi nộp hồ sơ' },
  { ma: 'CHAM_DIEM_DOC_LAP', ten: 'Chấm điểm độc lập', moTa: 'Mỗi thành viên hội đồng chấm riêng, không thấy điểm người khác' },
  { ma: 'BO_PHIEU_KIN', ten: 'Bỏ phiếu kín', moTa: 'Kết quả bỏ phiếu chỉ lộ sau khi chốt phiên họp' },
  { ma: 'TAO_BIEN_BAN', ten: 'Tạo biên bản họp', moTa: 'Sinh biên bản họp hội đồng theo mẫu' },
  { ma: 'KY_SO', ten: 'Ký số', moTa: 'Yêu cầu ký số biên bản và quyết định' },
  { ma: 'GUI_EMAIL', ten: 'Gửi email', moTa: 'Thông báo qua email khi đổi trạng thái' },
  { ma: 'GUI_SMS', ten: 'Gửi SMS', moTa: 'Thông báo qua SMS khi đổi trạng thái' },
  { ma: 'XUAT_BIEU_MAU', ten: 'Xuất biểu mẫu', moTa: 'Cho phép xuất phiếu/biên bản từ hồ sơ' },
  { ma: 'CONG_KHAI_KET_QUA', ten: 'Công khai kết quả', moTa: 'Hiển thị kết quả trên cổng tra cứu công khai' },
];

/**
 * Luật kiểm tra một bước trong quy trình.
 *
 * Một bước không thể vừa là bước bắt đầu vừa là bước kết thúc: hồ sơ vào bước đó sẽ đóng ngay
 * mà chưa qua bất kỳ khâu xử lý nào, tức là quy trình rỗng nhưng vẫn "hợp lệ" trên giấy.
 */
const luatBuoc = z
  .object({
    ma: maKyThuat(50),
    ten: batBuoc('Tên bước'),
    loaiBuoc: z.string(),
    soNgayXuLy: soNguyen('Số ngày xử lý', 0, 365),
    tinhTheoNgayLamViec: z.boolean(),
    batBuocNhapYKien: z.boolean(),
    batBuocDinhKem: z.boolean(),
    choPhepThuHoi: z.boolean(),
    laBuocBatDau: z.boolean(),
    laBuocKetThuc: z.boolean(),
    moTaHuongDan: tuyChon(2000),
  })
  .superRefine((v, ctx) => {
    if (v.laBuocBatDau && v.laBuocKetThuc) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['laBuocKetThuc'],
        message: 'Một bước không thể vừa mở đầu vừa kết thúc quy trình.',
      });
    }
  });

type GiaTriBuoc = z.infer<typeof luatBuoc>;

const MAC_DINH_BUOC: GiaTriBuoc = {
  ma: '',
  ten: '',
  loaiBuoc: 'XU_LY',
  soNgayXuLy: 0,
  tinhTheoNgayLamViec: true,
  batBuocNhapYKien: false,
  batBuocDinhKem: false,
  choPhepThuHoi: false,
  laBuocBatDau: false,
  laBuocKetThuc: false,
};

/** Rút phần cấu hình sửa được của một bước ra khỏi bản ghi đầy đủ. */
function doiSangGiaTri(b: BuocQuyTrinh): GiaTriBuoc {
  return {
    ma: b.ma,
    ten: b.ten,
    loaiBuoc: b.loaiBuoc,
    soNgayXuLy: b.soNgayXuLy,
    tinhTheoNgayLamViec: b.tinhTheoNgayLamViec,
    batBuocNhapYKien: b.batBuocNhapYKien,
    batBuocDinhKem: b.batBuocDinhKem,
    choPhepThuHoi: b.choPhepThuHoi,
    laBuocBatDau: b.laBuocBatDau,
    laBuocKetThuc: b.laBuocKetThuc,
    moTaHuongDan: b.moTaHuongDan ?? undefined,
  };
}

/** Chức năng 10–15 — Trình thiết kế quy trình trực quan trên ReactFlow. */
export default function TrangThietKeQuyTrinh() {
  const { id = '' } = useParams<{ id: string }>();
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [soDo, setSoDo] = useState<SoDoQuyTrinh | null>(null);
  const [buocDangChon, setBuocDangChon] = useState<BuocQuyTrinh | null>(null);
  const [nodes, setNodes, onNodesChange] = useNodesState<Node[]>([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState<Edge[]>([]);
  const formBuoc = useBieuMau(luatBuoc, MAC_DINH_BUOC);

  const truyVan = useQuery({
    queryKey: ['quy-trinh', id, 'so-do'],
    queryFn: () => apiQuyTrinh.soDo(id),
  });

  useEffect(() => {
    if (truyVan.data) setSoDo(truyVan.data);
  }, [truyVan.data]);

  // Dựng node/edge từ cấu hình bước và trường hợp chuyển tiếp.
  useEffect(() => {
    if (!soDo) return;

    const viTriLuu = (soDo.soDoLayout ?? {}) as Record<string, { x: number; y: number }>;

    const nodeMoi: Node[] = soDo.danhSachBuoc.map((b, i) => ({
      id: b.id,
      position: viTriLuu[b.id] ?? { x: 60 + (i % 3) * 300, y: 60 + Math.floor(i / 3) * 190 },
      data: {
        label: (
          <div style={{ textAlign: 'left' }}>
            <div style={{ fontWeight: 600 }}>{b.ten}</div>
            <div style={{ fontSize: 11, color: '#888' }}>{b.loaiBuoc.replace(/_/g, ' ')}</div>
            <div style={{ fontSize: 11, color: '#888' }}>
              {b.soNgayXuLy} ngày{b.tinhTheoNgayLamViec ? ' làm việc' : ''} •{' '}
              {b.tacNhan.length} tác nhân
            </div>
            {b.laBuocBatDau && <Tag color="green">Bắt đầu</Tag>}
            {b.laBuocKetThuc && <Tag color="red">Kết thúc</Tag>}
          </div>
        ),
      },
      style: {
        width: 240,
        borderRadius: 8,
        border: b.laBuocBatDau
          ? '2px solid #52c41a'
          : b.laBuocKetThuc
            ? '2px solid #ff4d4f'
            : '1px solid #d9d9d9',
        padding: 10,
        background: '#fff',
      },
    }));

    const edgeMoi: Edge[] = soDo.danhSachBuoc.flatMap((b) =>
      b.truongHop
        .filter((t) => t.buocTiepTheoId)
        .map((t) => ({
          id: t.id,
          source: b.id,
          target: t.buocTiepTheoId!,
          label: t.ten,
          animated: t.ma === 'DAT',
          markerEnd: { type: MarkerType.ArrowClosed },
          style: {
            stroke: t.ma === 'KHONG_DAT' || t.ma === 'TRA_LAI' ? '#ff4d4f' : '#1677ff',
          },
          labelStyle: { fontSize: 11 },
        })),
    );

    setNodes(nodeMoi);
    setEdges(edgeMoi);
  }, [soDo, setNodes, setEdges]);

  const luu = useMutation({
    mutationFn: () => {
      const layout: Record<string, { x: number; y: number }> = {};
      nodes.forEach((n) => {
        layout[n.id] = { x: n.position.x, y: n.position.y };
      });

      return apiQuyTrinh.luuSoDo(id, { ...soDo!, soDoLayout: layout });
    },
    onSuccess: () => {
      message.success('Đã lưu sơ đồ quy trình');
      void queryClient.invalidateQueries({ queryKey: ['quy-trinh', id] });
    },
    onError: (loi) =>
      modal.error({
        title: 'Không lưu được sơ đồ',
        content: loi instanceof LoiApi ? loi.message : 'Đã xảy ra lỗi.',
      }),
  });

  const kiemTra = useMutation({
    mutationFn: () => apiQuyTrinh.kiemTra(id),
    onSuccess: (kq) => {
      modal.info({
        title: kq.hopLe ? 'Quy trình hợp lệ' : 'Quy trình chưa hợp lệ',
        width: 640,
        content: (
          <div>
            {kq.danhSachLoi.length > 0 && (
              <>
                <Typography.Text strong type="danger">
                  Lỗi phải sửa ({kq.danhSachLoi.length})
                </Typography.Text>
                <List
                  size="small"
                  dataSource={kq.danhSachLoi}
                  renderItem={(l) => (
                    <List.Item>
                      <Typography.Text type="danger">• {l.thongBao}</Typography.Text>
                    </List.Item>
                  )}
                />
              </>
            )}
            {kq.danhSachCanhBao.length > 0 && (
              <>
                <Typography.Text strong type="warning">
                  Cảnh báo ({kq.danhSachCanhBao.length})
                </Typography.Text>
                <List
                  size="small"
                  dataSource={kq.danhSachCanhBao}
                  renderItem={(l) => (
                    <List.Item>
                      <Typography.Text type="warning">• {l.thongBao}</Typography.Text>
                    </List.Item>
                  )}
                />
              </>
            )}
            {kq.hopLe && kq.danhSachCanhBao.length === 0 && (
              <Alert type="success" showIcon message="Quy trình đạt toàn bộ 7 quy tắc kiểm tra." />
            )}
          </div>
        ),
      });
    },
  });

  const kichHoat = useMutation({
    mutationFn: () => apiQuyTrinh.kichHoat(id),
    onSuccess: () => message.success('Đã kích hoạt quy trình'),
    onError: (loi) =>
      modal.error({
        title: 'Không kích hoạt được',
        content: loi instanceof LoiApi ? loi.message : 'Đã xảy ra lỗi.',
      }),
  });

  const capNhatBuoc = useCallback(
    (giaTri: Partial<BuocQuyTrinh>) => {
      if (!buocDangChon || !soDo) return;

      setSoDo({
        ...soDo,
        danhSachBuoc: soDo.danhSachBuoc.map((b) =>
          b.id === buocDangChon.id ? { ...b, ...giaTri } : b,
        ),
      });
    },
    [buocDangChon, soDo],
  );

  /*
   * Ngăn kéo cấu hình bước tự lưu: không có nút Lưu riêng, mọi thay đổi đẩy thẳng vào sơ đồ
   * trong bộ nhớ rồi mới ghi xuống máy chủ khi bấm Lưu sơ đồ.
   *
   * Bỏ qua lần gọi do `reset` sinh ra (không kèm tên trường) — đó là lúc người dùng bấm sang
   * bước khác chứ không phải sửa nội dung, và nếu xử lý sẽ ghi đè bước vừa chọn bằng chính nó.
   */
  useEffect(() => {
    const dangKy = formBuoc.watch((giaTri, { name }) => {
      if (!name) return;
      capNhatBuoc(giaTri as Partial<BuocQuyTrinh>);
    });

    return () => dangKy.unsubscribe();
  }, [formBuoc, capNhatBuoc]);

  const thongKe = useMemo(
    () => ({
      soBuoc: soDo?.danhSachBuoc.length ?? 0,
      soTruongHop: soDo?.danhSachBuoc.reduce((s, b) => s + b.truongHop.length, 0) ?? 0,
      soThanhPhan: soDo?.thanhPhanHoSo.length ?? 0,
    }),
    [soDo],
  );

  const khungSoDo = useRef<HTMLDivElement>(null);

  /**
   * Xuất sơ đồ ra ảnh PNG.
   *
   * Chụp đúng khung canvas (không chụp cả trang) và ẩn phần điều khiển của ReactFlow, để ảnh
   * đưa vào hồ sơ trình ký chỉ còn sơ đồ.
   */
  const chupSoDo = useCallback(async () => {
    const khung = khungSoDo.current?.querySelector<HTMLElement>('.react-flow__viewport');
    if (!khung) return null;

    return toPng(khung, {
      backgroundColor: '#ffffff',
      pixelRatio: 2,
      filter: (nut) =>
        !(nut instanceof HTMLElement)
        || !(nut.classList.contains('react-flow__controls')
             || nut.classList.contains('react-flow__minimap')),
    });
  }, []);

  const xuatPng = useCallback(async () => {
    try {
      const anh = await chupSoDo();
      if (!anh) return;

      const the = document.createElement('a');
      the.href = anh;
      the.download = `so-do-quy-trinh-${id}.png`;
      the.click();
    } catch {
      message.error('Không xuất được sơ đồ. Thử phóng to sơ đồ rồi xuất lại.');
    }
  }, [chupSoDo, id, message]);

  /**
   * Xuất sơ đồ ra PDF khổ A4 nằm ngang.
   *
   * Dựng PDF bằng cửa sổ in của trình duyệt thay vì thêm một thư viện PDF phía client: gói PDF
   * đủ dùng nặng cỡ vài trăm KB cho đúng một màn hình, trong khi sơ đồ cần in ra chỉ là một tấm
   * ảnh đặt vừa trang giấy.
   */
  const xuatPdf = useCallback(async () => {
    try {
      const anh = await chupSoDo();
      if (!anh) return;

      const cuaSo = window.open('', '_blank');

      if (!cuaSo) {
        message.warning('Trình duyệt đã chặn cửa sổ in. Cho phép cửa sổ bật lên rồi thử lại.');
        return;
      }

      cuaSo.document.write(
        '<!doctype html><html lang="vi"><head><meta charset="utf-8">'
          + `<title>So do quy trinh ${id}</title>`
          + '<style>@page{size:A4 landscape;margin:12mm}'
          + 'body{margin:0;display:flex;align-items:center;justify-content:center;height:100vh}'
          + 'img{max-width:100%;max-height:100%}</style></head>'
          + `<body><img src="${anh}" alt="Sơ đồ quy trình"></body></html>`,
      );
      cuaSo.document.close();

      // In sau khi ảnh đã tải xong, nếu không hộp thoại in mở ra khi trang còn trắng.
      const anhTrongCuaSo = cuaSo.document.querySelector('img');

      if (anhTrongCuaSo?.complete) {
        cuaSo.print();
      } else {
        anhTrongCuaSo?.addEventListener('load', () => cuaSo.print());
      }
    } catch {
      message.error('Không xuất được sơ đồ ra PDF.');
    }
  }, [chupSoDo, id, message]);

  /** Xem sơ đồ toàn màn hình — quy trình nhiều bước không đọc nổi trong khung nhỏ. */
  const doiToanManHinh = useCallback(() => {
    const khung = khungSoDo.current;
    if (!khung) return;

    if (document.fullscreenElement) {
      void document.exitFullscreen();
      return;
    }

    void khung.requestFullscreen?.().catch(() => {
      message.warning('Trình duyệt không cho phép chế độ toàn màn hình ở trang này.');
    });
  }, [message]);

  /** Bật/tắt một chức năng bổ sung ở cấp quy trình (không gắn với bước cụ thể). */
  const doiChucNangBoSung = useCallback(
    (maChucNang: string, bat: boolean) => {
      if (!soDo) return;

      const conLai = soDo.chucNangBoSung.filter((c) => c.maChucNang !== maChucNang);

      setSoDo({
        ...soDo,
        chucNangBoSung: bat
          ? [...conLai, { id: crypto.randomUUID(), buocId: null, maChucNang, batBuoc: false }]
          : conLai,
      });
    },
    [soDo],
  );

  if (truyVan.isLoading) return <KhoiDangTai />;
  if (truyVan.error) return <KhoiLoi loi={truyVan.error} thuLai={truyVan.refetch} />;

  return (
    <Card
      title="Trình thiết kế quy trình"
      extra={
        <Space wrap>
          <Tag>{thongKe.soBuoc} bước</Tag>
          <Tag>{thongKe.soTruongHop} nhánh</Tag>
          <Tag>{thongKe.soThanhPhan} thành phần hồ sơ</Tag>
          <Button
            icon={<SafetyCertificateOutlined />}
            loading={kiemTra.isPending}
            onClick={() => kiemTra.mutate()}
          >
            Kiểm tra hợp lệ
          </Button>
          <Button icon={<SaveOutlined />} loading={luu.isPending} onClick={() => luu.mutate()}>
            Lưu sơ đồ
          </Button>
          <Button icon={<PictureOutlined />} onClick={() => void xuatPng()}>
            Xuất PNG
          </Button>
          <Button icon={<FilePdfOutlined />} onClick={() => void xuatPdf()}>
            Xuất PDF
          </Button>
          <Tooltip title="Xem sơ đồ toàn màn hình (Esc để thoát)">
            <Button icon={<ExpandOutlined />} onClick={doiToanManHinh} />
          </Tooltip>
          <Link to={`/quan-tri/quy-trinh/${id}/thanh-phan`}>
            <Button>Thành phần hồ sơ</Button>
          </Link>
          <Link to={`/quan-tri/quy-trinh/${id}/lien-thong`}>
            <Button>Liên thông</Button>
          </Link>
          <Button
            type="primary"
            icon={<CheckCircleOutlined />}
            loading={kichHoat.isPending}
            onClick={() => kichHoat.mutate()}
          >
            Kích hoạt
          </Button>
        </Space>
      }
      styles={{ body: { padding: 0 } }}
    >
      <Alert
        type="info"
        showIcon
        banner
        message="Nhấp vào một bước để mở bảng cấu hình. Kéo thả để sắp xếp sơ đồ, sau đó bấm Lưu sơ đồ."
      />

      <div style={{ padding: '12px 16px', borderBottom: '1px solid #f0f0f0' }}>
        <div style={{ fontSize: 14, fontWeight: 600, marginBottom: 8 }}>
          Chức năng bổ sung của quy trình
        </div>
        <Row gutter={[12, 8]}>
          {CHUC_NANG_BO_SUNG.map((c) => {
            const bat = soDo?.chucNangBoSung.some((x) => x.maChucNang === c.ma) ?? false;

            return (
              <Col key={c.ma} xs={24} sm={12} lg={8}>
                <div style={{ display: 'flex', alignItems: 'flex-start', gap: 8 }}>
                  <Switch
                    size="small"
                    checked={bat}
                    onChange={(v) => doiChucNangBoSung(c.ma, v)}
                    style={{ marginTop: 3, flexShrink: 0 }}
                  />
                  <div style={{ minWidth: 0 }}>
                    <div style={{ fontSize: 13.5 }}>{c.ten}</div>
                    <div style={{ fontSize: 12, color: 'rgba(0,0,0,0.45)', lineHeight: 1.4 }}>
                      {c.moTa}
                    </div>
                  </div>
                </div>
              </Col>
            );
          })}
        </Row>
      </div>

      <div ref={khungSoDo} style={{ height: 'calc(100vh - 260px)', minHeight: 460 }}>
        <ReactFlow
          nodes={nodes}
          edges={edges}
          onNodesChange={onNodesChange}
          onEdgesChange={onEdgesChange}
          onNodeClick={(_su, node) => {
            const buoc = soDo?.danhSachBuoc.find((b) => b.id === node.id) ?? null;
            setBuocDangChon(buoc);
            if (buoc) formBuoc.reset(doiSangGiaTri(buoc));
          }}
          fitView
          proOptions={{ hideAttribution: true }}
        >
          <Background />
          <Controls />
          <MiniMap pannable zoomable />
        </ReactFlow>
      </div>

      <Drawer
        open={!!buocDangChon}
        title={`Cấu hình bước: ${buocDangChon?.ten ?? ''}`}
        width={520}
        onClose={() => setBuocDangChon(null)}
      >
        <BieuMau form={formBuoc} onGui={() => {}}>
          <Truong<GiaTriBuoc> ten="ma" label="Mã bước">
            {(o) => <Input {...o} value={o.value as string} />}
          </Truong>
          <Truong<GiaTriBuoc> ten="ten" label="Tên bước">
            {(o) => <Input {...o} value={o.value as string} />}
          </Truong>
          <Truong<GiaTriBuoc> ten="loaiBuoc" label="Loại bước">
            {(o) => <Select {...o} value={o.value as string} options={LOAI_BUOC} />}
          </Truong>

          <Row gutter={8}>
            <Col span={12}>
              <Truong<GiaTriBuoc> ten="soNgayXuLy" label="Số ngày xử lý">
                {(o) => (
                  <InputNumber
                    {...o}
                    value={o.value as number}
                    min={0}
                    max={365}
                    style={{ width: '100%' }}
                  />
                )}
              </Truong>
            </Col>
            <Col span={12}>
              <Truong<GiaTriBuoc> ten="tinhTheoNgayLamViec" label="Theo ngày làm việc">
                {(o) => <Switch checked={!!o.value} onChange={o.onChange} />}
              </Truong>
            </Col>
          </Row>

          <Row gutter={8}>
            <Col span={8}>
              <Truong<GiaTriBuoc> ten="batBuocNhapYKien" label="Bắt buộc ý kiến">
                {(o) => <Switch checked={!!o.value} onChange={o.onChange} />}
              </Truong>
            </Col>
            <Col span={8}>
              <Truong<GiaTriBuoc> ten="batBuocDinhKem" label="Bắt buộc tệp">
                {(o) => <Switch checked={!!o.value} onChange={o.onChange} />}
              </Truong>
            </Col>
            <Col span={8}>
              <Truong<GiaTriBuoc> ten="choPhepThuHoi" label="Cho thu hồi">
                {(o) => <Switch checked={!!o.value} onChange={o.onChange} />}
              </Truong>
            </Col>
          </Row>

          <Row gutter={8}>
            <Col span={12}>
              <Truong<GiaTriBuoc> ten="laBuocBatDau" label="Bước bắt đầu">
                {(o) => <Switch checked={!!o.value} onChange={o.onChange} />}
              </Truong>
            </Col>
            <Col span={12}>
              <Truong<GiaTriBuoc> ten="laBuocKetThuc" label="Bước kết thúc">
                {(o) => <Switch checked={!!o.value} onChange={o.onChange} />}
              </Truong>
            </Col>
          </Row>

          <Truong<GiaTriBuoc> ten="moTaHuongDan" label="Hướng dẫn xử lý">
            {(o) => <Input.TextArea {...o} value={o.value as string} rows={2} />}
          </Truong>
        </BieuMau>

        <Card size="small" title="Tác nhân xử lý" style={{ marginBottom: 12 }}>
          <List
            size="small"
            dataSource={buocDangChon?.tacNhan ?? []}
            locale={{ emptyText: 'Chưa cấu hình tác nhân — bước sẽ không hợp lệ.' }}
            renderItem={(tn) => (
              <List.Item>
                <Space wrap>
                  <Tag>{LOAI_TAC_NHAN.find((l) => l.value === tn.loaiTacNhan)?.label}</Tag>
                  <Typography.Text strong>{tn.thamChieuMa ?? tn.thamChieuId}</Typography.Text>
                  <Tag color="blue">
                    {QUY_TAC.find((q) => q.value === tn.quyTacXuLy)?.label}
                    {tn.tyLeDongThuan ? ` (${tn.tyLeDongThuan}%)` : ''}
                  </Tag>
                </Space>
              </List.Item>
            )}
          />
        </Card>

        <Card size="small" title="Trường hợp chuyển tiếp">
          <List
            size="small"
            dataSource={buocDangChon?.truongHop ?? []}
            locale={{ emptyText: 'Chưa có nhánh chuyển tiếp.' }}
            renderItem={(th) => {
              const buocTiep = soDo?.danhSachBuoc.find((b) => b.id === th.buocTiepTheoId);
              return (
                <List.Item>
                  <Space direction="vertical" size={2} style={{ width: '100%' }}>
                    <Space wrap>
                      <Tag color={th.ma === 'DAT' ? 'green' : th.ma === 'KHONG_DAT' ? 'red' : 'default'}>
                        {th.ma}
                      </Tag>
                      <Typography.Text strong>{th.ten}</Typography.Text>
                      {th.laMacDinh && <Tag color="blue">mặc định</Tag>}
                    </Space>
                    <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                      → {buocTiep?.ten ?? 'Kết thúc quy trình'}
                      {th.dieuKien ? ` • có điều kiện` : ''}
                      {th.hanhDong.length > 0 ? ` • ${th.hanhDong.join(', ')}` : ''}
                    </Typography.Text>
                  </Space>
                </List.Item>
              );
            }}
          />
        </Card>
      </Drawer>
    </Card>
  );
}
