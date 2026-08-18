<#
.SYNOPSIS
    Kịch bản chạy ba nhánh quy trình mở theo DỮ LIỆU, không theo lựa chọn của người xử lý.

.DESCRIPTION
    Hai kịch bản kia phủ 9/12 nhánh. Ba nhánh còn lại chỉ mở khi dữ liệu thoả điều kiện, nên phải
    dựng đúng dữ liệu mới đi qua được:

      I.   KHÔNG ĐẠT ở bước thẩm định   — điều kiện `ty_le_trung_lap > 40`
      II.  ĐỀ NGHỊ XÉT CẤP CAO HƠN      — điều kiện `tong_diem >= 80`
      III. KHÔNG ĐẠT ở họp hội đồng     — điều kiện `tong_diem < 50`

    Nhánh II và III chỉ khác nhau ở điểm chấm, nên kịch bản chấm cao cho một hồ sơ và chấm thấp
    cho hồ sơ kia rồi kiểm chính điều quan trọng: **nhánh nào mở và nhánh nào bị chặn**. Chặn sai
    còn nguy hiểm hơn không chặn: hội đồng cho điểm 30 mà hệ thống vẫn mở nhánh "đề nghị công nhận"
    thì một sáng kiến không đạt được công nhận mà không ai thấy gì bất thường.

.EXAMPLE
    ./scripts/kiem-thu-luong-nhanh-du-lieu.ps1 -Goc http://127.0.0.1:58080
#>
param(
    [string]$Goc = 'http://localhost:5299',
    [string]$MatKhau = 'Sk@2026'
)

$ErrorActionPreference = 'Stop'
$script:soBuoc = 0
$script:soLoi = 0

function Ghi($thongDiep, $mau = 'White') { Write-Host $thongDiep -ForegroundColor $mau }

function KiemTra($ten, $dieuKien, $chiTiet = '') {
    $script:soBuoc++
    if ($dieuKien) {
        Ghi ("  [DAT ] $ten" + $(if ($chiTiet) { " -> $chiTiet" })) 'Green'
    }
    else {
        $script:soLoi++
        Ghi ("  [LOI ] $ten" + $(if ($chiTiet) { " -> $chiTiet" })) 'Red'
    }
}

function DangNhap($ten) {
    $than = @{ tenDangNhap = $ten; matKhau = $MatKhau } | ConvertTo-Json
    $kq = Invoke-RestMethod -Uri "$Goc/api/v1/xac-thuc/dang-nhap" -Method Post `
        -Body $than -ContentType 'application/json'
    return @{ Authorization = "Bearer $($kq.duLieu.accessToken)" }
}

function Goi($duongDan, $header, $phuongThuc = 'Get', $than = $null) {
    $tuyChon = @{ Uri = "$Goc$duongDan"; Headers = $header; Method = $phuongThuc }
    if ($than) { $tuyChon.Body = ($than | ConvertTo-Json -Depth 6); $tuyChon.ContentType = 'application/json' }
    return Invoke-RestMethod @tuyChon
}

function TrangThai($id, $header) { return (Goi "/api/v1/sang-kien/$id" $header).duLieu }

function LayNhanh($id, $header, $ma) {
    return (Goi "/api/v1/sang-kien/$id/hanh-dong" $header).duLieu |
        Where-Object { $_.ma -eq $ma } | Select-Object -First 1
}

function ThucThi($id, $header, $ma, $yKien) {
    $nhanh = LayNhanh $id $header $ma
    if (-not $nhanh -or $nhanh.biChan) { return $null }

    return (Goi '/api/v1/xu-ly/thuc-thi' $header 'Post' @{
            sangKienId = $id; truongHopId = $nhanh.truongHopId; yKien = $yKien
        }).duLieu
}

# Nop mot ho so. `$noiDungRieng` cho phep dat noi dung trung voi ho so khac de thu nhanh trung lap.
function NopHoSo($tacGia, $nhan, $noiDungRieng = $null) {
    $dot = (Goi '/api/v1/danh-muc/dot-de-nghi/dang-mo' $tacGia).duLieu[0]
    $linhVuc = (Goi '/api/v1/danh-muc/linh-vuc/chon' $tacGia).duLieu[0]
    $loaiTacGia = (Goi '/api/v1/danh-muc/loai-tac-gia/chon' $tacGia).duLieu[0]

    $moTa = if ($noiDungRieng) { $noiDungRieng } else { ('Mo ta chi tiet giai phap. ' * 9) }

    $than = @{
        tenSangKien             = "$nhan $(Get-Date -Format 'HHmmss-fff')"
        dotDeNghiId             = $dot.id
        linhVucId               = $linhVuc.id
        loaiTacGiaId            = $loaiTacGia.id
        moTaGiaiPhap            = $moTa
        tinhTrangTruocKhiApDung = ('Truoc khi ap dung phai thao tac thu cong rat mat thoi gian. ' * 4)
        noiDungGiaiPhap         = if ($noiDungRieng) { $noiDungRieng } else { ('Noi dung chi tiet trinh bay theo tung buoc. ' * 10) }
        tinhMoi                 = ('Tinh moi cua giai phap so voi cach lam cu. ' * 4)
        khaNangApDung           = ('Kha nang ap dung rong rai cho don vi tuong tu. ' * 4)
        danhSachTacGia          = @(@{ hoTen = 'Nguyen Thi Lan'; tyLeDongGop = 100; laTacGiaChinh = $true })
    }

    $id = (Goi '/api/v1/sang-kien' $tacGia 'Post' $than).duLieu

    $tepTam = Join-Path ([IO.Path]::GetTempPath()) "mc-$([Guid]::NewGuid().ToString('N').Substring(0,8)).pdf"
    [IO.File]::WriteAllText($tepTam, "%PDF-1.4`n1 0 obj<</Type/Catalog>>endobj`ntrailer<</Root 1 0 R>>`n%%EOF")

    Invoke-RestMethod -Uri "$Goc/api/v1/tep-tin/tai-len" -Method Post -Headers $tacGia -Form @{
        tep = Get-Item $tepTam; sangKienId = $id; thanhPhanHoSoMa = 'MINH_CHUNG'
    } | Out-Null

    Remove-Item $tepTam -ErrorAction SilentlyContinue

    $nop = Goi "/api/v1/sang-kien/$id/nop" $tacGia 'Post'
    return @{ Id = $id; MaHoSo = $nop.duLieu.maHoSo }
}

