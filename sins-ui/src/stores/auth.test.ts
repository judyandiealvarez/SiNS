import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useAuthStore } from './auth'

function jsonResponse(payload: unknown, status = 200): Response {
  return new Response(JSON.stringify(payload), {
    status,
    headers: { 'Content-Type': 'application/json' }
  })
}

describe('auth store provider switching', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
    const storage = new Map<string, string>()
    vi.stubGlobal('localStorage', {
      getItem: (key: string) => storage.get(key) ?? null,
      setItem: (key: string, value: string) => {
        storage.set(key, value)
      },
      removeItem: (key: string) => {
        storage.delete(key)
      },
      clear: () => {
        storage.clear()
      }
    })
    localStorage.clear()
    setActivePinia(createPinia())
  })

  it('uses embedded token endpoint when provider is Embedded', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = input.toString()
      if (url === '/api/auth/provider') {
        return jsonResponse({ provider: 'Embedded' })
      }
      if (url === '/connect/token') {
        return jsonResponse({ access_token: 'embedded-token', refresh_token: 'embedded-refresh' })
      }
      if (url === '/api/auth/me') {
        return jsonResponse({ username: 'admin', role: 'Admin' })
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    const store = useAuthStore()
    const result = await store.login('admin', 'admin123')

    expect(result).toBe(true)
    expect(store.token).toBe('embedded-token')
    expect(fetchMock).toHaveBeenCalledWith('/connect/token', expect.any(Object))
    expect(fetchMock).not.toHaveBeenCalledWith('/api/auth/keycloak/login', expect.any(Object))
  })

  it('uses keycloak endpoints when provider is Keycloak', async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = input.toString()
      if (url === '/api/auth/provider') {
        return jsonResponse({ provider: 'Keycloak' })
      }
      if (url === '/api/auth/keycloak/login') {
        return jsonResponse({ access_token: 'keycloak-token', refresh_token: 'keycloak-refresh' })
      }
      if (url === '/api/auth/me') {
        return jsonResponse({ username: 'admin', role: 'Admin' })
      }
      if (url === '/api/auth/keycloak/logout') {
        return new Response('', { status: 200 })
      }

      throw new Error(`Unexpected request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    const store = useAuthStore()
    const loginOk = await store.login('admin', 'admin123')
    expect(loginOk).toBe(true)
    expect(store.refreshToken).toBe('keycloak-refresh')

    await store.logout()

    expect(fetchMock).toHaveBeenCalledWith('/api/auth/keycloak/login', expect.any(Object))
    expect(fetchMock).toHaveBeenCalledWith('/api/auth/keycloak/logout', expect.any(Object))
    expect(store.token).toBeNull()
    expect(store.currentUser).toBeNull()
  })
})
