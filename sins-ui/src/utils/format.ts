export function formatDate(dateString: string): string {
  return new Date(dateString).toLocaleString()
}

export function isExpired(dateString: string): boolean {
  return new Date(dateString) < new Date()
}
