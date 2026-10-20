"use client";

import { EventsView } from "@/src/features/events";
import { RequireAuth } from "@/src/shared/auth/require-auth";
import { SiteHeader } from "@/src/shared/ui/site-header";

export default function EventsPage() {
  return (
    <RequireAuth>
      <SiteHeader />

      <main className="mx-auto w-full max-w-5xl flex-1 space-y-6 px-6 py-10">
        <header className="space-y-1">
          <h1 className="text-2xl font-semibold text-slate-900">Ивенты</h1>
          <p className="text-sm text-slate-600">
            Сезонные события, задания и награды за ивент-очки.
          </p>
        </header>

        <EventsView />
      </main>
    </RequireAuth>
  );
}
