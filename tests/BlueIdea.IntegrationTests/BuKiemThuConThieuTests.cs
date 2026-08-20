using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Bu cac phep kiem tung duoc ghi la "no test for..." trong traceability.
///
/// Chuc nang deu chay duoc, cai thieu la phep kiem tu dong khoa lai — nghia la khong co gi chan
/// mot lan sua vo tinh lam hong chung. Uu tien nhung cho ma hong se im lang: diem lo truoc khi
/// gui phieu, chong gui trung, tong ty le dong gop.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class BuKiemThuConThieuTests
{
    private readonly UngDungKiemThu _ungDung;

    public BuKiemThuConThieuTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    // ------------------------------------------- REQ-33/34: che diem truoc khi gui

    /// <summary>
    /// Ma tran diem chi duoc lo diem cua phieu DA GUI.
    ///
    /// Cham doc lap chi con la khau hieu neu thu ky nhin duoc diem nhap do khi thanh vien khac
    /// chua cham xong: nguoi cham sau se biet minh dang lech voi ai bao nhieu.
    /// </summary>
    [Fact]
    public async Task Ma_Tran_Diem_Khong_Lo_Diem_Cua_Phieu_Con_Nhap()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var hoiDongId = await LayHoiDongMauAsync(admin);

        // Ma tran chi liet ke ho so DA PHAN CONG cho hoi dong do — lay tu chinh ma tran ra thay
        // vi doan mot ho so bat ky.
        var banDau = await LayMaTranAsync(admin, hoiDongId);

        if (banDau.ValueKind != JsonValueKind.Array || banDau.GetArrayLength() == 0)
        {
            return; // Du lieu mau chua phan cong cham cho hoi dong nay.
        }

        var sangKienId = banDau[0].GetProperty("sangKienId").GetGuid();
        var thanhVienId = banDau[0].GetProperty("o")[0].GetProperty("thanhVienId").GetGuid();

        using (var pham = _ungDung.Services.CreateScope())
        {
            var db = pham.ServiceProvider
                .GetRequiredService<Infrastructure.Persistence.AppDbContext>();

            // Phieu con o trang thai NHAP nhung DA co diem — tinh huong that khi thanh vien luu
            // nhap giua chung.
            var phieuCu = await db.PhieuDanhGia
                .Where(x => x.SangKienId == sangKienId && x.ThanhVienId == thanhVienId)
                .ToListAsync();

            db.PhieuDanhGia.RemoveRange(phieuCu);

            db.PhieuDanhGia.Add(new Domain.SangKien.PhieuDanhGia
            {
                Id = Guid.NewGuid(),
                SangKienId = sangKienId,
                HoiDongId = hoiDongId,
                ThanhVienId = thanhVienId,
                TongDiem = 87.5m,
                TrangThaiPhieu = Domain.SangKien.TrangThaiPhieuDanhGia.Nhap
            });

            await db.SaveChangesAsync();
        }

        var phanHoi = await admin.GetAsync(
            $"/api/v1/danh-gia/ma-tran-diem?hoiDongId={hoiDongId}");

        phanHoi.EnsureSuccessStatusCode();

        var duLieu = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");

        var dong = duLieu.EnumerateArray()
            .FirstOrDefault(x => x.GetProperty("sangKienId").GetGuid() == sangKienId);

        dong.ValueKind.Should().NotBe(JsonValueKind.Undefined,
            "hồ sơ vừa lấy từ chính ma trận thì phải còn trong ma trận");

        var o = dong.GetProperty("o").EnumerateArray()
            .First(x => x.GetProperty("thanhVienId").GetGuid() == thanhVienId);

        o.GetProperty("diem").ValueKind.Should().Be(JsonValueKind.Null,
            "phiếu còn ở trạng thái nháp thì điểm không được lộ ra ma trận");
    }

    // ------------------------------------------------ REQ-29: chong gui trung

    /// <summary>
    /// Cung mot Idempotency-Key gui hai lan thi lan hai bi chan — chong double-submit khi mang
    /// chap chon hoac nguoi dung bam hai lan.
    /// </summary>
    [Fact]
    public async Task Cung_Idempotency_Key_Gui_Hai_Lan_Thi_Lan_Hai_Bi_Chan()
    {
        var canBo = await _ungDung.TaoClientDaDangNhapAsync("tiepnhan");

        var (sangKienId, truongHopId) = await LayHoSoVaHanhDongAsync(canBo);

        if (sangKienId == Guid.Empty)
        {
            return; // Du lieu mau khong co ho so nao dang cho can bo tiep nhan xu ly.
        }

        var khoa = $"kt-{Guid.NewGuid():N}";

        var lanMot = await GuiThucThiAsync(canBo, sangKienId, truongHopId, khoa);
        lanMot.EnsureSuccessStatusCode();

        var lanHai = await GuiThucThiAsync(canBo, sangKienId, truongHopId, khoa);

        var noiDung = await lanHai.Content.ReadFromJsonAsync<JsonElement>();

        /*
         * Lan hai phai bi tu choi bang ma loi YEU_CAU_TRUNG_LAP.
         *
         * Ket qua xu ly duoc boc trong PhanHoiApi nen ma loi nam o duLieu.maLoi chu khong o goc:
         * request ve mat HTTP la hop le, chinh NGHIEP VU moi tu choi.
         */
        var ketQua = noiDung.GetProperty("duLieu");

        ketQua.GetProperty("thanhCong").GetBoolean().Should().BeFalse();
        (ketQua.GetProperty("maLoi").GetString() ?? string.Empty).Should().Contain("TRUNG_LAP");
    }

    // ------------------------------------------ REQ-22: tong ty le dong gop 100%

    [Fact]
    public async Task Tong_Ty_Le_Dong_Gop_Khac_100_Thi_Khong_Nop_Duoc()
    {
        var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");

        var dot = await LayDotDangMoAsync(tacGia);
        if (dot is null)
        {
            return; // Khong co dot nao dang mo trong du lieu mau.
        }

        var tao = await tacGia.PostAsJsonAsync("/api/v1/sang-kien", new
        {
            tenSangKien = "Hồ sơ kiểm thử tỷ lệ đóng góp",
            dotDeNghiId = dot,
            danhSachTacGia = new[]
            {
                new { hoTen = "Nguyễn Thị Lan", tyLeDongGop = 60m, laTacGiaChinh = true },
                new { hoTen = "Trần Mạnh Hùng", tyLeDongGop = 30m, laTacGiaChinh = false }
            }
        });

        if (!tao.IsSuccessStatusCode)
        {
            // Tao nhap co the bi chan ngay tu buoc nay — cung la mot cach chan hop le.
            tao.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            return;
        }

        var sangKienId = (await tao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("id").GetGuid();

        var nop = await tacGia.PostAsync($"/api/v1/sang-kien/{sangKienId}/nop", null);

        nop.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "tổng tỷ lệ 90% phải bị chặn khi nộp");

        var loi = await nop.Content.ReadFromJsonAsync<JsonElement>();
        var chuoi = loi.ToString() ?? string.Empty;

        chuoi.Should().Contain("100", "thông báo phải nói rõ tổng phải bằng 100%");
    }

    // ------------------------------------------------- REQ-01: tim khong dau

    [Fact]
    public async Task Tim_Danh_Muc_Khong_Dau_Van_Ra_Ket_Qua_Co_Dau()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var phanHoi = await admin.GetAsync("/api/v1/danh-muc/linh-vuc?tuKhoa=giao duc&soDong=20");
        phanHoi.EnsureSuccessStatusCode();

        var duLieu = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu");

        var ten = duLieu.EnumerateArray()
            .Select(x => x.GetProperty("ten").GetString() ?? string.Empty)
            .ToList();

        ten.Should().NotBeEmpty("gõ không dấu vẫn phải ra lĩnh vực có dấu");
        ten.Should().Contain(x => x.Contains("Giáo dục", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Xuat_Excel_Danh_Muc_Tra_Ve_Tep_Xlsx()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var phanHoi = await admin.GetAsync("/api/v1/danh-muc/linh-vuc/xuat-excel");
        phanHoi.EnsureSuccessStatusCode();

        var noiDung = await phanHoi.Content.ReadAsByteArrayAsync();

        noiDung.Length.Should().BeGreaterThan(0);

        // Tep .xlsx la mot ZIP — bat dau bang "PK".
        noiDung[0].Should().Be((byte)'P');
        noiDung[1].Should().Be((byte)'K');
    }

    // ------------------------------------------- REQ-03: job tu dong dong dot

    /// <summary>
    /// Dot qua han nop va co bat "tu dong khoa" thi job dong lai; dot khong bat co thi giu nguyen.
    /// </summary>
    [Fact]
    public async Task Job_Chi_Dong_Dot_Qua_Han_Co_Bat_Tu_Dong_Khoa()
    {
        Guid dotTuDong;
        Guid dotThuCong;

        using (var pham = _ungDung.Services.CreateScope())
        {
            var db = pham.ServiceProvider
                .GetRequiredService<Infrastructure.Persistence.AppDbContext>();

            dotTuDong = await TaoDotAsync(db, tuDongKhoa: true);
            dotThuCong = await TaoDotAsync(db, tuDongKhoa: false);

            await db.SaveChangesAsync();
        }

        using (var pham = _ungDung.Services.CreateScope())
        {
            await pham.ServiceProvider
                .GetRequiredService<Infrastructure.CongViecNen.CongViecDongDotHetHan>()
                .ChayAsync();
        }

        using (var pham = _ungDung.Services.CreateScope())
        {
            var db = pham.ServiceProvider
                .GetRequiredService<Infrastructure.Persistence.AppDbContext>();

            (await db.DotDeNghi.AsNoTracking().FirstAsync(x => x.Id == dotTuDong))
                .TrangThaiDot.Should().Be("DA_DONG");

            (await db.DotDeNghi.AsNoTracking().FirstAsync(x => x.Id == dotThuCong))
                .TrangThaiDot.Should().Be("DANG_MO",
                    "đợt không bật tự động khoá thì job không được đụng vào");
        }
    }

    // ---------------------------------------------------------------------

    private static async Task<Guid> TaoDotAsync(
        Infrastructure.Persistence.AppDbContext db, bool tuDongKhoa)
    {
        var dot = new Domain.DanhMuc.DotDeNghi
        {
            Id = Guid.NewGuid(),
            Ma = $"KT{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            Ten = $"Đợt kiểm thử tự động khoá ({tuDongKhoa})",
            Nam = DateTime.UtcNow.Year,
            TrangThaiDot = "DANG_MO",
            TuDongKhoa = tuDongKhoa,
            HanNopHoSo = DateTimeOffset.UtcNow.AddDays(-1)
        };

        db.DotDeNghi.Add(dot);
        await Task.CompletedTask;

        return dot.Id;
    }

    private static async Task<JsonElement> LayMaTranAsync(HttpClient client, Guid hoiDongId)
    {
        var phanHoi = await client.GetAsync($"/api/v1/danh-gia/ma-tran-diem?hoiDongId={hoiDongId}");
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
    }

    private static async Task<Guid> LayHoiDongMauAsync(HttpClient client)
    {
        var phanHoi = await client.GetAsync("/api/v1/hoi-dong?soDong=1");
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu")[0].GetProperty("id").GetGuid();
    }

    private static async Task<Guid?> LayDotDangMoAsync(HttpClient client)
    {
        var phanHoi = await client.GetAsync("/api/v1/danh-muc/dot-de-nghi/dang-mo");

        if (!phanHoi.IsSuccessStatusCode)
        {
            return null;
        }

        var duLieu = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");

        if (duLieu.ValueKind != JsonValueKind.Array || duLieu.GetArrayLength() == 0)
        {
            return null;
        }

        return duLieu[0].GetProperty("id").GetGuid();
    }

    private static async Task<(Guid SangKienId, Guid TruongHopId)> LayHoSoVaHanhDongAsync(
        HttpClient client)
    {
        var ds = await client.GetAsync("/api/v1/sang-kien?trang=1&soDong=30");
        ds.EnsureSuccessStatusCode();

        var duLieu = (await ds.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");

        foreach (var hoSo in duLieu.EnumerateArray())
        {
            var id = hoSo.GetProperty("id").GetGuid();

            var hanhDong = await client.GetAsync($"/api/v1/sang-kien/{id}/hanh-dong");
            if (!hanhDong.IsSuccessStatusCode)
            {
                continue;
            }

            var ds2 = (await hanhDong.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("duLieu");

            if (ds2.ValueKind == JsonValueKind.Array && ds2.GetArrayLength() > 0)
            {
                return (id, ds2[0].GetProperty("truongHopId").GetGuid());
            }
        }

        return (Guid.Empty, Guid.Empty);
    }

    private static Task<HttpResponseMessage> GuiThucThiAsync(
        HttpClient client, Guid sangKienId, Guid truongHopId, string khoa)
    {
        var yeuCau = new HttpRequestMessage(HttpMethod.Post, "/api/v1/xu-ly/thuc-thi")
        {
            Content = JsonContent.Create(new
            {
                sangKienId,
                truongHopId,
                yKien = "Kiểm thử chống gửi trùng"
            })
        };

        yeuCau.Headers.Add("Idempotency-Key", khoa);

        return client.SendAsync(yeuCau);
    }
}
