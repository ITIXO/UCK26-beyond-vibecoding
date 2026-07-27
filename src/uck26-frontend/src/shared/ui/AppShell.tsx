import type { ReactNode } from "react";
import { Link, useLocation, useNavigate } from "react-router";
import { AuthenticatedLayout, Badge } from "@itixo/component-library";
import type { IRoute } from "@itixo/component-library";
import { CalendarDays, Home, UserCog } from "lucide-react";
import { useAuth } from "@/features/auth/AuthProvider";
import { Role } from "@/shared/lib/api/users.contracts.api.ts";
import companyLogo from "@/assets/company-logo.svg";

interface Props {
  title: string;
  children: ReactNode;
}

export function AppShell({ title, children }: Props) {
  const { user, isAdmin, logout } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const sidebarRoutes: IRoute[] = [
    {
      id: 1,
      name: "Home",
      icon: <Home className="size-4" />,
      isActive: false,
      agenda: "Home",
      path: "/",
      roles: [Role.User, Role.Admin],
    },
    {
      id: 2,
      name: "Work",
      icon: <CalendarDays className="size-4" />,
      isActive: false,
      agenda: "Work",
      path: "/work",
      roles: [Role.User, Role.Admin],
    },
    ...(isAdmin
      ? [
          {
            id: 3,
            name: "Users",
            icon: <UserCog className="size-4" />,
            isActive: false,
            agenda: "Users",
            path: "/users",
            roles: [Role.Admin],
          },
        ]
      : []),
  ];

  function handleLogout() {
    logout();
    navigate("/login", { replace: true });
  }

  return (
    <AuthenticatedLayout
      userName={user?.userName ?? ""}
      appName="UCK26"
      companyLogo={companyLogo}
      badges={
        user?.role ? (
          <Badge variant={user.role === Role.Admin ? "purple" : "secondary"}>{user.role}</Badge>
        ) : undefined
      }
      signOut={handleLogout}
      sidebarRoutes={sidebarRoutes}
      currentRoute={location.pathname}
      agendas={[]}
      LinkComponent={Link}
    >
      <main className="mx-auto max-w-6xl px-4 py-6">
        {title && (<h1 className="mb-4 text-2xl font-semibold">{title}</h1>)}
        {children}
      </main>
    </AuthenticatedLayout>
  );
}
