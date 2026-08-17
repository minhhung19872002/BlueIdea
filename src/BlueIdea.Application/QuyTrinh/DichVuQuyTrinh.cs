using BlueIdea.Application.Chung;
using BlueIdea.Application.DanhMuc;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuyTrinh;
using BlueIdea.Shared.KetQua;
using BlueIdea.Shared.TiengViet;
using BlueIdea.Workflow.KiemTra;
using Microsoft.EntityFrameworkCore;
using ThucTheQuyTrinh = BlueIdea.Domain.QuyTrinh.QuyTrinh;

namespace BlueIdea.Application.CauHinhQuyTrinh;

/// <summary>
/// Nhom chuc nang 9-16: cau hinh quy trinh dong, trinh thiet ke, validator, phien ban.
/// </summary>
public sealed class DichVuQuyTrinh : DichVuDanhMucCoSo<ThucTheQuyTrinh>
{
    private readonly IKiemTraQuyTrinh _kiemTra;

    public DichVuQuyTrinh(
        IAppDbContext db, IDichVuPhanQuyen phanQuyen, IDongHoHeThong dongHo, IKiemTraQuyTrinh kiemTra)
        : base(db, phanQuyen, dongHo)
        => _kiemTra = kiemTra;

    protected override DbSet<ThucTheQuyTrinh> BangDuLieu => Db.QuyTrinh;

    protected override string TenDanhMuc => "Quy trình";

    protected override IQueryable<ThucTheQuyTrinh> TaoTruyVanChiTiet()
        => Db.QuyTrinh.AsNoTracking()
            .Include(q => q.DanhSachBuoc).ThenInclude(b => b.TacNhan)
            .Include(q => q.DanhSachBuoc).ThenInclude(b => b.TruongHop)
            .Include(q => q.DanhSachBuoc).ThenInclude(b => b.TrangThai)
            .Include(q => q.ThanhPhanHoSo)
            .Include(q => q.ChucNangBoSung)
            .Include(q => q.TrangThaiToanCuc);

    /// <summary>Nap quy trinh day du (tracking) de sua so do.</summary>
    private IQueryable<ThucTheQuyTrinh> TruyVanDayDu()
        => Db.QuyTrinh
            .Include(q => q.DanhSachBuoc).ThenInclude(b => b.TacNhan)
            .Include(q => q.DanhSachBuoc).ThenInclude(b => b.TruongHop)
            .Include(q => q.DanhSachBuoc).ThenInclude(b => b.TrangThai)
            .Include(q => q.ThanhPhanHoSo)
            .Include(q => q.ChucNangBoSung)
            .Include(q => q.TrangThaiToanCuc);

    /// <summary>Lay so do day du de render tren ReactFlow.</summary>
    public async Task<SoDoQuyTrinhDto> LaySoDoAsync(Guid id, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.QuyTrinhXem, id, ct).ConfigureAwait(false);

        var quyTrinh = await TaoTruyVanChiTiet()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException(TenDanhMuc, id);

