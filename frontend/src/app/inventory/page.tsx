"use client";

import { InventoryView } from "@/src/features/inventory";
import { RequireAuth } from "@/src/shared/auth/require-auth";
import { SiteHeader } from "@/src/shared/ui/site-header";

export default function InventoryPage() {
  return (
    <RequireAuth>
      <SiteHeader />

      <main className="mx-auto w-full max-w-5xl flex-1 space-y-6 px-6 py-10">
        <header className="space-y-1">
          <h1 className="text-2xl font-semibold text-slate-900">Инвентарь</h1>
          <p className="text-sm text-slate-600">
            Предметы во владении, надетый аутфит и гараж с тюнингом.
          </p>
        </header>

        <InventoryView />
      </main>
    </RequireAuth>
  );
}
