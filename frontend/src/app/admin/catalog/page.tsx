import { CatalogManager } from "@/src/features/admin-catalog";

export default function AdminCatalogPage() {
  return (
    <div className="space-y-6">
      <header className="space-y-1">
        <h1 className="text-2xl font-semibold text-slate-900">Каталог</h1>
        <p className="text-sm text-slate-600">
          Редактор контента CatalogService: предметы, цены, характеристики и
          совместимость деталей. Изменения видны клиентам после публикации
          версии.
        </p>
      </header>

      <CatalogManager />
    </div>
  );
}
