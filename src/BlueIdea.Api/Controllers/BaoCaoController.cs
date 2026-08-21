using BlueIdea.Api.Chung;
using BlueIdea.Application.BaoCao;
using BlueIdea.Application.Chung;
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
    /// <summary>Cac loai bao cao xuat nen duoc — chan bang danh sach trang, khong nhan chuoi tuy y.</summary>
    private static readonly HashSet<string> LoaiXuatNenChoPhep = new(StringComparer.OrdinalIgnoreCase)
    {
        "sang-kien-dat", "sang-kien-chua-dat", "theo-don-vi", "theo-tac-gia", "thoi-gian-xu-ly"
    };

    private readonly DichVuBaoCao _dichVu;
    private readonly BoNhoDemBaoCao _dem;
    private readonly IHangDoiCongViecNen _hangDoi;
    private readonly INguoiDungHienTai _nguoiDung;

    public BaoCaoController(
        DichVuBaoCao dichVu, BoNhoDemBaoCao dem, IHangDoiCongViecNen hangDoi,
        INguoiDungHienTai nguoiDung)
    {
        _dichVu = dichVu;
        _dem = dem;
        _hangDoi = hangDoi;
        _nguoiDung = nguoiDung;
    }

    /// <summary>
    /// Đặt lệnh xuất báo cáo ở tiến trình nền, hệ thống gửi thông báo kèm liên kết tải về khi xong.
    ///
    /// Dùng cho báo cáo nhiều năm / toàn hệ thống: giữ người dùng chờ trên một request HTTP vừa dễ
    /// time-out ở reverse proxy, vừa chiếm một luồng xử lý của máy chủ suốt thời gian đó.
    /// </summary>
    [HttpPost("{loai}/xuat-nen")]
    [Authorize(Policy = MaQuyen.BaoCaoXuat)]
    public IActionResult DatLenhXuatNen(
        string loai, [FromQuery] ThamSoBaoCao thamSo)
    {
        if (!LoaiXuatNenChoPhep.Contains(loai))
        {
            return BadRequest(PhanHoiApi.Loi(
                "LOAI_BAO_CAO_KHONG_HO_TRO",
                $"Loại báo cáo '{loai}' không hỗ trợ xuất nền."));
        }

        if (_nguoiDung.Id is null)
        {
            return Unauthorized(PhanHoiApi.Loi("CHUA_XAC_THUC", "Chưa đăng nhập."));
        }

        _hangDoi.XepLichXuatBaoCao(loai.ToLowerInvariant(), thamSo, _nguoiDung.Id.Value);

        return Accepted(PhanHoiApi.Ok(
            "Đã nhận yêu cầu xuất báo cáo. Hệ thống sẽ gửi thông báo kèm liên kết tải về khi xong."));
    }

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
    [Authorize(Policy = MaQuyen.BaoCaoXem)]
    public async Task<IActionResult> TongQuanAsync(
        [FromQuery] ThamSoBaoCao thamSo, CancellationToken ct)
        => Ok(PhanHoiApi<ThongKeTongQuan>.Ok(await _dem.LayHoacTinhAsync(
            "tong-quan", thamSo, () => _dichVu.TongQuanAsync(thamSo, ct), ct)));

    /// <summary>Chức năng 38 — Danh sách sáng kiến đạt.</summary>
    [HttpGet("sang-kien-dat")]
    [Authorize(Policy = MaQuyen.BaoCaoXem)]
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
    [Authorize(Policy = MaQuyen.BaoCaoXem)]
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
    [Authorize(Policy = MaQuyen.BaoCaoXem)]
    public async Task<IActionResult> TheoDonViAsync(
        [FromQuery] ThamSoBaoCao thamSo, CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<DongBaoCaoDonVi>>.Ok(await _dem.LayHoacTinhAsync(
            "theo-don-vi", thamSo, () => _dichVu.TheoDonViAsync(thamSo, ct), ct)));

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

    /// <summary>Thống kê theo tác giả — ai có bao nhiêu sáng kiến, bao nhiêu đạt.</summary>
    [HttpGet("theo-tac-gia")]
    [Authorize(Policy = MaQuyen.BaoCaoXem)]
    public async Task<IActionResult> TheoTacGiaAsync(
        [FromQuery] ThamSoBaoCao thamSo, CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<DongBaoCaoTacGia>>.Ok(await _dem.LayHoacTinhAsync(
            "theo-tac-gia", thamSo, () => _dichVu.TheoTacGiaAsync(thamSo, ct), ct)));

    [HttpGet("theo-tac-gia/xuat-excel")]
    [Authorize(Policy = MaQuyen.BaoCaoXuat)]
    public async Task<IActionResult> XuatTheoTacGiaAsync(
        [FromQuery] ThamSoBaoCao thamSo, CancellationToken ct)
    {
        var duLieu = await _dichVu.TheoTacGiaAsync(thamSo, ct);

        var tep = BoXuatExcel.Xuat("Theo tac gia", "THỐNG KÊ SÁNG KIẾN THEO TÁC GIẢ", duLieu,
            new List<CotXuat<DongBaoCaoTacGia>>
            {
                new("Họ và tên", x => x.HoTen, 30),
                new("Đơn vị công tác", x => x.DonViCongTac, 35),
                new("Chức vụ", x => x.ChucVu, 25),
                new("Tổng số", x => x.TongSo, 12),
                new("Là tác giả chính", x => x.SoLaTacGiaChinh, 18),
                new("Đạt", x => x.SoDat, 10),
                new("Điểm trung bình", x => x.DiemTrungBinh, 18),
                new("Tỷ lệ đạt (%)", x => x.TyLeDat, 16)
            });

        return File(tep, ThamSoPhanTrangApi.MimeExcel, "thong-ke-theo-tac-gia.xlsx");
    }

    /// <summary>Thời gian xử lý trung bình theo từng bước quy trình.</summary>
    [HttpGet("thoi-gian-xu-ly")]
    [Authorize(Policy = MaQuyen.BaoCaoXem)]
    public async Task<IActionResult> ThoiGianXuLyAsync(
        [FromQuery] ThamSoBaoCao thamSo, CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<DongThoiGianXuLy>>.Ok(await _dem.LayHoacTinhAsync(
            "thoi-gian-xu-ly", thamSo, () => _dichVu.ThoiGianXuLyAsync(thamSo, ct), ct)));

    [HttpGet("thoi-gian-xu-ly/xuat-excel")]
    [Authorize(Policy = MaQuyen.BaoCaoXuat)]
    public async Task<IActionResult> XuatThoiGianXuLyAsync(
        [FromQuery] ThamSoBaoCao thamSo, CancellationToken ct)
    {
        var duLieu = await _dichVu.ThoiGianXuLyAsync(thamSo, ct);

        var tep = BoXuatExcel.Xuat("Thoi gian xu ly", "THỜI GIAN XỬ LÝ TRUNG BÌNH THEO BƯỚC",
            duLieu,
            new List<CotXuat<DongThoiGianXuLy>>
            {
                new("Bước xử lý", x => x.TenBuoc, 40),
                new("Số lượt", x => x.SoLuot, 12),
                new("Số ngày trung bình", x => x.SoNgayTrungBinh, 20),
                new("Lâu nhất (ngày)", x => x.SoNgayLauNhat, 18),
                new("Số lượt quá hạn", x => x.SoLuotQuaHan, 18)
            });

        return File(tep, ThamSoPhanTrangApi.MimeExcel, "thoi-gian-xu-ly.xlsx");
    }

    /// <summary>Báo cáo tổng hợp một năm — số liệu cho báo cáo cuối năm.</summary>
    [HttpGet("tong-hop-nam/{nam:int}")]
    [Authorize(Policy = MaQuyen.BaoCaoXem)]
    public async Task<IActionResult> TongHopNamAsync(
        int nam, [FromQuery] ThamSoBaoCao thamSo, CancellationToken ct)
        => Ok(PhanHoiApi<BaoCaoTongHopNam>.Ok(await _dem.LayHoacTinhAsync(
            "tong-hop-nam", new { nam, thamSo }, () => _dichVu.TongHopNamAsync(nam, thamSo, ct), ct)));

    /// <summary>Xuất báo cáo tổng hợp năm ra PDF theo mẫu văn bản hành chính.</summary>
    [HttpGet("tong-hop-nam/{nam:int}/xuat-pdf")]
    [Authorize(Policy = MaQuyen.BaoCaoXuat)]
    public async Task<IActionResult> XuatTongHopNamAsync(
        int nam, [FromQuery] ThamSoBaoCao thamSo, CancellationToken ct)
    {
        var b = await _dichVu.TongHopNamAsync(nam, thamSo, ct);

        var bang = new List<BangPdf>
        {
            new("PHÂN BỔ THEO LĨNH VỰC", new[] { "Lĩnh vực", "Số hồ sơ" },
                b.TheoLinhVuc.Select(x => new[] { x.Ten, x.SoLuong.ToString() }).ToList()),
            new("PHÂN BỔ THEO ĐỢT", new[] { "Đợt đề nghị", "Số hồ sơ" },
                b.TheoDot.Select(x => new[] { x.Ten, x.SoLuong.ToString() }).ToList()),
            new("PHÂN BỔ THEO MỨC CÔNG NHẬN", new[] { "Mức công nhận", "Số hồ sơ" },
                b.TheoMucCongNhan.Select(x => new[] { x.Ten, x.SoLuong.ToString() }).ToList())
        };

        var pdf = BoXuatPdf.XuatTaiLieu(
            tenCoQuanChuQuan: string.Empty,
            tenDonVi: string.Empty,
            tieuDe: $"BÁO CÁO TỔNG HỢP CÔNG TÁC SÁNG KIẾN NĂM {b.Nam}",
            phuDe: null,
            thongTin: new[]
            {
                new DongThongTin("Tổng số hồ sơ", b.TongHoSo.ToString()),
                new DongThongTin("Được công nhận", b.SoDat.ToString()),
                new DongThongTin("Không đạt", b.SoKhongDat.ToString()),
                new DongThongTin("Đang xử lý", b.SoDangXuLy.ToString()),
                new DongThongTin("Tỷ lệ đạt", $"{b.TyLeDat}%"),
                new DongThongTin("Số tác giả tham gia", b.SoTacGia.ToString()),
                new DongThongTin("Số đơn vị có hồ sơ", b.SoDonViThamGia.ToString()),
                new DongThongTin("Tổng giá trị làm lợi ước tính",
                    b.TongGiaTriLamLoi?.ToString("#,0") ?? "—")
            },
            bang: bang.Where(x => x.CacDong.Count > 0).ToList());

        return File(pdf, "application/pdf", $"bao-cao-tong-hop-nam-{nam}.pdf");
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
    /// <summary>Chức năng 39 — Xuất PDF danh sách sáng kiến chưa đạt (kèm lý do và điểm).</summary>
    [HttpGet("sang-kien-chua-dat/xuat-pdf")]
    [Authorize(Policy = MaQuyen.BaoCaoXuat)]
    public async Task<IActionResult> XuatPdfSangKienChuaDatAsync(
        [FromQuery] ThamSoBaoCao thamSo, CancellationToken ct)
    {
        var duLieu = await _dichVu.SangKienChuaDatAsync(thamSo, ct);

        var tep = BoXuatPdf.XuatTaiLieu(
            tenCoQuanChuQuan: "Ủy ban nhân dân thành phố",
            tenDonVi: "Hội đồng sáng kiến",
            tieuDe: "Danh sách sáng kiến chưa được công nhận",
            phuDe: null,
            thongTin: new List<DongThongTin>
            {
                new("Tổng số hồ sơ", duLieu.Count.ToString()),
                new("Thời điểm lập", ThoiDiemLap())
            },
            bang: new List<BangPdf>
            {
                new("Danh sách chi tiết",
                    new[] { "Mã hồ sơ", "Tên sáng kiến", "Tác giả", "Đơn vị", "Điểm", "Lý do" },
                    duLieu.Select(x => new[]
                    {
                        x.MaHoSo,
                        x.TenSangKien,
                        x.TacGia,
                        x.TenDonVi ?? string.Empty,
                        x.TongDiem?.ToString() ?? string.Empty,
                        x.LyDo ?? string.Empty
                    }).ToList())
            });

        return File(tep, ThamSoPhanTrangApi.MimePdf, "sang-kien-chua-dat.pdf");
    }

    /// <summary>Chức năng 40 — Xuất PDF thống kê theo đơn vị (phục vụ đánh giá thi đua).</summary>
    [HttpGet("theo-don-vi/xuat-pdf")]
    [Authorize(Policy = MaQuyen.BaoCaoXuat)]
    public async Task<IActionResult> XuatPdfTheoDonViAsync(
        [FromQuery] ThamSoBaoCao thamSo, CancellationToken ct)
    {
        var duLieu = await _dichVu.TheoDonViAsync(thamSo, ct);

        var tep = BoXuatPdf.XuatTaiLieu(
            tenCoQuanChuQuan: "Ủy ban nhân dân thành phố",
            tenDonVi: "Hội đồng sáng kiến",
            tieuDe: "Thống kê sáng kiến theo đơn vị",
            phuDe: null,
            thongTin: new List<DongThongTin>
            {
                new("Số đơn vị", duLieu.Count.ToString()),
                new("Tổng hồ sơ", duLieu.Sum(x => x.TongSo).ToString()),
                new("Tổng số đạt", duLieu.Sum(x => x.SoDat).ToString()),
                new("Thời điểm lập", ThoiDiemLap())
            },
            bang: new List<BangPdf>
            {
                new("Chi tiết theo đơn vị",
                    new[] { "Mã đơn vị", "Tên đơn vị", "Tổng", "Đạt", "Không đạt", "Đang xử lý", "Tỷ lệ đạt (%)" },
                    duLieu.Select(x => new[]
                    {
                        x.MaDonVi,
                        x.TenDonVi,
                        x.TongSo.ToString(),
                        x.SoDat.ToString(),
                        x.SoKhongDat.ToString(),
                        x.SoDangXuLy.ToString(),
                        x.TyLeDat.ToString()
                    }).ToList())
            });

        return File(tep, ThamSoPhanTrangApi.MimePdf, "thong-ke-theo-don-vi.pdf");
    }

    /// <summary>Xuất PDF thống kê theo tác giả.</summary>
    [HttpGet("theo-tac-gia/xuat-pdf")]
    [Authorize(Policy = MaQuyen.BaoCaoXuat)]
    public async Task<IActionResult> XuatPdfTheoTacGiaAsync(
        [FromQuery] ThamSoBaoCao thamSo, CancellationToken ct)
    {
        var duLieu = await _dichVu.TheoTacGiaAsync(thamSo, ct);

        var tep = BoXuatPdf.XuatTaiLieu(
            tenCoQuanChuQuan: "Ủy ban nhân dân thành phố",
            tenDonVi: "Hội đồng sáng kiến",
            tieuDe: "Thống kê sáng kiến theo tác giả",
            phuDe: null,
            thongTin: new List<DongThongTin>
            {
                new("Số tác giả", duLieu.Count.ToString()),
                new("Thời điểm lập", ThoiDiemLap())
            },
            bang: new List<BangPdf>
            {
                new("Chi tiết theo tác giả",
                    new[] { "Họ và tên", "Đơn vị công tác", "Tổng số", "Tác giả chính", "Đạt", "Tỷ lệ đạt (%)" },
                    duLieu.Select(x => new[]
                    {
                        x.HoTen,
                        x.DonViCongTac ?? string.Empty,
                        x.TongSo.ToString(),
                        x.SoLaTacGiaChinh.ToString(),
                        x.SoDat.ToString(),
                        x.TyLeDat.ToString()
                    }).ToList())
            });

        return File(tep, ThamSoPhanTrangApi.MimePdf, "thong-ke-theo-tac-gia.pdf");
    }

    /// <summary>Xuất PDF thời gian xử lý trung bình theo bước.</summary>
    [HttpGet("thoi-gian-xu-ly/xuat-pdf")]
    [Authorize(Policy = MaQuyen.BaoCaoXuat)]
    public async Task<IActionResult> XuatPdfThoiGianXuLyAsync(
        [FromQuery] ThamSoBaoCao thamSo, CancellationToken ct)
    {
        var duLieu = await _dichVu.ThoiGianXuLyAsync(thamSo, ct);

        var tep = BoXuatPdf.XuatTaiLieu(
            tenCoQuanChuQuan: "Ủy ban nhân dân thành phố",
            tenDonVi: "Hội đồng sáng kiến",
            tieuDe: "Thời gian xử lý trung bình theo bước",
            phuDe: null,
            thongTin: new List<DongThongTin>
            {
                new("Số bước thống kê", duLieu.Count.ToString()),
                new("Tổng lượt quá hạn", duLieu.Sum(x => x.SoLuotQuaHan).ToString()),
                new("Thời điểm lập", ThoiDiemLap())
            },
            bang: new List<BangPdf>
            {
                new("Chi tiết theo bước",
                    new[] { "Bước xử lý", "Số lượt", "TB (ngày)", "Lâu nhất (ngày)", "Quá hạn" },
                    duLieu.Select(x => new[]
                    {
                        x.TenBuoc,
                        x.SoLuot.ToString(),
                        x.SoNgayTrungBinh.ToString(),
                        x.SoNgayLauNhat.ToString(),
                        x.SoLuotQuaHan.ToString()
                    }).ToList())
            });

        return File(tep, ThamSoPhanTrangApi.MimePdf, "thoi-gian-xu-ly.pdf");
    }

    /// <summary>Thời điểm lập báo cáo theo giờ Việt Nam — in trên mọi bản xuất.</summary>
    private static string ThoiDiemLap()
        => DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm");
}
