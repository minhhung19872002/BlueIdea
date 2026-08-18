using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuyTrinh;
using BlueIdea.Domain.SangKien;
using BlueIdea.Domain.TieuChi;

namespace BlueIdea.UnitTests.TienIch;

/// <summary>
/// Xuong tao du lieu mau cho test: quy trinh, bo tieu chi, ho so.
/// Giup test doc duoc va tranh lap lai cau hinh dai dong.
/// </summary>
public static class XuongDuLieuTest
{
    public static QuyTrinhBuoc TaoBuoc(
        string ma,
        string ten,
        string loaiBuoc = LoaiBuoc.TiepNhan,
        bool batDau = false,
        bool ketThuc = false,
        int soNgayXuLy = 3,
        Guid? hoiDongId = null,
        Guid? boTieuChiId = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Ma = ma,
            Ten = ten,
            LoaiBuoc = loaiBuoc,
            LaBuocBatDau = batDau,
            LaBuocKetThuc = ketThuc,
            SoNgayXuLy = soNgayXuLy,
            HoiDongId = hoiDongId,
            BoTieuChiId = boTieuChiId
        };

    public static QuyTrinhBuoc ThemTacNhanVaiTro(
        this QuyTrinhBuoc buoc,
        string maVaiTro,
        string quyTac = QuyTacXuLy.MotNguoi,
        decimal? tyLeDongThuan = null)
    {
        buoc.TacNhan.Add(new QuyTrinhBuocTacNhan
        {
            Id = Guid.NewGuid(),
            BuocId = buoc.Id,
            LoaiTacNhan = LoaiTacNhan.VaiTro,
            ThamChieuMa = maVaiTro,
            QuyTacXuLy = quyTac,
            TyLeDongThuan = tyLeDongThuan
        });

        return buoc;
    }

    public static QuyTrinhTruongHop ThemTruongHop(
        this QuyTrinhBuoc buoc,
        string ma,
        string ten,
        QuyTrinhBuoc? buocTiepTheo,
        BieuThucDieuKien? dieuKien = null,
        bool laMacDinh = false,
        params string[] hanhDong)
    {
        var th = new QuyTrinhTruongHop
        {
            Id = Guid.NewGuid(),
            BuocId = buoc.Id,
            Ma = ma,
            Ten = ten,
            BuocTiepTheoId = buocTiepTheo?.Id,
            DieuKien = dieuKien,
            LaMacDinh = laMacDinh,
            HanhDong = hanhDong.ToList(),
            ThuTu = buoc.TruongHop.Count + 1
        };

        buoc.TruongHop.Add(th);
        return th;
    }

