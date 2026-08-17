import { addDaysIso, todayIso } from '../lib/format';

interface DateTabsProps {
  selectedDate: string;
  onSelect: (date: string) => void;
}

const SHORTCUTS: { label: string; offset: number }[] = [
  { label: 'Bugün', offset: 0 },
  { label: 'Yarın', offset: 1 },
  { label: '3 Gün', offset: 2 },
  { label: '7 Gün', offset: 6 },
  { label: '14 Gün', offset: 13 },
  { label: '30 Gün', offset: 29 },
];

/** Şartname bölüm 8: Bugün / Yarın / 3 Gün / 7 Gün / 14 Gün / 30 Gün / Tarih seç. */
export function DateTabs({ selectedDate, onSelect }: DateTabsProps) {
  const today = todayIso();

  return (
    <div className="date-tabs">
      {SHORTCUTS.map((s) => {
        const iso = addDaysIso(today, s.offset);
        const isActive = iso === selectedDate;
        return (
          <button key={s.label} className={`date-tab ${isActive ? 'active' : ''}`} onClick={() => onSelect(iso)}>
            {s.label}
          </button>
        );
      })}
      <input
        type="date"
        className="date-tab date-picker"
        value={selectedDate}
        min={today}
        onChange={(e) => e.target.value && onSelect(e.target.value)}
        aria-label="Tarih seç"
      />
    </div>
  );
}
