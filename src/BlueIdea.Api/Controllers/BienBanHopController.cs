using BlueIdea.Api.Chung;
using BlueIdea.Application.Chung;
using BlueIdea.Application.HoiDong;
using BlueIdea.Application.KySo;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Domain.SangKien;
using BlueIdea.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Api.Controllers;

/// <summary>
/// Nhóm IV — Biên bản phiên họp hội đồng: lập, xem, ký nhận, xuất PDF và ký số.
///
/// Biên bản sinh từ dữ liệu có thật của phiên họp (điểm danh, phiếu bầu, kết luận) chứ không
/// phải form nhập tay — biên bản là căn cứ hành chính, gõ tay lại số liệu là mở đường cho sai
/// lệch giữa biên bản và dữ liệu hệ thống.
/// </summary>
[ApiController]
[Route("api/v1/bien-ban-hop")]
[Authorize]
[Produces("application/json")]
public sealed class BienBanHopController : ControllerBase
{
    private readonly DichVuBienBanHop _dichVu;
    private readonly DichVuKySo _kySo;
    private readonly IAppDbContext _db;
    private readonly ILuuTruTep _luuTru;
    private readonly IDongHoHeThong _dongHo;
    private readonly INguoiDungHienTai _nguoiDung;

    public BienBanHopController(
        DichVuBienBanHop dichVu, DichVuKySo kySo, IAppDbContext db,
        ILuuTruTep luuTru, IDongHoHeThong dongHo, INguoiDungHienTai nguoiDung)
    {
        _dichVu = dichVu;
        _kySo = kySo;
        _db = db;
        _luuTru = luuTru;
        _dongHo = dongHo;
        _nguoiDung = nguoiDung;
    }

    /// <summary>Lập (hoặc làm mới) biên bản của một phiên họp đã kết thúc.</summary>
    [HttpPost("phien-hop/{phienHopId:guid}")]
    [Authorize(Policy = MaQuyen.HoiDongHopPhien)]
    public async Task<IActionResult> LapAsync(Guid phienHopId, CancellationToken ct)
        => Ok(PhanHoiApi<BienBanHopDto>.Ok(
            await _dichVu.LapAsync(phienHopId, ct), "Đã lập biên bản phiên họp"));

