using System.Collections.Concurrent;
using BlueIdea.Api.Chung;
using BlueIdea.Application.Chung;
using BlueIdea.Shared.KetQua;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueIdea.Api.Controllers;

public sealed record BatDauTaiManhDto(string TenTep, long TongKichThuoc, int SoManh);

public sealed record PhienTaiManhDto(
    Guid PhienId, int SoManh, int KichThuocManhToiDa, IReadOnlyList<int> ManhDaNhan);

/// <summary>
/// Chức năng 25 — Tải tệp lớn theo từng mảnh.
///
/// Vì sao cần: đường truyền ở nhiều đơn vị chập chờn, một tệp 80MB gửi trong MỘT yêu cầu mà rớt
/// ở phút thứ ba là mất trắng, người dùng phải gửi lại từ đầu. Chia mảnh cho phép gửi tiếp đúng
/// những mảnh còn thiếu.
///
/// Mảnh được ghi xuống thư mục tạm chứ không giữ trong bộ nhớ: giữ cả tệp 80MB × nhiều người
/// tải cùng lúc trong RAM là cách nhanh nhất để hạ tiến trình API.
///
/// Sau khi ghép xong, tệp đi qua ĐÚNG đường kiểm tra như tải lên thường (chữ ký định dạng, quét
/// mã độc, khử trùng nội dung) — chia mảnh không được trở thành lối vòng qua các bước đó.
/// </summary>
public sealed partial class TepTinController
{
    /// <summary>Mỗi mảnh tối đa 5MB — đủ nhỏ để gửi lại nhanh khi rớt mạng.</summary>
    public const int KichThuocManhToiDa = 5 * 1024 * 1024;

    private const long TongKichThuocToiDa = 100L * 1024 * 1024;
    private const int SoManhToiDa = 200;

    /// <summary>Phiên tải dở quá thời gian này thì bị dọn — tránh rác chiếm đĩa vô hạn.</summary>
    private static readonly TimeSpan ThoiGianSongPhien = TimeSpan.FromHours(6);

    private static readonly ConcurrentDictionary<Guid, PhienTaiManh> CacPhien = new();

