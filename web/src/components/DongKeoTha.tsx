import { createContext, useContext, useMemo } from 'react';
import { DndContext, KeyboardSensor, PointerSensor, closestCenter, useSensor, useSensors } from '@dnd-kit/core';
import type { DragEndEvent } from '@dnd-kit/core';
import {
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { HolderOutlined } from '@ant-design/icons';
import { Button } from 'antd';

interface NgamKeoTha {
  keoDuoc: boolean;
}

const NgamDong = createContext<NgamKeoTha>({ keoDuoc: false });

/**
 * Bọc một bảng antd để kéo–thả đổi thứ tự dòng.
 *
 * Vẫn giữ nút lên/xuống ở nơi dùng: kéo–thả trong bảng cuộn ngang trên điện thoại gần như không
 * dùng nổi, và người thao tác bằng bàn phím cũng cần một đường khác. dnd-kit có sẵn cảm biến bàn
 * phím nên bản thân thao tác kéo cũng dùng được bằng Space + phím mũi tên.
 */
export function BocKeoTha({
  khoaTheoThuTu,
  keoDuoc = true,
  onDoiThuTu,
  children,
}: {
  khoaTheoThuTu: string[];
  keoDuoc?: boolean;
  onDoiThuTu: (khoaMoi: string[]) => void;
  children: React.ReactNode;
}) {
  const camBien = useSensors(
    // Phải kéo quá 5px mới tính là kéo, nếu không mỗi lần bấm nút trong dòng đều bị hiểu nhầm.
    useSensor(PointerSensor, { activationConstraint: { distance: 5 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  );

  const ngam = useMemo(() => ({ keoDuoc }), [keoDuoc]);

  function ketThucKeo(su: DragEndEvent) {
    const { active, over } = su;
    if (!over || active.id === over.id) return;

    const tu = khoaTheoThuTu.indexOf(String(active.id));
    const den = khoaTheoThuTu.indexOf(String(over.id));
    if (tu < 0 || den < 0) return;

    const moi = [...khoaTheoThuTu];
    moi.splice(den, 0, moi.splice(tu, 1)[0]);

    onDoiThuTu(moi);
  }

  return (
    <NgamDong.Provider value={ngam}>
      <DndContext
        sensors={camBien}
        collisionDetection={closestCenter}
        onDragEnd={ketThucKeo}
      >
        <SortableContext items={khoaTheoThuTu} strategy={verticalListSortingStrategy}>
          {children}
        </SortableContext>
      </DndContext>
    </NgamDong.Provider>
  );
}

/** Dòng bảng kéo được — truyền vào `components.body.row` của antd Table. */
export function DongKeoTha({
  'data-row-key': khoa,
  ...conLai
}: React.HTMLAttributes<HTMLTableRowElement> & { 'data-row-key': string }) {
  const { keoDuoc } = useContext(NgamDong);
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: khoa,
    disabled: !keoDuoc,
  });

  return (
    <tr
      {...conLai}
      ref={setNodeRef}
      style={{
        ...conLai.style,
        // Chỉ lấy dịch chuyển theo trục dọc: bảng đổi thứ tự dòng, kéo ngang không có nghĩa gì
        // và làm dòng trôi ra khỏi bảng khi bảng có cuộn ngang.
        transform: transform ? CSS.Translate.toString({ ...transform, x: 0 }) : undefined,
        transition,
        ...(isDragging ? { position: 'relative', zIndex: 9, background: '#e6f4ff' } : {}),
      }}
      {...attributes}
    >
      {Array.isArray(conLai.children)
        ? conLai.children.map((o) =>
            (o as { key?: string })?.key === 'tay-cam' ? (
              <td key="tay-cam" style={{ width: 44, textAlign: 'center' }}>
                {keoDuoc ? (
                  <Button
                    type="text"
                    size="small"
                    icon={<HolderOutlined />}
                    style={{ cursor: 'move' }}
                    {...listeners}
                  />
                ) : null}
              </td>
            ) : (
              o
            ),
          )
        : conLai.children}
    </tr>
  );
}
