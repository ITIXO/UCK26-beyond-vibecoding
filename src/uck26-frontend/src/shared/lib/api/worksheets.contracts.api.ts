export interface WorksheetDto {
  id: number;
  userId: number;
  userName: string;
  year: number;
  month: number;
  entries: WorkEntryDto[];
}

export interface WorkEntryDto {
  id: number;
  date: string;
  type: WorkEntryType;
  start: string;
  end: string;
  description: string;
  hours: number;
}

export interface CreateWorkEntryRequest {
  date: string;
  type: WorkEntryType;
  start: string;
  end: string;
  description: string;
  userId?: number;
}

export interface UpdateWorkEntryRequest {
  start: string;
  end: string;
  description: string;
}

export enum WorkEntryType {
  Work = "work",
  Holiday = "holiday",
  Doctor = "doctor",
}

export type AuditAction = "Create" | "Update" | "Delete";

export interface AuditChangedFieldDto {
  field: string;
  oldValue: string | null;
  newValue: string | null;
}

export interface AuditLogEntryDto {
  entityId: number;
  action: AuditAction;
  performedBy: string;
  performedAt: string;
  entryType: WorkEntryType | null;
  changedFields: AuditChangedFieldDto[] | null;
}
