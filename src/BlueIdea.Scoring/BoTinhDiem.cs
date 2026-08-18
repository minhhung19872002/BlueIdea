using BlueIdea.Domain.Chung;
using BlueIdea.Domain.SangKien;
using BlueIdea.Domain.TieuChi;

namespace BlueIdea.Scoring;

/// <summary>
/// Engine tinh diem (Muc 5 - Nhom III dac ta). Thuan logic, khong phu thuoc CSDL.
/// </summary>
public interface IBoTinhDiem
{
    /// <summary>Tinh diem cua mot phieu danh gia theo bo tieu chi.</summary>
    KetQuaChamDiem TinhDiemPhieu(PhieuDanhGia phieu, BoTieuChi boTieuChi);

    /// <summary>Tong hop diem cua nhieu phieu (ca hoi dong) tren cung mot ho so.</summary>
    KetQuaTongHop TongHopDiemHoiDong(IEnumerable<PhieuDanhGia> cacPhieu, BoTieuChi boTieuChi);

    /// <summary>Xac dinh muc cong nhan theo khoang diem cau hinh.</summary>
    MucCongNhan? XacDinhMucCongNhan(decimal diem, BoTieuChi boTieuChi);

    /// <summary>Kiem tra tinh hop le cua bo tieu chi (trong so, khoang diem muc cong nhan).</summary>
    IReadOnlyList<string> KiemTraBoTieuChi(BoTieuChi boTieuChi);
}

/// <summary>
/// Hop dong theo dung dac ta (Muc 5 - Nhom III). Cai dat o tang Infrastructure:
/// nap bo tieu chi tu CSDL roi uy quyen cho <see cref="IBoTinhDiem"/>.
/// </summary>
public interface IScoringEngine
{
    KetQuaChamDiem TinhDiemPhieu(PhieuDanhGia phieu, BoTieuChi boTieuChi);

    KetQuaTongHop TongHopDiemHoiDong(IEnumerable<PhieuDanhGia> cacPhieu, BoTieuChi boTieuChi);

    MucCongNhan? XacDinhMucCongNhan(decimal diem, Guid boTieuChiId);
}

/// <inheritdoc />
public sealed class BoTinhDiem : IBoTinhDiem
{
    public KetQuaChamDiem TinhDiemPhieu(PhieuDanhGia phieu, BoTieuChi boTieuChi)
    {
        ArgumentNullException.ThrowIfNull(phieu);
        ArgumentNullException.ThrowIfNull(boTieuChi);

        var loi = new List<string>();
        var nhomTheoId = boTieuChi.DanhSachNhom.Where(n => !n.DaXoa).ToDictionary(n => n.Id);
        var tieuChiTheoId = boTieuChi.DanhSachNhom
            .Where(n => !n.DaXoa)
            .SelectMany(n => n.DanhSachTieuChi.Where(t => !t.DaXoa))
            .ToDictionary(t => t.Id);

        var diemTheoNhom = nhomTheoId.Keys.ToDictionary(id => id, _ => 0m);
        var tongTho = 0m;

        foreach (var chiTiet in phieu.ChiTiet.Where(c => !c.DaXoa))
        {
            if (!tieuChiTheoId.TryGetValue(chiTiet.TieuChiId, out var tieuChi))
            {
                loi.Add($"Tiêu chí '{chiTiet.TenTieuChiSnapshot}' không thuộc bộ tiêu chí đang áp dụng.");
                continue;
            }

            if (chiTiet.Diem < 0)
            {
                loi.Add($"Điểm của tiêu chí '{tieuChi.Ten}' không được âm.");
                continue;
            }

            if (chiTiet.Diem > tieuChi.DiemToiDa)
            {
                loi.Add($"Điểm tiêu chí '{tieuChi.Ten}' ({chiTiet.Diem}) vượt điểm tối đa ({tieuChi.DiemToiDa}).");
                continue;
            }

            if (tieuChi.BatBuocNhanXet && string.IsNullOrWhiteSpace(chiTiet.NhanXet))
            {
                loi.Add($"Tiêu chí '{tieuChi.Ten}' bắt buộc nhập nhận xét.");
            }

            tongTho += chiTiet.Diem;
            diemTheoNhom[tieuChi.NhomTieuChiId] = diemTheoNhom.GetValueOrDefault(tieuChi.NhomTieuChiId)
                                                  + chiTiet.Diem;
        }

        // Kiem tra da cham du tieu chi bat buoc chua.
        var daCham = phieu.ChiTiet.Where(c => !c.DaXoa).Select(c => c.TieuChiId).ToHashSet();
        var thieu = tieuChiTheoId.Values
            .Where(t => t.TrangThai == TrangThaiDanhMuc.HoatDong && !daCham.Contains(t.Id))
            .Select(t => t.Ten)
            .ToList();

        if (thieu.Count > 0)
        {
            loi.Add($"Chưa chấm {thieu.Count} tiêu chí: {string.Join(", ", thieu)}.");
        }

        var tongDiem = boTieuChi.CachTinh switch
        {
            CachTinhDiem.TrungBinhTrongSo => TinhTheoTrongSo(diemTheoNhom, nhomTheoId),
            CachTinhDiem.TrungBinhCong => nhomTheoId.Count > 0
                ? diemTheoNhom.Values.Sum() / nhomTheoId.Count
                : 0m,
            _ => tongTho
        };

        tongDiem = LamTron(tongDiem, boTieuChi.LamTron);

        var tyLe = boTieuChi.ThangDiemToiDa > 0
            ? LamTron(tongDiem / boTieuChi.ThangDiemToiDa * 100m, 2)
            : 0m;

        var muc = XacDinhMucCongNhan(tongDiem, boTieuChi);

        return new KetQuaChamDiem
        {
            HopLe = loi.Count == 0,
            DanhSachLoi = loi,
            TongDiem = tongDiem,
            TongDiemTho = LamTron(tongTho, boTieuChi.LamTron),
            DiemTheoNhom = diemTheoNhom.ToDictionary(c => c.Key, c => LamTron(c.Value, boTieuChi.LamTron)),
            TyLePhanTram = tyLe,
            Dat = tongDiem >= boTieuChi.DiemDatToiThieu,
            MucCongNhan = muc
        };
    }

