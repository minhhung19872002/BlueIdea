using BlueIdea.Api.Chung;
using BlueIdea.Application.Chung;
using BlueIdea.Application.DanhMuc;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.DanhMuc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Api.Controllers;

public sealed class LuuDotDeNghiDto : LuuDanhMucDto
{
    public int Nam { get; set; }

    public string? Ky { get; set; }

    public DateOnly? TuNgay { get; set; }

    public DateOnly? DenNgay { get; set; }

    public DateTimeOffset? HanNopHoSo { get; set; }

    public DateTimeOffset? HanChamDiem { get; set; }

    public string CapXetDuyet { get; set; } = Domain.Chung.CapXetDuyet.CoSo;

    public Guid? QuyTrinhId { get; set; }

    public Guid? BoTieuChiId { get; set; }

    public List<Guid> DonViApDungIds { get; set; } = new();

    public bool TuDongKhoa { get; set; } = true;

    public string? GhiChu { get; set; }
}

public sealed record SaoChepDotDto(string Ma, string Ten, int Nam);

/// <summary>Chức năng 3 — Đợt đề nghị (tiếp nhận và xét duyệt theo năm/quý).</summary>
[ApiController]
[Route("api/v1/danh-muc/dot-de-nghi")]
[Authorize]
[Produces("application/json")]
public sealed class DotDeNghiController : ControllerBase
{
    private readonly DichVuDotDeNghi _dichVu;

    public DotDeNghiController(DichVuDotDeNghi dichVu) => _dichVu = dichVu;

    [HttpGet]
    [Authorize(Policy = MaQuyen.DanhMucXem)]
    public async Task<IActionResult> LayDanhSachAsync(
        [FromQuery] ThamSoLocDanhMuc thamSo, CancellationToken ct)
        => Ok(PhanHoiPhanTrang<DanhMucDto>.Tu(await _dichVu.LayDanhSachAsync(thamSo, ct)));

    /// <summary>
    /// Danh sách đợt kèm trạng thái vòng đời — màn hình quản trị đợt dùng để bật/tắt nút
    /// Mở / Đóng / Khoá đợt.
    /// </summary>
    [HttpGet("quan-ly")]
    [Authorize(Policy = MaQuyen.DanhMucXem)]
    public async Task<IActionResult> LayDanhSachQuanLyAsync(
        [FromQuery] ThamSoLocDanhMuc thamSo, CancellationToken ct)
        => Ok(PhanHoiPhanTrang<DotDeNghiQuanLyDto>.Tu(
            await _dichVu.LayDanhSachQuanLyAsync(thamSo, ct)));

    /// <summary>
    /// Danh sách đợt để đổ vào ô chọn — dùng ở mọi màn hình LỌC theo đợt.
    ///
    /// Khác <c>dang-mo</c>: ô lọc phải liệt kê cả những đợt đã đóng, vì người dùng thường tra
    /// cứu và làm báo cáo cho các đợt của những năm trước. <c>dang-mo</c> chỉ dành cho bước 1 của
    /// wizard nộp hồ sơ, nơi chỉ được nộp vào đợt còn hạn.
    ///
    /// Thiếu endpoint này thì 11 màn hình gọi tới đều nhận 404 và ô chọn đợt trống trơn — lọc
    /// theo đợt không dùng được ở đâu cả, mà giao diện vẫn hiện ô chọn như bình thường.
    /// </summary>
    [HttpGet("chon")]
    public async Task<IActionResult> LayDanhSachChonAsync(CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<DanhMucDto>>.Ok(await _dichVu.LayDanhSachChonAsync(ct)));

