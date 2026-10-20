export const routes = {
  home: "/",
  login: "/login",
  register: "/register",
  profile: "/profile",
  inventory: "/inventory",
  store: "/store",
  dashboard: "/dashboard",
  admin: "/admin",
  adminUsers: "/admin/users",
  adminWallets: "/admin/wallets",
  adminCatalog: "/admin/catalog",
  adminInventory: "/admin/inventory",
  adminStore: "/admin/store",
  adminPlayer: (walletId: string) => `/admin/players/${walletId}`,
} as const;

export type RouteKey = keyof typeof routes;
