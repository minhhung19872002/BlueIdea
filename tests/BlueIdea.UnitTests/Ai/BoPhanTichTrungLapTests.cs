using BlueIdea.Ai.TrungLap;
using BlueIdea.Ai.XuLyVanBan;
using BlueIdea.Domain.Chung;

namespace BlueIdea.UnitTests.Ai;

/// <summary>
/// Kiem thu pipeline phat hien trung lap end-to-end.
/// Muc tieu nghiem thu (Muc 12 - Phase 9): cap ho so seed trung ~60% phai bi phat hien dung.
/// </summary>
public class BoPhanTichTrungLapTests
{
    private readonly BoPhanTichTrungLap _boPhanTich = new();

    private const string DoanA1 =
        "Ứng dụng công nghệ thông tin trong quản lý hồ sơ một cửa tại Ủy ban nhân dân phường. " +
        "Trước khi áp dụng giải pháp, cán bộ phải nhập liệu thủ công trên sổ giấy, " +
        "thời gian tra cứu một hồ sơ trung bình mất mười lăm phút và dễ thất lạc.";

    private const string DoanA2 =
        "Giải pháp xây dựng phần mềm quản lý hồ sơ tập trung, cho phép quét mã vạch để tra cứu, " +
        "tự động nhắc hạn xử lý và thống kê tiến độ theo từng cán bộ. " +
        "Kết quả rút ngắn thời gian tra cứu xuống còn hai phút.";

    private const string DoanRiengB =
        "Bên cạnh đó đơn vị triển khai mô hình tiếp nhận hồ sơ lưu động tại khu dân cư, " +
        "bố trí tổ công tác hỗ trợ người cao tuổi và người khuyết tật hoàn thiện thủ tục tại nhà.";

    private const string DoanRiengC =
        "Mô hình trồng rau thủy canh trong nhà lưới phục vụ nông nghiệp đô thị, " +
        "tiết kiệm diện tích canh tác, hạn chế thuốc bảo vệ thực vật và cho năng suất cao gấp đôi.";

    private static TaiLieuSoSanh TaoTaiLieu(string ma, string ten, params string[] doanVan)
    {
        var toanVan = string.Join("\n\n", doanVan);
        var doan = doanVan.Select((noiDung, i) =>
        {
            var cat = BoCatDoanVan.Cat(noiDung, new ThamSoCatDoan
            {
                SoTuMoiDoan = 200,
                SoTuChongLan = 50,
                SoTuToiThieu = 5
            })[0];

            return new DoanVanSoSanh
            {
                ChiMuc = i,
                NoiDung = cat.NoiDung,
                NoiDungChuanHoa = cat.NoiDungChuanHoa,
                SimHash = cat.SimHash,
                ViTriBatDau = cat.ViTriBatDau,
                ViTriKetThuc = cat.ViTriKetThuc
            };
        }).ToList();

        return new TaiLieuSoSanh
        {
            SangKienId = Guid.NewGuid(),
            MaHoSo = ma,
            TenSangKien = ten,
            ToanVan = toanVan,
            CacDoan = doan
        };
    }

    [Fact]
    public async Task Phat_Hien_Cap_Ho_So_Sao_Chep_Toan_Bo()
    {
        var goc = TaoTaiLieu("SK-2026-0001", "Số hóa hồ sơ một cửa", DoanA1, DoanA2);
        var saoChep = TaoTaiLieu("SK-2026-0002", "Số hóa hồ sơ một cửa (bản sao)", DoanA1, DoanA2);
        var khacHan = TaoTaiLieu("SK-2026-0003", "Rau thủy canh", DoanRiengC);

        var ketQua = await _boPhanTich.PhanTichAsync(goc, new[] { saoChep, khacHan });

        ketQua.TyLeCaoNhat.Should().BeGreaterThan(90m);
        ketQua.MucCanhBao.Should().Be(MucCanhBaoTrungLap.NghiemTrong);
        ketQua.ChiTiet[0].SangKienDoiChieuId.Should().Be(saoChep.SangKienId);
        ketQua.ChiTiet[0].SoDoanTrung.Should().Be(2);
    }

    [Fact]
    public async Task Phat_Hien_Cap_Ho_So_Trung_Mot_Phan_Khoang_Sau_Muoi_Phan_Tram()
    {
        // Ho so B giu nguyen 2/3 noi dung cua A, phan con lai viet moi -> ky vong ~60%.
        var goc = TaoTaiLieu("SK-2026-0010", "Số hóa hồ sơ một cửa", DoanA1, DoanA2, DoanRiengB);
        var trungMotPhan = TaoTaiLieu("SK-2026-0011", "Cải tiến tiếp nhận hồ sơ", DoanA1, DoanA2, DoanRiengC);
        var khongLienQuan = TaoTaiLieu("SK-2026-0012", "Rau thủy canh", DoanRiengC);

        var ketQua = await _boPhanTich.PhanTichAsync(goc, new[] { trungMotPhan, khongLienQuan });

        var capTrung = ketQua.ChiTiet.First(c => c.SangKienDoiChieuId == trungMotPhan.SangKienId);

        capTrung.TyLeTuongDong.Should().BeInRange(40m, 85m,
            "hai hồ sơ chia sẻ khoảng 2/3 nội dung nên tỷ lệ phải nằm ở vùng cảnh báo");
        capTrung.SoDoanTrung.Should().Be(2, "đúng 2 đoạn được sao chép nguyên văn");
        ketQua.MucCanhBao.Should().NotBe(MucCanhBaoTrungLap.AnToan);
    }

