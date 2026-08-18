using BlueIdea.Application.Chung;
using BlueIdea.Application.DanhGia;
using BlueIdea.Application.DanhMuc;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.TieuChi;
using BlueIdea.Scoring;
using BlueIdea.Shared.KetQua;
using BlueIdea.Shared.TiengViet;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.TieuChi;

/// <summary>Chuc nang 17-18: cau hinh bo tieu chi dong (cay 2 cap) + muc cong nhan.</summary>
public sealed class DichVuTieuChi : DichVuDanhMucCoSo<BoTieuChi>
{
    private readonly IBoTinhDiem _tinhDiem;

    public DichVuTieuChi(
        IAppDbContext db, IDichVuPhanQuyen phanQuyen, IDongHoHeThong dongHo, IBoTinhDiem tinhDiem)
        : base(db, phanQuyen, dongHo)
        => _tinhDiem = tinhDiem;

    protected override DbSet<BoTieuChi> BangDuLieu => Db.BoTieuChi;

    protected override string TenDanhMuc => "Bộ tiêu chí";

    protected override IQueryable<BoTieuChi> TaoTruyVanChiTiet()
        => Db.BoTieuChi.AsNoTracking()
            .Include(x => x.DanhSachNhom).ThenInclude(n => n.DanhSachTieuChi).ThenInclude(t => t.DanhSachMucDiem)
            .Include(x => x.DanhSachMucCongNhan);

    /// <summary>Lay bo tieu chi day du de render cay 2 cap tren man hinh cau hinh.</summary>
    public async Task<BoTieuChiDto> LayChiTietDayDuAsync(Guid id, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.TieuChiXem, id, ct).ConfigureAwait(false);

        var bo = await TaoTruyVanChiTiet()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException(TenDanhMuc, id);

