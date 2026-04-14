<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useDnsAdminStore } from '@/stores/dnsAdmin'

const auth = useAuthStore()
const dns = useDnsAdminStore()
const router = useRouter()

onMounted(() => {
  void dns.loadVersion()
  if (auth.isAuthenticated) void dns.loadDashboard()
})

function logout() {
  void auth.logout()
  void router.push({ name: 'login' })
}
</script>

<template>
  <div class="page">
    <aside class="app-sidebar navbar navbar-vertical navbar-expand-md navbar-dark d-flex flex-column">
      <div class="container-fluid app-sidebar-inner d-flex flex-column flex-grow-1 px-3 pt-3 pb-3">
        <h1 class="navbar-brand app-sidebar-brand mb-0">
          <span class="navbar-brand-icon d-inline-flex align-items-center justify-content-center">
            <i class="ti ti-server-2"></i>
          </span>
          <span class="ps-2 fw-bold">DNS Server</span>
        </h1>
        <div class="app-sidebar-muted small mb-1">Management Console</div>
        <div class="app-sidebar-muted small mb-4">
          <i class="ti ti-git-branch me-1"></i>
          v{{ dns.version }}
        </div>

        <nav class="nav app-sidebar-nav flex-column gap-1">
          <router-link class="nav-link app-sidebar-link" active-class="active" :to="{ name: 'dashboard' }">
            <span class="nav-link-icon d-inline-flex align-items-center justify-content-center">
              <i class="ti ti-dashboard"></i>
            </span>
            <span class="nav-link-title">Dashboard</span>
          </router-link>
          <router-link class="nav-link app-sidebar-link" active-class="active" :to="{ name: 'records' }">
            <span class="nav-link-icon d-inline-flex align-items-center justify-content-center">
              <i class="ti ti-list"></i>
            </span>
            <span class="nav-link-title">DNS Records</span>
          </router-link>
          <router-link class="nav-link app-sidebar-link" active-class="active" :to="{ name: 'cache' }">
            <span class="nav-link-icon d-inline-flex align-items-center justify-content-center">
              <i class="ti ti-database"></i>
            </span>
            <span class="nav-link-title">Cache</span>
          </router-link>
          <router-link class="nav-link app-sidebar-link" active-class="active" :to="{ name: 'settings' }">
            <span class="nav-link-icon d-inline-flex align-items-center justify-content-center">
              <i class="ti ti-settings"></i>
            </span>
            <span class="nav-link-title">Settings</span>
          </router-link>
          <router-link class="nav-link app-sidebar-link" active-class="active" :to="{ name: 'users' }">
            <span class="nav-link-icon d-inline-flex align-items-center justify-content-center">
              <i class="ti ti-users"></i>
            </span>
            <span class="nav-link-title">Users</span>
          </router-link>
        </nav>

        <div v-if="auth.currentUser" class="app-sidebar-footer mt-auto pt-4">
          <div class="app-sidebar-muted small mb-2">
            Logged in as <strong class="app-sidebar-user">{{ auth.currentUser.username }}</strong>
          </div>
          <button type="button" class="btn app-sidebar-logout w-100" @click="logout">
            <i class="ti ti-logout me-1"></i>
            Logout
          </button>
        </div>
      </div>
    </aside>

    <div class="page-wrapper">
      <div class="page-body">
        <div class="container-xl py-4">
          <router-view />
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.app-sidebar {
  --app-sidebar-bg: #6a73d1;
  --app-sidebar-active-inner: rgba(0, 0, 0, 0.14);
  --app-sidebar-hover: rgba(255, 255, 255, 0.14);
  --app-sidebar-muted: rgba(255, 255, 255, 0.72);
  background-color: var(--app-sidebar-bg) !important;
  border-right: none;
  width: 16rem;
  min-height: 100vh;
}

.app-sidebar-inner {
  min-height: 0;
}

.app-sidebar-brand,
.app-sidebar-brand .navbar-brand-icon {
  color: #fff !important;
}

.app-sidebar-muted {
  color: var(--app-sidebar-muted);
}

.app-sidebar-user {
  color: #fff;
  font-weight: 700;
}

.app-sidebar-nav {
  margin-bottom: 1rem;
}

.app-sidebar-link {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: #fff !important;
  border-radius: 7px;
  padding: 0.55rem 0.65rem;
  border: 1px solid transparent;
  font-weight: 500;
  transition:
    background-color 0.15s ease,
    border-color 0.15s ease;
}

.app-sidebar-link:hover {
  background-color: var(--app-sidebar-hover);
  color: #fff !important;
}

.app-sidebar-link.active {
  font-weight: 600;
  background-color: var(--app-sidebar-active-inner);
  border-color: #fff;
  color: #fff !important;
}

.app-sidebar-link .nav-link-icon {
  width: 1.35rem;
  font-size: 1.1rem;
  opacity: 0.95;
}

.app-sidebar-footer {
  border-top: 1px solid rgba(255, 255, 255, 0.22);
}

.app-sidebar-logout {
  color: #fff;
  border: 1px solid #fff;
  border-radius: 7px;
  padding: 0.45rem 0.75rem;
  font-weight: 500;
  background: transparent;
}

.app-sidebar-logout:hover {
  color: var(--app-sidebar-bg);
  background: #fff;
  border-color: #fff;
}

.page {
  display: flex;
  min-height: 100vh;
}

.page-wrapper {
  flex: 1;
  min-width: 0;
  background: var(--tblr-bg-surface-secondary, #f1f5f9);
}
</style>
