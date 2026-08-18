using BlueIdea.Api.Chung;
using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Shared.KetQua;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Api.Controllers;

public sealed class LuuMucMenuDto
{
    public string Ma { get; set; } = string.Empty;

    public string Ten { get; set; } = string.Empty;

    public string? Icon { get; set; }

    public string? DuongDan { get; set; }

    public Guid? MenuChaId { get; set; }

    public int ThuTu { get; set; }

    public string? QuyenMa { get; set; }

    /// <summary>WEB | MOBILE</summary>
    public string Loai { get; set; } = "WEB";

    public bool HienThi { get; set; } = true;

    public bool MoTabMoi { get; set; }
}

/// <summary>Một nút trong cây menu sau khi kéo thả — thứ tự là vị trí trong mảng.</summary>
public sealed record NutSapXepMenu(Guid Id, List<NutSapXepMenu>? Con);

public sealed record MucMenuQuanTriDto(
    Guid Id,
    string Ma,
    string Ten,
    string? Icon,
    string? DuongDan,
    Guid? MenuChaId,
    int ThuTu,
    string? QuyenMa,
    string Loai,
    bool HienThi,
    bool MoTabMoi);

/// <summary>
/// Chức năng 48 — Quản trị cấu hình menu (cây, kéo thả, tách riêng Web và Mobile).
///
/// Menu Web và Mobile là hai cây độc lập: điện thoại chỉ hiển thị được vài mục chính, ép dùng
/// chung một cây thì hoặc menu web nghèo đi, hoặc menu mobile dài không dùng nổi.
/// </summary>
[ApiController]
[Route("api/v1/cau-hinh-menu")]
[Authorize(Policy = MaQuyen.CauHinhSua)]
[Produces("application/json")]
public sealed class CauHinhMenuController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly IDichVuNhatKy _nhatKy;

    public CauHinhMenuController(IAppDbContext db, IDichVuNhatKy nhatKy)
    {
        _db = db;
        _nhatKy = nhatKy;
    }

    /// <summary>Toàn bộ mục menu của một loại (WEB hoặc MOBILE), kể cả mục đang ẩn.</summary>
    [HttpGet]
    public async Task<IActionResult> LayDanhSachAsync(
        [FromQuery] string loai = "WEB", CancellationToken ct = default)
    {
        var danhSach = await _db.CauHinhMenu.AsNoTracking()
            .Where(x => x.Loai == loai)
            .OrderBy(x => x.ThuTu)
            .Select(x => new MucMenuQuanTriDto(
                x.Id, x.Ma, x.Ten, x.Icon, x.DuongDan, x.MenuChaId, x.ThuTu, x.QuyenMa,
                x.Loai, x.HienThi, x.MoTabMoi))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return Ok(PhanHoiApi<IReadOnlyList<MucMenuQuanTriDto>>.Ok(danhSach));
    }

    [HttpPost]
    public async Task<IActionResult> ThemAsync([FromBody] LuuMucMenuDto duLieu, CancellationToken ct)
    {
        await BatBuocKhongTrungMaAsync(duLieu.Ma, duLieu.Loai, null, ct).ConfigureAwait(false);

        var muc = new CauHinhMenu { Id = Guid.NewGuid() };
        ApDung(muc, duLieu);

        _db.CauHinhMenu.Add(muc);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await _nhatKy.GhiAsync("THEM", "CAU_HINH", "cau_hinh_menu", muc.Id,
            duLieuSau: muc, ct: ct).ConfigureAwait(false);

        return Ok(PhanHoiApi<Guid>.Ok(muc.Id, "Đã thêm mục menu"));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> SuaAsync(
        Guid id, [FromBody] LuuMucMenuDto duLieu, CancellationToken ct)
    {
        var muc = await _db.CauHinhMenu.FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("mục menu", id);

        await BatBuocKhongTrungMaAsync(duLieu.Ma, duLieu.Loai, id, ct).ConfigureAwait(false);

        if (duLieu.MenuChaId == id)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Không đặt một mục menu làm cấp trên của chính nó.");
        }

        ApDung(muc, duLieu);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await _nhatKy.GhiAsync("SUA", "CAU_HINH", "cau_hinh_menu", id,
            duLieuSau: muc, ct: ct).ConfigureAwait(false);

        return Ok(PhanHoiApi.Ok("Đã cập nhật mục menu"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> XoaAsync(Guid id, CancellationToken ct)
    {
        var muc = await _db.CauHinhMenu.FirstOrDefaultAsync(x => x.Id == id, ct)
            .ConfigureAwait(false) ?? throw new KhongTimThayException("mục menu", id);

        var soCon = await _db.CauHinhMenu.CountAsync(x => x.MenuChaId == id, ct)
            .ConfigureAwait(false);

        if (soCon > 0)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                $"Mục \"{muc.Ten}\" còn {soCon} mục con — chuyển hoặc xoá mục con trước.");
        }

        muc.DaXoa = true;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await _nhatKy.GhiAsync("XOA", "CAU_HINH", "cau_hinh_menu", id,
            duLieuTruoc: muc, ct: ct).ConfigureAwait(false);

        return Ok(PhanHoiApi.Ok("Đã xoá mục menu"));
    }

    /// <summary>
    /// Lưu lại toàn bộ cây sau khi kéo thả: thứ tự là vị trí trong mảng, cấp cha là vị trí lồng.
    ///
    /// Nhận nguyên cây thay vì từng thao tác kéo lẻ: kéo một mục thường làm đổi thứ tự nhiều
    /// mục khác, gửi từng cái một sẽ để cây ở trạng thái nửa vời nếu có lệnh nào hỏng giữa chừng.
    /// </summary>
    [HttpPut("sap-xep")]
    public async Task<IActionResult> SapXepAsync(
        [FromQuery] string loai, [FromBody] List<NutSapXepMenu> cay, CancellationToken ct)
    {
        var tatCa = await _db.CauHinhMenu.Where(x => x.Loai == loai).ToListAsync(ct)
            .ConfigureAwait(false);

        var theoId = tatCa.ToDictionary(x => x.Id);
        var daGap = new HashSet<Guid>();

        void Duyet(List<NutSapXepMenu> cacNut, Guid? chaId)
        {
            for (var i = 0; i < cacNut.Count; i++)
            {
                if (!theoId.TryGetValue(cacNut[i].Id, out var muc)) continue;

                // Mot Id xuat hien hai lan trong cay gui len la du lieu hong — bo qua lan sau
                // thay vi ghi de, de khong tao vong lap cha-con lam menu bien mat.
                if (!daGap.Add(muc.Id)) continue;

                muc.MenuChaId = chaId;
                muc.ThuTu = i;

                if (cacNut[i].Con is { Count: > 0 } con)
                {
                    Duyet(con, muc.Id);
                }
            }
        }

        Duyet(cay, null);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await _nhatKy.GhiAsync("SUA", "CAU_HINH", "cau_hinh_menu",
            duLieuSau: new { loai, soMuc = daGap.Count }, ct: ct).ConfigureAwait(false);

        return Ok(PhanHoiApi.Ok($"Đã lưu thứ tự {daGap.Count} mục menu"));
    }

    private async Task BatBuocKhongTrungMaAsync(
        string ma, string loai, Guid? boQuaId, CancellationToken ct)
    {
        var trung = await _db.CauHinhMenu.AsNoTracking()
            .AnyAsync(x => x.Ma == ma && x.Loai == loai && (boQuaId == null || x.Id != boQuaId), ct)
            .ConfigureAwait(false);

        if (trung)
        {
            throw new NghiepVuException(MaLoiHeThong.TrungMa,
                $"Mã menu \"{ma}\" đã tồn tại trong menu {loai}.");
        }
    }

    private static void ApDung(CauHinhMenu x, LuuMucMenuDto d)
    {
        x.Ma = d.Ma.Trim();
        x.Ten = d.Ten.Trim();
        x.Icon = d.Icon;
        x.DuongDan = string.IsNullOrWhiteSpace(d.DuongDan) ? null : d.DuongDan.Trim();
        x.MenuChaId = d.MenuChaId;
        x.ThuTu = d.ThuTu;
        x.QuyenMa = string.IsNullOrWhiteSpace(d.QuyenMa) ? null : d.QuyenMa.Trim();
        x.Loai = d.Loai;
        x.HienThi = d.HienThi;
        x.MoTabMoi = d.MoTabMoi;
    }
}
