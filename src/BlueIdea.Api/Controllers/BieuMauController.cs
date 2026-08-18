using BlueIdea.Api.Chung;
using BlueIdea.Application.BaoCao;
using BlueIdea.Application.Chung;
using BlueIdea.Application.DanhMuc;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.DanhMuc;
using BlueIdea.Reporting;
using BlueIdea.Shared.KetQua;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueIdea.Api.Controllers;

public sealed class LuuBieuMauXuatDto : LuuDanhMucDto
{
    public string Loai { get; set; } = LoaiBieuMau.Khac;

    public string DinhDang { get; set; } = "DOCX";

    public Guid? FileTemplateId { get; set; }

    public List<CauHinhTruongBieuMau> CauHinhTruong { get; set; } = new();
}

public sealed class LuuBieuMauThongKeDto : LuuDanhMucDto
{
    public string? LoaiBaoCao { get; set; }

    public List<CotBaoCao> CauHinhCot { get; set; } = new();

    public List<string> DinhDangXuat { get; set; } = new() { "XLSX", "PDF" };
}

/// <summary>Chức năng 6 — Biểu mẫu xuất và ánh xạ placeholder.</summary>
[ApiController]
[Route("api/v1/danh-muc/bieu-mau-xuat")]
[Authorize]
[Produces("application/json")]
public sealed class BieuMauXuatController : ControllerBase
{
    private readonly DichVuBieuMauXuat _dichVu;
    private readonly DichVuSinhBieuMau _sinhBieuMau;

    public BieuMauXuatController(DichVuBieuMauXuat dichVu, DichVuSinhBieuMau sinhBieuMau)
    {
        _dichVu = dichVu;
        _sinhBieuMau = sinhBieuMau;
    }

    [HttpGet]
    [Authorize(Policy = MaQuyen.DanhMucXem)]
    public async Task<IActionResult> LayDanhSachAsync(
        [FromQuery] ThamSoLocDanhMuc thamSo, CancellationToken ct)
        => Ok(PhanHoiPhanTrang<DanhMucDto>.Tu(await _dichVu.LayDanhSachAsync(thamSo, ct)));

    [HttpGet("chon")]
    public async Task<IActionResult> LayDanhSachChonAsync(CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<DanhMucDto>>.Ok(await _dichVu.LayDanhSachChonAsync(ct)));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = MaQuyen.DanhMucXem)]
    public async Task<IActionResult> LayTheoIdAsync(Guid id, CancellationToken ct)
        => Ok(PhanHoiApi<BieuMauXuat>.Ok(await _dichVu.LayTheoIdAsync(id, ct)));

    [HttpPost]
    [Authorize(Policy = MaQuyen.DanhMucThem)]
    public async Task<IActionResult> ThemAsync([FromBody] LuuBieuMauXuatDto duLieu, CancellationToken ct)
        => Ok(PhanHoiApi<BieuMauXuat>.Ok(
            await _dichVu.ThemAsync(ApDung(new BieuMauXuat(), duLieu), ct), "Đã thêm biểu mẫu"));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = MaQuyen.DanhMucSua)]
    public async Task<IActionResult> SuaAsync(
        Guid id, [FromBody] LuuBieuMauXuatDto duLieu, CancellationToken ct)
        => Ok(PhanHoiApi<BieuMauXuat>.Ok(
            await _dichVu.CapNhatAsync(id, x => ApDung(x, duLieu), ct), "Đã cập nhật"));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = MaQuyen.DanhMucXoa)]
    public async Task<IActionResult> XoaAsync(Guid id, CancellationToken ct)
    {
        await _dichVu.XoaAsync(id, ct);
        return Ok(PhanHoiApi.Ok("Đã xoá"));
    }

    /// <summary>Danh sách trường dữ liệu dùng được cho một loại biểu mẫu.</summary>
    [HttpGet("truong-kha-dung")]
    [Authorize(Policy = MaQuyen.DanhMucXem)]
    public IActionResult LayTruongKhaDung([FromQuery] string loai)
        => Ok(PhanHoiApi<IReadOnlyList<TruongBieuMauKhaDung>>.Ok(
            DichVuSinhBieuMau.TruongKhaDung(loai)));

    /// <summary>
    /// Xem trước biểu mẫu. Không truyền <c>hoSoId</c> thì dựng bằng dữ liệu mẫu, để người cấu hình
    /// kiểm tra bố cục ngay lúc soạn mà không cần đi tìm một hồ sơ thật.
    /// </summary>
    [HttpGet("{id:guid}/xem-truoc")]
    [Authorize(Policy = MaQuyen.DanhMucXem)]
    public async Task<IActionResult> XemTruocAsync(
        Guid id, [FromQuery] Guid? hoSoId, [FromQuery] Guid? phienHopId, CancellationToken ct)
    {
        var ketQua = phienHopId is not null
            ? await _sinhBieuMau.SinhChoPhienHopAsync(id, phienHopId.Value, ct)
            : await _sinhBieuMau.SinhChoHoSoAsync(id, hoSoId, ct);

        return Ok(PhanHoiApi<BieuMauDaPhanGiai>.Ok(ketQua));
    }

    /// <summary>Xem trước biểu mẫu dưới dạng PDF đúng bố cục văn bản hành chính.</summary>
    [HttpGet("{id:guid}/xem-truoc.pdf")]
    [Authorize(Policy = MaQuyen.DanhMucXem)]
    public async Task<IActionResult> XemTruocPdfAsync(
        Guid id, [FromQuery] Guid? hoSoId, [FromQuery] Guid? phienHopId, CancellationToken ct)
    {
        var ketQua = phienHopId is not null
            ? await _sinhBieuMau.SinhChoPhienHopAsync(id, phienHopId.Value, ct)
            : await _sinhBieuMau.SinhChoHoSoAsync(id, hoSoId, ct);

        var pdf = BoXuatPdf.XuatTaiLieu(
            tenCoQuanChuQuan: string.Empty,
            tenDonVi: string.Empty,
            tieuDe: ketQua.TieuDe.ToUpperInvariant(),
            phuDe: ketQua.PhuDe,
            thongTin: ketQua.DongDuLieu
                .Select(x => new DongThongTin(x.Nhan, x.GiaTri))
                .ToList());

        return File(pdf, "application/pdf", $"xem-truoc-bieu-mau-{id:N}.pdf");
    }

    private static BieuMauXuat ApDung(BieuMauXuat x, LuuBieuMauXuatDto d)
    {
        x.Ma = d.Ma;
        x.Ten = d.Ten;
        x.MoTa = d.MoTa;
        x.ThuTu = d.ThuTu;
        x.TrangThai = d.TrangThai;
        x.Loai = d.Loai;
        x.DinhDang = d.DinhDang;
        x.FileTemplateId = d.FileTemplateId;
        x.CauHinhTruong = d.CauHinhTruong;
        return x;
    }
}

