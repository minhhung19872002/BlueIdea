using System.Diagnostics;
using BlueIdea.Ai.Nhung;
using BlueIdea.Ai.ThuatToan;

namespace BlueIdea.Ai.TrungLap;

/// <summary>
/// Pipeline phat hien trung lap / dao van chay hoan toan noi bo (dac ta Muc 5 - chuc nang 26):
/// 1. Loc tho: SimHash + Hamming, MinHash/LSH tren shingle 5-gram.
/// 2. So khop tinh: TF-IDF cosine + Jaccard (tu vung) va cosine embedding (ngu nghia).
/// 3. Tong hop: ty_le = heSoTuVung x tuVung + heSoNguNghia x nguNghia.
/// 4. Doi chieu tung cap doan van de UI highlight 2 cot.
/// </summary>
public interface IBoPhanTichTrungLap
{
    Task<KetQuaPhanTichTrungLap> PhanTichAsync(
        TaiLieuSoSanh taiLieuNguon,
        IReadOnlyList<TaiLieuSoSanh> khoDoiChieu,
        ThamSoTrungLap? thamSo = null,
        CancellationToken ct = default);
}

/// <inheritdoc />
public sealed class BoPhanTichTrungLap : IBoPhanTichTrungLap
{
    private readonly IBoNhungVanBan _boNhung;

    public BoPhanTichTrungLap(IBoNhungVanBan? boNhung = null)
        => _boNhung = boNhung ?? new BoNhungBamTuVung();

    public async Task<KetQuaPhanTichTrungLap> PhanTichAsync(
        TaiLieuSoSanh taiLieuNguon,
        IReadOnlyList<TaiLieuSoSanh> khoDoiChieu,
        ThamSoTrungLap? thamSo = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(taiLieuNguon);
        ArgumentNullException.ThrowIfNull(khoDoiChieu);
        thamSo ??= ThamSoTrungLap.MacDinh;

        var dongHo = Stopwatch.StartNew();

        // Loai chinh no ra khoi kho doi chieu.
        var ungVien = khoDoiChieu
            .Where(t => t.SangKienId != taiLieuNguon.SangKienId)
            .ToList();

        if (ungVien.Count == 0)
        {
            dongHo.Stop();
            return new KetQuaPhanTichTrungLap
            {
                SangKienId = taiLieuNguon.SangKienId,
                TongSoDoiChieu = 0,
                TyLeCaoNhat = 0m,
                MucCanhBao = Domain.Chung.MucCanhBaoTrungLap.AnToan,
                PhienBanThuatToan = thamSo.PhienBanThuatToan,
                TenMoHinhNhung = _boNhung.TenMoHinh,
                ThoiGianXuLyMs = dongHo.ElapsedMilliseconds
            };
        }

        // --- Buoc 1: loc tho (recall cao, chi phi thap) ---
        var daLoc = LocTho(taiLieuNguon, ungVien, thamSo);

        // --- Buoc 2: chuan bi mo hinh TF-IDF tren corpus (nguon + ung vien) ---
        var corpus = new List<string> { taiLieuNguon.ToanVan };
        corpus.AddRange(daLoc.Select(t => t.ToanVan));
        var tfIdf = MoHinhTfIdf.HuanLuyen(corpus);

        // --- Buoc 3: nhung ngu nghia (chay noi bo) ---
        await BaoDamEmbeddingAsync(taiLieuNguon, ct).ConfigureAwait(false);
        foreach (var tl in daLoc)
        {
            ct.ThrowIfCancellationRequested();
            await BaoDamEmbeddingAsync(tl, ct).ConfigureAwait(false);
        }

        // --- Buoc 4: so khop tinh tung cap ---
        var chiTiet = new List<ChiTietDoiChieu>();
        foreach (var tl in daLoc)
        {
            ct.ThrowIfCancellationRequested();

            var tuVung = TinhTuVung(tfIdf, taiLieuNguon, tl);
            var nguNghia = TinhNguNghia(taiLieuNguon, tl);

            var tong = thamSo.HeSoTuVung * tuVung + thamSo.HeSoNguNghia * nguNghia;
            var tyLe = LamTronPhanTram(tong);

            if (tyLe < thamSo.NguongLuuChiTiet)
            {
                continue;
            }

            var doanTrung = DoiChieuDoanVan(taiLieuNguon, tl, thamSo);

            chiTiet.Add(new ChiTietDoiChieu
            {
                SangKienDoiChieuId = tl.SangKienId,
                MaHoSo = tl.MaHoSo,
                TenSangKien = tl.TenSangKien,
                TyLeTuongDong = tyLe,
                TyLeTuVung = LamTronPhanTram(tuVung),
                TyLeNguNghia = LamTronPhanTram(nguNghia),
                SoDoanTrung = doanTrung.Count,
                CacDoanTrung = doanTrung
            });
        }

        chiTiet = chiTiet.OrderByDescending(c => c.TyLeTuongDong).ToList();
        var caoNhat = chiTiet.Count > 0 ? chiTiet[0].TyLeTuongDong : 0m;

        dongHo.Stop();

        return new KetQuaPhanTichTrungLap
        {
            SangKienId = taiLieuNguon.SangKienId,
            TongSoDoiChieu = ungVien.Count,
            TyLeCaoNhat = caoNhat,
            MucCanhBao = KetQuaPhanTichTrungLap.TinhMucCanhBao(caoNhat, thamSo),
            PhienBanThuatToan = thamSo.PhienBanThuatToan,
            TenMoHinhNhung = _boNhung.TenMoHinh,
            ThoiGianXuLyMs = dongHo.ElapsedMilliseconds,
            ChiTiet = chiTiet
        };
    }

