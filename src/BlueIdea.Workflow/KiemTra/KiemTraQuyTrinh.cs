using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuyTrinh;
using BlueIdea.Workflow.DieuKien;

namespace BlueIdea.Workflow.KiemTra;

/// <summary>Muc do cua mot phat hien khi kiem tra quy trinh.</summary>
public enum MucDoPhatHien
{
    CanhBao = 0,
    Loi = 1
}

/// <summary>Mot phat hien khi kiem tra tinh hop le cua quy trinh.</summary>
public sealed record PhatHienQuyTrinh(
    MucDoPhatHien MucDo,
    string Ma,
    string ThongBao,
    Guid? BuocId = null,
    string? TenBuoc = null);

/// <summary>Ket qua kiem tra quy trinh truoc khi kich hoat.</summary>
public sealed class KetQuaKiemTraQuyTrinh
{
    private readonly List<PhatHienQuyTrinh> _phatHien = new();

    public IReadOnlyList<PhatHienQuyTrinh> PhatHien => _phatHien;

    public IReadOnlyList<PhatHienQuyTrinh> DanhSachLoi
        => _phatHien.Where(p => p.MucDo == MucDoPhatHien.Loi).ToList();

    public IReadOnlyList<PhatHienQuyTrinh> DanhSachCanhBao
        => _phatHien.Where(p => p.MucDo == MucDoPhatHien.CanhBao).ToList();

    /// <summary>Quy trinh chi duoc kich hoat khi khong con loi (canh bao van cho phep).</summary>
    public bool HopLe => DanhSachLoi.Count == 0;

    public void ThemLoi(string ma, string thongBao, QuyTrinhBuoc? buoc = null)
        => _phatHien.Add(new PhatHienQuyTrinh(MucDoPhatHien.Loi, ma, thongBao, buoc?.Id, buoc?.Ten));

    public void ThemCanhBao(string ma, string thongBao, QuyTrinhBuoc? buoc = null)
        => _phatHien.Add(new PhatHienQuyTrinh(MucDoPhatHien.CanhBao, ma, thongBao, buoc?.Id, buoc?.Ten));
}

/// <summary>Ma phat hien - frontend dua vao ma nay de hien thi va dieu huong toi node loi.</summary>
public static class MaPhatHienQuyTrinh
{
    public const string KhongCoBuoc = "KHONG_CO_BUOC";
    public const string KhongCoBuocBatDau = "KHONG_CO_BUOC_BAT_DAU";
    public const string NhieuBuocBatDau = "NHIEU_BUOC_BAT_DAU";
    public const string KhongCoBuocKetThuc = "KHONG_CO_BUOC_KET_THUC";
    public const string BuocMoCoi = "BUOC_MO_COI";
    public const string BuocCut = "BUOC_CUT";
    public const string VongLapVoHan = "VONG_LAP_VO_HAN";
    public const string BuocKhongCoTacNhan = "BUOC_KHONG_CO_TAC_NHAN";
    public const string BuocChamDiemThieuCauHinh = "BUOC_CHAM_DIEM_THIEU_CAU_HINH";
    public const string DieuKienKhongPhuHet = "DIEU_KIEN_KHONG_PHU_HET";
    public const string DieuKienMauThuan = "DIEU_KIEN_MAU_THUAN";
    public const string DieuKienSaiCuPhap = "DIEU_KIEN_SAI_CU_PHAP";
    public const string BuocTiepTheoKhongTonTai = "BUOC_TIEP_THEO_KHONG_TON_TAI";
    public const string TrungMaBuoc = "TRUNG_MA_BUOC";
    public const string KhongDenDuocBuocKetThuc = "KHONG_DEN_DUOC_BUOC_KET_THUC";
}

/// <summary>
/// Bo kiem tra tinh hop le cua quy trinh (chay khi bam "Kich hoat").
/// Cai dat day du 7 rule bat buoc trong Muc 5 - Nhom II cua dac ta.
/// </summary>
public interface IKiemTraQuyTrinh
{
    KetQuaKiemTraQuyTrinh KiemTra(QuyTrinh quyTrinh);
}

/// <inheritdoc />
public sealed class KiemTraQuyTrinh : IKiemTraQuyTrinh
{
    private readonly IBoDanhGiaDieuKien _boDanhGia;

