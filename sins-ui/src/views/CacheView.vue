<script setup lang="ts">
import { onMounted } from 'vue'
import { useDnsAdminStore } from '@/stores/dnsAdmin'
import { formatDate, isExpired } from '@/utils/format'

const dns = useDnsAdminStore()

onMounted(() => {
  void dns.loadCache()
})
</script>

<template>
  <div>
    <div class="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-4">
      <h2 class="mb-0"><i class="ti ti-database me-2"></i>Cache</h2>
      <div class="btn-list">
        <button type="button" class="btn btn-warning" @click="dns.clearExpiredCache()">
          <i class="ti ti-broom me-1"></i>
          Clear Expired
        </button>
        <button type="button" class="btn btn-danger" @click="dns.clearAllCache()">
          <i class="ti ti-trash me-1"></i>
          Clear All
        </button>
      </div>
    </div>

    <div class="card">
      <div class="table-responsive">
        <table class="table table-vcenter card-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Type</th>
              <th>Resolved IPs</th>
              <th>Response Size</th>
              <th>Upstream Server</th>
              <th>Cached At</th>
              <th>Expires At</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="c in dns.cacheRecords" :key="c.id">
              <td>{{ c.name }}</td>
              <td><span class="badge bg-azure text-azure-fg">{{ c.type }}</span></td>
              <td>
                {{
                  Array.isArray(c.resolvedIPs)
                    ? c.resolvedIPs.join(', ') || 'N/A'
                    : c.resolvedIPs || 'N/A'
                }}
              </td>
              <td>{{ c.responseSize }} bytes</td>
              <td>{{ c.upstreamServer || 'N/A' }}</td>
              <td>{{ formatDate(c.cachedAt) }}</td>
              <td>
                <span :class="isExpired(c.expiresAt) ? 'text-danger' : 'text-success'">
                  {{ formatDate(c.expiresAt) }}
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