    /// <summary>
    /// Loc tho bang SimHash muc tai lieu va MinHash/LSH tren shingle.
    /// Uu tien recall: tai lieu chi can khop 1 trong 2 tin hieu la duoc giu lai.
    /// </summary>
    private static List<TaiLieuSoSanh> LocTho(
        TaiLieuSoSanh nguon, List<TaiLieuSoSanh> ungVien, ThamSoTrungLap thamSo)
    {
        var minHash = new MinHash();
        var simHashNguon = SimHash.Tinh(nguon.ToanVan);
        var chuKyNguon = minHash.TinhChuKyTuVanBan(nguon.ToanVan);
        var khoaNguon = minHash.SinhKhoaLsh(chuKyNguon).ToHashSet(StringComparer.Ordinal);

        var chamDiem = new List<(TaiLieuSoSanh TaiLieu, double Diem)>();

        foreach (var tl in ungVien)
        {
            var simHashDich = SimHash.Tinh(tl.ToanVan);
            var ganTheoSimHash = SimHash.LaUngVien(simHashNguon, simHashDich, thamSo.NguongHamming);

            var chuKyDich = minHash.TinhChuKyTuVanBan(tl.ToanVan);
            var khopBand = minHash.SinhKhoaLsh(chuKyDich).Any(khoaNguon.Contains);
            var uocLuongJaccard = MinHash.UocLuongJaccard(chuKyNguon, chuKyDich);

            // Diem loc tho de xep hang khi so ung vien vuot gioi han.
            var diem = Math.Max(SimHash.DoTuongDong(simHashNguon, simHashDich), uocLuongJaccard);

            if (ganTheoSimHash || khopBand || uocLuongJaccard > 0d)
            {
                chamDiem.Add((tl, diem));
            }
        }

        // Neu loc tho khong giu lai gi (van ban qua khac nhau) van doi chieu tinh voi top N
        // de tranh bo sot truong hop dien dat lai hoan toan.
        if (chamDiem.Count == 0)
        {
            chamDiem = ungVien.Select(t => (t, 0d)).ToList();
        }

        return chamDiem
            .OrderByDescending(c => c.Diem)
            .Take(Math.Max(thamSo.SoUngVienToiDa, 1))
            .Select(c => c.TaiLieu)
            .ToList();
    }

    /// <summary>Thanh phan tu vung = trung binh cua TF-IDF cosine va Jaccard tren shingle 5-gram.</summary>
    private static decimal TinhTuVung(MoHinhTfIdf tfIdf, TaiLieuSoSanh nguon, TaiLieuSoSanh dich)
    {
        var cosine = tfIdf.Cosine(nguon.ToanVan, dich.ToanVan);
        var jaccard = DoTuongDong.JaccardVanBan(nguon.ToanVan, dich.ToanVan);
        return (decimal)((cosine + jaccard) / 2d);
    }

