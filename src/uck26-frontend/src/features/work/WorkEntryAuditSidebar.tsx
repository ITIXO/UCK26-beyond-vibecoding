import { useState } from "react";
import { History, X, ChevronDown } from "lucide-react";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  IconButton,
} from "@itixo/component-library";
import { useWorkEntryAudit } from "./worksheets.queries";
import type { AuditLogEntryDto } from "@/shared/lib/api/worksheets.contracts.api";

interface WorkEntryAuditSidebarProps {
  open: boolean;
  date: string | null;
  dateLabel: string;
  userId?: number;
  year: number;
  month: number;
  onClose: () => void;
}

function actionColor(action: AuditLogEntryDto["action"]): string {
  if (action === "Create") return "bg-green-500";
  if (action === "Update") return "bg-blue-500";
  return "bg-red-500";
}

function AuditEvent({ event, index, expanded, onToggle }: {
  event: AuditLogEntryDto;
  index: number;
  expanded: boolean;
  onToggle: () => void;
}) {
  const time = new Date(event.performedAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });

  return (
    <div
      className="border rounded-md overflow-hidden"
      data-test-id={`work-audit-event-${index}`}
    >
      <button
        type="button"
        className="w-full flex items-center gap-3 px-3 py-2 text-left hover:bg-muted/50"
        onClick={onToggle}
      >
        <span className={`w-2 h-2 rounded-full shrink-0 ${actionColor(event.action)}`} />
        <span className="text-sm font-medium flex-1">{event.action}</span>
        {event.entryType && (
          <span className="text-xs px-1.5 py-0.5 rounded bg-muted text-muted-foreground">{event.entryType}</span>
        )}
        <span className="text-xs text-muted-foreground">{event.performedBy}</span>
        <span className="text-xs text-muted-foreground">{time}</span>
        <ChevronDown
          className={`w-4 h-4 shrink-0 transition-transform ${expanded ? "rotate-180" : ""}`}
        />
      </button>

      {expanded && (
        <div className="px-3 pb-3 pt-1 border-t bg-muted/20 text-sm">
          {event.action === "Delete" && (
            <p className="text-muted-foreground italic">Entry removed</p>
          )}
          {event.action === "Create" && event.changedFields && event.changedFields.length > 0 && (
            <ul className="space-y-1">
              {event.changedFields.map((f) => (
                <li key={f.field} className="flex gap-2">
                  <span className="font-medium w-24 shrink-0">{f.field}</span>
                  <span className="text-muted-foreground">{f.newValue ?? "—"}</span>
                </li>
              ))}
            </ul>
          )}
          {event.action === "Update" && event.changedFields && event.changedFields.length > 0 && (
            <ul className="space-y-1">
              {event.changedFields.map((f) => (
                <li key={f.field} className="flex gap-2">
                  <span className="font-medium w-24 shrink-0">{f.field}</span>
                  <span className="line-through text-muted-foreground">{f.oldValue ?? "—"}</span>
                  <span>→</span>
                  <span>{f.newValue ?? "—"}</span>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  );
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
  const { data, isLoading } = useWorkEntryAudit(year, month, date, userId);
  const [expandedIndex, setExpandedIndex] = useState<number>(0);

  const toggle = (i: number) => setExpandedIndex((prev) => (prev === i ? -1 : i));

  return (
    <Sheet open={open} onOpenChange={(o) => !o && onClose()}>
      <SheetContent side="right" className="w-96 flex flex-col" data-test-id="work-history-sidebar">
        <SheetHeader className="flex flex-row items-center gap-2 pb-4 border-b">
          <History className="w-5 h-5 shrink-0" />
          <SheetTitle className="flex-1 text-base">{dateLabel}</SheetTitle>
          <IconButton variant="ghost" size="sm" aria-label="Close" onClick={onClose}>
            <X className="w-4 h-4" />
          </IconButton>
        </SheetHeader>

        <div className="flex-1 overflow-y-auto py-4 space-y-2">
          {isLoading && (
            <>
              {[0, 1, 2].map((i) => (
                <div key={i} className="h-10 rounded-md bg-muted animate-pulse" />
              ))}
            </>
          )}

          {!isLoading && (!data || data.length === 0) && (
            <p className="text-sm text-muted-foreground text-center py-8">No history for this day.</p>
          )}

          {!isLoading && data && data.length > 0 && data.map((event, i) => (
            <AuditEvent
              key={`${event.entityId}-${event.performedAt}`}
              event={event}
              index={i}
              expanded={expandedIndex === i}
              onToggle={() => toggle(i)}
            />
          ))}
        </div>
      </SheetContent>
    </Sheet>
  );
}
