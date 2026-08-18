using BlueIdea.Application.TichHop;
using BlueIdea.Domain.QuanTri;

namespace BlueIdea.Infrastructure.TichHop;

/// <summary>
/// Dung than yeu cau gui sang MOT he thong ngoai cu the.
///
/// Moi he thong dung chung cua tinh/thanh co hop dong du lieu rieng: Thi dua khen thuong nhan
/// ho so khen thuong, IOC nhan chi so tong hop de len bang dieu hanh. Ep chung mot dang than
/// thi ben nao cung phai tu viet lop chuyen doi o phia ho, va moi lan ho doi la minh phai sua
/// ma nguon thay vi sua cau hinh.
/// </summary>
public interface IBoAnhXaLienThong
{
    /// <summary>Ma he thong ma bo anh xa nay phuc vu (khop <see cref="MaHeThongTichHop"/>).</summary>
    string MaHeThong { get; }

    object TaoThan(HeThongTichHop heThong, IReadOnlyList<BanGhiDongBo> duLieu);
}

/// <summary>
/// Anh xa mac dinh — dung khi he thong dich khong co bo anh xa rieng.
///
/// Giu nguyen tung ban ghi va chi doi TEN TRUONG theo cau hinh anh xa, de them mot he thong moi
/// chi can khai bao trong bang <c>he_thong_tich_hop</c>, khong phai sua ma nguon.
/// </summary>
public sealed class AnhXaLienThongChung : IBoAnhXaLienThong
{
    public string MaHeThong => "*";

    public object TaoThan(HeThongTichHop heThong, IReadOnlyList<BanGhiDongBo> duLieu)
    {
        ArgumentNullException.ThrowIfNull(heThong);
        ArgumentNullException.ThrowIfNull(duLieu);

        return new
        {
            nguon = "BLUEIDEA",
            loaiDuLieu = "SANG_KIEN_DUOC_CONG_NHAN",
            tongBanGhi = duLieu.Count,
            duLieu = duLieu.Select(x => DoiTenTruong(TruongGoc(x), heThong.CauHinhMapping)).ToList()
        };
    }

    /// <summary>Bo truong goc dung chung cho moi bo anh xa.</summary>
    internal static Dictionary<string, object?> TruongGoc(BanGhiDongBo x)
    {
        ArgumentNullException.ThrowIfNull(x);

        return new Dictionary<string, object?>
        {
            ["maHoSo"] = x.MaHoSo,
            ["tenSangKien"] = x.TenSangKien,
            ["tacGiaChinh"] = x.TacGiaChinh,
            ["donVi"] = x.DonVi,
            ["linhVuc"] = x.LinhVuc,
            ["tongDiem"] = x.TongDiem,
            ["mucCongNhan"] = x.MucCongNhan,
            ["soQuyetDinh"] = x.SoQuyetDinh,
            ["ngayCongNhan"] = x.NgayCongNhan,
            ["nam"] = x.Nam,
        };
    }

    /// <summary>Doi ten truong theo cau hinh; khong co cau hinh thi giu nguyen ten goc.</summary>
    internal static Dictionary<string, object?> DoiTenTruong(
        Dictionary<string, object?> goc, IReadOnlyDictionary<string, string>? mapping)
    {
        if (mapping is not { Count: > 0 }) return goc;

        var ketQua = new Dictionary<string, object?>();

        foreach (var (tenGoc, giaTri) in goc)
        {
            var tenMoi = mapping.TryGetValue(tenGoc, out var t) && !string.IsNullOrWhiteSpace(t)
                ? t
                : tenGoc;

            ketQua[tenMoi] = giaTri;
        }

        return ketQua;
    }
}

/// <summary>
/// Anh xa sang he thong Thi dua — Khen thuong.
///
/// He thong nay lam viec theo DOT KHEN THUONG: no can biet dot nao, cap nao, va tung ho so gan
/// voi quyet dinh cong nhan nao. Vi vay gui kem khoi <c>dot</c> o cap ngoai thay vi lap lai nam
/// va cap tren tung ban ghi.
/// </summary>
public sealed class AnhXaThiDuaKhenThuong : IBoAnhXaLienThong
{
    public string MaHeThong => MaHeThongTichHop.ThiDuaKhenThuong;

