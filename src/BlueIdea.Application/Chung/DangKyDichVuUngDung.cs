using System.Reflection;
using BlueIdea.Ai.Nhung;
using BlueIdea.Ai.TrungLap;
using BlueIdea.Application.BaoCao;
using BlueIdea.Application.DanhGia;
using BlueIdea.Application.DanhMuc;
using BlueIdea.Application.HoiDong;
using BlueIdea.Application.CauHinhQuyTrinh;
using BlueIdea.Application.SangKien;
using BlueIdea.Application.TieuChi;
using BlueIdea.Application.TrungLap;
using BlueIdea.Application.XuLy;
using BlueIdea.Scoring;
using BlueIdea.Workflow;
using BlueIdea.Workflow.DieuKien;
using BlueIdea.Workflow.KiemTra;
using BlueIdea.Workflow.ThoiHan;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace BlueIdea.Application.Chung;

/// <summary>Dang ky toan bo dich vu cua tang Application vao DI container.</summary>
public static class DangKyDichVuUngDung
{
    public static IServiceCollection ThemTangUngDung(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            // Thu tu quan trong: log -> kiem tra du lieu -> phan quyen -> ghi nhat ky -> handler.
            cfg.AddOpenBehavior(typeof(HanhViGhiLog<,>));
            cfg.AddOpenBehavior(typeof(HanhViKiemTraDuLieu<,>));
            cfg.AddOpenBehavior(typeof(HanhViPhanQuyen<,>));
            cfg.AddOpenBehavior(typeof(HanhViGhiNhatKy<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // --- Engine thuan logic (khong trang thai, dung singleton) ---
        services.AddSingleton<IBoDanhGiaDieuKien, BoDanhGiaDieuKien>();
        services.AddSingleton<IKiemTraQuyTrinh, KiemTraQuyTrinh>();
        services.AddSingleton<IBoTinhDiem, BoTinhDiem>();
        services.AddSingleton<IBoChuyenDoiSnapshotQuyTrinh, BoChuyenDoiSnapshotQuyTrinh>();
        services.AddSingleton<IBoNhungVanBan>(_ => new BoNhungBamTuVung());
        services.AddSingleton<IBoPhanTichTrungLap, BoPhanTichTrungLap>();

        // Tinh han xu ly can doc bang ngay nghi le -> dang ky scoped, nguon do Infrastructure cung cap.
        services.AddScoped<ITinhHanXuLy, TinhHanXuLy>();
        services.AddScoped<IBoMayQuyTrinh, BoMayQuyTrinh>();
        services.AddScoped<IWorkflowEngine, DichVuWorkflow>();

        // --- Dich vu nghiep vu ---
        services.AddScoped<DichVuLinhVuc>();
        services.AddScoped<DichVuDoiTuong>();
        services.AddScoped<DichVuLoaiTacGia>();
        services.AddScoped<DichVuDonVi>();
        services.AddScoped<DichVuDotDeNghi>();
        services.AddScoped<DichVuBieuMauXuat>();
        services.AddScoped<DichVuBieuMauThongKe>();
        services.AddScoped<DichVuQuyTrinh>();
        services.AddScoped<DichVuTieuChi>();
        services.AddScoped<DichVuDanhGia>();
        services.AddScoped<DichVuTruyVanSangKien>();
        services.AddScoped<DichVuKiemTraTrungLap>();
        services.AddScoped<DichVuBaoCao>();
        services.AddScoped<DichVuHoiDong>();
        services.AddScoped<HoiDong.DichVuBienBanHop>();
        services.AddScoped<BaoCao.DichVuSinhBieuMau>();
        services.AddScoped<DanhMuc.DichVuNhapDanhMuc>();
        services.AddScoped<BanHanh.DichVuQuyetDinh>();
        services.AddScoped<CongKhai.DichVuTraCuuCongKhai>();
        services.AddScoped<XacThuc.DichVuMfa>();
        services.AddScoped<XacThuc.DichVuCaptcha>();
        services.AddScoped<XacThuc.DichVuQuenMatKhau>();
        services.AddScoped<QuanTri.DichVuBoLocYeuThich>();
        services.AddScoped<QuanTri.DichVuQuanTriNguoiDung>();
        services.AddScoped<QuanTri.DichVuCauHinhGuiTin>();
        services.AddScoped<QuanTri.DichVuNhapNguoiDung>();
        services.AddScoped<BaoCao.DichVuBaoCaoTuyBien>();
        services.AddScoped<DanhGia.DichVuXuatPhieuCham>();
        services.AddScoped<TichHop.DichVuDongBoLienThong>();
        services.AddScoped<TichHop.DichVuKhoaApiNgoai>();
        services.AddScoped<TraCuu.DichVuTimNguNghia>();
        services.AddScoped<XacThuc.DichVuDangNhapSso>();
        services.AddScoped<KySo.DichVuKySo>();

        return services;
    }
}
