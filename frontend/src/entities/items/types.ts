import type { PaginationResponse } from "@/src/shared/api/types";

// Зеркалит контракты TemplateService.Contracts (camelCase).

export type Item = {
  id: string;
  name: string;
  description: string | null;
  ownerId: string;
  createdAt: string;
  updatedAt: string;
};

export type ItemsRequest = {
  page: number;
  pageSize: number;
  search: string;
};

export type CreateItemRequest = {
  name: string;
  description?: string | null;
};

export type UpdateItemRequest = {
  name: string;
  description?: string | null;
};

export type ItemsPage = PaginationResponse<Item>;
