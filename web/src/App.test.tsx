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
  const issue = { id: '1', title: '机柜异常', priority: 'high', status: 'open' };
  const fetchMock = vi.fn().mockResolvedValueOnce({ ok: true, json: async () => [issue] }).mockResolvedValueOnce({ ok: true, json: async () => ({ ...issue, status: 'resolved' }) });
  vi.stubGlobal('fetch', fetchMock);
  render(<App />);
  const select = await screen.findByRole('combobox');
  expect(screen.getByText('机柜异常')).toBeTruthy();
  fireEvent.change(select, { target: { value: 'resolved' } });
  await waitFor(() => expect((select as HTMLSelectElement).value).toBe('resolved'));
  expect(fetchMock.mock.calls[1][1].method).toBe('PATCH');
});
