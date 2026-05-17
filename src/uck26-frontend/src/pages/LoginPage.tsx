import { Navigate } from "react-router";
import { LoginForm } from "@/features/auth/LoginForm";
import { useAuth } from "@/features/auth/AuthProvider";

export function LoginPage() {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return null;
  }
  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-50 px-4">
      <LoginForm />
    </div>
  );
}