    /// <summary>
    /// Quy trinh mau 6 buoc theo dac ta Muc 10:
    /// Tiep nhan → Tham dinh → Phan cong cham → Cham diem → Hop hoi dong → Ban hanh quyet dinh.
    /// </summary>
    public static QuyTrinh TaoQuyTrinhMau(Guid? hoiDongId = null, Guid? boTieuChiId = null)
    {
        hoiDongId ??= Guid.NewGuid();
        boTieuChiId ??= Guid.NewGuid();

        var tiepNhan = TaoBuoc("B1", "Tiếp nhận hồ sơ", LoaiBuoc.TiepNhan, batDau: true)
            .ThemTacNhanVaiTro(MaVaiTro.CanBoTiepNhan);

        var thamDinh = TaoBuoc("B2", "Thẩm định sơ bộ", LoaiBuoc.ThamDinh)
            .ThemTacNhanVaiTro(MaVaiTro.ThuKyHoiDong);

        var phanCong = TaoBuoc("B3", "Phân công chấm", LoaiBuoc.PhanCongCham)
            .ThemTacNhanVaiTro(MaVaiTro.ThuKyHoiDong);

        var chamDiem = TaoBuoc("B4", "Chấm điểm hội đồng", LoaiBuoc.ChamDiem,
                hoiDongId: hoiDongId, boTieuChiId: boTieuChiId)
            .ThemTacNhanVaiTro(MaVaiTro.ThanhVienHoiDong, QuyTacXuLy.TatCa);

        var hopHoiDong = TaoBuoc("B5", "Họp hội đồng & kết luận", LoaiBuoc.HopHoiDong)
            .ThemTacNhanVaiTro(MaVaiTro.ChuTichHoiDong);

        var banHanh = TaoBuoc("B6", "Ban hành quyết định", LoaiBuoc.BanHanhQuyetDinh, ketThuc: true)
            .ThemTacNhanVaiTro(MaVaiTro.LanhDaoPheDuyet);

        tiepNhan.ThemTruongHop(MaTruongHop.Dat, "Tiếp nhận", thamDinh, null, true,
            HanhDongTuDong.GuiEmail, HanhDongTuDong.KiemTraTrungLap);
        tiepNhan.ThemTruongHop(MaTruongHop.BoSungHoSo, "Yêu cầu bổ sung", tiepNhan,
            new BieuThucDieuKien { Truong = "hanh_dong_nguoi_dung", ToanTu = "=", GiaTri = "BO_SUNG" },
            hanhDong: HanhDongTuDong.GuiEmail);
        tiepNhan.ThemTruongHop(MaTruongHop.TraLai, "Từ chối tiếp nhận", null,
            new BieuThucDieuKien { Truong = "hanh_dong_nguoi_dung", ToanTu = "=", GiaTri = "TU_CHOI" });

        thamDinh.ThemTruongHop(MaTruongHop.Dat, "Đạt thẩm định", phanCong, laMacDinh: true);
        thamDinh.ThemTruongHop(MaTruongHop.KhongDat, "Không đạt", null,
            new BieuThucDieuKien { Truong = "ty_le_trung_lap", ToanTu = ">", GiaTri = 40 });

        phanCong.ThemTruongHop(MaTruongHop.Dat, "Đã phân công", chamDiem, laMacDinh: true);

        chamDiem.ThemTruongHop(MaTruongHop.Dat, "Hoàn thành chấm", hopHoiDong, laMacDinh: true);

        hopHoiDong.ThemTruongHop(MaTruongHop.Dat, "Đạt", banHanh,
            new BieuThucDieuKien { Truong = "tong_diem", ToanTu = ">=", GiaTri = 50 });
        hopHoiDong.ThemTruongHop(MaTruongHop.KhongDat, "Không đạt", null,
            new BieuThucDieuKien { Truong = "tong_diem", ToanTu = "<", GiaTri = 50 });

        banHanh.ThemTruongHop(MaTruongHop.Dat, "Hoàn tất", null, laMacDinh: true,
            hanhDong: new[] { HanhDongTuDong.TaoQuyetDinh, HanhDongTuDong.CapNhatKetQua });

        var quyTrinh = new QuyTrinh
        {
            Id = Guid.NewGuid(),
            Ma = "QT-CO-SO-2026",
            Ten = "Quy trình xét sáng kiến cấp cơ sở 2026",
            Cap = CapXetDuyet.CoSo,
            TrangThaiQuyTrinh = TrangThaiQuyTrinh.DangApDung,
            LaMacDinh = true
        };

        foreach (var b in new[] { tiepNhan, thamDinh, phanCong, chamDiem, hopHoiDong, banHanh })
        {
            b.QuyTrinhId = quyTrinh.Id;
            quyTrinh.DanhSachBuoc.Add(b);
        }

        quyTrinh.TrangThaiToanCuc.Add(new QuyTrinhTrangThai
        {
            Id = Guid.NewGuid(),
            QuyTrinhId = quyTrinh.Id,
            Ma = "CHO_XU_LY",
            Ten = "Chờ xử lý",
            MauSac = "#faad14",
            ThuTu = 1
        });

        return quyTrinh;
    }

    /// <summary>Bo tieu chi mau 100 diem theo dac ta Muc 10.</summary>
    public static BoTieuChi TaoBoTieuChiMau(
        string cachTinh = CachTinhDiem.TongDiem, bool loaiBoDiemCaoThap = false)
    {
        var bo = new BoTieuChi
        {
            Id = Guid.NewGuid(),
            Ma = "BTC-2026",
            Ten = "Bộ tiêu chí sáng kiến 2026",
            Nam = 2026,
            ThangDiemToiDa = 100m,
            DiemDatToiThieu = 50m,
            CachTinh = cachTinh,
            LamTron = 2,
            LoaiBoDiemCaoThap = loaiBoDiemCaoThap
        };

        ThemNhom(bo, "TINH_MOI", "Tính mới", 30m, 30m, ("Giải pháp chưa từng được áp dụng", 20m), ("Mức độ sáng tạo", 10m));
        ThemNhom(bo, "HIEU_QUA", "Tính hiệu quả", 30m, 30m, ("Hiệu quả kinh tế", 15m), ("Hiệu quả xã hội", 15m));
        ThemNhom(bo, "AP_DUNG", "Khả năng áp dụng", 25m, 25m, ("Khả năng nhân rộng", 15m), ("Tính khả thi", 10m));
        ThemNhom(bo, "PHAM_VI", "Phạm vi ảnh hưởng", 15m, 15m, ("Phạm vi áp dụng", 15m));

        bo.DanhSachMucCongNhan.Add(TaoMuc("KHONG_CN", "Không công nhận", 0m, 49.99m, false));
        bo.DanhSachMucCongNhan.Add(TaoMuc("CAP_CO_SO", "Sáng kiến cấp cơ sở", 50m, 79.99m, true));
        bo.DanhSachMucCongNhan.Add(TaoMuc("CAP_TP", "Sáng kiến cấp thành phố", 80m, 100m, true));

        foreach (var m in bo.DanhSachMucCongNhan)
        {
            m.BoTieuChiId = bo.Id;
        }

        return bo;
    }