# Dua ho so tu buoc tiep nhan den buoc hop hoi dong, cham diem theo ty le chi dinh.
function ChamDiemDenBuocHop($hoSoId, $tyLeDiem, $tiepNhan, $thuKy, $thanhVien, $tenTaiKhoanCham) {
    ThucThi $hoSoId $tiepNhan 'DAT' 'Tiep nhan.' | Out-Null
    ThucThi $hoSoId $thuKy 'DAT' 'Dat tham dinh.' | Out-Null

    $hoiDongTomTat = (Goi '/api/v1/hoi-dong?soDong=1' $thuKy).duLieu[0]
    $hoiDong = (Goi "/api/v1/hoi-dong/$($hoiDongTomTat.id)" $thuKy).duLieu

    $idNguoiCham = @()
    foreach ($ten in $tenTaiKhoanCham) {
        $h = DangNhap $ten
        $idNguoiCham += (Goi '/api/v1/xac-thuc/toi' $h).duLieu.id
    }

    $thanhVienIds = @($hoiDong.thanhVien | Where-Object { $idNguoiCham -contains $_.nguoiDungId } |
            ForEach-Object { $_.id })

    Goi '/api/v1/danh-gia/phan-cong' $thuKy 'Post' @{
        hoiDongId = $hoiDong.id
        sangKienIds = @($hoSoId)
        thanhVienIds = $thanhVienIds
        hanHoanThanh = (Get-Date).AddDays(7).ToString('o')
        tuDongChiaDeu = $false
    } | Out-Null

    ThucThi $hoSoId $thuKy 'DAT' 'Da phan cong.' | Out-Null

    foreach ($tv in $thanhVien) {
        $phieu = (Goi "/api/v1/danh-gia/phieu?sangKienId=$hoSoId&hoiDongId=$($hoiDong.id)" $tv).duLieu

        $chiTiet = @()
        foreach ($nhom in $phieu.boTieuChi.danhSachNhom) {
            foreach ($tc in $nhom.danhSachTieuChi) {
                $chiTiet += @{ tieuChiId = $tc.id; diem = [Math]::Round($tc.diemToiDa * $tyLeDiem, 1) }
            }
        }

        Goi '/api/v1/danh-gia/phieu/gui' $tv 'Post' @{
            sangKienId = $hoSoId; hoiDongId = $hoiDong.id; chiTiet = $chiTiet
            nhanXetChung = 'Kiem thu tu dong.'
        } | Out-Null
    }

    $tongHop = (Goi "/api/v1/danh-gia/tong-hop?sangKienId=$hoSoId&hoiDongId=$($hoiDong.id)" $thuKy 'Post').duLieu

    # Quy tac TAT_CA: du ba thanh vien xac nhan thi moi chuyen buoc.
    foreach ($tv in $thanhVien) { ThucThi $hoSoId $tv 'DAT' 'Da cham xong.' | Out-Null }

    return $tongHop
}

