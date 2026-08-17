import { useEffect, useState } from 'react';
import { api } from '../api/client';
import type { FlightSummary, DailyFlightCounts } from '../api/types';
import { DateTabs } from '../components/DateTabs';
import { FlightCard } from '../components/FlightCard';
import { formatDateLong, todayIso } from '../lib/format';
import { useFlightHub } from '../hooks/useFlightHub';

type Tab = 'departure' | 'arrival';

export function Dashboard() {
  const [selectedDate, setSelectedDate] = useState(todayIso());
  const [activeTab, setActiveTab] = useState<Tab>('departure');
  const [departures, setDepartures] = useState<FlightSummary[]>([]);
  const [arrivals, setArrivals] = useState<FlightSummary[]>([]);
  const [counts, setCounts] = useState<DailyFlightCounts | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [cancelBanner, setCancelBanner] = useState<string | null>(null);

  async function loadData(date: string) {
    setLoading(true);
    setError(null);
    try {
      const [dep, arr, dailyCounts] = await Promise.all([
        api.getFlights(date, 'departure'),
        api.getFlights(date, 'arrival'),
        api.getDailyCounts(date),
      ]);
      setDepartures(dep);
      setArrivals(arr);
      setCounts(dailyCounts);
    } catch {
      setError('Veri kaynağı şu anda güncellenemiyor. Lütfen daha sonra tekrar deneyin.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadData(selectedDate);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedDate]);

  // Kullanıcı sayfayı yenilemeden canlı durum güncellemelerini (ADS-B durum geçişleri VE
  // AeroDataBox/AviationStack'ten gelen iptal/gecikme bilgisi) görebilsin diye, seçili tarih
  // etkilendiğinde listeyi otomatik yeniden çeker.
  useFlightHub({
    onStatusChanged: () => {
      if (selectedDate === todayIso()) loadData(selectedDate);
    },
    onCancelled: (evt) => {
      setCancelBanner(`${evt.flightNumber} numaralı uçuş iptal edildi.`);
      if (selectedDate === todayIso()) loadData(selectedDate);
      setTimeout(() => setCancelBanner(null), 8000);
    },
    onScheduleSynced: () => {
      loadData(selectedDate);
    },
  });

  const list = activeTab === 'departure' ? departures : arrivals;

  return (
    <div className="page dashboard">
      {cancelBanner && <div className="cancel-banner">⚠ {cancelBanner}</div>}

      <DateTabs selectedDate={selectedDate} onSelect={setSelectedDate} />

      <h2 className="date-heading">{formatDateLong(selectedDate)}</h2>

      <div className="counts-row">
        <button className={`count-pill ${activeTab === 'departure' ? 'active' : ''}`} onClick={() => setActiveTab('departure')}>
          <span className="count-value">{counts?.departures ?? '–'}</span>
          <span className="count-label">GİDEN</span>
        </button>
        <button className={`count-pill ${activeTab === 'arrival' ? 'active' : ''}`} onClick={() => setActiveTab('arrival')}>
          <span className="count-value">{counts?.arrivals ?? '–'}</span>
          <span className="count-label">GELEN</span>
        </button>
        <div className="count-pill live">
          <span className="count-value">{counts?.live ?? '–'}</span>
          <span className="count-label">CANLI</span>
        </div>
      </div>

      {loading && <div className="state-message">Yükleniyor…</div>}
      {error && <div className="state-message error">{error}</div>}

      {!loading && !error && (
        <div className="flight-list">
          {list.length === 0 && <div className="state-message">Bu tarih için uçuş bulunamadı.</div>}
          {list.map((f) => (
            <FlightCard key={f.id} flight={f} direction={activeTab} />
          ))}
        </div>
      )}
    </div>
  );
}
