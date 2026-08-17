using BlueIdea.Api.Chung;
using BlueIdea.Application.BaoCao;
using BlueIdea.Application.Chung;
using BlueIdea.Application.DanhGia;
using BlueIdea.Application.QuanTri;
using BlueIdea.Domain.Chung;
using BlueIdea.Reporting;
using BlueIdea.Shared.KetQua;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueIdea.Api.Controllers;

/// <summary>
/// Chức năng 6, 7, 35, 43 — Nhập người dùng từ Excel, xuất phiếu chấm hàng loạt,
/// báo cáo tuỳ biến và quét placeholder biểu mẫu.
/// </summary>
[ApiController]
[Route("api/v1/nhap-xuat")]
[Authorize]
[Produces("application/json")]
public sealed class NhapXuatController : ControllerBase
{
    /// <summary>Thứ tự cột bắt buộc của tệp nhập người dùng.</summary>
    private static readonly string[] CotNhapNguoiDung =
    {
        "Tên đăng nhập", "Họ và tên", "Email", "Điện thoại", "Chức vụ", "Mã đơn vị", "Mã vai trò"
    };

    private const long DungLuongTepNhapToiDa = 10L * 1024 * 1024;

    private readonly DichVuNhapNguoiDung _nhapNguoiDung;
    private readonly DichVuXuatPhieuCham _xuatPhieu;
    private readonly DichVuBaoCaoTuyBien _baoCaoTuyBien;
    private readonly IDichVuCauHinh _cauHinh;

    public NhapXuatController(
        DichVuNhapNguoiDung nhapNguoiDung, DichVuXuatPhieuCham xuatPhieu,
        DichVuBaoCaoTuyBien baoCaoTuyBien, IDichVuCauHinh cauHinh)
    {
        _nhapNguoiDung = nhapNguoiDung;
        _xuatPhieu = xuatPhieu;
        _baoCaoTuyBien = baoCaoTuyBien;
        _cauHinh = cauHinh;
    }

    // ------------------------------------------------ Chức năng 43 — nhập người dùng

    /// <summary>Tải tệp mẫu Excel kèm hướng dẫn và một dòng ví dụ.</summary>
    [HttpGet("nguoi-dung/mau-nhap")]
    [Authorize(Policy = MaQuyen.NguoiDungThem)]
    public IActionResult TaiMauNhapNguoiDung()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Người dùng");

        for (var i = 0; i < CotNhapNguoiDung.Length; i++)
        {
            var o = sheet.Cell(1, i + 1);
            o.Value = CotNhapNguoiDung[i];
            o.Style.Font.Bold = true;
            o.Style.Fill.BackgroundColor = XLColor.FromHtml("#1677FF");
            o.Style.Font.FontColor = XLColor.White;
        }

        var viDu = new[]
        {
            "nguyen.van.a", "Nguyễn Văn A", "a.nguyen@donvi.gov.vn", "0912345678",
            "Chuyên viên", "PHONG_GDDT", "TAC_GIA"
        };

        for (var i = 0; i < viDu.Length; i++)
        {
            sheet.Cell(2, i + 1).Value = viDu[i];
        }

        sheet.Cell(4, 1).Value =
            "Hướng dẫn: xoá dòng ví dụ trước khi nhập. Tên đăng nhập chỉ dùng chữ thường không dấu, "
            + "số và các ký tự . _ -  •  Mã đơn vị và mã vai trò phải khớp với danh mục trong hệ thống "
            + "(để trống mã đơn vị nếu tài khoản không thuộc đơn vị nào)  •  "
            + "Mật khẩu do hệ thống sinh, người dùng bắt buộc đổi ở lần đăng nhập đầu tiên.";
        sheet.Cell(4, 1).Style.Font.Italic = true;

        sheet.Columns(1, CotNhapNguoiDung.Length).AdjustToContents(10d, 40d);

        using var bo = new MemoryStream();
        workbook.SaveAs(bo);