    private static MucCongNhan TaoMuc(string ma, string ten, decimal tu, decimal den, bool laDat) => new()
    {
        Id = Guid.NewGuid(),
        Ma = ma,
        Ten = ten,
        DiemTu = tu,
        DiemDen = den,
        LaDat = laDat
    };

    private static void ThemNhom(
        BoTieuChi bo, string ma, string ten, decimal trongSo, decimal diemToiDa,
        params (string Ten, decimal Diem)[] tieuChi)
    {
        var nhom = new NhomTieuChi
        {
            Id = Guid.NewGuid(),
            BoTieuChiId = bo.Id,
            Ma = ma,
            Ten = ten,
            TrongSo = trongSo,
            DiemToiDa = diemToiDa,
            ThuTu = bo.DanhSachNhom.Count + 1
        };

        foreach (var (tenTc, diem) in tieuChi)
        {
            nhom.DanhSachTieuChi.Add(new TieuChiChamDiem
            {
                Id = Guid.NewGuid(),
                NhomTieuChiId = nhom.Id,
                Ma = $"{ma}_{nhom.DanhSachTieuChi.Count + 1}",
                Ten = tenTc,
                DiemToiDa = diem,
                KieuNhap = KieuNhapTieuChi.NhapSo,
                ThuTu = nhom.DanhSachTieuChi.Count + 1
            });
        }

        bo.DanhSachNhom.Add(nhom);
    }

    /// <summary>Tao phieu danh gia da gui voi diem chi dinh cho tung tieu chi (theo thu tu).</summary>
    public static PhieuDanhGia TaoPhieu(BoTieuChi bo, params decimal[] diemTheoTieuChi)
    {
        var phieu = new PhieuDanhGia
        {
            Id = Guid.NewGuid(),
            BoTieuChiId = bo.Id,
            HoiDongId = Guid.NewGuid(),
            ThanhVienId = Guid.NewGuid(),
            SangKienId = Guid.NewGuid(),
            TrangThaiPhieu = TrangThaiPhieuDanhGia.DaGui
        };

        var tieuChis = bo.DanhSachNhom.SelectMany(n => n.DanhSachTieuChi).ToList();
        for (var i = 0; i < tieuChis.Count; i++)
        {
            var diem = i < diemTheoTieuChi.Length ? diemTheoTieuChi[i] : 0m;
            phieu.ChiTiet.Add(new PhieuDanhGiaChiTiet
            {
                Id = Guid.NewGuid(),
                PhieuDanhGiaId = phieu.Id,
                TieuChiId = tieuChis[i].Id,
                TenTieuChiSnapshot = tieuChis[i].Ten,
                DiemToiDaSnapshot = tieuChis[i].DiemToiDa,
                Diem = diem
            });
        }

        return phieu;
    }

    /// <summary>Tao phieu voi cung mot ty le phan tram diem cho moi tieu chi.</summary>
    public static PhieuDanhGia TaoPhieuTheoTyLe(BoTieuChi bo, decimal tyLe)
    {
        var diem = bo.DanhSachNhom
            .SelectMany(n => n.DanhSachTieuChi)
            .Select(t => Math.Round(t.DiemToiDa * tyLe, 2))
            .ToArray();

        return TaoPhieu(bo, diem);
    }

    public static QuyTrinh ThemChucNangBoSung(
        this QuyTrinh quyTrinh, string maChucNang, Guid? buocId = null, bool batBuoc = false)
    {
        quyTrinh.ChucNangBoSung.Add(new QuyTrinhChucNangBoSung
        {
            Id = Guid.NewGuid(),
            QuyTrinhId = quyTrinh.Id,
            BuocId = buocId,
            MaChucNang = maChucNang,
            BatBuoc = batBuoc
        });

        return quyTrinh;
    }

    public static HoSoSangKien TaoHoSo(Guid? dotId = null, Guid? linhVucId = null) => new()
    {
        Id = Guid.NewGuid(),
        MaHoSo = "SK-2026-0001",
        TenSangKien = "Ứng dụng chuyển đổi số trong quản lý hồ sơ một cửa",
        DotDeNghiId = dotId ?? Guid.NewGuid(),
        LinhVucId = linhVucId ?? Guid.NewGuid(),
        DonViId = Guid.NewGuid(),
        TrangThaiTong = TrangThaiTongHoSo.Nhap
    };
}
