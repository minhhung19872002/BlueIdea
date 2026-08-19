using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuyTrinh;
using BlueIdea.Domain.SangKien;
using BlueIdea.Shared.KetQua;
using BlueIdea.UnitTests.TienIch;
using BlueIdea.Workflow;
using BlueIdea.Workflow.DieuKien;
using BlueIdea.Workflow.MoHinh;

namespace BlueIdea.UnitTests.Workflow;

/// <summary>Kiem thu loi engine quy trinh: quyen tac nhan, dieu kien chuyen tiep, quy tac TAT_CA/DA_SO.</summary>
public class BoMayQuyTrinhTests
{
    private static readonly DateTimeOffset ThoiDiemTest =
        new(2026, 8, 17, 9, 0, 0, TimeSpan.FromHours(7));

    private readonly BoMayQuyTrinh _boMay = new();

    private static NguCanhNguoiXuLy NguoiVoiVaiTro(params string[] vaiTro) => new()
    {
        NguoiDungId = Guid.NewGuid(),
        DonViId = Guid.NewGuid(),
        MaVaiTro = vaiTro.ToHashSet()
    };

    private static NguCanhNguoiXuLy NguoiVoiChucDanh(Guid hoiDongId, string chucDanh) => new()
    {
        NguoiDungId = Guid.NewGuid(),
        DonViId = Guid.NewGuid(),
        MaVaiTro = new HashSet<string>(),
        ChucDanhTheoHoiDong = new Dictionary<Guid, string> { [hoiDongId] = chucDanh }
    };

    private static NguCanhThucThi TaoNguCanh(
        HoSoSangKien hoSo,
        QuyTrinh quyTrinh,
        NguCanhNguoiXuLy nguoi,
        IReadOnlyList<SangKienXuLy>? lichSu = null,
        int soTacNhanDuKien = 1,
        IReadOnlyDictionary<string, object?>? bienBoSung = null) => new()
    {
        HoSo = hoSo,
        QuyTrinh = quyTrinh,
        NguoiXuLy = nguoi,
        LichSuXuLy = lichSu ?? Array.Empty<SangKienXuLy>(),
        SoTacNhanDuKien = soTacNhanDuKien,
        BienBoSung = bienBoSung ?? new Dictionary<string, object?>(),
        ThoiDiem = ThoiDiemTest
    };

    // ---- Khoi tao ----

    [Fact]
    public void Khoi_Tao_Dua_Ho_So_Vao_Buoc_Bat_Dau()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        var hoSo = XuongDuLieuTest.TaoHoSo();

        var ketQua = _boMay.KhoiTao(hoSo, quyTrinh, ThoiDiemTest, out var banGhi);

