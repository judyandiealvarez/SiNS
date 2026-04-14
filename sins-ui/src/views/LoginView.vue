<script setup lang="ts">
import { onMounted, reactive } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useDnsAdminStore } from '@/stores/dnsAdmin'

const auth = useAuthStore()
const dns = useDnsAdminStore()
const route = useRoute()
const router = useRouter()

const form = reactive({ username: '', password: '' })

onMounted(() => {
  void dns.loadVersion()
})

async function submit() {
  const ok = await auth.login(form.username, form.password)
  if (ok) {
    await dns.loadDashboard()
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : null
    await router.replace(redirect && redirect !== '/login' ? redirect : { name: 'dashboard' })
  }
}
</script>

<template>
  <div class="page page-center">
    <div class="container container-tight py-4">
      <div class="text-center text-secondary mb-4">
        <span class="avatar avatar-lg bg-azure-lt text-azure mb-2">
          <i class="ti ti-server-2 fs-1"></i>
        </span>
        <h1 class="h2">DNS Server</h1>
        <div class="text-secondary small">Management Console · v{{ dns.version }}</div>
      </div>

      <div class="card card-md">
        <div class="card-body">
          <h2 class="h3 text-center mb-4">
            <i class="ti ti-lock me-2"></i>
            Login
          </h2>
          <form @submit.prevent="submit">
            <div class="mb-3">
              <label class="form-label">Username</label>
              <input v-model="form.username" type="text" class="form-control" required autocomplete="username" />
            </div>
            <div class="mb-3">
              <label class="form-label">Password</label>
              <input
                v-model="form.password"
                type="password"
                class="form-control"
                required
                autocomplete="current-password"
              />
            </div>
            <button type="submit" class="btn btn-primary w-100" :disabled="auth.loginPending">
              <span
                v-if="auth.loginPending"
                class="spinner-border spinner-border-sm me-2"
                role="status"
                aria-hidden="true"
              ></span>
              <i v-else class="ti ti-login me-2"></i>
              {{ auth.loginPending ? 'Logging in…' : 'Login' }}
            </button>
          </form>
          <div v-if="auth.loginError" class="alert alert-danger mt-3 mb-0" role="alert">
            {{ auth.loginError }}
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
