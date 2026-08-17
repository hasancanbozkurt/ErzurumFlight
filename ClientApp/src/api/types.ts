// Server/DTOs/*.cs ile birebir eşleşen TypeScript tipleri.
// Not: Server tarafında camelCase JSON serileştirme yapılandırıldı (Program.cs).

export type FlightStatus =
  | 'Scheduled'
  | 'Monitoring'
  | 'AircraftDetected'
  | 'Taxiing'
  | 'Departed'
  | 'Airborne'
  | 'Approaching'
  | 'NearAirport'
  | 'Landed'
  | 'Delayed'
  | 'Cancelled'
  | 'Diverted'
  | 'Unknown';

export interface FlightSummary {
  id: number;
  flightNumber: string;
  airlineName: string | null;
  originIata: string;
  destinationIata: string;
  scheduledDepartureUtc: string;
  scheduledArrivalUtc: string;
  scheduledDepartureLocal: string;
  scheduledArrivalLocal: string;
  status: FlightStatus;
  isVerified: boolean;
  hasLiveTracking: boolean;
}

export interface FlightDetail {
  id: number;
  flightNumber: string;
  airlineName: string | null;
  originIata: string;
  originName: string;
  destinationIata: string;
  destinationName: string;
  aircraftType: string | null;
  registration: string | null;
  scheduledDepartureUtc: string;
  estimatedDepartureUtc: string | null;
  actualDepartureUtc: string | null;
  scheduledArrivalUtc: string;
  estimatedArrivalUtc: string | null;
  actualArrivalUtc: string | null;
  status: FlightStatus;
  lastUpdateUtc: string | null;
  sourceName: string | null;
  isVerified: boolean;
  hasLiveTracking: boolean;
}

export interface DailyFlightCounts {
  date: string;
  departures: number;
  arrivals: number;
  live: number;
}

export interface CalendarDay {
  date: string;
  totalFlights: number;
  verifiedFlights: number;
  anyUnverified: boolean;
}

export interface LiveAircraft {
  flightOperationId: number | null;
  flightNumber: string | null;
  icaoHex: string;
  callsign: string | null;
  registration: string | null;
  latitude: number;
  longitude: number;
  altitude: number | null;
  groundSpeed: number | null;
  heading: number | null;
  status: string;
  timestampUtc: string;
}

export interface FlightStatusChangedEvent {
  flightInstanceId: number;
  flightNumber: string;
  status: string;
  timestampUtc: string;
}

export interface FlightPositionUpdatedEvent {
  flightOperationId: number;
  flightNumber: string | null;
  latitude: number;
  longitude: number;
  heading: number | null;
  timestampUtc: string;
}

export interface DataSourceStatus {
  id: number;
  name: string;
  type: string;
  isEnabled: boolean;
  priority: number;
  lastSuccessUtc: string | null;
  lastFailureUtc: string | null;
  lastError: string | null;
}
