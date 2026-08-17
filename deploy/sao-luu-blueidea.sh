#!/usr/bin/env bash
#
# Sao lưu và khôi phục dữ liệu BlueIdea.
#
# Yêu cầu Chương V mục 3.4: "Thiết lập chính sách sao lưu định kỳ và hỗ trợ khôi phục dữ liệu
# theo từng thời điểm, bảo đảm an toàn trước các rủi ro mất mát dữ liệu."
#
# Sao lưu gồm HAI phần, thiếu một phần là khôi phục ra hệ thống hỏng:
#   1. Cơ sở dữ liệu  (pg_dump định dạng custom, nén sẵn, khôi phục chọn lọc được)
#   2. Kho tệp đính kèm (tar.gz) — hồ sơ trong CSDL trỏ tới tệp; mất tệp thì hồ sơ rỗng ruột
#
# Cách dùng:
#   ./sao-luu-blueidea.sh sao-luu              # tạo bản sao lưu mới
#   ./sao-luu-blueidea.sh liet-ke              # liệt kê các bản đang có
#   ./sao-luu-blueidea.sh khoi-phuc <thư-mục>  # khôi phục từ một bản
#   ./sao-luu-blueidea.sh don-dep              # xoá bản cũ quá hạn giữ
#
set -euo pipefail

THU_MUC_GOC="${THU_MUC_SAO_LUU:-/var/backups/blueidea}"
SO_NGAY_GIU="${SO_NGAY_GIU_SAO_LUU:-30}"

CONTAINER_DB="${CONTAINER_DB:-blueidea-postgres}"
CONTAINER_API="${CONTAINER_API:-blueidea-api}"
TEN_CSDL="${TEN_CSDL:-blueidea}"
NGUOI_DUNG_DB="${NGUOI_DUNG_DB:-blueidea}"
THU_MUC_TEP="${THU_MUC_TEP:-/app/du-lieu/tep-tin}"

mau_xanh() { printf '\033[0;32m%s\033[0m\n' "$1"; }
mau_do()   { printf '\033[0;31m%s\033[0m\n' "$1"; }
mau_vang() { printf '\033[0;33m%s\033[0m\n' "$1"; }

kiem_tra_container() {
  if ! docker ps --format '{{.Names}}' | grep -qx "$1"; then
    mau_do "Không tìm thấy container đang chạy: $1"
    exit 1
  fi
}

