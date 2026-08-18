using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.DanhMuc;
using BlueIdea.Shared.KetQua;
using BlueIdea.Shared.TiengViet;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.DanhMuc;

/// <summary>Mot dong danh muc doc tu tep Excel.</summary>
public sealed record DongNhapDanhMuc(int SoDong, string Ma, string Ten, string? MoTa, string? ThuTu);

/// <summary>Ket qua kiem tra / nhap mot dong.</summary>
public sealed record KetQuaDongDanhMuc(int SoDong, string Ma, string Ten, bool HopLe, string? Loi);

/// <summary>Ket qua tong the cua mot lan nhap danh muc.</summary>
public sealed record KetQuaNhapDanhMuc(
    bool ChayThu,
    string Loai,
    int TongDong,
    int SoHopLe,
    int SoLoi,
    int SoThemMoi,
    int SoCapNhat,
    IReadOnlyList<KetQuaDongDanhMuc> ChiTiet);

/// <summary>
/// Nhap danh muc dung chung (linh vuc / doi tuong / loai tac gia) tu Excel.
///
/// Ma da ton tai thi CAP NHAT ten va mo ta, khong bao loi trung: cac don vi thuong gui lai ca
/// bang danh muc moi khi co thay doi, bat ho tach rieng dong moi va dong sua la bat lam thu cong
/// dung viec ma may lam duoc.
///
/// KHONG doi trang thai cua ban ghi da co: mot danh muc bi ngung dung co the do nghiep vu chu
/// dong tat, tep nhap khong nen am tham bat lai.
/// </summary>
public sealed class DichVuNhapDanhMuc
{
    private readonly IAppDbContext _db;
    private readonly IDichVuPhanQuyen _phanQuyen;
    private readonly IDongHoHeThong _dongHo;

    public DichVuNhapDanhMuc(IAppDbContext db, IDichVuPhanQuyen phanQuyen, IDongHoHeThong dongHo)
    {
        _db = db;
        _phanQuyen = phanQuyen;
        _dongHo = dongHo;
    }

    /// <summary>Cac loai danh muc nhap duoc bang tep Excel.</summary>
    public static IReadOnlyList<string> LoaiHoTro { get; } =
        new[] { "linh-vuc", "doi-tuong", "loai-tac-gia" };

    public async Task<KetQuaNhapDanhMuc> NhapAsync(
        string loai, IReadOnlyList<DongNhapDanhMuc> cacDong, bool chayThu,
        CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.DanhMucSua, ct: ct).ConfigureAwait(false);

