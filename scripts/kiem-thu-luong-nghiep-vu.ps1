<#
.SYNOPSIS
    Kịch bản kiểm thử luồng nghiệp vụ end-to-end qua API thật (Mục 11 đặc tả).

.DESCRIPTION
    Nộp hồ sơ → Tiếp nhận → Thẩm định → Phân công chấm → 3 thành viên chấm
    → Tổng hợp điểm → Họp hội đồng kết luận Đạt → Ban hành quyết định.

.EXAMPLE
    ./scripts/kiem-thu-luong-nghiep-vu.ps1 -Goc http://localhost:5299
#>
param(
    [string]$Goc = 'http://localhost:5299',
    [string]$MatKhau = 'Sk@2026'
)

$ErrorActionPreference = 'Stop'
$script:soBuoc = 0
$script:soLoi = 0

function Ghi($thongDiep, $mau = 'White') {
    Write-Host $thongDiep -ForegroundColor $mau
}

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

function DangNhap($tenDangNhap) {
    $than = @{ tenDangNhap = $tenDangNhap; matKhau = $MatKhau } | ConvertTo-Json
    $kq = Invoke-RestMethod -Uri "$Goc/api/v1/xac-thuc/dang-nhap" -Method Post `
        -Body $than -ContentType 'application/json'
    return @{ Authorization = "Bearer $($kq.duLieu.accessToken)" }
}

function LoiNghiepVu($khoi) {
    try { & $khoi; return $null }
    catch { return ($_.ErrorDetails.Message | ConvertFrom-Json) }
}

Ghi "`n=== KIEM THU LUONG NGHIEP VU END-TO-END ===`n" 'Cyan'

# --- Chuan bi tai khoan ------------------------------------------------------
$tacGia = DangNhap 'gv.lan'
$tiepNhan = DangNhap 'tiepnhan'
$thuKy = DangNhap 'thuky'
$chuTich = DangNhap 'chutich'
$lanhDao = DangNhap 'lanhdao'
$thanhVien = @('hoidong01', 'hoidong02', 'hoidong03') | ForEach-Object { DangNhap $_ }
KiemTra 'Dang nhap 8 tai khoan voi vai tro khac nhau' $true

# --- 1. Tac gia tao va nop ho so --------------------------------------------
Ghi "`n[1] TAC GIA NOP HO SO" 'Yellow'

$dot = (Invoke-RestMethod -Uri "$Goc/api/v1/danh-muc/dot-de-nghi/dang-mo" -Headers $tacGia).duLieu[0]
$linhVuc = (Invoke-RestMethod -Uri "$Goc/api/v1/danh-muc/linh-vuc/chon" -Headers $tacGia).duLieu[0]
$loaiTacGia = (Invoke-RestMethod -Uri "$Goc/api/v1/danh-muc/loai-tac-gia/chon" -Headers $tacGia).duLieu[0]
KiemTra 'Lay duoc dot dang mo' ($null -ne $dot) $dot.ten

$noiDung = @{
    tenSangKien             = "Kiem thu tu dong luong nghiep vu $(Get-Date -Format 'yyyyMMdd-HHmmss')"
    dotDeNghiId             = $dot.id
    linhVucId               = $linhVuc.id
    loaiTacGiaId            = $loaiTacGia.id
    moTaGiaiPhap            = ('Mo ta chi tiet giai phap kiem thu tu dong cho he thong sang kien. ' * 8)
    tinhTrangTruocKhiApDung = ('Truoc khi ap dung phai thao tac thu cong rat mat thoi gian. ' * 4)
    noiDungGiaiPhap         = ('Noi dung chi tiet cua giai phap duoc trinh bay day du theo tung buoc. ' * 10)
    tinhMoi                 = ('Tinh moi cua giai phap so voi cach lam cu tai don vi. ' * 4)
    khaNangApDung           = ('Kha nang ap dung rong rai cho cac don vi tuong tu. ' * 4)
    danhSachTacGia          = @(@{ hoTen = 'Nguyen Thi Lan'; tyLeDongGop = 100; laTacGiaChinh = $true })
} | ConvertTo-Json -Depth 5

$hoSoId = (Invoke-RestMethod -Uri "$Goc/api/v1/sang-kien" -Method Post -Body $noiDung `
        -ContentType 'application/json' -Headers $tacGia).duLieu
KiemTra 'Tao ho so nhap' ($null -ne $hoSoId) $hoSoId

# Nop khi thieu minh chung -> phai bi chan
$loi = LoiNghiepVu { Invoke-RestMethod -Uri "$Goc/api/v1/sang-kien/$hoSoId/nop" -Method Post -Headers $tacGia }
KiemTra 'Chan nop khi thieu thanh phan bat buoc' `
    ($loi.maLoi -eq 'THIEU_THANH_PHAN_BAT_BUOC') $loi.thongBao

# Tai len minh chung (PDF toi thieu hop le)
$tepTam = Join-Path ([IO.Path]::GetTempPath()) 'minh-chung.pdf'
$noiDungPdf = "%PDF-1.4`n1 0 obj<</Type/Catalog>>endobj`ntrailer<</Root 1 0 R>>`n%%EOF"
[IO.File]::WriteAllText($tepTam, $noiDungPdf)

$form = @{
    tep              = Get-Item $tepTam
    sangKienId       = $hoSoId
    thanhPhanHoSoMa  = 'MINH_CHUNG'
    moTa             = 'Tep minh chung kiem thu'
}
$tep = Invoke-RestMethod -Uri "$Goc/api/v1/tep-tin/tai-len" -Method Post -Form $form -Headers $tacGia
KiemTra 'Tai len tep minh chung (kiem tra magic number)' $tep.thanhCong $tep.duLieu.tenGoc

$nop = Invoke-RestMethod -Uri "$Goc/api/v1/sang-kien/$hoSoId/nop" -Method Post -Headers $tacGia
KiemTra 'Nop ho so thanh cong' $nop.thanhCong "$($nop.duLieu.maHoSo) -> $($nop.duLieu.tenBuocHienTai)"

# --- 2. Can bo tiep nhan -----------------------------------------------------
Ghi "`n[2] CAN BO TIEP NHAN" 'Yellow'

$hanhDong = (Invoke-RestMethod -Uri "$Goc/api/v1/sang-kien/$hoSoId/hanh-dong" -Headers $tiepNhan).duLieu
KiemTra 'Can bo tiep nhan thay hanh dong dong (khong hardcode)' `
    ($hanhDong.Count -ge 1) ($hanhDong.ten -join ', ')

$hanhDongTacGia = (Invoke-RestMethod -Uri "$Goc/api/v1/sang-kien/$hoSoId/hanh-dong" -Headers $tacGia).duLieu
KiemTra 'Tac gia KHONG thay hanh dong xu ly (phan quyen tac nhan)' ($hanhDongTacGia.Count -eq 0)

$thTiepNhan = $hanhDong | Where-Object { $_.ma -eq 'DAT' } | Select-Object -First 1
$than = @{ sangKienId = $hoSoId; truongHopId = $thTiepNhan.truongHopId; yKien = 'Ho so hop le, du dieu kien.' } | ConvertTo-Json
$kq = Invoke-RestMethod -Uri "$Goc/api/v1/xu-ly/thuc-thi" -Method Post -Body $than `
    -ContentType 'application/json' -Headers $tiepNhan
KiemTra 'Tiep nhan ho so -> chuyen buoc Tham dinh' `
    ($kq.duLieu.tenBuocMoi -like '*Thẩm định*') $kq.duLieu.tenBuocMoi

# --- 3. Thu ky tham dinh + phan cong ----------------------------------------
Ghi "`n[3] THU KY THAM DINH VA PHAN CONG CHAM" 'Yellow'

$hanhDong = (Invoke-RestMethod -Uri "$Goc/api/v1/sang-kien/$hoSoId/hanh-dong" -Headers $thuKy).duLieu
$thDat = $hanhDong | Where-Object { $_.ma -eq 'DAT' } | Select-Object -First 1
$than = @{ sangKienId = $hoSoId; truongHopId = $thDat.truongHopId; yKien = 'Dat tham dinh so bo.' } | ConvertTo-Json
$kq = Invoke-RestMethod -Uri "$Goc/api/v1/xu-ly/thuc-thi" -Method Post -Body $than `
    -ContentType 'application/json' -Headers $thuKy
KiemTra 'Tham dinh dat -> chuyen buoc Phan cong' `
    ($kq.duLieu.tenBuocMoi -like '*Phân công*') $kq.duLieu.tenBuocMoi

$hoiDongTomTat = (Invoke-RestMethod -Uri "$Goc/api/v1/hoi-dong?soDong=1" -Headers $thuKy).duLieu[0]
$hoiDong = (Invoke-RestMethod -Uri "$Goc/api/v1/hoi-dong/$($hoiDongTomTat.id)" -Headers $thuKy).duLieu
KiemTra 'Lay duoc hoi dong' ($null -ne $hoiDong) "$($hoiDong.ten) - $($hoiDong.thanhVien.Count) thanh vien"

# Chi phan cong dung 3 thanh vien se cham (bang so thanh vien toi thieu cua hoi dong).
$tenTaiKhoanCham = @('hoidong01', 'hoidong02', 'hoidong03')
$nguoiDungCham = @{}
foreach ($ten in $tenTaiKhoanCham) {
    $h = DangNhap $ten
    $nguoiDungCham[$ten] = (Invoke-RestMethod -Uri "$Goc/api/v1/xac-thuc/toi" -Headers $h).duLieu.id
}
$thanhVienIds = $hoiDong.thanhVien |
    Where-Object { $nguoiDungCham.Values -contains $_.nguoiDungId } |
    ForEach-Object { $_.id }
KiemTra 'Xac dinh 3 thanh vien cham diem' ($thanhVienIds.Count -eq 3)

$than = @{
    hoiDongId      = $hoiDong.id
    sangKienIds    = @($hoSoId)
    thanhVienIds   = @($thanhVienIds)
    hanHoanThanh   = (Get-Date).AddDays(7).ToString('o')
    tuDongChiaDeu  = $false
} | ConvertTo-Json
$pc = Invoke-RestMethod -Uri "$Goc/api/v1/danh-gia/phan-cong" -Method Post -Body $than `
    -ContentType 'application/json' -Headers $thuKy
KiemTra 'Phan cong cham diem cho dung 3 thanh vien' `
    ($pc.duLieu.soLuotPhanCong -eq 3) "$($pc.duLieu.soLuotPhanCong) luot"

$hanhDong = (Invoke-RestMethod -Uri "$Goc/api/v1/sang-kien/$hoSoId/hanh-dong" -Headers $thuKy).duLieu
$thDat = $hanhDong | Where-Object { $_.ma -eq 'DAT' } | Select-Object -First 1
$than = @{ sangKienId = $hoSoId; truongHopId = $thDat.truongHopId; yKien = 'Da phan cong cham.' } | ConvertTo-Json
$kq = Invoke-RestMethod -Uri "$Goc/api/v1/xu-ly/thuc-thi" -Method Post -Body $than `
    -ContentType 'application/json' -Headers $thuKy
KiemTra 'Chuyen sang buoc Cham diem hoi dong' `
    ($kq.duLieu.tenBuocMoi -like '*Chấm điểm*') $kq.duLieu.tenBuocMoi

# --- 4. Thanh vien hoi dong cham diem ---------------------------------------
Ghi "`n[4] HOI DONG CHAM DIEM (quy tac TAT_CA)" 'Yellow'

$soDaCham = 0
foreach ($tv in $thanhVien) {
    $phieu = (Invoke-RestMethod -Uri "$Goc/api/v1/danh-gia/phieu?sangKienId=$hoSoId&hoiDongId=$($hoiDong.id)" `
            -Headers $tv).duLieu

    $chiTiet = @()
    foreach ($nhom in $phieu.boTieuChi.danhSachNhom) {
        foreach ($tc in $nhom.danhSachTieuChi) {
            # Cham 85% diem toi da de tong diem vuot nguong Dat (>= 50).
            $chiTiet += @{ tieuChiId = $tc.id; diem = [Math]::Round($tc.diemToiDa * 0.85, 1) }
        }
    }

    $than = @{
        sangKienId   = $hoSoId
        hoiDongId    = $hoiDong.id
        chiTiet      = $chiTiet
        nhanXetChung = 'Giai phap co tinh moi va kha nang ap dung tot.'
    } | ConvertTo-Json -Depth 5

    $gui = Invoke-RestMethod -Uri "$Goc/api/v1/danh-gia/phieu/gui" -Method Post -Body $than `
        -ContentType 'application/json' -Headers $tv
    $soDaCham++
    Ghi "      - Thanh vien $soDaCham cham: $($gui.duLieu.tongDiem) diem" 'Gray'
}
KiemTra 'Ba thanh vien gui phieu cham' ($soDaCham -eq 3)

$tongHop = (Invoke-RestMethod -Uri "$Goc/api/v1/danh-gia/tong-hop?sangKienId=$hoSoId&hoiDongId=$($hoiDong.id)" `
        -Method Post -Headers $thuKy).duLieu
KiemTra 'Tong hop diem hoi dong' ($tongHop.soPhieu -eq 3) `
    "TB=$($tongHop.diemTrungBinh) | cuoi=$($tongHop.diemCuoiCung) | dat=$($tongHop.dat) | muc=$($tongHop.tenMucCongNhan)"

# Chuyen buoc cham diem: quy tac TAT_CA can du 3 thanh vien thuc thi
$soThucThi = 0
foreach ($tv in $thanhVien) {
    $hanhDong = (Invoke-RestMethod -Uri "$Goc/api/v1/sang-kien/$hoSoId/hanh-dong" -Headers $tv).duLieu
    $th = $hanhDong | Where-Object { $_.ma -eq 'DAT' } | Select-Object -First 1
    if (-not $th) { continue }

    $than = @{ sangKienId = $hoSoId; truongHopId = $th.truongHopId; yKien = 'Da cham xong.' } | ConvertTo-Json
    $kq = Invoke-RestMethod -Uri "$Goc/api/v1/xu-ly/thuc-thi" -Method Post -Body $than `
        -ContentType 'application/json' -Headers $tv
    $soThucThi++

    Ghi "      - Thanh vien $soThucThi xac nhan: choThemTacNhan=$($kq.duLieu.choThemTacNhan) ($($kq.duLieu.soTacNhanDaXuLy)/$($kq.duLieu.soTacNhanCanThiet))" 'Gray'

    if ($soThucThi -eq 1) {
        KiemTra 'Quy tac TAT_CA: nguoi dau tien CHUA lam chuyen buoc' `
            ($kq.duLieu.choThemTacNhan -eq $true) "$($kq.duLieu.soTacNhanDaXuLy)/$($kq.duLieu.soTacNhanCanThiet)"
    }
}
KiemTra 'Quy tac TAT_CA: chuyen buoc khi du tac nhan' `
    ($kq.duLieu.choThemTacNhan -eq $false) "buoc moi = $($kq.duLieu.tenBuocMoi)"

# --- 5. Chu tich hoi dong ket luan ------------------------------------------
Ghi "`n[5] CHU TICH HOI DONG KET LUAN" 'Yellow'

$hanhDong = (Invoke-RestMethod -Uri "$Goc/api/v1/sang-kien/$hoSoId/hanh-dong" -Headers $chuTich).duLieu
Ghi "      Hanh dong kha dung: $(($hanhDong | ForEach-Object { "$($_.ten)$(if($_.biChan){' (bi chan)'})" }) -join ', ')" 'Gray'

$thDat = $hanhDong | Where-Object { $_.ma -eq 'DAT' -and -not $_.biChan } | Select-Object -First 1
KiemTra 'Dieu kien tong_diem >= 50 mo khoa nhanh "Dat"' ($null -ne $thDat)

$than = @{ sangKienId = $hoSoId; truongHopId = $thDat.truongHopId; yKien = 'Hoi dong thong nhat cong nhan.' } | ConvertTo-Json
$kq = Invoke-RestMethod -Uri "$Goc/api/v1/xu-ly/thuc-thi" -Method Post -Body $than `
    -ContentType 'application/json' -Headers $chuTich
KiemTra 'Ket luan Dat -> chuyen buoc Ban hanh quyet dinh' `
    ($kq.duLieu.tenBuocMoi -like '*quyết định*') $kq.duLieu.tenBuocMoi

# --- 6. Lanh dao ban hanh quyet dinh ----------------------------------------
Ghi "`n[6] LANH DAO BAN HANH QUYET DINH" 'Yellow'

$hanhDong = (Invoke-RestMethod -Uri "$Goc/api/v1/sang-kien/$hoSoId/hanh-dong" -Headers $lanhDao).duLieu
$th = $hanhDong | Where-Object { -not $_.biChan } | Select-Object -First 1
$than = @{ sangKienId = $hoSoId; truongHopId = $th.truongHopId; yKien = 'Dong y ban hanh quyet dinh cong nhan.' } | ConvertTo-Json
$kq = Invoke-RestMethod -Uri "$Goc/api/v1/xu-ly/thuc-thi" -Method Post -Body $than `
    -ContentType 'application/json' -Headers $lanhDao
KiemTra 'Ket thuc quy trinh' $kq.duLieu.daKetThucQuyTrinh "trang thai = $($kq.duLieu.trangThaiTongMoi)"

$ct = (Invoke-RestMethod -Uri "$Goc/api/v1/sang-kien/$hoSoId" -Headers $lanhDao).duLieu
KiemTra 'Ho so o trang thai DA_PHE_DUYET' ($ct.trangThaiTong -eq 'DA_PHE_DUYET')
KiemTra 'Ket qua = DAT' ($ct.ketQua -eq 'DAT')
KiemTra 'Co diem tong hop' ($null -ne $ct.tongDiem) "$($ct.tongDiem) diem"

# --- 7. Tien do va bao cao ---------------------------------------------------
Ghi "`n[7] TIEN DO VA BAO CAO" 'Yellow'

$tienDo = (Invoke-RestMethod -Uri "$Goc/api/v1/sang-kien/$hoSoId/tien-do" -Headers $lanhDao).duLieu
KiemTra 'Timeline tien do day du cac buoc' ($tienDo.Count -ge 6) "$($tienDo.Count) moc"

$lichSu = (Invoke-RestMethod -Uri "$Goc/api/v1/sang-kien/$hoSoId/lich-su" -Headers $lanhDao).duLieu
KiemTra 'Lich su chinh sua duoc ghi nhan' ($lichSu.Count -ge 2) "$($lichSu.Count) ban ghi"

$bc = (Invoke-RestMethod -Uri "$Goc/api/v1/bao-cao/sang-kien-dat" -Headers $lanhDao).duLieu
KiemTra 'Bao cao 38 - sang kien dat' ($bc.Count -ge 1) "$($bc.Count) ho so"

$bcDv = (Invoke-RestMethod -Uri "$Goc/api/v1/bao-cao/theo-don-vi" -Headers $lanhDao).duLieu
KiemTra 'Bao cao 40 - theo don vi' ($bcDv.Count -ge 1) "$($bcDv.Count) don vi"

$tq = (Invoke-RestMethod -Uri "$Goc/api/v1/bao-cao/tong-quan" -Headers $lanhDao).duLieu
KiemTra 'Dashboard tong quan' ($tq.tongHoSo -gt 0) `
    "tong=$($tq.tongHoSo) dat=$($tq.hoSoDat) tyLeDat=$($tq.tyLeDat)%"

# --- Ket qua ----------------------------------------------------------------
Ghi "`n=== KET QUA: $($script:soBuoc - $script:soLoi)/$($script:soBuoc) buoc DAT ===`n" `
    $(if ($script:soLoi -eq 0) { 'Green' } else { 'Red' })

exit $script:soLoi
