import { AdminEventsManager } from "@/src/features/admin-events";

export default function AdminEventsPage() {
  return (
    <div className="space-y-6">
      <header className="space-y-1">
        <h1 className="text-2xl font-semibold text-slate-900">Ивенты</h1>
        <p className="text-sm text-slate-600">
          EventService: календарь ивентов, задания, награды и публикация.
          Редактировать можно только черновики.
        </p>
      </header>

      <AdminEventsManager />
    </div>
  );
}
