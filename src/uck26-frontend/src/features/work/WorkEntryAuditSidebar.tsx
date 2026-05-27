import { useState, useEffect } from "react";
import { History, ChevronDown } from "lucide-react";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
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

const dotFilled: Record<AuditLogEntryDto["action"], string> = {
  Create: "bg-green-500 border-green-500",
  Update: "bg-blue-500 border-blue-500",
  Delete: "bg-red-500 border-red-500",
};

const dotOutline: Record<AuditLogEntryDto["action"], string> = {
  Create: "bg-background border-green-500",
  Update: "bg-background border-blue-500",
  Delete: "bg-background border-red-500",
};

function FieldList({ fields, strikethrough }: {
  fields: AuditLogEntryDto["changedFields"];
  strikethrough?: boolean;
}) {
  if (!fields || fields.length === 0) return null;
  return (
    <ul className="space-y-1 pt-2 border-t mt-2">
      {fields.map((f) => (
        <li key={f.field} className="flex gap-2 text-sm">
          <span className="font-medium w-24 shrink-0 text-foreground">{f.field}</span>
          {strikethrough ? (
            <span className="text-muted-foreground line-through">{f.oldValue ?? "—"}</span>
          ) : f.oldValue !== null && f.newValue !== null ? (
            <>
              <span className="line-through text-muted-foreground">{f.oldValue}</span>
              <span className="text-muted-foreground">→</span>
              <span>{f.newValue}</span>
            </>
          ) : (
            <span className="text-muted-foreground">{f.newValue ?? f.oldValue ?? "—"}</span>
          )}
        </li>
      ))}
    </ul>
  );
}

function AuditEvent({ event, index, expanded, onToggle, isLast }: {
  event: AuditLogEntryDto;
  index: number;
  expanded: boolean;
  onToggle: () => void;
  isLast: boolean;
}) {
  const time = new Date(event.performedAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });

  return (
    <div className="flex gap-3" data-test-id={`work-audit-event-${index}`}>
      {/* Timeline column */}
      <div className="flex flex-col items-center w-5 shrink-0">
        <span className={`w-3 h-3 rounded-full shrink-0 mt-3 z-10 border-2 transition-colors ${expanded ? dotFilled[event.action] : dotOutline[event.action]}`} />
        {!isLast && <div className="w-px flex-1 bg-border mt-1" />}
      </div>

      {/* Card */}
      <div className="flex-1 mb-3 border rounded-md overflow-hidden">
        <button
          type="button"
          className="w-full flex items-center gap-2 px-3 py-2 text-left hover:bg-muted/50"
          onClick={onToggle}
        >
          <span className="text-sm font-medium flex-1">{event.action}</span>
          {event.entryType && (
            <span className="text-xs px-1.5 py-0.5 rounded bg-muted text-muted-foreground">{event.entryType}</span>
          )}
          <span className="text-xs text-muted-foreground">{event.performedBy}</span>
          <span className="text-xs text-muted-foreground">{time}</span>
          <ChevronDown className={`w-4 h-4 shrink-0 transition-transform ${expanded ? "rotate-180" : ""}`} />
        </button>

        {expanded && (
          <div className="px-3 pb-3">
            {event.action === "Create" && (
              <FieldList fields={event.changedFields} />
            )}
            {event.action === "Update" && (
              <FieldList fields={event.changedFields} />
            )}
            {event.action === "Delete" && (
              <p className="text-sm text-muted-foreground pt-2 border-t mt-2" data-test-id={`work-audit-delete-${index}`}>
                Entry removed
              </p>
            )}
          </div>
        )}
      </div>
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
  useEffect(() => setExpandedIndex(0), [date]);

  const toggle = (i: number) => setExpandedIndex((prev) => (prev === i ? -1 : i));

  return (
    <Sheet open={open} onOpenChange={(o) => !o && onClose()}>
      <SheetContent side="right" className="w-96 flex flex-col p-6 gap-0" data-test-id="work-history-sidebar">
        <SheetHeader className="flex flex-row items-center gap-2 pb-4 border-b mb-4">
          <History className="w-5 h-5 shrink-0" />
          <SheetTitle className="flex-1 text-base">{dateLabel}</SheetTitle>
        </SheetHeader>

        <div className="flex-1 overflow-y-auto">
          {isLoading && (
            <div className="space-y-3">
              {[0, 1, 2].map((i) => (
                <div key={i} className="h-10 rounded-md bg-muted animate-pulse" />
              ))}
            </div>
          )}

          {!isLoading && (!data || data.length === 0) && (
            <p className="text-sm text-muted-foreground text-center py-8" data-test-id="work-history-empty">
              No history for this day.
            </p>
          )}

          {!isLoading && data && data.length > 0 && data.map((event, i) => (
            <AuditEvent
              key={`${event.entityId}-${event.performedAt}`}
              event={event}
              index={i}
              expanded={expandedIndex === i}
              onToggle={() => toggle(i)}
              isLast={i === data.length - 1}
            />
          ))}
        </div>
      </SheetContent>
    </Sheet>
  );
}
