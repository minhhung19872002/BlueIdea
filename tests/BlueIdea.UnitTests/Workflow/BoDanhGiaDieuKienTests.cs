using System.Text.Json;
using BlueIdea.Domain.QuyTrinh;
using BlueIdea.Workflow.DieuKien;

namespace BlueIdea.UnitTests.Workflow;

/// <summary>Kiem thu rule evaluator - Muc 11 dac ta: "dieu kien long nhau, AND/OR/NOT".</summary>
public class BoDanhGiaDieuKienTests
{
    private readonly BoDanhGiaDieuKien _boDanhGia = new();

    private static BieuThucDieuKien SoSanh(string truong, string toanTu, object? giaTri)
        => new() { Truong = truong, ToanTu = toanTu, GiaTri = giaTri };

    private static BieuThucDieuKien Nhom(string phep, params BieuThucDieuKien[] con)
        => new() { Phep = phep, CacDieuKien = con.ToList() };

    [Fact]
    public void DieuKien_Null_Thi_Luon_Dung()
    {
        var ketQua = _boDanhGia.DanhGia(null, new NguCanhDieuKien());
        ketQua.Should().BeTrue("điều kiện trống nghĩa là nhánh luôn đi được");
    }

    [Theory]
    [InlineData(">=", 80, 80, true)]
    [InlineData(">=", 80, 79.99, false)]
    [InlineData(">", 80, 80, false)]
    [InlineData("<", 50, 49, true)]
    [InlineData("<=", 50, 50, true)]
    [InlineData("=", 75, 75, true)]
    [InlineData("!=", 75, 75, false)]
    public void So_Sanh_So_Hoc_Dung_Ket_Qua(string toanTu, double nguong, double thucTe, bool mongDoi)
    {
        var nguCanh = NguCanhDieuKien.Tu((BienNguCanh.TongDiem, (decimal)thucTe));
        var dieuKien = SoSanh(BienNguCanh.TongDiem, toanTu, nguong);

        _boDanhGia.DanhGia(dieuKien, nguCanh).Should().Be(mongDoi);
    }

    [Fact]
    public void So_Sanh_Chuoi_Khong_Phan_Biet_Hoa_Thuong()
    {
        var nguCanh = NguCanhDieuKien.Tu((BienNguCanh.CapXetDuyet, "CO_SO"));

        _boDanhGia.DanhGia(SoSanh(BienNguCanh.CapXetDuyet, "=", "co_so"), nguCanh).Should().BeTrue();
    }

    [Fact]
    public void So_Sanh_Chuoi_So_Voi_So_Van_Khop()
    {
        // Gia tri doc tu jsonb co the la chuoi "80" trong khi ngu canh la so 80.
        var nguCanh = NguCanhDieuKien.Tu((BienNguCanh.TongDiem, 80m));

        _boDanhGia.DanhGia(SoSanh(BienNguCanh.TongDiem, "=", "80"), nguCanh).Should().BeTrue();
        _boDanhGia.DanhGia(SoSanh(BienNguCanh.TongDiem, ">=", "80.0"), nguCanh).Should().BeTrue();
    }

    [Fact]
    public void Bien_Khong_Ton_Tai_Thi_So_Sanh_Thu_Tu_Tra_Ve_Sai()
    {
        var nguCanh = new NguCanhDieuKien();

        _boDanhGia.DanhGia(SoSanh("khong_ton_tai", ">=", 10), nguCanh).Should().BeFalse();
        _boDanhGia.DanhGia(SoSanh("khong_ton_tai", "=", null), nguCanh).Should().BeTrue();
    }

    [Fact]
    public void Toan_Tu_IN_Kiem_Tra_Thuoc_Danh_Sach()
    {
        var nguCanh = NguCanhDieuKien.Tu((BienNguCanh.KetQua, "DAT"));

        _boDanhGia.DanhGia(SoSanh(BienNguCanh.KetQua, "IN", new List<object> { "DAT", "CHUYEN_CAP" }), nguCanh)
            .Should().BeTrue();

        _boDanhGia.DanhGia(SoSanh(BienNguCanh.KetQua, "IN", new List<object> { "KHONG_DAT" }), nguCanh)
            .Should().BeFalse();
    }

    [Fact]
    public void Toan_Tu_CONTAINS_Tren_Danh_Sach_Va_Van_Ban()
    {
        var idLinhVuc = Guid.NewGuid();
        var nguCanhDanhSach = NguCanhDieuKien.Tu(("linh_vuc_ids", new List<object> { idLinhVuc.ToString() }));
        _boDanhGia.DanhGia(SoSanh("linh_vuc_ids", "CONTAINS", idLinhVuc.ToString()), nguCanhDanhSach)
            .Should().BeTrue();

        // Van ban: khong phan biet dau tieng Viet.
        var nguCanhVanBan = NguCanhDieuKien.Tu(("ten_sang_kien", "Chuyển đổi số trong quản lý"));
        _boDanhGia.DanhGia(SoSanh("ten_sang_kien", "CONTAINS", "chuyen doi so"), nguCanhVanBan)
            .Should().BeTrue("tìm kiếm không dấu phải ra kết quả có dấu");
    }

