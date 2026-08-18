using System.Globalization;
using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.DanhMuc;
using BlueIdea.Shared.KetQua;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.BaoCao;

/// <summary>Mot truong du lieu dung duoc trong bieu mau, de UI goi y thay vi bat go tay.</summary>
public sealed record TruongBieuMauKhaDung(string Nguon, string Nhan, string Kieu, string ViDu);

/// <summary>Mot dong nhan - gia tri da phan giai xong, san sang do vao PDF.</summary>
public sealed record DongBieuMauDaPhanGiai(string Nhan, string? GiaTri);

/// <summary>Ket qua sinh bieu mau: tieu de + cac dong da phan giai.</summary>
public sealed record BieuMauDaPhanGiai(
    string TieuDe,
    string? PhuDe,
    IReadOnlyList<DongBieuMauDaPhanGiai> DongDuLieu,
    IReadOnlyList<string> TruongThieu);

/// <summary>
/// Chuc nang 6 — Sinh van ban tu cau hinh <c>bieu_mau_xuat</c>.
///
/// Truoc day bang nay chi la khai bao suong: quan tri vien anh xa placeholder nhung khong co gi
/// doc no ra. Dich vu nay phan giai anh xa do tren du lieu that, de bieu mau cau hinh anh huong
/// den van ban in ra thay vi nam khong trong CSDL.
///
/// Placeholder khong khop nguon nao KHONG bi bo im lang ma duoc bao lai trong <c>TruongThieu</c>:
/// mot phieu tiep nhan in ra thieu o ma quan tri vien khong biet la loi kho phat hien nhat.
/// </summary>
public sealed class DichVuSinhBieuMau
{
    private readonly IAppDbContext _db;

    public DichVuSinhBieuMau(IAppDbContext db) => _db = db;

    /// <summary>Cac truong du lieu dung duoc theo tung loai bieu mau.</summary>
    public static IReadOnlyList<TruongBieuMauKhaDung> TruongKhaDung(string loai) => loai switch
    {
        LoaiBieuMau.PhieuTiepNhan or LoaiBieuMau.PhieuDanhGia or LoaiBieuMau.QuyetDinh
            => TruongHoSo,
        LoaiBieuMau.BienBanHop => TruongPhienHop,
        _ => TruongHoSo,
    };

    /// <summary>
    /// Dung DUNG bo ma nguon cua bao cao tuy bien (<see cref="DichVuBaoCaoTuyBien"/>): man hinh
    /// cau hinh bieu mau va bo sinh van ban phai noi cung mot thu ngu, neu khong quan tri vien
    /// chon nguon o dropdown xong thi luc in ra bao "khong tim thay nguon".
    /// </summary>
    private static readonly TruongBieuMauKhaDung[] TruongHoSo =
    {
        new("maHoSo", "Mã hồ sơ", "text", "SK-2026-0001"),
        new("tenSangKien", "Tên sáng kiến", "text", "Số hoá quy trình tiếp nhận hồ sơ"),
        new("tacGiaChinh", "Tác giả chính", "text", "Nguyễn Văn A"),
        new("dongTacGia", "Đồng tác giả", "text", "Trần Thị B, Lê Văn C"),
        new("tenDonVi", "Đơn vị", "text", "Phòng Kế hoạch"),
        new("tenLinhVuc", "Lĩnh vực", "text", "Công nghệ thông tin"),
        new("tenDot", "Đợt đề nghị", "text", "Đợt 1 năm 2026"),
        new("trangThaiTong", "Trạng thái", "text", "Đang xử lý"),
        new("ketQua", "Kết quả xét", "text", "Đạt"),
        new("tongDiem", "Tổng điểm", "number", "87,5"),
        new("diemTrungBinh", "Điểm trung bình", "number", "87,5"),
        new("tenMucCongNhan", "Mức công nhận", "text", "Cấp cơ sở"),
        new("tyLeTrungLap", "Tỷ lệ trùng lặp (%)", "number", "12,4"),
        new("ngayNop", "Ngày nộp", "date", "18/08/2026"),
        new("hanXuLy", "Hạn xử lý", "date", "02/09/2026"),
        new("ngayCongNhan", "Ngày công nhận", "date", "30/09/2026"),
        new("phamViApDung", "Phạm vi áp dụng", "text", "Toàn đơn vị"),
        new("hieuQuaKinhTe", "Hiệu quả kinh tế", "text", "Tiết kiệm 120 giờ công/năm"),
        new("giaTriLamLoi", "Giá trị làm lợi ước tính", "number", "45.000.000"),
    };