    public KiemTraQuyTrinh(IBoDanhGiaDieuKien? boDanhGia = null)
        => _boDanhGia = boDanhGia ?? new BoDanhGiaDieuKien();

    public KetQuaKiemTraQuyTrinh KiemTra(QuyTrinh quyTrinh)
    {
        ArgumentNullException.ThrowIfNull(quyTrinh);

        var ketQua = new KetQuaKiemTraQuyTrinh();
        var buocs = quyTrinh.DanhSachBuoc.Where(b => !b.DaXoa).ToList();

        if (buocs.Count == 0)
        {
            ketQua.ThemLoi(MaPhatHienQuyTrinh.KhongCoBuoc, "Quy trình chưa có bước xử lý nào.");
            return ketQua;
        }

        KiemTraTrungMa(buocs, ketQua);
        var buocBatDau = KiemTraBuocDauCuoi(buocs, ketQua);      // Rule 1
        KiemTraBuocTiepTheoTonTai(buocs, ketQua);
        KiemTraBuocMoCoi(buocs, buocBatDau, ketQua);              // Rule 2
        KiemTraBuocCut(buocs, ketQua);                            // Rule 3
        KiemTraVongLap(buocs, ketQua);                            // Rule 4
        KiemTraTacNhan(buocs, ketQua);                            // Rule 5
        KiemTraBuocChamDiem(buocs, ketQua);                       // Rule 6
        KiemTraDieuKienChuyenTiep(buocs, ketQua);                 // Rule 7

        if (buocBatDau is not null)
        {
            KiemTraDenDuocKetThuc(buocs, buocBatDau, ketQua);
        }

        return ketQua;
    }

    private static void KiemTraTrungMa(List<QuyTrinhBuoc> buocs, KetQuaKiemTraQuyTrinh ketQua)
    {
        var trung = buocs
            .Where(b => !string.IsNullOrWhiteSpace(b.Ma))
            .GroupBy(b => b.Ma, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1);

        foreach (var nhom in trung)
        {
            ketQua.ThemLoi(
                MaPhatHienQuyTrinh.TrungMaBuoc,
                $"Mã bước '{nhom.Key}' bị trùng ở {nhom.Count()} bước.",
                nhom.First());
        }
    }

    /// <summary>Rule 1: co dung 1 buoc bat dau va it nhat 1 buoc ket thuc.</summary>
    private static QuyTrinhBuoc? KiemTraBuocDauCuoi(List<QuyTrinhBuoc> buocs, KetQuaKiemTraQuyTrinh ketQua)
    {
        var batDau = buocs.Where(b => b.LaBuocBatDau).ToList();
        var ketThuc = buocs.Where(b => b.LaBuocKetThuc).ToList();

        switch (batDau.Count)
        {
            case 0:
                ketQua.ThemLoi(MaPhatHienQuyTrinh.KhongCoBuocBatDau,
                    "Quy trình phải có đúng 1 bước bắt đầu, hiện chưa có bước nào được đánh dấu.");
                break;
            case > 1:
                ketQua.ThemLoi(MaPhatHienQuyTrinh.NhieuBuocBatDau,
                    $"Quy trình chỉ được có 1 bước bắt đầu, hiện có {batDau.Count}: "
                    + string.Join(", ", batDau.Select(b => b.Ten)));
                break;
        }

        if (ketThuc.Count == 0)
        {
            ketQua.ThemLoi(MaPhatHienQuyTrinh.KhongCoBuocKetThuc,
                "Quy trình phải có ít nhất 1 bước kết thúc.");
        }

        return batDau.Count == 1 ? batDau[0] : batDau.FirstOrDefault();
    }

    private static void KiemTraBuocTiepTheoTonTai(List<QuyTrinhBuoc> buocs, KetQuaKiemTraQuyTrinh ketQua)
    {
        var idHopLe = buocs.Select(b => b.Id).ToHashSet();

        foreach (var buoc in buocs)
        {
            foreach (var th in buoc.TruongHop.Where(t => !t.DaXoa && t.BuocTiepTheoId.HasValue))
            {
                if (!idHopLe.Contains(th.BuocTiepTheoId!.Value))
                {
                    ketQua.ThemLoi(MaPhatHienQuyTrinh.BuocTiepTheoKhongTonTai,
                        $"Trường hợp '{th.Ten}' của bước '{buoc.Ten}' trỏ tới bước không tồn tại.",
                        buoc);
                }
            }
        }
    }

