// Зеркалит контракты InventoryService.Contracts (camelCase).

export type OwnedItem = {
  itemId: string;
  code: string;
  kind: string;
  slot: string | null;
  source: string;
  quantity: number;
  acquiredAt: string;
};

export type OutfitEntry = {
  slot: string;
  itemId: string;
};

export type CarInstallation = {
  slot: string;
  partItemId: string;
};

export type Car = {
  id: string;
  carItemId: string;
  code: string;
  color: string | null;
  plate: string | null;
  installations: CarInstallation[];
  createdAt: string;
};

export type Inventory = {
  accountId: string;
  items: OwnedItem[];
  outfit: OutfitEntry[];
  cars: Car[];
  createdAt: string;
  updatedAt: string;
};

export type GrantItemRequest = {
  itemId: string;
  code: string;
  kind: string;
  slot: string | null;
  quantity: number;
};

export type GrantItemsRequest = {
  items: GrantItemRequest[];
  source: string;
  operationId?: string;
  referenceId?: string | null;
};

export type RevokeItemsRequest = {
  itemIds: string[];
};

export type AddCarRequest = {
  carItemId: string;
  code: string;
  color?: string | null;
  plate?: string | null;
};
