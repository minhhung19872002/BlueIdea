using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Kiem thu tich hop luong end-to-end theo Muc 11 dac ta:
/// Nop ho so → Tiep nhan → Yeu cau bo sung → Bo sung → Tiep nhan → Tham dinh
/// → Phan cong → 3 thanh vien cham → Tong hop → Hop hoi dong ket luan Dat
/// → Ban hanh quyet dinh → Xuat bao cao.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class LuongNghiepVuTests
{
    private readonly UngDungKiemThu _ungDung;

    public LuongNghiepVuTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    [Fact]
    public async Task Luong_Day_Du_Tu_Nop_Ho_So_Den_Ban_Hanh_Quyet_Dinh()
    {
        var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");
        var tiepNhan = await _ungDung.TaoClientDaDangNhapAsync("tiepnhan");
        var thuKy = await _ungDung.TaoClientDaDangNhapAsync("thuky");
        var chuTich = await _ungDung.TaoClientDaDangNhapAsync("chutich");
        var lanhDao = await _ungDung.TaoClientDaDangNhapAsync("lanhdao");

        // --- 1. Tac gia tao ho so -------------------------------------------------
        var dot = (await LayDuLieuAsync(tacGia, "/api/v1/danh-muc/dot-de-nghi/dang-mo"))[0];
        var linhVuc = (await LayDuLieuAsync(tacGia, "/api/v1/danh-muc/linh-vuc/chon"))[0];
        var loaiTacGia = (await LayDuLieuAsync(tacGia, "/api/v1/danh-muc/loai-tac-gia/chon"))[0];

        var noiDung = TaoNoiDungHoSo(
            dot.GetProperty("id").GetString()!,
            linhVuc.GetProperty("id").GetString()!,
            loaiTacGia.GetProperty("id").GetString()!);

        var taoPhanHoi = await tacGia.PostAsJsonAsync("/api/v1/sang-kien", noiDung);
        taoPhanHoi.EnsureSuccessStatusCode();

        var hoSoId = (await taoPhanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetString()!;

        hoSoId.Should().NotBeNullOrEmpty();

        // --- 2. Nop khi chua co minh chung -> phai bi chan ------------------------
        var nopThieu = await tacGia.PostAsync($"/api/v1/sang-kien/{hoSoId}/nop", null);

        nopThieu.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var loi = await nopThieu.Content.ReadFromJsonAsync<JsonElement>();
        loi.GetProperty("maLoi").GetString().Should().Be("THIEU_THANH_PHAN_BAT_BUOC");

        // --- 3. Tai len minh chung roi nop ---------------------------------------
        await TaiLenMinhChungAsync(tacGia, hoSoId);

        var nop = await tacGia.PostAsync($"/api/v1/sang-kien/{hoSoId}/nop", null);
        nop.EnsureSuccessStatusCode();

        var ketQuaNop = (await nop.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
        ketQuaNop.GetProperty("maHoSo").GetString().Should().StartWith("SK-");
        ketQuaNop.GetProperty("tenBuocHienTai").GetString().Should().Contain("Tiếp nhận");

        // --- 4. Tac gia KHONG duoc xu ly buoc tiep nhan ---------------------------
        var hanhDongTacGia = await LayHanhDongAsync(tacGia, hoSoId);
        hanhDongTacGia.Should().BeEmpty("tác giả không phải tác nhân của bước tiếp nhận");

        // --- 5. Can bo tiep nhan co du ba lua chon -------------------------------
        var hanhDongTiepNhan = await LayHanhDongAsync(tiepNhan, hoSoId);
        hanhDongTiepNhan.Should().HaveCountGreaterThan(1);

        /*
         * Ba nhanh cua buoc tiep nhan deu phai KHA DUNG: tiep nhan, yeu cau bo sung, tu choi.
         *
         * Truoc day phep kiem nay khang dinh nguoc lai — rang nhanh bo sung PHAI bi chan — va no
         * da dong bang chinh mot loi thanh hanh vi mong doi: hai nhanh "yeu cau bo sung" va "tu
         * choi tiep nhan" khai dieu kien tren bien `hanh_dong_nguoi_dung` ma khong co duong nao
         * dat gia tri, nen can bo tiep nhan khong bao gio yeu cau bo sung hay tu choi duoc ho so
         * nao. Co mot phep kiem bao ve loi thi loi song rat lau.
         */
        foreach (var ma in new[] { "DAT", "BO_SUNG_HO_SO", "TRA_LAI" })
        {
            var nhanh = hanhDongTiepNhan.First(h => h.GetProperty("ma").GetString() == ma);

            nhanh.GetProperty("biChan").GetBoolean().Should().BeFalse(
                $"nhánh '{ma}' do cán bộ chủ động chọn nên phải bấm được, "
                + "không phụ thuộc điều kiện dữ liệu nào");
        }

        // --- 6. Tiep nhan ho so ---------------------------------------------------
        var tiepNhanDat = hanhDongTiepNhan.First(h => h.GetProperty("ma").GetString() == "DAT");
        var sauTiepNhan = await ThucThiAsync(tiepNhan, hoSoId,
            tiepNhanDat.GetProperty("truongHopId").GetString()!, "Hồ sơ hợp lệ.");

        sauTiepNhan.GetProperty("tenBuocMoi").GetString().Should().Contain("Thẩm định");

        // --- 7. Tham dinh --------------------------------------------------------
        var hanhDongThamDinh = await LayHanhDongAsync(thuKy, hoSoId);
        var thamDinhDat = hanhDongThamDinh.First(h => h.GetProperty("ma").GetString() == "DAT");
        var sauThamDinh = await ThucThiAsync(thuKy, hoSoId,
            thamDinhDat.GetProperty("truongHopId").GetString()!, "Đạt thẩm định sơ bộ.");

        sauThamDinh.GetProperty("tenBuocMoi").GetString().Should().Contain("Phân công");

        // --- 8. Phan cong 3 thanh vien cham --------------------------------------
        var hoiDongTomTat = (await LayDuLieuPhanTrangAsync(thuKy, "/api/v1/hoi-dong?soDong=1"))[0];
        var hoiDongId = hoiDongTomTat.GetProperty("id").GetString()!;

        var hoiDong = await LayMotDuLieuAsync(thuKy, $"/api/v1/hoi-dong/{hoiDongId}");
        var thanhVien = hoiDong.GetProperty("thanhVien").EnumerateArray().ToList();

        var taiKhoanCham = new[] { "hoidong01", "hoidong02", "hoidong03" };
        var nguoiDungIds = new List<string>();

        foreach (var taiKhoan in taiKhoanCham)
        {
            var client = await _ungDung.TaoClientDaDangNhapAsync(taiKhoan);
            var toi = await LayMotDuLieuAsync(client, "/api/v1/xac-thuc/toi");
            nguoiDungIds.Add(toi.GetProperty("id").GetString()!);
        }

        var thanhVienIds = thanhVien
            .Where(tv => tv.TryGetProperty("nguoiDungId", out var nd)
                         && nd.ValueKind == JsonValueKind.String
                         && nguoiDungIds.Contains(nd.GetString()!))
            .Select(tv => tv.GetProperty("id").GetString()!)
            .ToList();

        thanhVienIds.Should().HaveCount(3);

        var phanCong = await thuKy.PostAsJsonAsync("/api/v1/danh-gia/phan-cong", new
        {
            hoiDongId,
            sangKienIds = new[] { hoSoId },
            thanhVienIds,
            hanHoanThanh = DateTimeOffset.UtcNow.AddDays(7),
            tuDongChiaDeu = false
        });

        phanCong.EnsureSuccessStatusCode();

        var hanhDongPhanCong = await LayHanhDongAsync(thuKy, hoSoId);
        var sauPhanCong = await ThucThiAsync(thuKy, hoSoId,
            hanhDongPhanCong.First(h => h.GetProperty("ma").GetString() == "DAT")
                .GetProperty("truongHopId").GetString()!,
            "Đã phân công chấm điểm.");

        sauPhanCong.GetProperty("tenBuocMoi").GetString().Should().Contain("Chấm điểm");

        // --- 9. Ba thanh vien cham diem ------------------------------------------
        foreach (var taiKhoan in taiKhoanCham)
        {
            var client = await _ungDung.TaoClientDaDangNhapAsync(taiKhoan);
            var phieu = await LayMotDuLieuAsync(client,
                $"/api/v1/danh-gia/phieu?sangKienId={hoSoId}&hoiDongId={hoiDongId}");

            var chiTiet = phieu.GetProperty("boTieuChi").GetProperty("danhSachNhom")
                .EnumerateArray()
                .SelectMany(n => n.GetProperty("danhSachTieuChi").EnumerateArray())
                .Select(t => new
                {
                    tieuChiId = t.GetProperty("id").GetString(),
                    // Cham 85% diem toi da de tong diem vuot nguong dat (>= 50).
                    diem = Math.Round(t.GetProperty("diemToiDa").GetDecimal() * 0.85m, 1)
                })
                .ToList();

            var gui = await client.PostAsJsonAsync("/api/v1/danh-gia/phieu/gui", new
            {
                sangKienId = hoSoId,
                hoiDongId,
                chiTiet,
                nhanXetChung = "Giải pháp có tính mới và khả năng áp dụng tốt."
            });

            gui.EnsureSuccessStatusCode();
        }

        // --- 10. Tong hop diem ---------------------------------------------------
        var tongHopPhanHoi = await thuKy.PostAsync(
            $"/api/v1/danh-gia/tong-hop?sangKienId={hoSoId}&hoiDongId={hoiDongId}", null);
        tongHopPhanHoi.EnsureSuccessStatusCode();

        var tongHop = (await tongHopPhanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu");

        tongHop.GetProperty("soPhieu").GetInt32().Should().Be(3);
        tongHop.GetProperty("dat").GetBoolean().Should().BeTrue();
        tongHop.GetProperty("diemCuoiCung").GetDecimal().Should().BeGreaterThan(50m);

        // --- 11. Quy tac TAT_CA: can du 3 thanh vien xac nhan --------------------
        JsonElement sauChamDiem = default;

        for (var i = 0; i < taiKhoanCham.Length; i++)
        {
            var client = await _ungDung.TaoClientDaDangNhapAsync(taiKhoanCham[i]);
            var hanhDong = await LayHanhDongAsync(client, hoSoId);

            sauChamDiem = await ThucThiAsync(client, hoSoId,
                hanhDong.First().GetProperty("truongHopId").GetString()!, "Đã chấm xong.");

            if (i < taiKhoanCham.Length - 1)
            {
                sauChamDiem.GetProperty("choThemTacNhan").GetBoolean().Should()
                    .BeTrue($"mới có {i + 1}/3 thành viên xác nhận");
            }
        }

        sauChamDiem.GetProperty("choThemTacNhan").GetBoolean().Should().BeFalse();
        sauChamDiem.GetProperty("tenBuocMoi").GetString().Should().Contain("Họp hội đồng");

        // --- 12. Chu tich ket luan Dat -------------------------------------------
        var hanhDongHop = await LayHanhDongAsync(chuTich, hoSoId);

        var nhanhKhongDat = hanhDongHop.First(h => h.GetProperty("ma").GetString() == "KHONG_DAT");
        nhanhKhongDat.GetProperty("biChan").GetBoolean().Should()
            .BeTrue("tổng điểm >= 50 nên nhánh Không đạt phải bị chặn");

        var nhanhDat = hanhDongHop.First(h =>
            h.GetProperty("ma").GetString() == "DAT" && !h.GetProperty("biChan").GetBoolean());

        var sauKetLuan = await ThucThiAsync(chuTich, hoSoId,
            nhanhDat.GetProperty("truongHopId").GetString()!, "Hội đồng thống nhất công nhận.");

        sauKetLuan.GetProperty("tenBuocMoi").GetString().Should().Contain("quyết định");

        // --- 13. Lanh dao ban hanh quyet dinh ------------------------------------
        var hanhDongBanHanh = await LayHanhDongAsync(lanhDao, hoSoId);
        var sauBanHanh = await ThucThiAsync(lanhDao, hoSoId,
            hanhDongBanHanh.First(h => !h.GetProperty("biChan").GetBoolean())
                .GetProperty("truongHopId").GetString()!,
            "Đồng ý ban hành quyết định công nhận.");

        sauBanHanh.GetProperty("daKetThucQuyTrinh").GetBoolean().Should().BeTrue();
        sauBanHanh.GetProperty("trangThaiTongMoi").GetString().Should().Be("DA_PHE_DUYET");

        // --- 14. Kiem chung ket qua cuoi cung ------------------------------------
        var chiTietHoSo = await LayMotDuLieuAsync(lanhDao, $"/api/v1/sang-kien/{hoSoId}");

        chiTietHoSo.GetProperty("trangThaiTong").GetString().Should().Be("DA_PHE_DUYET");
        chiTietHoSo.GetProperty("ketQua").GetString().Should().Be("DAT");
        chiTietHoSo.GetProperty("tongDiem").GetDecimal().Should().BeGreaterThan(50m);

        var tienDo = await LayDuLieuAsync(lanhDao, $"/api/v1/sang-kien/{hoSoId}/tien-do");
        tienDo.Should().HaveCountGreaterThanOrEqualTo(6, "timeline phải ghi đủ các bước đã đi qua");

        // --- 15. Bao cao ---------------------------------------------------------
        var baoCaoDat = await LayDuLieuAsync(lanhDao, "/api/v1/bao-cao/sang-kien-dat");
        baoCaoDat.Should().Contain(x =>
            x.GetProperty("maHoSo").GetString() == ketQuaNop.GetProperty("maHoSo").GetString());

        var xuatExcel = await lanhDao.GetAsync("/api/v1/bao-cao/sang-kien-dat/xuat-excel");
        xuatExcel.EnsureSuccessStatusCode();
        (await xuatExcel.Content.ReadAsByteArrayAsync()).Should().NotBeEmpty();
    }

    [Fact]
    public async Task Khong_Dang_Nhap_Thi_Bi_Tu_Choi()
    {
        var client = _ungDung.CreateClient();

        var phanHoi = await client.GetAsync("/api/v1/sang-kien");

        phanHoi.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Tac_Gia_Khong_Duoc_Cau_Hinh_Quy_Trinh()
    {
        var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");

        var phanHoi = await tacGia.PostAsync(
            $"/api/v1/quy-trinh/{Guid.NewGuid()}/kich-hoat", null);

        phanHoi.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Dang_Nhap_Sai_Mat_Khau_Tra_Ve_Ma_Loi_Nghiep_Vu()
    {
        var client = _ungDung.CreateClient();

        var phanHoi = await client.PostAsJsonAsync("/api/v1/xac-thuc/dang-nhap", new
        {
            tenDangNhap = "admin",
            matKhau = "sai-mat-khau"
        });

        phanHoi.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var noiDung = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
        noiDung.GetProperty("maLoi").GetString().Should().Be("SAI_TAI_KHOAN_MAT_KHAU");
        noiDung.GetProperty("thongBao").GetString().Should()
            .NotContain("admin", "thông báo không được tiết lộ tài khoản có tồn tại hay không");
    }

    [Fact]
    public async Task Tim_Kiem_Khong_Dau_Ra_Ket_Qua_Co_Dau()
    {
        var client = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var phanHoi = await client.GetAsync("/api/v1/sang-kien?tuKhoa=so hoa quy trinh&soDong=10");
        phanHoi.EnsureSuccessStatusCode();

        var noiDung = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
        var duLieu = noiDung.GetProperty("duLieu").EnumerateArray().ToList();

        duLieu.Should().NotBeEmpty();
        duLieu.Should().Contain(x => x.GetProperty("tenSangKien").GetString()!.Contains("Số hóa"));
    }

    [Fact]
    public async Task Phat_Hien_Cap_Ho_So_Trung_Lap_Trong_Du_Lieu_Mau()
    {
        var client = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var danhSach = await LayDuLieuPhanTrangAsync(client,
            "/api/v1/sang-kien?tuKhoa=Ung dung phan mem quan ly ho so mot cua&soDong=1");

        danhSach.Should().NotBeEmpty();
        var hoSoId = danhSach[0].GetProperty("id").GetString()!;

        var chay = await client.PostAsync($"/api/v1/sang-kien/{hoSoId}/trung-lap/chay-lai", null);
        chay.EnsureSuccessStatusCode();

        var ketQua = (await chay.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");

        ketQua.GetProperty("tyLeCaoNhat").GetDecimal().Should()
            .BeGreaterThan(40m, "cặp hồ sơ seed cố ý trùng phải vượt ngưỡng nghiêm trọng");
        ketQua.GetProperty("mucCanhBao").GetString().Should().Be("NGHIEM_TRONG");

        var chiTiet = ketQua.GetProperty("chiTiet").EnumerateArray().ToList();
        chiTiet.Should().NotBeEmpty();
        chiTiet[0].GetProperty("soDoanTrung").GetInt32().Should()
            .BeGreaterThan(0, "phải chỉ ra được các đoạn văn trùng cụ thể");
    }

    // ------------------------------------------------------------------------------------

    private static object TaoNoiDungHoSo(string dotId, string linhVucId, string loaiTacGiaId) => new
    {
        tenSangKien = $"Kiểm thử tích hợp {Guid.NewGuid():N}",
        dotDeNghiId = dotId,
        linhVucId,
        loaiTacGiaId,
        moTaGiaiPhap = string.Concat(Enumerable.Repeat(
            "Mô tả chi tiết giải pháp kiểm thử tự động cho hệ thống sáng kiến. ", 8)),
        tinhTrangTruocKhiApDung = string.Concat(Enumerable.Repeat(
            "Trước khi áp dụng phải thao tác thủ công rất mất thời gian. ", 4)),
        noiDungGiaiPhap = string.Concat(Enumerable.Repeat(
            "Nội dung chi tiết của giải pháp được trình bày đầy đủ theo từng bước. ", 10)),
        tinhMoi = string.Concat(Enumerable.Repeat(
            "Tính mới của giải pháp so với cách làm cũ tại đơn vị. ", 4)),
        khaNangApDung = string.Concat(Enumerable.Repeat(
            "Khả năng áp dụng rộng rãi cho các đơn vị tương tự. ", 4)),
        danhSachTacGia = new[]
        {
            new { hoTen = "Nguyễn Thị Lan", tyLeDongGop = 100, laTacGiaChinh = true }
        }
    };

    private static async Task TaiLenMinhChungAsync(HttpClient client, string hoSoId)
    {
        // Tep PDF toi thieu hop le de qua duoc buoc kiem tra magic number.
        var noiDungPdf = "%PDF-1.4\n1 0 obj<</Type/Catalog>>endobj\ntrailer<</Root 1 0 R>>\n%%EOF"u8
            .ToArray();

        using var form = new MultipartFormDataContent();
        var tep = new ByteArrayContent(noiDungPdf);
        tep.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

        form.Add(tep, "tep", "minh-chung.pdf");
        form.Add(new StringContent(hoSoId), "sangKienId");
        form.Add(new StringContent("MINH_CHUNG"), "thanhPhanHoSoMa");

        var phanHoi = await client.PostAsync("/api/v1/tep-tin/tai-len", form);
        phanHoi.EnsureSuccessStatusCode();
    }

    private static async Task<List<JsonElement>> LayHanhDongAsync(HttpClient client, string hoSoId)
        => await LayDuLieuAsync(client, $"/api/v1/sang-kien/{hoSoId}/hanh-dong");

    private static async Task<JsonElement> ThucThiAsync(
        HttpClient client, string hoSoId, string truongHopId, string yKien)
    {
        var phanHoi = await client.PostAsJsonAsync("/api/v1/xu-ly/thuc-thi", new
        {
            sangKienId = hoSoId,
            truongHopId,
            yKien
        });

        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
    }

    private static async Task<List<JsonElement>> LayDuLieuAsync(HttpClient client, string duongDan)
    {
        var phanHoi = await client.GetAsync(duongDan);
        phanHoi.EnsureSuccessStatusCode();

        var noiDung = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
        return noiDung.GetProperty("duLieu").EnumerateArray().ToList();
    }

    private static async Task<JsonElement> LayMotDuLieuAsync(HttpClient client, string duongDan)
    {
        var phanHoi = await client.GetAsync(duongDan);
        phanHoi.EnsureSuccessStatusCode();

        var noiDung = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
        return noiDung.GetProperty("duLieu");
    }

    /// <summary>Doc endpoint tra ve phan trang (khong boc trong truong duLieu cua PhanHoiApi).</summary>
    private static async Task<List<JsonElement>> LayDuLieuPhanTrangAsync(
        HttpClient client, string duongDan)
    {
        var phanHoi = await client.GetAsync(duongDan);
        phanHoi.EnsureSuccessStatusCode();

        var noiDung = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
        return noiDung.GetProperty("duLieu").EnumerateArray().ToList();
    }
}