    /// <summary>Biên bản của một phiên họp — trả <c>null</c> khi chưa lập.</summary>
    [HttpGet("phien-hop/{phienHopId:guid}")]
    public async Task<IActionResult> LayTheoPhienAsync(Guid phienHopId, CancellationToken ct)
        => Ok(PhanHoiApi<BienBanHopDto?>.Ok(await _dichVu.LayTheoPhienAsync(phienHopId, ct)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> LayAsync(Guid id, CancellationToken ct)
        => Ok(PhanHoiApi<BienBanHopDto>.Ok(await _dichVu.LayAsync(id, ct)));

    /// <summary>Thành viên ký nhận nội dung biên bản (khác với ký số bằng chứng thư).</summary>
    [HttpPost("{id:guid}/ky")]
    [Authorize(Policy = MaQuyen.HoiDongXem)]
    public async Task<IActionResult> KyAsync(Guid id, CancellationToken ct)
    {
        await _dichVu.KyAsync(id, ct);
        return Ok(PhanHoiApi.Ok("Đã ký nhận biên bản"));
    }

    /// <summary>Xuất biên bản ra PDF theo mẫu văn bản hành chính.</summary>
    [HttpGet("{id:guid}/xuat-pdf")]
    public async Task<IActionResult> XuatPdfAsync(Guid id, CancellationToken ct)
    {
        var bienBan = await _dichVu.LayAsync(id, ct);
        var tep = TaoPdf(bienBan);

        return File(tep, "application/pdf",
            $"bien-ban-{bienBan.SoBienBan.Replace('/', '-')}.pdf");
    }

    /// <summary>
    /// Ký số biên bản: sinh PDF, lưu thành tệp rồi ký bằng chứng thư đang cấu hình.
    ///
    /// Sinh PDF ngay tại thời điểm ký (không dùng lại tệp cũ) để chữ ký luôn gắn với đúng nội
    /// dung biên bản hiện hành.
    /// </summary>
    [HttpPost("{id:guid}/ky-so")]
    [Authorize(Policy = MaQuyen.QuyetDinhKySo)]
    public async Task<IActionResult> KySoAsync(Guid id, CancellationToken ct)
    {
        var bienBan = await _dichVu.LayAsync(id, ct);
        var noiDung = TaoPdf(bienBan);

        var tenLuuTru = $"{Guid.NewGuid():N}.pdf";

        await using (var luong = new MemoryStream(noiDung))
        {
            var duongDan = await _luuTru
                .TaiLenAsync(luong, tenLuuTru, "application/pdf", "blueidea", ct);

            _db.TepTin.Add(new TepTin
            {
                Id = Guid.NewGuid(),
                TenGoc = $"bien-ban-{bienBan.SoBienBan.Replace('/', '-')}.pdf",
                TenLuuTru = tenLuuTru,
                DuongDan = duongDan,
                Bucket = "blueidea",
                KichThuoc = noiDung.LongLength,
                MimeType = "application/pdf",
                PhanMoRong = ".pdf",
                HashSha256 = Convert.ToHexString(
                    System.Security.Cryptography.SHA256.HashData(noiDung)).ToLowerInvariant(),
                NguoiTaiLenId = _nguoiDung.Id,
                NgayTaiLen = _dongHo.BayGio,
                DaQuetVirus = true
            });
        }

        var tepBienBan = _db.TepTin.Local.First(x => x.TenLuuTru == tenLuuTru);
        await _db.SaveChangesAsync(ct);

        await _dichVu.GanTepAsync(id, tepBienBan.Id, ct);

        var tepDaKyId = await _kySo.KyTepAsync(tepBienBan.Id, "BIEN_BAN", id, ct);

        return Ok(PhanHoiApi<Guid>.Ok(tepDaKyId, "Đã ký số biên bản"));
    }

    /// <summary>Lịch sử ký số của biên bản.</summary>
    [HttpGet("{id:guid}/lich-su-ky-so")]
    public async Task<IActionResult> LayLichSuKySoAsync(Guid id, CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<NhatKyKySo>>.Ok(
            await _kySo.LichSuAsync("BIEN_BAN", id, ct)));

    // -----------------------------------------------------------------------------------

    private static byte[] TaoPdf(BienBanHopDto b)
    {
        var bangHoSo = new BangPdf(
            "KẾT QUẢ XÉT TỪNG HỒ SƠ",
            new[] { "TT", "Mã hồ sơ", "Tên sáng kiến", "Tác giả chính", "Điểm", "Đồng ý", "Tỷ lệ", "Kết luận" },
            b.DanhSachHoSo.Select((x, i) => new[]
            {
                (i + 1).ToString(),
                x.MaHoSo,
                x.TenSangKien,
                x.TacGiaChinh ?? string.Empty,
                x.TongDiem?.ToString("0.##") ?? string.Empty,
                $"{x.SoPhieuDongY}/{x.SoPhieuDongY + x.SoPhieuKhongDongY + x.SoPhieuYKienKhac}",
                $"{x.TyLeDongY:0.##}%",
                x.DatNguong ? "Thông qua" : "Không thông qua"
            }).ToList());

        var bangThanhPhan = new BangPdf(
            "THÀNH PHẦN DỰ HỌP",
            new[] { "TT", "Họ tên và chức danh" },
            b.ThanhPhanDuHop.Select((x, i) => new[] { (i + 1).ToString(), x }).ToList());

        var bang = new List<BangPdf> { bangThanhPhan, bangHoSo };

        if (b.ThanhVienVang.Count > 0)
        {
            bang.Insert(1, new BangPdf(
                "THÀNH VIÊN VẮNG MẶT",
                new[] { "TT", "Họ tên và chức danh" },
                b.ThanhVienVang.Select((x, i) => new[] { (i + 1).ToString(), x }).ToList()));
        }

        var chuTich = b.ChuKy.FirstOrDefault(x => x.ChucDanh == ChucDanhHoiDong.ChuTich);

        return BoXuatPdf.XuatTaiLieu(
            tenCoQuanChuQuan: b.TenHoiDong,
            tenDonVi: string.Empty,
            tieuDe: $"BIÊN BẢN HỌP HỘI ĐỒNG SỐ {b.SoBienBan}",
            phuDe: b.TenPhien,
            thongTin: new[]
            {
                new DongThongTin("Thời gian họp", b.ThoiGianBatDau.ToString("HH:mm 'ngày' dd/MM/yyyy")),
                new DongThongTin("Địa điểm", b.DiaDiem ?? "—"),
                new DongThongTin("Hình thức", TenHinhThuc(b.HinhThuc)),
                new DongThongTin("Số thành viên", $"{b.SoCoMat}/{b.SoThanhVien} có mặt"),
                new DongThongTin("Số hồ sơ đưa ra xét", b.DanhSachHoSo.Count.ToString())
            },
            bang: bang,
            noiDungThem: string.IsNullOrWhiteSpace(b.KetLuanChung)
                ? null
                : $"KẾT LUẬN CỦA HỘI ĐỒNG: {b.KetLuanChung}",
            nguoiKy: chuTich?.HoTen,
            chucVuNguoiKy: chuTich is null ? null : DichVuBienBanHop.TenChucDanh(chuTich.ChucDanh));
    }

    private static string TenHinhThuc(string ma) => ma switch
    {
        "TRUC_TUYEN" => "Trực tuyến",
        "KET_HOP" => "Kết hợp trực tiếp và trực tuyến",
        _ => "Trực tiếp"
    };
}
