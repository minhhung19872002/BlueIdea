using BlueIdea.Domain.Chung;
using BlueIdea.Domain.SangKien;
using BlueIdea.Shared.TiengViet;
using BlueIdea.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlueIdea.Infrastructure.Seed;

public sealed partial class DuLieuMau
{
    /// <summary>Seed co dinh de du lieu mau tai lap duoc giua cac lan chay.</summary>
    private const int HatGiongNgauNhien = 20260817;

    /// <summary>
    /// Sinh 40 ho so o du moi trang thai, trong do co MOT CAP CO Y TRUNG ~60%
    /// de demo chuc nang kiem tra trung lap (yeu cau Muc 10 dac ta).
    /// </summary>
    private async Task SeedHoSoSangKienAsync(CancellationToken ct)
    {
        if (await _db.SangKien.AnyAsync(ct).ConfigureAwait(false))
        {
            return;
        }

        var random = new Random(HatGiongNgauNhien);

        var dot2026 = await _db.DotDeNghi.FirstAsync(x => x.Ma == "DOT-2026", ct).ConfigureAwait(false);
        var dot2025 = await _db.DotDeNghi.FirstAsync(x => x.Ma == "DOT-2025", ct).ConfigureAwait(false);
        var linhVuc = await _db.LinhVuc.ToListAsync(ct).ConfigureAwait(false);
        var doiTuong = await _db.DoiTuong.ToListAsync(ct).ConfigureAwait(false);
        var loaiTacGia = await _db.LoaiTacGia.ToListAsync(ct).ConfigureAwait(false);
        var tacGias = await _db.NguoiDung
            .Where(x => x.TenDangNhap.StartsWith("gv.") || x.TenDangNhap.StartsWith("cb.")
                                                       || x.TenDangNhap.StartsWith("bs.")
                                                       || x.TenDangNhap.StartsWith("yt."))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var quyTrinh = await _db.QuyTrinh
            .Include(q => q.DanhSachBuoc).ThenInclude(b => b.TacNhan)
            .Include(q => q.DanhSachBuoc).ThenInclude(b => b.TruongHop)
            .Include(q => q.DanhSachBuoc).ThenInclude(b => b.TrangThai)
            .Include(q => q.ThanhPhanHoSo)
            .Include(q => q.ChucNangBoSung)
            .Include(q => q.TrangThaiToanCuc)
            .FirstAsync(ct)
            .ConfigureAwait(false);

        var boChuyenDoi = new BoChuyenDoiSnapshotQuyTrinh();
        var snapshot = boChuyenDoi.TaoSnapshot(quyTrinh);
        var buocs = quyTrinh.DanhSachBuoc.OrderBy(b => b.ThuTu).ToList();
        var trangThaiDangXuLy = quyTrinh.TrangThaiToanCuc.First(t => t.Ma == "DANG_XU_LY");
        var trangThaiDaDuyet = quyTrinh.TrangThaiToanCuc.First(t => t.Ma == "DA_PHE_DUYET");

        var mauDeTai = TaoDanhSachDeTai();
        var stt = 1;
        var danhSachHoSo = new List<HoSoSangKien>();

        HoSoSangKien Tao(
            string ten, string noiDung, string trangThaiTong, int chiSoBuoc,
            decimal? tongDiem, string? ketQua, bool congKhai, int nam)
        {
            var tacGia = tacGias[random.Next(tacGias.Count)];
            var dot = nam == 2026 ? dot2026 : dot2025;
            var lv = linhVuc[random.Next(linhVuc.Count)];

            var hoSo = new HoSoSangKien
            {
                Id = Guid.NewGuid(),
                MaHoSo = $"SK-{nam}-{stt++:0000}",
                TenSangKien = ten,
                TenKhongDau = VanBanTiengViet.TaoKhongDau(ten),
                DotDeNghiId = dot.Id,
                LinhVucId = lv.Id,
                DoiTuongId = doiTuong[random.Next(doiTuong.Count)].Id,
                LoaiTacGiaId = loaiTacGia[0].Id,
                DonViId = tacGia.DonViId,
                QuyTrinhId = quyTrinh.Id,
                TrangThaiTong = trangThaiTong,
                MoTaGiaiPhap = noiDung,
                // Cac truong phu duoc dien tu chinh noi dung de moi ho so co van phong rieng.
                // Neu dung chung mot doan mau cho tat ca ho so, thuat toan so khop se bao
                // ty le tuong dong cao gia tao giua cac ho so hoan toan khong lien quan.
                TinhTrangTruocKhiApDung = TrichDoan(noiDung, 0),
                NoiDungGiaiPhap = noiDung,
                TinhMoi = TrichDoan(noiDung, 1),
                KhaNangApDung = TrichDoan(noiDung, 2),
                PhamViApDung = TrichDoan(noiDung, 3),
                HieuQuaKinhTe = TrichDoan(noiDung, 4),
                GiaTriLamLoiUocTinh = random.Next(10, 500) * 1_000_000m,
                HieuQuaXaHoi = TrichDoan(noiDung, 5),
                ThoiGianApDungTu = new DateOnly(nam, 1, 1),
                ThoiGianApDungDen = new DateOnly(nam, 12, 31),
                CongKhai = congKhai,
                TongDiem = tongDiem,
                DiemTrungBinh = tongDiem,
                KetQua = ketQua,
                SoLuotXem = random.Next(0, 200)
            };

            hoSo.DanhSachTacGia.Add(new SangKienTacGia
            {
                SangKienId = hoSo.Id,
                NguoiDungId = tacGia.Id,
                HoTen = tacGia.HoTen,
                ChucVu = tacGia.ChucVu,
                Email = tacGia.Email,
                DienThoai = tacGia.DienThoai,
                TyLeDongGop = 100m,
                LaTacGiaChinh = true,
                ThuTu = 1
            });

            if (trangThaiTong != TrangThaiTongHoSo.Nhap)
            {
                hoSo.NgayNop = new DateTimeOffset(nam, random.Next(1, 10), random.Next(1, 28),
                    9, 0, 0, TimeSpan.FromHours(7));
                hoSo.QuyTrinhSnapshot = snapshot;

                var buoc = buocs[Math.Clamp(chiSoBuoc, 0, buocs.Count - 1)];
                var ketThuc = trangThaiTong is TrangThaiTongHoSo.DaPheDuyet
                    or TrangThaiTongHoSo.KhongDat or TrangThaiTongHoSo.DaRut;

                hoSo.BuocHienTaiId = ketThuc ? null : buoc.Id;
                hoSo.TrangThaiHienTaiId = ketThuc ? trangThaiDaDuyet.Id : trangThaiDangXuLy.Id;
                hoSo.HanXuLyHienTai = ketThuc
                    ? null
                    : hoSo.NgayNop.Value.AddDays(buoc.SoNgayXuLy);

                if (ketThuc)
                {
                    hoSo.NgayHoanThanh = hoSo.NgayNop.Value.AddDays(random.Next(20, 60));
                    if (trangThaiTong == TrangThaiTongHoSo.DaPheDuyet)
                    {
                        hoSo.NgayCongNhan = DateOnly.FromDateTime(hoSo.NgayHoanThanh.Value.DateTime);
                    }
                }

                // Lich su xu ly tuong ung cac buoc da di qua.
                for (var i = 0; i <= Math.Min(chiSoBuoc, buocs.Count - 1); i++)
                {
                    var b = buocs[i];
                    var daXong = i < chiSoBuoc || ketThuc;

                    hoSo.LichSuXuLy.Add(new SangKienXuLy
                    {
                        SangKienId = hoSo.Id,
                        BuocId = b.Id,
                        TenBuocSnapshot = b.Ten,
                        TrangThaiId = trangThaiDangXuLy.Id,
                        NguoiXuLyId = daXong ? tacGia.Id : null,
                        YKien = daXong ? "Hồ sơ đầy đủ, đủ điều kiện chuyển bước tiếp theo." : null,
                        ThoiGianNhan = hoSo.NgayNop.Value.AddDays(i * 2),
                        HanXuLy = hoSo.NgayNop.Value.AddDays(i * 2 + b.SoNgayXuLy),
                        ThoiGianXuLy = daXong ? hoSo.NgayNop.Value.AddDays(i * 2 + 1) : null,
                        SoNgayXuLy = daXong ? 1m : null,
                        ThuTu = i + 1
                    });
                }
            }

            danhSachHoSo.Add(hoSo);
            return hoSo;
        }

        // --- Cap ho so CO Y TRUNG ~60% de demo AI kiem tra trung lap ---
        var (noiDungGoc, noiDungTrung) = TaoCapNoiDungTrungLap();

        Tao("Số hóa quy trình tiếp nhận và trả kết quả hồ sơ một cửa",
            noiDungGoc, TrangThaiTongHoSo.DaPheDuyet, 5, 86m, KetQuaXetDuyetGiaTri.Dat, true, 2025);

        Tao("Ứng dụng phần mềm quản lý hồ sơ một cửa tại bộ phận tiếp nhận",
            noiDungTrung, TrangThaiTongHoSo.DangXuLy, 1, null, null, false, 2026);

        // --- Cac ho so con lai trai deu tren cac trang thai ---
        var kichBan = new (string TrangThai, int ChiSoBuoc, decimal? Diem, string? KetQua, bool CongKhai)[]
        {
            (TrangThaiTongHoSo.Nhap, 0, null, null, false),
            (TrangThaiTongHoSo.DaNop, 0, null, null, false),
            (TrangThaiTongHoSo.DangXuLy, 1, null, null, false),
            (TrangThaiTongHoSo.DangXuLy, 2, null, null, false),
            (TrangThaiTongHoSo.DangXuLy, 3, 72m, null, false),
            (TrangThaiTongHoSo.YeuCauBoSung, 0, null, null, false),
            (TrangThaiTongHoSo.DaPheDuyet, 5, 84m, KetQuaXetDuyetGiaTri.Dat, true),
            (TrangThaiTongHoSo.DaPheDuyet, 5, 91m, KetQuaXetDuyetGiaTri.Dat, true),
            (TrangThaiTongHoSo.KhongDat, 4, 42m, KetQuaXetDuyetGiaTri.KhongDat, false),
            (TrangThaiTongHoSo.DaRut, 1, null, null, false)
        };

        var chiSoDeTai = 0;
        for (var i = 0; i < 38 && chiSoDeTai < mauDeTai.Count; i++)
        {
            var kb = kichBan[i % kichBan.Length];
            var (ten, noiDung) = mauDeTai[chiSoDeTai++];
            var nam = i % 3 == 0 ? 2025 : 2026;

            Tao(ten, noiDung, kb.TrangThai, kb.ChiSoBuoc, kb.Diem, kb.KetQua, kb.CongKhai, nam);
        }

        _db.SangKien.AddRange(danhSachHoSo);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Đã tạo {SoLuong} hồ sơ sáng kiến mẫu.", danhSachHoSo.Count);
    }

