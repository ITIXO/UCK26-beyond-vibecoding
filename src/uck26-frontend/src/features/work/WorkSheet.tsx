import { useMemo, useState } from "react";
import {
  Button,
  IconButton,
  Input,
  Popover,
  PopoverContent,
  PopoverTrigger,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@itixo/component-library";
import { ChevronLeft, ChevronRight, History } from "lucide-react";
import { WorkEntryType } from "@/shared/lib/api/worksheets.contracts.api";
import { useUpsertWorkEntry, useWorksheet } from "./worksheets.queries";

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

export function WorkSheet() {
  const today = new Date();
  const [year, setYear] = useState(today.getFullYear());
  const [month, setMonth] = useState(today.getMonth() + 1);
  const [pickerYear, setPickerYear] = useState(year);

  const worksheetQuery = useWorksheet(year, month);
  const upsertEntry = useUpsertWorkEntry(year, month);

  const days = useMemo(() => buildDays(year, month), [year, month]);
  const hoursByCell = useMemo(() => {
    const map = new Map<string, number>();
    worksheetQuery.data?.entries.forEach((entry) => {
      map.set(`${entry.date}:${entry.type}`, entry.hours);
    });
    return map;
  }, [worksheetQuery.data]);

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

  function saveEntry(date: string, type: WorkEntryType, value: string) {
    const hours = value.trim() === "" ? 0 : Number(value);
    if (!Number.isFinite(hours) || hours < 0) {
      return;
    }

    upsertEntry.mutate({ date, type, hours });
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-2xl font-semibold tracking-normal">
          {worksheetQuery.data?.userName ?? "Work"}
        </h1>

        <div className="flex items-center gap-2">
          <IconButton variant="outline" size="sm" onClick={() => shiftMonth(-1)} aria-label="Previous month">
            <ChevronLeft className="size-4" />
          </IconButton>

          <Popover>
            <PopoverTrigger asChild>
              <Input
                readOnly
                value={`${monthNames[month - 1]} ${year}`}
                className="h-10 w-40 cursor-pointer text-center"
                onClick={() => setPickerYear(year)}
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
                    <ChevronLeft className="size-4" />
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
                    <ChevronRight className="size-4" />
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
            <ChevronRight className="size-4" />
          </IconButton>
        </div>
      </div>

      <section className="overflow-auto rounded-lg border border-gray-200 bg-white shadow-sm">
        {worksheetQuery.isLoading && (
          <div className="px-4 py-6 text-sm text-gray-500">Loading worksheet...</div>
        )}

        {worksheetQuery.isError && (
          <div className="px-4 py-6 text-sm text-red-700">
            Failed to load worksheet: {worksheetQuery.error instanceof Error ? worksheetQuery.error.message : "unknown error"}
          </div>
        )}

        {worksheetQuery.data && (
          <Table className="min-w-[900px]">
            <TableHeader>
              <TableRow>
                <TableHead className="sticky left-0 z-20 w-32 bg-gray-50">Date</TableHead>
                <TableHead className="sticky left-32 z-20 w-24 bg-gray-50">Start</TableHead>
                <TableHead className="sticky left-56 z-20 w-24 bg-gray-50 shadow-[8px_0_16px_rgba(15,23,42,0.08)]">End</TableHead>
                <TableHead>Work</TableHead>
                <TableHead>Holiday</TableHead>
                <TableHead>Doctor</TableHead>
                <TableHead className="w-20 text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {days.map((day) => (
                <TableRow key={day.date} className={day.isWeekend ? "bg-gray-50/70" : undefined}>
                  <TableCell className="sticky left-0 z-10 w-32 bg-inherit font-medium">
                    {day.label}
                  </TableCell>
                  <TableCell className="sticky left-32 z-10 w-24 bg-inherit text-gray-600">
                    {day.isWeekend ? "" : "08:00"}
                  </TableCell>
                  <TableCell className="sticky left-56 z-10 w-24 bg-inherit text-gray-600 shadow-[8px_0_16px_rgba(15,23,42,0.08)]">
                    {day.isWeekend ? "" : "16:30"}
                  </TableCell>
                  {entryTypes.map((type) => {
                    const value = hoursByCell.get(`${day.date}:${type}`) ?? "";
                    return (
                      <TableCell key={type}>
                        <Input
                          key={`${day.date}-${type}-${value}`}
                          type="number"
                          min="0"
                          step="0.25"
                          defaultValue={value}
                          className="h-9 text-right"
                          onBlur={(event) => saveEntry(day.date, type, event.target.value)}
                        />
                      </TableCell>
                    );
                  })}
                  <TableCell className="text-right">
                    <IconButton variant="ghost" size="sm" aria-label={`History for ${day.label}`}>
                      <History className="size-4" />
                    </IconButton>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </section>
    </div>
  );
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
