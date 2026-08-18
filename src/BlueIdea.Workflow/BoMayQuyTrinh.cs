using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuyTrinh;
using BlueIdea.Domain.SangKien;
using BlueIdea.Shared.KetQua;
using BlueIdea.Workflow.DieuKien;
using BlueIdea.Workflow.MoHinh;
using BlueIdea.Workflow.ThoiHan;

namespace BlueIdea.Workflow;

/// <summary>
/// Loi (kernel) cua engine quy trinh - THUAN LOGIC, khong phu thuoc CSDL.
/// Tang Infrastructure nap du lieu vao <see cref="NguCanhThucThi"/> roi goi cac ham o day,
/// nho vay toan bo nghiep vu chuyen buoc deu unit-test duoc.
/// </summary>
public interface IBoMayQuyTrinh
{
    /// <summary>Khoi tao ho so vao buoc bat dau cua quy trinh.</summary>
    KetQuaXuLy KhoiTao(HoSoSangKien hoSo, QuyTrinh quyTrinh, DateTimeOffset thoiDiem, out SangKienXuLy? banGhi);

    /// <summary>Liet ke cac hanh dong ma nguoi dung duoc phep thuc hien tren ho so.</summary>
    IReadOnlyList<HanhDongKhaDung> LayHanhDongKhaDung(NguCanhThucThi nguCanh);

    /// <summary>Kiem tra nguoi dung co phai tac nhan cua buoc hay khong.</summary>
    bool KiemTraQuyenXuLy(NguCanhThucThi nguCanh, Guid buocId);

    /// <summary>Thuc thi mot buoc: ghi nhan y kien, kiem tra du tac nhan, chuyen buoc.</summary>
    KetQuaXuLy ThucThi(NguCanhThucThi nguCanh, XuLyBuocRequest yeuCau, out SangKienXuLy? banGhi);

    /// <summary>Tao ngu canh bien de danh gia dieu kien chuyen tiep.</summary>
    NguCanhDieuKien TaoNguCanhDieuKien(NguCanhThucThi nguCanh);
}

/// <inheritdoc />
public sealed class BoMayQuyTrinh : IBoMayQuyTrinh
{
    private readonly IBoDanhGiaDieuKien _boDanhGia;
    private readonly ITinhHanXuLy _tinhHan;

    public BoMayQuyTrinh(IBoDanhGiaDieuKien? boDanhGia = null, ITinhHanXuLy? tinhHan = null)
    {
        _boDanhGia = boDanhGia ?? new BoDanhGiaDieuKien();
        _tinhHan = tinhHan ?? new TinhHanXuLy();
    }

    // ------------------------------------------------------------------------------------
    // Khoi tao
    // ------------------------------------------------------------------------------------

    public KetQuaXuLy KhoiTao(
        HoSoSangKien hoSo, QuyTrinh quyTrinh, DateTimeOffset thoiDiem, out SangKienXuLy? banGhi)
    {
        ArgumentNullException.ThrowIfNull(hoSo);
        ArgumentNullException.ThrowIfNull(quyTrinh);
        banGhi = null;

        if (quyTrinh.TrangThaiQuyTrinh != TrangThaiQuyTrinh.DangApDung)
        {
            return KetQuaXuLy.Loi(MaLoiHeThong.QuyTrinhChuaKichHoat,
                $"Quy trình '{quyTrinh.Ten}' chưa được kích hoạt.");
        }

        var buocBatDau = quyTrinh.DanhSachBuoc.FirstOrDefault(b => !b.DaXoa && b.LaBuocBatDau);
        if (buocBatDau is null)
        {
            return KetQuaXuLy.Loi(MaLoiHeThong.QuyTrinhKhongHopLe,
                "Quy trình không có bước bắt đầu.");
        }

        var han = _tinhHan.TinhHan(thoiDiem, buocBatDau.SoNgayXuLy, buocBatDau.TinhTheoNgayLamViec);
        var trangThaiDau = LayTrangThaiMacDinh(quyTrinh, buocBatDau);

        hoSo.QuyTrinhId = quyTrinh.Id;
        hoSo.BuocHienTaiId = buocBatDau.Id;
        hoSo.TrangThaiHienTaiId = trangThaiDau?.Id;
        hoSo.HanXuLyHienTai = han;
        hoSo.TrangThaiTong = TrangThaiTongHoSo.DaNop;
        hoSo.NgayNop ??= thoiDiem;

        banGhi = new SangKienXuLy
        {
            SangKienId = hoSo.Id,
            BuocId = buocBatDau.Id,
            TenBuocSnapshot = buocBatDau.Ten,
            TrangThaiId = trangThaiDau?.Id,
            ThoiGianNhan = thoiDiem,
            HanXuLy = han,
            ThuTu = 1
        };

        return new KetQuaXuLy
        {
            ThanhCong = true,
            ThongBao = $"Hồ sơ đã vào bước '{buocBatDau.Ten}'.",
            BuocMoiId = buocBatDau.Id,
            TenBuocMoi = buocBatDau.Ten,
            TrangThaiMoiId = trangThaiDau?.Id,
            TrangThaiTongMoi = hoSo.TrangThaiTong,
            HanXuLyMoi = han
        };
    }

