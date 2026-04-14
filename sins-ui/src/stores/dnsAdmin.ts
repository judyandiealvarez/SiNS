import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { apiFetch } from '@/api/client'
import { useAuthStore } from '@/stores/auth'
import type {
  CacheDetailRow,
  DashboardStats,
  DnsRecordRow,
  DomainMappingRow,
  ServerSettings,
  UserRow
} from '@/types'

function normalizeSettings(data: Record<string, unknown>): ServerSettings {
  return {
    cacheTimeoutMinutes: Number(data.cacheTimeoutMinutes ?? data.CacheTimeoutMinutes ?? 60),
    udpPort: Number(data.udpPort ?? data.UdpPort ?? 53),
    tcpPort: Number(data.tcpPort ?? data.TcpPort ?? 53),
    upstreamServers: (data.upstreamServers ?? data.UpstreamServers ?? []) as string[],
    haproxy: (data.haproxy ?? data.Haproxy ?? null) as string | null
  }
}

const defaultSettings = (): ServerSettings => ({
  cacheTimeoutMinutes: 60,
  udpPort: 53,
  tcpPort: 53,
  upstreamServers: ['8.8.8.8', '1.1.1.1', '2001:4860:4860::8888', '2606:4700:4700::1111'],
  haproxy: null
})