    /// <summary>Mở một phiên tải theo mảnh.</summary>
    [HttpPost("manh/bat-dau")]
    public IActionResult BatDau([FromBody] BatDauTaiManhDto duLieu)
    {
        ArgumentNullException.ThrowIfNull(duLieu);
        DonPhienCu();

        if (duLieu.TongKichThuoc is <= 0 or > TongKichThuocToiDa)
        {
            throw new NghiepVuException(MaLoiHeThong.VuotDungLuongToiDa,
                $"Tệp phải lớn hơn 0 và không quá {TongKichThuocToiDa / 1024 / 1024}MB.");
        }

        if (duLieu.SoManh is <= 0 or > SoManhToiDa)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                $"Số mảnh phải từ 1 đến {SoManhToiDa}.");
        }

        var phien = new PhienTaiManh(
            Guid.NewGuid(),
            _nguoiDung.Id ?? Guid.Empty,
            Path.GetFileName(duLieu.TenTep),
            duLieu.TongKichThuoc,
            duLieu.SoManh,
            _dongHo.BayGio);

        Directory.CreateDirectory(phien.ThuMuc);
        CacPhien[phien.Id] = phien;

        return Ok(PhanHoiApi<PhienTaiManhDto>.Ok(
            new PhienTaiManhDto(phien.Id, phien.SoManh, KichThuocManhToiDa, Array.Empty<int>()),
            "Đã mở phiên tải tệp"));
    }

    /// <summary>Gửi một mảnh. Gửi lại cùng chỉ số mảnh là ghi đè — dùng khi mảnh đó lỗi.</summary>
    [HttpPost("manh/{phienId:guid}/{chiSo:int}")]
    [RequestSizeLimit(KichThuocManhToiDa + 1024)]
    public async Task<IActionResult> GuiManhAsync(
        Guid phienId, int chiSo, IFormFile manh, CancellationToken ct)
    {
        var phien = LayPhienCuaToi(phienId);

        if (chiSo < 0 || chiSo >= phien.SoManh)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                $"Chỉ số mảnh phải từ 0 đến {phien.SoManh - 1}.");
        }

        if (manh is null || manh.Length == 0)
        {
            throw new NghiepVuException(MaLoiHeThong.TepKhongHopLe, "Mảnh rỗng.");
        }

        if (manh.Length > KichThuocManhToiDa)
        {
            throw new NghiepVuException(MaLoiHeThong.VuotDungLuongToiDa,
                $"Mỗi mảnh không quá {KichThuocManhToiDa / 1024 / 1024}MB.");
        }

        var duongDan = phien.DuongDanManh(chiSo);

        await using (var ra = System.IO.File.Create(duongDan))
        {
            await manh.CopyToAsync(ra, ct).ConfigureAwait(false);
        }

        phien.DaNhan.Add(chiSo);

        return Ok(PhanHoiApi<PhienTaiManhDto>.Ok(
            new PhienTaiManhDto(phien.Id, phien.SoManh, KichThuocManhToiDa,
                phien.DaNhan.OrderBy(x => x).ToList())));
    }

    /// <summary>Tình trạng phiên — client dùng để biết còn thiếu mảnh nào sau khi mất mạng.</summary>
    [HttpGet("manh/{phienId:guid}")]
    public IActionResult LayTinhTrang(Guid phienId)
    {
        var phien = LayPhienCuaToi(phienId);

        return Ok(PhanHoiApi<PhienTaiManhDto>.Ok(
            new PhienTaiManhDto(phien.Id, phien.SoManh, KichThuocManhToiDa,
                phien.DaNhan.OrderBy(x => x).ToList())));
    }

    /// <summary>
    /// Ghép các mảnh thành tệp hoàn chỉnh rồi đưa qua ĐÚNG đường tải lên thường.
    ///
    /// Gọi thẳng <see cref="TaiLenAsync"/> chứ không tự ghi vào kho: chia mảnh chỉ là cách vận
    /// chuyển, không được trở thành lối vòng qua kiểm tra chữ ký định dạng, quét mã độc và các
    /// ràng buộc hồ sơ.
    /// </summary>
    [HttpPost("manh/{phienId:guid}/hoan-tat")]
    [RequestSizeLimit(1024)]
    public async Task<IActionResult> HoanTatAsync(
        Guid phienId, [FromQuery] Guid? sangKienId, [FromQuery] string? thanhPhanHoSoMa,
        [FromQuery] string? moTa, CancellationToken ct)
    {
        var phien = LayPhienCuaToi(phienId);
        var thieu = Enumerable.Range(0, phien.SoManh).Where(i => !phien.DaNhan.Contains(i)).ToList();

        if (thieu.Count > 0)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                $"Còn thiếu {thieu.Count} mảnh: {string.Join(", ", thieu.Take(10))}"
                + (thieu.Count > 10 ? "…" : string.Empty));
        }

        var duongDanGhep = Path.Combine(phien.ThuMuc, "day-du.bin");

        await using (var ra = System.IO.File.Create(duongDanGhep))
        {
            for (var i = 0; i < phien.SoManh; i++)
            {
                await using var vao = System.IO.File.OpenRead(phien.DuongDanManh(i));
                await vao.CopyToAsync(ra, ct).ConfigureAwait(false);
            }
        }

        try
        {
            await using var luong = System.IO.File.OpenRead(duongDanGhep);
            var tep = new TepTuLuong(luong, phien.TenTep);

            return await TaiLenAsync(tep, sangKienId, thanhPhanHoSoMa, moTa, ct)
                .ConfigureAwait(false);
        }
        finally
        {
            Don(phien);
        }
    }

    /// <summary>Huỷ phiên và xoá các mảnh đã gửi.</summary>
    [HttpDelete("manh/{phienId:guid}")]
    public IActionResult Huy(Guid phienId)
    {
        Don(LayPhienCuaToi(phienId));
        return Ok(PhanHoiApi.Ok("Đã huỷ phiên tải tệp"));
    }

    private PhienTaiManh LayPhienCuaToi(Guid phienId)
    {
        if (!CacPhien.TryGetValue(phienId, out var phien))
        {
            throw new KhongTimThayException("phiên tải tệp", phienId);
        }

        // Phien chi thuoc ve nguoi mo no: khong kiem tra thi bat ky ai doan duoc Id phien deu
        // chen duoc mot manh vao tep nguoi khac dang tai.
        if (phien.NguoiTaoId != (_nguoiDung.Id ?? Guid.Empty))
        {
            throw new KhongTimThayException("phiên tải tệp", phienId);
        }

        return phien;
    }

    private static void Don(PhienTaiManh phien)
    {
        CacPhien.TryRemove(phien.Id, out _);

        try
        {
            if (Directory.Exists(phien.ThuMuc)) Directory.Delete(phien.ThuMuc, recursive: true);
        }
        catch (IOException)
        {
            // Xoa that bai chi de lai rac tam, khong duoc lam hong ket qua tai len cua nguoi dung.
        }
    }

    private void DonPhienCu()
    {
        var moc = _dongHo.BayGio - ThoiGianSongPhien;

        foreach (var phien in CacPhien.Values.Where(x => x.ThoiGianMo < moc).ToList())
        {
            Don(phien);
        }
    }

    /// <summary>Bọc một luồng tệp thành <see cref="IFormFile"/> để dùng lại đường tải lên thường.</summary>
    private sealed class TepTuLuong : IFormFile
    {
        private readonly Stream _luong;

        public TepTuLuong(Stream luong, string tenTep)
        {
            _luong = luong;
            FileName = tenTep;
            Name = "tep";
        }

        public string ContentType => "application/octet-stream";

        public string ContentDisposition => $"form-data; name=\"tep\"; filename=\"{FileName}\"";

        public IHeaderDictionary Headers { get; } = new HeaderDictionary();

        public long Length => _luong.Length;

        public string Name { get; }

        public string FileName { get; }

        public void CopyTo(Stream target)
        {
            _luong.Position = 0;
            _luong.CopyTo(target);
        }

        public async Task CopyToAsync(Stream target, CancellationToken ct = default)
        {
            _luong.Position = 0;
            await _luong.CopyToAsync(target, ct).ConfigureAwait(false);
        }

        public Stream OpenReadStream()
        {
            _luong.Position = 0;
            return _luong;
        }
    }

    private sealed class PhienTaiManh
    {
        public PhienTaiManh(
            Guid id, Guid nguoiTaoId, string tenTep, long tongKichThuoc, int soManh,
            DateTimeOffset thoiGianMo)
        {
            Id = id;
            NguoiTaoId = nguoiTaoId;
            TenTep = tenTep;
            TongKichThuoc = tongKichThuoc;
            SoManh = soManh;
            ThoiGianMo = thoiGianMo;
            ThuMuc = Path.Combine(Path.GetTempPath(), "blueidea-manh", id.ToString("N"));
        }

        public Guid Id { get; }

        public Guid NguoiTaoId { get; }

        public string TenTep { get; }

        public long TongKichThuoc { get; }

        public int SoManh { get; }

        public DateTimeOffset ThoiGianMo { get; }

        public string ThuMuc { get; }

        public HashSet<int> DaNhan { get; } = new();

        public string DuongDanManh(int chiSo) => Path.Combine(ThuMuc, $"{chiSo:D4}.manh");
    }
}
