import { Link } from "react-router";
import { Button } from "@itixo/component-library";
import { CalendarDays } from "lucide-react";
import { useAuth } from "@/features/auth/AuthProvider";
import { AppShell } from "@/shared/ui/AppShell";

export function HomePage() {
  const { user } = useAuth();

  return (
    <AppShell title="Home">
      <div className="space-y-4">
        <div className="rounded-lg border border-gray-200 bg-white p-6 shadow-sm">
          <p className="text-lg">
            Welcome, <span className="font-semibold">{user?.userName}</span>.
          </p>
          <p className="mt-1 text-sm text-gray-500">
            You are signed in as <span className="font-medium" data-test-id="current-user">{user?.role}</span>.
          </p>
        </div>
        <section className="rounded-lg border border-gray-200 bg-white p-5 shadow-sm" data-test-id="work-card">
          <div className="flex items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <div className="flex size-10 items-center justify-center rounded-md bg-gray-100">
                <CalendarDays className="size-5 text-gray-700" />
              </div>
              <div>
                <h2 className="text-lg font-semibold">Work</h2>
                <p className="text-sm text-gray-500">Open monthly worksheet.</p>
              </div>
            </div>
            <Button asChild>
              <Link to="/work" data-test-id="work-card-link">Open</Link>
            </Button>
          </div>
        </section>
      </div>
    </AppShell>
  );
}
