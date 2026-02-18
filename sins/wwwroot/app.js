// Helper function to handle API calls with automatic 401 handling
let storeInstance = null;

function setStoreInstance(store) {
    storeInstance = store;
}

async function apiFetch(url, options = {}) {
    const token = localStorage.getItem('token');
    
    // Add Authorization header if token exists
    const headers = {
        ...options.headers,
        ...(token && { 'Authorization': `Bearer ${token}` })
    };
    
    const response = await fetch(url, {
        ...options,
        headers
    });
    
    // Handle 401 Unauthorized - token expired or invalid
    if (response.status === 401) {
        // Clear auth state in store if available
        if (storeInstance) {
            storeInstance.commit('CLEAR_AUTH');
        } else {
            // Fallback: clear localStorage directly
            localStorage.removeItem('token');
            localStorage.removeItem('currentUser');
        }
        
        // Redirect to login page
        window.location.href = '/';
        
        // Return a rejected promise to stop further execution
        throw new Error('Session expired. Please login again.');
    }
    
    return response;
}

// Vuex Store
const store = Vuex.createStore({
    state() {
        return {
            // Authentication
            token: localStorage.getItem('token') || null,
            currentUser: JSON.parse(localStorage.getItem('currentUser') || 'null') || null,
            
            // Version
            version: '1.0.0.0',
            
            // UI State
            loading: false,
            error: null,
            currentSection: 'dashboard',
            
            // Data
            stats: {
                totalRecords: 0,
                totalCache: 0,
                totalUsers: 0,
                expiredCache: 0
            },
            records: [],
            cacheRecords: [],
            users: [],
            settings: {
                cacheTimeoutMinutes: 60,
                udpPort: 53,
                tcpPort: 53,
                upstreamServers: ['8.8.8.8', '1.1.1.1', '2001:4860:4860::8888', '2606:4700:4700::1111'],
                haproxy: null
            },
            domainUpstreamMappings: [],
            
            // Modals
            showAddRecordModal: false,
            showEditRecordModal: false,
            showAddUserModal: false,
            showAddDomainMappingModal: false,
            showEditDomainMappingModal: false,
            
            // Forms
            loginForm: {
                username: '',
                password: ''
            },
            newRecord: {
                name: '',
                type: 'A',
                value: '',
                ttl: 3600
            },
            newUser: {
                username: '',
                email: '',
                password: '',
                role: 'User'
            },
            
            // Edit Record
            editingRecord: {
                id: null,
                name: '',
                type: 'A',
                value: '',
                ttl: 3600
            },
            newDomainMapping: { domain: '', upstreamServer: '' },
            editingDomainMapping: { id: null, domain: '', upstreamServer: '' }
        }
    },
    
    getters: {
        isAuthenticated: (state) => !!state.token && !!state.currentUser,
        upstreamServersText: (state) => state.settings.upstreamServers.join('\n')
    },
    
    mutations: {
        SET_TOKEN(state, token) {
            state.token = token;
            localStorage.setItem('token', token);
        },
        
        SET_CURRENT_USER(state, user) {
            state.currentUser = user;
            localStorage.setItem('currentUser', JSON.stringify(user));
        },
        
        CLEAR_AUTH(state) {
            state.token = null;
            state.currentUser = null;
            localStorage.removeItem('token');
            localStorage.removeItem('currentUser');
        },
        
        SET_LOADING(state, loading) {
            state.loading = loading;
        },
        
        SET_ERROR(state, error) {
            state.error = error;
        },
        
        SET_CURRENT_SECTION(state, section) {
            state.currentSection = section;
        },
        
        SET_STATS(state, stats) {
            state.stats = stats;
        },
        
        SET_RECORDS(state, records) {
            state.records = records;
        },
        
        SET_CACHE_RECORDS(state, records) {
            state.cacheRecords = records;
        },
        
        SET_USERS(state, users) {
            state.users = users;
        },
        
        SET_SETTINGS(state, settings) {
            state.settings = settings;
        },
        
        SET_DOMAIN_UPSTREAM_MAPPINGS(state, mappings) {
            state.domainUpstreamMappings = mappings || [];
        },
        
        SET_SHOW_ADD_RECORD_MODAL(state, show) {
            state.showAddRecordModal = show;
        },
        
        SET_SHOW_EDIT_RECORD_MODAL(state, show) {
            state.showEditRecordModal = show;
        },
        
        SET_SHOW_ADD_USER_MODAL(state, show) {
            state.showAddUserModal = show;
        },
        
        SET_SHOW_ADD_DOMAIN_MAPPING_MODAL(state, show) {
            state.showAddDomainMappingModal = show;
        },
        
        SET_SHOW_EDIT_DOMAIN_MAPPING_MODAL(state, show) {
            state.showEditDomainMappingModal = show;
        },
        
        RESET_NEW_RECORD(state) {
            state.newRecord = {
                name: '',
                type: 'A',
                value: '',
                ttl: 3600
            };
        },
        
        SET_EDITING_RECORD(state, record) {
            state.editingRecord = {
                id: record.id,
                name: record.name,
                type: record.type,
                value: record.value,
                ttl: record.ttl
            };
        },
        
        RESET_EDITING_RECORD(state) {
            state.editingRecord = {
                id: null,
                name: '',
                type: 'A',
                value: '',
                ttl: 3600
            };
        },
        
        SET_EDITING_DOMAIN_MAPPING(state, mapping) {
            state.editingDomainMapping = {
                id: mapping.id,
                domain: mapping.domain,
                upstreamServer: mapping.upstreamServer
            };
        },
        
        RESET_EDITING_DOMAIN_MAPPING(state) {
            state.editingDomainMapping = { id: null, domain: '', upstreamServer: '' };
        },
        
        RESET_NEW_DOMAIN_MAPPING(state) {
            state.newDomainMapping = { domain: '', upstreamServer: '' };
        },
        
        RESET_NEW_USER(state) {
            state.newUser = {
                username: '',
                email: '',
                password: '',
                role: 'User'
            };
        },
        
        UPDATE_UPSTREAM_SERVERS_TEXT(state, text) {
            state.settings.upstreamServers = text.split('\n').filter(s => s.trim());
        },
        
        SET_VERSION(state, version) {
            state.version = version;
        }
    },
    
    actions: {
        async login({ commit, state }) {
            commit('SET_LOADING', true);
            commit('SET_ERROR', null);
            
            try {
                const response = await fetch('/api/auth/login', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(state.loginForm)
                });
                
                const data = await response.json();
                
                if (response.ok) {
                    commit('SET_TOKEN', data.token);
                    // Handle both old and new API response formats
                    const userRole = data.user?.role || 'Admin'; // Default to Admin for existing users
                    commit('SET_CURRENT_USER', {
                        username: state.loginForm.username,
                        role: userRole
                    });
                    await this.dispatch('loadDashboard');
                } else {
                    commit('SET_ERROR', data.message || 'Login failed');
                }
            } catch (error) {
                commit('SET_ERROR', 'Network error. Please try again.');
            } finally {
                commit('SET_LOADING', false);
            }
        },
        
        logout({ commit }) {
            commit('CLEAR_AUTH');
            commit('SET_CURRENT_SECTION', 'dashboard');
        },
        
        async loadDashboard({ commit, state }) {
            if (!state.token) return;
            
            try {
                const response = await apiFetch('/api/dns/stats');
                
                if (response.ok) {
                    const stats = await response.json();
                    commit('SET_STATS', stats);
                }
            } catch (error) {
                console.error('Failed to load dashboard:', error);
            }
        },
        
        async loadVersion({ commit }) {
            try {
                const response = await fetch('/api/dns/version');
                if (response.ok) {
                    const data = await response.json();
                    commit('SET_VERSION', data.version);
                }
            } catch (error) {
                console.error('Failed to load version:', error);
            }
        },
        
        async loadRecords({ commit, state }) {
            if (!state.token) return;
            
            try {
                const response = await apiFetch('/api/dns/records');
                
                if (response.ok) {
                    const records = await response.json();
                    commit('SET_RECORDS', records);
                }
            } catch (error) {
                console.error('Failed to load records:', error);
            }
        },
        
        async loadCache({ commit, state }) {
            if (!state.token) return;
            
            try {
                const response = await apiFetch('/api/dns/cache/details');
                
                if (response.ok) {
                    const records = await response.json();
                    commit('SET_CACHE_RECORDS', records);
                }
            } catch (error) {
                console.error('Failed to load cache:', error);
            }
        },
        
        async loadUsers({ commit, state }) {
            if (!state.token) return;
            
            try {
                const response = await apiFetch('/api/auth/users');
                
                if (response.ok) {
                    const users = await response.json();
                    commit('SET_USERS', users);
                }
            } catch (error) {
                console.error('Failed to load users:', error);
            }
        },
        
        async loadSettings({ commit, state }) {
            if (!state.token) return;
            
            try {
                const [configRes, mappingsRes] = await Promise.all([
                    apiFetch('/api/dns/config'),
                    apiFetch('/api/dns/domain-upstreams')
                ]);
                
                if (configRes.ok) {
                    const settings = await configRes.json();
                    commit('SET_SETTINGS', settings);
                }
                if (mappingsRes.ok) {
                    const mappings = await mappingsRes.json();
                    commit('SET_DOMAIN_UPSTREAM_MAPPINGS', mappings);
                }
            } catch (error) {
                console.error('Failed to load settings:', error);
            }
        },
        
        async addDomainMapping({ commit, state, dispatch }) {
            if (!state.token) return;
            if (!state.newDomainMapping.domain.trim() || !state.newDomainMapping.upstreamServer.trim()) {
                commit('SET_ERROR', 'Domain and upstream server are required');
                return;
            }
            commit('SET_LOADING', true);
            commit('SET_ERROR', null);
            try {
                const response = await apiFetch('/api/dns/domain-upstreams', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        domain: state.newDomainMapping.domain.trim(),
                        upstreamServer: state.newDomainMapping.upstreamServer.trim()
                    })
                });
                if (response.ok) {
                    commit('SET_SHOW_ADD_DOMAIN_MAPPING_MODAL', false);
                    commit('RESET_NEW_DOMAIN_MAPPING');
                    await dispatch('loadSettings');
                } else {
                    const data = await response.json();
                    commit('SET_ERROR', data.message || 'Failed to add mapping');
                }
            } catch (error) {
                commit('SET_ERROR', 'Network error. Please try again.');
            } finally {
                commit('SET_LOADING', false);
            }
        },
        
        async updateDomainMapping({ commit, state, dispatch }) {
            if (!state.token) return;
            if (!state.editingDomainMapping.domain.trim() || !state.editingDomainMapping.upstreamServer.trim()) {
                commit('SET_ERROR', 'Domain and upstream server are required');
                return;
            }
            commit('SET_LOADING', true);
            commit('SET_ERROR', null);
            try {
                const response = await apiFetch(`/api/dns/domain-upstreams/${state.editingDomainMapping.id}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        domain: state.editingDomainMapping.domain.trim(),
                        upstreamServer: state.editingDomainMapping.upstreamServer.trim()
                    })
                });
                if (response.ok) {
                    commit('SET_SHOW_EDIT_DOMAIN_MAPPING_MODAL', false);
                    commit('RESET_EDITING_DOMAIN_MAPPING');
                    await dispatch('loadSettings');
                } else {
                    const data = await response.json();
                    commit('SET_ERROR', data.message || 'Failed to update mapping');
                }
            } catch (error) {
                commit('SET_ERROR', 'Network error. Please try again.');
            } finally {
                commit('SET_LOADING', false);
            }
        },
        
        async deleteDomainMapping({ commit, state, dispatch }, id) {
            if (!state.token) return;
            if (!confirm('Remove this domain → upstream mapping?')) return;
            try {
                const response = await apiFetch(`/api/dns/domain-upstreams/${id}`, { method: 'DELETE' });
                if (response.ok) {
                    await dispatch('loadSettings');
                } else {
                    const data = await response.json();
                    commit('SET_ERROR', data.message || 'Failed to delete mapping');
                }
            } catch (error) {
                commit('SET_ERROR', 'Network error. Please try again.');
            }
        },
        
        async addRecord({ commit, state, dispatch }) {
            if (!state.token) return;
            
            // Validate required fields
            if (!state.newRecord.name || !state.newRecord.type || !state.newRecord.value) {
                commit('SET_ERROR', 'Name, type, and value are required');
                return;
            }
            
            // Check if record already exists (only active records for frontend validation)
            const existingRecord = state.records.find(record => 
                record.name === state.newRecord.name && 
                record.type === state.newRecord.type
            );
            
            if (existingRecord) {
                commit('SET_ERROR', `A DNS record with name '${state.newRecord.name}' and type '${state.newRecord.type}' already exists.`);
                return;
            }
            
            // TEMPORARY: Log what we're about to send
            console.log('About to send record:', state.newRecord);
            
            commit('SET_LOADING', true);
            
            try {
                const response = await apiFetch('/api/dns/records', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(state.newRecord)
                });
                
                if (response.ok) {
                    commit('SET_SHOW_ADD_RECORD_MODAL', false);
                    commit('RESET_NEW_RECORD');
                    await dispatch('loadRecords');
                } else {
                    const data = await response.json();
                    commit('SET_ERROR', data.message || 'Failed to add record');
                }
            } catch (error) {
                commit('SET_ERROR', 'Network error. Please try again.');
            } finally {
                commit('SET_LOADING', false);
            }
        },
        
        async deleteRecord({ commit, state, dispatch }, recordId) {
            if (!state.token) return;
            
            if (!confirm('Are you sure you want to delete this record?')) return;
            
            try {
                const response = await apiFetch(`/api/dns/records/${recordId}`, {
                    method: 'DELETE'
                });
                
                if (response.ok) {
                    await dispatch('loadRecords');
                } else {
                    const data = await response.json();
                    commit('SET_ERROR', data.message || 'Failed to delete record');
                }
            } catch (error) {
                commit('SET_ERROR', 'Network error. Please try again.');
            }
        },
        
        async updateRecord({ commit, state, dispatch }) {
            if (!state.token) return;
            
            commit('SET_LOADING', true);
            
            try {
                const response = await apiFetch(`/api/dns/records/${state.editingRecord.id}`, {
                    method: 'PUT',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        name: state.editingRecord.name,
                        type: state.editingRecord.type,
                        value: state.editingRecord.value,
                        ttl: state.editingRecord.ttl
                    })
                });
                
                if (response.ok) {
                    commit('SET_SHOW_EDIT_RECORD_MODAL', false);
                    commit('RESET_EDITING_RECORD');
                    await dispatch('loadRecords');
                } else {
                    const data = await response.json();
                    commit('SET_ERROR', data.message || 'Failed to update record');
                }
            } catch (error) {
                commit('SET_ERROR', 'Network error. Please try again.');
            } finally {
                commit('SET_LOADING', false);
            }
        },
        
        async clearExpiredCache({ commit, state, dispatch }) {
            if (!state.token) return;
            
            try {
                const response = await apiFetch('/api/dns/cache/expired', {
                    method: 'DELETE'
                });
                
                if (response.ok) {
                    await dispatch('loadCache');
                    await dispatch('loadDashboard');
                }
            } catch (error) {
                commit('SET_ERROR', 'Network error. Please try again.');
            }
        },
        
        async clearAllCache({ commit, state, dispatch }) {
            if (!state.token) return;
            
            if (!confirm('Are you sure you want to clear all cache?')) return;
            
            try {
                const response = await apiFetch('/api/dns/cache', {
                    method: 'DELETE'
                });
                
                if (response.ok) {
                    await dispatch('loadCache');
                    await dispatch('loadDashboard');
                }
            } catch (error) {
                commit('SET_ERROR', 'Network error. Please try again.');
            }
        },
        
        async saveSettings({ commit, state, dispatch }) {
            if (!state.token) return;
            
            commit('SET_LOADING', true);
            
            try {
                const response = await apiFetch('/api/dns/config', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(state.settings)
                });
                
                if (response.ok) {
                    const data = await response.json();
                    commit('SET_ERROR', null);
                    alert(data.message);
                } else {
                    const data = await response.json();
                    commit('SET_ERROR', data.message || 'Failed to save settings');
                }
            } catch (error) {
                commit('SET_ERROR', 'Network error. Please try again.');
            } finally {
                commit('SET_LOADING', false);
            }
        },
        
        async addUser({ commit, state, dispatch }) {
            if (!state.token) return;
            
            commit('SET_LOADING', true);
            
            try {
                const response = await apiFetch('/api/auth/register', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(state.newUser)
                });
                
                if (response.ok) {
                    commit('SET_SHOW_ADD_USER_MODAL', false);
                    commit('RESET_NEW_USER');
                    await dispatch('loadUsers');
                } else {
                    const data = await response.json();
                    commit('SET_ERROR', data.message || 'Failed to add user');
                }
            } catch (error) {
                commit('SET_ERROR', 'Network error. Please try again.');
            } finally {
                commit('SET_LOADING', false);
            }
        },
        
        async deleteUser({ commit, state, dispatch }, userId) {
            if (!state.token) return;
            
            if (!confirm('Are you sure you want to delete this user?')) return;
            
            try {
                const response = await apiFetch(`/api/auth/users/${userId}`, {
                    method: 'DELETE'
                });
                
                if (response.ok) {
                    await dispatch('loadUsers');
                } else {
                    const data = await response.json();
                    commit('SET_ERROR', data.message || 'Failed to delete user');
                }
            } catch (error) {
                commit('SET_ERROR', 'Network error. Please try again.');
            }
        },
        
        showSection({ commit, dispatch }, section) {
            commit('SET_CURRENT_SECTION', section);
            
            // Load data based on section
            switch (section) {
                case 'dashboard':
                    dispatch('loadDashboard');
                    break;
                case 'records':
                    dispatch('loadRecords');
                    break;
                case 'cache':
                    dispatch('loadCache');
                    break;
                case 'users':
                    dispatch('loadUsers');
                    break;
                case 'settings':
                    dispatch('loadSettings');
                    break;
            }
        }
    }
});

// Vue App
const app = Vue.createApp({
    computed: {
        ...Vuex.mapState([
            'token', 'currentUser', 'loading', 'error', 'currentSection',
            'stats', 'records', 'cacheRecords', 'users', 'settings', 'version',
            'domainUpstreamMappings',
            'showAddRecordModal', 'showEditRecordModal', 'showAddUserModal',
            'showAddDomainMappingModal', 'showEditDomainMappingModal',
            'loginForm', 'newRecord', 'editingRecord', 'newUser',
            'newDomainMapping', 'editingDomainMapping'
        ]),
        
        ...Vuex.mapGetters(['isAuthenticated']),
        
        upstreamServersText: {
            get() {
                return this.settings.upstreamServers.join('\n');
            },
            set(value) {
                this.$store.commit('UPDATE_UPSTREAM_SERVERS_TEXT', value);
            }
        }
    },
    
    methods: {
        ...Vuex.mapActions([
            'login', 'logout', 'loadDashboard', 'loadRecords', 'loadCache',
            'loadUsers', 'loadSettings', 'loadVersion', 'addRecord', 'deleteRecord', 'updateRecord',
            'clearExpiredCache', 'clearAllCache', 'saveSettings',
            'addDomainMapping', 'updateDomainMapping', 'deleteDomainMapping',
            'addUser', 'deleteUser', 'showSection'
        ]),
        
        formatDate(dateString) {
            return new Date(dateString).toLocaleString();
        },
        
        isExpired(dateString) {
            return new Date(dateString) < new Date();
        },
        
        editRecord(record) {
            this.$store.commit('SET_EDITING_RECORD', record);
            this.$store.commit('SET_SHOW_EDIT_RECORD_MODAL', true);
        },
        
        submitAddRecord() {
            // Clear any previous errors
            this.$store.commit('SET_ERROR', null);
            
            // Validate form
            const form = document.getElementById('addRecordForm');
            if (form && form.checkValidity()) {
                this.addRecord();
            } else {
                // Trigger browser validation
                form.reportValidity();
            }
        },
        
        openAddRecordModal() {
            this.$store.commit('SET_ERROR', null);
            this.$store.commit('SET_SHOW_ADD_RECORD_MODAL', true);
            this.$store.commit('RESET_NEW_RECORD');
        },
        
        openAddUserModal() {
            this.$store.commit('SET_SHOW_ADD_USER_MODAL', true);
            this.$store.commit('RESET_NEW_USER');
        },
        
        closeAddRecordModal() {
            this.$store.commit('SET_SHOW_ADD_RECORD_MODAL', false);
            this.$store.commit('RESET_NEW_RECORD');
        },
        
        closeEditRecordModal() {
            this.$store.commit('SET_SHOW_EDIT_RECORD_MODAL', false);
            this.$store.commit('RESET_EDITING_RECORD');
        },
        
        submitEditRecord() {
            const form = document.getElementById('editRecordForm');
            if (form && form.checkValidity()) {
                this.updateRecord();
            } else {
                form.reportValidity();
            }
        },
        
        openAddDomainMappingModal() {
            this.$store.commit('SET_ERROR', null);
            this.$store.commit('RESET_NEW_DOMAIN_MAPPING');
            this.$store.commit('SET_SHOW_ADD_DOMAIN_MAPPING_MODAL', true);
        },
        
        closeAddDomainMappingModal() {
            this.$store.commit('SET_SHOW_ADD_DOMAIN_MAPPING_MODAL', false);
            this.$store.commit('RESET_NEW_DOMAIN_MAPPING');
        },
        
        editDomainMapping(mapping) {
            this.$store.commit('SET_EDITING_DOMAIN_MAPPING', { id: mapping.id, domain: mapping.domain, upstreamServer: mapping.upstreamServer });
            this.$store.commit('SET_SHOW_EDIT_DOMAIN_MAPPING_MODAL', true);
        },
        
        closeEditDomainMappingModal() {
            this.$store.commit('SET_SHOW_EDIT_DOMAIN_MAPPING_MODAL', false);
            this.$store.commit('RESET_EDITING_DOMAIN_MAPPING');
        },
        
        submitAddDomainMapping() {
            this.$store.commit('SET_ERROR', null);
            if (this.newDomainMapping.domain && this.newDomainMapping.upstreamServer) this.addDomainMapping();
        },
        
        submitEditDomainMapping() {
            this.$store.commit('SET_ERROR', null);
            if (this.editingDomainMapping.domain && this.editingDomainMapping.upstreamServer) this.updateDomainMapping();
        }
    },
    
    mounted() {
        // Load version info
        this.loadVersion();
        
        // Auto-load dashboard if authenticated
        if (this.isAuthenticated) {
            this.loadDashboard();
        }
    }
});

// Use Vuex
app.use(store);

// Set store instance for apiFetch to use
setStoreInstance(store);

// Mount the app
app.mount('#app');
