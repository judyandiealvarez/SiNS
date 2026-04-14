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
  const refreshToken = ref<string | null>(localStorage.getItem('refreshToken'))
  const currentUser = ref<CurrentUser | null>(readUser())
  const loginError = ref<string | null>(null)
  const loginPending = ref(false)

  const isAuthenticated = computed(() => !!token.value && !!currentUser.value)

  function setSession(newToken: string, user: CurrentUser, newRefreshToken?: string | null) {
    token.value = newToken
    currentUser.value = user
    localStorage.setItem('token', newToken)
    localStorage.setItem('currentUser', JSON.stringify(user))
    if (newRefreshToken) {
      refreshToken.value = newRefreshToken
      localStorage.setItem('refreshToken', newRefreshToken)
    }
  }

  function clearAuth() {
    token.value = null
    refreshToken.value = null
    currentUser.value = null
    localStorage.removeItem('token')
    localStorage.removeItem('refreshToken')
    localStorage.removeItem('currentUser')
  }

  async function loadCurrentUser(accessToken: string): Promise<CurrentUser | null> {
    const response = await fetch('/api/auth/me', {
      headers: {
        Authorization: `Bearer ${accessToken}`
      }
    })
    if (!response.ok) return null
    const profile = (await response.json()) as { username?: string; role?: string }
    if (!profile.username) return null
    return {
      username: profile.username,
      role: profile.role ?? 'User'
    }
  }

  async function login(username: string, password: string) {
    loginPending.value = true
    loginError.value = null

    try {
      const form = new URLSearchParams()
      form.set('grant_type', 'password')
      form.set('client_id', 'sins-spa')
      form.set('scope', 'openid profile email offline_access api')
      form.set('username', username)
      form.set('password', password)

      const response = await fetch('/connect/token', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: form.toString()
      })
      const raw = await response.text()
      let data: {
        access_token?: string
        refresh_token?: string
        error?: string
        error_description?: string
      }
      try {
        data = JSON.parse(raw) as typeof data
      } catch {
        loginError.value =
          response.status === 502 || response.status === 504
            ? 'Cannot reach API (is dotnet running on the URL Vite is proxying to?).'
            : raw.slice(0, 200) || `HTTP ${response.status} (non-JSON response)`
        return false
      }

      if (response.ok && data.access_token) {
        const user = await loadCurrentUser(data.access_token)
        if (!user) {
          loginError.value = 'Login succeeded but profile lookup failed.'
          return false
        }
        setSession(data.access_token, user, data.refresh_token ?? null)
        return true
      }

      loginError.value = data.error_description ?? data.error ?? 'Login failed'
      return false
    } catch {
      loginError.value = 'Network error (browser could not reach Vite or Vite crashed).'
      return false
    } finally {
      loginPending.value = false
    }
  }

  async function logout() {
    if (refreshToken.value) {
      const form = new URLSearchParams()
      form.set('token', refreshToken.value)
      form.set('client_id', 'sins-spa')
      await fetch('/connect/revocation', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: form.toString()
      })
    }
    clearAuth()
  }

  return {
    token,
    refreshToken,
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
