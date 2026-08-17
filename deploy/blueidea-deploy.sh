#!/usr/bin/env bash
# =============================================================================
# Script trien khai BlueIdea tren VM. Duoc goi qua SSH tu GitHub Actions (.github/cd.yml).
#
# Day la FORCED COMMAND cua khoa deploy trong ~/.ssh/authorized_keys, nghia la khoa do
# khong the chay bat ky lenh nao khac ngoai script nay - ke ca khi khoa bi lo, ke tan cong
# cung chi kich hoat duoc mot lan trien khai chu khong vao duoc shell.
#
# Cach goi:
#     printf '%s\n%s\n' "<user>" "<ghcr-token>" | ssh hung@may "deploy <commit-sha>"
#     ssh hung@may "status"
#
# Thong tin dang nhap GHCR di qua STDIN chu khong qua tham so dong lenh, vi tham so dong
# lenh hien trong `ps aux` cho moi user tren may doc duoc.
#
# -----------------------------------------------------------------------------
# QUAN TRONG - tep nay co HAI ban:
#
#   1. Ban goc: chinh tep nay, trong repo. Moi thay doi sua o day.
#   2. Ban dang chay: ~/deploy/blueidea-deploy.sh tren VM, la duong dan ghi trong
#      authorized_keys.
#
# Phai tach ra nhu vay vi ban dang chay khong duoc nam trong thu muc bi `git reset --hard`
# o giua lan trien khai. Sau khi sua tep nay, cap nhat ban dang chay:
#
#     cp ~/deploy/blueidea-src/deploy/blueidea-deploy.sh ~/deploy/blueidea-deploy.sh
#
# Quen buoc do thi deploy van chay binh thuong nhung bang logic CU - rat kho phat hien,
# nen script tu so sanh hai ban va ghi canh bao vao log neu lech.
# =============================================================================
set -euo pipefail

# Ban sao ma nguon RIENG cho trien khai, khong dung thu muc lap trinh (~/BlueIdea).
#
# Dung chung mot thu muc voi noi lap trinh la mot cai bay: chi can co tep sua do chua
# commit la `git checkout` that bai va lan trien khai do chet giua duong, dung luc dang
# can no chay. Thu muc nay chi script nay cham vao, nen `reset --hard` luon an toan.
REPO=/home/hung/deploy/blueidea-src
NGUON_GIT=https://github.com/minhhung19872002/BlueIdea.git
ENV_FILE=/home/hung/deploy/blueidea.env
COMPOSE_FILE=deploy/docker-compose.prod.yml
LOG=/home/hung/deploy/blueidea-deploy.log

ghi() { echo "[$(date '+%F %T')] $*" | tee -a "$LOG"; }

# --- Chi cho phep dung tap lenh da biet -------------------------------------
doc="${SSH_ORIGINAL_COMMAND:-}"
verb=$(printf '%s' "$doc" | awk '{print $1}')
tham=$(printf '%s' "$doc" | awk '{print $2}')

case "$verb" in
  status)
    docker compose -f "$REPO/$COMPOSE_FILE" --env-file "$ENV_FILE" ps
    exit 0
    ;;
  deploy)
    ;;
  *)
    echo "Khoa nay chi chay duoc: 'deploy <commit-sha>' hoac 'status'." >&2
    exit 64
    ;;
esac

# Chan chen lenh: sha phai la hex 40 ky tu, khong nhan gi khac.
if [[ ! "$tham" =~ ^[0-9a-f]{40}$ ]]; then
  echo "commit-sha khong hop le (can 40 ky tu hex): '$tham'" >&2
  exit 64
fi
SHA="$tham"
TAG="sha-${SHA}"

ghi "===== Bat dau trien khai $SHA ====="

# --- Dang nhap GHCR bang thong tin doc tu stdin -----------------------------
# Doc DUNG hai dong: dong 1 la user, dong 2 la token. Phai lay user tu ben goi chu khong
# ghi cung ten tai khoan: GitHub Actions dung GITHUB_TOKEN co actor la "github-actions[bot]",
# ghi cung "minhhung19872002" thi GHCR tu choi xac thuc.
IFS= read -r -t 30 GH_USER || true
IFS= read -r -t 30 GH_TOKEN || true
if [ -z "${GH_USER:-}" ] || [ -z "${GH_TOKEN:-}" ]; then
  ghi "LOI: stdin phai co 2 dong - user GHCR roi den token."
  exit 65
