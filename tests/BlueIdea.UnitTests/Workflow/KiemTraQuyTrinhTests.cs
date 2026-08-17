using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuyTrinh;
using BlueIdea.UnitTests.TienIch;
using BlueIdea.Workflow.KiemTra;

namespace BlueIdea.UnitTests.Workflow;

/// <summary>Kiem thu 7 rule bat buoc cua validator quy trinh (Muc 5 - Nhom II dac ta).</summary>
public class KiemTraQuyTrinhTests
{
    private readonly KiemTraQuyTrinh _kiemTra = new();

    [Fact]
    public void Quy_Trinh_Mau_6_Buoc_Phai_Hop_Le()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();

        var ketQua = _kiemTra.KiemTra(quyTrinh);

        ketQua.HopLe.Should().BeTrue(
            "quy trình mẫu trong đặc tả phải qua được validator. Lỗi: "
            + string.Join(" | ", ketQua.DanhSachLoi.Select(l => l.ThongBao)));
    }

    [Fact]
    public void Quy_Trinh_Rong_Bao_Loi()
    {
        var quyTrinh = new QuyTrinh { Id = Guid.NewGuid(), Ma = "QT", Ten = "Rỗng" };

        var ketQua = _kiemTra.KiemTra(quyTrinh);

        ketQua.HopLe.Should().BeFalse();
        ketQua.DanhSachLoi.Should().ContainSingle(l => l.Ma == MaPhatHienQuyTrinh.KhongCoBuoc);
    }

    // ---- Rule 1: dung 1 buoc bat dau + it nhat 1 buoc ket thuc ----

    [Fact]
    public void Rule1_Khong_Co_Buoc_Bat_Dau_Bao_Loi()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        quyTrinh.DanhSachBuoc.First(b => b.LaBuocBatDau).LaBuocBatDau = false;

        var ketQua = _kiemTra.KiemTra(quyTrinh);

        ketQua.DanhSachLoi.Should().Contain(l => l.Ma == MaPhatHienQuyTrinh.KhongCoBuocBatDau);
    }

    [Fact]
    public void Rule1_Nhieu_Buoc_Bat_Dau_Bao_Loi()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        quyTrinh.DanhSachBuoc[1].LaBuocBatDau = true;

        var ketQua = _kiemTra.KiemTra(quyTrinh);

        ketQua.DanhSachLoi.Should().Contain(l => l.Ma == MaPhatHienQuyTrinh.NhieuBuocBatDau);
    }

    [Fact]
    public void Rule1_Khong_Co_Buoc_Ket_Thuc_Bao_Loi()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        foreach (var b in quyTrinh.DanhSachBuoc)
        {
            b.LaBuocKetThuc = false;
        }

        var ketQua = _kiemTra.KiemTra(quyTrinh);

        ketQua.DanhSachLoi.Should().Contain(l => l.Ma == MaPhatHienQuyTrinh.KhongCoBuocKetThuc);
    }

    // ---- Rule 2: buoc mo coi ----

    [Fact]
    public void Rule2_Buoc_Mo_Coi_Bao_Loi()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        var buocLe = XuongDuLieuTest.TaoBuoc("BX", "Bước lẻ không ai trỏ tới")
            .ThemTacNhanVaiTro(MaVaiTro.CanBoTiepNhan);
        buocLe.QuyTrinhId = quyTrinh.Id;
        buocLe.ThemTruongHop(MaTruongHop.Dat, "Xong", quyTrinh.DanhSachBuoc.Last(), laMacDinh: true);
        quyTrinh.DanhSachBuoc.Add(buocLe);

        var ketQua = _kiemTra.KiemTra(quyTrinh);

        ketQua.DanhSachLoi.Should().Contain(l =>
            l.Ma == MaPhatHienQuyTrinh.BuocMoCoi && l.TenBuoc == "Bước lẻ không ai trỏ tới");
    }

    // ---- Rule 3: buoc cut ----

    [Fact]
    public void Rule3_Buoc_Cut_Bao_Loi()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        var buocThamDinh = quyTrinh.DanhSachBuoc.First(b => b.Ma == "B2");
        buocThamDinh.TruongHop.Clear();

        var ketQua = _kiemTra.KiemTra(quyTrinh);

        ketQua.DanhSachLoi.Should().Contain(l =>
            l.Ma == MaPhatHienQuyTrinh.BuocCut && l.TenBuoc == "Thẩm định sơ bộ");
    }

    // ---- Rule 4: vong lap ----

    [Fact]
    public void Rule4_Vong_Lap_Khong_Co_Dieu_Kien_Thoat_Bao_Loi()
    {
        var a = XuongDuLieuTest.TaoBuoc("A", "Bước A", batDau: true).ThemTacNhanVaiTro(MaVaiTro.CanBoTiepNhan);
        var b = XuongDuLieuTest.TaoBuoc("B", "Bước B").ThemTacNhanVaiTro(MaVaiTro.CanBoTiepNhan);
        var ketThuc = XuongDuLieuTest.TaoBuoc("Z", "Kết thúc", LoaiBuoc.KetThuc, ketThuc: true);

        // A -> B -> A: khong nhanh nao co dieu kien -> lap vo han.
        a.ThemTruongHop("DI_B", "Sang B", b);
        b.ThemTruongHop("VE_A", "Về A", a);
        a.ThemTruongHop("XONG", "Kết thúc", ketThuc);

        var quyTrinh = TaoQuyTrinh(a, b, ketThuc);

        var ketQua = _kiemTra.KiemTra(quyTrinh);

        ketQua.DanhSachLoi.Should().Contain(l => l.Ma == MaPhatHienQuyTrinh.VongLapVoHan);
    }

    [Fact]
    public void Rule4_Vong_Lap_Co_Dieu_Kien_Thoat_Chi_Canh_Bao()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();

        // Quy trinh mau co vong Tiep nhan -> Tiep nhan (bo sung ho so) nhung co dieu kien.
        var ketQua = _kiemTra.KiemTra(quyTrinh);

        ketQua.HopLe.Should().BeTrue();
        ketQua.DanhSachCanhBao.Should().Contain(c => c.Ma == MaPhatHienQuyTrinh.VongLapVoHan);
    }

    // ---- Rule 5: tac nhan ----

    [Fact]
    public void Rule5_Buoc_Khong_Co_Tac_Nhan_Bao_Loi()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        quyTrinh.DanhSachBuoc.First(b => b.Ma == "B3").TacNhan.Clear();

        var ketQua = _kiemTra.KiemTra(quyTrinh);

        ketQua.DanhSachLoi.Should().Contain(l =>
            l.Ma == MaPhatHienQuyTrinh.BuocKhongCoTacNhan && l.TenBuoc == "Phân công chấm");
    }

    // ---- Rule 6: buoc cham diem ----

    [Fact]
    public void Rule6_Buoc_Cham_Diem_Thieu_Hoi_Dong_Hoac_Bo_Tieu_Chi_Bao_Loi()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        var buocCham = quyTrinh.DanhSachBuoc.First(b => b.LoaiBuoc == LoaiBuoc.ChamDiem);
        buocCham.HoiDongId = null;
        buocCham.BoTieuChiId = null;

        var ketQua = _kiemTra.KiemTra(quyTrinh);

        var loi = ketQua.DanhSachLoi.Should()
            .ContainSingle(l => l.Ma == MaPhatHienQuyTrinh.BuocChamDiemThieuCauHinh).Subject;
        loi.ThongBao.Should().Contain("hội đồng").And.Contain("bộ tiêu chí");
    }

    // ---- Rule 7: dieu kien chuyen tiep ----

    [Fact]
    public void Rule7_Khong_Co_Nhanh_Mac_Dinh_Thi_Canh_Bao_Khong_Phu_Het()
    {
        var a = XuongDuLieuTest.TaoBuoc("A", "Bước A", batDau: true).ThemTacNhanVaiTro(MaVaiTro.CanBoTiepNhan);
        var ketThuc = XuongDuLieuTest.TaoBuoc("Z", "Kết thúc", LoaiBuoc.KetThuc, ketThuc: true);

        a.ThemTruongHop("CAO", "Điểm cao", ketThuc,
            new BieuThucDieuKien { Truong = "tong_diem", ToanTu = ">=", GiaTri = 80 });
        a.ThemTruongHop("THAP", "Điểm thấp", ketThuc,
            new BieuThucDieuKien { Truong = "tong_diem", ToanTu = "<", GiaTri = 50 });

        var ketQua = _kiemTra.KiemTra(TaoQuyTrinh(a, ketThuc));

        ketQua.HopLe.Should().BeTrue("thiếu nhánh mặc định chỉ là cảnh báo");
        ketQua.DanhSachCanhBao.Should().Contain(c => c.Ma == MaPhatHienQuyTrinh.DieuKienKhongPhuHet);
    }

    [Fact]
    public void Rule7_Hai_Nhanh_Trung_Dieu_Kien_Thi_Canh_Bao_Mau_Thuan()
    {
        var a = XuongDuLieuTest.TaoBuoc("A", "Bước A", batDau: true).ThemTacNhanVaiTro(MaVaiTro.CanBoTiepNhan);
        var ketThuc = XuongDuLieuTest.TaoBuoc("Z", "Kết thúc", LoaiBuoc.KetThuc, ketThuc: true);

        a.ThemTruongHop("MOT", "Nhánh 1", ketThuc,
            new BieuThucDieuKien { Truong = "tong_diem", ToanTu = ">=", GiaTri = 80 });
        a.ThemTruongHop("HAI", "Nhánh 2", ketThuc,
            new BieuThucDieuKien { Truong = "tong_diem", ToanTu = ">=", GiaTri = 80 });
        a.ThemTruongHop("MAC_DINH", "Mặc định", ketThuc, laMacDinh: true);

        var ketQua = _kiemTra.KiemTra(TaoQuyTrinh(a, ketThuc));

        ketQua.DanhSachCanhBao.Should().Contain(c => c.Ma == MaPhatHienQuyTrinh.DieuKienMauThuan);
    }

    [Fact]
    public void Rule7_Dieu_Kien_Sai_Cu_Phap_Bao_Loi()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        quyTrinh.DanhSachBuoc[0].TruongHop[1].DieuKien =
            new BieuThucDieuKien { Truong = "tong_diem", ToanTu = "~~", GiaTri = 1 };

        var ketQua = _kiemTra.KiemTra(quyTrinh);

        ketQua.DanhSachLoi.Should().Contain(l => l.Ma == MaPhatHienQuyTrinh.DieuKienSaiCuPhap);
    }

    // ---- Kiem tra bo sung ----

    [Fact]
    public void Trung_Ma_Buoc_Bao_Loi()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        quyTrinh.DanhSachBuoc[1].Ma = quyTrinh.DanhSachBuoc[0].Ma;

        var ketQua = _kiemTra.KiemTra(quyTrinh);

        ketQua.DanhSachLoi.Should().Contain(l => l.Ma == MaPhatHienQuyTrinh.TrungMaBuoc);
    }

    [Fact]
    public void Truong_Hop_Tro_Toi_Buoc_Khong_Ton_Tai_Bao_Loi()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        quyTrinh.DanhSachBuoc[0].TruongHop[0].BuocTiepTheoId = Guid.NewGuid();

        var ketQua = _kiemTra.KiemTra(quyTrinh);

        ketQua.DanhSachLoi.Should().Contain(l => l.Ma == MaPhatHienQuyTrinh.BuocTiepTheoKhongTonTai);
    }

    [Fact]
    public void Buoc_Da_Xoa_Mem_Khong_Duoc_Tinh()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        var buocLe = XuongDuLieuTest.TaoBuoc("BX", "Bước đã xóa");
        buocLe.DaXoa = true;
        quyTrinh.DanhSachBuoc.Add(buocLe);

        var ketQua = _kiemTra.KiemTra(quyTrinh);

        ketQua.HopLe.Should().BeTrue();
        ketQua.PhatHien.Should().NotContain(p => p.TenBuoc == "Bước đã xóa");
    }

    private static QuyTrinh TaoQuyTrinh(params QuyTrinhBuoc[] buocs)
    {
        var quyTrinh = new QuyTrinh
        {
            Id = Guid.NewGuid(),
            Ma = "QT-TEST",
            Ten = "Quy trình test",
            TrangThaiQuyTrinh = TrangThaiQuyTrinh.Nhap
        };

        foreach (var b in buocs)
        {
            b.QuyTrinhId = quyTrinh.Id;
            quyTrinh.DanhSachBuoc.Add(b);
        }

        return quyTrinh;
    }
}
