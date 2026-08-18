# Backend Rules

## Stack

.NET 8 LTS, C# 12, nullable enable, `TreatWarningsAsErrors=true`, PostgreSQL 16, MediatR, FluentValidation, Mapster, Serilog, Hangfire, SignalR, MinIO/local disk (`IFileStorage`).

## CQRS with MediatR

Every business operation is a Command (write) or Query (read). Handlers go in `BlueIdea.Application/`.

```csharp
public record NopHoSoCommand(Guid SangKienId) : IRequest<Result<SangKienDto>>;
```

## FluentValidation

Validation rules are separate classes, auto-run via MediatR pipeline. Server-side validation is authoritative — frontend validates only for UX.

## Domain Entity Guards

Business rules belong in the Domain layer, enforced via guard methods that throw `NghiepVuException` with specific error codes.

```csharp
public void NopHoSo()
{
    if (TrangThaiTong != TrangThaiTong.Nhap)
        throw new NghiepVuException(MaLoi.KHONG_THE_NOP_HO_SO_KHONG_O_TRANG_THAI_NHAP);
    // ...
}
```

## Static Factory Methods

Create entities via factory methods, not `new` directly.

## Domain Services

Stateless, no Repository injection — receive aggregates as parameters. Use when logic spans multiple aggregates.

## API Convention

- Base: `/api/v1`, JSON camelCase, ISO-8601 timestamps with offset
- Response: `{ "thanhCong": true, "duLieu": {}, "thongBao": "", "maLoi": null, "chiTietLoi": [] }`
- Pagination: `?trang=1&soDong=20&sapXep=ngayTao&huong=desc`
- Validation errors: HTTP 422 with `chiTietLoi`
- Business error codes: `DOT_DE_NGHI_DA_DONG`, `KHONG_CO_QUYEN_XU_LY_BUOC`, etc.
- Every endpoint has `[Authorize(Policy = "...")]` and Vietnamese Swagger description

## Naming Convention

- Database tables/columns: Vietnamese without diacritics, snake_case (`sang_kien`, `dot_de_nghi`)
- C# Domain classes: Vietnamese or English, consistent within module
- API routes: Vietnamese with hyphens (`/api/v1/sang-kien`, `/api/v1/quy-trinh`)
- Error codes: Vietnamese without diacritics, UPPER_SNAKE_CASE

## Prohibited

- Do NOT mock DbContext, repositories, or application services in tests
- Do NOT use `any` equivalent loose typing
- Do NOT use ABP Framework
- Do NOT use dynamic eval in rule evaluator
