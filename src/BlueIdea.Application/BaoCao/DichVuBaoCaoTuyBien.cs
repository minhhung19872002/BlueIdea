using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.DanhMuc;
using BlueIdea.Domain.SangKien;
using BlueIdea.Shared.KetQua;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.BaoCao;

/// <summary>Mot cot da duoc giai nghia tu cau hinh, kem ham lay gia tri.</summary>
public sealed record CotDaGiaiNghia(string Ma, string TieuDe, string Nguon, string? HamTongHop);

public sealed record KetQuaBaoCaoTuyBien(
    string TenBaoCao,
    IReadOnlyList<string> TieuDeCot,
    IReadOnlyList<IReadOnlyList<string>> CacDong,
    IReadOnlyDictionary<string, string> BoLocDaApDung,
    int TongSo);

/// <summary>
/// Chuc nang 7 - Sinh bao cao thong ke tu cau hinh trong bang <c>bieu_mau_thong_ke</c>,
/// khong hardcode tung bao cao.
///
/// Nguon du lieu duoc chon tu mot BANG TRANG cac truong da biet. Tuyet doi khong ghep chuoi SQL
/// hay dung reflection theo ten do quan tri vien nhap - do la duong dan tro thanh lo hong injection.
/// </summary>
public sealed class DichVuBaoCaoTuyBien
{
    /// <summary>Cac truong duoc phep dua vao bao cao, anh xa sang ham lay gia tri.</summary>
    private static readonly Dictionary<string, Func<DongDuLieuBaoCao, object?>> NguonChoPhep =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["maHoSo"] = x => x.MaHoSo,
            ["tenSangKien"] = x => x.TenSangKien,
            ["tacGiaChinh"] = x => x.TacGiaChinh,
            ["tenDonVi"] = x => x.TenDonVi,
            ["tenLinhVuc"] = x => x.TenLinhVuc,
            ["tenDot"] = x => x.TenDot,
            ["trangThaiTong"] = x => x.TrangThaiTong,
            ["ketQua"] = x => x.KetQua,
            ["tongDiem"] = x => x.TongDiem,
            ["diemTrungBinh"] = x => x.DiemTrungBinh,
            ["tenMucCongNhan"] = x => x.TenMucCongNhan,
            ["tyLeTrungLap"] = x => x.TyLeTrungLap,
            ["ngayNop"] = x => x.NgayNop,
            ["ngayCongNhan"] = x => x.NgayCongNhan,
            ["soQuyetDinh"] = x => x.SoQuyetDinh,
            ["daCongBo"] = x => x.DaCongBo,
            ["phamViApDung"] = x => x.PhamViApDung,
            ["giaTriLamLoi"] = x => x.GiaTriLamLoi
        };

    /// <summary>Ham tong hop duoc phep khai bao trong cau hinh cot.</summary>
    private static readonly HashSet<string> HamChoPhep =
        new(StringComparer.OrdinalIgnoreCase) { "DEM", "TONG", "TRUNG_BINH", "LON_NHAT", "NHO_NHAT" };

    private readonly IAppDbContext _db;
    private readonly IDichVuPhanQuyen _phanQuyen;

    public DichVuBaoCaoTuyBien(IAppDbContext db, IDichVuPhanQuyen phanQuyen)
    {
        _db = db;
        _phanQuyen = phanQuyen;
    }

    /// <summary>Danh sach truong co the dua vao bao cao — man hinh cau hinh doc tu day.</summary>
    public static IReadOnlyList<(string Ma, string TieuDeGoiY)> NguonDuLieuKhaDung() => new[]
    {
        ("maHoSo", "Mã hồ sơ"),
        ("tenSangKien", "Tên sáng kiến"),
        ("tacGiaChinh", "Tác giả chính"),
        ("tenDonVi", "Đơn vị"),
        ("tenLinhVuc", "Lĩnh vực"),
        ("tenDot", "Đợt đề nghị"),
        ("trangThaiTong", "Trạng thái"),
        ("ketQua", "Kết quả"),
        ("tongDiem", "Tổng điểm"),
        ("diemTrungBinh", "Điểm trung bình"),
        ("tenMucCongNhan", "Mức công nhận"),
        ("tyLeTrungLap", "Tỷ lệ trùng lặp (%)"),
        ("ngayNop", "Ngày nộp"),
        ("ngayCongNhan", "Ngày công nhận"),
        ("soQuyetDinh", "Số quyết định"),
        ("daCongBo", "Đã công bố"),
        ("phamViApDung", "Phạm vi áp dụng"),
        ("giaTriLamLoi", "Giá trị làm lợi ước tính")
    };

    /// <summary>Kiem tra cau hinh bao cao truoc khi luu — bao loi ro thay vi de vo luc chay.</summary>
    public static IReadOnlyList<string> KiemTraCauHinh(IReadOnlyList<CotBaoCao> cacCot)
    {
        var loi = new List<string>();

        if (cacCot.Count == 0)
        {
            loi.Add("Báo cáo phải có ít nhất một cột.");
            return loi;
        }

        foreach (var cot in cacCot)
        {
            if (string.IsNullOrWhiteSpace(cot.Nguon) || !NguonChoPhep.ContainsKey(cot.Nguon))
            {
                loi.Add($"Cột '{cot.TieuDe}': nguồn dữ liệu '{cot.Nguon}' không nằm trong danh sách cho phép.");
            }

            if (!string.IsNullOrWhiteSpace(cot.HamTongHop) && !HamChoPhep.Contains(cot.HamTongHop))
            {
                loi.Add($"Cột '{cot.TieuDe}': hàm tổng hợp '{cot.HamTongHop}' không hợp lệ "
                        + $"(chỉ {string.Join(", ", HamChoPhep)}).");
            }
        }

        var trung = cacCot.GroupBy(x => x.Ma, StringComparer.OrdinalIgnoreCase)
            .Where(g => !string.IsNullOrWhiteSpace(g.Key) && g.Count() > 1)
            .Select(g => g.Key);

        foreach (var ma in trung)
        {
            loi.Add($"Mã cột '{ma}' bị trùng.");
        }

        return loi;
    }

    public async Task<KetQuaBaoCaoTuyBien> ChayAsync(
        Guid bieuMauId, Guid? dotDeNghiId, Guid? donViId, Guid? linhVucId, string? ketQua,
        CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.BaoCaoXem, ct: ct).ConfigureAwait(false);

        var bieuMau = await _db.BieuMauThongKe.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == bieuMauId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("biểu mẫu thống kê", bieuMauId);

        var loiCauHinh = KiemTraCauHinh(bieuMau.CauHinhCot);
        if (loiCauHinh.Count > 0)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Cấu hình báo cáo không hợp lệ: " + string.Join(" ", loiCauHinh));
        }

        var cot = bieuMau.CauHinhCot
            .OrderBy(x => x.ThuTu)
            .Select(x => new CotDaGiaiNghia(x.Ma, x.TieuDe, x.Nguon!, x.HamTongHop))
            .ToList();

        var duLieu = await NapDuLieuAsync(dotDeNghiId, donViId, linhVucId, ketQua, ct)
            .ConfigureAwait(false);

        var cacDong = duLieu
            .Select(dong => (IReadOnlyList<string>)cot
                .Select(c => DinhDang(NguonChoPhep[c.Nguon](dong)))
                .ToList())
            .ToList();

        // Dong tong hop chi xuat hien khi co it nhat mot cot khai bao ham.
        if (cot.Any(c => !string.IsNullOrWhiteSpace(c.HamTongHop)))
        {
            cacDong.Add(TinhDongTongHop(cot, duLieu));
        }

        var boLoc = new Dictionary<string, string>();
        await ThemMoTaBoLocAsync(boLoc, dotDeNghiId, donViId, linhVucId, ketQua, ct)
            .ConfigureAwait(false);

        return new KetQuaBaoCaoTuyBien(
            bieuMau.Ten,
            cot.Select(c => c.TieuDe).ToList(),
            cacDong,
            boLoc,
            duLieu.Count);
    }

    // -----------------------------------------------------------------------------------

    private async Task<List<DongDuLieuBaoCao>> NapDuLieuAsync(
        Guid? dotDeNghiId, Guid? donViId, Guid? linhVucId, string? ketQua, CancellationToken ct)
    {
        var truyVan = _db.SangKien.AsNoTracking()
            .Where(x => x.TrangThaiTong != TrangThaiTongHoSo.Nhap);

        if (dotDeNghiId.HasValue)
        {
            truyVan = truyVan.Where(x => x.DotDeNghiId == dotDeNghiId.Value);
        }

        if (donViId.HasValue)
        {
            truyVan = truyVan.Where(x => x.DonViId == donViId.Value);
        }

        if (linhVucId.HasValue)
        {
            truyVan = truyVan.Where(x => x.LinhVucId == linhVucId.Value);
        }

        if (!string.IsNullOrWhiteSpace(ketQua))
        {
            truyVan = truyVan.Where(x => x.KetQua == ketQua);
        }

        return await truyVan
            .OrderByDescending(x => x.NgayNop)
            .Select(x => new DongDuLieuBaoCao
            {
                MaHoSo = x.MaHoSo,
                TenSangKien = x.TenSangKien,
                TacGiaChinh = _db.SangKienTacGia
                    .Where(t => t.SangKienId == x.Id && t.LaTacGiaChinh)
                    .Select(t => t.HoTen).FirstOrDefault(),
                TenDonVi = _db.DonVi.Where(d => d.Id == x.DonViId).Select(d => d.Ten).FirstOrDefault(),
                TenLinhVuc = _db.LinhVuc.Where(l => l.Id == x.LinhVucId).Select(l => l.Ten).FirstOrDefault(),
                TenDot = _db.DotDeNghi.Where(d => d.Id == x.DotDeNghiId).Select(d => d.Ten).FirstOrDefault(),
                TrangThaiTong = x.TrangThaiTong,
                KetQua = x.KetQua,
                TongDiem = x.TongDiem,
                DiemTrungBinh = x.DiemTrungBinh,
                TenMucCongNhan = _db.MucCongNhan
                    .Where(m => m.Id == x.MucCongNhanId).Select(m => m.Ten).FirstOrDefault(),
                TyLeTrungLap = x.TyLeTrungLap,
                NgayNop = x.NgayNop,
                NgayCongNhan = x.NgayCongNhan,
                SoQuyetDinh = _db.QuyetDinh
                    .Where(q => q.Id == x.QuyetDinhId).Select(q => q.SoQuyetDinh).FirstOrDefault(),
                DaCongBo = x.DaCongBoKetQua,
                PhamViApDung = x.PhamViApDung,
                GiaTriLamLoi = x.GiaTriLamLoiUocTinh
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private static IReadOnlyList<string> TinhDongTongHop(
        IReadOnlyList<CotDaGiaiNghia> cot, IReadOnlyList<DongDuLieuBaoCao> duLieu)
    {
        var dong = new List<string>(cot.Count);

        foreach (var c in cot)
        {
            if (string.IsNullOrWhiteSpace(c.HamTongHop))
            {
                dong.Add(string.Empty);
                continue;
            }

            var lay = NguonChoPhep[c.Nguon];

            if (c.HamTongHop.Equals("DEM", StringComparison.OrdinalIgnoreCase))
            {
                dong.Add(duLieu.Count(x => lay(x) is not null).ToString());
                continue;
            }

            var so = duLieu
                .Select(lay)
                .Select(ChuyenSangSo)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();

            if (so.Count == 0)
            {
                dong.Add("—");
                continue;
            }

            var giaTri = c.HamTongHop.ToUpperInvariant() switch
            {
                "TONG" => so.Sum(),
                "TRUNG_BINH" => Math.Round(so.Average(), 2, MidpointRounding.AwayFromZero),
                "LON_NHAT" => so.Max(),
                "NHO_NHAT" => so.Min(),
                _ => 0m
            };

            dong.Add(giaTri.ToString("#,##0.##"));
        }

        // Cot dau tien khong khai bao ham thi dung lam nhan cho de doc.
        var viTriNhan = cot.ToList().FindIndex(c => string.IsNullOrWhiteSpace(c.HamTongHop));
        if (viTriNhan >= 0)
        {
            dong[viTriNhan] = "TỔNG CỘNG";
        }

        return dong;
    }

    private static decimal? ChuyenSangSo(object? giaTri) => giaTri switch
    {
        null => null,
        decimal m => m,
        int i => i,
        long l => l,
        double d => (decimal)d,
        bool b => b ? 1m : 0m,
        _ => null
    };

    private static string DinhDang(object? giaTri) => giaTri switch
    {
        null => string.Empty,
        bool b => b ? "Có" : "Không",
        decimal m => m.ToString("#,##0.##"),
        DateTimeOffset dto => dto.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm"),
        DateOnly d => d.ToString("dd/MM/yyyy"),
        _ => giaTri.ToString() ?? string.Empty
    };

    private async Task ThemMoTaBoLocAsync(
        IDictionary<string, string> boLoc, Guid? dotDeNghiId, Guid? donViId, Guid? linhVucId,
        string? ketQua, CancellationToken ct)
    {
        if (dotDeNghiId.HasValue)
        {
            var ten = await _db.DotDeNghi.AsNoTracking()
                .Where(x => x.Id == dotDeNghiId.Value).Select(x => x.Ten)
                .FirstOrDefaultAsync(ct).ConfigureAwait(false);

            boLoc["Đợt đề nghị"] = ten ?? dotDeNghiId.Value.ToString();
        }

        if (donViId.HasValue)
        {
            var ten = await _db.DonVi.AsNoTracking()
                .Where(x => x.Id == donViId.Value).Select(x => x.Ten)
                .FirstOrDefaultAsync(ct).ConfigureAwait(false);

            boLoc["Đơn vị"] = ten ?? donViId.Value.ToString();
        }

        if (linhVucId.HasValue)
        {
            var ten = await _db.LinhVuc.AsNoTracking()
                .Where(x => x.Id == linhVucId.Value).Select(x => x.Ten)
                .FirstOrDefaultAsync(ct).ConfigureAwait(false);

            boLoc["Lĩnh vực"] = ten ?? linhVucId.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(ketQua))
        {
            boLoc["Kết quả"] = ketQua == KetQuaXetDuyetGiaTri.Dat ? "Đạt" : "Không đạt";
        }
    }
}

/// <summary>Ban ghi phang phuc vu bao cao tuy bien.</summary>
public sealed class DongDuLieuBaoCao
{
    public string MaHoSo { get; init; } = string.Empty;

    public string TenSangKien { get; init; } = string.Empty;

    public string? TacGiaChinh { get; init; }

    public string? TenDonVi { get; init; }

    public string? TenLinhVuc { get; init; }

    public string? TenDot { get; init; }

    public string TrangThaiTong { get; init; } = string.Empty;

    public string? KetQua { get; init; }

    public decimal? TongDiem { get; init; }

    public decimal? DiemTrungBinh { get; init; }

    public string? TenMucCongNhan { get; init; }

    public decimal? TyLeTrungLap { get; init; }

    public DateTimeOffset? NgayNop { get; init; }

    public DateOnly? NgayCongNhan { get; init; }

    public string? SoQuyetDinh { get; init; }

    public bool DaCongBo { get; init; }

    public string? PhamViApDung { get; init; }

    public decimal? GiaTriLamLoi { get; init; }
}
