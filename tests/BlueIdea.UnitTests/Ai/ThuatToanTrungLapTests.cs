using BlueIdea.Ai.Nhung;
using BlueIdea.Ai.ThuatToan;
using BlueIdea.Ai.XuLyVanBan;

namespace BlueIdea.UnitTests.Ai;

/// <summary>
/// Kiem thu thuat toan SimHash / MinHash / Jaccard / TF-IDF cosine tren bo du lieu co dinh
/// (Muc 11 dac ta).
/// </summary>
public class ThuatToanTrungLapTests
{
    private const string VanBanGoc =
        "Ứng dụng công nghệ thông tin trong quản lý hồ sơ một cửa tại Ủy ban nhân dân phường. " +
        "Giải pháp giúp rút ngắn thời gian xử lý hồ sơ từ năm ngày xuống còn hai ngày, " +
        "giảm chi phí in ấn và tăng mức độ hài lòng của người dân.";

    private const string VanBanGiongHet = VanBanGoc;

    private const string VanBanSuaNhe =
        "Ứng dụng công nghệ thông tin trong quản lý hồ sơ một cửa tại Ủy ban nhân dân phường. " +
        "Giải pháp giúp rút ngắn thời gian xử lý hồ sơ từ năm ngày xuống còn ba ngày, " +
        "giảm chi phí in ấn và nâng cao mức độ hài lòng của người dân.";

    private const string VanBanKhacHan =
        "Mô hình trồng rau thủy canh trong nhà lưới phục vụ phát triển nông nghiệp đô thị. " +
        "Giải pháp tiết kiệm diện tích canh tác và hạn chế sử dụng thuốc bảo vệ thực vật.";

    // ---- SimHash ----

    [Fact]
    public void SimHash_Cua_Hai_Van_Ban_Giong_Het_Phai_Bang_Nhau()
    {
        SimHash.Tinh(VanBanGoc).Should().Be(SimHash.Tinh(VanBanGiongHet));
    }

    [Fact]
    public void SimHash_Tat_Dinh_Giua_Cac_Lan_Chay()
    {
        var lan1 = SimHash.Tinh(VanBanGoc);
        var lan2 = SimHash.Tinh(VanBanGoc);

        lan1.Should().Be(lan2, "SimHash phải tất định để so khớp giữa các lần chạy job");
    }

    [Fact]
    public void SimHash_Khong_Phu_Thuoc_Dau_Va_Hoa_Thuong()
    {
        SimHash.Tinh("Sáng kiến cải tiến kỹ thuật trong quản lý hành chính công")
            .Should().Be(SimHash.Tinh("SANG KIEN CAI TIEN KY THUAT TRONG QUAN LY HANH CHINH CONG"));
    }

    [Fact]
    public void SimHash_Van_Ban_Sua_Nhe_Nam_Trong_Nguong_Hamming()
    {
        var khoangCach = HamBam.KhoangCachHamming(SimHash.Tinh(VanBanGoc), SimHash.Tinh(VanBanSuaNhe));

        khoangCach.Should().BeLessThanOrEqualTo(SimHash.NguongHammingMacDinh,
            "sửa vài từ không được làm mất dấu vết near-duplicate");
        SimHash.LaUngVien(SimHash.Tinh(VanBanGoc), SimHash.Tinh(VanBanSuaNhe)).Should().BeTrue();
    }

    [Fact]
    public void SimHash_Van_Ban_Khac_Han_Vuot_Nguong_Hamming()
    {
        var khoangCach = HamBam.KhoangCachHamming(SimHash.Tinh(VanBanGoc), SimHash.Tinh(VanBanKhacHan));

        khoangCach.Should().BeGreaterThan(SimHash.NguongHammingMacDinh);
    }

    [Fact]
    public void SimHash_Van_Ban_Rong_Tra_Ve_Khong()
    {
        SimHash.Tinh("").Should().Be(0L);
        SimHash.Tinh(null).Should().Be(0L);
    }

    // ---- Shingle & Jaccard ----

    [Fact]
    public void Tao_Shingle_Dung_So_Luong()
    {
        var tu = new[] { "a", "b", "c", "d", "e", "f" };

        Shingle.TaoShingleTu(tu, 5).Should().HaveCount(2);
        Shingle.TaoShingleTu(tu, 3).Should().HaveCount(4);
    }

    [Fact]
    public void Shingle_Van_Ban_Ngan_Hon_Cua_So_Tra_Ve_Mot_Phan_Tu()
    {
        Shingle.TaoShingleTu(new[] { "a", "b" }, 5).Should().ContainSingle().Which.Should().Be("a b");
    }

    [Fact]
    public void Jaccard_Hai_Tap_Giong_Het_Bang_Mot()
    {
        DoTuongDong.JaccardVanBan(VanBanGoc, VanBanGiongHet).Should().Be(1d);
    }