    /// <summary>Các đợt đang mở và còn hạn nộp — dùng ở bước 1 của wizard nộp hồ sơ.</summary>
    [HttpGet("dang-mo")]
    public async Task<IActionResult> LayDotDangMoAsync(CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<DanhMucDto>>.Ok(await _dichVu.LayDotDangMoAsync(ct)));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = MaQuyen.DanhMucXem)]
    public async Task<IActionResult> LayTheoIdAsync(Guid id, CancellationToken ct)
        => Ok(PhanHoiApi<DotDeNghi>.Ok(await _dichVu.LayTheoIdAsync(id, ct)));

    [HttpPost]
    [Authorize(Policy = MaQuyen.DanhMucThem)]
    public async Task<IActionResult> ThemAsync([FromBody] LuuDotDeNghiDto duLieu, CancellationToken ct)
    {
        var banGhi = await _dichVu.ThemAsync(TaoThucThe(new DotDeNghi(), duLieu), ct);
        return Ok(PhanHoiApi<DotDeNghi>.Ok(banGhi, "Thêm đợt đề nghị thành công"));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = MaQuyen.DanhMucSua)]
    public async Task<IActionResult> SuaAsync(
        Guid id, [FromBody] LuuDotDeNghiDto duLieu, CancellationToken ct)
    {
        var banGhi = await _dichVu.CapNhatAsync(id, x => TaoThucThe(x, duLieu), ct);
        return Ok(PhanHoiApi<DotDeNghi>.Ok(banGhi, "Cập nhật thành công"));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = MaQuyen.DanhMucXoa)]
    public async Task<IActionResult> XoaAsync(Guid id, CancellationToken ct)
    {
        await _dichVu.XoaAsync(id, ct);
        return Ok(PhanHoiApi.Ok("Đã xóa"));
    }

    /// <summary>Mở đợt — bắt đầu nhận hồ sơ.</summary>
    [HttpPost("{id:guid}/mo-dot")]
    [Authorize(Policy = MaQuyen.DanhMucSua)]
    public async Task<IActionResult> MoDotAsync(Guid id, CancellationToken ct)
    {
        await _dichVu.DoiTrangThaiDotAsync(id, TrangThaiDot.DangMo, ct);
        return Ok(PhanHoiApi.Ok("Đã mở đợt"));
    }

    /// <summary>Đóng đợt — ngừng nhận hồ sơ mới nhưng vẫn xử lý hồ sơ đã nộp.</summary>
    [HttpPost("{id:guid}/dong-dot")]
    [Authorize(Policy = MaQuyen.DanhMucSua)]
    public async Task<IActionResult> DongDotAsync(Guid id, CancellationToken ct)
    {
        await _dichVu.DoiTrangThaiDotAsync(id, TrangThaiDot.DaDong, ct);
        return Ok(PhanHoiApi.Ok("Đã đóng đợt"));
    }

    /// <summary>Khóa đợt — toàn bộ dữ liệu chỉ đọc.</summary>
    [HttpPost("{id:guid}/khoa-dot")]
    [Authorize(Policy = MaQuyen.DanhMucSua)]
    public async Task<IActionResult> KhoaDotAsync(Guid id, CancellationToken ct)
    {
        await _dichVu.DoiTrangThaiDotAsync(id, TrangThaiDot.DaKhoa, ct);
        return Ok(PhanHoiApi.Ok("Đã khóa đợt"));
    }

    /// <summary>Sao chép cấu hình đợt từ năm trước.</summary>
    /// <summary>Số liệu tổng quan của một đợt — dùng cho màn hình chi tiết đợt.</summary>
    [HttpGet("{id:guid}/tong-quan")]
    [Authorize(Policy = MaQuyen.DanhMucXem)]
    public async Task<IActionResult> LayTongQuanAsync(Guid id, CancellationToken ct)
        => Ok(PhanHoiApi<TongQuanDotDto>.Ok(await _dichVu.LayTongQuanAsync(id, ct)));

    [HttpPost("{id:guid}/sao-chep")]
    [Authorize(Policy = MaQuyen.DanhMucThem)]
    public async Task<IActionResult> SaoChepAsync(
        Guid id, [FromBody] SaoChepDotDto duLieu, CancellationToken ct)
    {
        var banGhi = await _dichVu.SaoChepAsync(id, duLieu.Ma, duLieu.Ten, duLieu.Nam, ct);
        return Ok(PhanHoiApi<DotDeNghi>.Ok(banGhi, "Đã sao chép đợt"));
    }

    private static DotDeNghi TaoThucThe(DotDeNghi x, LuuDotDeNghiDto d)
    {
        x.Ma = d.Ma;
        x.Ten = d.Ten;
        x.MoTa = d.MoTa;
        x.ThuTu = d.ThuTu;
        x.TrangThai = d.TrangThai;
        x.Nam = d.Nam;
        x.Ky = d.Ky;
        x.TuNgay = d.TuNgay;
        x.DenNgay = d.DenNgay;
        x.HanNopHoSo = d.HanNopHoSo;
        x.HanChamDiem = d.HanChamDiem;
        x.CapXetDuyet = d.CapXetDuyet;
        x.QuyTrinhId = d.QuyTrinhId;
        x.BoTieuChiId = d.BoTieuChiId;
        x.DonViApDungIds = d.DonViApDungIds;
        x.TuDongKhoa = d.TuDongKhoa;
        x.GhiChu = d.GhiChu;
        return x;
    }
}

