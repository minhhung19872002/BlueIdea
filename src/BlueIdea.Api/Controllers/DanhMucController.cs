using BlueIdea.Api.Chung;
using BlueIdea.Application.DanhMuc;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.DanhMuc;
using BlueIdea.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueIdea.Api.Controllers;

/// <summary>Dữ liệu gửi lên khi thêm/sửa danh mục đơn giản.</summary>
public class LuuDanhMucDto
{
    public string Ma { get; set; } = string.Empty;

    public string Ten { get; set; } = string.Empty;

    public string? MoTa { get; set; }

    public int ThuTu { get; set; }

    public short TrangThai { get; set; } = TrangThaiDanhMuc.HoatDong;
}

public sealed class LuuLinhVucDto : LuuDanhMucDto
{
    public Guid? LinhVucChaId { get; set; }
}

public sealed class LuuLoaiTacGiaDto : LuuDanhMucDto
{
    public bool ChoPhepNhieuTacGia { get; set; }

    public int SoTacGiaToiDa { get; set; } = 1;
}

/// <summary>Chức năng 1 — Danh mục lĩnh vực áp dụng sáng kiến.</summary>
[ApiController]
[Route("api/v1/danh-muc/linh-vuc")]
[Authorize]
[Produces("application/json")]
public sealed class LinhVucController : ControllerBase
{
    private readonly DichVuLinhVuc _dichVu;

    public LinhVucController(DichVuLinhVuc dichVu) => _dichVu = dichVu;

    /// <summary>Danh sách lĩnh vực có phân trang, tìm kiếm không dấu.</summary>
    [HttpGet]
    public async Task<IActionResult> LayDanhSachAsync(
        [FromQuery] ThamSoLocDanhMuc thamSo, CancellationToken ct)
        => Ok(PhanHoiPhanTrang<DanhMucDto>.Tu(await _dichVu.LayDanhSachAsync(thamSo, ct)));

    /// <summary>Danh sách rút gọn cho dropdown.</summary>
    [HttpGet("chon")]
    public async Task<IActionResult> LayDanhSachChonAsync(CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<DanhMucDto>>.Ok(await _dichVu.LayDanhSachChonAsync(ct)));

