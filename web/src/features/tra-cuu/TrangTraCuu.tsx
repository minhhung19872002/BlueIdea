import { useCallback, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import {
  App,
  AutoComplete,
  Button,
  Card,
  Col,
  Collapse,
  DatePicker,
  Input,
  InputNumber,
  Row,
  Select,
  Space,
  Table,
  Tag,
  Typography,
} from 'antd';
import { ClearOutlined, SearchOutlined } from '@ant-design/icons';
import { useMutation, useQuery } from '@tanstack/react-query';

import { LoiApi } from '@/api/client';
import {
  apiDonVi,
  apiDotDeNghi,
  apiLinhVuc,
  apiSangKien,
  type KetQuaTimNguNghia,
} from '@/api/endpoints';
import { BangSangKien } from '@/features/sang-kien/BangSangKien';
import { ThanhBoLocYeuThich } from '@/components/BoLocYeuThich';
import { KhoiLoi, KhoiRong } from '@/components/ThanhPhanChung';

const NHAN_LOAI_GOI_Y: Record<string, string> = {
  MA_HO_SO: 'Mã hồ sơ',
  SANG_KIEN: 'Sáng kiến',
  TAC_GIA: 'Tác giả',
};

/** Chức năng 37 — Tra cứu, tìm kiếm nâng cao. */
export default function TrangTraCuu() {
  const [thamSoUrl, datThamSoUrl] = useSearchParams();
  const [trang, setTrang] = useState(1);
  const [soDong, setSoDong] = useState(20);

  const doc = (khoa: string) => thamSoUrl.get(khoa) ?? undefined;
  const docSo = (khoa: string) => {
    const v = thamSoUrl.get(khoa);
    return v ? Number(v) : undefined;
  };

  const thamSo = {
    trang,
    soDong,
    tuKhoa: doc('tuKhoa'),
    dotDeNghiId: doc('dotDeNghiId'),
    linhVucId: doc('linhVucId'),
    donViId: doc('donViId'),
    trangThaiTong: doc('trangThaiTong'),
    nam: docSo('nam'),
    sapXep: doc('sapXep'),
    huong: doc('huong'),
    ketQua: doc('ketQua'),
    diemTu: docSo('diemTu'),
    diemDen: docSo('diemDen'),
    trungLapTu: docSo('trungLapTu'),
    trungLapDen: docSo('trungLapDen'),
    ngayNopTu: doc('ngayNopTu'),
    ngayNopDen: doc('ngayNopDen'),
  };

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['tra-cuu', thamSo],
    queryFn: () => apiSangKien.danhSach(thamSo),
  });

  // Ô tìm kiếm giữ giá trị đang gõ riêng: gọi API gợi ý theo từng phím, còn tham số URL chỉ đổi
  // khi người dùng thật sự bấm tìm — nếu không mỗi phím gõ sẽ là một lần chạy truy vấn đầy đủ.
  const [dangGo, setDangGo] = useState(thamSoUrl.get('tuKhoa') ?? '');

  const { data: goiY } = useQuery({
    queryKey: ['goi-y-tim-kiem', dangGo],
    queryFn: () => apiSangKien.goiY(dangGo),
    enabled: dangGo.trim().length >= 2,
    staleTime: 30_000,
  });

  const { data: cacDot } = useQuery({ queryKey: ['dot-chon'], queryFn: apiDotDeNghi.chon });
  const { data: cacLinhVuc } = useQuery({ queryKey: ['linh-vuc-chon'], queryFn: apiLinhVuc.chon });
  const { data: cacDonVi } = useQuery({ queryKey: ['don-vi-chon'], queryFn: apiDonVi.chon });

  const apBoLocDaLuu = useCallback(
    (chuoi: string) => {
      const moi = new URLSearchParams(chuoi);
      datThamSoUrl(moi);
      setDangGo(moi.get('tuKhoa') ?? '');
      setTrang(1);
    },
    [datThamSoUrl],
  );

  function dat(khoa: string, giaTri?: string | number | null) {
    const moi = new URLSearchParams(thamSoUrl);
    if (giaTri !== undefined && giaTri !== null && giaTri !== '') {
      moi.set(khoa, String(giaTri));
    } else {
      moi.delete(khoa);
    }
    datThamSoUrl(moi);
    setTrang(1);
  }

  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  return (
    <Card title="Tra cứu sáng kiến">
      <ThanhBoLocYeuThich
        manHinh="TRA_CUU"
        thamSoHienTai={thamSoUrl.toString()}
        onApDung={apBoLocDaLuu}
      />

      <AutoComplete
        style={{ width: '100%', marginBottom: 12 }}
        value={dangGo}
        onChange={setDangGo}
        onSelect={(v: string) => dat('tuKhoa', v)}
        options={(goiY ?? []).map((x) => ({
          value: x.giaTri,
          label: (
            <Space size={6}>
              <Tag color={x.loai === 'TAC_GIA' ? 'purple' : 'blue'}>
                {NHAN_LOAI_GOI_Y[x.loai] ?? x.loai}
              </Tag>
              <span>{x.giaTri}</span>
              {x.moTa && (
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  {x.moTa}
                </Typography.Text>
              )}
            </Space>
          ),
        }))}
      >
        <Input.Search
          size="large"
          placeholder="Nhập tên sáng kiến, mã hồ sơ hoặc tên tác giả (gõ không dấu vẫn ra kết quả)"
          allowClear
          enterButton={<SearchOutlined />}
          onSearch={(v) => dat('tuKhoa', v)}
        />
      </AutoComplete>

      <Collapse
        style={{ marginBottom: 12 }}
        items={[
          {
            key: 'ngu-nghia',
            label: 'Tìm theo ý nghĩa (không cần trùng từ khoá)',
            children: <KhoiTimNguNghia cacLinhVuc={cacLinhVuc ?? []} />,
          },
          {
            key: 'nang-cao',
            label: 'Tìm kiếm nâng cao',
            children: (
              <Row gutter={[8, 8]}>
                <Col xs={24} md={8}>
                  <Select
                    style={{ width: '100%' }}
                    placeholder="Đợt đề nghị"
                    allowClear
                    value={thamSo.dotDeNghiId}
                    options={(cacDot ?? []).map((x) => ({ value: x.id, label: x.ten }))}
                    onChange={(v) => dat('dotDeNghiId', v)}
                  />
                </Col>
                <Col xs={24} md={8}>
                  <Select
                    style={{ width: '100%' }}
                    placeholder="Lĩnh vực"
                    allowClear
                    value={thamSo.linhVucId}
                    options={(cacLinhVuc ?? []).map((x) => ({ value: x.id, label: x.ten }))}
                    onChange={(v) => dat('linhVucId', v)}
                  />
                </Col>
                <Col xs={24} md={8}>
                  <Select
                    style={{ width: '100%' }}
                    placeholder="Đơn vị"
                    allowClear
                    showSearch
                    optionFilterProp="label"
                    value={thamSo.donViId}
                    options={(cacDonVi ?? []).map((x) => ({ value: x.id, label: x.ten }))}
                    onChange={(v) => dat('donViId', v)}
                  />
                </Col>

                <Col xs={12} md={6}>
                  <Select
                    style={{ width: '100%' }}
                    placeholder="Kết quả"
                    allowClear
                    value={thamSo.ketQua}
                    options={[
                      { value: 'DAT', label: 'Đạt' },
                      { value: 'KHONG_DAT', label: 'Không đạt' },
                    ]}
                    onChange={(v) => dat('ketQua', v)}
                  />
                </Col>
                <Col xs={6} md={4}>
                  <InputNumber
                    style={{ width: '100%' }}
                    placeholder="Điểm từ"
                    min={0}
                    value={thamSo.diemTu}
                    onChange={(v) => dat('diemTu', v)}
                  />
                </Col>
                <Col xs={6} md={4}>
                  <InputNumber
                    style={{ width: '100%' }}
                    placeholder="Điểm đến"
                    min={0}
                    value={thamSo.diemDen}
                    onChange={(v) => dat('diemDen', v)}
                  />
                </Col>
                <Col xs={6} md={4}>
                  <InputNumber
                    style={{ width: '100%' }}
                    placeholder="Trùng lặp từ (%)"
                    min={0}
                    max={100}
                    value={thamSo.trungLapTu}
                    onChange={(v) => dat('trungLapTu', v)}
                  />
                </Col>
                <Col xs={6} md={6}>
                  <DatePicker.RangePicker
                    style={{ width: '100%' }}
                    format="DD/MM/YYYY"
                    placeholder={['Nộp từ', 'Nộp đến']}
                    onChange={(khoang) => {
                      dat('ngayNopTu', khoang?.[0]?.format('YYYY-MM-DD'));
                      dat('ngayNopDen', khoang?.[1]?.format('YYYY-MM-DD'));
                    }}
                  />
                </Col>

                <Col span={24}>
                  <Space>
                    <Button icon={<ClearOutlined />} onClick={() => datThamSoUrl(new URLSearchParams())}>
                      Xóa bộ lọc
                    </Button>
                    <Button
                      onClick={() => {
                        void navigator.clipboard.writeText(window.location.href);
                      }}
                    >
                      Sao chép liên kết truy vấn
                    </Button>
                  </Space>
                </Col>
              </Row>
            ),
          },
        ]}
      />

      {!isLoading && (data?.tongSo ?? 0) === 0 ? (
        <KhoiRong moTa="Không tìm thấy sáng kiến nào khớp điều kiện tìm kiếm." />
      ) : (
        <BangSangKien
          duLieu={data?.duLieu ?? []}
          tongSo={data?.tongSo ?? 0}
          trang={trang}
          soDong={soDong}
          dangTai={isLoading}
          thamSoXuat={thamSo}
          tenTepXuat="ket-qua-tra-cuu.xlsx"
          tuKhoaToDam={thamSo.tuKhoa}
          chonCot
          onDoiSapXep={(cot, chieu) => {
            // Giữ trong URL như mọi bộ lọc khác: liên kết chia sẻ phải mở ra đúng thứ tự
            // người gửi đang nhìn thấy.
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
          onDoiTrang={(t, s) => {
            setTrang(t);
            setSoDong(s);
          }}
        />
      )}
    </Card>
  );
}

// ---------------------------------------------------------------------------

/**
 * Chức năng 37 — Tìm ngữ nghĩa.
 *
 * Khác ô tìm kiếm phía trên: câu hỏi được so bằng vector nội bộ nên hồ sơ không chứa đúng từ
 * khoá vẫn ra, miễn nội dung nói về cùng một việc. Kết quả kèm độ tương đồng và đoạn khớp nhất
 * để người dùng tự đánh giá — chứ không trộn lẫn vào bảng kết quả tìm từ khoá.
 */
function KhoiTimNguNghia({ cacLinhVuc }: { cacLinhVuc: { id: string; ten: string }[] }) {
  const { message } = App.useApp();

  const [cauHoi, setCauHoi] = useState('');
  const [linhVucId, setLinhVucId] = useState<string | undefined>();
  const [nam, setNam] = useState<number | undefined>();
  const [ketQua, setKetQua] = useState<KetQuaTimNguNghia[] | null>(null);

  const tim = useMutation({
    mutationFn: () => apiSangKien.timNguNghia({ cauHoi, soKetQua: 20, linhVucId, nam }),
    onSuccess: (kq) => {
      setKetQua(kq);
      if (kq.length === 0) message.info('Không tìm thấy sáng kiến nào gần với nội dung câu hỏi.');
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không tìm được.'),
  });

  return (
    <>
      <Row gutter={[8, 8]}>
        <Col xs={24} md={12}>
          <Input.TextArea
            rows={2}
            value={cauHoi}
            placeholder="Mô tả nội dung cần tìm, ví dụ: giải pháp tiết kiệm điện chiếu sáng công cộng"
            onChange={(e) => setCauHoi(e.target.value)}
          />
        </Col>
        <Col xs={12} md={5}>
          <Select
            style={{ width: '100%' }}
            allowClear
            placeholder="Lĩnh vực"
            value={linhVucId}
            options={cacLinhVuc.map((x) => ({ value: x.id, label: x.ten }))}
            onChange={setLinhVucId}
          />
        </Col>
        <Col xs={12} md={4}>
          <InputNumber
            style={{ width: '100%' }}
            min={2000}
            max={2100}
            placeholder="Năm công nhận"
            value={nam}
            onChange={(v) => setNam(v ?? undefined)}
          />
        </Col>
        <Col xs={24} md={3}>
          <Button
            type="primary"
            block
            icon={<SearchOutlined />}
            loading={tim.isPending}
            disabled={cauHoi.trim().length < 3}
            onClick={() => tim.mutate()}
          >
            Tìm
          </Button>
        </Col>
      </Row>

      {ketQua && ketQua.length > 0 && (
        <Table<KetQuaTimNguNghia>
          rowKey="sangKienId"
          size="small"
          style={{ marginTop: 12 }}
          dataSource={ketQua}
          pagination={false}
          scroll={{ x: 800 }}
          columns={[
            { title: 'Mã hồ sơ', dataIndex: 'maHoSo', width: 130 },
            {
              title: 'Sáng kiến',
              dataIndex: 'tenSangKien',
              render: (v: string, dong) => (
                <Space direction="vertical" size={0}>
                  <Link to={`/sang-kien/${dong.sangKienId}`}>{v}</Link>
                  <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                    {[dong.tenTacGiaChinh, dong.tenDonVi, dong.tenLinhVuc]
                      .filter(Boolean)
                      .join(' — ')}
                  </Typography.Text>
                </Space>
              ),
            },
            {
              title: 'Độ tương đồng',
              dataIndex: 'doTuongDong',
              width: 130,
              align: 'right',
              render: (v: number) => (
                <Tag color={v >= 70 ? 'success' : v >= 40 ? 'processing' : 'default'}>
                  {v.toFixed(1)}%
                </Tag>
              ),
            },
            {
              title: 'Đoạn khớp nhất',
              dataIndex: 'doanKhopNhat',
              width: 320,
              responsive: ['lg'],
              render: (v: string) => (
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  {v}
                </Typography.Text>
              ),
            },
          ]}
        />
      )}
    </>
  );
}