    public KetQuaTongHop TongHopDiemHoiDong(IEnumerable<PhieuDanhGia> cacPhieu, BoTieuChi boTieuChi)
    {
        ArgumentNullException.ThrowIfNull(cacPhieu);
        ArgumentNullException.ThrowIfNull(boTieuChi);

        var canhBao = new List<string>();

        // Chi tong hop cac phieu DA GUI - phieu nhap khong duoc tinh.
        var phieus = cacPhieu.Where(p => !p.DaXoa && p.DaGui).ToList();
        if (phieus.Count == 0)
        {
            return new KetQuaTongHop
            {
                SoPhieu = 0,
                SoPhieuSuDung = 0,
                DanhSachCanhBao = new[] { "Chưa có phiếu đánh giá nào được gửi." }
            };
        }

        var diemMoiPhieu = phieus
            .Select(p => (Phieu: p, KetQua: TinhDiemPhieu(p, boTieuChi)))
            .ToList();

        var caoNhat = diemMoiPhieu.Max(d => d.KetQua.TongDiem);
        var thapNhat = diemMoiPhieu.Min(d => d.KetQua.TongDiem);

        // Quy tac loai 1 diem cao nhat + 1 diem thap nhat khi so phieu >= 5.
        var suDung = diemMoiPhieu;
        var biLoai = new List<Guid>();

        if (boTieuChi.LoaiBoDiemCaoThap)
        {
            if (diemMoiPhieu.Count >= 5)
            {
                var sapXep = diemMoiPhieu.OrderBy(d => d.KetQua.TongDiem).ToList();
                var phieuThap = sapXep.First();
                var phieuCao = sapXep.Last();
                biLoai.Add(phieuThap.Phieu.Id);
                biLoai.Add(phieuCao.Phieu.Id);
                suDung = sapXep.Skip(1).Take(sapXep.Count - 2).ToList();
            }
            else
            {
                canhBao.Add(
                    $"Cấu hình loại điểm cao/thấp chỉ áp dụng khi có từ 5 phiếu, hiện có {diemMoiPhieu.Count} phiếu.");
            }
        }

        var trungBinh = LamTron(suDung.Average(d => d.KetQua.TongDiem), boTieuChi.LamTron);

        // Diem trung binh theo tung nhom tieu chi (dung cho bang tong hop cua thu ky).
        var nhomIds = boTieuChi.DanhSachNhom.Where(n => !n.DaXoa).Select(n => n.Id).ToList();
        var trungBinhNhom = nhomIds.ToDictionary(
            id => id,
            id => LamTron(suDung.Average(d => d.KetQua.DiemTheoNhom.GetValueOrDefault(id)), boTieuChi.LamTron));

        // TONG_DIEM va TRUNG_BINH_TRONG_SO deu da duoc ap dung o muc phieu,
        // nen buoc tong hop giua cac phieu luon lay trung binh cong.
        var diemCuoi = trungBinh;

        var muc = XacDinhMucCongNhan(diemCuoi, boTieuChi);

        return new KetQuaTongHop
        {
            SoPhieu = phieus.Count,
            SoPhieuSuDung = suDung.Count,
            DiemCaoNhat = LamTron(caoNhat, boTieuChi.LamTron),
            DiemThapNhat = LamTron(thapNhat, boTieuChi.LamTron),
            DiemTrungBinh = trungBinh,
            DiemCuoiCung = diemCuoi,
            DiemTrungBinhTheoNhom = trungBinhNhom,
            PhieuBiLoai = biLoai,
            Dat = diemCuoi >= boTieuChi.DiemDatToiThieu,
            MucCongNhan = muc,
            DanhSachCanhBao = canhBao
        };
    }

