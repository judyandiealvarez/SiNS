# Web Interface Guide

This guide covers using the DNS Server web interface for managing DNS records, cache, users, and server configuration.

## Table of Contents

- [Getting Started](#getting-started)
- [Authentication](#authentication)
- [Dashboard](#dashboard)
- [DNS Records Management](#dns-records-management)
- [Cache Management](#cache-management)
- [User Management](#user-management)
- [Settings](#settings)
- [DNSSEC (REST API)](#dnssec-rest-api)
- [Navigation](#navigation)
- [Troubleshooting](#troubleshooting)

## Getting Started

### Accessing the Web Interface

1. **Open your web browser**
2. **Navigate to**: `http://localhost`
3. **Login** with your credentials

### Default Credentials

- **Username**: `admin`
- **Password**: `admin123`

**Important**: Change these default credentials immediately after first login.

## Authentication

### Login Process

1. **Enter your username and password**
2. **Click "Login"**
3. **Authentication provider** is resolved from `GET /api/auth/provider`
4. **JWT token** is automatically stored in browser
5. **Session persists** until logout or token expiration

### Authentication Providers

- **Embedded mode**:
  - UI submits credentials to `/connect/token`
  - SINS issues and validates access tokens
- **Keycloak mode**:
  - UI submits credentials to `/api/auth/keycloak/login`
  - SINS exchanges credentials with Keycloak and validates Keycloak JWTs
  - Logout uses `/api/auth/keycloak/logout`

### Logout

- **Click the logout button** in the top-right corner
- **Session is cleared** and you're redirected to login page

### Session Management

- **Token expiration**: 24 hours (configurable)
- **Auto-refresh**: Token is automatically refreshed
- **Security**: Token is stored securely in browser

## Dashboard

The dashboard provides an overview of your DNS server status and performance metrics.

### Key Metrics

#### DNS Records
- **Total Records**: Number of active DNS records
- **Record Types**: Breakdown by A, AAAA, CNAME, etc.
- **Recent Activity**: Recently added/modified records

#### Cache Performance
- **Total Cache**: Number of cached DNS responses
- **Cache Hit Rate**: Percentage of queries served from cache
- **Expired Cache**: Number of expired cache entries

#### System Status
- **Server Health**: Overall system health status
- **Response Time**: Average DNS query response time
- **Error Rate**: Percentage of failed queries

### Real-time Updates

- **Auto-refresh**: Dashboard updates every 30 seconds
- **Live counters**: Metrics update in real-time
- **Status indicators**: Visual health status indicators

## DNS Records Management

### Viewing DNS Records

1. **Navigate to "DNS Records"** in the sidebar
2. **View all active records** in a table format
3. **Filter records** by type or search by name
4. **Sort records** by any column

### Record Information

Each DNS record displays:
- **Name**: Domain name
- **Type**: Record type (A, AAAA, CNAME, etc.)
- **Value**: Record value (IP address, domain, etc.)
- **TTL**: Time to live in seconds
- **Status**: Active/Inactive
- **Created**: Creation date
- **Updated**: Last modification date

### Adding DNS Records

#### Step 1: Open Add Record Modal
1. **Click "Add Record"** button
2. **Modal opens** with form fields

#### Step 2: Fill Record Details
- **Name**: Domain name (e.g., `example.com`)
- **Type**: Record type from dropdown
- **Value**: Record value based on type
- **TTL**: Time to live (default: 3600 seconds)

#### Step 3: Submit Record
1. **Click "Add Record"** to save
2. **Record appears** in the list immediately
3. **DNS server** is updated automatically

### Supported Record Types

#### A Record
- **Purpose**: IPv4 address mapping
- **Value Format**: IPv4 address (e.g., `192.168.1.100`)
- **Example**: `example.com` → `192.168.1.100`

#### AAAA Record
- **Purpose**: IPv6 address mapping
- **Value Format**: IPv6 address (e.g., `2001:db8::1`)
- **Example**: `example.com` → `2001:db8::1`

#### CNAME Record
- **Purpose**: Canonical name alias
- **Value Format**: Domain name (e.g., `www.example.com`)
- **Example**: `www.example.com` → `example.com`

#### MX Record
- **Purpose**: Mail exchange server
- **Value Format**: Domain name with priority (e.g., `10 mail.example.com`)
- **Example**: `example.com` → `10 mail.example.com`

#### NS Record
- **Purpose**: Name server delegation
- **Value Format**: Domain name (e.g., `ns1.example.com`)
- **Example**: `example.com` → `ns1.example.com`

#### TXT Record
- **Purpose**: Text information
- **Value Format**: Text string (e.g., `"v=spf1 include:_spf.google.com ~all"`)
- **Example**: `example.com` → `"v=spf1 include:_spf.google.com ~all"`

### Editing DNS Records

#### Step 1: Open Edit Modal
1. **Click "Edit"** button on any record
2. **Modal opens** with current values

#### Step 2: Modify Record
- **Update any field** as needed
- **Validation** occurs in real-time
- **Error messages** appear for invalid data

#### Step 3: Save Changes
1. **Click "Update Record"** to save
2. **Changes applied** immediately
3. **DNS server** updated automatically

### Deleting DNS Records

#### Step 1: Confirm Deletion
1. **Click "Delete"** button on record
2. **Confirmation dialog** appears

#### Step 2: Confirm Action
1. **Click "Delete"** to confirm
2. **Record is removed** from database
3. **DNS server** updated immediately

### Bulk Operations

#### Selecting Multiple Records
- **Checkbox selection**: Select individual records
- **Select All**: Select all visible records
- **Bulk actions**: Apply operations to multiple records

#### Available Bulk Actions
- **Delete**: Remove multiple records
- **Enable/Disable**: Change status of multiple records
- **Export**: Export selected records

## Cache Management

### Viewing Cache

1. **Navigate to "Cache"** in the sidebar
2. **View all cached responses** in table format
3. **Filter by domain** or expiration status
4. **Sort by any column**

### Cache Information

Each cache entry displays:
- **Domain**: Cached domain name
- **Type**: Record type
- **Resolved IPs**: IP addresses resolved
- **Upstream Server**: Server that provided the response
- **Created**: When the cache entry was created
- **Expires**: When the cache entry expires
- **Status**: Active/Expired

### Cache Actions

#### Clear All Cache
1. **Click "Clear All Cache"** button
2. **Confirmation dialog** appears
3. **All cache entries** are removed
4. **DNS server** starts fresh

#### Clear Expired Cache
1. **Click "Clear Expired Cache"** button
2. **Only expired entries** are removed
3. **Active cache** remains intact

### Cache Details

#### Viewing Cache Details
1. **Click on any cache entry**
2. **Detailed view** shows:
   - Full DNS response data
   - Response time information
   - Query metadata

#### Cache Performance
- **Hit Rate**: Percentage of queries served from cache
- **Miss Rate**: Percentage of queries requiring upstream lookup
- **Average Response Time**: Performance metrics

## User Management

### Viewing Users

1. **Navigate to "Users"** in the sidebar
2. **View all users** in table format
3. **See user details** including role and status

### User Information

Each user displays:
- **Username**: Login username
- **Email**: User email address
- **Role**: Admin or User
- **Created**: Account creation date
- **Status**: Active/Inactive

### Adding Users

#### Step 1: Open Add User Modal
1. **Click "Add User"** button
2. **Modal opens** with form fields

#### Step 2: Fill User Details
- **Username**: Unique username
- **Email**: Valid email address
- **Password**: Secure password
- **Role**: Admin or User role

#### Step 3: Create User
1. **Click "Add User"** to save
2. **User account** is created immediately
3. **User can login** with new credentials in embedded mode

In Keycloak mode, users created in SINS are not automatically provisioned in Keycloak.

### User Roles

#### Admin Role
- **Full access** to all features
- **Can manage** DNS records
- **Can manage** users
- **Can configure** server settings
- **Can manage** cache

#### User Role
- **Read-only access** to DNS records
- **Read-only access** to cache
- **Can view** statistics
- **Cannot modify** configuration

### Editing Users

#### Step 1: Open Edit Modal
1. **Click "Edit"** button on user
2. **Modal opens** with current values

#### Step 2: Modify User
- **Update email** or role
- **Change password** (optional)
- **Enable/disable** account

#### Step 3: Save Changes
1. **Click "Update User"** to save
2. **Changes applied** immediately

### Deleting Users

#### Step 1: Confirm Deletion
1. **Click "Delete"** button on user
2. **Confirmation dialog** appears

#### Step 2: Confirm Action
1. **Click "Delete"** to confirm
2. **User account** is removed
3. **Active sessions** are terminated

## Settings

### Server Configuration

1. **Navigate to "Settings"** in the sidebar
2. **View current configuration** values
3. **Modify settings** as needed

### Available Settings

#### Cache Timeout
- **Purpose**: How long to cache DNS responses
- **Default**: 60 minutes
- **Range**: 1-1440 minutes (1 day)
- **Impact**: Affects cache expiration

#### Upstream DNS Servers
- **Purpose**: Fallback DNS servers
- **Default**: `8.8.8.8`, `1.1.1.1`
- **Format**: Comma-separated IP addresses
- **Order**: Tried in sequence

#### DNS Ports
- **UDP Port**: DNS UDP port (default: 53)
- **TCP Port**: DNS TCP port (default: 53)
- **Note**: Changes require server restart

### Saving Configuration

#### Step 1: Modify Settings
1. **Change any setting** values
2. **Validation** occurs in real-time
3. **Error messages** appear for invalid data

#### Step 2: Save Changes
1. **Click "Save Settings"** button
2. **Configuration** is saved to database
3. **DNS server** reloads configuration
4. **Changes take effect** immediately

### Configuration Validation

#### Automatic Validation
- **Port ranges**: Valid port numbers
- **IP addresses**: Valid IPv4/IPv6 format
- **Timeout values**: Reasonable ranges
- **Required fields**: All required fields filled

#### Error Handling
- **Validation errors**: Displayed immediately
- **Save prevention**: Invalid settings cannot be saved
- **Error messages**: Clear explanation of issues

## DNSSEC (REST API)

**Authoritative DNSSEC** (zone keys, enable/disable, DS export for the parent zone) is configured through the **`/api/dnssec/*`** endpoints, not through a dedicated screen in the web UI. Use the API (curl, Postman, or your own tooling) as described in **[DNSSEC](dnssec.md)** and the **[API reference — DNSSEC](api-reference.md#dnssec-zone-endpoints)** section.

## Navigation

### Sidebar Navigation

#### Main Sections
- **Dashboard**: Overview and statistics
- **DNS Records**: Manage DNS records
- **Cache**: View and manage cache
- **Users**: Manage user accounts
- **Settings**: Server configuration

#### User Information
- **Current User**: Display logged-in user
- **Role**: Show user role (Admin/User)
- **Logout**: Sign out of the system

### Responsive Design

#### Desktop View
- **Full sidebar**: All navigation options visible
- **Large tables**: Complete data display
- **Modal dialogs**: Full-size forms

#### Mobile View
- **Collapsible sidebar**: Hamburger menu
- **Responsive tables**: Scrollable data
- **Touch-friendly**: Optimized for touch input

### Keyboard Shortcuts

#### Navigation
- **Ctrl+1**: Dashboard
- **Ctrl+2**: DNS Records
- **Ctrl+3**: Cache
- **Ctrl+4**: Users
- **Ctrl+5**: Settings

#### Actions
- **Ctrl+N**: Add new record
- **Ctrl+S**: Save changes
- **Ctrl+F**: Search/filter
- **Escape**: Close modal

## Troubleshooting

### Common Issues

#### Login Problems
- **Check credentials**: Verify username/password
- **Clear browser cache**: Remove stored data
- **Check server status**: Ensure DNS server is running

#### Page Not Loading
- **Check URL**: Verify correct address
- **Server status**: Ensure services are running
- **Network connectivity**: Check network connection

#### Data Not Updating
- **Refresh page**: Reload the page
- **Check permissions**: Verify user role
- **Server logs**: Check for errors

#### Form Validation Errors
- **Required fields**: Fill all required fields
- **Data format**: Check input format
- **Field limits**: Respect character limits

### Error Messages

#### Authentication Errors
- **"Invalid credentials"**: Wrong username/password
- **"Token expired"**: Session expired, re-login required
- **"Access denied"**: Insufficient permissions

#### Validation Errors
- **"Invalid domain name"**: Check domain format
- **"Invalid IP address"**: Check IP format
- **"TTL out of range"**: TTL value too high/low

#### Server Errors
- **"Service unavailable"**: Server temporarily down
- **"Database error"**: Database connection issue
- **"Configuration error"**: Invalid server configuration

### Getting Help

#### Self-Service
- **Documentation**: Check this guide
- **Error messages**: Read error details
- **Browser console**: Check for JavaScript errors

#### Support
- **Logs**: Check server logs for details
- **API testing**: Test API endpoints directly
- **Community**: Check project documentation

### Performance Tips

#### Browser Optimization
- **Clear cache**: Regular browser cache clearing
- **Disable extensions**: Test without browser extensions
- **Update browser**: Use latest browser version

#### Network Optimization
- **Stable connection**: Ensure reliable network
- **DNS resolution**: Use reliable DNS servers
- **Firewall settings**: Allow necessary ports

#### Server Optimization
- **Resource monitoring**: Check server resources
- **Database optimization**: Optimize database queries
- **Cache tuning**: Adjust cache settings
