import { useCallback, useState } from 'react';
import { useLocation, useSearchParams } from 'react-router-dom';
import { App, Alert, Button, Card, Col, Input, Modal, Row, Select, Space } from 'antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { LoiApi } from '@/api/client';
import { apiDotDeNghi, apiLinhVuc, apiSangKien, apiXuLy } from '@/api/endpoints';
import { BangSangKien } from '@/features/sang-kien/BangSangKien';
import { KhoiLoi, KhoiRong } from '@/components/ThanhPhanChung';
import { ThanhBoLocYeuThich } from '@/components/BoLocYeuThich';

const TRANG_THAI = [
  { value: 'DA_NOP', label: 'Đã nộp (chờ tiếp nhận)' },
  { value: 'DANG_XU_LY', label: 'Đang xử lý' },
  { value: 'YEU_CAU_BO_SUNG', label: 'Yêu cầu bổ sung' },
  { value: 'DA_PHE_DUYET', label: 'Đã phê duyệt' },
  { value: 'KHONG_DAT', label: 'Không đạt' },
];

/**
 * Chức năng 27–28 — Danh sách hồ sơ chờ tiếp nhận và việc cần xử lý.
 * Cùng một màn hình, khác bộ lọc mặc định theo đường dẫn.
 */
export default function TrangDanhSachXuLy() {
  const viTri = useLocation();
  const laTiepNhan = viTri.pathname.startsWith('/tiep-nhan');
  const { message } = App.useApp();
  const queryClient = useQueryClient();

  const [thamSoUrl, datThamSoUrl] = useSearchParams();
  const [trang, setTrang] = useState(1);
  const [soDong, setSoDong] = useState(20);
  const [daChon, setDaChon] = useState<string[]>([]);
  const [moHangLoat, setMoHangLoat] = useState(false);
  const [yKienHangLoat, setYKienHangLoat] = useState('');

  const thamSo = {
    trang,
    soDong,
    tuKhoa: thamSoUrl.get('tuKhoa') ?? undefined,
    trangThaiTong: laTiepNhan ? undefined : (thamSoUrl.get('trangThaiTong') ?? undefined),
    dotDeNghiId: thamSoUrl.get('dotDeNghiId') ?? undefined,
    linhVucId: thamSoUrl.get('linhVucId') ?? undefined,
    chiQuaHan: thamSoUrl.get('chiQuaHan') === 'true' ? true : undefined,
    sapXep: thamSoUrl.get('sapXep') ?? undefined,
    huong: thamSoUrl.get('huong') ?? undefined,
  };

  // Hai màn hình, hai endpoint: hàng chờ tiếp nhận là chức năng riêng nên có quyền riêng
  // (TIEP_NHAN.XEM), không dùng chung SANG_KIEN.XEM với danh sách việc cần xử lý.
  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['sang-kien', laTiepNhan ? 'tiep-nhan' : 'xu-ly', thamSo],
    queryFn: () => (laTiepNhan ? apiSangKien.choTiepNhan(thamSo) : apiSangKien.danhSach(thamSo)),
  });

  const { data: cacDot } = useQuery({ queryKey: ['dot-chon'], queryFn: apiDotDeNghi.chon });
  const { data: cacLinhVuc } = useQuery({ queryKey: ['linh-vuc-chon'], queryFn: apiLinhVuc.chon });

  // Hành động khả dụng của hồ sơ đầu tiên được chọn — dùng làm hành động cho cả lô.
  const { data: hanhDongMau } = useQuery({
    queryKey: ['hanh-dong-mau', daChon[0]],
    queryFn: () => apiSangKien.hanhDong(daChon[0]),
    enabled: daChon.length > 0,
  });

  const xuLyHangLoat = useMutation({
    mutationFn: (truongHopId: string) =>
      apiXuLy.thucThiHangLoat({ sangKienIds: daChon, truongHopId, yKien: yKienHangLoat }),
    onSuccess: (ketQua) => {
      message.success(`Đã xử lý ${ketQua.thanhCong}/${ketQua.tongSo} hồ sơ`);
      if (ketQua.chiTietLoi.length > 0) {
        Modal.warning({
          title: 'Một số hồ sơ không xử lý được',
          content: (
            <ul style={{ paddingLeft: 18 }}>
              {ketQua.chiTietLoi.map((l) => (
                <li key={l}>{l}</li>
              ))}
            </ul>
          ),
        });
      }
      setDaChon([]);
      setMoHangLoat(false);
      void queryClient.invalidateQueries({ queryKey: ['sang-kien'] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Xử lý hàng loạt lỗi.'),
  });

  function datLoc(khoa: string, giaTri?: string) {
    const moi = new URLSearchParams(thamSoUrl);
    if (giaTri) moi.set(khoa, giaTri);
    else moi.delete(khoa);
    datThamSoUrl(moi);
    setTrang(1);
  }

  // Áp một bộ lọc đã lưu = thay toàn bộ chuỗi query của màn hình.
  const apBoLocDaLuu = useCallback(
    (chuoiThamSo: string) => {
      datThamSoUrl(new URLSearchParams(chuoiThamSo));
      setTrang(1);
    },
    [datThamSoUrl],
  );

  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  return (
    <Card title={laTiepNhan ? 'Hồ sơ chờ tiếp nhận' : 'Việc cần xử lý'}>
      <ThanhBoLocYeuThich
        manHinh={laTiepNhan ? 'TIEP_NHAN' : 'XU_LY'}
        thamSoHienTai={thamSoUrl.toString()}
        onApDung={apBoLocDaLuu}
      />

      <Row gutter={[8, 8]} style={{ marginBottom: 12 }}>
        <Col xs={24} md={8}>
          <Input.Search
            placeholder="Tìm theo tên, mã hồ sơ, tác giả"
            defaultValue={thamSo.tuKhoa}
            allowClear
            onSearch={(v) => datLoc('tuKhoa', v)}
          />
        </Col>
        {/* Màn hình tiếp nhận luôn là hàng chờ "đã nộp" — máy chủ ép trạng thái, nên ô lọc
            trạng thái ở đây chỉ gây hiểu nhầm là chọn được. */}
        {!laTiepNhan && (
          <Col xs={12} md={5}>
            <Select
              style={{ width: '100%' }}
              placeholder="Trạng thái"
              allowClear
              value={thamSo.trangThaiTong}
              options={TRANG_THAI}
              onChange={(v) => datLoc('trangThaiTong', v)}
            />
          </Col>
        )}
        <Col xs={12} md={5}>
          <Select
            style={{ width: '100%' }}
            placeholder="Lĩnh vực"
            allowClear
            value={thamSo.linhVucId}
            options={(cacLinhVuc ?? []).map((x) => ({ value: x.id, label: x.ten }))}
            onChange={(v) => datLoc('linhVucId', v)}
          />
        </Col>
        <Col xs={12} md={4}>
          <Select
            style={{ width: '100%' }}
            placeholder="Đợt"
            allowClear
            value={thamSo.dotDeNghiId}
            options={(cacDot ?? []).map((x) => ({ value: x.id, label: x.ten }))}
            onChange={(v) => datLoc('dotDeNghiId', v)}
          />
        </Col>
        <Col xs={12} md={2}>
          <Select
            style={{ width: '100%' }}
            placeholder="Quá hạn"
            allowClear
            value={thamSo.chiQuaHan ? 'true' : undefined}
            options={[{ value: 'true', label: 'Quá hạn' }]}
            onChange={(v) => datLoc('chiQuaHan', v)}
          />
        </Col>
      </Row>

      {daChon.length > 0 && (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 12 }}
          message={`Đã chọn ${daChon.length} hồ sơ`}
          action={
            <Space>
              <Button size="small" type="primary" onClick={() => setMoHangLoat(true)}>
                Xử lý hàng loạt
              </Button>
              <Button size="small" onClick={() => setDaChon([])}>
                Bỏ chọn
              </Button>
            </Space>
          }
        />
      )}

      {!isLoading && (data?.tongSo ?? 0) === 0 ? (
        <KhoiRong moTa="Không có hồ sơ nào khớp bộ lọc hiện tại." />
      ) : (
        <BangSangKien
          duLieu={data?.duLieu ?? []}
          tongSo={data?.tongSo ?? 0}
          trang={trang}
          soDong={soDong}
          dangTai={isLoading}
          thamSoXuat={thamSo}
          chonNhieu={{ khoaDaChon: daChon, onDoiChon: setDaChon }}
          chonCot
          onDoiTrang={(t, s) => {
            setTrang(t);
            setSoDong(s);
          }}
          onDoiSapXep={(cot, chieu) => {
            const moi = new URLSearchParams(thamSoUrl);

            if (cot && chieu) {
              moi.set('sapXep', cot);
              moi.set('huong', chieu);
            } else {
              moi.delete('sapXep');
              moi.delete('huong');
            }

            datThamSoUrl(moi);
            setTrang(1);
          }}
        />
      )}

      <Modal
        open={moHangLoat}
        title={`Xử lý hàng loạt ${daChon.length} hồ sơ`}
        footer={null}
        onCancel={() => setMoHangLoat(false)}
      >
        <Alert
          type="warning"
          showIcon
          style={{ marginBottom: 12 }}
          message="Hồ sơ không đủ điều kiện sẽ bị bỏ qua và báo lại chi tiết."
        />

        <Input.TextArea
          rows={3}
          placeholder="Ý kiến xử lý áp dụng cho tất cả hồ sơ"
          value={yKienHangLoat}
          onChange={(e) => setYKienHangLoat(e.target.value)}
          style={{ marginBottom: 12 }}
        />

        <Space wrap>
          {(hanhDongMau ?? []).map((hd) => (
            <Button
              key={hd.truongHopId}
              type={hd.mauNut === 'primary' ? 'primary' : 'default'}
              danger={hd.mauNut === 'danger'}
              loading={xuLyHangLoat.isPending}
              onClick={() => xuLyHangLoat.mutate(hd.truongHopId)}
            >
              {hd.ten}
            </Button>
          ))}
        </Space>
      </Modal>
    </Card>
  );
}
