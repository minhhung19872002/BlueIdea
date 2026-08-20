using System.Linq.Expressions;
using System.Text.Json;
using BlueIdea.Application.Chung;
using BlueIdea.Domain.Ai;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.DanhMuc;
using BlueIdea.Domain.HoiDong;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Domain.QuyTrinh;
using BlueIdea.Domain.SangKien;
using BlueIdea.Domain.TieuChi;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using EntityQuyTrinh = BlueIdea.Domain.QuyTrinh.QuyTrinh;

namespace BlueIdea.Infrastructure.Persistence;

/// <summary>
/// DbContext chinh cua he thong. Ap dung quy uoc Muc 4 dac ta:
/// - Ten bang / cot snake_case tieng Viet khong dau.
/// - Soft delete toan he thong bang global query filter.
/// - Truong jsonb cho du lieu bán cấu trúc.
/// - Toan bo thoi gian luu timestamptz (UTC).
/// </summary>
public class AppDbContext : DbContext, IAppDbContext
{
    /// <summary>So chieu vector embedding (pgvector) - khop voi IBoNhungVanBan.</summary>
    public const int SoChieuEmbedding = 768;

    private static readonly JsonSerializerOptions TuyChonJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // --- Danh muc ---
    public DbSet<LinhVuc> LinhVuc => Set<LinhVuc>();
    public DbSet<DoiTuong> DoiTuong => Set<DoiTuong>();
    public DbSet<LoaiTacGia> LoaiTacGia => Set<LoaiTacGia>();
    public DbSet<DonVi> DonVi => Set<DonVi>();
    public DbSet<DotDeNghi> DotDeNghi => Set<DotDeNghi>();
    public DbSet<CauHinhCapPheDuyet> CauHinhCapPheDuyet => Set<CauHinhCapPheDuyet>();
    public DbSet<BieuMauXuat> BieuMauXuat => Set<BieuMauXuat>();
    public DbSet<BieuMauThongKe> BieuMauThongKe => Set<BieuMauThongKe>();
    public DbSet<QuyetDinh> QuyetDinh => Set<QuyetDinh>();
    public DbSet<QuyetDinhSangKien> QuyetDinhSangKien => Set<QuyetDinhSangKien>();
    public DbSet<NgayNghiLe> NgayNghiLe => Set<NgayNghiLe>();

    // --- Quy trinh ---
    public DbSet<EntityQuyTrinh> QuyTrinh => Set<EntityQuyTrinh>();
    public DbSet<QuyTrinhBuoc> QuyTrinhBuoc => Set<QuyTrinhBuoc>();
    public DbSet<QuyTrinhBuocTacNhan> QuyTrinhBuocTacNhan => Set<QuyTrinhBuocTacNhan>();
    public DbSet<QuyTrinhTrangThai> QuyTrinhTrangThai => Set<QuyTrinhTrangThai>();
    public DbSet<QuyTrinhTruongHop> QuyTrinhTruongHop => Set<QuyTrinhTruongHop>();
    public DbSet<QuyTrinhThanhPhanHoSo> QuyTrinhThanhPhanHoSo => Set<QuyTrinhThanhPhanHoSo>();
    public DbSet<QuyTrinhChucNangBoSung> QuyTrinhChucNangBoSung => Set<QuyTrinhChucNangBoSung>();
    public DbSet<QuyTrinhLienThong> QuyTrinhLienThong => Set<QuyTrinhLienThong>();

    // --- Tieu chi ---
    public DbSet<BoTieuChi> BoTieuChi => Set<BoTieuChi>();
    public DbSet<NhomTieuChi> NhomTieuChi => Set<NhomTieuChi>();
    public DbSet<TieuChiChamDiem> TieuChi => Set<TieuChiChamDiem>();
    public DbSet<TieuChiMucDiem> TieuChiMucDiem => Set<TieuChiMucDiem>();
    public DbSet<MucCongNhan> MucCongNhan => Set<MucCongNhan>();

    // --- Hoi dong ---
    public DbSet<HoiDongSangKien> HoiDong => Set<HoiDongSangKien>();
    public DbSet<HoiDongThanhVien> HoiDongThanhVien => Set<HoiDongThanhVien>();
    public DbSet<PhienHopHoiDong> PhienHop => Set<PhienHopHoiDong>();
    public DbSet<PhienHopHoSo> PhienHopHoSo => Set<PhienHopHoSo>();
    public DbSet<PhienHopDiemDanh> PhienHopDiemDanh => Set<PhienHopDiemDanh>();
    public DbSet<BienBanHop> BienBanHop => Set<BienBanHop>();
    public DbSet<BienBanChuKy> BienBanChuKy => Set<BienBanChuKy>();
    public DbSet<PhieuBoPhieu> PhieuBoPhieu => Set<PhieuBoPhieu>();

