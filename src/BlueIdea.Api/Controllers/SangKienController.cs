using BlueIdea.Api.Chung;
using BlueIdea.Application.BaoCao;
using BlueIdea.Application.Chung;
using BlueIdea.Application.SangKien;
using BlueIdea.Application.TraCuu;
using BlueIdea.Application.TrungLap;
using BlueIdea.Application.XuLy;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.DanhMuc;
using BlueIdea.Reporting;
using BlueIdea.Shared.KetQua;
using BlueIdea.Workflow.MoHinh;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueIdea.Api.Controllers;

/// <summary>Chức năng 22–32 — Hồ sơ sáng kiến: nộp, sửa, rút, theo dõi, xử lý.</summary>
[ApiController]
[Route("api/v1/sang-kien")]
[Authorize]
[Produces("application/json")]
public sealed class SangKienController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly DichVuTruyVanSangKien _truyVan;
    private readonly DichVuKiemTraTrungLap _trungLap;
    private readonly DichVuTimNguNghia _timNguNghia;
    private readonly DichVuSinhBieuMau _sinhBieuMau;

    public SangKienController(
        IMediator mediator, DichVuTruyVanSangKien truyVan, DichVuKiemTraTrungLap trungLap,
        DichVuTimNguNghia timNguNghia, DichVuSinhBieuMau sinhBieuMau)
    {
        _mediator = mediator;
        _truyVan = truyVan;
        _trungLap = trungLap;
        _timNguNghia = timNguNghia;
        _sinhBieuMau = sinhBieuMau;
    }

    /// <summary>Chức năng 28 — Danh sách hồ sơ với bộ lọc đa tiêu chí.</summary>
    [HttpGet]
    [Authorize(Policy = MaQuyen.SangKienXem)]
    public async Task<IActionResult> LayDanhSachAsync(
        [FromQuery] ThamSoLocSangKien thamSo, CancellationToken ct)
        => Ok(PhanHoiPhanTrang<SangKienTomTatDto>.Tu(await _truyVan.LayDanhSachAsync(thamSo, ct)));

    /// <summary>Chức năng 37 — Gợi ý từ khoá khi gõ ở ô tìm kiếm.</summary>
    [HttpGet("goi-y")]
    [Authorize(Policy = MaQuyen.SangKienXem)]
    public async Task<IActionResult> GoiYAsync(
        [FromQuery] string tuKhoa, [FromQuery] int soLuong = 8, CancellationToken ct = default)
        => Ok(PhanHoiApi<IReadOnlyList<GoiYTimKiem>>.Ok(
            await _truyVan.GoiYAsync(tuKhoa, soLuong, ct)));

    /// <summary>
    /// Chức năng 26, 37 — Tìm kiếm ngữ nghĩa.
    ///
    /// Khác tìm theo từ khoá: câu hỏi "giải pháp tiết kiệm điện ở trường học" vẫn tìm ra sáng kiến
    /// đặt tên "Ứng dụng cảm biến ánh sáng giảm tiêu thụ năng lượng lớp học" dù không trùng từ nào.
    /// Vector nhúng sinh hoàn toàn nội bộ, không gọi API AI bên thứ ba.
    /// </summary>
    [HttpGet("tim-ngu-nghia")]
    [Authorize(Policy = MaQuyen.SangKienXem)]
    public async Task<IActionResult> TimNguNghiaAsync(
        [FromQuery] string cauHoi,
        [FromQuery] int soKetQua = 20,
        [FromQuery] Guid? linhVucId = null,
        [FromQuery] int? nam = null,
        CancellationToken ct = default)
        => Ok(PhanHoiApi<IReadOnlyList<KetQuaTimNguNghia>>.Ok(
            await _timNguNghia.TimAsync(cauHoi, soKetQua, linhVucId, nam, ct)));

    /// <summary>Chức năng 23 — Hồ sơ của tôi.</summary>
    [HttpGet("cua-toi")]
    [Authorize(Policy = MaQuyen.SangKienXem)]
    public async Task<IActionResult> LayHoSoCuaToiAsync(
        [FromQuery] ThamSoLocSangKien thamSo, CancellationToken ct)
    {
        thamSo.ChiCuaToi = true;
        return Ok(PhanHoiPhanTrang<SangKienTomTatDto>.Tu(await _truyVan.LayDanhSachAsync(thamSo, ct)));
    }

    /// <summary>Chi tiết hồ sơ kèm checklist thành phần và tệp đính kèm.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = MaQuyen.SangKienXem)]
    public async Task<IActionResult> LayChiTietAsync(Guid id, CancellationToken ct)
        => Ok(PhanHoiApi<SangKienChiTietDto>.Ok(await _truyVan.LayChiTietAsync(id, ct)));

    /// <summary>Chức năng 30 — Timeline tiến độ xử lý.</summary>
    [HttpGet("{id:guid}/tien-do")]
    [Authorize(Policy = MaQuyen.SangKienXem)]
    public async Task<IActionResult> LayTienDoAsync(Guid id, CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<MocTienDoDto>>.Ok(await _truyVan.LayTienDoAsync(id, ct)));

    /// <summary>Chức năng 23 — Lịch sử chỉnh sửa (diff giá trị trước/sau).</summary>
    [HttpGet("{id:guid}/lich-su")]
    [Authorize(Policy = MaQuyen.SangKienXem)]
    public async Task<IActionResult> LayLichSuAsync(Guid id, CancellationToken ct)
    {
        var lichSu = await _truyVan.LayLichSuAsync(id, ct);
        return Ok(PhanHoiApi<IReadOnlyList<Domain.SangKien.SangKienLichSu>>.Ok(lichSu));
    }

    /// <summary>Chức năng 29 — Danh sách hành động khả dụng (frontend render nút động).</summary>
    [HttpGet("{id:guid}/hanh-dong")]
    [Authorize(Policy = MaQuyen.SangKienXem)]
    public async Task<IActionResult> LayHanhDongAsync(Guid id, CancellationToken ct)
    {
        var hanhDong = await _mediator.Send(new LayHanhDongKhaDungQuery(id), ct);
        return Ok(PhanHoiApi<IReadOnlyList<HanhDongKhaDung>>.Ok(hanhDong));
    }

    /// <summary>Chức năng 26 — Kết quả kiểm tra trùng lặp gần nhất.</summary>
    [HttpGet("{id:guid}/trung-lap")]
    [Authorize(Policy = MaQuyen.TrungLapXem)]
    public async Task<IActionResult> LayTrungLapAsync(Guid id, CancellationToken ct)
    {
        var ketQua = await _trungLap.LayKetQuaGanNhatAsync(id, ct);
        return Ok(PhanHoiApi<Domain.Ai.KiemTraTrungLap?>.Ok(ketQua));
    }

    /// <summary>Chạy lại kiểm tra trùng lặp thủ công.</summary>
    [HttpPost("{id:guid}/trung-lap/chay-lai")]
    [Authorize(Policy = MaQuyen.TrungLapChayLai)]
    public async Task<IActionResult> ChayLaiTrungLapAsync(Guid id, CancellationToken ct)
    {
        var ketQua = await _trungLap.ChayAsync(id, batBuocChayLai: true, ct);
        return Ok(PhanHoiApi<object?>.Ok(ketQua, "Đã hoàn tất kiểm tra trùng lặp"));
    }

    /// <summary>Chức năng 22 — Tạo hồ sơ nháp.</summary>
    [HttpPost]
    [Authorize(Policy = MaQuyen.SangKienThem)]
    public async Task<IActionResult> TaoAsync(
        [FromBody] NoiDungHoSoDto noiDung, CancellationToken ct)
    {
        var id = await _mediator.Send(new TaoHoSoCommand(noiDung), ct);
        return Ok(PhanHoiApi<Guid>.Ok(id, "Đã lưu hồ sơ nháp"));
    }

    /// <summary>Cập nhật hồ sơ (chỉ khi ở trạng thái Nháp hoặc Yêu cầu bổ sung).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = MaQuyen.SangKienSua)]
    public async Task<IActionResult> CapNhatAsync(
        Guid id, [FromBody] NoiDungHoSoDto noiDung,
        [FromQuery] int? phienBan, CancellationToken ct)
    {
        await _mediator.Send(new CapNhatHoSoCommand(id, noiDung, phienBan), ct);
        return Ok(PhanHoiApi.Ok("Đã lưu thay đổi"));
    }

    /// <summary>Chức năng 22 — Nộp hồ sơ chính thức, khởi tạo quy trình xử lý.</summary>
    [HttpPost("{id:guid}/nop")]
    [Authorize(Policy = MaQuyen.SangKienNop)]
    public async Task<IActionResult> NopAsync(Guid id, CancellationToken ct)
    {
        var ketQua = await _mediator.Send(new NopHoSoCommand(id), ct);
        return Ok(PhanHoiApi<KetQuaNopHoSo>.Ok(ketQua, "Nộp hồ sơ thành công"));
    }

    /// <summary>
    /// Chức năng 22 — Phiếu tiếp nhận hồ sơ (PDF) để tác giả in làm bằng chứng đã nộp.
    ///
    /// Dùng bố cục do quản trị viên cấu hình ở biểu mẫu loại PHIEU_TIEP_NHAN; chưa cấu hình biểu
    /// mẫu nào thì in bố cục mặc định — thiếu cấu hình không được làm mất luôn chức năng in.
    /// </summary>
    [HttpGet("{id:guid}/phieu-tiep-nhan")]
    [Authorize(Policy = MaQuyen.SangKienXem)]
    public async Task<IActionResult> PhieuTiepNhanAsync(Guid id, CancellationToken ct)
    {
        var hoSo = await _truyVan.LayChiTietAsync(id, ct);

        if (hoSo.TrangThaiTong == TrangThaiTongHoSo.Nhap)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Hồ sơ chưa nộp nên chưa có phiếu tiếp nhận.");
        }

        var mau = await _sinhBieuMau.TimMauHoatDongAsync(LoaiBieuMau.PhieuTiepNhan, ct);

        var thongTin = mau is null
            ? new List<DongThongTin>
            {
                new("Mã hồ sơ", hoSo.MaHoSo),
                new("Tên sáng kiến", hoSo.TenSangKien),
                new("Tác giả chính", hoSo.DanhSachTacGia.FirstOrDefault(x => x.LaTacGiaChinh)?.HoTen),
                new("Đơn vị", hoSo.TenDonVi),
                new("Lĩnh vực", hoSo.TenLinhVuc),
                new("Đợt đề nghị", hoSo.TenDot),
                new("Ngày nộp", hoSo.NgayNop?.ToLocalTime().ToString("HH:mm 'ngày' dd/MM/yyyy")),
                new("Hạn xử lý", hoSo.HanXuLyHienTai?.ToLocalTime().ToString("dd/MM/yyyy")),
                new("Trạng thái", hoSo.TenTrangThaiHienTai),
            }
            : (await _sinhBieuMau.SinhChoHoSoAsync(mau.Id, id, ct)).DongDuLieu
                .Select(x => new DongThongTin(x.Nhan, x.GiaTri))
                .ToList();

        var pdf = BoXuatPdf.XuatTaiLieu(
            tenCoQuanChuQuan: hoSo.TenDonVi ?? string.Empty,
            tenDonVi: string.Empty,
            tieuDe: "PHIẾU TIẾP NHẬN HỒ SƠ SÁNG KIẾN",
            phuDe: hoSo.MaHoSo,
            thongTin: thongTin,
            noiDungThem: "Phiếu này xác nhận hệ thống đã tiếp nhận hồ sơ nêu trên. "
                + "Tác giả theo dõi tiến độ xử lý trên hệ thống bằng mã hồ sơ.");

        return File(pdf, "application/pdf", $"phieu-tiep-nhan-{hoSo.MaHoSo}.pdf");
    }

    /// <summary>Chức năng 23 — Rút hồ sơ (chỉ khi chưa vào bước chấm điểm).</summary>
    [HttpPost("{id:guid}/rut")]
    [Authorize(Policy = MaQuyen.SangKienRut)]
    public async Task<IActionResult> RutAsync(
        Guid id, [FromBody] RutHoSoDto duLieu, CancellationToken ct)
    {
        await _mediator.Send(new RutHoSoCommand(id, duLieu.LyDo), ct);
        return Ok(PhanHoiApi.Ok("Đã rút hồ sơ"));
    }

    /// <summary>Xuất danh sách hồ sơ ra Excel theo bộ lọc hiện tại.</summary>
    [HttpGet("xuat-excel")]
    [Authorize(Policy = MaQuyen.SangKienXuat)]
    public async Task<IActionResult> XuatExcelAsync(
        [FromQuery] ThamSoLocSangKien thamSo, CancellationToken ct)
    {
        thamSo.SoDong = ThamSoPhanTrangApi.SoDongXuatToiDa;
        var duLieu = await _truyVan.LayDanhSachAsync(thamSo, ct);

        var tep = BoXuatExcel.Xuat("Danh sach sang kien", "DANH SÁCH HỒ SƠ SÁNG KIẾN", duLieu.DuLieu,
            new List<CotXuat<SangKienTomTatDto>>
            {
                new("Mã hồ sơ", x => x.MaHoSo, 18),
                new("Tên sáng kiến", x => x.TenSangKien, 50),
                new("Tác giả chính", x => x.TacGiaChinh, 25),
                new("Đơn vị", x => x.TenDonVi, 30),
                new("Lĩnh vực", x => x.TenLinhVuc, 25),
                new("Đợt", x => x.TenDot, 30),
                new("Trạng thái", x => x.TrangThaiTong, 18),
                new("Bước hiện tại", x => x.TenBuocHienTai, 25),
                new("Tổng điểm", x => x.TongDiem, 12),
                new("Trùng lặp (%)", x => x.TyLeTrungLap, 14),
                new("Kết quả", x => x.KetQua, 14),
                new("Ngày nộp", x => x.NgayNop, 18)
            });

        return File(tep, ThamSoPhanTrangApi.MimeExcel, "danh-sach-sang-kien.xlsx");
    }
}

