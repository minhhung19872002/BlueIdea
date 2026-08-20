using BlueIdea.IntegrationTests.HaTang;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Viec nen tao truoc phan vung thang (TD-004).
///
/// Kiem tren PostgreSQL that: tao mot bang phan vung rieng cho kiem thu roi goi dung ham ma
/// viec nen dung. Khong dung bang nhat ky that vi ban trien khai mac dinh CHUA phan vung —
/// va dung nen la vay: chi phan vung khi du lieu du lon.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class PhanVungThangTests
{
    private readonly UngDungKiemThu _ungDung;

    public PhanVungThangTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    [Fact]
    public async Task Bang_Chua_Phan_Vung_Thi_Viec_Nen_Khong_Lam_Gi()
    {
        using var pham = _ungDung.Services.CreateScope();

        var congViec = pham.ServiceProvider
            .GetRequiredService<Infrastructure.CongViecNen.CongViecTaoPhanVungThang>();

        // Ban trien khai kiem thu chua phan vung bang nhat ky nao.
        var daTao = await congViec.ChayAsync();

        daTao.Should().Be(0,
            "bảng chưa phân vùng thì việc nền phải bỏ qua, không được ném lỗi hay tạo bừa");
    }

    [Fact]
    public async Task Tao_Du_Phan_Vung_Cho_Thang_Nay_Va_Ba_Thang_Toi()
    {
        var tenBang = $"kt_phan_vung_{Guid.NewGuid():N}"[..24];

        using var pham = _ungDung.Services.CreateScope();
        var db = pham.ServiceProvider.GetRequiredService<Infrastructure.Persistence.AppDbContext>();

        // Dung bien trung gian: bo phan tich EF1002 chan chuoi noi suy truyen thang vao ...Raw.
        // Ten bang o day do chinh kiem thu sinh ra (Guid), khong den tu dau vao ben ngoai.
        var sqlTaoBang = $"""
            CREATE TABLE "{tenBang}" (
                id uuid NOT NULL,
                thoi_gian timestamptz NOT NULL,
                noi_dung text,
                PRIMARY KEY (id, thoi_gian)
            ) PARTITION BY RANGE (thoi_gian)
            """;

        await db.Database.ExecuteSqlRawAsync(sqlTaoBang);

        try
        {
            var congViec = pham.ServiceProvider
                .GetRequiredService<Infrastructure.CongViecNen.CongViecTaoPhanVungThang>();

            var lanDau = await congViec.TaoPhanVungAsync(tenBang, "thoi_gian");

            lanDau.Should().Be(4, "tháng này + 3 tháng tới");

            // Chay lai khong duoc tao trung.
            var lanHai = await congViec.TaoPhanVungAsync(tenBang, "thoi_gian");
            lanHai.Should().Be(0, "phân vùng đã có thì không tạo lại");

            // Ghi thu mot ban ghi cua thang nay — phai vao dung phan vung, khong loi.
            var id = Guid.NewGuid();

            var sqlChen =
                $"""INSERT INTO "{tenBang}" (id, thoi_gian, noi_dung) VALUES ('{id}', now(), 'kiem thu')""";

            await db.Database.ExecuteSqlRawAsync(sqlChen);

            var sqlDem = $"""SELECT count(*)::int AS "Value" FROM "{tenBang}" """;

            var soDong = await db.Database.SqlQueryRaw<int>(sqlDem).FirstAsync();

            soDong.Should().Be(1);

            var sqlDemPhanVung = $"""
                SELECT count(*)::int AS "Value"
                FROM pg_inherits i
                JOIN pg_class p ON p.oid = i.inhparent
                WHERE p.relname = '{tenBang}'
                """;

            var soPhanVung = await db.Database.SqlQueryRaw<int>(sqlDemPhanVung).FirstAsync();

            soPhanVung.Should().Be(4);
        }
        finally
        {
            var sqlXoa = $"""DROP TABLE IF EXISTS "{tenBang}" CASCADE""";
            await db.Database.ExecuteSqlRawAsync(sqlXoa);
        }
    }
}