    public MucCongNhan? XacDinhMucCongNhan(decimal diem, BoTieuChi boTieuChi)
    {
        ArgumentNullException.ThrowIfNull(boTieuChi);

        return boTieuChi.DanhSachMucCongNhan
            .Where(m => !m.DaXoa && m.TrangThai == TrangThaiDanhMuc.HoatDong && m.ChuaDiem(diem))
            .OrderByDescending(m => m.DiemTu)
            .FirstOrDefault();
    }

    public IReadOnlyList<string> KiemTraBoTieuChi(BoTieuChi boTieuChi)
    {
        ArgumentNullException.ThrowIfNull(boTieuChi);

        var loi = new List<string>();
        var nhom = boTieuChi.DanhSachNhom.Where(n => !n.DaXoa).ToList();

        if (nhom.Count == 0)
        {
            loi.Add("Bộ tiêu chí chưa có nhóm tiêu chí nào.");
            return loi;
        }

        foreach (var n in nhom.Where(n => n.DanhSachTieuChi.Count(t => !t.DaXoa) == 0))
        {
            loi.Add($"Nhóm '{n.Ten}' chưa có tiêu chí con.");
        }

        // Tong trong so phai bang 100% khi tinh theo trong so.
        if (boTieuChi.CachTinh == CachTinhDiem.TrungBinhTrongSo)
        {
            var tongTrongSo = nhom.Sum(n => n.TrongSo);
            if (Math.Abs(tongTrongSo - 100m) > 0.01m)
            {
                loi.Add($"Tổng trọng số các nhóm phải bằng 100%, hiện tại là {tongTrongSo}%.");
            }
        }

        // Tong diem toi da cac nhom khong duoc vuot thang diem.
        var tongDiemToiDa = nhom.Sum(n => n.DanhSachTieuChi.Where(t => !t.DaXoa).Sum(t => t.DiemToiDa));
        if (boTieuChi.CachTinh == CachTinhDiem.TongDiem
            && Math.Abs(tongDiemToiDa - boTieuChi.ThangDiemToiDa) > 0.01m)
        {
            loi.Add($"Tổng điểm tối đa của các tiêu chí ({tongDiemToiDa}) khác thang điểm "
                    + $"của bộ tiêu chí ({boTieuChi.ThangDiemToiDa}).");
        }

        foreach (var n in nhom)
        {
            var tongCon = n.DanhSachTieuChi.Where(t => !t.DaXoa).Sum(t => t.DiemToiDa);
            if (n.DiemToiDa > 0 && Math.Abs(tongCon - n.DiemToiDa) > 0.01m)
            {
                loi.Add($"Nhóm '{n.Ten}': tổng điểm tiêu chí con ({tongCon}) khác điểm tối đa của nhóm ({n.DiemToiDa}).");
            }
        }

        loi.AddRange(KiemTraKhoangMucCongNhan(boTieuChi));
        return loi;
    }

    /// <summary>Kiem tra khoang diem cac muc cong nhan khong chong lan va khong bi ho.</summary>
    private static IEnumerable<string> KiemTraKhoangMucCongNhan(BoTieuChi boTieuChi)
    {
        var loi = new List<string>();
        var mucs = boTieuChi.DanhSachMucCongNhan
            .Where(m => !m.DaXoa && m.TrangThai == TrangThaiDanhMuc.HoatDong)
            .OrderBy(m => m.DiemTu)
            .ToList();

        if (mucs.Count == 0)
        {
            return loi;
        }

        for (var i = 0; i < mucs.Count; i++)
        {
            if (mucs[i].DiemTu > mucs[i].DiemDen)
            {
                loi.Add($"Mức '{mucs[i].Ten}' có điểm từ ({mucs[i].DiemTu}) lớn hơn điểm đến ({mucs[i].DiemDen}).");
            }

            if (i == 0)
            {
                continue;
            }

            var truoc = mucs[i - 1];
            var hienTai = mucs[i];

            if (hienTai.DiemTu <= truoc.DiemDen)
            {
                loi.Add($"Khoảng điểm của mức '{truoc.Ten}' và '{hienTai.Ten}' bị chồng lấn "
                        + $"tại {hienTai.DiemTu}.");
            }
        }

        return loi;
    }

    private static decimal TinhTheoTrongSo(
        IReadOnlyDictionary<Guid, decimal> diemTheoNhom, IReadOnlyDictionary<Guid, NhomTieuChi> nhomTheoId)
    {
        var tong = 0m;
        foreach (var (nhomId, diem) in diemTheoNhom)
        {
            if (nhomTheoId.TryGetValue(nhomId, out var nhom))
            {
                tong += diem * nhom.TrongSo / 100m;
            }
        }

        return tong;
    }

    private static decimal LamTron(decimal giaTri, int soChuSo)
        => Math.Round(giaTri, Math.Clamp(soChuSo, 0, 6), MidpointRounding.AwayFromZero);
}
