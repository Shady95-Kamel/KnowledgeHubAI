
export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

export interface RegisterResponse {
  userId: string;
  message: string;
}

export interface VerifyEmailRequest {
  token: string;
}

export interface VerifyEmailResponse {
  success: boolean;
  message: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

/**
 * The BFF returns user information only.
 * Access and refresh tokens remain on the server.
 */

 export interface LoginResponse {
  success: boolean;
  message: string;
  data: {
    userId: string;
    email: string;
    role: string;
  };
}


export interface AuthMessageResponse {
  success: boolean;
  message: string;
}

export  interface CsrfTokenResponse {
  success: boolean;
  data: {
    requestToken: string;
  };
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
  confirmPassword: string;
}

export interface ApiMessage {
  message: string;
}
