import { useMemo, useRef, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  App,
  Alert,
  Badge,
  Button,
  Card,
  Col,
  Descriptions,
  Dropdown,
  Input,
  Modal,
  Progress,
  Row,
  Space,
  Statistic,
  Table,
  Tabs,
  Tag,
  Timeline,
  Tooltip,
  Typography,
} from 'antd';
import {
  CalculatorOutlined,
  DownloadOutlined,
  EditOutlined,
  FilePdfOutlined,
  FileTextOutlined,
  ReloadOutlined,
  RollbackOutlined,
  TeamOutlined,
  UndoOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';

import { LoiApi, taiTep } from '@/api/client';
import {
  apiDanhGia,
  apiHoiDong,
  apiNhapXuat,
  apiSangKien,
  apiXuLy,
  type HanhDongKhaDung,
  type SangKienChiTiet,
} from '@/api/endpoints';
import { useAuthStore } from '@/app/store/authStore';
import {
  HienThiHan,
  KhoiDangTai,
  KhoiLoi,
  NhanTrangThai,
  NhanTrungLap,
  ngayGio,
} from '@/components/ThanhPhanChung';
import { XemTruocVanBan } from '@/components/ONoiDungDai';
import { BieuMau, Truong, useBieuMau } from '@/components/bieu-mau/BieuMau';
import { tuyChon } from '@/components/bieu-mau/luat';
import { DatePicker, Select, Switch } from 'antd';
import dayjs from 'dayjs';

/**
 * Luật kiểm tra ô ý kiến xử lý.
 *
 * Ràng buộc bật/tắt theo từng bước quy trình nên đọc qua hàm thay vì hằng số: cùng một form được
 * dùng lại cho mọi nhánh chuyển bước, mỗi nhánh có yêu cầu ý kiến khác nhau.
 *
 * Bước có bật "bắt buộc nhập ý kiến" thì không cho gửi rỗng — ý kiến xử lý là căn cứ để giải
 * trình vì sao hồ sơ được duyệt hay bị loại, thiếu nó thì hồ sơ đi tiếp mà không ai truy được lý do.
 */
function taoLuatXuLy(batBuocYKien: () => boolean) {
  return z
    .object({ yKien: tuyChon(2000) })
    .superRefine((giaTri, ctx) => {
      if (batBuocYKien() && !giaTri.yKien?.trim()) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          path: ['yKien'],
          message: 'Bước này bắt buộc nhập ý kiến xử lý.',
        });
      }
    });
}

type GiaTriXuLy = { yKien?: string };

