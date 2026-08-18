using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.HoiDong;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Domain.SangKien;
using BlueIdea.Domain.TieuChi;
using BlueIdea.Scoring;
using BlueIdea.Shared.KetQua;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.DanhGia;

/// <summary>
/// Nhom chuc nang 33-35: phan cong cham, cham diem, gui phieu, tong hop diem hoi dong.
/// </summary>
public sealed class DichVuDanhGia
{
    private readonly IAppDbContext _db;
    private readonly INguoiDungHienTai _nguoiDung;
    private readonly IDichVuPhanQuyen _phanQuyen;
    private readonly IDongHoHeThong _dongHo;
    private readonly IBoTinhDiem _tinhDiem;
    private readonly IDichVuThongBao _thongBao;

    public DichVuDanhGia(
        IAppDbContext db, INguoiDungHienTai nguoiDung, IDichVuPhanQuyen phanQuyen,
        IDongHoHeThong dongHo, IBoTinhDiem tinhDiem, IDichVuThongBao thongBao)
    {
        _db = db;
        _nguoiDung = nguoiDung;
        _phanQuyen = phanQuyen;
        _dongHo = dongHo;
        _tinhDiem = tinhDiem;
        _thongBao = thongBao;
    }

    // ------------------------------------------------------------------------------------
    // Phan cong cham (chuc nang 33)
    // ------------------------------------------------------------------------------------

    /// <summary>
    /// Phan cong thanh vien hoi dong cham ho so.
    /// Tu dong loai tru xung dot loi ich: thanh vien la tac gia cua chinh ho so do.
    /// </summary>
    public async Task<KetQuaPhanCong> PhanCongAsync(
        Guid hoiDongId,
        IReadOnlyList<Guid> sangKienIds,
        IReadOnlyList<Guid>? thanhVienIds,
        DateTimeOffset? hanHoanThanh,
        bool tuDongChiaDeu,
        CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.DanhGiaPhanCong, ct: ct).ConfigureAwait(false);

        var hoiDong = await _db.HoiDong.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == hoiDongId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("hội đồng", hoiDongId);

        var thanhVien = await _db.HoiDongThanhVien.AsNoTracking()
            .Where(x => x.HoiDongId == hoiDongId
                        && x.QuyenChamDiem
                        && x.TrangThai == TrangThaiDanhMuc.HoatDong)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (thanhVienIds is { Count: > 0 })
        {
            thanhVien = thanhVien.Where(x => thanhVienIds.Contains(x.Id)).ToList();
        }

        if (thanhVien.Count == 0)
        {
            throw new NghiepVuException(MaLoiHeThong.ThanhVienKhongCoQuyenChamDiem,
                "Không có thành viên nào của hội đồng có quyền chấm điểm.");
        }

        if (thanhVien.Count < hoiDong.SoThanhVienToiThieu)
        {
            throw new NghiepVuException(MaLoiHeThong.KhongDuThanhVienToiThieu,
                $"Hội đồng yêu cầu tối thiểu {hoiDong.SoThanhVienToiThieu} thành viên chấm, "
                + $"hiện chỉ chọn được {thanhVien.Count}.");
        }

        var daPhanCong = 0;
        var boQuaXungDot = new List<string>();
        var viTri = 0;

