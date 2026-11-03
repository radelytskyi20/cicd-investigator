import { StrictMode, useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import './style.css';

function App() {
  const [connection, setConnection] = useState('Checking connection…');
  useEffect(() => {
    const controller = new AbortController();
    fetch('/api/status', { signal: controller.signal })
      .then(response => { if (!response.ok) throw new Error(); return response.json(); })
      .then(() => setConnection('API connected'))
      .catch(() => { if (!controller.signal.aborted) setConnection('API unavailable'); });
    return () => controller.abort();
  }, []);
  return <main><p className="eyebrow">LOCAL INVESTIGATION WORKSPACE</p><h1>CI/CD Investigator</h1>
    <p>Investigate failed runs with evidence from your pipeline.</p>
    <p role="status">{connection}</p><p>The application foundation is ready. Run investigation is coming next.</p></main>;
}

createRoot(document.getElementById('root')!).render(<StrictMode><App /></StrictMode>);
