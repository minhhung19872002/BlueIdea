using System.Security.Cryptography;
using System.Text;
using BlueIdea.Application.Chung;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Shared.KetQua;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.XacThuc;

/// <summary>Mot thu thach CAPTCHA: dinh danh de doi chieu va anh SVG de hien thi.</summary>
public sealed record ThuThachCaptchaDto(string Id, string AnhSvg);

/// <summary>
/// Chuc nang 21 — CAPTCHA sau 3 lan dang nhap sai.
///
/// Tu sinh anh SVG chu KHONG dung reCAPTCHA/hCaptcha: rang buoc Muc 3.2 E-HSMT cam goi dich vu
/// ben thu ba, va nhung dich vu do con gui dia chi IP cua can bo ra ngoai he thong.
///
/// SVG duoc chon vi dung duoc bang chuoi thuan, khong can thu vien do hoa nao — anh bitmap thi
/// phai keo them System.Drawing hoac SkiaSharp va ca hai deu keo theo phu thuoc he dieu hanh.
/// </summary>
public sealed class DichVuCaptcha
{
    /// <summary>Bo ky tu da bo I, O, 0, 1 — nhung cap nguoi doc hay nham nhat.</summary>
    private const string BangKyTu = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private const int SoKyTu = 5;
    private const int SoPhutHieuLuc = 5;

    private readonly IAppDbContext _db;
    private readonly IDongHoHeThong _dongHo;
    private readonly INguoiDungHienTai _nguoiDung;

    public DichVuCaptcha(IAppDbContext db, IDongHoHeThong dongHo, INguoiDungHienTai nguoiDung)
    {
        _db = db;
        _dongHo = dongHo;
        _nguoiDung = nguoiDung;
    }

    /// <summary>Sinh thu thach moi.</summary>
    public async Task<ThuThachCaptchaDto> TaoAsync(CancellationToken ct = default)
    {
        var loiGiai = TaoChuoiNgauNhien();
        var id = Guid.NewGuid().ToString("N");

        _db.MaXacThucTam.Add(new MaXacThucTam
        {
            Loai = LoaiMaXacThucTam.Captcha,
            Khoa = id,
            MaBam = Bam(loiGiai),
            HetHan = _dongHo.BayGio.AddMinutes(SoPhutHieuLuc),
            DiaChiIp = _nguoiDung.DiaChiIp
        });

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new ThuThachCaptchaDto(id, VeSvg(loiGiai));
    }

    /// <summary>
    /// Doi chieu loi giai. Dung hay sai thi thu thach cung bi TIEU HUY.
    ///
    /// Tieu huy ca khi sai la co y: giu lai thi ke tan cong doan di doan lai tren cung mot anh
    /// cho den khi trung — chi 32^5 kha nang, doan tu dong vai giay la ra.
    /// </summary>
    public async Task<bool> KiemTraAsync(string? id, string? loiGiai, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(loiGiai))
        {
            return false;
        }

        var thuThach = await _db.MaXacThucTam
            .FirstOrDefaultAsync(
                x => x.Loai == LoaiMaXacThucTam.Captcha && x.Khoa == id, ct)
            .ConfigureAwait(false);

        if (thuThach is null || !thuThach.ConHieuLuc(_dongHo.BayGio))
        {
            return false;
        }

        thuThach.DaDung = true;

        var dung = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(thuThach.MaBam),
            Encoding.UTF8.GetBytes(Bam(loiGiai)));

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return dung;
    }

    /// <summary>Don dep ma het han — goi tu cong viec nen dinh ky.</summary>
    public async Task<int> DonDepAsync(CancellationToken ct = default)
    {
        var het = await _db.MaXacThucTam
            .Where(x => x.HetHan < _dongHo.BayGio)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        _db.MaXacThucTam.RemoveRange(het);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return het.Count;
    }

    // ------------------------------------------------------------------------------

    private static string TaoChuoiNgauNhien()
    {
        var ky = new char[SoKyTu];
        for (var i = 0; i < ky.Length; i++)
        {
            ky[i] = BangKyTu[RandomNumberGenerator.GetInt32(BangKyTu.Length)];
        }

        return new string(ky);
    }

    /// <summary>So sanh khong phan biet hoa thuong — nguoi dung khong doan duoc kieu chu tren anh.</summary>
    private static string Bam(string loiGiai)
        => Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(loiGiai.Trim().ToUpperInvariant())));

    /// <summary>
    /// Ve anh SVG: nen nhieu, moi ky tu xoay va lech ngau nhien, them vai duong cat ngang.
    /// Muc do lam nhieu vua du chong doc tu dong don gian ma nguoi van doc duoc.
    /// </summary>
    private static string VeSvg(string loiGiai)
    {
        const int rong = 160;
        const int cao = 56;

        var sb = new StringBuilder();
        sb.Append(
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{rong}\" height=\"{cao}\" "
            + $"viewBox=\"0 0 {rong} {cao}\" role=\"img\" aria-label=\"Mã xác nhận\">");
        sb.Append($"<rect width=\"{rong}\" height=\"{cao}\" fill=\"#f5f5f5\"/>");

        // Cham nhieu nen.
        for (var i = 0; i < 40; i++)
        {
            sb.Append(
                $"<circle cx=\"{RandomNumberGenerator.GetInt32(rong)}\" "
                + $"cy=\"{RandomNumberGenerator.GetInt32(cao)}\" r=\"1\" fill=\"#c8c8c8\"/>");
        }

        var buocX = rong / (loiGiai.Length + 1);

        for (var i = 0; i < loiGiai.Length; i++)
        {
            var x = buocX * (i + 1);
            var y = cao / 2 + 8 + RandomNumberGenerator.GetInt32(-5, 6);
            var xoay = RandomNumberGenerator.GetInt32(-28, 29);
            var mau = new[] { "#1677ff", "#003eb3", "#531dab", "#c41d7f", "#237804" }[
                RandomNumberGenerator.GetInt32(5)];

            sb.Append(
                $"<text x=\"{x}\" y=\"{y}\" font-family=\"monospace\" font-size=\"28\" "
                + $"font-weight=\"700\" fill=\"{mau}\" "
                + $"transform=\"rotate({xoay} {x} {y})\">{loiGiai[i]}</text>");
        }

        // Duong cat ngang.
        for (var i = 0; i < 3; i++)
        {
            sb.Append(
                $"<line x1=\"0\" y1=\"{RandomNumberGenerator.GetInt32(cao)}\" "
                + $"x2=\"{rong}\" y2=\"{RandomNumberGenerator.GetInt32(cao)}\" "
                + "stroke=\"#8c8c8c\" stroke-width=\"1\" opacity=\"0.6\"/>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }
}
