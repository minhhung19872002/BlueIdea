using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Domain.QuyTrinh;
using BlueIdea.Domain.SangKien;
using BlueIdea.Shared.KetQua;
using BlueIdea.Workflow;
using BlueIdea.Workflow.DieuKien;
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
        var nguCanh = await TaoNguCanhAsync(
                request.SangKienId, request.NguoiDungId, ct, request.HanhDongNguoiDung)
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
        Guid sangKienId, Guid nguoiDungId, CancellationToken ct,
        string? hanhDongNguoiDung = null)
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
        var bienBoSung = await TaoBienBoSungAsync(hoSo, quyTrinh, lichSu, ct).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(hanhDongNguoiDung))
        {
            // Bien nay truoc day duoc khai bao va duoc quy trinh mau dung lam dieu kien, nhung
            // khong co duong nao dat gia tri — nen nhanh nao phu thuoc vao no la vinh vien bi chan.
            bienBoSung = new Dictionary<string, object?>(bienBoSung)
            {
                [BienNguCanh.HanhDongNguoiDung] = hanhDongNguoiDung
            };
        }

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
    /// <summary>
    /// Chuc nang 15/29 — Danh sach nguoi CO THE xu ly buoc hien tai cua mot ho so.
    ///
    /// Dung cho o chon "xu ly thay cho ai" khi buoc cho phep uy quyen: khong the dung danh sach
    /// nguoi dung toan he thong vi can bo tiep nhan khong co quyen NGUOI_DUNG.XEM, va cung khong
    /// nen cho chon mot nguoi von khong phai tac nhan cua buoc.
    /// </summary>
    public async Task<IReadOnlyList<TacNhanBuocDto>> LayTacNhanBuocHienTaiAsync(
        Guid sangKienId, CancellationToken ct)
    {
        var hoSo = await _db.SangKien.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == sangKienId, ct)
            .ConfigureAwait(false);

        if (hoSo?.BuocHienTaiId is null) return Array.Empty<TacNhanBuocDto>();

        var quyTrinh = _snapshot.DocSnapshot(hoSo.QuyTrinhSnapshot);
        var buoc = quyTrinh?.DanhSachBuoc.FirstOrDefault(b => b.Id == hoSo.BuocHienTaiId.Value);

        if (buoc is null) return Array.Empty<TacNhanBuocDto>();

        var ketQua = new Dictionary<Guid, TacNhanBuocDto>();

        foreach (var tn in buoc.TacNhan.Where(t => !t.DaXoa))
        {
            switch (tn.LoaiTacNhan)
            {
                case LoaiTacNhan.NguoiDung when tn.ThamChieuId.HasValue:
                    await ThemNguoiAsync(ketQua, new[] { tn.ThamChieuId.Value }, ct)
                        .ConfigureAwait(false);
                    break;

                case LoaiTacNhan.NguoiTaoHoSo when hoSo.NguoiTaoId.HasValue:
                    await ThemNguoiAsync(ketQua, new[] { hoSo.NguoiTaoId.Value }, ct)
                        .ConfigureAwait(false);
                    break;

                case LoaiTacNhan.HoiDong when tn.ThamChieuId.HasValue:
                {
                    var ids = await _db.HoiDongThanhVien.AsNoTracking()
                        .Where(x => x.HoiDongId == tn.ThamChieuId.Value
                                    && x.NguoiDungId != null
                                    && x.TrangThai == TrangThaiDanhMuc.HoatDong)
                        .Select(x => x.NguoiDungId!.Value)
                        .ToListAsync(ct)
                        .ConfigureAwait(false);

                    await ThemNguoiAsync(ketQua, ids, ct).ConfigureAwait(false);
                    break;
                }

                case LoaiTacNhan.VaiTro when !string.IsNullOrEmpty(tn.ThamChieuMa):
                {
                    var homNay = _dongHo.HomNay;

                    var ids = await _db.NguoiDungVaiTro.AsNoTracking()
                        .Where(x => _db.VaiTro.Any(v => v.Id == x.VaiTroId && v.Ma == tn.ThamChieuMa)
                                    && (x.TuNgay == null || x.TuNgay <= homNay)
                                    && (x.DenNgay == null || x.DenNgay >= homNay))
                        .Select(x => x.NguoiDungId)
                        .ToListAsync(ct)
                        .ConfigureAwait(false);

                    await ThemNguoiAsync(ketQua, ids, ct).ConfigureAwait(false);
                    break;
                }

                case LoaiTacNhan.DonVi when tn.ThamChieuId.HasValue:
                {
                    var ids = await _db.NguoiDung.AsNoTracking()
                        .Where(x => x.DonViId == tn.ThamChieuId.Value
                                    && x.TrangThaiTaiKhoan == TrangThaiNguoiDung.HoatDong)
                        .Select(x => x.Id)
                        .ToListAsync(ct)
                        .ConfigureAwait(false);

                    await ThemNguoiAsync(ketQua, ids, ct).ConfigureAwait(false);
                    break;
                }
            }
        }

        return ketQua.Values.OrderBy(x => x.HoTen).ToList();
    }

    private async Task ThemNguoiAsync(
        Dictionary<Guid, TacNhanBuocDto> ketQua, IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        var canLay = ids.Where(x => !ketQua.ContainsKey(x)).Distinct().ToList();

        if (canLay.Count == 0) return;

        var nguoi = await _db.NguoiDung.AsNoTracking()
            .Where(x => canLay.Contains(x.Id) && x.TrangThaiTaiKhoan == TrangThaiNguoiDung.HoatDong)
            .Select(x => new { x.Id, x.HoTen, x.ChucVu, x.TenDangNhap })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var n in nguoi)
        {
            ketQua[n.Id] = new TacNhanBuocDto(n.Id, n.HoTen, n.ChucVu, n.TenDangNhap);
        }
    }

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
        HoSoSangKien hoSo,
        QuyTrinh quyTrinh,
        IReadOnlyList<SangKienXuLy> lichSu,
        CancellationToken ct)
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

        var capPheDuyet = await TaoBienCapPheDuyetAsync(hoSo, quyTrinh, lichSu, ct)
            .ConfigureAwait(false);

        var bien = new Dictionary<string, object?>
        {
            ["so_phieu_cham"] = soPhieuCham,
            ["so_phieu_dong_y"] = ketQua?.SoPhieuDongY ?? 0,
            ["so_phieu_khong_dong_y"] = ketQua?.SoPhieuKhongDongY ?? 0,
            ["ty_le_dong_thuan"] = ketQua is null || ketQua.SoPhieuDongY + ketQua.SoPhieuKhongDongY == 0
                ? 0m
                : Math.Round(
                    ketQua.SoPhieuDongY * 100m / (ketQua.SoPhieuDongY + ketQua.SoPhieuKhongDongY), 2)
        };

        foreach (var cap in capPheDuyet)
        {
            bien[cap.Key] = cap.Value;
        }

        return bien;
    }

    /// <summary>
    /// Chuc nang 5 — Bien ngu canh ve CAP PHE DUYET, lay tu bang <c>cau_hinh_cap_phe_duyet</c>.
    ///
    /// Nho cac bien nay, quan tri vien khai duoc nhanh "Chuyển cấp cao hơn" bang DIEU KIEN
    /// (<c>con_cap_phe_duyet_cao_hon = true</c>) thay vi phai sua code moi khi don vi doi so cap
    /// xet duyet. Truoc day bang cau hinh chi de xem — khai xong khong tac dong gi den luong chay.
    ///
    /// Cap hien tai duoc dem bang so buoc PHE_DUYET da xu ly xong trong chinh ho so: moi lan mot
    /// cap ky duyet xong la ho so len mot cap.
    /// </summary>
    private async Task<IReadOnlyDictionary<string, object?>> TaoBienCapPheDuyetAsync(
        HoSoSangKien hoSo,
        QuyTrinh quyTrinh,
        IReadOnlyList<SangKienXuLy> lichSu,
        CancellationToken ct)
    {
        var cauHinh = await _db.CauHinhCapPheDuyet.AsNoTracking()
            .Where(x => !x.DaXoa
                        && (x.DotDeNghiId == null || x.DotDeNghiId == hoSo.DotDeNghiId)
                        && (x.LinhVucId == null || x.LinhVucId == hoSo.LinhVucId))
            .OrderBy(x => x.ThuTuCap)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var soCap = cauHinh.Count;

        var buocPheDuyet = quyTrinh.DanhSachBuoc
            .Where(b => b.LoaiBuoc == LoaiBuoc.PheDuyet)
            .Select(b => b.Id)
            .ToHashSet();

        var daQuaCap = lichSu.Count(x => x.ThoiGianXuLy != null && buocPheDuyet.Contains(x.BuocId));

        // Cap dang xet = so cap da ky xong + 1, nhung khong vuot qua so cap da khai.
        var capHienTai = soCap == 0 ? 0 : Math.Min(daQuaCap + 1, soCap);
        var conCapCaoHon = soCap > 0 && daQuaCap + 1 < soCap;

        var keTiep = conCapCaoHon ? cauHinh[daQuaCap + 1] : null;

        return new Dictionary<string, object?>
        {
            [BienNguCanh.SoCapPheDuyet] = soCap,
            [BienNguCanh.CapPheDuyetHienTai] = capHienTai,
            [BienNguCanh.ConCapPheDuyetCaoHon] = conCapCaoHon,
            [BienNguCanh.DonViPheDuyetKeTiep] = keTiep?.DonViPheDuyetId
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

/// <summary>Mot nguoi co the xu ly buoc hien tai (dung cho o chon nguoi uy quyen).</summary>
public sealed record TacNhanBuocDto(Guid Id, string HoTen, string? ChucVu, string TenDangNhap);
