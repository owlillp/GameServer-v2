// Зеркалит контракты WalletService.Contracts (camelCase).

export type WalletBalance = {
  currency: string;
  available: number;
};

export type Wallet = {
  walletId: string;
  balances: WalletBalance[];
  createdAt: string;
  updatedAt: string;
};

export type LedgerDirection = "Credit" | "Debit";

export type LedgerEntry = {
  id: string;
  currency: string;
  direction: LedgerDirection;
  amount: number;
  reason: string;
  operationId: string;
  referenceId: string | null;
  actorType: string;
  actorId: string | null;
  comment: string | null;
  createdAt: string;
};

export type CurrencyKind = "Soft" | "Hard" | "Event";

export type Currency = {
  code: string;
  displayName: string;
  kind: CurrencyKind;
  isActive: boolean;
  createdAt: string;
};

export type WalletOperationRequest = {
  currency: string;
  amount: number;
  reason: string;
  comment: string;
  operationId?: string;
  referenceId?: string | null;
};

export type CreateCurrencyRequest = {
  code: string;
  displayName: string;
  kind: CurrencyKind;
};
