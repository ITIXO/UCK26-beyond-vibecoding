import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createWorkEntry, deleteWorkEntry, getWorkEntryAudit, getWorksheet, updateWorkEntry } from "@/shared/lib/api/worksheets.api";
import type { CreateWorkEntryRequest, UpdateWorkEntryRequest } from "@/shared/lib/api/worksheets.contracts.api";

const worksheetKey = (year: number, month: number, userId?: number) => ["worksheet", year, month, userId] as const;
const worksheetAuditKey = (year: number, month: number, date: string | null, userId?: number) =>
  ["worksheet-audit", year, month, date, userId] as const;

export function useWorksheet(year: number, month: number, userId?: number) {
  return useQuery({
    queryKey: worksheetKey(year, month, userId),
    queryFn: () => getWorksheet(year, month, userId),
  });
}

export function useCreateWorkEntry(year: number, month: number, userId?: number) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateWorkEntryRequest) => createWorkEntry(body),
    onSuccess: () => qc.invalidateQueries({ queryKey: worksheetKey(year, month, userId) }),
  });
}

export function useUpdateWorkEntry(year: number, month: number, userId?: number) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: number; body: UpdateWorkEntryRequest }) => updateWorkEntry(id, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: worksheetKey(year, month, userId) }),
  });
}

export function useDeleteWorkEntry(year: number, month: number, userId?: number) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => deleteWorkEntry(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: worksheetKey(year, month, userId) }),
  });
}

export function useWorkEntryAudit(year: number, month: number, date: string | null, userId?: number) {
  return useQuery({
    queryKey: worksheetAuditKey(year, month, date, userId),
    queryFn: () => getWorkEntryAudit(year, month, date!, userId),
    enabled: date !== null,
  });
}