    private static readonly TruongBieuMauKhaDung[] TruongPhienHop =
    {
        new("tenPhien", "Tên phiên họp", "text", "Phiên họp xét sáng kiến đợt 1"),
        new("tenHoiDong", "Hội đồng", "text", "Hội đồng sáng kiến cấp cơ sở"),
        new("thoiGianHop", "Thời gian họp", "date", "18/08/2026"),
        new("diaDiem", "Địa điểm", "text", "Phòng họp A"),
        new("soCoMat", "Số thành viên có mặt", "text", "7/9"),
        new("soHoSo", "Số hồ sơ xét", "number", "12"),
        new("ketLuan", "Kết luận chung", "text", "Thông qua 10/12 hồ sơ"),
    };

    /// <summary>
    /// Sinh noi dung bieu mau cho MOT ho so. <paramref name="hoSoId"/> null = sinh du lieu mau
    /// (dung cho chuc nang xem truoc luc cau hinh, khi chua chon ho so cu the nao).
    /// </summary>
    public async Task<BieuMauDaPhanGiai> SinhChoHoSoAsync(
        Guid bieuMauId, Guid? hoSoId, CancellationToken ct = default)
    {
        var bieuMau = await _db.BieuMauXuat.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == bieuMauId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("biểu mẫu", bieuMauId);

        var giaTri = hoSoId is null
            ? DuLieuMauTheoLoai(bieuMau.Loai)
            : await DuLieuHoSoAsync(hoSoId.Value, ct).ConfigureAwait(false);

        return PhanGiai(bieuMau, giaTri, hoSoId is null);
    }

    /// <summary>Sinh noi dung bieu mau bien ban cho mot phien hop.</summary>
    public async Task<BieuMauDaPhanGiai> SinhChoPhienHopAsync(
        Guid bieuMauId, Guid phienHopId, CancellationToken ct = default)
    {
        var bieuMau = await _db.BieuMauXuat.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == bieuMauId, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("biểu mẫu", bieuMauId);

        var giaTri = await DuLieuPhienHopAsync(phienHopId, ct).ConfigureAwait(false);

        return PhanGiai(bieuMau, giaTri, false);
    }

    /// <summary>
    /// Tim bieu mau dang hoat dong theo loai. Khong co thi tra null de noi goi dung bo cuc mac
    /// dinh — thieu cau hinh bieu mau khong duoc lam nguoi dung mat luon chuc nang in.
    /// </summary>
    public async Task<BieuMauXuat?> TimMauHoatDongAsync(string loai, CancellationToken ct = default)
        => await _db.BieuMauXuat.AsNoTracking()
            .Where(x => x.Loai == loai && x.TrangThai == 1)
            .OrderBy(x => x.ThuTu)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

