using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Shared.KetQua;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlueIdea.Application.QuanTri;

/// <summary>Mot dong doc duoc tu tep Excel, chua qua kiem tra nghiep vu.</summary>
public sealed record DongNhapNguoiDung
{
    public int SoDong { get; init; }

    public string? TenDangNhap { get; init; }

    public string? HoTen { get; init; }

    public string? Email { get; init; }

    public string? DienThoai { get; init; }

    public string? ChucVu { get; init; }

    public string? MaDonVi { get; init; }

    public string? MaVaiTro { get; init; }
}

public sealed record KetQuaDongNhap(
    int SoDong,
    string? TenDangNhap,
    string? HoTen,
    bool HopLe,
    string? Loi,
    string? MatKhauTam);

public sealed record KetQuaNhapNguoiDung(
    bool ChayThu,
    int TongDong,
    int SoHopLe,
    int SoLoi,
    IReadOnlyList<KetQuaDongNhap> ChiTiet);

/// <summary>
/// Chuc nang 43 - Nhap danh sach nguoi dung tu Excel.
///
/// Nguyen tac: TOAN BO hoac KHONG. Neu con dong loi thi khong ghi dong nao ca - nhap nua vo nua
/// khien quan tri vien khong biet phai sua tu dau, va lan nhap lai se bao trung tai khoan.
/// </summary>
public sealed class DichVuNhapNguoiDung
{
    /// <summary>Chan tep qua lon de mot lan nhap khong khoa CSDL qua lau.</summary>
    private const int SoDongToiDa = 2000;

    private readonly IAppDbContext _db;
    private readonly IDichVuPhanQuyen _phanQuyen;
    private readonly IDichVuMatKhau _matKhau;
    private readonly IDichVuCauHinh _cauHinh;
    private readonly IDongHoHeThong _dongHo;
    private readonly IDichVuNhatKy _nhatKy;
    private readonly ILogger<DichVuNhapNguoiDung> _logger;

    public DichVuNhapNguoiDung(
        IAppDbContext db, IDichVuPhanQuyen phanQuyen, IDichVuMatKhau matKhau,
        IDichVuCauHinh cauHinh, IDongHoHeThong dongHo, IDichVuNhatKy nhatKy,
        ILogger<DichVuNhapNguoiDung> logger)
    {
        _db = db;
        _phanQuyen = phanQuyen;
        _matKhau = matKhau;
        _cauHinh = cauHinh;
        _dongHo = dongHo;
        _nhatKy = nhatKy;
        _logger = logger;
    }

    /// <summary>
    /// Kiem tra va (neu khong phai chay thu) tao tai khoan.
    /// </summary>
    /// <param name="chayThu">
    /// True = chi kiem tra va bao loi, khong ghi gi. Man hinh luon chay thu truoc de quan tri vien
    /// thay truoc ket qua roi moi xac nhan.
    /// </param>
    public async Task<KetQuaNhapNguoiDung> NhapAsync(
        IReadOnlyList<DongNhapNguoiDung> cacDong, bool chayThu, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.NguoiDungThem, ct: ct).ConfigureAwait(false);

