import { useEffect, useState } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { Header } from './components/Header';
import { Dashboard } from './pages/Dashboard';
import { FlightDetail } from './pages/FlightDetail';
import { LiveMap } from './pages/LiveMap';
import { AdminLogin } from './pages/AdminLogin';
import { AdminSchedules } from './pages/AdminSchedules';
import { api } from './api/client';
import './styles.css';

function App() {
  const [isAdmin, setIsAdmin] = useState<boolean | null>(null);

  useEffect(() => {
    api.me().then(() => setIsAdmin(true)).catch(() => setIsAdmin(false));
  }, []);

  return (
    <div className="app-shell">
      <Header />
      <main className="app-main">
        <Routes>
          <Route path="/" element={<Dashboard />} />
          <Route path="/flights/:id" element={<FlightDetail />} />
          <Route path="/live" element={<LiveMap />} />
          <Route
            path="/admin"
            element={isAdmin ? <Navigate to="/admin/schedules" replace /> : <AdminLogin onLoggedIn={() => setIsAdmin(true)} />}
          />
          <Route
            path="/admin/schedules"
            element={isAdmin ? <AdminSchedules /> : <AdminLogin onLoggedIn={() => setIsAdmin(true)} />}
          />
        </Routes>
      </main>
    </div>
  );
}

export default App;
