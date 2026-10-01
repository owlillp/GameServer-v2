import { ItemsExplorer } from "@/src/features/items";
import { ApiTokenPanel } from "@/src/shared/ui/api-token-panel";
import { SiteHeader } from "@/src/shared/ui/site-header";

export default function ItemsPage() {
  return (
    <>
      <SiteHeader />

      <main className="mx-auto w-full max-w-3xl flex-1 space-y-6 px-6 py-10">
        <header className="space-y-1">
          <h1 className="text-2xl font-semibold text-slate-900">Items</h1>
          <p className="text-sm text-slate-600">
            Пример вертикального среза: список с поиском и пагинацией, создание,
            редактирование и удаление.
          </p>
        </header>

        <ApiTokenPanel />

        <ItemsExplorer />
      </main>
    </>
  );
}
