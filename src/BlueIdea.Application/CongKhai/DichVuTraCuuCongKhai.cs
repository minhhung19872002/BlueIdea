using BlueIdea.Application.Chung;
using BlueIdea.Domain.SangKien;
using BlueIdea.Shared.KetQua;
using BlueIdea.Shared.TiengViet;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.CongKhai;

/// <summary>Mot dong ket qua tren cong tra cuu cong khai.</summary>
public sealed record SangKienCongKhaiDto(
    Guid Id,
    string MaHoSo,
    string TenSangKien,
    string? TacGiaChinh,
    int? Nam,
    string? SoQuyetDinh,
    string? TenLinhVuc,
    string? TenDonVi);

/// <summary>Ba con so tren dai thong ke dau trang cong khai.</summary>
public sealed record ThongKeCongKhaiDto(int SoSangKien, int SoNam, int SoQuyetDinh);

/// <summary>Mot chip loc linh vuc kem so luong sang kien cong khai thuoc linh vuc do.</summary>
public sealed record LinhVucCongKhaiDto(Guid Id, string Ten, int SoLuong);

/// <summary>
/// Chuc nang 37 — Cong tra cuu cong khai, KHONG can dang nhap.
///
/// Tach hoan toan khoi <see cref="SangKien.DichVuTruyVanSangKien"/> chu khong dung chung mot
/// endpoint co co "chiCongKhai": endpoint dung chung nghia la mot lan sua bo loc pham vi du
/// lieu cho nguoi dung noi bo cung co the vo tinh ho ra du lieu cho ca Internet. O day truy van
/// duoc viet rieng, dieu kien cong khai la co dinh trong ma nguon va khong nhan tham so nao co
/// the noi long no.
///
/// Chi 5 truong trong ban thiet ke duoc tra ve. Diem so, ty le trung lap, trang thai xu ly,
/// nguoi danh gia... deu la thong tin noi bo va khong bao gio roi khoi lop nay.
/// </summary>
public sealed class DichVuTraCuuCongKhai
{
    private readonly IAppDbContext _db;

    public DichVuTraCuuCongKhai(IAppDbContext db) => _db = db;

    /// <summary>
    /// Dieu kien cong khai — dung o CA ba truy van ben duoi de ba con so tren dai thong ke
    /// luon khop voi so dong that su liet ke duoc.
    ///
    /// Chi hai dieu kien: co-cong-khai va ket qua Dat. KHONG rang buoc them
    /// <c>DaCongBoKetQua</c> du co-cong-khai trong luong that luon duoc bat boi thao tac cong
    /// bo: co <c>CongKhai</c> la thu quan tri vien tu tay bat, neu trang cong khai doi them
    /// mot dieu kien nua thi ho bat co xong ma khong thay gi hien ra va khong co cho nao giai
    /// thich tai sao. Dieu kien o day cung phai TRUNG voi bo loc cong khai trong
    /// <see cref="SangKien.DichVuTruyVanSangKien"/> de hai man hinh khong bao hai so khac nhau.
    /// </summary>
    private IQueryable<HoSoSangKien> ChiHoSoCongKhai()
        => _db.SangKien.AsNoTracking()
            .Where(x => x.CongKhai && x.KetQua == KetQuaXetDuyetGiaTri.Dat);

    /// <summary>Danh sach sang kien da cong nhan, co phan trang.</summary>
    public async Task<PagedResult<SangKienCongKhaiDto>> TimAsync(
        string? tuKhoa, Guid? linhVucId, int trang, int soDong, CancellationToken ct = default)
    {
        // Chan so dong: cong khai thi ai cung goi duoc, khong gioi han thi mot request
        // "soDong=100000" du de keo sap CSDL.
        soDong = Math.Clamp(soDong, 1, 100);
        trang = Math.Max(trang, 1);

        var truyVan = ChiHoSoCongKhai();

        if (linhVucId.HasValue)
        {
            truyVan = truyVan.Where(x => x.LinhVucId == linhVucId.Value);
        }

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var khongDau = VanBanTiengViet.TaoKhongDau(tuKhoa);
            var hoa = tuKhoa.Trim().ToUpperInvariant();

            // Tim ca theo so quyet dinh — o nhap tren ban thiet ke ghi ro
            // "Nhap ten sang kien, tac gia, so quyet dinh...".
            var hoSoIdTheoQuyetDinh = _db.QuyetDinh.AsNoTracking()
                .Where(q => q.SoQuyetDinh.ToUpper().Contains(hoa))
                .Select(q => (Guid?)q.Id);

            truyVan = truyVan.Where(x =>
                x.TenKhongDau.Contains(khongDau)
                || x.MaHoSo.ToUpper().Contains(hoa)
                || x.DanhSachTacGia.Any(t => t.HoTen.ToUpper().Contains(hoa))
                || (x.QuyetDinhId.HasValue && hoSoIdTheoQuyetDinh.Contains(x.QuyetDinhId)));
        }

