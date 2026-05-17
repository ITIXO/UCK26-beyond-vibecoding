import { apiGet, apiPost } from "./api";

export interface MeResponse {
  id: number;
  userName: string;
  role: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresInSeconds: number;
  user: MeResponse;
}

export const login = (userName: string, password: string) =>
  apiPost<LoginResponse>("/api/auth/login", { userName, password });

export const fetchMe = () => apiGet<MeResponse>("/api/auth/me");
