using BlueIdea.Domain.Chung;
using BlueIdea.Domain.SangKien;
using BlueIdea.Scoring;
using BlueIdea.UnitTests.TienIch;

namespace BlueIdea.UnitTests.Scoring;

/// <summary>
/// Kiem thu engine tinh diem (Muc 11 dac ta): 3 cach tinh, loai diem cao/thap,
/// lam tron, xac dinh muc cong nhan.
/// </summary>
public class BoTinhDiemTests
{
    private readonly BoTinhDiem _boTinhDiem = new();

    [Fact]
    public void Tong_Diem_La_Tong_Diem_Cac_Tieu_Chi()
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau();
        // 7 tiêu chí: 20+10 | 15+15 | 15+10 | 15 = 100 điểm tối đa.
        var phieu = XuongDuLieuTest.TaoPhieu(bo, 18, 8, 12, 13, 12, 8, 12);

        var ketQua = _boTinhDiem.TinhDiemPhieu(phieu, bo);

        ketQua.HopLe.Should().BeTrue(string.Join(" | ", ketQua.DanhSachLoi));
        ketQua.TongDiem.Should().Be(83m);
        ketQua.TyLePhanTram.Should().Be(83m);
        ketQua.Dat.Should().BeTrue();
    }

    [Fact]
    public void Diem_Theo_Nhom_Duoc_Gom_Dung()
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau();
        var phieu = XuongDuLieuTest.TaoPhieu(bo, 18, 8, 12, 13, 12, 8, 12);

        var ketQua = _boTinhDiem.TinhDiemPhieu(phieu, bo);

        var nhomTinhMoi = bo.DanhSachNhom.First(n => n.Ma == "TINH_MOI");
        var nhomHieuQua = bo.DanhSachNhom.First(n => n.Ma == "HIEU_QUA");

        ketQua.DiemTheoNhom[nhomTinhMoi.Id].Should().Be(26m);
        ketQua.DiemTheoNhom[nhomHieuQua.Id].Should().Be(25m);
    }

    [Fact]
    public void Trung_Binh_Trong_So_Tinh_Theo_Cong_Thuc_Dac_Ta()
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau(CachTinhDiem.TrungBinhTrongSo);
        // Điểm nhóm: TINH_MOI=30, HIEU_QUA=30, AP_DUNG=25, PHAM_VI=15 (chấm tối đa).
        // Σ(điểm_nhóm × trọng_số)/100 = (30×30 + 30×30 + 25×25 + 15×15)/100 = 2650/100 = 26.5
        var phieu = XuongDuLieuTest.TaoPhieu(bo, 20, 10, 15, 15, 15, 10, 15);

        var ketQua = _boTinhDiem.TinhDiemPhieu(phieu, bo);

        ketQua.TongDiem.Should().Be(26.5m);
        ketQua.TongDiemTho.Should().Be(100m, "tổng thô vẫn là tổng điểm tiêu chí");
    }

    [Fact]
    public void Trung_Binh_Cong_La_Trung_Binh_Diem_Cac_Nhom()
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau(CachTinhDiem.TrungBinhCong);
        // 4 nhóm, chấm tối đa: TINH_MOI=30, HIEU_QUA=30, AP_DUNG=25, PHAM_VI=15.
        // Trung bình cộng = (30+30+25+15)/4 = 25.
        var phieu = XuongDuLieuTest.TaoPhieu(bo, 20, 10, 15, 15, 15, 10, 15);

        var ketQua = _boTinhDiem.TinhDiemPhieu(phieu, bo);

        ketQua.HopLe.Should().BeTrue(string.Join(" | ", ketQua.DanhSachLoi));
        ketQua.TongDiem.Should().Be(25m);
        ketQua.TongDiemTho.Should().Be(100m, "tổng thô vẫn là tổng điểm tiêu chí");
    }

    [Fact]
    public void Trung_Binh_Cong_Khong_Giong_Tong_Diem()
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau(CachTinhDiem.TrungBinhCong);
        // Chấm: TINH_MOI=18+8=26, HIEU_QUA=12+13=25, AP_DUNG=12+8=20, PHAM_VI=12.
        // Trung bình cộng = (26+25+20+12)/4 = 83/4 = 20.75
        var phieu = XuongDuLieuTest.TaoPhieu(bo, 18, 8, 12, 13, 12, 8, 12);

        var ketQua = _boTinhDiem.TinhDiemPhieu(phieu, bo);

        ketQua.HopLe.Should().BeTrue(string.Join(" | ", ketQua.DanhSachLoi));
        ketQua.TongDiem.Should().Be(20.75m);
        ketQua.TongDiemTho.Should().Be(83m);
    }

    [Fact]
    public void Diem_Vuot_Toi_Da_Cua_Tieu_Chi_Bi_Bao_Loi()
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau();
        var phieu = XuongDuLieuTest.TaoPhieu(bo, 25, 8, 12, 13, 12, 8, 12); // 25 > 20

        var ketQua = _boTinhDiem.TinhDiemPhieu(phieu, bo);

        ketQua.HopLe.Should().BeFalse();
        ketQua.DanhSachLoi.Should().Contain(l => l.Contains("vượt điểm tối đa"));
    }

    [Fact]
    public void Diem_Am_Bi_Bao_Loi()
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau();
        var phieu = XuongDuLieuTest.TaoPhieu(bo, -1, 8, 12, 13, 12, 8, 12);

        var ketQua = _boTinhDiem.TinhDiemPhieu(phieu, bo);

        ketQua.HopLe.Should().BeFalse();
        ketQua.DanhSachLoi.Should().Contain(l => l.Contains("không được âm"));
    }

    [Fact]
    public void Thieu_Tieu_Chi_Chua_Cham_Bi_Bao_Loi()
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau();
        var phieu = XuongDuLieuTest.TaoPhieu(bo, 18, 8, 12, 13, 12, 8, 12);
        phieu.ChiTiet.Remove(phieu.ChiTiet.Last());

        var ketQua = _boTinhDiem.TinhDiemPhieu(phieu, bo);

        ketQua.HopLe.Should().BeFalse();
        ketQua.DanhSachLoi.Should().Contain(l => l.Contains("Chưa chấm"));
    }

    [Fact]
    public void Tieu_Chi_Bat_Buoc_Nhan_Xet_Ma_De_Trong_Thi_Bao_Loi()
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau();
        bo.DanhSachNhom[0].DanhSachTieuChi[0].BatBuocNhanXet = true;
        var phieu = XuongDuLieuTest.TaoPhieu(bo, 18, 8, 12, 13, 12, 8, 12);

        var ketQua = _boTinhDiem.TinhDiemPhieu(phieu, bo);

        ketQua.DanhSachLoi.Should().Contain(l => l.Contains("bắt buộc nhập nhận xét"));
    }

    [Fact]
    public void Lam_Tron_Theo_Cau_Hinh_Lam_Tron()
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau(CachTinhDiem.TrungBinhTrongSo);
        bo.LamTron = 0;
        var phieu = XuongDuLieuTest.TaoPhieu(bo, 20, 10, 15, 15, 15, 10, 15); // 26.5 -> 27 (AwayFromZero)

        var ketQua = _boTinhDiem.TinhDiemPhieu(phieu, bo);

        ketQua.TongDiem.Should().Be(27m);
    }

    [Fact]
    public void Lam_Tron_Kieu_Away_From_Zero()
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau();
        bo.LamTron = 1;
        var phieu = XuongDuLieuTest.TaoPhieu(bo, 18.25m, 8, 12, 13, 12, 8, 12); // 83.25

        var ketQua = _boTinhDiem.TinhDiemPhieu(phieu, bo);

        ketQua.TongDiem.Should().Be(83.3m, "0.25 làm tròn 1 chữ số theo AwayFromZero phải ra 0.3");
    }

    // ---- Xac dinh muc cong nhan ----

    [Theory]
    [InlineData(0, "KHONG_CN")]
    [InlineData(49.99, "KHONG_CN")]
    [InlineData(50, "CAP_CO_SO")]
    [InlineData(79.99, "CAP_CO_SO")]
    [InlineData(80, "CAP_TP")]
    [InlineData(100, "CAP_TP")]
    public void Xac_Dinh_Muc_Cong_Nhan_Theo_Khoang_Diem(double diem, string maMongDoi)
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau();

        var muc = _boTinhDiem.XacDinhMucCongNhan((decimal)diem, bo);

        muc.Should().NotBeNull();
        muc!.Ma.Should().Be(maMongDoi);
    }

    [Fact]
    public void Diem_Ngoai_Moi_Khoang_Tra_Ve_Null()
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau();

        _boTinhDiem.XacDinhMucCongNhan(120m, bo).Should().BeNull();
    }

    // ---- Tong hop hoi dong ----

    [Fact]
    public void Tong_Hop_Lay_Trung_Binh_Cac_Phieu_Da_Gui()
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau();
        var phieus = new[]
        {
            XuongDuLieuTest.TaoPhieu(bo, 20, 10, 15, 15, 15, 10, 15), // 100
            XuongDuLieuTest.TaoPhieu(bo, 10, 5, 8, 7, 8, 5, 7),       // 50
            XuongDuLieuTest.TaoPhieu(bo, 15, 8, 12, 11, 12, 8, 11)    // 77
        };

        var ketQua = _boTinhDiem.TongHopDiemHoiDong(phieus, bo);

        ketQua.SoPhieu.Should().Be(3);
        ketQua.DiemCaoNhat.Should().Be(100m);
        ketQua.DiemThapNhat.Should().Be(50m);
        ketQua.DiemTrungBinh.Should().Be(75.67m);
        ketQua.Dat.Should().BeTrue();
        ketQua.MucCongNhan!.Ma.Should().Be("CAP_CO_SO");
    }

    [Fact]
    public void Phieu_Chua_Gui_Khong_Duoc_Tinh_Vao_Tong_Hop()
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau();
        var phieuDaGui = XuongDuLieuTest.TaoPhieu(bo, 20, 10, 15, 15, 15, 10, 15);
        var phieuNhap = XuongDuLieuTest.TaoPhieu(bo, 0, 0, 0, 0, 0, 0, 0);
        phieuNhap.TrangThaiPhieu = TrangThaiPhieuDanhGia.Nhap;

        var ketQua = _boTinhDiem.TongHopDiemHoiDong(new[] { phieuDaGui, phieuNhap }, bo);

        ketQua.SoPhieu.Should().Be(1);
        ketQua.DiemTrungBinh.Should().Be(100m);
    }

    [Fact]
    public void Khong_Co_Phieu_Nao_Thi_Tra_Ve_Canh_Bao()
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau();

        var ketQua = _boTinhDiem.TongHopDiemHoiDong(Array.Empty<PhieuDanhGia>(), bo);

        ketQua.SoPhieu.Should().Be(0);
        ketQua.DanhSachCanhBao.Should().ContainSingle();
    }

    [Fact]
    public void Loai_Bo_Diem_Cao_Thap_Khi_Du_5_Phieu()
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau(loaiBoDiemCaoThap: true);
        // Điểm: 100, 90, 80, 70, 20 -> loại 100 và 20 -> trung bình (90+80+70)/3 = 80
        var phieus = new[]
        {
            XuongDuLieuTest.TaoPhieu(bo, 20, 10, 15, 15, 15, 10, 15), // 100
            XuongDuLieuTest.TaoPhieu(bo, 18, 9, 14, 13, 13, 9, 14),   // 90
            XuongDuLieuTest.TaoPhieu(bo, 16, 8, 12, 12, 12, 8, 12),   // 80
            XuongDuLieuTest.TaoPhieu(bo, 14, 7, 11, 10, 10, 7, 11),   // 70
            XuongDuLieuTest.TaoPhieu(bo, 4, 2, 3, 3, 3, 2, 3)         // 20
        };

        var ketQua = _boTinhDiem.TongHopDiemHoiDong(phieus, bo);

        ketQua.SoPhieu.Should().Be(5);
        ketQua.SoPhieuSuDung.Should().Be(3);
        ketQua.PhieuBiLoai.Should().HaveCount(2);
        ketQua.DiemTrungBinh.Should().Be(80m);
    }

    [Fact]
    public void Khong_Loai_Diem_Cao_Thap_Khi_Duoi_5_Phieu_Va_Co_Canh_Bao()
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau(loaiBoDiemCaoThap: true);
        var phieus = new[]
        {
            XuongDuLieuTest.TaoPhieu(bo, 20, 10, 15, 15, 15, 10, 15), // 100
            XuongDuLieuTest.TaoPhieu(bo, 10, 5, 8, 7, 8, 5, 7)        // 50
        };

        var ketQua = _boTinhDiem.TongHopDiemHoiDong(phieus, bo);

        ketQua.SoPhieuSuDung.Should().Be(2);
        ketQua.PhieuBiLoai.Should().BeEmpty();
        ketQua.DanhSachCanhBao.Should().Contain(c => c.Contains("từ 5 phiếu"));
        ketQua.DiemTrungBinh.Should().Be(75m);
    }

    [Fact]
    public void Diem_Trung_Binh_Theo_Nhom_Duoc_Tinh_Cho_Bang_Tong_Hop()
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau();
        var phieus = new[]
        {
            XuongDuLieuTest.TaoPhieu(bo, 20, 10, 15, 15, 15, 10, 15),
            XuongDuLieuTest.TaoPhieu(bo, 10, 5, 8, 7, 8, 5, 7)
        };

        var ketQua = _boTinhDiem.TongHopDiemHoiDong(phieus, bo);

        var nhomTinhMoi = bo.DanhSachNhom.First(n => n.Ma == "TINH_MOI");
        ketQua.DiemTrungBinhTheoNhom[nhomTinhMoi.Id].Should().Be(22.5m); // (30 + 15)/2
    }

    // ---- Kiem tra cau hinh bo tieu chi ----

    [Fact]
    public void Bo_Tieu_Chi_Mau_Phai_Hop_Le()
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau();

        _boTinhDiem.KiemTraBoTieuChi(bo).Should().BeEmpty();
    }

    [Fact]
    public void Tong_Trong_So_Khac_100_Bi_Bao_Loi_Khi_Tinh_Theo_Trong_So()
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau(CachTinhDiem.TrungBinhTrongSo);
        bo.DanhSachNhom[0].TrongSo = 40m; // tổng thành 110

        var loi = _boTinhDiem.KiemTraBoTieuChi(bo);

        loi.Should().Contain(l => l.Contains("Tổng trọng số"));
    }

    [Fact]
    public void Khoang_Diem_Muc_Cong_Nhan_Chong_Lan_Bi_Bao_Loi()
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau();
        bo.DanhSachMucCongNhan.First(m => m.Ma == "CAP_TP").DiemTu = 70m; // chồng lấn với 50-79.99

        var loi = _boTinhDiem.KiemTraBoTieuChi(bo);

        loi.Should().Contain(l => l.Contains("chồng lấn"));
    }

    [Fact]
    public void Nhom_Khong_Co_Tieu_Chi_Con_Bi_Bao_Loi()
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau();
        bo.DanhSachNhom[0].DanhSachTieuChi.Clear();

        var loi = _boTinhDiem.KiemTraBoTieuChi(bo);

        loi.Should().Contain(l => l.Contains("chưa có tiêu chí con"));
    }
}
