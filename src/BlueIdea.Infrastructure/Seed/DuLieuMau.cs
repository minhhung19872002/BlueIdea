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

        // Don du lieu cho he thong da cai dat — xem DuLieuMau.SuaDuLieuCu.cs.
        await BoDieuKienTuChanAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Nạp dữ liệu mẫu hoàn tất.");
    }

    // ------------------------------------------------------------------------------------
    // Quyen + vai tro
    // ------------------------------------------------------------------------------------

    private async Task SeedQuyenVaVaiTroAsync(CancellationToken ct)
    {
        /*
         * Doi chieu theo MA thay vi "bang rong thi nap".
         *
         * Truoc day chi nap khi bang con rong, nen mot quyen them o phien ban sau khong bao gio
         * toi duoc he thong da cai dat. Hau qua khong hien ra ngay ma rat kho lan: khong co dong
         * trong bang quyen -> khong hien tren man hinh ma tran vai tro -> khong ai cap duoc, ke ca
         * quan tri vien -> endpoint bao ve bang quyen do tra 403 cho tat ca moi nguoi, vinh vien.
         */
        var quyenDaCo = await _db.Quyen
            .Where(x => !x.DaXoa)
            .ToDictionaryAsync(x => x.Ma, ct)
            .ConfigureAwait(false);

        var thuTu = quyenDaCo.Count + 1;
        var quyenMoi = new List<Quyen>();

        foreach (var (ma, ten, nhom) in DanhSachQuyenChuan())
        {
            if (quyenDaCo.ContainsKey(ma)) continue;

            var q = new Quyen
            {
                Ma = ma,
                Ten = ten,
                NhomChucNang = nhom,
                ThuTu = thuTu++
            };

            _db.Quyen.Add(q);
            quyenMoi.Add(q);
        }

        if (quyenMoi.Count > 0)
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("Da bo sung {SoQuyen} quyen con thieu.", quyenMoi.Count);
        }

        var tatCaQuyen = await _db.Quyen
            .Where(x => !x.DaXoa)
            .ToDictionaryAsync(x => x.Ma, ct)
            .ConfigureAwait(false);

        await CapQuyenMoiChoQuanTriAsync(tatCaQuyen, ct).ConfigureAwait(false);

        if (await _db.VaiTro.AnyAsync(ct).ConfigureAwait(false))
        {
            return;
        }

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

    /// <summary>
    /// Cap moi quyen vua bo sung cho vai tro Quan tri he thong.
    ///
    /// Vai tro nay duoc dac ta dinh nghia la "toan quyen cau hinh", va danh sach quyen cua no
    /// truoc day duoc tinh MOT LAN luc cai dat. Khong lam buoc nay thi quyen moi co dong trong
    /// bang nhung khong ai duoc cap — ke ca quan tri vien — nen chuc nang moi khoa cung.
    ///
    /// Chi THEM, khong bao gio go: quan tri vien co the da co y bo bot quyen cua mot vai tro khac,
    /// va viec cua ham nay khong phai la dat lai ma tran quyen ve mac dinh.
    /// </summary>
    private async Task CapQuyenMoiChoQuanTriAsync(
        IReadOnlyDictionary<string, Quyen> tatCaQuyen, CancellationToken ct)
    {
        var quanTri = await _db.VaiTro
            .Include(x => x.DanhSachQuyen)
            .FirstOrDefaultAsync(x => x.Ma == MaVaiTro.QuanTriHeThong && !x.DaXoa, ct)
            .ConfigureAwait(false);

        if (quanTri is null) return;

        var daCo = quanTri.DanhSachQuyen.Select(x => x.QuyenId).ToHashSet();
        var soThem = 0;

        foreach (var q in tatCaQuyen.Values)
        {
            if (daCo.Contains(q.Id)) continue;

            quanTri.DanhSachQuyen.Add(new VaiTroQuyen { VaiTroId = quanTri.Id, QuyenId = q.Id });
            soThem++;
        }

        if (soThem > 0)
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            _logger.LogInformation(
                "Da cap {SoQuyen} quyen moi cho vai tro Quan tri he thong.", soThem);
        }
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
        (MaQuyen.SangKienHuy, "Huỷ hồ sơ đã nộp", "Sáng kiến"),
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
        (MaQuyen.XuLyGiaHan, "Gia hạn xử lý", "Xử lý"),

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
        (MaQuyen.TrungLapXemXet, "Ghi ý kiến xem xét trùng lặp", "Trùng lặp"),

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
                MaQuyen.SangKienHuy,
                MaQuyen.TiepNhanXem, MaQuyen.TiepNhanXuLy,
                MaQuyen.XuLyXem, MaQuyen.XuLyThucThi, MaQuyen.XuLyThuHoi, MaQuyen.XuLyGiaHan,
                MaQuyen.TrungLapXem, MaQuyen.TrungLapXemXet,
                MaQuyen.DanhMucXem, MaQuyen.BaoCaoXem
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
                MaQuyen.TrungLapXem, MaQuyen.TrungLapChayLai, MaQuyen.TrungLapXemXet,
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
                MaQuyen.TrungLapXem, MaQuyen.TrungLapXemXet,
                MaQuyen.DanhMucXem, MaQuyen.BaoCaoXem
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
        var cauHinh = new (string Nhom, string Khoa, string GiaTri, string Kieu, string TenHienThi)[]
        {
            ("CHUNG", KhoaCauHinh.TenHeThong, "Nền tảng số dùng chung phục vụ hoạt động sáng kiến",
                "TEXT", "Tên hệ thống"),
            ("CHUNG", KhoaCauHinh.TenDonVi, "Ủy ban nhân dân thành phố", "TEXT", "Tên đơn vị"),
            ("CHUNG", KhoaCauHinh.DiaChi, "Số 1, đường Trung tâm, thành phố", "TEXT", "Địa chỉ"),
            ("CHUNG", KhoaCauHinh.EmailHoTro, "hotro@sangkien.gov.vn", "TEXT", "Email hỗ trợ"),
            ("CHUNG", KhoaCauHinh.DienThoaiHoTro, "0236.3888.999", "TEXT", "Điện thoại hỗ trợ"),
            ("GIAO_DIEN", KhoaCauHinh.MauChuDao, "#1677ff", "COLOR", "Màu chủ đạo"),

            // Kieu TEP: gia tri la id cua tep da tai len, man hinh cau hinh hien o tai anh.
            // De trong thi giao dien dung chu viet tat cua ten he thong nhu truoc.
            ("GIAO_DIEN", KhoaCauHinh.LogoId, "", "TEP", "Logo hiển thị trên đầu trang"),
            ("GIAO_DIEN", KhoaCauHinh.FaviconId, "", "TEP", "Biểu tượng trên thẻ trình duyệt"),
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

            // Hai cong tac duoi day duoc code doc tu truoc nhung KHONG duoc seed, nen khong hien
            // tren man hinh cau hinh: muon bat/tat phai sua thang co so du lieu. Dac ta yeu cau
            // moi quyet dinh nghiep vu deu chinh duoc tren giao dien quan tri.
            ("BAO_MAT", KhoaCauHinh.SoLanSaiCanCaptcha, "3", "NUMBER",
                "Số lần sai trước khi bắt nhập CAPTCHA (đặt 0 để tắt CAPTCHA)"),
            ("TICH_HOP", KhoaCauHinh.SsoTuDongTaoTaiKhoan, "false", "BOOLEAN",
                "Tự tạo tài khoản khi đăng nhập SSO lần đầu"),

            // Hai muc duoi day phuc vu yeu cau an toan thong tin cap do 2. Mac dinh TAT de khong
            // khoa quan tri vien ra ngoai ngay khi nang cap; don vi bat len khi da san sang.
            ("BAO_MAT", KhoaCauHinh.MfaBatBuocQuanTri, "false", "BOOLEAN",
                "Bắt buộc quản trị viên bật xác thực hai lớp"),
            ("BAO_MAT", KhoaCauHinh.IpChoPhepQuanTri, "", "TEXT",
                "IP/CIDR được phép dùng tài khoản quản trị (cách nhau bởi dấu phẩy, để trống = không giới hạn)"),
            ("BAO_MAT", KhoaCauHinh.ThoiGianKhoaTaiKhoanPhut, "15", "NUMBER", "Thời gian khóa (phút)")
        };

        /*
         * Doi chieu theo KHOA thay vi "bang rong thi nap".
         *
         * Truoc day chi nap khi bang con rong, nen mot khoa cau hinh moi khong bao gio toi duoc
         * he thong da cai dat: nang cap len ban moi thi o cau hinh tuong ung khong hien ra, va
         * cach duy nhat de bat/tat la sua thang co so du lieu.
         *
         * Khoa da co thi GIU NGUYEN GIA TRI: day la thiet lap van hanh cua don vi, ghi de moi lan
         * trien khai la lang le dat lai chinh sach cua ho ve mac dinh.
         */
        var khoaDaCo = await _db.CauHinhHeThong
            .Where(x => !x.DaXoa)
            .Select(x => x.Khoa)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var daCo = new HashSet<string>(khoaDaCo, StringComparer.Ordinal);

        var thuTu = 1;
        var soThem = 0;

        foreach (var (nhom, khoa, giaTri, kieu, ten) in cauHinh)
        {
            var viTri = thuTu++;

            if (daCo.Contains(khoa))
            {
                continue;
            }

            _db.CauHinhHeThong.Add(new CauHinhHeThong
            {
                Nhom = nhom,
                Khoa = khoa,
                GiaTri = giaTri,
                KieuDuLieu = kieu,
                TenHienThi = ten,
                ThuTu = viTri
            });

            soThem++;
        }

        if (soThem > 0)
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("Da bo sung {SoKhoa} khoa cau hinh con thieu.", soThem);
        }
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
    private const string KhoaPhienBanMenu = "PHIEN_BAN_MENU";

    /// <summary>Phien ban cau truc menu - tang len khi doi bo cuc de he thong dang chay nhan duoc.</summary>
    private const string PhienBanMenu = "7";

    /// <summary>Cac ma menu do seed tung sinh ra - dung de go bo cuc cu khi nang cap.</summary>
    private static readonly string[] MaMenuSeedCu =
    {
        "DASHBOARD", "SK_CUA_TOI", "SK_NOP_MOI", "TIEP_NHAN", "XU_LY", "DANH_GIA", "HOI_DONG",
        "QUYET_DINH", "TRA_CUU", "BAO_CAO", "BC_DAT", "BC_CHUA_DAT", "BC_DON_VI", "BC_KET_QUA",
        "BC_TUY_BIEN", "QUAN_TRI", "DANH_MUC", "DM_LINH_VUC", "DM_DOI_TUONG", "DM_DOT",
        "DM_LOAI_TG", "DM_BM_XUAT", "DM_BM_TK", "DM_QUYET_DINH", "QT_QUY_TRINH", "QT_TIEU_CHI",
        "QT_NGUOI_DUNG", "QT_DON_VI", "QT_VAI_TRO", "QT_CAU_HINH", "CH_HE_THONG", "CH_MENU",
        "CH_EMAIL_SMS", "CH_CHU_KY_SO", "CH_SANG_KIEN", "CH_TICH_HOP", "QT_NHAT_KY",
        "NK_HE_THONG", "NK_DANG_NHAP", "NK_LOI", "NK_DONG_BO", "QT_GUI_TIN",
        "NHOM_TONG_QUAN", "NHOM_SANG_KIEN", "NHOM_XU_LY",

        // Ma chi xuat hien tu phien ban menu 2. Thieu chung thi khi nang len phien ban 3,
        // cac muc nay khong bi go va nguoi dung thay hai muc trung ten canh nhau.
        "NHOM_QUAN_TRI", "NHOM_KHAC", "CONG_KHAI", "DOI_MAT_KHAU",

        // Ma chi xuat hien tu phien ban menu 4 tro di.
        "BAO_MAT_TAI_KHOAN", "QT_KHOA_API", "QT_DANH_MUC",

        // Ma chi xuat hien tu phien ban menu 5 tro di.
        "QT_LIEN_THONG",

        // Ma chi xuat hien tu phien ban menu 6 tro di.
        "QT_CHU_KY_SO"
    };

    /// <summary>
    /// Nap cau hinh menu theo dung ban thiet ke: 5 nhom, MOT cap, icon emoji.
    ///
    /// Danh muc con (danh muc dung chung, cau hinh, nhat ky, bao cao) khong nam o thanh dieu huong
    /// ma la TAB trong trang - thiet ke chon vay de thanh dieu huong khong dai qua man hinh.
    /// </summary>
    private async Task SeedMenuAsync(CancellationToken ct)
    {
        var phienBan = await _db.CauHinhHeThong.AsNoTracking()
            .Where(x => x.Khoa == KhoaPhienBanMenu)
            .Select(x => x.GiaTri)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var daCoMenu = await _db.CauHinhMenu.AsNoTracking().AnyAsync(ct).ConfigureAwait(false);

        if (daCoMenu && phienBan == PhienBanMenu)
        {
            return;
        }

        var menu = new List<CauHinhMenu>();
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

            menu.Add(m);
            return m;
        }

        var nhomTongQuan = Them("NHOM_TONG_QUAN", "Tổng quan", null, null, null);
        Them("DASHBOARD", "Bảng điều khiển", "/", "🏠", null, nhomTongQuan.Id);
        Them("BAO_CAO", "Thống kê tổng hợp", "/bao-cao/sang-kien-dat", "📊",
            MaQuyen.BaoCaoXem, nhomTongQuan.Id);

        var nhomSangKien = Them("NHOM_SANG_KIEN", "Sáng kiến", null, null, MaQuyen.SangKienXem);
        Them("SK_CUA_TOI", "Hồ sơ của tôi", "/sang-kien/cua-toi", "📁",
            MaQuyen.SangKienXem, nhomSangKien.Id);
        Them("SK_NOP_MOI", "Nộp hồ sơ mới", "/sang-kien/nop-moi", "📝",
            MaQuyen.SangKienThem, nhomSangKien.Id);
        Them("TRA_CUU", "Tra cứu", "/tra-cuu", "🔍", MaQuyen.SangKienXem, nhomSangKien.Id);

        var nhomXuLy = Them("NHOM_XU_LY", "Xử lý & đánh giá", null, null, MaQuyen.XuLyXem);
        Them("TIEP_NHAN", "Chờ tiếp nhận", "/tiep-nhan", "📥",
            MaQuyen.TiepNhanXem, nhomXuLy.Id);
        Them("XU_LY", "Việc cần xử lý", "/xu-ly", "⚙️", MaQuyen.XuLyXem, nhomXuLy.Id);
        Them("DANH_GIA", "Việc đánh giá", "/danh-gia", "✅",
            MaQuyen.DanhGiaXem, nhomXuLy.Id);
        Them("HOI_DONG", "Hội đồng sáng kiến", "/hoi-dong", "🧮",
            MaQuyen.HoiDongXem, nhomXuLy.Id);
        Them("QUYET_DINH", "Quyết định", "/quyet-dinh", "📜",
            MaQuyen.QuyetDinhXem, nhomXuLy.Id);

        var nhomQuanTri = Them("NHOM_QUAN_TRI", "Quản trị", null, null, MaQuyen.CauHinhXem);
        Them("QT_NGUOI_DUNG", "Người dùng", "/quan-tri/nguoi-dung", "👤",
            MaQuyen.NguoiDungXem, nhomQuanTri.Id);
        Them("QT_VAI_TRO", "Vai trò & phân quyền", "/quan-tri/vai-tro", "🛡️",
            MaQuyen.VaiTroXem, nhomQuanTri.Id);
        Them("QT_DON_VI", "Đơn vị, tổ chức", "/quan-tri/don-vi", "🏢",
            MaQuyen.DonViXem, nhomQuanTri.Id);
        Them("QT_DANH_MUC", "Danh mục", "/quan-tri/danh-muc/linh-vuc", "🗂️",
            MaQuyen.DanhMucXem, nhomQuanTri.Id);
        Them("QT_TIEU_CHI", "Bộ tiêu chí", "/quan-tri/tieu-chi", "📐",
            MaQuyen.TieuChiXem, nhomQuanTri.Id);
        Them("QT_QUY_TRINH", "Quy trình động", "/quan-tri/quy-trinh", "🔀",
            MaQuyen.QuyTrinhXem, nhomQuanTri.Id);
        Them("QT_CAU_HINH", "Cấu hình hệ thống", "/quan-tri/cau-hinh/he-thong", "🔧",
            MaQuyen.CauHinhXem, nhomQuanTri.Id);
        Them("QT_LIEN_THONG", "Liên thông hệ thống ngoài", "/quan-tri/lien-thong", "🔗",
            MaQuyen.TichHopCauHinh, nhomQuanTri.Id);
        Them("QT_CHU_KY_SO", "Chữ ký số", "/quan-tri/chu-ky-so", "✍️",
            MaQuyen.CauHinhXem, nhomQuanTri.Id);
        Them("QT_KHOA_API", "Khoá API hệ thống ngoài", "/quan-tri/khoa-api", "🔑",
            MaQuyen.TichHopCauHinh, nhomQuanTri.Id);
        Them("QT_NHAT_KY", "Nhật ký hệ thống", "/quan-tri/nhat-ky/he-thong", "🕐",
            MaQuyen.NhatKyXem, nhomQuanTri.Id);
        Them("QT_NHAT_KY_LOI", "Nhật ký lỗi", "/quan-tri/nhat-ky-loi", "🐞",
            MaQuyen.NhatKyXem, nhomQuanTri.Id);

        var nhomKhac = Them("NHOM_KHAC", "Khác", null, null, null);
        Them("CONG_KHAI", "Cổng tra cứu công khai", "/cong-khai/tra-cuu", "🌐",
            null, nhomKhac.Id);
        Them("DOI_MAT_KHAU", "Đổi mật khẩu", "/doi-mat-khau", "🔒", null, nhomKhac.Id);
        Them("BAO_MAT_TAI_KHOAN", "Bảo mật tài khoản", "/bao-mat-tai-khoan", "🔐",
            null, nhomKhac.Id);

        // Doi bo cuc menu -> go bo menu CU do chinh seed tao ra roi dung lai theo cau truc moi.
        //
        // Tap ma can go = danh sach cua cac bo cuc DA NGHI HUU (MaMenuSeedCu) HOP voi ma cua
        // chinh bo cuc dang dung. Ve sau chi can them muc vao ham nay, khong phai nho cap nhat
        // MaMenuSeedCu nua — quen cap nhat la nguoi dung thay hai muc trung ten canh nhau, va
        // loi do chi lo ra tren he thong da chay chu khong lo ra khi cai moi.
        //
        // Muc quan tri vien tu them co ma khac nen khong bi dung toi.
        if (daCoMenu)
        {
            var maCanGo = MaMenuSeedCu.Concat(menu.Select(x => x.Ma)).ToHashSet(StringComparer.Ordinal);

            var cu = await _db.CauHinhMenu
                .Where(x => maCanGo.Contains(x.Ma))
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (var m in cu)
            {
                m.DaXoa = true;
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("Da go {SoMuc} muc menu cua bo cuc cu.", cu.Count);
        }

        _db.CauHinhMenu.AddRange(menu);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await GhiPhienBanMenuAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Da nap {SoMuc} muc menu theo bo cuc moi.", menu.Count);
    }

    private async Task GhiPhienBanMenuAsync(CancellationToken ct)
    {
        var muc = await _db.CauHinhHeThong
            .FirstOrDefaultAsync(x => x.Khoa == KhoaPhienBanMenu, ct)
            .ConfigureAwait(false);

        if (muc is null)
        {
            _db.CauHinhHeThong.Add(new CauHinhHeThong
            {
                Id = Guid.NewGuid(),
                Nhom = "HE_THONG",
                Khoa = KhoaPhienBanMenu,
                GiaTri = PhienBanMenu,
                KieuDuLieu = "TEXT",
                TenHienThi = "Phiên bản bố cục menu",
                MoTa = "Dùng nội bộ để biết có phải dựng lại menu khi nâng cấp hay không.",
                ChoPhepSua = false
            });
        }
        else
        {
            muc.GiaTri = PhienBanMenu;
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

}
