import { STAFF_ROLES } from "@/src/shared/auth/roles";
import { RequireAuth } from "@/src/shared/auth/require-auth";
import { SiteHeader } from "@/src/shared/ui/site-header";
import { AdminNav } from "./admin-nav";

export default function AdminLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <RequireAuth roles={STAFF_ROLES}>
      <SiteHeader />
      <AdminNav />
      <main className="mx-auto w-full max-w-5xl flex-1 px-6 py-8">
        {children}
      </main>
    </RequireAuth>
  );
}
