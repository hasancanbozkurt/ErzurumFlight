import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../api/client';

export function AdminLogin({ onLoggedIn }: { onLoggedIn: () => void }) {
  const [userName, setUserName] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const navigate = useNavigate();

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      await api.login(userName, password);
      onLoggedIn();
      navigate('/admin/schedules');
    } catch {
      setError('Kullanıcı adı veya parola hatalı.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="page admin-login">
      <h2>Yönetici Girişi</h2>
      <form onSubmit={handleSubmit} className="login-form">
        <label>
          Kullanıcı adı
          <input value={userName} onChange={(e) => setUserName(e.target.value)} autoFocus required />
        </label>
        <label>
          Parola
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
        </label>
        {error && <div className="state-message error">{error}</div>}
        <button type="submit" disabled={submitting}>{submitting ? 'Giriş yapılıyor…' : 'Giriş Yap'}</button>
      </form>
    </div>
  );
}
