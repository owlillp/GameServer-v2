"use client";

import { useIsHydrated } from "@/src/shared/lib/use-is-hydrated";
import { tokenStorage } from "@/src/shared/auth/token-storage";
import { Button } from "@/src/shared/ui/button";
import { Input } from "@/src/shared/ui/input";
import { useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { toast } from "sonner";

// API защищено JWT от AuthService. Панель позволяет вставить access-токен без
// реализации login-флоу — интерсептор axios подставит его в Authorization.
export function ApiTokenPanel() {
  const hydrated = useIsHydrated();
  const queryClient = useQueryClient();
  const [savedToken, setSavedToken] = useState<string | null>(null);
  const [input, setInput] = useState("");

  const storedToken = hydrated ? tokenStorage.get() : null;
  const currentToken = savedToken ?? storedToken;
  const hasToken = Boolean(currentToken);

  const save = () => {
    const value = input.trim();
    if (!value) {
      toast.error("Вставь access-токен");
      return;
    }

    tokenStorage.set(value);
    setSavedToken(value);
    setInput("");
    void queryClient.invalidateQueries();
    toast.success("Токен сохранён");
  };

  const clear = () => {
    tokenStorage.clear();
    setSavedToken("");
    void queryClient.invalidateQueries();
    toast.success("Токен удалён");
  };

  return (
    <section className="rounded-lg border border-slate-200 bg-white p-5">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2 className="text-sm font-semibold text-slate-900">Access-токен</h2>
        <span
          className={
            hasToken
              ? "text-xs font-medium text-emerald-600"
              : "text-xs text-slate-500"
          }
        >
          {!hydrated ? "…" : hasToken ? "токен сохранён" : "токен не задан"}
        </span>
      </div>

      <p className="mt-2 text-xs text-slate-500">
        Endpoints сервиса требуют JWT, выданный AuthService. Вставь access-токен
        — он будет подставляться в заголовок Authorization.
      </p>

      <div className="mt-3 flex flex-wrap items-center gap-2">
        <label htmlFor="api-token" className="sr-only">
          Access-токен
        </label>
        <Input
          id="api-token"
          name="api-token"
          type="password"
          autoComplete="off"
          spellCheck={false}
          placeholder="eyJhbGciOi…"
          className="max-w-md flex-1"
          value={input}
          onChange={(event) => setInput(event.target.value)}
        />
        <Button onClick={save}>Сохранить</Button>
        <Button variant="ghost" onClick={clear} disabled={!hasToken}>
          Очистить
        </Button>
      </div>
    </section>
  );
}