export default function TrangChiTietHoSo() {
  const { id = '' } = useParams<{ id: string }>();
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [hanhDongDangChon, setHanhDongDangChon] = useState<HanhDongKhaDung | null>(null);
  /*
   * Ràng buộc "bắt buộc ý kiến" thay đổi theo nhánh người dùng bấm, nhưng resolver của form thì
   * chỉ nhận một lần. Đọc qua ref để luật luôn thấy nhánh đang chọn mà form không phải dựng lại.
   */
  const refBatBuocYKien = useRef(false);
  refBatBuocYKien.current = !!hanhDongDangChon?.batBuocNhapYKien;
  const formXuLy = useBieuMau<GiaTriXuLy>(
    useMemo(() => taoLuatXuLy(() => refBatBuocYKien.current), []),
    { yKien: '' },
  );
  const [moPhanCong, setMoPhanCong] = useState(false);

  const duocThuHoi = useAuthStore((st) => st.coQuyen('XU_LY.THU_HOI'));
  const duocPhanCong = useAuthStore((st) => st.coQuyen('DANH_GIA.PHAN_CONG'));
  const duocTongHop = useAuthStore((st) => st.coQuyen('DANH_GIA.TONG_HOP'));

  const chiTiet = useQuery({
    queryKey: ['sang-kien', id],
    queryFn: () => apiSangKien.chiTiet(id),
  });

  const hanhDong = useQuery({
    queryKey: ['sang-kien', id, 'hanh-dong'],
    queryFn: () => apiSangKien.hanhDong(id),
  });

  const tienDo = useQuery({
    queryKey: ['sang-kien', id, 'tien-do'],
    queryFn: () => apiSangKien.tienDo(id),
  });

  const lichSu = useQuery({
    queryKey: ['sang-kien', id, 'lich-su'],
    queryFn: () => apiSangKien.lichSu(id),
  });

  const trungLap = useQuery({
    queryKey: ['sang-kien', id, 'trung-lap'],
    queryFn: () => apiSangKien.trungLap(id),
  });

  const thucThi = useMutation({
    mutationFn: (duLieu: { truongHopId: string; yKien?: string }) =>
      apiXuLy.thucThi({
        sangKienId: id,
        truongHopId: duLieu.truongHopId,
        yKien: duLieu.yKien,
        phienBanHoSo: chiTiet.data?.phienBan,
      }),
    onSuccess: (ketQua) => {
      message.success(ketQua.thongBao);
      setHanhDongDangChon(null);
      formXuLy.reset({ yKien: '' });
      void queryClient.invalidateQueries({ queryKey: ['sang-kien', id] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Xử lý thất bại.'),
  });

  /** Chức năng 29 — thu hồi bước vừa xử lý khi bấm nhầm nhánh. */
  const thuHoi = useMutation({
    mutationFn: (lyDo: string) => apiXuLy.thuHoi(id, lyDo),
    onSuccess: () => {
      message.success('Đã thu hồi bước xử lý');
      void queryClient.invalidateQueries({ queryKey: ['sang-kien', id] });
    },
    onError: (loi) =>
      modal.error({
        title: 'Không thu hồi được',
        content: loi instanceof LoiApi ? loi.message : 'Bước hiện tại không cho phép thu hồi.',
      }),
  });

  /** Chức năng 32 — tổng hợp điểm của hội đồng cho hồ sơ này. */
  const tongHop = useMutation({
    mutationFn: (hoiDongId: string) => apiDanhGia.tongHop(id, hoiDongId),
    onSuccess: (ketQua) => {
      void queryClient.invalidateQueries({ queryKey: ['sang-kien', id] });

      modal.info({
        title: 'Kết quả tổng hợp điểm',
        width: 520,
        content: (
          <Descriptions bordered size="small" column={1} style={{ marginTop: 12 }}>
            <Descriptions.Item label="Số phiếu">
              {ketQua.soPhieuSuDung}/{ketQua.soPhieu} phiếu được dùng để tính
            </Descriptions.Item>
            <Descriptions.Item label="Cao nhất / thấp nhất">
              {ketQua.diemCaoNhat.toFixed(2)} / {ketQua.diemThapNhat.toFixed(2)}
            </Descriptions.Item>
            <Descriptions.Item label="Trung bình">
              {ketQua.diemTrungBinh.toFixed(2)}
            </Descriptions.Item>
            <Descriptions.Item label="Điểm cuối cùng">
              <Typography.Text strong>{ketQua.diemCuoiCung.toFixed(2)}</Typography.Text>
            </Descriptions.Item>
            <Descriptions.Item label="Kết quả">
              {ketQua.dat ? (
                <Tag color="success">Đạt — {ketQua.tenMucCongNhan ?? 'chưa gán mức'}</Tag>
              ) : (
                <Tag color="error">Chưa đạt</Tag>
              )}
            </Descriptions.Item>
            {ketQua.canhBao.length > 0 && (
              <Descriptions.Item label="Cảnh báo">
                <ul style={{ paddingLeft: 16, marginBottom: 0 }}>
                  {ketQua.canhBao.map((c) => (
                    <li key={c}>{c}</li>
                  ))}
                </ul>
              </Descriptions.Item>
            )}
          </Descriptions>
        ),
      });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không tổng hợp được.'),
  });

  const chayLaiTrungLap = useMutation({
    mutationFn: () => apiSangKien.chayLaiTrungLap(id),
    onSuccess: () => {
      message.success('Đã hoàn tất kiểm tra trùng lặp');
      void queryClient.invalidateQueries({ queryKey: ['sang-kien', id] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Kiểm tra thất bại.'),
  });

  const rutHoSo = useMutation({
    mutationFn: (lyDo: string) => apiSangKien.rut(id, lyDo),
    onSuccess: () => {
      message.success('Đã rút hồ sơ');
      void queryClient.invalidateQueries({ queryKey: ['sang-kien', id] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không rút được hồ sơ.'),
  });

  if (chiTiet.isLoading) return <KhoiDangTai />;
  if (chiTiet.error) return <KhoiLoi loi={chiTiet.error} thuLai={chiTiet.refetch} />;

  const hs = chiTiet.data!;

  function moHopThoaiThuHoi() {
    let lyDo = '';

    modal.confirm({
      title: 'Thu hồi bước xử lý',
      content: (
        <div>
          <p>
            Hồ sơ quay lại bước trước đó. Chỉ thu hồi được khi bước hiện tại cho phép và chưa ai
            xử lý tiếp.
          </p>
          <Input.TextArea
            rows={3}
            placeholder="Lý do thu hồi (bắt buộc)"
            onChange={(e) => {
              lyDo = e.target.value;
            }}
          />
        </div>
      ),
      okText: 'Thu hồi',
      okButtonProps: { danger: true },
      cancelText: 'Huỷ',
      onOk: () => {
        if (!lyDo.trim()) {
          message.warning('Vui lòng nhập lý do thu hồi.');
          return Promise.reject(new Error('thieu-ly-do'));
        }

        return thuHoi.mutateAsync(lyDo.trim());
      },
    });
  }

  function moHopThoaiTongHop() {
    let hoiDongId: string | undefined;

    modal.confirm({
      title: 'Tổng hợp điểm của hội đồng',
      width: 520,
      content: (
        <div>
          <p>Chọn hội đồng đã chấm hồ sơ này để tính điểm cuối cùng theo bộ tiêu chí của đợt.</p>
          <ChonHoiDong onChon={(v) => (hoiDongId = v)} />
        </div>
      ),
      okText: 'Tổng hợp',
      cancelText: 'Huỷ',
      onOk: () => {
        if (!hoiDongId) {
          message.warning('Vui lòng chọn hội đồng.');
          return Promise.reject(new Error('thieu-hoi-dong'));
        }

        return tongHop.mutateAsync(hoiDongId);
      },
    });
  }

  return (
    <div>
      <Card
        title={
          <Space wrap>
            <Typography.Text strong>{hs.maHoSo}</Typography.Text>
            <NhanTrangThai trangThai={hs.trangThaiTong} />
            {hs.tenBuocHienTai && <Tag color="blue">{hs.tenBuocHienTai}</Tag>}
          </Space>
        }
        extra={
          <Space wrap className="khong-in">
            {/* Hồ sơ chưa nộp thì chưa có gì để xác nhận tiếp nhận. */}
            {hs.trangThaiTong !== 'NHAP' && (
              <Button
                icon={<FileTextOutlined />}
                onClick={async () => {
                  try {
                    await taiTep(
                      apiSangKien.duongDanPhieuTiepNhan(id!),
                      `phieu-tiep-nhan-${hs.maHoSo}.pdf`,
                    );
                  } catch (loi) {
                    message.error(
                      loi instanceof LoiApi ? loi.message : 'Không xuất được phiếu tiếp nhận.',
                    );
                  }
                }}
              >
                Phiếu tiếp nhận
              </Button>
            )}

            {/* Chỉ hiện khi hồ sơ đã qua chấm điểm — trước đó chưa có phiếu nào để xuất. */}
            {hs.tongDiem != null && (
              <Dropdown
                menu={{
                  items: [
                    { key: 'PDF', label: 'Một tệp PDF (mỗi phiếu một trang)' },
                    { key: 'ZIP', label: 'ZIP — mỗi phiếu một tệp riêng' },
                  ],
                  onClick: async ({ key }) => {
                    try {
                      await taiTep(
                        apiNhapXuat.duongDanPhieuChamHoSo(id!, key as 'PDF' | 'ZIP'),
                        `phieu-cham-${hs.maHoSo}.${key === 'ZIP' ? 'zip' : 'pdf'}`,
                      );
                    } catch (loi) {
                      message.error(
                        loi instanceof LoiApi ? loi.message : 'Không xuất được phiếu chấm.',
                      );
                    }
                  },
                }}
              >
                <Button icon={<FilePdfOutlined />}>Xuất phiếu chấm</Button>
              </Dropdown>
            )}
            {hs.choPhepSua && (
              <Link to={`/sang-kien/${id}/sua`}>
                <Button icon={<EditOutlined />}>Sửa hồ sơ</Button>
              </Link>
            )}
            {hs.choPhepRut && (
              <Button
                danger
                icon={<RollbackOutlined />}
                onClick={() =>
                  modal.confirm({
                    title: 'Rút hồ sơ',
                    content: (
                      <Input.TextArea id="ly-do-rut" rows={3} placeholder="Nhập lý do rút hồ sơ" />
                    ),
                    okText: 'Rút hồ sơ',
                    cancelText: 'Hủy',
                    onOk: () => {
                      const o = document.getElementById('ly-do-rut') as HTMLTextAreaElement | null;
                      const lyDo = o?.value?.trim();
                      if (!lyDo) {
                        message.error('Vui lòng nhập lý do rút hồ sơ.');
                        return Promise.reject();
                      }
                      return rutHoSo.mutateAsync(lyDo);
                    },
                  })
                }
              >
                Rút hồ sơ
              </Button>
            )}
          </Space>
        }
      >
        <Typography.Title level={5} style={{ marginTop: 0 }}>
          {hs.tenSangKien}
        </Typography.Title>

        {hs.dangKhoa && (
          <Alert
            type="warning"
            showIcon
            style={{ marginBottom: 12 }}
            message="Hồ sơ đang bị khóa"
            description={hs.lyDoKhoa}
          />
        )}

        <Row gutter={[12, 12]}>
          <Col xs={12} md={6}>
            <Statistic title="Tổng điểm" value={hs.tongDiem ?? 0} precision={2} />
          </Col>
          <Col xs={12} md={6}>
            <Statistic
              title="Mức công nhận"
              value={hs.tenMucCongNhan ?? '—'}
              valueStyle={{ fontSize: 18 }}
            />
          </Col>
          <Col xs={12} md={6}>
            <div>
              <Typography.Text type="secondary">Trùng lặp</Typography.Text>
              <div style={{ marginTop: 4 }}>
                <NhanTrungLap tyLe={hs.tyLeTrungLap} />
              </div>
            </div>
          </Col>
          <Col xs={12} md={6}>
            <div>
              <Typography.Text type="secondary">Hạn xử lý</Typography.Text>
              <div style={{ marginTop: 4 }}>
                <HienThiHan
                  han={hs.hanXuLyHienTai}
                  quaHan={!!hs.hanXuLyHienTai && new Date(hs.hanXuLyHienTai) < new Date()}
                />
              </div>
            </div>
          </Col>
        </Row>

        {/* Nút hành động sinh động theo quy trình — KHÔNG hardcode. */}
        {(hanhDong.data?.length ?? 0) > 0 && (
          <Card size="small" title="Xử lý hồ sơ" style={{ marginTop: 16 }} className="khong-in">
            <Space wrap>
              {hanhDong.data!.map((hd) => (
                <Tooltip key={hd.truongHopId} title={hd.biChan ? hd.lyDoChan : hd.tenBuocTiepTheo}>
                  <Button
                    type={hd.mauNut === 'primary' ? 'primary' : 'default'}
                    danger={hd.mauNut === 'danger'}
                    disabled={hd.biChan}
                    onClick={() => setHanhDongDangChon(hd)}
                  >
                    {hd.ten}
                  </Button>
                </Tooltip>
              ))}
            </Space>
          </Card>
        )}

        {(duocThuHoi || duocPhanCong || duocTongHop) && (
          <Card
            size="small"
            title="Nghiệp vụ hội đồng"
            style={{ marginTop: 12 }}
            className="khong-in"
          >
            <Space wrap>
              {duocPhanCong && (
                <Button icon={<TeamOutlined />} onClick={() => setMoPhanCong(true)}>
                  Phân công chấm điểm
                </Button>
              )}
              {duocTongHop && (
                <Button
                  icon={<CalculatorOutlined />}
                  loading={tongHop.isPending}
                  onClick={moHopThoaiTongHop}
                >
                  Tổng hợp điểm
                </Button>
              )}
              {duocThuHoi && (
                <Button
                  icon={<UndoOutlined />}
                  danger
                  loading={thuHoi.isPending}
                  onClick={moHopThoaiThuHoi}
                >
                  Thu hồi bước
                </Button>
              )}
            </Space>
          </Card>
        )}
      </Card>

      <Card style={{ marginTop: 12 }}>
        <Tabs
          defaultActiveKey="noi-dung"
          items={[
            {
              key: 'noi-dung',
              label: 'Nội dung',
              children: <TabNoiDung hs={hs} />,
            },
            {
              key: 'tep',
              label: <Badge count={hs.tepDinhKem.length} size="small" offset={[10, 0]}>Tệp đính kèm</Badge>,
              children: <TabTepDinhKem hs={hs} />,
            },
            {
              key: 'tien-do',
              label: 'Tiến độ xử lý',
              children: <TabTienDo duLieu={tienDo.data ?? []} dangTai={tienDo.isLoading} />,
            },
            {
              key: 'lich-su',
              label: 'Lịch sử chỉnh sửa',
              children: <TabLichSu duLieu={lichSu.data ?? []} dangTai={lichSu.isLoading} />,
            },
            {
              key: 'trung-lap',
              label: 'Kiểm tra trùng lặp',
              children: (
                <TabTrungLap
                  duLieu={trungLap.data ?? null}
                  dangTai={trungLap.isLoading || chayLaiTrungLap.isPending}
                  onChayLai={() => chayLaiTrungLap.mutate()}
                />
              ),
            },
          ]}
        />
      </Card>

      {moPhanCong && (
        <ModalPhanCongCham
          sangKienId={id}
          onDong={() => setMoPhanCong(false)}
          onXong={() => {
            setMoPhanCong(false);
            void queryClient.invalidateQueries({ queryKey: ['sang-kien', id] });
          }}
        />
      )}

      <Modal
        open={!!hanhDongDangChon}
        title={hanhDongDangChon?.ten}
        okText="Xác nhận"
        cancelText="Hủy"
        confirmLoading={thucThi.isPending || formXuLy.formState.isSubmitting}
        onCancel={() => setHanhDongDangChon(null)}
        okButtonProps={{ htmlType: 'submit', form: 'form-xu-ly' }}
      >
        {hanhDongDangChon?.tenBuocTiepTheo && (
          <Alert
            type="info"
            showIcon
            style={{ marginBottom: 12 }}
            message={`Hồ sơ sẽ chuyển sang bước: ${hanhDongDangChon.tenBuocTiepTheo}`}
          />
        )}

        <BieuMau
          id="form-xu-ly"
          form={formXuLy}
          onGui={(giaTri) =>
            thucThi.mutateAsync({
              truongHopId: hanhDongDangChon!.truongHopId,
              yKien: giaTri.yKien,
            })
          }
        >
          <Truong<GiaTriXuLy>
            ten="yKien"
            label="Ý kiến xử lý"
            required={hanhDongDangChon?.batBuocNhapYKien}
          >
            {(o) => (
              <Input.TextArea
                {...o}
                value={o.value as string}
                rows={4}
                placeholder="Nhập ý kiến xử lý..."
              />
            )}
          </Truong>
        </BieuMau>
      </Modal>
    </div>
  );
}

function TabNoiDung({ hs }: { hs: SangKienChiTiet }) {
  const muc: { nhan: string; giaTri?: string | null }[] = [
    { nhan: 'Tình trạng trước khi áp dụng', giaTri: hs.tinhTrangTruocKhiApDung },
    { nhan: 'Mô tả giải pháp', giaTri: hs.moTaGiaiPhap },
    { nhan: 'Nội dung giải pháp', giaTri: hs.noiDungGiaiPhap },
    { nhan: 'Tính mới', giaTri: hs.tinhMoi },
    { nhan: 'Khả năng áp dụng', giaTri: hs.khaNangApDung },
    { nhan: 'Phạm vi áp dụng', giaTri: hs.phamViApDung },
    { nhan: 'Hiệu quả kinh tế', giaTri: hs.hieuQuaKinhTe },
    { nhan: 'Hiệu quả xã hội', giaTri: hs.hieuQuaXaHoi },
  ];

  return (
    <div>
      <Descriptions bordered size="small" column={{ xs: 1, md: 2 }} style={{ marginBottom: 16 }}>
        <Descriptions.Item label="Đợt đề nghị">{hs.tenDot}</Descriptions.Item>
        <Descriptions.Item label="Lĩnh vực">{hs.tenLinhVuc}</Descriptions.Item>
        <Descriptions.Item label="Đối tượng">{hs.tenDoiTuong ?? '—'}</Descriptions.Item>
        <Descriptions.Item label="Đơn vị">{hs.tenDonVi ?? '—'}</Descriptions.Item>
        <Descriptions.Item label="Ngày nộp">{ngayGio(hs.ngayNop)}</Descriptions.Item>
        <Descriptions.Item label="Ngày công nhận">
          {ngayGio(hs.ngayCongNhan, false)}
        </Descriptions.Item>
        <Descriptions.Item label="Giá trị làm lợi ước tính" span={2}>
          {hs.giaTriLamLoiUocTinh
            ? `${hs.giaTriLamLoiUocTinh.toLocaleString('vi-VN')} đ`
            : '—'}
        </Descriptions.Item>
      </Descriptions>

      <Card size="small" title="Tác giả" style={{ marginBottom: 16 }}>
        <Table
          rowKey={(_, i) => String(i)}
          size="small"
          pagination={false}
          dataSource={hs.danhSachTacGia}
          columns={[
            {
              title: 'Họ và tên',
              dataIndex: 'hoTen',
              render: (giaTri: string, dong) => (
                <Space>
                  {giaTri}
                  {dong.laTacGiaChinh && <Tag color="blue">Tác giả chính</Tag>}
                </Space>
              ),
            },
            { title: 'Chức vụ', dataIndex: 'chucVu' },
            { title: 'Đơn vị công tác', dataIndex: 'donViCongTac' },
            {
              title: 'Tỷ lệ đóng góp',
              dataIndex: 'tyLeDongGop',
              width: 140,
              align: 'right',
              render: (giaTri: number) => `${giaTri}%`,
            },
          ]}
        />
      </Card>

      {muc
        .filter((m) => m.giaTri)
        .map((m) => (
          <Card key={m.nhan} size="small" title={m.nhan} style={{ marginBottom: 12 }}>
            <XemTruocVanBan noiDung={m.giaTri ?? ''} />
          </Card>
        ))}
    </div>
  );
}

function TabTepDinhKem({ hs }: { hs: SangKienChiTiet }) {
  return (
    <Table
      rowKey="id"
      size="small"
      dataSource={hs.tepDinhKem}
      columns={[
        { title: 'Tên tệp', dataIndex: 'tenGoc' },
        { title: 'Thành phần', dataIndex: 'thanhPhanHoSoMa', width: 200 },
        {
          title: 'Dung lượng',
          dataIndex: 'kichThuoc',
          width: 130,
          align: 'right',
          render: (giaTri: number) => `${(giaTri / 1024).toFixed(1)} KB`,
        },
        {
          title: 'Ngày tải lên',
          dataIndex: 'ngayTaiLen',
          width: 160,
          render: (giaTri: string) => ngayGio(giaTri),
        },
        {
          title: '',
          width: 60,
          render: (_giaTri, dong) => (
            <a href={`/api/v1/tep-tin/${dong.tepTinId}/tai-ve`} download>
              <Button type="text" icon={<DownloadOutlined />} />
            </a>
          ),
        },
      ]}
    />
  );
}

function TabTienDo({
  duLieu,
  dangTai,
}: {
  duLieu: { id: string; tenBuoc: string; tenTruongHop?: string | null; nguoiXuLy?: string | null; yKien?: string | null; thoiGianNhan: string; hanXuLy?: string | null; thoiGianXuLy?: string | null; soNgayXuLy?: number | null; quaHan: boolean }[];
  dangTai: boolean;
}) {
  if (dangTai) return <KhoiDangTai soDong={4} />;

  return (
    <Timeline
      mode="left"
      items={duLieu.map((m) => ({
        color: m.thoiGianXuLy ? (m.quaHan ? 'red' : 'green') : 'blue',
        children: (
          <div>
            <Typography.Text strong>{m.tenBuoc}</Typography.Text>
            {m.tenTruongHop && <Tag style={{ marginLeft: 8 }}>{m.tenTruongHop}</Tag>}
            {m.quaHan && <Tag color="red">Quá hạn</Tag>}
            <div style={{ fontSize: 13, color: '#666', marginTop: 4 }}>
              Nhận: {ngayGio(m.thoiGianNhan)}
              {m.hanXuLy && ` • Hạn: ${ngayGio(m.hanXuLy)}`}
              {m.thoiGianXuLy && ` • Xử lý: ${ngayGio(m.thoiGianXuLy)}`}
              {m.soNgayXuLy !== null && m.soNgayXuLy !== undefined && ` • ${m.soNgayXuLy} ngày`}
            </div>
            {m.nguoiXuLy && (
              <div style={{ fontSize: 13, color: '#666' }}>Người xử lý: {m.nguoiXuLy}</div>
            )}
            {m.yKien && (
              <Typography.Paragraph style={{ marginTop: 4, marginBottom: 0, fontStyle: 'italic' }}>
                “{m.yKien}”
              </Typography.Paragraph>
            )}
          </div>
        ),
      }))}
    />
  );
}

function TabLichSu({
  duLieu,
  dangTai,
}: {
  duLieu: { id: string; hanhDong: string; truongThayDoi: string[]; giaTriTruoc?: Record<string, string | null> | null; giaTriSau?: Record<string, string | null> | null; thoiGian: string; ghiChu?: string | null }[];
  dangTai: boolean;
}) {
  if (dangTai) return <KhoiDangTai soDong={4} />;

  return (
    <Table
      rowKey="id"
      size="small"
      dataSource={duLieu}
      expandable={{
        rowExpandable: (dong) => dong.truongThayDoi.length > 0,
        expandedRowRender: (dong) => (
          <Table
            size="small"
            pagination={false}
            rowKey={(t) => t}
            dataSource={dong.truongThayDoi}
            columns={[
              { title: 'Trường', render: (t: string) => t, width: 220 },
              {
                title: 'Giá trị trước',
                render: (t: string) => (
                  <Typography.Text type="secondary" ellipsis={{ tooltip: dong.giaTriTruoc?.[t] }}>
                    {dong.giaTriTruoc?.[t] ?? '—'}
                  </Typography.Text>
                ),
              },
              {
                title: 'Giá trị sau',
                render: (t: string) => (
                  <Typography.Text ellipsis={{ tooltip: dong.giaTriSau?.[t] }}>
                    {dong.giaTriSau?.[t] ?? '—'}
                  </Typography.Text>
                ),
              },
            ]}
          />
        ),
      }}
      columns={[
        { title: 'Hành động', dataIndex: 'hanhDong', width: 140 },
        {
          title: 'Số trường thay đổi',
          dataIndex: 'truongThayDoi',
          width: 160,
          render: (giaTri: string[]) => giaTri.length,
        },
        { title: 'Ghi chú', dataIndex: 'ghiChu' },
        {
          title: 'Thời gian',
          dataIndex: 'thoiGian',
          width: 170,
          render: (giaTri: string) => ngayGio(giaTri),
        },
      ]}
    />
  );
}

function TabTrungLap({
  duLieu,
  dangTai,
  onChayLai,
}: {
  duLieu: {
    tongSoDoiChieu: number;
    tyLeCaoNhat: number;
    mucCanhBao: string;
    thoiGianXuLyMs: number;
    ngayChay: string;
    chiTiet: {
      sangKienDoiChieuId: string;
      tyLeTuongDong: number;
      tyLeTuVung: number;
      tyLeNguNghia: number;
      soDoanTrung: number;
      cacDoanTrung: { doanNguon: string; doanDich: string; tyLe: number }[];
    }[];
  } | null;
  dangTai: boolean;
  onChayLai: () => void;
}) {
  const [capDangXem, setCapDangXem] = useState<number | null>(null);

  if (dangTai) return <KhoiDangTai soDong={4} />;

  if (!duLieu) {
    return (
      <Alert
        type="info"
        showIcon
        message="Hồ sơ chưa được kiểm tra trùng lặp"
        action={
          <Button icon={<ReloadOutlined />} onClick={onChayLai}>
            Chạy kiểm tra
          </Button>
        }
      />
    );
  }

  const mauTien =
    duLieu.mucCanhBao === 'NGHIEM_TRONG'
      ? '#ff4d4f'
      : duLieu.mucCanhBao === 'CANH_BAO'
        ? '#faad14'
        : '#52c41a';

  return (
    <div>
      <Row gutter={[12, 12]} align="middle" style={{ marginBottom: 16 }}>
        <Col xs={24} md={8}>
          <Progress
            type="dashboard"
            percent={Math.round(duLieu.tyLeCaoNhat)}
            strokeColor={mauTien}
            format={(p) => (
              <div>
                <div style={{ fontSize: 22 }}>{p}%</div>
                <div style={{ fontSize: 12, color: '#888' }}>trùng lặp cao nhất</div>
              </div>
            )}
          />
        </Col>
        <Col xs={24} md={16}>
          <Descriptions size="small" column={1} bordered>
            <Descriptions.Item label="Mức cảnh báo">
              <Tag color={mauTien === '#ff4d4f' ? 'error' : mauTien === '#faad14' ? 'warning' : 'success'}>
                {duLieu.mucCanhBao.replace('_', ' ')}
              </Tag>
            </Descriptions.Item>
            <Descriptions.Item label="Số hồ sơ đối chiếu">{duLieu.tongSoDoiChieu}</Descriptions.Item>
            <Descriptions.Item label="Thời gian xử lý">{duLieu.thoiGianXuLyMs} ms</Descriptions.Item>
            <Descriptions.Item label="Lần chạy gần nhất">{ngayGio(duLieu.ngayChay)}</Descriptions.Item>
          </Descriptions>
          <Button
            icon={<ReloadOutlined />}
            style={{ marginTop: 12 }}
            onClick={onChayLai}
            className="khong-in"
          >
            Chạy lại kiểm tra
          </Button>
        </Col>
      </Row>

      <Table
        rowKey="sangKienDoiChieuId"
        size="small"
        dataSource={duLieu.chiTiet}
        onRow={(_dong, chiSo) => ({ onClick: () => setCapDangXem(chiSo ?? null) })}
        columns={[
          {
            title: 'Hồ sơ đối chiếu',
            dataIndex: 'sangKienDoiChieuId',
            render: (giaTri: string) => <Link to={`/sang-kien/${giaTri}`}>Xem hồ sơ</Link>,
          },
          {
            title: 'Tỷ lệ tổng hợp',
            dataIndex: 'tyLeTuongDong',
            width: 150,
            sorter: (a, b) => a.tyLeTuongDong - b.tyLeTuongDong,
            defaultSortOrder: 'descend',
            render: (giaTri: number) => (
              <Progress
                percent={Math.round(giaTri)}
                size="small"
                strokeColor={giaTri > 40 ? '#ff4d4f' : giaTri >= 20 ? '#faad14' : '#52c41a'}
              />
            ),
          },
          {
            title: 'Từ vựng',
            dataIndex: 'tyLeTuVung',
            width: 100,
            render: (giaTri: number) => `${giaTri.toFixed(1)}%`,
          },
          {
            title: 'Ngữ nghĩa',
            dataIndex: 'tyLeNguNghia',
            width: 100,
            render: (giaTri: number) => `${giaTri.toFixed(1)}%`,
          },
          { title: 'Số đoạn trùng', dataIndex: 'soDoanTrung', width: 130, align: 'right' },
        ]}
      />

      <Modal
        open={capDangXem !== null}
        title="Đối chiếu đoạn trùng lặp"
        width={1000}
        footer={null}
        onCancel={() => setCapDangXem(null)}
      >
        {capDangXem !== null &&
          duLieu.chiTiet[capDangXem]?.cacDoanTrung.map((doan, i) => (
            <Card key={i} size="small" style={{ marginBottom: 12 }}
              title={`Đoạn ${i + 1} — tương đồng ${doan.tyLe.toFixed(1)}%`}
            >
              <div className="doi-chieu-trung-lap">
                <div>
                  <Typography.Text type="secondary">Hồ sơ này</Typography.Text>
                  <Typography.Paragraph className="doan-trung" style={{ whiteSpace: 'pre-wrap' }}>
                    {doan.doanNguon}
                  </Typography.Paragraph>
                </div>
                <div>
                  <Typography.Text type="secondary">Hồ sơ đối chiếu</Typography.Text>
                  <Typography.Paragraph className="doan-trung" style={{ whiteSpace: 'pre-wrap' }}>
                    {doan.doanDich}
                  </Typography.Paragraph>
                </div>
              </div>
            </Card>
          ))}

        {capDangXem !== null && duLieu.chiTiet[capDangXem]?.cacDoanTrung.length === 0 && (
          <Alert
            type="success"
            showIcon
            message="Không có đoạn văn nào trùng nhau vượt ngưỡng"
            description="Tỷ lệ tương đồng đến từ mức độ giống nhau chung của toàn văn, không phải sao chép nguyên văn."
          />
        )}
      </Modal>
    </div>
  );
}

// ---------------------------------------------------------------------------

/** Ô chọn hội đồng dùng trong hộp thoại tổng hợp điểm. */
function ChonHoiDong({ onChon }: { onChon: (id: string) => void }) {
  const { data } = useQuery({ queryKey: ['hoi-dong-chon'], queryFn: apiHoiDong.chon });

  return (
    <Select
      style={{ width: '100%' }}
      placeholder="Chọn hội đồng"
      options={(data ?? []).map((x) => ({ value: x.id, label: x.ten }))}
      onChange={onChon}
    />
  );
}

/**
 * Chức năng 33 — Phân công thành viên hội đồng chấm hồ sơ.
 *
 * Để trống danh sách thành viên = giao cho toàn bộ thành viên có quyền chấm của hội đồng.
 * Máy chủ tự loại thành viên là tác giả của chính hồ sơ (xung đột lợi ích) và báo lại.
 */
function ModalPhanCongCham({
  sangKienId,
  onDong,
  onXong,
}: {
  sangKienId: string;
  onDong: () => void;
  onXong: () => void;
}) {
  const { message } = App.useApp();

  const [hoiDongId, setHoiDongId] = useState<string | undefined>();
  const [thanhVienIds, setThanhVienIds] = useState<string[]>([]);
  const [hanHoanThanh, setHanHoanThanh] = useState<dayjs.Dayjs | null>(dayjs().add(7, 'day'));
  const [tuDongChiaDeu, setTuDongChiaDeu] = useState(true);

  const { data: cacHoiDong } = useQuery({ queryKey: ['hoi-dong-chon'], queryFn: apiHoiDong.chon });

  const { data: chiTietHoiDong } = useQuery({
    queryKey: ['hoi-dong', hoiDongId],
    queryFn: () => apiHoiDong.theoId(hoiDongId!),
    enabled: !!hoiDongId,
  });

  const phanCong = useMutation({
    mutationFn: () =>
      apiDanhGia.phanCong({
        hoiDongId,
        sangKienIds: [sangKienId],
        thanhVienIds: thanhVienIds.length > 0 ? thanhVienIds : null,
        hanHoanThanh: hanHoanThanh ? hanHoanThanh.toISOString() : null,
        tuDongChiaDeu,
      }),
    onSuccess: () => {
      message.success('Đã phân công chấm điểm');
      onXong();
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không phân công được.'),
  });

  const thanhVienChamDuoc = (chiTietHoiDong?.thanhVien ?? []).filter((x) => x.quyenChamDiem);

  return (
    <Modal
      open
      title="Phân công chấm điểm"
      okText="Phân công"
      cancelText="Huỷ"
      confirmLoading={phanCong.isPending}
      onCancel={onDong}
      onOk={() => {
        if (!hoiDongId) {
          message.warning('Vui lòng chọn hội đồng.');
          return;
        }

        phanCong.mutate();
      }}
    >
      <Space direction="vertical" size={12} style={{ width: '100%' }}>
        <div>
          <Typography.Text strong>Hội đồng</Typography.Text>
          <Select
            style={{ width: '100%', marginTop: 4 }}
            placeholder="Chọn hội đồng chấm"
            value={hoiDongId}
            options={(cacHoiDong ?? []).map((x) => ({ value: x.id, label: x.ten }))}
            onChange={(v) => {
              setHoiDongId(v);
              setThanhVienIds([]);
            }}
          />
        </div>

        <div>
          <Typography.Text strong>Thành viên chấm</Typography.Text>
          <Select
            mode="multiple"
            style={{ width: '100%', marginTop: 4 }}
            placeholder="Để trống = giao cho tất cả thành viên có quyền chấm"
            value={thanhVienIds}
            disabled={!hoiDongId}
            options={thanhVienChamDuoc.map((x) => ({ value: x.id, label: x.hoTenHienThi }))}
            onChange={setThanhVienIds}
          />
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            Thành viên là tác giả của hồ sơ sẽ tự bị loại do xung đột lợi ích.
          </Typography.Text>
        </div>

        <Space size="large" wrap>
          <div>
            <Typography.Text strong>Hạn hoàn thành</Typography.Text>
            <div style={{ marginTop: 4 }}>
              <DatePicker
                showTime
                format="DD/MM/YYYY HH:mm"
                value={hanHoanThanh}
                onChange={setHanHoanThanh}
              />
            </div>
          </div>
          <div>
            <Typography.Text strong>Tự động chia đều</Typography.Text>
            <div style={{ marginTop: 4 }}>
              <Switch checked={tuDongChiaDeu} onChange={setTuDongChiaDeu} />
            </div>
          </div>
        </Space>
      </Space>
    </Modal>
  );
}
