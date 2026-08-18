using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.SangKien;
using BlueIdea.Shared.KetQua;
using BlueIdea.Shared.TiengViet;
using BlueIdea.Workflow;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.SangKien;

/// <summary>
/// Truy van doc (read model) cho ho so sang kien: danh sach da bo loc, chi tiet,
/// timeline tien do, checklist thanh phan.
/// </summary>
public sealed class DichVuTruyVanSangKien
{
    private readonly IAppDbContext _db;
    private readonly INguoiDungHienTai _nguoiDung;
    private readonly IDichVuPhanQuyen _phanQuyen;
    private readonly IDongHoHeThong _dongHo;
    private readonly IBoChuyenDoiSnapshotQuyTrinh _snapshot;

    public DichVuTruyVanSangKien(
        IAppDbContext db, INguoiDungHienTai nguoiDung, IDichVuPhanQuyen phanQuyen,
        IDongHoHeThong dongHo, IBoChuyenDoiSnapshotQuyTrinh snapshot)
    {
        _db = db;
        _nguoiDung = nguoiDung;
        _phanQuyen = phanQuyen;
        _dongHo = dongHo;
        _snapshot = snapshot;
    }

    /// <summary>Chuc nang 28 - Danh sach ho so voi bo loc da tieu chi + pham vi du lieu.</summary>
    public async Task<PagedResult<SangKienTomTatDto>> LayDanhSachAsync(
        ThamSoLocSangKien thamSo, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.SangKienXem, ct: ct).ConfigureAwait(false);

        var truyVan = _db.SangKien.AsNoTracking();
        truyVan = await ApDungPhamViDuLieuAsync(truyVan, thamSo, ct).ConfigureAwait(false);
        truyVan = ApDungBoLoc(truyVan, thamSo);

        var tongSo = await truyVan.CountAsync(ct).ConfigureAwait(false);

        var bayGio = _dongHo.BayGio;