    [Fact]
    public void Toan_Tu_BETWEEN_Bao_Gom_Hai_Dau_Mut()
    {
        var dieuKien = SoSanh(BienNguCanh.TongDiem, "BETWEEN", new List<object> { 50, 80 });

        _boDanhGia.DanhGia(dieuKien, NguCanhDieuKien.Tu((BienNguCanh.TongDiem, 50m))).Should().BeTrue();
        _boDanhGia.DanhGia(dieuKien, NguCanhDieuKien.Tu((BienNguCanh.TongDiem, 80m))).Should().BeTrue();
        _boDanhGia.DanhGia(dieuKien, NguCanhDieuKien.Tu((BienNguCanh.TongDiem, 49.9m))).Should().BeFalse();
        _boDanhGia.DanhGia(dieuKien, NguCanhDieuKien.Tu((BienNguCanh.TongDiem, 80.1m))).Should().BeFalse();
    }

    [Fact]
    public void Nhom_AND_Yeu_Cau_Tat_Ca_Dieu_Kien_Con_Dung()
    {
        var dieuKien = Nhom("AND",
            SoSanh(BienNguCanh.TongDiem, ">=", 80),
            SoSanh(BienNguCanh.TyLeTrungLap, "<", 20));

        var dat = NguCanhDieuKien.Tu((BienNguCanh.TongDiem, 85m), (BienNguCanh.TyLeTrungLap, 10m));
        var truot = NguCanhDieuKien.Tu((BienNguCanh.TongDiem, 85m), (BienNguCanh.TyLeTrungLap, 30m));

        _boDanhGia.DanhGia(dieuKien, dat).Should().BeTrue();
        _boDanhGia.DanhGia(dieuKien, truot).Should().BeFalse();
    }

    [Fact]
    public void Nhom_OR_Chi_Can_Mot_Dieu_Kien_Dung()
    {
        var dieuKien = Nhom("OR",
            SoSanh(BienNguCanh.TongDiem, ">=", 90),
            SoSanh(BienNguCanh.SoPhieuDongY, ">=", 5));

        var nguCanh = NguCanhDieuKien.Tu((BienNguCanh.TongDiem, 60m), (BienNguCanh.SoPhieuDongY, 6));

        _boDanhGia.DanhGia(dieuKien, nguCanh).Should().BeTrue();
    }

    [Fact]
    public void Nhom_NOT_Dao_Nguoc_Ket_Qua()
    {
        var dieuKien = Nhom("NOT", SoSanh(BienNguCanh.TyLeTrungLap, ">", 40));
        var nguCanh = NguCanhDieuKien.Tu((BienNguCanh.TyLeTrungLap, 15m));

        _boDanhGia.DanhGia(dieuKien, nguCanh).Should().BeTrue();
    }

    [Fact]
    public void Bieu_Thuc_Long_Ba_Cap_Danh_Gia_Dung()
    {
        // (tổng điểm >= 80 VÀ (trùng lặp < 20 HOẶC KHÔNG(kết quả = KHONG_DAT)))
        var dieuKien = Nhom("AND",
            SoSanh(BienNguCanh.TongDiem, ">=", 80),
            Nhom("OR",
                SoSanh(BienNguCanh.TyLeTrungLap, "<", 20),
                Nhom("NOT", SoSanh(BienNguCanh.KetQua, "=", "KHONG_DAT"))));

        var nguCanh = NguCanhDieuKien.Tu(
            (BienNguCanh.TongDiem, 85m),
            (BienNguCanh.TyLeTrungLap, 55m),
            (BienNguCanh.KetQua, "DAT"));

        _boDanhGia.DanhGia(dieuKien, nguCanh).Should().BeTrue();

        var nguCanhTruot = NguCanhDieuKien.Tu(
            (BienNguCanh.TongDiem, 85m),
            (BienNguCanh.TyLeTrungLap, 55m),
            (BienNguCanh.KetQua, "KHONG_DAT"));

        _boDanhGia.DanhGia(dieuKien, nguCanhTruot).Should().BeFalse();
    }

