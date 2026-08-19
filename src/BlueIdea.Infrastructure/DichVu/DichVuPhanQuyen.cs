using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuanTri;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BlueIdea.Infrastructure.DichVu;

/// <summary>
/// Kiem tra quyen tren tung chuc nang va xac dinh pham vi du lieu (Muc 6 dac ta).
/// Ket qua duoc cache ngan (2 phut) vi duoc goi rat nhieu lan trong mot request.
/// </summary>
public sealed class DichVuPhanQuyen : IDichVuPhanQuyen
{
    private static readonly TimeSpan ThoiGianCache = TimeSpan.FromMinutes(2);

    private readonly IAppDbContext _db;
    private readonly INguoiDungHienTai _nguoiDung;
    private readonly IMemoryCache _cache;
    private readonly IDongHoHeThong _dongHo;

    public DichVuPhanQuyen(
        IAppDbContext db, INguoiDungHienTai nguoiDung, IMemoryCache cache, IDongHoHeThong dongHo)
    {
        _db = db;
        _nguoiDung = nguoiDung;
        _cache = cache;
        _dongHo = dongHo;
    }

    public async Task<bool> KiemTraQuyenAsync(
        Guid nguoiDungId, string maQuyen, CancellationToken ct = default)
    {
        if (_nguoiDung.Id == nguoiDungId && _nguoiDung.Quyen.Count > 0)
        {
            if (_nguoiDung.VaiTro.Contains(MaVaiTro.QuanTriHeThong))
            {
                return true;
            }

            return _nguoiDung.Quyen.Contains(maQuyen);
        }

        var quyen = await LayQuyenAsync(nguoiDungId, ct).ConfigureAwait(false);

        return quyen.Contains(maQuyen);
    }

    public async Task BatBuocCoQuyenAsync(
        string maQuyen, CancellationToken ct = default)
    {
        if (_nguoiDung.Id is null)
        {
            throw new NghiepVuException(Shared.KetQua.MaLoiHeThong.ChuaXacThuc,
                "Vui lòng đăng nhập để sử dụng chức năng này.");
        }

        var duQuyen = await KiemTraQuyenAsync(_nguoiDung.Id.Value, maQuyen, ct)
            .ConfigureAwait(false);

        if (!duQuyen)
        {
            throw new KhongCoQuyenException(maQuyen);
        }
    }

