<script setup lang="ts">
import { onMounted } from 'vue'
import { useDnsAdminStore } from '@/stores/dnsAdmin'

const dns = useDnsAdminStore()

onMounted(() => {
  void dns.loadUsers()
})

function openAdd() {
  dns.showAddUserModal = true
  dns.resetNewUser()
}

function closeAdd() {
  dns.showAddUserModal = false
}
</script>

<template>
  <div>
    <div class="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-4">
      <h2 class="mb-0"><i class="ti ti-users me-2"></i>Users</h2>
      <button type="button" class="btn btn-primary" @click="openAdd">
        <i class="ti ti-plus me-1"></i>
        Add User
      </button>
    </div>

    <div class="card">
      <div class="table-responsive">
        <table class="table table-vcenter card-table">
          <thead>
            <tr>
              <th>Username</th>
              <th>Email</th>
              <th>Role</th>
              <th class="w-1">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="u in dns.users" :key="u.id">
              <td>{{ u.username }}</td>
              <td>{{ u.email }}</td>
              <td>
                <span class="badge" :class="u.role === 'Admin' ? 'bg-danger' : 'bg-secondary'">{{ u.role }}</span>
              </td>
              <td>
                <button type="button" class="btn btn-sm btn-outline-danger" @click="dns.deleteUser(u.id)">
                  <i class="ti ti-trash"></i>
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <div
      v-if="dns.showAddUserModal"
      class="modal modal-blur fade show d-block"
      tabindex="-1"
      role="dialog"
      aria-modal="true"
    >
      <div class="modal-dialog modal-dialog-centered" role="document">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Add User</h5>
            <button type="button" class="btn-close" aria-label="Close" @click="closeAdd" />
          </div>
          <div class="modal-body">
            <form id="addUserForm" @submit.prevent="dns.addUser()">
              <div class="mb-3">
                <label class="form-label">Username</label>
                <input v-model="dns.newUser.username" type="text" class="form-control" required />
              </div>
              <div class="mb-3">
                <label class="form-label">Email</label>
                <input v-model="dns.newUser.email" type="email" class="form-control" required />
              </div>
              <div class="mb-3">
                <label class="form-label">Password</label>
                <input v-model="dns.newUser.password" type="password" class="form-control" required />
              </div>
              <div class="mb-3">
                <label class="form-label">Role</label>
                <select v-model="dns.newUser.role" class="form-select" required>
                  <option value="User">User</option>
                  <option value="Admin">Admin</option>
                </select>
              </div>
            </form>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" @click="closeAdd">Cancel</button>
            <button type="button" class="btn btn-primary" @click="dns.addUser()">Add User</button>
          </div>
        </div>
      </div>
    </div>
    <div v-if="dns.showAddUserModal" class="modal-backdrop show"></div>
  </div>
</template>
