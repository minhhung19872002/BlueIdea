using BlueIdea.Domain.Chung;
using BlueIdea.Domain.DanhMuc;
using BlueIdea.Domain.HoiDong;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Domain.QuyTrinh;
using BlueIdea.Domain.SangKien;
using BlueIdea.Domain.TieuChi;
using BlueIdea.Shared.TiengViet;
using Microsoft.EntityFrameworkCore;
using EntityQuyTrinh = BlueIdea.Domain.QuyTrinh.QuyTrinh;

namespace BlueIdea.Infrastructure.Seed;

public sealed partial class DuLieuMau
{
    // ------------------------------------------------------------------------------------
    // Don vi (cay 3 cap)
    // ------------------------------------------------------------------------------------

    private async Task<IReadOnlyList<DonVi>> SeedDonViAsync(CancellationToken ct)
    {
        if (await _db.DonVi.AnyAsync(ct).ConfigureAwait(false))
        {
            return await _db.DonVi.ToListAsync(ct).ConfigureAwait(false);
        }

        var danhSach = new List<DonVi>();

        DonVi Them(string ma, string ten, string loai, DonVi? cha, bool laPheDuyet = false)
        {
            var dv = new DonVi
            {
                Id = Guid.NewGuid(),
                Ma = ma,
                Ten = ten,
                TenKhongDau = VanBanTiengViet.TaoKhongDau(ten),
                Loai = loai,
                DonViChaId = cha?.Id,
                Cap = (cha?.Cap ?? 0) + 1,
                Path = $"{cha?.Path ?? "/"}{VanBanTiengViet.TaoSlug(ma)}/",
                LaDonViPheDuyet = laPheDuyet,
                CapPheDuyet = laPheDuyet ? CapXetDuyet.CoSo : null,
                ThuTu = danhSach.Count + 1
            };

            danhSach.Add(dv);
            return dv;
        }

        var ubnd = Them("UBND_TP", "Ủy ban nhân dân thành phố", LoaiDonVi.Ubnd, null, laPheDuyet: true);

        var phongNoiVu = Them("PHONG_NOI_VU", "Phòng Nội vụ", LoaiDonVi.PhongBan, ubnd, laPheDuyet: true);
        var phongGd = Them("PHONG_GDDT", "Phòng Giáo dục và Đào tạo", LoaiDonVi.PhongBan, ubnd, true);
        var phongYte = Them("PHONG_YTE", "Phòng Y tế", LoaiDonVi.PhongBan, ubnd, true);
        var phongVhtt = Them("PHONG_VHTT", "Phòng Văn hóa - Thông tin", LoaiDonVi.PhongBan, ubnd, true);
        var phongKt = Them("PHONG_KTHT", "Phòng Kinh tế - Hạ tầng", LoaiDonVi.PhongBan, ubnd, true);
        var vanPhong = Them("VAN_PHONG", "Văn phòng HĐND và UBND", LoaiDonVi.PhongBan, ubnd);

        var phuong1 = Them("PHUONG_01", "Phường Trung tâm", LoaiDonVi.DonVi, ubnd, laPheDuyet: true);
        var phuong2 = Them("PHUONG_02", "Phường Hải Châu", LoaiDonVi.DonVi, ubnd, laPheDuyet: true);
        var phuong3 = Them("PHUONG_03", "Phường An Khê", LoaiDonVi.DonVi, ubnd, laPheDuyet: true);

        Them("TH_LE_LOI", "Trường Tiểu học Lê Lợi", LoaiDonVi.DonVi, phongGd);
        Them("THCS_QUANG_TRUNG", "Trường THCS Quang Trung", LoaiDonVi.DonVi, phongGd);
        Them("THPT_TRAN_PHU", "Trường THPT Trần Phú", LoaiDonVi.DonVi, phongGd);
        Them("MN_HOA_SEN", "Trường Mầm non Hoa Sen", LoaiDonVi.DonVi, phongGd);

        Them("TTYT_TP", "Trung tâm Y tế thành phố", LoaiDonVi.DonVi, phongYte);
        Them("TRAM_YT_P1", "Trạm Y tế Phường Trung tâm", LoaiDonVi.DonVi, phongYte);

        Them("TT_VHTT", "Trung tâm Văn hóa - Thể thao", LoaiDonVi.DonVi, phongVhtt);
        Them("BQL_DUAN", "Ban Quản lý dự án", LoaiDonVi.DonVi, phongKt);

        Them("TO_MOT_CUA_P1", "Bộ phận Một cửa Phường Trung tâm", LoaiDonVi.PhongBan, phuong1);
        Them("TO_MOT_CUA_P2", "Bộ phận Một cửa Phường Hải Châu", LoaiDonVi.PhongBan, phuong2);
        Them("TO_MOT_CUA_P3", "Bộ phận Một cửa Phường An Khê", LoaiDonVi.PhongBan, phuong3);

        Them("HD_SANG_KIEN", "Hội đồng sáng kiến cấp cơ sở", LoaiDonVi.HoiDong, ubnd);

        _ = phongNoiVu;
        _ = vanPhong;

        _db.DonVi.AddRange(danhSach);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return danhSach;
    }

    // ------------------------------------------------------------------------------------
    // Danh muc co ban
    // ------------------------------------------------------------------------------------