    public object TaoThan(HeThongTichHop heThong, IReadOnlyList<BanGhiDongBo> duLieu)
    {
        ArgumentNullException.ThrowIfNull(heThong);
        ArgumentNullException.ThrowIfNull(duLieu);

        var nam = duLieu.Where(x => x.Nam.HasValue).Select(x => x.Nam!.Value).DefaultIfEmpty(0).Max();

        return new
        {
            nguon = "BLUEIDEA",
            loaiHoSo = "SANG_KIEN",
            dot = new
            {
                nam,
                capXetDuyet = heThong.CauHinhMapping?.GetValueOrDefault("__capXetDuyet") ?? "CO_SO",
                donViDeNghi = heThong.CauHinhMapping?.GetValueOrDefault("__donViDeNghi"),
            },
            tongHoSo = duLieu.Count,
            hoSo = duLieu.Select(x => AnhXaLienThongChung.DoiTenTruong(
                new Dictionary<string, object?>
                {
                    ["maHoSo"] = x.MaHoSo,
                    ["tenSangKien"] = x.TenSangKien,
                    ["tacGiaChinh"] = x.TacGiaChinh,
                    ["donVi"] = x.DonVi,
                    ["linhVuc"] = x.LinhVuc,
                    ["hinhThucKhenThuong"] = x.MucCongNhan,
                    ["soQuyetDinhCongNhan"] = x.SoQuyetDinh,
                    ["ngayQuyetDinh"] = x.NgayCongNhan,
                    ["diemDatDuoc"] = x.TongDiem,
                },
                heThong.CauHinhMapping)).ToList()
        };
    }
}

/// <summary>
/// Anh xa sang Trung tam dieu hanh thong minh (IOC).
///
/// IOC hien so lieu tren bang dieu hanh chu khong luu tung ho so, nen gui CHI SO TONG HOP kem
/// phan ra theo don vi va linh vuc. Day nguyen danh sach ho so sang IOC vua thua vua dua du lieu
/// chi tiet ve ca nhan len mot he thong khong can den no.
/// </summary>
public sealed class AnhXaIoc : IBoAnhXaLienThong
{
    public string MaHeThong => MaHeThongTichHop.Ioc;

    public object TaoThan(HeThongTichHop heThong, IReadOnlyList<BanGhiDongBo> duLieu)
    {
        ArgumentNullException.ThrowIfNull(heThong);
        ArgumentNullException.ThrowIfNull(duLieu);

        var nam = duLieu.Where(x => x.Nam.HasValue).Select(x => x.Nam!.Value).DefaultIfEmpty(0).Max();

        return new
        {
            nguon = "BLUEIDEA",
            maChiSo = heThong.CauHinhMapping?.GetValueOrDefault("__maChiSo") ?? "SANG_KIEN_CONG_NHAN",
            kyBaoCao = nam.ToString(System.Globalization.CultureInfo.InvariantCulture),
            tongSoSangKienCongNhan = duLieu.Count,
            diemTrungBinh = duLieu.Count == 0
                ? 0m
                : Math.Round(duLieu.Where(x => x.TongDiem.HasValue)
                    .Select(x => x.TongDiem!.Value)
                    .DefaultIfEmpty(0m)
                    .Average(), 2),
            theoDonVi = duLieu
                .GroupBy(x => x.DonVi ?? "(không rõ)")
                .Select(g => new { donVi = g.Key, soLuong = g.Count() })
                .OrderByDescending(x => x.soLuong)
                .ToList(),
            theoLinhVuc = duLieu
                .GroupBy(x => x.LinhVuc ?? "(không rõ)")
                .Select(g => new { linhVuc = g.Key, soLuong = g.Count() })
                .OrderByDescending(x => x.soLuong)
                .ToList()
        };
    }
}
