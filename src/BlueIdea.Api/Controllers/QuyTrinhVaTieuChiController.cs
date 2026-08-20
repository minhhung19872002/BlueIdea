using BlueIdea.Api.Chung;
using BlueIdea.Application.CauHinhQuyTrinh;
using BlueIdea.Application.DanhGia;
using BlueIdea.Application.DanhMuc;
using BlueIdea.Application.TieuChi;
using BlueIdea.Domain.Chung;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EntityQuyTrinh = BlueIdea.Domain.QuyTrinh.QuyTrinh;

namespace BlueIdea.Api.Controllers;

public sealed class LuuQuyTrinhDto : LuuDanhMucDto
{
    public string Cap { get; set; } = CapXetDuyet.CoSo;

    public bool LaMacDinh { get; set; }
}

public sealed record SaoChepQuyTrinhDto(string Ma, string Ten);

/// <summary>Chức năng 9–16 — Cấu hình quy trình động và trình thiết kế.</summary>
[ApiController]
[Route("api/v1/quy-trinh")]
[Authorize]
[Produces("application/json")]
public sealed class QuyTrinhController : ControllerBase
{
    private readonly DichVuQuyTrinh _dichVu;

    public QuyTrinhController(DichVuQuyTrinh dichVu) => _dichVu = dichVu;

    [HttpGet]
    [Authorize(Policy = MaQuyen.QuyTrinhXem)]
    public async Task<IActionResult> LayDanhSachAsync(
        [FromQuery] ThamSoLocDanhMuc thamSo, CancellationToken ct)
        => Ok(PhanHoiPhanTrang<DanhMucDto>.Tu(await _dichVu.LayDanhSachAsync(thamSo, ct)));

