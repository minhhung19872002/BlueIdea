using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace BlueIdea.KySoUsb;

/// <summary>
/// Cong cu ky so bang USB token, chay tren MAY TRAM cua nguoi ky (chuc nang 49).
///
/// Vi sao phai co no: khoa bi mat nam trong USB token va khong bao gio roi khoi thiet bi, nen
/// may chu khong ky thay duoc. Luong bat buoc la ba nhip — may chu phat gia tri bam, may tram ky
/// gia tri bam bang token, may chu xac minh roi moi ghi nhan. Truoc day nhip giua lam bang tay:
/// nguoi dung sao chep gia tri bam ra, dan chu ky vao. Cong cu nay lam thay ca ba nhip.
///
/// Chay o may tram chu khong phai trong trinh duyet vi trinh duyet khong voi toi duoc token;
/// va chay bang tien trinh CO DESKTOP de driver cua token bat duoc hop thoai nhap ma PIN.
/// </summary>
public static class Program
{
    private const string ChuThichSuDung = """
        blueidea-kyso — công cụ ký số bằng USB token cho BlueIdea

        Cách dùng:
          blueidea-kyso liet-ke
              Liệt kê chứng thư có khoá bí mật đang cắm trên máy.

          blueidea-kyso ky --hash <base64> [--van-tay <thumbprint>]
              Ký một giá trị băm do máy chủ phát, in ra JSON gồm chữ ký và chứng thư.
              Dùng khi muốn tự dán kết quả vào màn hình ký của BlueIdea.

          blueidea-kyso tu-dong --api <url> --tep <guid>
                                --doi-tuong <QUYET_DINH|BIEN_BAN_HOP> --doi-tuong-id <guid>
                                (--token <jwt> | --tai-khoan <tên> --mat-khau <mật khẩu>)
                                [--van-tay <thumbprint>]
              Chạy trọn ba nhịp: xin giá trị băm, ký bằng token, gửi chữ ký về máy chủ.

        LƯU Ý: phải chạy trong cửa sổ dòng lệnh của chính người ký. Token chỉ bật được hộp
        thoại nhập PIN khi tiến trình có desktop; chạy từ dịch vụ nền hoặc phiên không tương
        tác thì lệnh sẽ treo im lặng ở bước ký.

        Tuỳ chọn chung:
          --van-tay <thumbprint>   Chọn đúng chứng thư khi máy cắm nhiều token.
          --pin <mã>               Nhập PIN không qua hộp thoại (dùng cho chạy tự động).
                                   Bỏ trống thì driver của token tự hỏi PIN.
          --cho <giây>             Thời gian chờ token phản hồi, mặc định 120 giây.
        """;