    private static BieuMauDaPhanGiai PhanGiai(
        BieuMauXuat bieuMau, IReadOnlyDictionary<string, string?> giaTri, bool laDuLieuMau)
    {
        var dong = new List<DongBieuMauDaPhanGiai>();
        var thieu = new List<string>();

        foreach (var truong in bieuMau.CauHinhTruong)
        {
            var nhan = string.IsNullOrWhiteSpace(truong.Placeholder)
                ? truong.Nguon
                : truong.Placeholder;

            if (giaTri.TryGetValue(truong.Nguon, out var v))
            {
                dong.Add(new DongBieuMauDaPhanGiai(nhan, DinhDang(v, truong)));
                continue;
            }

            // Nguon khong khop: van in ra dong do nhung de trong va bao len tren, de nguoi cau hinh
            // sua duoc anh xa sai thay vi thay van ban thieu o ma khong hieu tai sao.
            thieu.Add(truong.Nguon);
            dong.Add(new DongBieuMauDaPhanGiai(nhan, null));
        }

        // Bieu mau chua cau hinh truong nao thi in toan bo du lieu co san, con hon in ra to giay
        // trang: quan tri vien nhin thay du lieu se biet minh chua anh xa truong.
        if (dong.Count == 0)
        {
            dong.AddRange(giaTri.Select(x => new DongBieuMauDaPhanGiai(x.Key, x.Value)));
        }

        return new BieuMauDaPhanGiai(
            bieuMau.Ten,
            laDuLieuMau ? "XEM TRƯỚC — số liệu bên dưới là dữ liệu mẫu, không phải hồ sơ thật" : null,
            dong,
            thieu);
    }

    private static string? DinhDang(string? giaTri, CauHinhTruongBieuMau truong)
    {
        if (string.IsNullOrWhiteSpace(giaTri)) return giaTri;

        if (truong.Kieu == "date"
            && DateTimeOffset.TryParse(giaTri, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var ngay))
        {
            return ngay.ToLocalTime()
                .ToString(truong.DinhDangHienThi ?? "dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"));
        }

        if (truong.Kieu == "number"
            && decimal.TryParse(giaTri, NumberStyles.Any, CultureInfo.InvariantCulture, out var so))
        {
            return so.ToString(truong.DinhDangHienThi ?? "#,0.##", CultureInfo.GetCultureInfo("vi-VN"));
        }