/// <summary>Chức năng 7 — Biểu mẫu thống kê cho báo cáo tuỳ biến.</summary>
[ApiController]
[Route("api/v1/danh-muc/bieu-mau-thong-ke")]
[Authorize]
[Produces("application/json")]
public sealed class BieuMauThongKeController : ControllerBase
{
    private readonly DichVuBieuMauThongKe _dichVu;

    public BieuMauThongKeController(DichVuBieuMauThongKe dichVu) => _dichVu = dichVu;

    [HttpGet]
    [Authorize(Policy = MaQuyen.DanhMucXem)]
    public async Task<IActionResult> LayDanhSachAsync(
        [FromQuery] ThamSoLocDanhMuc thamSo, CancellationToken ct)
        => Ok(PhanHoiPhanTrang<DanhMucDto>.Tu(await _dichVu.LayDanhSachAsync(thamSo, ct)));

    [HttpGet("chon")]
    public async Task<IActionResult> LayDanhSachChonAsync(CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<DanhMucDto>>.Ok(await _dichVu.LayDanhSachChonAsync(ct)));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = MaQuyen.DanhMucXem)]
    public async Task<IActionResult> LayTheoIdAsync(Guid id, CancellationToken ct)
        => Ok(PhanHoiApi<BieuMauThongKe>.Ok(await _dichVu.LayTheoIdAsync(id, ct)));

    [HttpPost]
    [Authorize(Policy = MaQuyen.DanhMucThem)]
    public async Task<IActionResult> ThemAsync(
        [FromBody] LuuBieuMauThongKeDto duLieu, CancellationToken ct)
    {
        BatBuocCauHinhHopLe(duLieu.CauHinhCot);

        return Ok(PhanHoiApi<BieuMauThongKe>.Ok(
            await _dichVu.ThemAsync(ApDung(new BieuMauThongKe(), duLieu), ct), "Đã thêm biểu mẫu"));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = MaQuyen.DanhMucSua)]
    public async Task<IActionResult> SuaAsync(
        Guid id, [FromBody] LuuBieuMauThongKeDto duLieu, CancellationToken ct)
    {
        BatBuocCauHinhHopLe(duLieu.CauHinhCot);

        return Ok(PhanHoiApi<BieuMauThongKe>.Ok(
            await _dichVu.CapNhatAsync(id, x => ApDung(x, duLieu), ct), "Đã cập nhật"));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = MaQuyen.DanhMucXoa)]
    public async Task<IActionResult> XoaAsync(Guid id, CancellationToken ct)
    {
        await _dichVu.XoaAsync(id, ct);
        return Ok(PhanHoiApi.Ok("Đã xoá"));
    }

    /// <summary>
    /// Chặn cấu hình sai NGAY KHI LƯU thay vì để vỡ lúc chạy báo cáo — quản trị viên nhìn thấy
    /// lỗi ở đúng màn hình vừa nhập, không phải mò lại sau vài ngày.
    /// </summary>
    private static void BatBuocCauHinhHopLe(IReadOnlyList<CotBaoCao> cacCot)
    {
        var loi = DichVuBaoCaoTuyBien.KiemTraCauHinh(cacCot);

        if (loi.Count > 0)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe, string.Join(" ", loi));
        }
    }

    private static BieuMauThongKe ApDung(BieuMauThongKe x, LuuBieuMauThongKeDto d)
    {
        x.Ma = d.Ma;
        x.Ten = d.Ten;
        x.MoTa = d.MoTa;
        x.ThuTu = d.ThuTu;
        x.TrangThai = d.TrangThai;
        x.LoaiBaoCao = d.LoaiBaoCao;
        x.CauHinhCot = d.CauHinhCot;
        x.DinhDangXuat = d.DinhDangXuat;
        return x;
    }
}
