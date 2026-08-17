using System.Text.Json;
using BlueIdea.Application.Chung;
using BlueIdea.Application.DanhMuc;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Domain.QuyTrinh;
using BlueIdea.Domain.SangKien;
using BlueIdea.Shared.KetQua;
using BlueIdea.Shared.TiengViet;
using BlueIdea.Workflow;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.SangKien;

/// <summary>Chuc nang 22 - Tao ho so nhap (luu nhap tu wizard).</summary>
public sealed record TaoHoSoCommand(NoiDungHoSoDto NoiDung)
    : IRequest<Guid>, ICoYeuCauQuyen, ICoGhiNhatKy
{
    public string MaQuyenYeuCau => MaQuyen.SangKienThem;

    public string HanhDongNhatKy => HanhDongAudit.Tao;

    public string ModuleNhatKy => "SANG_KIEN";
}

public sealed class TaoHoSoCommandValidator : AbstractValidator<TaoHoSoCommand>
{
    public TaoHoSoCommandValidator()
    {
        RuleFor(x => x.NoiDung.TenSangKien)
            .NotEmpty().WithMessage("Vui lòng nhập tên sáng kiến")
            .MaximumLength(1000).WithMessage("Tên sáng kiến tối đa 1000 ký tự");

        RuleFor(x => x.NoiDung.DotDeNghiId)
            .NotEmpty().WithMessage("Vui lòng chọn đợt đề nghị");

        RuleFor(x => x.NoiDung.LinhVucId)
            .NotEmpty().WithMessage("Vui lòng chọn lĩnh vực");
    }
}

public sealed class TaoHoSoCommandHandler : IRequestHandler<TaoHoSoCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly INguoiDungHienTai _nguoiDung;
    private readonly IDongHoHeThong _dongHo;
    private readonly ISinhMaHoSo _sinhMa;
    private readonly DichVuDotDeNghi _dichVuDot;
    private readonly IDichVuMaHoa _maHoa;

    public TaoHoSoCommandHandler(
        IAppDbContext db, INguoiDungHienTai nguoiDung, IDongHoHeThong dongHo,
        ISinhMaHoSo sinhMa, DichVuDotDeNghi dichVuDot, IDichVuMaHoa maHoa)
    {
        _db = db;
        _nguoiDung = nguoiDung;
        _dongHo = dongHo;
        _sinhMa = sinhMa;
        _dichVuDot = dichVuDot;
        _maHoa = maHoa;
    }

    public async Task<Guid> Handle(TaoHoSoCommand request, CancellationToken ct)
    {
        var noiDung = request.NoiDung;

        await _dichVuDot.BatBuocDotChoPhepNopAsync(noiDung.DotDeNghiId, ct).ConfigureAwait(false);

        var dot = await _db.DotDeNghi.AsNoTracking()
            .FirstAsync(x => x.Id == noiDung.DotDeNghiId, ct)
            .ConfigureAwait(false);

        var hoSo = new HoSoSangKien
        {
            Id = Guid.NewGuid(),
            MaHoSo = await _sinhMa.SinhAsync(dot.Nam, ct).ConfigureAwait(false),
            TrangThaiTong = TrangThaiTongHoSo.Nhap,
            DonViId = noiDung.DonViId ?? _nguoiDung.DonViId
        };

        ApDungNoiDung(hoSo, noiDung, _maHoa);

        _db.SangKien.Add(hoSo);
        _db.SangKienLichSu.Add(new SangKienLichSu
        {
            SangKienId = hoSo.Id,
            HanhDong = HanhDongLichSuHoSo.Tao,
            NguoiThucHienId = _nguoiDung.Id,
            ThoiGian = _dongHo.BayGio,
            DiaChiIp = _nguoiDung.DiaChiIp,
            UserAgent = _nguoiDung.UserAgent
        });

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return hoSo.Id;
    }

    /// <summary>Ap dung noi dung tu DTO vao entity (dung chung cho tao moi va cap nhat).</summary>
    internal static void ApDungNoiDung(
        HoSoSangKien hoSo, NoiDungHoSoDto noiDung, IDichVuMaHoa maHoa)
    {
        hoSo.TenSangKien = noiDung.TenSangKien.Trim();
        hoSo.TenKhongDau = VanBanTiengViet.TaoKhongDau(hoSo.TenSangKien);
        hoSo.DotDeNghiId = noiDung.DotDeNghiId;
        hoSo.LinhVucId = noiDung.LinhVucId;
        hoSo.DoiTuongId = noiDung.DoiTuongId;
        hoSo.LoaiTacGiaId = noiDung.LoaiTacGiaId;
        hoSo.MoTaGiaiPhap = noiDung.MoTaGiaiPhap;
        hoSo.TinhTrangTruocKhiApDung = noiDung.TinhTrangTruocKhiApDung;
        hoSo.NoiDungGiaiPhap = noiDung.NoiDungGiaiPhap;
        hoSo.TinhMoi = noiDung.TinhMoi;
        hoSo.KhaNangApDung = noiDung.KhaNangApDung;
        hoSo.PhamViApDung = noiDung.PhamViApDung;
        hoSo.HieuQuaKinhTe = noiDung.HieuQuaKinhTe;
        hoSo.GiaTriLamLoiUocTinh = noiDung.GiaTriLamLoiUocTinh;
        hoSo.HieuQuaXaHoi = noiDung.HieuQuaXaHoi;
        hoSo.ThoiGianApDungTu = noiDung.ThoiGianApDungTu;
        hoSo.ThoiGianApDungDen = noiDung.ThoiGianApDungDen;
        hoSo.NoiDungDong = new Dictionary<string, string>(noiDung.NoiDungDong);

        DongBoTacGia(hoSo, noiDung.DanhSachTacGia, maHoa);
    }

    internal static void DongBoTacGia(
        HoSoSangKien hoSo, IReadOnlyList<TacGiaDto> danhSach, IDichVuMaHoa maHoa)
    {
        hoSo.DanhSachTacGia.Clear();

        var thuTu = 1;
        foreach (var tg in danhSach)
        {
            hoSo.DanhSachTacGia.Add(new SangKienTacGia
            {
                Id = tg.Id ?? Guid.NewGuid(),
                SangKienId = hoSo.Id,
                NguoiDungId = tg.NguoiDungId,
                HoTen = tg.HoTen.Trim(),
                NgaySinh = tg.NgaySinh,
                GioiTinh = tg.GioiTinh,
                // So CCCD la du lieu ca nhan -> ma hoa truoc khi luu (Muc 6 dac ta).
                SoCccd = string.IsNullOrWhiteSpace(tg.SoCccd) ? null : maHoa.MaHoa(tg.SoCccd),
                ChucVu = tg.ChucVu,
                DonViCongTac = tg.DonViCongTac,
                TrinhDoChuyenMon = tg.TrinhDoChuyenMon,
                Email = tg.Email,
                DienThoai = tg.DienThoai,
                TyLeDongGop = tg.TyLeDongGop,
                LaTacGiaChinh = tg.LaTacGiaChinh,
                ThuTu = thuTu++
            });
        }
    }
}

