using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuyTrinh;
using BlueIdea.Domain.SangKien;
using BlueIdea.Shared.KetQua;
using BlueIdea.Workflow;
using BlueIdea.Workflow.MoHinh;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.XuLy;

/// <summary>
/// Cai dat <see cref="IWorkflowEngine"/>: nap du lieu tu CSDL, uy quyen nghiep vu cho
/// <see cref="IBoMayQuyTrinh"/> (thuan logic, da co unit test), roi luu ket qua.
/// </summary>
public sealed class DichVuWorkflow : IWorkflowEngine
{
    private readonly IAppDbContext _db;
    private readonly IBoMayQuyTrinh _boMay;
    private readonly IBoChuyenDoiSnapshotQuyTrinh _snapshot;
    private readonly IDongHoHeThong _dongHo;
    private readonly INguoiDungHienTai _nguoiDungHienTai;

    public DichVuWorkflow(
        IAppDbContext db, IBoMayQuyTrinh boMay, IBoChuyenDoiSnapshotQuyTrinh snapshot,
        IDongHoHeThong dongHo, INguoiDungHienTai nguoiDungHienTai)
    {
        _db = db;
        _boMay = boMay;
        _snapshot = snapshot;
        _dongHo = dongHo;
        _nguoiDungHienTai = nguoiDungHienTai;
    }

