import { apiGet, apiPut } from "./api";
import type { UpsertWorkEntryRequest, WorkEntryDto, WorksheetDto } from "./worksheets.contracts.api";

export const getWorksheet = (year: number, month: number) =>
  apiGet<WorksheetDto>(`/api/worksheets?year=${year}&month=${month}`);

export const upsertWorkEntry = (body: UpsertWorkEntryRequest) =>
  apiPut<WorkEntryDto | void>("/api/worksheets/entries", body);