    // ------------------------------------------------------------------------------------
    // Hanh dong kha dung
    // ------------------------------------------------------------------------------------

    public IReadOnlyList<HanhDongKhaDung> LayHanhDongKhaDung(NguCanhThucThi nguCanh)
    {
        ArgumentNullException.ThrowIfNull(nguCanh);

        var buoc = LayBuocHienTai(nguCanh);
        if (buoc is null || nguCanh.HoSo.DangKhoa)
        {
            return Array.Empty<HanhDongKhaDung>();
        }

        if (!LaTacNhanCuaBuoc(buoc, nguCanh))
        {
            return Array.Empty<HanhDongKhaDung>();
        }

        var nguCanhDieuKien = TaoNguCanhDieuKien(nguCanh);
        var bangBuoc = nguCanh.QuyTrinh.DanhSachBuoc.Where(b => !b.DaXoa).ToDictionary(b => b.Id);
        var ketQua = new List<HanhDongKhaDung>();

        foreach (var th in buoc.TruongHop.Where(t => !t.DaXoa).OrderBy(t => t.ThuTu))
        {
            var danhGia = _boDanhGia.DanhGiaChiTiet(th.DieuKien, nguCanhDieuKien);
            QuyTrinhBuoc? buocTiep = null;
            if (th.BuocTiepTheoId.HasValue)
            {
                bangBuoc.TryGetValue(th.BuocTiepTheoId.Value, out buocTiep);
            }

            ketQua.Add(new HanhDongKhaDung
            {
                TruongHopId = th.Id,
                Ma = th.Ma,
                Ten = th.Ten,
                BuocId = buoc.Id,
                TenBuoc = buoc.Ten,
                MauNut = th.MauNut ?? SuyRaMauNut(th.Ma),
                BatBuocNhapYKien = buoc.BatBuocNhapYKien,
                BatBuocDinhKem = buoc.BatBuocDinhKem,
                TepBatBuoc = buoc.DanhSachTepBatBuoc,
                BiChan = !danhGia.Khop,
                LyDoChan = danhGia.Khop ? null : danhGia.GiaiThich,
                BuocTiepTheoId = th.BuocTiepTheoId,
                TenBuocTiepTheo = buocTiep?.Ten,
                HanhDongTuDong = th.HanhDong
            });
        }

        return ketQua;
    }

    public bool KiemTraQuyenXuLy(NguCanhThucThi nguCanh, Guid buocId)
    {
        ArgumentNullException.ThrowIfNull(nguCanh);

        var buoc = nguCanh.QuyTrinh.DanhSachBuoc.FirstOrDefault(b => !b.DaXoa && b.Id == buocId);
        return buoc is not null && LaTacNhanCuaBuoc(buoc, nguCanh);
    }

