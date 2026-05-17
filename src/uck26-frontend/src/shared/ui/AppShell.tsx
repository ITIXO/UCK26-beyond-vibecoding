import type { ReactNode } from "react";
import { Link, useNavigate } from "react-router";
import { Button } from "@itixo/component-library";
import { useAuth } from "@/features/auth/AuthProvider";

interface Props {
  title: string;
  children: ReactNode;
}

export function AppShell({ title, children }: Props) {
  const { user, isAdmin, logout } = useAuth();
  const navigate = useNavigate();

  function handleLogout() {
    logout();
    navigate("/login", { replace: true });
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="border-b border-gray-200 bg-white">
        <div className="mx-auto flex max-w-5xl items-center justify-between px-4 py-3">
          <div className="flex items-center gap-6">
            <Link to="/" className="text-base font-semibold">
              UCK26
            </Link>
            <nav className="flex items-center gap-4 text-sm text-gray-600">
              <Link to="/" className="hover:text-gray-900">
                Home
              </Link>
              {isAdmin && (
                <Link to="/users" className="hover:text-gray-900">
                  Users
                </Link>
              )}
            </nav>
          </div>
          <div className="flex items-center gap-3 text-sm">
            <span className="text-gray-600">
              {user?.userName} · {user?.role}
            </span>
            <Button size="sm" variant="outline" onClick={handleLogout}>
              Sign out
            </Button>
          </div>
        </div>
      </header>

      <main className="mx-auto max-w-5xl px-4 py-6">
        <h1 className="mb-4 text-2xl font-semibold">{title}</h1>
        {children}
      </main>
    </div>
  );
}