/// <summary>Chuc nang 23 - Cap nhat ho so (chi khi o trang thai NHAP hoac YEU_CAU_BO_SUNG).</summary>
public sealed record CapNhatHoSoCommand(Guid Id, NoiDungHoSoDto NoiDung, int? PhienBan = null)
    : IRequest<Unit>, ICoYeuCauQuyen, ICoGhiNhatKy
{
    public string MaQuyenYeuCau => MaQuyen.SangKienSua;

    public Guid? DoiTuongId => Id;

    public string HanhDongNhatKy => HanhDongAudit.Sua;

    public string ModuleNhatKy => "SANG_KIEN";
}

public sealed class CapNhatHoSoCommandHandler : IRequestHandler<CapNhatHoSoCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly INguoiDungHienTai _nguoiDung;
    private readonly IDongHoHeThong _dongHo;
    private readonly DichVuDotDeNghi _dichVuDot;
    private readonly IDichVuMaHoa _maHoa;

    public CapNhatHoSoCommandHandler(
        IAppDbContext db, INguoiDungHienTai nguoiDung, IDongHoHeThong dongHo,
        DichVuDotDeNghi dichVuDot, IDichVuMaHoa maHoa)
    {
        _db = db;
        _nguoiDung = nguoiDung;
        _dongHo = dongHo;
        _dichVuDot = dichVuDot;
        _maHoa = maHoa;
    }

    public async Task<Unit> Handle(CapNhatHoSoCommand request, CancellationToken ct)
    {
        var hoSo = await _db.SangKien
            .Include(x => x.DanhSachTacGia)
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("hồ sơ sáng kiến", request.Id);

        if (request.PhienBan.HasValue && request.PhienBan.Value != hoSo.PhienBan)
        {
            throw new NghiepVuException(MaLoiHeThong.XungDotDuLieu,
                "Hồ sơ đã được cập nhật bởi người khác. Vui lòng tải lại trang.");
        }

        if (!hoSo.ChoPhepSua())
        {
            throw new NghiepVuException(MaLoiHeThong.TrangThaiKhongChoPhepSua,
                $"Hồ sơ ở trạng thái '{hoSo.TrangThaiTong}' nên không thể chỉnh sửa.");
        }

        await _dichVuDot.BatBuocDotChoPhepNopAsync(hoSo.DotDeNghiId, ct).ConfigureAwait(false);

        var truoc = ChupGiaTri(hoSo);

        TaoHoSoCommandHandler.ApDungNoiDung(hoSo, request.NoiDung, _maHoa);
        hoSo.PhienBan++;

        var sau = ChupGiaTri(hoSo);
        var truongThayDoi = truoc.Keys
            .Where(k => !string.Equals(truoc[k], sau[k], StringComparison.Ordinal))
            .ToList();

        if (truongThayDoi.Count > 0)
        {
            _db.SangKienLichSu.Add(new SangKienLichSu
            {
                SangKienId = hoSo.Id,
                HanhDong = HanhDongLichSuHoSo.Sua,
                TruongThayDoi = truongThayDoi,
                GiaTriTruoc = truongThayDoi.ToDictionary(k => k, k => truoc[k]),
                GiaTriSau = truongThayDoi.ToDictionary(k => k, k => sau[k]),
                NguoiThucHienId = _nguoiDung.Id,
                ThoiGian = _dongHo.BayGio,
                DiaChiIp = _nguoiDung.DiaChiIp,
                UserAgent = _nguoiDung.UserAgent
            });
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Unit.Value;
    }

    /// <summary>Chup gia tri cac truong noi dung de tinh diff luu vao lich su chinh sua.</summary>
    private static Dictionary<string, string?> ChupGiaTri(HoSoSangKien hoSo) => new()
    {
        ["tenSangKien"] = hoSo.TenSangKien,
        ["linhVucId"] = hoSo.LinhVucId.ToString(),
        ["doiTuongId"] = hoSo.DoiTuongId?.ToString(),
        ["moTaGiaiPhap"] = hoSo.MoTaGiaiPhap,
        ["tinhTrangTruocKhiApDung"] = hoSo.TinhTrangTruocKhiApDung,
        ["noiDungGiaiPhap"] = hoSo.NoiDungGiaiPhap,
        ["tinhMoi"] = hoSo.TinhMoi,
        ["khaNangApDung"] = hoSo.KhaNangApDung,
        ["phamViApDung"] = hoSo.PhamViApDung,
        ["hieuQuaKinhTe"] = hoSo.HieuQuaKinhTe,
        ["hieuQuaXaHoi"] = hoSo.HieuQuaXaHoi,
        ["giaTriLamLoiUocTinh"] = hoSo.GiaTriLamLoiUocTinh?.ToString(),
        ["noiDungDong"] = JsonSerializer.Serialize(hoSo.NoiDungDong),
        ["danhSachTacGia"] = JsonSerializer.Serialize(
            hoSo.DanhSachTacGia.OrderBy(t => t.ThuTu)
                .Select(t => new { t.HoTen, t.TyLeDongGop, t.LaTacGiaChinh }))
    };
}

