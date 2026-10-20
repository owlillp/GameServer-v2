"use client";

import { StoreView } from "@/src/features/store";
import { RequireAuth } from "@/src/shared/auth/require-auth";
import { SiteHeader } from "@/src/shared/ui/site-header";

export default function StorePage() {
  return (
    <RequireAuth>
      <SiteHeader />

      <main className="mx-auto w-full max-w-5xl flex-1 space-y-6 px-6 py-10">
        <header className="space-y-1">
          <h1 className="text-2xl font-semibold text-slate-900">Магазин</h1>
          <p className="text-sm text-slate-600">
            Офферы за игровую валюту: одежда, машины и детали тюнинга.
          </p>
        </header>

        <StoreView />
      </main>
    </RequireAuth>
  );
}