    public async Task<WorkflowInstance> KhoiTaoAsync(Guid sangKienId, Guid quyTrinhId, CancellationToken ct)
    {
        var hoSo = await _db.SangKien.FirstOrDefaultAsync(x => x.Id == sangKienId, ct).ConfigureAwait(false)
                   ?? throw new KhongTimThayException("hồ sơ sáng kiến", sangKienId);

        var quyTrinh = await NapQuyTrinhTuDbAsync(quyTrinhId, ct).ConfigureAwait(false);

        hoSo.QuyTrinhSnapshot = _snapshot.TaoSnapshot(quyTrinh);
        var ketQua = _boMay.KhoiTao(hoSo, quyTrinh, _dongHo.BayGio, out var banGhi);

        if (!ketQua.ThanhCong)
        {
            throw new NghiepVuException(ketQua.MaLoi ?? MaLoiHeThong.QuyTrinhKhongHopLe, ketQua.ThongBao);
        }

        if (banGhi is not null)
        {
            _db.SangKienXuLy.Add(banGhi);
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return await TaoWorkflowInstanceAsync(hoSo, quyTrinh, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<HanhDongKhaDung>> LayHanhDongKhaDungAsync(
        Guid sangKienId, Guid nguoiDungId, CancellationToken ct)
    {
        var nguCanh = await TaoNguCanhAsync(sangKienId, nguoiDungId, ct).ConfigureAwait(false);
        return nguCanh is null ? Array.Empty<HanhDongKhaDung>() : _boMay.LayHanhDongKhaDung(nguCanh);
    }

    public async Task<bool> KiemTraQuyenXuLyAsync(
        Guid sangKienId, Guid buocId, Guid nguoiDungId, CancellationToken ct)
    {
        var nguCanh = await TaoNguCanhAsync(sangKienId, nguoiDungId, ct).ConfigureAwait(false);
        return nguCanh is not null && _boMay.KiemTraQuyenXuLy(nguCanh, buocId);
    }

    public async Task<KetQuaXuLy> ThucThiAsync(XuLyBuocRequest request, CancellationToken ct)
    {
        var nguCanh = await TaoNguCanhAsync(request.SangKienId, request.NguoiDungId, ct)
            .ConfigureAwait(false);

        if (nguCanh is null)
        {
            return KetQuaXuLy.Loi(MaLoiHeThong.QuyTrinhKhongHopLe,
                "Hồ sơ chưa gắn quy trình xử lý hợp lệ.");
        }

        var ketQua = _boMay.ThucThi(nguCanh, request, out var banGhiMoi);

        if (!ketQua.ThanhCong)
        {
            return ketQua;
        }

        if (banGhiMoi is not null)
        {
            _db.SangKienXuLy.Add(banGhiMoi);
        }

        // Khi chuyen sang buoc moi: mo ban ghi cho cac tac nhan cua buoc do.
        if (!ketQua.ChoThemTacNhan && ketQua.BuocMoiId.HasValue && !ketQua.DaKetThucQuyTrinh)
        {
            var buocMoi = nguCanh.QuyTrinh.DanhSachBuoc.First(b => b.Id == ketQua.BuocMoiId.Value);
            _db.SangKienXuLy.Add(new SangKienXuLy
            {
                SangKienId = nguCanh.HoSo.Id,
                BuocId = buocMoi.Id,
                TenBuocSnapshot = buocMoi.Ten,
                TrangThaiId = ketQua.TrangThaiMoiId,
                ThoiGianNhan = _dongHo.BayGio,
                HanXuLy = ketQua.HanXuLyMoi,
                ThuTu = nguCanh.LichSuXuLy.Count + 2
            });
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return ketQua;
    }

    public async Task ThuHoiAsync(Guid sangKienId, Guid nguoiDungId, string lyDo, CancellationToken ct)
    {
        var hoSo = await _db.SangKien.FirstOrDefaultAsync(x => x.Id == sangKienId, ct).ConfigureAwait(false)
                   ?? throw new KhongTimThayException("hồ sơ sáng kiến", sangKienId);

        var quyTrinh = _snapshot.DocSnapshot(hoSo.QuyTrinhSnapshot)
                       ?? throw new NghiepVuException(MaLoiHeThong.QuyTrinhKhongHopLe,
                           "Hồ sơ chưa có snapshot quy trình.");

        var lichSu = await _db.SangKienXuLy
            .Where(x => x.SangKienId == sangKienId)
            .OrderBy(x => x.ThuTu)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // Buoc gan nhat da hoan thanh boi chinh nguoi nay va cho phep thu hoi.
        var banGhiCuoi = lichSu
            .Where(x => x.DaHoanThanh && x.NguoiXuLyId == nguoiDungId)
            .OrderByDescending(x => x.ThoiGianXuLy)
            .FirstOrDefault();

        if (banGhiCuoi is null)
        {
            throw new NghiepVuException(MaLoiHeThong.KhongCoQuyenXuLyBuoc,
                "Không tìm thấy bước xử lý nào của bạn để thu hồi.");
        }

        var buoc = quyTrinh.DanhSachBuoc.FirstOrDefault(b => b.Id == banGhiCuoi.BuocId);
        if (buoc is null || !buoc.ChoPhepThuHoi)
        {
            throw new NghiepVuException(MaLoiHeThong.KhongCoQuyenXuLyBuoc,
                $"Bước '{banGhiCuoi.TenBuocSnapshot}' không cho phép thu hồi.");
        }

        // Xoa cac ban ghi phat sinh sau do, dua ho so ve lai buoc cu.
        var phatSinhSau = lichSu.Where(x => x.ThuTu > banGhiCuoi.ThuTu).ToList();
        foreach (var x in phatSinhSau)
        {
            x.DaXoa = true;
        }

        banGhiCuoi.ThoiGianXuLy = null;
        banGhiCuoi.TruongHopId = null;
        banGhiCuoi.TenTruongHopSnapshot = null;
        banGhiCuoi.SoNgayXuLy = null;

        hoSo.BuocHienTaiId = banGhiCuoi.BuocId;
        hoSo.TrangThaiHienTaiId = banGhiCuoi.TrangThaiId;
        hoSo.HanXuLyHienTai = banGhiCuoi.HanXuLy;
        hoSo.TrangThaiTong = TrangThaiTongHoSo.DangXuLy;
        hoSo.NgayHoanThanh = null;
        hoSo.PhienBan++;

        _db.SangKienLichSu.Add(new SangKienLichSu
        {
            SangKienId = hoSo.Id,
            HanhDong = "THU_HOI",
            GhiChu = lyDo,
            NguoiThucHienId = nguoiDungId,
            ThoiGian = _dongHo.BayGio,
            DiaChiIp = _nguoiDungHienTai.DiaChiIp,
            UserAgent = _nguoiDungHienTai.UserAgent
        });

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<WorkflowInstance?> LayTrangThaiAsync(Guid sangKienId, CancellationToken ct)
    {
        var hoSo = await _db.SangKien.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == sangKienId, ct)
            .ConfigureAwait(false);

        if (hoSo is null)
        {
            return null;
        }

        var quyTrinh = _snapshot.DocSnapshot(hoSo.QuyTrinhSnapshot);
        return quyTrinh is null
            ? null
            : await TaoWorkflowInstanceAsync(hoSo, quyTrinh, ct).ConfigureAwait(false);
    }

    // ------------------------------------------------------------------------------------

    /// <summary>Nap toan bo du lieu can thiet cho engine (ho so + quy trinh snapshot + nguoi xu ly).</summary>
    internal async Task<NguCanhThucThi?> TaoNguCanhAsync(
        Guid sangKienId, Guid nguoiDungId, CancellationToken ct)
    {
        var hoSo = await _db.SangKien
            .Include(x => x.DanhSachTacGia)
            .FirstOrDefaultAsync(x => x.Id == sangKienId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("hồ sơ sáng kiến", sangKienId);

        var quyTrinh = _snapshot.DocSnapshot(hoSo.QuyTrinhSnapshot);
        if (quyTrinh is null)
        {
            return null;
        }

        var lichSu = await _db.SangKienXuLy
            .Where(x => x.SangKienId == sangKienId)
            .OrderBy(x => x.ThuTu)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var nguoiXuLy = await TaoNguCanhNguoiXuLyAsync(nguoiDungId, hoSo, ct).ConfigureAwait(false);
        var soTacNhan = await DemTacNhanDuKienAsync(hoSo, quyTrinh, ct).ConfigureAwait(false);
        var bienBoSung = await TaoBienBoSungAsync(hoSo, ct).ConfigureAwait(false);

        return new NguCanhThucThi
        {
            HoSo = hoSo,
            QuyTrinh = quyTrinh,
            NguoiXuLy = nguoiXuLy,
            LichSuXuLy = lichSu,
            SoTacNhanDuKien = soTacNhan,
            BienBoSung = bienBoSung,
            ThoiDiem = _dongHo.BayGio
        };
    }

    private async Task<NguCanhNguoiXuLy> TaoNguCanhNguoiXuLyAsync(
        Guid nguoiDungId, HoSoSangKien hoSo, CancellationToken ct)
    {
        var nguoiDung = await _db.NguoiDung.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == nguoiDungId, ct)
            .ConfigureAwait(false);

        var homNay = _dongHo.HomNay;

        var vaiTroIds = await _db.NguoiDungVaiTro.AsNoTracking()
            .Where(x => x.NguoiDungId == nguoiDungId
                        && (x.TuNgay == null || x.TuNgay <= homNay)
                        && (x.DenNgay == null || x.DenNgay >= homNay))
            .Select(x => x.VaiTroId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var maVaiTro = await _db.VaiTro.AsNoTracking()
            .Where(x => vaiTroIds.Contains(x.Id))
            .Select(x => x.Ma)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var chucDanhHoiDong = await _db.HoiDongThanhVien.AsNoTracking()
            .Where(x => x.NguoiDungId == nguoiDungId && x.TrangThai == TrangThaiDanhMuc.HoatDong)
            .Select(x => new { x.HoiDongId, x.ChucDanh })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var donViTrongPhamVi = new HashSet<Guid>();
        if (nguoiDung?.DonViId is not null)
        {
            var donVi = await _db.DonVi.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == nguoiDung.DonViId.Value, ct)
                .ConfigureAwait(false);

            if (donVi is not null)
            {
                var ids = await _db.DonVi.AsNoTracking()
                    .Where(x => x.Id == donVi.Id || x.Path.StartsWith(donVi.Path))
                    .Select(x => x.Id)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                foreach (var id in ids)
                {
                    donViTrongPhamVi.Add(id);
                }
            }
        }

        var laLanhDao = maVaiTro.Contains(MaVaiTro.LanhDaoPheDuyet)
                        && hoSo.DonViId.HasValue
                        && donViTrongPhamVi.Contains(hoSo.DonViId.Value);

        return new NguCanhNguoiXuLy
        {
            NguoiDungId = nguoiDungId,
            DonViId = nguoiDung?.DonViId,
            MaVaiTro = maVaiTro.ToHashSet(),
            VaiTroIds = vaiTroIds.ToHashSet(),
            DonViTrongPhamVi = donViTrongPhamVi,
            ChucDanhTheoHoiDong = chucDanhHoiDong
                .GroupBy(x => x.HoiDongId)
                .ToDictionary(g => g.Key, g => g.First().ChucDanh),
            LaNguoiTaoHoSo = hoSo.NguoiTaoId == nguoiDungId
                             || hoSo.DanhSachTacGia.Any(t => t.NguoiDungId == nguoiDungId),
            LaLanhDaoDonViTacGia = laLanhDao
        };
    }

    /// <summary>Dem so tac nhan du kien phai xu ly buoc hien tai (cho quy tac TAT_CA / DA_SO).</summary>
    private async Task<int> DemTacNhanDuKienAsync(
        HoSoSangKien hoSo, QuyTrinh quyTrinh, CancellationToken ct)
    {
        if (hoSo.BuocHienTaiId is null)
        {
            return 1;
        }

        var buoc = quyTrinh.DanhSachBuoc.FirstOrDefault(b => b.Id == hoSo.BuocHienTaiId.Value);
        if (buoc is null)
        {
            return 1;
        }

        // Buoc cham diem: so tac nhan = so thanh vien duoc phan cong cham ho so nay.
        if (buoc.LoaiBuoc == LoaiBuoc.ChamDiem)
        {
            var soPhanCong = await _db.SangKienPhanCong
                .CountAsync(x => x.SangKienId == hoSo.Id, ct)
                .ConfigureAwait(false);

            if (soPhanCong > 0)
            {
                return soPhanCong;
            }
        }

        var tong = 0;
        foreach (var tn in buoc.TacNhan)
        {
            tong += tn.LoaiTacNhan switch
            {
                LoaiTacNhan.NguoiDung => 1,
                LoaiTacNhan.NguoiTaoHoSo => 1,
                LoaiTacNhan.HoiDong when tn.ThamChieuId.HasValue =>
                    await _db.HoiDongThanhVien
                        .CountAsync(x => x.HoiDongId == tn.ThamChieuId.Value
                                         && x.QuyenChamDiem
                                         && x.TrangThai == TrangThaiDanhMuc.HoatDong, ct)
                        .ConfigureAwait(false),
                LoaiTacNhan.VaiTro when !string.IsNullOrEmpty(tn.ThamChieuMa) =>
                    await DemNguoiTheoVaiTroAsync(tn.ThamChieuMa, ct).ConfigureAwait(false),
                _ => 1
            };
        }

        return Math.Max(tong, 1);
    }

    private async Task<int> DemNguoiTheoVaiTroAsync(string maVaiTro, CancellationToken ct)
    {
        var vaiTroId = await _db.VaiTro.AsNoTracking()
            .Where(x => x.Ma == maVaiTro)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (vaiTroId is null)
        {
            return 1;
        }

        var so = await _db.NguoiDungVaiTro.AsNoTracking()
            .CountAsync(x => x.VaiTroId == vaiTroId.Value, ct)
            .ConfigureAwait(false);

        return Math.Max(so, 1);
    }

    /// <summary>Bien bo sung cho bo danh gia dieu kien (so phieu, diem tong hop...).</summary>
    private async Task<IReadOnlyDictionary<string, object?>> TaoBienBoSungAsync(
        HoSoSangKien hoSo, CancellationToken ct)
    {
        var ketQua = await _db.KetQuaXetDuyet.AsNoTracking()
            .Where(x => x.SangKienId == hoSo.Id)
            .OrderByDescending(x => x.NgayTao)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var soPhieuCham = await _db.PhieuDanhGia.AsNoTracking()
            .CountAsync(x => x.SangKienId == hoSo.Id
                             && (x.TrangThaiPhieu == TrangThaiPhieuDanhGia.DaGui
                                 || x.TrangThaiPhieu == TrangThaiPhieuDanhGia.DaKy), ct)
            .ConfigureAwait(false);

        return new Dictionary<string, object?>
        {
            ["so_phieu_cham"] = soPhieuCham,
            ["so_phieu_dong_y"] = ketQua?.SoPhieuDongY ?? 0,
            ["so_phieu_khong_dong_y"] = ketQua?.SoPhieuKhongDongY ?? 0,
            ["ty_le_dong_thuan"] = ketQua is null || ketQua.SoPhieuDongY + ketQua.SoPhieuKhongDongY == 0
                ? 0m
                : Math.Round(
                    ketQua.SoPhieuDongY * 100m / (ketQua.SoPhieuDongY + ketQua.SoPhieuKhongDongY), 2)
        };
    }

    private async Task<QuyTrinh> NapQuyTrinhTuDbAsync(Guid quyTrinhId, CancellationToken ct)
    {
        var quyTrinh = await _db.QuyTrinh.AsNoTracking()
            .Include(q => q.DanhSachBuoc).ThenInclude(b => b.TacNhan)
            .Include(q => q.DanhSachBuoc).ThenInclude(b => b.TruongHop)
            .Include(q => q.DanhSachBuoc).ThenInclude(b => b.TrangThai)
            .Include(q => q.ThanhPhanHoSo)
            .Include(q => q.ChucNangBoSung)
            .Include(q => q.TrangThaiToanCuc)
            .FirstOrDefaultAsync(q => q.Id == quyTrinhId, ct)
            .ConfigureAwait(false);

        return quyTrinh ?? throw new KhongTimThayException("quy trình", quyTrinhId);
    }

    private async Task<WorkflowInstance> TaoWorkflowInstanceAsync(
        HoSoSangKien hoSo, QuyTrinh quyTrinh, CancellationToken ct)
    {
        var lichSu = await _db.SangKienXuLy.AsNoTracking()
            .Where(x => x.SangKienId == hoSo.Id)
            .OrderBy(x => x.ThuTu)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var buoc = quyTrinh.DanhSachBuoc.FirstOrDefault(b => b.Id == hoSo.BuocHienTaiId);
        var trangThai = quyTrinh.DanhSachBuoc
            .SelectMany(b => b.TrangThai)
            .Concat(quyTrinh.TrangThaiToanCuc)
            .FirstOrDefault(t => t.Id == hoSo.TrangThaiHienTaiId);

        return new WorkflowInstance
        {
            SangKienId = hoSo.Id,
            QuyTrinhId = quyTrinh.Id,
            PhienBanQuyTrinh = quyTrinh.PhienBan,
            BuocHienTaiId = hoSo.BuocHienTaiId,
            TenBuocHienTai = buoc?.Ten,
            TrangThaiHienTaiId = hoSo.TrangThaiHienTaiId,
            TenTrangThaiHienTai = trangThai?.Ten,
            HanXuLy = hoSo.HanXuLyHienTai,
            DaKetThuc = hoSo.BuocHienTaiId is null,
            TrangThaiTong = hoSo.TrangThaiTong,
            LichSu = lichSu
        };
    }
}