        var duLieu = await ApDungSapXep(truyVan, thamSo)
            .Skip(thamSo.BoQua)
            .Take(thamSo.SoDong)
            .Select(x => new
            {
                x.Id,
                x.MaHoSo,
                x.TenSangKien,
                x.LinhVucId,
                x.DonViId,
                x.DotDeNghiId,
                x.TrangThaiTong,
                x.BuocHienTaiId,
                x.TrangThaiHienTaiId,
                x.TongDiem,
                x.TyLeTrungLap,
                x.KetQua,
                x.NgayNop,
                x.HanXuLyHienTai,
                x.PhienBan,
                TacGiaChinh = x.DanhSachTacGia
                    .Where(t => t.LaTacGiaChinh)
                    .Select(t => t.HoTen)
                    .FirstOrDefault()
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var linhVucIds = duLieu.Select(x => x.LinhVucId).Distinct().ToList();
        var donViIds = duLieu.Where(x => x.DonViId.HasValue).Select(x => x.DonViId!.Value).Distinct().ToList();
        var dotIds = duLieu.Select(x => x.DotDeNghiId).Distinct().ToList();
        var buocIds = duLieu.Where(x => x.BuocHienTaiId.HasValue)
            .Select(x => x.BuocHienTaiId!.Value).Distinct().ToList();
        var trangThaiIds = duLieu.Where(x => x.TrangThaiHienTaiId.HasValue)
            .Select(x => x.TrangThaiHienTaiId!.Value).Distinct().ToList();

        var tenLinhVuc = await _db.LinhVuc.AsNoTracking()
            .Where(x => linhVucIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct).ConfigureAwait(false);

        var tenDonVi = await _db.DonVi.AsNoTracking()
            .Where(x => donViIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct).ConfigureAwait(false);

        var tenDot = await _db.DotDeNghi.AsNoTracking()
            .Where(x => dotIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct).ConfigureAwait(false);

        var tenBuoc = await _db.QuyTrinhBuoc.AsNoTracking()
            .Where(x => buocIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct).ConfigureAwait(false);

        var tenTrangThai = await _db.QuyTrinhTrangThai.AsNoTracking()
            .Where(x => trangThaiIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct).ConfigureAwait(false);

        var ketQua = duLieu.Select(x => new SangKienTomTatDto(
            x.Id,
            x.MaHoSo,
            x.TenSangKien,
            tenLinhVuc.GetValueOrDefault(x.LinhVucId),
            x.DonViId.HasValue ? tenDonVi.GetValueOrDefault(x.DonViId.Value) : null,
            tenDot.GetValueOrDefault(x.DotDeNghiId),
            x.TrangThaiTong,
            x.BuocHienTaiId.HasValue ? tenBuoc.GetValueOrDefault(x.BuocHienTaiId.Value) : null,
            x.TrangThaiHienTaiId.HasValue ? tenTrangThai.GetValueOrDefault(x.TrangThaiHienTaiId.Value) : null,
            x.TongDiem,
            x.TyLeTrungLap,
            x.KetQua,
            x.NgayNop,
            x.HanXuLyHienTai,
            x.HanXuLyHienTai.HasValue && x.HanXuLyHienTai.Value < bayGio,
            x.TacGiaChinh ?? string.Empty,
            x.PhienBan)).ToList();

        return new PagedResult<SangKienTomTatDto>(ketQua, tongSo, thamSo.Trang, thamSo.SoDong);
    }

    public async Task<SangKienChiTietDto> LayChiTietAsync(Guid id, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.SangKienXem, ct: ct).ConfigureAwait(false);

        var hoSo = await _db.SangKien.AsNoTracking()
            .Include(x => x.DanhSachTacGia)
            .Include(x => x.TepDinhKem).ThenInclude(t => t.TepTin)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("hồ sơ sáng kiến", id);

        await BatBuocTrongPhamViAsync(hoSo, ct).ConfigureAwait(false);

        var dto = new SangKienChiTietDto
        {
            Id = hoSo.Id,
            MaHoSo = hoSo.MaHoSo,
            TenSangKien = hoSo.TenSangKien,
            DotDeNghiId = hoSo.DotDeNghiId,
            LinhVucId = hoSo.LinhVucId,
            DoiTuongId = hoSo.DoiTuongId,
            LoaiTacGiaId = hoSo.LoaiTacGiaId,
            DonViId = hoSo.DonViId,
            TrangThaiTong = hoSo.TrangThaiTong,
            BuocHienTaiId = hoSo.BuocHienTaiId,
            TrangThaiHienTaiId = hoSo.TrangThaiHienTaiId,
            MoTaGiaiPhap = hoSo.MoTaGiaiPhap,
            TinhTrangTruocKhiApDung = hoSo.TinhTrangTruocKhiApDung,
            NoiDungGiaiPhap = hoSo.NoiDungGiaiPhap,
            TinhMoi = hoSo.TinhMoi,
            KhaNangApDung = hoSo.KhaNangApDung,
            PhamViApDung = hoSo.PhamViApDung,
            HieuQuaKinhTe = hoSo.HieuQuaKinhTe,
            GiaTriLamLoiUocTinh = hoSo.GiaTriLamLoiUocTinh,
            HieuQuaXaHoi = hoSo.HieuQuaXaHoi,
            ThoiGianApDungTu = hoSo.ThoiGianApDungTu,
            ThoiGianApDungDen = hoSo.ThoiGianApDungDen,
            NoiDungDong = hoSo.NoiDungDong,
            TyLeTrungLap = hoSo.TyLeTrungLap,
            TrangThaiKiemTraTrungLap = hoSo.TrangThaiKiemTraTrungLap,
            TongDiem = hoSo.TongDiem,
            DiemTrungBinh = hoSo.DiemTrungBinh,
            KetQua = hoSo.KetQua,
            MucCongNhanId = hoSo.MucCongNhanId,
            NgayCongNhan = hoSo.NgayCongNhan,
            NgayNop = hoSo.NgayNop,
            HanXuLyHienTai = hoSo.HanXuLyHienTai,
            DangKhoa = hoSo.DangKhoa,
            LyDoKhoa = hoSo.LyDoKhoa,
            CongKhai = hoSo.CongKhai,
            PhienBan = hoSo.PhienBan,
            ChoPhepSua = hoSo.ChoPhepSua(),
            ChoPhepRut = hoSo.ChoPhepRut(),
            DanhSachTacGia = hoSo.DanhSachTacGia
                .OrderBy(t => t.ThuTu)
                .Select(t => new TacGiaDto
                {
                    Id = t.Id,
                    NguoiDungId = t.NguoiDungId,
                    HoTen = t.HoTen,
                    NgaySinh = t.NgaySinh,
                    GioiTinh = t.GioiTinh,
                    ChucVu = t.ChucVu,
                    DonViCongTac = t.DonViCongTac,
                    TrinhDoChuyenMon = t.TrinhDoChuyenMon,
                    Email = t.Email,
                    DienThoai = t.DienThoai,
                    TyLeDongGop = t.TyLeDongGop,
                    LaTacGiaChinh = t.LaTacGiaChinh,
                    ThuTu = t.ThuTu
                }).ToList(),
            TepDinhKem = hoSo.TepDinhKem
                .Where(t => t.TepTin is not null)
                .OrderBy(t => t.ThuTu)
                .Select(t => new TepDinhKemDto(
                    t.Id, t.TepTinId, t.TepTin!.TenGoc, t.TepTin.KichThuoc,
                    t.TepTin.MimeType, t.ThanhPhanHoSoMa, t.MoTa, t.TepTin.NgayTaiLen))
                .ToList()
        };

        // Ten hien thi cua cac khoa ngoai.
        dto.TenLinhVuc = await _db.LinhVuc.AsNoTracking()
            .Where(x => x.Id == hoSo.LinhVucId).Select(x => x.Ten)
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);