        ketQua.ThanhCong.Should().BeTrue();
        hoSo.BuocHienTaiId.Should().Be(quyTrinh.DanhSachBuoc.First(b => b.LaBuocBatDau).Id);
        hoSo.TrangThaiTong.Should().Be(TrangThaiTongHoSo.DaNop);
        hoSo.NgayNop.Should().Be(ThoiDiemTest);
        hoSo.HanXuLyHienTai.Should().NotBeNull();
        banGhi.Should().NotBeNull();
        banGhi!.TenBuocSnapshot.Should().Be("Tiếp nhận hồ sơ");
    }

    [Fact]
    public void Khoi_Tao_Voi_Quy_Trinh_Chua_Kich_Hoat_Bi_Tu_Choi()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        quyTrinh.TrangThaiQuyTrinh = TrangThaiQuyTrinh.Nhap;

        var ketQua = _boMay.KhoiTao(XuongDuLieuTest.TaoHoSo(), quyTrinh, ThoiDiemTest, out _);

        ketQua.ThanhCong.Should().BeFalse();
        ketQua.MaLoi.Should().Be(MaLoiHeThong.QuyTrinhChuaKichHoat);
    }

    [Fact]
    public void Han_Xu_Ly_Duoc_Tinh_Theo_Ngay_Lam_Viec_Cua_Buoc()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        var hoSo = XuongDuLieuTest.TaoHoSo();

        _boMay.KhoiTao(hoSo, quyTrinh, ThoiDiemTest, out _);

        // 17/8/2026 (Thứ Hai) + 3 ngày làm việc = 20/8/2026 (Thứ Năm).
        hoSo.HanXuLyHienTai!.Value.ToOffset(TimeSpan.FromHours(7)).Date
            .Should().Be(new DateTime(2026, 8, 20));
    }

    // ---- Quyen tac nhan ----

    [Fact]
    public void Nguoi_Dung_Khong_Dung_Vai_Tro_Khong_Co_Hanh_Dong_Nao()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        var hoSo = XuongDuLieuTest.TaoHoSo();
        _boMay.KhoiTao(hoSo, quyTrinh, ThoiDiemTest, out _);

        var nguCanh = TaoNguCanh(hoSo, quyTrinh, NguoiVoiVaiTro(MaVaiTro.TacGia));

        _boMay.LayHanhDongKhaDung(nguCanh).Should().BeEmpty();
        _boMay.KiemTraQuyenXuLy(nguCanh, hoSo.BuocHienTaiId!.Value).Should().BeFalse();
    }

    [Fact]
    public void Can_Bo_Tiep_Nhan_Thay_Du_Ba_Hanh_Dong_O_Buoc_Tiep_Nhan()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        var hoSo = XuongDuLieuTest.TaoHoSo();
        _boMay.KhoiTao(hoSo, quyTrinh, ThoiDiemTest, out _);

        var nguCanh = TaoNguCanh(hoSo, quyTrinh, NguoiVoiVaiTro(MaVaiTro.CanBoTiepNhan));

        var hanhDong = _boMay.LayHanhDongKhaDung(nguCanh);

        hanhDong.Should().HaveCount(3);
        hanhDong.Select(h => h.Ma).Should()
            .Contain(new[] { MaTruongHop.Dat, MaTruongHop.BoSungHoSo, MaTruongHop.TraLai });
    }

    [Fact]
    public void Hanh_Dong_Khong_Thoa_Dieu_Kien_Bi_Danh_Dau_Chan_Kem_Ly_Do()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        var hoSo = XuongDuLieuTest.TaoHoSo();
        _boMay.KhoiTao(hoSo, quyTrinh, ThoiDiemTest, out _);

        var nguCanh = TaoNguCanh(hoSo, quyTrinh, NguoiVoiVaiTro(MaVaiTro.CanBoTiepNhan));
        var boSung = _boMay.LayHanhDongKhaDung(nguCanh).First(h => h.Ma == MaTruongHop.BoSungHoSo);

        boSung.BiChan.Should().BeTrue("chưa có biến hanh_dong_nguoi_dung nên điều kiện không thỏa");
        boSung.LyDoChan.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Ho_So_Dang_Khoa_Khong_Co_Hanh_Dong_Nao()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        var hoSo = XuongDuLieuTest.TaoHoSo();
        _boMay.KhoiTao(hoSo, quyTrinh, ThoiDiemTest, out _);
        hoSo.DangKhoa = true;

        var nguCanh = TaoNguCanh(hoSo, quyTrinh, NguoiVoiVaiTro(MaVaiTro.CanBoTiepNhan));

        _boMay.LayHanhDongKhaDung(nguCanh).Should().BeEmpty();
    }

    // ---- Thuc thi ----

    [Fact]
    public void Thuc_Thi_Chuyen_Sang_Buoc_Tiep_Theo()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        var buocTiepNhan = quyTrinh.DanhSachBuoc[0];
        quyTrinh.ThemChucNangBoSung(MaChucNangBoSung.GuiEmail, buocTiepNhan.Id);
        quyTrinh.ThemChucNangBoSung(MaChucNangBoSung.KiemTraTrungLap, buocTiepNhan.Id);

        var hoSo = XuongDuLieuTest.TaoHoSo();
        _boMay.KhoiTao(hoSo, quyTrinh, ThoiDiemTest, out _);

        var nguoi = NguoiVoiVaiTro(MaVaiTro.CanBoTiepNhan);
        var nguCanh = TaoNguCanh(hoSo, quyTrinh, nguoi);
        var truongHopDat = buocTiepNhan.TruongHop.First(t => t.Ma == MaTruongHop.Dat);

        var ketQua = _boMay.ThucThi(nguCanh, new XuLyBuocRequest
        {
            SangKienId = hoSo.Id,
            NguoiDungId = nguoi.NguoiDungId,
            TruongHopId = truongHopDat.Id,
            YKien = "Hồ sơ hợp lệ, chuyển thẩm định."
        }, out var banGhi);

        ketQua.ThanhCong.Should().BeTrue(ketQua.ThongBao);
        ketQua.TenBuocMoi.Should().Be("Thẩm định sơ bộ");
        hoSo.BuocHienTaiId.Should().Be(quyTrinh.DanhSachBuoc[1].Id);
        hoSo.TrangThaiTong.Should().Be(TrangThaiTongHoSo.DangXuLy);
        banGhi.Should().NotBeNull();
        banGhi!.ThoiGianXuLy.Should().Be(ThoiDiemTest);
        ketQua.HanhDongCanChay.Should().Contain(HanhDongTuDong.KiemTraTrungLap);
    }

    [Fact]
    public void Thuc_Thi_Khi_Khong_Phai_Tac_Nhan_Bi_Tu_Choi()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        var hoSo = XuongDuLieuTest.TaoHoSo();
        _boMay.KhoiTao(hoSo, quyTrinh, ThoiDiemTest, out _);

        var nguoi = NguoiVoiVaiTro(MaVaiTro.TacGia);
        var nguCanh = TaoNguCanh(hoSo, quyTrinh, nguoi);

        var ketQua = _boMay.ThucThi(nguCanh, new XuLyBuocRequest
        {
            SangKienId = hoSo.Id,
            NguoiDungId = nguoi.NguoiDungId,
            TruongHopId = quyTrinh.DanhSachBuoc[0].TruongHop[0].Id
        }, out _);

        ketQua.ThanhCong.Should().BeFalse();
        ketQua.MaLoi.Should().Be(MaLoiHeThong.KhongCoQuyenXuLyBuoc);
    }

    [Fact]
    public void Thuc_Thi_Truong_Hop_Chua_Thoa_Dieu_Kien_Bi_Tu_Choi()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        var hoSo = XuongDuLieuTest.TaoHoSo();
        hoSo.BuocHienTaiId = quyTrinh.DanhSachBuoc.First(b => b.Ma == "B5").Id;
        hoSo.TongDiem = 30m; // < 50 nên không được chọn nhánh "Đạt"

        var nguoi = NguoiVoiVaiTro(MaVaiTro.ChuTichHoiDong);
        var nguCanh = TaoNguCanh(hoSo, quyTrinh, nguoi);
        var truongHopDat = quyTrinh.DanhSachBuoc
            .First(b => b.Ma == "B5").TruongHop.First(t => t.Ma == MaTruongHop.Dat);

        var ketQua = _boMay.ThucThi(nguCanh, new XuLyBuocRequest
        {
            SangKienId = hoSo.Id,
            NguoiDungId = nguoi.NguoiDungId,
            TruongHopId = truongHopDat.Id
        }, out _);

        ketQua.ThanhCong.Should().BeFalse();
        ketQua.MaLoi.Should().Be(MaLoiHeThong.KhongCoTruongHopPhuHop);
    }

    [Fact]
    public void Buoc_Bat_Buoc_Nhap_Y_Kien_Ma_De_Trong_Thi_Bi_Tu_Choi()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        quyTrinh.DanhSachBuoc[0].BatBuocNhapYKien = true;
        var hoSo = XuongDuLieuTest.TaoHoSo();
        _boMay.KhoiTao(hoSo, quyTrinh, ThoiDiemTest, out _);

        var nguoi = NguoiVoiVaiTro(MaVaiTro.CanBoTiepNhan);
        var ketQua = _boMay.ThucThi(TaoNguCanh(hoSo, quyTrinh, nguoi), new XuLyBuocRequest
        {
            SangKienId = hoSo.Id,
            NguoiDungId = nguoi.NguoiDungId,
            TruongHopId = quyTrinh.DanhSachBuoc[0].TruongHop[0].Id
        }, out _);

        ketQua.ThanhCong.Should().BeFalse();
        ketQua.ThongBao.Should().Contain("ý kiến");
    }

    [Fact]
    public void Buoc_Bat_Buoc_Dinh_Kem_Ma_Thieu_Tep_Thi_Bi_Tu_Choi()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        quyTrinh.DanhSachBuoc[0].BatBuocDinhKem = true;
        var hoSo = XuongDuLieuTest.TaoHoSo();
        _boMay.KhoiTao(hoSo, quyTrinh, ThoiDiemTest, out _);

        var nguoi = NguoiVoiVaiTro(MaVaiTro.CanBoTiepNhan);
        var ketQua = _boMay.ThucThi(TaoNguCanh(hoSo, quyTrinh, nguoi), new XuLyBuocRequest
        {
            SangKienId = hoSo.Id,
            NguoiDungId = nguoi.NguoiDungId,
            TruongHopId = quyTrinh.DanhSachBuoc[0].TruongHop[0].Id
        }, out _);

        ketQua.ThanhCong.Should().BeFalse();
        ketQua.ThongBao.Should().Contain("đính kèm");
    }

    [Fact]
    public void Xung_Dot_Phien_Ban_Ho_So_Bi_Chan()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        var hoSo = XuongDuLieuTest.TaoHoSo();
        _boMay.KhoiTao(hoSo, quyTrinh, ThoiDiemTest, out _);

        var nguoi = NguoiVoiVaiTro(MaVaiTro.CanBoTiepNhan);
        var ketQua = _boMay.ThucThi(TaoNguCanh(hoSo, quyTrinh, nguoi), new XuLyBuocRequest
        {
            SangKienId = hoSo.Id,
            NguoiDungId = nguoi.NguoiDungId,
            TruongHopId = quyTrinh.DanhSachBuoc[0].TruongHop[0].Id,
            PhienBanHoSo = hoSo.PhienBan + 5
        }, out _);

        ketQua.ThanhCong.Should().BeFalse();
        ketQua.MaLoi.Should().Be(MaLoiHeThong.XungDotDuLieu);
    }

    [Fact]
    public void Truong_Hop_Bo_Sung_Chuyen_Trang_Thai_Tong_Sang_Yeu_Cau_Bo_Sung()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        var hoSo = XuongDuLieuTest.TaoHoSo();
        _boMay.KhoiTao(hoSo, quyTrinh, ThoiDiemTest, out _);

        var nguoi = NguoiVoiVaiTro(MaVaiTro.CanBoTiepNhan);
        var nguCanh = TaoNguCanh(hoSo, quyTrinh, nguoi,
            bienBoSung: new Dictionary<string, object?> { ["hanh_dong_nguoi_dung"] = "BO_SUNG" });

        var truongHop = quyTrinh.DanhSachBuoc[0].TruongHop.First(t => t.Ma == MaTruongHop.BoSungHoSo);

        var ketQua = _boMay.ThucThi(nguCanh, new XuLyBuocRequest
        {
            SangKienId = hoSo.Id,
            NguoiDungId = nguoi.NguoiDungId,
            TruongHopId = truongHop.Id,
            YKien = "Thiếu báo cáo hiệu quả."
        }, out _);

        ketQua.ThanhCong.Should().BeTrue(ketQua.ThongBao);
        hoSo.TrangThaiTong.Should().Be(TrangThaiTongHoSo.YeuCauBoSung);
        hoSo.BuocHienTaiId.Should().Be(quyTrinh.DanhSachBuoc[0].Id, "hồ sơ quay lại bước tiếp nhận");
    }

    [Fact]
    public void Ket_Thuc_Quy_Trinh_Cap_Nhat_Ket_Qua_Dat()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        var hoSo = XuongDuLieuTest.TaoHoSo();
        hoSo.BuocHienTaiId = quyTrinh.DanhSachBuoc.First(b => b.Ma == "B6").Id;

        var nguoi = NguoiVoiVaiTro(MaVaiTro.LanhDaoPheDuyet);
        var truongHop = quyTrinh.DanhSachBuoc.First(b => b.Ma == "B6").TruongHop[0];

        var ketQua = _boMay.ThucThi(TaoNguCanh(hoSo, quyTrinh, nguoi), new XuLyBuocRequest
        {
            SangKienId = hoSo.Id,
            NguoiDungId = nguoi.NguoiDungId,
            TruongHopId = truongHop.Id
        }, out _);

        ketQua.ThanhCong.Should().BeTrue();
        ketQua.DaKetThucQuyTrinh.Should().BeTrue();
        hoSo.TrangThaiTong.Should().Be(TrangThaiTongHoSo.DaPheDuyet);
        hoSo.KetQua.Should().Be(KetQuaXetDuyetGiaTri.Dat);
        hoSo.NgayHoanThanh.Should().Be(ThoiDiemTest);
        hoSo.HanXuLyHienTai.Should().BeNull();
    }

    [Fact]
    public void Nhanh_Khong_Dat_Ket_Thuc_Voi_Ket_Qua_Khong_Dat()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        var hoSo = XuongDuLieuTest.TaoHoSo();
        hoSo.BuocHienTaiId = quyTrinh.DanhSachBuoc.First(b => b.Ma == "B5").Id;
        hoSo.TongDiem = 30m;

        var nguoi = NguoiVoiVaiTro(MaVaiTro.ChuTichHoiDong);
        var truongHop = quyTrinh.DanhSachBuoc
            .First(b => b.Ma == "B5").TruongHop.First(t => t.Ma == MaTruongHop.KhongDat);

        var ketQua = _boMay.ThucThi(TaoNguCanh(hoSo, quyTrinh, nguoi), new XuLyBuocRequest
        {
            SangKienId = hoSo.Id,
            NguoiDungId = nguoi.NguoiDungId,
            TruongHopId = truongHop.Id
        }, out _);

        ketQua.ThanhCong.Should().BeTrue(ketQua.ThongBao);
        hoSo.TrangThaiTong.Should().Be(TrangThaiTongHoSo.KhongDat);
        hoSo.KetQua.Should().Be(KetQuaXetDuyetGiaTri.KhongDat);
    }

    // ---- Quy tac TAT_CA / DA_SO ----

    [Fact]
    public void Quy_Tac_TAT_CA_Chua_Du_Nguoi_Thi_Chua_Chuyen_Buoc()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        var buocCham = quyTrinh.DanhSachBuoc.First(b => b.Ma == "B4");
        var hoSo = XuongDuLieuTest.TaoHoSo();
        hoSo.BuocHienTaiId = buocCham.Id;

        var nguoi = NguoiVoiVaiTro(MaVaiTro.ThanhVienHoiDong);
        var nguCanh = TaoNguCanh(hoSo, quyTrinh, nguoi, soTacNhanDuKien: 3);

        var ketQua = _boMay.ThucThi(nguCanh, new XuLyBuocRequest
        {
            SangKienId = hoSo.Id,
            NguoiDungId = nguoi.NguoiDungId,
            TruongHopId = buocCham.TruongHop[0].Id
        }, out _);

        ketQua.ThanhCong.Should().BeTrue();
        ketQua.ChoThemTacNhan.Should().BeTrue();
        ketQua.SoTacNhanCanThiet.Should().Be(3);
        ketQua.SoTacNhanDaXuLy.Should().Be(1);
        hoSo.BuocHienTaiId.Should().Be(buocCham.Id, "hồ sơ chưa được chuyển bước");
    }

    [Fact]
    public void Quy_Tac_TAT_CA_Du_Nguoi_Thi_Chuyen_Buoc()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        var buocCham = quyTrinh.DanhSachBuoc.First(b => b.Ma == "B4");
        var hoSo = XuongDuLieuTest.TaoHoSo();
        hoSo.BuocHienTaiId = buocCham.Id;

        // Hai thanh vien truoc da cham xong.
        var lichSu = new List<SangKienXuLy>
        {
            new()
            {
                Id = Guid.NewGuid(), SangKienId = hoSo.Id, BuocId = buocCham.Id,
                NguoiXuLyId = Guid.NewGuid(), ThoiGianXuLy = ThoiDiemTest.AddHours(-2)
            },
            new()
            {
                Id = Guid.NewGuid(), SangKienId = hoSo.Id, BuocId = buocCham.Id,
                NguoiXuLyId = Guid.NewGuid(), ThoiGianXuLy = ThoiDiemTest.AddHours(-1)
            }
        };

        var nguoi = NguoiVoiVaiTro(MaVaiTro.ThanhVienHoiDong);
        var nguCanh = TaoNguCanh(hoSo, quyTrinh, nguoi, lichSu, soTacNhanDuKien: 3);

        var ketQua = _boMay.ThucThi(nguCanh, new XuLyBuocRequest
        {
            SangKienId = hoSo.Id,
            NguoiDungId = nguoi.NguoiDungId,
            TruongHopId = buocCham.TruongHop[0].Id
        }, out _);

        ketQua.ChoThemTacNhan.Should().BeFalse();
        ketQua.TenBuocMoi.Should().Be("Họp hội đồng & kết luận");
    }

    [Fact]
    public void Quy_Tac_DA_SO_Chi_Can_Qua_Ban_So_Tac_Nhan()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        var buocCham = quyTrinh.DanhSachBuoc.First(b => b.Ma == "B4");
        buocCham.TacNhan.Clear();
        buocCham.ThemTacNhanVaiTro(MaVaiTro.ThanhVienHoiDong, QuyTacXuLy.DaSo, tyLeDongThuan: 50m);

        var hoSo = XuongDuLieuTest.TaoHoSo();
        hoSo.BuocHienTaiId = buocCham.Id;

        // 5 tac nhan, da so = floor(5*50/100)+1 = 3. Da co 2 nguoi cham -> nguoi thu 3 lam chuyen buoc.
        var lichSu = Enumerable.Range(0, 2).Select(i => new SangKienXuLy
        {
            Id = Guid.NewGuid(),
            SangKienId = hoSo.Id,
            BuocId = buocCham.Id,
            NguoiXuLyId = Guid.NewGuid(),
            ThoiGianXuLy = ThoiDiemTest.AddHours(-i - 1)
        }).ToList();

        var nguoi = NguoiVoiVaiTro(MaVaiTro.ThanhVienHoiDong);
        var nguCanh = TaoNguCanh(hoSo, quyTrinh, nguoi, lichSu, soTacNhanDuKien: 5);

        var ketQua = _boMay.ThucThi(nguCanh, new XuLyBuocRequest
        {
            SangKienId = hoSo.Id,
            NguoiDungId = nguoi.NguoiDungId,
            TruongHopId = buocCham.TruongHop[0].Id
        }, out _);

        ketQua.ChoThemTacNhan.Should().BeFalse("đã đủ 3/5 tác nhân theo quy tắc đa số");
        hoSo.BuocHienTaiId.Should().NotBe(buocCham.Id);
    }

    // ---- Snapshot quy trinh ----

    [Fact]
    public void Snapshot_Giu_Nguyen_Cau_Hinh_Quy_Trinh()
    {
        var boChuyenDoi = new BoChuyenDoiSnapshotQuyTrinh();
        var goc = XuongDuLieuTest.TaoQuyTrinhMau();

        var json = boChuyenDoi.TaoSnapshot(goc);
        var khoiPhuc = boChuyenDoi.DocSnapshot(json);

        khoiPhuc.Should().NotBeNull();
        khoiPhuc!.Id.Should().Be(goc.Id);
        khoiPhuc.DanhSachBuoc.Should().HaveCount(goc.DanhSachBuoc.Count);

        var buocGoc = goc.DanhSachBuoc[0];
        var buocKhoiPhuc = khoiPhuc.DanhSachBuoc.First(b => b.Id == buocGoc.Id);
        buocKhoiPhuc.TruongHop.Should().HaveCount(buocGoc.TruongHop.Count);
        buocKhoiPhuc.TacNhan.Should().HaveCount(buocGoc.TacNhan.Count);
        buocKhoiPhuc.TacNhan.First().ThamChieuMa.Should().Be(MaVaiTro.CanBoTiepNhan);
    }

    [Fact]
    public void Snapshot_Van_Chay_Duoc_Engine_Sau_Khi_Khoi_Phuc()
    {
        var boChuyenDoi = new BoChuyenDoiSnapshotQuyTrinh();
        var goc = XuongDuLieuTest.TaoQuyTrinhMau();
        var khoiPhuc = boChuyenDoi.DocSnapshot(boChuyenDoi.TaoSnapshot(goc))!;

        var hoSo = XuongDuLieuTest.TaoHoSo();
        _boMay.KhoiTao(hoSo, khoiPhuc, ThoiDiemTest, out _);

        var nguoi = NguoiVoiVaiTro(MaVaiTro.CanBoTiepNhan);
        var nguCanh = TaoNguCanh(hoSo, khoiPhuc, nguoi);

        _boMay.LayHanhDongKhaDung(nguCanh).Should().HaveCount(3);
    }

    [Fact]
    public void Doc_Snapshot_Hong_Tra_Ve_Null_Thay_Vi_Nem_Loi()
    {
        var boChuyenDoi = new BoChuyenDoiSnapshotQuyTrinh();

        boChuyenDoi.DocSnapshot("{khong-phai-json").Should().BeNull();
        boChuyenDoi.DocSnapshot(null).Should().BeNull();
        boChuyenDoi.DocSnapshot("   ").Should().BeNull();
    }

    // ---- Chuc nang bo sung (REQ-12) ----

    [Fact]
    public void Hanh_Dong_Can_Chay_Loc_Theo_Chuc_Nang_Bo_Sung_Dang_Bat()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        var buocTiepNhan = quyTrinh.DanhSachBuoc[0];
        quyTrinh.ThemChucNangBoSung(MaChucNangBoSung.GuiEmail, buocTiepNhan.Id);
        quyTrinh.ThemChucNangBoSung(MaChucNangBoSung.KiemTraTrungLap, buocTiepNhan.Id);

        var hoSo = XuongDuLieuTest.TaoHoSo();
        _boMay.KhoiTao(hoSo, quyTrinh, ThoiDiemTest, out _);

        var nguoi = NguoiVoiVaiTro(MaVaiTro.CanBoTiepNhan);
        var truongHopDat = buocTiepNhan.TruongHop.First(t => t.Ma == MaTruongHop.Dat);

        var ketQua = _boMay.ThucThi(TaoNguCanh(hoSo, quyTrinh, nguoi), new XuLyBuocRequest
        {
            SangKienId = hoSo.Id,
            NguoiDungId = nguoi.NguoiDungId,
            TruongHopId = truongHopDat.Id,
            YKien = "Hợp lệ"
        }, out _);

        ketQua.ThanhCong.Should().BeTrue(ketQua.ThongBao);
        ketQua.HanhDongCanChay.Should().Contain(HanhDongTuDong.GuiEmail);
        ketQua.HanhDongCanChay.Should().Contain(HanhDongTuDong.KiemTraTrungLap);
    }

    [Fact]
    public void Hanh_Dong_Can_Chay_Bi_Loc_Khi_Chuc_Nang_Khong_Bat()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        // No ChucNangBoSung added — all optional features are OFF.
        var hoSo = XuongDuLieuTest.TaoHoSo();
        _boMay.KhoiTao(hoSo, quyTrinh, ThoiDiemTest, out _);

        var nguoi = NguoiVoiVaiTro(MaVaiTro.CanBoTiepNhan);
        var truongHopDat = quyTrinh.DanhSachBuoc[0].TruongHop.First(t => t.Ma == MaTruongHop.Dat);

        var ketQua = _boMay.ThucThi(TaoNguCanh(hoSo, quyTrinh, nguoi), new XuLyBuocRequest
        {
            SangKienId = hoSo.Id,
            NguoiDungId = nguoi.NguoiDungId,
            TruongHopId = truongHopDat.Id,
            YKien = "Hợp lệ"
        }, out _);

        ketQua.ThanhCong.Should().BeTrue(ketQua.ThongBao);
        ketQua.HanhDongCanChay.Should().NotContain(HanhDongTuDong.GuiEmail,
            "GUI_EMAIL không bật nên hành động bị lọc bỏ");
        ketQua.HanhDongCanChay.Should().NotContain(HanhDongTuDong.KiemTraTrungLap,
            "KIEM_TRA_TRUNG_LAP không bật nên hành động bị lọc bỏ");
    }

    [Fact]
    public void Hanh_Dong_Khong_Chi_Phoi_Boi_Chuc_Nang_Van_Giu_Nguyen()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        // No ChucNangBoSung — but TaoQuyetDinh and CapNhatKetQua on B6 are not feature-gated.
        var hoSo = XuongDuLieuTest.TaoHoSo();
        hoSo.BuocHienTaiId = quyTrinh.DanhSachBuoc.First(b => b.Ma == "B6").Id;

        var nguoi = NguoiVoiVaiTro(MaVaiTro.LanhDaoPheDuyet);
        var truongHop = quyTrinh.DanhSachBuoc.First(b => b.Ma == "B6").TruongHop[0];

        var ketQua = _boMay.ThucThi(TaoNguCanh(hoSo, quyTrinh, nguoi), new XuLyBuocRequest
        {
            SangKienId = hoSo.Id,
            NguoiDungId = nguoi.NguoiDungId,
            TruongHopId = truongHop.Id
        }, out _);

        ketQua.ThanhCong.Should().BeTrue();
        ketQua.HanhDongCanChay.Should().Contain(HanhDongTuDong.TaoQuyetDinh,
            "TAO_QUYET_DINH không bị chi phối bởi chức năng bổ sung nên luôn giữ nguyên");
        ketQua.HanhDongCanChay.Should().Contain(HanhDongTuDong.CapNhatKetQua);
    }

    [Fact]
    public void Chuc_Nang_Toan_Quy_Trinh_Ap_Dung_Cho_Moi_Buoc()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        quyTrinh.ThemChucNangBoSung(MaChucNangBoSung.GuiEmail);  // BuocId = null → toàn quy trình

        var hoSo = XuongDuLieuTest.TaoHoSo();
        _boMay.KhoiTao(hoSo, quyTrinh, ThoiDiemTest, out _);

        var nguoi = NguoiVoiVaiTro(MaVaiTro.CanBoTiepNhan);
        var truongHopDat = quyTrinh.DanhSachBuoc[0].TruongHop.First(t => t.Ma == MaTruongHop.Dat);

        var ketQua = _boMay.ThucThi(TaoNguCanh(hoSo, quyTrinh, nguoi), new XuLyBuocRequest
        {
            SangKienId = hoSo.Id,
            NguoiDungId = nguoi.NguoiDungId,
            TruongHopId = truongHopDat.Id,
            YKien = "OK"
        }, out _);

        ketQua.ThanhCong.Should().BeTrue(ketQua.ThongBao);
        ketQua.HanhDongCanChay.Should().Contain(HanhDongTuDong.GuiEmail,
            "GUI_EMAIL bật toàn quy trình nên áp dụng cho bước tiếp nhận");
        ketQua.HanhDongCanChay.Should().NotContain(HanhDongTuDong.KiemTraTrungLap,
            "KIEM_TRA_TRUNG_LAP không bật nên vẫn bị lọc");
    }

    [Fact]
    public void Chuc_Nang_Bat_Tra_Ve_Danh_Sach_Chuc_Nang_Dang_Bat_Cho_Buoc()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        var buocTiepNhan = quyTrinh.DanhSachBuoc[0];
        quyTrinh.ThemChucNangBoSung(MaChucNangBoSung.KiemTraTrungLap, buocTiepNhan.Id);
        quyTrinh.ThemChucNangBoSung(MaChucNangBoSung.ChamDiemDocLap);  // toàn quy trình

        var hoSo = XuongDuLieuTest.TaoHoSo();
        _boMay.KhoiTao(hoSo, quyTrinh, ThoiDiemTest, out _);

        var nguoi = NguoiVoiVaiTro(MaVaiTro.CanBoTiepNhan);
        var truongHopDat = buocTiepNhan.TruongHop.First(t => t.Ma == MaTruongHop.Dat);

        var ketQua = _boMay.ThucThi(TaoNguCanh(hoSo, quyTrinh, nguoi), new XuLyBuocRequest
        {
            SangKienId = hoSo.Id,
            NguoiDungId = nguoi.NguoiDungId,
            TruongHopId = truongHopDat.Id,
            YKien = "OK"
        }, out _);

        ketQua.ChucNangBat.Should().Contain(MaChucNangBoSung.KiemTraTrungLap);
        ketQua.ChucNangBat.Should().Contain(MaChucNangBoSung.ChamDiemDocLap);
        ketQua.ChucNangBat.Should().NotContain(MaChucNangBoSung.GuiEmail);
    }

    [Fact]
    public void Hanh_Dong_Kha_Dung_Bao_Gom_Chuc_Nang_Buoc()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        var buocTiepNhan = quyTrinh.DanhSachBuoc[0];
        quyTrinh.ThemChucNangBoSung(MaChucNangBoSung.BoPhieuKin, buocTiepNhan.Id);
        quyTrinh.ThemChucNangBoSung(MaChucNangBoSung.XuatBieuMau);  // toàn quy trình

        var hoSo = XuongDuLieuTest.TaoHoSo();
        _boMay.KhoiTao(hoSo, quyTrinh, ThoiDiemTest, out _);

        var nguCanh = TaoNguCanh(hoSo, quyTrinh, NguoiVoiVaiTro(MaVaiTro.CanBoTiepNhan));
        var hanhDong = _boMay.LayHanhDongKhaDung(nguCanh);

        hanhDong.Should().NotBeEmpty();
        hanhDong.Should().AllSatisfy(hd =>
        {
            hd.ChucNangBuoc.Should().Contain(MaChucNangBoSung.BoPhieuKin);
            hd.ChucNangBuoc.Should().Contain(MaChucNangBoSung.XuatBieuMau);
            hd.ChucNangBuoc.Should().NotContain(MaChucNangBoSung.KySo);
        });
    }

    [Fact]
    public void Hanh_Dong_Pha_Tron_Loc_Chi_Hanh_Dong_Bi_Chi_Phoi_Chua_Bat()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        var buocBanHanh = quyTrinh.DanhSachBuoc.First(b => b.Ma == "B6");
        // B6 has [TaoQuyetDinh (non-gated), CapNhatKetQua (non-gated)].
        // Add GuiEmail (gated) to the transition but do NOT enable the feature toggle.
        buocBanHanh.TruongHop[0].HanhDong.Add(HanhDongTuDong.GuiEmail);

        var hoSo = XuongDuLieuTest.TaoHoSo();
        hoSo.BuocHienTaiId = buocBanHanh.Id;

        var nguoi = NguoiVoiVaiTro(MaVaiTro.LanhDaoPheDuyet);

        var ketQua = _boMay.ThucThi(TaoNguCanh(hoSo, quyTrinh, nguoi), new XuLyBuocRequest
        {
            SangKienId = hoSo.Id,
            NguoiDungId = nguoi.NguoiDungId,
            TruongHopId = buocBanHanh.TruongHop[0].Id
        }, out _);

        ketQua.ThanhCong.Should().BeTrue();
        ketQua.HanhDongCanChay.Should().Contain(HanhDongTuDong.TaoQuyetDinh,
            "non-gated action survives");
        ketQua.HanhDongCanChay.Should().Contain(HanhDongTuDong.CapNhatKetQua,
            "non-gated action survives");
        ketQua.HanhDongCanChay.Should().NotContain(HanhDongTuDong.GuiEmail,
            "gated action with feature OFF is filtered out");
    }

    [Fact]
    public void Snapshot_Giu_Nguyen_Chuc_Nang_Bo_Sung_Qua_Vong_Doi()
    {
        var boChuyenDoi = new BoChuyenDoiSnapshotQuyTrinh();
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        var buocTiepNhan = quyTrinh.DanhSachBuoc[0];
        quyTrinh.ThemChucNangBoSung(MaChucNangBoSung.GuiEmail, buocTiepNhan.Id);
        quyTrinh.ThemChucNangBoSung(MaChucNangBoSung.KiemTraTrungLap, buocTiepNhan.Id);

        var khoiPhuc = boChuyenDoi.DocSnapshot(boChuyenDoi.TaoSnapshot(quyTrinh))!;
        var hoSo = XuongDuLieuTest.TaoHoSo();
        _boMay.KhoiTao(hoSo, khoiPhuc, ThoiDiemTest, out _);

        var nguoi = NguoiVoiVaiTro(MaVaiTro.CanBoTiepNhan);
        var truongHopDat = khoiPhuc.DanhSachBuoc[0].TruongHop.First(t => t.Ma == MaTruongHop.Dat);

        var ketQua = _boMay.ThucThi(TaoNguCanh(hoSo, khoiPhuc, nguoi), new XuLyBuocRequest
        {
            SangKienId = hoSo.Id,
            NguoiDungId = nguoi.NguoiDungId,
            TruongHopId = truongHopDat.Id,
            YKien = "OK"
        }, out _);

        ketQua.ThanhCong.Should().BeTrue(ketQua.ThongBao);
        ketQua.HanhDongCanChay.Should().Contain(HanhDongTuDong.GuiEmail);
        ketQua.HanhDongCanChay.Should().Contain(HanhDongTuDong.KiemTraTrungLap);
        ketQua.ChucNangBat.Should().Contain(MaChucNangBoSung.GuiEmail);
        ketQua.ChucNangBat.Should().Contain(MaChucNangBoSung.KiemTraTrungLap);
    }

    // ---- Dieu kien tu ngu canh ho so ----

    [Fact]
    public void Ngu_Canh_Dieu_Kien_Chua_Day_Du_Bien_Nghiep_Vu()
    {
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau();
        var hoSo = XuongDuLieuTest.TaoHoSo();
        hoSo.TongDiem = 85m;
        hoSo.TyLeTrungLap = 12.5m;

        var nguCanh = _boMay.TaoNguCanhDieuKien(
            TaoNguCanh(hoSo, quyTrinh, NguoiVoiVaiTro(MaVaiTro.CanBoTiepNhan)));

        nguCanh.Lay(BienNguCanh.TongDiem).Should().Be(85m);
        nguCanh.Lay(BienNguCanh.TyLeTrungLap).Should().Be(12.5m);
        nguCanh.Lay(BienNguCanh.LinhVucId).Should().Be(hoSo.LinhVucId);
        nguCanh.Lay(BienNguCanh.CapXetDuyet).Should().Be(CapXetDuyet.CoSo);
    }

    // ---- Quy tac CHU_TICH_QUYET_DINH ----

    [Fact]
    public void Quy_Tac_CHU_TICH_QUYET_DINH_Chu_Tich_Chuyen_Buoc_Ngay()
    {
        var hoiDongId = Guid.NewGuid();
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau(hoiDongId: hoiDongId);
        var buocHop = quyTrinh.DanhSachBuoc.First(b => b.Ma == "B5");
        buocHop.TacNhan.Clear();
        buocHop.HoiDongId = hoiDongId;
        buocHop.ThemTacNhanChucDanh(ChucDanhHoiDong.UyVien, QuyTacXuLy.MotNguoi, hoiDongId);
        buocHop.ThemTacNhanChucDanh(ChucDanhHoiDong.ChuTich, QuyTacXuLy.ChuTichQuyetDinh, hoiDongId);

        var hoSo = XuongDuLieuTest.TaoHoSo();
        hoSo.BuocHienTaiId = buocHop.Id;
        hoSo.TongDiem = 80m;

        var chuTich = NguoiVoiChucDanh(hoiDongId, ChucDanhHoiDong.ChuTich);
        var nguCanh = TaoNguCanh(hoSo, quyTrinh, chuTich);
        var truongHopDat = buocHop.TruongHop.First(t => t.Ma == MaTruongHop.Dat);

        var ketQua = _boMay.ThucThi(nguCanh, new XuLyBuocRequest
        {
            SangKienId = hoSo.Id,
            NguoiDungId = chuTich.NguoiDungId,
            TruongHopId = truongHopDat.Id,
            YKien = "Đồng ý công nhận."
        }, out _);

        ketQua.ThanhCong.Should().BeTrue(ketQua.ThongBao);
        ketQua.ChoThemTacNhan.Should().BeFalse("chủ tịch quyết định nên chuyển bước ngay");
        hoSo.BuocHienTaiId.Should().NotBe(buocHop.Id, "đã rời khỏi bước họp hội đồng");
    }

    [Fact]
    public void Quy_Tac_CHU_TICH_QUYET_DINH_Uy_Vien_Ghi_Nhan_Nhung_Chua_Chuyen_Buoc()
    {
        var hoiDongId = Guid.NewGuid();
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau(hoiDongId: hoiDongId);
        var buocHop = quyTrinh.DanhSachBuoc.First(b => b.Ma == "B5");
        buocHop.TacNhan.Clear();
        buocHop.HoiDongId = hoiDongId;
        buocHop.ThemTacNhanChucDanh(ChucDanhHoiDong.UyVien, QuyTacXuLy.MotNguoi, hoiDongId);
        buocHop.ThemTacNhanChucDanh(ChucDanhHoiDong.ChuTich, QuyTacXuLy.ChuTichQuyetDinh, hoiDongId);

        var hoSo = XuongDuLieuTest.TaoHoSo();
        hoSo.BuocHienTaiId = buocHop.Id;
        hoSo.TongDiem = 80m;

        var uyVien = NguoiVoiChucDanh(hoiDongId, ChucDanhHoiDong.UyVien);
        var nguCanh = TaoNguCanh(hoSo, quyTrinh, uyVien);
        var truongHopDat = buocHop.TruongHop.First(t => t.Ma == MaTruongHop.Dat);

        var ketQua = _boMay.ThucThi(nguCanh, new XuLyBuocRequest
        {
            SangKienId = hoSo.Id,
            NguoiDungId = uyVien.NguoiDungId,
            TruongHopId = truongHopDat.Id,
            YKien = "Tôi đồng ý."
        }, out _);

        ketQua.ThanhCong.Should().BeTrue(ketQua.ThongBao);
        ketQua.ChoThemTacNhan.Should().BeTrue("ủy viên không phải chủ tịch nên bước chưa chuyển");
        ketQua.SoTacNhanCanThiet.Should().Be(1);
        ketQua.SoTacNhanDaXuLy.Should().Be(0);
        hoSo.BuocHienTaiId.Should().Be(buocHop.Id, "hồ sơ vẫn ở bước họp hội đồng");
    }

    [Fact]
    public void Quy_Tac_CHU_TICH_QUYET_DINH_Sau_Uy_Vien_Chu_Tich_Chuyen_Buoc()
    {
        var hoiDongId = Guid.NewGuid();
        var quyTrinh = XuongDuLieuTest.TaoQuyTrinhMau(hoiDongId: hoiDongId);
        var buocHop = quyTrinh.DanhSachBuoc.First(b => b.Ma == "B5");
        buocHop.TacNhan.Clear();
        buocHop.HoiDongId = hoiDongId;
        buocHop.ThemTacNhanChucDanh(ChucDanhHoiDong.UyVien, QuyTacXuLy.MotNguoi, hoiDongId);
        buocHop.ThemTacNhanChucDanh(ChucDanhHoiDong.ChuTich, QuyTacXuLy.ChuTichQuyetDinh, hoiDongId);

        var hoSo = XuongDuLieuTest.TaoHoSo();
        hoSo.BuocHienTaiId = buocHop.Id;
        hoSo.TongDiem = 80m;

        var lichSuUyVien = new List<SangKienXuLy>
        {
            new()
            {
                Id = Guid.NewGuid(), SangKienId = hoSo.Id, BuocId = buocHop.Id,
                NguoiXuLyId = Guid.NewGuid(), ThoiGianXuLy = ThoiDiemTest.AddHours(-1)
            }
        };

        var chuTich = NguoiVoiChucDanh(hoiDongId, ChucDanhHoiDong.ChuTich);
        var nguCanh = TaoNguCanh(hoSo, quyTrinh, chuTich, lichSuUyVien);
        var truongHopDat = buocHop.TruongHop.First(t => t.Ma == MaTruongHop.Dat);

        var ketQua = _boMay.ThucThi(nguCanh, new XuLyBuocRequest
        {
            SangKienId = hoSo.Id,
            NguoiDungId = chuTich.NguoiDungId,
            TruongHopId = truongHopDat.Id,
            YKien = "Chủ tịch kết luận: đồng ý."
        }, out _);

        ketQua.ThanhCong.Should().BeTrue(ketQua.ThongBao);
        ketQua.ChoThemTacNhan.Should().BeFalse("chủ tịch đã quyết định");
        hoSo.BuocHienTaiId.Should().NotBe(buocHop.Id);
    }
}