/// <summary>Chuc nang 22 - Nop ho so chinh thuc, khoi tao quy trinh xu ly.</summary>
public sealed record NopHoSoCommand(Guid Id)
    : IRequest<KetQuaNopHoSo>, ICoYeuCauQuyen, ICoGhiNhatKy
{
    public string MaQuyenYeuCau => MaQuyen.SangKienNop;

    public Guid? DoiTuongId => Id;

    public string HanhDongNhatKy => "NOP_HO_SO";

    public string ModuleNhatKy => "SANG_KIEN";
}

public sealed record KetQuaNopHoSo(Guid Id, string MaHoSo, string TrangThaiTong, string? TenBuocHienTai);

public sealed class NopHoSoCommandHandler : IRequestHandler<NopHoSoCommand, KetQuaNopHoSo>
{
    private readonly IAppDbContext _db;
    private readonly INguoiDungHienTai _nguoiDung;
    private readonly IDongHoHeThong _dongHo;
    private readonly DichVuDotDeNghi _dichVuDot;
    private readonly IBoMayQuyTrinh _boMay;
    private readonly IBoChuyenDoiSnapshotQuyTrinh _snapshot;
    private readonly IDichVuThongBao _thongBao;

    public NopHoSoCommandHandler(
        IAppDbContext db, INguoiDungHienTai nguoiDung, IDongHoHeThong dongHo,
        DichVuDotDeNghi dichVuDot, IBoMayQuyTrinh boMay,
        IBoChuyenDoiSnapshotQuyTrinh snapshot, IDichVuThongBao thongBao)
    {
        _db = db;
        _nguoiDung = nguoiDung;
        _dongHo = dongHo;
        _dichVuDot = dichVuDot;
        _boMay = boMay;
        _snapshot = snapshot;
        _thongBao = thongBao;
    }

