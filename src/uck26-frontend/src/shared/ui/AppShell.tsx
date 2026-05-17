import type { ReactNode } from "react";
import { Link, useLocation, useNavigate } from "react-router";
import { AuthenticatedLayout } from "@itixo/component-library";
import type { IRoute } from "@itixo/component-library";
import { UserCog } from "lucide-react";
import { useAuth } from "@/features/auth/AuthProvider";

interface Props {
  title: string;
  children: ReactNode;
}

export function AppShell({ title, children }: Props) {
  const { user, isAdmin, logout } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const sidebarRoutes: IRoute[] | undefined = isAdmin
    ? [
        {
          id: 1,
          name: "UserManagement",
          icon: <UserCog className="size-4" />,
          isActive: location.pathname === "/users",
          agenda: "UserManagement",
          path: "/users",
          roles: ["Admin"],
        },
      ]
    : undefined;

  function handleLogout() {
    logout();
    navigate("/login", { replace: true });
  }

  return (
    <AuthenticatedLayout
      userName={user?.userName ?? ""}
      signOut={handleLogout}
      sidebarRoutes={sidebarRoutes}
      currentRoute={location.pathname}
      agendas={[]}
      LinkComponent={Link}
      NavbarActions={
        <span data-test-id="current-user" className="text-sm text-muted-foreground">
          {user?.userName} · {user?.role}
        </span>
      }
    >
      <main className="mx-auto max-w-5xl px-4 py-6">
        <h1 className="mb-4 text-2xl font-semibold">{title}</h1>
        {children}
      </main>
    </AuthenticatedLayout>
  );
}
