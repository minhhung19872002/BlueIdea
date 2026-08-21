using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Kiem thu cac luong duoc bo sung sau dot ra soat: y kien hoi dong ve trung lap, xuat bao cao
/// PDF con thieu, ho so ca nhan, uy quyen xu ly, xuat bao cao nen va ban Word cua phieu cham.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class LuongBoSungTests
{
    private readonly UngDungKiemThu _ungDung;

    public LuongBoSungTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    // ------------------------------------------------------------------ Chuc nang 26

    [Fact]
    public async Task Ghi_Y_Kien_Xem_Xet_Trung_Lap_Duoc_Luu_Va_Doc_Lai()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var sangKienId = await LaySangKienDaNopAsync(admin);

        // Bao dam ho so co ket qua kiem tra de co cai ma ghi y kien len.
        (await admin.PostAsync($"/api/v1/sang-kien/{sangKienId}/trung-lap/chay-lai", null))
            .EnsureSuccessStatusCode();

        var ghi = await admin.PostAsJsonAsync(
            $"/api/v1/sang-kien/{sangKienId}/trung-lap/xem-xet",
            new { yKienHoiDong = "Hội đồng đã đối chiếu, xác định không phải sao chép." });

        ghi.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = await admin.GetFromJsonAsync<JsonElement>(
            $"/api/v1/sang-kien/{sangKienId}/trung-lap");

        var duLieu = doc.GetProperty("duLieu");
        duLieu.GetProperty("daXemXet").GetBoolean().Should().BeTrue();
        duLieu.GetProperty("yKienHoiDong").GetString()
            .Should().Be("Hội đồng đã đối chiếu, xác định không phải sao chép.");
    }

    [Fact]
    public async Task Tac_Gia_Khong_Duoc_Ghi_Y_Kien_Xem_Xet_Trung_Lap()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");

        var sangKienId = await LaySangKienDaNopAsync(admin);

        var phanHoi = await tacGia.PostAsJsonAsync(
            $"/api/v1/sang-kien/{sangKienId}/trung-lap/xem-xet",
            new { yKienHoiDong = "Tôi tự kết luận là không trùng" });

        phanHoi.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Xuat_Bao_Cao_Trung_Lap_Ra_PDF()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var sangKienId = await LaySangKienDaNopAsync(admin);

        (await admin.PostAsync($"/api/v1/sang-kien/{sangKienId}/trung-lap/chay-lai", null))
            .EnsureSuccessStatusCode();

        var phanHoi = await admin.GetAsync($"/api/v1/sang-kien/{sangKienId}/trung-lap/xuat-pdf");

        phanHoi.StatusCode.Should().Be(HttpStatusCode.OK);
        phanHoi.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");

        var noiDung = await phanHoi.Content.ReadAsByteArrayAsync();
        noiDung.Length.Should().BeGreaterThan(1000);
        System.Text.Encoding.ASCII.GetString(noiDung, 0, 4).Should().Be("%PDF");
    }

    /// <summary>
    /// Doi mo hinh nhung: vector cu phai bi bo qua (khong dem cosine giua hai khong gian khac
    /// nhau) va cong viec nen phai nhung lai cho den khi kho sach.
    /// </summary>
    [Fact]
    public async Task Doi_Mo_Hinh_Nhung_Thi_Bo_Qua_Vector_Cu_Va_Nhung_Lai()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var sangKienId = await LaySangKienDaNopAsync(admin);

        // Chay kiem tra trung lap de chac chan ho so co doan van kem vector.
        (await admin.PostAsync($"/api/v1/sang-kien/{sangKienId}/trung-lap/chay-lai", null))
            .EnsureSuccessStatusCode();

        string tenMoHinhHienTai;

        using (var pham = _ungDung.Services.CreateScope())
        {
            tenMoHinhHienTai = pham.ServiceProvider
                .GetRequiredService<Ai.Nhung.IBoNhungVanBan>().TenMoHinh;

            var db = pham.ServiceProvider
                .GetRequiredService<Infrastructure.Persistence.AppDbContext>();

            var doan = await db.SangKienDoanVan
                .Where(x => x.SangKienId == sangKienId)
                .ToListAsync();

            doan.Should().NotBeEmpty("hồ sơ đã chạy kiểm tra trùng lặp phải có đoạn văn");

            // Giả lập kho vector do một mô hình cũ sinh ra.
            foreach (var d in doan)
            {
                d.MoHinhNhung = "mo-hinh-cu-gia-lap";
            }

            await db.SaveChangesAsync();
        }

        // Tìm ngữ nghĩa không được đọc vector của mô hình khác.
        var tim = await admin.GetAsync(
            "/api/v1/sang-kien/tim-ngu-nghia?cauHoi=tiết kiệm điện chiếu sáng");

        tim.EnsureSuccessStatusCode();

        var ketQua = (await tim.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");

        ketQua.EnumerateArray().Should().NotContain(
            x => x.GetProperty("sangKienId").GetGuid() == sangKienId,
            "vector của mô hình khác không được đem ra so sánh");

        // Công việc nền nhúng lại đến khi hết đoạn cũ.
        int daNhungLai;
        var soVong = 0;

        do
        {
            using var pham = _ungDung.Services.CreateScope();
            var congViec = pham.ServiceProvider
                .GetRequiredService<Infrastructure.CongViecNen.CongViecNhungLaiDoanVan>();

            daNhungLai = await congViec.ChayAsync();
            soVong++;
        }
        while (daNhungLai > 0 && soVong < 20);

        using (var pham = _ungDung.Services.CreateScope())
        {
            var db = pham.ServiceProvider
                .GetRequiredService<Infrastructure.Persistence.AppDbContext>();

            var conCu = await db.SangKienDoanVan
                .CountAsync(x => x.SangKienId == sangKienId && x.MoHinhNhung != tenMoHinhHienTai);

            conCu.Should().Be(0, "công việc nền phải nhúng lại hết đoạn văn của mô hình cũ");

            var thieuVector = await db.SangKienDoanVan
                .CountAsync(x => x.SangKienId == sangKienId && x.Embedding == null);

            thieuVector.Should().Be(0, "nhúng lại xong thì đoạn nào cũng phải có vector");
        }
    }

    /// <summary>
    /// Canh bao suc khoe he thong: lo loi vuot nguong thi quan tri vien duoc bao ngay tren chuong
    /// thong bao, va mot dot loi keo dai khong bi bien thanh hang chuc thong bao giong nhau.
    /// </summary>
    [Fact]
    public async Task Lo_Loi_Vuot_Nguong_Thi_Quan_Tri_Duoc_Canh_Bao_Mot_Lan()
    {
        var nguon = $"KiemThuCanhBao-{Guid.NewGuid():N}";

        using (var pham = _ungDung.Services.CreateScope())
        {
            var db = pham.ServiceProvider
                .GetRequiredService<Infrastructure.Persistence.AppDbContext>();

            // Ngưỡng mặc định là 20 lỗi chưa xử lý trong 15 phút.
            for (var i = 0; i < 25; i++)
            {
                db.NhatKyLoi.Add(new Domain.QuanTri.NhatKyLoi
                {
                    MucDo = "LOI",
                    Nguon = nguon,
                    ThongBao = $"Lỗi giả lập {i}",
                    ThoiGian = DateTimeOffset.UtcNow.AddMinutes(-1),
                    DaXuLy = false
                });
            }

            await db.SaveChangesAsync();
        }

        var truoc = await DemCanhBaoAsync();

        int lanDau;
        using (var pham = _ungDung.Services.CreateScope())
        {
            lanDau = await pham.ServiceProvider
                .GetRequiredService<Infrastructure.CongViecNen.CongViecCanhBaoSucKhoe>()
                .ChayAsync();
        }

        lanDau.Should().BeGreaterThan(0, "phải gửi cảnh báo cho ít nhất một quản trị viên");
        (await DemCanhBaoAsync()).Should().Be(truoc + lanDau);

        // Chạy lại ngay: không được gửi thêm lần nữa trong cửa sổ chống lặp.
        int lanHai;
        using (var pham = _ungDung.Services.CreateScope())
        {
            lanHai = await pham.ServiceProvider
                .GetRequiredService<Infrastructure.CongViecNen.CongViecCanhBaoSucKhoe>()
                .ChayAsync();
        }

        lanHai.Should().Be(0, "một đợt lỗi kéo dài chỉ nên báo một lần");
        (await DemCanhBaoAsync()).Should().Be(truoc + lanDau);

        // Dọn dữ liệu giả lập để không ảnh hưởng các kiểm thử khác.
        using (var pham = _ungDung.Services.CreateScope())
        {
            var db = pham.ServiceProvider
                .GetRequiredService<Infrastructure.Persistence.AppDbContext>();

            var rac = await db.NhatKyLoi.Where(x => x.Nguon == nguon).ToListAsync();
            db.NhatKyLoi.RemoveRange(rac);
            await db.SaveChangesAsync();
        }
    }

    private async Task<int> DemCanhBaoAsync()
    {
        using var pham = _ungDung.Services.CreateScope();
        var db = pham.ServiceProvider
            .GetRequiredService<Infrastructure.Persistence.AppDbContext>();

        return await db.ThongBao
            .CountAsync(x => x.LoaiSuKien == Domain.QuanTri.SuKienThongBao.CanhBaoHeThong);
    }

    /// <summary>
    /// Cong viec nen don CAPTCHA / OTP het han: chi xoa ban ghi da qua han, giu nguyen ban ghi
    /// con hieu luc. Truoc day ham don da co san nhung khong lich nao goi, nen bang chi phinh ra.
    /// </summary>
    [Fact]
    public async Task Cong_Viec_Don_Ma_Xac_Thuc_Chi_Xoa_Ban_Ghi_Het_Han()
    {
        var khoaHetHan = $"kt-het-han-{Guid.NewGuid():N}";
        var khoaConHan = $"kt-con-han-{Guid.NewGuid():N}";

        using (var pham = _ungDung.Services.CreateScope())
        {
            var db = pham.ServiceProvider
                .GetRequiredService<Infrastructure.Persistence.AppDbContext>();

            db.MaXacThucTam.AddRange(
                new Domain.QuanTri.MaXacThucTam
                {
                    Loai = Domain.QuanTri.LoaiMaXacThucTam.Captcha,
                    Khoa = khoaHetHan,
                    MaBam = "BAM",
                    HetHan = DateTimeOffset.UtcNow.AddHours(-2)
                },
                new Domain.QuanTri.MaXacThucTam
                {
                    Loai = Domain.QuanTri.LoaiMaXacThucTam.Captcha,
                    Khoa = khoaConHan,
                    MaBam = "BAM",
                    HetHan = DateTimeOffset.UtcNow.AddHours(2)
                });

            await db.SaveChangesAsync();
        }

        using (var pham = _ungDung.Services.CreateScope())
        {
            var congViec = pham.ServiceProvider
                .GetRequiredService<Infrastructure.CongViecNen.CongViecDonMaXacThucTam>();

            await congViec.ChayAsync();
        }

        using (var pham = _ungDung.Services.CreateScope())
        {
            var db = pham.ServiceProvider
                .GetRequiredService<Infrastructure.Persistence.AppDbContext>();

            (await db.MaXacThucTam.AnyAsync(x => x.Khoa == khoaHetHan))
                .Should().BeFalse("bản ghi hết hạn phải bị dọn");

            (await db.MaXacThucTam.AnyAsync(x => x.Khoa == khoaConHan))
                .Should().BeTrue("bản ghi còn hiệu lực không được đụng tới");
        }
    }

    [Fact]
    public async Task Chi_Thanh_Phan_Duoc_Tick_Moi_Di_Vao_Kiem_Tra_Trung_Lap()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var sangKienId = await LaySangKienDaNopAsync(admin);

        const string dauHieuDuocSoKhop = "dauhieutinhmoi9271";
        const string dauHieuBiLoaiTru = "dauhieuphuluc4835";

        using (var pham = _ungDung.Services.CreateScope())
        {
            var db = pham.ServiceProvider
                .GetRequiredService<Infrastructure.Persistence.AppDbContext>();

            var hoSo = await db.SangKien.FirstAsync(x => x.Id == sangKienId);

            // TINH_MOI được tick "dùng để kiểm tra trùng lặp" trong quy trình mẫu, PHU_LUC thì không.
            hoSo.TinhMoi = $"{hoSo.TinhMoi} {dauHieuDuocSoKhop}";
            hoSo.NoiDungDong["PHU_LUC"] = $"Phụ lục kèm theo {dauHieuBiLoaiTru}";
            db.SangKien.Update(hoSo);

            await db.SaveChangesAsync();
        }

        (await admin.PostAsync($"/api/v1/sang-kien/{sangKienId}/trung-lap/chay-lai", null))
            .EnsureSuccessStatusCode();

        using (var pham = _ungDung.Services.CreateScope())
        {
            var db = pham.ServiceProvider
                .GetRequiredService<Infrastructure.Persistence.AppDbContext>();

            var doanVan = await db.SangKienDoanVan.AsNoTracking()
                .Where(x => x.SangKienId == sangKienId)
                .Select(x => x.NoiDungChuanHoa)
                .ToListAsync();

            doanVan.Should().NotBeEmpty("hồ sơ có nội dung nên phải cắt được đoạn văn");

            string.Join(" ", doanVan).Should().Contain(dauHieuDuocSoKhop,
                "thành phần được tick phải đi vào pipeline so khớp");

            string.Join(" ", doanVan).Should().NotContain(dauHieuBiLoaiTru,
                "phụ lục không được tick thì không được đưa vào so khớp (chức năng 13)");
        }
    }

    // ------------------------------------------------------------------ Chuc nang 39, 40

    [Theory]
    [InlineData("sang-kien-chua-dat")]
    [InlineData("theo-don-vi")]
    [InlineData("theo-tac-gia")]
    [InlineData("thoi-gian-xu-ly")]
    public async Task Bao_Cao_Deu_Xuat_Duoc_PDF(string loai)
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var phanHoi = await admin.GetAsync($"/api/v1/bao-cao/{loai}/xuat-pdf");

        phanHoi.StatusCode.Should().Be(HttpStatusCode.OK);
        phanHoi.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");

        var noiDung = await phanHoi.Content.ReadAsByteArrayAsync();
        System.Text.Encoding.ASCII.GetString(noiDung, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public async Task Tac_Gia_Khong_Xuat_Duoc_Bao_Cao_PDF()
    {
        var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");

        var phanHoi = await tacGia.GetAsync("/api/v1/bao-cao/theo-don-vi/xuat-pdf");

        phanHoi.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ------------------------------------------------------------------ Xuat bao cao nen

    [Fact]
    public async Task Dat_Lenh_Xuat_Bao_Cao_Nen_Tra_Ve_202()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var phanHoi = await admin.PostAsync("/api/v1/bao-cao/theo-don-vi/xuat-nen", null);

        phanHoi.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Loai_Bao_Cao_La_Bi_Chan_Khi_Dat_Lenh_Xuat_Nen()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var phanHoi = await admin.PostAsync("/api/v1/bao-cao/khong-ton-tai/xuat-nen", null);

        phanHoi.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ------------------------------------------------------------------ Ho so ca nhan

    [Fact]
    public async Task Nguoi_Dung_Tu_Cap_Nhat_Duoc_Thong_Tin_Ca_Nhan()
    {
        var client = await _ungDung.TaoClientDaDangNhapAsync("gv.hung");

        var capNhat = await client.PutAsJsonAsync("/api/v1/xac-thuc/toi", new
        {
            hoTen = "Nguyễn Văn Hùng (đã sửa)",
            email = "gv.hung.moi@blueidea.test",
            dienThoai = "0912345678",
            chucVu = "Giáo viên chủ nhiệm",
            gioiTinh = "NAM",
            ngaySinh = "1988-05-20"
        });

        capNhat.StatusCode.Should().Be(HttpStatusCode.OK);

        var toi = await client.GetFromJsonAsync<JsonElement>("/api/v1/xac-thuc/toi");
        var duLieu = toi.GetProperty("duLieu");

        duLieu.GetProperty("hoTen").GetString().Should().Be("Nguyễn Văn Hùng (đã sửa)");
        duLieu.GetProperty("email").GetString().Should().Be("gv.hung.moi@blueidea.test");
        duLieu.GetProperty("dienThoai").GetString().Should().Be("0912345678");
        duLieu.GetProperty("chucVu").GetString().Should().Be("Giáo viên chủ nhiệm");
    }

    [Fact]
    public async Task Cap_Nhat_Ho_So_Ca_Nhan_Chan_Du_Lieu_Khong_Hop_Le()
    {
        var client = await _ungDung.TaoClientDaDangNhapAsync("gv.thuy");

        var phanHoi = await client.PutAsJsonAsync("/api/v1/xac-thuc/toi", new
        {
            hoTen = string.Empty,
            dienThoai = "khong-phai-so"
        });

        phanHoi.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Cap_Nhat_Ho_So_Ca_Nhan_Khong_Doi_Duoc_Don_Vi_Va_Vai_Tro()
    {
        var client = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");

        var truoc = await client.GetFromJsonAsync<JsonElement>("/api/v1/xac-thuc/toi");
        var donViTruoc = truoc.GetProperty("duLieu").GetProperty("donViId").GetString();
        var vaiTroTruoc = truoc.GetProperty("duLieu").GetProperty("vaiTro")
            .EnumerateArray().Select(x => x.GetString()).ToList();

        // Gui kem donViId + vaiTro: cac truong nay khong nam trong hop dong lenh nen phai bi bo qua.
        var phanHoi = await client.PutAsJsonAsync("/api/v1/xac-thuc/toi", new
        {
            hoTen = "Tự đổi đơn vị",
            donViId = Guid.NewGuid(),
            vaiTro = new[] { "QUAN_TRI_HE_THONG" }
        });

        phanHoi.EnsureSuccessStatusCode();

        var sau = await client.GetFromJsonAsync<JsonElement>("/api/v1/xac-thuc/toi");
        sau.GetProperty("duLieu").GetProperty("donViId").GetString().Should().Be(donViTruoc);
        sau.GetProperty("duLieu").GetProperty("vaiTro").EnumerateArray()
            .Select(x => x.GetString()).Should().BeEquivalentTo(vaiTroTruoc);
    }

    // ------------------------------------------------------------------ Uy quyen xu ly

    [Fact]
    public async Task Danh_Sach_Tac_Nhan_Buoc_Chi_Gom_Nguoi_Dang_Hoat_Dong()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var sangKienId = await LaySangKienDaNopAsync(admin);

        var phanHoi = await admin.GetAsync($"/api/v1/xu-ly/tac-nhan-buoc/{sangKienId}");
        phanHoi.EnsureSuccessStatusCode();

        var duLieu = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").EnumerateArray().ToList();

        foreach (var x in duLieu)
        {
            x.GetProperty("hoTen").GetString().Should().NotBeNullOrWhiteSpace();
            x.GetProperty("tenDangNhap").GetString().Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task Uy_Quyen_Cho_Nguoi_Khong_Phai_Tac_Nhan_Bi_Chan()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var sangKienId = await LaySangKienDaNopAsync(admin);

        var hanhDong = (await admin.GetFromJsonAsync<JsonElement>(
                $"/api/v1/sang-kien/{sangKienId}/hanh-dong"))
            .GetProperty("duLieu").EnumerateArray().FirstOrDefault();

        if (hanhDong.ValueKind == JsonValueKind.Undefined)
        {
            return; // Ho so khong con hanh dong nao — khong con gi de kiem o day.
        }

        Guid idTacGia;

        using (var pham = _ungDung.Services.CreateScope())
        {
            var db = pham.ServiceProvider
                .GetRequiredService<Infrastructure.Persistence.AppDbContext>();

            idTacGia = await db.NguoiDung.AsNoTracking()
                .Where(x => x.TenDangNhap == "gv.lan")
                .Select(x => x.Id)
                .FirstAsync();
        }

        var phanHoi = await admin.PostAsJsonAsync("/api/v1/xu-ly/thuc-thi", new
        {
            sangKienId,
            truongHopId = hanhDong.GetProperty("truongHopId").GetGuid(),
            yKien = "Xử lý thay người không có thẩm quyền",
            nguoiUyQuyenId = idTacGia
        });

        phanHoi.IsSuccessStatusCode.Should().BeFalse(
            "tác giả không phải tác nhân của bước nên không uỷ quyền cho người xử lý được");
    }

    // ------------------------------------------------------------------ Chuc nang 35

    [Fact]
    public async Task Xuat_Phieu_Cham_Ban_Word()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        Guid? sangKienId;

        using (var pham = _ungDung.Services.CreateScope())
        {
            var db = pham.ServiceProvider
                .GetRequiredService<Infrastructure.Persistence.AppDbContext>();

            sangKienId = await db.PhieuDanhGia.AsNoTracking()
                .Where(x => x.TrangThaiPhieu != "NHAP")
                .Select(x => (Guid?)x.SangKienId)
                .FirstOrDefaultAsync();
        }

        if (sangKienId is null)
        {
            return; // Du lieu mau khong co phieu da gui — bo qua thay vi bao that bai gia.
        }

        var phanHoi = await admin.GetAsync(
            $"/api/v1/nhap-xuat/phieu-cham/ho-so/{sangKienId}?dinhDang=DOCX");

        phanHoi.StatusCode.Should().Be(HttpStatusCode.OK);
        phanHoi.Content.Headers.ContentType!.MediaType.Should()
            .Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        var noiDung = await phanHoi.Content.ReadAsByteArrayAsync();

        // .docx la mot goi ZIP — hai byte dau luon la "PK".
        noiDung.Length.Should().BeGreaterThan(1000);
        noiDung[0].Should().Be((byte)'P');
        noiDung[1].Should().Be((byte)'K');
    }

    // ------------------------------------------------------------------

    private static async Task<Guid> LaySangKienDaNopAsync(HttpClient client)
    {
        var danhSach = await client.GetFromJsonAsync<JsonElement>(
            "/api/v1/sang-kien?trang=1&soDong=50");

        var hoSo = danhSach.GetProperty("duLieu").EnumerateArray()
            .FirstOrDefault(x => x.GetProperty("trangThaiTong").GetString() != "NHAP");

        hoSo.ValueKind.Should().NotBe(JsonValueKind.Undefined, "dữ liệu mẫu phải có hồ sơ đã nộp");

        return hoSo.GetProperty("id").GetGuid();
    }
}
