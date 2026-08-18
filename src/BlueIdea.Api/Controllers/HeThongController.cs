using BlueIdea.Api.Chung;
using BlueIdea.Application.Chung;
using BlueIdea.Application.QuanTri;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Shared.KetQua;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Api.Controllers;

public sealed record LuuCauHinhDto(string Khoa, string? GiaTri);

/// <summary>Chức năng 46, 51 — Cấu hình hệ thống và thông tin sáng kiến.</summary>
[ApiController]
[Route("api/v1/he-thong")]
[Produces("application/json")]
public sealed class HeThongController : ControllerBase
{
    /// <summary>Các khoá cấu hình được phép đọc khi chưa đăng nhập (dùng cho trang đăng nhập).</summary>
    private static readonly string[] KhoaCongKhai =
    {
        KhoaCauHinh.TenHeThong,
        KhoaCauHinh.TenDonVi,
        KhoaCauHinh.MauChuDao,
        KhoaCauHinh.LogoId,
        KhoaCauHinh.FaviconId,
        KhoaCauHinh.EmailHoTro,
        KhoaCauHinh.DienThoaiHoTro,
        KhoaCauHinh.DiaChi
    };

    private readonly IAppDbContext _db;
    private readonly IDichVuCauHinh _cauHinh;
    private readonly IDichVuPhanQuyen _phanQuyen;
    private readonly DichVuQuanTriNguoiDung _quanTri;

    public HeThongController(
        IAppDbContext db, IDichVuCauHinh cauHinh, IDichVuPhanQuyen phanQuyen,
        DichVuQuanTriNguoiDung quanTri)
    {
        _db = db;
        _cauHinh = cauHinh;
        _phanQuyen = phanQuyen;
        _quanTri = quanTri;
    }

    /// <summary>Cấu hình hiển thị công khai — không cần đăng nhập.</summary>
    [HttpGet("cau-hinh-cong-khai")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PhanHoiApi<Dictionary<string, string?>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LayCauHinhCongKhaiAsync(CancellationToken ct)
    {
        var cauHinh = await _db.CauHinhHeThong.AsNoTracking()
            .Where(x => KhoaCongKhai.Contains(x.Khoa))
            .ToDictionaryAsync(x => x.Khoa, x => x.GiaTri, ct);

        return Ok(PhanHoiApi<Dictionary<string, string?>>.Ok(cauHinh));
    }

    /// <summary>Toàn bộ cấu hình hệ thống (theo nhóm) — cần quyền CAU_HINH.XEM.</summary>
    [HttpGet("cau-hinh")]
    [Authorize(Policy = MaQuyen.CauHinhXem)]
    public async Task<IActionResult> LayCauHinhAsync([FromQuery] string? nhom, CancellationToken ct)
    {
        var truyVan = _db.CauHinhHeThong.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(nhom))
        {
            truyVan = truyVan.Where(x => x.Nhom == nhom);
        }

        var danhSach = await truyVan
            .OrderBy(x => x.Nhom).ThenBy(x => x.ThuTu)
            .ToListAsync(ct);

        return Ok(PhanHoiApi<List<CauHinhHeThong>>.Ok(danhSach));
    }

    /// <summary>Cập nhật nhiều khoá cấu hình cùng lúc.</summary>
    [HttpPut("cau-hinh")]
    [Authorize(Policy = MaQuyen.CauHinhSua)]
    public async Task<IActionResult> LuuCauHinhAsync(
        [FromBody] List<LuuCauHinhDto> danhSach, CancellationToken ct)
    {
        foreach (var muc in danhSach)
        {
            await _cauHinh.DatAsync(muc.Khoa, muc.GiaTri, ct);
        }

        return Ok(PhanHoiApi.Ok($"Đã lưu {danhSach.Count} mục cấu hình"));
    }