        return DichVuDanhGia.ChuyenDoiBoTieuChi(bo);
    }

    /// <summary>Kiem tra tinh hop le cua bo tieu chi (trong so, khoang diem, tong diem).</summary>
    public async Task<IReadOnlyList<string>> KiemTraAsync(Guid id, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.TieuChiXem, id, ct).ConfigureAwait(false);

        var bo = await TaoTruyVanChiTiet()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException(TenDanhMuc, id);

        return _tinhDiem.KiemTraBoTieuChi(bo);
    }

    /// <summary>Luu toan bo cay nhom/tieu chi trong mot giao dich (keo tha sap xep tren UI).</summary>
    public async Task LuuCayTieuChiAsync(
        Guid boTieuChiId, IReadOnlyList<NhomTieuChiLuuDto> nhomMoi, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.TieuChiCauHinh, boTieuChiId, ct).ConfigureAwait(false);

        var bo = await Db.BoTieuChi
            .Include(x => x.DanhSachNhom).ThenInclude(n => n.DanhSachTieuChi).ThenInclude(t => t.DanhSachMucDiem)
            .Include(x => x.DanhSachMucCongNhan)
            .FirstOrDefaultAsync(x => x.Id == boTieuChiId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException(TenDanhMuc, boTieuChiId);

        await BatBuocChuaSuDungAsync(boTieuChiId, ct).ConfigureAwait(false);

        foreach (var n in bo.DanhSachNhom)
        {
            n.DaXoa = true;
            foreach (var t in n.DanhSachTieuChi)
            {
                t.DaXoa = true;
                foreach (var m in t.DanhSachMucDiem)
                {
                    m.DaXoa = true;
                }
            }
        }

        var thuTuNhom = 1;
        foreach (var n in nhomMoi)
        {
            var nhom = new NhomTieuChi
            {
                Id = n.Id == Guid.Empty ? Guid.NewGuid() : n.Id,
                BoTieuChiId = bo.Id,
                Ma = string.IsNullOrWhiteSpace(n.Ma) ? $"NHOM_{thuTuNhom}" : n.Ma,
                Ten = n.Ten,
                TenKhongDau = VanBanTiengViet.TaoKhongDau(n.Ten),
                MoTa = n.MoTa,
                TrongSo = n.TrongSo,
                DiemToiDa = n.DiemToiDa,
                ThuTu = thuTuNhom++
            };

            var thuTuTieuChi = 1;
            foreach (var t in n.DanhSachTieuChi)
            {
                var tieuChi = new TieuChiChamDiem
                {
                    Id = t.Id == Guid.Empty ? Guid.NewGuid() : t.Id,
                    NhomTieuChiId = nhom.Id,
                    Ma = string.IsNullOrWhiteSpace(t.Ma) ? $"{nhom.Ma}_{thuTuTieuChi}" : t.Ma,
                    Ten = t.Ten,
                    TenKhongDau = VanBanTiengViet.TaoKhongDau(t.Ten),
                    MoTa = t.MoTa,
                    DiemToiDa = t.DiemToiDa,
                    DiemToiThieu = t.DiemToiThieu,
                    TrongSo = t.TrongSo,
                    KieuNhap = t.KieuNhap,
                    BuocNhay = t.BuocNhay,
                    BatBuocNhanXet = t.BatBuocNhanXet,
                    HuongDanCham = t.HuongDanCham,
                    ThuTu = thuTuTieuChi++
                };

                var thuTuMuc = 1;
                foreach (var m in t.DanhSachMucDiem)
                {
                    tieuChi.DanhSachMucDiem.Add(new TieuChiMucDiem
                    {
                        Id = m.Id == Guid.Empty ? Guid.NewGuid() : m.Id,
                        TieuChiId = tieuChi.Id,
                        Ten = m.Ten,
                        Diem = m.Diem,
                        MoTa = m.MoTa,
                        ThuTu = thuTuMuc++
                    });
                }

                nhom.DanhSachTieuChi.Add(tieuChi);
            }

            bo.DanhSachNhom.Add(nhom);
        }

        var loi = _tinhDiem.KiemTraBoTieuChi(bo);
        if (loi.Count > 0)
        {
            throw new NghiepVuException(MaLoiHeThong.BoTieuChiKhongHopLe, string.Join(" ", loi));
        }

        await Db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Luu danh sach muc cong nhan, kiem tra khoang diem khong chong lan.</summary>
    public async Task LuuMucCongNhanAsync(
        Guid boTieuChiId, IReadOnlyList<MucCongNhanLuuDto> danhSach, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.TieuChiCauHinh, boTieuChiId, ct).ConfigureAwait(false);

        var cu = await Db.MucCongNhan
            .Where(x => x.BoTieuChiId == boTieuChiId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var m in cu)
        {
            m.DaXoa = true;
        }

        var thuTu = 1;
        foreach (var m in danhSach.OrderBy(x => x.DiemTu))
        {
            Db.MucCongNhan.Add(new MucCongNhan
            {
                Id = m.Id == Guid.Empty ? Guid.NewGuid() : m.Id,
                BoTieuChiId = boTieuChiId,
                Ma = m.Ma,
                Ten = m.Ten,
                TenKhongDau = VanBanTiengViet.TaoKhongDau(m.Ten),
                DiemTu = m.DiemTu,
                DiemDen = m.DiemDen,
                MauSac = m.MauSac,
                LaDat = m.LaDat,
                ThuTu = thuTu++
            });
        }

        await Db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Kiem tra lai sau khi luu de canh bao som cho quan tri vien.
        var loi = await KiemTraAsync(boTieuChiId, ct).ConfigureAwait(false);
        var loiKhoang = loi.Where(l => l.Contains("chồng lấn") || l.Contains("điểm từ")).ToList();

        if (loiKhoang.Count > 0)
        {
            throw new NghiepVuException(MaLoiHeThong.KhoangDiemChongLan, string.Join(" ", loiKhoang));
        }
    }

    /// <summary>Sao chep bo tieu chi sang nam / dot khac.</summary>
    public async Task<Guid> SaoChepAsync(
        Guid id, string maMoi, string tenMoi, int namMoi, CancellationToken ct = default)
    {
        await PhanQuyen.BatBuocCoQuyenAsync(MaQuyen.TieuChiCauHinh, ct: ct).ConfigureAwait(false);
        await BatBuocMaChuaTonTaiAsync(maMoi, null, ct).ConfigureAwait(false);

        var goc = await TaoTruyVanChiTiet()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException(TenDanhMuc, id);

        var moi = new BoTieuChi
        {
            Id = Guid.NewGuid(),
            Ma = maMoi,
            Ten = tenMoi,
            TenKhongDau = VanBanTiengViet.TaoKhongDau(tenMoi),
            MoTa = goc.MoTa,
            Nam = namMoi,
            Cap = goc.Cap,
            ThangDiemToiDa = goc.ThangDiemToiDa,
            DiemDatToiThieu = goc.DiemDatToiThieu,
            CachTinh = goc.CachTinh,
            LamTron = goc.LamTron,
            ChoPhepChamDocLap = goc.ChoPhepChamDocLap,
            TuDongTongHop = goc.TuDongTongHop,
            LoaiBoDiemCaoThap = goc.LoaiBoDiemCaoThap,
            ThuTu = goc.ThuTu
        };

        Db.BoTieuChi.Add(moi);

        foreach (var n in goc.DanhSachNhom.Where(x => !x.DaXoa))
        {
            var nhom = new NhomTieuChi
            {
                Id = Guid.NewGuid(),
                BoTieuChiId = moi.Id,
                Ma = n.Ma,
                Ten = n.Ten,
                TenKhongDau = n.TenKhongDau,
                MoTa = n.MoTa,
                TrongSo = n.TrongSo,
                DiemToiDa = n.DiemToiDa,
                ThuTu = n.ThuTu
            };

            foreach (var t in n.DanhSachTieuChi.Where(x => !x.DaXoa))
            {
                var tieuChi = new TieuChiChamDiem
                {
                    Id = Guid.NewGuid(),
                    NhomTieuChiId = nhom.Id,
                    Ma = t.Ma,
                    Ten = t.Ten,
                    TenKhongDau = t.TenKhongDau,
                    MoTa = t.MoTa,
                    DiemToiDa = t.DiemToiDa,
                    DiemToiThieu = t.DiemToiThieu,
                    TrongSo = t.TrongSo,
                    KieuNhap = t.KieuNhap,
                    BuocNhay = t.BuocNhay,
                    BatBuocNhanXet = t.BatBuocNhanXet,
                    HuongDanCham = t.HuongDanCham,
                    ThuTu = t.ThuTu
                };

                foreach (var m in t.DanhSachMucDiem.Where(x => !x.DaXoa))
                {
                    tieuChi.DanhSachMucDiem.Add(new TieuChiMucDiem
                    {
                        Id = Guid.NewGuid(),
                        TieuChiId = tieuChi.Id,
                        Ten = m.Ten,
                        Diem = m.Diem,
                        MoTa = m.MoTa,
                        ThuTu = m.ThuTu
                    });
                }

                nhom.DanhSachTieuChi.Add(tieuChi);
            }

            Db.NhomTieuChi.Add(nhom);
        }

        foreach (var m in goc.DanhSachMucCongNhan.Where(x => !x.DaXoa))
        {
            Db.MucCongNhan.Add(new MucCongNhan
            {
                Id = Guid.NewGuid(),
                BoTieuChiId = moi.Id,
                Ma = m.Ma,
                Ten = m.Ten,
                TenKhongDau = m.TenKhongDau,
                DiemTu = m.DiemTu,
                DiemDen = m.DiemDen,
                MauSac = m.MauSac,
                LaDat = m.LaDat,
                ThuTu = m.ThuTu
            });
        }

        await Db.SaveChangesAsync(ct).ConfigureAwait(false);
        return moi.Id;
    }

    /// <summary>Bo tieu chi da co phieu cham thi khong duoc sua cau truc.</summary>
    private async Task BatBuocChuaSuDungAsync(Guid boTieuChiId, CancellationToken ct)
    {
        var soPhieu = await Db.PhieuDanhGia.AsNoTracking()
            .CountAsync(x => x.BoTieuChiId == boTieuChiId, ct)
            .ConfigureAwait(false);

        if (soPhieu > 0)
        {
            throw new NghiepVuException(MaLoiHeThong.DangDuocThamChieu,
                $"Bộ tiêu chí đã có {soPhieu} phiếu chấm. "
                + "Vui lòng sao chép sang bộ tiêu chí mới thay vì sửa trực tiếp.");
        }
    }

    protected override async Task<IReadOnlyList<NoiThamChieu>> LayNoiThamChieuAsync(
        Guid id, CancellationToken ct)
    {
        var ketQua = new List<NoiThamChieu>();

        var soPhieu = await Db.PhieuDanhGia.CountAsync(x => x.BoTieuChiId == id, ct).ConfigureAwait(false);
        if (soPhieu > 0)
        {
            ketQua.Add(new NoiThamChieu("phieu_danh_gia", "Phiếu đánh giá", soPhieu));
        }

        var soDot = await Db.DotDeNghi.CountAsync(x => x.BoTieuChiId == id, ct).ConfigureAwait(false);
        if (soDot > 0)
        {
            ketQua.Add(new NoiThamChieu("dot_de_nghi", "Đợt đề nghị", soDot));
        }

        return ketQua;
    }
}

