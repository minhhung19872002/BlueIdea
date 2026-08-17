using BlueIdea.Application.Chung;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.DanhMuc;
using BlueIdea.Domain.HoiDong;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Domain.QuyTrinh;
using BlueIdea.Domain.TieuChi;
using BlueIdea.Infrastructure.Persistence;
using BlueIdea.Shared.TiengViet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EntityQuyTrinh = BlueIdea.Domain.QuyTrinh.QuyTrinh;

namespace BlueIdea.Infrastructure.Seed;

/// <summary>
/// Sinh du lieu mau bat buoc theo Muc 10 dac ta, cho phep demo duoc ngay sau khi chay migration.
/// Ham nay idempotent: chay nhieu lan khong tao trung du lieu.
/// </summary>
public sealed partial class DuLieuMau
{
    /// <summary>Mat khau thong nhat cho tai khoan mau (buoc doi lan dau).</summary>
    public const string MatKhauMacDinh = "Sk@2026";

    private readonly AppDbContext _db;
    private readonly IDichVuMatKhau _matKhau;
    private readonly ILogger<DuLieuMau> _logger;

    public DuLieuMau(AppDbContext db, IDichVuMatKhau matKhau, ILogger<DuLieuMau> logger)
    {
        _db = db;
        _matKhau = matKhau;
        _logger = logger;
    }

    public async Task ChayAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Bắt đầu nạp dữ liệu mẫu...");

