import type { Role } from "@/shared/lib/api/users.contracts.api.ts";

export interface MeResponse {
  id: number;
  userName: string;
  role: Role;
}

export interface LoginResponse {
  accessToken: string;
  expiresInSeconds: number;
  user: MeResponse;
}