        foreach (var sangKienId in sangKienIds)
        {
            var hoSo = await _db.SangKien.AsNoTracking()
                .Include(x => x.DanhSachTacGia)
                .FirstOrDefaultAsync(x => x.Id == sangKienId, ct)
                .ConfigureAwait(false);

            if (hoSo is null)
            {
                continue;
            }

            var tacGiaIds = hoSo.DanhSachTacGia
                .Where(t => t.NguoiDungId.HasValue)
                .Select(t => t.NguoiDungId!.Value)
                .ToHashSet();

            // Loai tru xung dot loi ich (yeu cau Muc 5 - Nhom IV).
            var duocPhep = thanhVien
                .Where(tv => tv.NguoiDungId is null || !tacGiaIds.Contains(tv.NguoiDungId.Value))
                .ToList();

            var biLoai = thanhVien.Count - duocPhep.Count;
            if (biLoai > 0)
            {
                boQuaXungDot.Add($"{hoSo.MaHoSo}: loại {biLoai} thành viên do xung đột lợi ích.");
            }

            if (duocPhep.Count == 0)
            {
                continue;
            }

            // Tu dong chia deu: moi ho so giao cho mot nhom thanh vien luan phien.
            var nhomCham = tuDongChiaDeu && duocPhep.Count > hoiDong.SoThanhVienToiThieu
                ? duocPhep.Skip(viTri % duocPhep.Count)
                    .Concat(duocPhep.Take(viTri % duocPhep.Count))
                    .Take(hoiDong.SoThanhVienToiThieu)
                    .ToList()
                : duocPhep;

            viTri++;

            foreach (var tv in nhomCham)
            {
                var daCo = await _db.SangKienPhanCong
                    .AnyAsync(x => x.SangKienId == sangKienId && x.ThanhVienId == tv.Id, ct)
                    .ConfigureAwait(false);

                if (daCo)
                {
                    continue;
                }

                _db.SangKienPhanCong.Add(new SangKienPhanCong
                {
                    SangKienId = sangKienId,
                    HoiDongId = hoiDongId,
                    ThanhVienId = tv.Id,
                    NguoiPhanCongId = _nguoiDung.Id,
                    NgayPhanCong = _dongHo.BayGio,
                    HanHoanThanh = hanHoanThanh,
                    TrangThaiPhanCong = TrangThaiPhanCong.ChuaCham
                });

                daPhanCong++;
            }
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Thong bao cho cac thanh vien duoc phan cong.
        var nguoiNhan = thanhVien
            .Where(t => t.NguoiDungId.HasValue)
            .Select(t => t.NguoiDungId!.Value)
            .Distinct()
            .ToList();

        if (nguoiNhan.Count > 0 && daPhanCong > 0)
        {
            await _thongBao.GuiTheoSuKienAsync(
                SuKienThongBao.DuocPhanCongCham, nguoiNhan,
                new Dictionary<string, object?>
                {
                    ["soHoSo"] = sangKienIds.Count,
                    ["tenHoiDong"] = hoiDong.Ten,
                    ["hanHoanThanh"] = hanHoanThanh,
                    // Mot lan phan cong gom nhieu ho so nen tro ve danh sach viec, khong tro ho so le.
                    ["duongDan"] = DuongDanGiaoDien.DanhSachViecDanhGia
                }, ct).ConfigureAwait(false);
        }

        return new KetQuaPhanCong(daPhanCong, boQuaXungDot);
    }

    public sealed record KetQuaPhanCong(int SoLuotPhanCong, IReadOnlyList<string> CanhBao);

    /// <summary>Chuc nang 33 - Danh sach ho so duoc phan cong cho thanh vien dang dang nhap.</summary>
    public async Task<PagedResult<PhanCongChamDto>> LayViecCuaToiAsync(
        ThamSoPhanTrang thamSo, string? trangThai = null, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.DanhGiaXem, ct: ct).ConfigureAwait(false);

        if (_nguoiDung.Id is null)
        {
            return PagedResult<PhanCongChamDto>.Rong(thamSo.Trang, thamSo.SoDong);
        }

