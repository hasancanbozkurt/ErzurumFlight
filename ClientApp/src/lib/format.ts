import type { FlightStatus } from '../api/types';

/** Bugünün tarihini YYYY-MM-DD formatında döndürür (yerel tarayıcı saatine göre, sunucu Europe/Istanbul kullanır). */
export function todayIso(): string {
  const now = new Date();
  return toIsoDate(now);
}

export function toIsoDate(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

export function addDaysIso(iso: string, days: number): string {
  const [y, m, d] = iso.split('-').map(Number);
  const date = new Date(y, m - 1, d + days);
  return toIsoDate(date);
}

export function formatDateLong(iso: string): string {
  const [y, m, d] = iso.split('-').map(Number);
  const date = new Date(y, m - 1, d);
  return date.toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', year: 'numeric', weekday: 'long' });
}

const STATUS_LABELS_TR: Record<FlightStatus, string> = {
  Scheduled: 'Planlandı',
  Monitoring: 'İzleniyor',
  AircraftDetected: 'Uçak Tespit Edildi',
  Taxiing: 'Taksi',
  Departed: 'Kalktı',
  Airborne: 'Havada',
  Approaching: 'Yaklaşıyor',
  NearAirport: 'Havalimanı Yakınında',
  Landed: 'İndi',
  Delayed: 'Gecikti',
  Cancelled: 'İptal Edildi',
  Diverted: 'Yönlendirildi',
  Unknown: 'Bilinmiyor',
};

export function statusLabel(status: FlightStatus): string {
  return STATUS_LABELS_TR[status] ?? status;
}

export function isLiveStatus(status: FlightStatus): boolean {
  return ['Monitoring', 'AircraftDetected', 'Taxiing', 'Departed', 'Airborne', 'Approaching', 'NearAirport'].includes(status);
}
