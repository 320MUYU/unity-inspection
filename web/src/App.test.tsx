// @vitest-environment jsdom
import { afterEach, expect, test, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import App from './App';

afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

test('shows an error when the server is unavailable', async () => {
  vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('offline')));
  render(<App />);
  await waitFor(() => expect(screen.getByRole('alert').textContent).toContain('无法获取问题'));
});

test('lists issues and updates their status', async () => {
  const issue = { id: '1', title: '机柜异常', priority: 'high', status: 'open', createdAt: '2026-10-29T10:20:00Z' };
  const fetchMock = vi.fn().mockResolvedValueOnce({ ok: true, json: async () => [issue] }).mockResolvedValueOnce({ ok: true, json: async () => ({ ...issue, status: 'resolved' }) });
  vi.stubGlobal('fetch', fetchMock);
  render(<App />);
  const select = await screen.findByRole('combobox');
  expect(screen.getByText('机柜异常')).toBeTruthy();
  expect(screen.getByRole('columnheader', { name: '上报时间' })).toBeTruthy();
  expect(document.querySelector('time')?.getAttribute('datetime')).toBe(issue.createdAt);
  fireEvent.change(select, { target: { value: 'resolved' } });
  await waitFor(() => expect((select as HTMLSelectElement).value).toBe('resolved'));
  expect(fetchMock.mock.calls[1][1].method).toBe('PATCH');
});

test('keeps the previous status and shows a useful error when updating fails', async () => {
  const issue = { id: '1', title: '机柜异常', priority: 'high', status: 'open', createdAt: '2026-10-29T10:20:00Z' };
  vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce({ ok: true, json: async () => [issue] }).mockRejectedValueOnce(new Error('offline')));
  render(<App />);
  const select = await screen.findByRole('combobox');
  fireEvent.change(select, { target: { value: 'resolved' } });
  await waitFor(() => expect(screen.getByRole('alert').textContent).toContain('状态更新失败'));
  expect((select as HTMLSelectElement).value).toBe('open');
});

test('shows an empty state only after a successful empty response', async () => {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true, json: async () => [] }));
  render(<App />);
  expect(await screen.findByText('暂无巡检问题。')).toBeTruthy();
});
