import { useEffect, useState } from 'react';
import './style.css';

type Status = 'open' | 'in_progress' | 'resolved';
type Issue = { id: string; title: string; priority: 'low' | 'medium' | 'high'; status: Status };
const api = 'http://localhost:18765';
const statuses: Record<Status, string> = { open: '待处理', in_progress: '处理中', resolved: '已解决' };
const priorities = { low: '低', medium: '中', high: '高' };

export default function App() {
  const [issues, setIssues] = useState<Issue[]>([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState('');
  async function load() {
    setLoading(true); setError('');
    try { const response = await fetch(`${api}/issues`); if (!response.ok) throw new Error(); setIssues(await response.json()); }
    catch { setError('无法获取问题，请检查后端服务并重试。'); }
    finally { setLoading(false); }
  }
  useEffect(() => { void load(); }, []);
  async function update(id: string, status: Status) {
    setSaving(id); setError('');
    try {
      const response = await fetch(`${api}/issues/${encodeURIComponent(id)}/status`, { method: 'PATCH', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ status }) });
      if (!response.ok) throw new Error();
      const updated: Issue = await response.json();
      setIssues(previous => previous.map(issue => issue.id === id ? updated : issue));
    } catch { setError('状态更新失败，请检查后端服务并重试。'); }
    finally { setSaving(''); }
  }
  return <main><header><div><p className="eyebrow">现场巡检</p><h1>问题管理</h1><p>查看上报的问题并更新处理状态。</p></div><button onClick={() => void load()} disabled={loading}>刷新列表</button></header>
    {error && <p role="alert" className="error">{error}</p>}
    {loading ? <p>正在加载问题…</p> : issues.length === 0 ? <p className="empty">暂无巡检问题。</p> : <div className="table-wrap"><table><thead><tr><th>问题标题</th><th>优先级</th><th>处理状态</th></tr></thead><tbody>{issues.map(issue => <tr key={issue.id}><td>{issue.title}</td><td><span className={`priority ${issue.priority}`}>{priorities[issue.priority]}</span></td><td><select aria-label={`${issue.title}的处理状态`} value={issue.status} disabled={saving === issue.id} onChange={event => void update(issue.id, event.target.value as Status)}>{(Object.keys(statuses) as Status[]).map(status => <option key={status} value={status}>{statuses[status]}</option>)}</select></td></tr>)}</tbody></table></div>}
  </main>;
}
