# Contributing to SiNS

Thank you for your interest in contributing to SiNS (Simple Name Server)! This document provides guidelines and information for contributors.

## Table of Contents

- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Code Style and Standards](#code-style-and-standards)
- [Testing](#testing)
- [Pull Request Process](#pull-request-process)
- [Issue Reporting](#issue-reporting)
- [Documentation](#documentation)
- [Release Process](#release-process)
- [Community Guidelines](#community-guidelines)

## Getting Started

### Prerequisites

Before contributing, ensure you have:

- **Git**: Version control system
- **.NET 8.0 SDK**: For building and running the application
- **Docker & Docker Compose**: For containerized development
- **PostgreSQL**: Database (or use Docker)
- **Node.js**: For frontend development (optional)
- **IDE**: Visual Studio, VS Code, or Rider recommended

### Quick Start

1. **Fork the repository** on GitHub
2. **Clone your fork**:
   ```bash
   git clone https://github.com/YOUR_USERNAME/SiNS.git
   cd SiNS
   ```
3. **Add upstream remote**:
   ```bash
   git remote add upstream https://github.com/swipentap/SiNS.git
   ```
4. **Create a feature branch**:
   ```bash
   git checkout -b feature/your-feature-name
   ```

## Development Setup

### Local Development Environment

#### Option 1: Docker Development (Recommended)

```bash
# Build and run with Docker Compose
docker-compose up -d --build

# View logs
docker-compose logs -f dns-server

# Stop services
docker-compose down
```

#### Option 2: Local Development

```bash
# Install dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run --project sins

# Run tests
dotnet test
```

### Database Setup

#### Using Docker PostgreSQL
```bash
# Start PostgreSQL container
docker run -d \
  --name sins-postgres \
  -e POSTGRES_DB=dns_server \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  postgres:15
```

#### Using Local PostgreSQL
```bash
# Create database
createdb dns_server

# Run migrations
dotnet ef database update --project sins
```

### Frontend Development

The web interface is built with Vue.js and is served as static files.

```bash
# Install Node.js dependencies (if modifying frontend)
cd sins/wwwroot
npm install

# Build frontend assets
npm run build
```

## Code Style and Standards

### C# Coding Standards

#### General Guidelines
- Use **PascalCase** for public members, classes, and methods
- Use **camelCase** for private fields and local variables
- Use **UPPER_CASE** for constants
- Prefer **async/await** over Task.ContinueWith
- Use **var** when the type is obvious from the context

#### Naming Conventions
```csharp
// ✅ Good
public class DnsRecordService
{
    private readonly IDnsRepository _repository;
    private const int DefaultTtl = 3600;
    
    public async Task<DnsRecord> GetRecordAsync(string name)
    {
        var record = await _repository.GetByNameAsync(name);
        return record;
    }
}

// ❌ Avoid
public class dnsRecordService
{
    private readonly IDnsRepository repository;
    private const int default_ttl = 3600;
    
    public async Task<DnsRecord> getRecord(string name)
    {
        var record = await repository.getByName(name);
        return record;
    }
}
```

#### File Organization
- One class per file
- Use regions for organizing large classes
- Group related functionality together

```csharp
#region Properties
public string Name { get; set; }
public int Ttl { get; set; }
#endregion

#region Methods
public async Task<bool> ValidateAsync()
{
    // Implementation
}
#endregion
```

### JavaScript/Vue.js Standards

#### Vue.js Guidelines
- Use **kebab-case** for component names in templates
- Use **PascalCase** for component names in JavaScript
- Use **camelCase** for methods and properties

```javascript
// ✅ Good
export default {
  name: 'DnsRecordForm',
  data() {
    return {
      recordName: '',
      recordType: 'A'
    }
  },
  methods: {
    async saveRecord() {
      // Implementation
    }
  }
}
```

### Documentation Standards

#### XML Documentation
```csharp
/// <summary>
/// Retrieves a DNS record by its name.
/// </summary>
/// <param name="name">The domain name to search for.</param>
/// <returns>A DNS record if found; otherwise, null.</returns>
/// <exception cref="ArgumentNullException">Thrown when name is null or empty.</exception>
public async Task<DnsRecord> GetRecordAsync(string name)
{
    // Implementation
}
```

#### README Updates
- Update relevant documentation when adding new features
- Include usage examples for new functionality
- Update API documentation if endpoints change

## Testing

### Unit Testing

#### Test Structure
```csharp
[TestClass]
public class DnsRecordServiceTests
{
    private DnsRecordService _service;
    private Mock<IDnsRepository> _mockRepository;

    [TestInitialize]
    public void Setup()
    {
        _mockRepository = new Mock<IDnsRepository>();
        _service = new DnsRecordService(_mockRepository.Object);
    }

    [TestMethod]
    public async Task GetRecordAsync_ValidName_ReturnsRecord()
    {
        // Arrange
        var expectedRecord = new DnsRecord { Name = "example.com", Type = "A" };
        _mockRepository.Setup(r => r.GetByNameAsync("example.com"))
            .ReturnsAsync(expectedRecord);

        // Act
        var result = await _service.GetRecordAsync("example.com");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("example.com", result.Name);
    }
}
```

#### Running Tests
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test sins.Tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName~DnsRecordServiceTests"
```

### Integration Testing

#### Docker Integration Tests
```bash
# Run integration tests with Docker
docker-compose -f docker-compose.test.yml up --build --abort-on-container-exit
```

### Manual Testing

Use the **host and port** your SiNS instance actually listens on (Docker Compose often maps **53/udp** to localhost; Kubernetes may use **ClusterIP**, **NodePort**, or in-cluster `dig` — see [deploy/rancher-desktop/README.md](deploy/rancher-desktop/README.md) and [DNSSEC](docs/dnssec.md)). For **DNSSEC** APIs and verification, see [API Reference — DNSSEC](docs/api-reference.md#dnssec-zone-endpoints) and [docs/dnssec.md](docs/dnssec.md).

#### DNS Functionality
```bash
# Test DNS resolution (add -p <port> if not 53)
dig @localhost example.com

# Test with nslookup
nslookup example.com localhost

# Test TCP DNS
dig @localhost example.com +tcp
```

#### Web Interface
```bash
# Test web interface
curl http://localhost/api/dns/health

# Test authentication
curl -X POST http://localhost/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'
```

## Pull Request Process

### Before Submitting

1. **Ensure tests pass**:
   ```bash
   dotnet test
   dotnet build --configuration Release
   ```

2. **Check code formatting**:
   ```bash
   dotnet format --verify-no-changes
   ```

3. **Update documentation** if needed

4. **Test your changes** thoroughly

### Pull Request Guidelines

#### Title Format
```
type(scope): brief description

Examples:
feat(dns): add support for CNAME records
fix(web): resolve authentication issue
docs(readme): update installation instructions
test(api): add integration tests for DNS queries
```

#### Description Template
```markdown
## Description
Brief description of the changes.

## Type of Change
- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update

## Testing
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] Manual testing completed
- [ ] Documentation updated

## Checklist
- [ ] My code follows the style guidelines of this project
- [ ] I have performed a self-review of my own code
- [ ] I have commented my code, particularly in hard-to-understand areas
- [ ] I have made corresponding changes to the documentation
- [ ] My changes generate no new warnings
- [ ] I have added tests that prove my fix is effective or that my feature works
- [ ] New and existing unit tests pass locally with my changes
```

### Review Process

1. **Automated Checks**: CI/CD pipeline runs tests and checks
2. **Code Review**: At least one maintainer must approve
3. **Documentation Review**: Ensure documentation is updated
4. **Testing Review**: Verify tests are adequate

## Issue Reporting

### Bug Reports

Use the bug report template:

```markdown
## Bug Description
Clear and concise description of the bug.

## Steps to Reproduce
1. Go to '...'
2. Click on '....'
3. Scroll down to '....'
4. See error

## Expected Behavior
What you expected to happen.

## Actual Behavior
What actually happened.

## Environment
- OS: [e.g. Ubuntu 20.04]
- .NET Version: [e.g. 8.0]
- Docker Version: [e.g. 20.10]
- SiNS Version: [e.g. 1.0.6]

## Additional Context
Add any other context about the problem here.
```

### Feature Requests

Use the feature request template:

```markdown
## Feature Description
Clear and concise description of the feature.

## Problem Statement
What problem does this feature solve?

## Proposed Solution
Describe the solution you'd like to see.

## Alternative Solutions
Describe any alternative solutions you've considered.

## Additional Context
Add any other context or screenshots about the feature request.
```

## Documentation

### Documentation Guidelines

#### Code Comments
- Comment complex algorithms and business logic
- Use XML documentation for public APIs
- Keep comments up-to-date with code changes

#### README Updates
- Update installation instructions for new dependencies
- Add examples for new features
- Update troubleshooting section for common issues

#### API Documentation
- Document all public endpoints
- Include request/response examples
- Document error codes and messages

### Documentation Structure

```
docs/
├── README.md              # Main documentation index
├── installation.md        # Installation guide
├── quick-start.md         # Quick start guide
├── api-reference.md       # API documentation
├── web-interface.md       # Web interface guide
├── architecture.md        # System architecture
├── dockerhub-overview.md  # Docker Hub guide
└── contributing.md        # This file
```

## Release Process

### Versioning

We follow [Semantic Versioning](https://semver.org/):

- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes (backward compatible)

### Release Checklist

1. **Update version numbers**:
   - Update `sins.csproj` version
   - Update Docker image tags
   - Update documentation references

2. **Create release notes**:
   - List new features
   - Document breaking changes
   - Include migration guide if needed

3. **Create Git tag**:
   ```bash
   git tag v1.0.7
   git push origin v1.0.7
   ```

4. **Monitor CI/CD pipeline**:
   - Ensure Docker images are built
   - Verify GitHub release is created
   - Check security scans pass

### Release Notes Template

```markdown
## [1.0.7] - 2025-01-XX

### Added
- New feature A
- New feature B

### Changed
- Updated behavior of feature X

### Fixed
- Bug fix for issue Y
- Performance improvement for Z

### Breaking Changes
- Description of breaking change (if any)

### Migration Guide
- Steps to migrate from previous version (if needed)
```

## Community Guidelines

### Code of Conduct

We are committed to providing a welcoming and inclusive environment for all contributors. Please:

- Be respectful and considerate of others
- Use inclusive language
- Focus on constructive feedback
- Help others learn and grow

### Communication

- **GitHub Issues**: For bug reports and feature requests
- **GitHub Discussions**: For questions and general discussion
- **Pull Requests**: For code contributions

### Recognition

Contributors will be recognized in:
- GitHub contributors list
- Release notes
- Project documentation

### Getting Help

If you need help:

1. Check the documentation first
2. Search existing issues and discussions
3. Create a new issue or discussion
4. Tag maintainers if needed

## License

By contributing to SiNS, you agree that your contributions will be licensed under the [GNU Lesser General Public License v3.0](LICENSE).

---

Thank you for contributing to SiNS! Your contributions help make this project better for everyone.