    /// <summary>Rule 2: khong co buoc "mo coi" (khong co duong vao, tru buoc bat dau).</summary>
    private static void KiemTraBuocMoCoi(
        List<QuyTrinhBuoc> buocs, QuyTrinhBuoc? buocBatDau, KetQuaKiemTraQuyTrinh ketQua)
    {
        var coDuongVao = buocs
            .SelectMany(b => b.TruongHop.Where(t => !t.DaXoa))
            .Where(t => t.BuocTiepTheoId.HasValue)
            .Select(t => t.BuocTiepTheoId!.Value)
            .ToHashSet();

        foreach (var buoc in buocs)
        {
            if (buoc.LaBuocBatDau || buoc.Id == buocBatDau?.Id)
            {
                continue;
            }

            if (!coDuongVao.Contains(buoc.Id))
            {
                ketQua.ThemLoi(MaPhatHienQuyTrinh.BuocMoCoi,
                    $"Bước '{buoc.Ten}' không có đường vào từ bước nào khác (bước mồ côi).",
                    buoc);
            }
        }
    }

    /// <summary>Rule 3: khong co buoc "cut" (khong co truong hop di ra, tru buoc ket thuc).</summary>
    private static void KiemTraBuocCut(List<QuyTrinhBuoc> buocs, KetQuaKiemTraQuyTrinh ketQua)
    {
        foreach (var buoc in buocs)
        {
            if (buoc.LaBuocKetThuc)
            {
                continue;
            }

            var truongHop = buoc.TruongHop.Where(t => !t.DaXoa).ToList();
            if (truongHop.Count == 0)
            {
                ketQua.ThemLoi(MaPhatHienQuyTrinh.BuocCut,
                    $"Bước '{buoc.Ten}' không có trường hợp chuyển tiếp nào và cũng không phải bước kết thúc.",
                    buoc);
            }
        }
    }

    /// <summary>
    /// Rule 4: phat hien vong lap khong co dieu kien thoat.
    /// DFS tim chu trinh; chu trinh chi bi coi la nguy hiem khi TAT CA canh trong chu trinh
    /// deu khong co dieu kien (luon di) -> chac chan lap vo han.
    /// </summary>
    private static void KiemTraVongLap(List<QuyTrinhBuoc> buocs, KetQuaKiemTraQuyTrinh ketQua)
    {
        var bang = buocs.ToDictionary(b => b.Id);
        var mauSac = buocs.ToDictionary(b => b.Id, _ => 0); // 0 trang, 1 xam (dang duyet), 2 den (xong)
        var duongDi = new List<(Guid BuocId, QuyTrinhTruongHop TruongHop)>();
        var daBaoCao = new HashSet<string>(StringComparer.Ordinal);

        void Duyet(Guid buocId)
        {
            mauSac[buocId] = 1;

            foreach (var th in bang[buocId].TruongHop.Where(t => !t.DaXoa && t.BuocTiepTheoId.HasValue))
            {
                var tiep = th.BuocTiepTheoId!.Value;
                if (!bang.ContainsKey(tiep))
                {
                    continue;
                }

                duongDi.Add((buocId, th));

                if (mauSac[tiep] == 1)
                {
                    BaoCaoChuTrinh(tiep);
                }
                else if (mauSac[tiep] == 0)
                {
                    Duyet(tiep);
                }

                duongDi.RemoveAt(duongDi.Count - 1);
            }

            mauSac[buocId] = 2;
        }

        void BaoCaoChuTrinh(Guid dinhQuayLai)
        {
            var viTri = duongDi.FindIndex(d => d.BuocId == dinhQuayLai);
            if (viTri < 0)
            {
                return;
            }

            var canhTrongChuTrinh = duongDi.Skip(viTri).ToList();
            var tenBuoc = canhTrongChuTrinh.Select(c => bang[c.BuocId].Ten).ToList();
            var khoa = string.Join("->", canhTrongChuTrinh.Select(c => c.BuocId));

            if (!daBaoCao.Add(khoa))
            {
                return;
            }

            var moTaVong = string.Join(" → ", tenBuoc) + " → " + bang[dinhQuayLai].Ten;
            var khongCoDieuKien = canhTrongChuTrinh.All(c => c.TruongHop.DieuKien is null);

            if (khongCoDieuKien)
            {
                ketQua.ThemLoi(MaPhatHienQuyTrinh.VongLapVoHan,
                    $"Phát hiện vòng lặp không có điều kiện thoát: {moTaVong}. "
                    + "Cần bổ sung điều kiện cho ít nhất một nhánh trong vòng lặp.",
                    bang[dinhQuayLai]);
            }
            else
            {
                ketQua.ThemCanhBao(MaPhatHienQuyTrinh.VongLapVoHan,
                    $"Quy trình có vòng lặp: {moTaVong}. Vòng lặp có điều kiện thoát — hãy kiểm tra lại nghiệp vụ.",
                    bang[dinhQuayLai]);
            }
        }

        foreach (var buoc in buocs.Where(b => mauSac[b.Id] == 0))
        {
            Duyet(buoc.Id);
        }
    }

