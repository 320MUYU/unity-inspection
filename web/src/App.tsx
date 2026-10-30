import { useEffect, useState } from 'react';
import './style.css';

type Status = 'open' | 'in_progress' | 'resolved';
type Issue = { id: string; title: string; deviceId?: string; deviceName?: string; priority: 'low' | 'medium' | 'high'; status: Status; createdAt: string };
const api = 'http://localhost:18765';
const statuses: Record<Status, string> = { open: '待处理', in_progress: '处理中', resolved: '已解决' };
const priorities = { low: '低', medium: '中', high: '高' };
const timeFormat = new Intl.DateTimeFormat('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', hour12: false });

function displayTime(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? '时间未知' : timeFormat.format(date);
}

export default function App() {
  const [issues, setIssues] = useState<Issue[]>([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const [loaded, setLoaded] = useState(false);
  const [saving, setSaving] = useState('');
  const [deleting, setDeleting] = useState('');

  async function load() {
    setLoading(true); setError('');
    try {
      const response = await fetch(`${api}/issues`);
      if (!response.ok) throw new Error();
      setIssues(await response.json()); setLoaded(true);
    } catch { setError('无法获取问题，请检查后端服务并重试。'); }
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

  async function remove(issue: Issue) {
    if (!window.confirm(`确定永久删除已解决的问题“${issue.title}”吗？此操作无法撤销。`)) return;
    setDeleting(issue.id); setError('');
    try {
      const response = await fetch(`${api}/issues/${encodeURIComponent(issue.id)}`, { method: 'DELETE' });
      if (!response.ok) {
        if (response.status === 409) throw new Error('status_conflict');
        throw new Error('request_failed');
      }
      setIssues(previous => previous.filter(item => item.id !== issue.id));
    } catch (cause) {
      const detail = cause instanceof Error && cause.message === 'status_conflict' ? '状态可能已变化，请刷新列表后重试。' : '请检查后端服务后重试。';
      setError(`删除失败：${detail}`);
    } finally { setDeleting(''); }
  }

  const pendingCount = issues.filter(issue => issue.status !== 'resolved').length;

  return <main className="workspace">
    <header className="page-header">
      <div>
        <p className="location">园区机房 / 巡检管理</p>
        <h1>巡检问题台账</h1>
        <p className="intro">查看现场上报的问题，跟进处理进度。</p>
      </div>
      <button className="refresh" onClick={() => void load()} disabled={loading || !!saving || !!deleting}>
        <span aria-hidden="true">↻</span> {loading ? '加载中' : '刷新列表'}
      </button>
    </header>

    <section className="issue-section" aria-labelledby="issues-heading">
      <div className="section-heading">
        <div><h2 id="issues-heading">问题列表</h2><p>来自巡检现场的上报记录</p></div>
        {loaded && <div className="counts"><span>共 {issues.length} 项</span><span className="pending">{pendingCount} 项待完成</span></div>}
      </div>

      {error && <p role="alert" className="error">{error}</p>}
      {loading ? <p className="state" role="status">正在加载问题…</p> : !loaded ? null : issues.length === 0 ?
        <div className="state empty"><span className="empty-mark" aria-hidden="true">✓</span><p>暂无巡检问题。</p><p>现场提交问题后，刷新列表即可查看。</p></div> :
        <div className="table-wrap"><table>
          <thead><tr><th scope="col">问题标题</th><th scope="col">设备</th><th scope="col">优先级</th><th scope="col">上报时间</th><th scope="col">处理状态</th><th scope="col">操作</th></tr></thead>
          <tbody>{issues.map(issue => <tr key={issue.id}>
            <td className="issue-title">{issue.title}</td>
            <td className="issue-device">{issue.deviceName || '未记录设备'}</td>
            <td><span className={`priority ${issue.priority}`}><span className="priority-dot" aria-hidden="true" />{priorities[issue.priority]}</span></td>
            <td className="issue-time"><time dateTime={issue.createdAt}>{displayTime(issue.createdAt)}</time></td>
            <td><select aria-label={`${issue.title}的处理状态`} value={issue.status} disabled={!!saving || !!deleting} onChange={event => void update(issue.id, event.target.value as Status)}>{(Object.keys(statuses) as Status[]).map(status => <option key={status} value={status}>{statuses[status]}</option>)}</select>{saving === issue.id && <span className="saving" role="status">保存中</span>}</td>
            <td>{issue.status === 'resolved' ? <button className="delete" aria-label={`删除${issue.title}`} disabled={!!saving || !!deleting} onClick={() => void remove(issue)}>{deleting === issue.id ? '删除中' : '删除'}</button> : <span className="delete-unavailable">解决后可删除</span>}</td>
          </tr>)}</tbody>
        </table></div>}
    </section>
  </main>;
}
