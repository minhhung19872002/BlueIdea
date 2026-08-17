using BlueIdea.Domain.QuyTrinh;
using BlueIdea.Domain.SangKien;
using BlueIdea.Shared.KetQua;

namespace BlueIdea.Application.SangKien;

/// <summary>
/// Chuc nang 24 - Kiem tra checklist thanh phan ho so.
/// Thuan logic de unit-test duoc: nhan cau hinh thanh phan + du lieu ho so, tra ve trang thai tung muc.
/// </summary>
public static class BoKiemTraThanhPhanHoSo
{
    /// <summary>
    /// Lap checklist cho toan bo thanh phan ho so cua quy trinh.
    /// </summary>
    public static IReadOnlyList<ThanhPhanHoSoDto> LapChecklist(
        IReadOnlyList<QuyTrinhThanhPhanHoSo> cauHinh,
        HoSoSangKien hoSo,
        IReadOnlyList<SangKienTepDinhKem> tepDinhKem)
    {
        ArgumentNullException.ThrowIfNull(cauHinh);
        ArgumentNullException.ThrowIfNull(hoSo);
        ArgumentNullException.ThrowIfNull(tepDinhKem);

        var soTepTheoThanhPhan = tepDinhKem
            .Where(t => !t.DaXoa)
            .GroupBy(t => t.ThanhPhanHoSoMa)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var ketQua = new List<ThanhPhanHoSoDto>(cauHinh.Count);

        foreach (var tp in cauHinh.Where(t => !t.DaXoa).OrderBy(t => t.ThuTu))
        {
            var noiDung = LayNoiDung(hoSo, tp.Ma);
            var soTep = soTepTheoThanhPhan.GetValueOrDefault(tp.Ma);

            var (trangThai, canhBao) = DanhGiaThanhPhan(tp, noiDung, soTep);

            ketQua.Add(new ThanhPhanHoSoDto(
                tp.Ma, tp.Ten, tp.BatBuoc, tp.LoaiDuLieu,
                tp.SoKyTuToiThieu, tp.SoKyTuToiDa, tp.SoLuongToiDa, tp.DungLuongToiDaMb,
                tp.DinhDangChoPhep, tp.MoTaHuongDan, trangThai, canhBao));
        }

        return ketQua;
    }

    /// <summary>Danh sach thanh phan bat buoc con thieu - dung de chan nut "Nộp hồ sơ".</summary>
    public static IReadOnlyList<ThanhPhanHoSoDto> LayThanhPhanChuaDat(
        IReadOnlyList<ThanhPhanHoSoDto> checklist)
        => checklist
            .Where(t => t.BatBuoc && t.TrangThai is TrangThaiThanhPhan.Thieu or TrangThaiThanhPhan.ChuaDat)
            .ToList();

    private static (string TrangThai, string? CanhBao) DanhGiaThanhPhan(
        QuyTrinhThanhPhanHoSo tp, string? noiDung, int soTep)
    {
        var canVanBan = tp.LoaiDuLieu is LoaiDuLieuThanhPhan.VanBan or LoaiDuLieuThanhPhan.CaHai;
        var canTep = tp.LoaiDuLieu is LoaiDuLieuThanhPhan.Tep or LoaiDuLieuThanhPhan.CaHai;

        var doDai = noiDung?.Trim().Length ?? 0;
        var coVanBan = doDai > 0;
        var coTep = soTep > 0;

        // Voi loai CA_HAI, chi can co it nhat mot trong hai la duoc coi la da nhap.
        var daNhap = tp.LoaiDuLieu switch
        {
            LoaiDuLieuThanhPhan.VanBan => coVanBan,
            LoaiDuLieuThanhPhan.Tep => coTep,
            _ => coVanBan || coTep
        };

        if (!daNhap)
        {
            return tp.BatBuoc
                ? (TrangThaiThanhPhan.Thieu, $"Chưa nhập '{tp.Ten}'.")
                : (TrangThaiThanhPhan.KhongBatBuoc, null);
        }

        if (canVanBan && coVanBan && tp.SoKyTuToiThieu > 0 && doDai < tp.SoKyTuToiThieu)
        {
            return (TrangThaiThanhPhan.ChuaDat,
                $"'{tp.Ten}' cần tối thiểu {tp.SoKyTuToiThieu} ký tự, hiện có {doDai}.");
        }

        if (canVanBan && coVanBan && tp.SoKyTuToiDa > 0 && doDai > tp.SoKyTuToiDa)
        {
            return (TrangThaiThanhPhan.ChuaDat,
                $"'{tp.Ten}' vượt quá {tp.SoKyTuToiDa} ký tự, hiện có {doDai}.");
        }

        if (canTep && tp.SoLuongToiDa > 0 && soTep > tp.SoLuongToiDa)
        {
            return (TrangThaiThanhPhan.ChuaDat,
                $"'{tp.Ten}' chỉ cho phép tối đa {tp.SoLuongToiDa} tệp, hiện có {soTep}.");
        }

        return (TrangThaiThanhPhan.Du, null);
    }