        await SeedQuyenVaVaiTroAsync(ct).ConfigureAwait(false);
        await SeedCauHinhHeThongAsync(ct).ConfigureAwait(false);
        await SeedMenuAsync(ct).ConfigureAwait(false);
        await SeedNgayNghiLeAsync(ct).ConfigureAwait(false);
        var donVi = await SeedDonViAsync(ct).ConfigureAwait(false);
        await SeedDanhMucAsync(ct).ConfigureAwait(false);
        var nguoiDung = await SeedNguoiDungAsync(donVi, ct).ConfigureAwait(false);
        var boTieuChi = await SeedBoTieuChiAsync(ct).ConfigureAwait(false);
        var hoiDong = await SeedHoiDongAsync(donVi, nguoiDung, ct).ConfigureAwait(false);
        var quyTrinh = await SeedQuyTrinhAsync(hoiDong.Id, boTieuChi.Id, ct).ConfigureAwait(false);
        await SeedDotDeNghiAsync(quyTrinh.Id, boTieuChi.Id, donVi, ct).ConfigureAwait(false);
        await SeedMauThongBaoAsync(ct).ConfigureAwait(false);
        await SeedHoSoSangKienAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Nạp dữ liệu mẫu hoàn tất.");
    }

    // ------------------------------------------------------------------------------------
    // Quyen + vai tro
    // ------------------------------------------------------------------------------------

    private async Task SeedQuyenVaVaiTroAsync(CancellationToken ct)
    {
        if (!await _db.Quyen.AnyAsync(ct).ConfigureAwait(false))
        {
            var thuTu = 1;
            foreach (var (ma, ten, nhom) in DanhSachQuyenChuan())
            {
                _db.Quyen.Add(new Quyen
                {
                    Ma = ma,
                    Ten = ten,
                    NhomChucNang = nhom,
                    ThuTu = thuTu++
                });
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        if (await _db.VaiTro.AnyAsync(ct).ConfigureAwait(false))
        {
            return;
        }

        var tatCaQuyen = await _db.Quyen.ToDictionaryAsync(x => x.Ma, ct).ConfigureAwait(false);

        foreach (var (ma, ten, moTa, quyenCuaVaiTro, phamVi) in DanhSachVaiTroChuan())
        {
            var vaiTro = new VaiTro
            {
                Ma = ma,
                Ten = ten,
                TenKhongDau = VanBanTiengViet.TaoKhongDau(ten),
                MoTa = moTa,
                LaHeThong = true
            };

            // Quan tri he thong duoc cap toan bo quyen (dac ta: "toan quyen cau hinh").
            var quyenApDung = ma == MaVaiTro.QuanTriHeThong
                ? tatCaQuyen.Keys.ToArray()
                : quyenCuaVaiTro;

            foreach (var maQuyen in quyenApDung)
            {
                if (tatCaQuyen.TryGetValue(maQuyen, out var q))
                {
                    vaiTro.DanhSachQuyen.Add(new VaiTroQuyen { VaiTroId = vaiTro.Id, QuyenId = q.Id });
                }
            }

            vaiTro.PhamViDuLieu.Add(new PhamViDuLieu
            {
                VaiTroId = vaiTro.Id,
                LoaiPhamVi = phamVi
            });

            _db.VaiTro.Add(vaiTro);
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static IEnumerable<(string Ma, string Ten, string Nhom)> DanhSachQuyenChuan() => new[]
    {
        (MaQuyen.DanhMucXem, "Xem danh mục", "Danh mục"),
        (MaQuyen.DanhMucThem, "Thêm danh mục", "Danh mục"),
        (MaQuyen.DanhMucSua, "Sửa danh mục", "Danh mục"),
        (MaQuyen.DanhMucXoa, "Xóa danh mục", "Danh mục"),
        (MaQuyen.DanhMucXuat, "Xuất danh mục", "Danh mục"),
        (MaQuyen.DanhMucNhap, "Nhập danh mục", "Danh mục"),

        (MaQuyen.SangKienXem, "Xem hồ sơ sáng kiến", "Sáng kiến"),
        (MaQuyen.SangKienThem, "Tạo hồ sơ sáng kiến", "Sáng kiến"),
        (MaQuyen.SangKienSua, "Sửa hồ sơ sáng kiến", "Sáng kiến"),
        (MaQuyen.SangKienXoa, "Xóa hồ sơ sáng kiến", "Sáng kiến"),
        (MaQuyen.SangKienNop, "Nộp hồ sơ sáng kiến", "Sáng kiến"),
        (MaQuyen.SangKienRut, "Rút hồ sơ sáng kiến", "Sáng kiến"),
        (MaQuyen.SangKienXuat, "Xuất danh sách sáng kiến", "Sáng kiến"),
        (MaQuyen.SangKienXemTatCa, "Xem toàn bộ hồ sơ", "Sáng kiến"),

        (MaQuyen.TiepNhanXem, "Xem danh sách tiếp nhận", "Tiếp nhận"),
        (MaQuyen.TiepNhanXuLy, "Tiếp nhận hồ sơ", "Tiếp nhận"),
        (MaQuyen.XuLyXem, "Xem việc cần xử lý", "Xử lý"),
        (MaQuyen.XuLyThucThi, "Thực thi bước xử lý", "Xử lý"),
        (MaQuyen.XuLyThuHoi, "Thu hồi bước đã xử lý", "Xử lý"),
        (MaQuyen.XuLyUyQuyen, "Ủy quyền xử lý", "Xử lý"),

        (MaQuyen.DanhGiaXem, "Xem hồ sơ đánh giá", "Đánh giá"),
        (MaQuyen.DanhGiaChamDiem, "Chấm điểm hồ sơ", "Đánh giá"),
        (MaQuyen.DanhGiaPhanCong, "Phân công chấm điểm", "Đánh giá"),
        (MaQuyen.DanhGiaTongHop, "Tổng hợp điểm", "Đánh giá"),
        (MaQuyen.DanhGiaMoLaiPhieu, "Mở lại phiếu đã gửi", "Đánh giá"),

        (MaQuyen.HoiDongXem, "Xem hội đồng", "Hội đồng"),
        (MaQuyen.HoiDongCauHinh, "Cấu hình hội đồng", "Hội đồng"),
        (MaQuyen.HoiDongHopPhien, "Quản lý phiên họp", "Hội đồng"),
        (MaQuyen.HoiDongBoPhieu, "Bỏ phiếu", "Hội đồng"),
        (MaQuyen.HoiDongKetLuan, "Kết luận hội đồng", "Hội đồng"),

        (MaQuyen.QuyTrinhXem, "Xem quy trình", "Quy trình"),
        (MaQuyen.QuyTrinhCauHinh, "Cấu hình quy trình", "Quy trình"),
        (MaQuyen.TieuChiXem, "Xem bộ tiêu chí", "Tiêu chí"),
        (MaQuyen.TieuChiCauHinh, "Cấu hình bộ tiêu chí", "Tiêu chí"),

        (MaQuyen.QuyetDinhXem, "Xem quyết định", "Quyết định"),
        (MaQuyen.QuyetDinhBanHanh, "Ban hành quyết định", "Quyết định"),
        (MaQuyen.QuyetDinhKySo, "Ký số quyết định", "Quyết định"),

        (MaQuyen.BaoCaoXem, "Xem báo cáo", "Báo cáo"),
        (MaQuyen.BaoCaoXuat, "Xuất báo cáo", "Báo cáo"),
        (MaQuyen.BaoCaoCauHinh, "Cấu hình mẫu báo cáo", "Báo cáo"),

        (MaQuyen.TrungLapXem, "Xem báo cáo trùng lặp", "Trùng lặp"),
        (MaQuyen.TrungLapChayLai, "Chạy lại kiểm tra trùng lặp", "Trùng lặp"),

        (MaQuyen.NguoiDungXem, "Xem người dùng", "Quản trị"),
        (MaQuyen.NguoiDungThem, "Thêm người dùng", "Quản trị"),
        (MaQuyen.NguoiDungSua, "Sửa người dùng", "Quản trị"),
        (MaQuyen.NguoiDungXoa, "Xóa người dùng", "Quản trị"),
        (MaQuyen.NguoiDungDatLaiMatKhau, "Đặt lại mật khẩu", "Quản trị"),
        (MaQuyen.DonViXem, "Xem đơn vị", "Quản trị"),
        (MaQuyen.DonViCauHinh, "Cấu hình đơn vị", "Quản trị"),
        (MaQuyen.VaiTroXem, "Xem vai trò", "Quản trị"),
        (MaQuyen.VaiTroCauHinh, "Cấu hình vai trò", "Quản trị"),
        (MaQuyen.CauHinhXem, "Xem cấu hình hệ thống", "Quản trị"),
        (MaQuyen.CauHinhSua, "Sửa cấu hình hệ thống", "Quản trị"),
        (MaQuyen.NhatKyXem, "Xem nhật ký", "Quản trị"),
        (MaQuyen.TichHopCauHinh, "Cấu hình tích hợp", "Tích hợp"),
        (MaQuyen.TichHopDongBo, "Đồng bộ dữ liệu", "Tích hợp")
    };

    private static IEnumerable<(string Ma, string Ten, string MoTa, string[] Quyen, string PhamVi)>
        DanhSachVaiTroChuan() => new[]
    {
        (MaVaiTro.TacGia, "Tác giả", "Nộp, sửa, rút hồ sơ và theo dõi tiến độ",
            new[]
            {
                MaQuyen.SangKienXem, MaQuyen.SangKienThem, MaQuyen.SangKienSua,
                MaQuyen.SangKienNop, MaQuyen.SangKienRut, MaQuyen.TrungLapXem,
                MaQuyen.DanhMucXem
            },
            LoaiPhamViDuLieu.CaNhan),

        (MaVaiTro.CanBoTiepNhan, "Cán bộ tiếp nhận", "Kiểm tra tính hợp lệ, tiếp nhận hoặc trả hồ sơ",
            new[]
            {
                MaQuyen.SangKienXem, MaQuyen.SangKienXemTatCa, MaQuyen.SangKienXuat,
                MaQuyen.TiepNhanXem, MaQuyen.TiepNhanXuLy,
                MaQuyen.XuLyXem, MaQuyen.XuLyThucThi, MaQuyen.XuLyThuHoi,
                MaQuyen.TrungLapXem, MaQuyen.DanhMucXem, MaQuyen.BaoCaoXem
            },
            LoaiPhamViDuLieu.DonViVaCapDuoi),

        (MaVaiTro.ThuKyHoiDong, "Thư ký hội đồng", "Phân công chấm, tổng hợp điểm, lập biên bản",
            new[]
            {
                MaQuyen.SangKienXem, MaQuyen.SangKienXemTatCa, MaQuyen.SangKienXuat,
                MaQuyen.XuLyXem, MaQuyen.XuLyThucThi,
                MaQuyen.DanhGiaXem, MaQuyen.DanhGiaPhanCong, MaQuyen.DanhGiaTongHop,
                MaQuyen.DanhGiaMoLaiPhieu,
                MaQuyen.HoiDongXem, MaQuyen.HoiDongHopPhien,
                MaQuyen.TrungLapXem, MaQuyen.TrungLapChayLai,
                MaQuyen.DanhMucXem, MaQuyen.BaoCaoXem, MaQuyen.BaoCaoXuat
            },
            LoaiPhamViDuLieu.DonViVaCapDuoi),

        (MaVaiTro.ThanhVienHoiDong, "Thành viên hội đồng", "Chấm điểm, nhận xét, bỏ phiếu",
            new[]
            {
                MaQuyen.SangKienXem, MaQuyen.DanhGiaXem, MaQuyen.DanhGiaChamDiem,
                MaQuyen.HoiDongXem, MaQuyen.HoiDongBoPhieu,
                MaQuyen.XuLyXem, MaQuyen.XuLyThucThi,
                MaQuyen.TrungLapXem, MaQuyen.DanhMucXem
            },
            LoaiPhamViDuLieu.DonViVaCapDuoi),

        (MaVaiTro.ChuTichHoiDong, "Chủ tịch hội đồng", "Kết luận, ký biên bản",
            new[]
            {
                MaQuyen.SangKienXem, MaQuyen.SangKienXemTatCa,
                MaQuyen.DanhGiaXem, MaQuyen.DanhGiaChamDiem, MaQuyen.DanhGiaTongHop,
                MaQuyen.HoiDongXem, MaQuyen.HoiDongBoPhieu, MaQuyen.HoiDongKetLuan,
                MaQuyen.HoiDongHopPhien,
                MaQuyen.XuLyXem, MaQuyen.XuLyThucThi,
                MaQuyen.TrungLapXem, MaQuyen.DanhMucXem, MaQuyen.BaoCaoXem
            },
            LoaiPhamViDuLieu.DonViVaCapDuoi),

        (MaVaiTro.LanhDaoPheDuyet, "Lãnh đạo phê duyệt", "Phê duyệt và ban hành quyết định",
            new[]
            {
                MaQuyen.SangKienXem, MaQuyen.SangKienXemTatCa,
                MaQuyen.XuLyXem, MaQuyen.XuLyThucThi,
                MaQuyen.QuyetDinhXem, MaQuyen.QuyetDinhBanHanh, MaQuyen.QuyetDinhKySo,
                MaQuyen.TrungLapXem, MaQuyen.DanhMucXem,
                MaQuyen.BaoCaoXem, MaQuyen.BaoCaoXuat
            },
            LoaiPhamViDuLieu.DonViVaCapDuoi),

        (MaVaiTro.QuanTriDonVi, "Quản trị đơn vị", "Quản lý người dùng trong đơn vị, xem thống kê đơn vị",
            new[]
            {
                MaQuyen.SangKienXem, MaQuyen.SangKienXemTatCa,
                MaQuyen.NguoiDungXem, MaQuyen.NguoiDungThem, MaQuyen.NguoiDungSua,
                MaQuyen.NguoiDungDatLaiMatKhau,
                MaQuyen.DonViXem, MaQuyen.DanhMucXem,
                MaQuyen.BaoCaoXem, MaQuyen.BaoCaoXuat, MaQuyen.NhatKyXem
            },
            LoaiPhamViDuLieu.DonViVaCapDuoi),

        (MaVaiTro.QuanTriHeThong, "Quản trị hệ thống", "Toàn quyền cấu hình hệ thống",
            Array.Empty<string>(), // Quan tri he thong duoc cap toan bo quyen o duoi.
            LoaiPhamViDuLieu.ToanHeThong),

        (MaVaiTro.LanhDaoXem, "Lãnh đạo/xem báo cáo", "Chỉ đọc dashboard và báo cáo",
            new[]
            {
                MaQuyen.SangKienXem, MaQuyen.SangKienXemTatCa,
                MaQuyen.BaoCaoXem, MaQuyen.BaoCaoXuat, MaQuyen.DanhMucXem
            },
            LoaiPhamViDuLieu.ToanHeThong)
    };

    // ------------------------------------------------------------------------------------
    // Cau hinh he thong + menu + ngay le
    // ------------------------------------------------------------------------------------

    private async Task SeedCauHinhHeThongAsync(CancellationToken ct)
    {
        if (await _db.CauHinhHeThong.AnyAsync(ct).ConfigureAwait(false))
        {
            return;
        }

        var cauHinh = new (string Nhom, string Khoa, string GiaTri, string Kieu, string TenHienThi)[]
        {
            ("CHUNG", KhoaCauHinh.TenHeThong, "Nền tảng số dùng chung phục vụ hoạt động sáng kiến",
                "TEXT", "Tên hệ thống"),
            ("CHUNG", KhoaCauHinh.TenDonVi, "Ủy ban nhân dân thành phố", "TEXT", "Tên đơn vị"),
            ("CHUNG", KhoaCauHinh.DiaChi, "Số 1, đường Trung tâm, thành phố", "TEXT", "Địa chỉ"),
            ("CHUNG", KhoaCauHinh.EmailHoTro, "hotro@sangkien.gov.vn", "TEXT", "Email hỗ trợ"),
            ("CHUNG", KhoaCauHinh.DienThoaiHoTro, "0236.3888.999", "TEXT", "Điện thoại hỗ trợ"),
            ("GIAO_DIEN", KhoaCauHinh.MauChuDao, "#1677ff", "COLOR", "Màu chủ đạo"),
            ("HO_SO", KhoaCauHinh.MauMaHoSo, "SK-{NAM}-{STT:0000}", "TEXT", "Mẫu mã hồ sơ"),
            ("HO_SO", KhoaCauHinh.DungLuongTepToiDaMb, "20", "NUMBER", "Dung lượng tệp tối đa (MB)"),
            ("HO_SO", KhoaCauHinh.SoTepToiDa, "20", "NUMBER", "Số tệp tối đa mỗi hồ sơ"),
            ("TRUNG_LAP", KhoaCauHinh.MucCanhBaoTrungLapVang, "20", "NUMBER", "Ngưỡng cảnh báo vàng (%)"),
            ("TRUNG_LAP", KhoaCauHinh.MucCanhBaoTrungLapDo, "40", "NUMBER", "Ngưỡng cảnh báo đỏ (%)"),
            ("TRUNG_LAP", KhoaCauHinh.HeSoTuVung, "0.4", "NUMBER", "Hệ số thành phần từ vựng"),
            ("TRUNG_LAP", KhoaCauHinh.HeSoNguNghia, "0.6", "NUMBER", "Hệ số thành phần ngữ nghĩa"),
            ("TRUNG_LAP", KhoaCauHinh.TuDongKiemTraTrungLap, "true", "BOOLEAN",
                "Tự động kiểm tra trùng lặp khi nộp"),
            ("XU_LY", KhoaCauHinh.SoNgayNhacTruocHan, "1", "NUMBER", "Số ngày nhắc trước hạn"),
            ("BAO_MAT", KhoaCauHinh.ChinhSachMatKhauDoDaiToiThieu, "8", "NUMBER",
                "Độ dài tối thiểu mật khẩu"),
            ("BAO_MAT", KhoaCauHinh.ChinhSachMatKhauSoNgayHetHan, "90", "NUMBER",
                "Số ngày buộc đổi mật khẩu"),
            ("BAO_MAT", KhoaCauHinh.ChinhSachMatKhauSoLanKhongTrung, "3", "NUMBER",
                "Không trùng N mật khẩu gần nhất"),
            ("BAO_MAT", KhoaCauHinh.SoLanDangNhapSaiToiDa, "5", "NUMBER", "Số lần đăng nhập sai tối đa"),
            ("BAO_MAT", KhoaCauHinh.ThoiGianKhoaTaiKhoanPhut, "15", "NUMBER", "Thời gian khóa (phút)")
        };

        var thuTu = 1;
        foreach (var (nhom, khoa, giaTri, kieu, ten) in cauHinh)
        {
            _db.CauHinhHeThong.Add(new CauHinhHeThong
            {
                Nhom = nhom,
                Khoa = khoa,
                GiaTri = giaTri,
                KieuDuLieu = kieu,
                TenHienThi = ten,
                ThuTu = thuTu++
            });
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task SeedNgayNghiLeAsync(CancellationToken ct)
    {
        if (await _db.NgayNghiLe.AnyAsync(ct).ConfigureAwait(false))
        {
            return;
        }

        var ngayLe = new (int Thang, int Ngay, string Ten)[]
        {
            (1, 1, "Tết Dương lịch"),
            (4, 30, "Ngày Giải phóng miền Nam"),
            (5, 1, "Ngày Quốc tế Lao động"),
            (9, 1, "Quốc khánh (ngày liền kề)"),
            (9, 2, "Quốc khánh"),
            (3, 10, "Giỗ Tổ Hùng Vương (âm lịch - cấu hình lại theo năm)")
        };

        foreach (var (thang, ngay, ten) in ngayLe)
        {
            _db.NgayNghiLe.Add(new NgayNghiLe
            {
                Ngay = new DateOnly(2000, thang, ngay),
                Ten = ten,
                LapLaiHangNam = true
            });
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Nap cau hinh menu.
    ///
    /// Bo sung THEO TUNG MA thay vi bo qua ca bang khi da co du lieu: khi nang cap len phien ban
    /// co them man hinh moi, he thong dang chay phai nhan duoc muc menu moi ma khong lam mat cac
    /// muc quan tri vien da tu sua.
    /// </summary>
    private async Task SeedMenuAsync(CancellationToken ct)
    {
        var maDaCo = await _db.CauHinhMenu.AsNoTracking()
            .Select(x => x.Ma)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var daCo = maDaCo.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var menu = new List<CauHinhMenu>();

        // Ma tuong ung voi TUNG Id sinh ra - ke ca muc khong duoc them, de con tra nguoc ve cha.
        var maTheoId = new Dictionary<Guid, string>();
        var thuTu = 1;

        CauHinhMenu Them(string ma, string ten, string? duongDan, string? icon,
            string? quyen, Guid? chaId = null)
        {
            var m = new CauHinhMenu
            {
                Id = Guid.NewGuid(),
                Ma = ma,
                Ten = ten,
                DuongDan = duongDan,
                Icon = icon,
                QuyenMa = quyen,
                MenuChaId = chaId,
                ThuTu = thuTu++,
                Loai = "WEB"
            };

            maTheoId[m.Id] = ma;

            // Muc da ton tai thi giu nguyen ban ghi cua quan tri vien, chi bo qua o buoc them.
            if (!daCo.Contains(ma))
            {
                menu.Add(m);
            }

            return m;
        }

        Them("DASHBOARD", "Trang chủ", "/", "DashboardOutlined", null);
        Them("SK_CUA_TOI", "Hồ sơ của tôi", "/sang-kien/cua-toi", "FileTextOutlined", MaQuyen.SangKienXem);
        Them("SK_NOP_MOI", "Nộp sáng kiến", "/sang-kien/nop-moi", "PlusCircleOutlined", MaQuyen.SangKienThem);
        Them("TIEP_NHAN", "Tiếp nhận hồ sơ", "/tiep-nhan", "InboxOutlined", MaQuyen.TiepNhanXem);
        Them("XU_LY", "Việc cần xử lý", "/xu-ly", "AuditOutlined", MaQuyen.XuLyXem);
        Them("DANH_GIA", "Hồ sơ đánh giá", "/danh-gia", "StarOutlined", MaQuyen.DanhGiaXem);
        Them("HOI_DONG", "Hội đồng sáng kiến", "/hoi-dong", "TeamOutlined", MaQuyen.HoiDongXem);
        Them("QUYET_DINH", "Quyết định công nhận", "/quyet-dinh", "SafetyCertificateOutlined",
            MaQuyen.QuyetDinhXem);
        Them("TRA_CUU", "Tra cứu", "/tra-cuu", "SearchOutlined", MaQuyen.SangKienXem);

        var baoCao = Them("BAO_CAO", "Báo cáo thống kê", null, "BarChartOutlined", MaQuyen.BaoCaoXem);
        Them("BC_DAT", "Sáng kiến đạt", "/bao-cao/sang-kien-dat", null, MaQuyen.BaoCaoXem, baoCao.Id);
        Them("BC_CHUA_DAT", "Sáng kiến chưa đạt", "/bao-cao/sang-kien-chua-dat", null,
            MaQuyen.BaoCaoXem, baoCao.Id);
        Them("BC_DON_VI", "Theo đơn vị", "/bao-cao/theo-don-vi", null, MaQuyen.BaoCaoXem, baoCao.Id);
        Them("BC_KET_QUA", "Kết quả sáng kiến", "/bao-cao/ket-qua", null, MaQuyen.BaoCaoXem, baoCao.Id);

        var quanTri = Them("QUAN_TRI", "Quản trị hệ thống", null, "SettingOutlined", MaQuyen.CauHinhXem);
        var danhMuc = Them("DANH_MUC", "Danh mục", null, "AppstoreOutlined", MaQuyen.DanhMucXem, quanTri.Id);
        Them("DM_LINH_VUC", "Lĩnh vực", "/quan-tri/danh-muc/linh-vuc", null, MaQuyen.DanhMucXem, danhMuc.Id);
        Them("DM_DOI_TUONG", "Đối tượng", "/quan-tri/danh-muc/doi-tuong", null, MaQuyen.DanhMucXem, danhMuc.Id);
        Them("DM_DOT", "Đợt đề nghị", "/quan-tri/danh-muc/dot-de-nghi", null, MaQuyen.DanhMucXem, danhMuc.Id);
        Them("DM_LOAI_TG", "Loại tác giả", "/quan-tri/danh-muc/loai-tac-gia", null,
            MaQuyen.DanhMucXem, danhMuc.Id);
        Them("DM_BM_XUAT", "Biểu mẫu xuất", "/quan-tri/danh-muc/bieu-mau-xuat", null,
            MaQuyen.DanhMucXem, danhMuc.Id);
        Them("DM_BM_TK", "Biểu mẫu thống kê", "/quan-tri/danh-muc/bieu-mau-thong-ke", null,
            MaQuyen.BaoCaoCauHinh, danhMuc.Id);
        Them("DM_QUYET_DINH", "Quyết định", "/quan-tri/danh-muc/quyet-dinh", null,
            MaQuyen.QuyetDinhXem, danhMuc.Id);

        Them("QT_QUY_TRINH", "Quy trình xử lý", "/quan-tri/quy-trinh", null,
            MaQuyen.QuyTrinhXem, quanTri.Id);
        Them("QT_TIEU_CHI", "Bộ tiêu chí", "/quan-tri/tieu-chi", null, MaQuyen.TieuChiXem, quanTri.Id);
        Them("QT_NGUOI_DUNG", "Người dùng", "/quan-tri/nguoi-dung", null, MaQuyen.NguoiDungXem, quanTri.Id);
        Them("QT_DON_VI", "Đơn vị", "/quan-tri/don-vi", null, MaQuyen.DonViXem, quanTri.Id);
        Them("QT_VAI_TRO", "Vai trò & phân quyền", "/quan-tri/vai-tro", null, MaQuyen.VaiTroXem, quanTri.Id);

        var cauHinh = Them("QT_CAU_HINH", "Cấu hình", null, null, MaQuyen.CauHinhXem, quanTri.Id);
        Them("CH_HE_THONG", "Hệ thống", "/quan-tri/cau-hinh/he-thong", null, MaQuyen.CauHinhXem, cauHinh.Id);
        Them("CH_MENU", "Menu", "/quan-tri/cau-hinh/menu", null, MaQuyen.CauHinhSua, cauHinh.Id);
        Them("CH_EMAIL_SMS", "Email & SMS", "/quan-tri/cau-hinh/email-sms", null,
            MaQuyen.CauHinhSua, cauHinh.Id);
        Them("CH_CHU_KY_SO", "Chữ ký số", "/quan-tri/cau-hinh/chu-ky-so", null,
            MaQuyen.CauHinhSua, cauHinh.Id);
        Them("CH_SANG_KIEN", "Thông tin sáng kiến", "/quan-tri/cau-hinh/sang-kien", null,
            MaQuyen.CauHinhSua, cauHinh.Id);
        Them("CH_TICH_HOP", "Tích hợp hệ thống", "/quan-tri/cau-hinh/tich-hop", null,
            MaQuyen.TichHopCauHinh, cauHinh.Id);

        var nhatKy = Them("QT_NHAT_KY", "Nhật ký", null, null, MaQuyen.NhatKyXem, quanTri.Id);
        Them("NK_HE_THONG", "Nhật ký hệ thống", "/quan-tri/nhat-ky/he-thong", null,
            MaQuyen.NhatKyXem, nhatKy.Id);
        Them("NK_DANG_NHAP", "Nhật ký đăng nhập", "/quan-tri/nhat-ky/dang-nhap", null,
            MaQuyen.NhatKyXem, nhatKy.Id);
        Them("NK_LOI", "Nhật ký lỗi", "/quan-tri/nhat-ky/loi", null, MaQuyen.NhatKyXem, nhatKy.Id);
        Them("NK_DONG_BO", "Nhật ký đồng bộ", "/quan-tri/nhat-ky/dong-bo", null,
            MaQuyen.NhatKyXem, nhatKy.Id);

        if (menu.Count == 0)
        {
            return;
        }

        // Muc cha co the da co san trong CSDL (chi muc con la moi). Khi do Id sinh ra o tren chi la
        // Id tam, phai tro lai Id THAT trong CSDL, neu khong cay menu se co nhanh mo coi.
        var idTheoMa = await _db.CauHinhMenu.AsNoTracking()
            .ToDictionaryAsync(x => x.Ma, x => x.Id, StringComparer.OrdinalIgnoreCase, ct)
            .ConfigureAwait(false);

        var idSeThem = menu.Select(x => x.Id).ToHashSet();

        foreach (var m in menu.Where(x => x.MenuChaId.HasValue))
        {
            if (idSeThem.Contains(m.MenuChaId!.Value))
            {
                continue;
            }

            m.MenuChaId = maTheoId.TryGetValue(m.MenuChaId.Value, out var maCha)
                          && idTheoMa.TryGetValue(maCha, out var idThat)
                ? idThat
                : null;
        }

        _db.CauHinhMenu.AddRange(menu);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Đã bổ sung {SoMuc} mục menu mới.", menu.Count);
    }
}
