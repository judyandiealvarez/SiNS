# SiNS Architecture Documentation

This document provides a detailed overview of the SiNS (Simple Name Server) architecture, including system design, components, data flow, and technical decisions.

## Table of Contents

- [System Overview](#system-overview)
- [Architecture Components](#architecture-components)
- [Data Flow](#data-flow)
- [Technology Stack](#technology-stack)
- [Network Architecture](#network-architecture)
- [Database Design](#database-design)
- [Security Architecture](#security-architecture)
- [Performance Considerations](#performance-considerations)
- [Scalability](#scalability)
- [Monitoring and Observability](#monitoring-and-observability)

## System Overview

SiNS (Simple Name Server) is a hybrid authoritative and recursive DNS server with web-based management capabilities. It combines the functionality of a traditional DNS server with modern web technologies for easy management and monitoring.

### High-Level Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   DNS Client    │    │   Web Browser   │    │  Upstream DNS   │
│                 │    │                 │    │    Servers      │
└─────────┬───────┘    └─────────┬───────┘    └─────────┬───────┘
          │                      │                      │
          │ UDP/TCP:53           │ HTTP:80              │ UDP:53
          │                      │                      │
    ┌─────▼──────────────────────▼──────────────────────▼─────┐
    │                  SiNS DNS Server                        │
    │  ┌─────────────────┐  ┌─────────────────┐              │
    │  │   DNS Engine    │  │   Web API       │              │
    │  │                 │  │                 │              │
    │  │ • UDP/TCP       │  │ • Authentication│              │
    │  │ • Caching       │  │ • Record Mgmt   │              │
    │  │ • Recursion     │  │ • Statistics    │              │
    │  │ • Response Gen  │  │ • Configuration │              │
    │  └─────────────────┘  └─────────────────┘              │
    └─────────────────────────────────────────────────────────┘
                              │
                              │ Database Connection
                              │
                    ┌─────────▼─────────┐
                    │    PostgreSQL     │
                    │                   │
                    │ • DNS Records     │
                    │ • Cache Records   │
                    │ • Users           │
                    │ • Configuration   │
                    └───────────────────┘
```

## Architecture Components

### 1. DNS Engine

The DNS Engine is the core component responsible for handling DNS queries and responses.

#### Components

- **UDP Listener**: Handles UDP DNS queries on port 53
- **TCP Listener**: Handles TCP DNS queries on port 53
- **Query Parser**: Parses incoming DNS queries
- **Response Generator**: Creates DNS responses
- **Cache Manager**: Manages DNS response caching
- **Recursive Resolver**: Resolves queries from upstream servers

#### Key Features

- **Protocol Support**: Full support for UDP and TCP DNS protocols
- **Message Parsing**: Robust DNS message parsing and validation
- **Response Generation**: Proper DNS response formatting
- **Error Handling**: Comprehensive error handling and logging

### 2. Web API

The Web API provides RESTful endpoints for managing the DNS server.

#### Components

- **Authentication Controller**: Handles user authentication and JWT tokens
- **DNS Controller**: Manages DNS records and cache
- **Configuration Controller**: Handles server configuration
- **Statistics Controller**: Provides system statistics

#### Key Features

- **RESTful Design**: Standard REST API patterns
- **JWT Authentication**: Secure token-based authentication
- **Role-Based Access**: Admin and User role permissions
- **Input Validation**: Comprehensive request validation
- **Error Handling**: Standardized error responses

### 3. Web Interface

The Web Interface provides a modern, responsive user interface for managing the DNS server.

#### Components

- **Vue.js Frontend**: Modern JavaScript framework
- **Vuex State Management**: Centralized state management
- **Bootstrap UI**: Responsive design framework
- **Real-time Updates**: Live data updates

#### Key Features

- **Responsive Design**: Works on desktop and mobile devices
- **Real-time Data**: Live updates without page refresh
- **User-friendly Interface**: Intuitive navigation and controls
- **Role-based UI**: Different interfaces for Admin and User roles

### 4. Database Layer

The Database Layer provides persistent storage for all system data.

#### Components

- **PostgreSQL Database**: Primary data store
- **Entity Framework Core**: Object-relational mapping
- **Database Context**: Database connection and configuration
- **Migrations**: Database schema management

#### Key Features

- **ACID Compliance**: Full transaction support
- **Data Integrity**: Foreign key constraints and validation
- **Performance**: Optimized queries and indexing
- **Backup Support**: Easy backup and restore procedures

## Data Flow

### DNS Query Flow

```
1. DNS Client → DNS Server (UDP/TCP:53)
2. Query Parser → Validates and parses query
3. Cache Check → Look for cached response
4. If Cache Hit → Return cached response
5. If Cache Miss → Check authoritative records
6. If Authoritative → Return authoritative response
7. If Not Authoritative → Query upstream servers
8. Cache Response → Store response in cache
9. Return Response → Send response to client
```

### Web API Flow

```
1. Client Request → Web API (HTTP:80)
2. Authentication → Validate JWT token
3. Authorization → Check user permissions
4. Request Validation → Validate input data
5. Business Logic → Process request
6. Database Operation → Execute database queries
7. Response Generation → Format response
8. Return Response → Send response to client
```

### Configuration Flow

```
1. Admin → Web Interface → Update Configuration
2. Web API → Validate Configuration
3. Database → Store Configuration
4. DNS Server → Reload Configuration
5. Apply Changes → Immediate effect
```

## Technology Stack

### Backend

- **.NET 8**: Modern, cross-platform framework
- **ASP.NET Core**: Web framework for building APIs
- **Entity Framework Core**: Object-relational mapping
- **PostgreSQL**: Relational database
- **JWT**: JSON Web Token authentication
- **BCrypt**: Password hashing

### Frontend

- **Vue.js 3**: Progressive JavaScript framework
- **Vuex**: State management for Vue.js
- **Bootstrap 5**: CSS framework for responsive design
- **Axios**: HTTP client for API communication

### Infrastructure

- **Docker**: Containerization platform
- **Docker Compose**: Multi-container orchestration
- **Linux**: Operating system (Ubuntu/Debian/CentOS)

### Development Tools

- **Git**: Version control
- **Visual Studio Code**: Development environment
- **Postman**: API testing
- **pgAdmin**: Database management

## Network Architecture

### Container Network

```
┌─────────────────────────────────────────────────────────────┐
│                    Docker Network                           │
│                    172.20.0.0/16                           │
│                                                             │
│  ┌─────────────────┐    ┌─────────────────┐                │
│  │   PostgreSQL    │    │   DNS Server    │                │
│  │   172.20.0.2    │    │   172.20.0.3    │                │
│  │   Port 5432     │    │   Port 53,80    │                │
│  └─────────────────┘    └─────────────────┘                │
└─────────────────────────────────────────────────────────────┘
```

### Port Mappings

- **53/UDP**: DNS queries (UDP)
- **53/TCP**: DNS queries (TCP)
- **80/TCP**: Web interface and API
- **5432/TCP**: PostgreSQL (internal)

### Network Security

- **Container Isolation**: Each service runs in its own container
- **Static IP Addressing**: Predictable network configuration
- **Port Binding**: Only necessary ports exposed
- **Internal Communication**: Services communicate via internal network

## Database Design

### Entity Relationship Diagram

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│     Users       │    │   DnsRecords    │    │  CacheRecords   │
│                 │    │                 │    │                 │
│ • id (PK)       │    │ • id (PK)       │    │ • id (PK)       │
│ • username      │    │ • name          │    │ • domain        │
│ • email         │    │ • type          │    │ • type          │
│ • password_hash │    │ • value         │    │ • response      │
│ • role          │    │ • ttl           │    │ • resolved_ips  │
│ • created_at    │    │ • is_active     │    │ • upstream_srv  │
│ • updated_at    │    │ • created_at    │    │ • created_at    │
│                 │    │ • updated_at    │    │ • expires_at    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                │
                                │
                       ┌─────────────────┐
                       │  ServerConfig   │
                       │                 │
                       │ • key (PK)      │
                       │ • value         │
                       │ • updated_at    │
                       │ • updated_by    │
                       └─────────────────┘
```

### Database Schema

#### Users Table
```sql
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role VARCHAR(20) NOT NULL DEFAULT 'User',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

#### DnsRecords Table
```sql
CREATE TABLE dns_records (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    type VARCHAR(10) NOT NULL,
    value TEXT NOT NULL,
    ttl INTEGER NOT NULL DEFAULT 3600,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

#### CacheRecords Table
```sql
CREATE TABLE cache_records (
    id SERIAL PRIMARY KEY,
    domain VARCHAR(255) NOT NULL,
    type VARCHAR(10) NOT NULL,
    response BYTEA NOT NULL,
    resolved_ips TEXT[],
    upstream_server VARCHAR(50),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP NOT NULL
);
```

#### ServerConfig Table
```sql
CREATE TABLE server_config (
    key VARCHAR(100) PRIMARY KEY,
    value TEXT NOT NULL,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_by VARCHAR(50) NOT NULL
);
```

### Indexing Strategy

```sql
-- Performance indexes
CREATE INDEX idx_dns_records_name ON dns_records(name);
CREATE INDEX idx_dns_records_type ON dns_records(type);
CREATE INDEX idx_dns_records_active ON dns_records(is_active);

CREATE INDEX idx_cache_records_domain ON cache_records(domain);
CREATE INDEX idx_cache_records_expires ON cache_records(expires_at);
CREATE INDEX idx_cache_records_domain_type ON cache_records(domain, type);

CREATE INDEX idx_users_username ON users(username);
CREATE INDEX idx_users_email ON users(email);
```

## Security Architecture

### Authentication & Authorization

#### JWT Token Structure
```json
{
  "header": {
    "alg": "HS256",
    "typ": "JWT"
  },
  "payload": {
    "sub": "user_id",
    "username": "admin",
    "role": "Admin",
    "iat": 1640995200,
    "exp": 1640998800
  }
}
```

#### Role-Based Access Control

- **Admin Role**:
  - Full access to all features
  - Can manage DNS records
  - Can manage users
  - Can configure server settings
  - Can manage cache

- **User Role**:
  - Read-only access to DNS records
  - Read-only access to cache
  - Can view statistics
  - Cannot modify configuration

### Data Security

#### Password Security
- **Hashing**: BCrypt with salt
- **Cost Factor**: 12 rounds
- **Storage**: Hashed passwords only

#### Database Security
- **Connection Encryption**: SSL/TLS
- **Access Control**: Role-based permissions
- **Input Validation**: Parameterized queries

#### Network Security
- **Container Isolation**: Network namespace isolation
- **Port Security**: Only necessary ports exposed
- **Static IPs**: Predictable network configuration

## Performance Considerations

### DNS Performance

#### Caching Strategy
- **Cache Duration**: Configurable TTL (default: 60 minutes)
- **Cache Storage**: PostgreSQL with automatic cleanup
- **Cache Hit Rate**: Monitored and optimized

#### Query Processing
- **UDP Optimization**: Efficient UDP packet handling
- **TCP Support**: Large query support via TCP
- **Response Time**: Sub-millisecond for cached responses

### Database Performance

#### Query Optimization
- **Indexing**: Strategic indexes on frequently queried columns
- **Connection Pooling**: Efficient database connection management
- **Query Caching**: Entity Framework query caching

#### Storage Optimization
- **Data Compression**: Efficient storage of DNS responses
- **Cleanup Jobs**: Automatic removal of expired records
- **Partitioning**: Future support for large datasets

### Web Interface Performance

#### Frontend Optimization
- **Vue.js Reactivity**: Efficient DOM updates
- **Lazy Loading**: On-demand component loading
- **Caching**: Browser caching for static assets

#### API Performance
- **Response Caching**: HTTP response caching
- **Pagination**: Efficient data pagination
- **Compression**: GZIP compression for responses

## Scalability

### Horizontal Scaling

#### Load Balancing
- **DNS Load Balancing**: Multiple DNS server instances
- **Web Load Balancing**: Multiple web server instances
- **Database Clustering**: PostgreSQL read replicas

#### Container Orchestration
- **Kubernetes**: Production container orchestration
- **Docker Swarm**: Alternative orchestration platform
- **Service Discovery**: Dynamic service registration

### Vertical Scaling

#### Resource Allocation
- **CPU Scaling**: Multi-core processing support
- **Memory Scaling**: Configurable memory limits
- **Storage Scaling**: Expandable storage volumes

#### Performance Tuning
- **Database Tuning**: PostgreSQL performance optimization
- **Application Tuning**: .NET performance optimization
- **Network Tuning**: Network performance optimization

## Monitoring and Observability

### Health Monitoring

#### Service Health Checks
- **DNS Server**: HTTP health check endpoint
- **PostgreSQL**: Database connectivity check
- **Container Health**: Docker health check integration

#### Performance Metrics
- **DNS Queries**: Query count and response time
- **Cache Performance**: Hit rate and efficiency
- **System Resources**: CPU, memory, and disk usage

### Logging

#### Log Levels
- **Debug**: Detailed debugging information
- **Info**: General operational information
- **Warning**: Potential issues
- **Error**: Error conditions
- **Critical**: Critical system failures

#### Log Structure
```json
{
  "timestamp": "2024-01-01T00:00:00Z",
  "level": "INFO",
  "service": "dns-server",
  "message": "DNS query received",
  "data": {
    "query": "google.com",
    "type": "A",
    "client_ip": "192.168.1.100"
  }
}
```

### Alerting

#### Alert Types
- **Service Down**: Service health check failures
- **High Load**: Resource usage thresholds
- **Error Rate**: High error rate detection
- **Cache Miss**: Low cache hit rate

#### Notification Channels
- **Email**: Email notifications
- **Slack**: Slack integration
- **Webhook**: Custom webhook notifications
- **SMS**: SMS notifications

### Dashboard

#### Real-time Metrics
- **DNS Queries**: Live query count and types
- **Cache Status**: Current cache hit rate
- **System Health**: Service status and resource usage
- **Error Rates**: Error count and types

#### Historical Data
- **Performance Trends**: Historical performance data
- **Usage Patterns**: Query patterns and trends
- **Error Analysis**: Error pattern analysis
- **Capacity Planning**: Resource usage trends

## Deployment Architecture

### Production Deployment

#### Infrastructure
- **Linux Server**: Ubuntu/Debian/CentOS
- **Docker Engine**: Container runtime
- **Docker Compose**: Service orchestration
- **Static IPs**: Predictable network configuration

#### Security
- **Firewall**: UFW/iptables configuration
- **SSL/TLS**: HTTPS encryption
- **Access Control**: Network access restrictions
- **Backup**: Automated backup procedures

### Development Environment

#### Local Development
- **Docker Desktop**: Local container environment
- **Hot Reload**: Development with live reload
- **Debugging**: Integrated debugging support
- **Testing**: Automated testing framework

#### CI/CD Pipeline
- **Source Control**: Git-based version control
- **Build Pipeline**: Automated build process
- **Testing**: Automated testing and validation
- **Deployment**: Automated deployment process

## Future Enhancements

### Planned Features

#### Advanced DNS Features
- **DNSSEC**: DNS Security Extensions
- **DNS over HTTPS**: DoH support
- **DNS over TLS**: DoT support
- **GeoDNS**: Geographic DNS routing

#### Management Features
- **Zone Management**: DNS zone management
- **Bulk Operations**: Bulk record operations
- **API Rate Limiting**: Advanced rate limiting
- **Webhook Integration**: External system integration

#### Monitoring Features
- **Advanced Analytics**: Detailed analytics and reporting
- **Custom Dashboards**: User-defined dashboards
- **Alert Management**: Advanced alerting system
- **Performance Optimization**: Automated performance tuning

### Scalability Roadmap

#### Short Term
- **Performance Optimization**: Query optimization and caching
- **Monitoring Enhancement**: Advanced monitoring capabilities
- **Security Hardening**: Additional security features

#### Long Term
- **Microservices Architecture**: Service decomposition
- **Cloud Native**: Kubernetes deployment
- **Global Distribution**: Multi-region deployment
- **AI/ML Integration**: Intelligent DNS management