    [HttpGet("chon")]
    public async Task<IActionResult> LayDanhSachChonAsync(CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<DanhMucDto>>.Ok(await _dichVu.LayDanhSachChonAsync(ct)));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = MaQuyen.QuyTrinhXem)]
    public async Task<IActionResult> LayTheoIdAsync(Guid id, CancellationToken ct)
        => Ok(PhanHoiApi<EntityQuyTrinh>.Ok(await _dichVu.LayTheoIdAsync(id, ct)));

    /// <summary>Sơ đồ đầy đủ để render trên ReactFlow.</summary>
    [HttpGet("{id:guid}/so-do")]
    [Authorize(Policy = MaQuyen.QuyTrinhXem)]
    public async Task<IActionResult> LaySoDoAsync(Guid id, CancellationToken ct)
        => Ok(PhanHoiApi<SoDoQuyTrinhDto>.Ok(await _dichVu.LaySoDoAsync(id, ct)));

    /// <summary>Lưu toàn bộ sơ đồ (bước + trường hợp + tác nhân + trạng thái) trong một giao dịch.</summary>
    [HttpPut("{id:guid}/so-do")]
    [Authorize(Policy = MaQuyen.QuyTrinhCauHinh)]
    public async Task<IActionResult> LuuSoDoAsync(
        Guid id, [FromBody] SoDoQuyTrinhDto soDo, CancellationToken ct)
    {
        await _dichVu.LuuSoDoAsync(id, soDo, ct);
        return Ok(PhanHoiApi.Ok("Đã lưu sơ đồ quy trình"));
    }

    /// <summary>Kiểm tra tính hợp lệ theo 7 rule bắt buộc.</summary>
    /// <summary>
    /// Chức năng 12 — Thành phần hồ sơ của quy trình (CRUD riêng, không đi qua sơ đồ).
    ///
    /// Thành phần hồ sơ không phải một nút trên sơ đồ nên không có lý do bắt sửa nó bằng cách gửi
    /// lại cả sơ đồ: hai người cùng mở một quy trình sẽ ghi đè lên nhau.
    /// </summary>
    [HttpGet("{id:guid}/thanh-phan-ho-so")]
    [Authorize(Policy = MaQuyen.QuyTrinhXem)]
    public async Task<IActionResult> LayThanhPhanAsync(Guid id, CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<ThanhPhanHoSoCauHinhDto>>.Ok(
            await _dichVu.LayThanhPhanAsync(id, ct)));

    [HttpPost("{id:guid}/thanh-phan-ho-so")]
    [Authorize(Policy = MaQuyen.QuyTrinhCauHinh)]
    public async Task<IActionResult> ThemThanhPhanAsync(
        Guid id, [FromBody] ThanhPhanHoSoCauHinhDto duLieu, CancellationToken ct)
        => Ok(PhanHoiApi<Guid>.Ok(
            await _dichVu.ThemThanhPhanAsync(id, duLieu, ct), "Đã thêm thành phần hồ sơ"));

    [HttpPut("{id:guid}/thanh-phan-ho-so/{thanhPhanId:guid}")]
    [Authorize(Policy = MaQuyen.QuyTrinhCauHinh)]
    public async Task<IActionResult> SuaThanhPhanAsync(
        Guid id, Guid thanhPhanId, [FromBody] ThanhPhanHoSoCauHinhDto duLieu, CancellationToken ct)
    {
        await _dichVu.SuaThanhPhanAsync(id, thanhPhanId, duLieu, ct);
        return Ok(PhanHoiApi.Ok("Đã cập nhật thành phần hồ sơ"));
    }

    [HttpDelete("{id:guid}/thanh-phan-ho-so/{thanhPhanId:guid}")]
    [Authorize(Policy = MaQuyen.QuyTrinhCauHinh)]
    public async Task<IActionResult> XoaThanhPhanAsync(
        Guid id, Guid thanhPhanId, CancellationToken ct)
    {
        await _dichVu.XoaThanhPhanAsync(id, thanhPhanId, ct);
        return Ok(PhanHoiApi.Ok("Đã xoá thành phần hồ sơ"));
    }

    [HttpPut("{id:guid}/thanh-phan-ho-so/sap-xep")]
    [Authorize(Policy = MaQuyen.QuyTrinhCauHinh)]
    public async Task<IActionResult> SapXepThanhPhanAsync(
        Guid id, [FromBody] List<Guid> idTheoThuTu, CancellationToken ct)
    {
        await _dichVu.SapXepThanhPhanAsync(id, idTheoThuTu, ct);
        return Ok(PhanHoiApi.Ok("Đã lưu thứ tự"));
    }

    [HttpPost("{id:guid}/kiem-tra")]
    [Authorize(Policy = MaQuyen.QuyTrinhXem)]
    public async Task<IActionResult> KiemTraAsync(Guid id, CancellationToken ct)
        => Ok(PhanHoiApi<KetQuaKiemTraDto>.Ok(await _dichVu.KiemTraAsync(id, ct)));

    /// <summary>Kích hoạt quy trình (chỉ khi validator không còn lỗi).</summary>
    [HttpPost("{id:guid}/kich-hoat")]
    [Authorize(Policy = MaQuyen.QuyTrinhCauHinh)]
    public async Task<IActionResult> KichHoatAsync(Guid id, CancellationToken ct)
    {
        var ketQua = await _dichVu.KichHoatAsync(id, ct);
        return Ok(PhanHoiApi<KetQuaKiemTraDto>.Ok(ketQua, "Đã kích hoạt quy trình"));
    }

    [HttpPost("{id:guid}/ngung-ap-dung")]
    [Authorize(Policy = MaQuyen.QuyTrinhCauHinh)]
    public async Task<IActionResult> NgungApDungAsync(Guid id, CancellationToken ct)
    {
        await _dichVu.NgungApDungAsync(id, ct);
        return Ok(PhanHoiApi.Ok("Đã ngừng áp dụng quy trình"));
    }

    [HttpPost("{id:guid}/sao-chep")]
    [Authorize(Policy = MaQuyen.QuyTrinhCauHinh)]
    public async Task<IActionResult> SaoChepAsync(
        Guid id, [FromBody] SaoChepQuyTrinhDto duLieu, CancellationToken ct)
    {
        var idMoi = await _dichVu.SaoChepAsync(id, duLieu.Ma, duLieu.Ten, ct);
        return Ok(PhanHoiApi<Guid>.Ok(idMoi, "Đã sao chép quy trình"));
    }

    /// <summary>Tạo phiên bản mới — hồ sơ cũ vẫn chạy theo snapshot của phiên bản cũ.</summary>
    [HttpPost("{id:guid}/phien-ban-moi")]
    [Authorize(Policy = MaQuyen.QuyTrinhCauHinh)]
    public async Task<IActionResult> TaoPhienBanMoiAsync(Guid id, CancellationToken ct)
    {
        var idMoi = await _dichVu.TaoPhienBanMoiAsync(id, ct);
        return Ok(PhanHoiApi<Guid>.Ok(idMoi, "Đã tạo phiên bản mới"));
    }

    [HttpPost]
    [Authorize(Policy = MaQuyen.QuyTrinhCauHinh)]
    public async Task<IActionResult> ThemAsync([FromBody] LuuQuyTrinhDto duLieu, CancellationToken ct)
    {
        var banGhi = await _dichVu.ThemAsync(new EntityQuyTrinh
        {
            Ma = duLieu.Ma,
            Ten = duLieu.Ten,
            MoTa = duLieu.MoTa,
            ThuTu = duLieu.ThuTu,
            TrangThai = duLieu.TrangThai,
            Cap = duLieu.Cap,
            LaMacDinh = duLieu.LaMacDinh
        }, ct);

        return Ok(PhanHoiApi<EntityQuyTrinh>.Ok(banGhi, "Đã tạo quy trình"));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = MaQuyen.QuyTrinhCauHinh)]
    public async Task<IActionResult> SuaAsync(
        Guid id, [FromBody] LuuQuyTrinhDto duLieu, CancellationToken ct)
    {
        var banGhi = await _dichVu.CapNhatAsync(id, x =>
        {
            x.Ma = duLieu.Ma;
            x.Ten = duLieu.Ten;
            x.MoTa = duLieu.MoTa;
            x.ThuTu = duLieu.ThuTu;
            x.TrangThai = duLieu.TrangThai;
            x.Cap = duLieu.Cap;
            x.LaMacDinh = duLieu.LaMacDinh;
        }, ct);

        return Ok(PhanHoiApi<EntityQuyTrinh>.Ok(banGhi, "Cập nhật thành công"));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = MaQuyen.QuyTrinhCauHinh)]
    public async Task<IActionResult> XoaAsync(Guid id, CancellationToken ct)
    {
        await _dichVu.XoaAsync(id, ct);
        return Ok(PhanHoiApi.Ok("Đã xóa"));
    }
}