    // ------------------------------------------------------------------------------------
    // Thuc thi
    // ------------------------------------------------------------------------------------

    public KetQuaXuLy ThucThi(NguCanhThucThi nguCanh, XuLyBuocRequest yeuCau, out SangKienXuLy? banGhi)
    {
        ArgumentNullException.ThrowIfNull(nguCanh);
        ArgumentNullException.ThrowIfNull(yeuCau);
        banGhi = null;

        var hoSo = nguCanh.HoSo;

        if (hoSo.DangKhoa)
        {
            return KetQuaXuLy.Loi(MaLoiHeThong.HoSoDangKhoa,
                $"Hồ sơ đang bị khóa. Lý do: {hoSo.LyDoKhoa ?? "không rõ"}.");
        }

        if (yeuCau.PhienBanHoSo.HasValue && yeuCau.PhienBanHoSo.Value != hoSo.PhienBan)
        {
            return KetQuaXuLy.Loi(MaLoiHeThong.XungDotDuLieu,
                "Hồ sơ đã được người khác cập nhật. Vui lòng tải lại trang.");
        }

        var buoc = LayBuocHienTai(nguCanh);
        if (buoc is null)
        {
            return KetQuaXuLy.Loi(MaLoiHeThong.BuocKhongHopLe,
                "Hồ sơ không ở bước xử lý nào hoặc bước hiện tại không tồn tại trong quy trình.");
        }

        if (!LaTacNhanCuaBuoc(buoc, nguCanh))
        {
            return KetQuaXuLy.Loi(MaLoiHeThong.KhongCoQuyenXuLyBuoc,
                $"Bạn không phải tác nhân được cấu hình xử lý bước '{buoc.Ten}'.");
        }

        var truongHop = buoc.TruongHop.FirstOrDefault(t => !t.DaXoa && t.Id == yeuCau.TruongHopId);
        if (truongHop is null)
        {
            return KetQuaXuLy.Loi(MaLoiHeThong.KhongCoTruongHopPhuHop,
                "Trường hợp chuyển tiếp không hợp lệ ở bước hiện tại.");
        }

        // Rang buoc nhap lieu do cau hinh buoc quy dinh.
        if (buoc.BatBuocNhapYKien && string.IsNullOrWhiteSpace(yeuCau.YKien))
        {
            return KetQuaXuLy.Loi(MaLoiHeThong.DuLieuKhongHopLe,
                $"Bước '{buoc.Ten}' bắt buộc nhập ý kiến xử lý.");
        }

        if (buoc.BatBuocDinhKem && yeuCau.TepDinhKemIds.Count == 0)
        {
            return KetQuaXuLy.Loi(MaLoiHeThong.DuLieuKhongHopLe,
                $"Bước '{buoc.Ten}' bắt buộc đính kèm tệp.");
        }

        if (yeuCau.NguoiUyQuyenId.HasValue && !buoc.ChoPhepUyQuyen)
        {
            return KetQuaXuLy.Loi(MaLoiHeThong.DuLieuKhongHopLe,
                $"Bước '{buoc.Ten}' không cho phép ủy quyền xử lý.");
        }

        // Dieu kien chuyen tiep phai thoa man tai thoi diem thuc thi.
        var nguCanhDieuKien = TaoNguCanhDieuKien(nguCanh);
        var danhGia = _boDanhGia.DanhGiaChiTiet(truongHop.DieuKien, nguCanhDieuKien);
        if (!danhGia.Khop)
        {
            return KetQuaXuLy.Loi(MaLoiHeThong.KhongCoTruongHopPhuHop,
                $"Không thể chọn '{truongHop.Ten}' vì điều kiện chưa thỏa mãn: {danhGia.GiaiThich}");
        }

        var thoiDiem = nguCanh.ThoiDiem;

        // Ghi nhan luot xu ly cua nguoi dung hien tai.
        var banGhiHienTai = nguCanh.LichSuXuLy
            .FirstOrDefault(x => x.BuocId == buoc.Id
                                 && x.NguoiXuLyId == yeuCau.NguoiDungId
                                 && !x.DaHoanThanh);

        var laBanGhiMoi = banGhiHienTai is null;
        banGhiHienTai ??= new SangKienXuLy
        {
            SangKienId = hoSo.Id,
            BuocId = buoc.Id,
            TenBuocSnapshot = buoc.Ten,
            ThoiGianNhan = hoSo.HanXuLyHienTai.HasValue ? thoiDiem : thoiDiem,
            HanXuLy = hoSo.HanXuLyHienTai,
            ThuTu = nguCanh.LichSuXuLy.Count + 1
        };

        banGhiHienTai.NguoiXuLyId = yeuCau.NguoiDungId;
        banGhiHienTai.NguoiUyQuyenId = yeuCau.NguoiUyQuyenId;
        banGhiHienTai.TruongHopId = truongHop.Id;
        banGhiHienTai.TenTruongHopSnapshot = truongHop.Ten;
        banGhiHienTai.TrangThaiId = truongHop.TrangThaiGanId ?? banGhiHienTai.TrangThaiId;
        banGhiHienTai.YKien = yeuCau.YKien;
        banGhiHienTai.TepDinhKemIds = yeuCau.TepDinhKemIds.ToList();
        banGhiHienTai.ThoiGianXuLy = thoiDiem;
        banGhiHienTai.SoNgayXuLy = _tinhHan.TinhSoNgayXuLy(
            banGhiHienTai.ThoiGianNhan, thoiDiem, buoc.TinhTheoNgayLamViec);
        banGhiHienTai.QuaHan = banGhiHienTai.HanXuLy.HasValue && thoiDiem > banGhiHienTai.HanXuLy.Value;

        banGhi = laBanGhiMoi ? banGhiHienTai : null;

        // Quy tac TAT_CA / DA_SO: chi chuyen buoc khi du so tac nhan hoan thanh.
        var (daXuLy, canThiet) = DemTacNhan(buoc, nguCanh, truongHop.Id, banGhiHienTai);
        if (daXuLy < canThiet)
        {
            hoSo.TrangThaiTong = TrangThaiTongHoSo.DangXuLy;
            return new KetQuaXuLy
            {
                ThanhCong = true,
                ThongBao = $"Đã ghi nhận ý kiến. Chờ thêm {canThiet - daXuLy} người xử lý bước '{buoc.Ten}'.",
                BuocTruocId = buoc.Id,
                BuocMoiId = buoc.Id,
                TenBuocMoi = buoc.Ten,
                TrangThaiMoiId = banGhiHienTai.TrangThaiId,
                TrangThaiTongMoi = hoSo.TrangThaiTong,
                HanXuLyMoi = hoSo.HanXuLyHienTai,
                ChoThemTacNhan = true,
                SoTacNhanDaXuLy = daXuLy,
                SoTacNhanCanThiet = canThiet
            };
        }

        return ChuyenBuoc(nguCanh, buoc, truongHop, thoiDiem);
    }

