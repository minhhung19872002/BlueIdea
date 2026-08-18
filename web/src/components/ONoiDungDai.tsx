import { useMemo, useRef, useState } from 'react';
import { Button, Input, Progress, Space, Tabs, Tooltip, Typography } from 'antd';
import {
  BoldOutlined,
  ItalicOutlined,
  OrderedListOutlined,
  UnorderedListOutlined,
} from '@ant-design/icons';
import type { TextAreaRef } from 'antd/es/input/TextArea';

interface Props {
  value?: string;
  onChange?: (giaTri: string) => void;
  rows?: number;
  disabled?: boolean;
  placeholder?: string;
  /** Số ký tự tối thiểu — hiện thanh tiến độ để người viết biết còn thiếu bao nhiêu. */
  soKyTuToiThieu?: number;
  maxLength?: number;
}

/**
 * Ô soạn nội dung dài cho hồ sơ sáng kiến (chức năng 24).
 *
 * Định dạng dùng ký hiệu Markdown rút gọn và LƯU NGUYÊN VĂN BẢN THƯỜNG, không lưu HTML: nội dung
 * hồ sơ được đổ vào PDF, Excel và cổng công khai — lưu HTML thì mỗi nơi lại phải tự lọc thẻ, và
 * chỉ cần một chỗ quên lọc là thành lỗ hổng chèn mã. Bản xem trước dựng bằng phần tử React chứ
 * không đổ chuỗi vào innerHTML, nên người dùng gõ thẻ HTML vào đây cũng chỉ hiện ra chữ.
 */
export function ONoiDungDai({
  value = '',
  onChange,
  rows = 6,
  disabled,
  placeholder,
  soKyTuToiThieu = 0,
  maxLength,
}: Props) {
  const oRef = useRef<TextAreaRef>(null);
  const [tab, setTab] = useState('soan');

  const soTu = useMemo(() => value.trim().split(/\s+/).filter(Boolean).length, [value]);
  const dat = value.trim().length >= soKyTuToiThieu;

  /** Bọc phần đang bôi đen bằng ký hiệu định dạng; không bôi đen thì chèn tại con trỏ. */
  function boc(kyHieu: string) {
    const o = oRef.current?.resizableTextArea?.textArea;
    if (!o) return;

    const dau = o.selectionStart;
    const cuoi = o.selectionEnd;
    const chon = value.slice(dau, cuoi) || 'nội dung';
    const moi = `${value.slice(0, dau)}${kyHieu}${chon}${kyHieu}${value.slice(cuoi)}`;

    onChange?.(moi);

    // Đặt lại con trỏ vào giữa cặp ký hiệu để gõ tiếp được ngay.
    requestAnimationFrame(() => {
      o.focus();
      o.setSelectionRange(dau + kyHieu.length, dau + kyHieu.length + chon.length);
    });
  }

  /** Thêm ký hiệu đầu dòng cho các dòng đang bôi đen. */
  function danhSach(coSo: boolean) {
    const o = oRef.current?.resizableTextArea?.textArea;
    if (!o) return;

    const dau = value.lastIndexOf('\n', Math.max(0, o.selectionStart - 1)) + 1;
    const cuoiTam = value.indexOf('\n', o.selectionEnd);
    const cuoi = cuoiTam === -1 ? value.length : cuoiTam;

    const dong = value.slice(dau, cuoi).split('\n');
    const daDanhDau = dong.map((d, i) => (coSo ? `${i + 1}. ${d}` : `- ${d}`));

    onChange?.(`${value.slice(0, dau)}${daDanhDau.join('\n')}${value.slice(cuoi)}`);
    requestAnimationFrame(() => o.focus());
  }

  return (
    <div>
      <Tabs
        size="small"
        activeKey={tab}
        onChange={setTab}
        tabBarExtraContent={
          tab === 'soan' && !disabled ? (
            <Space size={2}>
              <Tooltip title="Chữ đậm">
                <Button size="small" type="text" icon={<BoldOutlined />} onClick={() => boc('**')} />
              </Tooltip>
              <Tooltip title="Chữ nghiêng">
                <Button size="small" type="text" icon={<ItalicOutlined />} onClick={() => boc('*')} />
              </Tooltip>
              <Tooltip title="Gạch đầu dòng">
                <Button
                  size="small"
                  type="text"
                  icon={<UnorderedListOutlined />}
                  onClick={() => danhSach(false)}
                />
              </Tooltip>
              <Tooltip title="Danh sách đánh số">
                <Button
                  size="small"
                  type="text"
                  icon={<OrderedListOutlined />}
                  onClick={() => danhSach(true)}
                />
              </Tooltip>
            </Space>
          ) : null
        }
        items={[
          {
            key: 'soan',
            label: 'Soạn thảo',
            children: (
              <Input.TextArea
                ref={oRef}
                rows={rows}
                value={value}
                disabled={disabled}
                placeholder={placeholder}
                maxLength={maxLength}
                onChange={(e) => onChange?.(e.target.value)}
              />
            ),
          },
          {
            key: 'xem',
            label: 'Xem trước',
            children: (
              <div
                style={{
                  minHeight: rows * 22,
                  padding: 12,
                  border: '1px solid rgba(0,0,0,0.1)',
                  borderRadius: 6,
                }}
              >
                {value.trim() ? (
                  <XemTruocVanBan noiDung={value} />
                ) : (
                  <Typography.Text type="secondary">Chưa có nội dung.</Typography.Text>
                )}
              </div>
            ),
          },
        ]}
      />

      <Space size="large" style={{ marginTop: 4 }} wrap>
        <Typography.Text type="secondary" style={{ fontSize: 12 }}>
          {value.length} ký tự · {soTu} từ
        </Typography.Text>

        {soKyTuToiThieu > 0 && (
          <Space size={6}>
            <Progress
              size="small"
              style={{ width: 120, marginBottom: 0 }}
              percent={Math.min(100, Math.round((value.trim().length / soKyTuToiThieu) * 100))}
              status={dat ? 'success' : 'active'}
              showInfo={false}
            />
            <Typography.Text type={dat ? 'success' : 'warning'} style={{ fontSize: 12 }}>
              {dat
                ? `Đủ tối thiểu ${soKyTuToiThieu} ký tự`
                : `Còn thiếu ${soKyTuToiThieu - value.trim().length} ký tự`}
            </Typography.Text>
          </Space>
        )}
      </Space>
    </div>
  );
}

