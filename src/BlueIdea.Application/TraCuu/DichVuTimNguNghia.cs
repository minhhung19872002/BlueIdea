using BlueIdea.Ai.Nhung;
using BlueIdea.Ai.ThuatToan;
using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Shared.KetQua;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlueIdea.Application.TraCuu;

public sealed record KetQuaTimNguNghia(
    Guid SangKienId,
    string MaHoSo,
    string TenSangKien,
    string? TenTacGiaChinh,
    string? TenDonVi,
    string? TenLinhVuc,
    int? NamCongNhan,
    string? KetQua,
    decimal DoTuongDong,
    string DoanKhopNhat);

/// <summary>
/// Chuc nang 26, 37 - Tim kiem NGU NGHIA tren kho sang kien.
///
/// Khac tim theo tu khoa: cau hoi "giai phap tiet kiem dien o truong hoc" van tim ra sang kien
/// dat ten "Ung dung cam bien anh sang giam tieu thu nang luong lop hoc" du khong trung tu nao.
///
/// Vector nhung sinh HOAN TOAN NOI BO (xem docs/ADR/0001-ai-noi-bo.md) - khong goi API AI ben
/// thu ba, dung rang buoc cua E-HSMT.
/// </summary>
public sealed class DichVuTimNguNghia
{
    /// <summary>
    /// Duoi nguong nay coi nhu khong lien quan.
    ///
    /// GIOI HAN HIEN TAI: bo nhung dang dung la "hashing trick" tren tu vung (xem
    /// <c>BoNhungBamTuVung</c>) — no bat duoc quan he TU VUNG chu chua bat duoc quan he NGU NGHIA
    /// that (vi du "tiet kiem dien" ~ "cam bien anh sang"). Vi vay nguong phai de thap hon nhieu
    /// so voi mo hinh sentence-transformer. Khi nap duoc mo hinh ONNX noi bo thi nang nguong len.
    /// </summary>
    private const double NguongToiThieu = 0.2;

    /// <summary>Chan so doan nap vao bo nho moi lan tim.</summary>
    private const int SoDoanToiDa = 20_000;

    private readonly IAppDbContext _db;
    private readonly IBoNhungVanBan _boNhung;
    private readonly IDichVuPhanQuyen _phanQuyen;
    private readonly INguoiDungHienTai _nguoiDung;
    private readonly ILogger<DichVuTimNguNghia> _logger;

    public DichVuTimNguNghia(
        IAppDbContext db, IBoNhungVanBan boNhung, IDichVuPhanQuyen phanQuyen,
        INguoiDungHienTai nguoiDung, ILogger<DichVuTimNguNghia> logger)
    {
        _db = db;
        _boNhung = boNhung;
        _phanQuyen = phanQuyen;
        _nguoiDung = nguoiDung;
        _logger = logger;
    }

