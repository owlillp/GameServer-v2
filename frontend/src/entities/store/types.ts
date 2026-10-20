// Зеркалит контракты StoreService.Contracts (camelCase).

export const OFFER_STATUSES = ["DRAFT", "ACTIVE", "ARCHIVED"] as const;

export type OfferStatus = (typeof OFFER_STATUSES)[number];

export const ORDER_STATUSES = [
  "PENDING",
  "RESERVED",
  "GRANT_REQUESTED",
  "COMPLETED",
  "FAILED",
] as const;

export type OrderStatus = (typeof ORDER_STATUSES)[number];

export type OfferItem = {
  catalogItemId: string;
  code: string;
  kind: string;
  slot: string | null;
  quantity: number;
};

export type Offer = {
  id: string;
  code: string;
  displayName: string;
  description: string | null;
  status: OfferStatus;
  priceCurrency: string;
  priceAmount: number;
  startsAt: string | null;
  endsAt: string | null;
  sortOrder: number;
  items: OfferItem[];
  createdAt: string;
  updatedAt: string;
};

export type OrderItem = {
  catalogItemId: string;
  code: string;
  kind: string;
  slot: string | null;
  quantity: number;
};

export type Order = {
  id: string;
  accountId: string;
  offerId: string;
  offerCode: string;
  offerName: string;
  currency: string;
  totalAmount: number;
  status: OrderStatus;
  failureReason: string | null;
  items: OrderItem[];
  createdAt: string;
  completedAt: string | null;
};

export type PurchaseOfferRequest = {
  offerId: string;
};

export type OfferItemRequest = {
  catalogItemId: string;
  quantity: number;
};

export type CreateOfferRequest = {
  code: string;
  displayName: string;
  description: string | null;
  priceCurrency: string;
  priceAmount: number;
  startsAt: string | null;
  endsAt: string | null;
  sortOrder: number;
  items: OfferItemRequest[];
};

export type UpdateOfferRequest = Omit<CreateOfferRequest, "code">;
