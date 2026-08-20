using System.Globalization;
using System.Text;

namespace BlueIdea.Ai.Nhung;

/// <summary>
/// Bo tach tu WordPiece doc tu tep <c>vocab.txt</c> cua mo hinh (moi dong mot token, so dong = id).
///
/// Tu viet thay vi keo them thu vien tach tu: bo tach tu phai khop TUYET DOI voi tu vung cua mo
/// hinh, nen phan phai dung la du lieu (vocab.txt) chu khong phai thu vien. WordPiece la thuat
/// toan ngan, tat dinh, kiem thu duoc — dung nhu cach du an tu viet SimHash / MinHash / TF-IDF.
///
/// KHONG bo dau tieng Viet trong moi truong hop: "sang" va "sáng" la hai tu khac nhau, bo dau la
/// nhap nhang ngu nghia ngay tu buoc dau vao. (BERT da ngon ngu co tuy chon strip_accents, nhung
/// mo hinh tieng Viet nao cung tat no.)
/// </summary>
public sealed class BoTachTuWordPiece
{
    /// <summary>Do dai toi da cua mot "tu" truoc khi bo qua — chan chuoi rac dai bat thuong.</summary>
    private const int DoDaiTuToiDa = 100;

    private const string TienToNoiTiep = "##";

    private readonly Dictionary<string, int> _tuVung;
    private readonly bool _haThapChu;

    public BoTachTuWordPiece(IReadOnlyDictionary<string, int> tuVung, bool haThapChu = true)
    {
        ArgumentNullException.ThrowIfNull(tuVung);

        if (tuVung.Count == 0)
        {
            throw new ArgumentException("Từ vựng rỗng.", nameof(tuVung));
        }

        _tuVung = new Dictionary<string, int>(tuVung, StringComparer.Ordinal);
        _haThapChu = haThapChu;

        IdPad = LayIdBatBuoc("[PAD]");
        IdUnk = LayIdBatBuoc("[UNK]");
        IdCls = LayIdBatBuoc("[CLS]");
        IdSep = LayIdBatBuoc("[SEP]");
    }

    public int IdPad { get; }

    public int IdUnk { get; }

    public int IdCls { get; }

    public int IdSep { get; }

    public int SoTuVung => _tuVung.Count;

    /// <summary>Doc <c>vocab.txt</c>: dong thu i la token co id i.</summary>
    public static BoTachTuWordPiece TuTep(string duongDan, bool haThapChu = true)
    {
        if (!File.Exists(duongDan))
        {
            throw new FileNotFoundException($"Không tìm thấy tệp từ vựng '{duongDan}'.", duongDan);
        }

        var tuVung = new Dictionary<string, int>(StringComparer.Ordinal);
        var id = 0;

        foreach (var dong in File.ReadLines(duongDan, Encoding.UTF8))
        {
            // Khong Trim() ca dong: mot so tu vung co token la dau cach. Chi bo ky tu xuong dong.
            var token = dong.TrimEnd('\r', '\n');

            // Tu vung chuan khong co dong trung; neu co, giu id NHO NHAT cho giong bo tach tu goc.
            _ = _tuVung_ThemNeuChua(tuVung, token, id);
            id++;
        }

        return new BoTachTuWordPiece(tuVung, haThapChu);
    }

    /// <summary>
    /// Tach van ban thanh day id kem mat na chu y, da them [CLS] / [SEP] va cat theo do dai toi da.
    /// </summary>
    public (long[] Ids, long[] MatNa) Tach(string? vanBan, int soTokenToiDa)
    {
        if (soTokenToiDa < 2)
        {
            throw new ArgumentOutOfRangeException(
                nameof(soTokenToiDa), "Cần ít nhất 2 token cho [CLS] và [SEP].");
        }

        var ids = new List<long>(soTokenToiDa) { IdCls };

        // Chua cho [SEP] o cuoi.
        var conLai = soTokenToiDa - 2;

        foreach (var tu in TachTho(vanBan))
        {
            if (conLai <= 0)
            {
                break;
            }

            foreach (var manh in TachWordPiece(tu))
            {
                if (conLai <= 0)
                {
                    break;
                }

                ids.Add(manh);
                conLai--;
            }
        }

        ids.Add(IdSep);

        var matNa = new long[ids.Count];
        Array.Fill(matNa, 1L);

        return (ids.ToArray(), matNa);
    }

    /// <summary>Tach tho: khoang trang cat tu, dau cau tach thanh token rieng.</summary>
    internal IEnumerable<string> TachTho(string? vanBan)
    {
        if (string.IsNullOrWhiteSpace(vanBan))
        {
            yield break;
        }

        var chuan = vanBan.Normalize(NormalizationForm.FormC);
        var dem = new StringBuilder();

        foreach (var ky in chuan)
        {
            if (char.IsWhiteSpace(ky))
            {
                if (dem.Length > 0)
                {
                    yield return LayChuanHoa(dem);
                }

                continue;
            }

            if (char.IsPunctuation(ky) || char.IsSymbol(ky))
            {
                if (dem.Length > 0)
                {
                    yield return LayChuanHoa(dem);
                }

                yield return _haThapChu ? ky.ToString().ToLowerInvariant() : ky.ToString();
                continue;
            }

            dem.Append(ky);
        }

        if (dem.Length > 0)
        {
            yield return LayChuanHoa(dem);
        }
    }

    /// <summary>WordPiece tham lam: khop doan dai nhat tu trai sang, phan sau mang tien to "##".</summary>
    internal IEnumerable<long> TachWordPiece(string tu)
    {
        if (tu.Length > DoDaiTuToiDa)
        {
            yield return IdUnk;
            yield break;
        }

        var batDau = 0;
        var manh = new List<long>();

        while (batDau < tu.Length)
        {
            var ket = tu.Length;
            var timThay = -1;

            while (batDau < ket)
            {
                var doan = batDau == 0
                    ? tu[batDau..ket]
                    : TienToNoiTiep + tu[batDau..ket];

                if (_tuVung.TryGetValue(doan, out var id))
                {
                    timThay = id;
                    break;
                }

                ket--;
            }

            if (timThay < 0)
            {
                // Mot manh khong khop thi CA TU thanh [UNK] — dung theo WordPiece goc, khong ghep
                // nua vo nua [UNK] vi nhu vay tao ra vector khong giong bat ky lan huan luyen nao.
                yield return IdUnk;
                yield break;
            }

            manh.Add(timThay);
            batDau = ket;
        }

        foreach (var id in manh)
        {
            yield return id;
        }
    }

    // ------------------------------------------------------------------------------------

    private string LayChuanHoa(StringBuilder dem)
    {
        var tu = dem.ToString();
        dem.Clear();

        return _haThapChu ? tu.ToLower(CultureInfo.InvariantCulture) : tu;
    }

    private int LayIdBatBuoc(string token)
        => _tuVung.TryGetValue(token, out var id)
            ? id
            : throw new ArgumentException(
                $"Từ vựng thiếu token đặc biệt '{token}' — tệp vocab.txt không phải của mô hình BERT/WordPiece.");

    private static bool _tuVung_ThemNeuChua(Dictionary<string, int> tuVung, string token, int id)
        => tuVung.TryAdd(token, id);
}