    /// <summary>
    /// Thanh phan ngu nghia = cosine embedding muc tai lieu, ket hop do phu doan van
    /// (trung binh cosine cao nhat cua tung doan nguon) de nhay hon voi trung lap cuc bo.
    /// </summary>
    private static decimal TinhNguNghia(TaiLieuSoSanh nguon, TaiLieuSoSanh dich)
    {
        var mucTaiLieu = nguon.EmbeddingTaiLieu is null || dich.EmbeddingTaiLieu is null
            ? 0d
            : Math.Max(0d, DoTuongDong.CosineDac(nguon.EmbeddingTaiLieu, dich.EmbeddingTaiLieu));

        if (nguon.CacDoan.Count == 0 || dich.CacDoan.Count == 0)
        {
            return (decimal)mucTaiLieu;
        }

        var tongTotNhat = 0d;
        var soDoan = 0;

        foreach (var doanNguon in nguon.CacDoan)
        {
            if (doanNguon.Embedding is null)
            {
                continue;
            }

            var totNhat = 0d;
            foreach (var doanDich in dich.CacDoan)
            {
                if (doanDich.Embedding is null)
                {
                    continue;
                }

                var cosine = DoTuongDong.CosineDac(doanNguon.Embedding, doanDich.Embedding);
                totNhat = Math.Max(totNhat, cosine);
            }

            tongTotNhat += Math.Max(0d, totNhat);
            soDoan++;
        }

        var mucDoan = soDoan == 0 ? 0d : tongTotNhat / soDoan;
        return (decimal)Math.Max(mucTaiLieu, mucDoan);
    }

    /// <summary>Tim cac cap doan van trung nhau de highlight doi chieu 2 cot tren UI.</summary>
    private static List<CapDoanTrung> DoiChieuDoanVan(
        TaiLieuSoSanh nguon, TaiLieuSoSanh dich, ThamSoTrungLap thamSo)
    {
        var ketQua = new List<CapDoanTrung>();

        foreach (var doanNguon in nguon.CacDoan)
        {
            DoanVanSoSanh? totNhat = null;
            var diemTotNhat = 0d;

            foreach (var doanDich in dich.CacDoan)
            {
                // Loc tho o muc doan bang SimHash de giam so phep so sanh nang.
                var gan = SimHash.LaUngVien(doanNguon.SimHash, doanDich.SimHash, thamSo.NguongHamming + 6);

                var jaccard = DoTuongDong.Jaccard(
                    Shingle.TaoTapShingle(doanNguon.NoiDungChuanHoa, 3),
                    Shingle.TaoTapShingle(doanDich.NoiDungChuanHoa, 3));

                var cosine = doanNguon.Embedding is not null && doanDich.Embedding is not null
                    ? Math.Max(0d, DoTuongDong.CosineDac(doanNguon.Embedding, doanDich.Embedding))
                    : 0d;

                var diem = gan ? Math.Max(jaccard, cosine) : 0.4 * jaccard + 0.6 * cosine;

                if (diem > diemTotNhat)
                {
                    diemTotNhat = diem;
                    totNhat = doanDich;
                }
            }

            if (totNhat is not null && diemTotNhat >= thamSo.NguongDoanTrung)
            {
                ketQua.Add(new CapDoanTrung(
                    doanNguon.ChiMuc,
                    doanNguon.NoiDung,
                    totNhat.ChiMuc,
                    totNhat.NoiDung,
                    LamTronPhanTram((decimal)diemTotNhat),
                    doanNguon.ViTriBatDau,
                    doanNguon.ViTriKetThuc));
            }
        }

        return ketQua;
    }

    private async Task BaoDamEmbeddingAsync(TaiLieuSoSanh taiLieu, CancellationToken ct)
    {
        taiLieu.EmbeddingTaiLieu ??= await _boNhung.TaoVectorAsync(taiLieu.ToanVan, ct).ConfigureAwait(false);

        foreach (var doan in taiLieu.CacDoan.Where(d => d.Embedding is null))
        {
            doan.Embedding = await _boNhung
                .TaoVectorAsync(doan.NoiDungChuanHoa, ct)
                .ConfigureAwait(false);
        }
    }

    private static decimal LamTronPhanTram(decimal tyLe0Den1)
        => Math.Round(Math.Clamp(tyLe0Den1, 0m, 1m) * 100m, 2, MidpointRounding.AwayFromZero);
}
