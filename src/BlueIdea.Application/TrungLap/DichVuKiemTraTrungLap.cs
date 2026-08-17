using BlueIdea.Ai.Nhung;
using BlueIdea.Ai.TrungLap;
using BlueIdea.Ai.XuLyVanBan;
using BlueIdea.Application.Chung;
using BlueIdea.Domain.Ai;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.SangKien;
using BlueIdea.Shared.KetQua;
using Microsoft.Extensions.Logging;
using EntityKiemTra = BlueIdea.Domain.Ai.KiemTraTrungLap;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.TrungLap;

/// <summary>
/// Chuc nang 26 - Dieu phoi pipeline kiem tra trung lap: nap du lieu, cat doan, sinh embedding,
/// so khop, luu ket qua. Toan bo chay noi bo (xem docs/ADR/0001-ai-noi-bo.md).
/// </summary>
public sealed class DichVuKiemTraTrungLap
{
    private readonly IAppDbContext _db;
    private readonly IBoPhanTichTrungLap _phanTich;
    private readonly IBoNhungVanBan _boNhung;
    private readonly IDichVuCauHinh _cauHinh;
    private readonly IDongHoHeThong _dongHo;
    private readonly IDichVuPhanQuyen _phanQuyen;
    private readonly ILogger<DichVuKiemTraTrungLap> _logger;

    public DichVuKiemTraTrungLap(
        IAppDbContext db,
        IBoPhanTichTrungLap phanTich,
        IBoNhungVanBan boNhung,
        IDichVuCauHinh cauHinh,
        IDongHoHeThong dongHo,
        IDichVuPhanQuyen phanQuyen,
        ILogger<DichVuKiemTraTrungLap> logger)
    {
        _db = db;
        _phanTich = phanTich;
        _boNhung = boNhung;
        _cauHinh = cauHinh;
        _dongHo = dongHo;
        _phanQuyen = phanQuyen;
        _logger = logger;
    }

    /// <summary>
    /// Chay kiem tra trung lap cho mot ho so. Duoc goi tu Hangfire job hoac tu nut "Chạy lại".
    /// Loi cua buoc nay KHONG duoc lam hong luong nop ho so (graceful degradation - Muc 7).
    /// </summary>
    public async Task<KetQuaPhanTichTrungLap?> ChayAsync(
        Guid sangKienId, bool batBuocChayLai = false, CancellationToken ct = default)
    {
        var hoSo = await _db.SangKien.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == sangKienId, ct)
            .ConfigureAwait(false);

        if (hoSo is null)
        {
            return null;
        }

        if (!batBuocChayLai
            && hoSo.TrangThaiKiemTraTrungLap == TrangThaiKiemTraTrungLap.HoanThanh)
        {
            return null;
        }

        var banGhi = new EntityKiemTra
        {
            SangKienId = sangKienId,
            NgayChay = _dongHo.BayGio,
            TrangThaiChay = TrangThaiKiemTraTrungLap.DangChay
        };

        _db.KiemTraTrungLap.Add(banGhi);