    [Fact]
    public void Jaccard_Hai_Tap_Roi_Nhau_Bang_Khong()
    {
        DoTuongDong.Jaccard(new[] { "a", "b" }, new[] { "c", "d" }).Should().Be(0d);
    }

    [Fact]
    public void Jaccard_Tinh_Dung_Cong_Thuc_Giao_Tren_Hop()
    {
        // {a,b,c} ∩ {b,c,d} = {b,c} = 2 ; hợp = 4 -> 0.5
        DoTuongDong.Jaccard(new[] { "a", "b", "c" }, new[] { "b", "c", "d" }).Should().Be(0.5d);
    }

    [Fact]
    public void Jaccard_Van_Ban_Sua_Nhe_Cao_Hon_Van_Ban_Khac_Han()
    {
        var gan = DoTuongDong.JaccardVanBan(VanBanGoc, VanBanSuaNhe);
        var xa = DoTuongDong.JaccardVanBan(VanBanGoc, VanBanKhacHan);

        gan.Should().BeGreaterThan(0.5d);
        xa.Should().BeLessThan(0.05d);
    }

    // ---- MinHash ----

    [Fact]
    public void MinHash_Uoc_Luong_Jaccard_Sat_Voi_Gia_Tri_That()
    {
        var minHash = new MinHash(256);
        var thatSu = DoTuongDong.JaccardVanBan(VanBanGoc, VanBanSuaNhe);

        var uocLuong = MinHash.UocLuongJaccard(
            minHash.TinhChuKyTuVanBan(VanBanGoc),
            minHash.TinhChuKyTuVanBan(VanBanSuaNhe));

        uocLuong.Should().BeApproximately(thatSu, 0.12d,
            "MinHash 256 hàm băm phải ước lượng Jaccard với sai số nhỏ");
    }

    [Fact]
    public void MinHash_Hai_Van_Ban_Giong_Het_Cho_Chu_Ky_Giong_Het()
    {
        var minHash = new MinHash();

        minHash.TinhChuKyTuVanBan(VanBanGoc)
            .Should().Equal(minHash.TinhChuKyTuVanBan(VanBanGiongHet));
    }

    [Fact]
    public void MinHash_Lsh_Sinh_Khoa_Trung_Cho_Van_Ban_Gan_Giong()
    {
        var minHash = new MinHash();
        var khoaA = minHash.SinhKhoaLsh(minHash.TinhChuKyTuVanBan(VanBanGoc));
        var khoaB = minHash.SinhKhoaLsh(minHash.TinhChuKyTuVanBan(VanBanSuaNhe));

        khoaA.Intersect(khoaB).Should().NotBeEmpty("LSH phải đưa cặp gần giống vào cùng bucket");
    }

