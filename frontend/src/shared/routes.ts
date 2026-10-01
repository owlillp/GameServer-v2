export const routes = {
  home: "/",
  items: "/items",
} as const;

export type RouteKey = keyof typeof routes;
