import { PlayerCard } from "@/src/features/admin-wallet";

export default async function AdminPlayerPage({
  params,
}: {
  params: Promise<{ walletId: string }>;
}) {
  const { walletId } = await params;

  return (
    <div className="space-y-6">
      <header className="space-y-1">
        <h1 className="text-2xl font-semibold text-slate-900">
          Карточка игрока
        </h1>
        <p className="text-sm text-slate-600">
          Информация о кошельке и ручные операции с балансом.
        </p>
      </header>

      <PlayerCard walletId={walletId} />
    </div>
  );
}
