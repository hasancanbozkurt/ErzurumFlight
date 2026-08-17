import { useEffect, useMemo, useState } from 'react';
import { MapContainer, TileLayer, Marker, Popup } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { api } from '../api/client';
import type { LiveAircraft } from '../api/types';
import { useFlightHub } from '../hooks/useFlightHub';

const ERZURUM_LAT = 39.9565;
const ERZURUM_LON = 41.1702;

type FilterKey = 'all' | 'departure' | 'arrival' | 'active';

/** Uçak yönünü (heading) yansıtan basit SVG ikon; her uçuş için ayrı div-icon oluşturulur. */
function aircraftIcon(heading: number | null): L.DivIcon {
  const rotation = heading ?? 0;
  return L.divIcon({
    className: 'aircraft-marker',
    html: `<div style="transform: rotate(${rotation}deg)">✈</div>`,
    iconSize: [24, 24],
    iconAnchor: [12, 12],
  });
}

export function LiveMap() {
  const [aircraft, setAircraft] = useState<LiveAircraft[]>([]);
  const [filter, setFilter] = useState<FilterKey>('all');
  const [error, setError] = useState<string | null>(null);

  async function load() {
    try {
      const data = await api.getLiveAircraft();
      setAircraft(data);
      setError(null);
    } catch {
      setError('Canlı veri kaynağı şu anda güncellenemiyor. Son bilinen konumlar gösteriliyor.');
    }
  }

  useEffect(() => {
    load();
    const interval = setInterval(load, 30_000); // Polling geri düşüşü; asıl güncellemeler SignalR ile gelir.
    return () => clearInterval(interval);
  }, []);

  const { isConnected } = useFlightHub({
    onPositionUpdated: (evt) => {
      setAircraft((prev) => {
        const idx = prev.findIndex((a) => a.flightOperationId === evt.flightOperationId);
        if (idx === -1) return prev;
        const updated = [...prev];
        updated[idx] = { ...updated[idx], latitude: evt.latitude, longitude: evt.longitude, heading: evt.heading, timestampUtc: evt.timestampUtc };
        return updated;
      });
    },
  });

  const filtered = useMemo(() => {
    switch (filter) {
      case 'departure':
        return aircraft.filter((a) => a.status === 'Departed' || a.status === 'Taxiing');
      case 'arrival':
        return aircraft.filter((a) => a.status === 'Approaching' || a.status === 'NearAirport');
      case 'active':
        return aircraft.filter((a) => a.flightOperationId !== null);
      default:
        return aircraft;
    }
  }, [aircraft, filter]);

  return (
    <div className="page live-map-page">
      <div className="live-map-header">
        <h2>Canlı Takip</h2>
        <span className={`connection-dot ${isConnected ? 'connected' : ''}`} title={isConnected ? 'Bağlı' : 'Bağlantı yok'} />
      </div>

      <div className="filter-row">
        {(['all', 'departure', 'arrival', 'active'] as FilterKey[]).map((f) => (
          <button key={f} className={`filter-chip ${filter === f ? 'active' : ''}`} onClick={() => setFilter(f)}>
            {{ all: 'Tümü', departure: 'Giden', arrival: 'Gelen', active: 'Aktif' }[f]}
          </button>
        ))}
      </div>

      {error && <div className="state-message error">{error}</div>}

      <div className="map-wrapper">
        <MapContainer center={[ERZURUM_LAT, ERZURUM_LON]} zoom={7} style={{ height: '100%', width: '100%' }}>
          <TileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> katkıda bulunanlar'
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />
          {filtered.map((a) => (
            <Marker key={a.icaoHex} position={[a.latitude, a.longitude]} icon={aircraftIcon(a.heading)}>
              <Popup>
                <strong>{a.flightNumber ?? a.callsign ?? 'Bilinmeyen uçuş'}</strong>
                <br />
                {a.registration && <>Kayıt: {a.registration}<br /></>}
                Durum: {a.status}
              </Popup>
            </Marker>
          ))}
        </MapContainer>
      </div>
    </div>
  );
}
