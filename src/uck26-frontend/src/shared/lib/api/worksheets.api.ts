import { apiDelete, apiGet, apiPost, apiPut } from "./api";
import type { CreateWorkEntryRequest, UpdateWorkEntryRequest, WorkEntryDto, WorksheetDto } from "./worksheets.contracts.api";

export const getWorksheet = (year: number, month: number, userId?: number) => {
  const userQuery = userId === undefined ? "" : `&userId=${userId}`;
  return apiGet<WorksheetDto>(`/api/worksheets?year=${year}&month=${month}${userQuery}`);
};

export const createWorkEntry = (body: CreateWorkEntryRequest) =>
  apiPost<WorkEntryDto>("/api/worksheets/entries", body);

export const updateWorkEntry = (id: number, body: UpdateWorkEntryRequest) =>
  apiPut<WorkEntryDto>(`/api/worksheets/entries/${id}`, body);

export const deleteWorkEntry = (id: number) =>
  apiDelete<void>(`/api/worksheets/entries/${id}`);
