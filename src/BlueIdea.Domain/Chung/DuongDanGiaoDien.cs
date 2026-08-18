namespace BlueIdea.Domain.Chung;

/// <summary>
/// Duong dan man hinh web ma may chu can nhac toi — hien tai la lien ket dinh kem trong thong bao.
///
/// Vi sao gom ve mot cho: truoc day moi noi tu ghep chuoi lay, va ca ba lien ket deu tro sai
/// (<c>/ho-so/{id}</c>, <c>/xu-ly/{id}</c>, <c>/danh-gia/{id}</c> — khong route nao ton tai).
/// Bam vao thong bao la ra trang 404 bao "khong ton tai hoac ban khong co quyen", trong khi ho so
/// co that va nguoi dung co quyen xem. Sai lang le nhu vay vi khong co gi noi hai ben voi nhau:
/// duong dan la chuoi o C#, route khai o TypeScript.
///
/// Nay chi co MOT cho de sai, va co kiem thu doi chieu thang voi <c>web/src/app/router.tsx</c>.
/// Sua duong dan o day thi phai sua router va nguoc lai.
/// </summary>
public static class DuongDanGiaoDien
{
    /// <summary>Chi tiet ho so — cung la noi bam nut xu ly buoc. Khop route <c>sang-kien/:id</c>.</summary>
    public static string ChiTietHoSo(Guid sangKienId) => $"/sang-kien/{sangKienId}";

    /// <summary>Man hinh cham diem mot ho so. Khop route <c>danh-gia/:id/cham-diem</c>.</summary>
    public static string ChamDiem(Guid sangKienId) => $"/danh-gia/{sangKienId}/cham-diem";

    /// <summary>Chi tiet hoi dong, noi xem phien hop. Khop route <c>hoi-dong/:id</c>.</summary>
    public static string ChiTietHoiDong(Guid hoiDongId) => $"/hoi-dong/{hoiDongId}";

    /// <summary>
    /// Danh sach viec cham diem duoc giao. Khop route <c>danh-gia</c>.
    ///
    /// Dung cho thong bao phan cong: mot lan phan cong thuong gom nhieu ho so nen khong co ho so
    /// don le nao de tro toi.
    /// </summary>
    public const string DanhSachViecDanhGia = "/danh-gia";
}
