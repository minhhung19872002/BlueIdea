#!/usr/bin/env bash
# =============================================================================
# Cai vhost Nginx cho BlueIdea tren may chu. Phai chay bang sudo.
#
#     sudo bash deploy/cai-nginx-blueidea.sh [ten-mien]
#
# Mac dinh ten mien la blueidea.bluestar.com.vn. Truyen tham so de doi.
#
# Nen chay tu ban sao trien khai de cau hinh Nginx khop voi phien ban dang chay:
#     sudo bash ~/deploy/blueidea-src/deploy/cai-nginx-blueidea.sh
#
# Script chi cai khoi HTTP (cong 80). Buoc xin chung thu TLS de rieng o cuoi vi
# Certbot doi ten mien DA tro DNS ve may nay - chay som se that bai va de lai
# cau hinh nua voi.
# =============================================================================
set -euo pipefail

TEN_MIEN="${1:-blueidea.bluestar.com.vn}"

# Lay tep cau hinh nam CANH script nay, khong ghi cung duong dan tuyet doi. Nho vay chay
# tu ban sao trien khai thi duoc dung cau hinh cua dung commit dang chay, con chay tu thu
# muc lap trinh thi duoc ban dang sua - trong ca hai truong hop deu la ban minh mong doi.
THU_MUC=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
NGUON="$THU_MUC/nginx/blueidea.conf"
DICH=/etc/nginx/sites-available/blueidea

if [ "$EUID" -ne 0 ]; then
  echo "Phai chay bang sudo: sudo bash $0 $TEN_MIEN" >&2
  exit 1
fi

if [ ! -f "$NGUON" ]; then
  echo "Khong thay $NGUON" >&2
  exit 1
fi

echo "==> Ten mien: $TEN_MIEN"
echo "==> Nguon cau hinh: $NGUON"

# Sao luu cau hinh cu neu da co, de con duong lui.
if [ -f "$DICH" ]; then
  cp "$DICH" "$DICH.bak-$(date +%Y%m%d-%H%M%S)"
  echo "==> Da sao luu cau hinh cu."
fi

install -m 644 "$NGUON" "$DICH"
sed -i "s/blueidea\.bluestar\.com\.vn/${TEN_MIEN}/g" "$DICH"
ln -sfn "$DICH" /etc/nginx/sites-enabled/blueidea
echo "==> Da cai va bat site."

# Kiem tra TRUOC khi reload: reload voi cau hinh sai se lam Nginx tu choi nap lai, va cac
# site khac dang chay tren cung Nginx nay se giu cau hinh cu - khong sap, nhung thay doi
# cua ta am tham khong co hieu luc.
if ! nginx -t; then
  echo "==> Cau hinh sai, KHONG reload. Go site vua bat de lan reload sau khong bi ket." >&2
  rm -f /etc/nginx/sites-enabled/blueidea
  exit 1
fi

systemctl reload nginx
echo "==> Da reload Nginx."

# `systemctl reload` tra ve ngay khi da gui tin hieu, con worker cua Nginx thi nap cau hinh
# moi mat them mot chut. Kiem tra ngay lap tuc se con gap vhost CU va bao 404 gay hieu nham,
# nen phai cho den khi thay ket qua that.
echo
echo "Kiem tra tai cho (chua co TLS van chay duoc nho Host header):"
for _ in $(seq 1 10); do
  ma=$(curl -s -o /dev/null -w '%{http_code}' -H "Host: $TEN_MIEN" http://127.0.0.1/ || echo 000)
  [ "$ma" = "200" ] && break
  sleep 1
done
echo "  http://$TEN_MIEN/  -> HTTP $ma"

if [ "$ma" != "200" ]; then
  echo "  CANH BAO: chua nhan duoc 200. Kiem tra container web:  docker ps | grep blueidea-web" >&2
fi

echo
echo "==> Con lai (lam sau khi DNS da tro ve IP may nay):"
echo "    sudo certbot --nginx -d $TEN_MIEN --redirect"