    public async Task<PhamViTruyCap> LayPhamViTruyCapAsync(
        Guid nguoiDungId, CancellationToken ct = default)
    {
        var khoa = $"pham-vi:{nguoiDungId}";
        if (_cache.TryGetValue<PhamViTruyCap>(khoa, out var cache) && cache is not null)
        {
            return cache;
        }

        var homNay = _dongHo.HomNay;

        var vaiTroIds = await _db.NguoiDungVaiTro.AsNoTracking()
            .Where(x => x.NguoiDungId == nguoiDungId
                        && (x.TuNgay == null || x.TuNgay <= homNay)
                        && (x.DenNgay == null || x.DenNgay >= homNay))
            .Select(x => x.VaiTroId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var phamVis = await _db.PhamViDuLieu.AsNoTracking()
            .Where(x => vaiTroIds.Contains(x.VaiTroId))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        PhamViTruyCap ketQua;

        /*
         * Quyen SANG_KIEN.XEM_TAT_CA mo pham vi ra toan bo ho so.
         *
         * Truoc day pham vi chi doc bang pham_vi_du_lieu, nen quyen nay khong co tac dung nao —
         * va no gay ra mot loi rat nang: bon vai tro trong chuoi xu ly (can bo tiep nhan, thu ky,
         * chu tich hoi dong, lanh dao) deu duoc cap quyen nay nhung pham vi lai la DON_VI_VA_CAP_DUOI.
         * Can bo tiep nhan ngoi o Phong Noi vu khong thay ho so cua Truong Tieu hoc Le Loi (thuoc
         * nhanh Phong GDDT), nen man hinh "Cho tiep nhan" cua ho TRONG TRON du he thong day ho so.
         *
         * Bo may quy trinh thi lai cho phep xu ly theo TAC NHAN CUA BUOC, khong theo pham vi — nen
         * kich ban kiem thu truyen thang id van chay tot va loi nay khong lo ra o dau ca.
         *
         * Pham vi nay chi duoc dung cho ho so, tep dinh kem va trung lap — dung dung nhom doi tuong
         * ma quyen XEM_TAT_CA noi toi.
         */
        var xemTatCaHoSo = await KiemTraQuyenAsync(nguoiDungId, MaQuyen.SangKienXemTatCa, ct: ct)
            .ConfigureAwait(false);

        if (xemTatCaHoSo)
        {
            ketQua = PhamViTruyCap.Tat_Ca;
        }
        else if (phamVis.Count == 0)
        {
            ketQua = PhamViTruyCap.CaNhan;
        }
        else if (phamVis.Any(x => x.LoaiPhamVi == LoaiPhamViDuLieu.ToanHeThong))
        {
            ketQua = PhamViTruyCap.Tat_Ca;
        }
        else
        {
            var donViIds = new HashSet<Guid>();
            var nguoiDung = await _db.NguoiDung.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == nguoiDungId, ct)
                .ConfigureAwait(false);

            foreach (var pv in phamVis)
            {
                switch (pv.LoaiPhamVi)
                {
                    case LoaiPhamViDuLieu.DonVi when nguoiDung?.DonViId is not null:
                        donViIds.Add(nguoiDung.DonViId.Value);
                        break;

                    case LoaiPhamViDuLieu.DonViVaCapDuoi when nguoiDung?.DonViId is not null:
                        foreach (var id in await LayDonViVaCapDuoiAsync(nguoiDung.DonViId.Value, ct)
                                     .ConfigureAwait(false))
                        {
                            donViIds.Add(id);
                        }

                        break;

                    case LoaiPhamViDuLieu.TuyChinh:
                        foreach (var id in pv.DonViIds)
                        {
                            donViIds.Add(id);
                        }

                        break;
                }
            }

            ketQua = donViIds.Count > 0
                ? new PhamViTruyCap { DonViIds = donViIds }
                : PhamViTruyCap.CaNhan;
        }

        _cache.Set(khoa, ketQua, ThoiGianCache);
        return ketQua;
    }

    private async Task<IReadOnlyList<Guid>> LayDonViVaCapDuoiAsync(Guid donViId, CancellationToken ct)
    {
        var donVi = await _db.DonVi.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == donViId, ct)
            .ConfigureAwait(false);

        if (donVi is null)
        {
            return Array.Empty<Guid>();
        }

        return await _db.DonVi.AsNoTracking()
            .Where(x => x.Id == donViId || x.Path.StartsWith(donVi.Path))
            .Select(x => x.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task<IReadOnlySet<string>> LayQuyenAsync(Guid nguoiDungId, CancellationToken ct)
    {
        var khoa = $"quyen:{nguoiDungId}";
        if (_cache.TryGetValue<IReadOnlySet<string>>(khoa, out var cache) && cache is not null)
        {
            return cache;
        }

        var homNay = _dongHo.HomNay;

        var vaiTroIds = await _db.NguoiDungVaiTro.AsNoTracking()
            .Where(x => x.NguoiDungId == nguoiDungId
                        && (x.TuNgay == null || x.TuNgay <= homNay)
                        && (x.DenNgay == null || x.DenNgay >= homNay))
            .Select(x => x.VaiTroId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var laQuanTri = await _db.VaiTro.AsNoTracking()
            .AnyAsync(x => vaiTroIds.Contains(x.Id) && x.Ma == MaVaiTro.QuanTriHeThong, ct)
            .ConfigureAwait(false);

        IReadOnlySet<string> ketQua;

        if (laQuanTri)
        {
            var tatCa = await _db.Quyen.AsNoTracking()
                .Select(x => x.Ma)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            ketQua = tatCa.ToHashSet();
        }
        else
        {
            var quyen = await _db.VaiTroQuyen.AsNoTracking()
                .Where(x => vaiTroIds.Contains(x.VaiTroId))
                .Join(_db.Quyen.AsNoTracking(), vq => vq.QuyenId, q => q.Id, (vq, q) => q.Ma)
                .Distinct()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            ketQua = quyen.ToHashSet();
        }

        _cache.Set(khoa, ketQua, ThoiGianCache);
        return ketQua;
    }
}
