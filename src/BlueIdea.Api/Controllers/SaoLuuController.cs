using System.Globalization;
using BlueIdea.Api.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Shared.KetQua;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueIdea.Api.Controllers;

/// <summary>Một bản sao lưu đã có trên máy chủ.</summary>
public sealed record BanSaoLuuDto(
    string Ten,
    DateTimeOffset ThoiGian,
    long KichThuocByte,
    bool CoCsdl,
    bool CoTepDinhKem,
    bool DayDu);

/// <summary>Tình trạng sao lưu tổng thể.</summary>
public sealed record TinhTrangSaoLuuDto(
    bool DaCauHinh,
    string ThuMuc,
    int SoBan,
    long TongKichThuocByte,
    DateTimeOffset? LanGanNhat,
    double? SoGioTuLanGanNhat,
    string MucCanhBao,
    string ThongDiep,
    IReadOnlyList<BanSaoLuuDto> DanhSach);

/// <summary>
/// Theo dõi tình trạng sao lưu dữ liệu.
///
/// Chỉ ĐỌC danh sách bản sao lưu, không có nút tạo và không có nút khôi phục trên web. Hai việc
/// đó chạy bằng <c>deploy/sao-luu-blueidea.sh</c> trên máy chủ vì:
/// khôi phục ghi đè toàn bộ cơ sở dữ liệu đang chạy — một cú bấm nhầm trên trình duyệt là mất
/// dữ liệu thật; và để tạo bản sao, tiến trình API sẽ phải gọi được docker trên máy chủ, tức là
/// mở cho ứng dụng web quyền tương đương root của máy đó.
/// </summary>
[ApiController]
[Route("api/v1/sao-luu")]
[Authorize(Policy = MaQuyen.CauHinhXem)]
[Produces("application/json")]
public sealed class SaoLuuController : ControllerBase
{
    /// <summary>Quá mốc này mà chưa có bản sao lưu mới thì coi là bất thường.</summary>
    private const double SoGioCanhBao = 48;

    private readonly IConfiguration _cauHinh;

    public SaoLuuController(IConfiguration cauHinh) => _cauHinh = cauHinh;

    [HttpGet]
    public IActionResult LayTinhTrang()
    {
        var thuMuc = _cauHinh["SaoLuu:ThuMuc"]
            ?? Environment.GetEnvironmentVariable("THU_MUC_SAO_LUU")
            ?? "/var/backups/blueidea";

        if (!Directory.Exists(thuMuc))
        {
            return Ok(PhanHoiApi<TinhTrangSaoLuuDto>.Ok(new TinhTrangSaoLuuDto(
                false, thuMuc, 0, 0, null, null, "CHUA_CAU_HINH",
                "Chưa gắn thư mục sao lưu vào máy chủ ứng dụng nên không đọc được tình trạng. "
                + "Sao lưu vẫn có thể đang chạy trên máy chủ — kiểm tra bằng lệnh sao-luu-blueidea.sh liet-ke.",
                Array.Empty<BanSaoLuuDto>())));
        }

        var danhSach = new List<BanSaoLuuDto>();

        foreach (var duong in Directory.EnumerateDirectories(thuMuc))
        {
            var ten = Path.GetFileName(duong);

            var tep = Directory.EnumerateFiles(duong, "*", SearchOption.AllDirectories).ToList();
            var kichThuoc = tep.Sum(x => new FileInfo(x).Length);

            var coCsdl = tep.Any(x => x.EndsWith(".dump", StringComparison.OrdinalIgnoreCase)
                                      || x.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
                                      || x.EndsWith(".sql.gz", StringComparison.OrdinalIgnoreCase));

            var coTep = tep.Any(x => x.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase)
                                     || x.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase));

            danhSach.Add(new BanSaoLuuDto(
                ten,
                DocThoiGian(ten, duong),
                kichThuoc,
                coCsdl,
                coTep,
                // Thieu mot trong hai phan la ban sao luu KHONG khoi phuc ra he thong chay duoc:
                // ho so trong CSDL tro toi tep dinh kem, mat tep thi ho so rong ruot.
                coCsdl && coTep));
        }

        danhSach = danhSach.OrderByDescending(x => x.ThoiGian).ToList();

        var ganNhat = danhSach.FirstOrDefault();
        var soGio = ganNhat is null
            ? (double?)null
            : (DateTimeOffset.UtcNow - ganNhat.ThoiGian).TotalHours;

        var (mucCanhBao, thongDiep) = ganNhat switch
        {
            null => ("NGUY_HIEM",
                "Chưa có bản sao lưu nào trong thư mục. Chạy sao-luu-blueidea.sh sao-luu ngay."),
            _ when soGio > SoGioCanhBao => ("CANH_BAO",
                $"Bản sao lưu gần nhất đã {soGio:0} giờ trước — kiểm tra lịch chạy tự động."),
            _ when !ganNhat.DayDu => ("CANH_BAO",
                "Bản gần nhất thiếu cơ sở dữ liệu hoặc kho tệp đính kèm nên chưa khôi phục đủ được."),
            _ => ("BINH_THUONG", "Sao lưu đang chạy đúng lịch."),
        };

        return Ok(PhanHoiApi<TinhTrangSaoLuuDto>.Ok(new TinhTrangSaoLuuDto(
            true,
            thuMuc,
            danhSach.Count,
            danhSach.Sum(x => x.KichThuocByte),
            ganNhat?.ThoiGian,
            soGio,
            mucCanhBao,
            thongDiep,
            danhSach)));
    }

    /// <summary>
    /// Thời điểm của bản sao lưu, ưu tiên đọc từ tên thư mục dạng yyyyMMdd-HHmmss.
    ///
    /// Ngày sửa thư mục không đáng tin: chép bản sao lưu sang ổ khác là mốc đó đổi theo lần chép,
    /// khiến bản cũ trông như vừa tạo và cảnh báo quá hạn không bao giờ bật.
    /// </summary>
    private static DateTimeOffset DocThoiGian(string ten, string duong)
        => DateTime.TryParseExact(ten, "yyyyMMdd-HHmmss", CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var ngay)
            ? new DateTimeOffset(ngay, TimeSpan.Zero)
            : new DateTimeOffset(Directory.GetLastWriteTimeUtc(duong), TimeSpan.Zero);
}
