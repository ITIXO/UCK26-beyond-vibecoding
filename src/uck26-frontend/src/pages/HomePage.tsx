import { useAuth } from "@/features/auth/AuthProvider";
import { AppShell } from "@/shared/ui/AppShell";

export function HomePage() {
  const { user } = useAuth();

  return (
    <AppShell title="Home">
      <div className="rounded-lg border border-gray-200 bg-white p-6 shadow-sm">
        <p className="text-lg">
          Welcome, <span className="font-semibold">{user?.userName}</span>.
        </p>
        <p className="mt-1 text-sm text-gray-500">
          You are signed in as <span className="font-medium" data-test-id="current-user">{user?.role}</span>.
        </p>
      </div>
    </AppShell>
  );
}
