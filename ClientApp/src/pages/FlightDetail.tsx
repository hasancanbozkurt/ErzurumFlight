import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api } from '../api/client';
import type { FlightDetail as FlightDetailType } from '../api/types';
import { statusLabel, isLiveStatus } from '../lib/format';
import { useFlightHub } from '../hooks/useFlightHub';

function fmt(dt: string | null): string {
  if (!dt) return '—';
  return new Date(dt).toLocaleString('tr-TR', {
    timeZone: 'Europe/Istanbul',
    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit',
  });
}

export function FlightDetail() {
  const { id } = useParams<{ id: string }>();
  const [flight, setFlight] = useState<FlightDetailType | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    if (!id) return;
    try {
      const data = await api.getFlightDetail(Number(id));
      setFlight(data);
    } catch {
      setError('Uçuş bulunamadı.');
    }
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  useFlightHub({
    onStatusChanged: (evt) => {
      if (flight && evt.flightInstanceId === flight.id) load();
    },
  });

  if (error) return <div className="page state-message error">{error}</div>;
  if (!flight) return <div className="page state-message">Yükleniyor…</div>;

  const rows: [string, string][] = [
    ['Uçuş Numarası', flight.flightNumber],
    ['Havayolu', flight.airlineName ?? '—'],
    ['Uçak', flight.aircraftType ?? '—'],
    ['Kayıt', flight.registration ?? '—'],
    ['Kalkış', `${flight.originIata} — ${flight.originName}`],
    ['Varış', `${flight.destinationIata} — ${flight.destinationName}`],
    ['Planlanan Kalkış', fmt(flight.scheduledDepartureUtc)],
    ['Tahmini Kalkış', fmt(flight.estimatedDepartureUtc)],
    ['Gerçek Kalkış', fmt(flight.actualDepartureUtc)],
    ['Planlanan Varış', fmt(flight.scheduledArrivalUtc)],
    ['Tahmini Varış', fmt(flight.estimatedArrivalUtc)],
    ['Gerçek Varış', fmt(flight.actualArrivalUtc)],
    ['Durum', statusLabel(flight.status)],
    ['Son Güncelleme', fmt(flight.lastUpdateUtc)],
    ['Veri Kaynağı', flight.sourceName ?? '—'],
  ];

  return (
    <div className="page flight-detail">
      <Link to="/" className="back-link">← Ana sayfaya dön</Link>

      <h2>{flight.flightNumber} · {flight.originIata} → {flight.destinationIata}</h2>

      {flight.isVerified ? (
        <span className="verified-badge verified">✓ Tarife doğrulandı</span>
      ) : (
        <span className="verified-badge unverified">! Tarife doğrulanmadı</span>
      )}

      <table className="detail-table">
        <tbody>
          {rows.map(([label, value]) => (
            <tr key={label}>
              <th>{label}</th>
              <td>{value}</td>
            </tr>
          ))}
        </tbody>
      </table>

      {isLiveStatus(flight.status) && (
        <Link to="/live" className="live-track-button">CANLI TAKİP ET</Link>
      )}
    </div>
  );
}