    private async Task SeedDanhMucAsync(CancellationToken ct)
    {
        if (!await _db.LinhVuc.AnyAsync(ct).ConfigureAwait(false))
        {
            var linhVuc = new (string Ma, string Ten)[]
            {
                ("GIAO_DUC", "Giáo dục và Đào tạo"),
                ("Y_TE", "Y tế"),
                ("CNTT", "Công nghệ thông tin"),
                ("QLHC", "Quản lý hành chính"),
                ("NONG_NGHIEP", "Nông nghiệp"),
                ("MOI_TRUONG", "Môi trường"),
                ("VAN_HOA", "Văn hóa - Xã hội"),
                ("KHAC", "Lĩnh vực khác")
            };

            var thuTu = 1;
            foreach (var (ma, ten) in linhVuc)
            {
                _db.LinhVuc.Add(new LinhVuc
                {
                    Ma = ma,
                    Ten = ten,
                    TenKhongDau = VanBanTiengViet.TaoKhongDau(ten),
                    ThuTu = thuTu++
                });
            }
        }

        if (!await _db.DoiTuong.AnyAsync(ct).ConfigureAwait(false))
        {
            var doiTuong = new (string Ma, string Ten)[]
            {
                ("CA_NHAN", "Cá nhân"),
                ("TAP_THE", "Tập thể"),
                ("HOC_SINH", "Học sinh, sinh viên"),
                ("CAN_BO", "Cán bộ, công chức, viên chức"),
                ("NGUOI_DAN", "Người dân"),
                ("DOANH_NGHIEP", "Doanh nghiệp")
            };

            var thuTu = 1;
            foreach (var (ma, ten) in doiTuong)
            {
                _db.DoiTuong.Add(new DoiTuong
                {
                    Ma = ma,
                    Ten = ten,
                    TenKhongDau = VanBanTiengViet.TaoKhongDau(ten),
                    ThuTu = thuTu++
                });
            }
        }

        if (!await _db.LoaiTacGia.AnyAsync(ct).ConfigureAwait(false))
        {
            var loai = new (string Ma, string Ten, bool Nhieu, int ToiDa)[]
            {
                ("CA_NHAN", "Cá nhân", false, 1),
                ("NHOM_TAC_GIA", "Nhóm tác giả", true, 5),
                ("DON_VI", "Đơn vị", true, 10)
            };

            var thuTu = 1;
            foreach (var (ma, ten, nhieu, toiDa) in loai)
            {
                _db.LoaiTacGia.Add(new LoaiTacGia
                {
                    Ma = ma,
                    Ten = ten,
                    TenKhongDau = VanBanTiengViet.TaoKhongDau(ten),
                    ChoPhepNhieuTacGia = nhieu,
                    SoTacGiaToiDa = toiDa,
                    ThuTu = thuTu++
                });
            }
        }

        if (!await _db.BieuMauXuat.AnyAsync(ct).ConfigureAwait(false))
        {
            var bieuMau = new (string Ma, string Ten, string Loai)[]
            {
                ("BM_PHIEU_TIEP_NHAN", "Phiếu tiếp nhận hồ sơ sáng kiến", LoaiBieuMau.PhieuTiepNhan),
                ("BM_PHIEU_DANH_GIA", "Phiếu đánh giá sáng kiến", LoaiBieuMau.PhieuDanhGia),
                ("BM_BIEN_BAN_HOP", "Biên bản họp hội đồng sáng kiến", LoaiBieuMau.BienBanHop),
                ("BM_QUYET_DINH", "Quyết định công nhận sáng kiến", LoaiBieuMau.QuyetDinh),
                ("BM_TONG_HOP", "Bảng tổng hợp kết quả xét sáng kiến", LoaiBieuMau.TongHop)
            };

            var thuTu = 1;
            foreach (var (ma, ten, loai) in bieuMau)
            {
                _db.BieuMauXuat.Add(new BieuMauXuat
                {
                    Ma = ma,
                    Ten = ten,
                    TenKhongDau = VanBanTiengViet.TaoKhongDau(ten),
                    Loai = loai,
                    DinhDang = "DOCX",
                    ThuTu = thuTu++,
                    CauHinhTruong = new List<CauHinhTruongBieuMau>
                    {
                        new() { Placeholder = "{{MA_HO_SO}}", Nguon = "sang_kien.ma_ho_so", Kieu = "text" },
                        new() { Placeholder = "{{TEN_SANG_KIEN}}", Nguon = "sang_kien.ten_sang_kien", Kieu = "text" },
                        new()
                        {
                            Placeholder = "{{DS_TAC_GIA}}",
                            Nguon = "sang_kien_tac_gia[]",
                            Kieu = "table",
                            Cot = new List<string> { "ho_ten", "chuc_vu", "don_vi_cong_tac", "ty_le_dong_gop" }
                        },
                        new() { Placeholder = "{{NGAY_NOP}}", Nguon = "sang_kien.ngay_nop", Kieu = "date" },
                        new() { Placeholder = "{{TONG_DIEM}}", Nguon = "sang_kien.tong_diem", Kieu = "number" }
                    }
                });
            }
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // ------------------------------------------------------------------------------------
    // Nguoi dung
    // ------------------------------------------------------------------------------------

    private async Task<IReadOnlyList<NguoiDung>> SeedNguoiDungAsync(
        IReadOnlyList<DonVi> donVi, CancellationToken ct)
    {
        if (await _db.NguoiDung.AnyAsync(ct).ConfigureAwait(false))
        {
            return await _db.NguoiDung.ToListAsync(ct).ConfigureAwait(false);
        }

        var vaiTro = await _db.VaiTro.ToDictionaryAsync(x => x.Ma, ct).ConfigureAwait(false);
        var (hash, salt) = _matKhau.BamMatKhau(MatKhauMacDinh);
        var danhSach = new List<NguoiDung>();

        NguoiDung Them(
            string tenDangNhap, string hoTen, string chucVu, string maDonVi,
            string[] maVaiTro, bool buocDoiMatKhau = true)
        {
            var dv = donVi.FirstOrDefault(x => x.Ma == maDonVi) ?? donVi[0];

            var nd = new NguoiDung
            {
                Id = Guid.NewGuid(),
                TenDangNhap = tenDangNhap,
                MatKhauHash = hash,
                MatKhauSalt = salt,
                HoTen = hoTen,
                HoTenKhongDau = VanBanTiengViet.TaoKhongDau(hoTen),
                Email = $"{tenDangNhap}@sangkien.gov.vn",
                DienThoai = $"09{Random.Shared.Next(10000000, 99999999)}",
                ChucVu = chucVu,
                DonViId = dv.Id,
                TrangThaiTaiKhoan = TrangThaiNguoiDung.HoatDong,
                BuocDoiMatKhau = buocDoiMatKhau
            };

            foreach (var mv in maVaiTro)
            {
                if (vaiTro.TryGetValue(mv, out var vt))
                {
                    nd.VaiTro.Add(new NguoiDungVaiTro
                    {
                        NguoiDungId = nd.Id,
                        VaiTroId = vt.Id,
                        DonViId = dv.Id
                    });
                }
            }

            danhSach.Add(nd);
            return nd;
        }

        // Tai khoan quan tri khong buoc doi mat khau de demo duoc ngay.
        Them("admin", "Quản trị viên hệ thống", "Quản trị viên", "UBND_TP",
            new[] { MaVaiTro.QuanTriHeThong }, buocDoiMatKhau: false);

        Them("lanhdao", "Nguyễn Văn Bình", "Phó Chủ tịch UBND thành phố", "UBND_TP",
            new[] { MaVaiTro.LanhDaoPheDuyet, MaVaiTro.LanhDaoXem }, false);

        Them("tiepnhan", "Trần Thị Hồng", "Chuyên viên Phòng Nội vụ", "PHONG_NOI_VU",
            new[] { MaVaiTro.CanBoTiepNhan }, false);

        Them("thuky", "Lê Minh Quân", "Chuyên viên Phòng Nội vụ", "PHONG_NOI_VU",
            new[] { MaVaiTro.ThuKyHoiDong, MaVaiTro.ThanhVienHoiDong }, false);

        Them("chutich", "Phạm Thị Lan", "Trưởng phòng Nội vụ", "PHONG_NOI_VU",
            new[] { MaVaiTro.ChuTichHoiDong, MaVaiTro.ThanhVienHoiDong }, false);

        var tenThanhVien = new (string TaiKhoan, string HoTen, string ChucVu, string DonVi)[]
        {
            ("hoidong01", "Võ Thanh Tùng", "Trưởng phòng GD&ĐT", "PHONG_GDDT"),
            ("hoidong02", "Đặng Thu Hà", "Trưởng phòng Y tế", "PHONG_YTE"),
            ("hoidong03", "Bùi Quốc Việt", "Trưởng phòng Văn hóa - Thông tin", "PHONG_VHTT"),
            ("hoidong04", "Hoàng Thị Mai", "Phó Trưởng phòng Kinh tế - Hạ tầng", "PHONG_KTHT"),
            ("hoidong05", "Ngô Đức Anh", "Chuyên viên Văn phòng", "VAN_PHONG")
        };

        foreach (var (taiKhoan, hoTen, chucVu, dv) in tenThanhVien)
        {
            Them(taiKhoan, hoTen, chucVu, dv, new[] { MaVaiTro.ThanhVienHoiDong }, false);
        }

        Them("qtdonvi", "Trịnh Văn Sơn", "Chánh Văn phòng", "VAN_PHONG",
            new[] { MaVaiTro.QuanTriDonVi }, false);

        var tacGia = new (string TaiKhoan, string HoTen, string ChucVu, string DonVi)[]
        {
            ("gv.lan", "Nguyễn Thị Lan", "Giáo viên", "TH_LE_LOI"),
            ("gv.hung", "Trần Mạnh Hùng", "Tổ trưởng chuyên môn", "THCS_QUANG_TRUNG"),
            ("gv.thuy", "Lê Thị Thúy", "Giáo viên", "THPT_TRAN_PHU"),
            ("gv.nam", "Phạm Hoài Nam", "Hiệu phó", "THCS_QUANG_TRUNG"),
            ("gv.hoa", "Đỗ Thị Hoa", "Giáo viên mầm non", "MN_HOA_SEN"),
            ("bs.tuan", "Nguyễn Anh Tuấn", "Bác sĩ", "TTYT_TP"),
            ("bs.chi", "Vũ Thị Chi", "Điều dưỡng trưởng", "TTYT_TP"),
            ("yt.dung", "Lý Văn Dũng", "Trạm trưởng", "TRAM_YT_P1"),
            ("cb.tam", "Hoàng Minh Tâm", "Chuyên viên", "TO_MOT_CUA_P1"),
            ("cb.linh", "Nguyễn Khánh Linh", "Chuyên viên", "TO_MOT_CUA_P2"),
            ("cb.phuc", "Trương Hồng Phúc", "Công chức Văn phòng - Thống kê", "TO_MOT_CUA_P3"),
            ("cb.trang", "Đinh Thùy Trang", "Chuyên viên", "PHONG_KTHT"),
            ("cb.khoa", "Nguyễn Đăng Khoa", "Chuyên viên CNTT", "VAN_PHONG"),
            ("cb.mai", "Lâm Ngọc Mai", "Chuyên viên", "PHONG_VHTT"),
            ("cb.long", "Phan Thành Long", "Kỹ sư", "BQL_DUAN"),
            ("cb.ngan", "Tô Kim Ngân", "Chuyên viên", "TT_VHTT"),
            ("cb.duy", "Nguyễn Quang Duy", "Chuyên viên", "PHONG_GDDT"),
            ("cb.yen", "Trần Hải Yến", "Chuyên viên", "PHONG_YTE")
        };

        foreach (var (taiKhoan, hoTen, chucVu, dv) in tacGia)
        {
            Them(taiKhoan, hoTen, chucVu, dv, new[] { MaVaiTro.TacGia }, false);
        }

        _db.NguoiDung.AddRange(danhSach);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return danhSach;
    }

    // ------------------------------------------------------------------------------------
    // Bo tieu chi 100 diem
    // ------------------------------------------------------------------------------------

    private async Task<BoTieuChi> SeedBoTieuChiAsync(CancellationToken ct)
    {
        var daCo = await _db.BoTieuChi
            .Include(x => x.DanhSachNhom)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (daCo is not null)
        {
            return daCo;
        }

        var bo = new BoTieuChi
        {
            Id = Guid.NewGuid(),
            Ma = "BTC-CO-SO-2026",
            Ten = "Bộ tiêu chí đánh giá sáng kiến cấp cơ sở năm 2026",
            TenKhongDau = VanBanTiengViet.TaoKhongDau("Bộ tiêu chí đánh giá sáng kiến cấp cơ sở năm 2026"),
            Nam = 2026,
            Cap = CapXetDuyet.CoSo,
            ThangDiemToiDa = 100m,
            DiemDatToiThieu = 50m,
            CachTinh = CachTinhDiem.TongDiem,
            LamTron = 2,
            ChoPhepChamDocLap = true,
            TuDongTongHop = true,
            LoaiBoDiemCaoThap = false
        };

        void ThemNhom(string ma, string ten, decimal trongSo, decimal diemToiDa,
            params (string Ten, decimal Diem, string HuongDan)[] tieuChi)
        {
            var nhom = new NhomTieuChi
            {
                Id = Guid.NewGuid(),
                BoTieuChiId = bo.Id,
                Ma = ma,
                Ten = ten,
                TenKhongDau = VanBanTiengViet.TaoKhongDau(ten),
                TrongSo = trongSo,
                DiemToiDa = diemToiDa,
                ThuTu = bo.DanhSachNhom.Count + 1
            };

            var thuTu = 1;
            foreach (var (tenTc, diem, huongDan) in tieuChi)
            {
                nhom.DanhSachTieuChi.Add(new TieuChiChamDiem
                {
                    Id = Guid.NewGuid(),
                    NhomTieuChiId = nhom.Id,
                    Ma = $"{ma}_{thuTu}",
                    Ten = tenTc,
                    TenKhongDau = VanBanTiengViet.TaoKhongDau(tenTc),
                    DiemToiDa = diem,
                    DiemToiThieu = 0m,
                    TrongSo = 100m,
                    KieuNhap = KieuNhapTieuChi.NhapSo,
                    BuocNhay = 0.5m,
                    HuongDanCham = huongDan,
                    ThuTu = thuTu++
                });
            }

            bo.DanhSachNhom.Add(nhom);
        }

        ThemNhom("TINH_MOI", "Tính mới", 30m, 30m,
            ("Giải pháp chưa từng được áp dụng tại đơn vị", 15m,
                "Đối chiếu với kho sáng kiến các năm trước và kết quả kiểm tra trùng lặp."),
            ("Mức độ sáng tạo, cải tiến so với giải pháp cũ", 15m,
                "Đánh giá mức độ khác biệt về cách tiếp cận, công nghệ, quy trình."));

        ThemNhom("HIEU_QUA", "Tính hiệu quả", 30m, 30m,
            ("Hiệu quả kinh tế, tiết kiệm chi phí", 15m,
                "Có số liệu định lượng về chi phí tiết kiệm hoặc giá trị làm lợi."),
            ("Hiệu quả xã hội, cải thiện chất lượng phục vụ", 15m,
                "Mức độ hài lòng của người dân, giảm thời gian xử lý."));

        ThemNhom("AP_DUNG", "Khả năng áp dụng", 25m, 25m,
            ("Khả năng nhân rộng ra đơn vị khác", 15m,
                "Giải pháp có thể triển khai ở đơn vị tương tự mà không cần đầu tư lớn."),
            ("Tính khả thi khi triển khai", 10m,
                "Nguồn lực, hạ tầng, con người đáp ứng được."));

        ThemNhom("PHAM_VI", "Phạm vi ảnh hưởng", 15m, 15m,
            ("Phạm vi và mức độ tác động", 15m,
                "Số đơn vị/người thụ hưởng, phạm vi địa bàn áp dụng."));

        var mucCongNhan = new (string Ma, string Ten, decimal Tu, decimal Den, string Mau, bool Dat)[]
        {
            ("KHONG_CONG_NHAN", "Không công nhận", 0m, 49.99m, "#ff4d4f", false),
            ("CAP_CO_SO", "Sáng kiến cấp cơ sở", 50m, 79.99m, "#1677ff", true),
            ("CAP_THANH_PHO", "Sáng kiến cấp thành phố", 80m, 100m, "#52c41a", true)
        };

        var thuTuMuc = 1;
        foreach (var (ma, ten, tu, den, mau, dat) in mucCongNhan)
        {
            bo.DanhSachMucCongNhan.Add(new MucCongNhan
            {
                Id = Guid.NewGuid(),
                BoTieuChiId = bo.Id,
                Ma = ma,
                Ten = ten,
                TenKhongDau = VanBanTiengViet.TaoKhongDau(ten),
                DiemTu = tu,
                DiemDen = den,
                MauSac = mau,
                LaDat = dat,
                ThuTu = thuTuMuc++
            });
        }

        _db.BoTieuChi.Add(bo);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return bo;
    }

    // ------------------------------------------------------------------------------------
    // Hoi dong 7 thanh vien
    // ------------------------------------------------------------------------------------

    private async Task<HoiDongSangKien> SeedHoiDongAsync(
        IReadOnlyList<DonVi> donVi, IReadOnlyList<NguoiDung> nguoiDung, CancellationToken ct)
    {
        var daCo = await _db.HoiDong.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (daCo is not null)
        {
            return daCo;
        }

        var donViHoiDong = donVi.FirstOrDefault(x => x.Ma == "HD_SANG_KIEN") ?? donVi[0];

        var hoiDong = new HoiDongSangKien
        {
            Id = Guid.NewGuid(),
            Ma = "HD-CO-SO-2026",
            Ten = "Hội đồng sáng kiến cấp cơ sở năm 2026",
            TenKhongDau = VanBanTiengViet.TaoKhongDau("Hội đồng sáng kiến cấp cơ sở năm 2026"),
            Cap = CapXetDuyet.CoSo,
            DonViId = donViHoiDong.Id,
            SoQuyetDinhThanhLap = "125/QĐ-UBND",
            NgayQuyetDinh = new DateOnly(2026, 1, 15),
            ThoiGianHoatDongTu = new DateOnly(2026, 1, 15),
            ThoiGianHoatDongDen = new DateOnly(2026, 12, 31),
            SoThanhVienToiThieu = 3,
            TyLeThongQua = 50m,
            TrangThaiHoatDong = "DANG_HOAT_DONG"
        };

        var thanhVien = new (string TaiKhoan, string ChucDanh, bool Cham, bool BoPhieu,
            bool KyBienBan, bool KetLuan)[]
        {
            ("chutich", ChucDanhHoiDong.ChuTich, true, true, true, true),
            ("hoidong01", ChucDanhHoiDong.PhoChuTich, true, true, true, false),
            ("hoidong02", ChucDanhHoiDong.UyVien, true, true, false, false),
            ("hoidong03", ChucDanhHoiDong.UyVien, true, true, false, false),
            ("hoidong04", ChucDanhHoiDong.UyVien, true, true, false, false),
            ("hoidong05", ChucDanhHoiDong.UyVien, true, true, false, false),
            ("thuky", ChucDanhHoiDong.UyVienThuKy, true, true, true, false)
        };

        var thuTu = 1;
        foreach (var (taiKhoan, chucDanh, cham, boPhieu, kyBienBan, ketLuan) in thanhVien)
        {
            var nd = nguoiDung.FirstOrDefault(x => x.TenDangNhap == taiKhoan);
            if (nd is null)
            {
                continue;
            }

            hoiDong.ThanhVien.Add(new HoiDongThanhVien
            {
                Id = Guid.NewGuid(),
                HoiDongId = hoiDong.Id,
                NguoiDungId = nd.Id,
                HoTenHienThi = nd.HoTen,
                ChucVuCongTac = nd.ChucVu,
                ChucDanh = chucDanh,
                QuyenChamDiem = cham,
                QuyenNhanXet = true,
                QuyenBoPhieu = boPhieu,
                QuyenKyBienBan = kyBienBan,
                QuyenKetLuan = ketLuan,
                ThuTu = thuTu++
            });
        }

        _db.HoiDong.Add(hoiDong);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return hoiDong;
    }

    // ------------------------------------------------------------------------------------
    // Quy trinh mau 6 buoc
    // ------------------------------------------------------------------------------------

    private async Task<EntityQuyTrinh> SeedQuyTrinhAsync(
        Guid hoiDongId, Guid boTieuChiId, CancellationToken ct)
    {
        var daCo = await _db.QuyTrinh.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (daCo is not null)
        {
            return daCo;
        }

        var quyTrinh = new EntityQuyTrinh
        {
            Id = Guid.NewGuid(),
            Ma = "QT-CO-SO-2026",
            Ten = "Quy trình xét công nhận sáng kiến cấp cơ sở năm 2026",
            TenKhongDau = VanBanTiengViet.TaoKhongDau("Quy trinh xet cong nhan sang kien cap co so nam 2026"),
            MoTa = "Quy trình 6 bước: Tiếp nhận → Thẩm định → Phân công chấm → Chấm điểm → "
                   + "Họp hội đồng → Ban hành quyết định",
            Cap = CapXetDuyet.CoSo,
            PhienBan = 1,
            LaMacDinh = true,
            TrangThaiQuyTrinh = TrangThaiQuyTrinh.DangApDung
        };

        var trangThaiChoXuLy = new QuyTrinhTrangThai
        {
            Id = Guid.NewGuid(),
            QuyTrinhId = quyTrinh.Id,
            Ma = "CHO_XU_LY",
            Ten = "Chờ xử lý",
            MauSac = "#faad14",
            ThuTu = 1
        };

        var trangThaiDangXuLy = new QuyTrinhTrangThai
        {
            Id = Guid.NewGuid(),
            QuyTrinhId = quyTrinh.Id,
            Ma = "DANG_XU_LY",
            Ten = "Đang xử lý",
            MauSac = "#1677ff",
            ThuTu = 2
        };

        var trangThaiBoSung = new QuyTrinhTrangThai
        {
            Id = Guid.NewGuid(),
            QuyTrinhId = quyTrinh.Id,
            Ma = "YEU_CAU_BO_SUNG",
            Ten = "Yêu cầu bổ sung",
            MauSac = "#fa8c16",
            ThuTu = 3
        };

        var trangThaiDaDuyet = new QuyTrinhTrangThai
        {
            Id = Guid.NewGuid(),
            QuyTrinhId = quyTrinh.Id,
            Ma = "DA_PHE_DUYET",
            Ten = "Đã phê duyệt",
            MauSac = "#52c41a",
            LaTrangThaiKetThuc = true,
            ThuTu = 4
        };

        var trangThaiKhongDat = new QuyTrinhTrangThai
        {
            Id = Guid.NewGuid(),
            QuyTrinhId = quyTrinh.Id,
            Ma = "KHONG_DAT",
            Ten = "Không đạt",
            MauSac = "#ff4d4f",
            LaTrangThaiKetThuc = true,
            ThuTu = 5
        };

        quyTrinh.TrangThaiToanCuc.Add(trangThaiChoXuLy);
        quyTrinh.TrangThaiToanCuc.Add(trangThaiDangXuLy);
        quyTrinh.TrangThaiToanCuc.Add(trangThaiBoSung);
        quyTrinh.TrangThaiToanCuc.Add(trangThaiDaDuyet);
        quyTrinh.TrangThaiToanCuc.Add(trangThaiKhongDat);

        QuyTrinhBuoc TaoBuoc(
            string ma, string ten, string loai, int thuTu, int soNgay,
            string maVaiTro, string quyTac = QuyTacXuLy.MotNguoi,
            bool batDau = false, bool ketThuc = false, bool batBuocYKien = true)
        {
            var buoc = new QuyTrinhBuoc
            {
                Id = Guid.NewGuid(),
                QuyTrinhId = quyTrinh.Id,
                Ma = ma,
                Ten = ten,
                LoaiBuoc = loai,
                ThuTu = thuTu,
                SoNgayXuLy = soNgay,
                TinhTheoNgayLamViec = true,
                BatBuocNhapYKien = batBuocYKien,
                ChoPhepThuHoi = true,
                LaBuocBatDau = batDau,
                LaBuocKetThuc = ketThuc,
                CanhBaoTruocHanGio = 24
            };

            buoc.TacNhan.Add(new QuyTrinhBuocTacNhan
            {
                Id = Guid.NewGuid(),
                BuocId = buoc.Id,
                LoaiTacNhan = LoaiTacNhan.VaiTro,
                ThamChieuMa = maVaiTro,
                QuyTacXuLy = quyTac,
                TyLeDongThuan = quyTac == QuyTacXuLy.DaSo ? 50m : null,
                ThuTu = 1
            });

            quyTrinh.DanhSachBuoc.Add(buoc);
            return buoc;
        }

        var b1 = TaoBuoc("B1", "Tiếp nhận hồ sơ", LoaiBuoc.TiepNhan, 1, 3,
            MaVaiTro.CanBoTiepNhan, batDau: true);
        var b2 = TaoBuoc("B2", "Thẩm định sơ bộ", LoaiBuoc.ThamDinh, 2, 5, MaVaiTro.ThuKyHoiDong);
        var b3 = TaoBuoc("B3", "Phân công chấm điểm", LoaiBuoc.PhanCongCham, 3, 2, MaVaiTro.ThuKyHoiDong);
        var b4 = TaoBuoc("B4", "Chấm điểm hội đồng", LoaiBuoc.ChamDiem, 4, 7,
            MaVaiTro.ThanhVienHoiDong, QuyTacXuLy.TatCa, batBuocYKien: false);
        var b5 = TaoBuoc("B5", "Họp hội đồng và kết luận", LoaiBuoc.HopHoiDong, 5, 5,
            MaVaiTro.ChuTichHoiDong);
        var b6 = TaoBuoc("B6", "Ban hành quyết định công nhận", LoaiBuoc.BanHanhQuyetDinh, 6, 10,
            MaVaiTro.LanhDaoPheDuyet, ketThuc: true);

        b4.HoiDongId = hoiDongId;
        b4.BoTieuChiId = boTieuChiId;

        void ThemTruongHop(
            QuyTrinhBuoc buoc, string ma, string ten, QuyTrinhBuoc? tiepTheo,
            Guid? trangThaiGan, BieuThucDieuKien? dieuKien, bool macDinh,
            string? mauNut, params string[] hanhDong)
        {
            buoc.TruongHop.Add(new QuyTrinhTruongHop
            {
                Id = Guid.NewGuid(),
                BuocId = buoc.Id,
                Ma = ma,
                Ten = ten,
                BuocTiepTheoId = tiepTheo?.Id,
                TrangThaiGanId = trangThaiGan,
                DieuKien = dieuKien,
                LaMacDinh = macDinh,
                MauNut = mauNut,
                HanhDong = hanhDong.ToList(),
                ThuTu = buoc.TruongHop.Count + 1
            });
        }

        // B1 - Tiep nhan
        ThemTruongHop(b1, MaTruongHop.Dat, "Tiếp nhận hồ sơ", b2, trangThaiDangXuLy.Id, null, true,
            "primary", HanhDongTuDong.GuiEmail, HanhDongTuDong.KiemTraTrungLap);
        ThemTruongHop(b1, MaTruongHop.BoSungHoSo, "Yêu cầu bổ sung", b1, trangThaiBoSung.Id,
            new BieuThucDieuKien
            {
                Truong = BienNguCanhSeed.HanhDongNguoiDung, ToanTu = "=", GiaTri = "BO_SUNG"
            },
            false, "dashed", HanhDongTuDong.GuiEmail, HanhDongTuDong.GuiSms);
        ThemTruongHop(b1, MaTruongHop.TraLai, "Từ chối tiếp nhận", null, trangThaiKhongDat.Id,
            new BieuThucDieuKien
            {
                Truong = BienNguCanhSeed.HanhDongNguoiDung, ToanTu = "=", GiaTri = "TU_CHOI"
            },
            false, "danger", HanhDongTuDong.GuiEmail);

        // B2 - Tham dinh
        ThemTruongHop(b2, MaTruongHop.Dat, "Đạt thẩm định", b3, trangThaiDangXuLy.Id, null, true,
            "primary");
        ThemTruongHop(b2, MaTruongHop.KhongDat, "Không đạt (trùng lặp quá ngưỡng)", null,
            trangThaiKhongDat.Id,
            new BieuThucDieuKien
            {
                Truong = BienNguCanhSeed.TyLeTrungLap, ToanTu = ">", GiaTri = 40
            },
            false, "danger", HanhDongTuDong.GuiEmail, HanhDongTuDong.CapNhatKetQua);
        ThemTruongHop(b2, MaTruongHop.BoSungHoSo, "Trả lại bổ sung", b1, trangThaiBoSung.Id,
            new BieuThucDieuKien
            {
                Truong = BienNguCanhSeed.HanhDongNguoiDung, ToanTu = "=", GiaTri = "BO_SUNG"
            },
            false, "dashed", HanhDongTuDong.GuiEmail);

        // B3 - Phan cong
        ThemTruongHop(b3, MaTruongHop.Dat, "Đã phân công chấm", b4, trangThaiDangXuLy.Id, null, true,
            "primary");

        // B4 - Cham diem
        ThemTruongHop(b4, MaTruongHop.Dat, "Hoàn thành chấm điểm", b5, trangThaiDangXuLy.Id, null, true,
            "primary");

        // B5 - Hop hoi dong
        ThemTruongHop(b5, MaTruongHop.Dat, "Đạt - đề nghị công nhận", b6, trangThaiDangXuLy.Id,
            new BieuThucDieuKien
            {
                Truong = BienNguCanhSeed.TongDiem, ToanTu = ">=", GiaTri = 50
            },
            false, "primary", HanhDongTuDong.TaoBienBan);
        ThemTruongHop(b5, MaTruongHop.ChuyenCapCaoHon, "Đề nghị xét cấp thành phố", b6,
            trangThaiDangXuLy.Id,
            new BieuThucDieuKien
            {
                Truong = BienNguCanhSeed.TongDiem, ToanTu = ">=", GiaTri = 80
            },
            false, "primary");
        ThemTruongHop(b5, MaTruongHop.KhongDat, "Không đạt", null, trangThaiKhongDat.Id,
            new BieuThucDieuKien
            {
                Truong = BienNguCanhSeed.TongDiem, ToanTu = "<", GiaTri = 50
            },
            false, "danger", HanhDongTuDong.GuiEmail, HanhDongTuDong.CapNhatKetQua);

        // B6 - Ban hanh quyet dinh
        ThemTruongHop(b6, MaTruongHop.Dat, "Đã ban hành quyết định", null, trangThaiDaDuyet.Id, null,
            true, "primary",
            HanhDongTuDong.TaoQuyetDinh, HanhDongTuDong.CapNhatKetQua,
            HanhDongTuDong.GuiEmail, HanhDongTuDong.DongBoLienThong);

        // Thanh phan ho so
        var thanhPhan = new (string Ma, string Ten, bool BatBuoc, string Loai, int ToiThieu,
            bool KiemTraTrungLap)[]
        {
            ("MO_TA_GIAI_PHAP", "Mô tả giải pháp", true, LoaiDuLieuThanhPhan.VanBan, 200, true),
            ("TINH_TRANG_TRUOC", "Tình trạng trước khi áp dụng", true, LoaiDuLieuThanhPhan.VanBan, 100, true),
            ("NOI_DUNG_GIAI_PHAP", "Nội dung giải pháp", true, LoaiDuLieuThanhPhan.VanBan, 300, true),
            ("TINH_MOI", "Tính mới của giải pháp", true, LoaiDuLieuThanhPhan.VanBan, 100, true),
            ("KHA_NANG_AP_DUNG", "Khả năng áp dụng", true, LoaiDuLieuThanhPhan.VanBan, 100, true),
            ("HIEU_QUA_KINH_TE", "Hiệu quả kinh tế", false, LoaiDuLieuThanhPhan.VanBan, 0, true),
            ("HIEU_QUA_XA_HOI", "Hiệu quả xã hội", false, LoaiDuLieuThanhPhan.VanBan, 0, true),
            ("MINH_CHUNG", "Minh chứng, tài liệu kèm theo", true, LoaiDuLieuThanhPhan.Tep, 0, true),
            ("PHU_LUC", "Phụ lục", false, LoaiDuLieuThanhPhan.Tep, 0, false)
        };

        var thuTuTp = 1;
        foreach (var (ma, ten, batBuoc, loai, toiThieu, kiemTra) in thanhPhan)
        {
            quyTrinh.ThanhPhanHoSo.Add(new QuyTrinhThanhPhanHoSo
            {
                Id = Guid.NewGuid(),
                QuyTrinhId = quyTrinh.Id,
                Ma = ma,
                Ten = ten,
                BatBuoc = batBuoc,
                LoaiDuLieu = loai,
                DinhDangChoPhep = new List<string>
                    { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".jpg", ".png", ".zip" },
                DungLuongToiDaMb = 20,
                SoLuongToiDa = 10,
                SoKyTuToiThieu = toiThieu,
                SoKyTuToiDa = 0,
                DungDeKiemTraTrungLap = kiemTra,
                ThuTu = thuTuTp++
            });
        }

        // Chuc nang bo sung
        quyTrinh.ChucNangBoSung.Add(new QuyTrinhChucNangBoSung
        {
            Id = Guid.NewGuid(),
            QuyTrinhId = quyTrinh.Id,
            MaChucNang = MaChucNangBoSung.KiemTraTrungLap,
            BatBuoc = true
        });

        quyTrinh.ChucNangBoSung.Add(new QuyTrinhChucNangBoSung
        {
            Id = Guid.NewGuid(),
            QuyTrinhId = quyTrinh.Id,
            BuocId = b4.Id,
            MaChucNang = MaChucNangBoSung.ChamDiemDocLap,
            BatBuoc = true
        });

        quyTrinh.ChucNangBoSung.Add(new QuyTrinhChucNangBoSung
        {
            Id = Guid.NewGuid(),
            QuyTrinhId = quyTrinh.Id,
            BuocId = b5.Id,
            MaChucNang = MaChucNangBoSung.TaoBienBan,
            BatBuoc = true
        });

        quyTrinh.ChucNangBoSung.Add(new QuyTrinhChucNangBoSung
        {
            Id = Guid.NewGuid(),
            QuyTrinhId = quyTrinh.Id,
            BuocId = b6.Id,
            MaChucNang = MaChucNangBoSung.KySo,
            BatBuoc = false
        });

        _db.QuyTrinh.Add(quyTrinh);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return quyTrinh;
    }

    // ------------------------------------------------------------------------------------
    // Dot de nghi
    // ------------------------------------------------------------------------------------

    private async Task SeedDotDeNghiAsync(
        Guid quyTrinhId, Guid boTieuChiId, IReadOnlyList<DonVi> donVi, CancellationToken ct)
    {
        if (await _db.DotDeNghi.AnyAsync(ct).ConfigureAwait(false))
        {
            return;
        }

        var donViIds = donVi.Select(x => x.Id).ToList();

        _db.DotDeNghi.Add(new DotDeNghi
        {
            Ma = "DOT-2025",
            Ten = "Đợt xét công nhận sáng kiến năm 2025",
            TenKhongDau = VanBanTiengViet.TaoKhongDau("Dot xet cong nhan sang kien nam 2025"),
            Nam = 2025,
            Ky = "NAM",
            TuNgay = new DateOnly(2025, 1, 1),
            DenNgay = new DateOnly(2025, 12, 31),
            HanNopHoSo = new DateTimeOffset(2025, 10, 31, 17, 0, 0, TimeSpan.FromHours(7)),
            HanChamDiem = new DateTimeOffset(2025, 11, 30, 17, 0, 0, TimeSpan.FromHours(7)),
            CapXetDuyet = CapXetDuyet.CoSo,
            QuyTrinhId = quyTrinhId,
            BoTieuChiId = boTieuChiId,
            DonViApDungIds = donViIds,
            TrangThaiDot = TrangThaiDot.DaDong,
            TuDongKhoa = true,
            ThuTu = 1
        });

        _db.DotDeNghi.Add(new DotDeNghi
        {
            Ma = "DOT-2026",
            Ten = "Đợt xét công nhận sáng kiến năm 2026",
            TenKhongDau = VanBanTiengViet.TaoKhongDau("Dot xet cong nhan sang kien nam 2026"),
            Nam = 2026,
            Ky = "NAM",
            TuNgay = new DateOnly(2026, 1, 1),
            DenNgay = new DateOnly(2026, 12, 31),
            HanNopHoSo = new DateTimeOffset(2026, 10, 31, 17, 0, 0, TimeSpan.FromHours(7)),
            HanChamDiem = new DateTimeOffset(2026, 11, 30, 17, 0, 0, TimeSpan.FromHours(7)),
            CapXetDuyet = CapXetDuyet.CoSo,
            QuyTrinhId = quyTrinhId,
            BoTieuChiId = boTieuChiId,
            DonViApDungIds = donViIds,
            TrangThaiDot = TrangThaiDot.DangMo,
            TuDongKhoa = true,
            ThuTu = 2
        });

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // ------------------------------------------------------------------------------------
    // Mau thong bao
    // ------------------------------------------------------------------------------------

    private async Task SeedMauThongBaoAsync(CancellationToken ct)
    {
        if (await _db.MauThongBao.AnyAsync(ct).ConfigureAwait(false))
        {
            return;
        }

        var mau = new (string Ma, string SuKien, string Kenh, string TieuDe, string NoiDung)[]
        {
            ("MTB_TIEP_NHAN", SuKienThongBao.HoSoDuocTiepNhan, "TAT_CA",
                "Hồ sơ {{ maHoSo }} đã được tiếp nhận",
                "Kính gửi {{ hoTen }},\nHồ sơ sáng kiến \"{{ tenSangKien }}\" (mã {{ maHoSo }}) "
                + "đã được tiếp nhận và chuyển sang bước {{ tenBuoc }}."),

            ("MTB_BO_SUNG", SuKienThongBao.YeuCauBoSung, "TAT_CA",
                "Yêu cầu bổ sung hồ sơ {{ maHoSo }}",
                "Kính gửi {{ hoTen }},\nHồ sơ \"{{ tenSangKien }}\" cần bổ sung. "
                + "Nội dung: {{ thongBao }}. Vui lòng đăng nhập hệ thống để cập nhật."),

            ("MTB_PHAN_CONG", SuKienThongBao.DuocPhanCongCham, "TAT_CA",
                "Bạn được phân công chấm {{ soHoSo }} hồ sơ",
                "Kính gửi {{ hoTen }},\nBạn được phân công chấm {{ soHoSo }} hồ sơ của "
                + "{{ tenHoiDong }}. Hạn hoàn thành: {{ hanHoanThanh }}."),

            ("MTB_SAP_HET_HAN", SuKienThongBao.SapHetHan, "TAT_CA",
                "Nhắc hạn xử lý hồ sơ {{ maHoSo }}",
                "Hồ sơ \"{{ tenSangKien }}\" sắp đến hạn xử lý ({{ hanXuLy }}). "
                + "Vui lòng xử lý sớm để tránh quá hạn."),

            ("MTB_CO_KET_QUA", SuKienThongBao.CoKetQua, "TAT_CA",
                "Kết quả xét sáng kiến {{ maHoSo }}",
                "Kính gửi {{ hoTen }},\nHồ sơ \"{{ tenSangKien }}\" đã có kết quả: {{ trangThai }}."),

            ("MTB_DA_PHE_DUYET", SuKienThongBao.DaPheDuyet, "TAT_CA",
                "Sáng kiến {{ maHoSo }} đã được công nhận",
                "Chúc mừng {{ hoTen }}! Sáng kiến \"{{ tenSangKien }}\" đã được công nhận."),

            ("MTB_TU_CHOI", SuKienThongBao.HoSoBiTuChoi, "TAT_CA",
                "Hồ sơ {{ maHoSo }} không được tiếp nhận",
                "Kính gửi {{ hoTen }},\nHồ sơ \"{{ tenSangKien }}\" không được tiếp nhận. "
                + "Lý do: {{ thongBao }}."),

            ("MTB_MOI_HOP", SuKienThongBao.MoiHopHoiDong, "TAT_CA",
                "Mời họp hội đồng sáng kiến",
                "Kính mời {{ hoTen }} tham dự phiên họp hội đồng sáng kiến.")
        };

        var thuTu = 1;
        foreach (var (ma, suKien, kenh, tieuDe, noiDung) in mau)
        {
            _db.MauThongBao.Add(new MauThongBao
            {
                Ma = ma,
                Ten = tieuDe,
                TenKhongDau = VanBanTiengViet.TaoKhongDau(tieuDe),
                SuKien = suKien,
                Kenh = kenh,
                TieuDe = tieuDe,
                NoiDung = noiDung,
                DanhSachBien = new List<string>
                    { "hoTen", "maHoSo", "tenSangKien", "tenBuoc", "trangThai", "thongBao" },
                ThuTu = thuTu++
            });
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

/// <summary>Ten bien dung trong dieu kien cua quy trinh mau (khop voi BienNguCanh cua engine).</summary>
internal static class BienNguCanhSeed
{
    public const string TongDiem = "tong_diem";
    public const string TyLeTrungLap = "ty_le_trung_lap";
    public const string HanhDongNguoiDung = "hanh_dong_nguoi_dung";
}
