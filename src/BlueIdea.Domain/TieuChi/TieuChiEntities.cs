using BlueIdea.Domain.Chung;

namespace BlueIdea.Domain.TieuChi;

/// <summary>Bo tieu chi cham diem (cau hinh dong theo nam / cap).</summary>
public class BoTieuChi : ThucTheDanhMuc
{
    public int Nam { get; set; }

    public string Cap { get; set; } = CapXetDuyet.CoSo;

    public decimal ThangDiemToiDa { get; set; } = 100m;

    public decimal DiemDatToiThieu { get; set; } = 50m;

    /// <summary>TONG_DIEM | TRUNG_BINH_CONG | TRUNG_BINH_TRONG_SO</summary>
    public string CachTinh { get; set; } = CachTinhDiem.TongDiem;

    /// <summary>So chu so thap phan khi lam tron ket qua.</summary>
    public int LamTron { get; set; } = 2;

    public bool ChoPhepChamDocLap { get; set; } = true;

    public bool TuDongTongHop { get; set; } = true;

    /// <summary>Loai 1 diem cao nhat + 1 thap nhat khi so phieu &gt;= 5.</summary>
    public bool LoaiBoDiemCaoThap { get; set; }

    public Dictionary<string, object>? PhamViApDung { get; set; }

    public List<NhomTieuChi> DanhSachNhom { get; set; } = new List<NhomTieuChi>();

    public List<MucCongNhan> DanhSachMucCongNhan { get; set; } = new List<MucCongNhan>();
}

/// <summary>Chuc nang 17 - Nhom tieu chi (Tinh moi, Hieu qua, Kha nang ap dung...).</summary>
public class NhomTieuChi : ThucTheDanhMuc
{
    public Guid BoTieuChiId { get; set; }

    public BoTieuChi? BoTieuChi { get; set; }

    /// <summary>Trong so nhom (%) - tong cac nhom phai = 100 khi cach tinh la TRUNG_BINH_TRONG_SO.</summary>
    public decimal TrongSo { get; set; }

    public decimal DiemToiDa { get; set; }

    public List<TieuChiChamDiem> DanhSachTieuChi { get; set; } = new List<TieuChiChamDiem>();
}

/// <summary>Chuc nang 18 - Tieu chi cham diem cu the.</summary>
public class TieuChiChamDiem : ThucTheDanhMuc
{
    public Guid NhomTieuChiId { get; set; }

    public NhomTieuChi? NhomTieuChi { get; set; }

    public decimal DiemToiDa { get; set; }

    public decimal DiemToiThieu { get; set; }

    public decimal TrongSo { get; set; } = 100m;

    /// <summary>NHAP_SO | THANG_DIEM | LUA_CHON | CO_KHONG</summary>
    public string KieuNhap { get; set; } = KieuNhapTieuChi.NhapSo;

    public decimal BuocNhay { get; set; } = 0.5m;

    public bool BatBuocNhanXet { get; set; }

    public string? HuongDanCham { get; set; }

    public List<TieuChiMucDiem> DanhSachMucDiem { get; set; } = new List<TieuChiMucDiem>();
}

/// <summary>Muc diem chon san khi tieu chi co kieu nhap LUA_CHON.</summary>
public class TieuChiMucDiem : ThucThe
{
    public Guid TieuChiId { get; set; }

    public TieuChiChamDiem? TieuChi { get; set; }

    public string Ten { get; set; } = string.Empty;

    public decimal Diem { get; set; }

    public string? MoTa { get; set; }

    public int ThuTu { get; set; }
}

/// <summary>Thang xep loai theo khoang diem.</summary>
public class MucCongNhan : ThucTheDanhMuc
{
    public Guid? BoTieuChiId { get; set; }

    public BoTieuChi? BoTieuChi { get; set; }

    public decimal DiemTu { get; set; }

    public decimal DiemDen { get; set; }

    public string? MauSac { get; set; }

    /// <summary>Muc nay co nghia la "duoc cong nhan" hay khong.</summary>
    public bool LaDat { get; set; } = true;

    /// <summary>Kiem tra diem co roi vao khoang [DiemTu, DiemDen] hay khong.</summary>
    public bool ChuaDiem(decimal diem) => diem >= DiemTu && diem <= DiemDen;
}
