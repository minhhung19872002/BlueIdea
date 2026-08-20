using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace BlueIdea.Ai.Nhung;

/// <summary>Cau hinh nap mo hinh nhung ONNX chay noi bo.</summary>
public sealed record CauHinhNhungOnnx
{
    /// <summary>Duong dan tep <c>.onnx</c> tren may chu.</summary>
    public string DuongDanMoHinh { get; init; } = string.Empty;

    /// <summary>Duong dan <c>vocab.txt</c> di kem mo hinh.</summary>
    public string DuongDanTuVung { get; init; } = string.Empty;

    /// <summary>Ten ghi vao ket qua kiem tra trung lap de truy vet mo hinh nao sinh ra vector.</summary>
    public string TenMoHinh { get; init; } = "onnx";

    /// <summary>So chieu vector cot pgvector dang dung — lech la tu choi nap, khong ghi bua.</summary>
    public int SoChieuMongDoi { get; init; } = 768;

    public int SoTokenToiDa { get; init; } = 256;

    public bool HaThapChu { get; init; } = true;
}

/// <summary>
/// Bo nhung chay mo hinh sentence-transformer dinh dang ONNX ngay tren may chu don vi.
///
/// Khong goi API AI ben thu ba (rang buoc Muc 3.2 E-HSMT, xem docs/ADR/0001-ai-noi-bo.md): toan
/// bo suy luan nam trong tien trinh API, du lieu ho so khong roi khoi ha tang cua don vi.
///
/// Ba viec bo nay lam ma mo hinh khong lam ho: tach tu theo dung tu vung cua mo hinh, gop token
/// (mean pooling) co loai [PAD], va chuan hoa L2 de cosine chi phu thuoc huong vector.
/// </summary>
public sealed class BoNhungOnnx : IBoNhungVanBan, IDisposable
{
    private readonly InferenceSession _phien;
    private readonly BoTachTuWordPiece _tachTu;
    private readonly CauHinhNhungOnnx _cauHinh;

    private readonly string _tenInputIds;
    private readonly string? _tenMatNa;
    private readonly string? _tenLoaiToken;
    private readonly string _tenDauRa;

    public BoNhungOnnx(CauHinhNhungOnnx cauHinh)
    {
        ArgumentNullException.ThrowIfNull(cauHinh);
        _cauHinh = cauHinh;

        if (!File.Exists(cauHinh.DuongDanMoHinh))
        {
            throw new FileNotFoundException(
                $"Không tìm thấy mô hình ONNX '{cauHinh.DuongDanMoHinh}'.", cauHinh.DuongDanMoHinh);
        }

        _tachTu = BoTachTuWordPiece.TuTep(cauHinh.DuongDanTuVung, cauHinh.HaThapChu);
        _phien = new InferenceSession(cauHinh.DuongDanMoHinh);

        var dauVao = _phien.InputMetadata.Keys.ToList();

        _tenInputIds = dauVao.FirstOrDefault(x => x.Contains("input_ids", StringComparison.OrdinalIgnoreCase))
                       ?? dauVao.FirstOrDefault()
                       ?? throw new InvalidOperationException("Mô hình ONNX không khai báo đầu vào nào.");

        _tenMatNa = dauVao.FirstOrDefault(
            x => x.Contains("attention_mask", StringComparison.OrdinalIgnoreCase));

        _tenLoaiToken = dauVao.FirstOrDefault(
            x => x.Contains("token_type_ids", StringComparison.OrdinalIgnoreCase));

        _tenDauRa = _phien.OutputMetadata.Keys
                        .FirstOrDefault(x => x.Contains("last_hidden_state", StringComparison.OrdinalIgnoreCase))
                    ?? _phien.OutputMetadata.Keys
                        .FirstOrDefault(x => x.Contains("sentence_embedding", StringComparison.OrdinalIgnoreCase))
                    ?? _phien.OutputMetadata.Keys.FirstOrDefault()
                    ?? throw new InvalidOperationException("Mô hình ONNX không khai báo đầu ra nào.");

        // Chay thu ngay luc nap: sai so chieu ma de den luc ghi CSDL moi lo thi cot pgvector se
        // tu choi tung ban ghi mot, rai rac trong log, khong ai hieu vi sao.
        SoChieu = SuyLuan("kiểm tra mô hình").Length;

        if (SoChieu != cauHinh.SoChieuMongDoi)
        {
            _phien.Dispose();

            throw new InvalidOperationException(
                $"Mô hình '{cauHinh.DuongDanMoHinh}' sinh vector {SoChieu} chiều nhưng cột "
                + $"embedding đang là {cauHinh.SoChieuMongDoi} chiều. Dùng mô hình khác hoặc "
                + "chuyển cột sang đúng số chiều rồi nhúng lại toàn bộ dữ liệu.");
        }
    }

