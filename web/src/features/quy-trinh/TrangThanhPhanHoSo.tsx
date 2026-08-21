import { useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  App,
  Alert,
  Button,
  Card,
  Checkbox,
  Input,
  InputNumber,
  Select,
  Space,
  Table,
  Tag,
  Tooltip,
  Typography,
} from 'antd';
import {
  ArrowLeftOutlined,
  ArrowDownOutlined,
  ArrowUpOutlined,
  DeleteOutlined,
  PlusOutlined,
  SaveOutlined,
} from '@ant-design/icons';
import { useMutation, useQuery } from '@tanstack/react-query';

import { LoiApi } from '@/api/client';
import { apiQuyTrinh, type ThanhPhanHoSoCauHinh } from '@/api/endpoints';
import { KhoiDangTai, KhoiLoi } from '@/components/ThanhPhanChung';

const LOAI_DU_LIEU = [
  { value: 'CA_HAI', label: 'Văn bản và tệp' },
  { value: 'VAN_BAN', label: 'Chỉ nhập văn bản' },
  { value: 'TEP', label: 'Chỉ tải tệp' },
];

const DINH_DANG_GOI_Y = ['.pdf', '.doc', '.docx', '.xls', '.xlsx', '.jpg', '.png', '.zip'];

/** Dòng chưa có trên máy chủ vẫn phải gửi một Guid hợp lệ, nếu không khớp kiểu sẽ bị 400. */
const GUID_RONG = '00000000-0000-0000-0000-000000000000';

/** Dòng đang sửa trên bảng. `khoa` ổn định kể cả khi dòng chưa có id từ máy chủ. */
type DongThanhPhan = ThanhPhanHoSoCauHinh & { khoa: string };

/** So sánh nội dung một dòng, bỏ qua thứ tự — thứ tự đã có API sắp xếp riêng. */
function chuKyNoiDung(x: ThanhPhanHoSoCauHinh): string {
  return JSON.stringify([
    x.ma.trim().toUpperCase(),
    x.ten.trim(),
    x.batBuoc,
    x.loaiDuLieu,
    [...(x.dinhDangChoPhep ?? [])].sort(),
    x.dungLuongToiDaMb,
    x.soLuongToiDa,
    x.soKyTuToiThieu,
    x.soKyTuToiDa,
    x.dungDeKiemTraTrungLap,
  ]);
}

/**
 * Chức năng 13 — Cấu hình thành phần hồ sơ của một quy trình.
 *
 * Đây chính là danh sách mà wizard nộp hồ sơ dựng checklist ✓/✗ và chặn nộp khi thiếu, nên mỗi
 * dòng ở đây là một ràng buộc thật với tác giả — không phải mô tả cho vui.
 *
 * Màn hình này ghi qua **API riêng của từng thành phần**, không gửi lại cả sơ đồ quy trình:
 * thành phần hồ sơ không phải một nút trên sơ đồ, và nếu lưu bằng cách gửi lại cả sơ đồ thì hai
 * người cùng mở một quy trình — một người sửa bước, một người thêm thành phần — sẽ ghi đè lên
 * nhau, ai bấm Lưu sau thì thắng. Bấm Lưu ở đây chỉ gửi đúng những dòng thực sự đổi.
 */
