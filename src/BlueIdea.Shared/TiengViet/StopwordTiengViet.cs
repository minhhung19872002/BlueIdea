namespace BlueIdea.Shared.TiengViet;

/// <summary>
/// Danh sach tu dung (stopword) tieng Viet dung khi chuan hoa van ban truoc so khop trung lap.
/// Tu trong danh sach da bo dau va viet thuong, khop voi ket qua cua
/// <see cref="VanBanTiengViet.ChuanHoaDeSoKhop"/>.
///
/// QUAN TRONG - danh sach nay duoc chon rat than trong:
/// vi so khop chay tren van ban DA BO DAU, nhieu hu tu sau khi bo dau se trung voi
/// thuc tu quan trong cua nghiep vu. Vi du: "hồ sơ" -> "ho so", "đơn vị" -> "don vi",
/// "trọng số" -> "trong so", "văn bản" -> "van ban", "tài liệu" -> "tai lieu",
/// "đề nghị" -> "de nghi", "năm" -> "nam", "mã" -> "ma", "kết quả" -> "ket qua".
/// Neu dua cac tu "ho", "so", "vi", "trong", "van", "ban", "tai", "de", "nam", "ma", "qua"
/// vao stopword thi cac thuat ngu tren bi pha huy, lam sai lech ket qua kiem tra trung lap.
/// Vi vay chi giu lai nhung hu tu KHONG trung voi thuat ngu nghiep vu sau khi bo dau.
/// </summary>
public static class StopwordTiengViet
{
    private static readonly HashSet<string> DanhSach = new(StringComparer.Ordinal)
    {
        // Lien tu / gioi tu an toan
        "va", "cua", "cac", "nhung", "duoc", "voi", "den", "mot", "nay",
        "khi", "tren", "theo", "se", "boi", "neu", "nhu", "hoac", "hay",
        "ra", "vao", "len", "xuong", "lai", "hon", "nua", "tat",
        "nhieu", "sau", "truoc", "giua", "ngoai", "chung", "minh",

        // Tu de hoi / tro tu an toan
        "ai", "gi", "nao", "sao", "khong", "chua", "chang", "vua", "tung",
        "luon", "rat", "phai", "la"
    };

    public static bool LaStopword(string tu) => DanhSach.Contains(tu);

    public static IReadOnlyCollection<string> TatCa => DanhSach;
}