    /// <summary>Chức năng 45 — Danh sách vai trò kèm ma trận phân quyền.</summary>
    [HttpGet("vai-tro")]
    [Authorize(Policy = MaQuyen.VaiTroXem)]
    public async Task<IActionResult> LayVaiTroAsync(CancellationToken ct)
    {
        var vaiTro = await _db.VaiTro.AsNoTracking()
            .Include(x => x.DanhSachQuyen)
            .Include(x => x.PhamViDuLieu)
            .OrderBy(x => x.ThuTu)
            .ToListAsync(ct);

        var quyen = await _db.Quyen.AsNoTracking()
            .OrderBy(x => x.NhomChucNang).ThenBy(x => x.ThuTu)
            .ToListAsync(ct);

        var ketQua = new
        {
            vaiTro = vaiTro.Select(v => new
            {
                v.Id,
                v.Ma,
                v.Ten,
                v.MoTa,
                v.LaHeThong,
                v.TrangThai,
                quyenIds = v.DanhSachQuyen.Select(q => q.QuyenId).ToList(),
                phamVi = v.PhamViDuLieu.Select(p => new { p.LoaiPhamVi, p.DonViIds }).ToList()
            }),
            quyen = quyen.Select(q => new { q.Id, q.Ma, q.Ten, q.NhomChucNang })
        };

        return Ok(PhanHoiApi<object>.Ok(ketQua));
    }

    /// <summary>Chức năng 43 — Danh sách người dùng.</summary>
    [HttpGet("nguoi-dung")]
    [Authorize(Policy = MaQuyen.NguoiDungXem)]
    public async Task<IActionResult> LayNguoiDungAsync(
        [FromQuery] ThamSoPhanTrang thamSo,
        [FromQuery] string? tuKhoa,
        [FromQuery] Guid? donViId,
        CancellationToken ct)
    {
        var truyVan = _db.NguoiDung.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var khongDau = Shared.TiengViet.VanBanTiengViet.TaoKhongDau(tuKhoa);
            var hoa = tuKhoa.Trim().ToUpperInvariant();
            truyVan = truyVan.Where(x =>
                x.HoTenKhongDau.Contains(khongDau)
                || x.TenDangNhap.ToUpper().Contains(hoa));
        }

        if (donViId.HasValue)
        {
            truyVan = truyVan.Where(x => x.DonViId == donViId.Value);
        }

        var tongSo = await truyVan.CountAsync(ct);

        var duLieu = await truyVan
            .OrderBy(x => x.HoTen)
            .Skip(thamSo.BoQua)
            .Take(thamSo.SoDong)
            .Select(x => new
            {
                x.Id,
                x.TenDangNhap,
                x.HoTen,
                x.Email,
                x.DienThoai,
                x.ChucVu,
                x.DonViId,
                x.TrangThaiTaiKhoan,
                x.LanDangNhapCuoi,
                x.MfaEnabled
            })
            .ToListAsync(ct);

