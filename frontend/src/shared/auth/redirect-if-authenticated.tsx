"use client";

import { useIsHydrated } from "@/src/shared/lib/use-is-hydrated";
import {
  sessionSelectors,
  useSessionStore,
} from "@/src/shared/stores/session-store";
import { PageLoader } from "@/src/shared/ui/page-loader";
import { useRouter } from "next/navigation";
import { useEffect, type ReactNode } from "react";
import { resolveHomeRoute } from "./roles";

// Для страниц входа/регистрации: уже вошедшего уводим на его домашний маршрут.
export function RedirectIfAuthenticated({ children }: { children: ReactNode }) {
  const router = useRouter();
  const hydrated = useIsHydrated();
  const isAuthenticated = useSessionStore(sessionSelectors.isAuthenticated);
  const user = useSessionStore(sessionSelectors.user);

  useEffect(() => {
    if (hydrated && isAuthenticated) {
      router.replace(resolveHomeRoute(user?.roles ?? []));
    }
  }, [hydrated, isAuthenticated, router, user]);

  if (!hydrated || isAuthenticated) {
    return <PageLoader />;
  }

  return <>{children}</>;
}