        if (cacDong.Count == 0)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Tệp không có dòng dữ liệu nào.");
        }

        if (cacDong.Count > SoDongToiDa)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                $"Tệp có {cacDong.Count} dòng, vượt giới hạn {SoDongToiDa} dòng mỗi lần nhập.");
        }

        var donViTheoMa = await _db.DonVi.AsNoTracking()
            .ToDictionaryAsync(x => x.Ma, x => x.Id, StringComparer.OrdinalIgnoreCase, ct)
            .ConfigureAwait(false);

        var vaiTroTheoMa = await _db.VaiTro.AsNoTracking()
            .ToDictionaryAsync(x => x.Ma, x => x.Id, StringComparer.OrdinalIgnoreCase, ct)
            .ConfigureAwait(false);

        var tenDaCo = (await _db.NguoiDung.AsNoTracking()
                .Select(x => x.TenDangNhap)
                .ToListAsync(ct)
                .ConfigureAwait(false))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Trung ngay trong chinh tep cung phai bat, khong chi trung voi CSDL.
        var tenTrongTep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var ketQua = new List<KetQuaDongNhap>(cacDong.Count);
        var canTao = new List<(DongNhapNguoiDung Dong, Guid? DonViId, Guid VaiTroId, string MatKhauTam)>();

        foreach (var dong in cacDong)
        {
            var loi = KiemTraDong(dong, donViTheoMa, vaiTroTheoMa, tenDaCo, tenTrongTep);

            if (loi is not null)
            {
                ketQua.Add(new KetQuaDongNhap(dong.SoDong, dong.TenDangNhap, dong.HoTen, false, loi, null));
                continue;
            }

            var ten = dong.TenDangNhap!.Trim().ToLowerInvariant();
            tenTrongTep.Add(ten);

            var matKhauTam = await SinhMatKhauTamAsync(ct).ConfigureAwait(false);

            canTao.Add((
                dong,
                string.IsNullOrWhiteSpace(dong.MaDonVi) ? null : donViTheoMa[dong.MaDonVi],
                vaiTroTheoMa[dong.MaVaiTro!],
                matKhauTam));

            ketQua.Add(new KetQuaDongNhap(
                dong.SoDong, ten, dong.HoTen, true, null, chayThu ? null : matKhauTam));
        }

        var soLoi = ketQua.Count(x => !x.HopLe);

        // Con loi -> khong ghi gi ca, tra ve bao cao de quan tri vien sua tep.
        if (chayThu || soLoi > 0)
        {
            return new KetQuaNhapNguoiDung(
                ChayThu: true,
                TongDong: cacDong.Count,
                SoHopLe: ketQua.Count - soLoi,
                SoLoi: soLoi,
                ChiTiet: ketQua);
        }

        foreach (var (dong, donViId, vaiTroId, matKhauTam) in canTao)
        {
            var (hash, salt) = _matKhau.BamMatKhau(matKhauTam);

            var nguoiDung = new NguoiDung
            {
                Id = Guid.NewGuid(),
                TenDangNhap = dong.TenDangNhap!.Trim().ToLowerInvariant(),
                HoTen = dong.HoTen!.Trim(),
                HoTenKhongDau = Shared.TiengViet.VanBanTiengViet.TaoKhongDau(dong.HoTen),
                Email = string.IsNullOrWhiteSpace(dong.Email) ? null : dong.Email.Trim(),
                DienThoai = string.IsNullOrWhiteSpace(dong.DienThoai) ? null : dong.DienThoai.Trim(),
                ChucVu = dong.ChucVu,
                DonViId = donViId,
                TrangThaiTaiKhoan = TrangThaiNguoiDung.HoatDong,
                MatKhauHash = hash,
                MatKhauSalt = salt,
                BuocDoiMatKhau = true,
                NgayDoiMatKhauCuoi = _dongHo.BayGio
            };

            _db.NguoiDung.Add(nguoiDung);

            _db.NguoiDungVaiTro.Add(new NguoiDungVaiTro
            {
                Id = Guid.NewGuid(),
                NguoiDungId = nguoiDung.Id,
                VaiTroId = vaiTroId,
                DonViId = donViId
            });
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await _nhatKy.GhiAsync("NHAP_NGUOI_DUNG", "QUAN_TRI", "NguoiDung", null,
            $"Nhập {canTao.Count} tài khoản từ tệp Excel",
            duLieuSau: canTao.Select(x => x.Dong.TenDangNhap).ToList(),
            ct: ct).ConfigureAwait(false);

        _logger.LogInformation("Đã nhập {SoTaiKhoan} tài khoản từ Excel.", canTao.Count);

        return new KetQuaNhapNguoiDung(
            ChayThu: false,
            TongDong: cacDong.Count,
            SoHopLe: canTao.Count,
            SoLoi: 0,
            ChiTiet: ketQua);
    }

    /// <summary>Tra ve null neu dong hop le, nguoc lai la thong bao loi cu the.</summary>
    private static string? KiemTraDong(
        DongNhapNguoiDung dong,
        IReadOnlyDictionary<string, Guid> donViTheoMa,
        IReadOnlyDictionary<string, Guid> vaiTroTheoMa,
        IReadOnlySet<string> tenDaCo,
        IReadOnlySet<string> tenTrongTep)
    {
        if (string.IsNullOrWhiteSpace(dong.TenDangNhap))
        {
            return "Thiếu tên đăng nhập.";
        }

        // Chu HOA duoc chap nhan roi chuan hoa ve chu thuong: danh sach nhan su thuong xuat ra
        // dang "NguyenVanA". Nhung ky tu ngoai bang chu cai ASCII (dau tieng Viet, khoang trang,
        // @, ...) thi phai bao loi chu khong duoc am tham cat bo.
        var ten = dong.TenDangNhap.Trim().ToLowerInvariant();

        if (!ten.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c is '.' or '_' or '-'))
        {
            return $"Tên đăng nhập '{dong.TenDangNhap.Trim()}' chỉ được dùng chữ cái không dấu, "
                   + "số và các ký tự . _ - (không khoảng trắng, không dấu tiếng Việt).";
        }

        if (tenDaCo.Contains(ten))
        {
            return $"Tên đăng nhập '{ten}' đã tồn tại trong hệ thống.";
        }

        if (tenTrongTep.Contains(ten))
        {
            return $"Tên đăng nhập '{ten}' bị lặp lại trong chính tệp này.";
        }

        if (string.IsNullOrWhiteSpace(dong.HoTen))
        {
            return "Thiếu họ và tên.";
        }

        if (!string.IsNullOrWhiteSpace(dong.Email)
            && (!dong.Email.Contains('@') || dong.Email.Trim().Length < 5))
        {
            return $"Email '{dong.Email}' không hợp lệ.";
        }

        if (string.IsNullOrWhiteSpace(dong.MaVaiTro))
        {
            return "Thiếu mã vai trò.";
        }

        if (!vaiTroTheoMa.ContainsKey(dong.MaVaiTro))
        {
            return $"Mã vai trò '{dong.MaVaiTro}' không tồn tại.";
        }

        if (!string.IsNullOrWhiteSpace(dong.MaDonVi) && !donViTheoMa.ContainsKey(dong.MaDonVi))
        {
            return $"Mã đơn vị '{dong.MaDonVi}' không tồn tại.";
        }

        return null;
    }

    private async Task<string> SinhMatKhauTamAsync(CancellationToken ct)
    {
        var doDaiToiThieu = await _cauHinh
            .LayAsync(KhoaCauHinh.ChinhSachMatKhauDoDaiToiThieu, 8, ct)
            .ConfigureAwait(false);

        return BoSinhMatKhauTam.Sinh(Math.Max(12, doDaiToiThieu));
    }
}
