using BlueIdea.Domain.QuyTrinh;

namespace BlueIdea.Application.CauHinhQuyTrinh;

/// <summary>Toan bo so do quy trinh gui len tu trinh thiet ke ReactFlow (luu trong 1 transaction).</summary>
public sealed class SoDoQuyTrinhDto
{
    public List<BuocDto> DanhSachBuoc { get; set; } = new();

    public List<TrangThaiToanCucDto> TrangThaiToanCuc { get; set; } = new();

    public List<ThanhPhanHoSoCauHinhDto> ThanhPhanHoSo { get; set; } = new();

    public List<ChucNangBoSungDto> ChucNangBoSung { get; set; } = new();

    /// <summary>Toa do node tren canvas ReactFlow.</summary>
    public Dictionary<string, object>? SoDoLayout { get; set; }
}

public sealed class BuocDto
{
    public Guid Id { get; set; }

    public string Ma { get; set; } = string.Empty;

    public string Ten { get; set; } = string.Empty;

    public int ThuTu { get; set; }

    public string LoaiBuoc { get; set; } = string.Empty;

    public int SoNgayXuLy { get; set; } = 3;

    public bool TinhTheoNgayLamViec { get; set; } = true;

    public bool BatBuocDinhKem { get; set; }

    public List<string> DanhSachTepBatBuoc { get; set; } = new();

    public bool BatBuocNhapYKien { get; set; }

    public bool ChoPhepUyQuyen { get; set; }

    public bool ChoPhepThuHoi { get; set; }

    public bool LaBuocBatDau { get; set; }

    public bool LaBuocKetThuc { get; set; }

    public int CanhBaoTruocHanGio { get; set; } = 24;

    public string? MoTaHuongDan { get; set; }

    public Guid? HoiDongId { get; set; }

    public Guid? BoTieuChiId { get; set; }

    public List<TacNhanDto> TacNhan { get; set; } = new();

    public List<TruongHopDto> TruongHop { get; set; } = new();

    public List<TrangThaiBuocDto> TrangThai { get; set; } = new();
}

public sealed class TacNhanDto
{
    public Guid Id { get; set; }

    public string LoaiTacNhan { get; set; } = string.Empty;

    public Guid? ThamChieuId { get; set; }

    public string? ThamChieuMa { get; set; }

    public string QuyTacXuLy { get; set; } = "MOT_NGUOI";

    public decimal? TyLeDongThuan { get; set; }

    public int ThuTu { get; set; }
}

public sealed class TruongHopDto
{
    public Guid Id { get; set; }

    public string Ma { get; set; } = string.Empty;

    public string Ten { get; set; } = string.Empty;

    public Guid? BuocTiepTheoId { get; set; }

    public Guid? TrangThaiGanId { get; set; }

    public BieuThucDieuKien? DieuKien { get; set; }

    public List<string> HanhDong { get; set; } = new();

    public Guid? MauThongBaoId { get; set; }

    public string? MauNut { get; set; }

    public int ThuTu { get; set; }

    public bool LaMacDinh { get; set; }
}

public class TrangThaiBuocDto
{
    public Guid Id { get; set; }

    public string Ma { get; set; } = string.Empty;

    public string Ten { get; set; } = string.Empty;

    public string? MauSac { get; set; }

    public string? Icon { get; set; }

    public bool LaTrangThaiKetThuc { get; set; }

    public bool HienThiChoTacGia { get; set; } = true;

    public int ThuTu { get; set; }
}

public sealed class TrangThaiToanCucDto : TrangThaiBuocDto
{
}

public sealed class ThanhPhanHoSoCauHinhDto
{
    public Guid Id { get; set; }

    public string Ma { get; set; } = string.Empty;

    public string Ten { get; set; } = string.Empty;

    public bool BatBuoc { get; set; }

    public string LoaiDuLieu { get; set; } = "CA_HAI";

    public List<string> DinhDangChoPhep { get; set; } = new();

    public int DungLuongToiDaMb { get; set; } = 20;

    public int SoLuongToiDa { get; set; } = 5;

    public int SoKyTuToiThieu { get; set; }

    public int SoKyTuToiDa { get; set; }

    public bool DungDeKiemTraTrungLap { get; set; }

    public int ThuTu { get; set; }

    public string? MoTaHuongDan { get; set; }
}

public sealed class ChucNangBoSungDto
{
    public Guid Id { get; set; }

    public Guid? BuocId { get; set; }

    public string MaChucNang { get; set; } = string.Empty;

    public bool BatBuoc { get; set; }

    public Dictionary<string, object>? CauHinh { get; set; }
}

/// <summary>Ket qua kiem tra tinh hop le tra ve cho trinh thiet ke.</summary>
public sealed record KetQuaKiemTraDto(
    bool HopLe,
    IReadOnlyList<PhatHienDto> DanhSachLoi,
    IReadOnlyList<PhatHienDto> DanhSachCanhBao);

public sealed record PhatHienDto(string Ma, string ThongBao, Guid? BuocId, string? TenBuoc);
