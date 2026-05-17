import { apiDelete, apiGet, apiPost, apiPut } from "./api";

export interface UserDto {
  id: number;
  userName: string;
  role: string;
  createdAt: string;
}

export interface CreateUserRequest {
  userName: string;
  password: string;
  role: "Admin" | "User";
}

export interface UpdateUserRequest {
  userName?: string;
  role?: "Admin" | "User";
  password?: string;
}

export const listUsers = () => apiGet<UserDto[]>("/api/users");

export const createUser = (body: CreateUserRequest) => apiPost<UserDto>("/api/users", body);

export const updateUser = (id: number, body: UpdateUserRequest) =>
  apiPut<UserDto>(`/api/users/${id}`, body);

export const deleteUser = (id: number) => apiDelete<void>(`/api/users/${id}`);
