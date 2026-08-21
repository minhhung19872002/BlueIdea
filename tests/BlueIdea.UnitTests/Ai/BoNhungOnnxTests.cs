using BlueIdea.Ai.Nhung;

namespace BlueIdea.UnitTests.Ai;

/// <summary>
/// Kiem thu bo nhung ONNX chay noi bo (TD-001).
///
/// Dung mot mo hinh ONNX TI HON co dung chu ky cua sentence-transformer
/// (input_ids + attention_mask -> last_hidden_state) thay vi tai mo hinh that vai tram MB:
/// phan can kiem la doan ma cua du an — tach tu, gop token co loai [PAD], chuan hoa L2 — chu
/// khong phai chat luong cua mo hinh nha cung cap.
/// </summary>
public class BoNhungOnnxTests
{
    private static string DuongDan(string ten)
        => Path.Combine(AppContext.BaseDirectory, "TaiNguyen", ten);

    private static CauHinhNhungOnnx CauHinh(int soChieuMongDoi = 8) => new()
    {
        DuongDanMoHinh = DuongDan("mo-hinh-nhung-thu.onnx"),
        DuongDanTuVung = DuongDan("tu-vung-thu.txt"),
        TenMoHinh = "mo-hinh-thu",
        SoChieuMongDoi = soChieuMongDoi,
        SoTokenToiDa = 32
    };

    // ---- Tach tu ----

    [Fact]
    public void Tach_Tu_Them_Cls_Sep_Va_Tach_Dau_Cau()
    {
        var tachTu = BoTachTuWordPiece.TuTep(DuongDan("tu-vung-thu.txt"));

        var (ids, matNa) = tachTu.Tach("Sáng kiến tiết kiệm điện.", 32);

        ids[0].Should().Be(tachTu.IdCls);
        ids[^1].Should().Be(tachTu.IdSep);

        // sáng kiến tiết kiệm điện . -> 6 token nghiệp vụ + [CLS] + [SEP]
        ids.Should().HaveCount(8);
        matNa.Should().OnlyContain(x => x == 1L);
        ids.Should().NotContain(tachTu.IdUnk, "mọi từ trong câu đều có trong từ vựng thử");
    }

    [Fact]
    public void Tu_Ngoai_Tu_Vung_Thanh_Unk_Chu_Khong_Lam_Hong_Ca_Cau()
    {
        var tachTu = BoTachTuWordPiece.TuTep(DuongDan("tu-vung-thu.txt"));

        var (ids, _) = tachTu.Tach("sáng zzzz kiến", 32);

        ids.Should().Contain(tachTu.IdUnk);
        ids.Should().HaveCount(5, "[CLS] sáng [UNK] kiến [SEP]");
    }

    [Fact]
    public void Tu_Ghep_Duoc_Tach_Theo_Manh_Noi_Tiep()
    {
        var tachTu = BoTachTuWordPiece.TuTep(DuongDan("tu-vung-thu.txt"));

        var (ids, _) = tachTu.Tach("trườnghọc", 32);

        // "trường" + "##học" — không được rơi về [UNK].
        ids.Should().HaveCount(4);
        ids.Should().NotContain(tachTu.IdUnk);
    }

    [Fact]
    public void Cat_Bot_Khi_Vuot_So_Token_Toi_Da()
    {
        var tachTu = BoTachTuWordPiece.TuTep(DuongDan("tu-vung-thu.txt"));

        var dai = string.Join(' ', Enumerable.Repeat("sáng kiến", 50));
        var (ids, matNa) = tachTu.Tach(dai, 16);

        ids.Should().HaveCount(16);
        ids[^1].Should().Be(tachTu.IdSep, "phải luôn đóng bằng [SEP] dù bị cắt");
        matNa.Should().HaveCount(16);
    }

    // ---- Suy luan ----

    [Fact]
    public async Task Vector_Duoc_Chuan_Hoa_L2_Va_Tat_Dinh()
    {
        using var boNhung = new BoNhungOnnx(CauHinh());

        var lan1 = await boNhung.TaoVectorAsync("sáng kiến tiết kiệm điện");
        var lan2 = await boNhung.TaoVectorAsync("sáng kiến tiết kiệm điện");

        boNhung.SoChieu.Should().Be(8);
        boNhung.TenMoHinh.Should().Be("mo-hinh-thu");

        var doDai = Math.Sqrt(lan1.Sum(v => (double)v * v));
        doDai.Should().BeApproximately(1.0, 1e-5);

        lan2.Should().Equal(lan1, "cùng một văn bản phải cho đúng một vector");
    }

    [Fact]
    public async Task Van_Ban_Khac_Nhau_Cho_Vector_Khac_Nhau()
    {
        using var boNhung = new BoNhungOnnx(CauHinh());

        var a = await boNhung.TaoVectorAsync("sáng kiến tiết kiệm điện");
        var b = await boNhung.TaoVectorAsync("nâng cao chất lượng");

        a.Should().NotEqual(b);
    }

    [Fact]
    public async Task Hang_Loat_Cho_Ket_Qua_Giong_Goi_Tung_Cai()
    {
        using var boNhung = new BoNhungOnnx(CauHinh());

        var vanBan = new[] { "sáng kiến", "nâng cao chất lượng", "hệthống" };

        var hangLoat = await boNhung.TaoVectorHangLoatAsync(vanBan);

        hangLoat.Should().HaveCount(3);

        for (var i = 0; i < vanBan.Length; i++)
        {
            hangLoat[i].Should().Equal(await boNhung.TaoVectorAsync(vanBan[i]));
        }
    }

    [Fact]
    public async Task Van_Ban_Rong_Cho_Vector_Hop_Le_Chu_Khong_No()
    {
        using var boNhung = new BoNhungOnnx(CauHinh());

        var vector = await boNhung.TaoVectorAsync("   ");

        vector.Should().HaveCount(8);
        vector.Should().OnlyContain(v => !float.IsNaN(v) && !float.IsInfinity(v));
    }

    // ---- Kiem tra luc nap ----

    [Fact]
    public void Sai_So_Chieu_Thi_Tu_Choi_Nap_Ngay_Luc_Khoi_Dong()
    {
        // Cột pgvector 768 chiều mà mô hình sinh 8 chiều: phải chặn ở đây, không để tới lúc ghi.
        var nap = () => new BoNhungOnnx(CauHinh(soChieuMongDoi: 768));

        nap.Should().Throw<InvalidOperationException>()
            .WithMessage("*768 chiều*");
    }

    [Fact]
    public void Thieu_Tep_Mo_Hinh_Thi_Bao_Loi_Ro_Rang()
    {
        var nap = () => new BoNhungOnnx(CauHinh() with
        {
            DuongDanMoHinh = DuongDan("khong-ton-tai.onnx")
        });

        nap.Should().Throw<FileNotFoundException>();
    }
}
