using BlueIdea.Api.Chung;
using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.DanhMuc;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Infrastructure.DichVu;
using BlueIdea.Shared.KetQua;
using BlueIdea.Shared.TiengViet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Api.Controllers;

public sealed record MauThongBaoDto(
    Guid Id,
    string Ma,
    string Ten,
    string Kenh,
    string SuKien,
    string TieuDe,
    string NoiDung,
    List<string> DanhSachBien,
    short TrangThai);

public sealed record LuuMauThongBaoDto
{
    public string Ma { get; init; } = string.Empty;

    public string Ten { get; init; } = string.Empty;

    /// <summary>EMAIL | SMS | APP | TAT_CA</summary>
    public string Kenh { get; init; } = "TAT_CA";

    public string SuKien { get; init; } = string.Empty;

    public string TieuDe { get; init; } = string.Empty;

    public string NoiDung { get; init; } = string.Empty;

    public List<string> DanhSachBien { get; init; } = new();

    public short TrangThai { get; init; } = TrangThaiDanhMuc.HoatDong;
}

public sealed record NgayNghiLeDto(Guid Id, DateOnly Ngay, string Ten, bool LapLaiHangNam, short TrangThai);

public sealed record LuuNgayNghiLeDto
{
    public DateOnly Ngay { get; init; }

    public string Ten { get; init; } = string.Empty;

    public bool LapLaiHangNam { get; init; }

    public short TrangThai { get; init; } = TrangThaiDanhMuc.HoatDong;
}

