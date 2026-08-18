using System.Security.Cryptography;
using BlueIdea.Api.Chung;
using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.SangKien;
using BlueIdea.Infrastructure.DichVu;
using BlueIdea.Shared.KetQua;
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
public sealed partial class TepTinController : ControllerBase
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

    /// <summary>
    /// Định dạng mở inline được trong trình duyệt.
    ///
    /// Cố ý KHÔNG có .html và .svg: cả hai chạy được script, mở inline dưới tên miền hệ thống là
    /// để mã của người tải lên đọc phiên đăng nhập của người xem.
    /// </summary>
    private static readonly HashSet<string> XemTruocDuoc =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".png", ".jpg", ".jpeg", ".gif", ".webp" };

    private static string MimeXemTruoc(string phanMoRong) => phanMoRong switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        _ => "image/jpeg",
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
    private readonly IDichVuPhanQuyen _phanQuyen;
    private readonly IDichVuCauHinh _cauHinh;
    private readonly IDongHoHeThong _dongHo;
    private readonly IDichVuOcr _ocr;
    private readonly IHangDoiCongViecNen _hangDoi;
    private readonly IDichVuQuetVirus _quetVirus;

    public TepTinController(
        IAppDbContext db, ILuuTruTep luuTru, INguoiDungHienTai nguoiDung,
        IDichVuPhanQuyen phanQuyen, IDichVuCauHinh cauHinh, IDongHoHeThong dongHo,
        IDichVuOcr ocr, IHangDoiCongViecNen hangDoi, IDichVuQuetVirus quetVirus)
    {
        _db = db;
        _luuTru = luuTru;
        _nguoiDung = nguoiDung;
        _phanQuyen = phanQuyen;
        _cauHinh = cauHinh;
        _dongHo = dongHo;
        _ocr = ocr;
        _hangDoi = hangDoi;
        _quetVirus = quetVirus;
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

        // Quét mã độc TRƯỚC khi ghi xuống kho lưu trữ — tệp nhiễm không được phép chạm vào
        // nơi lưu trữ dù chỉ trong chốc lát (chức năng 25).
        bo.Position = 0;
        var ketQuaQuet = await _quetVirus.QuetAsync(bo, ct).ConfigureAwait(false);

        if (ketQuaQuet.Nhiem)
        {
            throw new NghiepVuException(Shared.KetQua.MaLoiHeThong.TepChuaMaDoc,
                $"Tệp '{tep.FileName}' chứa mã độc ({ketQuaQuet.TenMaDoc}) nên đã bị từ chối.");
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
                NgayTaiLen = _dongHo.BayGio,
                DaQuetVirus = ketQuaQuet.Sach
            };

            _db.TepTin.Add(tepTin);
        }

        if (sangKienId.HasValue && !string.IsNullOrWhiteSpace(thanhPhanHoSoMa))
        {
            await BatBuocHoSoTrongPhamViAsync(sangKienId.Value, ct).ConfigureAwait(false);

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

            /*
             * Gioi han so tep moi ho so.
             *
             * Truoc day khoa SO_TEP_TOI_DA duoc seed, hien tren man hinh cau hinh, sua duoc va luu
             * duoc — nhung khong noi nao doc, nen quan tri vien dat gioi han 20 tep roi nguoi dung
             * van tai len bao nhieu cung duoc. O ngay ben canh (dung luong toi da) thi lai co hieu
             * luc, nen khong ai nghi rang o nay khong chay.
             *
             * Dat 0 de bo gioi han — cung quy uoc voi cac nguong khac trong he thong.
             */
            var soTepToiDa = await _cauHinh
                .LayAsync(KhoaCauHinh.SoTepToiDa, 20, ct)
                .ConfigureAwait(false);

            if (soTepToiDa > 0 && soHienCo >= soTepToiDa)
            {
                throw new NghiepVuException(Shared.KetQua.MaLoiHeThong.VuotDungLuongToiDa,
                    $"Hồ sơ chỉ cho phép tối đa {soTepToiDa} tệp, hiện đã có {soHienCo}. "
                    + "Hãy xoá bớt tệp không cần thiết trước khi tải thêm.");
            }

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

    /// <summary>
    /// Sinh liên kết tải xuống có thời hạn cho một tệp.
    ///
    /// Với kho MinIO đây là presigned URL: trình duyệt tải thẳng từ kho đối tượng, luồng tệp
    /// không phải đi xuyên qua tiến trình API. Với kho đĩa cục bộ là liên kết có ký HMAC và có
    /// hạn, kiểm tra ở <see cref="TaiXuongTheoLienKetAsync"/>.
    ///
    /// Quyền được kiểm tra Ở ĐÂY, lúc phát liên kết — sau đó liên kết tự đứng một mình, nên
    /// thời hạn để ngắn (mặc định 10 phút) thay vì hàng giờ.
    /// </summary>
    [HttpGet("{id:guid}/lien-ket-tai-xuong")]
    public async Task<IActionResult> LayLienKetTaiXuongAsync(
        Guid id, [FromQuery] int soPhut = 10, CancellationToken ct = default)
    {
        await BatBuocTepTrongPhamViAsync(id, ct).ConfigureAwait(false);

        var tepTin = await _db.TepTin.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("tệp tin", id);

        var url = await _luuTru.TaoUrlCoThoiHanAsync(
            tepTin.Bucket, tepTin.DuongDan, TimeSpan.FromMinutes(Math.Clamp(soPhut, 1, 60)), ct);

        return Ok(PhanHoiApi<object>.Ok(new
        {
            url,
            tenGoc = tepTin.TenGoc,
            hetHanSauPhut = Math.Clamp(soPhut, 1, 60)
        }));
    }

    /// <summary>
    /// Tải tệp bằng liên kết đã ký — dùng cho kho đĩa cục bộ.
    ///
    /// Không đòi đăng nhập vì liên kết tự mang chữ ký và thời hạn: đó chính là điểm của liên kết
    /// có thời hạn (nhúng được vào thẻ img, mở được ở tab mới). Chữ ký sai hoặc quá hạn thì trả
    /// 404 chứ không phải 403 — báo "hết hạn" cho một đường dẫn đoán mò cũng là xác nhận tệp đó
    /// có thật.
    /// </summary>
    [HttpGet("tai-xuong")]
    [AllowAnonymous]
    public async Task<IActionResult> TaiXuongTheoLienKetAsync(
        [FromQuery] string bucket, [FromQuery] string duongDan, [FromQuery] long hetHan,
        [FromQuery] string chuKy, CancellationToken ct)
    {
        if (_luuTru is not LuuTruTepCucBo cucBo || !cucBo.KiemTraChuKy(bucket, duongDan, hetHan, chuKy))
        {
            return NotFound();
        }

        var tepTin = await _db.TepTin.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Bucket == bucket && x.DuongDan == duongDan, ct)
            .ConfigureAwait(false);

        if (tepTin is null) return NotFound();

        var luong = await _luuTru.TaiXuongAsync(bucket, duongDan, ct).ConfigureAwait(false);

        return File(luong, tepTin.MimeType ?? "application/octet-stream", tepTin.TenGoc);
    }

    /// <summary>
    /// Xem trước tệp ngay trong trình duyệt (không tải về).
    ///
    /// Chỉ mở inline các định dạng trình duyệt tự dựng được và KHÔNG chạy mã của tệp: PDF và ảnh.
    /// Mở inline một tệp HTML/SVG do người dùng tải lên là mở đường cho mã chạy dưới tên miền của
    /// hệ thống, đọc được phiên đăng nhập của người xem.
    /// </summary>
    [HttpGet("{id:guid}/xem-truoc")]
    public async Task<IActionResult> XemTruocAsync(Guid id, CancellationToken ct)
    {
        await BatBuocTepTrongPhamViAsync(id, ct).ConfigureAwait(false);

        var tepTin = await _db.TepTin.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("tệp tin", id);

        var phanMoRong = (tepTin.PhanMoRong ?? string.Empty).ToLowerInvariant();

        if (!XemTruocDuoc.Contains(phanMoRong))
        {
            throw new NghiepVuException(MaLoiHeThong.TepKhongHopLe,
                $"Định dạng {phanMoRong} không xem trước được trong trình duyệt — hãy tải về.");
        }

        var luong = await _luuTru.TaiXuongAsync(tepTin.Bucket, tepTin.DuongDan, ct)
            .ConfigureAwait(false);

        // Chan trinh duyet tu doan lai kieu noi dung: mot tep .png chua ma HTML ma bi doan thanh
        // text/html se duoc chay nhu trang web.
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.ContentDisposition = $"inline; filename=\"{tepTin.Id}{phanMoRong}\"";

        return File(luong, MimeXemTruoc(phanMoRong));
    }

    [HttpGet("{id:guid}/tai-ve")]
    public async Task<IActionResult> TaiVeAsync(Guid id, CancellationToken ct)
    {
        await BatBuocTepTrongPhamViAsync(id, ct).ConfigureAwait(false);

        var tepTin = await _db.TepTin.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("tệp tin", id);

        var luong = await _luuTru
            .TaiXuongAsync(tepTin.Bucket, tepTin.DuongDan, ct)
            .ConfigureAwait(false);

        return File(luong, tepTin.MimeType ?? "application/octet-stream", tepTin.TenGoc);
    }

    [HttpDelete("dinh-kem/{id:guid}")]
    public async Task<IActionResult> GoDinhKemAsync(Guid id, CancellationToken ct)
    {
        var dinhKem = await _db.SangKienTepDinhKem
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("tệp đính kèm", id);

        await BatBuocHoSoTrongPhamViAsync(dinhKem.SangKienId, ct).ConfigureAwait(false);

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

    private async Task BatBuocTepTrongPhamViAsync(Guid tepTinId, CancellationToken ct)
    {
        if (_nguoiDung.Id is null)
            throw new KhongTimThayException("tệp tin", tepTinId);

        var danhSachDinhKem = await _db.SangKienTepDinhKem.AsNoTracking()
            .Where(x => x.TepTinId == tepTinId)
            .Select(x => x.SangKienId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (danhSachDinhKem.Count == 0)
        {
            var tepTin = await _db.TepTin.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == tepTinId, ct)
                .ConfigureAwait(false);
            if (tepTin is not null && tepTin.NguoiTaiLenId != _nguoiDung.Id)
                throw new KhongTimThayException("tệp tin", tepTinId);
            return;
        }

        foreach (var sangKienId in danhSachDinhKem)
        {
            try
            {
                await BatBuocHoSoTrongPhamViAsync(sangKienId, ct).ConfigureAwait(false);
                return;
            }
            catch (KhongTimThayException)
            {
            }
        }

        throw new KhongTimThayException("tệp tin", tepTinId);
    }

    private async Task BatBuocHoSoTrongPhamViAsync(Guid sangKienId, CancellationToken ct)
    {
        if (_nguoiDung.Id is null)
            throw new KhongTimThayException("hồ sơ sáng kiến", sangKienId);

        var nguoiDungId = _nguoiDung.Id.Value;
        var hoSo = await _db.SangKien.AsNoTracking()
            .Include(x => x.DanhSachTacGia)
            .FirstOrDefaultAsync(x => x.Id == sangKienId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("hồ sơ sáng kiến", sangKienId);

        var phamVi = await _phanQuyen.LayPhamViTruyCapAsync(nguoiDungId, ct).ConfigureAwait(false);

        if (phamVi.ToanHeThong) return;

        var laTacGia = hoSo.NguoiTaoId == nguoiDungId
                       || hoSo.DanhSachTacGia.Any(t => t.NguoiDungId == nguoiDungId);

        if (phamVi.ChiCaNhan)
        {
            if (!laTacGia) throw new KhongTimThayException("tệp tin", sangKienId);
            return;
        }

        var trongDonVi = hoSo.DonViId.HasValue && phamVi.DonViIds.Contains(hoSo.DonViId.Value);
        if (!laTacGia && !trongDonVi)
            throw new KhongTimThayException("tệp tin", sangKienId);
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