        return giaTri;
    }

    private static Dictionary<string, string?> DuLieuMauTheoLoai(string loai)
        => TruongKhaDung(loai).ToDictionary(x => x.Nguon, x => (string?)x.ViDu);

    private async Task<Dictionary<string, string?>> DuLieuHoSoAsync(Guid hoSoId, CancellationToken ct)
    {
        var hoSo = await _db.SangKien.AsNoTracking()
            .Where(x => x.Id == hoSoId)
            .Select(x => new
            {
                x.MaHoSo,
                x.TenSangKien,
                x.TrangThaiTong,
                x.NgayNop,
                x.HanXuLyHienTai,
                x.TongDiem,
                x.DiemTrungBinh,
                x.TyLeTrungLap,
                x.NgayCongNhan,
                x.GiaTriLamLoiUocTinh,
                x.KetQua,
                x.PhamViApDung,
                x.HieuQuaKinhTe,
                TenMucCongNhan = _db.MucCongNhan.Where(m => m.Id == x.MucCongNhanId)
                    .Select(m => m.Ten).FirstOrDefault(),
                TenLinhVuc = _db.LinhVuc.Where(l => l.Id == x.LinhVucId)
                    .Select(l => l.Ten).FirstOrDefault(),
                TenDonVi = _db.DonVi.Where(d => d.Id == x.DonViId)
                    .Select(d => d.Ten).FirstOrDefault(),
                TenDot = _db.DotDeNghi.Where(d => d.Id == x.DotDeNghiId)
                    .Select(d => d.Ten).FirstOrDefault(),
                TacGia = _db.SangKienTacGia.Where(t => t.SangKienId == x.Id)
                    .OrderByDescending(t => t.LaTacGiaChinh).ThenBy(t => t.ThuTu)
                    .Select(t => new { t.HoTen, t.LaTacGiaChinh })
                    .ToList(),
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("hồ sơ sáng kiến", hoSoId);

        return new Dictionary<string, string?>
        {
            ["maHoSo"] = hoSo.MaHoSo,
            ["tenSangKien"] = hoSo.TenSangKien,
            ["tenLinhVuc"] = hoSo.TenLinhVuc,
            ["tenDonVi"] = hoSo.TenDonVi,
            ["tenDot"] = hoSo.TenDot,
            ["tacGiaChinh"] = hoSo.TacGia.FirstOrDefault(t => t.LaTacGiaChinh)?.HoTen
                ?? hoSo.TacGia.FirstOrDefault()?.HoTen,
            ["dongTacGia"] = string.Join(", ",
                hoSo.TacGia.Where(t => !t.LaTacGiaChinh).Select(t => t.HoTen)),
            ["ngayNop"] = hoSo.NgayNop?.ToString("o", CultureInfo.InvariantCulture),
            ["hanXuLy"] = hoSo.HanXuLyHienTai?.ToString("o", CultureInfo.InvariantCulture),
            ["ngayCongNhan"] = hoSo.NgayCongNhan?.ToString("yyyy-MM-dd"),
            ["trangThaiTong"] = TenTrangThai(hoSo.TrangThaiTong),
            ["tongDiem"] = hoSo.TongDiem?.ToString(CultureInfo.InvariantCulture),
            ["diemTrungBinh"] = hoSo.DiemTrungBinh?.ToString(CultureInfo.InvariantCulture),
            ["tenMucCongNhan"] = hoSo.TenMucCongNhan,
            ["tyLeTrungLap"] = hoSo.TyLeTrungLap?.ToString(CultureInfo.InvariantCulture),
            ["ketQua"] = hoSo.KetQua switch
            {
                "DAT" => "Đạt",
                "KHONG_DAT" => "Không đạt",
                _ => null,
            },
            ["phamViApDung"] = hoSo.PhamViApDung,
            ["hieuQuaKinhTe"] = hoSo.HieuQuaKinhTe,
            ["giaTriLamLoi"] = hoSo.GiaTriLamLoiUocTinh?.ToString(CultureInfo.InvariantCulture),
        };
    }

    private async Task<Dictionary<string, string?>> DuLieuPhienHopAsync(
        Guid phienHopId, CancellationToken ct)
    {
        var phien = await _db.PhienHop.AsNoTracking()
            .Where(x => x.Id == phienHopId)
            .Select(x => new
            {
                x.TenPhien,
                x.ThoiGianBatDau,
                x.DiaDiem,
                x.KetLuan,
                TenHoiDong = _db.HoiDong.Where(h => h.Id == x.HoiDongId)
                    .Select(h => h.Ten).FirstOrDefault(),
                SoHoSo = _db.PhienHopHoSo.Count(h => h.PhienHopId == x.Id),
                SoCoMat = _db.PhienHopDiemDanh.Count(d => d.PhienHopId == x.Id && d.CoMat),
                TongThanhVien = _db.PhienHopDiemDanh.Count(d => d.PhienHopId == x.Id),
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("phiên họp", phienHopId);

        return new Dictionary<string, string?>
        {
            ["tenPhien"] = phien.TenPhien,
            ["tenHoiDong"] = phien.TenHoiDong,
            ["thoiGianHop"] = phien.ThoiGianBatDau.ToString("o", CultureInfo.InvariantCulture),
            ["diaDiem"] = phien.DiaDiem,
            ["soCoMat"] = $"{phien.SoCoMat}/{phien.TongThanhVien}",
            ["soHoSo"] = phien.SoHoSo.ToString(CultureInfo.InvariantCulture),
            ["ketLuan"] = phien.KetLuan,
        };
    }

    private static string TenTrangThai(string ma) => ma switch
    {
        TrangThaiTongHoSo.Nhap => "Nháp",
        TrangThaiTongHoSo.DaNop => "Đã nộp",
        TrangThaiTongHoSo.DangXuLy => "Đang xử lý",
        TrangThaiTongHoSo.YeuCauBoSung => "Yêu cầu bổ sung",
        TrangThaiTongHoSo.DaPheDuyet => "Đã phê duyệt",
        TrangThaiTongHoSo.KhongDat => "Không đạt",
        TrangThaiTongHoSo.DaRut => "Đã rút",
        _ => ma,
    };
}
