using BlueIdea.Api.Chung;
using BlueIdea.Application.Chung;
using BlueIdea.Application.DanhGia;
using BlueIdea.Application.KySo;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Domain.SangKien;
using BlueIdea.Reporting;
using BlueIdea.Domain.Chung;
using BlueIdea.Shared.KetQua;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueIdea.Api.Controllers;

public sealed class PhanCongDto
{
    public Guid HoiDongId { get; set; }

    public List<Guid> SangKienIds { get; set; } = new();

    public List<Guid>? ThanhVienIds { get; set; }

    public DateTimeOffset? HanHoanThanh { get; set; }

    public bool TuDongChiaDeu { get; set; } = true;
}

/// <summary>Chức năng 33–35 — Phân công chấm, chấm điểm, tổng hợp điểm hội đồng.</summary>
[ApiController]
[Route("api/v1/danh-gia")]
[Authorize]
[Produces("application/json")]
public sealed class DanhGiaController : ControllerBase
{
    private readonly DichVuDanhGia _dichVu;
    private readonly DichVuXuatPhieuCham _xuatPhieu;
    private readonly DichVuKySo _kySo;
    private readonly IAppDbContext _db;
    private readonly ILuuTruTep _luuTru;
    private readonly IDongHoHeThong _dongHo;
    private readonly INguoiDungHienTai _nguoiDung;
    private readonly IDichVuCauHinh _cauHinh;

    public DanhGiaController(
        DichVuDanhGia dichVu, DichVuXuatPhieuCham xuatPhieu, DichVuKySo kySo, IAppDbContext db,
        ILuuTruTep luuTru, IDongHoHeThong dongHo, INguoiDungHienTai nguoiDung,
        IDichVuCauHinh cauHinh)
    {
        _dichVu = dichVu;
        _xuatPhieu = xuatPhieu;
        _kySo = kySo;
        _db = db;
        _luuTru = luuTru;
        _dongHo = dongHo;
        _nguoiDung = nguoiDung;
        _cauHinh = cauHinh;
    }

