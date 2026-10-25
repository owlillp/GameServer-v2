export const routes = {
  home: "/",
  login: "/login",
  register: "/register",
  profile: "/profile",
  inventory: "/inventory",
  store: "/store",
  events: "/events",
  matchmaking: "/matchmaking",
  clans: "/clans",
  dashboard: "/dashboard",
  admin: "/admin",
  adminUsers: "/admin/users",
  adminWallets: "/admin/wallets",
  adminCatalog: "/admin/catalog",
  adminInventory: "/admin/inventory",
  adminStore: "/admin/store",
  adminEvents: "/admin/events",
  adminPlayer: (walletId: string) => `/admin/players/${walletId}`,
} as const;

export type RouteKey = keyof typeof routes;
