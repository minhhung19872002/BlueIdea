using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BlueIdea.Application.Chung;
using Microsoft.Extensions.Caching.Memory;

namespace BlueIdea.Application.BaoCao;

/// <summary>
/// Bo nho dem ket qua bao cao trong thoi gian ngan.
///
/// Cac bao cao thong ke quet toan bo bang ho so va nhom nhieu chieu; tren dashboard chung con bi
/// goi lai moi lan doi tab. Dem 5 phut cat phan lon so lan quet do ma van du moi cho so lieu
/// dieu hanh.
///
/// KHOA DEM LUON GOM DANH TINH VA PHAM VI DU LIEU cua nguoi goi. Neu chi dem theo tham so loc,
/// mot nguoi co pham vi toan he thong chay bao cao truoc se de lai ket qua toan he thong trong
/// dem, va nguoi chi duoc xem don vi minh goi sau se nhan dung ket qua do — ro ri du lieu ma
/// khong he co loi phan quyen nao o tang duoi.
/// </summary>
public sealed class BoNhoDemBaoCao
{
    /// <summary>Thoi gian song cua mot muc dem.</summary>
    public static readonly TimeSpan ThoiGianSong = TimeSpan.FromMinutes(5);

    private readonly KhoDemBaoCao _kho;
    private readonly INguoiDungHienTai _nguoiDung;

    public BoNhoDemBaoCao(KhoDemBaoCao kho, INguoiDungHienTai nguoiDung)
    {
        _kho = kho;
        _nguoiDung = nguoiDung;
    }

    public async Task<T> LayHoacTinhAsync<T>(
        string ten, object? thamSo, Func<Task<T>> tinh, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(tinh);

        var khoa = TaoKhoa(ten, thamSo);

        if (_kho.Dem.TryGetValue(khoa, out var daCo) && daCo is T ketQuaCu)
        {
            return ketQuaCu;
        }

        var ketQua = await tinh().ConfigureAwait(false);

        _kho.Dem.Set(khoa, ketQua, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ThoiGianSong,
            // Gioi han so muc dem: moi to hop bo loc la mot khoa, khong dat kich thuoc thi mot
            // nguoi bam qua lai cac bo loc du de day bo nho tien trinh len khong kiem soat.
            Size = 1,
        });

        return ketQua;
    }

    /// <summary>Xoa toan bo muc dem — dung khi du lieu nen doi dot ngot (nhap khau, dong bo lon).</summary>
    public void XoaTatCa() => _kho.Dem.Compact(1.0);

    private string TaoKhoa(string ten, object? thamSo)
    {
        var danhTinh = _nguoiDung.Id?.ToString() ?? "khach";
        var vaiTro = string.Join(',', _nguoiDung.VaiTro.OrderBy(x => x, StringComparer.Ordinal));
        var noiDung = thamSo is null ? string.Empty : JsonSerializer.Serialize(thamSo);

        var bam = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes($"{danhTinh}|{vaiTro}|{ten}|{noiDung}")));

        return $"bao-cao:{ten}:{bam[..32]}";
    }
}

/// <summary>
/// Kho dem RIENG cua bao cao, khong dung chung <see cref="IMemoryCache"/> toan ung dung.
///
/// Bo dem nay dat SizeLimit, ma mot MemoryCache co SizeLimit thi MOI muc ghi vao deu bat buoc
/// khai bao Size — dung chung se lam cac thanh phan khac (xac thuc, gioi han truy cap...) nem
/// loi ngay khi ghi dem. Dang ky singleton de dem song qua cac request.
/// </summary>
public sealed class KhoDemBaoCao : IDisposable
{
    public MemoryCache Dem { get; } = new(new MemoryCacheOptions { SizeLimit = 500 });

    public void Dispose() => Dem.Dispose();
}
