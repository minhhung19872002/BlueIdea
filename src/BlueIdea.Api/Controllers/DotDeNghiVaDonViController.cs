using BlueIdea.Api.Chung;
using BlueIdea.Application.DanhMuc;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.DanhMuc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<IActionResult> LayDanhSachAsync(
        [FromQuery] ThamSoLocDanhMuc thamSo, CancellationToken ct)
        => Ok(PhanHoiPhanTrang<DanhMucDto>.Tu(await _dichVu.LayDanhSachAsync(thamSo, ct)));

    /// <summary>
    /// Danh sách đợt kèm trạng thái vòng đời — màn hình quản trị đợt dùng để bật/tắt nút
    /// Mở / Đóng / Khoá đợt.
    /// </summary>
    [HttpGet("quan-ly")]
    public async Task<IActionResult> LayDanhSachQuanLyAsync(
        [FromQuery] ThamSoLocDanhMuc thamSo, CancellationToken ct)
        => Ok(PhanHoiPhanTrang<DotDeNghiQuanLyDto>.Tu(
            await _dichVu.LayDanhSachQuanLyAsync(thamSo, ct)));

    /// <summary>Các đợt đang mở và còn hạn nộp — dùng ở bước 1 của wizard nộp hồ sơ.</summary>
    [HttpGet("dang-mo")]
    public async Task<IActionResult> LayDotDangMoAsync(CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<DanhMucDto>>.Ok(await _dichVu.LayDotDangMoAsync(ct)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> LayTheoIdAsync(Guid id, CancellationToken ct)
        => Ok(PhanHoiApi<DotDeNghi>.Ok(await _dichVu.LayTheoIdAsync(id, ct)));

    [HttpPost]
    public async Task<IActionResult> ThemAsync([FromBody] LuuDotDeNghiDto duLieu, CancellationToken ct)
    {
        var banGhi = await _dichVu.ThemAsync(TaoThucThe(new DotDeNghi(), duLieu), ct);
        return Ok(PhanHoiApi<DotDeNghi>.Ok(banGhi, "Thêm đợt đề nghị thành công"));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> SuaAsync(
        Guid id, [FromBody] LuuDotDeNghiDto duLieu, CancellationToken ct)
    {
        var banGhi = await _dichVu.CapNhatAsync(id, x => TaoThucThe(x, duLieu), ct);
        return Ok(PhanHoiApi<DotDeNghi>.Ok(banGhi, "Cập nhật thành công"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> XoaAsync(Guid id, CancellationToken ct)
    {
        await _dichVu.XoaAsync(id, ct);
        return Ok(PhanHoiApi.Ok("Đã xóa"));
    }

    /// <summary>Mở đợt — bắt đầu nhận hồ sơ.</summary>
    [HttpPost("{id:guid}/mo-dot")]
    public async Task<IActionResult> MoDotAsync(Guid id, CancellationToken ct)
    {
        await _dichVu.DoiTrangThaiDotAsync(id, TrangThaiDot.DangMo, ct);
        return Ok(PhanHoiApi.Ok("Đã mở đợt"));
    }

    /// <summary>Đóng đợt — ngừng nhận hồ sơ mới nhưng vẫn xử lý hồ sơ đã nộp.</summary>
    [HttpPost("{id:guid}/dong-dot")]
    public async Task<IActionResult> DongDotAsync(Guid id, CancellationToken ct)
    {
        await _dichVu.DoiTrangThaiDotAsync(id, TrangThaiDot.DaDong, ct);
        return Ok(PhanHoiApi.Ok("Đã đóng đợt"));
    }

    /// <summary>Khóa đợt — toàn bộ dữ liệu chỉ đọc.</summary>
    [HttpPost("{id:guid}/khoa-dot")]
    public async Task<IActionResult> KhoaDotAsync(Guid id, CancellationToken ct)
    {
        await _dichVu.DoiTrangThaiDotAsync(id, TrangThaiDot.DaKhoa, ct);
        return Ok(PhanHoiApi.Ok("Đã khóa đợt"));
    }

    /// <summary>Sao chép cấu hình đợt từ năm trước.</summary>
    [HttpPost("{id:guid}/sao-chep")]
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

    public DonViController(DichVuDonVi dichVu) => _dichVu = dichVu;

    [HttpGet]
    public async Task<IActionResult> LayDanhSachAsync(
        [FromQuery] ThamSoLocDanhMuc thamSo, CancellationToken ct)
        => Ok(PhanHoiPhanTrang<DanhMucDto>.Tu(await _dichVu.LayDanhSachAsync(thamSo, ct)));

    /// <summary>Cây tổ chức đầy đủ.</summary>
    [HttpGet("cay")]
    public async Task<IActionResult> LayCayAsync(CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<NutCay>>.Ok(await _dichVu.LayCayAsync(ct)));

    [HttpGet("chon")]
    public async Task<IActionResult> LayDanhSachChonAsync(CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<DanhMucDto>>.Ok(await _dichVu.LayDanhSachChonAsync(ct)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> LayTheoIdAsync(Guid id, CancellationToken ct)
        => Ok(PhanHoiApi<DonVi>.Ok(await _dichVu.LayTheoIdAsync(id, ct)));

    [HttpPost]
    public async Task<IActionResult> ThemAsync([FromBody] LuuDonViDto duLieu, CancellationToken ct)
    {
        var banGhi = await _dichVu.ThemAsync(TaoThucThe(new DonVi(), duLieu), ct);
        await _dichVu.CapNhatDuongDanCayAsync(banGhi.Id, ct);
        return Ok(PhanHoiApi<DonVi>.Ok(banGhi, "Thêm đơn vị thành công"));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> SuaAsync(
        Guid id, [FromBody] LuuDonViDto duLieu, CancellationToken ct)
    {
        var banGhi = await _dichVu.CapNhatAsync(id, x => TaoThucThe(x, duLieu), ct);
        await _dichVu.CapNhatDuongDanCayAsync(id, ct);
        return Ok(PhanHoiApi<DonVi>.Ok(banGhi, "Cập nhật thành công"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> XoaAsync(Guid id, CancellationToken ct)
    {
        await _dichVu.XoaAsync(id, ct);
        return Ok(PhanHoiApi.Ok("Đã xóa"));
    }

    /// <summary>Chức năng 44 — Chuyển đơn vị sang cấp trên khác (kéo thả trên cây tổ chức).</summary>
    [HttpPost("{id:guid}/chuyen-cha")]
    public async Task<IActionResult> ChuyenChaAsync(
        Guid id, [FromBody] ChuyenChaDonViDto duLieu, CancellationToken ct)
    {
        await _dichVu.ChuyenChaAsync(id, duLieu.DonViChaMoiId, ct);
        return Ok(PhanHoiApi.Ok("Đã chuyển đơn vị"));
    }

    /// <summary>Chức năng 44 — Gộp đơn vị khi sáp nhập.</summary>
    [HttpPost("{id:guid}/gop-vao/{dichId:guid}")]
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
        x.TieuDeVanBan = d.TieuDeVanBan;
        x.NguoiKyMacDinh = d.NguoiKyMacDinh;
        x.ChucVuNguoiKyMacDinh = d.ChucVuNguoiKyMacDinh;
        return x;
    }
}