    private KetQuaXuLy ChuyenBuoc(
        NguCanhThucThi nguCanh, QuyTrinhBuoc buocCu, QuyTrinhTruongHop truongHop, DateTimeOffset thoiDiem)
    {
        var hoSo = nguCanh.HoSo;
        var buocTiep = truongHop.BuocTiepTheoId.HasValue
            ? nguCanh.QuyTrinh.DanhSachBuoc.FirstOrDefault(b => !b.DaXoa && b.Id == truongHop.BuocTiepTheoId.Value)
            : null;

        var ketThuc = buocTiep is null || buocTiep.LaBuocKetThuc;

        hoSo.BuocHienTaiId = buocTiep?.Id;
        hoSo.TrangThaiHienTaiId = truongHop.TrangThaiGanId
                                  ?? LayTrangThaiMacDinh(nguCanh.QuyTrinh, buocTiep)?.Id;
        hoSo.PhienBan++;

        if (buocTiep is not null && !buocTiep.LaBuocKetThuc)
        {
            hoSo.HanXuLyHienTai = _tinhHan.TinhHan(
                thoiDiem, buocTiep.SoNgayXuLy, buocTiep.TinhTheoNgayLamViec);
        }
        else
        {
            hoSo.HanXuLyHienTai = null;
        }

        hoSo.TrangThaiTong = SuyRaTrangThaiTong(truongHop, ketThuc);

        if (ketThuc)
        {
            hoSo.NgayHoanThanh = thoiDiem;
            if (hoSo.TrangThaiTong == TrangThaiTongHoSo.DaPheDuyet)
            {
                hoSo.KetQua = KetQuaXetDuyetGiaTri.Dat;
            }
            else if (hoSo.TrangThaiTong == TrangThaiTongHoSo.KhongDat)
            {
                hoSo.KetQua = KetQuaXetDuyetGiaTri.KhongDat;
            }
        }

        return new KetQuaXuLy
        {
            ThanhCong = true,
            ThongBao = ketThuc
                ? $"Hồ sơ đã hoàn thành quy trình với kết quả '{truongHop.Ten}'."
                : $"Hồ sơ chuyển sang bước '{buocTiep!.Ten}'.",
            BuocTruocId = buocCu.Id,
            BuocMoiId = buocTiep?.Id,
            TenBuocMoi = buocTiep?.Ten,
            TrangThaiMoiId = hoSo.TrangThaiHienTaiId,
            TrangThaiTongMoi = hoSo.TrangThaiTong,
            HanXuLyMoi = hoSo.HanXuLyHienTai,
            DaKetThucQuyTrinh = ketThuc,
            SoTacNhanDaXuLy = 0,
            SoTacNhanCanThiet = 0,
            HanhDongCanChay = truongHop.HanhDong
        };
    }