    public async Task<KetQuaNopHoSo> Handle(NopHoSoCommand request, CancellationToken ct)
    {
        var hoSo = await _db.SangKien
            .Include(x => x.DanhSachTacGia)
            .Include(x => x.TepDinhKem)
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("hồ sơ sáng kiến", request.Id);

        if (hoSo.TrangThaiTong is not (TrangThaiTongHoSo.Nhap or TrangThaiTongHoSo.YeuCauBoSung))
        {
            throw new NghiepVuException(MaLoiHeThong.TrangThaiKhongChoPhepSua,
                $"Hồ sơ ở trạng thái '{hoSo.TrangThaiTong}' nên không thể nộp.");
        }

        await _dichVuDot.BatBuocDotChoPhepNopAsync(hoSo.DotDeNghiId, ct).ConfigureAwait(false);

        var dot = await _db.DotDeNghi.AsNoTracking()
            .FirstAsync(x => x.Id == hoSo.DotDeNghiId, ct)
            .ConfigureAwait(false);

        if (dot.QuyTrinhId is null)
        {
            throw new NghiepVuException(MaLoiHeThong.QuyTrinhChuaKichHoat,
                "Đợt đề nghị chưa được gán quy trình xử lý.");
        }

        // --- Kiem tra tac gia ---
        var loaiTacGia = hoSo.LoaiTacGiaId.HasValue
            ? await _db.LoaiTacGia.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == hoSo.LoaiTacGiaId.Value, ct).ConfigureAwait(false)
            : null;

        var tacGiaDto = hoSo.DanhSachTacGia.Select(t => new TacGiaDto
        {
            Id = t.Id,
            HoTen = t.HoTen,
            TyLeDongGop = t.TyLeDongGop,
            LaTacGiaChinh = t.LaTacGiaChinh
        }).ToList();

        var loiTacGia = BoKiemTraTacGia.KiemTra(
            tacGiaDto, loaiTacGia?.SoTacGiaToiDa, loaiTacGia?.ChoPhepNhieuTacGia);

        if (loiTacGia.Count > 0)
        {
            throw new NghiepVuException(MaLoiHeThong.TyLeDongGopKhongHopLe,
                string.Join(" ", loiTacGia.Select(l => l.ThongBao)));
        }

        // --- Kiem tra thanh phan ho so bat buoc ---
        var quyTrinh = await NapQuyTrinhAsync(dot.QuyTrinhId.Value, ct).ConfigureAwait(false);

        var checklist = BoKiemTraThanhPhanHoSo.LapChecklist(
            quyTrinh.ThanhPhanHoSo.ToList(), hoSo, hoSo.TepDinhKem.ToList());

        var chuaDat = BoKiemTraThanhPhanHoSo.LayThanhPhanChuaDat(checklist);
        if (chuaDat.Count > 0)
        {
            var moTa = string.Join(" ", chuaDat.Select(t => t.CanhBao ?? $"Thiếu '{t.Ten}'."));
            throw new NghiepVuException(MaLoiHeThong.ThieuThanhPhanBatBuoc, moTa);
        }

        // --- Dong bang quy trinh va khoi tao workflow ---
        hoSo.QuyTrinhSnapshot = _snapshot.TaoSnapshot(quyTrinh);

        var ketQua = _boMay.KhoiTao(hoSo, quyTrinh, _dongHo.BayGio, out var banGhi);
        if (!ketQua.ThanhCong)
        {
            throw new NghiepVuException(ketQua.MaLoi ?? MaLoiHeThong.QuyTrinhKhongHopLe, ketQua.ThongBao);
        }

        if (banGhi is not null)
        {
            _db.SangKienXuLy.Add(banGhi);
        }

        _db.SangKienLichSu.Add(new SangKienLichSu
        {
            SangKienId = hoSo.Id,
            HanhDong = HanhDongLichSuHoSo.Nop,
            NguoiThucHienId = _nguoiDung.Id,
            ThoiGian = _dongHo.BayGio,
            DiaChiIp = _nguoiDung.DiaChiIp,
            UserAgent = _nguoiDung.UserAgent
        });

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Thong bao cho tac gia (kenh app + email theo mau cau hinh).
        var nguoiNhan = hoSo.DanhSachTacGia
            .Where(t => t.NguoiDungId.HasValue)
            .Select(t => t.NguoiDungId!.Value)
            .Distinct()
            .ToList();