sao_luu() {
  kiem_tra_container "$CONTAINER_DB"

  local dau_thoi_gian thu_muc
  dau_thoi_gian="$(date +%Y%m%d-%H%M%S)"
  thu_muc="$THU_MUC_GOC/$dau_thoi_gian"

  mkdir -p "$thu_muc"
  mau_xanh "Sao lưu vào: $thu_muc"

  # --- 1. Cơ sở dữ liệu -----------------------------------------------------
  # Định dạng custom (-Fc): nén sẵn, và pg_restore khôi phục được từng bảng riêng
  # thay vì phải nạp lại toàn bộ.
  echo "  [1/3] Cơ sở dữ liệu..."
  docker exec "$CONTAINER_DB" pg_dump \
    -U "$NGUOI_DUNG_DB" -d "$TEN_CSDL" -Fc --no-owner --no-privileges \
    > "$thu_muc/csdl.dump"

  # --- 2. Kho tệp đính kèm --------------------------------------------------
  echo "  [2/3] Kho tệp đính kèm..."
  if docker ps --format '{{.Names}}' | grep -qx "$CONTAINER_API"; then
    # Đường dẫn nằm TRONG chuỗi lệnh của sh -c chứ không phải làm tham số cho docker:
    # Git Bash trên Windows tự đổi tham số dạng /app/... thành đường dẫn Windows, khiến tar
    # chạy sai thư mục và bản sao lưu tệp rỗng mà KHÔNG báo lỗi — mất tệp mà không ai biết.
    if docker exec "$CONTAINER_API" sh -c "tar czf - -C '$THU_MUC_TEP' ." > "$thu_muc/tep-tin.tar.gz"; then
      # Đọc gói qua stdin chứ không truyền đường dẫn: GNU tar hiểu tiền tố dạng "C:" là
      # tên máy chủ từ xa và bỏ cuộc, khiến khâu đếm báo 0 tệp dù gói hoàn toàn bình thường.
      local so_tep_trong_goi
      so_tep_trong_goi="$(tar tzf - < "$thu_muc/tep-tin.tar.gz" 2>/dev/null | grep -c -v '/$' || true)"
      echo "        $so_tep_trong_goi tệp"
    else
      mau_do "  Sao lưu kho tệp THẤT BẠI — bản sao lưu này không khôi phục đủ."
      rm -f "$thu_muc/tep-tin.tar.gz"
    fi
  else
    mau_vang "  (container API không chạy — bỏ qua kho tệp)"
  fi

  # --- 3. Siêu dữ liệu để biết bản sao lưu này là gì -------------------------
  echo "  [3/3] Siêu dữ liệu..."
  {
    echo "thoi_diem=$(date -Iseconds)"
    echo "csdl=$TEN_CSDL"
    echo "phien_ban_postgres=$(docker exec "$CONTAINER_DB" psql -U "$NGUOI_DUNG_DB" -d "$TEN_CSDL" -t -A -c 'SHOW server_version;')"
    echo "so_ho_so=$(docker exec "$CONTAINER_DB" psql -U "$NGUOI_DUNG_DB" -d "$TEN_CSDL" -t -A -c 'SELECT count(*) FROM sang_kien;')"
    echo "so_tep=$(docker exec "$CONTAINER_DB" psql -U "$NGUOI_DUNG_DB" -d "$TEN_CSDL" -t -A -c 'SELECT count(*) FROM tep_tin;')"
    echo "so_nguoi_dung=$(docker exec "$CONTAINER_DB" psql -U "$NGUOI_DUNG_DB" -d "$TEN_CSDL" -t -A -c 'SELECT count(*) FROM nguoi_dung;')"
  } > "$thu_muc/thong-tin.txt"

  # Checksum để phát hiện bản sao lưu hỏng TRƯỚC khi cần dùng đến nó.
  ( cd "$thu_muc" && sha256sum ./* > sha256.txt 2>/dev/null || true )

  mau_xanh "Xong. Dung lượng: $(du -sh "$thu_muc" | cut -f1)"
  cat "$thu_muc/thong-tin.txt" | sed 's/^/  /'
}

liet_ke() {
  if [ ! -d "$THU_MUC_GOC" ]; then
    mau_vang "Chưa có bản sao lưu nào tại $THU_MUC_GOC"
    return
  fi

  printf '%-20s %-10s %s\n' "BẢN SAO LƯU" "DUNG LƯỢNG" "SỐ HỒ SƠ"

  for d in "$THU_MUC_GOC"/*/; do
    [ -d "$d" ] || continue
    local ten so
    ten="$(basename "$d")"
    so="$(grep -E '^so_ho_so=' "$d/thong-tin.txt" 2>/dev/null | cut -d= -f2 || echo '?')"
    printf '%-20s %-10s %s\n' "$ten" "$(du -sh "$d" | cut -f1)" "$so"
  done
}

khoi_phuc() {
  local thu_muc="$1"

  [ -d "$thu_muc" ] || thu_muc="$THU_MUC_GOC/$thu_muc"

  if [ ! -f "$thu_muc/csdl.dump" ]; then
    mau_do "Không tìm thấy $thu_muc/csdl.dump"
    exit 1
  fi

  kiem_tra_container "$CONTAINER_DB"

  mau_vang "CẢNH BÁO: thao tác này GHI ĐÈ toàn bộ dữ liệu hiện tại của CSDL '$TEN_CSDL'."
  cat "$thu_muc/thong-tin.txt" 2>/dev/null | sed 's/^/  /'
  printf 'Gõ chính xác "KHOI-PHUC" để tiếp tục: '
  read -r xac_nhan

  if [ "$xac_nhan" != "KHOI-PHUC" ]; then
    mau_do "Đã huỷ."
    exit 1
  fi

  # Kiểm tra toàn vẹn trước khi động vào dữ liệu đang chạy.
  if [ -f "$thu_muc/sha256.txt" ]; then
    echo "Kiểm tra toàn vẹn bản sao lưu..."
    ( cd "$thu_muc" && sha256sum -c sha256.txt --quiet ) \
      || { mau_do "Bản sao lưu hỏng — dừng lại, không khôi phục."; exit 1; }
  fi

  echo "[1/2] Khôi phục cơ sở dữ liệu..."
  # --clean --if-exists: xoá đối tượng cũ trước khi nạp, tránh lỗi trùng tên.
  docker exec -i "$CONTAINER_DB" pg_restore \
    -U "$NGUOI_DUNG_DB" -d "$TEN_CSDL" --clean --if-exists --no-owner --no-privileges \
    < "$thu_muc/csdl.dump"

  if [ -f "$thu_muc/tep-tin.tar.gz" ] && docker ps --format '{{.Names}}' | grep -qx "$CONTAINER_API"; then
    echo "[2/2] Khôi phục kho tệp..."
    docker exec -i "$CONTAINER_API" sh -c "tar xzf - -C '$THU_MUC_TEP'" < "$thu_muc/tep-tin.tar.gz"
  else
    mau_vang "[2/2] Bỏ qua kho tệp."
  fi

  mau_xanh "Khôi phục xong. Khởi động lại API để làm mới bộ nhớ đệm:"
  echo "  docker restart $CONTAINER_API"
}

don_dep() {
  [ -d "$THU_MUC_GOC" ] || return 0

  local so_xoa=0

  while IFS= read -r d; do
    rm -rf "$d"
    so_xoa=$((so_xoa + 1))
    echo "  đã xoá $(basename "$d")"
  done < <(find "$THU_MUC_GOC" -mindepth 1 -maxdepth 1 -type d -mtime "+$SO_NGAY_GIU")

  mau_xanh "Đã dọn $so_xoa bản sao lưu quá $SO_NGAY_GIU ngày."
}

case "${1:-}" in
  sao-luu)   sao_luu ;;
  liet-ke)   liet_ke ;;
  khoi-phuc) khoi_phuc "${2:?Thiếu tên bản sao lưu. Chạy 'liet-ke' để xem danh sách.}" ;;
  don-dep)   don_dep ;;
  *)
    cat <<'HUONG_DAN'
Sao lưu và khôi phục BlueIdea

  sao-luu              Tạo bản sao lưu mới (CSDL + kho tệp)
  liet-ke              Liệt kê các bản sao lưu đang có
  khoi-phuc <tên>      Khôi phục từ một bản (hỏi xác nhận trước khi ghi đè)
  don-dep              Xoá bản cũ quá SO_NGAY_GIU_SAO_LUU ngày (mặc định 30)

Biến môi trường:
  THU_MUC_SAO_LUU        Nơi lưu (mặc định /var/backups/blueidea)
  SO_NGAY_GIU_SAO_LUU    Số ngày giữ bản cũ (mặc định 30)

Chạy định kỳ hằng ngày lúc 2h sáng — thêm vào crontab:
  0 2 * * * /opt/blueidea/deploy/sao-luu-blueidea.sh sao-luu >> /var/log/blueidea-sao-luu.log 2>&1
  0 3 * * * /opt/blueidea/deploy/sao-luu-blueidea.sh don-dep >> /var/log/blueidea-sao-luu.log 2>&1
HUONG_DAN
    exit 1
    ;;
esac
