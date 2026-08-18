# External Blockers

Items that depend on external parties and cannot be resolved by development alone.

## Active Blockers

### 1. SSO — City Authentication System

**Requirement**: Function 21, 41
**What's needed**: `SSO_ISSUER`, `SSO_CLIENT_ID`, `SSO_CLIENT_SECRET` from the city's SSO provider, plus redirect URI registration
**Current state**: OIDC Authorization Code + PKCE flow fully implemented and tested with local OIDC provider
**Impact**: Users cannot use single sign-on until configured
**Workaround**: Internal authentication (username/password) works fully

### 2. IOC/TDKT — External System Integration

**Requirement**: Function 41
**What's needed**: Endpoint URLs, API keys, and API documentation from IOC and Thi dua khen thuong systems
**Current state**: HTTP adapter implemented with 3 auth types (API_KEY/HMAC/OAUTH2), tested with local mock server
**Impact**: Cannot push innovation data to external systems
**Workaround**: Data stays within BlueIdea; can be exported manually via Excel/PDF

### 3. Digital Signature — CA Certificate

**Requirement**: Function 49
**What's needed**: Real certificate from an authorized CA (PFX file or USB token/HSM)
**Current state**: PKCS#7 detached signing and verification work with self-signed certificate
**Impact**: Digital signatures are not legally valid until real certificate is loaded
**Workaround**: Export PDF and sign externally

### 4. Semantic Search Model — ONNX

**Requirement**: Function 37 (enhanced)
**What's needed**: Vietnamese sentence-transformer model exported to ONNX format
**Current state**: Lexical hashing trick provides word-level matching; misses paraphrased content
**Impact**: Similarity detection catches verbatim copying but not semantic rephrasing
**Workaround**: Lexical matching (TF-IDF + Jaccard) still catches most plagiarism cases

## Resolved Blockers

(None yet — this section tracks blockers that have been resolved)
