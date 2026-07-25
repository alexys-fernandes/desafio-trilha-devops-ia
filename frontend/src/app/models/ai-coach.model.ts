export interface AICoachRequest {
  userId: number;
  message: string;
  contextSummary?: string;
}

export interface AICoachResponse {
  success: boolean;
  provider: string;
  response: string;
  error?: string;
}