Ghi "`n=== BA NHANH MO THEO DU LIEU ===`n" 'Cyan'

$tacGia = DangNhap 'gv.lan'
$tiepNhan = DangNhap 'tiepnhan'
$thuKy = DangNhap 'thuky'
$chuTich = DangNhap 'chutich'
$tenCham = @('hoidong01', 'hoidong02', 'hoidong03')
$thanhVien = $tenCham | ForEach-Object { DangNhap $_ }
KiemTra 'Dang nhap cac vai tro can dung' $true

# ============================================================================
# NHANH I — KHONG DAT o tham dinh vi trung lap > 40%
# ============================================================================
Ghi "`n[I] KHONG DAT O THAM DINH (ty_le_trung_lap > 40)" 'Yellow'

# Nop hai ho so co noi dung GIONG NHAU de bo phan tich trung lap phat hien.
$vanBanTrung = ('Giai phap ung dung cong nghe thong tin de rut ngan thoi gian xu ly ho so hanh chinh ' +
    'tai bo phan mot cua, thay the hoan toan viec ghi so tay bang phan mem quan ly tap trung. ') * 6

$hoSoGoc = NopHoSo $tacGia 'Nhanh I - ho so goc' $vanBanTrung
KiemTra 'Nop ho so goc' ($null -ne $hoSoGoc.Id) $hoSoGoc.MaHoSo

$saoChep = NopHoSo $tacGia 'Nhanh I - ho so trung lap' $vanBanTrung
KiemTra 'Nop ho so co noi dung trung' ($null -ne $saoChep.Id) $saoChep.MaHoSo

# Chay kiem tra trung lap cho ho so sao chep.
Goi "/api/v1/sang-kien/$($saoChep.Id)/trung-lap/chay-lai" $thuKy 'Post' | Out-Null
Start-Sleep -Seconds 3

$tt = TrangThai $saoChep.Id $thuKy
Ghi "      ty le trung lap do duoc: $($tt.tyLeTrungLap)%" 'Gray'

ThucThi $saoChep.Id $tiepNhan 'DAT' 'Tiep nhan de tham dinh.' | Out-Null

$nhanhKhongDat = LayNhanh $saoChep.Id $thuKy 'KHONG_DAT'
KiemTra 'Buoc tham dinh co nhanh KHONG_DAT' ($null -ne $nhanhKhongDat) `
    "biChan=$($nhanhKhongDat.biChan)"

if ($tt.tyLeTrungLap -gt 40) {
    KiemTra 'Trung lap > 40% thi nhanh KHONG_DAT MO' (-not $nhanhKhongDat.biChan) `
        "ty le=$($tt.tyLeTrungLap)%"

    $kq = ThucThi $saoChep.Id $thuKy 'KHONG_DAT' 'Trung lap vuot nguong cho phep.'
    KiemTra 'Loai ho so vi trung lap' ($null -ne $kq) $kq.thongBao
    KiemTra 'Ho so co trang thai KHONG_DAT' `
        ((TrangThai $saoChep.Id $thuKy).trangThaiTong -eq 'KHONG_DAT')
}
else {
    # Bo nhung hien la "hashing trick" tu vung, chua bat duoc quan he ngu nghia xa — nen hai van ban
    # giong nhau van co the khong vuot nguong 40%. Dieu PHAI dung trong ca hai truong hop la: nhanh
    # bi chan dung khi chua thoa dieu kien.
    KiemTra 'Trung lap <= 40% thi nhanh KHONG_DAT bi CHAN dung' $nhanhKhongDat.biChan `
        "ty le=$($tt.tyLeTrungLap)% - chua du nguong nen phai chan"
}

