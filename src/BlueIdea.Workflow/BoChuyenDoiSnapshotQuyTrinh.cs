using System.Text.Json;
using System.Text.Json.Serialization;
using BlueIdea.Domain.QuyTrinh;

namespace BlueIdea.Workflow;

/// <summary>
/// Snapshot quy trinh: khi ho so duoc nop, toan bo cau hinh quy trinh duoc "dong bang"
/// vao truong <c>quy_trinh_snapshot</c>. Engine luon chay theo snapshot nay nen admin
/// co the sua quy trinh ma khong lam hong cac ho so dang chay (dac ta Muc 5 - Nhom II).
/// </summary>
public sealed class BoChuyenDoiSnapshotQuyTrinh : IBoChuyenDoiSnapshotQuyTrinh
{
    private static readonly JsonSerializerOptions TuyChon = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    public string TaoSnapshot(QuyTrinh quyTrinh)
    {
        ArgumentNullException.ThrowIfNull(quyTrinh);

        // Cat bo tham chieu nguoc de tranh vong lap khi serialize.
        var ban = new QuyTrinh
        {
            Id = quyTrinh.Id,
            Ma = quyTrinh.Ma,
            Ten = quyTrinh.Ten,
            TenKhongDau = quyTrinh.TenKhongDau,
            MoTa = quyTrinh.MoTa,
            Cap = quyTrinh.Cap,
            PhienBan = quyTrinh.PhienBan,
            QuyTrinhGocId = quyTrinh.QuyTrinhGocId,
            PhamViApDung = quyTrinh.PhamViApDung,
            LaMacDinh = quyTrinh.LaMacDinh,
            TrangThaiQuyTrinh = quyTrinh.TrangThaiQuyTrinh,
            TrangThai = quyTrinh.TrangThai,
            SoDoLayout = quyTrinh.SoDoLayout,
            DanhSachBuoc = quyTrinh.DanhSachBuoc
                .Where(b => !b.DaXoa)
                .Select(SaoChepBuoc)
                .ToList(),
            ThanhPhanHoSo = quyTrinh.ThanhPhanHoSo
                .Where(t => !t.DaXoa)
                .Select(SaoChepThanhPhan)
                .ToList(),
            ChucNangBoSung = quyTrinh.ChucNangBoSung
                .Where(c => !c.DaXoa)
                .Select(SaoChepChucNang)
                .ToList(),
            TrangThaiToanCuc = quyTrinh.TrangThaiToanCuc
                .Where(t => !t.DaXoa)
                .Select(SaoChepTrangThai)
                .ToList()
        };

        return JsonSerializer.Serialize(ban, TuyChon);
    }