    [Fact]
    public async Task Ho_So_Khong_Lien_Quan_Duoc_Xep_Muc_An_Toan()
    {
        var goc = TaoTaiLieu("SK-2026-0020", "Số hóa hồ sơ một cửa", DoanA1, DoanA2);
        var khac1 = TaoTaiLieu("SK-2026-0021", "Rau thủy canh", DoanRiengC);

        var ketQua = await _boPhanTich.PhanTichAsync(goc, new[] { khac1 });

        ketQua.TyLeCaoNhat.Should().BeLessThan(20m);
        ketQua.MucCanhBao.Should().Be(MucCanhBaoTrungLap.AnToan);
    }

    [Fact]
    public async Task Kho_Doi_Chieu_Rong_Tra_Ve_Ty_Le_Khong()
    {
        var goc = TaoTaiLieu("SK-2026-0030", "Số hóa hồ sơ", DoanA1);

        var ketQua = await _boPhanTich.PhanTichAsync(goc, Array.Empty<TaiLieuSoSanh>());

        ketQua.TongSoDoiChieu.Should().Be(0);
        ketQua.TyLeCaoNhat.Should().Be(0m);
        ketQua.MucCanhBao.Should().Be(MucCanhBaoTrungLap.AnToan);
        ketQua.ChiTiet.Should().BeEmpty();
    }

    [Fact]
    public async Task Khong_Doi_Chieu_Ho_So_Voi_Chinh_No()
    {
        var goc = TaoTaiLieu("SK-2026-0040", "Số hóa hồ sơ", DoanA1);

        var ketQua = await _boPhanTich.PhanTichAsync(goc, new[] { goc });

        ketQua.TongSoDoiChieu.Should().Be(0);
        ketQua.ChiTiet.Should().BeEmpty();
    }

    [Theory]
    [InlineData(5, MucCanhBaoTrungLap.AnToan)]
    [InlineData(19.99, MucCanhBaoTrungLap.AnToan)]
    [InlineData(20, MucCanhBaoTrungLap.CanhBao)]
    [InlineData(40, MucCanhBaoTrungLap.CanhBao)]
    [InlineData(40.01, MucCanhBaoTrungLap.NghiemTrong)]
    [InlineData(95, MucCanhBaoTrungLap.NghiemTrong)]
    public void Muc_Canh_Bao_Duoc_Xac_Dinh_Theo_Nguong_Cau_Hinh(double tyLe, string mongDoi)
    {
        KetQuaPhanTichTrungLap.TinhMucCanhBao((decimal)tyLe, ThamSoTrungLap.MacDinh)
            .Should().Be(mongDoi);
    }

    [Fact]
    public async Task He_So_Tu_Vung_Ngu_Nghia_Cau_Hinh_Duoc()
    {
        var goc = TaoTaiLieu("SK-2026-0050", "Số hóa hồ sơ", DoanA1, DoanA2);
        var khac = TaoTaiLieu("SK-2026-0051", "Bản chép", DoanA1, DoanA2);

        var chiTuVung = await _boPhanTich.PhanTichAsync(goc, new[] { khac },
            new ThamSoTrungLap { HeSoTuVung = 1m, HeSoNguNghia = 0m });
        var chiNguNghia = await _boPhanTich.PhanTichAsync(goc, new[] { khac },
            new ThamSoTrungLap { HeSoTuVung = 0m, HeSoNguNghia = 1m });

        chiTuVung.TyLeCaoNhat.Should().BeGreaterThan(90m);
        chiNguNghia.TyLeCaoNhat.Should().BeGreaterThan(90m);
    }

    [Fact]
    public async Task Ket_Qua_Ghi_Nhan_Phien_Ban_Thuat_Toan_Va_Mo_Hinh()
    {
        var goc = TaoTaiLieu("SK-2026-0060", "Số hóa hồ sơ", DoanA1);
        var khac = TaoTaiLieu("SK-2026-0061", "Khác", DoanRiengC);

        var ketQua = await _boPhanTich.PhanTichAsync(goc, new[] { khac });

        ketQua.PhienBanThuatToan.Should().Be("1.0");
        ketQua.TenMoHinhNhung.Should().NotBeNullOrEmpty("phải truy vết được mô hình đã dùng");
    }

    [Fact]
    public async Task Cap_Doan_Trung_Ghi_Dung_Vi_Tri_De_Highlight()
    {
        var goc = TaoTaiLieu("SK-2026-0070", "Số hóa hồ sơ", DoanA1, DoanA2);
        var saoChep = TaoTaiLieu("SK-2026-0071", "Bản chép", DoanA1, DoanA2);

        var ketQua = await _boPhanTich.PhanTichAsync(goc, new[] { saoChep });

        var doanTrung = ketQua.ChiTiet[0].CacDoanTrung;
        doanTrung.Should().NotBeEmpty();
        doanTrung[0].TyLe.Should().BeGreaterThan(70m);
        doanTrung[0].ViTriKetThuc.Should().BeGreaterThan(doanTrung[0].ViTriBatDau);
    }
}
