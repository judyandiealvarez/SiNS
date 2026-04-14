import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import type { CurrentUser } from '@/types'

function readUser(): CurrentUser | null {
  try {
    const raw = localStorage.getItem('currentUser')
    if (!raw) return null
    return JSON.parse(raw) as CurrentUser
  } catch {
    return null
  }
}

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('token'))
  const currentUser = ref<CurrentUser | null>(readUser())
  const loginError = ref<string | null>(null)
  const loginPending = ref(false)

  const isAuthenticated = computed(() => !!token.value && !!currentUser.value)

  function setSession(newToken: string, user: CurrentUser) {
    token.value = newToken
    currentUser.value = user
    localStorage.setItem('token', newToken)
    localStorage.setItem('currentUser', JSON.stringify(user))
  }

  function clearAuth() {
    token.value = null
    currentUser.value = null
    localStorage.removeItem('token')
    localStorage.removeItem('currentUser')
  }

  async function login(username: string, password: string) {
    loginPending.value = true
    loginError.value = null

    try {
      const response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, password })
      })
      const raw = await response.text()
      let data: { token?: string; user?: { role?: string }; message?: string }
      try {
        data = JSON.parse(raw) as typeof data
      } catch {
        loginError.value =
          response.status === 502 || response.status === 504
            ? 'Cannot reach API (is dotnet running on the URL Vite is proxying to?).'
            : raw.slice(0, 200) || `HTTP ${response.status} (non-JSON response)`
        return false
      }

      if (response.ok && data.token) {
        const role = data.user?.role ?? 'Admin'
        setSession(data.token, { username, role })
        return true
      }

      loginError.value = data.message ?? 'Login failed'
      return false
    } catch {
      loginError.value = 'Network error (browser could not reach Vite or Vite crashed).'
      return false
    } finally {
      loginPending.value = false
    }
  }

  function logout() {
    clearAuth()
  }

  return {
    token,
    currentUser,
    loginError,
    loginPending,
    isAuthenticated,
    setSession,
    clearAuth,
    login,
    logout
  }
})