        if (nguoiNhan.Count > 0)
        {
            await _thongBao.GuiTheoSuKienAsync(
                SuKienThongBao.HoSoDuocTiepNhan,
                nguoiNhan,
                new Dictionary<string, object?>
                {
                    ["maHoSo"] = hoSo.MaHoSo,
                    ["tenSangKien"] = hoSo.TenSangKien,
                    ["tenBuoc"] = ketQua.TenBuocMoi
                },
                ct).ConfigureAwait(false);
        }

        return new KetQuaNopHoSo(hoSo.Id, hoSo.MaHoSo, hoSo.TrangThaiTong, ketQua.TenBuocMoi);
    }

    private async Task<QuyTrinh> NapQuyTrinhAsync(Guid quyTrinhId, CancellationToken ct)
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
}

/// <summary>Chuc nang 23 - Rut ho so (chi khi chua vao buoc cham diem).</summary>
public sealed record RutHoSoCommand(Guid Id, string LyDo)
    : IRequest<Unit>, ICoYeuCauQuyen, ICoGhiNhatKy
{
    public string MaQuyenYeuCau => MaQuyen.SangKienRut;

    public Guid? DoiTuongId => Id;

    public string HanhDongNhatKy => "RUT_HO_SO";

    public string ModuleNhatKy => "SANG_KIEN";
}

public sealed class RutHoSoCommandValidator : AbstractValidator<RutHoSoCommand>
{
    public RutHoSoCommandValidator()
        => RuleFor(x => x.LyDo)
            .NotEmpty().WithMessage("Vui lòng nhập lý do rút hồ sơ")
            .MaximumLength(2000);
}

public sealed class RutHoSoCommandHandler : IRequestHandler<RutHoSoCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly INguoiDungHienTai _nguoiDung;
    private readonly IDongHoHeThong _dongHo;
    private readonly IBoChuyenDoiSnapshotQuyTrinh _snapshot;

    public RutHoSoCommandHandler(
        IAppDbContext db, INguoiDungHienTai nguoiDung, IDongHoHeThong dongHo,
        IBoChuyenDoiSnapshotQuyTrinh snapshot)
    {
        _db = db;
        _nguoiDung = nguoiDung;
        _dongHo = dongHo;
        _snapshot = snapshot;
    }

    public async Task<Unit> Handle(RutHoSoCommand request, CancellationToken ct)
    {
        var hoSo = await _db.SangKien
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("hồ sơ sáng kiến", request.Id);

        if (!hoSo.ChoPhepRut())
        {
            throw new NghiepVuException(MaLoiHeThong.TrangThaiKhongChoPhepSua,
                $"Hồ sơ ở trạng thái '{hoSo.TrangThaiTong}' nên không thể rút.");
        }

        // Khong cho rut khi da vao buoc cham diem tro di.
        if (hoSo.BuocHienTaiId.HasValue)
        {
            var quyTrinh = _snapshot.DocSnapshot(hoSo.QuyTrinhSnapshot);
            var buoc = quyTrinh?.DanhSachBuoc.FirstOrDefault(b => b.Id == hoSo.BuocHienTaiId.Value);

            if (buoc is not null && buoc.LoaiBuoc is LoaiBuoc.ChamDiem or LoaiBuoc.HopHoiDong
                    or LoaiBuoc.BoPhieu or LoaiBuoc.PheDuyet or LoaiBuoc.BanHanhQuyetDinh)
            {
                throw new NghiepVuException(MaLoiHeThong.TrangThaiKhongChoPhepSua,
                    $"Hồ sơ đã vào bước '{buoc.Ten}' nên không thể rút.");
            }
        }

        hoSo.TrangThaiTong = TrangThaiTongHoSo.DaRut;
        hoSo.BuocHienTaiId = null;
        hoSo.HanXuLyHienTai = null;
        hoSo.NgayHoanThanh = _dongHo.BayGio;
        hoSo.PhienBan++;

        _db.SangKienLichSu.Add(new SangKienLichSu
        {
            SangKienId = hoSo.Id,
            HanhDong = HanhDongLichSuHoSo.Rut,
            GhiChu = request.LyDo,
            NguoiThucHienId = _nguoiDung.Id,
            ThoiGian = _dongHo.BayGio,
            DiaChiIp = _nguoiDung.DiaChiIp,
            UserAgent = _nguoiDung.UserAgent
        });

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Unit.Value;
    }
}
