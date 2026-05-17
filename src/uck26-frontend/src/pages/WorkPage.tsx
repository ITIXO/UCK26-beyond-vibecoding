import { WorkSheet } from "@/features/work/WorkSheet";
import { AppShell } from "@/shared/ui/AppShell";

export function WorkPage() {
  return (
    <AppShell title="">
      <WorkSheet />
    </AppShell>
  );
}
