import { StrictMode, useEffect, useRef, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import './style.css';

type Run = { id: string; ref: string; commitId: string; startedAt: string; state: string; outcome: string; datasetVersion: string; environment?: string; configuration?: string };
type Job = { id: string; name: string; attempt: number; outcome: string };
type Evidence = { tool: string; runId: string; jobId?: string; attempt?: number; datasetVersion: string; quote: string; completeness: string; sourceLocator?: string };
type Report = { runId: string; baselineId?: string; outcome: string; summary: string; confidence: string; confidenceReason: string; evidence: Evidence[]; hypotheses: { explanation: string; supportingEvidence: number[]; opposingEvidence: number[] }[]; limitations: string; nextStep: string };
type ReadyReport = { report: Report; durationMs: number; toolCallCount: number; resultId: string };
type Saved = { id: string; savedAt: string; runId: string; summary: string };
type AgentEvent = { type: string; text?: string; toolName?: string; report?: ReadyReport };
async function request<T>(url: string, options?: RequestInit): Promise<T> {
  const response = await fetch(url, options);
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail || body?.error || `Request failed (${response.status}).`);
  }
  return response.json();
}
const message = (error: unknown) => error instanceof Error ? error.message : 'Something went wrong. Please retry.';
const encode = encodeURIComponent;