    public static async Task<int> Main(string[] thamSo)
    {
        // Console mac dinh cua Windows dung code page 437/1258 nen tieng Viet ra thanh ky tu la.
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        try
        {
            return (thamSo.FirstOrDefault() ?? "giup") switch
            {
                "liet-ke" => LietKe(),
                "ky" => Ky(DocThamSo(thamSo)),
                "tu-dong" => await TuDongAsync(DocThamSo(thamSo)).ConfigureAwait(false),
                _ => InHuongDan()
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LỖI: {ex.Message}");
            return 1;
        }
    }

    private static int InHuongDan()
    {
        Console.WriteLine(ChuThichSuDung);
        return 0;
    }

    // ------------------------------------------------------------------ Liet ke

    private static int LietKe()
    {
        var danhSach = LayChungThuCoKhoa();

        if (danhSach.Count == 0)
        {
            Console.WriteLine("Không thấy chứng thư nào có khoá bí mật. Kiểm tra USB token đã cắm chưa.");
            return 2;
        }

        foreach (var ct in danhSach)
        {
            var conHan = ct.NotAfter.ToUniversalTime() >= DateTime.UtcNow
                         && ct.NotBefore.ToUniversalTime() <= DateTime.UtcNow;

            Console.WriteLine($"Vân tay   : {ct.Thumbprint}");
            Console.WriteLine($"Chủ thể   : {ct.Subject}");
            Console.WriteLine($"Nơi cấp   : {ct.Issuer}");
            Console.WriteLine($"Hiệu lực  : {ct.NotBefore:dd/MM/yyyy} → {ct.NotAfter:dd/MM/yyyy}"
                              + (conHan ? " (còn hạn)" : " (HẾT HẠN)"));
            Console.WriteLine($"Có khoá bí mật: {(ct.HasPrivateKey ? "có" : "không")}");
            Console.WriteLine(new string('-', 78));
        }

        return 0;
    }

    // ----------------------------------------------------------------------- Ky

    private static int Ky(IReadOnlyDictionary<string, string> tuyChon)
    {
        var hashBase64 = LayBatBuoc(tuyChon, "hash");
        var hash = Convert.FromBase64String(hashBase64);

        using var chungThu = ChonChungThu(tuyChon.GetValueOrDefault("van-tay"));
        var chuKy = KyGiaTriBam(
            chungThu, hash, tuyChon.GetValueOrDefault("pin"), DocThoiGianCho(tuyChon));

        Console.WriteLine(JsonSerializer.Serialize(new
        {
            chuKyBase64 = Convert.ToBase64String(chuKy),
            chungThuBase64 = Convert.ToBase64String(chungThu.RawData)
        }, new JsonSerializerOptions { WriteIndented = true }));

        return 0;
    }

    // ------------------------------------------------------------------- Tu dong

    private static async Task<int> TuDongAsync(IReadOnlyDictionary<string, string> tuyChon)
    {
        var api = LayBatBuoc(tuyChon, "api").TrimEnd('/');
        var tepTinId = LayBatBuoc(tuyChon, "tep");
        var doiTuong = LayBatBuoc(tuyChon, "doi-tuong");
        var doiTuongId = LayBatBuoc(tuyChon, "doi-tuong-id");

        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };

        var token = tuyChon.GetValueOrDefault("token");

        if (string.IsNullOrWhiteSpace(token))
        {
            token = await DangNhapAsync(
                http, api, LayBatBuoc(tuyChon, "tai-khoan"), LayBatBuoc(tuyChon, "mat-khau"))
                .ConfigureAwait(false);
        }

        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // --- Nhip 1: xin gia tri bam --------------------------------------------
        Console.WriteLine("→ Nhịp 1: xin giá trị băm của tệp cần ký...");

        var chuanBi = await http.PostAsJsonAsync($"{api}/api/v1/ky-so-usb/chuan-bi", new
        {
            tepTinId,
            doiTuong,
            doiTuongId
        }).ConfigureAwait(false);

        var noiDungChuanBi = await chuanBi.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (!chuanBi.IsSuccessStatusCode)
        {
            Console.Error.WriteLine($"Máy chủ từ chối ở nhịp 1 ({(int)chuanBi.StatusCode}): {noiDungChuanBi}");
            return 3;
        }

        var duLieu = JsonDocument.Parse(noiDungChuanBi).RootElement.GetProperty("duLieu");
        var phienId = duLieu.GetProperty("phienId").GetString()!;
        var hash = Convert.FromBase64String(duLieu.GetProperty("hashBase64").GetString()!);

        Console.WriteLine($"  Tệp      : {duLieu.GetProperty("tenTep").GetString()}");
        Console.WriteLine($"  Thuật toán: {duLieu.GetProperty("thuatToanBam").GetString()}");
        Console.WriteLine($"  Phiên ký : {phienId}");

        // --- Nhip 2: ky bang token ----------------------------------------------
        Console.WriteLine("→ Nhịp 2: ký bằng USB token (nhập mã PIN nếu được hỏi)...");

        using var chungThu = ChonChungThu(tuyChon.GetValueOrDefault("van-tay"));
        Console.WriteLine($"  Chứng thư: {chungThu.Subject}");

        var chuKy = KyGiaTriBam(
            chungThu, hash, tuyChon.GetValueOrDefault("pin"), DocThoiGianCho(tuyChon));

        Console.WriteLine($"  Đã ký, chữ ký dài {chuKy.Length} byte.");

        // --- Nhip 3: gui ve may chu de xac minh ----------------------------------
        Console.WriteLine("→ Nhịp 3: gửi chữ ký về máy chủ để xác minh...");

        var hoanTat = await http.PostAsJsonAsync(
            $"{api}/api/v1/ky-so-usb/{phienId}/hoan-tat",
            new
            {
                chuKyBase64 = Convert.ToBase64String(chuKy),
                chungThuBase64 = Convert.ToBase64String(chungThu.RawData)
            }).ConfigureAwait(false);

        var noiDungHoanTat = await hoanTat.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (!hoanTat.IsSuccessStatusCode)
        {
            Console.Error.WriteLine($"Máy chủ từ chối chữ ký ({(int)hoanTat.StatusCode}): {noiDungHoanTat}");
            return 4;
        }

        Console.WriteLine("✔ Máy chủ đã xác minh và ghi nhận chữ ký.");
        Console.WriteLine(noiDungHoanTat);
        return 0;
    }