        var tongSo = await truyVan.CountAsync(ct).ConfigureAwait(false);

        var tho = await truyVan
            .OrderByDescending(x => x.NgayCongNhan)
            .ThenByDescending(x => x.NgayNop)
            .ThenBy(x => x.MaHoSo)
            .Skip((trang - 1) * soDong)
            .Take(soDong)
            .Select(x => new
            {
                x.Id,
                x.MaHoSo,
                x.TenSangKien,
                x.LinhVucId,
                x.DonViId,
                x.QuyetDinhId,
                x.NgayCongNhan,
                x.NgayNop,
                TacGiaChinh = x.DanhSachTacGia
                    .Where(t => t.LaTacGiaChinh)
                    .Select(t => t.HoTen)
                    .FirstOrDefault()
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var linhVucIds = tho.Select(x => x.LinhVucId).Distinct().ToList();
        var donViIds = tho.Where(x => x.DonViId.HasValue).Select(x => x.DonViId!.Value).Distinct().ToList();
        var quyetDinhIds = tho.Where(x => x.QuyetDinhId.HasValue)
            .Select(x => x.QuyetDinhId!.Value).Distinct().ToList();

        var tenLinhVuc = await _db.LinhVuc.AsNoTracking()
            .Where(x => linhVucIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct).ConfigureAwait(false);

        var tenDonVi = await _db.DonVi.AsNoTracking()
            .Where(x => donViIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct).ConfigureAwait(false);

        var soQuyetDinh = await _db.QuyetDinh.AsNoTracking()
            .Where(x => quyetDinhIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.SoQuyetDinh, ct).ConfigureAwait(false);

        var duLieu = tho.Select(x => new SangKienCongKhaiDto(
            x.Id,
            x.MaHoSo,
            x.TenSangKien,
            x.TacGiaChinh,
            // Nam cong nhan la moc nguoi dan quan tam; ho so cu chua kip gan ngay cong nhan
            // thi lui ve nam nop de o cot "Nam" khong bi trong.
            x.NgayCongNhan?.Year ?? x.NgayNop?.Year,
            x.QuyetDinhId.HasValue && soQuyetDinh.TryGetValue(x.QuyetDinhId.Value, out var so) ? so : null,
            tenLinhVuc.GetValueOrDefault(x.LinhVucId),
            x.DonViId.HasValue ? tenDonVi.GetValueOrDefault(x.DonViId.Value) : null)).ToList();

        return new PagedResult<SangKienCongKhaiDto>(duLieu, tongSo, trang, soDong);
    }

    /// <summary>Ba con so tren dai thong ke: tong sang kien, so nam co du lieu, so quyet dinh.</summary>
    public async Task<ThongKeCongKhaiDto> ThongKeAsync(CancellationToken ct = default)
    {
        var moc = await ChiHoSoCongKhai()
            .Select(x => new { x.NgayCongNhan, x.NgayNop, x.QuyetDinhId })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var nam = moc
            .Select(x => x.NgayCongNhan?.Year ?? x.NgayNop?.Year)
            .Where(x => x.HasValue)
            .Distinct()
            .Count();

        // Dem quyet dinh THEO ho so cong khai chu khong dem ca bang quyet dinh: quyet dinh chua
        // cong bo, hoac chi chua ho so khong cong khai, khong duoc tinh vao con so hien cho dan.
        var soQuyetDinh = moc
            .Where(x => x.QuyetDinhId.HasValue)
            .Select(x => x.QuyetDinhId!.Value)
            .Distinct()
            .Count();

        return new ThongKeCongKhaiDto(moc.Count, nam, soQuyetDinh);
    }

    /// <summary>Chip loc linh vuc — chi liet ke linh vuc that su co sang kien cong khai.</summary>
    public async Task<IReadOnlyList<LinhVucCongKhaiDto>> LinhVucAsync(CancellationToken ct = default)
    {
        var dem = await ChiHoSoCongKhai()
            .GroupBy(x => x.LinhVucId)
            .Select(g => new { LinhVucId = g.Key, SoLuong = g.Count() })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var ids = dem.Select(x => x.LinhVucId).ToList();

        var ten = await _db.LinhVuc.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Ten, ct)
            .ConfigureAwait(false);

        return dem
            .Select(x => new LinhVucCongKhaiDto(
                x.LinhVucId, ten.GetValueOrDefault(x.LinhVucId) ?? "Khác", x.SoLuong))
            .OrderByDescending(x => x.SoLuong)
            .ThenBy(x => x.Ten, StringComparer.CurrentCulture)
            .ToList();
    }
}