        return new SoDoQuyTrinhDto
        {
            SoDoLayout = quyTrinh.SoDoLayout,
            DanhSachBuoc = quyTrinh.DanhSachBuoc
                .Where(b => !b.DaXoa)
                .OrderBy(b => b.ThuTu)
                .Select(b => new BuocDto
                {
                    Id = b.Id,
                    Ma = b.Ma,
                    Ten = b.Ten,
                    ThuTu = b.ThuTu,
                    LoaiBuoc = b.LoaiBuoc,
                    SoNgayXuLy = b.SoNgayXuLy,
                    TinhTheoNgayLamViec = b.TinhTheoNgayLamViec,
                    BatBuocDinhKem = b.BatBuocDinhKem,
                    DanhSachTepBatBuoc = b.DanhSachTepBatBuoc,
                    BatBuocNhapYKien = b.BatBuocNhapYKien,
                    ChoPhepUyQuyen = b.ChoPhepUyQuyen,
                    ChoPhepThuHoi = b.ChoPhepThuHoi,
                    LaBuocBatDau = b.LaBuocBatDau,
                    LaBuocKetThuc = b.LaBuocKetThuc,
                    CanhBaoTruocHanGio = b.CanhBaoTruocHanGio,
                    MoTaHuongDan = b.MoTaHuongDan,
                    HoiDongId = b.HoiDongId,
                    BoTieuChiId = b.BoTieuChiId,
                    TacNhan = b.TacNhan.Where(t => !t.DaXoa).OrderBy(t => t.ThuTu)
                        .Select(t => new TacNhanDto
                        {
                            Id = t.Id,
                            LoaiTacNhan = t.LoaiTacNhan,
                            ThamChieuId = t.ThamChieuId,
                            ThamChieuMa = t.ThamChieuMa,
                            QuyTacXuLy = t.QuyTacXuLy,
                            TyLeDongThuan = t.TyLeDongThuan,
                            ThuTu = t.ThuTu
                        }).ToList(),
                    TruongHop = b.TruongHop.Where(t => !t.DaXoa).OrderBy(t => t.ThuTu)
                        .Select(t => new TruongHopDto
                        {
                            Id = t.Id,
                            Ma = t.Ma,
                            Ten = t.Ten,
                            BuocTiepTheoId = t.BuocTiepTheoId,
                            TrangThaiGanId = t.TrangThaiGanId,
                            DieuKien = t.DieuKien,
                            HanhDong = t.HanhDong,
                            MauThongBaoId = t.MauThongBaoId,
                            MauNut = t.MauNut,
                            ThuTu = t.ThuTu,
                            LaMacDinh = t.LaMacDinh
                        }).ToList(),
                    TrangThai = b.TrangThai.Where(t => !t.DaXoa).OrderBy(t => t.ThuTu)
                        .Select(t => new TrangThaiBuocDto
                        {
                            Id = t.Id,
                            Ma = t.Ma,
                            Ten = t.Ten,
                            MauSac = t.MauSac,
                            Icon = t.Icon,
                            LaTrangThaiKetThuc = t.LaTrangThaiKetThuc,
                            HienThiChoTacGia = t.HienThiChoTacGia,
                            ThuTu = t.ThuTu
                        }).ToList()
                }).ToList(),
            TrangThaiToanCuc = quyTrinh.TrangThaiToanCuc
                .Where(t => !t.DaXoa && t.BuocId == null)
                .OrderBy(t => t.ThuTu)
                .Select(t => new TrangThaiToanCucDto
                {
                    Id = t.Id,
                    Ma = t.Ma,
                    Ten = t.Ten,
                    MauSac = t.MauSac,
                    Icon = t.Icon,
                    LaTrangThaiKetThuc = t.LaTrangThaiKetThuc,
                    HienThiChoTacGia = t.HienThiChoTacGia,
                    ThuTu = t.ThuTu
                }).ToList(),
            ThanhPhanHoSo = quyTrinh.ThanhPhanHoSo
                .Where(t => !t.DaXoa).OrderBy(t => t.ThuTu)
                .Select(t => new ThanhPhanHoSoCauHinhDto
                {
                    Id = t.Id,
                    Ma = t.Ma,
                    Ten = t.Ten,
                    BatBuoc = t.BatBuoc,
                    LoaiDuLieu = t.LoaiDuLieu,
                    DinhDangChoPhep = t.DinhDangChoPhep,
                    DungLuongToiDaMb = t.DungLuongToiDaMb,
                    SoLuongToiDa = t.SoLuongToiDa,
                    SoKyTuToiThieu = t.SoKyTuToiThieu,
                    SoKyTuToiDa = t.SoKyTuToiDa,
                    DungDeKiemTraTrungLap = t.DungDeKiemTraTrungLap,
                    ThuTu = t.ThuTu,
                    MoTaHuongDan = t.MoTaHuongDan
                }).ToList(),
            ChucNangBoSung = quyTrinh.ChucNangBoSung
                .Where(t => !t.DaXoa)
                .Select(t => new ChucNangBoSungDto
                {
                    Id = t.Id,
                    BuocId = t.BuocId,
                    MaChucNang = t.MaChucNang,
                    BatBuoc = t.BatBuoc,
                    CauHinh = t.CauHinh
                }).ToList()
        };
    }

    /// <summary>
    /// Luu toan bo so do trong MOT giao dich (dac ta yeu cau PUT /quy-trinh/{id}/so-do).
    /// Chan sua khi quy trinh dang co ho so chay -> bat buoc tao phien ban moi.
    /// </summary>
    public async Task LuuSoDoAsync(Guid id, SoDoQuyTrinhDto soDo, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.QuyTrinhCauHinh, id, ct).ConfigureAwait(false);

        var quyTrinh = await TruyVanDayDu()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException(TenDanhMuc, id);

        await BatBuocKhongCoHoSoDangChayAsync(id, ct).ConfigureAwait(false);

        // Xoa mem toan bo cau hinh cu roi dung lai theo du lieu gui len.
        foreach (var b in quyTrinh.DanhSachBuoc)
        {
            b.DaXoa = true;
            foreach (var t in b.TacNhan)
            {
                t.DaXoa = true;
            }

            foreach (var t in b.TruongHop)
            {
                t.DaXoa = true;
            }

            foreach (var t in b.TrangThai)
            {
                t.DaXoa = true;
            }
        }

        foreach (var t in quyTrinh.TrangThaiToanCuc)
        {
            t.DaXoa = true;
        }

        foreach (var t in quyTrinh.ThanhPhanHoSo)
        {
            t.DaXoa = true;
        }

        foreach (var t in quyTrinh.ChucNangBoSung)
        {
            t.DaXoa = true;
        }

        quyTrinh.SoDoLayout = soDo.SoDoLayout;

        foreach (var b in soDo.DanhSachBuoc)
        {
            var buoc = new QuyTrinhBuoc
            {
                Id = b.Id == Guid.Empty ? Guid.NewGuid() : b.Id,
                QuyTrinhId = quyTrinh.Id,
                Ma = b.Ma,
                Ten = b.Ten,
                ThuTu = b.ThuTu,
                LoaiBuoc = b.LoaiBuoc,
                SoNgayXuLy = b.SoNgayXuLy,
                TinhTheoNgayLamViec = b.TinhTheoNgayLamViec,
                BatBuocDinhKem = b.BatBuocDinhKem,
                DanhSachTepBatBuoc = b.DanhSachTepBatBuoc,
                BatBuocNhapYKien = b.BatBuocNhapYKien,
                ChoPhepUyQuyen = b.ChoPhepUyQuyen,
                ChoPhepThuHoi = b.ChoPhepThuHoi,
                LaBuocBatDau = b.LaBuocBatDau,
                LaBuocKetThuc = b.LaBuocKetThuc,
                CanhBaoTruocHanGio = b.CanhBaoTruocHanGio,
                MoTaHuongDan = b.MoTaHuongDan,
                HoiDongId = b.HoiDongId,
                BoTieuChiId = b.BoTieuChiId
            };

            foreach (var t in b.TacNhan)
            {
                buoc.TacNhan.Add(new QuyTrinhBuocTacNhan
                {
                    Id = t.Id == Guid.Empty ? Guid.NewGuid() : t.Id,
                    BuocId = buoc.Id,
                    LoaiTacNhan = t.LoaiTacNhan,
                    ThamChieuId = t.ThamChieuId,
                    ThamChieuMa = t.ThamChieuMa,
                    QuyTacXuLy = t.QuyTacXuLy,
                    TyLeDongThuan = t.TyLeDongThuan,
                    ThuTu = t.ThuTu
                });
            }

            foreach (var t in b.TruongHop)
            {
                buoc.TruongHop.Add(new QuyTrinhTruongHop
                {
                    Id = t.Id == Guid.Empty ? Guid.NewGuid() : t.Id,
                    BuocId = buoc.Id,
                    Ma = t.Ma,
                    Ten = t.Ten,
                    BuocTiepTheoId = t.BuocTiepTheoId,
                    TrangThaiGanId = t.TrangThaiGanId,
                    DieuKien = t.DieuKien,
                    HanhDong = t.HanhDong,
                    MauThongBaoId = t.MauThongBaoId,
                    MauNut = t.MauNut,
                    ThuTu = t.ThuTu,
                    LaMacDinh = t.LaMacDinh
                });
            }

            foreach (var t in b.TrangThai)
            {
                buoc.TrangThai.Add(new QuyTrinhTrangThai
                {
                    Id = t.Id == Guid.Empty ? Guid.NewGuid() : t.Id,
                    QuyTrinhId = quyTrinh.Id,
                    BuocId = buoc.Id,
                    Ma = t.Ma,
                    Ten = t.Ten,
                    MauSac = t.MauSac,
                    Icon = t.Icon,
                    LaTrangThaiKetThuc = t.LaTrangThaiKetThuc,
                    HienThiChoTacGia = t.HienThiChoTacGia,
                    ThuTu = t.ThuTu
                });
            }

            Db.QuyTrinhBuoc.Add(buoc);
        }

        foreach (var t in soDo.TrangThaiToanCuc)
        {
            Db.QuyTrinhTrangThai.Add(new QuyTrinhTrangThai
            {
                Id = t.Id == Guid.Empty ? Guid.NewGuid() : t.Id,
                QuyTrinhId = quyTrinh.Id,
                BuocId = null,
                Ma = t.Ma,
                Ten = t.Ten,
                MauSac = t.MauSac,
                Icon = t.Icon,
                LaTrangThaiKetThuc = t.LaTrangThaiKetThuc,
                HienThiChoTacGia = t.HienThiChoTacGia,
                ThuTu = t.ThuTu
            });
        }

        foreach (var t in soDo.ThanhPhanHoSo)
        {
            Db.QuyTrinhThanhPhanHoSo.Add(new QuyTrinhThanhPhanHoSo
            {
                Id = t.Id == Guid.Empty ? Guid.NewGuid() : t.Id,
                QuyTrinhId = quyTrinh.Id,
                Ma = t.Ma,
                Ten = t.Ten,
                BatBuoc = t.BatBuoc,
                LoaiDuLieu = t.LoaiDuLieu,
                DinhDangChoPhep = t.DinhDangChoPhep,
                DungLuongToiDaMb = t.DungLuongToiDaMb,
                SoLuongToiDa = t.SoLuongToiDa,
                SoKyTuToiThieu = t.SoKyTuToiThieu,
                SoKyTuToiDa = t.SoKyTuToiDa,
                DungDeKiemTraTrungLap = t.DungDeKiemTraTrungLap,
                ThuTu = t.ThuTu,
                MoTaHuongDan = t.MoTaHuongDan
            });
        }

        foreach (var t in soDo.ChucNangBoSung)
        {
            Db.QuyTrinhChucNangBoSung.Add(new QuyTrinhChucNangBoSung
            {
                Id = t.Id == Guid.Empty ? Guid.NewGuid() : t.Id,
                QuyTrinhId = quyTrinh.Id,
                BuocId = t.BuocId,
                MaChucNang = t.MaChucNang,
                BatBuoc = t.BatBuoc,
                CauHinh = t.CauHinh
            });
        }

        await Db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Chay validator 7 rule bat buoc.</summary>
    public async Task<KetQuaKiemTraDto> KiemTraAsync(Guid id, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.QuyTrinhXem, id, ct).ConfigureAwait(false);

        var quyTrinh = await TaoTruyVanChiTiet()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException(TenDanhMuc, id);

        var ketQua = _kiemTra.KiemTra(quyTrinh);

        return new KetQuaKiemTraDto(
            ketQua.HopLe,
            ketQua.DanhSachLoi.Select(l => new PhatHienDto(l.Ma, l.ThongBao, l.BuocId, l.TenBuoc)).ToList(),
            ketQua.DanhSachCanhBao.Select(l => new PhatHienDto(l.Ma, l.ThongBao, l.BuocId, l.TenBuoc)).ToList());
    }

    /// <summary>Kich hoat quy trinh (chi khi validator khong con loi).</summary>
    public async Task<KetQuaKiemTraDto> KichHoatAsync(Guid id, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.QuyTrinhCauHinh, id, ct).ConfigureAwait(false);

        var quyTrinh = await TruyVanDayDu()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException(TenDanhMuc, id);

        var ketQua = _kiemTra.KiemTra(quyTrinh);
        if (!ketQua.HopLe)
        {
            throw new NghiepVuException(MaLoiHeThong.QuyTrinhKhongHopLe,
                "Quy trình chưa hợp lệ: " + string.Join(" ", ketQua.DanhSachLoi.Select(l => l.ThongBao)));
        }

        quyTrinh.TrangThaiQuyTrinh = TrangThaiQuyTrinh.DangApDung;
        await Db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new KetQuaKiemTraDto(
            true,
            Array.Empty<PhatHienDto>(),
            ketQua.DanhSachCanhBao.Select(l => new PhatHienDto(l.Ma, l.ThongBao, l.BuocId, l.TenBuoc)).ToList());
    }

    public async Task NgungApDungAsync(Guid id, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.QuyTrinhCauHinh, id, ct).ConfigureAwait(false);

        var quyTrinh = await Db.QuyTrinh.FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException(TenDanhMuc, id);

        quyTrinh.TrangThaiQuyTrinh = TrangThaiQuyTrinh.NgungApDung;
        await Db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Sao chep toan bo quy trinh (buoc, truong hop, tac nhan, thanh phan ho so).</summary>
    public async Task<Guid> SaoChepAsync(
        Guid id, string maMoi, string tenMoi, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.QuyTrinhCauHinh, ct: ct).ConfigureAwait(false);
        await BatBuocMaChuaTonTaiAsync(maMoi, null, ct).ConfigureAwait(false);

        return await NhanBanAsync(id, maMoi, tenMoi, phienBanMoi: false, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Tao phien ban moi: giu nguyen ma, tang so phien ban, tro ve quy trinh goc.
    /// Ho so cu van chay theo snapshot nen khong bi anh huong.
    /// </summary>
    public async Task<Guid> TaoPhienBanMoiAsync(Guid id, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.QuyTrinhCauHinh, id, ct).ConfigureAwait(false);

        var goc = await Db.QuyTrinh.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException(TenDanhMuc, id);

        var phienBanLonNhat = await Db.QuyTrinh.AsNoTracking()
            .Where(x => x.Ma == goc.Ma)
            .MaxAsync(x => (int?)x.PhienBan, ct)
            .ConfigureAwait(false) ?? goc.PhienBan;

        var idMoi = await NhanBanAsync(
            id, goc.Ma, $"{goc.Ten} (phiên bản {phienBanLonNhat + 1})",
            phienBanMoi: true, ct).ConfigureAwait(false);

        return idMoi;
    }

    private async Task<Guid> NhanBanAsync(
        Guid id, string ma, string ten, bool phienBanMoi, CancellationToken ct)
    {
        var goc = await TaoTruyVanChiTiet()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException(TenDanhMuc, id);

        var phienBanLonNhat = await Db.QuyTrinh.AsNoTracking()
            .Where(x => x.Ma == ma)
            .MaxAsync(x => (int?)x.PhienBan, ct)
            .ConfigureAwait(false) ?? 0;

        var moi = new ThucTheQuyTrinh
        {
            Id = Guid.NewGuid(),
            Ma = ma,
            Ten = ten,
            TenKhongDau = VanBanTiengViet.TaoKhongDau(ten),
            MoTa = goc.MoTa,
            Cap = goc.Cap,
            PhienBan = phienBanMoi ? phienBanLonNhat + 1 : 1,
            QuyTrinhGocId = phienBanMoi ? goc.QuyTrinhGocId ?? goc.Id : goc.Id,
            PhamViApDung = goc.PhamViApDung,
            LaMacDinh = false,
            TrangThaiQuyTrinh = TrangThaiQuyTrinh.Nhap,
            SoDoLayout = goc.SoDoLayout,
            ThuTu = goc.ThuTu
        };

        // Anh xa Id cu -> Id moi de gan lai cac tham chieu noi bo.
        var anhXaBuoc = goc.DanhSachBuoc.Where(b => !b.DaXoa)
            .ToDictionary(b => b.Id, _ => Guid.NewGuid());

        var anhXaTrangThai = goc.DanhSachBuoc.SelectMany(b => b.TrangThai)
            .Concat(goc.TrangThaiToanCuc)
            .Where(t => !t.DaXoa)
            .ToDictionary(t => t.Id, _ => Guid.NewGuid());

        Db.QuyTrinh.Add(moi);

        foreach (var b in goc.DanhSachBuoc.Where(x => !x.DaXoa))
        {
            var buoc = new QuyTrinhBuoc
            {
                Id = anhXaBuoc[b.Id],
                QuyTrinhId = moi.Id,
                Ma = b.Ma,
                Ten = b.Ten,
                ThuTu = b.ThuTu,
                LoaiBuoc = b.LoaiBuoc,
                SoNgayXuLy = b.SoNgayXuLy,
                TinhTheoNgayLamViec = b.TinhTheoNgayLamViec,
                BatBuocDinhKem = b.BatBuocDinhKem,
                DanhSachTepBatBuoc = b.DanhSachTepBatBuoc.ToList(),
                BatBuocNhapYKien = b.BatBuocNhapYKien,
                ChoPhepUyQuyen = b.ChoPhepUyQuyen,
                ChoPhepThuHoi = b.ChoPhepThuHoi,
                LaBuocBatDau = b.LaBuocBatDau,
                LaBuocKetThuc = b.LaBuocKetThuc,
                CanhBaoTruocHanGio = b.CanhBaoTruocHanGio,
                MoTaHuongDan = b.MoTaHuongDan,
                HoiDongId = b.HoiDongId,
                BoTieuChiId = b.BoTieuChiId
            };

            foreach (var t in b.TacNhan.Where(x => !x.DaXoa))
            {
                buoc.TacNhan.Add(new QuyTrinhBuocTacNhan
                {
                    Id = Guid.NewGuid(),
                    BuocId = buoc.Id,
                    LoaiTacNhan = t.LoaiTacNhan,
                    ThamChieuId = t.ThamChieuId,
                    ThamChieuMa = t.ThamChieuMa,
                    QuyTacXuLy = t.QuyTacXuLy,
                    TyLeDongThuan = t.TyLeDongThuan,
                    ThuTu = t.ThuTu
                });
            }

            foreach (var t in b.TrangThai.Where(x => !x.DaXoa))
            {
                buoc.TrangThai.Add(new QuyTrinhTrangThai
                {
                    Id = anhXaTrangThai[t.Id],
                    QuyTrinhId = moi.Id,
                    BuocId = buoc.Id,
                    Ma = t.Ma,
                    Ten = t.Ten,
                    MauSac = t.MauSac,
                    Icon = t.Icon,
                    LaTrangThaiKetThuc = t.LaTrangThaiKetThuc,
                    HienThiChoTacGia = t.HienThiChoTacGia,
                    ThuTu = t.ThuTu
                });
            }

            foreach (var t in b.TruongHop.Where(x => !x.DaXoa))
            {
                buoc.TruongHop.Add(new QuyTrinhTruongHop
                {
                    Id = Guid.NewGuid(),
                    BuocId = buoc.Id,
                    Ma = t.Ma,
                    Ten = t.Ten,
                    BuocTiepTheoId = t.BuocTiepTheoId.HasValue
                                     && anhXaBuoc.TryGetValue(t.BuocTiepTheoId.Value, out var idMoi)
                        ? idMoi
                        : null,
                    TrangThaiGanId = t.TrangThaiGanId.HasValue
                                     && anhXaTrangThai.TryGetValue(t.TrangThaiGanId.Value, out var ttMoi)
                        ? ttMoi
                        : null,
                    DieuKien = t.DieuKien,
                    HanhDong = t.HanhDong.ToList(),
                    MauThongBaoId = t.MauThongBaoId,
                    MauNut = t.MauNut,
                    ThuTu = t.ThuTu,
                    LaMacDinh = t.LaMacDinh
                });
            }

            Db.QuyTrinhBuoc.Add(buoc);
        }

        foreach (var t in goc.TrangThaiToanCuc.Where(x => !x.DaXoa && x.BuocId == null))
        {
            Db.QuyTrinhTrangThai.Add(new QuyTrinhTrangThai
            {
                Id = anhXaTrangThai[t.Id],
                QuyTrinhId = moi.Id,
                BuocId = null,
                Ma = t.Ma,
                Ten = t.Ten,
                MauSac = t.MauSac,
                Icon = t.Icon,
                LaTrangThaiKetThuc = t.LaTrangThaiKetThuc,
                HienThiChoTacGia = t.HienThiChoTacGia,
                ThuTu = t.ThuTu
            });
        }

        foreach (var t in goc.ThanhPhanHoSo.Where(x => !x.DaXoa))
        {
            Db.QuyTrinhThanhPhanHoSo.Add(new QuyTrinhThanhPhanHoSo
            {
                Id = Guid.NewGuid(),
                QuyTrinhId = moi.Id,
                Ma = t.Ma,
                Ten = t.Ten,
                BatBuoc = t.BatBuoc,
                LoaiDuLieu = t.LoaiDuLieu,
                DinhDangChoPhep = t.DinhDangChoPhep.ToList(),
                DungLuongToiDaMb = t.DungLuongToiDaMb,
                SoLuongToiDa = t.SoLuongToiDa,
                SoKyTuToiThieu = t.SoKyTuToiThieu,
                SoKyTuToiDa = t.SoKyTuToiDa,
                DungDeKiemTraTrungLap = t.DungDeKiemTraTrungLap,
                ThuTu = t.ThuTu,
                MoTaHuongDan = t.MoTaHuongDan
            });
        }

        foreach (var t in goc.ChucNangBoSung.Where(x => !x.DaXoa))
        {
            Db.QuyTrinhChucNangBoSung.Add(new QuyTrinhChucNangBoSung
            {
                Id = Guid.NewGuid(),
                QuyTrinhId = moi.Id,
                BuocId = t.BuocId.HasValue && anhXaBuoc.TryGetValue(t.BuocId.Value, out var bId)
                    ? bId
                    : null,
                MaChucNang = t.MaChucNang,
                BatBuoc = t.BatBuoc,
                CauHinh = t.CauHinh
            });
        }

        await Db.SaveChangesAsync(ct).ConfigureAwait(false);
        return moi.Id;
    }

    /// <summary>
    /// Quy trinh dang co ho so chay thi khong duoc sua (dac ta Muc 4.2).
    /// </summary>
    private async Task BatBuocKhongCoHoSoDangChayAsync(Guid quyTrinhId, CancellationToken ct)
    {
        var dangChay = await Db.SangKien.AsNoTracking()
            .CountAsync(x => x.QuyTrinhId == quyTrinhId
                             && x.TrangThaiTong != TrangThaiTongHoSo.Nhap
                             && x.BuocHienTaiId != null, ct)
            .ConfigureAwait(false);

        if (dangChay > 0)
        {
            throw new NghiepVuException(MaLoiHeThong.QuyTrinhDangSuDung,
                $"Quy trình đang có {dangChay} hồ sơ xử lý dở dang. "
                + "Vui lòng dùng chức năng \"Tạo phiên bản mới\" thay vì sửa trực tiếp.");
        }
    }

    protected override async Task<IReadOnlyList<NoiThamChieu>> LayNoiThamChieuAsync(
        Guid id, CancellationToken ct)
    {
        var ketQua = new List<NoiThamChieu>();

        var soHoSo = await Db.SangKien.CountAsync(x => x.QuyTrinhId == id, ct).ConfigureAwait(false);
        if (soHoSo > 0)
        {
            ketQua.Add(new NoiThamChieu("sang_kien", "Hồ sơ sáng kiến", soHoSo));
        }

        var soDot = await Db.DotDeNghi.CountAsync(x => x.QuyTrinhId == id, ct).ConfigureAwait(false);
        if (soDot > 0)
        {
            ketQua.Add(new NoiThamChieu("dot_de_nghi", "Đợt đề nghị", soDot));
        }

        return ketQua;
    }
}