        return Ok(new PhanHoiPhanTrang<object>
        {
            DuLieu = duLieu.Cast<object>().ToList(),
            TongSo = tongSo,
            Trang = thamSo.Trang,
            SoDong = thamSo.SoDong
        });
    }

    /// <summary>Chức năng 43 — Khoá / mở khoá tài khoản.</summary>
    [HttpPatch("nguoi-dung/{id:guid}/trang-thai")]
    [Authorize(Policy = MaQuyen.NguoiDungSua)]
    public async Task<IActionResult> DoiTrangThaiNguoiDungAsync(
        Guid id, [FromQuery] string trangThai, CancellationToken ct)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.NguoiDungSua, id, ct);

        var nguoiDung = await _db.NguoiDung.FirstOrDefaultAsync(x => x.Id == id, ct)
                        ?? throw new KhongTimThayException("người dùng", id);

        var hopLe = trangThai is TrangThaiNguoiDung.HoatDong or TrangThaiNguoiDung.Khoa
            or TrangThaiNguoiDung.ChoKichHoat;

        if (!hopLe)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                $"Trạng thái '{trangThai}' không hợp lệ.");
        }

        nguoiDung.TrangThaiTaiKhoan = trangThai;

        // Mở khoá thì xoá luôn khoá tạm do đăng nhập sai nhiều lần.
        if (trangThai == TrangThaiNguoiDung.HoatDong)
        {
            nguoiDung.KhoaDen = null;
            nguoiDung.SoLanDangNhapSai = 0;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(PhanHoiApi.Ok("Đã cập nhật trạng thái tài khoản"));
    }

    /// <summary>Nhật ký hệ thống (audit log) có lọc.</summary>
    [HttpGet("nhat-ky/he-thong")]
    [Authorize(Policy = MaQuyen.NhatKyXem)]
    public async Task<IActionResult> LayNhatKyHeThongAsync(
        [FromQuery] ThamSoPhanTrang thamSo,
        [FromQuery] string? module,
        [FromQuery] string? hanhDong,
        [FromQuery] DateTimeOffset? tuNgay,
        [FromQuery] DateTimeOffset? denNgay,
        CancellationToken ct)
    {
        var truyVan = _db.NhatKyHeThong.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(module))
        {
            truyVan = truyVan.Where(x => x.Module == module);
        }

        if (!string.IsNullOrWhiteSpace(hanhDong))
        {
            truyVan = truyVan.Where(x => x.HanhDong == hanhDong);
        }

        if (tuNgay.HasValue)
        {
            truyVan = truyVan.Where(x => x.ThoiGian >= tuNgay.Value);
        }

        if (denNgay.HasValue)
        {
            truyVan = truyVan.Where(x => x.ThoiGian <= denNgay.Value);
        }

        var tongSo = await truyVan.CountAsync(ct);
        var duLieu = await truyVan
            .OrderByDescending(x => x.ThoiGian)
            .Skip(thamSo.BoQua)
            .Take(thamSo.SoDong)
            .ToListAsync(ct);

        return Ok(new PhanHoiPhanTrang<NhatKyHeThong>
        {
            DuLieu = duLieu,
            TongSo = tongSo,
            Trang = thamSo.Trang,
            SoDong = thamSo.SoDong
        });
    }

    /// <summary>Nhật ký đăng nhập.</summary>
    [HttpGet("nhat-ky/dang-nhap")]
    [Authorize(Policy = MaQuyen.NhatKyXem)]
    public async Task<IActionResult> LayNhatKyDangNhapAsync(
        [FromQuery] ThamSoPhanTrang thamSo, [FromQuery] string? tenDangNhap, CancellationToken ct)
    {
        var truyVan = _db.NhatKyDangNhap.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(tenDangNhap))
        {
            truyVan = truyVan.Where(x => x.TenDangNhap.Contains(tenDangNhap));
        }

        var tongSo = await truyVan.CountAsync(ct);
        var duLieu = await truyVan
            .OrderByDescending(x => x.ThoiGian)
            .Skip(thamSo.BoQua)
            .Take(thamSo.SoDong)
            .ToListAsync(ct);

        return Ok(new PhanHoiPhanTrang<NhatKyDangNhap>
        {
            DuLieu = duLieu,
            TongSo = tongSo,
            Trang = thamSo.Trang,
            SoDong = thamSo.SoDong
        });
    }

    /// <summary>
    /// Chức năng nhật ký — lỗi hệ thống (5xx) đã ghi lại, để quản trị viên xem ngay trên giao
    /// diện thay vì phải mở log container.
    /// </summary>
    [HttpGet("nhat-ky/loi")]
    [Authorize(Policy = MaQuyen.NhatKyXem)]
    public async Task<IActionResult> LayNhatKyLoiAsync(
        [FromQuery] ThamSoPhanTrang thamSo,
        [FromQuery] string? mucDo,
        [FromQuery] bool? daXuLy,
        CancellationToken ct)
    {
        var truyVan = _db.NhatKyLoi.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(mucDo))
        {
            truyVan = truyVan.Where(x => x.MucDo == mucDo);
        }

        if (daXuLy.HasValue)
        {
            truyVan = truyVan.Where(x => x.DaXuLy == daXuLy.Value);
        }

        var tongSo = await truyVan.CountAsync(ct);

        var duLieu = await truyVan
            .OrderByDescending(x => x.ThoiGian)
            .Skip(thamSo.BoQua)
            .Take(thamSo.SoDong)
            .Select(x => new
            {
                x.Id,
                x.MucDo,
                x.Nguon,
                x.ThongBao,
                x.StackTrace,
                x.DuLieuNguCanh,
                x.NguoiDungId,
                x.DiaChiIp,
                x.ThoiGian,
                x.DaXuLy
            })
            .ToListAsync(ct);

        return Ok(new PhanHoiPhanTrang<object>
        {
            DuLieu = duLieu.Cast<object>().ToList(),
            TongSo = tongSo,
            Trang = thamSo.Trang,
            SoDong = thamSo.SoDong
        });
    }

    /// <summary>Đánh dấu một lỗi đã được xử lý để không lẫn với lỗi mới.</summary>
    [HttpPost("nhat-ky/loi/{id:guid}/da-xu-ly")]
    [Authorize(Policy = MaQuyen.NhatKyXem)]
    public async Task<IActionResult> DanhDauLoiDaXuLyAsync(Guid id, CancellationToken ct)
    {
        var banGhi = await _db.NhatKyLoi.FirstOrDefaultAsync(x => x.Id == id, ct)
                     ?? throw new KhongTimThayException("nhật ký lỗi", id);

        banGhi.DaXuLy = true;
        await _db.SaveChangesAsync(ct);

        return Ok(PhanHoiApi.Ok("Đã đánh dấu lỗi đã xử lý"));
    }

    /// <summary>Thông báo trong ứng dụng của người dùng hiện tại.</summary>
    [HttpGet("thong-bao")]
    [Authorize]
    public async Task<IActionResult> LayThongBaoAsync(
        [FromQuery] ThamSoPhanTrang thamSo, [FromQuery] bool? chuaDoc, CancellationToken ct)
    {
        var nguoiDungId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(nguoiDungId, out var id))
        {
            return Unauthorized(PhanHoiApi.Loi(MaLoiHeThong.ChuaXacThuc, "Chưa đăng nhập."));
        }

        var truyVan = _db.ThongBao.AsNoTracking().Where(x => x.NguoiNhanId == id);

        if (chuaDoc == true)
        {
            truyVan = truyVan.Where(x => !x.DaDoc);
        }

        var tongSo = await truyVan.CountAsync(ct);
        var duLieu = await truyVan
            .OrderByDescending(x => x.ThoiGian)
            .Skip(thamSo.BoQua)
            .Take(thamSo.SoDong)
            .ToListAsync(ct);

        return Ok(new PhanHoiPhanTrang<ThongBao>
        {
            DuLieu = duLieu,
            TongSo = tongSo,
            Trang = thamSo.Trang,
            SoDong = thamSo.SoDong
        });
    }

    /// <summary>
    /// Đánh dấu đã đọc thông báo.
    ///
    /// Truy vấn lọc luôn theo người nhận: chỉ tìm theo Id rồi kiểm tra chủ sở hữu sau thì người
    /// dùng vẫn đánh dấu được thông báo của người khác, và thông báo lỗi còn để lộ Id nào có thật.
    /// </summary>
    [HttpPost("thong-bao/{id:guid}/da-doc")]
    [Authorize]
    public async Task<IActionResult> DanhDauDaDocAsync(Guid id, CancellationToken ct)
    {
        var nguoiDungId = LayNguoiDungHienTaiId();

        var thongBao = await _db.ThongBao
                           .FirstOrDefaultAsync(x => x.Id == id && x.NguoiNhanId == nguoiDungId, ct)
                       ?? throw new KhongTimThayException("thông báo", id);

        thongBao.DaDoc = true;
        thongBao.NgayDoc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(PhanHoiApi.Ok("Đã đánh dấu đã đọc"));
    }

    /// <summary>Đánh dấu đã đọc TẤT CẢ thông báo chưa đọc của người đăng nhập.</summary>
    [HttpPost("thong-bao/doc-tat-ca")]
    [Authorize]
    public async Task<IActionResult> DanhDauDocTatCaAsync(CancellationToken ct)
    {
        var nguoiDungId = LayNguoiDungHienTaiId();

        var danhSach = await _db.ThongBao
            .Where(x => x.NguoiNhanId == nguoiDungId && !x.DaDoc)
            .ToListAsync(ct);

        var bayGio = DateTimeOffset.UtcNow;

        foreach (var muc in danhSach)
        {
            muc.DaDoc = true;
            muc.NgayDoc = bayGio;
        }

        await _db.SaveChangesAsync(ct);

        return Ok(PhanHoiApi.Ok($"Đã đánh dấu đã đọc {danhSach.Count} thông báo"));
    }

    private Guid LayNguoiDungHienTaiId()
    {
        var giaTri = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(giaTri, out var id)
            ? id
            : throw new NghiepVuException(MaLoiHeThong.ChuaXacThuc, "Chưa đăng nhập.");
    }

    // ------------------------------------------------------ Chức năng 43 — Quản lý người dùng

    /// <summary>Chi tiết một tài khoản kèm vai trò đang gán.</summary>
    [HttpGet("nguoi-dung/{id:guid}")]
    [Authorize(Policy = MaQuyen.NguoiDungXem)]
    public async Task<IActionResult> LayChiTietNguoiDungAsync(Guid id, CancellationToken ct)
        => Ok(PhanHoiApi<ThongTinNguoiDungDto>.Ok(await _quanTri.ChiTietAsync(id, ct)));

    /// <summary>
    /// Tạo tài khoản mới. Hệ thống sinh mật khẩu tạm và trả về đúng MỘT LẦN trong phản hồi này —
    /// mật khẩu chỉ lưu dưới dạng băm Argon2id nên không thể xem lại về sau.
    /// </summary>
    [HttpPost("nguoi-dung")]
    [Authorize(Policy = MaQuyen.NguoiDungThem)]
    public async Task<IActionResult> ThemNguoiDungAsync(
        [FromBody] LuuNguoiDungDto duLieu, CancellationToken ct)
    {
        var (id, matKhauTam) = await _quanTri.ThemAsync(duLieu, ct);

        return Ok(PhanHoiApi<object>.Ok(
            new { id, matKhauTam },
            "Đã tạo tài khoản. Hãy bàn giao mật khẩu tạm cho người dùng — mật khẩu này không xem lại được."));
    }

    [HttpPut("nguoi-dung/{id:guid}")]
    [Authorize(Policy = MaQuyen.NguoiDungSua)]
    public async Task<IActionResult> SuaNguoiDungAsync(
        Guid id, [FromBody] LuuNguoiDungDto duLieu, CancellationToken ct)
    {
        await _quanTri.CapNhatAsync(id, duLieu, ct);
        return Ok(PhanHoiApi.Ok("Đã cập nhật tài khoản"));
    }

    /// <summary>Đặt lại mật khẩu, thu hồi mọi phiên đang mở và bắt đổi ở lần đăng nhập kế tiếp.</summary>
    [HttpPost("nguoi-dung/{id:guid}/dat-lai-mat-khau")]
    [Authorize(Policy = MaQuyen.NguoiDungDatLaiMatKhau)]
    public async Task<IActionResult> DatLaiMatKhauAsync(Guid id, CancellationToken ct)
    {
        var matKhauTam = await _quanTri.DatLaiMatKhauAsync(id, ct);

        return Ok(PhanHoiApi<object>.Ok(
            new { matKhauTam },
            "Đã đặt lại mật khẩu. Toàn bộ phiên đăng nhập cũ đã bị thu hồi."));
    }

    // --------------------------------------------------------- Chức năng 45 — Quản lý vai trò

    [HttpPost("vai-tro")]
    [Authorize(Policy = MaQuyen.VaiTroCauHinh)]
    public async Task<IActionResult> ThemVaiTroAsync([FromBody] LuuVaiTroDto duLieu, CancellationToken ct)
    {
        var id = await _quanTri.ThemVaiTroAsync(duLieu, ct);
        return Ok(PhanHoiApi<Guid>.Ok(id, "Đã tạo vai trò"));
    }

    [HttpPut("vai-tro/{id:guid}")]
    [Authorize(Policy = MaQuyen.VaiTroCauHinh)]
    public async Task<IActionResult> SuaVaiTroAsync(
        Guid id, [FromBody] LuuVaiTroDto duLieu, CancellationToken ct)
    {
        await _quanTri.CapNhatVaiTroAsync(id, duLieu, ct);
        return Ok(PhanHoiApi.Ok("Đã cập nhật ma trận phân quyền"));
    }

    [HttpDelete("vai-tro/{id:guid}")]
    [Authorize(Policy = MaQuyen.VaiTroCauHinh)]
    public async Task<IActionResult> XoaVaiTroAsync(Guid id, CancellationToken ct)
    {
        await _quanTri.XoaVaiTroAsync(id, ct);
        return Ok(PhanHoiApi.Ok("Đã xoá vai trò"));
    }
}
