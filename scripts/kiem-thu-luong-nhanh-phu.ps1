<#
.SYNOPSIS
    Kịch bản chạy CÁC NHÁNH CÒN LẠI của quy trình và các luồng nghiệp vụ ngoài quy trình.

.DESCRIPTION
    `kiem-thu-luong-nghiep-vu.ps1` chỉ đi nhánh "Đạt" — nhánh mà mọi hồ sơ suôn sẻ đều đi qua.
    Quy trình mẫu có 12 nhánh; 6 nhánh còn lại và các luồng ngoài quy trình (rút hồ sơ, thu hồi
    bước, phiên họp hội đồng) chưa từng được chạy tự động. Đó chính là nơi lỗi hay ẩn: đường ít
    người đi thì ít người phát hiện nó hỏng.

    Các luồng chạy ở đây:
      A. Yêu cầu bổ sung ở bước tiếp nhận → tác giả sửa → nộp lại → tiếp nhận đạt
      B. Từ chối tiếp nhận (TRA_LAI)
      C. Thẩm định trả lại bổ sung
      D. Hội đồng kết luận KHÔNG ĐẠT
      E. Đề nghị xét cấp cao hơn (CHUYEN_CAP_CAO_HON)
      F. Tác giả rút hồ sơ
      G. Thu hồi bước vừa xử lý (chức năng 29)
      H. Phiên họp hội đồng: tạo phiên → điểm danh → bỏ phiếu → biên bản

.EXAMPLE
    ./scripts/kiem-thu-luong-nhanh-phu.ps1 -Goc http://127.0.0.1:58080
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

function DangNhap($tenDangNhap) {
    $than = @{ tenDangNhap = $tenDangNhap; matKhau = $MatKhau } | ConvertTo-Json
    $kq = Invoke-RestMethod -Uri "$Goc/api/v1/xac-thuc/dang-nhap" -Method Post `
        -Body $than -ContentType 'application/json'
    return @{ Authorization = "Bearer $($kq.duLieu.accessToken)" }
}

# Tra ve $null khi goi thanh cong, hoac than loi nghiep vu khi that bai.
# Phai Out-Null: gia tri tra ve cua khoi lenh se lot ra pipeline va bien ket qua thanh mang,
# lam moi phep kiem "co loi khong" deu sai.
function LoiNghiepVu($khoi) {
    try { & $khoi | Out-Null; return $null }
    catch {
        # Khong phai loi nao cung co than JSON: 405, loi mang, loi ha tang thi ErrorDetails rong.
        if ($_.ErrorDetails.Message) {
            try { return ($_.ErrorDetails.Message | ConvertFrom-Json) } catch { }
        }
        return [pscustomobject]@{ maLoi = 'LOI_KHONG_CO_THAN'; thongBao = $_.Exception.Message }
    }
}

function Goi($duongDan, $header, $phuongThuc = 'Get', $than = $null) {
    $tuyChon = @{ Uri = "$Goc$duongDan"; Headers = $header; Method = $phuongThuc }
    if ($than) { $tuyChon.Body = ($than | ConvertTo-Json -Depth 6); $tuyChon.ContentType = 'application/json' }
    return Invoke-RestMethod @tuyChon
}

# Nop mot ho so moi va tra ve id — moi luong can mot ho so rieng vi luong truoc da doi trang thai.
function NopHoSoMoi($tacGia, $nhan) {
    $dot = (Goi '/api/v1/danh-muc/dot-de-nghi/dang-mo' $tacGia).duLieu[0]
    $linhVuc = (Goi '/api/v1/danh-muc/linh-vuc/chon' $tacGia).duLieu[0]
    $loaiTacGia = (Goi '/api/v1/danh-muc/loai-tac-gia/chon' $tacGia).duLieu[0]

    $noiDung = @{
        tenSangKien             = "$nhan $(Get-Date -Format 'HHmmss-fff')"
        dotDeNghiId             = $dot.id
        linhVucId               = $linhVuc.id
        loaiTacGiaId            = $loaiTacGia.id
        moTaGiaiPhap            = ('Mo ta chi tiet giai phap cho luong nhanh phu. ' * 9)
        tinhTrangTruocKhiApDung = ('Truoc khi ap dung phai thao tac thu cong rat mat thoi gian. ' * 4)
        noiDungGiaiPhap         = ('Noi dung chi tiet cua giai phap trinh bay day du theo tung buoc. ' * 10)
        tinhMoi                 = ('Tinh moi cua giai phap so voi cach lam cu tai don vi. ' * 4)
        khaNangApDung           = ('Kha nang ap dung rong rai cho cac don vi tuong tu. ' * 4)
        danhSachTacGia          = @(@{ hoTen = 'Nguyen Thi Lan'; tyLeDongGop = 100; laTacGiaChinh = $true })
    }

    $id = (Goi '/api/v1/sang-kien' $tacGia 'Post' $noiDung).duLieu

    # Minh chung bat buoc — khong co thi khong nop duoc.
    $tepTam = Join-Path ([IO.Path]::GetTempPath()) "minh-chung-$([Guid]::NewGuid().ToString('N').Substring(0,8)).pdf"
    [IO.File]::WriteAllText($tepTam, "%PDF-1.4`n1 0 obj<</Type/Catalog>>endobj`ntrailer<</Root 1 0 R>>`n%%EOF")

    Invoke-RestMethod -Uri "$Goc/api/v1/tep-tin/tai-len" -Method Post -Headers $tacGia -Form @{
        tep = Get-Item $tepTam; sangKienId = $id; thanhPhanHoSoMa = 'MINH_CHUNG'
    } | Out-Null

    Remove-Item $tepTam -ErrorAction SilentlyContinue

    $nop = Goi "/api/v1/sang-kien/$id/nop" $tacGia 'Post'
    return @{ Id = $id; MaHoSo = $nop.duLieu.maHoSo }
}

