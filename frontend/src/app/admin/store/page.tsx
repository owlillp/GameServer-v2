import { AdminStoreManager } from "@/src/features/admin-store";

export default function AdminStorePage() {
  return (
    <div className="space-y-6">
      <header className="space-y-1">
        <h1 className="text-2xl font-semibold text-slate-900">Магазин</h1>
        <p className="text-sm text-slate-600">
          Офферы StoreService: состав предметов, цена и окно доступности.
          Активные офферы видны игрокам на витрине.
        </p>
      </header>

      <AdminStoreManager />
    </div>
  );
}
