using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Shared.KetQua;
using BlueIdea.Shared.TiengViet;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.QuanTri;

public sealed record LuuNguoiDungDto
{
    public string TenDangNhap { get; init; } = string.Empty;

    public string HoTen { get; init; } = string.Empty;

    public string? Email { get; init; }

    public string? DienThoai { get; init; }

    public string? ChucVu { get; init; }

    public Guid? DonViId { get; init; }

    public string? SoCccd { get; init; }

    public DateOnly? NgaySinh { get; init; }

    public string? GioiTinh { get; init; }

    public string TrangThaiTaiKhoan { get; init; } = TrangThaiNguoiDung.HoatDong;

    public List<Guid> VaiTroIds { get; init; } = new();
}

public sealed record LuuVaiTroDto
{
    public string Ma { get; init; } = string.Empty;

    public string Ten { get; init; } = string.Empty;

    public string? MoTa { get; init; }

    public int ThuTu { get; init; }

    public short TrangThai { get; init; } = TrangThaiDanhMuc.HoatDong;

    public List<Guid> QuyenIds { get; init; } = new();

    /// <summary>CA_NHAN | DON_VI | DON_VI_VA_CAP_DUOI | TOAN_HE_THONG | DANH_SACH_DON_VI</summary>
    public string LoaiPhamVi { get; init; } = LoaiPhamViDuLieu.CaNhan;

    public List<Guid> DonViIds { get; init; } = new();
}

public sealed record ThongTinNguoiDungDto(
    Guid Id,
    string TenDangNhap,
    string HoTen,
    string? Email,
    string? DienThoai,
    string? ChucVu,
    Guid? DonViId,
    string? TenDonVi,
    DateOnly? NgaySinh,
    string? GioiTinh,
    string TrangThaiTaiKhoan,
    bool BuocDoiMatKhau,
    DateTimeOffset? LanDangNhapCuoi,
    IReadOnlyList<Guid> VaiTroIds,
    IReadOnlyList<string> TenVaiTro);

/// <summary>
/// Chuc nang 43, 45 - Quan tri tai khoan nguoi dung va ma tran phan quyen vai tro.
/// </summary>
public sealed class DichVuQuanTriNguoiDung
{
    private readonly IAppDbContext _db;
    private readonly IDichVuPhanQuyen _phanQuyen;
    private readonly IDichVuMatKhau _matKhau;
    private readonly IDichVuCauHinh _cauHinh;
    private readonly IDongHoHeThong _dongHo;
    private readonly IDichVuMaHoa _maHoa;
    private readonly IDichVuNhatKy _nhatKy;
    private readonly INguoiDungHienTai _nguoiDungHienTai;

    public DichVuQuanTriNguoiDung(
        IAppDbContext db, IDichVuPhanQuyen phanQuyen, IDichVuMatKhau matKhau,
        IDichVuCauHinh cauHinh, IDongHoHeThong dongHo, IDichVuMaHoa maHoa,
        IDichVuNhatKy nhatKy, INguoiDungHienTai nguoiDungHienTai)
    {
        _db = db;
        _phanQuyen = phanQuyen;
        _matKhau = matKhau;
        _cauHinh = cauHinh;
        _dongHo = dongHo;
        _maHoa = maHoa;
        _nhatKy = nhatKy;
        _nguoiDungHienTai = nguoiDungHienTai;
    }

    public async Task<ThongTinNguoiDungDto> ChiTietAsync(Guid id, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.NguoiDungXem, ct).ConfigureAwait(false);

        var nguoiDung = await _db.NguoiDung.AsNoTracking()
            .Include(x => x.VaiTro)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("người dùng", id);

        var vaiTroIds = nguoiDung.VaiTro.Select(v => v.VaiTroId).ToList();

