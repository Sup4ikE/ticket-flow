import type { EventDetails, EventSummary } from '@/types/event'

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080').replace(/\/+$/, '')

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response
  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      ...init,
      headers: { Accept: 'application/json', ...(init?.body ? { 'Content-Type': 'application/json' } : {}), ...init?.headers },
    })
  } catch {
    // fetch only rejects on network-level failures (gateway down, CORS, offline).
    throw new ApiError(0, 'Не вдалося зʼєднатися з сервером. Перевірте підключення та спробуйте ще раз.')
  }

  if (!response.ok) {
    // Backend errors come back as { error: "..." }; fall back to the status text otherwise.
    const body = await response.json().catch(() => null) as { error?: string } | null
    throw new ApiError(response.status, body?.error ?? `Помилка сервера (${response.status})`)
  }

  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}

export const getEvents = () => request<EventSummary[]>('/api/events')

export const getEventById = (id: string) => request<EventDetails>(`/api/events/${encodeURIComponent(id)}`)