    /// <summary>
    /// Lay mot doan cua noi dung theo chi muc (xoay vong) de sinh cac truong phu
    /// mang van phong rieng cua tung ho so.
    /// </summary>
    private static string TrichDoan(string noiDung, int chiMuc)
    {
        var cau = noiDung
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(c => c.Length > 20)
            .ToList();

        if (cau.Count == 0)
        {
            return noiDung;
        }

        var dau = chiMuc % cau.Count;
        var lay = cau.Skip(dau).Take(2).Concat(cau.Take(Math.Max(0, 2 - (cau.Count - dau))));

        return string.Join(". ", lay) + ".";
    }

    /// <summary>
    /// Sinh cap noi dung co ty le trung khoang 60%: giu nguyen 3/5 doan, viet lai 2/5 doan.
    /// </summary>
    private static (string Goc, string Trung) TaoCapNoiDungTrungLap()
    {
        const string doan1 =
            "Bộ phận tiếp nhận và trả kết quả của đơn vị mỗi ngày tiếp nhận khoảng một trăm hồ sơ "
            + "thủ tục hành chính thuộc nhiều lĩnh vực khác nhau. Trước khi áp dụng giải pháp, "
            + "cán bộ phải ghi chép thủ công vào sổ giấy, tra cứu một hồ sơ trung bình mất mười lăm phút "
            + "và thường xuyên xảy ra tình trạng thất lạc hoặc trả kết quả trễ hẹn.";

        const string doan2 =
            "Giải pháp xây dựng phần mềm quản lý hồ sơ tập trung trên nền tảng web, cho phép quét mã vạch "
            + "để tra cứu nhanh, tự động nhắc hạn xử lý trước hai ngày làm việc và thống kê tiến độ "
            + "theo từng cán bộ. Toàn bộ dữ liệu được lưu trữ tập trung, phân quyền theo chức danh "
            + "và ghi nhật ký đầy đủ mọi thao tác thay đổi.";

        const string doan3 =
            "Sau sáu tháng triển khai, thời gian tra cứu một hồ sơ giảm từ mười lăm phút xuống còn hai phút, "
            + "tỷ lệ hồ sơ trả đúng hạn tăng từ tám mươi hai phần trăm lên chín mươi tám phần trăm. "
            + "Chi phí in ấn và văn phòng phẩm giảm khoảng bốn mươi phần trăm so với cùng kỳ năm trước.";

        const string doan4Goc =
            "Đơn vị đã tổ chức tập huấn cho toàn bộ cán bộ tiếp nhận, ban hành quy chế sử dụng phần mềm "
            + "và phân công đầu mối hỗ trợ kỹ thuật thường trực. Giải pháp đã được nhân rộng "
            + "cho ba đơn vị trực thuộc trong quý tiếp theo.";

        const string doan5Goc =
            "Hướng phát triển tiếp theo là tích hợp với Cổng dịch vụ công quốc gia, bổ sung chức năng "
            + "thanh toán trực tuyến và mở rộng kênh tra cứu qua ứng dụng di động cho người dân.";

        const string doan4Trung =
            "Ngoài phần mềm, đơn vị còn triển khai mô hình tổ công tác lưu động hỗ trợ người cao tuổi "
            + "và người khuyết tật hoàn thiện thủ tục ngay tại khu dân cư, kết hợp tuyên truyền "
            + "qua hệ thống loa phát thanh phường.";

        const string doan5Trung =
            "Trong thời gian tới, đơn vị dự kiến xây dựng bộ chỉ số đánh giá chất lượng phục vụ "
            + "theo thời gian thực và công khai kết quả trên bảng điện tử đặt tại trụ sở.";

        var goc = string.Join("\n\n", doan1, doan2, doan3, doan4Goc, doan5Goc);
        var trung = string.Join("\n\n", doan1, doan2, doan3, doan4Trung, doan5Trung);

        return (goc, trung);
    }

