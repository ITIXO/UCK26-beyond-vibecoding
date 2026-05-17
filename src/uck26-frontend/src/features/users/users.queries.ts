import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createUser,
  deleteUser,
  listUsers,
  updateUser,
} from "@/shared/lib/api/users.api";
import type { CreateUserRequest, UpdateUserRequest } from "@/shared/lib/api/users.api";

const usersKey = ["users"] as const;

export function useUsers(options?: { enabled?: boolean }) {
  return useQuery({
    queryKey: usersKey,
    queryFn: listUsers,
    enabled: options?.enabled,
  });
}

export function useCreateUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateUserRequest) => createUser(body),
    onSuccess: () => qc.invalidateQueries({ queryKey: usersKey }),
  });
}

export function useUpdateUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: number; body: UpdateUserRequest }) => updateUser(id, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: usersKey }),
  });
}

export function useDeleteUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => deleteUser(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: usersKey }),
  });
}
