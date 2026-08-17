import { Link } from 'react-router-dom';
import type { FlightSummary } from '../api/types';
import { statusLabel, isLiveStatus } from '../lib/format';

interface FlightCardProps {
  flight: FlightSummary;
  direction: 'departure' | 'arrival';
}

export function FlightCard({ flight, direction }: FlightCardProps) {
  const time = direction === 'departure' ? flight.scheduledDepartureLocal : flight.scheduledArrivalLocal;
  const route = `${flight.originIata} → ${flight.destinationIata}`;

  return (
    <Link to={`/flights/${flight.id}`} className={`flight-card status-${flight.status.toLowerCase()}`}>
      <div className="flight-card-time">{time}</div>
      <div className="flight-card-main">
        <div className="flight-card-number">
          {flight.flightNumber}
          {flight.airlineName && <span className="flight-card-airline"> · {flight.airlineName}</span>}
        </div>
        <div className="flight-card-route">{route}</div>
      </div>
      <div className="flight-card-status">
        {isLiveStatus(flight.status) && <span className="live-dot" title="Canlı takip" />}
        <span>{statusLabel(flight.status)}</span>
        {flight.isVerified ? (
          <span className="verified-badge verified">✓ Tarife doğrulandı</span>
        ) : (
          <span className="verified-badge unverified">! Tarife doğrulanmadı</span>
        )}
      </div>
    </Link>
  );
}