    // --- Sang kien ---
    public DbSet<HoSoSangKien> SangKien => Set<HoSoSangKien>();
    public DbSet<SangKienTacGia> SangKienTacGia => Set<SangKienTacGia>();
    public DbSet<TepTin> TepTin => Set<TepTin>();
    public DbSet<SangKienTepDinhKem> SangKienTepDinhKem => Set<SangKienTepDinhKem>();
    public DbSet<SangKienLichSu> SangKienLichSu => Set<SangKienLichSu>();
    public DbSet<SangKienXuLy> SangKienXuLy => Set<SangKienXuLy>();
    public DbSet<SangKienPhanCong> SangKienPhanCong => Set<SangKienPhanCong>();
    public DbSet<PhieuDanhGia> PhieuDanhGia => Set<PhieuDanhGia>();
    public DbSet<PhieuDanhGiaChiTiet> PhieuDanhGiaChiTiet => Set<PhieuDanhGiaChiTiet>();
    public DbSet<KetQuaXetDuyet> KetQuaXetDuyet => Set<KetQuaXetDuyet>();

    // --- AI ---
    public DbSet<SangKienDoanVan> SangKienDoanVan => Set<SangKienDoanVan>();
    public DbSet<Domain.Ai.KiemTraTrungLap> KiemTraTrungLap => Set<Domain.Ai.KiemTraTrungLap>();
    public DbSet<KiemTraTrungLapChiTiet> KiemTraTrungLapChiTiet => Set<KiemTraTrungLapChiTiet>();

    // --- Quan tri ---
    public DbSet<NguoiDung> NguoiDung => Set<NguoiDung>();
    public DbSet<VaiTro> VaiTro => Set<VaiTro>();
    public DbSet<Quyen> Quyen => Set<Quyen>();
    public DbSet<VaiTroQuyen> VaiTroQuyen => Set<VaiTroQuyen>();
    public DbSet<NguoiDungVaiTro> NguoiDungVaiTro => Set<NguoiDungVaiTro>();
    public DbSet<PhamViDuLieu> PhamViDuLieu => Set<PhamViDuLieu>();
    public DbSet<RefreshToken> RefreshToken => Set<RefreshToken>();
    public DbSet<LichSuMatKhau> LichSuMatKhau => Set<LichSuMatKhau>();
    public DbSet<BoLocYeuThich> BoLocYeuThich => Set<BoLocYeuThich>();
    public DbSet<MaXacThucTam> MaXacThucTam => Set<MaXacThucTam>();
    public DbSet<KhoaApiNgoai> KhoaApiNgoai => Set<KhoaApiNgoai>();
    public DbSet<CauHinhHeThong> CauHinhHeThong => Set<CauHinhHeThong>();
    public DbSet<CauHinhMenu> CauHinhMenu => Set<CauHinhMenu>();
    public DbSet<CauHinhEmailSms> CauHinhEmailSms => Set<CauHinhEmailSms>();
    public DbSet<MauThongBao> MauThongBao => Set<MauThongBao>();
    public DbSet<ThongBao> ThongBao => Set<ThongBao>();
    public DbSet<HangDoiGuiTin> HangDoiGuiTin => Set<HangDoiGuiTin>();
    public DbSet<CauHinhChuKySo> CauHinhChuKySo => Set<CauHinhChuKySo>();
    public DbSet<NhatKyKySo> NhatKyKySo => Set<NhatKyKySo>();
    public DbSet<HeThongTichHop> HeThongTichHop => Set<HeThongTichHop>();
    public DbSet<NhatKyDongBo> NhatKyDongBo => Set<NhatKyDongBo>();
    public DbSet<NhatKyHeThong> NhatKyHeThong => Set<NhatKyHeThong>();
    public DbSet<NhatKyLoi> NhatKyLoi => Set<NhatKyLoi>();
    public DbSet<NhatKyDangNhap> NhatKyDangNhap => Set<NhatKyDangNhap>();

    Task<int> IAppDbContext.SaveChangesAsync(CancellationToken ct) => base.SaveChangesAsync(ct);

    void IAppDbContext.XoaTheoDoi() => ChangeTracker.Clear();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("uuid-ossp");
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.HasPostgresExtension("unaccent");
        modelBuilder.HasPostgresExtension("pgcrypto");