public sealed class NhomTieuChiLuuDto
{
    public Guid Id { get; set; }

    public string Ma { get; set; } = string.Empty;

    public string Ten { get; set; } = string.Empty;

    public string? MoTa { get; set; }

    public decimal TrongSo { get; set; }

    public decimal DiemToiDa { get; set; }

    public List<TieuChiLuuDto> DanhSachTieuChi { get; set; } = new();
}

public sealed class TieuChiLuuDto
{
    public Guid Id { get; set; }

    public string Ma { get; set; } = string.Empty;

    public string Ten { get; set; } = string.Empty;

    public string? MoTa { get; set; }

    public decimal DiemToiDa { get; set; }

    public decimal DiemToiThieu { get; set; }

    public decimal TrongSo { get; set; } = 100m;

    public string KieuNhap { get; set; } = KieuNhapTieuChi.NhapSo;

    public decimal BuocNhay { get; set; } = 0.5m;

    public bool BatBuocNhanXet { get; set; }

    public string? HuongDanCham { get; set; }

    public List<MucDiemLuuDto> DanhSachMucDiem { get; set; } = new();
}

public sealed class MucDiemLuuDto
{
    public Guid Id { get; set; }

    public string Ten { get; set; } = string.Empty;

    public decimal Diem { get; set; }

    public string? MoTa { get; set; }
}

public sealed class MucCongNhanLuuDto
{
    public Guid Id { get; set; }

    public string Ma { get; set; } = string.Empty;

    public string Ten { get; set; } = string.Empty;

    public decimal DiemTu { get; set; }

    public decimal DiemDen { get; set; }

    public string? MauSac { get; set; }

    public bool LaDat { get; set; } = true;
}
