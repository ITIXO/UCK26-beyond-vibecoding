import { Link } from "react-router";
import { useAuth } from "@/features/auth/AuthProvider";
import { AppShell } from "@/shared/ui/AppShell";

export function HomePage() {
  const { user, isAdmin } = useAuth();

  return (
    <AppShell title="Home">
      <div className="rounded-lg border border-gray-200 bg-white p-6 shadow-sm">
        <p className="text-lg">
          Welcome, <span className="font-semibold">{user?.userName}</span>.
        </p>
        <p className="mt-1 text-sm text-gray-500">
          You are signed in as <span className="font-medium">{user?.role}</span>.
        </p>

        {isAdmin && (
          <Link
            to="/users"
            className="mt-4 inline-flex rounded-md bg-gray-900 px-3 py-2 text-sm text-white hover:bg-gray-700"
          >
            Manage users
          </Link>
        )}
      </div>
    </AppShell>
  );
}
