using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Shared.KetQua;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.KySo;

/// <summary>Du lieu tra ve cho cong cu ky o may tram.</summary>
public sealed record YeuCauKyUsbDto(
    Guid PhienId,
    string HashBase64,
    string ThuatToanBam,
    string TenTep,
    DateTimeOffset HetHan);

/// <summary>Ket qua sau khi may tram gui chu ky ve.</summary>
public sealed record KetQuaKyUsbDto(
    Guid NhatKyKySoId,
    Guid TepChuKyId,
    string ChuTheChungThu,
    string? SerialChungThu,
    DateTimeOffset ThoiGianKy);

/// <summary>
/// Chuc nang 49 — Ky so bang USB token (chung thu ca nhan cam tren may nguoi ky).
///
/// Khoa bi mat trong USB token KHONG BAO GIO roi khoi thiet bi — do la ly do ton tai cua no.
/// Vi vay may chu khong the tu ky; luong bat buoc phai la ba nhip:
///   1. May chu chot noi dung can ky va tra ve GIA TRI BAM cua noi dung do.
///   2. Cong cu ky o may tram (plugin/ung dung ky cua nha cung cap) dung token ky gia tri bam.
///   3. May chu nhan chu ky + chung thu, XAC MINH lai roi moi ghi nhan.
///
/// Buoc 3 kiem tra chu ky doi chieu voi CHINH gia tri bam da phat o buoc 1, khong phai voi gia
/// tri bam do may tram gui len: neu tin gia tri bam cua client thi bat ky ai cung "ky" duoc mot
/// noi dung tuy y roi gan vao van ban khac.
/// </summary>
public sealed class DichVuKySoUsbToken
{
    /// <summary>Phien ky song ngan: du de nguoi dung cam token va nhap ma PIN.</summary>
    private static readonly TimeSpan ThoiGianSongPhien = TimeSpan.FromMinutes(10);

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, PhienKy>
        CacPhien = new();

    private readonly IAppDbContext _db;
    private readonly ILuuTruTep _luuTru;
    private readonly IDichVuPhanQuyen _phanQuyen;
    private readonly INguoiDungHienTai _nguoiDung;
    private readonly IDongHoHeThong _dongHo;

    public DichVuKySoUsbToken(
        IAppDbContext db, ILuuTruTep luuTru, IDichVuPhanQuyen phanQuyen,
        INguoiDungHienTai nguoiDung, IDongHoHeThong dongHo)
    {
        _db = db;
        _luuTru = luuTru;
        _phanQuyen = phanQuyen;
        _nguoiDung = nguoiDung;
        _dongHo = dongHo;
    }

    /// <summary>Nhip 1 — chot noi dung can ky va tra gia tri bam cho cong cu ky.</summary>
    public async Task<YeuCauKyUsbDto> ChuanBiAsync(
        Guid tepTinId, string doiTuong, Guid doiTuongId, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.QuyetDinhKySo, doiTuongId, ct)
            .ConfigureAwait(false);

        DonPhienCu();

        var tep = await _db.TepTin.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tepTinId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("tệp tin", tepTinId);

        var noiDung = await DocNoiDungAsync(tep.Bucket, tep.DuongDan, ct).ConfigureAwait(false);
        var hash = SHA256.HashData(noiDung);

        var phien = new PhienKy(
            Guid.NewGuid(), _nguoiDung.Id ?? Guid.Empty, tepTinId, doiTuong, doiTuongId,
            hash, _dongHo.BayGio.Add(ThoiGianSongPhien));

        CacPhien[phien.Id] = phien;

