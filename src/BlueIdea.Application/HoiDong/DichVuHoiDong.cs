using BlueIdea.Application.Chung;
using BlueIdea.Application.DanhMuc;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.HoiDong;
using BlueIdea.Shared.KetQua;
using BlueIdea.Shared.TiengViet;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.HoiDong;

/// <summary>Chuc nang 19-20: hoi dong sang kien, thanh vien, phien hop, bo phieu, bien ban.</summary>
public sealed class DichVuHoiDong : DichVuDanhMucCoSo<HoiDongSangKien>
{
    private readonly INguoiDungHienTai _nguoiDung;

    public DichVuHoiDong(
        IAppDbContext db, IDichVuPhanQuyen phanQuyen, IDongHoHeThong dongHo,
        INguoiDungHienTai nguoiDung)
        : base(db, phanQuyen, dongHo)
        => _nguoiDung = nguoiDung;

    protected override DbSet<HoiDongSangKien> BangDuLieu => Db.HoiDong;

    protected override string TenDanhMuc => "Hội đồng";

    protected override IQueryable<HoiDongSangKien> TaoTruyVanChiTiet()
        => Db.HoiDong.AsNoTracking()
            .Include(x => x.ThanhVien)
            .Include(x => x.PhienHop);

    // ------------------------------------------------------------------------------------
    // Thanh vien (chuc nang 20)
    // ------------------------------------------------------------------------------------

    /// <summary>
    /// Luu danh sach thanh vien. Rang buoc: moi hoi dong chi co DUNG MOT chu tich.
    /// </summary>
    public async Task LuuThanhVienAsync(
        Guid hoiDongId, IReadOnlyList<ThanhVienLuuDto> danhSach, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.HoiDongCauHinh, hoiDongId, ct).ConfigureAwait(false);

        var soChuTich = danhSach.Count(x => x.ChucDanh == ChucDanhHoiDong.ChuTich);
        if (soChuTich != 1)
        {
            throw new NghiepVuException(MaLoiHeThong.HoiDongDaCoChuTich,
                $"Hội đồng phải có đúng 1 Chủ tịch, hiện đang chọn {soChuTich}.");
        }

        var hoiDong = await Db.HoiDong
            .Include(x => x.ThanhVien)
            .FirstOrDefaultAsync(x => x.Id == hoiDongId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException(TenDanhMuc, hoiDongId);

        if (danhSach.Count < hoiDong.SoThanhVienToiThieu)
        {
            throw new NghiepVuException(MaLoiHeThong.KhongDuThanhVienToiThieu,
                $"Hội đồng yêu cầu tối thiểu {hoiDong.SoThanhVienToiThieu} thành viên.");
        }

        var giuLai = danhSach.Where(x => x.Id != Guid.Empty).Select(x => x.Id).ToHashSet();

        foreach (var cu in hoiDong.ThanhVien.Where(x => !giuLai.Contains(x.Id)))
        {
            cu.DaXoa = true;
        }

        var thuTu = 1;
        foreach (var tv in danhSach)
        {
            var hienCo = tv.Id != Guid.Empty
                ? hoiDong.ThanhVien.FirstOrDefault(x => x.Id == tv.Id)
                : null;

            if (hienCo is null)
            {
                Db.HoiDongThanhVien.Add(new HoiDongThanhVien
                {
                    Id = Guid.NewGuid(),
                    HoiDongId = hoiDongId,
                    NguoiDungId = tv.NguoiDungId,
                    HoTenHienThi = tv.HoTenHienThi,
                    ChucVuCongTac = tv.ChucVuCongTac,
                    DonViCongTac = tv.DonViCongTac,
                    ChucDanh = tv.ChucDanh,
                    QuyenChamDiem = tv.QuyenChamDiem,
                    QuyenNhanXet = tv.QuyenNhanXet,
                    QuyenBoPhieu = tv.QuyenBoPhieu,
                    QuyenKyBienBan = tv.QuyenKyBienBan,
                    QuyenKetLuan = tv.QuyenKetLuan,
                    ThuTu = thuTu++
                });
            }
            else
            {
                hienCo.DaXoa = false;
                hienCo.NguoiDungId = tv.NguoiDungId;
                hienCo.HoTenHienThi = tv.HoTenHienThi;
                hienCo.ChucVuCongTac = tv.ChucVuCongTac;
                hienCo.DonViCongTac = tv.DonViCongTac;
                hienCo.ChucDanh = tv.ChucDanh;
                hienCo.QuyenChamDiem = tv.QuyenChamDiem;
                hienCo.QuyenNhanXet = tv.QuyenNhanXet;
                hienCo.QuyenBoPhieu = tv.QuyenBoPhieu;
                hienCo.QuyenKyBienBan = tv.QuyenKyBienBan;
                hienCo.QuyenKetLuan = tv.QuyenKetLuan;
                hienCo.ThuTu = thuTu++;
            }
        }

        await Db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // ------------------------------------------------------------------------------------
    // Phien hop
    // ------------------------------------------------------------------------------------

    public async Task<PhienHopHoiDong> TaoPhienHopAsync(
        PhienHopLuuDto duLieu, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.HoiDongHopPhien, duLieu.HoiDongId, ct)
            .ConfigureAwait(false);

        var phien = new PhienHopHoiDong
        {
            Id = Guid.NewGuid(),
            HoiDongId = duLieu.HoiDongId,
            MaPhien = string.IsNullOrWhiteSpace(duLieu.MaPhien)
                ? $"PH-{DongHo.BayGio:yyyyMMddHHmm}"
                : duLieu.MaPhien,
            TenPhien = duLieu.TenPhien,
            ThoiGianBatDau = duLieu.ThoiGianBatDau,
            ThoiGianKetThuc = duLieu.ThoiGianKetThuc,
            DiaDiem = duLieu.DiaDiem,
            HinhThuc = duLieu.HinhThuc,
            ChuTriId = duLieu.ChuTriId,
            ThuKyId = duLieu.ThuKyId,
            NoiDung = duLieu.NoiDung,
            TrangThaiPhien = TrangThaiPhienHop.DuKien
        };

        var thuTu = 1;
        foreach (var sangKienId in duLieu.SangKienIds)
        {
            phien.DanhSachHoSo.Add(new PhienHopHoSo
            {
                Id = Guid.NewGuid(),
                PhienHopId = phien.Id,
                SangKienId = sangKienId,
                ThuTu = thuTu++
            });
        }

        // Diem danh mac dinh: tao san ban ghi cho moi thanh vien.
        var thanhVien = await Db.HoiDongThanhVien.AsNoTracking()
            .Where(x => x.HoiDongId == duLieu.HoiDongId && x.TrangThai == TrangThaiDanhMuc.HoatDong)
            .Select(x => x.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var tvId in thanhVien)
        {
            phien.DiemDanh.Add(new PhienHopDiemDanh
            {
                Id = Guid.NewGuid(),
                PhienHopId = phien.Id,
                ThanhVienId = tvId,
                CoMat = false
            });
        }

        Db.PhienHop.Add(phien);
        await Db.SaveChangesAsync(ct).ConfigureAwait(false);
        return phien;
    }

