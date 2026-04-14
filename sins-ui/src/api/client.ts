import { useAuthStore } from '@/stores/auth'
import router from '@/router'

export async function apiFetch(url: string, options: RequestInit = {}): Promise<Response> {
  const auth = useAuthStore()
  const token = auth.token

  const headers: HeadersInit = {
    ...options.headers,
    ...(token ? { Authorization: `Bearer ${token}` } : {})
  }

  const response = await fetch(url, {
    ...options,
    headers
  })

  if (response.status === 401) {
    auth.clearAuth()
    await router.push({ name: 'login' })
    throw new Error('Session expired. Please login again.')
  }

  return response
}