    /// <summary>
    /// Lay JWT bang tai khoan BlueIdea.
    ///
    /// Co san de nguoi ky khong phai tu di sao chep token tu trinh duyet — thao tac do vua phien
    /// vua de lam ro token ra ngoai (lich su lenh, anh chup man hinh).
    /// </summary>
    private static async Task<string> DangNhapAsync(
        HttpClient http, string api, string taiKhoan, string matKhau)
    {
        var phanHoi = await http.PostAsJsonAsync($"{api}/api/v1/xac-thuc/dang-nhap", new
        {
            tenDangNhap = taiKhoan,
            matKhau
        }).ConfigureAwait(false);

        var noiDung = await phanHoi.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (!phanHoi.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Đăng nhập thất bại ({(int)phanHoi.StatusCode}): {noiDung}");
        }

        return JsonDocument.Parse(noiDung).RootElement
                   .GetProperty("duLieu").GetProperty("accessToken").GetString()
               ?? throw new InvalidOperationException("Máy chủ không trả về accessToken.");
    }

    // ------------------------------------------------------------------ Ho tro

    private static List<X509Certificate2> LayChungThuCoKhoa()
    {
        using var kho = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        kho.Open(OpenFlags.ReadOnly);

        return kho.Certificates
            .Cast<X509Certificate2>()
            .Where(x => x.HasPrivateKey)
            .OrderByDescending(x => x.NotAfter)
            .ToList();
    }

    /// <summary>
    /// Chon chung thu de ky.
    ///
    /// Khong tu doan khi may co nhieu chung thu: ky nham danh tinh la sai sot khong sua duoc bang
    /// mot lan bam nut. Chi tu chon khi CHI CO DUNG MOT chung thu con han.
    /// </summary>
    private static X509Certificate2 ChonChungThu(string? vanTay)
    {
        var danhSach = LayChungThuCoKhoa();

        if (!string.IsNullOrWhiteSpace(vanTay))
        {
            return danhSach.FirstOrDefault(
                       x => string.Equals(x.Thumbprint, vanTay.Replace(" ", ""),
                           StringComparison.OrdinalIgnoreCase))
                   ?? throw new InvalidOperationException(
                       $"Không thấy chứng thư có vân tay '{vanTay}'. Chạy 'blueidea-kyso liet-ke' để xem danh sách.");
        }

        var conHan = danhSach
            .Where(x => x.NotAfter.ToUniversalTime() >= DateTime.UtcNow
                        && x.NotBefore.ToUniversalTime() <= DateTime.UtcNow)
            .ToList();

        return conHan.Count switch
        {
            0 => throw new InvalidOperationException(
                "Không có chứng thư nào còn hạn. Kiểm tra USB token đã cắm chưa."),
            1 => conHan[0],
            _ => throw new InvalidOperationException(
                "Máy có nhiều chứng thư còn hạn — chỉ rõ bằng --van-tay <thumbprint> "
                + "(xem 'blueidea-kyso liet-ke').")
        };
    }

