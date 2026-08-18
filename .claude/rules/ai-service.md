# AI Service Rules — Internal Processing Only

See `docs/ADR/0001-ai-noi-bo.md` for the full decision record.

## Absolute Constraint

**No third-party AI API calls** (OpenAI, Gemini, Claude API, Azure AI, AWS Bedrock, etc.). This is a legal and procurement requirement (E-HSMT Section 3.2). All AI processing runs on-premise.

## OCR

- **Tesseract 5** (vie + eng) in a separate FastAPI container (`ai-service/`).
- Listens only on internal Docker network (`127.0.0.1:8088`), not exposed to Internet.
- PDF: prefers text-layer extraction, OCR only for scanned pages.
- Triggered as background job when files are uploaded.

## Similarity Detection Pipeline

All algorithms run in-process (.NET), no external network calls:

| Layer | Algorithm | Purpose |
|---|---|---|
| Coarse filter | SimHash 64-bit + Hamming distance | Fast elimination |
| Coarse filter | MinHash + LSH banding on 5-gram shingles | Catch near-duplicates SimHash misses |
| Fine match (lexical) | TF-IDF cosine + Jaccard | Detect verbatim copying |
| Fine match (semantic) | Cosine on embedding vectors | Detect paraphrasing |

Combined score: `ratio = lexical_weight × lexical + semantic_weight × semantic` (default 0.4/0.6, configurable in `cau_hinh_he_thong`).

## Embedding

- `IBoNhungVanBan` is the extension point for Vietnamese embedding models.
- Default: `BoNhungBamTuVung` — 768-dim hashing trick (FNV-1a, deterministic, no Internet needed).
- Production upgrade: ONNX Runtime with Vietnamese sentence-transformer model (e.g., `dangvantuan/vietnamese-embedding`). Just register a different `IBoNhungVanBan` in DI.
- Model name recorded in every similarity check result (`tenMoHinhNhung`) for traceability.

## Processing Order

1. File upload → schedule OCR extraction (if extractable format).
2. Application submission → schedule similarity check **only if no files pending OCR**.
3. OCR complete → if last file for a submitted application, auto-trigger similarity check.
4. Periodic sweep (every 15 min) catches stuck applications (OCR failures, service outages).

## AI Results Are Advisory Only

- Results are **warnings**, not automatic rejections.
- Final decision belongs to the council, recorded in `kiem_tra_trung_lap.y_kien_hoi_dong`.

## Prohibited

- Do NOT add any external AI API dependency.
- Do NOT let application data leave the unit's infrastructure.
- Do NOT auto-reject applications based on AI similarity scores.