/**
 * Dựng nội dung từ Markdown rút gọn — chỉ đậm, nghiêng và danh sách.
 *
 * Dùng chung cho ô soạn thảo và màn hình xem hồ sơ: nội dung nhập bằng ký hiệu nào thì chỗ đọc
 * phải hiểu đúng ký hiệu đó, nếu không người xem thấy nguyên dấu ** trong văn bản.
 */
export function XemTruocVanBan({ noiDung }: { noiDung: string }) {
  return (
    <>
      {noiDung.split('\n').map((dong, i) => {
        const dsKhongSo = /^\s*[-*]\s+(.*)$/.exec(dong);
        const dsCoSo = /^\s*\d+\.\s+(.*)$/.exec(dong);
        const noiDungDong = dsKhongSo?.[1] ?? dsCoSo?.[1] ?? dong;

        const phanTu = <DongCoDinhDang chuoi={noiDungDong} />;

        if (dsKhongSo || dsCoSo) {
          return (
            <div key={i} style={{ display: 'flex', gap: 8, marginBottom: 2 }}>
              <span>{dsCoSo ? `${dong.trim().split('.')[0]}.` : '•'}</span>
              <span>{phanTu}</span>
            </div>
          );
        }

        return dong.trim() === '' ? (
          <div key={i} style={{ height: 8 }} />
        ) : (
          <div key={i} style={{ marginBottom: 2 }}>
            {phanTu}
          </div>
        );
      })}
    </>
  );
}

function DongCoDinhDang({ chuoi }: { chuoi: string }) {
  const phan = chuoi.split(/(\*\*[^*]+\*\*|\*[^*]+\*)/g).filter(Boolean);

  return (
    <>
      {phan.map((x, i) => {
        if (x.startsWith('**') && x.endsWith('**')) {
          return <strong key={i}>{x.slice(2, -2)}</strong>;
        }
        if (x.startsWith('*') && x.endsWith('*') && x.length > 2) {
          return <em key={i}>{x.slice(1, -1)}</em>;
        }
        return <span key={i}>{x}</span>;
      })}
    </>
  );
}