    // ------------------------------------------------------------------------------------
    // Ho tro
    // ------------------------------------------------------------------------------------

    public NguCanhDieuKien TaoNguCanhDieuKien(NguCanhThucThi nguCanh)
    {
        var hoSo = nguCanh.HoSo;
        var nc = new NguCanhDieuKien()
            .Dat(BienNguCanh.TongDiem, hoSo.TongDiem ?? 0m)
            .Dat(BienNguCanh.DiemTrungBinh, hoSo.DiemTrungBinh ?? 0m)
            .Dat(BienNguCanh.TyLeTrungLap, hoSo.TyLeTrungLap ?? 0m)
            .Dat(BienNguCanh.LinhVucId, hoSo.LinhVucId)
            .Dat(BienNguCanh.DonViId, hoSo.DonViId)
            .Dat(BienNguCanh.DotDeNghiId, hoSo.DotDeNghiId)
            .Dat(BienNguCanh.CapXetDuyet, nguCanh.QuyTrinh.Cap)
            .Dat(BienNguCanh.KetQua, hoSo.KetQua)
            .Dat(BienNguCanh.MucCongNhanId, hoSo.MucCongNhanId)
            .Dat(BienNguCanh.TrangThaiTong, hoSo.TrangThaiTong)
            .Dat(BienNguCanh.SoTacGia, hoSo.DanhSachTacGia.Count)
            .Dat(BienNguCanh.GiaTriLamLoi, hoSo.GiaTriLamLoiUocTinh ?? 0m);

        foreach (var cap in nguCanh.BienBoSung)
        {
            nc.Dat(cap.Key, cap.Value);
        }

        return nc;
    }

    private static QuyTrinhBuoc? LayBuocHienTai(NguCanhThucThi nguCanh)
        => nguCanh.HoSo.BuocHienTaiId is null
            ? null
            : nguCanh.QuyTrinh.DanhSachBuoc
                .FirstOrDefault(b => !b.DaXoa && b.Id == nguCanh.HoSo.BuocHienTaiId.Value);

