export type User = {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  token?: string;
};

export type LoginRequest = {
  email: string;
  password: string;
};

export type RegisterRequest = {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
};

export type AuthResponse = {
  userId: number;
  email: string;
  firstName?: string;
  lastName?: string;
  displayName?: string;
  roles: string[];
  token: string;
  refreshToken?: string;
};
