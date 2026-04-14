# API Reference

This document provides a complete reference for the DNS Server API endpoints.

## Table of Contents

- [Authentication](#authentication)
- [Base URL](#base-url)
- [Response Format](#response-format)
- [Error Handling](#error-handling)
- [Authentication Endpoints](#authentication-endpoints)
- [DNS Management Endpoints](#dns-management-endpoints)
- [Cache Management Endpoints](#cache-management-endpoints)
- [Configuration Endpoints](#configuration-endpoints)
- [Statistics Endpoints](#statistics-endpoints)
- [Health Check Endpoints](#health-check-endpoints)

## Authentication

The API supports two authentication providers:

- **Embedded**: SINS serves OAuth2/OIDC endpoints directly (`/connect/token`)
- **Keycloak**: SINS validates Keycloak tokens and proxies password/refresh/revoke calls

Most endpoints require a bearer token except health and provider discovery endpoints.

### Determine Active Provider

**GET** `/api/auth/provider`

```json
{
  "provider": "Embedded"
}
```

```json
{
  "provider": "Keycloak"
}
```

### Embedded OAuth2 Login (Password Grant)

**POST** `/connect/token`

`Content-Type: application/x-www-form-urlencoded`

```bash
curl -X POST http://localhost/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&client_id=sins-spa&scope=openid%20profile%20email%20offline_access%20api&username=admin&password=admin123"
```

### Keycloak Login (Proxy Endpoint)

**POST** `/api/auth/keycloak/login`

```bash
curl -X POST http://localhost/api/auth/keycloak/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'
```

### Use Token in Subsequent Requests

```bash
curl -X GET http://localhost/api/dns/records \
  -H "Authorization: Bearer <access_token>"
```

## Base URL

All API endpoints are relative to the base URL:
```
http://localhost/api
```

## Response Format

### Success Response

```json
{
  "data": "response data",
  "message": "success message",
  "timestamp": "2024-01-01T00:00:00Z"
}
```

### Error Response

```json
{
  "error": "error message",
  "details": "additional error details",
  "timestamp": "2024-01-01T00:00:00Z"
}
```

## Error Handling

### HTTP Status Codes

- `200 OK`: Request successful
- `201 Created`: Resource created successfully
- `400 Bad Request`: Invalid request data
- `401 Unauthorized`: Authentication required
- `403 Forbidden`: Insufficient permissions
- `404 Not Found`: Resource not found
- `500 Internal Server Error`: Server error

### Common Error Responses

```json
// 401 Unauthorized
{
  "error": "Authentication required",
  "details": "Valid JWT token required"
}

// 403 Forbidden
{
  "error": "Insufficient permissions",
  "details": "Admin role required for this operation"
}

// 400 Bad Request
{
  "error": "Invalid request data",
  "details": "Name, type, and value are required"
}
```

## Authentication Endpoints

### Provider Discovery

**GET** `/api/auth/provider`

Returns the active provider used by login flow.

#### Response

```json
{
  "provider": "Embedded"
}
```

### Embedded Token Endpoint

**POST** `/connect/token`

Used only when provider is `Embedded`.

### Keycloak Login Endpoint

**POST** `/api/auth/keycloak/login`

Used only when provider is `Keycloak`.

### Keycloak Refresh Endpoint

**POST** `/api/auth/keycloak/refresh`

Used only when provider is `Keycloak`.

### Keycloak Logout (Revocation) Endpoint

**POST** `/api/auth/keycloak/logout`

Used only when provider is `Keycloak`.

### Register User

**POST** `/api/auth/register`

Register a new user (Admin only).

#### Request Body

```json
{
  "username": "newuser",
  "password": "password123",
  "email": "user@example.com",
  "role": "User"
}
```

#### Response

```json
{
  "id": 2,
  "username": "newuser",
  "email": "user@example.com",
  "role": "User",
  "createdAt": "2024-01-01T00:00:00Z"
}
```

### Get Users

**GET** `/api/auth/users`

Get list of all users (Admin only).

#### Response

```json
[
  {
    "id": 1,
    "username": "admin",
    "email": "admin@example.com",
    "role": "Admin",
    "createdAt": "2024-01-01T00:00:00Z"
  },
  {
    "id": 2,
    "username": "user1",
    "email": "user1@example.com",
    "role": "User",
    "createdAt": "2024-01-01T00:00:00Z"
  }
]
```

## DNS Management Endpoints

### Get DNS Records

**GET** `/api/dns/records`

Get all active DNS records.

#### Query Parameters

- `type` (optional): Filter by record type (A, AAAA, CNAME, MX, etc.)
- `name` (optional): Filter by domain name

#### Response

```json
[
  {
    "id": 1,
    "name": "example.com",
    "type": "A",
    "value": "192.168.1.100",
    "ttl": 3600,
    "isActive": true,
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": "2024-01-01T00:00:00Z"
  }
]
```

#### Example

```bash
curl -X GET http://localhost/api/dns/records \
  -H "Authorization: Bearer <token>"
```

### Get DNS Record

**GET** `/api/dns/records/{id}`

Get a specific DNS record by ID.

#### Response

```json
{
  "id": 1,
  "name": "example.com",
  "type": "A",
  "value": "192.168.1.100",
  "ttl": 3600,
  "isActive": true,
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-01T00:00:00Z"
}
```

### Create DNS Record

**POST** `/api/dns/records`

Create a new DNS record (Admin only).

#### Request Body

```json
{
  "name": "example.com",
  "type": "A",
  "value": "192.168.1.100",
  "ttl": 3600
}
```

#### Response

```json
{
  "id": 2,
  "name": "example.com",
  "type": "A",
  "value": "192.168.1.100",
  "ttl": 3600,
  "isActive": true,
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-01T00:00:00Z"
}
```

### Update DNS Record

**PUT** `/api/dns/records/{id}`

Update an existing DNS record (Admin only).

#### Request Body

```json
{
  "name": "example.com",
  "type": "A",
  "value": "192.168.1.200",
  "ttl": 7200
}
```

#### Response

```json
{
  "id": 1,
  "name": "example.com",
  "type": "A",
  "value": "192.168.1.200",
  "ttl": 7200,
  "isActive": true,
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-01T01:00:00Z"
}
```

### Delete DNS Record

**DELETE** `/api/dns/records/{id}`

Delete a DNS record (Admin only).

#### Response

```json
{
  "message": "DNS record deleted successfully"
}
```

## Cache Management Endpoints

### Get Cache Records

**GET** `/api/dns/cache`

Get all cache records.

#### Query Parameters

- `domain` (optional): Filter by domain name
- `expired` (optional): Filter expired records (true/false)

#### Response

```json
[
  {
    "id": 1,
    "domain": "google.com",
    "type": "A",
    "response": "base64-encoded-dns-response",
    "resolvedIPs": ["142.250.203.206"],
    "upstreamServer": "8.8.8.8",
    "createdAt": "2024-01-01T00:00:00Z",
    "expiresAt": "2024-01-01T01:00:00Z"
  }
]
```

### Clear All Cache

**DELETE** `/api/dns/cache`

Clear all cache records (Admin only).

#### Response

```json
{
  "message": "Cleared 150 cache records"
}
```

### Clear Expired Cache

**DELETE** `/api/dns/cache/expired`

Clear only expired cache records (Admin only).

#### Response

```json
{
  "message": "Cleared 25 expired cache records"
}
```

## Configuration Endpoints

### Get Configuration

**GET** `/api/dns/config`

Get current server configuration.

#### Response

```json
{
  "cacheTimeoutMinutes": 60,
  "upstreamServers": ["8.8.8.8", "1.1.1.1"],
  "udpPort": 53,
  "tcpPort": 53
}
```

### Update Configuration

**POST** `/api/dns/config`

Update server configuration (Admin only).

#### Request Body

```json
{
  "cacheTimeoutMinutes": 120,
  "upstreamServers": ["8.8.8.8", "1.1.1.1", "8.8.4.4"],
  "udpPort": 53,
  "tcpPort": 53
}
```

#### Response

```json
{
  "message": "Configuration updated successfully. Changes will take effect immediately."
}
```

## Statistics Endpoints

### Get Statistics

**GET** `/api/dns/stats`

Get DNS server statistics.

#### Response

```json
{
  "totalRecords": 25,
  "totalCacheRecords": 150,
  "expiredCacheRecords": 25,
  "cacheHitRate": 0.857
}
```

## Health Check Endpoints

### Health Check

**GET** `/api/dns/health`

Check service health (no authentication required).

#### Response

```json
{
  "status": "healthy",
  "timestamp": "2024-01-01T00:00:00Z"
}
```

## Data Types

### DNS Record Types

- `A`: IPv4 address record
- `AAAA`: IPv6 address record
- `CNAME`: Canonical name record
- `MX`: Mail exchange record
- `NS`: Name server record
- `TXT`: Text record
- `PTR`: Pointer record
- `SRV`: Service record

### User Roles

- `Admin`: Full access to all features
- `User`: Read-only access to DNS records and cache

### TTL Values

Common TTL values in seconds:
- `300`: 5 minutes
- `3600`: 1 hour
- `86400`: 24 hours
- `604800`: 1 week

## Rate Limiting

The API implements rate limiting to prevent abuse:

- **Authentication endpoints**: 5 requests per minute
- **DNS management endpoints**: 100 requests per minute
- **Cache management endpoints**: 50 requests per minute
- **Configuration endpoints**: 10 requests per minute

## Pagination

For endpoints that return lists, pagination is supported:

### Query Parameters

- `page`: Page number (default: 1)
- `pageSize`: Items per page (default: 50, max: 100)

### Response Format

```json
{
  "data": [...],
  "pagination": {
    "page": 1,
    "pageSize": 50,
    "totalItems": 150,
    "totalPages": 3,
    "hasNext": true,
    "hasPrevious": false
  }
}
```

## WebSocket Support

For real-time updates, WebSocket connections are available:

### Connection

```
ws://localhost/api/ws
```

### Authentication

Send authentication message after connection:

```json
{
  "type": "auth",
  "token": "your-jwt-token"
}
```

### Message Types

- `dns_query`: New DNS query received
- `cache_update`: Cache record updated
- `config_change`: Configuration changed
- `stats_update`: Statistics updated

## SDK Examples

### JavaScript/Node.js

```javascript
const axios = require('axios');

class DnsServerAPI {
  constructor(baseURL, token) {
    this.client = axios.create({
      baseURL,
      headers: { Authorization: `Bearer ${token}` }
    });
  }

  async getRecords() {
    const response = await this.client.get('/api/dns/records');
    return response.data;
  }

  async createRecord(record) {
    const response = await this.client.post('/api/dns/records', record);
    return response.data;
  }
}

// Usage
const api = new DnsServerAPI('http://localhost', 'your-token');
const records = await api.getRecords();
```

### Python

```python
import requests

class DnsServerAPI:
    def __init__(self, base_url, token):
        self.base_url = base_url
        self.headers = {'Authorization': f'Bearer {token}'}
    
    def get_records(self):
        response = requests.get(
            f'{self.base_url}/api/dns/records',
            headers=self.headers
        )
        return response.json()
    
    def create_record(self, record):
        response = requests.post(
            f'{self.base_url}/api/dns/records',
            json=record,
            headers=self.headers
        )
        return response.json()

# Usage
api = DnsServerAPI('http://localhost', 'your-token')
records = api.get_records()
```

### cURL Examples

```bash
# Login in embedded mode
TOKEN=$(curl -s -X POST http://localhost/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&client_id=sins-spa&scope=openid%20profile%20email%20offline_access%20api&username=admin&password=admin123" | jq -r '.access_token')

# Get records
curl -X GET http://localhost/api/dns/records \
  -H "Authorization: Bearer $TOKEN"

# Create record
curl -X POST http://localhost/api/dns/records \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name": "test.com", "type": "A", "value": "192.168.1.100", "ttl": 3600}'
```

## Error Codes

| Code | Description |
|------|-------------|
| `AUTH_REQUIRED` | Authentication required |
| `INVALID_TOKEN` | Invalid or expired token |
| `INSUFFICIENT_PERMISSIONS` | User lacks required permissions |
| `INVALID_REQUEST` | Invalid request data |
| `RECORD_NOT_FOUND` | DNS record not found |
| `DUPLICATE_RECORD` | Record already exists |
| `INVALID_DOMAIN` | Invalid domain name |
| `INVALID_IP` | Invalid IP address |
| `DATABASE_ERROR` | Database operation failed |
| `SERVICE_UNAVAILABLE` | Service temporarily unavailable |

## Versioning

The API version is included in the URL path. Current version is v1:

```
http://localhost/api/v1/dns/records
```

For backward compatibility, the current endpoints without version prefix will continue to work.

## Deprecation Policy

- Deprecated endpoints will be marked with a `Deprecation` header
- Deprecated endpoints will be supported for at least 6 months
- Migration guides will be provided for breaking changes
