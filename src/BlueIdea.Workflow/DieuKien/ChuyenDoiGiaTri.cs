using System.Globalization;
using System.Text.Json;

namespace BlueIdea.Workflow.DieuKien;

/// <summary>
/// Chuyen doi gia tri thuoc nhieu kieu (ke ca <see cref="JsonElement"/> doc tu jsonb)
/// ve kieu chuan de so sanh. Khong dung eval dong - chi ep kieu tuong minh.
/// </summary>
internal static class ChuyenDoiGiaTri
{
    /// <summary>Bo lop JsonElement thanh gia tri .NET nguyen thuy.</summary>
    public static object? BocJson(object? giaTri)
    {
        if (giaTri is not JsonElement phanTu)
        {
            return giaTri;
        }

        return phanTu.ValueKind switch
        {
            JsonValueKind.String => phanTu.GetString(),
            JsonValueKind.Number => phanTu.TryGetInt64(out var so) ? so : phanTu.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.Array => phanTu.EnumerateArray().Select(p => BocJson(p)).ToList(),
            JsonValueKind.Object => phanTu,
            _ => phanTu.ToString()
        };
    }

    /// <summary>Thu ep ve so thap phan. Chap nhan chuoi so voi dau cham thap phan.</summary>
    public static bool ThuLaySo(object? giaTri, out decimal so)
    {
        so = 0m;
        var thuc = BocJson(giaTri);

        switch (thuc)
        {
            case null:
                return false;
            case decimal d:
                so = d;
                return true;
            case double db:
                so = (decimal)db;
                return true;
            case float f:
                so = (decimal)f;
                return true;
            case int i:
                so = i;
                return true;
            case long l:
                so = l;
                return true;
            case short s:
                so = s;
                return true;
            case byte b:
                so = b;
                return true;
            case bool:
                return false;
            case string chuoi:
                return decimal.TryParse(chuoi, NumberStyles.Any, CultureInfo.InvariantCulture, out so);
            default:
                return false;
        }
    }

    public static bool ThuLayBool(object? giaTri, out bool ketQua)
    {
        ketQua = false;
        var thuc = BocJson(giaTri);

        switch (thuc)
        {
            case bool b:
                ketQua = b;
                return true;
            case string chuoi when bool.TryParse(chuoi, out var parsed):
                ketQua = parsed;
                return true;
            default:
                return false;
        }
    }

    public static bool ThuLayThoiGian(object? giaTri, out DateTimeOffset thoiGian)
    {
        thoiGian = default;
        var thuc = BocJson(giaTri);

        switch (thuc)
        {
            case DateTimeOffset dto:
                thoiGian = dto;
                return true;
            case DateTime dt:
                thoiGian = new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc));
                return true;
            case DateOnly ngay:
                thoiGian = new DateTimeOffset(ngay.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
                return true;
            case string chuoi when DateTimeOffset.TryParse(
                chuoi, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed):
                thoiGian = parsed;
                return true;
            default:
                return false;
        }
    }

    /// <summary>Chuyen ve chuoi de so sanh van ban (Guid, enum, ma nghiep vu...).</summary>
    public static string? LayChuoi(object? giaTri)
    {
        var thuc = BocJson(giaTri);
        return thuc switch
        {
            null => null,
            string chuoi => chuoi,
            Guid g => g.ToString(),
            bool b => b ? "true" : "false",
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => thuc.ToString()
        };
    }

    /// <summary>Tra ve danh sach phan tu neu gia tri la mang / danh sach, nguoc lai null.</summary>
    public static IReadOnlyList<object?>? ThuLayDanhSach(object? giaTri)
    {
        var thuc = BocJson(giaTri);

        return thuc switch
        {
            null => null,
            string => null,
            IReadOnlyList<object?> ds => ds,
            System.Collections.IEnumerable ds => ds.Cast<object?>().Select(BocJson).ToList(),
            _ => null
        };
    }
}
