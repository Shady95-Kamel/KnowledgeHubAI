
export interface CurrentUser {
  userId: string;
  email: string;
  role: string;
  refreshTokenExpiresAtUtc: string;
}

export interface CurrentUserResponse {
  success: boolean;
  data: CurrentUser;
}
