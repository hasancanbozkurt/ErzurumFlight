import { useEffect, useState } from 'react';
import { api } from '../api/client';
import type { DataSourceStatus } from '../api/types';

interface AdminScheduleRow {
  id: number;
  flightNumber: string;
  airline: string | null;
  origin: string | null;
  destination: string | null;
  departureLocalTime: string;
  arrivalLocalTime: string;
  validFrom: string;
  validTo: string | null;
  source: string | null;
  isVerified: boolean;
  isActive: boolean;
}

export function AdminSchedules() {
  const [schedules, setSchedules] = useState<AdminScheduleRow[]>([]);
  const [sources, setSources] = useState<DataSourceStatus[]>([]);
  const [tab, setTab] = useState<'schedules' | 'sources'>('schedules');
  const [error, setError] = useState<string | null>(null);

  async function load() {
    try {
      const [scheduleData, sourceData] = await Promise.all([
        api.getAdminSchedules() as Promise<AdminScheduleRow[]>,
        api.getAdminSources(),
      ]);
      setSchedules(scheduleData);
      setSources(sourceData);
    } catch {
      setError('Veriler yüklenemedi. Oturumunuz sona ermiş olabilir.');
    }
  }

  useEffect(() => {
    load();
  }, []);

  return (
    <div className="page admin-page">
      <h2>Yönetim Paneli</h2>

      <div className="filter-row">
        <button className={`filter-chip ${tab === 'schedules' ? 'active' : ''}`} onClick={() => setTab('schedules')}>Tarifeler</button>
        <button className={`filter-chip ${tab === 'sources' ? 'active' : ''}`} onClick={() => setTab('sources')}>Veri Kaynakları</button>
      </div>

      {error && <div className="state-message error">{error}</div>}

      {tab === 'schedules' && (
        <table className="admin-table">
          <thead>
            <tr>
              <th>Uçuş</th><th>Havayolu</th><th>Rota</th><th>Kalkış</th><th>Varış</th>
              <th>Geçerlilik</th><th>Kaynak</th><th>Doğrulama</th><th>Durum</th>
            </tr>
          </thead>
          <tbody>
            {schedules.map((s) => (
              <tr key={s.id}>
                <td>{s.flightNumber}</td>
                <td>{s.airline ?? '—'}</td>
                <td>{s.origin} → {s.destination}</td>
                <td>{s.departureLocalTime}</td>
                <td>{s.arrivalLocalTime}</td>
                <td>{s.validFrom} — {s.validTo ?? 'süresiz'}</td>
                <td>{s.source ?? '—'}</td>
                <td>{s.isVerified ? '✓ Doğrulandı' : '! Doğrulanmadı'}</td>
                <td>{s.isActive ? 'Aktif' : 'Devre Dışı'}</td>
              </tr>
            ))}
            {schedules.length === 0 && (
              <tr><td colSpan={9} className="state-message">Henüz tarife eklenmemiş.</td></tr>
            )}
          </tbody>
        </table>
      )}

      {tab === 'sources' && (
        <table className="admin-table">
          <thead>
            <tr><th>Kaynak</th><th>Tür</th><th>Durum</th><th>Öncelik</th><th>Son Başarı</th><th>Son Hata</th></tr>
          </thead>
          <tbody>
            {sources.map((s) => (
              <tr key={s.id}>
                <td>{s.name}</td>
                <td>{s.type}</td>
                <td>{s.isEnabled ? 'Etkin' : 'Devre Dışı'}</td>
                <td>{s.priority}</td>
                <td>{s.lastSuccessUtc ? new Date(s.lastSuccessUtc).toLocaleString('tr-TR') : '—'}</td>
                <td className="error-cell">{s.lastError ?? '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