    [Fact]
    public void MinHash_So_Band_Vuot_So_Ham_Bam_Bi_Chan()
    {
        var minHash = new MinHash(16);
        var chuKy = minHash.TinhChuKyTuVanBan(VanBanGoc);

        var hanhDong = () => minHash.SinhKhoaLsh(chuKy, 32);

        hanhDong.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ---- TF-IDF cosine ----

    [Fact]
    public void TfIdf_Cosine_Van_Ban_Giong_Het_Bang_Mot()
    {
        var moHinh = MoHinhTfIdf.HuanLuyen(new[] { VanBanGoc, VanBanSuaNhe, VanBanKhacHan });

        moHinh.Cosine(VanBanGoc, VanBanGiongHet).Should().BeApproximately(1d, 1e-9);
    }

    [Fact]
    public void TfIdf_Cosine_Phan_Biet_Duoc_Van_Ban_Gan_Va_Xa()
    {
        var moHinh = MoHinhTfIdf.HuanLuyen(new[] { VanBanGoc, VanBanSuaNhe, VanBanKhacHan });

        var gan = moHinh.Cosine(VanBanGoc, VanBanSuaNhe);
        var xa = moHinh.Cosine(VanBanGoc, VanBanKhacHan);

        gan.Should().BeGreaterThan(0.7d);
        xa.Should().BeLessThan(0.2d);
        gan.Should().BeGreaterThan(xa);
    }

    [Fact]
    public void TfIdf_Vector_Duoc_Chuan_Hoa_L2()
    {
        var moHinh = MoHinhTfIdf.HuanLuyen(new[] { VanBanGoc, VanBanKhacHan });
        var vector = moHinh.TinhVector(VanBanGoc);

        var chuan = Math.Sqrt(vector.Values.Sum(v => v * v));
        chuan.Should().BeApproximately(1d, 1e-9);
    }

    [Fact]
    public void Cosine_Vector_Rong_Tra_Ve_Khong()
    {
        DoTuongDong.CosineThua(
            new Dictionary<string, double>(),
            new Dictionary<string, double> { ["a"] = 1d }).Should().Be(0d);
    }

    [Fact]
    public void Cosine_Dac_Khac_So_Chieu_Bi_Chan()
    {
        var hanhDong = () => DoTuongDong.CosineDac(new float[] { 1, 0 }, new float[] { 1, 0, 0 });

        hanhDong.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Levenshtein_Tinh_Dung()
    {
        DoTuongDong.Levenshtein("kitten", "sitting").Should().Be(3);
        DoTuongDong.Levenshtein("", "abc").Should().Be(3);
        DoTuongDong.Levenshtein("abc", "abc").Should().Be(0);
    }

    // ---- Nhung (embedding) noi bo ----

    [Fact]
    public async Task Bo_Nhung_Cho_Vector_Tat_Dinh_Va_Chuan_Hoa()
    {
        var boNhung = new BoNhungBamTuVung(768);

        var a = await boNhung.TaoVectorAsync(VanBanGoc);
        var b = await boNhung.TaoVectorAsync(VanBanGoc);

        a.Should().Equal(b);
        a.Should().HaveCount(768);
        Math.Sqrt(a.Sum(v => (double)v * v)).Should().BeApproximately(1d, 1e-5);
    }

    [Fact]
    public async Task Bo_Nhung_Cho_Cosine_Cao_Voi_Van_Ban_Gan_Giong()
    {
        var boNhung = new BoNhungBamTuVung();

        var goc = await boNhung.TaoVectorAsync(VanBanGoc);
        var suaNhe = await boNhung.TaoVectorAsync(VanBanSuaNhe);
        var khacHan = await boNhung.TaoVectorAsync(VanBanKhacHan);

        var gan = DoTuongDong.CosineDac(goc, suaNhe);
        var xa = DoTuongDong.CosineDac(goc, khacHan);

        gan.Should().BeGreaterThan(0.8d);
        xa.Should().BeLessThan(0.2d);
    }

    [Fact]
    public async Task Bo_Nhung_Van_Ban_Rong_Tra_Ve_Vector_Khong()
    {
        var boNhung = new BoNhungBamTuVung(64);

        var vector = await boNhung.TaoVectorAsync("");

        vector.Should().OnlyContain(v => v == 0f);
    }

    // ---- Cat doan van ----

    [Fact]
    public void Cat_Doan_Van_Theo_Cua_So_Truot_Co_Chong_Lan()
    {
        var vanBan = string.Join(' ', Enumerable.Range(1, 500).Select(i => $"tu{i}"));

        var doan = BoCatDoanVan.Cat(vanBan, new ThamSoCatDoan
        {
            SoTuMoiDoan = 200,
            SoTuChongLan = 50,
            SoTuToiThieu = 20
        });

        doan.Should().NotBeEmpty();
        doan[0].SoTu.Should().Be(200);
        doan[0].ChiMuc.Should().Be(0);

        // Buoc truot = 200 - 50 = 150 -> doan thu hai bat dau tu tu thu 151.
        doan[1].NoiDung.Should().StartWith("tu151 ");
    }

    [Fact]
    public void Cat_Doan_Van_Giu_Dung_Vi_Tri_Trong_Van_Ban_Goc()
    {
        var vanBan = string.Join(' ', Enumerable.Range(1, 300).Select(i => $"tu{i}"));

        var doan = BoCatDoanVan.Cat(vanBan, new ThamSoCatDoan { SoTuMoiDoan = 100, SoTuChongLan = 20 });

        foreach (var d in doan)
        {
            vanBan[d.ViTriBatDau..d.ViTriKetThuc].Should().Be(d.NoiDung,
                "vị trí lưu trong đoạn phải trỏ đúng vào văn bản gốc để UI highlight chính xác");
        }
    }

    [Fact]
    public void Cat_Doan_Van_Ngan_Tra_Ve_Mot_Doan()
    {
        var doan = BoCatDoanVan.Cat(VanBanGoc);

        doan.Should().ContainSingle();
        doan[0].NoiDungChuanHoa.Should().NotContain("của", "nội dung chuẩn hóa phải bỏ dấu và stopword");
    }

    [Fact]
    public void Cat_Doan_Van_Rong_Tra_Ve_Danh_Sach_Rong()
    {
        BoCatDoanVan.Cat("   ").Should().BeEmpty();
        BoCatDoanVan.Cat(null).Should().BeEmpty();
    }

    [Fact]
    public void Cat_Doan_Van_Chong_Lan_Lon_Hon_Kich_Thuoc_Bi_Chan()
    {
        var hanhDong = () => BoCatDoanVan.Cat(VanBanGoc, new ThamSoCatDoan
        {
            SoTuMoiDoan = 50,
            SoTuChongLan = 50
        });

        hanhDong.Should().Throw<ArgumentException>();
    }
}
