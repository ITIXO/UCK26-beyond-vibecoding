import { apiGet, apiPost } from "./api";
import type { LoginResponse, MeResponse } from "@/shared/lib/api/auth.contracts.api.ts";

export const login = (userName: string, password: string) =>
  apiPost<LoginResponse>("/api/auth/login", { userName, password });

export const fetchMe = () => apiGet<MeResponse>("/api/auth/me");
