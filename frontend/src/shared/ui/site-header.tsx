"use client";

import { cn } from "@/src/shared/lib/utils";
import { routes } from "@/src/shared/routes";
import Link from "next/link";
import { usePathname } from "next/navigation";

const navItems = [
  { href: routes.home, label: "Обзор" },
  { href: routes.items, label: "Items" },
] as const;

export function SiteHeader() {
  const pathname = usePathname();

  return (
    <header className="border-b border-slate-200 bg-white">
      <div className="mx-auto flex w-full max-w-5xl flex-wrap items-center gap-x-6 gap-y-2 px-6 py-3">
        <Link
          href={routes.home}
          className="text-sm font-semibold tracking-tight text-slate-900"
        >
          Template Service
        </Link>

        <nav aria-label="Основная навигация" className="flex items-center">
          {navItems.map((item) => {
            const active =
              pathname === item.href || pathname.startsWith(`${item.href}/`);

            return (
              <Link
                key={item.href}
                href={item.href}
                aria-current={active ? "page" : undefined}
                className={cn(
                  "rounded-md px-3 py-1.5 text-sm transition-colors",
                  active
                    ? "bg-slate-100 font-medium text-slate-900"
                    : "text-slate-600 hover:bg-slate-50 hover:text-slate-900",
                )}
              >
                {item.label}
              </Link>
            );
          })}
        </nav>
      </div>
    </header>
  );
}