        return File(bo.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "mau-nhap-nguoi-dung.xlsx");
    }

    /// <summary>
    /// Nhập người dùng từ Excel. Mặc định <c>chayThu=true</c>: chỉ kiểm tra và báo lỗi từng dòng,
    /// không ghi gì — để quản trị viên xem trước rồi mới xác nhận.
    /// </summary>
    [HttpPost("nguoi-dung")]
    [Authorize(Policy = MaQuyen.NguoiDungThem)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> NhapNguoiDungAsync(
        IFormFile tep, [FromQuery] bool chayThu = true, CancellationToken ct = default)
    {
        if (tep is null || tep.Length == 0)
        {
            throw new NghiepVuException(MaLoiHeThong.TepKhongHopLe, "Chưa chọn tệp.");
        }

        if (tep.Length > DungLuongTepNhapToiDa)
        {
            throw new NghiepVuException(MaLoiHeThong.VuotDungLuongToiDa,
                $"Tệp vượt quá {DungLuongTepNhapToiDa / 1024 / 1024}MB.");
        }

        var cacDong = DocTepNhapNguoiDung(tep);
        var ketQua = await _nhapNguoiDung.NhapAsync(cacDong, chayThu, ct);

        var thongBao = ketQua.ChayThu
            ? $"Kiểm tra {ketQua.TongDong} dòng: {ketQua.SoHopLe} hợp lệ, {ketQua.SoLoi} lỗi."
            : $"Đã tạo {ketQua.SoHopLe} tài khoản.";

        return Ok(PhanHoiApi<KetQuaNhapNguoiDung>.Ok(ketQua, thongBao));
    }

    // ------------------------------------------------ Chức năng 35 — xuất phiếu chấm

    /// <summary>Xuất toàn bộ phiếu chấm đã gửi của một hồ sơ ra PDF, mỗi phiếu một trang.</summary>
    [HttpGet("phieu-cham/ho-so/{sangKienId:guid}")]
    [Authorize(Policy = MaQuyen.DanhGiaXem)]
    public async Task<IActionResult> XuatPhieuChamTheoHoSoAsync(Guid sangKienId, CancellationToken ct)
    {
        var duLieu = await _xuatPhieu.TheoHoSoAsync(sangKienId, ct);
        return await TaoPdfPhieuAsync(duLieu, $"phieu-cham-{duLieu[0].MaHoSo}.pdf", ct);
    }

    /// <summary>Xuất phiếu chấm hàng loạt của một hội đồng — dùng khi in hồ sơ phiên họp.</summary>
    [HttpGet("phieu-cham/hoi-dong/{hoiDongId:guid}")]
    [Authorize(Policy = MaQuyen.DanhGiaXem)]
    public async Task<IActionResult> XuatPhieuChamTheoHoiDongAsync(
        Guid hoiDongId, [FromQuery] Guid? dotDeNghiId, CancellationToken ct)
    {
        var duLieu = await _xuatPhieu.TheoHoiDongAsync(hoiDongId, dotDeNghiId, ct);
        return await TaoPdfPhieuAsync(duLieu, $"phieu-cham-hoi-dong-{duLieu.Count}-phieu.pdf", ct);
    }

    // ------------------------------------------------ Chức năng 7 — báo cáo tuỳ biến

    /// <summary>Danh sách trường có thể đưa vào báo cáo — màn hình cấu hình đọc từ đây.</summary>
    [HttpGet("bao-cao-tuy-bien/nguon-du-lieu")]
    [Authorize(Policy = MaQuyen.BaoCaoXem)]
    public IActionResult LayNguonDuLieuBaoCao()
        => Ok(PhanHoiApi<object>.Ok(
            DichVuBaoCaoTuyBien.NguonDuLieuKhaDung()
                .Select(x => new { ma = x.Ma, tieuDeGoiY = x.TieuDeGoiY })));

    /// <summary>Chạy một biểu mẫu thống kê đã cấu hình.</summary>
    [HttpGet("bao-cao-tuy-bien/{bieuMauId:guid}")]
    [Authorize(Policy = MaQuyen.BaoCaoXem)]
    public async Task<IActionResult> ChayBaoCaoTuyBienAsync(
        Guid bieuMauId,
        [FromQuery] Guid? dotDeNghiId,
        [FromQuery] Guid? donViId,
        [FromQuery] Guid? linhVucId,
        [FromQuery] string? ketQua,
        CancellationToken ct)
        => Ok(PhanHoiApi<KetQuaBaoCaoTuyBien>.Ok(
            await _baoCaoTuyBien.ChayAsync(bieuMauId, dotDeNghiId, donViId, linhVucId, ketQua, ct)));

    /// <summary>Xuất báo cáo tuỳ biến ra Excel.</summary>
    [HttpGet("bao-cao-tuy-bien/{bieuMauId:guid}/xuat-excel")]
    [Authorize(Policy = MaQuyen.BaoCaoXuat)]
    public async Task<IActionResult> XuatBaoCaoTuyBienAsync(
        Guid bieuMauId,
        [FromQuery] Guid? dotDeNghiId,
        [FromQuery] Guid? donViId,
        [FromQuery] Guid? linhVucId,
        [FromQuery] string? ketQua,
        CancellationToken ct)
    {
        var bc = await _baoCaoTuyBien.ChayAsync(bieuMauId, dotDeNghiId, donViId, linhVucId, ketQua, ct);

        var cacCot = bc.TieuDeCot
            .Select((tieuDe, chiMuc) => new CotXuat<IReadOnlyList<string>>(
                tieuDe,
                dong => chiMuc < dong.Count ? dong[chiMuc] : string.Empty))
            .ToList();

        var tep = BoXuatExcel.Xuat(bc.TenBaoCao, bc.TenBaoCao, bc.CacDong, cacCot, bc.BoLocDaApDung);

        return File(tep,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{TaoTenTep(bc.TenBaoCao)}.xlsx");
    }

    // ------------------------------------------------ Chức năng 6 — quét placeholder .docx

    /// <summary>
    /// Quét placeholder <c>{{ tenBien }}</c> trong tệp mẫu .docx để cấu hình ánh xạ trường.
    /// Chỉ đọc, không lưu tệp.
    /// </summary>
    [HttpPost("bieu-mau/quet-placeholder")]
    [Authorize(Policy = MaQuyen.DanhMucSua)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> QuetPlaceholderAsync(IFormFile tep, CancellationToken ct)
    {
        if (tep is null || tep.Length == 0)
        {
            throw new NghiepVuException(MaLoiHeThong.TepKhongHopLe, "Chưa chọn tệp mẫu.");
        }

        if (!Path.GetExtension(tep.FileName).Equals(".docx", StringComparison.OrdinalIgnoreCase))
        {
            throw new NghiepVuException(MaLoiHeThong.TepKhongHopLe,
                "Chỉ hỗ trợ tệp .docx (Word 2007 trở lên).");
        }

        await using var luong = tep.OpenReadStream();

        try
        {
            var ketQua = BoQuetPlaceholderDocx.Quet(luong);

            return Ok(PhanHoiApi<object>.Ok(new
            {
                placeholder = ketQua.Placeholder,
                soDoanVan = ketQua.SoDoanVan,
                soBang = ketQua.SoBang,
                canhBao = ketQua.CanhBao,
                nguonGoiY = DichVuBaoCaoTuyBien.NguonDuLieuKhaDung()
                    .Select(x => new { ma = x.Ma, tieuDeGoiY = x.TieuDeGoiY })
            }));
        }
        catch (Exception ex) when (ex is not NghiepVuException)
        {
            throw new NghiepVuException(MaLoiHeThong.TepKhongHopLe,
                $"Không đọc được tệp .docx: {ex.Message}");
        }
    }

    // -----------------------------------------------------------------------------------

    private async Task<IActionResult> TaoPdfPhieuAsync(
        IReadOnlyList<DuLieuPhieuCham> duLieu, string tenTep, CancellationToken ct)
    {
        var tenCoQuan = await _cauHinh.LayAsync(KhoaCauHinh.TenDonVi, string.Empty, ct);
        var tenHeThong = await _cauHinh.LayAsync(KhoaCauHinh.TenHeThong, string.Empty, ct);

        var phieu = duLieu.Select(x => new PhieuChamPdf(
            x.SoPhieu, x.MaHoSo, x.TenSangKien, x.TenTacGiaChinh, x.TenDonVi,
            x.TenThanhVien, x.ChucDanh, x.TenHoiDong, x.TongDiem, x.KetLuan,
            x.TenMucCongNhanDeXuat, x.NhanXetChung, x.UuDiem, x.HanChe, x.NgayGui,
            x.ChiTiet.Select(c => new DongPhieuChamPdf(c.TenTieuChi, c.DiemToiDa, c.Diem, c.NhanXet))
                .ToList())).ToList();

        var tep = BoXuatPhieuChamPdf.Xuat(tenCoQuan, tenHeThong, phieu);

        return File(tep, "application/pdf", tenTep);
    }

    private static List<DongNhapNguoiDung> DocTepNhapNguoiDung(IFormFile tep)
    {
        using var luong = tep.OpenReadStream();

        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(luong);
        }
        catch (Exception ex)
        {
            throw new NghiepVuException(MaLoiHeThong.TepKhongHopLe,
                $"Không đọc được tệp Excel: {ex.Message}");
        }

        using (workbook)
        {
            var sheet = workbook.Worksheets.FirstOrDefault()
                        ?? throw new NghiepVuException(MaLoiHeThong.TepKhongHopLe,
                            "Tệp Excel không có sheet nào.");

            var cacDong = new List<DongNhapNguoiDung>();

            // Bo qua dong tieu de; dung LastRowUsed de khong quet het 1 trieu dong rong.
            var dongCuoi = sheet.LastRowUsed()?.RowNumber() ?? 1;

            for (var d = 2; d <= dongCuoi; d++)
            {
                var dong = sheet.Row(d);

                if (dong.IsEmpty())
                {
                    continue;
                }

                var muc = new DongNhapNguoiDung
                {
                    SoDong = d,
                    TenDangNhap = O(dong, 1),
                    HoTen = O(dong, 2),
                    Email = O(dong, 3),
                    DienThoai = O(dong, 4),
                    ChucVu = O(dong, 5),
                    MaDonVi = O(dong, 6),
                    MaVaiTro = O(dong, 7)
                };

                // Dong trong hoan toan (nguoi dung de dong trong giua bang) thi bo qua.
                if (string.IsNullOrWhiteSpace(muc.TenDangNhap)
                    && string.IsNullOrWhiteSpace(muc.HoTen))
                {
                    continue;
                }

                cacDong.Add(muc);
            }

            return cacDong;
        }

        static string? O(IXLRow dong, int cot)
        {
            var giaTri = dong.Cell(cot).GetString().Trim();
            return string.IsNullOrEmpty(giaTri) ? null : giaTri;
        }
    }

    private static string TaoTenTep(string ten)
        => Shared.TiengViet.VanBanTiengViet.TaoSlug(ten);
}
