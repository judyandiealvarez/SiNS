<script setup lang="ts">
import { onMounted } from 'vue'
import { useDnsAdminStore } from '@/stores/dnsAdmin'

const dns = useDnsAdminStore()

onMounted(() => {
  void dns.loadDashboard()
})
</script>

<template>
  <div>
    <h2 class="mb-4"><i class="ti ti-dashboard me-2"></i>Dashboard</h2>
    <div class="row row-cards">
      <div class="col-sm-6 col-lg-3">
        <div class="card">
          <div class="card-body text-center">
            <div class="text-primary mb-2"><i class="ti ti-list fs-1"></i></div>
            <div class="h1 mb-1">{{ dns.stats.totalRecords }}</div>
            <div class="text-secondary">DNS Records</div>
          </div>
        </div>
      </div>
      <div class="col-sm-6 col-lg-3">
        <div class="card">
          <div class="card-body text-center">
            <div class="text-success mb-2"><i class="ti ti-database fs-1"></i></div>
            <div class="h1 mb-1">{{ dns.stats.totalCache }}</div>
            <div class="text-secondary">Cache Entries</div>
          </div>
        </div>
      </div>
      <div class="col-sm-6 col-lg-3">
        <div class="card">
          <div class="card-body text-center">
            <div class="text-azure mb-2"><i class="ti ti-users fs-1"></i></div>
            <div class="h1 mb-1">{{ dns.stats.totalUsers }}</div>
            <div class="text-secondary">Users</div>
          </div>
        </div>
      </div>
      <div class="col-sm-6 col-lg-3">
        <div class="card">
          <div class="card-body text-center">
            <div class="text-warning mb-2"><i class="ti ti-clock fs-1"></i></div>
            <div class="h1 mb-1">{{ dns.stats.expiredCache }}</div>
            <div class="text-secondary">Expired Cache</div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
