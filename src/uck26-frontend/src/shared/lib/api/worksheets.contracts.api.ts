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