        var tenVaiTro = await _db.VaiTro.AsNoTracking()
            .Where(v => vaiTroIds.Contains(v.Id))
            .Select(v => v.Ten)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var tenDonVi = await _db.DonVi.AsNoTracking()
            .Where(d => d.Id == nguoiDung.DonViId).Select(d => d.Ten)
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);

        return new ThongTinNguoiDungDto(
            nguoiDung.Id, nguoiDung.TenDangNhap, nguoiDung.HoTen, nguoiDung.Email,
            nguoiDung.DienThoai, nguoiDung.ChucVu, nguoiDung.DonViId, tenDonVi,
            nguoiDung.NgaySinh, nguoiDung.GioiTinh, nguoiDung.TrangThaiTaiKhoan,
            nguoiDung.BuocDoiMatKhau, nguoiDung.LanDangNhapCuoi, vaiTroIds, tenVaiTro);
    }

    /// <summary>Tao tai khoan moi. Tra ve mat khau tam de quan tri vien ban giao cho nguoi dung.</summary>
    public async Task<(Guid Id, string MatKhauTam)> ThemAsync(
        LuuNguoiDungDto dto, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.NguoiDungThem, ct: ct).ConfigureAwait(false);

        var tenDangNhap = dto.TenDangNhap.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(tenDangNhap) || string.IsNullOrWhiteSpace(dto.HoTen))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Tên đăng nhập và họ tên là bắt buộc.");
        }

        var daTonTai = await _db.NguoiDung.AsNoTracking()
            .AnyAsync(x => x.TenDangNhap == tenDangNhap, ct)
            .ConfigureAwait(false);

        if (daTonTai)
        {
            throw new NghiepVuException(MaLoiHeThong.TrungMa,
                $"Tên đăng nhập '{tenDangNhap}' đã tồn tại.");
        }

        await BatBuocVaiTroTonTaiAsync(dto.VaiTroIds, ct).ConfigureAwait(false);

        var matKhauTam = await SinhMatKhauTamAsync(ct).ConfigureAwait(false);
        var (hash, salt) = _matKhau.BamMatKhau(matKhauTam);

        var nguoiDung = new NguoiDung
        {
            Id = Guid.NewGuid(),
            TenDangNhap = tenDangNhap,
            HoTen = dto.HoTen.Trim(),
            HoTenKhongDau = VanBanTiengViet.TaoKhongDau(dto.HoTen),
            Email = dto.Email?.Trim(),
            DienThoai = dto.DienThoai?.Trim(),
            ChucVu = dto.ChucVu,
            DonViId = dto.DonViId,
            NgaySinh = dto.NgaySinh,
            GioiTinh = dto.GioiTinh,
            SoCccd = string.IsNullOrWhiteSpace(dto.SoCccd) ? null : _maHoa.MaHoa(dto.SoCccd),
            TrangThaiTaiKhoan = dto.TrangThaiTaiKhoan,
            MatKhauHash = hash,
            MatKhauSalt = salt,

            // Bat buoc doi mat khau ngay lan dang nhap dau: mat khau tam da di qua tay quan tri vien.
            BuocDoiMatKhau = true,
            NgayDoiMatKhauCuoi = _dongHo.BayGio
        };

        _db.NguoiDung.Add(nguoiDung);

        foreach (var vaiTroId in dto.VaiTroIds.Distinct())
        {
            _db.NguoiDungVaiTro.Add(new NguoiDungVaiTro
            {
                Id = Guid.NewGuid(),
                NguoiDungId = nguoiDung.Id,
                VaiTroId = vaiTroId,
                DonViId = dto.DonViId
            });
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await _nhatKy.GhiAsync("THEM_NGUOI_DUNG", "QUAN_TRI", "NguoiDung", nguoiDung.Id,
            $"Tạo tài khoản {tenDangNhap}",
            duLieuSau: new { dto.TenDangNhap, dto.HoTen, dto.Email, dto.VaiTroIds },
            ct: ct).ConfigureAwait(false);

        return (nguoiDung.Id, matKhauTam);
    }

    public async Task CapNhatAsync(Guid id, LuuNguoiDungDto dto, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.NguoiDungSua, ct).ConfigureAwait(false);

        var nguoiDung = await _db.NguoiDung
            .Include(x => x.VaiTro)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("người dùng", id);

        await BatBuocVaiTroTonTaiAsync(dto.VaiTroIds, ct).ConfigureAwait(false);
        await BatBuocConQuanTriKhacAsync(id, dto.VaiTroIds, dto.TrangThaiTaiKhoan, ct)
            .ConfigureAwait(false);

        var truoc = new
        {
            nguoiDung.HoTen, nguoiDung.Email, nguoiDung.DienThoai, nguoiDung.ChucVu,
            nguoiDung.DonViId, nguoiDung.TrangThaiTaiKhoan,
            VaiTroIds = nguoiDung.VaiTro.Select(v => v.VaiTroId).ToList()
        };

        // Ten dang nhap la dinh danh dung trong nhat ky va token - khong cho doi.
        nguoiDung.HoTen = dto.HoTen.Trim();
        nguoiDung.HoTenKhongDau = VanBanTiengViet.TaoKhongDau(dto.HoTen);
        nguoiDung.Email = dto.Email?.Trim();
        nguoiDung.DienThoai = dto.DienThoai?.Trim();
        nguoiDung.ChucVu = dto.ChucVu;
        nguoiDung.DonViId = dto.DonViId;
        nguoiDung.NgaySinh = dto.NgaySinh;
        nguoiDung.GioiTinh = dto.GioiTinh;
        nguoiDung.TrangThaiTaiKhoan = dto.TrangThaiTaiKhoan;

        if (!string.IsNullOrWhiteSpace(dto.SoCccd))
        {
            nguoiDung.SoCccd = _maHoa.MaHoa(dto.SoCccd);
        }

        if (dto.TrangThaiTaiKhoan == TrangThaiNguoiDung.HoatDong)
        {
            nguoiDung.KhoaDen = null;
            nguoiDung.SoLanDangNhapSai = 0;
        }

        // Dong bo vai tro: go vai tro bi bo chon, them vai tro moi, giu nguyen phan trung.
        var vaiTroMoi = dto.VaiTroIds.Distinct().ToHashSet();
        var vaiTroHienCo = await _db.NguoiDungVaiTro
            .Where(x => x.NguoiDungId == id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var cu in vaiTroHienCo.Where(x => !vaiTroMoi.Contains(x.VaiTroId)))
        {
            cu.DaXoa = true;
        }

        var daCo = vaiTroHienCo.Where(x => !x.DaXoa).Select(x => x.VaiTroId).ToHashSet();

        foreach (var them in vaiTroMoi.Where(v => !daCo.Contains(v)))
        {
            _db.NguoiDungVaiTro.Add(new NguoiDungVaiTro
            {
                Id = Guid.NewGuid(),
                NguoiDungId = id,
                VaiTroId = them,
                DonViId = dto.DonViId
            });
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await _nhatKy.GhiAsync("SUA_NGUOI_DUNG", "QUAN_TRI", "NguoiDung", id,
            $"Cập nhật tài khoản {nguoiDung.TenDangNhap}",
            duLieuTruoc: truoc,
            duLieuSau: new { dto.HoTen, dto.Email, dto.DienThoai, dto.ChucVu, dto.DonViId,
                dto.TrangThaiTaiKhoan, dto.VaiTroIds },
            ct: ct).ConfigureAwait(false);
    }

    /// <summary>Dat lai mat khau ve mot mat khau tam va buoc doi o lan dang nhap ke tiep.</summary>
    public async Task<string> DatLaiMatKhauAsync(Guid id, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.NguoiDungDatLaiMatKhau, ct)
            .ConfigureAwait(false);

        var nguoiDung = await _db.NguoiDung.FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("người dùng", id);

        await BatBuocNguoiDungTrongPhamViAsync(nguoiDung, ct).ConfigureAwait(false);

        var matKhauTam = await SinhMatKhauTamAsync(ct).ConfigureAwait(false);
        var (hash, salt) = _matKhau.BamMatKhau(matKhauTam);

        nguoiDung.MatKhauHash = hash;
        nguoiDung.MatKhauSalt = salt;
        nguoiDung.BuocDoiMatKhau = true;
        nguoiDung.NgayDoiMatKhauCuoi = _dongHo.BayGio;
        nguoiDung.SoLanDangNhapSai = 0;
        nguoiDung.KhoaDen = null;

        // Thu hoi toan bo phien dang mo: mat khau cu khong con hieu luc thi token cung khong duoc song.
        var bayGio = _dongHo.BayGio;

        var tokenDangMo = await _db.RefreshToken
            .Where(x => x.NguoiDungId == id && x.ThoiGianThuHoi == null)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var token in tokenDangMo)
        {
            token.ThoiGianThuHoi = bayGio;
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await _nhatKy.GhiAsync("DAT_LAI_MAT_KHAU", "QUAN_TRI", "NguoiDung", id,
            $"Đặt lại mật khẩu cho {nguoiDung.TenDangNhap}", ct: ct).ConfigureAwait(false);

        return matKhauTam;
    }

    // ----------------------------------------------------------------------------- Vai tro

    /// <summary>
    /// Chuc nang 43 — xoa tai khoan (xoa mem).
    ///
    /// Xoa MEM chu khong xoa han: tai khoan da tung xu ly ho so con duoc tham chieu trong nhat ky
    /// xu ly, lich su chinh sua va nhat ky he thong. Xoa han se lam nhung ban ghi do tro toi mot
    /// nguoi khong con ton tai — dung thu ma ho so nghiem thu dua vao de truy nguoc trach nhiem.
    ///
    /// Khac "khoa tai khoan": khoa la tam thoi, nguoi dung van trong danh sach va mo lai duoc.
    /// </summary>
    public async Task XoaNguoiDungAsync(Guid id, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.NguoiDungXoa, ct).ConfigureAwait(false);

        if (id == _nguoiDungHienTai.Id)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Không tự xoá tài khoản của chính mình.");
        }

        var nguoiDung = await _db.NguoiDung.FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("người dùng", id);

        await BatBuocNguoiDungTrongPhamViAsync(nguoiDung, ct).ConfigureAwait(false);

        // Con ho so dang xu ly ma xoa nguoi phu trach thi buoc do ket cung, khong ai nhan tiep.
        var dangGiuBuoc = await _db.SangKienXuLy.AsNoTracking()
            .AnyAsync(x => x.NguoiXuLyId == id && x.ThoiGianXuLy == null, ct)
            .ConfigureAwait(false);

        if (dangGiuBuoc)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Tài khoản đang giữ bước xử lý của hồ sơ — chuyển việc cho người khác trước khi xoá.");
        }

        var truoc = new { nguoiDung.TenDangNhap, nguoiDung.HoTen, nguoiDung.TrangThaiTaiKhoan };

        nguoiDung.DaXoa = true;
        nguoiDung.TrangThaiTaiKhoan = TrangThaiNguoiDung.Khoa;

        // Thu hoi moi phien: xoa mem ma van con refresh token song thi tai khoan van dung duoc.
        var tokenDangMo = await _db.RefreshToken
            .Where(x => x.NguoiDungId == id && x.ThoiGianThuHoi == null)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var token in tokenDangMo)
        {
            token.ThoiGianThuHoi = _dongHo.BayGio;
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await _nhatKy.GhiAsync("XOA_NGUOI_DUNG", "QUAN_TRI", "NguoiDung", id,
            $"Xoá tài khoản {nguoiDung.TenDangNhap}", duLieuTruoc: truoc, ct: ct)
            .ConfigureAwait(false);
    }

    public async Task<Guid> ThemVaiTroAsync(LuuVaiTroDto dto, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.VaiTroCauHinh, ct: ct).ConfigureAwait(false);

        var ma = dto.Ma.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(ma) || string.IsNullOrWhiteSpace(dto.Ten))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe, "Mã và tên vai trò là bắt buộc.");
        }

        if (await _db.VaiTro.AsNoTracking().AnyAsync(x => x.Ma == ma, ct).ConfigureAwait(false))
        {
            throw new NghiepVuException(MaLoiHeThong.TrungMa, $"Mã vai trò '{ma}' đã tồn tại.");
        }

        var vaiTro = new VaiTro
        {
            Id = Guid.NewGuid(),
            Ma = ma,
            Ten = dto.Ten.Trim(),
            MoTa = dto.MoTa,
            ThuTu = dto.ThuTu,
            TrangThai = dto.TrangThai,
            LaHeThong = false
        };

        _db.VaiTro.Add(vaiTro);

        await GanQuyenAsync(vaiTro.Id, dto, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await _nhatKy.GhiAsync("THEM_VAI_TRO", "QUAN_TRI", "VaiTro", vaiTro.Id,
            $"Tạo vai trò {ma}", duLieuSau: dto, ct: ct).ConfigureAwait(false);

        return vaiTro.Id;
    }

    public async Task CapNhatVaiTroAsync(Guid id, LuuVaiTroDto dto, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.VaiTroCauHinh, ct).ConfigureAwait(false);

        var vaiTro = await _db.VaiTro
            .Include(x => x.DanhSachQuyen)
            .Include(x => x.PhamViDuLieu)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("vai trò", id);

        var truoc = new
        {
            vaiTro.Ma, vaiTro.Ten, vaiTro.TrangThai,
            QuyenIds = vaiTro.DanhSachQuyen.Select(q => q.QuyenId).ToList()
        };

        // Vai tro he thong: cho sua ma tran quyen nhung KHONG cho doi ma - ma duoc code tham chieu
        // truc tiep (vi du QUAN_TRI_HE_THONG duoc dung lam duong tat trong policy).
        if (!vaiTro.LaHeThong)
        {
            var ma = dto.Ma.Trim().ToUpperInvariant();

            if (ma != vaiTro.Ma
                && await _db.VaiTro.AsNoTracking().AnyAsync(x => x.Ma == ma && x.Id != id, ct)
                    .ConfigureAwait(false))
            {
                throw new NghiepVuException(MaLoiHeThong.TrungMa, $"Mã vai trò '{ma}' đã tồn tại.");
            }

            vaiTro.Ma = ma;
            vaiTro.TrangThai = dto.TrangThai;
        }

        vaiTro.Ten = dto.Ten.Trim();
        vaiTro.MoTa = dto.MoTa;
        vaiTro.ThuTu = dto.ThuTu;

        if (vaiTro.Ma == MaVaiTro.QuanTriHeThong && dto.QuyenIds.Count == 0)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Không được bỏ toàn bộ quyền của vai trò Quản trị hệ thống.");
        }

        foreach (var cu in await _db.VaiTroQuyen.Where(x => x.VaiTroId == id).ToListAsync(ct)
                     .ConfigureAwait(false))
        {
            cu.DaXoa = true;
        }

        foreach (var cu in await _db.PhamViDuLieu.Where(x => x.VaiTroId == id).ToListAsync(ct)
                     .ConfigureAwait(false))
        {
            cu.DaXoa = true;
        }

        await GanQuyenAsync(id, dto, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await _nhatKy.GhiAsync("SUA_VAI_TRO", "QUAN_TRI", "VaiTro", id,
            $"Cập nhật vai trò {vaiTro.Ma}",
            duLieuTruoc: truoc, duLieuSau: dto, ct: ct).ConfigureAwait(false);
    }

    public async Task XoaVaiTroAsync(Guid id, CancellationToken ct = default)
    {
        await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.VaiTroCauHinh, ct).ConfigureAwait(false);

        var vaiTro = await _db.VaiTro.FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("vai trò", id);

        if (vaiTro.LaHeThong)
        {
            throw new NghiepVuException(MaLoiHeThong.TrangThaiKhongChoPhepXoa,
                $"Vai trò hệ thống '{vaiTro.Ten}' không được xoá.");
        }

        var dangDung = await _db.NguoiDungVaiTro.AsNoTracking()
            .CountAsync(x => x.VaiTroId == id, ct)
            .ConfigureAwait(false);

        if (dangDung > 0)
        {
            throw new NghiepVuException(MaLoiHeThong.DangDuocThamChieu,
                $"Vai trò đang được gán cho {dangDung} tài khoản nên không xoá được.");
        }

        vaiTro.DaXoa = true;

        foreach (var q in await _db.VaiTroQuyen.Where(x => x.VaiTroId == id).ToListAsync(ct)
                     .ConfigureAwait(false))
        {
            q.DaXoa = true;
        }

        foreach (var p in await _db.PhamViDuLieu.Where(x => x.VaiTroId == id).ToListAsync(ct)
                     .ConfigureAwait(false))
        {
            p.DaXoa = true;
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await _nhatKy.GhiAsync("XOA_VAI_TRO", "QUAN_TRI", "VaiTro", id,
            $"Xoá vai trò {vaiTro.Ma}", ct: ct).ConfigureAwait(false);
    }

    // -----------------------------------------------------------------------------------

    private async Task GanQuyenAsync(Guid vaiTroId, LuuVaiTroDto dto, CancellationToken ct)
    {
        var quyenHopLe = await _db.Quyen.AsNoTracking()
            .Where(q => dto.QuyenIds.Contains(q.Id))
            .Select(q => q.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var quyenId in quyenHopLe)
        {
            _db.VaiTroQuyen.Add(new VaiTroQuyen
            {
                Id = Guid.NewGuid(),
                VaiTroId = vaiTroId,
                QuyenId = quyenId
            });
        }

        _db.PhamViDuLieu.Add(new PhamViDuLieu
        {
            Id = Guid.NewGuid(),
            VaiTroId = vaiTroId,
            LoaiPhamVi = dto.LoaiPhamVi,
            DonViIds = dto.DonViIds
        });
    }

    private async Task BatBuocVaiTroTonTaiAsync(List<Guid> vaiTroIds, CancellationToken ct)
    {
        if (vaiTroIds.Count == 0)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Phải gán ít nhất một vai trò cho tài khoản.");
        }

        var soHopLe = await _db.VaiTro.AsNoTracking()
            .CountAsync(x => vaiTroIds.Contains(x.Id), ct)
            .ConfigureAwait(false);

        if (soHopLe != vaiTroIds.Distinct().Count())
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Danh sách vai trò chứa vai trò không tồn tại.");
        }
    }

    /// <summary>
    /// Chan tinh huong tu khoa minh ra khoi he thong: khong duoc bo quyen quan tri / khoa tai khoan
    /// khi day la tai khoan quan tri hoat dong CUOI CUNG.
    /// </summary>
    private async Task BatBuocConQuanTriKhacAsync(
        Guid id, List<Guid> vaiTroMoi, string trangThaiMoi, CancellationToken ct)
    {
        var vaiTroQuanTriId = await _db.VaiTro.AsNoTracking()
            .Where(x => x.Ma == MaVaiTro.QuanTriHeThong)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (vaiTroQuanTriId is null)
        {
            return;
        }

        var dangLaQuanTri = await _db.NguoiDungVaiTro.AsNoTracking()
            .AnyAsync(x => x.NguoiDungId == id && x.VaiTroId == vaiTroQuanTriId.Value, ct)
            .ConfigureAwait(false);

        var vanLaQuanTri = vaiTroMoi.Contains(vaiTroQuanTriId.Value)
                           && trangThaiMoi == TrangThaiNguoiDung.HoatDong;

        if (!dangLaQuanTri || vanLaQuanTri)
        {
            return;
        }

        var soQuanTriKhac = await _db.NguoiDungVaiTro.AsNoTracking()
            .Where(x => x.VaiTroId == vaiTroQuanTriId.Value && x.NguoiDungId != id)
            .Join(_db.NguoiDung.AsNoTracking(), x => x.NguoiDungId, n => n.Id, (_, n) => n)
            .CountAsync(n => n.TrangThaiTaiKhoan == TrangThaiNguoiDung.HoatDong, ct)
            .ConfigureAwait(false);

        if (soQuanTriKhac == 0)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Đây là tài khoản quản trị hệ thống đang hoạt động cuối cùng. "
                + "Hãy tạo hoặc kích hoạt một quản trị viên khác trước khi thay đổi tài khoản này.");
        }
    }

    /// <summary>Sinh mat khau tam theo do dai toi thieu trong chinh sach dang cau hinh.</summary>
    private async Task<string> SinhMatKhauTamAsync(CancellationToken ct)
    {
        var doDaiToiThieu = await _cauHinh
            .LayAsync(KhoaCauHinh.ChinhSachMatKhauDoDaiToiThieu, 8, ct)
            .ConfigureAwait(false);

        return BoSinhMatKhauTam.Sinh(Math.Max(12, doDaiToiThieu));
    }

    private async Task BatBuocNguoiDungTrongPhamViAsync(NguoiDung mucTieu, CancellationToken ct)
    {
        var nguoiGoiId = _nguoiDungHienTai.Id
                         ?? throw new NghiepVuException(MaLoiHeThong.ChuaXacThuc, "Chưa đăng nhập.");

        var phamVi = await _phanQuyen.LayPhamViDonViAsync(nguoiGoiId, ct).ConfigureAwait(false);

        if (phamVi.ToanHeThong) return;

        if (phamVi.ChiCaNhan || !mucTieu.DonViId.HasValue
                              || !phamVi.DonViIds.Contains(mucTieu.DonViId.Value))
        {
            throw new KhongTimThayException("người dùng", mucTieu.Id);
        }
    }
}