    public int SoChieu { get; }

    public string TenMoHinh => _cauHinh.TenMoHinh;

    public Task<float[]> TaoVectorAsync(string? vanBan, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(SuyLuan(vanBan));
    }

    public Task<IReadOnlyList<float[]>> TaoVectorHangLoatAsync(
        IReadOnlyList<string> cacVanBan, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(cacVanBan);

        var ketQua = new List<float[]>(cacVanBan.Count);

        foreach (var vanBan in cacVanBan)
        {
            ct.ThrowIfCancellationRequested();
            ketQua.Add(SuyLuan(vanBan));
        }

        return Task.FromResult<IReadOnlyList<float[]>>(ketQua);
    }

    public void Dispose() => _phien.Dispose();

    // ------------------------------------------------------------------------------------

    private float[] SuyLuan(string? vanBan)
    {
        var (ids, matNa) = _tachTu.Tach(vanBan, _cauHinh.SoTokenToiDa);
        var soToken = ids.Length;

        var dauVao = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(
                _tenInputIds, new DenseTensor<long>(ids, new[] { 1, soToken }))
        };

        if (_tenMatNa is not null)
        {
            dauVao.Add(NamedOnnxValue.CreateFromTensor(
                _tenMatNa, new DenseTensor<long>(matNa, new[] { 1, soToken })));
        }

        if (_tenLoaiToken is not null)
        {
            dauVao.Add(NamedOnnxValue.CreateFromTensor(
                _tenLoaiToken, new DenseTensor<long>(new long[soToken], new[] { 1, soToken })));
        }

        using var ketQua = _phien.Run(dauVao, new[] { _tenDauRa });
        var tensor = ketQua.First().AsTensor<float>();

        var vector = tensor.Dimensions.Length == 2
            ? LayHangDau(tensor)
            : GopTrungBinhCoMatNa(tensor, matNa);

        ChuanHoaL2(vector);
        return vector;
    }

    /// <summary>Mo hinh da gop san (dau ra rank 2) thi lay nguyen hang dau.</summary>
    private static float[] LayHangDau(Tensor<float> tensor)
    {
        var soChieu = tensor.Dimensions[1];
        var vector = new float[soChieu];

        for (var i = 0; i < soChieu; i++)
        {
            vector[i] = tensor[0, i];
        }

        return vector;
    }

    /// <summary>
    /// Mean pooling co loai token [PAD].
    ///
    /// Khong loai [PAD] la loi im lang kinh dien: hai cau khac nhau nhung cung do dai dem se bi
    /// keo ve gan nhau, ty le trung lap bao cao sai ma khong co dau hieu gi.
    /// </summary>
    private static float[] GopTrungBinhCoMatNa(Tensor<float> tensor, IReadOnlyList<long> matNa)
    {
        var soToken = tensor.Dimensions[1];
        var soChieu = tensor.Dimensions[2];

        var vector = new float[soChieu];
        var soThuc = 0;

        for (var t = 0; t < soToken; t++)
        {
            if (t < matNa.Count && matNa[t] == 0)
            {
                continue;
            }

            soThuc++;

            for (var c = 0; c < soChieu; c++)
            {
                vector[c] += tensor[0, t, c];
            }
        }

        if (soThuc == 0)
        {
            return vector;
        }

        for (var c = 0; c < soChieu; c++)
        {
            vector[c] /= soThuc;
        }

        return vector;
    }

    private static void ChuanHoaL2(float[] vector)
    {
        var chuan = (float)Math.Sqrt(vector.Sum(v => (double)v * v));

        if (chuan <= 0f)
        {
            return;
        }

        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] /= chuan;
        }
    }
}
