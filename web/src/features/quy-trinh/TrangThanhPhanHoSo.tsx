import { useEffect, useState } from 'react';
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
  Typography,
} from 'antd';
import { ArrowLeftOutlined, DeleteOutlined, PlusOutlined, SaveOutlined } from '@ant-design/icons';
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

/**
 * Chức năng 13 — Cấu hình thành phần hồ sơ của một quy trình.
 *
 * Đây chính là danh sách mà wizard nộp hồ sơ dựng checklist ✓/✗ và chặn nộp khi thiếu, nên mỗi
 * dòng ở đây là một ràng buộc thật với tác giả — không phải mô tả cho vui.
 */
export default function TrangThanhPhanHoSo() {
  const { id = '' } = useParams<{ id: string }>();
  const { message, modal } = App.useApp();

  const [danhSach, setDanhSach] = useState<ThanhPhanHoSoCauHinh[]>([]);

  const truyVan = useQuery({
    queryKey: ['quy-trinh-so-do', id],
    queryFn: () => apiQuyTrinh.soDo(id),
  });

  useEffect(() => {
    if (truyVan.data) setDanhSach(truyVan.data.thanhPhanHoSo ?? []);
  }, [truyVan.data]);

  const luu = useMutation({
    mutationFn: () => apiQuyTrinh.luuSoDo(id, { ...truyVan.data!, thanhPhanHoSo: danhSach }),
    onSuccess: () => {
      message.success('Đã lưu cấu hình thành phần hồ sơ');
      void truyVan.refetch();
    },
    onError: (loi) =>
      modal.error({
        title: 'Không lưu được',
        content: loi instanceof LoiApi ? loi.message : 'Đã xảy ra lỗi.',
      }),
  });

  if (truyVan.isLoading) return <KhoiDangTai />;
  if (truyVan.error) return <KhoiLoi loi={truyVan.error} thuLai={truyVan.refetch} />;

  function sua(chiSo: number, thayDoi: Partial<ThanhPhanHoSoCauHinh>) {
    setDanhSach((cu) => cu.map((x, i) => (i === chiSo ? { ...x, ...thayDoi } : x)));
  }

  const maTrung = danhSach
    .map((x) => x.ma.trim().toUpperCase())
    .filter((ma, i, ds) => ma !== '' && ds.indexOf(ma) !== i);

  const hopLe = maTrung.length === 0 && danhSach.every((x) => x.ma.trim() && x.ten.trim());

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
            disabled={!hopLe}
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

      <Table<ThanhPhanHoSoCauHinh>
        rowKey={(x) => x.id || x.ma}
        size="small"
        pagination={false}
        dataSource={danhSach}
        scroll={{ x: 1500 }}
        locale={{ emptyText: 'Quy trình chưa cấu hình thành phần hồ sơ nào.' }}
        columns={[
          {
            title: 'Mã',
            dataIndex: 'ma',
            width: 180,
            render: (v: string, _dong, i) => (
              <Input
                value={v}
                status={v.trim() ? undefined : 'error'}
                placeholder="VD: BAO_CAO"
                onChange={(e) => sua(i, { ma: e.target.value.toUpperCase() })}
              />
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
            render: (_v, _dong, i) => (
              <Button
                type="text"
                danger
                icon={<DeleteOutlined />}
                onClick={() => setDanhSach((cu) => cu.filter((_x, j) => j !== i))}
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
        onClick={() =>
          setDanhSach((cu) => [
            ...cu,
            {
              id: '00000000-0000-0000-0000-000000000000',
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
              thuTu: cu.length + 1,
              moTaHuongDan: null,
            },
          ])
        }
      >
        Thêm thành phần hồ sơ
      </Button>

      <Typography.Paragraph type="secondary" style={{ marginTop: 12, fontSize: 12 }}>
        Lưu ý: đổi mã của thành phần đã có hồ sơ nộp sẽ làm dữ liệu cũ không còn khớp — muốn đổi
        tên hiển thị thì sửa cột Tên, giữ nguyên Mã.
      </Typography.Paragraph>
    </Card>
  );
}
