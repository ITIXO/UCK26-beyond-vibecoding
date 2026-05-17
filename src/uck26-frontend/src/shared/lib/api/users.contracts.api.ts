export interface UserDto {
  id: number;
  userName: string;
  role: Role;
  createdAt: string;
}

export interface CreateUserRequest {
  userName: string;
  password: string;
  role: Role;
}

export interface UpdateUserRequest {
  userName?: string;
  role?: Role;
  password?: string;
}

export enum Role { Admin = "admin", User = "user" }