    public QuyTrinh? DocSnapshot(string? snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot))
        {
            return null;
        }

        try
        {
            var quyTrinh = JsonSerializer.Deserialize<QuyTrinh>(snapshot, TuyChon);
            if (quyTrinh is null)
            {
                return null;
            }

            // Khoi phuc tham chieu nguoc de engine duyet duoc 2 chieu.
            foreach (var buoc in quyTrinh.DanhSachBuoc)
            {
                buoc.QuyTrinhId = quyTrinh.Id;
                foreach (var tn in buoc.TacNhan)
                {
                    tn.BuocId = buoc.Id;
                }

                foreach (var th in buoc.TruongHop)
                {
                    th.BuocId = buoc.Id;
                }

                foreach (var tt in buoc.TrangThai)
                {
                    tt.BuocId = buoc.Id;
                    tt.QuyTrinhId = quyTrinh.Id;
                }
            }

            return quyTrinh;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static QuyTrinhBuoc SaoChepBuoc(QuyTrinhBuoc nguon) => new()
    {
        Id = nguon.Id,
        QuyTrinhId = nguon.QuyTrinhId,
        Ma = nguon.Ma,
        Ten = nguon.Ten,
        ThuTu = nguon.ThuTu,
        LoaiBuoc = nguon.LoaiBuoc,
        SoNgayXuLy = nguon.SoNgayXuLy,
        TinhTheoNgayLamViec = nguon.TinhTheoNgayLamViec,
        BatBuocDinhKem = nguon.BatBuocDinhKem,
        DanhSachTepBatBuoc = nguon.DanhSachTepBatBuoc,
        BatBuocNhapYKien = nguon.BatBuocNhapYKien,
        ChoPhepUyQuyen = nguon.ChoPhepUyQuyen,
        ChoPhepThuHoi = nguon.ChoPhepThuHoi,
        LaBuocBatDau = nguon.LaBuocBatDau,
        LaBuocKetThuc = nguon.LaBuocKetThuc,
        CanhBaoTruocHanGio = nguon.CanhBaoTruocHanGio,
        MoTaHuongDan = nguon.MoTaHuongDan,
        HoiDongId = nguon.HoiDongId,
        BoTieuChiId = nguon.BoTieuChiId,
        TacNhan = nguon.TacNhan.Where(t => !t.DaXoa).Select(t => new QuyTrinhBuocTacNhan
        {
            Id = t.Id,
            BuocId = t.BuocId,
            LoaiTacNhan = t.LoaiTacNhan,
            ThamChieuId = t.ThamChieuId,
            ThamChieuMa = t.ThamChieuMa,
            QuyTacXuLy = t.QuyTacXuLy,
            TyLeDongThuan = t.TyLeDongThuan,
            ThuTu = t.ThuTu
        }).ToList(),
        TruongHop = nguon.TruongHop.Where(t => !t.DaXoa).Select(t => new QuyTrinhTruongHop
        {
            Id = t.Id,
            BuocId = t.BuocId,
            Ma = t.Ma,
            Ten = t.Ten,
            BuocTiepTheoId = t.BuocTiepTheoId,
            TrangThaiGanId = t.TrangThaiGanId,
            DieuKien = t.DieuKien,
            HanhDong = t.HanhDong,
            MauThongBaoId = t.MauThongBaoId,
            MauNut = t.MauNut,
            ThuTu = t.ThuTu,
            LaMacDinh = t.LaMacDinh
        }).ToList(),
        TrangThai = nguon.TrangThai.Where(t => !t.DaXoa).Select(SaoChepTrangThai).ToList()
    };

    private static QuyTrinhTrangThai SaoChepTrangThai(QuyTrinhTrangThai nguon) => new()
    {
        Id = nguon.Id,
        QuyTrinhId = nguon.QuyTrinhId,
        BuocId = nguon.BuocId,
        Ma = nguon.Ma,
        Ten = nguon.Ten,
        MauSac = nguon.MauSac,
        Icon = nguon.Icon,
        LaTrangThaiKetThuc = nguon.LaTrangThaiKetThuc,
        HienThiChoTacGia = nguon.HienThiChoTacGia,
        ThuTu = nguon.ThuTu
    };

    private static QuyTrinhThanhPhanHoSo SaoChepThanhPhan(QuyTrinhThanhPhanHoSo nguon) => new()
    {
        Id = nguon.Id,
        QuyTrinhId = nguon.QuyTrinhId,
        Ma = nguon.Ma,
        Ten = nguon.Ten,
        BatBuoc = nguon.BatBuoc,
        LoaiDuLieu = nguon.LoaiDuLieu,
        DinhDangChoPhep = nguon.DinhDangChoPhep,
        DungLuongToiDaMb = nguon.DungLuongToiDaMb,
        SoLuongToiDa = nguon.SoLuongToiDa,
        SoKyTuToiThieu = nguon.SoKyTuToiThieu,
        SoKyTuToiDa = nguon.SoKyTuToiDa,
        DungDeKiemTraTrungLap = nguon.DungDeKiemTraTrungLap,
        ThuTu = nguon.ThuTu,
        MoTaHuongDan = nguon.MoTaHuongDan
    };

    private static QuyTrinhChucNangBoSung SaoChepChucNang(QuyTrinhChucNangBoSung nguon) => new()
    {
        Id = nguon.Id,
        QuyTrinhId = nguon.QuyTrinhId,
        BuocId = nguon.BuocId,
        MaChucNang = nguon.MaChucNang,
        BatBuoc = nguon.BatBuoc,
        CauHinh = nguon.CauHinh
    };
}
