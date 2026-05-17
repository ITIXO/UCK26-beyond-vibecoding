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
  hours: number;
}

export interface UpsertWorkEntryRequest {
  date: string;
  type: WorkEntryType;
  hours: number;
}

export enum WorkEntryType {
  Work = "work",
  Holiday = "holiday",
  Doctor = "doctor",
}