function App() {
  const [runs, setRuns] = useState<Run[]>([]);
  const [selected, setSelected] = useState<Run | null>(null);
  const [jobs, setJobs] = useState<Job[]>([]);
  const [baseline, setBaseline] = useState<{ candidate?: Run; limitations: string[] } | null>(null);
  const [history, setHistory] = useState<Saved[]>([]);
  const [loading, setLoading] = useState(true);
  const [detailsLoading, setDetailsLoading] = useState(false);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [stopping, setStopping] = useState(false);
  const [connected, setConnected] = useState(false);
  const [active, setActive] = useState(false);
  const [goal, setGoal] = useState('Explain the failure and compare it with the preceding successful run.');
  const [followUp, setFollowUp] = useState('');
  const [activity, setActivity] = useState<string[]>([]);
  const [status, setStatus] = useState('Choose a run to begin');
  const [result, setResult] = useState<ReadyReport | null>(null);
  const [savedId, setSavedId] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [source, setSource] = useState<{ title: string; text: string } | null>(null);
  const connection = useRef<HubConnection | null>(null);
  const currentResult = useRef<string | null>(null);
  const selection = useRef(0);
  async function refreshHistory() { setHistory(await request<Saved[]>('/api/reports')); }
  useEffect(() => {
    let disposed = false;
    Promise.all([request<{ data: Run[] }>('/api/runs'), request<Saved[]>('/api/reports')])
      .then(([catalog, reports]) => { if (!disposed) { setRuns(catalog.data); setHistory(reports); } })
      .catch(error => { if (!disposed) setError(message(error)); })
      .finally(() => { if (!disposed) setLoading(false); });
    return () => { disposed = true; void connection.current?.stop(); };
  }, []);

  async function selectRun(run: Run) {
    const version = ++selection.current;
    setError(''); setSelected(run); setJobs([]); setBaseline(null); setResult(null); setSavedId(null); setSource(null);
    setActivity([]); setActive(false); currentResult.current = null; setDetailsLoading(true);
    try {
      await connection.current?.stop(); connection.current = null;
      const [details, comparison] = await Promise.all([
        request<{ data: { jobs: Job[] } }>(`/api/runs/${encode(run.id)}`),
        request<{ data: { candidate?: Run; limitations: string[] } }>(`/api/runs/${encode(run.id)}/baseline`)
      ]);
      if (version === selection.current) { setJobs(details.data.jobs); setBaseline(comparison.data); setStatus('Ready to investigate'); }
    } catch (error) { if (version === selection.current) setError(message(error)); }
    finally { if (version === selection.current) setDetailsLoading(false); }
  }
  async function connect() {
    if (connection.current?.state === HubConnectionState.Connected) return connection.current;
    const hub = new HubConnectionBuilder().withUrl('/investigation').build();
    hub.on('investigationEvent', (event: AgentEvent) => {
      if (event.type === 'toolStarted') { setActivity(items => [...items, `Reading ${event.toolName?.replace('cicd-', '') || 'source'}`]); setStatus('Reading pipeline evidence'); }
      if (event.type === 'toolCompleted') setActivity(items => [...items, 'Source call completed']);
      if (event.type === 'started') setStatus('Agent is investigating');
      if (event.type === 'answerReady' && event.report) { setResult(event.report); currentResult.current = event.report.resultId; setSavedId(null); setSource(null); }
      if (event.type === 'error') { setError(event.text || 'Investigation failed.'); setStatus('Needs attention'); }
      if (event.type === 'ended') setStatus(event.text === 'completed' ? 'Report ready' : event.text === 'cancelled' ? 'Stopped' : 'Needs attention');
    });
    hub.onclose(() => { setConnected(false); setActive(false); currentResult.current = null; setBusy(false); setStatus('Disconnected · start a fresh investigation'); });
    connection.current = hub;
    await hub.start(); setConnected(true); return hub;
  }
  async function investigate(isFollowUp = false) {
    if (!selected || busy || saving) return;
    setBusy(true); setError(''); setSource(null); setStatus('Connecting to agent');
    if (!isFollowUp) { setResult(null); setSavedId(null); setActivity([]); currentResult.current = null; }
    try {
      const hub = await connect(); setActive(true);
      if (isFollowUp) { const question = followUp; setFollowUp(''); setActivity(items => [...items, `Follow-up: ${question}`]); await hub.invoke('FollowUp', question); }
      else await hub.invoke('Start', { runId: selected.id, goal });
    } catch (error) { setError(message(error)); setStatus('Needs attention'); }
    finally { setBusy(false); setStopping(false); }
  }
  async function stop() {
    setStopping(true); setError('');
    try { await connection.current?.invoke('Stop'); } catch (error) { setError(message(error)); } finally { setStopping(false); }
  }
  async function save() {
    if (!result) return;
    setSaving(true); setError('');
    try {
      const saved = await request<Saved>('/api/reports', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ resultId: result.resultId }) });
      setSavedId(saved.id); await refreshHistory();
    } catch (error) { setError(message(error)); } finally { setSaving(false); }
  }
  async function openSaved(saved: Saved) {
    setError(''); setSource(null);
    try { const report = await request<{ result: ReadyReport }>(`/api/reports/${saved.id}`); setResult(report.result); setSavedId(saved.id); }
    catch (error) { setError(message(error)); }
  }
  async function openSource(url: string, title: string) {
    setSource({ title, text: 'Loading source…' });
    try { setSource({ title, text: JSON.stringify(await request(url), null, 2) }); }
    catch (error) { setSource({ title, text: message(error) }); }
  }
  function evidenceUrl(evidence: Evidence) {
    const base = `/api/runs/${encode(evidence.runId)}`;
    const parameters = new URLSearchParams({ datasetVersion: evidence.datasetVersion });
    if (evidence.tool === 'get_changes') return `${base}/changes?${parameters}`;
    if (evidence.jobId && evidence.attempt && ['read_job_log', 'get_test_results'].includes(evidence.tool)) {
      parameters.set('attempt', String(evidence.attempt));
      const line = evidence.sourceLocator?.match(/:line:(\d+)$/)?.[1];
      if (line) { parameters.set('start', line); parameters.set('count', '1'); }
      return `${base}/jobs/${encode(evidence.jobId)}/${evidence.tool === 'read_job_log' ? 'logs' : 'tests'}?${parameters}`;
    }
    return `${base}?${parameters}`;
  }
  const locked = busy || saving || detailsLoading;
  const report = result?.report;
  return <main className="app">
    <header className="masthead"><div><p className="eyebrow">LOCAL DEMO · READ-ONLY INVESTIGATION</p><h1>CI/CD Investigator<span className="accent">.</span></h1></div><span className="connection">{connected ? '● Agent connected' : '○ Local workspace'}</span></header>
    {error && <div role="alert" className="error">{error}<button className="quiet" onClick={() => setError('')}>Dismiss</button></div>}
    <div className="layout"><aside>
      <section className="panel"><div className="section-head"><h2>Pipeline runs</h2><span className="count">{runs.length}</span></div><p className="muted">Pricing CI · mock dataset</p>
        {loading ? <p>Loading runs…</p> : !runs.length ? <p>No runs available. {error && 'Check the API and reload.'}</p> : runs.map(run => <button key={run.id} disabled={locked} onClick={() => void selectRun(run)} className={`run-card ${selected?.id === run.id ? 'selected' : ''}`}><span className="row"><strong>{run.id}</strong><span className={`badge ${run.outcome.toLowerCase()}`}>{run.outcome}</span></span><span>{run.ref.replace('refs/heads/', '')} · {run.commitId}</span><small>{new Date(run.startedAt).toLocaleString()}</small></button>)}
      </section>
      <section className="panel"><div className="section-head"><h2>Saved reports</h2><button className="quiet" disabled={locked} onClick={() => void refreshHistory().catch(error => setError(message(error)))}>Refresh</button></div>
        {!history.length && <p className="muted">Completed reports appear here after you save them.</p>}
        {history.map(saved => <button className={`history-item ${saved.id === savedId ? 'selected' : ''}`} key={saved.id} disabled={locked} onClick={() => void openSaved(saved)}><strong>{saved.runId}</strong><span>{saved.summary}</span><small>{new Date(saved.savedAt).toLocaleString()}</small></button>)}<p className="footnote">Reports persist. Conversations stay only in the open session.</p>
      </section>
    </aside><div className="workspace">
      {selected ? <section className="panel"><div className="section-head"><div><p className="eyebrow">SELECTED RUN</p><h2>{selected.id} <span className={`badge ${selected.outcome.toLowerCase()}`}>{selected.outcome}</span></h2></div><span className="muted">{selected.datasetVersion}</span></div>
        <p className="run-meta">{selected.ref} · {selected.commitId}<br />{selected.environment} · {selected.configuration}</p>
        {detailsLoading ? <p>Loading details…</p> : <><div className="jobs">{jobs.map(job => <div className="job" key={job.id}><span><strong>{job.name}</strong> · attempt {job.attempt} · {job.outcome}</span><span><button className="link" onClick={() => void openSource(`/api/runs/${encode(selected.id)}/jobs/${encode(job.id)}/logs?attempt=${job.attempt}`, `${job.name} logs`)}>Logs</button><button className="link" onClick={() => void openSource(`/api/runs/${encode(selected.id)}/jobs/${encode(job.id)}/tests?attempt=${job.attempt}`, `${job.name} tests`)}>Tests</button></span></div>)}</div><p className="muted">Baseline: <strong>{baseline?.candidate?.id || 'No preceding successful run'}</strong>{baseline?.limitations?.length ? ` · ${baseline.limitations.join(' ')}` : ''} <button className="link" onClick={() => void openSource(`/api/runs/${encode(selected.id)}/changes`, 'Commit and changes')}>View changes</button></p></>}
        {selected.state === 'Completed' && selected.outcome === 'Failed' ? <form onSubmit={event => { event.preventDefault(); void investigate(); }}><label htmlFor="goal">Investigation goal</label><textarea id="goal" rows={2} value={goal} disabled={locked} onChange={event => setGoal(event.target.value)} /><div className="actions"><button className="primary" disabled={locked || !goal.trim()}>{active ? 'Start fresh investigation' : 'Investigate failure'}</button><span className="muted">Reads evidence. Makes no changes.</span></div></form> : <p className="notice">Successful runs are readable baselines. Select a failed run to investigate.</p>}
      </section> : !report ? <section className="panel welcome"><p className="eyebrow">EVIDENCE FIRST</p><h2>Understand a failed run.</h2><p>Select <strong>run-102</strong> to investigate a pricing test failure. The agent inspects logs, tests and changes, and compares with the successful baseline.</p><p className="muted">Ask a follow-up in the same session, then save the completed report.</p></section> : null}
      {(active || activity.length > 0) && <section className="panel activity"><div className="section-head"><h2><span className={busy ? 'working-dot' : 'idle-dot'} />{status}</h2>{busy && <button className="stop" disabled={stopping} onClick={() => void stop()}>{stopping ? 'Stopping…' : 'Stop'}</button>}</div><details open={busy}><summary>Agent activity · {activity.length} events</summary><ol>{activity.map((item, index) => <li key={index}>{item}</li>)}</ol></details><p className="footnote">Actual tool activity. Closing this tab ends the active session.</p></section>}
      {report && result && <section className="panel report"><div className="section-head"><div><p className="eyebrow">{savedId ? 'SAVED REPORT' : 'INVESTIGATION REPORT'} · {report.runId}</p><h2>{report.outcome === 'unknown' ? 'Cause remains unknown' : 'Findings'} <span className="badge neutral">{report.confidence} confidence</span></h2></div>{savedId ? <span className="saved">✓ Saved</span> : <button className="primary" disabled={busy || saving || !connected || currentResult.current !== result.resultId} onClick={() => void save()}>{saving ? 'Saving…' : 'Save report'}</button>}</div>
        <p className="conclusion">{report.summary}</p><p className="muted">{report.confidenceReason}</p><h3>Evidence</h3><div className="evidence-list">{report.evidence.map((evidence, index) => <div className="evidence" key={index}><div className="row"><button className="link" onClick={() => void openSource(evidenceUrl(evidence), `Evidence ${index + 1} · ${evidence.sourceLocator || evidence.runId}`)}>[{index + 1}] {evidence.tool.replaceAll('_', ' ')}</button><small>{evidence.completeness}</small></div><blockquote>{evidence.quote}</blockquote><small>{evidence.runId}{evidence.attempt ? ` · attempt ${evidence.attempt}` : ''} · {evidence.datasetVersion}</small></div>)}</div>
        <h3>Explanations considered</h3>{report.hypotheses.map((hypothesis, index) => <p key={index}>{hypothesis.explanation} <small className="muted">{hypothesis.supportingEvidence.length ? `Supports: ${hypothesis.supportingEvidence.map(i => `[${i + 1}]`).join(', ')}. ` : ''}{hypothesis.opposingEvidence.length ? `Against: ${hypothesis.opposingEvidence.map(i => `[${i + 1}]`).join(', ')}.` : ''}</small></p>)}<h3>Limits</h3><p>{report.limitations}</p><div className="next-step"><h3>Next check</h3><p>{report.nextStep}</p></div><p className="footnote">{(result.durationMs / 1000).toFixed(1)}s · {result.toolCallCount} tool calls in this turn · Baseline: {report.baselineId || 'none'}</p>
      </section>}
      {source && <section className="panel source"><div className="section-head"><h2>{source.title}</h2><button className="quiet" onClick={() => setSource(null)}>Close source</button></div><pre>{source.text}</pre></section>}
      {active && connected && <section className="panel"><form onSubmit={event => { event.preventDefault(); void investigate(true); }}><label htmlFor="followup">Ask a follow-up</label><p className="muted">Uses the current investigation context. A fresh investigation clears it.</p><textarea id="followup" value={followUp} disabled={busy || saving} onChange={event => setFollowUp(event.target.value)} placeholder="Could the cache warning explain the failure?" rows={2} /><div className="actions"><button className="primary" disabled={busy || saving || !followUp.trim()}>Send follow-up</button></div></form></section>}
    </div></div><footer>Local prototype · Read-only MCP tools · Explicitly saved reports only</footer>
  </main>;
}
createRoot(document.getElementById('root')!).render(<StrictMode><App /></StrictMode>);