    private static QuyTrinhTrangThai? LayTrangThaiMacDinh(QuyTrinh quyTrinh, QuyTrinhBuoc? buoc)
    {
        if (buoc is null)
        {
            return null;
        }

        var cuaBuoc = buoc.TrangThai.Where(t => !t.DaXoa).OrderBy(t => t.ThuTu).FirstOrDefault();
        if (cuaBuoc is not null)
        {
            return cuaBuoc;
        }

        return quyTrinh.TrangThaiToanCuc
            .Where(t => !t.DaXoa && t.BuocId is null)
            .OrderBy(t => t.ThuTu)
            .FirstOrDefault();
    }

    /// <summary>Doi chieu nguoi dung voi danh sach tac nhan duoc cau hinh cho buoc.</summary>
    private static bool LaTacNhanCuaBuoc(QuyTrinhBuoc buoc, NguCanhThucThi nguCanh)
    {
        var tacNhan = buoc.TacNhan.Where(t => !t.DaXoa).ToList();
        if (tacNhan.Count == 0)
        {
            return false;
        }

        var nguoi = nguCanh.NguoiXuLy;

        foreach (var tn in tacNhan)
        {
            var khop = tn.LoaiTacNhan switch
            {
                LoaiTacNhan.NguoiDung => tn.ThamChieuId == nguoi.NguoiDungId,

                LoaiTacNhan.VaiTro =>
                    (tn.ThamChieuId.HasValue && nguoi.VaiTroIds.Contains(tn.ThamChieuId.Value))
                    || (!string.IsNullOrEmpty(tn.ThamChieuMa) && nguoi.MaVaiTro.Contains(tn.ThamChieuMa)),

                LoaiTacNhan.DonVi =>
                    tn.ThamChieuId.HasValue
                    && (nguoi.DonViId == tn.ThamChieuId.Value
                        || nguoi.DonViTrongPhamVi.Contains(tn.ThamChieuId.Value)),

                LoaiTacNhan.HoiDong =>
                    tn.ThamChieuId.HasValue && nguoi.ChucDanhTheoHoiDong.ContainsKey(tn.ThamChieuId.Value),

                LoaiTacNhan.ChucDanhHoiDong => KhopChucDanhHoiDong(buoc, tn, nguoi),

                LoaiTacNhan.NguoiTaoHoSo => nguoi.LaNguoiTaoHoSo,

                LoaiTacNhan.LanhDaoDonViTacGia => nguoi.LaLanhDaoDonViTacGia,

                _ => false
            };

            if (khop)
            {
                return true;
            }
        }

        return false;
    }