        var thanhVienIds = await _db.HoiDongThanhVien.AsNoTracking()
            .Where(x => x.NguoiDungId == _nguoiDung.Id.Value)
            .Select(x => x.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var truyVan = _db.SangKienPhanCong.AsNoTracking()
            .Where(x => thanhVienIds.Contains(x.ThanhVienId));

        if (!string.IsNullOrWhiteSpace(trangThai))
        {
            truyVan = truyVan.Where(x => x.TrangThaiPhanCong == trangThai);
        }

        var tongSo = await truyVan.CountAsync(ct).ConfigureAwait(false);
        var bayGio = _dongHo.BayGio;

        var duLieu = await truyVan
            .OrderBy(x => x.HanHoanThanh ?? DateTimeOffset.MaxValue)
            .ThenByDescending(x => x.NgayPhanCong)
            .Skip(thamSo.BoQua)
            .Take(thamSo.SoDong)
            .Join(_db.SangKien.AsNoTracking(), pc => pc.SangKienId, sk => sk.Id,
                (pc, sk) => new { pc, sk })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var hoiDongIds = duLieu.Select(x => x.pc.HoiDongId).Distinct().ToList();
        var tenHoiDong = await _db.HoiDong.AsNoTracking()
            .Where(x => hoiDongIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct)
            .ConfigureAwait(false);

        var linhVucIds = duLieu.Select(x => x.sk.LinhVucId).Distinct().ToList();
        var tenLinhVuc = await _db.LinhVuc.AsNoTracking()
            .Where(x => linhVucIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct)
            .ConfigureAwait(false);

        var sangKienIds = duLieu.Select(x => x.sk.Id).ToList();
        var phieus = await _db.PhieuDanhGia.AsNoTracking()
            .Where(x => sangKienIds.Contains(x.SangKienId) && thanhVienIds.Contains(x.ThanhVienId))
            .Select(x => new { x.Id, x.SangKienId, x.ThanhVienId, x.TongDiem })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var ketQua = duLieu.Select(x =>
        {
            var phieu = phieus.FirstOrDefault(p =>
                p.SangKienId == x.sk.Id && p.ThanhVienId == x.pc.ThanhVienId);

            return new PhanCongChamDto(
                x.pc.Id,
                x.sk.Id,
                x.sk.MaHoSo,
                x.sk.TenSangKien,
                tenLinhVuc.GetValueOrDefault(x.sk.LinhVucId),
                x.pc.HoiDongId,
                tenHoiDong.GetValueOrDefault(x.pc.HoiDongId) ?? string.Empty,
                x.pc.TrangThaiPhanCong,
                x.pc.NgayPhanCong,
                x.pc.HanHoanThanh,
                x.pc.HanHoanThanh.HasValue
                && x.pc.HanHoanThanh.Value < bayGio
                && x.pc.TrangThaiPhanCong != TrangThaiPhanCong.DaCham,
                phieu?.TongDiem,
                phieu?.Id);
        }).ToList();

        return new PagedResult<PhanCongChamDto>(ketQua, tongSo, thamSo.Trang, thamSo.SoDong);
    }

    // ------------------------------------------------------------------------------------
    // Cham diem (chuc nang 34)
    // ------------------------------------------------------------------------------------

    /// <summary>Lay (hoac tao moi) phieu cham cua thanh vien dang dang nhap cho mot ho so.</summary>
    public async Task<PhieuDanhGiaDto> LayPhieuChamAsync(
        Guid sangKienId, Guid hoiDongId, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.DanhGiaChamDiem, sangKienId, ct).ConfigureAwait(false);

        var thanhVien = await LayThanhVienHienTaiAsync(hoiDongId, ct).ConfigureAwait(false);
        var hoSo = await _db.SangKien.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == sangKienId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("hồ sơ sáng kiến", sangKienId);

        var boTieuChi = await NapBoTieuChiAsync(sangKienId, hoiDongId, ct).ConfigureAwait(false);

        var phieu = await _db.PhieuDanhGia
            .Include(x => x.ChiTiet)
            .FirstOrDefaultAsync(x => x.SangKienId == sangKienId
                                      && x.HoiDongId == hoiDongId
                                      && x.ThanhVienId == thanhVien.Id, ct)
            .ConfigureAwait(false);