public sealed record RutHoSoDto(string LyDo);

/// <summary>Chức năng 27–29 — Tiếp nhận và xử lý hồ sơ theo quy trình động.</summary>
[ApiController]
[Route("api/v1/xu-ly")]
[Authorize]
[Produces("application/json")]
public sealed class XuLyController : ControllerBase
{
    private readonly IMediator _mediator;

    public XuLyController(IMediator mediator) => _mediator = mediator;

    /// <summary>Thực thi một bước xử lý trên hồ sơ.</summary>
    [HttpPost("thuc-thi")]
    [Authorize(Policy = MaQuyen.XuLyThucThi)]
    public async Task<IActionResult> ThucThiAsync(
        [FromBody] ThucThiBuocDto duLieu,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        var ketQua = await _mediator.Send(new ThucThiBuocCommand(
            duLieu.SangKienId,
            duLieu.TruongHopId,
            duLieu.YKien,
            duLieu.TepDinhKemIds,
            duLieu.NguoiUyQuyenId,
            duLieu.PhienBanHoSo,
            idempotencyKey), ct);

        return Ok(PhanHoiApi<KetQuaXuLy>.Ok(ketQua, ketQua.ThongBao));
    }

    /// <summary>Xử lý hàng loạt nhiều hồ sơ cùng bước.</summary>
    [HttpPost("thuc-thi-hang-loat")]
    [Authorize(Policy = MaQuyen.XuLyThucThi)]
    public async Task<IActionResult> ThucThiHangLoatAsync(
        [FromBody] ThucThiHangLoatDto duLieu, CancellationToken ct)
    {
        var ketQua = await _mediator.Send(
            new ThucThiHangLoatCommand(duLieu.SangKienIds, duLieu.TruongHopId, duLieu.YKien), ct);

        return Ok(PhanHoiApi<KetQuaXuLyHangLoat>.Ok(ketQua,
            $"Đã xử lý {ketQua.ThanhCong}/{ketQua.TongSo} hồ sơ"));
    }

    /// <summary>Thu hồi bước đã xử lý (nếu bước cho phép).</summary>
    [HttpPost("thu-hoi")]
    [Authorize(Policy = MaQuyen.XuLyThuHoi)]
    public async Task<IActionResult> ThuHoiAsync(
        [FromBody] ThuHoiDto duLieu, CancellationToken ct)
    {
        await _mediator.Send(new ThuHoiBuocCommand(duLieu.SangKienId, duLieu.LyDo), ct);
        return Ok(PhanHoiApi.Ok("Đã thu hồi bước xử lý"));
    }
}

public sealed class ThucThiBuocDto
{
    public Guid SangKienId { get; set; }

    public Guid TruongHopId { get; set; }

    public string? YKien { get; set; }

    public List<Guid> TepDinhKemIds { get; set; } = new();

    public Guid? NguoiUyQuyenId { get; set; }

    public int? PhienBanHoSo { get; set; }
}

public sealed record ThucThiHangLoatDto(List<Guid> SangKienIds, Guid TruongHopId, string? YKien);

public sealed record ThuHoiDto(Guid SangKienId, string LyDo);
