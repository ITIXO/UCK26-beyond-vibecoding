import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createWorkEntry, deleteWorkEntry, getWorksheet, updateWorkEntry } from "@/shared/lib/api/worksheets.api";
import type { CreateWorkEntryRequest, UpdateWorkEntryRequest } from "@/shared/lib/api/worksheets.contracts.api";

const worksheetKey = (year: number, month: number, userId?: number) => ["worksheet", year, month, userId] as const;

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
