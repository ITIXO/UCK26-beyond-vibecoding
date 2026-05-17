import { UserList } from "@/features/users/UserList";
import { AppShell } from "@/shared/ui/AppShell";

export function UsersPage() {
  return (
    <AppShell title="UserManagement">
      <UserList />
    </AppShell>
  );
}
