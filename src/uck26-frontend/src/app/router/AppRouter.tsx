import { Route, Routes } from "react-router";
import { RequireAuth } from "@/features/auth/RequireAuth";
import { HomePage } from "@/pages/HomePage";
import { LoginPage } from "@/pages/LoginPage";
import { UsersPage } from "@/pages/UsersPage";
import { WorkPage } from "@/pages/WorkPage";
import { Role } from "@/shared/lib/api/users.contracts.api.ts";

export function AppRouter() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage/>}/>
      <Route
        path="/"
        element={
          <RequireAuth>
            <HomePage/>
          </RequireAuth>
        }
      />
      <Route
        path="/work"
        element={
          <RequireAuth>
            <WorkPage/>
          </RequireAuth>
        }
      />
      <Route
        path="/users"
        element={
          <RequireAuth role={Role.Admin}>
            <UsersPage/>
          </RequireAuth>
        }
      />
      <Route path="*" element={<LoginPage/>}/>
    </Routes>
  );
}
