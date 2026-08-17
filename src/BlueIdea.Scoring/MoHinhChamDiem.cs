using BlueIdea.Domain.TieuChi;

namespace BlueIdea.Scoring;

/// <summary>Ket qua tinh diem cua MOT phieu danh gia.</summary>
public sealed class KetQuaChamDiem
{
    public bool HopLe { get; init; }

    public IReadOnlyList<string> DanhSachLoi { get; init; } = Array.Empty<string>();

    /// <summary>Diem cuoi cung cua phieu (da ap dung cach tinh + lam tron).</summary>
    public decimal TongDiem { get; init; }

    /// <summary>Tong diem tho (cong don diem cac tieu chi) truoc khi ap trong so.</summary>
    public decimal TongDiemTho { get; init; }

    /// <summary>Diem theo tung nhom tieu chi: nhomId -&gt; diem.</summary>
    public IReadOnlyDictionary<Guid, decimal> DiemTheoNhom { get; init; }
        = new Dictionary<Guid, decimal>();

    /// <summary>Ty le phan tram so voi thang diem toi da.</summary>
    public decimal TyLePhanTram { get; init; }

    public bool Dat { get; init; }

    public MucCongNhan? MucCongNhan { get; init; }
}

/// <summary>Ket qua tong hop diem cua ca hoi dong tren mot ho so.</summary>
public sealed class KetQuaTongHop
{
    public int SoPhieu { get; init; }

    /// <summary>So phieu thuc su duoc dung sau khi loai diem cao/thap (neu co cau hinh).</summary>
    public int SoPhieuSuDung { get; init; }

    public decimal DiemCaoNhat { get; init; }

    public decimal DiemThapNhat { get; init; }

    public decimal DiemTrungBinh { get; init; }

    /// <summary>Diem cuoi cung theo cach tinh cua bo tieu chi (da lam tron).</summary>
    public decimal DiemCuoiCung { get; init; }

    public IReadOnlyDictionary<Guid, decimal> DiemTrungBinhTheoNhom { get; init; }
        = new Dictionary<Guid, decimal>();

    /// <summary>Cac phieu bi loai khi ap dung quy tac loai diem cao nhat/thap nhat.</summary>
    public IReadOnlyList<Guid> PhieuBiLoai { get; init; } = Array.Empty<Guid>();

    public bool Dat { get; init; }

    public MucCongNhan? MucCongNhan { get; init; }

    public IReadOnlyList<string> DanhSachCanhBao { get; init; } = Array.Empty<string>();
}
