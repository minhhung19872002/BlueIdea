using System.Net;
using System.Text.Json;
using BlueIdea.Application.Chung;
using BlueIdea.Shared.KetQua;
using FluentValidation;

namespace BlueIdea.Api.Chung;

/// <summary>
/// Middleware bat toan bo ngoai le va chuyen thanh phan hoi chuan (Muc 8 dac ta).
/// Khong bao gio lo stack trace ra client o moi truong production.
/// </summary>
public sealed class MiddlewareXuLyLoi
{
    private static readonly JsonSerializerOptions TuyChonJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _tiepTheo;
    private readonly ILogger<MiddlewareXuLyLoi> _logger;
    private readonly IHostEnvironment _moiTruong;

    public MiddlewareXuLyLoi(
        RequestDelegate tiepTheo, ILogger<MiddlewareXuLyLoi> logger, IHostEnvironment moiTruong)
    {
        _tiepTheo = tiepTheo;
        _logger = logger;
        _moiTruong = moiTruong;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _tiepTheo(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await XuLyAsync(context, ex).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Ghi loi he thong vao bang nhat ky.
    ///
    /// Bao boc trong try/catch rieng: neu chinh viec ghi nhat ky loi lai nem ngoai le (mat ket
    /// noi CSDL chang han) thi khong duoc lam hong phan hoi loi gui ve cho nguoi dung.
    /// </summary>
    private async Task GhiNhatKyLoiAsync(HttpContext context, Exception ex)
    {
        try
        {
            var db = context.RequestServices.GetService<IAppDbContext>();
            if (db is null) return;

            var nguoiDungId = context.User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            db.NhatKyLoi.Add(new Domain.QuanTri.NhatKyLoi
            {
                Id = Guid.NewGuid(),
                MucDo = "NGHIEM_TRONG",
                Nguon = $"{context.Request.Method} {context.Request.Path}",
                ThongBao = ex.Message,

                // Stack trace chi luu o CSDL cho quan tri vien doc, KHONG bao gio tra ve client.
                StackTrace = ex.StackTrace,
                DuLieuNguCanh = new Dictionary<string, object>
                {
                    ["loaiNgoaiLe"] = ex.GetType().Name,
                    ["duongDan"] = context.Request.Path.ToString(),
                    ["phuongThuc"] = context.Request.Method
                },
                NguoiDungId = Guid.TryParse(nguoiDungId, out var id) ? id : null,
                DiaChiIp = context.Connection.RemoteIpAddress?.ToString(),
                ThoiGian = DateTimeOffset.UtcNow
            });

            await db.SaveChangesAsync(context.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception loiGhi)
        {
            _logger.LogWarning(loiGhi, "Không ghi được nhật ký lỗi vào cơ sở dữ liệu.");
        }
    }

    private async Task XuLyAsync(HttpContext context, Exception ex)
    {
        var (maHttp, maLoi, thongBao, chiTiet) = PhanLoai(ex);

        if (maHttp >= 500)
        {
            _logger.LogError(ex, "Lỗi hệ thống khi xử lý {Method} {Path}",
                context.Request.Method, context.Request.Path);

            // Ghi them vao bang nhat_ky_loi de quan tri vien xem duoc ngay tren giao dien,
            // khong phai vao Seq hay doc log container. Chi ghi loi 5xx: loi nghiep vu 4xx la
            // hanh vi binh thuong cua he thong, ghi vao day chi lam nhieu.
            await GhiNhatKyLoiAsync(context, ex).ConfigureAwait(false);
        }
        else
        {
            _logger.LogWarning("Lỗi nghiệp vụ {MaLoi} tại {Path}: {ThongBao}",
                maLoi, context.Request.Path, thongBao);
        }

        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = maHttp;
        context.Response.ContentType = "application/json; charset=utf-8";

        var phanHoi = PhanHoiApi.Loi(
            maLoi,
            maHttp >= 500 && !_moiTruong.IsDevelopment()
                ? "Đã xảy ra lỗi hệ thống. Vui lòng liên hệ quản trị viên."
                : thongBao,
            chiTiet);

        await context.Response
            .WriteAsync(JsonSerializer.Serialize(phanHoi, TuyChonJson))
            .ConfigureAwait(false);
    }

    private static (int MaHttp, string MaLoi, string ThongBao, IReadOnlyList<ChiTietLoiApi>? ChiTiet)
        PhanLoai(Exception ex) => ex switch
    {
        ValidationException v => (
            (int)HttpStatusCode.UnprocessableEntity,
            MaLoiHeThong.DuLieuKhongHopLe,
            "Dữ liệu không hợp lệ.",
            v.Errors.Select(e => new ChiTietLoiApi(ChuanHoaTenTruong(e.PropertyName), e.ErrorMessage))
                .ToList()),

        KhongCoQuyenException q => (
            (int)HttpStatusCode.Forbidden, MaLoiHeThong.KhongCoQuyen, q.Message, null),

        KhongTimThayException k => (
            (int)HttpStatusCode.NotFound, MaLoiHeThong.KhongTimThay, k.Message, null),

        NghiepVuException n => (
            MaHttpTheoMaLoi(n.MaLoi), n.MaLoi, n.Message, null),

        UnauthorizedAccessException => (
            (int)HttpStatusCode.Unauthorized, MaLoiHeThong.ChuaXacThuc,
            "Bạn cần đăng nhập để sử dụng chức năng này.", null),

        OperationCanceledException => (
            499, "YEU_CAU_BI_HUY", "Yêu cầu đã bị hủy.", null),

        _ => ((int)HttpStatusCode.InternalServerError, MaLoiHeThong.LoiHeThong, ex.Message, null)
    };

    /// <summary>Anh xa ma loi nghiep vu sang ma HTTP phu hop.</summary>
    private static int MaHttpTheoMaLoi(string maLoi) => maLoi switch
    {
        // 401 - chua/khong xac thuc duoc. Loi dang nhap KHONG duoc phan biet giua "sai tai khoan"
        // va "sai mat khau" nen deu tra ve cung mot ma, tranh do tai khoan ton tai.
        MaLoiHeThong.ChuaXacThuc or MaLoiHeThong.SaiTaiKhoanMatKhau
            or MaLoiHeThong.TaiKhoanBiKhoa or MaLoiHeThong.TaiKhoanChuaKichHoat
            or MaLoiHeThong.TokenKhongHopLe or MaLoiHeThong.TokenHetHan
            or MaLoiHeThong.CanXacThucMfa
            => (int)HttpStatusCode.Unauthorized,

        MaLoiHeThong.KhongCoQuyen or MaLoiHeThong.KhongCoQuyenXuLyBuoc
            => (int)HttpStatusCode.Forbidden,

        MaLoiHeThong.KhongTimThay => (int)HttpStatusCode.NotFound,

        // 409 - yeu cau hop le nhung XUNG DOT voi trang thai hien tai cua tai nguyen.
        MaLoiHeThong.DangDuocThamChieu or MaLoiHeThong.TrungMa
            or MaLoiHeThong.QuyTrinhDangSuDung or MaLoiHeThong.XungDotDuLieu
            or MaLoiHeThong.YeuCauTrungLap
            or MaLoiHeThong.TrangThaiKhongChoPhepSua or MaLoiHeThong.TrangThaiKhongChoPhepXoa
            or MaLoiHeThong.DotDeNghiDaDong or MaLoiHeThong.DotDeNghiDaKhoa
            or MaLoiHeThong.QuaHanNopHoSo or MaLoiHeThong.HoSoDangKhoa
            or MaLoiHeThong.PhieuDaGuiKhongSuaDuoc or MaLoiHeThong.QuyTrinhChuaKichHoat
            => (int)HttpStatusCode.Conflict,

        // 422 - du lieu gui len khong thoa man rang buoc nghiep vu.
        MaLoiHeThong.DuLieuKhongHopLe or MaLoiHeThong.TyLeDongGopKhongHopLe
            or MaLoiHeThong.ThieuThanhPhanBatBuoc or MaLoiHeThong.TrongSoKhongDu100
            or MaLoiHeThong.KhoangDiemChongLan or MaLoiHeThong.VuotSoTacGiaToiDa
            or MaLoiHeThong.TepKhongHopLe or MaLoiHeThong.VuotDungLuongToiDa
            or MaLoiHeThong.DiemVuotToiDa or MaLoiHeThong.XungDotLoiIch
            or MaLoiHeThong.MatKhauKhongDatChinhSach or MaLoiHeThong.MatKhauDaSuDung
            or MaLoiHeThong.BoTieuChiKhongHopLe or MaLoiHeThong.QuyTrinhKhongHopLe
            => (int)HttpStatusCode.UnprocessableEntity,

        _ => (int)HttpStatusCode.BadRequest
    };

    /// <summary>Doi ten truong tu PascalCase cua validator sang camelCase theo hop dong API.</summary>
    private static string ChuanHoaTenTruong(string ten)
    {
        if (string.IsNullOrEmpty(ten))
        {
            return ten;
        }

        var phan = ten.Split('.');
        return string.Join('.', phan.Select(p =>
            p.Length > 0 ? char.ToLowerInvariant(p[0]) + p[1..] : p));
    }
}

/// <summary>Them cac header bao mat bat buoc (Muc 6 dac ta).</summary>
public sealed class MiddlewareHeaderBaoMat
{
    private readonly RequestDelegate _tiepTheo;

    public MiddlewareHeaderBaoMat(RequestDelegate tiepTheo) => _tiepTheo = tiepTheo;

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
        headers["Content-Security-Policy"] =
            "default-src 'self'; frame-ancestors 'none'; object-src 'none'; base-uri 'self'";

        // Loai bo header lo thong tin phien ban may chu.
        headers.Remove("Server");
        headers.Remove("X-Powered-By");

        return _tiepTheo(context);
    }
}