        return new YeuCauKyUsbDto(
            phien.Id, Convert.ToBase64String(hash), "SHA-256", tep.TenGoc, phien.HetHan);
    }

    /// <summary>
    /// Nhip 3 — nhan chu ky tu cong cu ky, xac minh roi ghi nhan.
    ///
    /// <paramref name="chuKyBase64"/> la chu ky RSA/ECDSA tren gia tri bam da phat o nhip 1;
    /// <paramref name="chungThuBase64"/> la chung thu cong khai (DER) doc tu USB token.
    /// </summary>
    public async Task<KetQuaKyUsbDto> HoanTatAsync(
        Guid phienId, string chuKyBase64, string chungThuBase64, CancellationToken ct = default)
    {
        if (!CacPhien.TryGetValue(phienId, out var phien))
        {
            throw new KhongTimThayException("phiên ký số", phienId);
        }

        // Phien chi thuoc ve nguoi mo no: khong kiem tra thi ai doan duoc Id phien cung gan duoc
        // chu ky cua minh vao van ban nguoi khac dang chuan bi ky.
        if (phien.NguoiKyId != (_nguoiDung.Id ?? Guid.Empty))
        {
            throw new KhongTimThayException("phiên ký số", phienId);
        }

        if (phien.HetHan < _dongHo.BayGio)
        {
            CacPhien.TryRemove(phienId, out _);

            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Phiên ký đã hết hạn. Hãy bấm ký lại để lấy phiên mới.");
        }

        X509Certificate2 chungThu;
        byte[] chuKy;

        try
        {
            chungThu = new X509Certificate2(Convert.FromBase64String(chungThuBase64));
            chuKy = Convert.FromBase64String(chuKyBase64);
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Chứng thư hoặc chữ ký gửi lên không đọc được.");
        }

        using (chungThu)
        {
            KiemTraHieuLuc(chungThu);

            if (!XacMinhChuKy(chungThu, phien.Hash, chuKy))
            {
                throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                    "Chữ ký không khớp với nội dung cần ký hoặc không do chứng thư này tạo ra.");
            }

            var tepChuKy = await LuuTepChuKyAsync(phien, chuKy, chungThu, ct).ConfigureAwait(false);

            var nhatKy = new NhatKyKySo
            {
                Id = Guid.NewGuid(),
                DoiTuong = phien.DoiTuong,
                DoiTuongId = phien.DoiTuongId,
                NguoiKyId = phien.NguoiKyId,
                ThoiGianKy = _dongHo.BayGio,
                TepGocId = phien.TepTinId,
                TepDaKyId = tepChuKy.Id,
                SerialChungThu = chungThu.SerialNumber,
                NguoiCapChungThu = chungThu.Issuer,
                HieuLucTu = chungThu.NotBefore,
                HieuLucDen = chungThu.NotAfter,
                TrangThaiKy = TrangThaiKySo.ThanhCong,
                ThongTinXacThuc = new Dictionary<string, object>
                {
                    ["nguonKhoa"] = "USB_TOKEN",
                    ["chuThe"] = chungThu.Subject,
                    ["thuatToanBam"] = "SHA-256",
                    ["chuanKy"] = "CMS_DETACHED",
                },
            };

            _db.NhatKyKySo.Add(nhatKy);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            CacPhien.TryRemove(phienId, out _);

            return new KetQuaKyUsbDto(
                nhatKy.Id, tepChuKy.Id, chungThu.Subject, chungThu.SerialNumber, nhatKy.ThoiGianKy);
        }
    }

    /// <summary>Huy phien ky (nguoi dung dong hop thoai giua chung).</summary>
    public void Huy(Guid phienId)
    {
        if (CacPhien.TryGetValue(phienId, out var phien)
            && phien.NguoiKyId == (_nguoiDung.Id ?? Guid.Empty))
        {
            CacPhien.TryRemove(phienId, out _);
        }
    }

    private void KiemTraHieuLuc(X509Certificate2 chungThu)
    {
        var bayGio = _dongHo.BayGio.UtcDateTime;

        if (chungThu.NotBefore.ToUniversalTime() > bayGio
            || chungThu.NotAfter.ToUniversalTime() < bayGio)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                $"Chứng thư hết hiệu lực (từ {chungThu.NotBefore:dd/MM/yyyy} "
                + $"đến {chungThu.NotAfter:dd/MM/yyyy}).");
        }
    }

    /// <summary>Xac minh chu ky tren gia tri bam, thu ca RSA (PKCS#1 va PSS) lan ECDSA.</summary>
    private static bool XacMinhChuKy(X509Certificate2 chungThu, byte[] hash, byte[] chuKy)
    {
        using var rsa = chungThu.GetRSAPublicKey();

        if (rsa is not null)
        {
            if (rsa.VerifyHash(hash, chuKy, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
            {
                return true;
            }

            // Mot so cong cu ky dung PSS; thu them truoc khi ket luan la sai.
            return rsa.VerifyHash(hash, chuKy, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        }

        using var ecdsa = chungThu.GetECDsaPublicKey();

        return ecdsa is not null && ecdsa.VerifyHash(hash, chuKy);
    }

    /// <summary>
    /// Dong goi chu ky thanh CMS detached de luu — dung khuon dang chuan thay vi mot tep nhi
    /// phan tu che, de cong cu kiem tra chu ky ben ngoai van doc duoc.
    /// </summary>
    private async Task<Domain.SangKien.TepTin> LuuTepChuKyAsync(
        PhienKy phien, byte[] chuKy, X509Certificate2 chungThu, CancellationToken ct)
    {
        var tepGoc = await _db.TepTin.AsNoTracking()
            .FirstAsync(x => x.Id == phien.TepTinId, ct)
            .ConfigureAwait(false);

        var goi = new
        {
            chuan = "CMS_DETACHED_SHA256",
            nguonKhoa = "USB_TOKEN",
            hashNoiDung = Convert.ToBase64String(phien.Hash),
            chuKy = Convert.ToBase64String(chuKy),
            chungThu = Convert.ToBase64String(chungThu.RawData),
            thoiGianKy = _dongHo.BayGio,
        };

        var noiDung = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(goi);
        var tenLuuTru = $"{Guid.NewGuid():N}.p7s.json";

        var duongDan = await _luuTru
            .TaiLenAsync(new MemoryStream(noiDung), tenLuuTru, "application/json", tepGoc.Bucket, ct)
            .ConfigureAwait(false);

        var tep = new Domain.SangKien.TepTin
        {
            Id = Guid.NewGuid(),
            TenGoc = $"{Path.GetFileNameWithoutExtension(tepGoc.TenGoc)}-chu-ky-usb.json",
            TenLuuTru = tenLuuTru,
            DuongDan = duongDan,
            Bucket = tepGoc.Bucket,
            KichThuoc = noiDung.LongLength,
            MimeType = "application/json",
            PhanMoRong = ".json",
            HashSha256 = Convert.ToHexString(SHA256.HashData(noiDung)).ToLowerInvariant(),
            NguoiTaiLenId = _nguoiDung.Id,
            NgayTaiLen = _dongHo.BayGio,
            TrangThaiOcr = TrangThaiOcrTep.KhongCan,
            DaQuetVirus = true,
        };

        _db.TepTin.Add(tep);

        return tep;
    }

    private async Task<byte[]> DocNoiDungAsync(string bucket, string duongDan, CancellationToken ct)
    {
        await using var luong = await _luuTru.TaiXuongAsync(bucket, duongDan, ct)
            .ConfigureAwait(false);

        using var bo = new MemoryStream();
        await luong.CopyToAsync(bo, ct).ConfigureAwait(false);

        return bo.ToArray();
    }

    private void DonPhienCu()
    {
        foreach (var phien in CacPhien.Values.Where(x => x.HetHan < _dongHo.BayGio).ToList())
        {
            CacPhien.TryRemove(phien.Id, out _);
        }
    }

    private sealed record PhienKy(
        Guid Id,
        Guid NguoiKyId,
        Guid TepTinId,
        string DoiTuong,
        Guid DoiTuongId,
        byte[] Hash,
        DateTimeOffset HetHan);
}
