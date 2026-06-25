export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

export interface PagedResponse<T> {
  success: boolean;
  data: T[];
  page: number;
  limit: number;
  total: number;
}
