import { useMemo } from 'react';
import { Card, Tag, Tree, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import type { DataNode } from 'antd/es/tree';

import { apiDonVi, type NutCay } from '@/api/endpoints';
import { KhoiDangTai, KhoiLoi } from '@/components/ThanhPhanChung';

/** Chức năng 44 — Cây tổ chức đơn vị. */
export default function TrangDonVi() {
  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['don-vi', 'cay'],
    queryFn: apiDonVi.cay,
  });

  const cay = useMemo(() => chuyenDoi(data ?? []), [data]);

  if (isLoading) return <KhoiDangTai />;
  if (error) return <KhoiLoi loi={error} thuLai={refetch} />;

  return (
    <Card title="Cơ cấu tổ chức">
      <Typography.Paragraph type="secondary">
        Cây đơn vị dùng chung cho phân quyền theo phạm vi dữ liệu và cấp phê duyệt.
      </Typography.Paragraph>

      <Tree treeData={cay} defaultExpandAll showLine blockNode />
    </Card>
  );
}

function chuyenDoi(danhSach: NutCay[]): DataNode[] {
  return danhSach.map((n) => ({
    key: n.id,
    title: (
      <span>
        {n.ten} <Tag>{n.ma}</Tag>
        {n.trangThai !== 1 && <Tag color="default">Ngừng</Tag>}
      </span>
    ),
    children: n.con.length > 0 ? chuyenDoi(n.con) : undefined,
  }));
}