    /// <summary>
    /// Chức năng 35 — Ký số một phiếu chấm đã gửi.
    ///
    /// Ký trên bản PDF chốt tại thời điểm ký (không ký trên dữ liệu bảng): chữ ký phải gắn với
    /// đúng nội dung người ký nhìn thấy, còn dữ liệu bảng vẫn có thể đổi khi mở lại phiếu.
    /// </summary>
    [HttpPost("phieu/{id:guid}/ky-so")]
    [Authorize(Policy = MaQuyen.DanhGiaXem)]
    public async Task<IActionResult> KySoPhieuAsync(Guid id, CancellationToken ct)
    {
        var duLieu = await _xuatPhieu.TheoPhieuAsync(id, ct);
        var phieu = duLieu[0];

        var tenCoQuan = await _cauHinh.LayAsync(KhoaCauHinh.TenDonVi, string.Empty, ct);
        var tenHeThong = await _cauHinh.LayAsync(KhoaCauHinh.TenHeThong, string.Empty, ct);

        var noiDung = BoXuatPhieuChamPdf.Xuat(tenCoQuan, tenHeThong, new[]
        {
            new PhieuChamPdf(
                phieu.SoPhieu, phieu.MaHoSo, phieu.TenSangKien, phieu.TenTacGiaChinh,
                phieu.TenDonVi, phieu.TenThanhVien, phieu.ChucDanh, phieu.TenHoiDong,
                phieu.TongDiem, phieu.KetLuan, phieu.TenMucCongNhanDeXuat, phieu.NhanXetChung,
                phieu.UuDiem, phieu.HanChe, phieu.NgayGui,
                phieu.ChiTiet
                    .Select(c => new DongPhieuChamPdf(c.TenTieuChi, c.DiemToiDa, c.Diem, c.NhanXet))
                    .ToList())
        });

        var tenLuuTru = $"{Guid.NewGuid():N}.pdf";

        await using (var luong = new MemoryStream(noiDung))
        {
            var duongDan = await _luuTru
                .TaiLenAsync(luong, tenLuuTru, "application/pdf", "blueidea", ct);

            _db.TepTin.Add(new TepTin
            {
                Id = Guid.NewGuid(),
                TenGoc = $"phieu-cham-{phieu.SoPhieu.Replace('/', '-')}.pdf",
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

        var tep = _db.TepTin.Local.First(x => x.TenLuuTru == tenLuuTru);
        await _db.SaveChangesAsync(ct);

        var tepDaKyId = await _kySo.KyTepAsync(tep.Id, "PHIEU_DANH_GIA", id, ct);

        return Ok(PhanHoiApi<Guid>.Ok(tepDaKyId, "Đã ký số phiếu chấm"));
    }

    /// <summary>Lịch sử ký số của một phiếu chấm.</summary>
    [HttpGet("phieu/{id:guid}/lich-su-ky-so")]
    [Authorize(Policy = MaQuyen.DanhGiaXem)]
    public async Task<IActionResult> LichSuKySoPhieuAsync(Guid id, CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<NhatKyKySo>>.Ok(
            await _kySo.LichSuAsync("PHIEU_DANH_GIA", id, ct)));

    /// <summary>Chức năng 33 — Danh sách hồ sơ được phân công cho tôi ("Việc của tôi").</summary>
    [HttpGet("viec-cua-toi")]
    [Authorize(Policy = MaQuyen.DanhGiaChamDiem)]
    public async Task<IActionResult> LayViecCuaToiAsync(
        [FromQuery] ThamSoPhanTrang thamSo, [FromQuery] string? trangThai, CancellationToken ct)
        => Ok(PhanHoiPhanTrang<PhanCongChamDto>.Tu(
            await _dichVu.LayViecCuaToiAsync(thamSo, trangThai, ct)));

    /// <summary>Phân công thành viên hội đồng chấm hồ sơ (loại trừ xung đột lợi ích).</summary>
    [HttpPost("phan-cong")]
    [Authorize(Policy = MaQuyen.DanhGiaPhanCong)]
    public async Task<IActionResult> PhanCongAsync(
        [FromBody] PhanCongDto duLieu, CancellationToken ct)
    {
        var ketQua = await _dichVu.PhanCongAsync(
            duLieu.HoiDongId, duLieu.SangKienIds, duLieu.ThanhVienIds,
            duLieu.HanHoanThanh, duLieu.TuDongChiaDeu, ct);

        return Ok(PhanHoiApi<DichVuDanhGia.KetQuaPhanCong>.Ok(
            ketQua, $"Đã tạo {ketQua.SoLuotPhanCong} lượt phân công"));
    }

    /// <summary>Chức năng 34 — Lấy phiếu chấm (kèm bộ tiêu chí render động).</summary>
    [HttpGet("phieu")]
    [Authorize(Policy = MaQuyen.DanhGiaChamDiem)]
    public async Task<IActionResult> LayPhieuAsync(
        [FromQuery] Guid sangKienId, [FromQuery] Guid hoiDongId, CancellationToken ct)
        => Ok(PhanHoiApi<PhieuDanhGiaDto>.Ok(
            await _dichVu.LayPhieuChamAsync(sangKienId, hoiDongId, ct)));

    /// <summary>Lưu nháp phiếu chấm.</summary>
    [HttpPost("phieu/luu-nhap")]
    [Authorize(Policy = MaQuyen.DanhGiaChamDiem)]
    public async Task<IActionResult> LuuNhapAsync(
        [FromBody] PhieuChamDto duLieu, CancellationToken ct)
        => Ok(PhanHoiApi<PhieuDanhGiaDto>.Ok(
            await _dichVu.LuuPhieuAsync(duLieu, guiChinhThuc: false, ct), "Đã lưu nháp"));

    /// <summary>Gửi phiếu chấm chính thức (sau khi gửi chỉ thư ký mới mở lại được).</summary>
    [HttpPost("phieu/gui")]
    [Authorize(Policy = MaQuyen.DanhGiaChamDiem)]
    public async Task<IActionResult> GuiPhieuAsync(
        [FromBody] PhieuChamDto duLieu, CancellationToken ct)
        => Ok(PhanHoiApi<PhieuDanhGiaDto>.Ok(
            await _dichVu.LuuPhieuAsync(duLieu, guiChinhThuc: true, ct), "Đã gửi phiếu đánh giá"));

    /// <summary>Thư ký mở lại phiếu đã gửi để thành viên sửa.</summary>
    [HttpPost("phieu/{id:guid}/mo-lai")]
    [Authorize(Policy = MaQuyen.DanhGiaMoLaiPhieu)]
    public async Task<IActionResult> MoLaiPhieuAsync(Guid id, CancellationToken ct)
    {
        await _dichVu.MoLaiPhieuAsync(id, ct);
        return Ok(PhanHoiApi.Ok("Đã mở lại phiếu"));
    }

    /// <summary>Chức năng 32 — Tổng hợp điểm của hội đồng cho một hồ sơ.</summary>
    [HttpPost("tong-hop")]
    [Authorize(Policy = MaQuyen.DanhGiaTongHop)]
    public async Task<IActionResult> TongHopAsync(
        [FromQuery] Guid sangKienId, [FromQuery] Guid hoiDongId,
        [FromQuery] Guid? phienHopId, CancellationToken ct)
        => Ok(PhanHoiApi<KetQuaTongHopDto>.Ok(
            await _dichVu.TongHopDiemAsync(sangKienId, hoiDongId, phienHopId, ct),
            "Đã tổng hợp điểm"));

    /// <summary>Chức năng 35 — Bảng ma trận điểm (hàng = hồ sơ, cột = thành viên).</summary>
    [HttpGet("ma-tran-diem")]
    [Authorize(Policy = MaQuyen.DanhGiaTongHop)]
    public async Task<IActionResult> LayMaTranDiemAsync(
        [FromQuery] Guid hoiDongId, [FromQuery] Guid? dotDeNghiId, CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<DongMaTranDiem>>.Ok(
            await _dichVu.LayMaTranDiemAsync(hoiDongId, dotDeNghiId, ct)));
}
