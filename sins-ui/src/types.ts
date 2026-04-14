export interface DashboardStats {
  totalRecords: number
  totalCache: number
  totalUsers: number
  expiredCache: number
}

export interface DnsRecordRow {
  id: number
  name: string
  type: string
  value: string
  ttl: number
}

export interface CacheDetailRow {
  id: number
  name: string
  type: string
  resolvedIPs?: string[] | string
  responseSize: number
  upstreamServer?: string | null
  cachedAt: string
  expiresAt: string
}

export interface UserRow {
  id: number
  username: string
  email: string
  role: string
}

export interface ServerSettings {
  cacheTimeoutMinutes: number
  udpPort: number
  tcpPort: number
  upstreamServers: string[]
  haproxy: string | null
}

export interface DomainMappingRow {
  id: number
  domain: string
  upstreamServer: string
}

export interface CurrentUser {
  username: string
  role: string
}
