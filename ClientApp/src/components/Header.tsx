import { Link, useLocation } from 'react-router-dom';

export function Header() {
  const location = useLocation();

  const navItem = (to: string, label: string) => (
    <Link to={to} className={`nav-item ${location.pathname === to ? 'active' : ''}`}>
      {label}
    </Link>
  );

  return (
    <header className="app-header">
      <Link to="/" className="brand">
        <span className="brand-title">ERZURUM FLIGHT</span>
        <span className="brand-subtitle">Erzurum Havalimanı · ERZ / LTCE</span>
      </Link>
      <nav className="main-nav">
        {navItem('/', 'Ana Sayfa')}
        {navItem('/live', 'Canlı Takip')}
        {navItem('/admin', 'Yönetim')}
      </nav>
    </header>
  );
}