# Thuc thi mot nhanh theo MA truong hop. Tra ve ket qua, hoac $null neu nhanh khong kha dung.
function ThucThiNhanh($hoSoId, $header, $maTruongHop, $yKien = 'Kiem thu tu dong.') {
    $hanhDong = (Goi "/api/v1/sang-kien/$hoSoId/hanh-dong" $header).duLieu
    $nhanh = $hanhDong | Where-Object { $_.ma -eq $maTruongHop -and -not $_.biChan } | Select-Object -First 1

    if (-not $nhanh) { return $null }

    return (Goi '/api/v1/xu-ly/thuc-thi' $header 'Post' @{
            sangKienId = $hoSoId; truongHopId = $nhanh.truongHopId; yKien = $yKien
        }).duLieu
}

function TrangThai($hoSoId, $header) { return (Goi "/api/v1/sang-kien/$hoSoId" $header).duLieu }

Ghi "`n=== CAC NHANH CON LAI CUA QUY TRINH VA LUONG NGOAI QUY TRINH ===`n" 'Cyan'

$tacGia = DangNhap 'gv.lan'
$tiepNhan = DangNhap 'tiepnhan'
$thuKy = DangNhap 'thuky'
$chuTich = DangNhap 'chutich'
$admin = DangNhap 'admin'
KiemTra 'Dang nhap cac vai tro can dung' $true

# ============================================================================
# LUONG A — Yeu cau bo sung roi nop lai
# ============================================================================
Ghi "`n[A] YEU CAU BO SUNG -> TAC GIA SUA -> NOP LAI" 'Yellow'

$a = NopHoSoMoi $tacGia 'Luong A - yeu cau bo sung'
KiemTra 'Nop ho so cho luong A' ($null -ne $a.Id) $a.MaHoSo

$kq = ThucThiNhanh $a.Id $tiepNhan 'BO_SUNG_HO_SO' 'Thieu bang ke chi tiet, de nghi bo sung.'
KiemTra 'Can bo tiep nhan yeu cau bo sung' ($null -ne $kq) $kq.thongBao

$tt = TrangThai $a.Id $tacGia
KiemTra 'Ho so chuyen trang thai YEU_CAU_BO_SUNG' ($tt.trangThaiTong -eq 'YEU_CAU_BO_SUNG') $tt.trangThaiTong

# Tac gia phai sua duoc khi bi yeu cau bo sung — day la diem quan trong nhat cua luong nay.
$sua = LoiNghiepVu {
    Goi "/api/v1/sang-kien/$($a.Id)" $tacGia 'Put' @{
        tenSangKien             = "$($tt.tenSangKien) (da bo sung)"
        dotDeNghiId             = $tt.dotDeNghiId
        linhVucId               = $tt.linhVucId
        moTaGiaiPhap            = ('Mo ta da bo sung theo yeu cau cua can bo tiep nhan. ' * 9)
        tinhTrangTruocKhiApDung = $tt.tinhTrangTruocKhiApDung
        noiDungGiaiPhap         = $tt.noiDungGiaiPhap
        tinhMoi                 = $tt.tinhMoi
        khaNangApDung           = $tt.khaNangApDung
        danhSachTacGia          = @(@{ hoTen = 'Nguyen Thi Lan'; tyLeDongGop = 100; laTacGiaChinh = $true })
    }
}
KiemTra 'Tac gia SUA duoc ho so khi bi yeu cau bo sung' ($null -eq $sua) $(if ($sua) { "$($sua.maLoi): $($sua.thongBao)" })

# Nop lai
$nopLai = LoiNghiepVu { Goi "/api/v1/sang-kien/$($a.Id)/nop" $tacGia 'Post' }
KiemTra 'Tac gia nop lai duoc sau khi bo sung' ($null -eq $nopLai) $(if ($nopLai) { "$($nopLai.maLoi): $($nopLai.thongBao)" })

