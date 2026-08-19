using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Shared.KetQua;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.QuanTri;

/// <summary>
/// Cau hinh may chu gui tin tra ve cho giao dien.
///
/// KHONG bao gio chua mat khau hay API key da giai ma - chi bao da dat hay chua, de man hinh
/// hien "••••••" ma van biet co can nhap lai khong.
/// </summary>
public sealed record CauHinhGuiTinDto(
    Guid Id,
    string Loai,
    string? NhaCungCap,
    string? Host,
    int? Port,
    string? TenDangNhap,
    bool DaDatMatKhau,
    bool SuDungSsl,
    string? EmailGuiDi,
    string? TenHienThi,
    string? ApiEndpoint,
    bool DaDatApiKey,
    string? Brandname,
    short TrangThai,
    bool LaMacDinh);

public sealed record LuuCauHinhGuiTinDto
{
    /// <summary>EMAIL | SMS</summary>
    public string Loai { get; init; } = "EMAIL";

    public string? NhaCungCap { get; init; }

    public string? Host { get; init; }

    public int? Port { get; init; }

    public string? TenDangNhap { get; init; }

    /// <summary>Bo trong khi sua = giu nguyen mat khau dang co.</summary>
    public string? MatKhau { get; init; }

    public bool SuDungSsl { get; init; } = true;

    public string? EmailGuiDi { get; init; }

    public string? TenHienThi { get; init; }

    public string? ApiEndpoint { get; init; }

    /// <summary>Bo trong khi sua = giu nguyen API key dang co.</summary>
    public string? ApiKey { get; init; }

    public string? Brandname { get; init; }

    public short TrangThai { get; init; } = TrangThaiDanhMuc.HoatDong;

    public bool LaMacDinh { get; init; }
}

/// <summary>Chuc nang 50 - Quan tri cau hinh may chu email va nha cung cap SMS.</summary>
public sealed class DichVuCauHinhGuiTin
{
    private readonly IAppDbContext _db;
    private readonly IDichVuPhanQuyen _phanQuyen;
    private readonly IDichVuMaHoa _maHoa;
    private readonly IDichVuGuiTin _guiTin;
    private readonly IDichVuNhatKy _nhatKy;

    public DichVuCauHinhGuiTin(
        IAppDbContext db, IDichVuPhanQuyen phanQuyen, IDichVuMaHoa maHoa,
        IDichVuGuiTin guiTin, IDichVuNhatKy nhatKy)
    {
        _db = db;
        _phanQuyen = phanQuyen;
        _maHoa = maHoa;
        _guiTin = guiTin;
        _nhatKy = nhatKy;
    }

    public async Task<IReadOnlyList<CauHinhGuiTinDto>> DanhSachAsync(CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.CauHinhXem, ct: ct).ConfigureAwait(false);

        return await _db.CauHinhEmailSms.AsNoTracking()
            .OrderBy(x => x.Loai).ThenByDescending(x => x.LaMacDinh)
            .Select(x => new CauHinhGuiTinDto(
                x.Id, x.Loai, x.NhaCungCap, x.Host, x.Port, x.TenDangNhap,
                x.MatKhauMaHoa != null && x.MatKhauMaHoa != "",
                x.SuDungSsl, x.EmailGuiDi, x.TenHienThi, x.ApiEndpoint,
                x.ApiKeyMaHoa != null && x.ApiKeyMaHoa != "",
                x.Brandname, x.TrangThai, x.LaMacDinh))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<Guid> ThemAsync(LuuCauHinhGuiTinDto dto, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.CauHinhSua, ct: ct).ConfigureAwait(false);
        KiemTraHopLe(dto);

        var cauHinh = new CauHinhEmailSms { Id = Guid.NewGuid() };
        ApDung(cauHinh, dto);

        _db.CauHinhEmailSms.Add(cauHinh);

