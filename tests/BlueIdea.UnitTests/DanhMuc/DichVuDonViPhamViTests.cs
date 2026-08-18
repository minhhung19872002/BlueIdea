using BlueIdea.Application.Chung;
using BlueIdea.Application.DanhMuc;
using NSubstitute;

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