public sealed class LuuDonViDto : LuuDanhMucDto
{
    public string? TenVietTat { get; set; }

    public Guid? DonViChaId { get; set; }

    public string Loai { get; set; } = LoaiDonVi.DonVi;

    public string? DiaChi { get; set; }

    public string? DienThoai { get; set; }

    public string? Email { get; set; }

    public string? NguoiDaiDien { get; set; }

    public string? ChucVuNguoiDaiDien { get; set; }

    public bool LaDonViPheDuyet { get; set; }

    public string? CapPheDuyet { get; set; }

    /// <summary>Id tep logo rieng cua don vi (chuc nang 47).</summary>
    public Guid? LogoId { get; set; }

    public string? TieuDeVanBan { get; set; }

    public string? NguoiKyMacDinh { get; set; }

    public string? ChucVuNguoiKyMacDinh { get; set; }
}

public sealed record ChuyenChaDonViDto(Guid? DonViChaMoiId);

/// <summary>Chức năng 5, 44, 47 — Quản lý đơn vị/tổ chức dạng cây.</summary>
[ApiController]
[Route("api/v1/don-vi")]
[Authorize]
[Produces("application/json")]
public sealed class DonViController : ControllerBase
{
    private readonly DichVuDonVi _dichVu;

    private readonly IAppDbContext _db;
    private readonly ILuuTruTep _luuTru;

    public DonViController(DichVuDonVi dichVu, IAppDbContext db, ILuuTruTep luuTru)
    {
        _dichVu = dichVu;
        _db = db;
        _luuTru = luuTru;
    }

    [HttpGet]
    [Authorize(Policy = MaQuyen.DonViXem)]
    public async Task<IActionResult> LayDanhSachAsync(
        [FromQuery] ThamSoLocDanhMuc thamSo, CancellationToken ct)
        => Ok(PhanHoiPhanTrang<DanhMucDto>.Tu(await _dichVu.LayDanhSachAsync(thamSo, ct)));

    /// <summary>Cây tổ chức đầy đủ.</summary>
    [HttpGet("cay")]
    public async Task<IActionResult> LayCayAsync(CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<NutCay>>.Ok(await _dichVu.LayCayAsync(ct)));

