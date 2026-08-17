using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Shared.TiengViet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BlueIdea.Infrastructure.Persistence;

/// <summary>
/// Tu dong dien truong audit (nguoi tao / ngay tao / nguoi sua / ngay sua) va
/// chuyen lenh xoa cung thanh xoa mem. Nguyen tac 4 cua dac ta.
/// </summary>
public sealed class BoChanAudit : SaveChangesInterceptor
{
    private readonly INguoiDungHienTai _nguoiDung;
    private readonly IDongHoHeThong _dongHo;

    public BoChanAudit(INguoiDungHienTai nguoiDung, IDongHoHeThong dongHo)
    {
        _nguoiDung = nguoiDung;
        _dongHo = dongHo;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        CapNhatAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        CapNhatAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void CapNhatAudit(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var bayGio = _dongHo.BayGio;
        var nguoiDungId = _nguoiDung.Id;

        foreach (var muc in context.ChangeTracker.Entries<ThucThe>())
        {
            switch (muc.State)
            {
                case EntityState.Added:
                    muc.Entity.NgayTao = muc.Entity.NgayTao == default ? bayGio : muc.Entity.NgayTao;
                    muc.Entity.NguoiTaoId ??= nguoiDungId;
                    CapNhatTenKhongDau(muc.Entity);
                    break;

                case EntityState.Modified:
                    muc.Entity.NgaySua = bayGio;
                    muc.Entity.NguoiSuaId = nguoiDungId;
                    CapNhatTenKhongDau(muc.Entity);

                    // Danh dau thoi diem / nguoi xoa khi chuyen sang da_xoa = true.
                    if (muc.Entity.DaXoa && muc.Entity.NgayXoa is null)
                    {
                        muc.Entity.NgayXoa = bayGio;
                        muc.Entity.NguoiXoaId = nguoiDungId;
                    }

                    break;

                case EntityState.Deleted:
                    // Khong xoa cung: chuyen thanh xoa mem.
                    muc.State = EntityState.Modified;
                    muc.Entity.DaXoa = true;
                    muc.Entity.NgayXoa = bayGio;
                    muc.Entity.NguoiXoaId = nguoiDungId;
                    break;

                case EntityState.Detached:
                case EntityState.Unchanged:
                default:
                    break;
            }
        }
    }

    /// <summary>Dong bo cot *_khong_dau moi khi ten thay doi de tim kiem luon dung.</summary>
    private static void CapNhatTenKhongDau(ThucThe thucThe)
    {
        if (thucThe is ThucTheDanhMuc danhMuc)
        {
            danhMuc.TenKhongDau = VanBanTiengViet.TaoKhongDau(danhMuc.Ten);
            danhMuc.Ten = VanBanTiengViet.ChuanHoaNfc(danhMuc.Ten);
        }

        switch (thucThe)
        {
            case Domain.SangKien.HoSoSangKien hoSo:
                hoSo.TenSangKien = VanBanTiengViet.ChuanHoaNfc(hoSo.TenSangKien);
                hoSo.TenKhongDau = VanBanTiengViet.TaoKhongDau(hoSo.TenSangKien);
                break;

            case Domain.QuanTri.NguoiDung nguoiDung:
                nguoiDung.HoTen = VanBanTiengViet.ChuanHoaNfc(nguoiDung.HoTen);
                nguoiDung.HoTenKhongDau = VanBanTiengViet.TaoKhongDau(nguoiDung.HoTen);
                break;
        }
    }
}
