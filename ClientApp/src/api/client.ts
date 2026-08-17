import type {
  FlightSummary,
  FlightDetail,
  DailyFlightCounts,
  CalendarDay,
  LiveAircraft,
  DataSourceStatus,
} from './types';

// Vite dev proxy /api isteklerini backend'e yönlendirir (vite.config.ts); production'da
// aynı origin'den (ASP.NET Core, wwwroot üzerinden derlenmiş frontend'i de sunabilir) çalışır.
const API_BASE = '/api';

class ApiError extends Error {
  status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    credentials: 'include', // Admin cookie authentication için gerekli.
    headers: { 'Content-Type': 'application/json', ...(options?.headers ?? {}) },
    ...options,
  });

  if (!response.ok) {
    let message = `İstek başarısız oldu (${response.status})`;
    try {
      const body = await response.json();
      message = body?.error ?? message;
    } catch {
      /* JSON olmayan hata gövdesi göz ardı edilir. */
    }
    throw new ApiError(response.status, message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export const api = {
  // ---- Uçuşlar ----
  getFlights: (date: string, direction: 'departure' | 'arrival') =>
    request<FlightSummary[]>(`/flights?date=${date}&direction=${direction}`),

  getUpcoming: (days: number) =>
    request<FlightSummary[]>(`/flights/upcoming?days=${days}`),

  getDailyCounts: (date: string) =>
    request<DailyFlightCounts>(`/flights/counts?date=${date}`),

  getFlightDetail: (id: number) =>
    request<FlightDetail>(`/flights/${id}`),

  // ---- Takvim ----
  getCalendar: (from: string, to: string) =>
    request<CalendarDay[]>(`/calendar?from=${from}&to=${to}`),

  // ---- Canlı ----
  getLiveAircraft: () => request<LiveAircraft[]>('/live/aircraft'),

  // ---- Auth ----
  login: (userName: string, password: string) =>
    request<{ message: string }>('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ userName, password }),
    }),

  logout: () => request<{ message: string }>('/auth/logout', { method: 'POST' }),

  me: () => request<{ userName: string; displayName: string }>('/auth/me'),

  // ---- Admin ----
  getAdminSchedules: () => request<unknown[]>('/admin/schedules'),

  getAdminSources: () => request<DataSourceStatus[]>('/admin/sources'),
};

export { ApiError };