fi
printf '%s' "$GH_TOKEN" | docker login ghcr.io -u "$GH_USER" --password-stdin >/dev/null
unset GH_TOKEN
ghi "Da dang nhap GHCR voi user $GH_USER."

# --- Dua ma nguon ve dung commit da dung anh --------------------------------
# Bat buoc phai khop: tep compose va cau hinh Nginx deu di kem commit do. Deploy anh cua
# commit moi bang tep compose cua commit cu la nguon loi rat kho truy.
if [ ! -d "$REPO/.git" ]; then
  ghi "Chua co ban sao trien khai, dang clone..."
  git clone --quiet "$NGUON_GIT" "$REPO"
fi

cd "$REPO"
git fetch --quiet origin
# reset --hard thay vi checkout: thu muc nay khong ai lam viec trong do, nen bo moi thay
# doi la dung y muon. clean -fd de tep sinh ra tu lan truoc khong lam nhieu lan sau.
git reset --hard --quiet "$SHA"
git clean -qfd
ghi "Ma nguon da o commit $(git rev-parse --short HEAD)."

# --- Canh bao neu ban dang chay da lech so voi ban goc ----------------------
# Xem giai thich ve hai ban o dau tep. Chi canh bao chu khong tu ghi de: tu thay the tep
# dang chay giua lan trien khai la cach chac chan de gap loi cuc kho hieu.
BAN_GOC="$REPO/deploy/$(basename "$0")"
if [ -f "$BAN_GOC" ] && ! cmp -s "$0" "$BAN_GOC"; then
  ghi "CANH BAO: dang chay ban CU. $0 khac voi $BAN_GOC."
  ghi "CANH BAO: cap nhat bang -> cp '$BAN_GOC' '$0'"
fi

# --- Luu tag dang chay de con duong lui -------------------------------------
TRUOC=$(grep -E '^TAG=' "$ENV_FILE" | cut -d= -f2- || echo "")
ghi "Tag truoc khi doi: ${TRUOC:-<khong co>}"

# --- Keo anh moi TRUOC khi dung dich vu cu ----------------------------------
# Keo xong moi thay the: neu mang loi giua duong, dich vu cu van dang chay binh thuong.
export TAG
docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" pull --quiet
ghi "Da keo xong anh $TAG."

# Ghi tag moi vao tep env de lan `docker compose up` thu cong sau nay dung dung anh.
sed -i "s|^TAG=.*|TAG=${TAG}|" "$ENV_FILE"

# --- Thay the dich vu -------------------------------------------------------
docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" up -d --remove-orphans
ghi "Da khoi dong dich vu."

# --- Cho den khi that su khoe manh ------------------------------------------
# Lan dau chay phai migrate CSDL roi nap du lieu mau nen can nhieu thoi gian.
ghi "Cho api + web bao healthy (toi da 300s)..."
het=$((SECONDS + 300))
while [ $SECONDS -lt $het ]; do
  tt_api=$(docker inspect -f '{{.State.Health.Status}}' blueidea-api 2>/dev/null || echo "chua-co")
  tt_web=$(docker inspect -f '{{.State.Health.Status}}' blueidea-web 2>/dev/null || echo "chua-co")

  if [ "$tt_api" = "healthy" ] && [ "$tt_web" = "healthy" ]; then
    ghi "THANH CONG: api=$tt_api web=$tt_web"
    docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" ps
    # Don anh cu de khong day day dia.
    docker image prune -f --filter "until=168h" >/dev/null 2>&1 || true
    ghi "===== Hoan tat $SHA ====="
    exit 0
  fi

  if [ "$tt_api" = "unhealthy" ]; then
    ghi "LOI: api bao unhealthy. 50 dong log cuoi:"
    docker logs --tail 50 blueidea-api 2>&1 | tee -a "$LOG"
    exit 1
  fi

  sleep 5
done

ghi "LOI: het thoi gian cho. api=$tt_api web=$tt_web"
docker logs --tail 50 blueidea-api 2>&1 | tee -a "$LOG"
exit 1