    public async Task<PhienHopHoiDong> LayPhienHopAsync(Guid phienHopId, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.HoiDongXem, phienHopId, ct).ConfigureAwait(false);

        var phien = await Db.PhienHop.AsNoTracking()
            .Include(x => x.DanhSachHoSo)
            .Include(x => x.DiemDanh)
            .Include(x => x.PhieuBoPhieu)
            .FirstOrDefaultAsync(x => x.Id == phienHopId, ct)
            .ConfigureAwait(false);

        return phien ?? throw new KhongTimThayException("phiên họp", phienHopId);
    }

    public async Task DiemDanhAsync(
        Guid phienHopId, Guid thanhVienId, bool coMat, string? lyDoVang,
        CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.HoiDongHopPhien, phienHopId, ct)
            .ConfigureAwait(false);

        var banGhi = await Db.PhienHopDiemDanh
            .FirstOrDefaultAsync(x => x.PhienHopId == phienHopId && x.ThanhVienId == thanhVienId, ct)
            .ConfigureAwait(false);

        if (banGhi is null)
        {
            banGhi = new PhienHopDiemDanh
            {
                Id = Guid.NewGuid(),
                PhienHopId = phienHopId,
                ThanhVienId = thanhVienId
            };
            Db.PhienHopDiemDanh.Add(banGhi);
        }

        banGhi.CoMat = coMat;
        banGhi.LyDoVang = lyDoVang;
        banGhi.ThoiGianDiemDanh = DongHo.BayGio;

        await Db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Bo phieu cho mot ho so trong phien hop (ho tro phieu kin).</summary>
    public async Task BoPhieuAsync(BoPhieuDto duLieu, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.HoiDongBoPhieu, duLieu.SangKienId, ct)
            .ConfigureAwait(false);

        var phien = await Db.PhienHop.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == duLieu.PhienHopId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("phiên họp", duLieu.PhienHopId);

        if (phien.TrangThaiPhien == TrangThaiPhienHop.DaKetThuc)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Phiên họp đã kết thúc, không thể bỏ phiếu.");
        }

        var thanhVien = await Db.HoiDongThanhVien.AsNoTracking()
            .FirstOrDefaultAsync(x => x.HoiDongId == phien.HoiDongId
                                      && x.NguoiDungId == _nguoiDung.Id, ct)
            .ConfigureAwait(false)
            ?? throw new NghiepVuException(MaLoiHeThong.KhongCoQuyen,
                "Bạn không phải thành viên của hội đồng này.");

        if (!thanhVien.QuyenBoPhieu)
        {
            throw new NghiepVuException(MaLoiHeThong.KhongCoQuyen,
                "Bạn không có quyền bỏ phiếu trong hội đồng này.");
        }

        var phieu = await Db.PhieuBoPhieu
            .FirstOrDefaultAsync(x => x.PhienHopId == duLieu.PhienHopId
                                      && x.SangKienId == duLieu.SangKienId
                                      && x.ThanhVienId == thanhVien.Id, ct)
            .ConfigureAwait(false);

        if (phieu is null)
        {
            phieu = new PhieuBoPhieu
            {
                Id = Guid.NewGuid(),
                PhienHopId = duLieu.PhienHopId,
                SangKienId = duLieu.SangKienId,
                ThanhVienId = thanhVien.Id
            };
            Db.PhieuBoPhieu.Add(phieu);
        }

        phieu.YKien = duLieu.YKien;
        phieu.MucDeXuatId = duLieu.MucDeXuatId;
        phieu.GhiChu = duLieu.GhiChu;
        phieu.LaPhieuKin = duLieu.LaPhieuKin;
        phieu.ThoiGian = DongHo.BayGio;

        await Db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Ghi y kien / ket luan rieng cho MOT ho so trong phien hop.
    ///
    /// Tach khoi ket luan chung cua phien: hoi dong xet nhieu ho so mot phien, moi ho so co ket
    /// luan rieng, gop het vao mot o van ban thi bien ban khong tach duoc theo tung ho so.
    /// </summary>
    public async Task GhiYKienHoSoAsync(
        Guid phienHopId, Guid sangKienId, string? ketLuanRieng, string? ketQua,
        CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.HoiDongHopPhien, phienHopId, ct)
            .ConfigureAwait(false);

        var phien = await Db.PhienHop.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == phienHopId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("phiên họp", phienHopId);

        if (phien.TrangThaiPhien == TrangThaiPhienHop.DaKetThuc)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Phiên họp đã kết thúc, không sửa được ý kiến.");
        }

        var dong = await Db.PhienHopHoSo
            .FirstOrDefaultAsync(x => x.PhienHopId == phienHopId && x.SangKienId == sangKienId, ct)
            .ConfigureAwait(false)
            ?? throw new KhongTimThayException("hồ sơ trong phiên họp", sangKienId);

        if (ketQua is not null and not ("DAT" or "KHONG_DAT" or "HOAN"))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                $"Kết quả '{ketQua}' không hợp lệ (chỉ DAT, KHONG_DAT, HOAN).");
        }

        dong.KetLuanRieng = ketLuanRieng;
        dong.KetQua = ketQua;

        await Db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Ket qua bo phieu cua mot ho so trong phien hop.</summary>
    public async Task<KetQuaBoPhieuDto> LayKetQuaBoPhieuAsync(
        Guid phienHopId, Guid sangKienId, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.HoiDongXem, sangKienId, ct).ConfigureAwait(false);

        var phieus = await Db.PhieuBoPhieu.AsNoTracking()
            .Where(x => x.PhienHopId == phienHopId && x.SangKienId == sangKienId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var dongY = phieus.Count(x => x.YKien == YKienBoPhieu.DongY);
        var khongDongY = phieus.Count(x => x.YKien == YKienBoPhieu.KhongDongY);
        var yKienKhac = phieus.Count(x => x.YKien == YKienBoPhieu.YKienKhac);
        var tong = phieus.Count;

        return new KetQuaBoPhieuDto(
            tong, dongY, khongDongY, yKienKhac,
            tong == 0 ? 0m : Math.Round(dongY * 100m / tong, 2));
    }

    /// <summary>Ket thuc phien hop va ghi ket luan.</summary>
    public async Task KetThucPhienHopAsync(
        Guid phienHopId, string? ketLuan, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.HoiDongKetLuan, phienHopId, ct)
            .ConfigureAwait(false);

        var phien = await Db.PhienHop.FirstOrDefaultAsync(x => x.Id == phienHopId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("phiên họp", phienHopId);

        phien.TrangThaiPhien = TrangThaiPhienHop.DaKetThuc;
        phien.KetLuan = ketLuan;
        phien.ThoiGianKetThuc ??= DongHo.BayGio;

        await Db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    protected override async Task<IReadOnlyList<NoiThamChieu>> LayNoiThamChieuAsync(
        Guid id, CancellationToken ct)
    {
        var ketQua = new List<NoiThamChieu>();

        var soPhanCong = await Db.SangKienPhanCong.CountAsync(x => x.HoiDongId == id, ct)
            .ConfigureAwait(false);
        if (soPhanCong > 0)
        {
            ketQua.Add(new NoiThamChieu("sang_kien_phan_cong", "Phân công chấm điểm", soPhanCong));
        }

        var soPhien = await Db.PhienHop.CountAsync(x => x.HoiDongId == id, ct).ConfigureAwait(false);
        if (soPhien > 0)
        {
            ketQua.Add(new NoiThamChieu("phien_hop_hoi_dong", "Phiên họp", soPhien));
        }

        return ketQua;
    }

    /// <summary>Tao thuc the hoi dong tu DTO (dung cho them moi va cap nhat).</summary>
    public static HoiDongSangKien ApDung(HoiDongSangKien x, HoiDongLuuDto d)
    {
        x.Ma = d.Ma;
        x.Ten = d.Ten;
        x.TenKhongDau = VanBanTiengViet.TaoKhongDau(d.Ten);
        x.MoTa = d.MoTa;
        x.ThuTu = d.ThuTu;
        x.TrangThai = d.TrangThai;
        x.Cap = d.Cap;
        x.DotDeNghiId = d.DotDeNghiId;
        x.DonViId = d.DonViId;
        x.SoQuyetDinhThanhLap = d.SoQuyetDinhThanhLap;
        x.NgayQuyetDinh = d.NgayQuyetDinh;
        x.TepQuyetDinhId = d.TepQuyetDinhId;
        x.ThoiGianHoatDongTu = d.ThoiGianHoatDongTu;
        x.ThoiGianHoatDongDen = d.ThoiGianHoatDongDen;
        x.LinhVucPhuTrach = d.LinhVucPhuTrach;
        x.SoThanhVienToiThieu = d.SoThanhVienToiThieu;
        x.TyLeThongQua = d.TyLeThongQua;
        x.TrangThaiHoatDong = d.TrangThaiHoatDong;
        return x;
    }
}