    /// <summary>
    /// Ky gia tri bam bang khoa nam trong token.
    ///
    /// RSA PKCS#1 v1.5 tren SHA-256 — dung khuon may chu xac minh (DichVuKySoUsbToken).
    /// </summary>
    private static byte[] KyGiaTriBam(
        X509Certificate2 chungThu, byte[] hash, string? pin, TimeSpan? cho = null)
    {
        var thoiGianCho = cho ?? TimeSpan.FromSeconds(120);

        if (hash.Length != 32)
        {
            throw new InvalidOperationException(
                $"Giá trị băm phải là SHA-256 (32 byte), nhận được {hash.Length} byte.");
        }

        using var rsa = chungThu.GetRSAPrivateKey()
                        ?? throw new InvalidOperationException(
                            "Chứng thư không có khoá bí mật dùng được (token chưa cắm hoặc driver chưa nạp).");

        if (!string.IsNullOrEmpty(pin) && rsa is RSACng cng)
        {
            DatMaPin(cng, pin);
        }

        /*
         * Ky co thoi han cho, khong treo vo han.
         *
         * Da gap that: chay cong cu tu mot tien trinh KHONG gan voi console tuong tac thi driver
         * cua token van tao hop thoai PIN, nhung luong tao ra no chinh la luong dang bi chan trong
         * lenh ky — vong lap thong diep khong bao gio chay nen cua so khong bao gio duoc ve. Ket
         * qua: mot cua so ton tai tren giay to (enumerate ra duoc, co toa do) ma khong ai thay,
         * va lenh dung im mai mai. Treo im lang la kieu hong te nhat: khong ai biet phai sua gi.
         */
        var viecKy = Task.Run(() =>
            rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));

        if (!viecKy.Wait(thoiGianCho))
        {
            throw new InvalidOperationException(
                $"Token không phản hồi sau {thoiGianCho.TotalSeconds:0} giây.\n"
                + "Thường gặp nhất: lệnh đang chạy từ tiến trình không có console tương tác "
                + "(dịch vụ nền, tác vụ theo lịch, phiên SSH, hoặc công cụ tự động gọi hộ), nên "
                + "hộp thoại nhập PIN không hiện ra được.\n"
                + "Cách xử lý: chạy lệnh này trong cửa sổ dòng lệnh của chính người ký; "
                + "hoặc nếu buộc phải chạy nền thì truyền sẵn mã PIN bằng --pin.");
        }

        try
        {
            return viecKy.GetAwaiter().GetResult();
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException(
                "Token từ chối ký. Thường do sai mã PIN, huỷ hộp thoại PIN, hoặc token đã bị khoá "
                + $"sau nhiều lần nhập sai. Chi tiết: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Nap ma PIN thang vao khoa de khong hien hop thoai.
    ///
    /// Chi dung cho chay tu dong (vi du ky hang loat theo lich). Khong ghi PIN vao tep cau hinh
    /// hay vao lich su lenh — truyen qua bien moi truong hoac nhap luc chay.
    /// </summary>
    private static void DatMaPin(RSACng cng, string pin)
    {
        const string TenThuocTinhPin = "SmartCardPin";

        var byteePin = System.Text.Encoding.Unicode.GetBytes(pin + '\0');

        cng.Key.SetProperty(new CngProperty(
            TenThuocTinhPin, byteePin, CngPropertyOptions.None));
    }

    private static TimeSpan DocThoiGianCho(IReadOnlyDictionary<string, string> tuyChon)
        => tuyChon.TryGetValue("cho", out var giay) && int.TryParse(giay, out var so) && so > 0
            ? TimeSpan.FromSeconds(so)
            : TimeSpan.FromSeconds(120);

    private static Dictionary<string, string> DocThamSo(string[] thamSo)
    {
        var ketQua = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 1; i < thamSo.Length - 1; i++)
        {
            if (thamSo[i].StartsWith("--", StringComparison.Ordinal))
            {
                ketQua[thamSo[i][2..]] = thamSo[i + 1];
                i++;
            }
        }

        return ketQua;
    }

    private static string LayBatBuoc(IReadOnlyDictionary<string, string> tuyChon, string ten)
        => tuyChon.TryGetValue(ten, out var giaTri) && !string.IsNullOrWhiteSpace(giaTri)
            ? giaTri
            : throw new InvalidOperationException($"Thiếu tham số --{ten}.");
}
