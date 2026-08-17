"""
Dich vu OCR noi bo cho nen tang sang kien BlueIdea.

RANG BUOC BAT BUOC (Muc 3.2 E-HSMT / Muc 0 dac ta):
- Toan bo qua trinh suy luan chay tren ha tang noi bo cua don vi.
- KHONG goi bat ky API AI ben thu ba nao (OpenAI, Gemini, Claude, Azure AI...).
- Dich vu chi lang nghe trong mang noi bo cua docker-compose, khong expose ra Internet.

Chuc nang:
- POST /ocr        : trich xuat van ban tu anh hoac PDF scan (Tesseract, traineddata vie + eng)
- POST /trich-xuat : trich xuat van ban tu PDF co text-layer (khong can OCR)
- GET  /health     : kiem tra suc khoe
"""

from __future__ import annotations

import io
import logging
import os
import time
import unicodedata
from typing import Literal

import fitz  # PyMuPDF
import pytesseract
from fastapi import FastAPI, File, HTTPException, UploadFile
from PIL import Image
from pydantic import BaseModel

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s %(levelname)s %(message)s",
)
logger = logging.getLogger("blueidea-ai")

# Gioi han kich thuoc tep de tranh tu choi dich vu.
DUNG_LUONG_TOI_DA_MB = int(os.getenv("DUNG_LUONG_TOI_DA_MB", "50"))
SO_TRANG_TOI_DA = int(os.getenv("SO_TRANG_TOI_DA", "200"))
NGON_NGU_MAC_DINH = os.getenv("NGON_NGU_OCR", "vie+eng")
DPI_OCR = int(os.getenv("DPI_OCR", "200"))

app = FastAPI(
    title="BlueIdea AI Service (noi bo)",
    description="OCR va trich xuat van ban chay hoan toan tren ha tang noi bo.",
    version="1.0.0",
)


class KetQuaTrichXuat(BaseModel):
    thanh_cong: bool
    van_ban: str
    so_trang: int
    so_ky_tu: int
    phuong_phap: Literal["TEXT_LAYER", "OCR", "ANH"]
    thoi_gian_ms: int
    thong_bao: str = ""


@app.get("/health")
def kiem_tra_suc_khoe() -> dict:
    """Kiem tra dich vu va su san sang cua Tesseract."""
    try:
        phien_ban = str(pytesseract.get_tesseract_version())
        ngon_ngu = pytesseract.get_languages(config="")
    except Exception as loi:  # pragma: no cover - chi xay ra khi thieu Tesseract
        raise HTTPException(status_code=503, detail=f"Tesseract chưa sẵn sàng: {loi}") from loi

    return {
        "trang_thai": "SAN_SANG",
        "tesseract": phien_ban,
        "ngon_ngu": ngon_ngu,
        "goi_api_ben_ngoai": False,
    }


def _chuan_hoa(van_ban: str) -> str:
    """Chuan hoa Unicode ve NFC theo yeu cau cua he thong."""
    return unicodedata.normalize("NFC", van_ban).strip()


def _kiem_tra_dung_luong(du_lieu: bytes) -> None:
    so_mb = len(du_lieu) / (1024 * 1024)
    if so_mb > DUNG_LUONG_TOI_DA_MB:
        raise HTTPException(
            status_code=413,
            detail=f"Tệp {so_mb:.1f}MB vượt giới hạn {DUNG_LUONG_TOI_DA_MB}MB.",
        )


@app.post("/trich-xuat", response_model=KetQuaTrichXuat)
async def trich_xuat_text_layer(tep: UploadFile = File(...)) -> KetQuaTrichXuat:
    """Trich xuat van ban tu PDF co san text-layer (nhanh, khong can OCR)."""
    bat_dau = time.time()
    du_lieu = await tep.read()
    _kiem_tra_dung_luong(du_lieu)

    try:
        tai_lieu = fitz.open(stream=du_lieu, filetype="pdf")
    except Exception as loi:
        raise HTTPException(status_code=400, detail=f"Không đọc được PDF: {loi}") from loi

    cac_trang: list[str] = []
    with tai_lieu:
        for chi_muc, trang in enumerate(tai_lieu):
            if chi_muc >= SO_TRANG_TOI_DA:
                break
            cac_trang.append(trang.get_text("text"))

        so_trang = min(tai_lieu.page_count, SO_TRANG_TOI_DA)

    van_ban = _chuan_hoa("\n\n".join(cac_trang))

    return KetQuaTrichXuat(
        thanh_cong=True,
        van_ban=van_ban,
        so_trang=so_trang,
        so_ky_tu=len(van_ban),
        phuong_phap="TEXT_LAYER",
        thoi_gian_ms=int((time.time() - bat_dau) * 1000),
        thong_bao="" if van_ban else "PDF không có text-layer, hãy gọi /ocr.",
    )


@app.post("/ocr", response_model=KetQuaTrichXuat)
async def ocr(tep: UploadFile = File(...), ngon_ngu: str = NGON_NGU_MAC_DINH) -> KetQuaTrichXuat:
    """
    OCR anh hoac PDF scan bang Tesseract (traineddata vie + eng).
    Voi PDF, uu tien text-layer; chi OCR nhung trang khong co van ban.
    """
    bat_dau = time.time()
    du_lieu = await tep.read()
    _kiem_tra_dung_luong(du_lieu)

    ten = (tep.filename or "").lower()
    la_pdf = ten.endswith(".pdf") or du_lieu[:5] == b"%PDF-"

    if not la_pdf:
        try:
            anh = Image.open(io.BytesIO(du_lieu))
        except Exception as loi:
            raise HTTPException(status_code=400, detail=f"Không đọc được ảnh: {loi}") from loi

        van_ban = _chuan_hoa(pytesseract.image_to_string(anh, lang=ngon_ngu))
        return KetQuaTrichXuat(
            thanh_cong=True,
            van_ban=van_ban,
            so_trang=1,
            so_ky_tu=len(van_ban),
            phuong_phap="ANH",
            thoi_gian_ms=int((time.time() - bat_dau) * 1000),
        )

    try:
        tai_lieu = fitz.open(stream=du_lieu, filetype="pdf")
    except Exception as loi:
        raise HTTPException(status_code=400, detail=f"Không đọc được PDF: {loi}") from loi

    cac_trang: list[str] = []
    so_trang_ocr = 0

    with tai_lieu:
        for chi_muc, trang in enumerate(tai_lieu):
            if chi_muc >= SO_TRANG_TOI_DA:
                break

            van_ban_trang = trang.get_text("text").strip()

            # Trang khong co text-layer (PDF scan) -> render ra anh roi OCR.
            if len(van_ban_trang) < 20:
                pix = trang.get_pixmap(dpi=DPI_OCR)
                anh = Image.open(io.BytesIO(pix.tobytes("png")))
                van_ban_trang = pytesseract.image_to_string(anh, lang=ngon_ngu)
                so_trang_ocr += 1

            cac_trang.append(van_ban_trang)

        so_trang = min(tai_lieu.page_count, SO_TRANG_TOI_DA)

    van_ban = _chuan_hoa("\n\n".join(cac_trang))
    logger.info("OCR %s: %d trang (%d trang cần OCR)", tep.filename, so_trang, so_trang_ocr)

    return KetQuaTrichXuat(
        thanh_cong=True,
        van_ban=van_ban,
        so_trang=so_trang,
        so_ky_tu=len(van_ban),
        phuong_phap="OCR" if so_trang_ocr else "TEXT_LAYER",
        thoi_gian_ms=int((time.time() - bat_dau) * 1000),
    )
