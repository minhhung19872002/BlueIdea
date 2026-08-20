using BlueIdea.Ai.Nhung;
using BlueIdea.Ai.TrungLap;
using BlueIdea.Ai.XuLyVanBan;
using BlueIdea.Application.Chung;
using BlueIdea.Application.SangKien;
using BlueIdea.Workflow;
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
    private readonly INguoiDungHienTai _nguoiDung;
    private readonly ILogger<DichVuKiemTraTrungLap> _logger;
    private readonly IBoChuyenDoiSnapshotQuyTrinh _snapshot;
    private readonly IDichVuNhatKy _nhatKy;
    private readonly IBoDayRealtime? _realtime;

    public DichVuKiemTraTrungLap(
        IAppDbContext db,
        IBoPhanTichTrungLap phanTich,
        IBoNhungVanBan boNhung,
        IDichVuCauHinh cauHinh,
        IDongHoHeThong dongHo,
        IDichVuPhanQuyen phanQuyen,
        INguoiDungHienTai nguoiDung,
        ILogger<DichVuKiemTraTrungLap> logger,
        IBoChuyenDoiSnapshotQuyTrinh snapshot,
        IDichVuNhatKy nhatKy,
        IBoDayRealtime? realtime = null)
    {
        _snapshot = snapshot;
        _nhatKy = nhatKy;
        _db = db;
        _phanTich = phanTich;
        _boNhung = boNhung;
        _cauHinh = cauHinh;
        _dongHo = dongHo;
        _phanQuyen = phanQuyen;
        _nguoiDung = nguoiDung;
        _logger = logger;
        _realtime = realtime;
    }

    /// <summary>
    /// Bao cho man hinh dang mo ho so rang ket qua trung lap vua co.
    ///
    /// Chay SAU khi da luu va nuot loi: day realtime that bai chi lam man hinh cham mot nhip,
    /// khong duoc phep lam hong ket qua kiem tra da ghi vao CSDL.
    /// </summary>
    private async Task DayKetQuaAsync(Guid sangKienId)
    {
        if (_realtime is null) return;

        try
        {
            await _realtime.KetQuaTrungLapAsync(sangKienId, CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Khong day duoc tin hieu ket qua trung lap cho ho so {SangKienId}", sangKienId);
        }
    }

    /// <summary>
    /// Chay kiem tra trung lap cho mot ho so. Duoc goi tu Hangfire job hoac tu nut "Chạy lại".
    /// Loi cua buoc nay KHONG duoc lam hong luong nop ho so (graceful degradation - Muc 7).
    /// </summary>
    public async Task<KetQuaPhanTichTrungLap?> ChayAsync(
        Guid sangKienId, bool batBuocChayLai = false, CancellationToken ct = default)
    {
        // Hangfire runs without HTTP context (DaXacThuc is false); skip user-facing scope check there.
        if (_nguoiDung.DaXacThuc)
        {
            await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.TrungLapChayLai, ct: ct).ConfigureAwait(false);
            await BatBuocTrongPhamViSangKienAsync(sangKienId, ct).ConfigureAwait(false);
        }

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
                await DayKetQuaAsync(sangKienId).ConfigureAwait(false);
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
            await DayKetQuaAsync(sangKienId).ConfigureAwait(false);
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

            // Bao ca khi that bai: nguoi dung dang cho o tab Trung lap can biet la da chay xong
            // va khong dat, thay vi ngoi cho mai mot ket qua khong bao gio toi.
            await DayKetQuaAsync(sangKienId).ConfigureAwait(false);
            return null;
        }
    }

    /// <summary>Lay ket qua kiem tra gan nhat de hien thi tab "Trùng lặp".</summary>
    public async Task<EntityKiemTra?> LayKetQuaGanNhatAsync(
        Guid sangKienId, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.TrungLapXem, ct: ct).ConfigureAwait(false);
        await BatBuocTrongPhamViSangKienAsync(sangKienId, ct).ConfigureAwait(false);

        return await _db.KiemTraTrungLap.AsNoTracking()
            .Include(x => x.ChiTiet)
            .Where(x => x.SangKienId == sangKienId)
            .OrderByDescending(x => x.NgayChay)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Chuc nang 26 — Gom du lieu de xuat bao cao trung lap ra PDF.
    ///
    /// Tra ve ca ten ho so doi chieu (khong chi Id) vi ban PDF roi khoi he thong: nguoi doc
    /// khong tra cuu duoc Id, va bao cao dinh kem ho so hoi dong phai tu no doc duoc.
    /// </summary>
    public async Task<DuLieuBaoCaoTrungLap?> LayDuLieuBaoCaoAsync(
        Guid sangKienId, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.TrungLapXem, ct: ct).ConfigureAwait(false);
        await BatBuocTrongPhamViSangKienAsync(sangKienId, ct).ConfigureAwait(false);

        var ketQua = await _db.KiemTraTrungLap.AsNoTracking()
            .Include(x => x.ChiTiet)
            .Where(x => x.SangKienId == sangKienId)
            .OrderByDescending(x => x.NgayChay)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (ketQua is null) return null;

        var hoSo = await _db.SangKien.AsNoTracking()
            .Include(x => x.DanhSachTacGia)
            .FirstOrDefaultAsync(x => x.Id == sangKienId, ct)
            .ConfigureAwait(false);

        if (hoSo is null) return null;

        var idDoiChieu = ketQua.ChiTiet.Select(c => c.SangKienDoiChieuId).Distinct().ToList();

        var hoSoDoiChieu = await _db.SangKien.AsNoTracking()
            .Where(x => idDoiChieu.Contains(x.Id))
            .Select(x => new { x.Id, x.MaHoSo, x.TenSangKien, x.DonViId })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var idDonVi = hoSoDoiChieu.Where(x => x.DonViId.HasValue).Select(x => x.DonViId!.Value)
            .Append(hoSo.DonViId ?? Guid.Empty)
            .Distinct()
            .ToList();

        var tenDonVi = await _db.DonVi.AsNoTracking()
            .Where(x => idDonVi.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct)
            .ConfigureAwait(false);

        var tenMoHinh = ketQua.PhamVi is not null
                        && ketQua.PhamVi.TryGetValue("moHinhNhung", out var mh)
            ? mh?.ToString()
            : null;

        var chiTiet = ketQua.ChiTiet
            .OrderByDescending(c => c.TyLeTuongDong)
            .Select(c =>
            {
                var doi = hoSoDoiChieu.FirstOrDefault(x => x.Id == c.SangKienDoiChieuId);

                return new DongBaoCaoTrungLap(
                    doi?.MaHoSo ?? "(không còn)",
                    doi?.TenSangKien ?? "(hồ sơ đã bị xoá)",
                    doi?.DonViId is { } dvId ? tenDonVi.GetValueOrDefault(dvId) : null,
                    c.TyLeTuongDong,
                    c.TyLeTuVung,
                    c.TyLeNguNghia,
                    c.SoDoanTrung,
                    c.CacDoanTrung
                        .OrderByDescending(d => d.TyLe)
                        .Select(d => new CapDoanTrungBaoCao(d.DoanNguon, d.DoanDich, d.TyLe))
                        .ToList());
            })
            .ToList();

        return new DuLieuBaoCaoTrungLap(
            hoSo.MaHoSo,
            hoSo.TenSangKien,
            hoSo.DanhSachTacGia.FirstOrDefault(t => t.LaTacGiaChinh)?.HoTen
                ?? hoSo.DanhSachTacGia.FirstOrDefault()?.HoTen,
            hoSo.DonViId is { } donViId ? tenDonVi.GetValueOrDefault(donViId) : null,
            ketQua.NgayChay,
            ketQua.PhienBanThuatToan,
            tenMoHinh,
            ketQua.TongSoDoiChieu,
            ketQua.TyLeCaoNhat,
            ketQua.MucCanhBao,
            ketQua.DaXemXet,
            ketQua.YKienHoiDong,
            chiTiet);
    }

    /// <summary>
    /// Chuc nang 26 — Danh dau "Đã xem xét" kem y kien hoi dong tren ket qua kiem tra GAN NHAT
    /// cua mot ho so. Man hinh chi hien ket qua gan nhat nen nhan theo ho so la dung thao tac
    /// nguoi dung dang lam; tra ve ban ghi da cap nhat de giao dien khong phai goi lai.
    /// </summary>
    public async Task<EntityKiemTra> DanhDauDaXemXetTheoSangKienAsync(
        Guid sangKienId, string? yKien, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.TrungLapXemXet, ct: ct).ConfigureAwait(false);
        await BatBuocTrongPhamViSangKienAsync(sangKienId, ct).ConfigureAwait(false);

        var banGhi = await _db.KiemTraTrungLap
            .Where(x => x.SangKienId == sangKienId)
            .OrderByDescending(x => x.NgayChay)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false)
            ?? throw new KhongTimThayException("kết quả kiểm tra trùng lặp", sangKienId);

        return await GhiYKienAsync(banGhi, yKien, ct).ConfigureAwait(false);
    }

    private async Task<EntityKiemTra> GhiYKienAsync(
        EntityKiemTra banGhi, string? yKien, CancellationToken ct)
    {
        var truoc = new { banGhi.DaXemXet, banGhi.YKienHoiDong };

        banGhi.DaXemXet = true;
        banGhi.YKienHoiDong = string.IsNullOrWhiteSpace(yKien) ? null : yKien.Trim();
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Ket luan cua hoi dong ve mot canh bao dao van la quyet dinh nghiep vu — phai truy nguoc
        // duoc ai ghi, luc nao (dac ta Muc 6 - audit log).
        await _nhatKy.GhiAsync(
            "XEM_XET_TRUNG_LAP", "TRUNG_LAP", "KiemTraTrungLap", banGhi.Id,
            $"Đánh dấu đã xem xét kết quả kiểm tra trùng lặp của hồ sơ {banGhi.SangKienId}.",
            truoc, new { banGhi.DaXemXet, banGhi.YKienHoiDong }, ct: ct)
            .ConfigureAwait(false);

        return banGhi;
    }

    // ------------------------------------------------------------------------------------

    private async Task BatBuocTrongPhamViSangKienAsync(Guid sangKienId, CancellationToken ct)
    {
        if (_nguoiDung.Id is null)
            throw new KhongTimThayException("hồ sơ sáng kiến", sangKienId);

        var nguoiDungId = _nguoiDung.Id.Value;
        var hoSo = await _db.SangKien.AsNoTracking()
            .Include(x => x.DanhSachTacGia)
            .FirstOrDefaultAsync(x => x.Id == sangKienId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("hồ sơ sáng kiến", sangKienId);

        var phamVi = await _phanQuyen.LayPhamViTruyCapAsync(nguoiDungId, ct).ConfigureAwait(false);

        if (phamVi.ToanHeThong) return;

        var laTacGia = hoSo.NguoiTaoId == nguoiDungId
                       || hoSo.DanhSachTacGia.Any(t => t.NguoiDungId == nguoiDungId);

        if (phamVi.ChiCaNhan)
        {
            if (!laTacGia) throw new KhongTimThayException("hồ sơ sáng kiến", sangKienId);
            return;
        }

        var trongDonVi = hoSo.DonViId.HasValue && phamVi.DonViIds.Contains(hoSo.DonViId.Value);
        if (!laTacGia && !trongDonVi)
            throw new KhongTimThayException("hồ sơ sáng kiến", sangKienId);
    }

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

    /// <summary>
    /// Danh sach ma thanh phan ho so duoc dua vao so khop trung lap, doc tu snapshot quy trinh
    /// cua chinh ho so (chuc nang 13 - co <c>dung_de_kiem_tra_trung_lap</c>).
    ///
    /// Tra ve <c>null</c> khi ho so khong co snapshot hoac quy trinh khong khai thanh phan nao —
    /// khi do giu nguyen hanh vi cu (so khop toan bo) de khong lam mat kha nang phat hien trung lap.
    /// </summary>
    private IReadOnlySet<string>? LayThanhPhanDuocSoKhop(string? snapshot)
    {
        var quyTrinh = _snapshot.DocSnapshot(snapshot);
        var thanhPhan = quyTrinh?.ThanhPhanHoSo.Where(t => !t.DaXoa).ToList();

        if (thanhPhan is null || thanhPhan.Count == 0)
        {
            return null;
        }

        return thanhPhan
            .Where(t => t.DungDeKiemTraTrungLap)
            .Select(t => t.Ma)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
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

        // Ten sang kien luon duoc so khop: no khong phai thanh phan ho so nen khong nam trong
        // cau hinh "dung de kiem tra trung lap", nhung dat ten trung nhau la dau hieu ro nhat.
        var phan = new List<string?> { hoSo.TenSangKien };

        var maThanhPhanDuocSoKhop = LayThanhPhanDuocSoKhop(hoSo.QuyTrinhSnapshot);

        if (maThanhPhanDuocSoKhop is null)
        {
            // Ho so khong co snapshot quy trinh (du lieu cu, hoac quy trinh chua khai thanh phan)
            // thi giu nguyen hanh vi cu: so khop toan bo noi dung.
            phan.AddRange(new[]
            {
                hoSo.MoTaGiaiPhap,
                hoSo.TinhTrangTruocKhiApDung,
                hoSo.NoiDungGiaiPhap,
                hoSo.TinhMoi,
                hoSo.KhaNangApDung,
                hoSo.PhamViApDung,
                hoSo.HieuQuaKinhTe,
                hoSo.HieuQuaXaHoi
            });

            phan.AddRange(hoSo.NoiDungDong.Values);
            phan.AddRange(hoSo.TepDinhKem
                .Where(t => t.TepTin is not null && !string.IsNullOrWhiteSpace(t.TepTin.NoiDungTrichXuat))
                .Select(t => t.TepTin!.NoiDungTrichXuat));
        }
        else
        {
            // Chuc nang 13: chi thanh phan duoc tick "dùng để kiểm tra trùng lặp" moi di vao pipeline.
            // Phu luc / bieu mau hanh chinh giong nhau giua moi ho so, dua vao chi lam nhieu ty le.
            foreach (var ma in maThanhPhanDuocSoKhop)
            {
                phan.Add(BoKiemTraThanhPhanHoSo.LayNoiDung(hoSo, ma));
            }

            phan.AddRange(hoSo.TepDinhKem
                .Where(t => t.TepTin is not null
                            && !string.IsNullOrWhiteSpace(t.TepTin.NoiDungTrichXuat)
                            && maThanhPhanDuocSoKhop.Contains(t.ThanhPhanHoSoMa))
                .Select(t => t.TepTin!.NoiDungTrichXuat));
        }

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

/// <summary>Chuc nang 26 — Du lieu tho de sinh bao cao trung lap (PDF hoac man hinh).</summary>
public sealed record DuLieuBaoCaoTrungLap(
    string MaHoSo,
    string TenSangKien,
    string? TenTacGiaChinh,
    string? TenDonVi,
    DateTimeOffset NgayChay,
    string PhienBanThuatToan,
    string? TenMoHinhNhung,
    int TongSoDoiChieu,
    decimal TyLeCaoNhat,
    string MucCanhBao,
    bool DaXemXet,
    string? YKienHoiDong,
    IReadOnlyList<DongBaoCaoTrungLap> ChiTiet);

/// <summary>Mot ho so bi doi chieu trung trong bao cao.</summary>
public sealed record DongBaoCaoTrungLap(
    string MaHoSo,
    string TenSangKien,
    string? TenDonVi,
    decimal TyLeTuongDong,
    decimal TyLeTuVung,
    decimal TyLeNguNghia,
    int SoDoanTrung,
    IReadOnlyList<CapDoanTrungBaoCao> CacDoanTrung);

/// <summary>Mot cap doan van trung nhau trong bao cao.</summary>
public sealed record CapDoanTrungBaoCao(string DoanNguon, string DoanDich, decimal TyLe);