public sealed class LuuBoTieuChiDto : LuuDanhMucDto
{
    public int Nam { get; set; }

    public string Cap { get; set; } = CapXetDuyet.CoSo;

    public decimal ThangDiemToiDa { get; set; } = 100m;

    public decimal DiemDatToiThieu { get; set; } = 50m;

    public string CachTinh { get; set; } = CachTinhDiem.TongDiem;

    public int LamTron { get; set; } = 2;

    /// <summary>Tu dong tong hop diem khi nguoi cuoi cung trong danh sach phan cong gui phieu.</summary>
    public bool TuDongTongHop { get; set; } = true;

    /// <summary>Loai 1 diem cao nhat + 1 thap nhat khi so phieu &gt;= 5.</summary>
    public bool LoaiBoDiemCaoThap { get; set; }
}

public sealed record SaoChepBoTieuChiDto(string Ma, string Ten, int Nam);

/// <summary>Chức năng 17–18 — Cấu hình bộ tiêu chí động và mức công nhận.</summary>
[ApiController]
[Route("api/v1/tieu-chi")]
[Authorize]
[Produces("application/json")]
public sealed class TieuChiController : ControllerBase
{
    private readonly DichVuTieuChi _dichVu;

    public TieuChiController(DichVuTieuChi dichVu) => _dichVu = dichVu;

    [HttpGet]
    [Authorize(Policy = MaQuyen.TieuChiXem)]
    public async Task<IActionResult> LayDanhSachAsync(
        [FromQuery] ThamSoLocDanhMuc thamSo, CancellationToken ct)
        => Ok(PhanHoiPhanTrang<DanhMucDto>.Tu(await _dichVu.LayDanhSachAsync(thamSo, ct)));

    [HttpGet("chon")]
    public async Task<IActionResult> LayDanhSachChonAsync(CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<DanhMucDto>>.Ok(await _dichVu.LayDanhSachChonAsync(ct)));

    /// <summary>Bộ tiêu chí đầy đủ (cây 2 cấp) để render màn hình cấu hình và chấm điểm.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = MaQuyen.TieuChiXem)]
    public async Task<IActionResult> LayChiTietAsync(Guid id, CancellationToken ct)
        => Ok(PhanHoiApi<BoTieuChiDto>.Ok(await _dichVu.LayChiTietDayDuAsync(id, ct)));

    /// <summary>Kiểm tra tổng trọng số, tổng điểm và khoảng điểm mức công nhận.</summary>
    [HttpPost("{id:guid}/kiem-tra")]
    [Authorize(Policy = MaQuyen.TieuChiXem)]
    public async Task<IActionResult> KiemTraAsync(Guid id, CancellationToken ct)
        => Ok(PhanHoiApi<IReadOnlyList<string>>.Ok(await _dichVu.KiemTraAsync(id, ct)));

