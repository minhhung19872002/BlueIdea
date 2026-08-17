using BlueIdea.Api.Chung;
using BlueIdea.Application.BaoCao;
using BlueIdea.Domain.Chung;
using BlueIdea.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueIdea.Api.Controllers;

/// <summary>Chức năng 38–40 + Dashboard — Thống kê, báo cáo, xuất Excel/PDF.</summary>
[ApiController]
[Route("api/v1/bao-cao")]
[Authorize]
[Produces("application/json")]
public sealed class BaoCaoController : ControllerBase
{
    private readonly DichVuBaoCao _dichVu;

    public BaoCaoController(DichVuBaoCao dichVu) => _dichVu = dichVu;

    private static readonly List<CotXuat<DongBaoCaoSangKien>> CotSangKien = new()
    {
        new("Mã hồ sơ", x => x.MaHoSo, 18),
        new("Tên sáng kiến", x => x.TenSangKien, 50),
        new("Tác giả", x => x.TacGia, 30),
        new("Đơn vị", x => x.TenDonVi, 30),
        new("Lĩnh vực", x => x.TenLinhVuc, 25),
        new("Đợt", x => x.TenDot, 30),
        new("Tổng điểm", x => x.TongDiem, 12),
        new("Mức công nhận", x => x.TenMucCongNhan, 25),
        new("Kết quả", x => x.KetQua, 14),
        new("Lý do", x => x.LyDo, 40),
        new("Ngày công nhận", x => x.NgayCongNhan, 16),
        new("Số quyết định", x => x.SoQuyetDinh, 20)
    };

    /// <summary>Dashboard tổng quan (số liệu + dữ liệu biểu đồ).</summary>
    [HttpGet("tong-quan")]
    public async Task<IActionResult> TongQuanAsync(
        [FromQuery] ThamSoBaoCao thamSo, CancellationToken ct)
        => Ok(PhanHoiApi<ThongKeTongQuan>.Ok(await _dichVu.TongQuanAsync(thamSo, ct)));

    /// <summary>Chức năng 38 — Danh sách sáng kiến đạt.</summary>
    [HttpGet("sang-kien-dat")]
    public async Task<IActionResult> SangKienDatAsync(
        [FromQuery] ThamSoBaoCao thamSo, CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<DongBaoCaoSangKien>>.Ok(
            await _dichVu.SangKienDatAsync(thamSo, ct)));

    [HttpGet("sang-kien-dat/xuat-excel")]
    [Authorize(Policy = MaQuyen.BaoCaoXuat)]
    public async Task<IActionResult> XuatSangKienDatAsync(
        [FromQuery] ThamSoBaoCao thamSo, CancellationToken ct)
    {
        var duLieu = await _dichVu.SangKienDatAsync(thamSo, ct);
        var tep = BoXuatExcel.Xuat("Sang kien dat", "DANH SÁCH SÁNG KIẾN ĐƯỢC CÔNG NHẬN",
            duLieu, CotSangKien);

        return File(tep, ThamSoPhanTrangApi.MimeExcel, "sang-kien-dat.xlsx");
    }

    /// <summary>Chức năng 39 — Danh sách sáng kiến chưa đạt (kèm lý do và điểm).</summary>
    [HttpGet("sang-kien-chua-dat")]
    public async Task<IActionResult> SangKienChuaDatAsync(
        [FromQuery] ThamSoBaoCao thamSo, CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<DongBaoCaoSangKien>>.Ok(
            await _dichVu.SangKienChuaDatAsync(thamSo, ct)));

    [HttpGet("sang-kien-chua-dat/xuat-excel")]
    [Authorize(Policy = MaQuyen.BaoCaoXuat)]
    public async Task<IActionResult> XuatSangKienChuaDatAsync(
        [FromQuery] ThamSoBaoCao thamSo, CancellationToken ct)
    {
        var duLieu = await _dichVu.SangKienChuaDatAsync(thamSo, ct);
        var tep = BoXuatExcel.Xuat("Sang kien chua dat", "DANH SÁCH SÁNG KIẾN CHƯA ĐẠT",
            duLieu, CotSangKien);

        return File(tep, ThamSoPhanTrangApi.MimeExcel, "sang-kien-chua-dat.xlsx");
    }

    /// <summary>Chức năng 40 — Thống kê theo đơn vị (phục vụ đánh giá thi đua).</summary>
    [HttpGet("theo-don-vi")]
    public async Task<IActionResult> TheoDonViAsync(
        [FromQuery] ThamSoBaoCao thamSo, CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<DongBaoCaoDonVi>>.Ok(
            await _dichVu.TheoDonViAsync(thamSo, ct)));

    [HttpGet("theo-don-vi/xuat-excel")]
    [Authorize(Policy = MaQuyen.BaoCaoXuat)]
    public async Task<IActionResult> XuatTheoDonViAsync(
        [FromQuery] ThamSoBaoCao thamSo, CancellationToken ct)
    {
        var duLieu = await _dichVu.TheoDonViAsync(thamSo, ct);

        var tep = BoXuatExcel.Xuat("Theo don vi", "THỐNG KÊ SÁNG KIẾN THEO ĐƠN VỊ", duLieu,
            new List<CotXuat<DongBaoCaoDonVi>>
            {
                new("Mã đơn vị", x => x.MaDonVi, 18),
                new("Tên đơn vị", x => x.TenDonVi, 40),
                new("Tổng hồ sơ", x => x.TongSo, 14),
                new("Đạt", x => x.SoDat, 10),
                new("Không đạt", x => x.SoKhongDat, 14),
                new("Đang xử lý", x => x.SoDangXuLy, 14),
                new("Tỷ lệ đạt (%)", x => x.TyLeDat, 16)
            });

        return File(tep, ThamSoPhanTrangApi.MimeExcel, "thong-ke-theo-don-vi.xlsx");
    }

    /// <summary>Xuất báo cáo tổng hợp ra PDF (mẫu văn bản hành chính).</summary>
    [HttpGet("sang-kien-dat/xuat-pdf")]
    [Authorize(Policy = MaQuyen.BaoCaoXuat)]
    public async Task<IActionResult> XuatPdfSangKienDatAsync(
        [FromQuery] ThamSoBaoCao thamSo, CancellationToken ct)
    {
        var duLieu = await _dichVu.SangKienDatAsync(thamSo, ct);

        var tep = BoXuatPdf.XuatTaiLieu(
            tenCoQuanChuQuan: "Ủy ban nhân dân thành phố",
            tenDonVi: "Hội đồng sáng kiến",
            tieuDe: "Danh sách sáng kiến được công nhận",
            phuDe: null,
            thongTin: new List<DongThongTin>
            {
                new("Tổng số sáng kiến", duLieu.Count.ToString()),
                new("Thời điểm lập", DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7))
                    .ToString("dd/MM/yyyy HH:mm"))
            },
            bang: new List<BangPdf>
            {
                new("Danh sách chi tiết",
                    new[] { "Mã hồ sơ", "Tên sáng kiến", "Tác giả", "Đơn vị", "Điểm", "Mức công nhận" },
                    duLieu.Select(x => new[]
                    {
                        x.MaHoSo,
                        x.TenSangKien,
                        x.TacGia,
                        x.TenDonVi ?? string.Empty,
                        x.TongDiem?.ToString() ?? string.Empty,
                        x.TenMucCongNhan ?? string.Empty
                    }).ToList())
            });

        return File(tep, ThamSoPhanTrangApi.MimePdf, "sang-kien-dat.pdf");
    }
}
