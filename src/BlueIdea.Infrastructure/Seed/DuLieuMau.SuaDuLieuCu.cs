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
    }
}
