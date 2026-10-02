"use client";

import { cn } from "@/src/shared/lib/utils";
import { routes } from "@/src/shared/routes";
import Link from "next/link";
import { usePathname } from "next/navigation";

const items = [
  { href: routes.admin, label: "Обзор" },
  { href: routes.adminUsers, label: "Пользователи" },
] as const;

export function AdminNav() {
  const pathname = usePathname();

  return (
    <div className="border-b border-slate-200 bg-slate-50">
      <nav
        aria-label="Навигация админ-панели"
        className="mx-auto flex w-full max-w-5xl items-center gap-1 px-6"
      >
        {items.map((item) => {
          const active = pathname === item.href;

          return (
            <Link
              key={item.href}
              href={item.href}
              aria-current={active ? "page" : undefined}
              className={cn(
                "-mb-px border-b-2 px-3 py-2.5 text-sm font-medium transition-colors",
                active
                  ? "border-slate-900 text-slate-900"
                  : "border-transparent text-slate-500 hover:border-slate-300 hover:text-slate-900",
              )}
            >
              {item.label}
            </Link>
          );
        })}
      </nav>
    </div>
  );
}