export const useDnsAdminStore = defineStore('dnsAdmin', () => {
  const auth = useAuthStore()

  const version = ref('1.0.0.0')
  const loading = ref(false)
  const error = ref<string | null>(null)

  const stats = ref<DashboardStats>({
    totalRecords: 0,
    totalCache: 0,
    totalUsers: 0,
    expiredCache: 0
  })
  const records = ref<DnsRecordRow[]>([])
  const cacheRecords = ref<CacheDetailRow[]>([])
  const users = ref<UserRow[]>([])
  const settings = ref<ServerSettings>(defaultSettings())
  const domainUpstreamMappings = ref<DomainMappingRow[]>([])

  const showAddRecordModal = ref(false)
  const showEditRecordModal = ref(false)
  const showAddUserModal = ref(false)
  const showAddDomainMappingModal = ref(false)
  const showEditDomainMappingModal = ref(false)

  const newRecord = ref({ name: '', type: 'A', value: '', ttl: 3600 })
  const editingRecord = ref({ id: null as number | null, name: '', type: 'A', value: '', ttl: 3600 })
  const newUser = ref({ username: '', email: '', password: '', role: 'User' })
  const newDomainMapping = ref({ domain: '', upstreamServer: '' })
  const editingDomainMapping = ref({ id: null as number | null, domain: '', upstreamServer: '' })

  const upstreamServersText = computed({
    get: () => settings.value.upstreamServers.join('\n'),
    set: (text: string) => {
      settings.value = {
        ...settings.value,
        upstreamServers: text.split('\n').map((s) => s.trim()).filter(Boolean)
      }
    }
  })

  function setError(msg: string | null) {
    error.value = msg
  }

  async function loadVersion() {
    try {
      const response = await fetch('/api/dns/version')
      if (response.ok) {
        const data = (await response.json()) as { version?: string }
        if (data.version) version.value = data.version
      }
    } catch (e) {
      console.error('Failed to load version:', e)
    }
  }

  async function loadDashboard() {
    if (!auth.token) return
    try {
      const [statsRes, usersRes] = await Promise.all([apiFetch('/api/dns/stats'), apiFetch('/api/auth/users')])
      if (statsRes.ok) {
        const data = (await statsRes.json()) as Record<string, number>
        let totalUsers = 0
        if (usersRes.ok) {
          const list = (await usersRes.json()) as unknown[]
          totalUsers = list.length
        }
        stats.value = {
          totalRecords: data.totalRecords ?? 0,
          totalCache: data.totalCacheRecords ?? 0,
          totalUsers,
          expiredCache: data.expiredCacheRecords ?? 0
        }
      }
    } catch (e) {
      console.error('Failed to load dashboard:', e)
    }
  }

  async function loadRecords() {
    if (!auth.token) return
    try {
      const response = await apiFetch('/api/dns/records')
      if (response.ok) records.value = (await response.json()) as DnsRecordRow[]
    } catch (e) {
      console.error('Failed to load records:', e)
    }
  }

  async function loadCache() {
    if (!auth.token) return
    try {
      const response = await apiFetch('/api/dns/cache/details')
      if (response.ok) cacheRecords.value = (await response.json()) as CacheDetailRow[]
    } catch (e) {
      console.error('Failed to load cache:', e)
    }
  }

  async function loadUsers() {
    if (!auth.token) return
    try {
      const response = await apiFetch('/api/auth/users')
      if (response.ok) users.value = (await response.json()) as UserRow[]
    } catch (e) {
      console.error('Failed to load users:', e)
    }
  }

  async function loadSettings() {
    if (!auth.token) return
    try {
      const [configRes, mappingsRes] = await Promise.all([
        apiFetch('/api/dns/config'),
        apiFetch('/api/dns/domain-upstreams')
      ])
      if (configRes.ok) {
        const raw = (await configRes.json()) as Record<string, unknown>
        settings.value = normalizeSettings(raw)
      }
      if (mappingsRes.ok) {
        domainUpstreamMappings.value = ((await mappingsRes.json()) as DomainMappingRow[]) ?? []
      }
    } catch (e) {
      console.error('Failed to load settings:', e)
    }
  }

  function resetNewRecord() {
    newRecord.value = { name: '', type: 'A', value: '', ttl: 3600 }
  }

  function resetEditingRecord() {
    editingRecord.value = { id: null, name: '', type: 'A', value: '', ttl: 3600 }
  }

  function resetNewUser() {
    newUser.value = { username: '', email: '', password: '', role: 'User' }
  }

  function resetNewDomainMapping() {
    newDomainMapping.value = { domain: '', upstreamServer: '' }
  }

  function resetEditingDomainMapping() {
    editingDomainMapping.value = { id: null, domain: '', upstreamServer: '' }
  }

  async function addDomainMapping() {
    if (!auth.token) return
    if (!newDomainMapping.value.domain.trim() || !newDomainMapping.value.upstreamServer.trim()) {
      setError('Domain and upstream server are required')
      return
    }
    loading.value = true
    setError(null)
    try {
      const response = await apiFetch('/api/dns/domain-upstreams', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          domain: newDomainMapping.value.domain.trim(),
          upstreamServer: newDomainMapping.value.upstreamServer.trim()
        })
      })
      if (response.ok) {
        showAddDomainMappingModal.value = false
        resetNewDomainMapping()
        await loadSettings()
      } else {
        const data = (await response.json()) as { message?: string }
        setError(data.message ?? 'Failed to add mapping')
      }
    } catch {
      setError('Network error. Please try again.')
    } finally {
      loading.value = false
    }
  }

  async function updateDomainMapping() {
    if (!auth.token) return
    if (!editingDomainMapping.value.domain.trim() || !editingDomainMapping.value.upstreamServer.trim()) {
      setError('Domain and upstream server are required')
      return
    }
    if (editingDomainMapping.value.id == null) return
    loading.value = true
    setError(null)
    try {
      const response = await apiFetch(`/api/dns/domain-upstreams/${editingDomainMapping.value.id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          domain: editingDomainMapping.value.domain.trim(),
          upstreamServer: editingDomainMapping.value.upstreamServer.trim()
        })
      })
      if (response.ok) {
        showEditDomainMappingModal.value = false
        resetEditingDomainMapping()
        await loadSettings()
      } else {
        const data = (await response.json()) as { message?: string }
        setError(data.message ?? 'Failed to update mapping')
      }
    } catch {
      setError('Network error. Please try again.')
    } finally {
      loading.value = false
    }
  }

  async function deleteDomainMapping(id: number) {
    if (!auth.token) return
    if (!confirm('Remove this domain → upstream mapping?')) return
    try {
      const response = await apiFetch(`/api/dns/domain-upstreams/${id}`, { method: 'DELETE' })
      if (response.ok) await loadSettings()
      else {
        const data = (await response.json()) as { message?: string }
        setError(data.message ?? 'Failed to delete mapping')
      }
    } catch {
      setError('Network error. Please try again.')
    }
  }

  async function addRecord() {
    if (!auth.token) return
    if (!newRecord.value.name || !newRecord.value.type || !newRecord.value.value) {
      setError('Name, type, and value are required')
      return
    }
    const existing = records.value.find(
      (r) => r.name === newRecord.value.name && r.type === newRecord.value.type
    )
    if (existing) {
      setError(
        `A DNS record with name '${newRecord.value.name}' and type '${newRecord.value.type}' already exists.`
      )
      return
    }
    loading.value = true
    try {
      const response = await apiFetch('/api/dns/records', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(newRecord.value)
      })
      if (response.ok) {
        showAddRecordModal.value = false
        resetNewRecord()
        await loadRecords()
      } else {
        const data = (await response.json()) as { message?: string }
        setError(data.message ?? 'Failed to add record')
      }
    } catch {
      setError('Network error. Please try again.')
    } finally {
      loading.value = false
    }
  }

  async function deleteRecord(recordId: number) {
    if (!auth.token) return
    if (!confirm('Are you sure you want to delete this record?')) return
    try {
      const response = await apiFetch(`/api/dns/records/${recordId}`, { method: 'DELETE' })
      if (response.ok) await loadRecords()
      else {
        const data = (await response.json()) as { message?: string }
        setError(data.message ?? 'Failed to delete record')
      }
    } catch {
      setError('Network error. Please try again.')
    }
  }

  async function updateRecord() {
    if (!auth.token) return
    if (editingRecord.value.id == null) return
    loading.value = true
    try {
      const response = await apiFetch(`/api/dns/records/${editingRecord.value.id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: editingRecord.value.name,
          type: editingRecord.value.type,
          value: editingRecord.value.value,
          ttl: editingRecord.value.ttl
        })
      })
      if (response.ok) {
        showEditRecordModal.value = false
        resetEditingRecord()
        await loadRecords()
      } else {
        const data = (await response.json()) as { message?: string }
        setError(data.message ?? 'Failed to update record')
      }
    } catch {
      setError('Network error. Please try again.')
    } finally {
      loading.value = false
    }
  }

  async function clearExpiredCache() {
    if (!auth.token) return
    try {
      const response = await apiFetch('/api/dns/cache/expired', { method: 'DELETE' })
      if (response.ok) {
        await loadCache()
        await loadDashboard()
      }
    } catch {
      setError('Network error. Please try again.')
    }
  }

  async function clearAllCache() {
    if (!auth.token) return
    if (!confirm('Are you sure you want to clear all cache?')) return
    try {
      const response = await apiFetch('/api/dns/cache', { method: 'DELETE' })
      if (response.ok) {
        await loadCache()
        await loadDashboard()
      }
    } catch {
      setError('Network error. Please try again.')
    }
  }

  async function saveSettings() {
    if (!auth.token) return
    loading.value = true
    try {
      const response = await apiFetch('/api/dns/config', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(settings.value)
      })
      if (response.ok) {
        const data = (await response.json()) as { message?: string }
        setError(null)
        alert(data.message ?? 'Saved')
      } else {
        const data = (await response.json()) as { message?: string }
        setError(data.message ?? 'Failed to save settings')
      }
    } catch {
      setError('Network error. Please try again.')
    } finally {
      loading.value = false
    }
  }

  async function addUser() {
    if (!auth.token) return
    loading.value = true
    try {
      const response = await apiFetch('/api/auth/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(newUser.value)
      })
      if (response.ok) {
        showAddUserModal.value = false
        resetNewUser()
        await loadUsers()
      } else {
        const data = (await response.json()) as { message?: string }
        setError(data.message ?? 'Failed to add user')
      }
    } catch {
      setError('Network error. Please try again.')
    } finally {
      loading.value = false
    }
  }

  async function deleteUser(userId: number) {
    if (!auth.token) return
    if (!confirm('Are you sure you want to delete this user?')) return
    try {
      const response = await apiFetch(`/api/auth/users/${userId}`, { method: 'DELETE' })
      if (response.ok) await loadUsers()
      else {
        const data = (await response.json()) as { message?: string }
        setError(data.message ?? 'Failed to delete user')
      }
    } catch {
      setError('Network error. Please try again.')
    }
  }

  return {
    version,
    loading,
    error,
    stats,
    records,
    cacheRecords,
    users,
    settings,
    domainUpstreamMappings,
    showAddRecordModal,
    showEditRecordModal,
    showAddUserModal,
    showAddDomainMappingModal,
    showEditDomainMappingModal,
    newRecord,
    editingRecord,
    newUser,
    newDomainMapping,
    editingDomainMapping,
    upstreamServersText,
    setError,
    loadVersion,
    loadDashboard,
    loadRecords,
    loadCache,
    loadUsers,
    loadSettings,
    resetNewRecord,
    resetEditingRecord,
    resetNewUser,
    resetNewDomainMapping,
    resetEditingDomainMapping,
    addDomainMapping,
    updateDomainMapping,
    deleteDomainMapping,
    addRecord,
    deleteRecord,
    updateRecord,
    clearExpiredCache,
    clearAllCache,
    saveSettings,
    addUser,
    deleteUser
  }
})