public sealed class HoiDongLuuDto
{
    public string Ma { get; set; } = string.Empty;

    public string Ten { get; set; } = string.Empty;

    public string? MoTa { get; set; }

    public int ThuTu { get; set; }

    public short TrangThai { get; set; } = TrangThaiDanhMuc.HoatDong;

    public string Cap { get; set; } = CapXetDuyet.CoSo;

    public Guid? DotDeNghiId { get; set; }

    public Guid? DonViId { get; set; }

    public string? SoQuyetDinhThanhLap { get; set; }

    public DateOnly? NgayQuyetDinh { get; set; }

    /// <summary>Tep quyet dinh thanh lap hoi dong (PDF/anh scan).</summary>
    public Guid? TepQuyetDinhId { get; set; }

    public DateOnly? ThoiGianHoatDongTu { get; set; }

    public DateOnly? ThoiGianHoatDongDen { get; set; }

    public List<Guid> LinhVucPhuTrach { get; set; } = new();

    public int SoThanhVienToiThieu { get; set; } = 5;

    public decimal TyLeThongQua { get; set; } = 50m;

    public string TrangThaiHoatDong { get; set; } = "DANG_HOAT_DONG";
}

public sealed class ThanhVienLuuDto
{
    public Guid Id { get; set; }

    public Guid? NguoiDungId { get; set; }