    /// <summary>
    /// Danh sach don vi de chon. <paramref name="chiDonViPheDuyet"/> = true thi chi tra ve don vi
    /// da danh dau "la don vi phe duyet" — dung cho man hinh khai cap phe duyet, de nguoi dung
    /// khong chon nham mot don vi khong co tham quyen ky roi bi may chu tu choi.
    /// </summary>
    [HttpGet("chon")]
    public async Task<IActionResult> LayDanhSachChonAsync(
        CancellationToken ct, [FromQuery] bool chiDonViPheDuyet = false)
        => Ok(PhanHoiApi<IReadOnlyList<DanhMucDto>>.Ok(
            await _dichVu.LayDanhSachChonAsync(ct, chiDonViPheDuyet)));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = MaQuyen.DonViXem)]
    public async Task<IActionResult> LayTheoIdAsync(Guid id, CancellationToken ct)
        => Ok(PhanHoiApi<DonVi>.Ok(await _dichVu.LayTheoIdAsync(id, ct)));

    /// <summary>
    /// Doc logo rieng cua mot don vi (chuc nang 47).
    ///
    /// Phai co duong rieng chu khong dung duoc endpoint xem truoc tep dung chung: tep khong gan
    /// vao ho so nao thi chi NGUOI TAI LEN moi xem duoc, nen logo do quan tri vien A dat se vo
    /// hinh voi quan tri vien B.
    ///
    /// Chi phuc vu dung tep ma don vi do dang chi dinh lam logo — truyen id khac deu khong ra.
    /// </summary>
    [HttpGet("{id:guid}/logo")]
    public async Task<IActionResult> LayLogoAsync(Guid id, CancellationToken ct)
    {
        var donVi = await _dichVu.LayTheoIdAsync(id, ct).ConfigureAwait(false);

        if (donVi.LogoId is null) return NoContent();

        var tepTin = await _db.TepTin.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == donVi.LogoId.Value, ct)
            .ConfigureAwait(false);

        if (tepTin is null) return NoContent();

        var phanMoRong = (tepTin.PhanMoRong ?? string.Empty).ToLowerInvariant();

        if (!AnhDonViChoPhep.Contains(phanMoRong)) return NoContent();

        var luong = await _luuTru.TaiXuongAsync(tepTin.Bucket, tepTin.DuongDan, ct)
            .ConfigureAwait(false);

        Response.Headers["X-Content-Type-Options"] = "nosniff";

        return File(luong, phanMoRong switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "image/jpeg",
        });
    }

    /// <summary>Khong nhan SVG: tep SVG chua duoc ma script chay trong goc cua ung dung.</summary>
    private static readonly HashSet<string> AnhDonViChoPhep =
        new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".gif", ".webp" };

    [HttpPost]
    [Authorize(Policy = MaQuyen.DonViCauHinh)]
    public async Task<IActionResult> ThemAsync([FromBody] LuuDonViDto duLieu, CancellationToken ct)
    {
        var banGhi = await _dichVu.ThemAsync(TaoThucThe(new DonVi(), duLieu), ct);
        await _dichVu.CapNhatDuongDanCayAsync(banGhi.Id, ct);
        return Ok(PhanHoiApi<DonVi>.Ok(banGhi, "Thêm đơn vị thành công"));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = MaQuyen.DonViCauHinh)]
    public async Task<IActionResult> SuaAsync(
        Guid id, [FromBody] LuuDonViDto duLieu, CancellationToken ct)
    {
        var banGhi = await _dichVu.CapNhatAsync(id, x => TaoThucThe(x, duLieu), ct);
        await _dichVu.CapNhatDuongDanCayAsync(id, ct);
        return Ok(PhanHoiApi<DonVi>.Ok(banGhi, "Cập nhật thành công"));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = MaQuyen.DonViCauHinh)]
    public async Task<IActionResult> XoaAsync(Guid id, CancellationToken ct)
    {
        await _dichVu.XoaAsync(id, ct);
        return Ok(PhanHoiApi.Ok("Đã xóa"));
    }

    /// <summary>Chức năng 44 — Chuyển đơn vị sang cấp trên khác (kéo thả trên cây tổ chức).</summary>
    [HttpPost("{id:guid}/chuyen-cha")]
    [Authorize(Policy = MaQuyen.DonViCauHinh)]
    public async Task<IActionResult> ChuyenChaAsync(
        Guid id, [FromBody] ChuyenChaDonViDto duLieu, CancellationToken ct)
    {
        await _dichVu.ChuyenChaAsync(id, duLieu.DonViChaMoiId, ct);
        return Ok(PhanHoiApi.Ok("Đã chuyển đơn vị"));
    }

    /// <summary>Chức năng 44 — Gộp đơn vị khi sáp nhập.</summary>
    [HttpPost("{id:guid}/gop-vao/{dichId:guid}")]
    [Authorize(Policy = MaQuyen.DonViCauHinh)]
    public async Task<IActionResult> GopAsync(Guid id, Guid dichId, CancellationToken ct)
    {
        var soBanGhi = await _dichVu.GopAsync(id, dichId, ct);
        return Ok(PhanHoiApi<int>.Ok(soBanGhi, $"Đã gộp và chuyển {soBanGhi} bản ghi liên quan"));
    }

    private static DonVi TaoThucThe(DonVi x, LuuDonViDto d)
    {
        x.Ma = d.Ma;
        x.Ten = d.Ten;
        x.MoTa = d.MoTa;
        x.ThuTu = d.ThuTu;
        x.TrangThai = d.TrangThai;
        x.TenVietTat = d.TenVietTat;
        x.DonViChaId = d.DonViChaId;
        x.Loai = d.Loai;
        x.DiaChi = d.DiaChi;
        x.DienThoai = d.DienThoai;
        x.Email = d.Email;
        x.NguoiDaiDien = d.NguoiDaiDien;
        x.ChucVuNguoiDaiDien = d.ChucVuNguoiDaiDien;
        x.LaDonViPheDuyet = d.LaDonViPheDuyet;
        x.CapPheDuyet = d.CapPheDuyet;
        x.LogoId = d.LogoId;
        x.TieuDeVanBan = d.TieuDeVanBan;
        x.NguoiKyMacDinh = d.NguoiKyMacDinh;
        x.ChucVuNguoiKyMacDinh = d.ChucVuNguoiKyMacDinh;
        return x;
    }
}