    [Fact]
    public void Gia_Tri_Doc_Tu_Json_Duoc_Boc_Dung_Kieu()
    {
        // Mo phong dieu kien doc tu cot jsonb -> GiaTri la JsonElement.
        const string json = """
                            {"truong":"tong_diem","toanTu":">=","giaTri":80}
                            """;
        var bieuThuc = JsonSerializer.Deserialize<BieuThucDieuKien>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;

        bieuThuc.GiaTri.Should().BeOfType<JsonElement>();

        _boDanhGia.DanhGia(bieuThuc, NguCanhDieuKien.Tu((BienNguCanh.TongDiem, 80m)))
            .Should().BeTrue();
        _boDanhGia.DanhGia(bieuThuc, NguCanhDieuKien.Tu((BienNguCanh.TongDiem, 70m)))
            .Should().BeFalse();
    }

    [Fact]
    public void Mang_Trong_Json_Duoc_Boc_Cho_Toan_Tu_IN()
    {
        const string json = """
                            {"truong":"ket_qua","toanTu":"IN","giaTri":["DAT","CHUYEN_CAP_CAO_HON"]}
                            """;
        var bieuThuc = JsonSerializer.Deserialize<BieuThucDieuKien>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;

        _boDanhGia.DanhGia(bieuThuc, NguCanhDieuKien.Tu((BienNguCanh.KetQua, "DAT")))
            .Should().BeTrue();
        _boDanhGia.DanhGia(bieuThuc, NguCanhDieuKien.Tu((BienNguCanh.KetQua, "KHONG_DAT")))
            .Should().BeFalse();
    }

    [Fact]
    public void Toan_Tu_Khong_Ho_Tro_Bi_Bat_Loi_Chu_Khong_Nem_Exception()
    {
        var dieuKien = SoSanh(BienNguCanh.TongDiem, "LIKE", 80);

        var ketQua = _boDanhGia.DanhGiaChiTiet(dieuKien, NguCanhDieuKien.Tu((BienNguCanh.TongDiem, 80m)));

        ketQua.Khop.Should().BeFalse();
        ketQua.GiaiThich.Should().Contain("không hợp lệ");
    }

    [Fact]
    public void Kiem_Tra_Cu_Phap_Phat_Hien_Loi_Cau_Hinh()
    {
        var sai = Nhom("XOR", SoSanh("tong_diem", "LIKE", 1));

        var loi = _boDanhGia.KiemTraCuPhap(sai);

        loi.Should().HaveCount(2);
        loi.Should().Contain(l => l.Contains("XOR"));
        loi.Should().Contain(l => l.Contains("LIKE"));
    }

    [Fact]
    public void Kiem_Tra_Cu_Phap_Bat_Loi_NOT_Nhieu_Con()
    {
        var sai = Nhom("NOT",
            SoSanh("tong_diem", ">=", 1),
            SoSanh("tong_diem", "<=", 2));

        _boDanhGia.KiemTraCuPhap(sai).Should().Contain(l => l.Contains("NOT"));
    }

    [Fact]
    public void Kiem_Tra_Cu_Phap_Bat_Loi_BETWEEN_Sai_So_Phan_Tu()
    {
        var sai = SoSanh("tong_diem", "BETWEEN", new List<object> { 50 });

        _boDanhGia.KiemTraCuPhap(sai).Should().Contain(l => l.Contains("BETWEEN"));
    }

    [Fact]
    public void Bieu_Thuc_Long_Qua_Sau_Bi_Chan()
    {
        var goc = SoSanh("tong_diem", ">=", 1);
        var hienTai = goc;
        for (var i = 0; i < 25; i++)
        {
            hienTai = Nhom("NOT", hienTai);
        }

        var ketQua = _boDanhGia.DanhGiaChiTiet(hienTai, NguCanhDieuKien.Tu(("tong_diem", 10m)));

        ketQua.Khop.Should().BeFalse();
        ketQua.GiaiThich.Should().Contain("lồng quá");
    }

    [Fact]
    public void Nhom_Rong_AND_Dung_Va_OR_Sai()
    {
        _boDanhGia.DanhGia(new BieuThucDieuKien { Phep = "AND", CacDieuKien = new() }, new NguCanhDieuKien())
            .Should().BeTrue();
        _boDanhGia.DanhGia(new BieuThucDieuKien { Phep = "OR", CacDieuKien = new() }, new NguCanhDieuKien())
            .Should().BeFalse();
    }

    [Fact]
    public void Giai_Thich_Chua_Ten_Truong_Va_Ket_Luan()
    {
        var ketQua = _boDanhGia.DanhGiaChiTiet(
            SoSanh(BienNguCanh.TongDiem, ">=", 80),
            NguCanhDieuKien.Tu((BienNguCanh.TongDiem, 85m)));

        ketQua.Khop.Should().BeTrue();
        ketQua.GiaiThich.Should().Contain("tong_diem").And.Contain("ĐÚNG");
    }
}
