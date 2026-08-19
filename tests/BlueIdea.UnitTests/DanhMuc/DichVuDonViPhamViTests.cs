using BlueIdea.Application.Chung;
using BlueIdea.Application.DanhMuc;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.DanhMuc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace BlueIdea.UnitTests.DanhMuc;

public sealed class DichVuDonViPhamViTests
{
    private static readonly Guid NguoiGoiId = Guid.NewGuid();
    private static readonly Guid DonViA = Guid.NewGuid();
    private static readonly Guid DonViB = Guid.NewGuid();
    private static readonly Guid DonViNgoai = Guid.NewGuid();

    private static DichVuDonVi TaoDichVu(PhamViTruyCap phamVi)
    {
        var db = Substitute.For<IAppDbContext>();
        var phanQuyen = Substitute.For<IDichVuPhanQuyen>();
        var dongHo = Substitute.For<IDongHoHeThong>();
        var nguoiDung = Substitute.For<INguoiDungHienTai>();

        nguoiDung.Id.Returns(NguoiGoiId);
        phanQuyen.LayPhamViTruyCapAsync(NguoiGoiId, Arg.Any<CancellationToken>())
            .Returns(phamVi);

        return new DichVuDonVi(db, phanQuyen, dongHo, nguoiDung);
    }

    private static (DichVuDonVi dichVu, IDichVuPhanQuyen phanQuyen) TaoDichVuChanQuyen()
    {
        var db = Substitute.For<IAppDbContext>();
        var phanQuyen = Substitute.For<IDichVuPhanQuyen>();
        var dongHo = Substitute.For<IDongHoHeThong>();
        var nguoiDung = Substitute.For<INguoiDungHienTai>();

        phanQuyen.BatBuocCoQuyenAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(ci => new KhongCoQuyenException(ci.Arg<string>()));

        var dichVu = new DichVuDonVi(db, phanQuyen, dongHo, nguoiDung);
        return (dichVu, phanQuyen);
    }

    [Fact]
    public async Task LayDanhSach_KiemTraQuyenDonViXem_KhongDanhMucXem()
    {
        var (dichVu, phanQuyen) = TaoDichVuChanQuyen();

        await Assert.ThrowsAsync<KhongCoQuyenException>(
            () => dichVu.LayDanhSachAsync(new ThamSoLocDanhMuc()));

        await phanQuyen.Received(1).BatBuocCoQuyenAsync(
            MaQuyen.DonViXem, Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LayTheoId_KiemTraQuyenDonViXem_KhongDanhMucXem()
    {
        var (dichVu, phanQuyen) = TaoDichVuChanQuyen();

        await Assert.ThrowsAsync<KhongCoQuyenException>(
            () => dichVu.LayTheoIdAsync(DonViA));

        await phanQuyen.Received(1).BatBuocCoQuyenAsync(
            MaQuyen.DonViXem, DonViA, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Them_KiemTraQuyenDonViCauHinh_KhongDanhMucThem()
    {
        var (dichVu, phanQuyen) = TaoDichVuChanQuyen();

        await Assert.ThrowsAsync<KhongCoQuyenException>(
            () => dichVu.ThemAsync(new DonVi()));

        await phanQuyen.Received(1).BatBuocCoQuyenAsync(
            MaQuyen.DonViCauHinh, Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CapNhat_KiemTraQuyenDonViCauHinh_KhongDanhMucSua()
    {
        var (dichVu, phanQuyen) = TaoDichVuChanQuyen();

        await Assert.ThrowsAsync<KhongCoQuyenException>(
            () => dichVu.CapNhatAsync(DonViA, _ => { }));

        await phanQuyen.Received(1).BatBuocCoQuyenAsync(
            MaQuyen.DonViCauHinh, DonViA, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Xoa_KiemTraQuyenDonViCauHinh_KhongDanhMucXoa()
    {
        var (dichVu, phanQuyen) = TaoDichVuChanQuyen();

        await Assert.ThrowsAsync<KhongCoQuyenException>(
            () => dichVu.XoaAsync(DonViA));

        await phanQuyen.Received(1).BatBuocCoQuyenAsync(
            MaQuyen.DonViCauHinh, DonViA, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChuyenCha_DonViNgoaiPhamVi_NemKhongTimThay()
    {
        var phamVi = new PhamViTruyCap
        {
            DonViIds = new HashSet<Guid> { DonViA, DonViB }
        };
        var dichVu = TaoDichVu(phamVi);

        var act = () => dichVu.ChuyenChaAsync(DonViNgoai, DonViA);

        await act.Should().ThrowAsync<KhongTimThayException>();
    }

    [Fact]
    public async Task ChuyenCha_ChaNoiNgoaiPhamVi_NemKhongTimThay()
    {
        var phamVi = new PhamViTruyCap
        {
            DonViIds = new HashSet<Guid> { DonViA, DonViB }
        };
        var dichVu = TaoDichVu(phamVi);

        var act = () => dichVu.ChuyenChaAsync(DonViA, DonViNgoai);

        await act.Should().ThrowAsync<KhongTimThayException>();
    }

    [Fact]
    public async Task GopAsync_NguonNgoaiPhamVi_NemKhongTimThay()
    {
        var phamVi = new PhamViTruyCap
        {
            DonViIds = new HashSet<Guid> { DonViA, DonViB }
        };
        var dichVu = TaoDichVu(phamVi);

        var act = () => dichVu.GopAsync(DonViNgoai, DonViA);

        await act.Should().ThrowAsync<KhongTimThayException>();
    }

    [Fact]
    public async Task GopAsync_DichNgoaiPhamVi_NemKhongTimThay()
    {
        var phamVi = new PhamViTruyCap
        {
            DonViIds = new HashSet<Guid> { DonViA, DonViB }
        };
        var dichVu = TaoDichVu(phamVi);

        var act = () => dichVu.GopAsync(DonViA, DonViNgoai);

        await act.Should().ThrowAsync<KhongTimThayException>();
    }

    [Fact]
    public async Task ChuyenCha_PhamViCaNhan_NemKhongTimThay()
    {
        var dichVu = TaoDichVu(PhamViTruyCap.CaNhan);

        var act = () => dichVu.ChuyenChaAsync(DonViA, DonViB);

        await act.Should().ThrowAsync<KhongTimThayException>();
    }

    [Fact]
    public async Task GopAsync_PhamViCaNhan_NemKhongTimThay()
    {
        var dichVu = TaoDichVu(PhamViTruyCap.CaNhan);

        var act = () => dichVu.GopAsync(DonViA, DonViB);

        await act.Should().ThrowAsync<KhongTimThayException>();
    }
}
