import { apiDelete, apiGet, apiPost, apiPut } from "./api";
import type { CreateUserRequest, UpdateUserRequest, UserDto } from "@/shared/lib/api/users.contracts.api.ts";

export type { CreateUserRequest, UpdateUserRequest };

export const listUsers = () => apiGet<UserDto[]>("/api/users");

export const createUser = (body: CreateUserRequest) => apiPost<UserDto>("/api/users", body);

export const updateUser = (id: number, body: UpdateUserRequest) =>
  apiPut<UserDto>(`/api/users/${id}`, body);

export const deleteUser = (id: number) => apiDelete<void>(`/api/users/${id}`);