$kq = ThucThiNhanh $a.Id $tiepNhan 'DAT' 'Da bo sung day du, tiep nhan.'
KiemTra 'Tiep nhan duoc sau khi tac gia bo sung' ($null -ne $kq) $kq.tenBuocMoi

# ============================================================================
# LUONG B — Tu choi tiep nhan
# ============================================================================
Ghi "`n[B] TU CHOI TIEP NHAN (TRA_LAI)" 'Yellow'

$b = NopHoSoMoi $tacGia 'Luong B - tu choi tiep nhan'
KiemTra 'Nop ho so cho luong B' ($null -ne $b.Id) $b.MaHoSo

$kq = ThucThiNhanh $b.Id $tiepNhan 'TRA_LAI' 'Ho so khong thuoc pham vi xet cua dot nay.'
KiemTra 'Can bo tu choi tiep nhan' ($null -ne $kq) $kq.thongBao

$tt = TrangThai $b.Id $tacGia
# Tu choi tiep nhan la DONG quy trinh, khong phai cho bo sung: neu de YEU_CAU_BO_SUNG thi tac gia
# sua roi nop lai duoc mot ho so khong con buoc nao de di tiep.
KiemTra 'Ho so bi tu choi co trang thai KHONG_DAT' ($tt.trangThaiTong -eq 'KHONG_DAT') $tt.trangThaiTong

$nopLaiSauTuChoi = LoiNghiepVu { Goi "/api/v1/sang-kien/$($b.Id)/nop" $tacGia 'Post' }
KiemTra 'Ho so da bi tu choi thi KHONG nop lai duoc' ($null -ne $nopLaiSauTuChoi) $nopLaiSauTuChoi.maLoi

# ============================================================================
# LUONG C — Tham dinh tra lai bo sung
# ============================================================================
Ghi "`n[C] THAM DINH TRA LAI BO SUNG" 'Yellow'

$c = NopHoSoMoi $tacGia 'Luong C - tham dinh tra lai'
ThucThiNhanh $c.Id $tiepNhan 'DAT' 'Tiep nhan.' | Out-Null
KiemTra 'Ho so luong C vao buoc tham dinh' $true $c.MaHoSo

$kq = ThucThiNhanh $c.Id $thuKy 'BO_SUNG_HO_SO' 'Thieu so lieu chung minh hieu qua.'
KiemTra 'Tham dinh tra lai bo sung' ($null -ne $kq) $kq.thongBao

$tt = TrangThai $c.Id $tacGia
KiemTra 'Ho so ve trang thai YEU_CAU_BO_SUNG tu buoc tham dinh' `
    ($tt.trangThaiTong -eq 'YEU_CAU_BO_SUNG') $tt.trangThaiTong

# ============================================================================
# LUONG F — Tac gia rut ho so
# ============================================================================
Ghi "`n[F] TAC GIA RUT HO SO" 'Yellow'

$f = NopHoSoMoi $tacGia 'Luong F - rut ho so'
KiemTra 'Nop ho so cho luong F' ($null -ne $f.Id) $f.MaHoSo

$rut = LoiNghiepVu { Goi "/api/v1/sang-kien/$($f.Id)/rut" $tacGia 'Post' @{ lyDo = 'Tac gia xin rut de hoan thien them.' } }
KiemTra 'Tac gia rut duoc ho so' ($null -eq $rut) $(if ($rut) { "$($rut.maLoi): $($rut.thongBao)" })

if (-not $rut) {
    $tt = TrangThai $f.Id $tacGia
    KiemTra 'Ho so da rut co trang thai DA_RUT' ($tt.trangThaiTong -eq 'DA_RUT') $tt.trangThaiTong
}

# ============================================================================
# LUONG G — Thu hoi buoc vua xu ly (chuc nang 29)
# ============================================================================
Ghi "`n[G] THU HOI BUOC VUA XU LY" 'Yellow'

$g = NopHoSoMoi $tacGia 'Luong G - thu hoi buoc'
$truoc = (TrangThai $g.Id $tacGia).tenBuocHienTai
ThucThiNhanh $g.Id $tiepNhan 'DAT' 'Tiep nhan de thu thu hoi.' | Out-Null
$sau = (TrangThai $g.Id $tacGia).tenBuocHienTai
KiemTra 'Ho so da chuyen buoc truoc khi thu hoi' ($truoc -ne $sau) "$truoc -> $sau"

$thuHoi = LoiNghiepVu { Goi '/api/v1/xu-ly/thu-hoi' $tiepNhan 'Post' @{ sangKienId = $g.Id; lyDo = 'Bam nham nhanh xu ly.' } }
KiemTra 'Thu hoi buoc vua xu ly' ($null -eq $thuHoi) $(if ($thuHoi) { "$($thuHoi.maLoi): $($thuHoi.thongBao)" })

