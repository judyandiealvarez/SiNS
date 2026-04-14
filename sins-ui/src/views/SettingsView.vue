<script setup lang="ts">
import { onMounted } from 'vue'
import { useDnsAdminStore } from '@/stores/dnsAdmin'
import type { DomainMappingRow } from '@/types'

const dns = useDnsAdminStore()

onMounted(() => {
  void dns.loadSettings()
})

function openAddMapping() {
  dns.setError(null)
  dns.resetNewDomainMapping()
  dns.showAddDomainMappingModal = true
}

function closeAddMapping() {
  dns.showAddDomainMappingModal = false
  dns.resetNewDomainMapping()
}

function editMapping(m: DomainMappingRow) {
  dns.editingDomainMapping = { id: m.id, domain: m.domain, upstreamServer: m.upstreamServer }
  dns.showEditDomainMappingModal = true
}

function closeEditMapping() {
  dns.showEditDomainMappingModal = false
  dns.resetEditingDomainMapping()
}

function submitAddMapping() {
  dns.setError(null)
  if (dns.newDomainMapping.domain && dns.newDomainMapping.upstreamServer) void dns.addDomainMapping()
}

function submitEditMapping() {
  dns.setError(null)
  if (dns.editingDomainMapping.domain && dns.editingDomainMapping.upstreamServer) void dns.updateDomainMapping()
}
</script>

<template>
  <div>
    <h2 class="mb-4"><i class="ti ti-settings me-2"></i>Settings</h2>

    <div class="card">
      <div class="card-body">
        <form @submit.prevent="dns.saveSettings()">
          <div class="row">
            <div class="col-md-6 mb-3">
              <label class="form-label">Cache Timeout (minutes)</label>
              <input
                v-model.number="dns.settings.cacheTimeoutMinutes"
                type="number"
                class="form-control"
                min="1"
                max="1440"
                required
              />
              <div class="form-hint">How long to keep cache entries (1–1440 minutes)</div>
            </div>
            <div class="col-md-6 mb-3">
              <label class="form-label">UDP Port</label>
              <input v-model.number="dns.settings.udpPort" type="number" class="form-control" min="1" max="65535" required />
            </div>
          </div>
          <div class="row">
            <div class="col-md-6 mb-3">
              <label class="form-label">TCP Port</label>
              <input v-model.number="dns.settings.tcpPort" type="number" class="form-control" min="1" max="65535" required />
            </div>
            <div class="col-md-6 mb-3">
              <label class="form-label">Upstream DNS Servers</label>
              <textarea
                v-model="dns.upstreamServersText"
                class="form-control"
                rows="3"
                placeholder="8.8.8.8&#10;1.1.1.1&#10;9.9.9.9"
              />
              <div class="form-hint">One server per line (default for names with no domain mapping)</div>
            </div>
          </div>

          <hr class="my-4" />

          <h3 class="h4 mb-3"><i class="ti ti-affiliate me-2"></i>Domain → Upstream mapping</h3>
          <p class="text-secondary small">
            Queries for a name that matches a domain (e.g. <code>some.dev.net</code> for domain <code>dev.net</code>) are
            sent to that domain’s upstream and the result is cached.
          </p>
          <div class="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-2">
            <span class="text-secondary small">e.g. dev.net → 10.11.4.17, test.net → 10.11.3.17</span>
            <button type="button" class="btn btn-sm btn-primary" @click="openAddMapping">
              <i class="ti ti-plus me-1"></i>
              Add mapping
            </button>
          </div>

          <div class="table-responsive">
            <table class="table table-sm table-vcenter">
              <thead>
                <tr>
                  <th>Domain</th>
                  <th>Upstream server</th>
                  <th class="w-1">Actions</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="m in dns.domainUpstreamMappings" :key="m.id">
                  <td><code>{{ m.domain }}</code></td>
                  <td>{{ m.upstreamServer }}</td>
                  <td>
                    <div class="btn-list flex-nowrap">
                      <button type="button" class="btn btn-sm btn-outline-primary" @click="editMapping(m)">
                        <i class="ti ti-edit"></i>
                      </button>
                      <button type="button" class="btn btn-sm btn-outline-danger" @click="dns.deleteDomainMapping(m.id)">
                        <i class="ti ti-trash"></i>
                      </button>
                    </div>
                  </td>
                </tr>
                <tr v-if="dns.domainUpstreamMappings.length === 0">
                  <td colspan="3" class="text-secondary">
                    No domain mappings. Use env <code>DOMAIN_UPSTREAM_MAPPINGS</code> at startup or add below.
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <button type="submit" class="btn btn-primary mt-3" :disabled="dns.loading">
            <span v-if="dns.loading" class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
            <i v-else class="ti ti-device-floppy me-2"></i>
            {{ dns.loading ? 'Saving…' : 'Save Settings' }}
          </button>
        </form>
      </div>
    </div>

    <div
      v-if="dns.showAddDomainMappingModal"
      class="modal modal-blur fade show d-block"
      tabindex="-1"
      role="dialog"
      aria-modal="true"
    >
      <div class="modal-dialog modal-dialog-centered" role="document">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Add domain → upstream mapping</h5>
            <button type="button" class="btn-close" aria-label="Close" @click="closeAddMapping" />
          </div>
          <div class="modal-body">
            <div v-if="dns.error" class="alert alert-danger mb-3">{{ dns.error }}</div>
            <div class="mb-3">
              <label class="form-label">Domain</label>
              <input v-model="dns.newDomainMapping.domain" type="text" class="form-control" placeholder="dev.net" />
            </div>
            <div class="mb-3">
              <label class="form-label">Upstream server</label>
              <input
                v-model="dns.newDomainMapping.upstreamServer"
                type="text"
                class="form-control"
                placeholder="10.11.4.17"
              />
            </div>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" @click="closeAddMapping">Cancel</button>
            <button type="button" class="btn btn-primary" @click="submitAddMapping">Add</button>
          </div>
        </div>
      </div>
    </div>
    <div v-if="dns.showAddDomainMappingModal" class="modal-backdrop show"></div>

    <div
      v-if="dns.showEditDomainMappingModal"
      class="modal modal-blur fade show d-block"
      tabindex="-1"
      role="dialog"
      aria-modal="true"
    >
      <div class="modal-dialog modal-dialog-centered" role="document">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Edit domain → upstream mapping</h5>
            <button type="button" class="btn-close" aria-label="Close" @click="closeEditMapping" />
          </div>
          <div class="modal-body">
            <div v-if="dns.error" class="alert alert-danger mb-3">{{ dns.error }}</div>
            <div class="mb-3">
              <label class="form-label">Domain</label>
              <input v-model="dns.editingDomainMapping.domain" type="text" class="form-control" />
            </div>
            <div class="mb-3">
              <label class="form-label">Upstream server</label>
              <input v-model="dns.editingDomainMapping.upstreamServer" type="text" class="form-control" />
            </div>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-secondary" @click="closeEditMapping">Cancel</button>
            <button type="button" class="btn btn-primary" @click="submitEditMapping">Update</button>
          </div>
        </div>
      </div>
    </div>
    <div v-if="dns.showEditDomainMappingModal" class="modal-backdrop show"></div>
  </div>
</template>
