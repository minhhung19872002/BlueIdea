using System.Security.Cryptography;
using BlueIdea.Api.Chung;
using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.SangKien;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Api.Controllers;

public sealed record TepTinDto(
    Guid Id, string TenGoc, long KichThuoc, string? MimeType, string? PhanMoRong,
    string HashSha256, DateTimeOffset NgayTaiLen);

/// <summary>Chức năng 25 — Tải lên, tải xuống, xoá tệp đính kèm.</summary>
[ApiController]
[Route("api/v1/tep-tin")]
[Authorize]
[Produces("application/json")]
public sealed class TepTinController : ControllerBase
{
    /// <summary>
    /// "Magic number" của các định dạng được phép — KHÔNG tin phần mở rộng do client gửi
    /// (yêu cầu Mục 5 — chức năng 25).
    /// </summary>
    private static readonly Dictionary<string, byte[][]> ChuKyTep = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = new[] { "%PDF"u8.ToArray() },
        [".jpg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        [".png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
        // DOCX/XLSX/PPTX/ZIP đều là gói ZIP.
        [".docx"] = new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } },
        [".xlsx"] = new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } },
        [".pptx"] = new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } },
        [".zip"] = new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } },
        // Office 97-2003 (OLE compound file).
        [".doc"] = new[] { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } },
        [".xls"] = new[] { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } },
        [".ppt"] = new[] { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } }
    };

    /// <summary>Phần mở rộng bị chặn tuyệt đối (tệp thực thi / script).</summary>
    private static readonly HashSet<string> PhanMoRongCam = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".bat", ".cmd", ".com", ".msi", ".scr", ".ps1", ".sh",
        ".js", ".jar", ".vbs", ".wsf", ".hta", ".reg", ".lnk", ".php", ".asp", ".aspx"
    };

    private readonly IAppDbContext _db;
    private readonly ILuuTruTep _luuTru;
    private readonly INguoiDungHienTai _nguoiDung;
    private readonly IDichVuCauHinh _cauHinh;
    private readonly IDongHoHeThong _dongHo;
    private readonly IDichVuOcr _ocr;
    private readonly IHangDoiCongViecNen _hangDoi;

    public TepTinController(
        IAppDbContext db, ILuuTruTep luuTru, INguoiDungHienTai nguoiDung,
        IDichVuCauHinh cauHinh, IDongHoHeThong dongHo,
        IDichVuOcr ocr, IHangDoiCongViecNen hangDoi)
    {
        _db = db;
        _luuTru = luuTru;
        _nguoiDung = nguoiDung;
        _cauHinh = cauHinh;
        _dongHo = dongHo;
        _ocr = ocr;
        _hangDoi = hangDoi;
    }

    /// <summary>Tải tệp lên và gắn vào một thành phần hồ sơ của sáng kiến.</summary>
    [HttpPost("tai-len")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> TaiLenAsync(
        IFormFile tep,
        [FromForm] Guid? sangKienId,
        [FromForm] string? thanhPhanHoSoMa,
        [FromForm] string? moTa,
        CancellationToken ct)
    {
        if (tep is null || tep.Length == 0)
        {
            throw new NghiepVuException(Shared.KetQua.MaLoiHeThong.TepKhongHopLe, "Chưa chọn tệp.");
        }

        var phanMoRong = Path.GetExtension(tep.FileName);

        if (string.IsNullOrEmpty(phanMoRong) || PhanMoRongCam.Contains(phanMoRong))
        {
            throw new NghiepVuException(Shared.KetQua.MaLoiHeThong.TepKhongHopLe,
                $"Không cho phép tải lên tệp có phần mở rộng '{phanMoRong}'.");
        }

        var dungLuongToiDaMb = await _cauHinh
            .LayAsync(KhoaCauHinh.DungLuongTepToiDaMb, 20, ct)
            .ConfigureAwait(false);

        if (tep.Length > (long)dungLuongToiDaMb * 1024 * 1024)
        {
            throw new NghiepVuException(Shared.KetQua.MaLoiHeThong.VuotDungLuongToiDa,
                $"Tệp vượt quá dung lượng tối đa {dungLuongToiDaMb}MB.");
        }

        // Đọc vào bộ nhớ để kiểm tra magic number và tính SHA-256.
        await using var bo = new MemoryStream();
        await tep.CopyToAsync(bo, ct).ConfigureAwait(false);
        var duLieu = bo.ToArray();

        if (!KiemTraChuKyTep(phanMoRong, duLieu))
        {
            throw new NghiepVuException(Shared.KetQua.MaLoiHeThong.TepKhongHopLe,
                $"Nội dung tệp không khớp với định dạng '{phanMoRong}'.");
        }

        var hash = Convert.ToHexString(SHA256.HashData(duLieu)).ToLowerInvariant();

        // Trùng nội dung: dùng lại bản ghi đã có thay vì lưu thêm bản sao.
        var daCo = await _db.TepTin
            .FirstOrDefaultAsync(x => x.HashSha256 == hash, ct)
            .ConfigureAwait(false);

        TepTin tepTin;

        if (daCo is not null)
        {
            tepTin = daCo;
        }
        else
        {
            var tenLuuTru = $"{Guid.NewGuid():N}{phanMoRong}";
            bo.Position = 0;

            var duongDan = await _luuTru
                .TaiLenAsync(bo, tenLuuTru, tep.ContentType, "blueidea", ct)
                .ConfigureAwait(false);

            tepTin = new TepTin
            {
                Id = Guid.NewGuid(),
                TenGoc = Path.GetFileName(tep.FileName),
                TenLuuTru = tenLuuTru,
                DuongDan = duongDan,
                Bucket = "blueidea",
                KichThuoc = tep.Length,
                MimeType = tep.ContentType,
                PhanMoRong = phanMoRong,
                HashSha256 = hash,
                NguoiTaiLenId = _nguoiDung.Id,
                NgayTaiLen = _dongHo.BayGio
            };

            _db.TepTin.Add(tepTin);
        }

        if (sangKienId.HasValue && !string.IsNullOrWhiteSpace(thanhPhanHoSoMa))
        {
            var hoSo = await _db.SangKien
                .FirstOrDefaultAsync(x => x.Id == sangKienId.Value, ct)
                .ConfigureAwait(false)
                ?? throw new KhongTimThayException("hồ sơ sáng kiến", sangKienId.Value);

            if (!hoSo.ChoPhepSua())
            {
                throw new NghiepVuException(Shared.KetQua.MaLoiHeThong.TrangThaiKhongChoPhepSua,
                    $"Hồ sơ ở trạng thái '{hoSo.TrangThaiTong}' nên không thể thêm tệp.");
            }

            var soHienCo = await _db.SangKienTepDinhKem
                .CountAsync(x => x.SangKienId == hoSo.Id, ct)
                .ConfigureAwait(false);

            _db.SangKienTepDinhKem.Add(new SangKienTepDinhKem
            {
                Id = Guid.NewGuid(),
                SangKienId = hoSo.Id,
                TepTinId = tepTin.Id,
                ThanhPhanHoSoMa = thanhPhanHoSoMa,
                MoTa = moTa,
                ThuTu = soHienCo + 1
            });
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Trich xuat van ban chay NEN de khong keo dai thoi gian tai len: OCR mot PDF scan
        // co the mat vai phut.
        //
        // Dieu kien la TRANG THAI OCR chu khong phai "tep moi": mot tep trung noi dung duoc dung
        // lai van co the chua tung duoc trich xuat (vi du duoc tai len tu truoc khi bat OCR).
        // Trang thai HOAN_THANH / KHONG_CAN / LOI deu khong xep lich lai.
        if (_ocr.HoTro(tepTin.PhanMoRong) && tepTin.TrangThaiOcr == TrangThaiOcrTep.ChuaXuLy)
        {
            _hangDoi.XepLichTrichXuatVanBan(tepTin.Id);
        }

        return Ok(PhanHoiApi<TepTinDto>.Ok(
            new TepTinDto(tepTin.Id, tepTin.TenGoc, tepTin.KichThuoc, tepTin.MimeType,
                tepTin.PhanMoRong, tepTin.HashSha256 ?? string.Empty, tepTin.NgayTaiLen),
            "Tải tệp lên thành công"));
    }

    /// <summary>Tải tệp về (kiểm tra quyền trên từng tệp — chống IDOR).</summary>
    [HttpGet("{id:guid}/tai-ve")]
    public async Task<IActionResult> TaiVeAsync(Guid id, CancellationToken ct)
    {
        var tepTin = await _db.TepTin.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("tệp tin", id);

        var luong = await _luuTru
            .TaiXuongAsync(tepTin.Bucket, tepTin.DuongDan, ct)
            .ConfigureAwait(false);

        return File(luong, tepTin.MimeType ?? "application/octet-stream", tepTin.TenGoc);
    }

    /// <summary>Gỡ tệp khỏi hồ sơ (xoá mềm bản ghi đính kèm, giữ nguyên tệp gốc).</summary>
    [HttpDelete("dinh-kem/{id:guid}")]
    public async Task<IActionResult> GoDinhKemAsync(Guid id, CancellationToken ct)
    {
        var dinhKem = await _db.SangKienTepDinhKem
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("tệp đính kèm", id);

        var hoSo = await _db.SangKien
            .FirstOrDefaultAsync(x => x.Id == dinhKem.SangKienId, ct)
            .ConfigureAwait(false);

        if (hoSo is not null && !hoSo.ChoPhepSua())
        {
            throw new NghiepVuException(Shared.KetQua.MaLoiHeThong.TrangThaiKhongChoPhepSua,
                $"Hồ sơ ở trạng thái '{hoSo.TrangThaiTong}' nên không thể gỡ tệp.");
        }

        dinhKem.DaXoa = true;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return Ok(PhanHoiApi.Ok("Đã gỡ tệp đính kèm"));
    }

    private static bool KiemTraChuKyTep(string phanMoRong, byte[] duLieu)
    {
        if (!ChuKyTep.TryGetValue(phanMoRong, out var cacChuKy))
        {
            // Định dạng không nằm trong danh sách nhận diện được -> từ chối cho an toàn.
            return false;
        }

        return cacChuKy.Any(chuKy =>
            duLieu.Length >= chuKy.Length && duLieu.AsSpan(0, chuKy.Length).SequenceEqual(chuKy));
    }
}
