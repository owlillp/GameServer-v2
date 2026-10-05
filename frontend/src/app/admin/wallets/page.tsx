import { AdminCurrencies } from "@/src/features/admin-wallet";

export default function AdminWalletsPage() {
  return (
    <div className="space-y-6">
      <header className="space-y-1">
        <h1 className="text-2xl font-semibold text-slate-900">Кошельки</h1>
        <p className="text-sm text-slate-600">
          Управление списком валют WalletService. Ручные операции с балансом
          игрока — в его карточке (открывается из «Пользователи»).
        </p>
      </header>

      <AdminCurrencies />
    </div>
  );
}
