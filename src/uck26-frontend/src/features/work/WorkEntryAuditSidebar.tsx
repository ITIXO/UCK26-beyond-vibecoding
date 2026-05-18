import { useState } from "react";
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
  Drawer,
  DrawerContent,
  DrawerHeader,
  DrawerTitle,
  IconButton,
} from "@itixo/component-library";
import { History, X } from "lucide-react";
import type { AuditAction, AuditChangedFieldDto } from "@/shared/lib/api/worksheets.contracts.api";
import { useWorkEntryAudit } from "./worksheets.queries";

interface WorkEntryAuditSidebarProps {
  open: boolean;
  date: string | null;
  dateLabel: string;
  userId?: number;
  year: number;
  month: number;
  onClose: () => void;
}

export function WorkEntryAuditSidebar({
  open,
  date,
  dateLabel,
  userId,
  year,
  month,
  onClose,
}: WorkEntryAuditSidebarProps) {
  const auditQuery = useWorkEntryAudit(year, month, date, userId);
  const [openItem, setOpenItem] = useState("event-0");

  return (
    <Drawer open={open} onOpenChange={(nextOpen) => !nextOpen && onClose()} direction="right">
      <DrawerContent
        className="fixed inset-y-0 right-0 left-auto mt-0 h-full w-full max-w-md rounded-none border-l bg-white shadow-xl transition-transform"
        data-test-id="work-history-sidebar"
      >
        <DrawerHeader className="border-b px-5 py-4">
          <div className="flex items-center justify-between gap-3">
            <DrawerTitle className="flex items-center gap-2 text-base font-semibold">
              <History className="size-4 text-blue-600" />
              <span data-test-id="work-history-title">{dateLabel}</span>
            </DrawerTitle>
            <IconButton variant="ghost" size="mini" aria-label="Close history" onClick={onClose}>
              <X className="size-4" />
            </IconButton>
          </div>
        </DrawerHeader>

        <div className="h-full overflow-y-auto px-5 py-4">
          {auditQuery.isLoading && (
            <div className="space-y-3">
              {[0, 1, 2].map((item) => (
                <div key={item} className="h-20 animate-pulse rounded-md bg-gray-100" />
              ))}
            </div>
          )}

          {!auditQuery.isLoading && (auditQuery.data?.length ?? 0) === 0 && (
            <div className="flex h-48 items-center justify-center text-sm text-gray-500" data-test-id="work-history-empty">
              No history for this day.
            </div>
          )}

          {auditQuery.data && auditQuery.data.length > 0 && (
            <Accordion type="single" collapsible value={openItem} onValueChange={(value) => setOpenItem(value)}>
              <div className="space-y-3">
                {auditQuery.data.map((event, index) => (
                  <div key={`${event.performedAt}-${index}`} className="relative pl-6" data-test-id={`work-audit-event-${index}`}>
                    <span className={`absolute top-4 left-0 size-3 rounded-full ${dotColor(event.action)}`} />
                    <AccordionItem value={`event-${index}`} className="rounded-md border border-gray-200 px-3">
                      <AccordionTrigger className="py-3 text-left hover:no-underline">
                        <span className="flex flex-col gap-1">
                          <span className="text-sm font-medium">{event.action} by {event.performedBy}</span>
                          <span className="text-xs text-gray-500">{formatDateTime(event.performedAt)}</span>
                        </span>
                      </AccordionTrigger>
                      <AccordionContent>
                        <AuditEventBody action={event.action} fields={event.changedFields} />
                      </AccordionContent>
                    </AccordionItem>
                  </div>
                ))}
              </div>
            </Accordion>
          )}
        </div>
      </DrawerContent>
    </Drawer>
  );
}

function AuditEventBody({ action, fields }: { action: AuditAction; fields: AuditChangedFieldDto[] | null }) {
  if (action === "Delete") {
    return <div className="pb-3 text-sm text-gray-600">Entry removed</div>;
  }

  return (
    <div className="space-y-2 pb-3 text-sm">
      {(fields ?? []).map((field) => (
        <div key={field.field} className="flex items-start justify-between gap-3">
          <span className="font-medium text-gray-700">{field.field}</span>
          {action === "Create" ? (
            <span className="max-w-48 truncate text-gray-600">{field.newValue || "-"}</span>
          ) : (
            <span className="max-w-56 truncate text-gray-600">
              {field.oldValue || "-"} {"->"} {field.newValue || "-"}
            </span>
          )}
        </div>
      ))}
    </div>
  );
}

function dotColor(action: AuditAction) {
  if (action === "Create") return "bg-green-500";
  if (action === "Update") return "bg-blue-500";
  return "bg-red-500";
}

function formatDateTime(value: string) {
  return new Date(value).toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  });
}