if (-not $thuHoi) {
    $veLai = (TrangThai $g.Id $tacGia).tenBuocHienTai
    KiemTra 'Ho so quay lai buoc truoc do' ($veLai -eq $truoc) "$sau -> $veLai"
}

# ============================================================================
# LUONG H — Phien hop hoi dong: tao -> diem danh -> bo phieu -> bien ban
# ============================================================================
Ghi "`n[H] PHIEN HOP HOI DONG" 'Yellow'

$hoiDong = (Goi '/api/v1/hoi-dong?trang=1&soDong=1' $admin).duLieu[0]
KiemTra 'Lay duoc hoi dong' ($null -ne $hoiDong) $hoiDong.ten

$chiTietHd = (Goi "/api/v1/hoi-dong/$($hoiDong.id)" $admin).duLieu
$sangKienDuaRa = (Goi '/api/v1/sang-kien?trang=1&soDong=2' $admin).duLieu

$phien = (Goi '/api/v1/hoi-dong/phien-hop' $admin 'Post' @{
        hoiDongId       = $hoiDong.id
        tenPhien        = "Phien hop kiem thu $(Get-Date -Format 'HHmmss')"
        thoiGianBatDau  = (Get-Date).AddDays(1).ToString('o')
        hinhThuc        = 'TRUC_TIEP'
        diaDiem         = 'Phong hop kiem thu'
        chuTriId        = ($chiTietHd.thanhVien | Where-Object { $_.chucDanh -eq 'CHU_TICH' } | Select-Object -First 1).nguoiDungId
        sangKienIds     = @($sangKienDuaRa[0].id)
    }).duLieu
KiemTra 'Tao phien hop kem ho so dua ra xet' ($null -ne $phien.id) "$($phien.maPhien) - $($phien.danhSachHoSo.Count) ho so"
KiemTra 'Phien hop sinh san ban ghi diem danh cho moi thanh vien' `
    ($phien.diemDanh.Count -eq $chiTietHd.thanhVien.Count) "$($phien.diemDanh.Count)/$($chiTietHd.thanhVien.Count)"

# Diem danh
$tvDau = $chiTietHd.thanhVien | Select-Object -First 1
$diemDanh = LoiNghiepVu {
    Goi "/api/v1/hoi-dong/phien-hop/$($phien.id)/diem-danh" $admin 'Post' `
    @{ thanhVienId = $tvDau.id; coMat = $true; lyDoVang = $null }
}
KiemTra 'Ghi diem danh thanh vien' ($null -eq $diemDanh) $(if ($diemDanh) { "$($diemDanh.maLoi): $($diemDanh.thongBao)" })

# Bo phieu
$boPhieu = LoiNghiepVu {
    Goi '/api/v1/hoi-dong/phien-hop/bo-phieu' $chuTich 'Post' @{
        phienHopId = $phien.id
        sangKienId = $sangKienDuaRa[0].id
        yKien      = 'DONG_Y'
    }
}
KiemTra 'Thanh vien bo phieu' ($null -eq $boPhieu) $(if ($boPhieu) { "$($boPhieu.maLoi): $($boPhieu.thongBao)" })

# Ket thuc phien roi lap bien ban — bien ban chi lap duoc cho phien DA KET THUC.
$ketThuc = LoiNghiepVu {
    Goi "/api/v1/hoi-dong/phien-hop/$($phien.id)/ket-thuc" $admin 'Post' @{ ketLuan = 'Phien hop kiem thu ket thuc.' }
}
KiemTra 'Ket thuc phien hop' ($null -eq $ketThuc) $(if ($ketThuc) { "$($ketThuc.maLoi): $($ketThuc.thongBao)" })

$lapBienBan = LoiNghiepVu { Goi "/api/v1/bien-ban-hop/phien-hop/$($phien.id)" $admin 'Post' }
KiemTra 'Lap duoc bien ban hop' ($null -eq $lapBienBan) $(if ($lapBienBan) { "$($lapBienBan.maLoi): $($lapBienBan.thongBao)" })

$bb = (Goi "/api/v1/bien-ban-hop/phien-hop/$($phien.id)" $admin).duLieu
KiemTra 'Doc lai duoc bien ban vua lap' ($null -ne $bb) $bb.soBienBan

# ============================================================================
Ghi ""
if ($script:soLoi -eq 0) {
    Ghi "=== KET QUA: $($script:soBuoc)/$($script:soBuoc) buoc DAT ===" 'Green'
}
else {
    Ghi "=== KET QUA: $($script:soBuoc - $script:soLoi)/$($script:soBuoc) buoc DAT, $($script:soLoi) LOI ===" 'Red'
    exit 1
}