    /// <summary>Rule 5: moi buoc phai co it nhat 1 tac nhan xu ly.</summary>
    private static void KiemTraTacNhan(List<QuyTrinhBuoc> buocs, KetQuaKiemTraQuyTrinh ketQua)
    {
        foreach (var buoc in buocs)
        {
            // Buoc ket thuc thuan tuy (khong co hanh dong) khong bat buoc co tac nhan.
            if (buoc.LaBuocKetThuc && buoc.LoaiBuoc == LoaiBuoc.KetThuc)
            {
                continue;
            }

            if (buoc.TacNhan.Count(t => !t.DaXoa) == 0)
            {
                ketQua.ThemLoi(MaPhatHienQuyTrinh.BuocKhongCoTacNhan,
                    $"Bước '{buoc.Ten}' chưa được cấu hình tác nhân xử lý.",
                    buoc);
            }
        }
    }

    /// <summary>Rule 6: buoc CHAM_DIEM phai gan hoi dong va bo tieu chi.</summary>
    private static void KiemTraBuocChamDiem(List<QuyTrinhBuoc> buocs, KetQuaKiemTraQuyTrinh ketQua)
    {
        foreach (var buoc in buocs.Where(b => b.LoaiBuoc == LoaiBuoc.ChamDiem))
        {
            var thieu = new List<string>();
            if (buoc.HoiDongId is null)
            {
                thieu.Add("hội đồng");
            }

            if (buoc.BoTieuChiId is null)
            {
                thieu.Add("bộ tiêu chí");
            }

            if (thieu.Count > 0)
            {
                ketQua.ThemLoi(MaPhatHienQuyTrinh.BuocChamDiemThieuCauHinh,
                    $"Bước chấm điểm '{buoc.Ten}' chưa gắn {string.Join(" và ", thieu)}.",
                    buoc);
            }
        }
    }

