<script setup lang="ts">
import { onMounted } from 'vue'
import { useDnsAdminStore } from '@/stores/dnsAdmin'
import type { DnsRecordRow } from '@/types'

const dns = useDnsAdminStore()

onMounted(() => {
  void dns.loadRecords()
})

function openAdd() {
  dns.setError(null)
  dns.resetNewRecord()
  dns.showAddRecordModal = true
}

function closeAdd() {
  dns.showAddRecordModal = false
  dns.resetNewRecord()
}

function submitAdd(ev: SubmitEvent) {
  const form = ev.currentTarget as HTMLFormElement
  if (!form.checkValidity()) {
    form.reportValidity()
    return
  }
  dns.setError(null)
  void dns.addRecord()
}

function edit(r: DnsRecordRow) {
  dns.editingRecord = {
    id: r.id,
    name: r.name,
    type: r.type,
    value: r.value,
    ttl: r.ttl
  }
  dns.showEditRecordModal = true
}

function closeEdit() {
  dns.showEditRecordModal = false
  dns.resetEditingRecord()
}

function submitEdit(ev: SubmitEvent) {
  const form = ev.currentTarget as HTMLFormElement
  if (!form.checkValidity()) {
    form.reportValidity()
    return
  }
  void dns.updateRecord()
}
</script>

<template>
  <div>
    <div class="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-4">
      <h2 class="mb-0"><i class="ti ti-list me-2"></i>DNS Records</h2>
      <button type="button" class="btn btn-primary" @click="openAdd">
        <i class="ti ti-plus me-1"></i>
        Add Record
      </button>
    </div>

    <div v-if="dns.error" class="alert alert-danger alert-dismissible" role="alert">
      <div class="d-flex">
        <div><i class="ti ti-alert-triangle me-2"></i>{{ dns.error }}</div>
        <button type="button" class="btn-close ms-auto" aria-label="Close" @click="dns.setError(null)" />
      </div>
    </div>

    <div class="card">
      <div class="table-responsive">
        <table class="table table-vcenter card-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Type</th>
              <th>Value</th>
              <th>TTL</th>
              <th class="w-1">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="r in dns.records" :key="r.id">
              <td>{{ r.name }}</td>
              <td><span class="badge bg-primary">{{ r.type }}</span></td>
              <td>{{ r.value }}</td>
              <td>{{ r.ttl }}</td>
              <td>
                <div class="btn-list flex-nowrap">
                  <button type="button" class="btn btn-sm btn-outline-primary" @click="edit(r)">
                    <i class="ti ti-edit"></i>
                  </button>
                  <button type="button" class="btn btn-sm btn-outline-danger" @click="dns.deleteRecord(r.id)">
                    <i class="ti ti-trash"></i>
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <div
      v-if="dns.showAddRecordModal"
      class="modal modal-blur fade show d-block"
      tabindex="-1"
      role="dialog"
      aria-modal="true"
    >
      <div class="modal-dialog modal-dialog-centered" role="document">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Add DNS Record</h5>
            <button type="button" class="btn-close" aria-label="Close" @click="closeAdd" />
          </div>
          <form @submit.prevent="submitAdd">
            <div class="modal-body">
              <div v-if="dns.error" class="alert alert-danger mb-3">{{ dns.error }}</div>
              <div class="mb-3">
                <label class="form-label">Name</label>
                <input v-model="dns.newRecord.name" type="text" class="form-control" required />
              </div>
              <div class="mb-3">
                <label class="form-label">Type</label>
                <select v-model="dns.newRecord.type" class="form-select" required>
                  <option value="A">A</option>
                  <option value="AAAA">AAAA</option>
                  <option value="CNAME">CNAME</option>
                  <option value="MX">MX</option>
                  <option value="TXT">TXT</option>
                </select>
              </div>
              <div class="mb-3">
                <label class="form-label">Value</label>
                <input v-model="dns.newRecord.value" type="text" class="form-control" required />
              </div>
              <div class="mb-3">
                <label class="form-label">TTL</label>
                <input v-model.number="dns.newRecord.ttl" type="number" class="form-control" min="1" required />
              </div>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" @click="closeAdd">Cancel</button>
              <button type="submit" class="btn btn-primary">Add Record</button>
            </div>
          </form>
        </div>
      </div>
    </div>
    <div v-if="dns.showAddRecordModal" class="modal-backdrop show"></div>

    <div
      v-if="dns.showEditRecordModal"
      class="modal modal-blur fade show d-block"
      tabindex="-1"
      role="dialog"
      aria-modal="true"
    >
      <div class="modal-dialog modal-dialog-centered" role="document">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Edit DNS Record</h5>
            <button type="button" class="btn-close" aria-label="Close" @click="closeEdit" />
          </div>
          <form @submit.prevent="submitEdit">
            <div class="modal-body">
              <div class="mb-3">
                <label class="form-label">Name</label>
                <input v-model="dns.editingRecord.name" type="text" class="form-control" required />
              </div>
              <div class="mb-3">
                <label class="form-label">Type</label>
                <select v-model="dns.editingRecord.type" class="form-select" required>
                  <option value="A">A</option>
                  <option value="AAAA">AAAA</option>
                  <option value="CNAME">CNAME</option>
                  <option value="MX">MX</option>
                  <option value="TXT">TXT</option>
                </select>
              </div>
              <div class="mb-3">
                <label class="form-label">Value</label>
                <input v-model="dns.editingRecord.value" type="text" class="form-control" required />
              </div>
              <div class="mb-3">
                <label class="form-label">TTL</label>
                <input v-model.number="dns.editingRecord.ttl" type="number" class="form-control" min="1" required />
              </div>
            </div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" @click="closeEdit">Cancel</button>
              <button type="submit" class="btn btn-primary">Update Record</button>
            </div>
          </form>
        </div>
      </div>
    </div>
    <div v-if="dns.showEditRecordModal" class="modal-backdrop show"></div>
  </div>
</template>
