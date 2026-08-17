using System.Text.Json;
using BlueIdea.Application.Chung;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Shared.KetQua;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.QuanTri;

/// <summary>Mot bo loc da luu.</summary>
public sealed record BoLocYeuThichDto(
    Guid Id, string ManHinh, string Ten, string ThamSo, bool MacDinh, DateTimeOffset NgayTao);

/// <summary>Du lieu luu bo loc moi hoac cap nhat bo loc san co.</summary>
public sealed record LuuBoLocDto(string ManHinh, string Ten, string ThamSo, bool MacDinh);

/// <summary>
/// Chuc nang 28 — Luu bo loc yeu thich tren cac man hinh danh sach.
///
/// Bo loc gan chat voi TUNG NGUOI DUNG: moi truy van deu chan theo
/// <see cref="INguoiDungHienTai.Id"/> va khong endpoint nao nhan <c>nguoiDungId</c> tu client,
/// nen khong co duong nao doc hay sua bo loc cua nguoi khac.
/// </summary>
public sealed class DichVuBoLocYeuThich
{
    /// <summary>Chan so bo loc moi nguoi de mot vong lap phia client khong lam phinh bang.</summary>
    private const int SoBoLocToiDaMoiManHinh = 30;

    private readonly IAppDbContext _db;
    private readonly INguoiDungHienTai _nguoiDung;

    public DichVuBoLocYeuThich(IAppDbContext db, INguoiDungHienTai nguoiDung)
    {
        _db = db;
        _nguoiDung = nguoiDung;
    }

    public async Task<IReadOnlyList<BoLocYeuThichDto>> DanhSachAsync(
        string manHinh, CancellationToken ct = default)
    {
        var nguoiDungId = LayNguoiDungId();

        return await _db.BoLocYeuThich.AsNoTracking()
            .Where(x => x.NguoiDungId == nguoiDungId && x.ManHinh == manHinh)
            .OrderByDescending(x => x.MacDinh)
            .ThenBy(x => x.Ten)
            .Select(x => new BoLocYeuThichDto(
                x.Id, x.ManHinh, x.Ten, x.ThamSo, x.MacDinh, x.NgayTao))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<Guid> LuuAsync(LuuBoLocDto duLieu, CancellationToken ct = default)
    {
        var nguoiDungId = LayNguoiDungId();

        KiemTraThamSo(duLieu.ThamSo);

        var ten = duLieu.Ten.Trim();

        if (string.IsNullOrWhiteSpace(ten))
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe, "Vui lòng nhập tên bộ lọc.");
        }

        // Trung ten thi GHI DE chu khong bao loi: nguoi dung bam "Luu bo loc" lan hai voi cung
        // mot ten y la muon cap nhat, bat ho doi ten hoac xoa truoc la them thao tac vo ich.
        var daCo = await _db.BoLocYeuThich
            .FirstOrDefaultAsync(
                x => x.NguoiDungId == nguoiDungId && x.ManHinh == duLieu.ManHinh && x.Ten == ten, ct)
            .ConfigureAwait(false);

        if (daCo is null)
        {
            var soHienCo = await _db.BoLocYeuThich
                .CountAsync(x => x.NguoiDungId == nguoiDungId && x.ManHinh == duLieu.ManHinh, ct)
                .ConfigureAwait(false);

            if (soHienCo >= SoBoLocToiDaMoiManHinh)
            {
                throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                    $"Mỗi màn hình chỉ lưu tối đa {SoBoLocToiDaMoiManHinh} bộ lọc. "
                    + "Vui lòng xoá bớt bộ lọc cũ.");
            }

            daCo = new BoLocYeuThich
            {
                NguoiDungId = nguoiDungId,
                ManHinh = duLieu.ManHinh,
                Ten = ten
            };

            _db.BoLocYeuThich.Add(daCo);
        }

        daCo.ThamSo = duLieu.ThamSo;
        daCo.MacDinh = duLieu.MacDinh;

        if (duLieu.MacDinh)
        {
            await BoMacDinhCacBoLocKhacAsync(nguoiDungId, duLieu.ManHinh, daCo.Id, ct)
                .ConfigureAwait(false);
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return daCo.Id;
    }

    public async Task DatMacDinhAsync(Guid id, CancellationToken ct = default)
    {
        var nguoiDungId = LayNguoiDungId();

        var boLoc = await LayCuaChinhMinhAsync(id, nguoiDungId, ct).ConfigureAwait(false);

        await BoMacDinhCacBoLocKhacAsync(nguoiDungId, boLoc.ManHinh, boLoc.Id, ct)
            .ConfigureAwait(false);

        boLoc.MacDinh = true;

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task XoaAsync(Guid id, CancellationToken ct = default)
    {
        var nguoiDungId = LayNguoiDungId();

        var boLoc = await LayCuaChinhMinhAsync(id, nguoiDungId, ct).ConfigureAwait(false);

        _db.BoLocYeuThich.Remove(boLoc);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // ------------------------------------------------------------------------------

    private Guid LayNguoiDungId()
        => _nguoiDung.Id
           ?? throw new NghiepVuException(MaLoiHeThong.ChuaXacThuc, "Chưa đăng nhập.");

    private async Task<BoLocYeuThich> LayCuaChinhMinhAsync(
        Guid id, Guid nguoiDungId, CancellationToken ct)
        => await _db.BoLocYeuThich
               .FirstOrDefaultAsync(x => x.Id == id && x.NguoiDungId == nguoiDungId, ct)
               .ConfigureAwait(false)
           // Loc luon theo NguoiDungId: neu chi tim theo Id roi kiem tra chu so huu sau, ke tan
           // cong van phan biet duoc "khong ton tai" voi "cua nguoi khac" qua thong bao loi.
           ?? throw new NghiepVuException(MaLoiHeThong.KhongTimThay, "Không tìm thấy bộ lọc.");

    private async Task BoMacDinhCacBoLocKhacAsync(
        Guid nguoiDungId, string manHinh, Guid ngoaiTru, CancellationToken ct)
    {
        var khac = await _db.BoLocYeuThich
            .Where(x => x.NguoiDungId == nguoiDungId
                        && x.ManHinh == manHinh
                        && x.MacDinh
                        && x.Id != ngoaiTru)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var x in khac)
        {
            x.MacDinh = false;
        }
    }

    /// <summary>
    /// Tham so phai la JSON hop le va la mot doi tuong.
    ///
    /// Cot khai bao kieu jsonb nen chuoi khong hop le se lam SaveChanges nem loi CSDL tho —
    /// bat o day de nguoi dung nhan duoc thong bao ro rang thay vi loi 500.
    /// </summary>
    private static void KiemTraThamSo(string thamSo)
    {
        try
        {
            using var tep = JsonDocument.Parse(thamSo);

            if (tep.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                    "Tham số bộ lọc phải là một đối tượng JSON.");
            }
        }
        catch (JsonException)
        {
            throw new NghiepVuException(MaLoiHeThong.DuLieuKhongHopLe,
                "Tham số bộ lọc không phải JSON hợp lệ.");
        }
    }
}