        var hoSoTracking = await _db.SangKien.FirstAsync(x => x.Id == sangKienId, ct).ConfigureAwait(false);
        hoSoTracking.TrangThaiKiemTraTrungLap = TrangThaiKiemTraTrungLap.DangChay;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        try
        {
            var thamSo = await NapThamSoAsync(ct).ConfigureAwait(false);

            var taiLieuNguon = await NapTaiLieuAsync(sangKienId, ct).ConfigureAwait(false);
            if (taiLieuNguon is null || string.IsNullOrWhiteSpace(taiLieuNguon.ToanVan))
            {
                banGhi.TrangThaiChay = TrangThaiKiemTraTrungLap.HoanThanh;
                banGhi.TongSoDoiChieu = 0;
                banGhi.TyLeCaoNhat = 0m;
                banGhi.MucCanhBao = MucCanhBaoTrungLap.AnToan;
                hoSoTracking.TrangThaiKiemTraTrungLap = TrangThaiKiemTraTrungLap.HoanThanh;
                hoSoTracking.TyLeTrungLap = 0m;
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                return null;
            }

            var kho = await NapKhoDoiChieuAsync(sangKienId, ct).ConfigureAwait(false);

            var ketQua = await _phanTich
                .PhanTichAsync(taiLieuNguon, kho, thamSo, ct)
                .ConfigureAwait(false);

            await LuuDoanVanAsync(sangKienId, taiLieuNguon, ct).ConfigureAwait(false);

            banGhi.PhienBanThuatToan = ketQua.PhienBanThuatToan;
            banGhi.TongSoDoiChieu = ketQua.TongSoDoiChieu;
            banGhi.TyLeCaoNhat = ketQua.TyLeCaoNhat;
            banGhi.MucCanhBao = ketQua.MucCanhBao;
            banGhi.ThoiGianXuLyMs = (int)Math.Min(ketQua.ThoiGianXuLyMs, int.MaxValue);
            banGhi.TrangThaiChay = TrangThaiKiemTraTrungLap.HoanThanh;
            banGhi.PhamVi = new Dictionary<string, object>
            {
                ["moHinhNhung"] = ketQua.TenMoHinhNhung,
                ["soUngVien"] = kho.Count
            };

            foreach (var ct2 in ketQua.ChiTiet)
            {
                // Them truc tiep vao DbSet (khong qua navigation) de EF luon sinh INSERT
                // cho thuc the con moi cua mot ban ghi cha da duoc luu truoc do.
                _db.KiemTraTrungLapChiTiet.Add(new KiemTraTrungLapChiTiet
                {
                    Id = Guid.NewGuid(),
                    KiemTraId = banGhi.Id,
                    SangKienDoiChieuId = ct2.SangKienDoiChieuId,
                    TyLeTuongDong = ct2.TyLeTuongDong,
                    TyLeTuVung = ct2.TyLeTuVung,
                    TyLeNguNghia = ct2.TyLeNguNghia,
                    SoDoanTrung = ct2.SoDoanTrung,
                    CacDoanTrung = ct2.CacDoanTrung.Select(d => new DoanTrungLap
                    {
                        DoanNguon = d.DoanNguon,
                        DoanDich = d.DoanDich,
                        TyLe = d.TyLe,
                        ViTriBatDau = d.ViTriBatDau,
                        ViTriKetThuc = d.ViTriKetThuc
                    }).ToList()
                });
            }

            hoSoTracking.TyLeTrungLap = ketQua.TyLeCaoNhat;
            hoSoTracking.TrangThaiKiemTraTrungLap = TrangThaiKiemTraTrungLap.HoanThanh;

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return ketQua;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Lỗi khi kiểm tra trùng lặp hồ sơ {SangKienId}", sangKienId);

            // Context dang chua cac thay doi hong -> lam sach roi ghi trang thai loi bang du lieu moi nap.
            _db.XoaTheoDoi();

            var banGhiLoi = await _db.KiemTraTrungLap
                .FirstOrDefaultAsync(x => x.Id == banGhi.Id, CancellationToken.None)
                .ConfigureAwait(false);

            if (banGhiLoi is not null)
            {
                banGhiLoi.TrangThaiChay = TrangThaiKiemTraTrungLap.Loi;
                banGhiLoi.ThongBaoLoi = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            }

            var hoSoLoi = await _db.SangKien
                .FirstOrDefaultAsync(x => x.Id == sangKienId, CancellationToken.None)
                .ConfigureAwait(false);

            if (hoSoLoi is not null)
            {
                hoSoLoi.TrangThaiKiemTraTrungLap = TrangThaiKiemTraTrungLap.Loi;
            }

            await _db.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
            return null;
        }
    }

    /// <summary>Lay ket qua kiem tra gan nhat de hien thi tab "Trùng lặp".</summary>
    public async Task<EntityKiemTra?> LayKetQuaGanNhatAsync(
        Guid sangKienId, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.TrungLapXem, sangKienId, ct).ConfigureAwait(false);

        return await _db.KiemTraTrungLap.AsNoTracking()
            .Include(x => x.ChiTiet)
            .Where(x => x.SangKienId == sangKienId)
            .OrderByDescending(x => x.NgayChay)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    /// <summary>Danh dau "Đã xem xét" kem y kien hoi dong.</summary>
    public async Task DanhDauDaXemXetAsync(
        Guid kiemTraId, string? yKien, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.TrungLapXem, kiemTraId, ct).ConfigureAwait(false);

        var banGhi = await _db.KiemTraTrungLap
            .FirstOrDefaultAsync(x => x.Id == kiemTraId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("kết quả kiểm tra trùng lặp", kiemTraId);

        banGhi.DaXemXet = true;
        banGhi.YKienHoiDong = yKien;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // ------------------------------------------------------------------------------------

    private async Task<ThamSoTrungLap> NapThamSoAsync(CancellationToken ct)
    {
        var heSoTuVung = await _cauHinh.LayAsync(KhoaCauHinh.HeSoTuVung, 0.4m, ct).ConfigureAwait(false);
        var heSoNguNghia = await _cauHinh.LayAsync(KhoaCauHinh.HeSoNguNghia, 0.6m, ct).ConfigureAwait(false);
        var nguongVang = await _cauHinh
            .LayAsync(KhoaCauHinh.MucCanhBaoTrungLapVang, 20m, ct).ConfigureAwait(false);
        var nguongDo = await _cauHinh
            .LayAsync(KhoaCauHinh.MucCanhBaoTrungLapDo, 40m, ct).ConfigureAwait(false);

        return new ThamSoTrungLap
        {
            HeSoTuVung = heSoTuVung,
            HeSoNguNghia = heSoNguNghia,
            NguongCanhBaoVang = nguongVang,
            NguongCanhBaoDo = nguongDo
        };
    }

    /// <summary>Ghep toan van tu cac truong noi dung + noi dung trich xuat cua tep dinh kem.</summary>
    private async Task<TaiLieuSoSanh?> NapTaiLieuAsync(Guid sangKienId, CancellationToken ct)
    {
        var hoSo = await _db.SangKien.AsNoTracking()
            .Include(x => x.TepDinhKem).ThenInclude(t => t.TepTin)
            .FirstOrDefaultAsync(x => x.Id == sangKienId, ct)
            .ConfigureAwait(false);

        if (hoSo is null)
        {
            return null;
        }

        var phan = new List<string?>
        {
            hoSo.TenSangKien,
            hoSo.MoTaGiaiPhap,
            hoSo.TinhTrangTruocKhiApDung,
            hoSo.NoiDungGiaiPhap,
            hoSo.TinhMoi,
            hoSo.KhaNangApDung,
            hoSo.PhamViApDung,
            hoSo.HieuQuaKinhTe,
            hoSo.HieuQuaXaHoi
        };

        phan.AddRange(hoSo.NoiDungDong.Values);
        phan.AddRange(hoSo.TepDinhKem
            .Where(t => t.TepTin is not null && !string.IsNullOrWhiteSpace(t.TepTin.NoiDungTrichXuat))
            .Select(t => t.TepTin!.NoiDungTrichXuat));

        var toanVan = string.Join("\n\n", phan.Where(p => !string.IsNullOrWhiteSpace(p)));

        var doan = BoCatDoanVan.Cat(toanVan, ThamSoCatDoan.MacDinh)
            .Select(d => new DoanVanSoSanh
            {
                ChiMuc = d.ChiMuc,
                NoiDung = d.NoiDung,
                NoiDungChuanHoa = d.NoiDungChuanHoa,
                SimHash = d.SimHash,
                ViTriBatDau = d.ViTriBatDau,
                ViTriKetThuc = d.ViTriKetThuc
            })
            .ToList();

        return new TaiLieuSoSanh
        {
            SangKienId = hoSo.Id,
            MaHoSo = hoSo.MaHoSo,
            TenSangKien = hoSo.TenSangKien,
            LinhVucId = hoSo.LinhVucId,
            DonViId = hoSo.DonViId,
            ToanVan = toanVan,
            CacDoan = doan
        };
    }

    /// <summary>Nap kho doi chieu: cac ho so da nop khac (khong gioi han nam de bat duoc ca ho so cu).</summary>
    private async Task<IReadOnlyList<TaiLieuSoSanh>> NapKhoDoiChieuAsync(
        Guid sangKienId, CancellationToken ct)
    {
        var ids = await _db.SangKien.AsNoTracking()
            .Where(x => x.Id != sangKienId && x.TrangThaiTong != TrangThaiTongHoSo.Nhap)
            .OrderByDescending(x => x.NgayNop)
            .Select(x => x.Id)
            .Take(500)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var ketQua = new List<TaiLieuSoSanh>(ids.Count);
        foreach (var id in ids)
        {
            var taiLieu = await NapTaiLieuAsync(id, ct).ConfigureAwait(false);
            if (taiLieu is not null && !string.IsNullOrWhiteSpace(taiLieu.ToanVan))
            {
                ketQua.Add(taiLieu);
            }
        }

        return ketQua;
    }

    /// <summary>Luu cac doan van + embedding vao <c>sang_kien_doan_van</c> (pgvector).</summary>
    private async Task LuuDoanVanAsync(Guid sangKienId, TaiLieuSoSanh taiLieu, CancellationToken ct)
    {
        var cu = await _db.SangKienDoanVan
            .Where(x => x.SangKienId == sangKienId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var d in cu)
        {
            d.DaXoa = true;
        }

        foreach (var doan in taiLieu.CacDoan)
        {
            doan.Embedding ??= await _boNhung
                .TaoVectorAsync(doan.NoiDungChuanHoa, ct)
                .ConfigureAwait(false);

            _db.SangKienDoanVan.Add(new SangKienDoanVan
            {
                SangKienId = sangKienId,
                Nguon = "NOI_DUNG",
                ChiMuc = doan.ChiMuc,
                NoiDung = doan.NoiDung,
                NoiDungChuanHoa = doan.NoiDungChuanHoa,
                SoTu = doan.NoiDungChuanHoa.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
                SimHash = doan.SimHash,
                Embedding = doan.Embedding
            });
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