        return new PhieuDanhGiaDto
        {
            Id = phieu?.Id ?? Guid.Empty,
            SangKienId = sangKienId,
            MaHoSo = hoSo.MaHoSo,
            TenSangKien = hoSo.TenSangKien,
            HoiDongId = hoiDongId,
            BoTieuChiId = boTieuChi.Id,
            TrangThaiPhieu = phieu?.TrangThaiPhieu ?? TrangThaiPhieuDanhGia.Nhap,
            TongDiem = phieu?.TongDiem ?? 0m,
            NhanXetChung = phieu?.NhanXetChung,
            UuDiem = phieu?.UuDiem,
            HanChe = phieu?.HanChe,
            DeXuatMucCongNhanId = phieu?.DeXuatMucCongNhanId,
            KetLuan = phieu?.KetLuan,
            NgayCham = phieu?.NgayCham,
            NgayGui = phieu?.NgayGui,
            SoPhieu = phieu?.SoPhieu,
            ChoPhepSua = phieu is null || !phieu.DaGui,
            ChiTiet = phieu?.ChiTiet.Select(c => new ChiTietChamDiemDto
            {
                TieuChiId = c.TieuChiId,
                Diem = c.Diem,
                MucDiemId = c.MucDiemId,
                NhanXet = c.NhanXet
            }).ToList() ?? new List<ChiTietChamDiemDto>(),
            BoTieuChi = ChuyenDoiBoTieuChi(boTieuChi)
        };
    }

    /// <summary>Luu nhap hoac gui chinh thuc phieu cham.</summary>
    public async Task<PhieuDanhGiaDto> LuuPhieuAsync(
        PhieuChamDto duLieu, bool guiChinhThuc, CancellationToken ct = default)
    {
        await _phanQuyen
            .BatBuocCoQuyenAsync(MaQuyen.DanhGiaChamDiem, duLieu.SangKienId, ct)
            .ConfigureAwait(false);

        var thanhVien = await LayThanhVienHienTaiAsync(duLieu.HoiDongId, ct).ConfigureAwait(false);

        if (!thanhVien.QuyenChamDiem)
        {
            throw new NghiepVuException(MaLoiHeThong.ThanhVienKhongCoQuyenChamDiem,
                "Bạn không có quyền chấm điểm trong hội đồng này.");
        }

        await BatBuocKhongXungDotLoiIchAsync(duLieu.SangKienId, thanhVien, ct).ConfigureAwait(false);

        var boTieuChi = await NapBoTieuChiAsync(duLieu.SangKienId, duLieu.HoiDongId, ct)
            .ConfigureAwait(false);

        var phieu = await _db.PhieuDanhGia
            .Include(x => x.ChiTiet)
            .FirstOrDefaultAsync(x => x.SangKienId == duLieu.SangKienId
                                      && x.HoiDongId == duLieu.HoiDongId
                                      && x.ThanhVienId == thanhVien.Id, ct)
            .ConfigureAwait(false);

        if (phieu is not null && phieu.DaGui)
        {
            throw new NghiepVuException(MaLoiHeThong.PhieuDaGuiKhongSuaDuoc,
                "Phiếu đã gửi, chỉ thư ký hội đồng mới mở lại được.");
        }

        if (phieu is null)
        {
            phieu = new PhieuDanhGia
            {
                Id = Guid.NewGuid(),
                SangKienId = duLieu.SangKienId,
                HoiDongId = duLieu.HoiDongId,
                ThanhVienId = thanhVien.Id,
                BoTieuChiId = boTieuChi.Id
            };
            _db.PhieuDanhGia.Add(phieu);
        }

        phieu.ChiTiet.Clear();
        var tieuChiTheoId = boTieuChi.DanhSachNhom
            .SelectMany(n => n.DanhSachTieuChi)
            .ToDictionary(t => t.Id);

        foreach (var ct2 in duLieu.ChiTiet)
        {
            if (!tieuChiTheoId.TryGetValue(ct2.TieuChiId, out var tieuChi))
            {
                continue;
            }

            phieu.ChiTiet.Add(new PhieuDanhGiaChiTiet
            {
                Id = Guid.NewGuid(),
                PhieuDanhGiaId = phieu.Id,
                TieuChiId = tieuChi.Id,
                TenTieuChiSnapshot = tieuChi.Ten,
                DiemToiDaSnapshot = tieuChi.DiemToiDa,
                Diem = ct2.Diem,
                MucDiemId = ct2.MucDiemId,
                NhanXet = ct2.NhanXet
            });
        }

        phieu.NhanXetChung = duLieu.NhanXetChung;
        phieu.UuDiem = duLieu.UuDiem;
        phieu.HanChe = duLieu.HanChe;
        phieu.DeXuatMucCongNhanId = duLieu.DeXuatMucCongNhanId;
        phieu.NgayCham = _dongHo.BayGio;

        var ketQuaTinh = _tinhDiem.TinhDiemPhieu(phieu, boTieuChi);

        if (guiChinhThuc && !ketQuaTinh.HopLe)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                string.Join(" ", ketQuaTinh.DanhSachLoi));
        }

        phieu.TongDiem = ketQuaTinh.TongDiem;
        phieu.DiemTheoNhom = ketQuaTinh.DiemTheoNhom
            .ToDictionary(c => c.Key.ToString(), c => c.Value);
        phieu.KetLuan = ketQuaTinh.Dat
            ? KetQuaXetDuyetGiaTri.Dat
            : KetQuaXetDuyetGiaTri.KhongDat;

        if (guiChinhThuc)
        {
            phieu.TrangThaiPhieu = TrangThaiPhieuDanhGia.DaGui;
            phieu.NgayGui = _dongHo.BayGio;
            phieu.SoPhieu ??= $"PDG-{_dongHo.BayGio:yyyyMMdd}-{phieu.Id.ToString()[..8].ToUpperInvariant()}";

            var phanCong = await _db.SangKienPhanCong
                .FirstOrDefaultAsync(x => x.SangKienId == duLieu.SangKienId
                                          && x.ThanhVienId == thanhVien.Id, ct)
                .ConfigureAwait(false);

            if (phanCong is not null)
            {
                phanCong.TrangThaiPhanCong = TrangThaiPhanCong.DaCham;
            }
        }
        else
        {
            phieu.TrangThaiPhieu = TrangThaiPhieuDanhGia.Nhap;

            var phanCong = await _db.SangKienPhanCong
                .FirstOrDefaultAsync(x => x.SangKienId == duLieu.SangKienId
                                          && x.ThanhVienId == thanhVien.Id, ct)
                .ConfigureAwait(false);

            if (phanCong is not null && phanCong.TrangThaiPhanCong == TrangThaiPhanCong.ChuaCham)
            {
                phanCong.TrangThaiPhanCong = TrangThaiPhanCong.DangCham;
            }
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return await LayPhieuChamAsync(duLieu.SangKienId, duLieu.HoiDongId, ct).ConfigureAwait(false);
    }

    /// <summary>Thu ky mo lai phieu da gui de thanh vien sua.</summary>
    public async Task MoLaiPhieuAsync(Guid phieuId, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.DanhGiaMoLaiPhieu, phieuId, ct).ConfigureAwait(false);

        var phieu = await _db.PhieuDanhGia.FirstOrDefaultAsync(x => x.Id == phieuId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("phiếu đánh giá", phieuId);

        phieu.TrangThaiPhieu = TrangThaiPhieuDanhGia.Nhap;
        phieu.NgayGui = null;

        var phanCong = await _db.SangKienPhanCong
            .FirstOrDefaultAsync(x => x.SangKienId == phieu.SangKienId
                                      && x.ThanhVienId == phieu.ThanhVienId, ct)
            .ConfigureAwait(false);

        if (phanCong is not null)
        {
            phanCong.TrangThaiPhanCong = TrangThaiPhanCong.DangCham;
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // ------------------------------------------------------------------------------------
    // Tong hop diem (chuc nang 32, 35)
    // ------------------------------------------------------------------------------------

    public async Task<KetQuaTongHopDto> TongHopDiemAsync(
        Guid sangKienId, Guid hoiDongId, Guid? phienHopId = null, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.DanhGiaTongHop, sangKienId, ct).ConfigureAwait(false);

        var boTieuChi = await NapBoTieuChiAsync(sangKienId, hoiDongId, ct).ConfigureAwait(false);

        var phieus = await _db.PhieuDanhGia
            .Include(x => x.ChiTiet)
            .Where(x => x.SangKienId == sangKienId && x.HoiDongId == hoiDongId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var tongHop = _tinhDiem.TongHopDiemHoiDong(phieus, boTieuChi);

        var soDongY = await _db.PhieuBoPhieu.AsNoTracking()
            .CountAsync(x => x.SangKienId == sangKienId && x.YKien == YKienBoPhieu.DongY, ct)
            .ConfigureAwait(false);

        var soKhongDongY = await _db.PhieuBoPhieu.AsNoTracking()
            .CountAsync(x => x.SangKienId == sangKienId && x.YKien == YKienBoPhieu.KhongDongY, ct)
            .ConfigureAwait(false);

        var ketQua = await _db.KetQuaXetDuyet
            .FirstOrDefaultAsync(x => x.SangKienId == sangKienId && x.HoiDongId == hoiDongId, ct)
            .ConfigureAwait(false);

        if (ketQua is null)
        {
            ketQua = new KetQuaXetDuyet { SangKienId = sangKienId, HoiDongId = hoiDongId };
            _db.KetQuaXetDuyet.Add(ketQua);
        }

        ketQua.PhienHopId = phienHopId ?? ketQua.PhienHopId;
        ketQua.SoPhieuCham = tongHop.SoPhieu;
        ketQua.DiemCaoNhat = tongHop.DiemCaoNhat;
        ketQua.DiemThapNhat = tongHop.DiemThapNhat;
        ketQua.DiemTrungBinh = tongHop.DiemTrungBinh;
        ketQua.TongDiemTrongSo = tongHop.DiemCuoiCung;
        ketQua.SoPhieuDongY = soDongY;
        ketQua.SoPhieuKhongDongY = soKhongDongY;
        ketQua.KetQua = tongHop.Dat ? KetQuaXetDuyetGiaTri.Dat : KetQuaXetDuyetGiaTri.KhongDat;
        ketQua.MucCongNhanId = tongHop.MucCongNhan?.Id;
        ketQua.NguoiKetLuanId = _nguoiDung.Id;
        ketQua.NgayKetLuan = _dongHo.BayGio;

        // Cap nhat nguoc len ho so de dieu kien chuyen buoc su dung duoc.
        var hoSo = await _db.SangKien.FirstOrDefaultAsync(x => x.Id == sangKienId, ct)
            .ConfigureAwait(false);

        if (hoSo is not null)
        {
            hoSo.TongDiem = tongHop.DiemCuoiCung;
            hoSo.DiemTrungBinh = tongHop.DiemTrungBinh;
            hoSo.MucCongNhanId = tongHop.MucCongNhan?.Id;
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new KetQuaTongHopDto(
            sangKienId, tongHop.SoPhieu, tongHop.SoPhieuSuDung,
            tongHop.DiemCaoNhat, tongHop.DiemThapNhat, tongHop.DiemTrungBinh, tongHop.DiemCuoiCung,
            tongHop.Dat, tongHop.MucCongNhan?.Id, tongHop.MucCongNhan?.Ten, tongHop.DanhSachCanhBao);
    }

    /// <summary>Chuc nang 35 - Bang ma tran diem (hang = ho so, cot = thanh vien).</summary>
    public async Task<IReadOnlyList<DongMaTranDiem>> LayMaTranDiemAsync(
        Guid hoiDongId, Guid? dotDeNghiId = null, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.DanhGiaTongHop, ct: ct).ConfigureAwait(false);

        var thanhVien = await _db.HoiDongThanhVien.AsNoTracking()
            .Where(x => x.HoiDongId == hoiDongId && x.QuyenChamDiem)
            .OrderBy(x => x.ThuTu)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var phanCong = await _db.SangKienPhanCong.AsNoTracking()
            .Where(x => x.HoiDongId == hoiDongId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var sangKienIds = phanCong.Select(x => x.SangKienId).Distinct().ToList();

        var hoSos = await _db.SangKien.AsNoTracking()
            .Where(x => sangKienIds.Contains(x.Id)
                        && (dotDeNghiId == null || x.DotDeNghiId == dotDeNghiId))
            .Select(x => new { x.Id, x.MaHoSo, x.TenSangKien })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var phieus = await _db.PhieuDanhGia.AsNoTracking()
            .Where(x => x.HoiDongId == hoiDongId && sangKienIds.Contains(x.SangKienId))
            .Select(x => new { x.Id, x.SangKienId, x.ThanhVienId, x.TongDiem, x.TrangThaiPhieu })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return hoSos.Select(hs =>
        {
            var o = thanhVien.Select(tv =>
            {
                var phieu = phieus.FirstOrDefault(p => p.SangKienId == hs.Id && p.ThanhVienId == tv.Id);
                var daPhanCong = phanCong.Any(pc => pc.SangKienId == hs.Id && pc.ThanhVienId == tv.Id);

                var trangThai = phieu is null
                    ? daPhanCong ? TrangThaiPhanCong.ChuaCham : "-"
                    : phieu.TrangThaiPhieu;

                // Cham diem doc lap: chi hien diem khi phieu da gui.
                var diem = phieu is not null
                           && phieu.TrangThaiPhieu != TrangThaiPhieuDanhGia.Nhap
                    ? phieu.TongDiem
                    : (decimal?)null;

                return new ODiemMaTran(tv.Id, tv.HoTenHienThi, diem, trangThai, phieu?.Id);
            }).ToList();

            var diemDaCham = o.Where(x => x.Diem.HasValue).Select(x => x.Diem!.Value).ToList();

            return new DongMaTranDiem(
                hs.Id, hs.MaHoSo, hs.TenSangKien, o,
                diemDaCham.Count > 0 ? Math.Round(diemDaCham.Average(), 2) : null,
                diemDaCham.Count > 0 ? diemDaCham.Max() : null,
                diemDaCham.Count > 0 ? diemDaCham.Min() : null,
                diemDaCham.Count,
                phanCong.Count(pc => pc.SangKienId == hs.Id));
        }).ToList();
    }

    // ------------------------------------------------------------------------------------

    private async Task<HoiDongThanhVien> LayThanhVienHienTaiAsync(Guid hoiDongId, CancellationToken ct)
    {
        if (_nguoiDung.Id is null)
        {
            throw new NghiepVuException(MaLoiHeThong.ChuaXacThuc, "Chưa đăng nhập.");
        }

        var thanhVien = await _db.HoiDongThanhVien.AsNoTracking()
            .FirstOrDefaultAsync(x => x.HoiDongId == hoiDongId
                                      && x.NguoiDungId == _nguoiDung.Id.Value
                                      && x.TrangThai == TrangThaiDanhMuc.HoatDong, ct)
            .ConfigureAwait(false);

        return thanhVien ?? throw new NghiepVuException(MaLoiHeThong.KhongCoQuyen,
            "Bạn không phải thành viên của hội đồng này.");
    }

    private async Task BatBuocKhongXungDotLoiIchAsync(
        Guid sangKienId, HoiDongThanhVien thanhVien, CancellationToken ct)
    {
        if (thanhVien.NguoiDungId is null)
        {
            return;
        }

        var laTacGia = await _db.SangKienTacGia.AsNoTracking()
            .AnyAsync(x => x.SangKienId == sangKienId
                           && x.NguoiDungId == thanhVien.NguoiDungId.Value, ct)
            .ConfigureAwait(false);

        if (laTacGia)
        {
            throw new NghiepVuException(MaLoiHeThong.XungDotLoiIch,
                "Bạn là tác giả của hồ sơ này nên không được chấm điểm.");
        }
    }

    /// <summary>Nap bo tieu chi ap dung: uu tien buoc cham diem cua quy trinh, sau do la dot de nghi.</summary>
    private async Task<BoTieuChi> NapBoTieuChiAsync(
        Guid sangKienId, Guid hoiDongId, CancellationToken ct)
    {
        var hoSo = await _db.SangKien.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == sangKienId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("hồ sơ sáng kiến", sangKienId);

        var boTieuChiId = await _db.QuyTrinhBuoc.AsNoTracking()
            .Where(b => b.HoiDongId == hoiDongId && b.BoTieuChiId != null && b.QuyTrinhId == hoSo.QuyTrinhId)
            .Select(b => b.BoTieuChiId)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        boTieuChiId ??= await _db.DotDeNghi.AsNoTracking()
            .Where(x => x.Id == hoSo.DotDeNghiId)
            .Select(x => x.BoTieuChiId)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (boTieuChiId is null)
        {
            throw new NghiepVuException(MaLoiHeThong.BoTieuChiKhongHopLe,
                "Chưa cấu hình bộ tiêu chí cho đợt / bước chấm điểm này.");
        }

        var bo = await _db.BoTieuChi.AsNoTracking()
            .Include(x => x.DanhSachNhom).ThenInclude(n => n.DanhSachTieuChi).ThenInclude(t => t.DanhSachMucDiem)
            .Include(x => x.DanhSachMucCongNhan)
            .FirstOrDefaultAsync(x => x.Id == boTieuChiId.Value, ct)
            .ConfigureAwait(false);

        return bo ?? throw new KhongTimThayException("bộ tiêu chí", boTieuChiId.Value);
    }

    internal static BoTieuChiDto ChuyenDoiBoTieuChi(BoTieuChi bo) => new(
        bo.Id, bo.Ma, bo.Ten, bo.ThangDiemToiDa, bo.DiemDatToiThieu, bo.CachTinh, bo.LamTron,
        bo.DanhSachNhom
            .Where(n => !n.DaXoa && n.TrangThai == TrangThaiDanhMuc.HoatDong)
            .OrderBy(n => n.ThuTu)
            .Select(n => new NhomTieuChiDto(
                n.Id, n.Ma, n.Ten, n.MoTa, n.TrongSo, n.DiemToiDa, n.ThuTu,
                n.DanhSachTieuChi
                    .Where(t => !t.DaXoa && t.TrangThai == TrangThaiDanhMuc.HoatDong)
                    .OrderBy(t => t.ThuTu)
                    .Select(t => new TieuChiDto(
                        t.Id, t.Ma, t.Ten, t.MoTa, t.DiemToiDa, t.DiemToiThieu, t.TrongSo,
                        t.KieuNhap, t.BuocNhay, t.BatBuocNhanXet, t.HuongDanCham, t.ThuTu,
                        t.DanhSachMucDiem
                            .OrderBy(m => m.ThuTu)
                            .Select(m => new MucDiemDto(m.Id, m.Ten, m.Diem, m.MoTa, m.ThuTu))
                            .ToList()))
                    .ToList()))
            .ToList());
}