    public async Task<IReadOnlyList<KetQuaTimNguNghia>> TimAsync(
        string cauHoi, int soKetQua = 20, Guid? linhVucId = null, int? nam = null,
        CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.SangKienXem, ct: ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(cauHoi) || cauHoi.Trim().Length < 3)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Nội dung tìm kiếm phải có ít nhất 3 ký tự.");
        }

        var vectorHoi = await _boNhung.TaoVectorAsync(cauHoi, ct).ConfigureAwait(false);

        // Phạm vi dữ liệu: người dùng chỉ tìm được trong những hồ sơ họ vốn được xem.
        var hoSoChoPhep = await LayHoSoChoPhepAsync(linhVucId, nam, ct).ConfigureAwait(false);

        if (hoSoChoPhep.Count == 0)
        {
            return Array.Empty<KetQuaTimNguNghia>();
        }

        /*
         * Chỉ so với vector do CHÍNH mô hình đang chạy sinh ra.
         *
         * Vector của hai mô hình khác nhau nằm trong hai không gian khác nhau: đem cosine chúng
         * với nhau vẫn ra một con số, chỉ là con số đó không mang nghĩa gì. Nếu không lọc, ngày
         * đơn vị nạp mô hình ONNX mới là tìm ngữ nghĩa lặng lẽ trả kết quả sai.
         */
        var tenMoHinh = _boNhung.TenMoHinh;

        var doan = await _db.SangKienDoanVan.AsNoTracking()
            .Where(x => hoSoChoPhep.Contains(x.SangKienId)
                        && x.Embedding != null
                        && x.MoHinhNhung == tenMoHinh)
            .Select(x => new { x.SangKienId, x.NoiDung, x.Embedding })
            .Take(SoDoanToiDa)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (doan.Count == 0)
        {
            var soVectorMoHinhKhac = await _db.SangKienDoanVan.AsNoTracking()
                .CountAsync(x => x.Embedding != null && x.MoHinhNhung != tenMoHinh, ct)
                .ConfigureAwait(false);

            if (soVectorMoHinhKhac > 0)
            {
                _logger.LogWarning(
                    "Có {SoDoan} đoạn văn còn vector của mô hình khác '{MoHinhDangChay}' — "
                    + "công việc nền 'nhung-lai-doan-van' sẽ nhúng lại, tìm ngữ nghĩa tạm rỗng.",
                    soVectorMoHinhKhac, tenMoHinh);
            }
            else
            {
                _logger.LogInformation(
                    "Chưa có đoạn văn nào có vector nhúng — tìm ngữ nghĩa trả về rỗng.");
            }

            return Array.Empty<KetQuaTimNguNghia>();
        }

        // Lấy đoạn khớp nhất của MỖI hồ sơ, không phải mọi đoạn: một hồ sơ dài sẽ chiếm hết
        // trang kết quả nếu xếp hạng theo từng đoạn.
        var totNhatTheoHoSo = new Dictionary<Guid, (double Diem, string Doan)>();

        foreach (var d in doan)
        {
            var diem = DoTuongDong.CosineDac(vectorHoi, d.Embedding!);

            if (diem < NguongToiThieu)
            {
                continue;
            }

            if (!totNhatTheoHoSo.TryGetValue(d.SangKienId, out var hienTai) || diem > hienTai.Diem)
            {
                totNhatTheoHoSo[d.SangKienId] = (diem, d.NoiDung);
            }
        }

        if (totNhatTheoHoSo.Count == 0)
        {
            return Array.Empty<KetQuaTimNguNghia>();
        }

        var xepHang = totNhatTheoHoSo
            .OrderByDescending(x => x.Value.Diem)
            .Take(Math.Clamp(soKetQua, 1, 100))
            .ToList();

        var ids = xepHang.Select(x => x.Key).ToList();

        var thongTin = await _db.SangKien.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                x.MaHoSo,
                x.TenSangKien,
                x.KetQua,
                x.NgayCongNhan,
                TacGia = _db.SangKienTacGia
                    .Where(t => t.SangKienId == x.Id && t.LaTacGiaChinh)
                    .Select(t => t.HoTen).FirstOrDefault(),
                DonVi = _db.DonVi.Where(d => d.Id == x.DonViId).Select(d => d.Ten).FirstOrDefault(),
                LinhVuc = _db.LinhVuc.Where(l => l.Id == x.LinhVucId).Select(l => l.Ten).FirstOrDefault()
            })
            .ToDictionaryAsync(x => x.Id, ct)
            .ConfigureAwait(false);

        return xepHang
            .Where(x => thongTin.ContainsKey(x.Key))
            .Select(x =>
            {
                var t = thongTin[x.Key];

                return new KetQuaTimNguNghia(
                    x.Key,
                    t.MaHoSo,
                    t.TenSangKien,
                    t.TacGia,
                    t.DonVi,
                    t.LinhVuc,
                    t.NgayCongNhan?.Year,
                    t.KetQua,
                    Math.Round((decimal)x.Value.Diem * 100, 2, MidpointRounding.AwayFromZero),
                    CatDoan(x.Value.Doan));
            })
            .ToList();
    }

    /// <summary>Danh sách hồ sơ người dùng hiện tại được phép thấy, đã áp bộ lọc.</summary>
    private async Task<List<Guid>> LayHoSoChoPhepAsync(
        Guid? linhVucId, int? nam, CancellationToken ct)
    {
        var truyVan = _db.SangKien.AsNoTracking()
            .Where(x => x.TrangThaiTong != TrangThaiTongHoSo.Nhap);

        if (linhVucId.HasValue)
        {
            truyVan = truyVan.Where(x => x.LinhVucId == linhVucId.Value);
        }

        if (nam.HasValue)
        {
            truyVan = truyVan.Where(x =>
                x.NgayCongNhan != null && x.NgayCongNhan.Value.Year == nam.Value);
        }

        // Không có quyền xem toàn hệ thống thì chỉ thấy hồ sơ trong phạm vi đơn vị của mình
        // và hồ sơ đã công khai.
        if (_nguoiDung.Id is { } nguoiDungId
            && !_nguoiDung.Quyen.Contains(MaQuyen.SangKienXemTatCa)
            && !_nguoiDung.VaiTro.Contains(MaVaiTro.QuanTriHeThong))
        {
            var phamVi = await _phanQuyen.LayPhamViTruyCapAsync(nguoiDungId, ct).ConfigureAwait(false);

            if (!phamVi.ToanHeThong)
            {
                var donVi = phamVi.DonViIds.ToList();

                truyVan = phamVi.ChiCaNhan
                    ? truyVan.Where(x =>
                        x.CongKhai
                        || _db.SangKienTacGia.Any(t => t.SangKienId == x.Id && t.NguoiDungId == nguoiDungId))
                    : truyVan.Where(x =>
                        x.CongKhai
                        || (x.DonViId != null && donVi.Contains(x.DonViId.Value)));
            }
        }

        return await truyVan.Select(x => x.Id).ToListAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Cắt đoạn trích cho vừa một dòng kết quả.</summary>
    private static string CatDoan(string doan)
    {
        const int doDaiToiDa = 300;

        var sach = doan.Replace('\n', ' ').Replace('\r', ' ').Trim();

        return sach.Length <= doDaiToiDa ? sach : $"{sach[..doDaiToiDa]}…";
    }
}
