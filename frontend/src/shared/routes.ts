export const routes = {
  home: "/",
  login: "/login",
  register: "/register",
  profile: "/profile",
  inventory: "/inventory",
  dashboard: "/dashboard",
  admin: "/admin",
  adminUsers: "/admin/users",
  adminWallets: "/admin/wallets",
  adminCatalog: "/admin/catalog",
  adminInventory: "/admin/inventory",
  adminPlayer: (walletId: string) => `/admin/players/${walletId}`,
} as const;

export type RouteKey = keyof typeof routes;
