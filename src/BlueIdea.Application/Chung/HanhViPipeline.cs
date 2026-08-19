using System.Diagnostics;
using BlueIdea.Shared.KetQua;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BlueIdea.Application.Chung;

/// <summary>Danh dau request can kiem tra quyen truoc khi chay handler.</summary>
public interface ICoYeuCauQuyen
{
    /// <summary>Ma quyen bat buoc, vi du SANG_KIEN.SUA.</summary>
    string MaQuyenYeuCau { get; }
}

/// <summary>Danh dau command lam thay doi du lieu -&gt; bat buoc ghi audit log.</summary>
public interface ICoGhiNhatKy
{
    string HanhDongNhatKy { get; }

    string ModuleNhatKy { get; }

    /// <summary>Id doi tuong de ghi vao audit log (truy vet hanh dong tren doi tuong cu the).</summary>
    Guid? DoiTuongId => null;
}

/// <summary>
/// Chay FluentValidation truoc handler. Loi validation duoc gom thanh
/// <see cref="ValidationException"/> va middleware chuyen thanh HTTP 422 theo dac ta Muc 8.
/// </summary>
public sealed class HanhViKiemTraDuLieu<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public HanhViKiemTraDuLieu(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var danhSach = _validators.ToList();
        if (danhSach.Count == 0)
        {
            return await next().ConfigureAwait(false);
        }

        var nguCanh = new ValidationContext<TRequest>(request);
        var ketQua = await Task.WhenAll(
            danhSach.Select(v => v.ValidateAsync(nguCanh, cancellationToken))).ConfigureAwait(false);

        var loi = ketQua.SelectMany(r => r.Errors).Where(e => e is not null).ToList();
        if (loi.Count > 0)
        {
            throw new ValidationException(loi);
        }

        return await next().ConfigureAwait(false);
    }
}

/// <summary>Kiem tra quyen tren tung chuc nang truoc khi chay handler.</summary>
public sealed class HanhViPhanQuyen<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IDichVuPhanQuyen _phanQuyen;
    private readonly INguoiDungHienTai _nguoiDung;

    public HanhViPhanQuyen(IDichVuPhanQuyen phanQuyen, INguoiDungHienTai nguoiDung)
    {
        _phanQuyen = phanQuyen;
        _nguoiDung = nguoiDung;
    }

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is ICoYeuCauQuyen yeuCau)
        {
            if (!_nguoiDung.DaXacThuc)
            {
                throw new NghiepVuException(MaLoiHeThong.ChuaXacThuc, "Phiên đăng nhập không hợp lệ.");
            }

            await _phanQuyen
                .BatBuocCoQuyenAsync(yeuCau.MaQuyenYeuCau, cancellationToken)
                .ConfigureAwait(false);
        }

        return await next().ConfigureAwait(false);
    }
}

/// <summary>Ghi audit log cho moi command lam thay doi du lieu (nguyen tac 4 dac ta).</summary>
public sealed class HanhViGhiNhatKy<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IDichVuNhatKy _nhatKy;

    public HanhViGhiNhatKy(IDichVuNhatKy nhatKy) => _nhatKy = nhatKy;

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ICoGhiNhatKy thongTin)
        {
            return await next().ConfigureAwait(false);
        }

        try
        {
            var ketQua = await next().ConfigureAwait(false);

            await _nhatKy.GhiAsync(
                thongTin.HanhDongNhatKy,
                thongTin.ModuleNhatKy,
                doiTuong: typeof(TRequest).Name,
                doiTuongId: thongTin.DoiTuongId,
                duLieuSau: request,
                ct: cancellationToken).ConfigureAwait(false);

            return ketQua;
        }
        catch (Exception ex)
        {
            await _nhatKy.GhiAsync(
                thongTin.HanhDongNhatKy,
                thongTin.ModuleNhatKy,
                doiTuong: typeof(TRequest).Name,
                moTa: ex.Message,
                ketQua: "THAT_BAI",
                ct: cancellationToken).ConfigureAwait(false);

            throw;
        }
    }
}

/// <summary>Ghi log thoi gian xu ly - phuc vu muc tieu P95 &lt; 500ms (Muc 7 dac ta).</summary>
public sealed class HanhViGhiLog<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int NguongChamMs = 500;

    private readonly ILogger<HanhViGhiLog<TRequest, TResponse>> _logger;
    private readonly INguoiDungHienTai _nguoiDung;

    public HanhViGhiLog(ILogger<HanhViGhiLog<TRequest, TResponse>> logger, INguoiDungHienTai nguoiDung)
    {
        _logger = logger;
        _nguoiDung = nguoiDung;
    }

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var dongHo = Stopwatch.StartNew();
        var ketQua = await next().ConfigureAwait(false);
        dongHo.Stop();

        if (dongHo.ElapsedMilliseconds > NguongChamMs)
        {
            _logger.LogWarning(
                "Yêu cầu {TenYeuCau} của {TenDangNhap} xử lý chậm: {SoMs}ms",
                typeof(TRequest).Name, _nguoiDung.TenDangNhap ?? "(ẩn danh)", dongHo.ElapsedMilliseconds);
        }

        return ketQua;
    }
}