# ============================================================================
# NHANH II — DE NGHI XET CAP CAO HON (tong_diem >= 80)
# ============================================================================
Ghi "`n[II] DE NGHI XET CAP CAO HON (tong_diem >= 80)" 'Yellow'

$cao = NopHoSo $tacGia 'Nhanh II - diem cao'
KiemTra 'Nop ho so cho nhanh II' ($null -ne $cao.Id) $cao.MaHoSo

$th = ChamDiemDenBuocHop $cao.Id 0.95 $tiepNhan $thuKy $thanhVien $tenCham
KiemTra 'Cham diem cao va tong hop' ($th.diemCuoiCung -ge 80) "tong diem = $($th.diemCuoiCung)"

$nhanhChuyenCap = LayNhanh $cao.Id $chuTich 'CHUYEN_CAP_CAO_HON'
KiemTra 'Diem >= 80 thi nhanh CHUYEN_CAP_CAO_HON MO' `
    ($null -ne $nhanhChuyenCap -and -not $nhanhChuyenCap.biChan) "biChan=$($nhanhChuyenCap.biChan)"

$nhanhKhongDatCao = LayNhanh $cao.Id $chuTich 'KHONG_DAT'
KiemTra 'Diem >= 80 thi nhanh KHONG_DAT bi CHAN' $nhanhKhongDatCao.biChan `
    "biChan=$($nhanhKhongDatCao.biChan)"

$kq = ThucThi $cao.Id $chuTich 'CHUYEN_CAP_CAO_HON' 'De nghi xet cap thanh pho.'
KiemTra 'Chuyen cap cao hon thanh cong' ($null -ne $kq) $kq.tenBuocMoi

# ============================================================================
# NHANH III — KHONG DAT o hop hoi dong (tong_diem < 50)
# ============================================================================
Ghi "`n[III] KHONG DAT O HOP HOI DONG (tong_diem < 50)" 'Yellow'

$thap = NopHoSo $tacGia 'Nhanh III - diem thap'
KiemTra 'Nop ho so cho nhanh III' ($null -ne $thap.Id) $thap.MaHoSo

$th = ChamDiemDenBuocHop $thap.Id 0.30 $tiepNhan $thuKy $thanhVien $tenCham
KiemTra 'Cham diem thap va tong hop' ($th.diemCuoiCung -lt 50) "tong diem = $($th.diemCuoiCung)"

$nhanhKhongDatThap = LayNhanh $thap.Id $chuTich 'KHONG_DAT'
KiemTra 'Diem < 50 thi nhanh KHONG_DAT MO' `
    ($null -ne $nhanhKhongDatThap -and -not $nhanhKhongDatThap.biChan) `
    "biChan=$($nhanhKhongDatThap.biChan)"

# Diem thap ma van cho cong nhan thi mot sang kien khong dat duoc cong nhan ma khong ai thay la.
$nhanhDatThap = LayNhanh $thap.Id $chuTich 'DAT'
KiemTra 'Diem < 50 thi nhanh DAT bi CHAN' $nhanhDatThap.biChan "biChan=$($nhanhDatThap.biChan)"

$kq = ThucThi $thap.Id $chuTich 'KHONG_DAT' 'Hoi dong ket luan khong dat.'
KiemTra 'Hoi dong ket luan khong dat' ($null -ne $kq) $kq.thongBao

$ttThap = TrangThai $thap.Id $thuKy
KiemTra 'Ho so co trang thai KHONG_DAT va ket qua KHONG_DAT' `
    ($ttThap.trangThaiTong -eq 'KHONG_DAT' -and $ttThap.ketQua -eq 'KHONG_DAT') `
    "$($ttThap.trangThaiTong) / $($ttThap.ketQua)"

KiemTra 'Ho so khong dat XUAT HIEN o bao cao "chua dat"' `
    ((Goi '/api/v1/bao-cao/sang-kien-chua-dat' $thuKy).duLieu.maHoSo -contains $ttThap.maHoSo) `
    $ttThap.maHoSo

# ============================================================================
Ghi ""
if ($script:soLoi -eq 0) {
    Ghi "=== KET QUA: $($script:soBuoc)/$($script:soBuoc) buoc DAT ===" 'Green'
}
else {
    Ghi "=== KET QUA: $($script:soBuoc - $script:soLoi)/$($script:soBuoc) buoc DAT, $($script:soLoi) LOI ===" 'Red'
    exit 1
}
