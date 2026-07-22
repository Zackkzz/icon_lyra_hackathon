import React, { createContext, useContext, useEffect, useState } from "react";
import { api, setAuthToken, AuthResponse } from "../services/api";
import { storage } from "./storage";

const TOKEN_KEY = "fmp_token";
const USER_KEY = "fmp_user";

interface AuthUser {
  userId: number;
  email: string;
  displayName: string;
}

interface AuthState {
  user: AuthUser | null;
  loading: boolean;
  signIn: (email: string, password: string) => Promise<void>;
  signUp: (email: string, password: string, displayName?: string) => Promise<void>;
  signOut: () => Promise<void>;
}

const AuthContext = createContext<AuthState | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [loading, setLoading] = useState(true);

  // Restore a saved session on launch.
  useEffect(() => {
    (async () => {
      try {
        const token = await storage.get(TOKEN_KEY);
        const rawUser = await storage.get(USER_KEY);
        if (token && rawUser) {
          setAuthToken(token);
          setUser(JSON.parse(rawUser));
        }
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  async function persist(res: AuthResponse) {
    const u: AuthUser = { userId: res.userId, email: res.email, displayName: res.displayName };
    setAuthToken(res.token);
    await storage.set(TOKEN_KEY, res.token);
    await storage.set(USER_KEY, JSON.stringify(u));
    setUser(u);
  }

  async function signIn(email: string, password: string) {
    const res = await api.login(email.trim(), password);
    await persist(res);
  }

  async function signUp(email: string, password: string, displayName?: string) {
    const res = await api.register(email.trim(), password, displayName);
    await persist(res);
  }

  async function signOut() {
    setAuthToken(null);
    await storage.remove(TOKEN_KEY);
    await storage.remove(USER_KEY);
    setUser(null);
  }

  return (
    <AuthContext.Provider value={{ user, loading, signIn, signUp, signOut }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within an AuthProvider");
  return ctx;
}
