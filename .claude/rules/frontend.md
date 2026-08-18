# Frontend Rules

## Stack

React 18, TypeScript 5, Vite, Ant Design 5, TanStack Query v5, Zustand, react-hook-form + zod, React Router v6, ECharts, ReactFlow, @dnd-kit, i18next, axios.

## Folder Structure

```
web/src/
├── api/          # Axios instance, shared endpoints
├── app/          # Router, providers, global layout, stores
├── components/   # Shared UI components
├── features/     # Feature-based (one folder per bounded context)
│   └── [feature]/
│       ├── api/        # TanStack Query hooks (repository pattern)
│       ├── components/
│       ├── pages/
│       ├── types/
│       └── __tests__/
├── hooks/        # Shared hooks
├── lib/          # queryClient config, utilities
├── types/        # Shared types
├── locales/      # i18n translations
├── styles/       # Global styles
└── utils/
```

## Key Rules

- Each feature folder is **independent** — no cross-imports between features.
- Shared logic goes in `components/` or `hooks/`.
- API calls **only** through TanStack Query hooks in feature `api/` folder.
- **No `any`** in TypeScript.
- Form validation: Zod schema defined first, react-hook-form uses it.
- Vietnamese search: support unaccented input returning accented results.
- Every table: server-side pagination, sort, filter, Excel export, filters persisted in URL query.
- Skeleton loading, empty state, error boundary on every data view.
- Theme color read from API `cau_hinh_he_thong` — never hardcode colors.

## Design Patterns

1. **Compound Components**: `DataTable`, `SearchForm`, `FormModal`, `WorkflowActions`, `FileUploader`
2. **Container/Presenter**: Separate data fetching from rendering. Presenter is pure UI, no hooks/axios/navigate.
3. **Custom Hooks**: Stateful logic in dedicated hooks.
4. **Adapter Pattern**: Transform backend DTO → ViewModel in `api/` layer.
5. **Strategy Pattern**: Use config maps for conditional rendering instead of if/else chains.
6. **Builder Pattern**: Compose Zod schemas from reusable pieces.
7. **Repository Pattern**: Each feature's `api/` folder is the single source of API calls.

## Component/Hook Rules

- Presenter components must NOT import hooks, axios, or call navigate.
- Components and hooks must NOT call axios directly — go through `api/` folder.
- Use `@media print` CSS for evaluation forms, minutes.
- Responsive from 320px. Navigation becomes Drawer on mobile. Tables scroll horizontally in their own container.