    public string HoTenHienThi { get; set; } = string.Empty;

    public string? ChucVuCongTac { get; set; }

    public string? DonViCongTac { get; set; }

    public string ChucDanh { get; set; } = ChucDanhHoiDong.UyVien;

    public bool QuyenChamDiem { get; set; } = true;

    public bool QuyenNhanXet { get; set; } = true;

    public bool QuyenBoPhieu { get; set; } = true;

    public bool QuyenKyBienBan { get; set; }

    public bool QuyenKetLuan { get; set; }
}

public sealed class PhienHopLuuDto
{
    public Guid HoiDongId { get; set; }

    public string? MaPhien { get; set; }

    public string TenPhien { get; set; } = string.Empty;

    public DateTimeOffset ThoiGianBatDau { get; set; }

    public DateTimeOffset? ThoiGianKetThuc { get; set; }

    public string? DiaDiem { get; set; }

    public string HinhThuc { get; set; } = "TRUC_TIEP";

    public Guid? ChuTriId { get; set; }

    public Guid? ThuKyId { get; set; }

    public string? NoiDung { get; set; }

    public List<Guid> SangKienIds { get; set; } = new();
}

public sealed class BoPhieuDto
{
    public Guid PhienHopId { get; set; }

    public Guid SangKienId { get; set; }

    public string YKien { get; set; } = YKienBoPhieu.DongY;

    public Guid? MucDeXuatId { get; set; }

    public string? GhiChu { get; set; }

    public bool LaPhieuKin { get; set; }
}

public sealed record KetQuaBoPhieuDto(
    int TongPhieu, int DongY, int KhongDongY, int YKienKhac, decimal TyLeDongY);
