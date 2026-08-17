# =============================================================================
# Dich vu OCR noi bo - Tesseract 5 voi traineddata tieng Viet.
# KHONG goi API AI ben thu ba (Muc 3.2 E-HSMT).
# =============================================================================

FROM python:3.12-slim

ENV PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    TZ=Asia/Ho_Chi_Minh

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        tesseract-ocr \
        tesseract-ocr-vie \
        tesseract-ocr-eng \
        libgl1 \
        tzdata \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY requirements.txt ./
RUN pip install --no-cache-dir -r requirements.txt

COPY app.py ./

# Chay bang user khong phai root (Muc 6 dac ta).
RUN useradd --system --create-home aiuser && chown -R aiuser:aiuser /app
USER aiuser

EXPOSE 8000

CMD ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "8000", "--workers", "2"]