/// <summary>
/// Chức năng 50 và 46 — mẫu thông báo theo sự kiện, và ngày nghỉ lễ.
///
/// Hai thứ này trước đây chỉ nạp bằng dữ liệu mẫu rồi sửa thẳng trong cơ sở dữ liệu. Ngày nghỉ lễ
/// đặc biệt quan trọng: hệ thống dùng nó để tính hạn xử lý theo ngày làm việc, nên năm nào không
/// cập nhật là hạn xử lý sai cả năm đó.
/// </summary>
[ApiController]
[Route("api/v1")]
[Authorize]
[Produces("application/json")]
public sealed class MauThongBaoVaNgayNghiController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly IDichVuNhatKy _nhatKy;

    public MauThongBaoVaNgayNghiController(IAppDbContext db, IDichVuNhatKy nhatKy)
    {
        _db = db;
        _nhatKy = nhatKy;
    }

    // --------------------------------------------------- Chức năng 50: mẫu thông báo

    [HttpGet("mau-thong-bao")]
    [Authorize(Policy = MaQuyen.CauHinhXem)]
    public async Task<IActionResult> LayMauAsync(
        [FromQuery] string? suKien, [FromQuery] string? kenh, CancellationToken ct)
    {
        var truyVan = _db.MauThongBao.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(suKien))
        {
            truyVan = truyVan.Where(x => x.SuKien == suKien);
        }

        if (!string.IsNullOrWhiteSpace(kenh))
        {
            truyVan = truyVan.Where(x => x.Kenh == kenh || x.Kenh == "TAT_CA");
        }

        var ds = await truyVan
            .OrderBy(x => x.SuKien).ThenBy(x => x.Kenh)
            .Select(x => new MauThongBaoDto(
                x.Id, x.Ma, x.Ten, x.Kenh, x.SuKien, x.TieuDe, x.NoiDung,
                x.DanhSachBien, x.TrangThai))
            .ToListAsync(ct);

        return Ok(PhanHoiApi<List<MauThongBaoDto>>.Ok(ds));
    }

    /// <summary>Danh sách sự kiện hệ thống có thể gắn mẫu — giao diện đổ vào ô chọn.</summary>
    [HttpGet("mau-thong-bao/su-kien")]
    [Authorize(Policy = MaQuyen.CauHinhXem)]
    public IActionResult LaySuKien()
        => Ok(PhanHoiApi<object>.Ok(new[]
        {
            new { ma = SuKienThongBao.HoSoDuocTiepNhan, ten = "Hồ sơ được tiếp nhận" },
            new { ma = SuKienThongBao.YeuCauBoSung, ten = "Yêu cầu bổ sung hồ sơ" },
            new { ma = SuKienThongBao.DuocPhanCongCham, ten = "Được phân công chấm điểm" },
            new { ma = SuKienThongBao.SapHetHan, ten = "Sắp hết hạn xử lý" },
            new { ma = SuKienThongBao.CoKetQua, ten = "Có kết quả xét duyệt" },
            new { ma = SuKienThongBao.DaPheDuyet, ten = "Hồ sơ đã được phê duyệt" },
            new { ma = SuKienThongBao.HoSoBiTuChoi, ten = "Hồ sơ bị từ chối" },
            new { ma = SuKienThongBao.MoiHopHoiDong, ten = "Mời họp hội đồng" }
        }));

    [HttpPost("mau-thong-bao")]
    [Authorize(Policy = MaQuyen.CauHinhSua)]
    public async Task<IActionResult> ThemMauAsync(
        [FromBody] LuuMauThongBaoDto duLieu, CancellationToken ct)
    {
        KiemTraMau(duLieu);

        var ma = duLieu.Ma.Trim().ToUpperInvariant();

        if (await _db.MauThongBao.AsNoTracking().AnyAsync(x => x.Ma == ma, ct))
        {
            throw new NghiepVuException(MaLoiHeThong.TrungMa, $"Mã mẫu '{ma}' đã tồn tại.");
        }

        var banGhi = new MauThongBao { Id = Guid.NewGuid(), Ma = ma };
        ApDung(banGhi, duLieu);

        _db.MauThongBao.Add(banGhi);
        await _db.SaveChangesAsync(ct);

        await _nhatKy.GhiAsync("THEM_MAU_THONG_BAO", "CAU_HINH", "MauThongBao", banGhi.Id,
            $"Thêm mẫu thông báo {ma}", duLieuSau: duLieu, ct: ct);

        return Ok(PhanHoiApi<Guid>.Ok(banGhi.Id, "Đã thêm mẫu thông báo"));
    }

    [HttpPut("mau-thong-bao/{id:guid}")]
    [Authorize(Policy = MaQuyen.CauHinhSua)]
    public async Task<IActionResult> SuaMauAsync(
        Guid id, [FromBody] LuuMauThongBaoDto duLieu, CancellationToken ct)
    {
        KiemTraMau(duLieu);

        var banGhi = await _db.MauThongBao.FirstOrDefaultAsync(x => x.Id == id, ct)
                     ?? throw new KhongTimThayException("mẫu thông báo", id);

        ApDung(banGhi, duLieu);
        await _db.SaveChangesAsync(ct);

        await _nhatKy.GhiAsync("SUA_MAU_THONG_BAO", "CAU_HINH", "MauThongBao", id,
            $"Cập nhật mẫu thông báo {banGhi.Ma}", duLieuSau: duLieu, ct: ct);

        return Ok(PhanHoiApi.Ok("Đã cập nhật mẫu thông báo"));
    }

    [HttpDelete("mau-thong-bao/{id:guid}")]
    [Authorize(Policy = MaQuyen.CauHinhSua)]
    public async Task<IActionResult> XoaMauAsync(Guid id, CancellationToken ct)
    {
        var banGhi = await _db.MauThongBao.FirstOrDefaultAsync(x => x.Id == id, ct)
                     ?? throw new KhongTimThayException("mẫu thông báo", id);

        banGhi.DaXoa = true;
        await _db.SaveChangesAsync(ct);

        await _nhatKy.GhiAsync("XOA_MAU_THONG_BAO", "CAU_HINH", "MauThongBao", id,
            "Xoá mẫu thông báo", ct: ct);

        return Ok(PhanHoiApi.Ok("Đã xoá mẫu thông báo"));
    }

    /// <summary>
    /// Kết xuất thử mẫu với biến mẫu — xem trước đúng cái người nhận sẽ đọc, trước khi bật.
    /// </summary>
    [HttpPost("mau-thong-bao/{id:guid}/xem-truoc")]
    [Authorize(Policy = MaQuyen.CauHinhXem)]
    public async Task<IActionResult> XemTruocMauAsync(
        Guid id, [FromBody] Dictionary<string, string>? bien, CancellationToken ct)
    {
        var banGhi = await _db.MauThongBao.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)
                     ?? throw new KhongTimThayException("mẫu thông báo", id);

        var giaTri = bien ?? new Dictionary<string, string>();

        // Biến chưa được truyền thì điền giá trị mẫu để người dùng thấy hình dạng thật của tin.
        foreach (var ten in banGhi.DanhSachBien.Where(x => !giaTri.ContainsKey(x)))
        {
            giaTri[ten] = $"[{ten}]";
        }

        var doiTuong = giaTri.ToDictionary(x => x.Key, x => (object?)x.Value);

        return Ok(PhanHoiApi<object>.Ok(new
        {
            tieuDe = BoKetXuatMau.KetXuat(banGhi.TieuDe, doiTuong),
            noiDung = BoKetXuatMau.KetXuat(banGhi.NoiDung, doiTuong)
        }));
    }

    // ------------------------------------------------- Chức năng 46: ngày nghỉ lễ

    [HttpGet("ngay-nghi-le")]
    [Authorize(Policy = MaQuyen.DanhMucXem)]
    public async Task<IActionResult> LayNgayNghiAsync([FromQuery] int? nam, CancellationToken ct)
    {
        var truyVan = _db.NgayNghiLe.AsNoTracking();

        if (nam.HasValue)
        {
            // Ngày lặp lại hằng năm luôn thuộc mọi năm nên không lọc theo năm.
            truyVan = truyVan.Where(x => x.LapLaiHangNam || x.Ngay.Year == nam.Value);
        }

        var ds = await truyVan
            .OrderBy(x => x.Ngay)
            .Select(x => new NgayNghiLeDto(x.Id, x.Ngay, x.Ten, x.LapLaiHangNam, x.TrangThai))
            .ToListAsync(ct);

        return Ok(PhanHoiApi<List<NgayNghiLeDto>>.Ok(ds));
    }

    [HttpPost("ngay-nghi-le")]
    [Authorize(Policy = MaQuyen.DanhMucThem)]
    public async Task<IActionResult> ThemNgayNghiAsync(
        [FromBody] LuuNgayNghiLeDto duLieu, CancellationToken ct)
    {
        await KiemTraNgayNghiAsync(duLieu, null, ct);

        var banGhi = new NgayNghiLe
        {
            Id = Guid.NewGuid(),
            Ngay = duLieu.Ngay,
            Ten = duLieu.Ten.Trim(),
            LapLaiHangNam = duLieu.LapLaiHangNam,
            TrangThai = duLieu.TrangThai
        };

        _db.NgayNghiLe.Add(banGhi);
        await _db.SaveChangesAsync(ct);

        await _nhatKy.GhiAsync("THEM_NGAY_NGHI_LE", "DANH_MUC", "NgayNghiLe", banGhi.Id,
            $"Thêm ngày nghỉ {duLieu.Ngay:dd/MM/yyyy} — {duLieu.Ten}", duLieuSau: duLieu, ct: ct);

        return Ok(PhanHoiApi<Guid>.Ok(banGhi.Id, "Đã thêm ngày nghỉ lễ"));
    }

    [HttpPut("ngay-nghi-le/{id:guid}")]
    [Authorize(Policy = MaQuyen.DanhMucSua)]
    public async Task<IActionResult> SuaNgayNghiAsync(
        Guid id, [FromBody] LuuNgayNghiLeDto duLieu, CancellationToken ct)
    {
        var banGhi = await _db.NgayNghiLe.FirstOrDefaultAsync(x => x.Id == id, ct)
                     ?? throw new KhongTimThayException("ngày nghỉ lễ", id);

        await KiemTraNgayNghiAsync(duLieu, id, ct);

        banGhi.Ngay = duLieu.Ngay;
        banGhi.Ten = duLieu.Ten.Trim();
        banGhi.LapLaiHangNam = duLieu.LapLaiHangNam;
        banGhi.TrangThai = duLieu.TrangThai;

        await _db.SaveChangesAsync(ct);

        await _nhatKy.GhiAsync("SUA_NGAY_NGHI_LE", "DANH_MUC", "NgayNghiLe", id,
            "Cập nhật ngày nghỉ lễ", duLieuSau: duLieu, ct: ct);

        return Ok(PhanHoiApi.Ok("Đã cập nhật"));
    }

    [HttpDelete("ngay-nghi-le/{id:guid}")]
    [Authorize(Policy = MaQuyen.DanhMucXoa)]
    public async Task<IActionResult> XoaNgayNghiAsync(Guid id, CancellationToken ct)
    {
        var banGhi = await _db.NgayNghiLe.FirstOrDefaultAsync(x => x.Id == id, ct)
                     ?? throw new KhongTimThayException("ngày nghỉ lễ", id);

        banGhi.DaXoa = true;
        await _db.SaveChangesAsync(ct);

        await _nhatKy.GhiAsync("XOA_NGAY_NGHI_LE", "DANH_MUC", "NgayNghiLe", id,
            "Xoá ngày nghỉ lễ", ct: ct);

        return Ok(PhanHoiApi.Ok("Đã xoá"));
    }

    // -----------------------------------------------------------------------------------

    private static void ApDung(MauThongBao x, LuuMauThongBaoDto d)
    {
        x.Ten = d.Ten.Trim();
        x.TenKhongDau = VanBanTiengViet.TaoKhongDau(d.Ten);
        x.Kenh = d.Kenh;
        x.SuKien = d.SuKien;
        x.TieuDe = d.TieuDe;
        x.NoiDung = d.NoiDung;
        x.DanhSachBien = d.DanhSachBien;
        x.TrangThai = d.TrangThai;
    }

    private static void KiemTraMau(LuuMauThongBaoDto d)
    {
        if (string.IsNullOrWhiteSpace(d.Ma) || string.IsNullOrWhiteSpace(d.Ten))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe, "Mã và tên là bắt buộc.");
        }

        if (d.Kenh is not ("EMAIL" or "SMS" or "APP" or "TAT_CA"))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                $"Kênh '{d.Kenh}' không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(d.SuKien))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Phải chọn sự kiện kích hoạt mẫu.");
        }

        if (string.IsNullOrWhiteSpace(d.NoiDung))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Nội dung mẫu không được để trống.");
        }
    }

    private async Task KiemTraNgayNghiAsync(LuuNgayNghiLeDto d, Guid? ngoaiTru, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(d.Ten))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe, "Nhập tên ngày nghỉ.");
        }

        // Trung ngay = tinh han xu ly se tru hai lan cho cung mot ngay.
        var trung = await _db.NgayNghiLe.AsNoTracking()
            .AnyAsync(x => x.Ngay == d.Ngay && (ngoaiTru == null || x.Id != ngoaiTru), ct);

        if (trung)
        {
            throw new NghiepVuException(MaLoiHeThong.TrungMa,
                $"Ngày {d.Ngay:dd/MM/yyyy} đã có trong danh sách nghỉ lễ.");
        }
    }
}