    private static bool KhopChucDanhHoiDong(
        QuyTrinhBuoc buoc, QuyTrinhBuocTacNhan tacNhan, NguCanhNguoiXuLy nguoi)
    {
        if (string.IsNullOrEmpty(tacNhan.ThamChieuMa))
        {
            return false;
        }

        // Uu tien hoi dong gan truc tiep tren buoc, neu khong thi xet moi hoi dong nguoi dung tham gia.
        if (buoc.HoiDongId.HasValue)
        {
            return nguoi.ChucDanhTheoHoiDong.TryGetValue(buoc.HoiDongId.Value, out var chucDanh)
                   && string.Equals(chucDanh, tacNhan.ThamChieuMa, StringComparison.OrdinalIgnoreCase);
        }

        return nguoi.ChucDanhTheoHoiDong.Values
            .Any(cd => string.Equals(cd, tacNhan.ThamChieuMa, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Dem so tac nhan da xu ly buoc va so tac nhan toi thieu can co theo quy tac cau hinh.
    /// </summary>
    private static (int DaXuLy, int CanThiet) DemTacNhan(
        QuyTrinhBuoc buoc, NguCanhThucThi nguCanh, Guid truongHopId, SangKienXuLy banGhiHienTai)
    {
        var quyTac = buoc.TacNhan
            .Where(t => !t.DaXoa)
            .Select(t => t.QuyTacXuLy)
            .DefaultIfEmpty(QuyTacXuLy.MotNguoi)
            .ToList();

        // Quy tac "chat" nhat quyet dinh (TAT_CA > DA_SO > MOT_NGUOI).
        var quyTacApDung = quyTac.Contains(QuyTacXuLy.TatCa) ? QuyTacXuLy.TatCa
            : quyTac.Contains(QuyTacXuLy.DaSo) ? QuyTacXuLy.DaSo
            : quyTac.Contains(QuyTacXuLy.ChuTichQuyetDinh) ? QuyTacXuLy.ChuTichQuyetDinh
            : QuyTacXuLy.MotNguoi;

        if (quyTacApDung is QuyTacXuLy.MotNguoi or QuyTacXuLy.ChuTichQuyetDinh)
        {
            return (1, 1);
        }

        var tong = Math.Max(nguCanh.SoTacNhanDuKien, 1);

        // Dem cac ban ghi da hoan thanh tai buoc nay (khong tinh trung ban ghi hien tai).
        var daXuLy = nguCanh.LichSuXuLy
            .Where(x => x.BuocId == buoc.Id && x.DaHoanThanh && x.Id != banGhiHienTai.Id)
            .Select(x => x.NguoiXuLyId)
            .Where(id => id.HasValue && id != banGhiHienTai.NguoiXuLyId)
            .Distinct()
            .Count() + 1; // +1 cho luot vua ghi nhan

        int canThiet;
        if (quyTacApDung == QuyTacXuLy.TatCa)
        {
            canThiet = tong;
        }
        else
        {
            var tyLe = buoc.TacNhan
                .Where(t => !t.DaXoa && t.QuyTacXuLy == QuyTacXuLy.DaSo && t.TyLeDongThuan.HasValue)
                .Select(t => t.TyLeDongThuan!.Value)
                .DefaultIfEmpty(50m)
                .Max();

            canThiet = tyLe <= 0
                ? 1
                : (int)Math.Floor(tong * tyLe / 100m) + (tyLe < 100m ? 1 : 0);
            canThiet = Math.Clamp(canThiet, 1, tong);
        }

        _ = truongHopId;
        return (Math.Min(daXuLy, canThiet), canThiet);
    }

    private static string SuyRaTrangThaiTong(QuyTrinhTruongHop truongHop, bool ketThuc)
    {
        var ma = (truongHop.Ma ?? string.Empty).ToUpperInvariant();

        return ma switch
        {
            MaTruongHop.BoSungHoSo => TrangThaiTongHoSo.YeuCauBoSung,
            MaTruongHop.RutHoSo => TrangThaiTongHoSo.DaRut,

            // TRA_LAI dung cho hai viec khac nhau, phan biet bang ketThuc:
            //   - tra ho so ve buoc truoc de bo sung  -> YEU_CAU_BO_SUNG (tac gia sua roi nop lai)
            //   - tu choi tiep nhan, dong quy trinh   -> KHONG_DAT
            // Truoc day ca hai deu ra YEU_CAU_BO_SUNG, nen ho so bi TU CHOI lai mang trang thai cho
            // bo sung: vua "da hoan thanh quy trinh" vua "dang cho tac gia sua", va tac gia sua roi
            // nop lai duoc mot ho so khong con buoc nao de di tiep.
            MaTruongHop.TraLai => ketThuc
                ? TrangThaiTongHoSo.KhongDat
                : TrangThaiTongHoSo.YeuCauBoSung,
            MaTruongHop.KhongDat when ketThuc => TrangThaiTongHoSo.KhongDat,
            MaTruongHop.Dat when ketThuc => TrangThaiTongHoSo.DaPheDuyet,
            _ => ketThuc ? TrangThaiTongHoSo.DaPheDuyet : TrangThaiTongHoSo.DangXuLy
        };
    }

    private static string SuyRaMauNut(string ma) => ma.ToUpperInvariant() switch
    {
        MaTruongHop.Dat or MaTruongHop.ChuyenCapCaoHon => "primary",
        MaTruongHop.KhongDat or MaTruongHop.TraLai or MaTruongHop.RutHoSo => "danger",
        MaTruongHop.BoSungHoSo => "dashed",
        _ => "default"
    };
}