        await BoMacDinhKhacAsync(cauHinh, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await _nhatKy.GhiAsync("THEM_CAU_HINH_GUI_TIN", "QUAN_TRI", "CauHinhEmailSms", cauHinh.Id,
            $"Thêm cấu hình {dto.Loai} {dto.Host ?? dto.ApiEndpoint}",
            duLieuSau: AnBiMat(dto), ct: ct).ConfigureAwait(false);

        return cauHinh.Id;
    }

    public async Task CapNhatAsync(Guid id, LuuCauHinhGuiTinDto dto, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.CauHinhSua, ct).ConfigureAwait(false);
        KiemTraHopLe(dto);

        var cauHinh = await _db.CauHinhEmailSms.FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("cấu hình gửi tin", id);

        var truoc = new { cauHinh.Loai, cauHinh.Host, cauHinh.Port, cauHinh.EmailGuiDi, cauHinh.TrangThai };

        ApDung(cauHinh, dto);

        await BoMacDinhKhacAsync(cauHinh, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await _nhatKy.GhiAsync("SUA_CAU_HINH_GUI_TIN", "QUAN_TRI", "CauHinhEmailSms", id,
            $"Cập nhật cấu hình {dto.Loai}",
            duLieuTruoc: truoc, duLieuSau: AnBiMat(dto), ct: ct).ConfigureAwait(false);
    }

    public async Task XoaAsync(Guid id, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.CauHinhSua, ct).ConfigureAwait(false);

        var cauHinh = await _db.CauHinhEmailSms.FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("cấu hình gửi tin", id);

        cauHinh.DaXoa = true;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await _nhatKy.GhiAsync("XOA_CAU_HINH_GUI_TIN", "QUAN_TRI", "CauHinhEmailSms", id,
            $"Xoá cấu hình {cauHinh.Loai}", ct: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Gui thu mot ban tin bang cau hinh dang luu.
    ///
    /// Gui THANG chu khong qua hang doi: quan tri vien can biet ket qua ngay tai cho, va can thay
    /// dung thong bao loi cua may chu SMTP de sua cau hinh.
    /// </summary>
    public async Task<string> GuiThuAsync(Guid id, string nguoiNhan, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.CauHinhSua, ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(nguoiNhan))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Chưa nhập địa chỉ email hoặc số điện thoại nhận thử.");
        }

        var cauHinh = await _db.CauHinhEmailSms.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("cấu hình gửi tin", id);

        const string noiDung =
            "Đây là tin nhắn thử của Nền tảng số dùng chung phục vụ hoạt động sáng kiến. "
            + "Nếu bạn nhận được tin này, cấu hình máy chủ gửi tin đã hoạt động.";

        try
        {
            if (cauHinh.Loai == "SMS")
            {
                await _guiTin.GuiSmsAsync(nguoiNhan, noiDung, ct).ConfigureAwait(false);
            }
            else
            {
                await _guiTin.GuiEmailAsync(nguoiNhan, "[Sáng kiến] Thử cấu hình gửi tin", noiDung, ct)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            await _nhatKy.GhiAsync("GUI_THU_CAU_HINH", "QUAN_TRI", "CauHinhEmailSms", id,
                $"Gửi thử tới {nguoiNhan} thất bại", ketQua: "THAT_BAI", ct: ct).ConfigureAwait(false);

            // Tra nguyen thong bao cua may chu de quan tri vien sua duoc cau hinh.
            throw new NghiepVuException(MaLoiHeThong.LoiHeThong, $"Gửi thử thất bại: {ex.Message}");
        }

        await _nhatKy.GhiAsync("GUI_THU_CAU_HINH", "QUAN_TRI", "CauHinhEmailSms", id,
            $"Gửi thử tới {nguoiNhan} thành công", ct: ct).ConfigureAwait(false);

        return $"Đã gửi thử tới {nguoiNhan}.";
    }

    /// <summary>Thong ke hang doi de man hinh cau hinh hien duoc tinh trang gui thuc te.</summary>
    public async Task<IReadOnlyDictionary<string, int>> ThongKeHangDoiAsync(CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.CauHinhXem, ct: ct).ConfigureAwait(false);

