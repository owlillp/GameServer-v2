// Зеркалит контракты CatalogService.Contracts (camelCase).

export const ITEM_KINDS = ["CLOTHING", "CAR", "CAR_PART"] as const;

export type ItemKind = (typeof ITEM_KINDS)[number];

export const ITEM_RARITIES = [
  "COMMON",
  "UNCOMMON",
  "RARE",
  "EPIC",
  "LEGENDARY",
] as const;

export type ItemRarity = (typeof ITEM_RARITIES)[number];

export const ITEM_STATUSES = ["DRAFT", "PUBLISHED", "ARCHIVED"] as const;

export type ItemStatus = (typeof ITEM_STATUSES)[number];

export type ItemStat = {
  key: string;
  value: number;
};

export type ItemPrice = {
  currencyCode: string;
  amount: number;
  isActive: boolean;
  validFrom: string | null;
};

export type AvailabilityWindow = {
  startsAt: string | null;
  endsAt: string | null;
  channel: string | null;
};

// GET /admin/catalog/items — карточка предмета (включая черновики).
export type ItemDetail = {
  id: string;
  code: string;
  kind: ItemKind;
  rarity: ItemRarity;
  status: ItemStatus;
  displayName: string;
  description: string | null;
  modelCode: string | null;
  slot: string | null;
  tags: string[];
  fitTags: string[];
  lastPublishedVersion: number | null;
  stats: ItemStat[];
  prices: ItemPrice[];
  availability: AvailabilityWindow[];
  createdAt: string;
  updatedAt: string;
};

export type CatalogVersion = {
  id: string;
  number: number;
  note: string | null;
  publishedBy: string | null;
  publishedAt: string;
  itemCount: number;
  status: string;
};

export type CatalogVersionItem = {
  itemId: string;
  code: string;
  kind: ItemKind;
  rarity: ItemRarity;
  displayName: string;
  modelCode: string | null;
  slot: string | null;
  tags: string[];
  fitTags: string[];
};

export type CatalogVersionDetail = CatalogVersion & {
  items: CatalogVersionItem[];
};

export type CreateItemRequest = {
  code: string;
  kind: ItemKind;
  rarity: ItemRarity;
  displayName: string;
  description: string | null;
  modelCode: string | null;
  slot: string | null;
  tags: string[] | null;
  fitTags: string[] | null;
  stats: ItemStat[] | null;
  prices: ItemPrice[] | null;
  availability: AvailabilityWindow[] | null;
};

export type UpdateItemRequest = {
  displayName: string;
  description: string | null;
  rarity: ItemRarity;
  modelCode: string | null;
  slot: string | null;
  tags: string[] | null;
  fitTags: string[] | null;
  stats: ItemStat[] | null;
  prices: ItemPrice[] | null;
  availability: AvailabilityWindow[] | null;
};

export type AdminItemsRequest = {
  status?: ItemStatus;
  kind?: ItemKind;
};
