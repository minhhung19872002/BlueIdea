using BlueIdea.Workflow.DieuKien;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlueIdea.Infrastructure.Seed;

/// <summary>
/// Cac buoc don du lieu cho he thong DA CAI DAT.
///
/// Seed chi tao du lieu mau khi bang con rong, nen mot loi trong du lieu mau da nap khong bao gio
/// tu bien mat: sua code seed chi giup cai dat MOI. He thong dang chay van giu nguyen loi cu, va
/// day thuong la he thong that cua don vi.
///
/// Moi buoc o day phai co dieu kien CHAT de khong dung vao cau hinh quan tri vien tu khai.
/// </summary>
public sealed partial class DuLieuMau
{
    /// <summary>
    /// Bo dieu kien TU CHAN tren cac nhanh do nguoi xu ly chu dong chon.
    ///
    /// Quy trinh mau tung khai `hanh_dong_nguoi_dung = "BO_SUNG"` / `"TU_CHOI"` cho hai nhanh
    /// "Yeu cau bo sung" va "Tu choi tiep nhan". Bien do khong co duong nao dat gia tri, nen dieu
    /// kien vinh vien sai va hai nhanh bi chan mai mai — can bo tiep nhan KHONG the yeu cau bo
    /// sung hay tu choi mot ho so nao, du nut van hien tren giao dien.
    ///
    /// Chi bo khi dieu kien la DUNG MOT phep so sanh tren bien do va khong phai mot nhom AND/OR.
    /// Quan tri vien co the da co y dung bien nay trong mot bieu thuc phuc tap cho quy trinh rieng
    /// — truong hop do khong dung toi, vi ho co the dat gia tri qua truong hanhDongNguoiDung khi
    /// thuc thi.
    /// </summary>
    private async Task BoDieuKienTuChanAsync(CancellationToken ct)
    {
        var cacTruongHop = await _db.QuyTrinhTruongHop
            .Where(x => !x.DaXoa && x.DieuKien != null)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var soSua = 0;

        foreach (var th in cacTruongHop)
        {
            var dk = th.DieuKien;

            if (dk is null) continue;

            var laSoSanhDon = dk.Phep is null
                              && (dk.CacDieuKien is null || dk.CacDieuKien.Count == 0)
                              && string.Equals(
                                  dk.Truong, BienNguCanh.HanhDongNguoiDung, StringComparison.Ordinal);

            if (!laSoSanhDon) continue;

            th.DieuKien = null;
            soSua++;

            _logger.LogInformation(
                "Da bo dieu kien tu chan tren nhanh '{Ma}' ({Ten}).", th.Ma, th.Ten);
        }

        if (soSua > 0)
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("Da mo lai {SoNhanh} nhanh xu ly bi chan.", soSua);
        }

        await BoDieuKienTuChanTrongSnapshotAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Bo dieu kien tu chan trong SNAPSHOT quy trinh cua cac ho so DANG XU LY.
    ///
    /// Quy trinh duoc dong bang vao ho so luc nop (<c>quy_trinh_snapshot</c>) de ho so khong bi doi
    /// luat giua duong — dung ve nguyen tac, nhung nghia la sua bang quy trinh KHONG cham toi cac
    /// ho so dang chay. Tren may that co 40 ho so dang xu ly, tat ca deu mang ban dong bang cu nen
    /// van khong the yeu cau bo sung hay tu choi tiep nhan.
    ///
    /// Day khong phai doi luat nghiep vu giua duong: no bo mot dieu kien KHONG BAO GIO thoa duoc,
    /// tuc chi mo lai dung nhanh ma le ra luon phai mo. Khong nhanh nao bi dong them, khong ho so
    /// nao doi trang thai.
    ///
    /// Lam bang thay the chuoi tren JSON thay vi doc/ghi lai ca doi tuong: doc snapshot roi ghi lai
    /// se chuan hoa toan bo JSON theo phien ban hien tai cua mo hinh, tuc lang le xoa mat nhung
    /// truong ma ban cu co ma ban moi khong con — dung cai ma dong bang sinh ra de giu.
    /// </summary>
    private async Task BoDieuKienTuChanTrongSnapshotAsync(CancellationToken ct)
    {
        const string dauHieu = "\"hanh_dong_nguoi_dung\"";

        var hoSo = await _db.SangKien
            .Where(x => x.QuyTrinhSnapshot != null && x.QuyTrinhSnapshot.Contains(dauHieu))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (hoSo.Count == 0) return;

        foreach (var hs in hoSo)
        {
            hs.QuyTrinhSnapshot = XoaDieuKienHanhDongNguoiDung(hs.QuyTrinhSnapshot!);
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Da mo lai nhanh bi chan trong snapshot cua {SoHoSo} ho so dang xu ly.", hoSo.Count);
    }

    /// <summary>
    /// Doi dieu kien GOC cua mot truong hop tu phep so sanh tren <c>hanh_dong_nguoi_dung</c>
    /// thanh <c>null</c>.
    ///
    /// Chi thay khi khoi JSON dung nam ngay sau <c>"dieuKien":</c> — do la dau hieu chac chan rang
    /// no la dieu kien goc cua mot truong hop. Mot phep so sanh tren cung bien nhung nam BEN TRONG
    /// mot nhom AND/OR (trong <c>cacDieuKien</c>) thi giu nguyen: do la bieu thuc quan tri vien tu
    /// khai cho quy trinh rieng, va thay no bang null se lam vo ca bieu thuc.
    ///
    /// Lam bang thay the chuoi thay vi doc/ghi lai ca doi tuong: doc snapshot roi ghi lai se chuan
    /// hoa toan bo JSON theo phien ban hien tai cua mo hinh, tuc lang le xoa mat nhung truong ma
    /// ban dong bang cu co ma ban moi khong con — dung cai ma viec dong bang sinh ra de giu.
    /// </summary>
    public static string XoaDieuKienHanhDongNguoiDung(string snapshot)
    {
        const string moDau = "\"dieuKien\":{\"truong\":\"hanh_dong_nguoi_dung\"";

        var ketQua = new System.Text.StringBuilder(snapshot.Length);
        var i = 0;

        while (true)
        {
            var viTri = snapshot.IndexOf(moDau, i, StringComparison.Ordinal);

            if (viTri < 0)
            {
                ketQua.Append(snapshot, i, snapshot.Length - i);
                return ketQua.ToString();
            }

            // Bo qua phan dau khoi de nhay tu dau ngoac mo cua doi tuong dieu kien.
            var batDauKhoi = viTri + "\"dieuKien\":".Length;
            var sau = 0;
            var j = batDauKhoi;

            while (j < snapshot.Length)
            {
                if (snapshot[j] == '{') sau++;
                else if (snapshot[j] == '}') sau--;

                j++;

                if (sau == 0) break;
            }

            ketQua.Append(snapshot, i, batDauKhoi - i).Append("null");
            i = j;
        }
    }
}