        dto.TenDot = await _db.DotDeNghi.AsNoTracking()
            .Where(x => x.Id == hoSo.DotDeNghiId).Select(x => x.Ten)
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);

        if (hoSo.DoiTuongId.HasValue)
        {
            dto.TenDoiTuong = await _db.DoiTuong.AsNoTracking()
                .Where(x => x.Id == hoSo.DoiTuongId.Value).Select(x => x.Ten)
                .FirstOrDefaultAsync(ct).ConfigureAwait(false);
        }

        if (hoSo.DonViId.HasValue)
        {
            dto.TenDonVi = await _db.DonVi.AsNoTracking()
                .Where(x => x.Id == hoSo.DonViId.Value).Select(x => x.Ten)
                .FirstOrDefaultAsync(ct).ConfigureAwait(false);
        }

        if (hoSo.MucCongNhanId.HasValue)
        {
            dto.TenMucCongNhan = await _db.MucCongNhan.AsNoTracking()
                .Where(x => x.Id == hoSo.MucCongNhanId.Value).Select(x => x.Ten)
                .FirstOrDefaultAsync(ct).ConfigureAwait(false);
        }

        // Buoc / trang thai hien tai lay tu snapshot de dung voi phien ban quy trinh cua ho so.
        var quyTrinh = _snapshot.DocSnapshot(hoSo.QuyTrinhSnapshot);
        if (quyTrinh is not null)
        {
            var buoc = quyTrinh.DanhSachBuoc.FirstOrDefault(b => b.Id == hoSo.BuocHienTaiId);
            dto.TenBuocHienTai = buoc?.Ten;

            var trangThai = quyTrinh.DanhSachBuoc
                .SelectMany(b => b.TrangThai)
                .Concat(quyTrinh.TrangThaiToanCuc)
                .FirstOrDefault(t => t.Id == hoSo.TrangThaiHienTaiId);

            dto.TenTrangThaiHienTai = trangThai?.Ten;
            dto.MauTrangThai = trangThai?.MauSac;

            dto.ThanhPhanHoSo = BoKiemTraThanhPhanHoSo
                .LapChecklist(quyTrinh.ThanhPhanHoSo.ToList(), hoSo, hoSo.TepDinhKem.ToList())
                .ToList();
        }
        else
        {
            // Ho so con o trang thai nhap - lay thanh phan tu quy trinh cua dot.
            var quyTrinhId = await _db.DotDeNghi.AsNoTracking()
                .Where(x => x.Id == hoSo.DotDeNghiId)
                .Select(x => x.QuyTrinhId)
                .FirstOrDefaultAsync(ct).ConfigureAwait(false);

            if (quyTrinhId.HasValue)
            {
                var thanhPhan = await _db.QuyTrinhThanhPhanHoSo.AsNoTracking()
                    .Where(x => x.QuyTrinhId == quyTrinhId.Value)
                    .OrderBy(x => x.ThuTu)
                    .ToListAsync(ct).ConfigureAwait(false);

                dto.ThanhPhanHoSo = BoKiemTraThanhPhanHoSo
                    .LapChecklist(thanhPhan, hoSo, hoSo.TepDinhKem.ToList())
                    .ToList();
            }
        }

        return dto;
    }

    /// <summary>Chuc nang 30 - Timeline tien do xu ly cua ho so.</summary>
    public async Task<IReadOnlyList<MocTienDoDto>> LayTienDoAsync(Guid id, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.SangKienXem, ct: ct).ConfigureAwait(false);
        await BatBuocTrongPhamViSangKienAsync(id, ct).ConfigureAwait(false);

        var buocs = await _db.SangKienXuLy.AsNoTracking()
            .Where(x => x.SangKienId == id)
            .OrderBy(x => x.ThuTu).ThenBy(x => x.ThoiGianNhan)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var nguoiIds = buocs.Where(x => x.NguoiXuLyId.HasValue)
            .Select(x => x.NguoiXuLyId!.Value).Distinct().ToList();

        var tenNguoi = await _db.NguoiDung.AsNoTracking()
            .Where(x => nguoiIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.HoTen, ct)
            .ConfigureAwait(false);

        var tepIds = buocs.SelectMany(x => x.TepDinhKemIds).Distinct().ToList();
        var teps = await _db.TepTin.AsNoTracking()
            .Where(x => tepIds.Contains(x.Id))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var trangThaiIds = buocs.Where(x => x.TrangThaiId.HasValue)
            .Select(x => x.TrangThaiId!.Value).Distinct().ToList();

        var tenTrangThai = await _db.QuyTrinhTrangThai.AsNoTracking()
            .Where(x => trangThaiIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct)
            .ConfigureAwait(false);

        return buocs.Select(x => new MocTienDoDto(
            x.Id,
            x.BuocId,
            x.TenBuocSnapshot,
            x.TrangThaiId.HasValue ? tenTrangThai.GetValueOrDefault(x.TrangThaiId.Value) : null,
            x.TenTruongHopSnapshot,
            x.NguoiXuLyId.HasValue ? tenNguoi.GetValueOrDefault(x.NguoiXuLyId.Value) : null,
            x.YKien,
            x.ThoiGianNhan,
            x.HanXuLy,
            x.ThoiGianXuLy,
            x.SoNgayXuLy,
            x.QuaHan,
            teps.Where(t => x.TepDinhKemIds.Contains(t.Id))
                .Select(t => new TepDinhKemDto(t.Id, t.Id, t.TenGoc, t.KichThuoc,
                    t.MimeType, string.Empty, null, t.NgayTaiLen))
                .ToList())).ToList();
    }

    /// <summary>Chuc nang 23 - Lich su chinh sua (diff truoc/sau).</summary>
    public async Task<IReadOnlyList<SangKienLichSu>> LayLichSuAsync(Guid id, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.SangKienXem, ct: ct).ConfigureAwait(false);
        await BatBuocTrongPhamViSangKienAsync(id, ct).ConfigureAwait(false);

        return await _db.SangKienLichSu.AsNoTracking()
            .Where(x => x.SangKienId == id)
            .OrderByDescending(x => x.ThoiGian)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    // ------------------------------------------------------------------------------------

    private async Task BatBuocTrongPhamViAsync(HoSoSangKien hoSo, CancellationToken ct)
    {
        if (_nguoiDung.Id is null)
            throw new KhongTimThayException("hồ sơ sáng kiến", hoSo.Id);

        var nguoiDungId = _nguoiDung.Id.Value;
        var phamVi = await _phanQuyen.LayPhamViTruyCapAsync(nguoiDungId, ct).ConfigureAwait(false);

        if (phamVi.ToanHeThong) return;

        var laTacGia = hoSo.NguoiTaoId == nguoiDungId
                       || (hoSo.DanhSachTacGia?.Any(t => t.NguoiDungId == nguoiDungId) == true);

        if (phamVi.ChiCaNhan)
        {
            if (!laTacGia) throw new KhongTimThayException("hồ sơ sáng kiến", hoSo.Id);
            return;
        }

        var trongDonVi = hoSo.DonViId.HasValue && phamVi.DonViIds.Contains(hoSo.DonViId.Value);
        if (!laTacGia && !trongDonVi)
            throw new KhongTimThayException("hồ sơ sáng kiến", hoSo.Id);
    }

    private async Task BatBuocTrongPhamViSangKienAsync(Guid sangKienId, CancellationToken ct)
    {
        var hoSo = await _db.SangKien.AsNoTracking()
            .Include(x => x.DanhSachTacGia)
            .FirstOrDefaultAsync(x => x.Id == sangKienId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("hồ sơ sáng kiến", sangKienId);

        await BatBuocTrongPhamViAsync(hoSo, ct).ConfigureAwait(false);
    }

    /// <summary>Ap dung pham vi du lieu theo vai tro (Muc 6 dac ta) - chong IDOR o muc truy van.</summary>
    private async Task<IQueryable<HoSoSangKien>> ApDungPhamViDuLieuAsync(
        IQueryable<HoSoSangKien> truyVan, ThamSoLocSangKien thamSo, CancellationToken ct)
    {
        if (thamSo.ChiCongKhai == true)
        {
            return truyVan.Where(x => x.CongKhai && x.KetQua == KetQuaXetDuyetGiaTri.Dat);
        }

        if (_nguoiDung.Id is null)
        {
            return truyVan.Where(x => x.CongKhai && x.KetQua == KetQuaXetDuyetGiaTri.Dat);
        }

        var nguoiDungId = _nguoiDung.Id.Value;

        if (thamSo.ChiCuaToi == true)
        {
            return truyVan.Where(x => x.NguoiTaoId == nguoiDungId
                                      || x.DanhSachTacGia.Any(t => t.NguoiDungId == nguoiDungId));
        }

        var phamVi = await _phanQuyen.LayPhamViTruyCapAsync(nguoiDungId, ct).ConfigureAwait(false);

        if (phamVi.ToanHeThong)
        {
            return truyVan;
        }

        if (phamVi.ChiCaNhan)
        {
            return truyVan.Where(x => x.NguoiTaoId == nguoiDungId
                                      || x.DanhSachTacGia.Any(t => t.NguoiDungId == nguoiDungId));
        }

        var donViIds = phamVi.DonViIds.ToList();
        return truyVan.Where(x =>
            (x.DonViId.HasValue && donViIds.Contains(x.DonViId.Value))
            || x.NguoiTaoId == nguoiDungId
            || x.DanhSachTacGia.Any(t => t.NguoiDungId == nguoiDungId));
    }

    /// <summary>
    /// Chuc nang 37 — Goi y tu khoa khi go o o tim kiem.
    ///
    /// Chay TREN TAP DU LIEU NGUOI DUNG DUOC XEM (dung dung bo loc pham vi nhu danh sach): goi y
    /// khong duoc lo ten sang kien cua don vi ma nguoi do khong co quyen xem.
    /// </summary>
    public async Task<IReadOnlyList<GoiYTimKiem>> GoiYAsync(
        string tuKhoa, int soLuong = 8, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tuKhoa) || tuKhoa.Trim().Length < 2)
        {
            return Array.Empty<GoiYTimKiem>();
        }

        var khongDau = VanBanTiengViet.TaoKhongDau(tuKhoa);
        var hoa = tuKhoa.Trim().ToUpperInvariant();
        soLuong = Math.Clamp(soLuong, 1, 20);

        var trongPhamVi = await ApDungPhamViDuLieuAsync(
                _db.SangKien.AsNoTracking(), new ThamSoLocSangKien(), ct)
            .ConfigureAwait(false);

        var theoTen = await trongPhamVi
            .Where(x => x.TenKhongDau.Contains(khongDau))
            .OrderBy(x => x.TenSangKien.Length)
            .Take(soLuong)
            .Select(x => new GoiYTimKiem(x.TenSangKien, "SANG_KIEN", x.MaHoSo))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var theoMa = await trongPhamVi
            .Where(x => x.MaHoSo.ToUpper().Contains(hoa))
            .OrderBy(x => x.MaHoSo)
            .Take(soLuong)
            .Select(x => new GoiYTimKiem(x.MaHoSo, "MA_HO_SO", x.TenSangKien))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var theoTacGia = await _db.SangKienTacGia.AsNoTracking()
            .Where(t => t.HoTen.ToUpper().Contains(hoa))
            .Where(t => trongPhamVi.Any(h => h.Id == t.SangKienId))
            .Select(t => t.HoTen)
            .Distinct()
            .OrderBy(x => x)
            .Take(soLuong)
            .Select(x => new GoiYTimKiem(x, "TAC_GIA", null))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return theoMa.Concat(theoTen).Concat(theoTacGia)
            .DistinctBy(x => (x.Loai, x.GiaTri))
            .Take(soLuong * 2)
            .ToList();
    }

    private IQueryable<HoSoSangKien> ApDungBoLoc(
        IQueryable<HoSoSangKien> truyVan, ThamSoLocSangKien t)
    {
        if (!string.IsNullOrWhiteSpace(t.TuKhoa))
        {
            var khongDau = VanBanTiengViet.TaoKhongDau(t.TuKhoa);
            var hoa = t.TuKhoa.Trim().ToUpperInvariant();
            truyVan = truyVan.Where(x =>
                x.TenKhongDau.Contains(khongDau)
                || x.MaHoSo.ToUpper().Contains(hoa)
                || x.DanhSachTacGia.Any(tg => tg.HoTen.ToUpper().Contains(hoa)));
        }

        if (t.DotDeNghiId.HasValue)
        {
            truyVan = truyVan.Where(x => x.DotDeNghiId == t.DotDeNghiId.Value);
        }

        // Nam nam tren DOT DE NGHI chu khong tren ho so nen phai loc qua bang dot. Truoc day
        // tham so nay duoc khai bao nhung khong ai dung, nguoi dung loc theo nam thi ket qua
        // giu nguyen — im lang va rat kho phat hien.
        if (t.Nam.HasValue)
        {
            truyVan = truyVan.Where(x =>
                _db.DotDeNghi.Any(d => d.Id == x.DotDeNghiId && d.Nam == t.Nam!.Value));
        }

        if (t.LinhVucId.HasValue)
        {
            truyVan = truyVan.Where(x => x.LinhVucId == t.LinhVucId.Value);
        }

        if (t.DonViId.HasValue)
        {
            truyVan = truyVan.Where(x => x.DonViId == t.DonViId.Value);
        }

        if (!string.IsNullOrWhiteSpace(t.TrangThaiTong))
        {
            truyVan = truyVan.Where(x => x.TrangThaiTong == t.TrangThaiTong);
        }

        if (t.BuocHienTaiId.HasValue)
        {
            truyVan = truyVan.Where(x => x.BuocHienTaiId == t.BuocHienTaiId.Value);
        }

        if (!string.IsNullOrWhiteSpace(t.KetQua))
        {
            truyVan = truyVan.Where(x => x.KetQua == t.KetQua);
        }

        if (t.NgayNopTu.HasValue)
        {
            var tu = new DateTimeOffset(t.NgayNopTu.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            truyVan = truyVan.Where(x => x.NgayNop >= tu);
        }

        if (t.NgayNopDen.HasValue)
        {
            var den = new DateTimeOffset(t.NgayNopDen.Value.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
            truyVan = truyVan.Where(x => x.NgayNop <= den);
        }

        if (t.DiemTu.HasValue)
        {
            truyVan = truyVan.Where(x => x.TongDiem >= t.DiemTu.Value);
        }

        if (t.DiemDen.HasValue)
        {
            truyVan = truyVan.Where(x => x.TongDiem <= t.DiemDen.Value);
        }

        if (t.TrungLapTu.HasValue)
        {
            truyVan = truyVan.Where(x => x.TyLeTrungLap >= t.TrungLapTu.Value);
        }

        if (t.TrungLapDen.HasValue)
        {
            truyVan = truyVan.Where(x => x.TyLeTrungLap <= t.TrungLapDen.Value);
        }

        if (t.ChiQuaHan == true)
        {
            var bayGio = _dongHo.BayGio;
            truyVan = truyVan.Where(x => x.HanXuLyHienTai != null && x.HanXuLyHienTai < bayGio);
        }

        if (t.HoiDongId.HasValue)
        {
            var hoiDongId = t.HoiDongId.Value;
            truyVan = truyVan.Where(x =>
                _db.SangKienPhanCong.Any(pc => pc.SangKienId == x.Id && pc.HoiDongId == hoiDongId));
        }

        return truyVan;
    }

    /// <summary>
    /// Sap xep danh sach ho so theo cot nguoi dung chon.
    ///
    /// Cot co the null (diem, ty le trung lap, han xu ly) LUON day o cuoi du sap xep chieu nao:
    /// PostgreSQL mac dinh dat NULL len dau khi DESC, nen "sap xep diem giam dan" se do mot loat
    /// ho so CHUA CHAM len dau bang — dung cai nguoi dung muon xem nhat bi day xuong duoi.
    /// </summary>
    private static IQueryable<HoSoSangKien> ApDungSapXep(
        IQueryable<HoSoSangKien> truyVan, ThamSoPhanTrang thamSo)
    {
        var giam = thamSo.GiamDan;

        return (thamSo.SapXep ?? string.Empty).ToLowerInvariant() switch
        {
            "mahoso" => giam
                ? truyVan.OrderByDescending(x => x.MaHoSo)
                : truyVan.OrderBy(x => x.MaHoSo),

            "tensangkien" => giam
                ? truyVan.OrderByDescending(x => x.TenSangKien)
                : truyVan.OrderBy(x => x.TenSangKien),

            "tongdiem" => giam
                ? truyVan.OrderBy(x => x.TongDiem == null).ThenByDescending(x => x.TongDiem)
                : truyVan.OrderBy(x => x.TongDiem == null).ThenBy(x => x.TongDiem),

            "tyletrunglap" => giam
                ? truyVan.OrderBy(x => x.TyLeTrungLap == null).ThenByDescending(x => x.TyLeTrungLap)
                : truyVan.OrderBy(x => x.TyLeTrungLap == null).ThenBy(x => x.TyLeTrungLap),

            "hanxuly" => giam
                ? truyVan.OrderBy(x => x.HanXuLyHienTai == null)
                    .ThenByDescending(x => x.HanXuLyHienTai)
                : truyVan.OrderBy(x => x.HanXuLyHienTai == null)
                    .ThenBy(x => x.HanXuLyHienTai),

            // Khong chon cot: moi nhat truoc. Truoc day hai nhanh nay bi dao nguoc nen truyen
            // huong=desc ma khong kem ten cot lai ra ho so CU NHAT dau bang.
            _ => giam
                ? truyVan.OrderBy(x => x.NgayTao)
                : truyVan.OrderByDescending(x => x.NgayTao),
        };
    }
}
