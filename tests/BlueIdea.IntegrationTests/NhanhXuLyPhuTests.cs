using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Cac nhanh xu ly KHONG nam tren duong "suon se".
///
/// <c>LuongNghiepVuTests</c> chay nhanh Dat — nhanh moi ho so tot deu di qua. Quy trinh mau co 12
/// nhanh; phan con lai la noi lo hay an, vi khong ai di nen khong ai phat hien no hong.
///
/// Hai loi that duoc bat bang chinh cac phep kiem o day:
///   1. Nhanh "yeu cau bo sung" va "tu choi tiep nhan" khai dieu kien tren bien khong ai dat duoc,
///      nen can bo tiep nhan khong bao gio dung duoc hai nhanh ay.
///   2. Tu choi tiep nhan dat trang thai YEU_CAU_BO_SUNG thay vi KHONG_DAT, khien ho so vua "da
///      dong quy trinh" vua "dang cho tac gia sua" — va tac gia nop lai duoc mot ho so khong con
///      buoc nao de di tiep.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class NhanhXuLyPhuTests
{
    private readonly UngDungKiemThu _ungDung;

    public NhanhXuLyPhuTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    [Fact]
    public async Task Yeu_Cau_Bo_Sung_Roi_Tac_Gia_Sua_Va_Nop_Lai()
    {
        var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");
        var tiepNhan = await _ungDung.TaoClientDaDangNhapAsync("tiepnhan");

        var hoSoId = await NopHoSoAsync(tacGia);

        await ThucThiNhanhAsync(tiepNhan, hoSoId, "BO_SUNG_HO_SO", "Thiếu bảng kê chi tiết.");

        (await LayHoSoAsync(tacGia, hoSoId)).GetProperty("trangThaiTong").GetString()
            .Should().Be("YEU_CAU_BO_SUNG");

        // Diem quan trong nhat: bi yeu cau bo sung thi tac gia phai SUA va NOP LAI duoc.
        var suaLai = await tacGia.PutAsJsonAsync($"/api/v1/sang-kien/{hoSoId}", TaoNoiDung(
            await LayHoSoAsync(tacGia, hoSoId), "Đã bổ sung theo yêu cầu."));

        suaLai.EnsureSuccessStatusCode();

        (await tacGia.PostAsync($"/api/v1/sang-kien/{hoSoId}/nop", null)).EnsureSuccessStatusCode();

        // Va can bo tiep nhan tiep nhan duoc sau khi tac gia bo sung.
        var sauTiepNhan = await ThucThiNhanhAsync(tiepNhan, hoSoId, "DAT", "Đã bổ sung đầy đủ.");

        sauTiepNhan.GetProperty("tenBuocMoi").GetString().Should().Contain("Thẩm định");
    }

    [Fact]
    public async Task Tu_Choi_Tiep_Nhan_Dong_Quy_Trinh_Va_Khong_Nop_Lai_Duoc()
    {
        var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");
        var tiepNhan = await _ungDung.TaoClientDaDangNhapAsync("tiepnhan");

        var hoSoId = await NopHoSoAsync(tacGia);

        var kq = await ThucThiNhanhAsync(tiepNhan, hoSoId, "TRA_LAI", "Không thuộc phạm vi đợt này.");

        kq.GetProperty("daKetThucQuyTrinh").GetBoolean().Should().BeTrue();

        (await LayHoSoAsync(tacGia, hoSoId)).GetProperty("trangThaiTong").GetString()
            .Should().Be("KHONG_DAT",
                "từ chối tiếp nhận là đóng quy trình; để YEU_CAU_BO_SUNG thì tác giả sửa rồi nộp "
                + "lại được một hồ sơ không còn bước nào để đi tiếp");

        var nopLai = await tacGia.PostAsync($"/api/v1/sang-kien/{hoSoId}/nop", null);

        nopLai.IsSuccessStatusCode.Should().BeFalse("hồ sơ đã bị từ chối thì không nộp lại được");
    }

    /// <summary>
    /// Bon vai tro trong chuoi xu ly deu duoc cap quyen SANG_KIEN.XEM_TAT_CA. Thieu buoc nay thi
    /// pham vi du lieu theo don vi chan ho lai: can bo tiep nhan ngoi o Phong Noi vu khong thay
    /// ho so cua truong hoc thuoc nhanh Phong GDDT, nen man hinh "Cho tiep nhan" TRONG TRON.
    /// </summary>
    [Theory]
    [InlineData("tiepnhan")]
    [InlineData("thuky")]
    [InlineData("chutich")]
    [InlineData("lanhdao")]
    public async Task Vai_Tro_Xu_Ly_Thay_Duoc_Ho_So_Cua_Don_Vi_Khac(string tenDangNhap)
    {
        var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");
        var hoSoId = await NopHoSoAsync(tacGia);

        var nguoiXuLy = await _ungDung.TaoClientDaDangNhapAsync(tenDangNhap);

        var danhSach = await nguoiXuLy.GetAsync("/api/v1/sang-kien?trang=1&soDong=100");
        danhSach.EnsureSuccessStatusCode();

        var duLieu = (await danhSach.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");

        duLieu.EnumerateArray()
            .Select(x => x.GetProperty("id").GetString())
            .Should().Contain(hoSoId,
                $"'{tenDangNhap}' có quyền SANG_KIEN.XEM_TAT_CA nên phải thấy hồ sơ này trong "
                + "danh sách, dù tác giả thuộc đơn vị ở nhánh khác của cây tổ chức");
    }

    // ---------------------------------------------------------------------------------

    private async Task<string> NopHoSoAsync(HttpClient tacGia)
    {
        var dot = await LayMotAsync(tacGia, "/api/v1/danh-muc/dot-de-nghi/dang-mo");
        var linhVuc = await LayMotAsync(tacGia, "/api/v1/danh-muc/linh-vuc/chon");

        var tao = await tacGia.PostAsJsonAsync("/api/v1/sang-kien", new
        {
            tenSangKien = $"Kiểm thử nhánh phụ {Guid.NewGuid():N}",
            dotDeNghiId = dot,
            linhVucId = linhVuc,
            moTaGiaiPhap = Lap("Mô tả chi tiết giải pháp cho nhánh phụ. ", 9),
            tinhTrangTruocKhiApDung = Lap("Trước khi áp dụng phải làm thủ công. ", 4),
            noiDungGiaiPhap = Lap("Nội dung chi tiết trình bày theo từng bước. ", 10),
            tinhMoi = Lap("Tính mới so với cách làm cũ tại đơn vị. ", 4),
            khaNangApDung = Lap("Khả năng áp dụng rộng cho đơn vị tương tự. ", 4),
            danhSachTacGia = new[] { new { hoTen = "Nguyễn Thị Lan", tyLeDongGop = 100, laTacGiaChinh = true } }
        });

        tao.EnsureSuccessStatusCode();
        var id = (await tao.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu").GetString()!;

        await TaiMinhChungAsync(tacGia, id);
        (await tacGia.PostAsync($"/api/v1/sang-kien/{id}/nop", null)).EnsureSuccessStatusCode();

        return id;
    }

    private static object TaoNoiDung(JsonElement hoSo, string moTaMoi) => new
    {
        tenSangKien = hoSo.GetProperty("tenSangKien").GetString(),
        dotDeNghiId = hoSo.GetProperty("dotDeNghiId").GetString(),
        linhVucId = hoSo.GetProperty("linhVucId").GetString(),
        moTaGiaiPhap = Lap(moTaMoi + " ", 9),
        tinhTrangTruocKhiApDung = hoSo.GetProperty("tinhTrangTruocKhiApDung").GetString(),
        noiDungGiaiPhap = hoSo.GetProperty("noiDungGiaiPhap").GetString(),
        tinhMoi = hoSo.GetProperty("tinhMoi").GetString(),
        khaNangApDung = hoSo.GetProperty("khaNangApDung").GetString(),
        danhSachTacGia = new[] { new { hoTen = "Nguyễn Thị Lan", tyLeDongGop = 100, laTacGiaChinh = true } }
    };

    private static string Lap(string doan, int lan) => string.Concat(Enumerable.Repeat(doan, lan));

    private static async Task TaiMinhChungAsync(HttpClient client, string hoSoId)
    {
        var pdf = "%PDF-1.4\n1 0 obj<</Type/Catalog>>endobj\ntrailer<</Root 1 0 R>>\n%%EOF"u8.ToArray();

        using var form = new MultipartFormDataContent();
        var tep = new ByteArrayContent(pdf);
        tep.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

        form.Add(tep, "tep", "minh-chung.pdf");
        form.Add(new StringContent(hoSoId), "sangKienId");
        form.Add(new StringContent("MINH_CHUNG"), "thanhPhanHoSoMa");

        (await client.PostAsync("/api/v1/tep-tin/tai-len", form)).EnsureSuccessStatusCode();
    }

    private static async Task<JsonElement> ThucThiNhanhAsync(
        HttpClient client, string hoSoId, string maNhanh, string yKien)
    {
        var hanhDong = await client.GetAsync($"/api/v1/sang-kien/{hoSoId}/hanh-dong");
        hanhDong.EnsureSuccessStatusCode();

        var nhanh = (await hanhDong.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").EnumerateArray()
            .First(x => x.GetProperty("ma").GetString() == maNhanh);

        nhanh.GetProperty("biChan").GetBoolean().Should().BeFalse(
            $"nhánh '{maNhanh}' do người xử lý chủ động chọn nên phải bấm được");

        var thucThi = await client.PostAsJsonAsync("/api/v1/xu-ly/thuc-thi", new
        {
            sangKienId = hoSoId,
            truongHopId = nhanh.GetProperty("truongHopId").GetString(),
            yKien
        });

        thucThi.EnsureSuccessStatusCode();

        return (await thucThi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
    }

    private static async Task<JsonElement> LayHoSoAsync(HttpClient client, string hoSoId)
    {
        var phanHoi = await client.GetAsync($"/api/v1/sang-kien/{hoSoId}");
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
    }

    private static async Task<string> LayMotAsync(HttpClient client, string duongDan)
    {
        var phanHoi = await client.GetAsync(duongDan);
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").EnumerateArray().First().GetProperty("id").GetString()!;
    }
}
