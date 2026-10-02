import { AdminUsersTable } from "@/src/features/admin-users";

export default function AdminUsersPage() {
  return (
    <div className="space-y-6">
      <header className="space-y-1">
        <h1 className="text-2xl font-semibold text-slate-900">Пользователи</h1>
        <p className="text-sm text-slate-600">
          Все аккаунты AuthService. Данные отдаёт GET /admin/users (permission
          users.view).
        </p>
      </header>

      <AdminUsersTable />
    </div>
  );
}