    /// <summary>
    /// Lay noi dung cua mot thanh phan: uu tien truong co dinh tren bang <c>sang_kien</c>,
    /// neu khong co thi lay tu <c>noi_dung_dong</c> (jsonb).
    /// </summary>
    private static string? LayNoiDung(HoSoSangKien hoSo, string ma) => ma.ToUpperInvariant() switch
    {
        "MO_TA_GIAI_PHAP" => hoSo.MoTaGiaiPhap,
        "TINH_TRANG_TRUOC" => hoSo.TinhTrangTruocKhiApDung,
        "NOI_DUNG_GIAI_PHAP" => hoSo.NoiDungGiaiPhap,
        "TINH_MOI" => hoSo.TinhMoi,
        "KHA_NANG_AP_DUNG" => hoSo.KhaNangApDung,
        "PHAM_VI_AP_DUNG" => hoSo.PhamViApDung,
        "HIEU_QUA_KINH_TE" => hoSo.HieuQuaKinhTe,
        "HIEU_QUA_XA_HOI" => hoSo.HieuQuaXaHoi,
        _ => hoSo.NoiDungDong.GetValueOrDefault(ma)
    };
}

/// <summary>Kiem tra danh sach tac gia theo rang buoc nghiep vu.</summary>
public static class BoKiemTraTacGia
{
    /// <summary>Sai so cho phep khi cong don ty le dong gop (tranh loi lam tron cua UI).</summary>
    private const decimal SaiSoChoPhep = 0.01m;

    /// <summary>
    /// Rang buoc: tong ty le dong gop = 100%, so tac gia khong vuot gioi han cua loai tac gia,
    /// co dung 1 tac gia chinh.
    /// </summary>
    public static IReadOnlyList<ChiTietLoi> KiemTra(
        IReadOnlyList<TacGiaDto> danhSach, int? soTacGiaToiDa, bool? choPhepNhieuTacGia)
    {
        ArgumentNullException.ThrowIfNull(danhSach);
        var loi = new List<ChiTietLoi>();

        if (danhSach.Count == 0)
        {
            loi.Add(new ChiTietLoi("danhSachTacGia", "Hồ sơ phải có ít nhất một tác giả."));
            return loi;
        }

        if (choPhepNhieuTacGia == false && danhSach.Count > 1)
        {
            loi.Add(new ChiTietLoi("danhSachTacGia",
                "Loại tác giả đã chọn không cho phép nhiều tác giả."));
        }

        if (soTacGiaToiDa is > 0 && danhSach.Count > soTacGiaToiDa)
        {
            loi.Add(new ChiTietLoi("danhSachTacGia",
                $"Số tác giả ({danhSach.Count}) vượt quá giới hạn cho phép ({soTacGiaToiDa})."));
        }

        var tong = danhSach.Sum(t => t.TyLeDongGop);
        if (Math.Abs(tong - 100m) > SaiSoChoPhep)
        {
            loi.Add(new ChiTietLoi("tyLeDongGop",
                $"Tổng tỷ lệ đóng góp của các tác giả phải bằng 100%, hiện tại là {tong}%."));
        }

        if (danhSach.Any(t => t.TyLeDongGop <= 0))
        {
            loi.Add(new ChiTietLoi("tyLeDongGop", "Tỷ lệ đóng góp của mỗi tác giả phải lớn hơn 0."));
        }

        if (danhSach.Any(t => string.IsNullOrWhiteSpace(t.HoTen)))
        {
            loi.Add(new ChiTietLoi("hoTen", "Vui lòng nhập họ tên cho tất cả tác giả."));
        }

        var soTacGiaChinh = danhSach.Count(t => t.LaTacGiaChinh);
        if (soTacGiaChinh == 0)
        {
            loi.Add(new ChiTietLoi("laTacGiaChinh", "Phải chỉ định một tác giả chính."));
        }
        else if (soTacGiaChinh > 1)
        {
            loi.Add(new ChiTietLoi("laTacGiaChinh",
                $"Chỉ được có một tác giả chính, hiện đang chọn {soTacGiaChinh}."));
        }

        return loi;
    }
}
