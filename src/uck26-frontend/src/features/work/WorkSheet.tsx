import { type FormEvent, useEffect, useMemo, useState } from "react";
import {
  Badge,
  Button,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  IconButton,
  Input,
  Label,
  Popover,
  PopoverContent,
  PopoverTrigger,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Separator,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Textarea,
} from "@itixo/component-library";
import { ChevronLeft, ChevronRight, Pencil, Plus, Trash2 } from "lucide-react";
import { useAuth } from "@/features/auth/AuthProvider";
import { useUsers } from "@/features/users/users.queries";
import { type WorkEntryDto, WorkEntryType } from "@/shared/lib/api/worksheets.contracts.api";
import { useCreateWorkEntry, useDeleteWorkEntry, useUpdateWorkEntry, useWorksheet } from "./worksheets.queries";

const monthNames = [
  "January",
  "February",
  "March",
  "April",
  "May",
  "June",
  "July",
  "August",
  "September",
  "October",
  "November",
  "December",
];

const entryTypes = [WorkEntryType.Work, WorkEntryType.Holiday, WorkEntryType.Doctor] as const;

type DialogState =
  | { mode: "create"; date: string; type: WorkEntryType }
  | { mode: "edit"; entry: WorkEntryDto };

export function WorkSheet() {
  const { user, isAdmin } = useAuth();
  const today = new Date();
  const [year, setYear] = useState(today.getFullYear());
  const [month, setMonth] = useState(today.getMonth() + 1);
  const [pickerYear, setPickerYear] = useState(year);
  const [dialog, setDialog] = useState<DialogState | null>(null);
  const [selectedUserId, setSelectedUserId] = useState<number | undefined>(user?.id);

  const usersQuery = useUsers({ enabled: isAdmin });
  const worksheetUserId = isAdmin ? selectedUserId : undefined;
  const worksheetQuery = useWorksheet(year, month, worksheetUserId);
  const createEntry = useCreateWorkEntry(year, month, worksheetUserId);
  const updateEntry = useUpdateWorkEntry(year, month, worksheetUserId);
  const deleteEntry = useDeleteWorkEntry(year, month, worksheetUserId);

  useEffect(() => {
    if (selectedUserId === undefined && user?.id !== undefined) {
      setSelectedUserId(user.id);
    }
  }, [selectedUserId, user?.id]);

  const days = useMemo(() => buildDays(year, month), [year, month]);
  const entriesByDate = useMemo(() => {
    const map = new Map<string, WorkEntryDto[]>();
    worksheetQuery.data?.entries.forEach((entry) => {
      map.set(entry.date, [...(map.get(entry.date) ?? []), entry]);
    });
    return map;
  }, [worksheetQuery.data?.entries]);
  const totalsByType = useMemo(() => {
    const map = new Map<WorkEntryType, number>();
    entryTypes.forEach((type) => map.set(type, 0));
    worksheetQuery.data?.entries.forEach((entry) => {
      map.set(entry.type, (map.get(entry.type) ?? 0) + entry.hours);
    });
    return map;
  }, [worksheetQuery.data?.entries]);

  function shiftMonth(delta: number) {
    const next = new Date(year, month - 1 + delta, 1);
    setYear(next.getFullYear());
    setMonth(next.getMonth() + 1);
    setPickerYear(next.getFullYear());
  }

  function selectMonth(nextMonth: number) {
    setYear(pickerYear);
    setMonth(nextMonth);
  }

  function closeDialog() {
    setDialog(null);
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        {isAdmin ? (
          <div className="w-full max-w-xs">
            <Label className="mb-1">User</Label>
            <Select
              value={selectedUserId?.toString() ?? ""}
              onValueChange={(value) => setSelectedUserId(Number(value))}
            >
              <SelectTrigger data-test-id="work-user-selector">
                <SelectValue placeholder="Select user" />
              </SelectTrigger>
              <SelectContent>
                {(usersQuery.data ?? []).map((item) => (
                  <SelectItem key={item.id} value={item.id.toString()}>
                    {item.userName}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        ) : (
          <h1 className="text-2xl font-semibold tracking-normal" data-test-id="work-title">
            {worksheetQuery.data?.userName ?? "Work"}
          </h1>
        )}

        <div className="flex items-center gap-2">
          <IconButton variant="outline" size="sm" onClick={() => shiftMonth(-1)} aria-label="Previous month">
            <ChevronLeft className="size-4"/>
          </IconButton>

          <Popover>
            <PopoverTrigger asChild>
              <Input
                readOnly
                value={`${monthNames[month - 1]} ${year}`}
                className="h-10 w-40 cursor-pointer text-center"
                onClick={() => setPickerYear(year)}
                data-test-id="work-month-picker"
              />
            </PopoverTrigger>
            <PopoverContent align="end" className="w-72">
              <div className="space-y-3">
                <div className="flex items-center justify-between gap-2">
                  <IconButton
                    variant="ghost"
                    size="sm"
                    onClick={() => setPickerYear((current) => current - 1)}
                    aria-label="Previous year"
                  >
                    <ChevronLeft className="size-4"/>
                  </IconButton>
                  <Input
                    type="number"
                    value={pickerYear}
                    onChange={(event) => setPickerYear(Number(event.target.value))}
                    className="h-10 text-center font-medium"
                  />
                  <IconButton
                    variant="ghost"
                    size="sm"
                    onClick={() => setPickerYear((current) => current + 1)}
                    aria-label="Next year"
                  >
                    <ChevronRight className="size-4"/>
                  </IconButton>
                </div>

                <div className="grid grid-cols-3 gap-2">
                  {monthNames.map((name, index) => {
                    const nextMonth = index + 1;
                    const active = pickerYear === year && nextMonth === month;
                    return (
                      <Button
                        key={name}
                        type="button"
                        variant={active ? "default" : "outline"}
                        className="h-9"
                        onClick={() => selectMonth(nextMonth)}
                      >
                        {name.slice(0, 3)}
                      </Button>
                    );
                  })}
                </div>
              </div>
            </PopoverContent>
          </Popover>

          <IconButton variant="outline" size="sm" onClick={() => shiftMonth(1)} aria-label="Next month">
            <ChevronRight className="size-4"/>
          </IconButton>
        </div>
      </div>

      <section className="overflow-auto rounded-lg border border-gray-200 bg-white shadow-sm" data-test-id="work-sheet">
        {worksheetQuery.isLoading && (
          <div className="px-4 py-6 text-sm text-gray-500">Loading worksheet...</div>
        )}

        {worksheetQuery.isError && (
          <div className="px-4 py-6 text-sm text-red-700">
            Failed to load worksheet: {worksheetQuery.error instanceof Error ? worksheetQuery.error.message : "unknown error"}
          </div>
        )}

        {worksheetQuery.data && (
          <Table className="min-w-235">
            <TableHeader>
              <TableRow>
                <TableHead className="sticky left-0 z-20 w-32 bg-gray-50">Date</TableHead>
                <TableHead className="sticky left-32 z-20 w-24 bg-gray-50">Start</TableHead>
                <TableHead className="sticky left-56 z-20 w-24 bg-gray-50 shadow-[8px_0_16px_rgba(15,23,42,0.08)]">End</TableHead>
                <TableHead>Work</TableHead>
                <TableHead>Holiday</TableHead>
                <TableHead>Doctor</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {days.map((day) => {
                const dayEntries = entriesByDate.get(day.date) ?? [];
                return (
                  <TableRow key={day.date} className={day.isWeekend ? "bg-gray-50/70" : undefined} data-test-id={`work-row-${day.date}`}>
                    <TableCell className="sticky left-0 z-10 w-32 bg-inherit font-medium">
                      {day.label}
                    </TableCell>
                    <TableCell className="sticky left-32 z-10 w-24 bg-inherit text-gray-600" data-test-id={`work-start-${day.date}`}>
                      {minStart(dayEntries)}
                    </TableCell>
                    <TableCell className="sticky left-56 z-10 w-24 bg-inherit text-gray-600 shadow-[8px_0_16px_rgba(15,23,42,0.08)]" data-test-id={`work-end-${day.date}`}>
                      {maxEnd(dayEntries)}
                    </TableCell>
                    {entryTypes.map((type) => (
                      <TableCell key={type}>
                        <EntryCell
                          date={day.date}
                          type={type}
                          entries={dayEntries.filter((entry) => entry.type === type)}
                          onCreate={() => setDialog({ mode: "create", date: day.date, type })}
                          onEdit={(entry) => setDialog({ mode: "edit", entry })}
                          onDelete={(entry) => deleteEntry.mutate(entry.id)}
                        />
                      </TableCell>
                    ))}
                  </TableRow>
                );
              })}
              <TableRow className="bg-blue-50 font-medium" data-test-id="work-summary-row">
                <TableCell className="sticky left-0 z-10 w-32 bg-blue-50">
                  Summary
                </TableCell>
                <TableCell className="sticky left-32 z-10 w-24 bg-blue-50" />
                <TableCell className="sticky left-56 z-10 w-24 bg-blue-50 shadow-[8px_0_16px_rgba(15,23,42,0.08)]" />
                {entryTypes.map((type) => (
                  <TableCell key={type} className="tabular-nums font-bold" data-test-id={`work-summary-${type}`}>
                    {formatHours(totalsByType.get(type) ?? 0)}
                  </TableCell>
                ))}
                <TableCell />
              </TableRow>
            </TableBody>
          </Table>
        )}
      </section>

      <EntryDialog
        state={dialog}
        pending={createEntry.isPending || updateEntry.isPending}
        onClose={closeDialog}
        onSubmit={async (body) => {
          if (!dialog) return;
          if (dialog.mode === "create") {
            await createEntry.mutateAsync({ date: dialog.date, type: dialog.type, userId: worksheetUserId, ...body });
          } else {
            await updateEntry.mutateAsync({ id: dialog.entry.id, body });
          }
          closeDialog();
        }}
      />
    </div>
  );
}

function EntryCell({
                     date,
                     type,
                     entries,
                     onCreate,
                     onEdit,
                     onDelete,
                   }: {
  date: string;
  type: WorkEntryType;
  entries: WorkEntryDto[];
  onCreate: () => void;
  onEdit: (entry: WorkEntryDto) => void;
  onDelete: (entry: WorkEntryDto) => void;
}) {
  if (entries.length === 0) {
    return (
      <div className="flex h-9 min-w-44 items-center">
        <IconButton variant="ghost" size="sm" aria-label={`Add ${type} entry`} onClick={onCreate} data-test-id={`work-add-${date}-${type}`}
          className="text-green-500 hover:text-green-600">
          <Plus/>
        </IconButton>
      </div>
    );
  }

  return (
    <div className="flex h-9 min-w-44 items-center justify-between gap-2" data-test-id={`work-cell-${date}-${type}`}>
      <div className="flex items-center gap-2">
        <Badge variant="secondary">{entries.length}</Badge>
        <span className="tabular-nums">{formatHours(entries.reduce((sum, entry) => sum + entry.hours, 0))}</span>
      </div>
      <div className="flex items-center gap-1">
        <IconButton variant="ghost" size="mini" aria-label={`Add ${type} entry`} onClick={onCreate} data-test-id={`work-add-${date}-${type}`}
                    className="text-green-500 hover:text-green-600">
          <Plus/>
        </IconButton>
        <Separator orientation="vertical" className="h-5"/>
        {entries.length === 1 ? (
          <>
            <IconButton variant="ghost" size="mini" aria-label={`Edit ${type} entry`} onClick={() => onEdit(entries[0])}
                        className="text-blue-500 hover:text-blue-600">
              <Pencil/>
            </IconButton>
            <Separator orientation="vertical" className="h-5"/>
            <IconButton variant="ghost" size="mini" aria-label={`Delete ${type} entry`} onClick={() => onDelete(entries[0])}
                        className="text-red-500 hover:text-red-600">
              <Trash2/>
            </IconButton>
          </>
        ) : (
          <>
            <RecordPicker entries={entries} action="edit" onSelect={onEdit}/>
            <Separator orientation="vertical" className="h-5"/>
            <RecordPicker entries={entries} action="delete" onSelect={onDelete}/>
          </>
        )}
      </div>
    </div>
  );
}

function RecordPicker({
                        entries,
                        action,
                        onSelect,
                      }: {
  entries: WorkEntryDto[];
  action: "edit" | "delete";
  onSelect: (entry: WorkEntryDto) => void;
}) {
  const Icon = action === "edit" ? Pencil : Trash2;
  return (
    <Popover>
      <PopoverTrigger asChild>
        <IconButton variant="ghost" size="mini" aria-label={`${action} entry`}>
          <Icon className="size-3"/>
        </IconButton>
      </PopoverTrigger>
      <PopoverContent align="end" className="w-64">
        <div className="space-y-1">
          {entries.map((entry) => (
            <Button
              key={entry.id}
              type="button"
              variant="ghost"
              className="h-auto w-full justify-start px-2 py-2 text-left"
              onClick={() => onSelect(entry)}
            >
              <span className="flex w-full flex-col">
                <span className="font-medium">{formatTime(entry.start)}-{formatTime(entry.end)}</span>
                <span className="truncate text-xs text-gray-500">{entry.description || "No description"}</span>
              </span>
            </Button>
          ))}
        </div>
      </PopoverContent>
    </Popover>
  );
}

function EntryDialog({
                       state,
                       pending,
                       onClose,
                       onSubmit,
                     }: {
  state: DialogState | null;
  pending: boolean;
  onClose: () => void;
  onSubmit: (body: { start: string; end: string; description: string }) => Promise<void>;
}) {
  const entry = state?.mode === "edit" ? state.entry : null;
  const [start, setStart] = useState("08:00");
  const [end, setEnd] = useState("16:00");
  const [description, setDescription] = useState("");

  useEffect(() => {
    setStart(entry ? formatTime(entry.start) : "08:00");
    setEnd(entry ? formatTime(entry.end) : "16:00");
    setDescription(entry?.description ?? "");
  }, [entry, state]);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onSubmit({ start, end, description });
  }

  return (
    <Dialog open={state !== null} onOpenChange={(open) => !open && onClose()}>
      <DialogContent data-test-id="work-entry-dialog">
        <DialogHeader>
          <DialogTitle>{state?.mode === "edit" ? "Edit entry" : "Add entry"}</DialogTitle>
        </DialogHeader>
        <form onSubmit={submit} className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label htmlFor="work-entry-start">Start</Label>
              <Input id="work-entry-start" type="time" value={start} onChange={(event) => setStart(event.target.value)} data-test-id="work-entry-start" required/>
            </div>
            <div className="space-y-1">
              <Label htmlFor="work-entry-end">End</Label>
              <Input id="work-entry-end" type="time" value={end} onChange={(event) => setEnd(event.target.value)} data-test-id="work-entry-end" required/>
            </div>
          </div>
          <div className="space-y-1">
            <Label htmlFor="work-entry-description">Description</Label>
            <Textarea
              id="work-entry-description"
              value={description}
              onChange={(event) => setDescription(event.target.value)}
              data-test-id="work-entry-description"
            />
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" disabled={pending} data-test-id="work-entry-submit">
              {pending ? "Saving..." : "Save"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function minStart(entries: WorkEntryDto[]) {
  if (entries.length === 0) return "";
  return formatTime(entries.map((entry) => entry.start).sort()[0]);
}

function maxEnd(entries: WorkEntryDto[]) {
  if (entries.length === 0) return "";
  return formatTime(entries.map((entry) => entry.end).sort().at(-1) ?? "");
}

function formatTime(value: string) {
  return value.slice(0, 5);
}

function formatHours(hours: number) {
  const minutes = Math.round(hours * 60);
  return `${String(Math.floor(minutes / 60)).padStart(2, "0")}:${String(minutes % 60).padStart(2, "0")}`;
}

function buildDays(year: number, month: number) {
  const count = new Date(year, month, 0).getDate();
  return Array.from({ length: count }, (_, index) => {
    const day = index + 1;
    const date = new Date(year, month - 1, day);
    return {
      date: `${year}-${String(month).padStart(2, "0")}-${String(day).padStart(2, "0")}`,
      label: date.toLocaleDateString(undefined, { weekday: "short", day: "2-digit", month: "2-digit" }),
      isWeekend: date.getDay() === 0 || date.getDay() === 6,
    };
  });
}