    private static List<(string Ten, string NoiDung)> TaoDanhSachDeTai()
    {
        var deTai = new (string Ten, string TomTat)[]
        {
            ("Ứng dụng bản đồ số trong quản lý trật tự xây dựng đô thị",
                "quản lý vị trí công trình xây dựng bằng bản đồ số, cảnh báo vi phạm theo thời gian thực"),
            ("Xây dựng thư viện học liệu điện tử dùng chung cho khối tiểu học",
                "số hóa bài giảng, chia sẻ học liệu giữa các trường và hỗ trợ học sinh tự học tại nhà"),
            ("Mô hình lớp học đảo ngược trong dạy môn Toán trung học cơ sở",
                "học sinh xem bài giảng trước tại nhà, thời gian trên lớp dành cho thực hành và thảo luận"),
            ("Giải pháp quản lý hồ sơ sức khỏe điện tử tại trạm y tế phường",
                "lập hồ sơ sức khỏe cá nhân điện tử, nhắc lịch tiêm chủng và khám định kỳ tự động"),
            ("Ứng dụng công nghệ thông tin trong quản lý thuốc và vật tư y tế",
                "kiểm soát tồn kho theo thời gian thực, cảnh báo thuốc sắp hết hạn sử dụng"),
            ("Cải tiến quy trình quản lý văn bản đi đến bằng chữ ký số",
                "loại bỏ văn bản giấy trong luân chuyển nội bộ, rút ngắn thời gian phê duyệt"),
            ("Xây dựng hệ thống tổng đài tiếp nhận phản ánh kiến nghị của người dân",
                "tiếp nhận phản ánh đa kênh, phân loại tự động và theo dõi tiến độ xử lý"),
            ("Mô hình phân loại rác tại nguồn gắn với tích điểm đổi quà",
                "khuyến khích hộ dân phân loại rác thông qua ứng dụng tích điểm"),
            ("Giải pháp tưới tiêu tự động tiết kiệm nước cho vùng rau chuyên canh",
                "điều khiển tưới theo độ ẩm đất đo bằng cảm biến, tiết kiệm nước và nhân công"),
            ("Ứng dụng nhật ký điện tử trong quản lý an toàn thực phẩm chợ dân sinh",
                "ghi nhận nguồn gốc thực phẩm bằng mã QR, truy xuất nhanh khi có sự cố"),
            ("Số hóa hồ sơ cán bộ công chức viên chức toàn thành phố",
                "chuẩn hóa dữ liệu nhân sự, khai thác báo cáo tự động phục vụ công tác cán bộ"),
            ("Xây dựng phần mềm quản lý tài sản công tại các đơn vị sự nghiệp",
                "theo dõi vòng đời tài sản, tự động lập biên bản kiểm kê hằng năm"),
            ("Mô hình thư viện xanh và góc đọc mở tại trường tiểu học",
                "tăng thời lượng đọc sách của học sinh thông qua không gian đọc mở"),
            ("Giải pháp hỗ trợ học sinh khuyết tật hòa nhập bằng công cụ số",
                "sử dụng phần mềm chuyển văn bản thành giọng nói và bài tập tương tác"),
            ("Ứng dụng trí tuệ nhân tạo nội bộ trong tổng hợp báo cáo thống kê",
                "tự động tổng hợp số liệu từ nhiều nguồn, giảm thời gian lập báo cáo định kỳ"),
            ("Cải tiến công tác tiếp công dân theo mô hình một cửa liên thông",
                "gộp đầu mối tiếp nhận, giảm số lần người dân phải đi lại"),
            ("Xây dựng cơ sở dữ liệu di tích lịch sử văn hóa trên địa bàn",
                "số hóa hồ sơ di tích kèm ảnh và mô hình ba chiều phục vụ quảng bá"),
            ("Mô hình tổ công nghệ số cộng đồng hỗ trợ người dân dùng dịch vụ công",
                "đội ngũ tình nguyện viên hướng dẫn người dân nộp hồ sơ trực tuyến"),
            ("Giải pháp giám sát chất lượng nước sinh hoạt bằng cảm biến",
                "đo các chỉ tiêu cơ bản liên tục và cảnh báo khi vượt ngưỡng cho phép"),
            ("Ứng dụng phần mềm quản lý bếp ăn bán trú tại trường mầm non",
                "tính khẩu phần dinh dưỡng tự động, công khai thực đơn cho phụ huynh"),
            ("Xây dựng quy trình xử lý đơn thư khiếu nại theo hướng rút gọn",
                "chuẩn hóa biểu mẫu, rút ngắn thời gian xử lý so với quy định"),
            ("Mô hình camera giám sát an ninh kết hợp cảnh báo cộng đồng",
                "chia sẻ cảnh báo an ninh qua nhóm liên lạc của tổ dân phố"),
            ("Giải pháp quản lý dự án đầu tư công bằng bảng điều khiển trực quan",
                "theo dõi tiến độ và giải ngân theo thời gian thực trên một màn hình"),
            ("Ứng dụng học liệu song ngữ hỗ trợ học sinh dân tộc thiểu số",
                "biên soạn học liệu song ngữ và bài tập tương tác phù hợp năng lực"),
            ("Cải tiến công tác thi đua khen thưởng bằng chấm điểm trực tuyến",
                "chấm điểm thi đua trực tuyến, công khai kết quả và giảm hồ sơ giấy"),
            ("Xây dựng hệ thống nhắc lịch tiêm chủng qua tin nhắn cho phụ huynh",
                "tự động gửi tin nhắn nhắc lịch, tăng tỷ lệ tiêm chủng đúng hạn"),
            ("Mô hình vườn thuốc nam mẫu tại trạm y tế phục vụ khám chữa bệnh",
                "trồng và sử dụng dược liệu tại chỗ, giảm chi phí điều trị"),
            ("Giải pháp cải tạo hệ thống chiếu sáng công cộng tiết kiệm điện",
                "thay đèn LED kết hợp điều khiển theo lịch, giảm điện năng tiêu thụ"),
            ("Ứng dụng khảo sát hài lòng người dân bằng máy tính bảng tại quầy",
                "thu thập đánh giá ngay sau khi trả kết quả, tổng hợp báo cáo tự động"),
            ("Xây dựng quy trình sao lưu và phục hồi dữ liệu cho hệ thống dùng chung",
                "sao lưu tự động hằng ngày, có kịch bản phục hồi đã kiểm thử"),
            ("Mô hình câu lạc bộ khởi nghiệp học sinh trung học phổ thông",
                "hướng dẫn học sinh xây dựng dự án khởi nghiệp và trình bày trước hội đồng"),
            ("Giải pháp quản lý phương tiện công bằng thiết bị định vị",
                "theo dõi hành trình, kiểm soát nhiên liệu và lịch bảo dưỡng"),
            ("Ứng dụng biểu mẫu điện tử trong thu thập số liệu thống kê cơ sở",
                "thay biểu mẫu giấy bằng biểu mẫu điện tử, giảm sai sót nhập liệu"),
            ("Cải tiến phương pháp dạy học môn Ngữ văn theo hướng phát triển năng lực",
                "tổ chức dự án học tập gắn với thực tiễn địa phương"),
            ("Xây dựng phần mềm quản lý hoạt động của tổ dân phố",
                "quản lý danh sách hộ dân, thu chi quỹ và thông báo hoạt động"),
            ("Mô hình chăm sóc sức khỏe người cao tuổi tại cộng đồng",
                "khám sàng lọc định kỳ và theo dõi bệnh mạn tính tại nhà"),
            ("Giải pháp giảm thời gian chờ khám bệnh bằng đặt lịch trực tuyến",
                "người bệnh đặt lịch trước, phân bổ khung giờ khám hợp lý"),
            ("Ứng dụng công nghệ trong quản lý và quảng bá sản phẩm OCOP",
                "xây dựng gian hàng số và truy xuất nguồn gốc sản phẩm địa phương")
        };

        return deTai.Select(d => (
            d.Ten,
            $"Giải pháp {d.TomTat}. "
            + "Nội dung giải pháp được xây dựng trên cơ sở khảo sát thực trạng tại đơn vị, "
            + "phân tích các điểm nghẽn trong quy trình hiện hành và đề xuất phương án cải tiến "
            + "phù hợp với nguồn lực sẵn có. Quá trình triển khai được chia thành ba giai đoạn: "
            + "chuẩn bị, thí điểm và nhân rộng, kèm theo bộ chỉ số đo lường hiệu quả cụ thể. "
            + "Kết quả bước đầu cho thấy giải pháp giúp rút ngắn thời gian xử lý công việc, "
            + "giảm chi phí vận hành và nâng cao mức độ hài lòng của người thụ hưởng.")).ToList();
    }
}
