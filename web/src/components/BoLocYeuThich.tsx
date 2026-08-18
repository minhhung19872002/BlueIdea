import { useEffect, useRef, useState } from 'react';
import { App, Button, Checkbox, Input, Modal, Select, Space, Tooltip } from 'antd';
import { DeleteOutlined, StarFilled, StarOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { LoiApi } from '@/api/client';
import { apiBoLoc, type BoLocYeuThich } from '@/api/endpoints';

/** Chuỗi query của màn hình → JSON object mà máy chủ chấp nhận lưu. */
function thamSoSangJson(chuoiQuery: string) {
  return JSON.stringify(Object.fromEntries(new URLSearchParams(chuoiQuery).entries()));
}

/** JSON object đã lưu → chuỗi query để đặt lại lên URL màn hình. */
function jsonSangThamSo(json: string) {
  try {
    const doiTuong = JSON.parse(json) as Record<string, unknown>;

    return new URLSearchParams(
      Object.entries(doiTuong)
        .filter(([, v]) => v !== null && v !== undefined && v !== '')
        .map(([k, v]) => [k, String(v)]),
    ).toString();
  } catch {
    // Bộ lọc hỏng (sửa tay trong CSDL) không được làm sập màn hình danh sách.
    return '';
  }
}

/**
 * Chức năng 28 — Thanh bộ lọc yêu thích dùng chung cho các màn hình danh sách.
 *
 * Máy chủ lưu tiêu chí dưới dạng đối tượng JSON, còn màn hình làm việc bằng chuỗi query trên
 * URL, nên thanh này chuyển đổi hai chiều. Nhờ vậy thêm tiêu chí lọc mới cho màn hình không
 * phải sửa gì ở đây.
 *
 * Bộ lọc mặc định chỉ tự áp dụng khi người dùng vào màn hình mà URL CHƯA có tiêu chí nào —
 * link chia sẻ luôn thắng bộ lọc cá nhân.
 */
export function ThanhBoLocYeuThich({
  manHinh,
  thamSoHienTai,
  onApDung,
}: {
  manHinh: string;
  /** Chuỗi query đang áp dụng, ví dụ "trangThaiTong=DA_NOP&linhVucId=..." */
  thamSoHienTai: string;
  onApDung: (thamSo: string) => void;
}) {
  const { message, modal } = App.useApp();
  const queryClient = useQueryClient();

  const [dangChon, setDangChon] = useState<string | undefined>();
  const [moLuu, setMoLuu] = useState(false);
  const [tenMoi, setTenMoi] = useState('');
  const [datMacDinhKhiLuu, setDatMacDinhKhiLuu] = useState(false);
  const daApMacDinh = useRef(false);

  const { data } = useQuery({
    queryKey: ['bo-loc-yeu-thich', manHinh],
    queryFn: () => apiBoLoc.danhSach(manHinh),
  });

  useEffect(() => {
    if (daApMacDinh.current || !data || thamSoHienTai) return;
    daApMacDinh.current = true;

    const macDinh = data.find((x) => x.macDinh);
    if (macDinh) {
      setDangChon(macDinh.id);
      onApDung(jsonSangThamSo(macDinh.thamSo));
    }
  }, [data, thamSoHienTai, onApDung]);

  const luu = useMutation({
    mutationFn: () =>
      apiBoLoc.luu({
        manHinh,
        ten: tenMoi.trim(),
        thamSo: thamSoSangJson(thamSoHienTai),
        macDinh: datMacDinhKhiLuu,
      }),
    onSuccess: () => {
      message.success('Đã lưu bộ lọc');
      setMoLuu(false);
      setTenMoi('');
      setDatMacDinhKhiLuu(false);
      void queryClient.invalidateQueries({ queryKey: ['bo-loc-yeu-thich', manHinh] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không lưu được bộ lọc.'),
  });

  const datMacDinh = useMutation({
    mutationFn: (id: string) => apiBoLoc.datMacDinh(id),
    onSuccess: () => {
      message.success('Đã đặt bộ lọc mặc định cho màn hình này');
      void queryClient.invalidateQueries({ queryKey: ['bo-loc-yeu-thich', manHinh] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không đặt được.'),
  });

  const xoa = useMutation({
    mutationFn: (id: string) => apiBoLoc.xoa(id),
    onSuccess: () => {
      message.success('Đã xoá bộ lọc');
      setDangChon(undefined);
      void queryClient.invalidateQueries({ queryKey: ['bo-loc-yeu-thich', manHinh] });
    },
    onError: (loi) => message.error(loi instanceof LoiApi ? loi.message : 'Không xoá được.'),
  });

  const boLocDangChon: BoLocYeuThich | undefined = data?.find((x) => x.id === dangChon);

  return (
    <>
      <Space wrap style={{ marginBottom: 12 }}>
        <Select
          style={{ minWidth: 240 }}
          allowClear
          placeholder="Bộ lọc đã lưu"
          value={dangChon}
          options={(data ?? []).map((x) => ({
            value: x.id,
            label: x.macDinh ? `★ ${x.ten}` : x.ten,
          }))}
          onChange={(id) => {
            setDangChon(id);
            const chon = data?.find((x) => x.id === id);
            onApDung(chon ? jsonSangThamSo(chon.thamSo) : '');
          }}
        />

        <Button
          onClick={() => {
            setTenMoi(boLocDangChon?.ten ?? '');
            setMoLuu(true);
          }}
        >
          Lưu bộ lọc hiện tại
        </Button>

        {boLocDangChon && (
          <>
            <Tooltip title="Tự áp dụng bộ lọc này khi mở màn hình">
              <Button
                icon={boLocDangChon.macDinh ? <StarFilled /> : <StarOutlined />}
                loading={datMacDinh.isPending}
                disabled={boLocDangChon.macDinh}
                onClick={() => datMacDinh.mutate(boLocDangChon.id)}
              >
                {boLocDangChon.macDinh ? 'Đang là mặc định' : 'Đặt mặc định'}
              </Button>
            </Tooltip>

            <Button
              danger
              icon={<DeleteOutlined />}
              onClick={() =>
                modal.confirm({
                  title: 'Xoá bộ lọc',
                  content: `Xoá bộ lọc "${boLocDangChon.ten}"?`,
                  okText: 'Xoá',
                  okButtonProps: { danger: true },
                  cancelText: 'Huỷ',
                  onOk: () => xoa.mutateAsync(boLocDangChon.id),
                })
              }
            />
          </>
        )}
      </Space>

      <Modal
        open={moLuu}
        title="Lưu bộ lọc hiện tại"
        okText="Lưu"
        cancelText="Huỷ"
        confirmLoading={luu.isPending}
        okButtonProps={{ disabled: !tenMoi.trim() }}
        onCancel={() => setMoLuu(false)}
        onOk={() => luu.mutate()}
      >
        <Space direction="vertical" style={{ width: '100%' }}>
          <Input
            placeholder="Tên bộ lọc, VD: Hồ sơ quá hạn lĩnh vực Giáo dục"
            value={tenMoi}
            maxLength={100}
            onChange={(e) => setTenMoi(e.target.value)}
          />
          <Checkbox
            checked={datMacDinhKhiLuu}
            onChange={(e) => setDatMacDinhKhiLuu(e.target.checked)}
          >
            Đặt làm bộ lọc mặc định của màn hình này
          </Checkbox>
          <div style={{ fontSize: 12, color: 'rgba(0,0,0,0.45)' }}>
            Lưu trùng tên sẽ ghi đè bộ lọc cũ. Tiêu chí được lưu:{' '}
            <code>{thamSoHienTai || '(không có tiêu chí nào)'}</code>
          </div>
        </Space>
      </Modal>
    </>
  );
}