export default function TrangThanhPhanHoSo() {
  const { id = '' } = useParams<{ id: string }>();
  const { message, modal } = App.useApp();

  const [danhSach, setDanhSach] = useState<DongThanhPhan[]>([]);
  const [daXoa, setDaXoa] = useState<string[]>([]);

  const truyVan = useQuery({
    queryKey: ['quy-trinh-thanh-phan', id],
    queryFn: () => apiQuyTrinh.thanhPhan(id),
  });

  useEffect(() => {
    if (!truyVan.data) return;

    setDanhSach(truyVan.data.map((x) => ({ ...x, khoa: x.id })));
    setDaXoa([]);
  }, [truyVan.data]);

  /** Bản máy chủ đang giữ, để biết dòng nào thực sự đổi. */
  const banGoc = useMemo(
    () => new Map((truyVan.data ?? []).map((x) => [x.id, x])),
    [truyVan.data],
  );

  const thuTuGoc = useMemo(() => (truyVan.data ?? []).map((x) => x.id), [truyVan.data]);

  const luu = useMutation({
    mutationFn: async () => {
      for (const idXoa of daXoa) {
        await apiQuyTrinh.xoaThanhPhan(id, idXoa);
      }

      const idTheoThuTu: string[] = [];

      for (const [i, dong] of danhSach.entries()) {
        const { khoa: _khoa, ...noiDung } = dong;
        const duLieu: ThanhPhanHoSoCauHinh = {
          ...noiDung,
          id: dong.id || GUID_RONG,
          ma: dong.ma.trim().toUpperCase(),
          ten: dong.ten.trim(),
          thuTu: i,
        };

        try {
          if (!dong.id) {
            idTheoThuTu.push(await apiQuyTrinh.themThanhPhan(id, duLieu));
            continue;
          }

          const goc = banGoc.get(dong.id);
          if (!goc || chuKyNoiDung(goc) !== chuKyNoiDung(dong)) {
            await apiQuyTrinh.suaThanhPhan(id, dong.id, duLieu);
          }

          idTheoThuTu.push(dong.id);
        } catch (loi) {
          // Nói rõ dòng nào hỏng: bảng có nhiều dòng, báo chung chung thì người dùng phải tự dò.
          const moTa = loi instanceof LoiApi ? loi.message : 'Đã xảy ra lỗi.';
          throw new Error(`Dòng "${dong.ma || dong.ten || i + 1}": ${moTa}`);
        }
      }

      const doiThuTu =
        idTheoThuTu.length !== thuTuGoc.length ||
        idTheoThuTu.some((x, i) => x !== thuTuGoc[i]);

      if (doiThuTu) {
        await apiQuyTrinh.sapXepThanhPhan(id, idTheoThuTu);
      }
    },
    onSuccess: () => {
      message.success('Đã lưu cấu hình thành phần hồ sơ');
      void truyVan.refetch();
    },
    onError: (loi) => {
      modal.error({
        title: 'Không lưu được',
        content: loi instanceof Error ? loi.message : 'Đã xảy ra lỗi.',
      });

      // Đọc lại trạng thái thật: một phần thay đổi có thể đã ghi xuống trước khi gặp lỗi.
      void truyVan.refetch();
    },
  });

  if (truyVan.isLoading) return <KhoiDangTai />;
  if (truyVan.error) return <KhoiLoi loi={truyVan.error} thuLai={truyVan.refetch} />;

  function sua(chiSo: number, thayDoi: Partial<ThanhPhanHoSoCauHinh>) {
    setDanhSach((cu) => cu.map((x, i) => (i === chiSo ? { ...x, ...thayDoi } : x)));
  }

  function doiCho(chiSo: number, buoc: -1 | 1) {
    setDanhSach((cu) => {
      const dich = chiSo + buoc;
      if (dich < 0 || dich >= cu.length) return cu;

      const moi = [...cu];
      [moi[chiSo], moi[dich]] = [moi[dich], moi[chiSo]];
      return moi;
    });
  }

  const maTrung = danhSach
    .map((x) => x.ma.trim().toUpperCase())
    .filter((ma, i, ds) => ma !== '' && ds.indexOf(ma) !== i);

  const hopLe = maTrung.length === 0 && danhSach.every((x) => x.ma.trim() && x.ten.trim());

  const soThem = danhSach.filter((x) => !x.id).length;
  const soSua = danhSach.filter((x) => {
    const goc = x.id ? banGoc.get(x.id) : undefined;
    return goc !== undefined && chuKyNoiDung(goc) !== chuKyNoiDung(x);
  }).length;
  const doiThuTu = danhSach.filter((x) => x.id).some((x, i) => x.id !== thuTuGoc[i]);
  const coThayDoi = soThem > 0 || soSua > 0 || daXoa.length > 0 || doiThuTu;

  return (
    <Card
      title="Cấu hình thành phần hồ sơ"
      extra={
        <Space>
          <Link to="/quan-tri/quy-trinh">
            <Button icon={<ArrowLeftOutlined />}>Danh sách quy trình</Button>
          </Link>
          <Link to={`/quan-tri/quy-trinh/${id}/thiet-ke`}>
            <Button>Trình thiết kế</Button>
          </Link>
          <Button
            type="primary"
            icon={<SaveOutlined />}
            loading={luu.isPending}
            disabled={!hopLe || !coThayDoi}
            onClick={() => luu.mutate()}
          >
            Lưu
          </Button>
        </Space>
      }
    >
      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 12 }}
        message="Danh sách này dựng nên checklist ở màn hình nộp hồ sơ."
        description="Thành phần đánh dấu bắt buộc sẽ chặn tác giả nộp khi còn thiếu. Thành phần bật “dùng kiểm tra trùng lặp” sẽ được đưa vào so khớp nội dung."
      />

      {maTrung.length > 0 && (
        <Alert
          type="error"
          showIcon
          style={{ marginBottom: 12 }}
          message={`Mã bị trùng: ${[...new Set(maTrung)].join(', ')} — mã là khoá để lưu dữ liệu nộp nên không được trùng.`}
        />
      )}

      {coThayDoi && (
        <Alert
          type="warning"
          showIcon
          style={{ marginBottom: 12 }}
          message="Còn thay đổi chưa lưu"
          description={
            [
              soThem > 0 ? `${soThem} dòng thêm mới` : null,
              soSua > 0 ? `${soSua} dòng đã sửa` : null,
              daXoa.length > 0 ? `${daXoa.length} dòng sẽ xoá` : null,
              doiThuTu ? 'đổi thứ tự' : null,
            ]
              .filter(Boolean)
              .join(', ') + '. Bấm Lưu để ghi xuống — chỉ những dòng này được gửi đi.'
          }
        />
      )}

      <Table<DongThanhPhan>
        rowKey={(x) => x.khoa}
        size="small"
        pagination={false}
        dataSource={danhSach}
        scroll={{ x: 1600 }}
        locale={{ emptyText: 'Quy trình chưa cấu hình thành phần hồ sơ nào.' }}
        columns={[
          {
            title: 'Thứ tự',
            key: 'thuTu',
            width: 92,
            fixed: 'left',
            render: (_v, _dong, i) => (
              <Space size={0}>
                <Button
                  type="text"
                  size="small"
                  icon={<ArrowUpOutlined />}
                  disabled={i === 0}
                  aria-label="Đưa lên trên"
                  onClick={() => doiCho(i, -1)}
                />
                <Button
                  type="text"
                  size="small"
                  icon={<ArrowDownOutlined />}
                  disabled={i === danhSach.length - 1}
                  aria-label="Đưa xuống dưới"
                  onClick={() => doiCho(i, 1)}
                />
              </Space>
            ),
          },
          {
            title: 'Mã',
            dataIndex: 'ma',
            width: 180,
            render: (v: string, dong, i) => (
              <Space direction="vertical" size={2} style={{ width: '100%' }}>
                <Input
                  value={v}
                  status={v.trim() ? undefined : 'error'}
                  placeholder="VD: BAO_CAO"
                  onChange={(e) => sua(i, { ma: e.target.value.toUpperCase() })}
                />
                {!dong.id && <Tag color="processing">Mới</Tag>}
                {dong.id &&
                  banGoc.get(dong.id) !== undefined &&
                  chuKyNoiDung(banGoc.get(dong.id)!) !== chuKyNoiDung(dong) && (
                    <Tag color="warning">Đã sửa</Tag>
                  )}
              </Space>
            ),
          },
          {
            title: 'Tên hiển thị',
            dataIndex: 'ten',
            width: 260,
            render: (v: string, _dong, i) => (
              <Input
                value={v}
                status={v.trim() ? undefined : 'error'}
                onChange={(e) => sua(i, { ten: e.target.value })}
              />
            ),
          },
          {
            title: 'Bắt buộc',
            dataIndex: 'batBuoc',
            width: 100,
            align: 'center',
            render: (v: boolean, _dong, i) => (
              <Checkbox checked={v} onChange={(e) => sua(i, { batBuoc: e.target.checked })} />
            ),
          },
          {
            title: 'Kiểu dữ liệu',
            dataIndex: 'loaiDuLieu',
            width: 190,
            render: (v: string, _dong, i) => (
              <Select
                style={{ width: '100%' }}
                value={v}
                options={LOAI_DU_LIEU}
                onChange={(giaTri) => sua(i, { loaiDuLieu: giaTri })}
              />
            ),
          },
          {
            title: 'Định dạng cho phép',
            dataIndex: 'dinhDangChoPhep',
            width: 260,
            render: (v: string[], _dong, i) => (
              <Select
                mode="tags"
                style={{ width: '100%' }}
                value={v}
                placeholder="Để trống = mọi định dạng được phép"
                options={DINH_DANG_GOI_Y.map((x) => ({ value: x, label: x }))}
                onChange={(giaTri) => sua(i, { dinhDangChoPhep: giaTri })}
              />
            ),
          },
          {
            title: 'Dung lượng (MB)',
            dataIndex: 'dungLuongToiDaMb',
            width: 130,
            render: (v: number, _dong, i) => (
              <InputNumber
                min={1}
                max={100}
                value={v}
                style={{ width: '100%' }}
                onChange={(giaTri) => sua(i, { dungLuongToiDaMb: giaTri ?? 20 })}
              />
            ),
          },
          {
            title: 'Số tệp tối đa',
            dataIndex: 'soLuongToiDa',
            width: 120,
            render: (v: number, _dong, i) => (
              <InputNumber
                min={1}
                max={50}
                value={v}
                style={{ width: '100%' }}
                onChange={(giaTri) => sua(i, { soLuongToiDa: giaTri ?? 5 })}
              />
            ),
          },
          {
            title: 'Số ký tự tối thiểu',
            dataIndex: 'soKyTuToiThieu',
            width: 140,
            render: (v: number, _dong, i) => (
              <InputNumber
                min={0}
                value={v}
                style={{ width: '100%' }}
                onChange={(giaTri) => sua(i, { soKyTuToiThieu: giaTri ?? 0 })}
              />
            ),
          },
          {
            title: 'Kiểm tra trùng lặp',
            dataIndex: 'dungDeKiemTraTrungLap',
            width: 150,
            align: 'center',
            render: (v: boolean, _dong, i) => (
              <Checkbox
                checked={v}
                onChange={(e) => sua(i, { dungDeKiemTraTrungLap: e.target.checked })}
              />
            ),
          },
          {
            title: '',
            key: 'thaoTac',
            width: 60,
            fixed: 'right',
            render: (_v, dong, i) => (
              <Tooltip title="Xoá dòng — chỉ ghi xuống khi bấm Lưu">
                <Button
                  type="text"
                  danger
                  icon={<DeleteOutlined />}
                  aria-label={`Xoá thành phần ${dong.ma || i + 1}`}
                  onClick={() => {
                    if (dong.id) setDaXoa((cu) => [...cu, dong.id]);
                    setDanhSach((cu) => cu.filter((x) => x.khoa !== dong.khoa));
                  }}
                />
              </Tooltip>
            ),
          },
        ]}
      />

      <Button
        type="dashed"
        block
        icon={<PlusOutlined />}
        style={{ marginTop: 12 }}
        onClick={() =>
          setDanhSach((cu) => [
            ...cu,
            {
              // id rỗng = chưa có trên máy chủ; lúc Lưu sẽ đi bằng POST chứ không phải PUT.
              id: '',
              khoa: `moi-${cu.length}-${cu.filter((x) => !x.id).length}`,
              ma: '',
              ten: '',
              batBuoc: true,
              loaiDuLieu: 'CA_HAI',
              dinhDangChoPhep: ['.pdf', '.docx'],
              dungLuongToiDaMb: 20,
              soLuongToiDa: 5,
              soKyTuToiThieu: 0,
              soKyTuToiDa: 0,
              dungDeKiemTraTrungLap: false,
              thuTu: cu.length,
              moTaHuongDan: null,
            },
          ])
        }
      >
        Thêm thành phần hồ sơ
      </Button>

      <Typography.Paragraph type="secondary" style={{ marginTop: 12, fontSize: 12 }}>
        Lưu ý: đổi mã của thành phần đã có hồ sơ nộp sẽ làm dữ liệu cũ không còn khớp — muốn đổi
        tên hiển thị thì sửa cột Tên, giữ nguyên Mã. Quy trình đang áp dụng thì máy chủ chặn sửa;
        hãy tạo phiên bản mới rồi sửa trên đó.
      </Typography.Paragraph>
    </Card>
  );
}