    /// <summary>Lưu toàn bộ cây nhóm/tiêu chí (kéo thả sắp xếp trên giao diện).</summary>
    [HttpPut("{id:guid}/cay")]
    [Authorize(Policy = MaQuyen.TieuChiCauHinh)]
    public async Task<IActionResult> LuuCayAsync(
        Guid id, [FromBody] List<NhomTieuChiLuuDto> nhom, CancellationToken ct)
    {
        await _dichVu.LuuCayTieuChiAsync(id, nhom, ct);
        return Ok(PhanHoiApi.Ok("Đã lưu bộ tiêu chí"));
    }

    /// <summary>Lưu danh sách mức công nhận theo khoảng điểm.</summary>
    [HttpPut("{id:guid}/muc-cong-nhan")]
    [Authorize(Policy = MaQuyen.TieuChiCauHinh)]
    public async Task<IActionResult> LuuMucCongNhanAsync(
        Guid id, [FromBody] List<MucCongNhanLuuDto> danhSach, CancellationToken ct)
    {
        await _dichVu.LuuMucCongNhanAsync(id, danhSach, ct);
        return Ok(PhanHoiApi.Ok("Đã lưu mức công nhận"));
    }

    [HttpPost("{id:guid}/sao-chep")]
    [Authorize(Policy = MaQuyen.TieuChiCauHinh)]
    public async Task<IActionResult> SaoChepAsync(
        Guid id, [FromBody] SaoChepBoTieuChiDto duLieu, CancellationToken ct)
    {
        var idMoi = await _dichVu.SaoChepAsync(id, duLieu.Ma, duLieu.Ten, duLieu.Nam, ct);
        return Ok(PhanHoiApi<Guid>.Ok(idMoi, "Đã sao chép bộ tiêu chí"));
    }

    [HttpPost]
    [Authorize(Policy = MaQuyen.TieuChiCauHinh)]
    public async Task<IActionResult> ThemAsync(
        [FromBody] LuuBoTieuChiDto duLieu, CancellationToken ct)
    {
        var banGhi = await _dichVu.ThemAsync(new Domain.TieuChi.BoTieuChi
        {
            Ma = duLieu.Ma,
            Ten = duLieu.Ten,
            MoTa = duLieu.MoTa,
            ThuTu = duLieu.ThuTu,
            TrangThai = duLieu.TrangThai,
            Nam = duLieu.Nam,
            Cap = duLieu.Cap,
            ThangDiemToiDa = duLieu.ThangDiemToiDa,
            DiemDatToiThieu = duLieu.DiemDatToiThieu,
            CachTinh = duLieu.CachTinh,
            LamTron = duLieu.LamTron,
            TuDongTongHop = duLieu.TuDongTongHop,
            LoaiBoDiemCaoThap = duLieu.LoaiBoDiemCaoThap
        }, ct);

        return Ok(PhanHoiApi<Domain.TieuChi.BoTieuChi>.Ok(banGhi, "Đã tạo bộ tiêu chí"));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = MaQuyen.TieuChiCauHinh)]
    public async Task<IActionResult> SuaAsync(
        Guid id, [FromBody] LuuBoTieuChiDto duLieu, CancellationToken ct)
    {
        var banGhi = await _dichVu.CapNhatAsync(id, x =>
        {
            x.Ma = duLieu.Ma;
            x.Ten = duLieu.Ten;
            x.MoTa = duLieu.MoTa;
            x.ThuTu = duLieu.ThuTu;
            x.TrangThai = duLieu.TrangThai;
            x.Nam = duLieu.Nam;
            x.Cap = duLieu.Cap;
            x.ThangDiemToiDa = duLieu.ThangDiemToiDa;
            x.DiemDatToiThieu = duLieu.DiemDatToiThieu;
            x.CachTinh = duLieu.CachTinh;
            x.LamTron = duLieu.LamTron;
            x.TuDongTongHop = duLieu.TuDongTongHop;
            x.LoaiBoDiemCaoThap = duLieu.LoaiBoDiemCaoThap;
        }, ct);

        return Ok(PhanHoiApi<Domain.TieuChi.BoTieuChi>.Ok(banGhi, "Cập nhật thành công"));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = MaQuyen.TieuChiCauHinh)]
    public async Task<IActionResult> XoaAsync(Guid id, CancellationToken ct)
    {
        await _dichVu.XoaAsync(id, ct);
        return Ok(PhanHoiApi.Ok("Đã xóa"));
    }
}
