import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { getWorksheet, upsertWorkEntry } from "@/shared/lib/api/worksheets.api";
import type { UpsertWorkEntryRequest } from "@/shared/lib/api/worksheets.contracts.api";

const worksheetKey = (year: number, month: number) => ["worksheet", year, month] as const;

export function useWorksheet(year: number, month: number) {
  return useQuery({
    queryKey: worksheetKey(year, month),
    queryFn: () => getWorksheet(year, month),
  });
}

export function useUpsertWorkEntry(year: number, month: number) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: UpsertWorkEntryRequest) => upsertWorkEntry(body),
    onSuccess: () => qc.invalidateQueries({ queryKey: worksheetKey(year, month) }),
  });
}