    /// <summary>
    /// Rule 7: dieu kien chuyen tiep khong mau thuan / khong phu het truong hop.
    /// - Sai cu phap -> loi.
    /// - Khong co nhanh mac dinh (dieu kien null hoac LaMacDinh) -> canh bao "khong phu het".
    /// - Hai nhanh trung y het dieu kien -> canh bao mau thuan (nhanh sau khong bao gio chay).
    /// </summary>
    private void KiemTraDieuKienChuyenTiep(List<QuyTrinhBuoc> buocs, KetQuaKiemTraQuyTrinh ketQua)
    {
        foreach (var buoc in buocs)
        {
            var truongHop = buoc.TruongHop.Where(t => !t.DaXoa).OrderBy(t => t.ThuTu).ToList();
            if (truongHop.Count == 0)
            {
                continue;
            }

            foreach (var th in truongHop)
            {
                var loiCuPhap = _boDanhGia.KiemTraCuPhap(th.DieuKien);
                foreach (var loi in loiCuPhap)
                {
                    ketQua.ThemLoi(MaPhatHienQuyTrinh.DieuKienSaiCuPhap,
                        $"Trường hợp '{th.Ten}' của bước '{buoc.Ten}': {loi}",
                        buoc);
                }
            }

            var coNhanhMacDinh = truongHop.Any(t => t.LaMacDinh || t.DieuKien is null);
            if (!coNhanhMacDinh && truongHop.Count > 0)
            {
                ketQua.ThemCanhBao(MaPhatHienQuyTrinh.DieuKienKhongPhuHet,
                    $"Bước '{buoc.Ten}': tất cả {truongHop.Count} trường hợp đều có điều kiện, "
                    + "hồ sơ có thể không khớp trường hợp nào. Nên đặt 1 trường hợp làm mặc định.",
                    buoc);
            }

            // Hai nhanh cung dieu kien (so sanh theo dang chuan hoa) -> nhanh sau khong bao gio chay.
            var daGap = new Dictionary<string, QuyTrinhTruongHop>(StringComparer.Ordinal);
            foreach (var th in truongHop.Where(t => t.DieuKien is not null))
            {
                var khoa = ChuanHoaDieuKien(th.DieuKien!);
                if (daGap.TryGetValue(khoa, out var truoc))
                {
                    ketQua.ThemCanhBao(MaPhatHienQuyTrinh.DieuKienMauThuan,
                        $"Bước '{buoc.Ten}': trường hợp '{th.Ten}' có điều kiện trùng với '{truoc.Ten}' "
                        + "nên sẽ không bao giờ được chọn.",
                        buoc);
                }
                else
                {
                    daGap[khoa] = th;
                }
            }

            // Nhieu nhanh mac dinh -> chi nhanh dau tien chay.
            var soMacDinh = truongHop.Count(t => t.LaMacDinh);
            if (soMacDinh > 1)
            {
                ketQua.ThemCanhBao(MaPhatHienQuyTrinh.DieuKienMauThuan,
                    $"Bước '{buoc.Ten}' có {soMacDinh} trường hợp mặc định, chỉ trường hợp đầu tiên được áp dụng.",
                    buoc);
            }
        }
    }

    /// <summary>Kiem tra tu buoc bat dau co the di den it nhat 1 buoc ket thuc hay khong.</summary>
    private static void KiemTraDenDuocKetThuc(
        List<QuyTrinhBuoc> buocs, QuyTrinhBuoc buocBatDau, KetQuaKiemTraQuyTrinh ketQua)
    {
        var bang = buocs.ToDictionary(b => b.Id);
        var daTham = new HashSet<Guid>();
        var hangDoi = new Queue<Guid>();
        hangDoi.Enqueue(buocBatDau.Id);
        daTham.Add(buocBatDau.Id);

        var denDuoc = false;
        while (hangDoi.Count > 0)
        {
            var hienTai = hangDoi.Dequeue();
            if (bang[hienTai].LaBuocKetThuc)
            {
                denDuoc = true;
                break;
            }

            foreach (var th in bang[hienTai].TruongHop.Where(t => !t.DaXoa))
            {
                // Truong hop khong co buoc tiep theo = ket thuc quy trinh.
                if (th.BuocTiepTheoId is null)
                {
                    denDuoc = true;
                    break;
                }

                if (bang.ContainsKey(th.BuocTiepTheoId.Value) && daTham.Add(th.BuocTiepTheoId.Value))
                {
                    hangDoi.Enqueue(th.BuocTiepTheoId.Value);
                }
            }

            if (denDuoc)
            {
                break;
            }
        }

        if (!denDuoc)
        {
            ketQua.ThemLoi(MaPhatHienQuyTrinh.KhongDenDuocBuocKetThuc,
                $"Từ bước bắt đầu '{buocBatDau.Ten}' không thể đi tới bước kết thúc nào.",
                buocBatDau);
        }
    }

    /// <summary>Chuan hoa bieu thuc thanh chuoi de so sanh trung lap giua cac nhanh.</summary>
    private static string ChuanHoaDieuKien(BieuThucDieuKien bieuThuc)
    {
        if (bieuThuc.LaNhom)
        {
            var con = (bieuThuc.CacDieuKien ?? new List<BieuThucDieuKien>())
                .Select(ChuanHoaDieuKien)
                .OrderBy(s => s, StringComparer.Ordinal);
            return $"{bieuThuc.Phep?.ToUpperInvariant()}({string.Join("&", con)})";
        }

        var giaTri = bieuThuc.GiaTri?.ToString() ?? "null";
        return $"{bieuThuc.Truong?.ToLowerInvariant()}{bieuThuc.ToanTu}{giaTri}";
    }
}
