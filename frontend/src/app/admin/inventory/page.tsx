import { AdminInventoryManager } from "@/src/features/admin-inventory";

export default function AdminInventoryPage() {
  return (
    <div className="space-y-6">
      <header className="space-y-1">
        <h1 className="text-2xl font-semibold text-slate-900">Инвентарь</h1>
        <p className="text-sm text-slate-600">
          Просмотр и редактирование инвентаря игрока: выдача и отзыв предметов,
          аутфит, гараж и тюнинг.
        </p>
      </header>

      <AdminInventoryManager />
    </div>
  );
}