        DatTenBang(modelBuilder);
        CauHinhChung(modelBuilder);
        CauHinhJsonb(modelBuilder);
        CauHinhKhoaVaChiMuc(modelBuilder);
        CauHinhQuanHe(modelBuilder);
        ApDungLocXoaMem(modelBuilder);
    }

    /// <summary>Dat ten bang theo dung dac ta (Muc 4), khong phu thuoc ten class.</summary>
    private static void DatTenBang(ModelBuilder b)
    {
        b.Entity<LinhVuc>().ToTable("linh_vuc");
        b.Entity<DoiTuong>().ToTable("doi_tuong");
        b.Entity<LoaiTacGia>().ToTable("loai_tac_gia");
        b.Entity<DonVi>().ToTable("don_vi");
        b.Entity<DotDeNghi>().ToTable("dot_de_nghi");
        b.Entity<CauHinhCapPheDuyet>().ToTable("cau_hinh_cap_phe_duyet");
        b.Entity<BieuMauXuat>().ToTable("bieu_mau_xuat");
        b.Entity<BieuMauThongKe>().ToTable("bieu_mau_thong_ke");
        b.Entity<QuyetDinh>().ToTable("quyet_dinh");
        b.Entity<QuyetDinhSangKien>().ToTable("quyet_dinh_sang_kien");
        b.Entity<NgayNghiLe>().ToTable("ngay_nghi_le");

        b.Entity<EntityQuyTrinh>().ToTable("quy_trinh");
        b.Entity<QuyTrinhBuoc>().ToTable("quy_trinh_buoc");
        b.Entity<QuyTrinhBuocTacNhan>().ToTable("quy_trinh_buoc_tac_nhan");
        b.Entity<QuyTrinhTrangThai>().ToTable("quy_trinh_trang_thai");
        b.Entity<QuyTrinhTruongHop>().ToTable("quy_trinh_truong_hop");
        b.Entity<QuyTrinhThanhPhanHoSo>().ToTable("quy_trinh_thanh_phan_ho_so");
        b.Entity<QuyTrinhChucNangBoSung>().ToTable("quy_trinh_chuc_nang_bo_sung");
        b.Entity<QuyTrinhLienThong>().ToTable("quy_trinh_lien_thong");

        b.Entity<BoTieuChi>().ToTable("bo_tieu_chi");
        b.Entity<NhomTieuChi>().ToTable("nhom_tieu_chi");
        b.Entity<TieuChiChamDiem>().ToTable("tieu_chi");
        b.Entity<TieuChiMucDiem>().ToTable("tieu_chi_muc_diem");
        b.Entity<MucCongNhan>().ToTable("muc_cong_nhan");

        b.Entity<HoiDongSangKien>().ToTable("hoi_dong");
        b.Entity<HoiDongThanhVien>().ToTable("hoi_dong_thanh_vien");
        b.Entity<PhienHopHoiDong>().ToTable("phien_hop_hoi_dong");
        b.Entity<PhienHopHoSo>().ToTable("phien_hop_ho_so");
        b.Entity<PhienHopDiemDanh>().ToTable("phien_hop_diem_danh");
        b.Entity<BienBanHop>().ToTable("bien_ban_hop");
        b.Entity<BienBanChuKy>().ToTable("bien_ban_chu_ky");
        b.Entity<PhieuBoPhieu>().ToTable("phieu_bo_phieu");

        b.Entity<HoSoSangKien>().ToTable("sang_kien");
        b.Entity<SangKienTacGia>().ToTable("sang_kien_tac_gia");
        b.Entity<TepTin>().ToTable("tep_tin");
        b.Entity<SangKienTepDinhKem>().ToTable("sang_kien_tep_dinh_kem");
        b.Entity<SangKienLichSu>().ToTable("sang_kien_lich_su");
        b.Entity<SangKienXuLy>().ToTable("sang_kien_xu_ly");
        b.Entity<SangKienPhanCong>().ToTable("sang_kien_phan_cong");
        b.Entity<PhieuDanhGia>().ToTable("phieu_danh_gia");
        b.Entity<PhieuDanhGiaChiTiet>().ToTable("phieu_danh_gia_chi_tiet");
        b.Entity<KetQuaXetDuyet>().ToTable("ket_qua_xet_duyet");

        b.Entity<SangKienDoanVan>().ToTable("sang_kien_doan_van");
        b.Entity<Domain.Ai.KiemTraTrungLap>().ToTable("kiem_tra_trung_lap");
        b.Entity<KiemTraTrungLapChiTiet>().ToTable("kiem_tra_trung_lap_chi_tiet");

        b.Entity<NguoiDung>().ToTable("nguoi_dung");
        b.Entity<VaiTro>().ToTable("vai_tro");
        b.Entity<Quyen>().ToTable("quyen");
        b.Entity<VaiTroQuyen>().ToTable("vai_tro_quyen");
        b.Entity<NguoiDungVaiTro>().ToTable("nguoi_dung_vai_tro");
        b.Entity<PhamViDuLieu>().ToTable("pham_vi_du_lieu");
        b.Entity<RefreshToken>().ToTable("refresh_token");
        b.Entity<LichSuMatKhau>().ToTable("lich_su_mat_khau");
        b.Entity<BoLocYeuThich>().ToTable("bo_loc_yeu_thich");
        b.Entity<MaXacThucTam>().ToTable("ma_xac_thuc_tam");
        b.Entity<KhoaApiNgoai>().ToTable("khoa_api_ngoai");
        b.Entity<CauHinhHeThong>().ToTable("cau_hinh_he_thong");
        b.Entity<CauHinhMenu>().ToTable("cau_hinh_menu");
        b.Entity<CauHinhEmailSms>().ToTable("cau_hinh_email_sms");
        b.Entity<MauThongBao>().ToTable("mau_thong_bao");
        b.Entity<ThongBao>().ToTable("thong_bao");
        b.Entity<HangDoiGuiTin>().ToTable("hang_doi_gui_tin");
        b.Entity<CauHinhChuKySo>().ToTable("cau_hinh_chu_ky_so");
        b.Entity<NhatKyKySo>().ToTable("nhat_ky_ky_so");
        b.Entity<HeThongTichHop>().ToTable("he_thong_tich_hop");
        b.Entity<NhatKyDongBo>().ToTable("nhat_ky_dong_bo");
        b.Entity<NhatKyHeThong>().ToTable("nhat_ky_he_thong");
        b.Entity<NhatKyLoi>().ToTable("nhat_ky_loi");
        b.Entity<NhatKyDangNhap>().ToTable("nhat_ky_dang_nhap");
    }

    private static void CauHinhChung(ModelBuilder b)
    {
        foreach (var kieu in b.Model.GetEntityTypes())
        {
            if (!typeof(ThucThe).IsAssignableFrom(kieu.ClrType))
            {
                continue;
            }

            var builder = b.Entity(kieu.ClrType);

            // Giu DEFAULT gen_random_uuid() o tang CSDL (dac ta Muc 4) nhung KHONG de EF coi
            // day la khoa do CSDL sinh: ung dung luon tu sinh Guid. Neu de ValueGenerated.OnAdd,
            // EF dung heuristic "da co khoa => Modified" khi phat hien thuc the con moi trong
            // navigation cua mot thuc the da luu, dan toi sinh UPDATE thay vi INSERT.
            builder.Property(nameof(ThucThe.Id))
                .HasDefaultValueSql("gen_random_uuid()")
                .ValueGeneratedNever();

            builder.Property(nameof(ThucThe.NgayTao)).HasDefaultValueSql("now()");
            builder.Property(nameof(ThucThe.DaXoa)).HasDefaultValue(false);
            builder.HasIndex(nameof(ThucThe.DaXoa));

            // Danh muc: ma duy nhat trong pham vi chua bi xoa mem.
            if (typeof(ThucTheDanhMuc).IsAssignableFrom(kieu.ClrType))
            {
                builder.Property(nameof(ThucTheDanhMuc.Ma)).HasMaxLength(50).IsRequired();
                builder.Property(nameof(ThucTheDanhMuc.Ten)).HasMaxLength(500).IsRequired();
                builder.Property(nameof(ThucTheDanhMuc.TenKhongDau)).HasMaxLength(500);
                builder.HasIndex(nameof(ThucTheDanhMuc.Ma)).IsUnique()
                    .HasFilter("da_xoa = false");
                builder.HasIndex(nameof(ThucTheDanhMuc.TenKhongDau));
            }
        }

        // Npgsql chi chap nhan timestamptz voi offset UTC. Dac ta yeu cau luu UTC va hien thi
        // theo Asia/Ho_Chi_Minh, nen chuan hoa moi DateTimeOffset ve UTC ngay tai tang anh xa.
        var veUtc = new ValueConverter<DateTimeOffset, DateTimeOffset>(
            v => v.ToUniversalTime(),
            v => v.ToUniversalTime());

        var veUtcNullable = new ValueConverter<DateTimeOffset?, DateTimeOffset?>(
            v => v.HasValue ? v.Value.ToUniversalTime() : v,
            v => v.HasValue ? v.Value.ToUniversalTime() : v);

        foreach (var thuocTinh in b.Model.GetEntityTypes().SelectMany(t => t.GetProperties()))
        {
            if (thuocTinh.ClrType == typeof(DateTimeOffset))
            {
                thuocTinh.SetValueConverter(veUtc);
            }
            else if (thuocTinh.ClrType == typeof(DateTimeOffset?))
            {
                thuocTinh.SetValueConverter(veUtcNullable);
            }
        }

        // Cot decimal: dat precision ro rang de tranh canh bao va sai so.
        foreach (var thuocTinh in b.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            var ten = thuocTinh.Name;
            if (ten.Contains("GiaTri", StringComparison.OrdinalIgnoreCase))
            {
                thuocTinh.SetPrecision(18);
                thuocTinh.SetScale(2);
            }
            else if (ten.Contains("TyLe", StringComparison.OrdinalIgnoreCase)
                     || ten.Contains("TrongSo", StringComparison.OrdinalIgnoreCase))
            {
                thuocTinh.SetPrecision(5);
                thuocTinh.SetScale(2);
            }
            else
            {
                thuocTinh.SetPrecision(10);
                thuocTinh.SetScale(2);
            }
        }
    }

    /// <summary>Cau hinh cac cot jsonb (du lieu ban cau truc - yeu cau Muc 7 dac ta).</summary>
    private static void CauHinhJsonb(ModelBuilder b)
    {
        DatJsonb<DotDeNghi, List<Guid>>(b, x => x.DonViApDungIds);
        DatJsonb<KhoaApiNgoai, List<string>>(b, x => x.DanhSachIp);
        DatJsonb<BieuMauXuat, List<CauHinhTruongBieuMau>>(b, x => x.CauHinhTruong);
        DatJsonb<BieuMauXuat, Dictionary<string, object>?>(b, x => x.PhamViApDung);
        DatJsonb<BieuMauThongKe, Dictionary<string, object>?>(b, x => x.CauHinhTieuChi);
        DatJsonb<BieuMauThongKe, List<CotBaoCao>>(b, x => x.CauHinhCot);
        DatJsonb<BieuMauThongKe, Dictionary<string, object>?>(b, x => x.CauHinhBoLoc);
        DatJsonb<BieuMauThongKe, List<string>>(b, x => x.DinhDangXuat);

        DatJsonb<EntityQuyTrinh, PhamViApDungQuyTrinh>(b, x => x.PhamViApDung);
        DatJsonb<EntityQuyTrinh, Dictionary<string, object>?>(b, x => x.SoDoLayout);
        DatJsonb<QuyTrinhBuoc, List<string>>(b, x => x.DanhSachTepBatBuoc);
        DatJsonb<QuyTrinhTruongHop, BieuThucDieuKien?>(b, x => x.DieuKien);
        DatJsonb<QuyTrinhTruongHop, List<string>>(b, x => x.HanhDong);
        DatJsonb<QuyTrinhThanhPhanHoSo, List<string>>(b, x => x.DinhDangChoPhep);
        DatJsonb<QuyTrinhChucNangBoSung, Dictionary<string, object>?>(b, x => x.CauHinh);
        DatJsonb<QuyTrinhLienThong, Dictionary<string, string>?>(b, x => x.CauHinhMapping);

        DatJsonb<BoTieuChi, Dictionary<string, object>?>(b, x => x.PhamViApDung);

        DatJsonb<HoiDongSangKien, List<Guid>>(b, x => x.LinhVucPhuTrach);
        DatJsonb<BienBanHop, Dictionary<string, object>?>(b, x => x.NoiDungJson);

        DatJsonb<HoSoSangKien, Dictionary<string, string>>(b, x => x.NoiDungDong);
        DatJsonb<SangKienLichSu, List<string>>(b, x => x.TruongThayDoi);
        DatJsonb<SangKienLichSu, Dictionary<string, string?>?>(b, x => x.GiaTriTruoc);
        DatJsonb<SangKienLichSu, Dictionary<string, string?>?>(b, x => x.GiaTriSau);
        DatJsonb<SangKienXuLy, List<Guid>>(b, x => x.TepDinhKemIds);
        DatJsonb<PhieuDanhGia, Dictionary<string, decimal>>(b, x => x.DiemTheoNhom);

        DatJsonb<Domain.Ai.KiemTraTrungLap, Dictionary<string, object>?>(b, x => x.PhamVi);
        DatJsonb<KiemTraTrungLapChiTiet, List<DoanTrungLap>>(b, x => x.CacDoanTrung);

        DatJsonb<PhamViDuLieu, List<Guid>>(b, x => x.DonViIds);
        DatJsonb<CauHinhHeThong, Dictionary<string, object>?>(b, x => x.GiaTriJson);
        DatJsonb<MauThongBao, List<string>>(b, x => x.DanhSachBien);
        DatJsonb<NhatKyKySo, Dictionary<string, object>?>(b, x => x.ThongTinXacThuc);
        DatJsonb<HeThongTichHop, Dictionary<string, string>?>(b, x => x.CauHinhMapping);
        DatJsonb<NhatKyLoi, Dictionary<string, object>?>(b, x => x.DuLieuNguCanh);

        // Embedding luu dang mang so thuc; index HNSW cua pgvector duoc tao trong migration SQL.
        b.Entity<SangKienDoanVan>()
            .Property(x => x.Embedding)
            .HasColumnType("real[]");

        b.Entity<SangKienDoanVan>()
            .Property(x => x.MoHinhNhung)
            .HasMaxLength(100);
    }

    private static void DatJsonb<TEntity, TProperty>(
        ModelBuilder b, Expression<Func<TEntity, TProperty>> thuocTinh)
        where TEntity : class
    {
        var converter = new ValueConverter<TProperty, string>(
            v => JsonSerializer.Serialize(v, TuyChonJson),
            v => JsonSerializer.Deserialize<TProperty>(v, TuyChonJson)!);

        var comparer = new ValueComparer<TProperty>(
            (a, c) => JsonSerializer.Serialize(a, TuyChonJson) == JsonSerializer.Serialize(c, TuyChonJson),
            v => v == null ? 0 : JsonSerializer.Serialize(v, TuyChonJson).GetHashCode(StringComparison.Ordinal),
            v => JsonSerializer.Deserialize<TProperty>(JsonSerializer.Serialize(v, TuyChonJson), TuyChonJson)!);

        b.Entity<TEntity>()
            .Property(thuocTinh)
            .HasColumnType("jsonb")
            .HasConversion(converter, comparer);
    }

    private static void CauHinhKhoaVaChiMuc(ModelBuilder b)
    {
        b.Entity<KhoaApiNgoai>(e =>
        {
            e.Property(x => x.Ten).HasMaxLength(200).IsRequired();
            e.Property(x => x.TienTo).HasMaxLength(20).IsRequired();
            e.Property(x => x.KhoaBam).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.TienTo);
        });

        b.Entity<MaXacThucTam>(e =>
        {
            e.Property(x => x.Loai).HasMaxLength(40).IsRequired();
            e.Property(x => x.Khoa).HasMaxLength(200).IsRequired();
            e.Property(x => x.MaBam).HasMaxLength(100).IsRequired();

            e.HasIndex(x => new { x.Loai, x.Khoa });
            e.HasIndex(x => x.HetHan);
        });

        b.Entity<BoLocYeuThich>(e =>
        {
            e.Property(x => x.ManHinh).HasMaxLength(50).IsRequired();
            e.Property(x => x.Ten).HasMaxLength(200).IsRequired();

            // ThamSo la chuoi JSON do man hinh tu dinh nghia nen giu nguyen kieu chuoi, chi
            // khai bao cot jsonb de PostgreSQL van kiem tra tinh hop le va truy van duoc.
            e.Property(x => x.ThamSo).HasColumnType("jsonb").IsRequired();

            e.HasIndex(x => new { x.NguoiDungId, x.ManHinh });

            // Moi nguoi dung chi duoc dat mot ten bo loc tren mot man hinh: trung ten thi ho
            // khong phan biet duoc hai muc trong danh sach chon.
            e.HasIndex(x => new { x.NguoiDungId, x.ManHinh, x.Ten })
                .IsUnique()
                .HasFilter("da_xoa = false");
        });

        b.Entity<NguoiDung>(e =>
        {
            e.Property(x => x.TenDangNhap).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.TenDangNhap).IsUnique().HasFilter("da_xoa = false");
            e.HasIndex(x => x.HoTenKhongDau);
            e.HasIndex(x => x.Email);
            e.HasIndex(x => x.DonViId);
        });

        b.Entity<Quyen>(e =>
        {
            e.Property(x => x.Ma).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Ma).IsUnique().HasFilter("da_xoa = false");
        });

        b.Entity<HoSoSangKien>(e =>
        {
            e.Property(x => x.MaHoSo).HasMaxLength(50).IsRequired();
            e.Property(x => x.TenSangKien).HasMaxLength(1000).IsRequired();
            e.HasIndex(x => x.MaHoSo).IsUnique().HasFilter("da_xoa = false");
            e.HasIndex(x => x.TenKhongDau);
            e.HasIndex(x => x.DotDeNghiId);
            e.HasIndex(x => x.LinhVucId);
            e.HasIndex(x => x.DonViId);
            e.HasIndex(x => x.TrangThaiTong);
            e.HasIndex(x => x.BuocHienTaiId);
            e.HasIndex(x => new { x.CongKhai, x.KetQua });
            e.HasIndex(x => x.HanXuLyHienTai);
        });

        b.Entity<CauHinhHeThong>(e =>
        {
            e.Property(x => x.Khoa).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Khoa).IsUnique().HasFilter("da_xoa = false");
        });

        b.Entity<RefreshToken>(e =>
        {
            e.Property(x => x.TokenHash).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.TokenHash);
            e.HasIndex(x => x.NguoiDungId);
        });

        b.Entity<NhatKyHeThong>(e =>
        {
            e.HasIndex(x => x.ThoiGian);
            e.HasIndex(x => x.NguoiDungId);
            e.HasIndex(x => new { x.Module, x.HanhDong });
            e.HasIndex(x => x.DoiTuongId);
        });

        b.Entity<NhatKyDangNhap>(e =>
        {
            e.HasIndex(x => x.ThoiGian);
            e.HasIndex(x => x.TenDangNhap);
        });

        b.Entity<ThongBao>(e =>
        {
            e.HasIndex(x => new { x.NguoiNhanId, x.DaDoc });
            e.HasIndex(x => x.ThoiGian);
        });

        b.Entity<SangKienDoanVan>(e =>
        {
            e.HasIndex(x => x.SangKienId);
            e.HasIndex(x => x.SimHash);
        });

        b.Entity<SangKienXuLy>(e =>
        {
            e.HasIndex(x => x.SangKienId);
            e.HasIndex(x => new { x.NguoiXuLyId, x.ThoiGianXuLy });
        });

        b.Entity<PhieuDanhGia>(e =>
        {
            e.HasIndex(x => new { x.SangKienId, x.HoiDongId, x.ThanhVienId }).IsUnique()
                .HasFilter("da_xoa = false");
        });

        b.Entity<SangKienPhanCong>(e =>
        {
            e.HasIndex(x => new { x.SangKienId, x.ThanhVienId }).IsUnique()
                .HasFilter("da_xoa = false");
        });

        b.Entity<DonVi>(e => e.HasIndex(x => x.Path));

        b.Entity<NgayNghiLe>(e => e.HasIndex(x => x.Ngay));
    }

    private static void CauHinhQuanHe(ModelBuilder b)
    {
        b.Entity<LinhVuc>()
            .HasMany(x => x.LinhVucCon)
            .WithOne(x => x.LinhVucCha!)
            .HasForeignKey(x => x.LinhVucChaId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<DonVi>()
            .HasMany(x => x.DonViCon)
            .WithOne(x => x.DonViCha!)
            .HasForeignKey(x => x.DonViChaId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<CauHinhMenu>()
            .HasMany(x => x.MenuCon)
            .WithOne(x => x.MenuCha!)
            .HasForeignKey(x => x.MenuChaId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<EntityQuyTrinh>()
            .HasMany(x => x.DanhSachBuoc)
            .WithOne(x => x.QuyTrinh!)
            .HasForeignKey(x => x.QuyTrinhId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<QuyTrinhBuoc>()
            .HasMany(x => x.TacNhan)
            .WithOne(x => x.Buoc!)
            .HasForeignKey(x => x.BuocId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<QuyTrinhBuoc>()
            .HasMany(x => x.TruongHop)
            .WithOne(x => x.Buoc!)
            .HasForeignKey(x => x.BuocId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<QuyTrinhBuoc>()
            .HasMany(x => x.TrangThai)
            .WithOne(x => x.Buoc!)
            .HasForeignKey(x => x.BuocId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<EntityQuyTrinh>()
            .HasMany(x => x.ThanhPhanHoSo)
            .WithOne(x => x.QuyTrinh!)
            .HasForeignKey(x => x.QuyTrinhId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<EntityQuyTrinh>()
            .HasMany(x => x.ChucNangBoSung)
            .WithOne(x => x.QuyTrinh!)
            .HasForeignKey(x => x.QuyTrinhId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<EntityQuyTrinh>()
            .HasMany(x => x.LienThong)
            .WithOne(x => x.QuyTrinh!)
            .HasForeignKey(x => x.QuyTrinhId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<EntityQuyTrinh>()
            .HasMany(x => x.TrangThaiToanCuc)
            .WithOne()
            .HasForeignKey(x => x.QuyTrinhId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<BoTieuChi>()
            .HasMany(x => x.DanhSachNhom)
            .WithOne(x => x.BoTieuChi!)
            .HasForeignKey(x => x.BoTieuChiId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<NhomTieuChi>()
            .HasMany(x => x.DanhSachTieuChi)
            .WithOne(x => x.NhomTieuChi!)
            .HasForeignKey(x => x.NhomTieuChiId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<TieuChiChamDiem>()
            .HasMany(x => x.DanhSachMucDiem)
            .WithOne(x => x.TieuChi!)
            .HasForeignKey(x => x.TieuChiId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<BoTieuChi>()
            .HasMany(x => x.DanhSachMucCongNhan)
            .WithOne(x => x.BoTieuChi!)
            .HasForeignKey(x => x.BoTieuChiId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<HoiDongSangKien>()
            .HasMany(x => x.ThanhVien)
            .WithOne(x => x.HoiDong!)
            .HasForeignKey(x => x.HoiDongId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<HoiDongSangKien>()
            .HasMany(x => x.PhienHop)
            .WithOne(x => x.HoiDong!)
            .HasForeignKey(x => x.HoiDongId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<PhienHopHoiDong>()
            .HasMany(x => x.DanhSachHoSo)
            .WithOne(x => x.PhienHop!)
            .HasForeignKey(x => x.PhienHopId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<PhienHopHoiDong>()
            .HasMany(x => x.DiemDanh)
            .WithOne(x => x.PhienHop!)
            .HasForeignKey(x => x.PhienHopId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<PhienHopHoiDong>()
            .HasMany(x => x.PhieuBoPhieu)
            .WithOne(x => x.PhienHop!)
            .HasForeignKey(x => x.PhienHopId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<BienBanHop>()
            .HasMany(x => x.ChuKy)
            .WithOne(x => x.BienBan!)
            .HasForeignKey(x => x.BienBanId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<HoSoSangKien>()
            .HasMany(x => x.DanhSachTacGia)
            .WithOne(x => x.SangKien!)
            .HasForeignKey(x => x.SangKienId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<HoSoSangKien>()
            .HasMany(x => x.TepDinhKem)
            .WithOne(x => x.SangKien!)
            .HasForeignKey(x => x.SangKienId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<SangKienTepDinhKem>()
            .HasOne(x => x.TepTin)
            .WithMany()
            .HasForeignKey(x => x.TepTinId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<HoSoSangKien>()
            .HasMany(x => x.LichSuXuLy)
            .WithOne(x => x.SangKien!)
            .HasForeignKey(x => x.SangKienId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<HoSoSangKien>()
            .HasMany(x => x.LichSuChinhSua)
            .WithOne(x => x.SangKien!)
            .HasForeignKey(x => x.SangKienId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<PhieuDanhGia>()
            .HasMany(x => x.ChiTiet)
            .WithOne(x => x.PhieuDanhGia!)
            .HasForeignKey(x => x.PhieuDanhGiaId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Domain.Ai.KiemTraTrungLap>()
            .HasMany(x => x.ChiTiet)
            .WithOne(x => x.KiemTra!)
            .HasForeignKey(x => x.KiemTraId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<QuyetDinh>()
            .HasMany(x => x.DanhSachSangKien)
            .WithOne(x => x.QuyetDinh!)
            .HasForeignKey(x => x.QuyetDinhId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<VaiTro>()
            .HasMany(x => x.DanhSachQuyen)
            .WithOne(x => x.VaiTro!)
            .HasForeignKey(x => x.VaiTroId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<VaiTroQuyen>()
            .HasOne(x => x.Quyen)
            .WithMany()
            .HasForeignKey(x => x.QuyenId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<VaiTro>()
            .HasMany(x => x.PhamViDuLieu)
            .WithOne(x => x.VaiTro!)
            .HasForeignKey(x => x.VaiTroId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<NguoiDung>()
            .HasMany(x => x.VaiTro)
            .WithOne(x => x.NguoiDung!)
            .HasForeignKey(x => x.NguoiDungId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<NguoiDungVaiTro>()
            .HasOne(x => x.VaiTro)
            .WithMany()
            .HasForeignKey(x => x.VaiTroId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <summary>Soft delete toan he thong: moi truy van tu dong loai ban ghi da_xoa = true.</summary>
    private static void ApDungLocXoaMem(ModelBuilder b)
    {
        foreach (var kieu in b.Model.GetEntityTypes())
        {
            if (!typeof(ThucThe).IsAssignableFrom(kieu.ClrType))
            {
                continue;
            }

            var thamSo = Expression.Parameter(kieu.ClrType, "e");
            var thanThuocTinh = Expression.Property(thamSo, nameof(ThucThe.DaXoa));
            var bieuThuc = Expression.Lambda(Expression.Not(thanThuocTinh), thamSo);

            b.Entity(kieu.ClrType).HasQueryFilter(bieuThuc);
        }
    }
}
