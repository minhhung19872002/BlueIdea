using BlueIdea.Api.Chung;
using BlueIdea.Application.SangKien;
using BlueIdea.Application.TrungLap;
using BlueIdea.Application.XuLy;
using BlueIdea.Domain.Chung;
using BlueIdea.Reporting;
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

    public SangKienController(
        IMediator mediator, DichVuTruyVanSangKien truyVan, DichVuKiemTraTrungLap trungLap)
    {
        _mediator = mediator;
        _truyVan = truyVan;
        _trungLap = trungLap;
    }

    /// <summary>Chức năng 28 — Danh sách hồ sơ với bộ lọc đa tiêu chí.</summary>
    [HttpGet]
    public async Task<IActionResult> LayDanhSachAsync(
        [FromQuery] ThamSoLocSangKien thamSo, CancellationToken ct)
        => Ok(PhanHoiPhanTrang<SangKienTomTatDto>.Tu(await _truyVan.LayDanhSachAsync(thamSo, ct)));

    /// <summary>Chức năng 23 — Hồ sơ của tôi.</summary>
    [HttpGet("cua-toi")]
    public async Task<IActionResult> LayHoSoCuaToiAsync(
        [FromQuery] ThamSoLocSangKien thamSo, CancellationToken ct)
    {
        thamSo.ChiCuaToi = true;
        return Ok(PhanHoiPhanTrang<SangKienTomTatDto>.Tu(await _truyVan.LayDanhSachAsync(thamSo, ct)));
    }

    /// <summary>Chi tiết hồ sơ kèm checklist thành phần và tệp đính kèm.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> LayChiTietAsync(Guid id, CancellationToken ct)
        => Ok(PhanHoiApi<SangKienChiTietDto>.Ok(await _truyVan.LayChiTietAsync(id, ct)));

    /// <summary>Chức năng 30 — Timeline tiến độ xử lý.</summary>
    [HttpGet("{id:guid}/tien-do")]
    public async Task<IActionResult> LayTienDoAsync(Guid id, CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<MocTienDoDto>>.Ok(await _truyVan.LayTienDoAsync(id, ct)));

    /// <summary>Chức năng 23 — Lịch sử chỉnh sửa (diff giá trị trước/sau).</summary>
    [HttpGet("{id:guid}/lich-su")]
    public async Task<IActionResult> LayLichSuAsync(Guid id, CancellationToken ct)
    {
        var lichSu = await _truyVan.LayLichSuAsync(id, ct);
        return Ok(PhanHoiApi<IReadOnlyList<Domain.SangKien.SangKienLichSu>>.Ok(lichSu));
    }

    /// <summary>Chức năng 29 — Danh sách hành động khả dụng (frontend render nút động).</summary>
    [HttpGet("{id:guid}/hanh-dong")]
    public async Task<IActionResult> LayHanhDongAsync(Guid id, CancellationToken ct)
    {
        var hanhDong = await _mediator.Send(new LayHanhDongKhaDungQuery(id), ct);
        return Ok(PhanHoiApi<IReadOnlyList<HanhDongKhaDung>>.Ok(hanhDong));
    }

    /// <summary>Chức năng 26 — Kết quả kiểm tra trùng lặp gần nhất.</summary>
    [HttpGet("{id:guid}/trung-lap")]
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
    public async Task<IActionResult> TaoAsync(
        [FromBody] NoiDungHoSoDto noiDung, CancellationToken ct)
    {
        var id = await _mediator.Send(new TaoHoSoCommand(noiDung), ct);
        return Ok(PhanHoiApi<Guid>.Ok(id, "Đã lưu hồ sơ nháp"));
    }

    /// <summary>Cập nhật hồ sơ (chỉ khi ở trạng thái Nháp hoặc Yêu cầu bổ sung).</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> CapNhatAsync(
        Guid id, [FromBody] NoiDungHoSoDto noiDung,
        [FromQuery] int? phienBan, CancellationToken ct)
    {
        await _mediator.Send(new CapNhatHoSoCommand(id, noiDung, phienBan), ct);
        return Ok(PhanHoiApi.Ok("Đã lưu thay đổi"));
    }

    /// <summary>Chức năng 22 — Nộp hồ sơ chính thức, khởi tạo quy trình xử lý.</summary>
    [HttpPost("{id:guid}/nop")]
    public async Task<IActionResult> NopAsync(Guid id, CancellationToken ct)
    {
        var ketQua = await _mediator.Send(new NopHoSoCommand(id), ct);
        return Ok(PhanHoiApi<KetQuaNopHoSo>.Ok(ketQua, "Nộp hồ sơ thành công"));
    }

    /// <summary>Chức năng 23 — Rút hồ sơ (chỉ khi chưa vào bước chấm điểm).</summary>
    [HttpPost("{id:guid}/rut")]
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