        if (!LoaiHoTro.Contains(loai))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                $"Không hỗ trợ nhập danh mục '{loai}'.");
        }

        var chiTiet = new List<KetQuaDongDanhMuc>();
        var maTrongTep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hopLe = new List<DongNhapDanhMuc>();

        foreach (var dong in cacDong)
        {
            var loi = KiemTraDong(dong, maTrongTep);

            chiTiet.Add(new KetQuaDongDanhMuc(dong.SoDong, dong.Ma, dong.Ten, loi is null, loi));

            if (loi is null)
            {
                maTrongTep.Add(dong.Ma.Trim());
                hopLe.Add(dong);
            }
        }

        var maCanTim = hopLe.Select(x => x.Ma.Trim()).ToList();
        var daCo = await LayTheoMaAsync(loai, maCanTim, ct).ConfigureAwait(false);

        var soThem = hopLe.Count(x => !daCo.ContainsKey(x.Ma.Trim()));
        var soSua = hopLe.Count - soThem;

        if (!chayThu && hopLe.Count > 0)
        {
            await GhiAsync(loai, hopLe, daCo, ct).ConfigureAwait(false);
        }

        return new KetQuaNhapDanhMuc(
            chayThu, loai, cacDong.Count, hopLe.Count, cacDong.Count - hopLe.Count,
            soThem, soSua, chiTiet);
    }

    private static string? KiemTraDong(DongNhapDanhMuc dong, HashSet<string> maTrongTep)
    {
        if (string.IsNullOrWhiteSpace(dong.Ma)) return "Thiếu mã.";
        if (string.IsNullOrWhiteSpace(dong.Ten)) return "Thiếu tên.";

        var ma = dong.Ma.Trim();

        if (ma.Length > 50) return "Mã dài quá 50 ký tự.";
        if (dong.Ten.Trim().Length > 300) return "Tên dài quá 300 ký tự.";

        if (!ma.All(c => char.IsLetterOrDigit(c) || c is '_' or '-'))
        {
            return "Mã chỉ được dùng chữ, số, dấu gạch dưới và gạch ngang.";
        }

        // Trung ma NGAY TRONG TEP thi khong the xu ly tu dong: khong biet dong nao la dong dung,
        // ghi ca hai thi dong sau de len dong truoc ma nguoi nhap khong hay biet.
        if (maTrongTep.Contains(ma)) return "Mã bị lặp trong chính tệp nhập.";

        if (!string.IsNullOrWhiteSpace(dong.ThuTu) && !int.TryParse(dong.ThuTu, out _))
        {
            return $"Thứ tự '{dong.ThuTu}' không phải số.";
        }

        return null;
    }

    private async Task<Dictionary<string, ThucTheDanhMuc>> LayTheoMaAsync(
        string loai, List<string> ma, CancellationToken ct)
    {
        List<ThucTheDanhMuc> danhSach = loai switch
        {
            "linh-vuc" => (await _db.LinhVuc.Where(x => ma.Contains(x.Ma)).ToListAsync(ct)
                .ConfigureAwait(false)).Cast<ThucTheDanhMuc>().ToList(),
            "doi-tuong" => (await _db.DoiTuong.Where(x => ma.Contains(x.Ma)).ToListAsync(ct)
                .ConfigureAwait(false)).Cast<ThucTheDanhMuc>().ToList(),
            _ => (await _db.LoaiTacGia.Where(x => ma.Contains(x.Ma)).ToListAsync(ct)
                .ConfigureAwait(false)).Cast<ThucTheDanhMuc>().ToList(),
        };

        return danhSach.ToDictionary(x => x.Ma, StringComparer.OrdinalIgnoreCase);
    }

    private async Task GhiAsync(
        string loai, List<DongNhapDanhMuc> hopLe, Dictionary<string, ThucTheDanhMuc> daCo,
        CancellationToken ct)
    {
        foreach (var dong in hopLe)
        {
            var ma = dong.Ma.Trim();
            var ten = dong.Ten.Trim();
            var thuTu = int.TryParse(dong.ThuTu, out var t) ? t : 0;

            if (daCo.TryGetValue(ma, out var banGhi))
            {
                banGhi.Ten = ten;
                banGhi.TenKhongDau = VanBanTiengViet.TaoKhongDau(ten);
                banGhi.MoTa = dong.MoTa;
                banGhi.ThuTu = thuTu;
                banGhi.NgaySua = _dongHo.BayGio;
                continue;
            }

            ThemMoi(loai, ma, ten, dong.MoTa, thuTu);
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private void ThemMoi(string loai, string ma, string ten, string? moTa, int thuTu)
    {
        var khongDau = VanBanTiengViet.TaoKhongDau(ten);

        switch (loai)
        {
            case "linh-vuc":
                _db.LinhVuc.Add(new LinhVuc
                {
                    Id = Guid.NewGuid(),
                    Ma = ma,
                    Ten = ten,
                    TenKhongDau = khongDau,
                    MoTa = moTa,
                    ThuTu = thuTu,
                    TrangThai = TrangThaiDanhMuc.HoatDong,
                    NgayTao = _dongHo.BayGio,
                });
                break;

            case "doi-tuong":
                _db.DoiTuong.Add(new DoiTuong
                {
                    Id = Guid.NewGuid(),
                    Ma = ma,
                    Ten = ten,
                    TenKhongDau = khongDau,
                    MoTa = moTa,
                    ThuTu = thuTu,
                    TrangThai = TrangThaiDanhMuc.HoatDong,
                    NgayTao = _dongHo.BayGio,
                });
                break;

            default:
                _db.LoaiTacGia.Add(new LoaiTacGia
                {
                    Id = Guid.NewGuid(),
                    Ma = ma,
                    Ten = ten,
                    TenKhongDau = khongDau,
                    MoTa = moTa,
                    ThuTu = thuTu,
                    TrangThai = TrangThaiDanhMuc.HoatDong,
                    NgayTao = _dongHo.BayGio,
                });
                break;
        }
    }
}