        var nhom = await _db.HangDoiGuiTin.AsNoTracking()
            .GroupBy(x => x.TrangThaiGui)
            .Select(g => new { TrangThai = g.Key, SoLuong = g.Count() })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return nhom.ToDictionary(x => x.TrangThai, x => x.SoLuong);
    }

    // -----------------------------------------------------------------------------------

    private void ApDung(CauHinhEmailSms cauHinh, LuuCauHinhGuiTinDto dto)
    {
        cauHinh.Loai = dto.Loai;
        cauHinh.NhaCungCap = dto.NhaCungCap;
        cauHinh.Host = dto.Host;
        cauHinh.Port = dto.Port;
        cauHinh.TenDangNhap = dto.TenDangNhap;
        cauHinh.SuDungSsl = dto.SuDungSsl;
        cauHinh.EmailGuiDi = dto.EmailGuiDi;
        cauHinh.TenHienThi = dto.TenHienThi;
        cauHinh.ApiEndpoint = dto.ApiEndpoint;
        cauHinh.Brandname = dto.Brandname;
        cauHinh.TrangThai = dto.TrangThai;
        cauHinh.LaMacDinh = dto.LaMacDinh;

        // Bo trong = giu nguyen bi mat dang co, de man hinh khong phai nhap lai mat khau moi lan sua.
        if (!string.IsNullOrWhiteSpace(dto.MatKhau))
        {
            cauHinh.MatKhauMaHoa = _maHoa.MaHoa(dto.MatKhau);
        }

        if (!string.IsNullOrWhiteSpace(dto.ApiKey))
        {
            cauHinh.ApiKeyMaHoa = _maHoa.MaHoa(dto.ApiKey);
        }
    }

    private static void KiemTraHopLe(LuuCauHinhGuiTinDto dto)
    {
        if (dto.Loai is not ("EMAIL" or "SMS"))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                $"Loại cấu hình '{dto.Loai}' không hợp lệ (chỉ EMAIL hoặc SMS).");
        }

        if (dto.Loai == "EMAIL")
        {
            if (string.IsNullOrWhiteSpace(dto.Host) || string.IsNullOrWhiteSpace(dto.EmailGuiDi))
            {
                throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                    "Cấu hình email phải có máy chủ SMTP và địa chỉ gửi đi.");
            }

            if (dto.Port is < 1 or > 65535)
            {
                throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                    "Cổng SMTP phải nằm trong khoảng 1–65535.");
            }
        }
        else if (string.IsNullOrWhiteSpace(dto.ApiEndpoint))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Cấu hình SMS phải có endpoint API của nhà cung cấp.");
        }
    }

    /// <summary>Chi duoc co MOT cau hinh mac dinh cho moi loai - job gui tin chon theo co nay.</summary>
    private async Task BoMacDinhKhacAsync(CauHinhEmailSms cauHinh, CancellationToken ct)
    {
        if (!cauHinh.LaMacDinh)
        {
            return;
        }

        var khac = await _db.CauHinhEmailSms
            .Where(x => x.Loai == cauHinh.Loai && x.Id != cauHinh.Id && x.LaMacDinh)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var x in khac)
        {
            x.LaMacDinh = false;
        }
    }

    /// <summary>Loai bo bi mat truoc khi ghi vao nhat ky he thong.</summary>
    private static object AnBiMat(LuuCauHinhGuiTinDto dto) => new
    {
        dto.Loai,
        dto.NhaCungCap,
        dto.Host,
        dto.Port,
        dto.TenDangNhap,
        dto.SuDungSsl,
        dto.EmailGuiDi,
        dto.TenHienThi,
        dto.ApiEndpoint,
        dto.Brandname,
        dto.TrangThai,
        dto.LaMacDinh,
        MatKhau = string.IsNullOrWhiteSpace(dto.MatKhau) ? "(giữ nguyên)" : "(đã đổi)",
        ApiKey = string.IsNullOrWhiteSpace(dto.ApiKey) ? "(giữ nguyên)" : "(đã đổi)"
    };
}