    /// <summary>Cây lĩnh vực (hiển thị dạng Tree trên giao diện).</summary>
    [HttpGet("cay")]
    public async Task<IActionResult> LayCayAsync(CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<NutCay>>.Ok(await _dichVu.LayCayAsync(ct)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> LayTheoIdAsync(Guid id, CancellationToken ct)
        => Ok(PhanHoiApi<LinhVuc>.Ok(await _dichVu.LayTheoIdAsync(id, ct)));

    [HttpPost]
    public async Task<IActionResult> ThemAsync([FromBody] LuuLinhVucDto duLieu, CancellationToken ct)
    {
        var banGhi = await _dichVu.ThemAsync(new LinhVuc
        {
            Ma = duLieu.Ma,
            Ten = duLieu.Ten,
            MoTa = duLieu.MoTa,
            ThuTu = duLieu.ThuTu,
            TrangThai = duLieu.TrangThai,
            LinhVucChaId = duLieu.LinhVucChaId
        }, ct);

        return Ok(PhanHoiApi<LinhVuc>.Ok(banGhi, "Thêm lĩnh vực thành công"));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> SuaAsync(
        Guid id, [FromBody] LuuLinhVucDto duLieu, CancellationToken ct)
    {
        var banGhi = await _dichVu.CapNhatAsync(id, x =>
        {
            x.Ma = duLieu.Ma;
            x.Ten = duLieu.Ten;
            x.MoTa = duLieu.MoTa;
            x.ThuTu = duLieu.ThuTu;
            x.TrangThai = duLieu.TrangThai;
            x.LinhVucChaId = duLieu.LinhVucChaId;
        }, ct);

        return Ok(PhanHoiApi<LinhVuc>.Ok(banGhi, "Cập nhật thành công"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> XoaAsync(Guid id, CancellationToken ct)
    {
        await _dichVu.XoaAsync(id, ct);
        return Ok(PhanHoiApi.Ok("Đã xóa"));
    }

    [HttpPatch("{id:guid}/trang-thai")]
    public async Task<IActionResult> DoiTrangThaiAsync(
        Guid id, [FromQuery] short trangThai, CancellationToken ct)
    {
        await _dichVu.DoiTrangThaiAsync(id, trangThai, ct);
        return Ok(PhanHoiApi.Ok("Đã cập nhật trạng thái"));
    }

    [HttpPut("sap-xep")]
    public async Task<IActionResult> SapXepAsync([FromBody] List<Guid> thuTu, CancellationToken ct)
    {
        await _dichVu.SapXepAsync(thuTu, ct);
        return Ok(PhanHoiApi.Ok("Đã lưu thứ tự"));
    }

    /// <summary>Xuất danh sách ra Excel theo bộ lọc hiện tại.</summary>
    [HttpGet("xuat-excel")]
    public async Task<IActionResult> XuatExcelAsync(
        [FromQuery] ThamSoLocDanhMuc thamSo, CancellationToken ct)
    {
        thamSo.SoDong = ThamSoPhanTrangApi.SoDongXuatToiDa;
        var duLieu = await _dichVu.LayDanhSachAsync(thamSo, ct);

        var tep = BoXuatExcel.Xuat("Linh vuc", "DANH MỤC LĨNH VỰC", duLieu.DuLieu,
            new List<CotXuat<DanhMucDto>>
            {
                new("Mã", x => x.Ma, 15),
                new("Tên lĩnh vực", x => x.Ten, 40),
                new("Mô tả", x => x.MoTa, 50),
                new("Thứ tự", x => x.ThuTu, 10),
                new("Trạng thái", x => x.TrangThai == 1 ? "Hoạt động" : "Ngừng", 15)
            });

        return File(tep, ThamSoPhanTrangApi.MimeExcel, "danh-muc-linh-vuc.xlsx");
    }
}

/// <summary>Chức năng 2 — Danh mục đối tượng áp dụng.</summary>
[ApiController]
[Route("api/v1/danh-muc/doi-tuong")]
[Authorize]
[Produces("application/json")]
public sealed class DoiTuongController : ControllerBase
{
    private readonly DichVuDoiTuong _dichVu;

    public DoiTuongController(DichVuDoiTuong dichVu) => _dichVu = dichVu;

    [HttpGet]
    public async Task<IActionResult> LayDanhSachAsync(
        [FromQuery] ThamSoLocDanhMuc thamSo, CancellationToken ct)
        => Ok(PhanHoiPhanTrang<DanhMucDto>.Tu(await _dichVu.LayDanhSachAsync(thamSo, ct)));

    [HttpGet("chon")]
    public async Task<IActionResult> LayDanhSachChonAsync(CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<DanhMucDto>>.Ok(await _dichVu.LayDanhSachChonAsync(ct)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> LayTheoIdAsync(Guid id, CancellationToken ct)
        => Ok(PhanHoiApi<DoiTuong>.Ok(await _dichVu.LayTheoIdAsync(id, ct)));

    [HttpPost]
    public async Task<IActionResult> ThemAsync([FromBody] LuuDanhMucDto duLieu, CancellationToken ct)
    {
        var banGhi = await _dichVu.ThemAsync(new DoiTuong
        {
            Ma = duLieu.Ma,
            Ten = duLieu.Ten,
            MoTa = duLieu.MoTa,
            ThuTu = duLieu.ThuTu,
            TrangThai = duLieu.TrangThai
        }, ct);

        return Ok(PhanHoiApi<DoiTuong>.Ok(banGhi, "Thêm đối tượng thành công"));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> SuaAsync(
        Guid id, [FromBody] LuuDanhMucDto duLieu, CancellationToken ct)
    {
        var banGhi = await _dichVu.CapNhatAsync(id, x =>
        {
            x.Ma = duLieu.Ma;
            x.Ten = duLieu.Ten;
            x.MoTa = duLieu.MoTa;
            x.ThuTu = duLieu.ThuTu;
            x.TrangThai = duLieu.TrangThai;
        }, ct);

        return Ok(PhanHoiApi<DoiTuong>.Ok(banGhi, "Cập nhật thành công"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> XoaAsync(Guid id, CancellationToken ct)
    {
        await _dichVu.XoaAsync(id, ct);
        return Ok(PhanHoiApi.Ok("Đã xóa"));
    }
}

/// <summary>Chức năng 4 — Danh mục loại tác giả.</summary>
[ApiController]
[Route("api/v1/danh-muc/loai-tac-gia")]
[Authorize]
[Produces("application/json")]
public sealed class LoaiTacGiaController : ControllerBase
{
    private readonly DichVuLoaiTacGia _dichVu;

    public LoaiTacGiaController(DichVuLoaiTacGia dichVu) => _dichVu = dichVu;

    [HttpGet]
    public async Task<IActionResult> LayDanhSachAsync(
        [FromQuery] ThamSoLocDanhMuc thamSo, CancellationToken ct)
        => Ok(PhanHoiPhanTrang<DanhMucDto>.Tu(await _dichVu.LayDanhSachAsync(thamSo, ct)));

    [HttpGet("chon")]
    public async Task<IActionResult> LayDanhSachChonAsync(CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<DanhMucDto>>.Ok(await _dichVu.LayDanhSachChonAsync(ct)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> LayTheoIdAsync(Guid id, CancellationToken ct)
        => Ok(PhanHoiApi<LoaiTacGia>.Ok(await _dichVu.LayTheoIdAsync(id, ct)));

    [HttpPost]
    public async Task<IActionResult> ThemAsync(
        [FromBody] LuuLoaiTacGiaDto duLieu, CancellationToken ct)
    {
        var banGhi = await _dichVu.ThemAsync(new LoaiTacGia
        {
            Ma = duLieu.Ma,
            Ten = duLieu.Ten,
            MoTa = duLieu.MoTa,
            ThuTu = duLieu.ThuTu,
            TrangThai = duLieu.TrangThai,
            ChoPhepNhieuTacGia = duLieu.ChoPhepNhieuTacGia,
            SoTacGiaToiDa = duLieu.SoTacGiaToiDa
        }, ct);

        return Ok(PhanHoiApi<LoaiTacGia>.Ok(banGhi, "Thêm loại tác giả thành công"));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> SuaAsync(
        Guid id, [FromBody] LuuLoaiTacGiaDto duLieu, CancellationToken ct)
    {
        var banGhi = await _dichVu.CapNhatAsync(id, x =>
        {
            x.Ma = duLieu.Ma;
            x.Ten = duLieu.Ten;
            x.MoTa = duLieu.MoTa;
            x.ThuTu = duLieu.ThuTu;
            x.TrangThai = duLieu.TrangThai;
            x.ChoPhepNhieuTacGia = duLieu.ChoPhepNhieuTacGia;
            x.SoTacGiaToiDa = duLieu.SoTacGiaToiDa;
        }, ct);

        return Ok(PhanHoiApi<LoaiTacGia>.Ok(banGhi, "Cập nhật thành công"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> XoaAsync(Guid id, CancellationToken ct)
    {
        await _dichVu.XoaAsync(id, ct);
        return Ok(PhanHoiApi.Ok("Đã xóa"));
    }
}

/// <summary>Hằng số dùng chung cho các controller danh mục.</summary>
public static class ThamSoPhanTrangApi
{
    public const int SoDongXuatToiDa = 200;

    public const string MimeExcel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public const string MimePdf = "application/pdf";
}
