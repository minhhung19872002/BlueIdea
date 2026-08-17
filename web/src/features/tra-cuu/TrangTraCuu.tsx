import { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Button, Card, Col, Collapse, DatePicker, Input, InputNumber, Row, Select, Space } from 'antd';
import { ClearOutlined, SearchOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';

import { apiDonVi, apiDotDeNghi, apiLinhVuc, apiSangKien } from '@/api/endpoints';
import { BangSangKien } from '@/features/sang-kien/BangSangKien';
import { KhoiLoi, KhoiRong } from '@/components/ThanhPhanChung';

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

  const { data: cacDot } = useQuery({ queryKey: ['dot-chon'], queryFn: apiDotDeNghi.chon });
  const { data: cacLinhVuc } = useQuery({ queryKey: ['linh-vuc-chon'], queryFn: apiLinhVuc.chon });
  const { data: cacDonVi } = useQuery({ queryKey: ['don-vi-chon'], queryFn: apiDonVi.chon });

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
      <Input.Search
        size="large"
        placeholder="Nhập tên sáng kiến, mã hồ sơ hoặc tên tác giả (gõ không dấu vẫn ra kết quả)"
        defaultValue={thamSo.tuKhoa}
        allowClear
        enterButton={<SearchOutlined />}
        onSearch={(v) => dat('tuKhoa', v)}
        style={{ marginBottom: 12 }}
      />

      <Collapse
        style={{ marginBottom: 12 }}
        items={[
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
          onDoiTrang={(t, s) => {
            setTrang(t);
            setSoDong(s);
          }}
        />
      )}
    </Card>
  );
}
