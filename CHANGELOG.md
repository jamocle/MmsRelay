# Changelog

All notable changes to MmsRelay will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Console client application for command-line usage
- Comprehensive documentation suite for rookie developers
- Troubleshooting guide with common issues and solutions
- FAQ with beginner-friendly explanations
- Real-world usage examples
- Contributing guidelines for new developers
- Glossary of technical terms with simple explanations

### Changed
- Improved error messages to be more user-friendly
- Enhanced logging for better debugging experience

### Fixed
- Phone number validation for international formats
- Circuit breaker configuration for better resilience

## [1.0.0] - 2024-01-15

### Added
- Initial release of MmsRelay service
- HTTP API for sending SMS and MMS messages via Twilio
- Clean Architecture implementation with dependency injection
- Comprehensive input validation using FluentValidation
- Circuit breaker pattern for resilience (using Polly)
- Retry logic with exponential backoff
- Health checks for liveness and readiness probes
- Structured logging with Serilog
- Configuration via environment variables and user secrets
- Docker support with multi-stage builds
- Complete unit test suite with high coverage
- API documentation with OpenAPI/Swagger

### Security
- Secure credential management (no secrets in code)
- Input sanitization and validation
- Rate limiting protection
- HTTPS support in production

---

## Version History Details

### What Each Version Number Means

**Semantic Versioning (X.Y.Z):**
- **X (Major)**: Breaking changes that require users to update their code
- **Y (Minor)**: New features that don't break existing functionality  
- **Z (Patch)**: Bug fixes and small improvements

**Examples:**
- `1.0.0` → `1.1.0`: Added new API endpoint (safe to upgrade)
- `1.1.0` → `1.1.1`: Fixed a bug (safe to upgrade)
- `1.1.1` → `2.0.0`: Changed API format (requires code changes)

### Migration Guides

When we release breaking changes, we'll include migration instructions here.

---

## Release Process

### For Contributors

When your changes are merged, they appear in the "Unreleased" section above. When we create a new release:

1. Move items from "Unreleased" to a new version section
2. Add release date
3. Create Git tag
4. Publish release notes on GitHub

### For Users  

**Staying Updated:**
- **Patch releases** (1.0.0 → 1.0.1): Safe to auto-update
- **Minor releases** (1.0.0 → 1.1.0): Review new features, usually safe
- **Major releases** (1.0.0 → 2.0.0): Review breaking changes carefully

**Getting Notifications:**
- Watch this repository on GitHub for release notifications
- Subscribe to release RSS feed: `https://github.com/jamocle/MmsRelay/releases.atom`

---

## Categories Explained

### Added
New features, endpoints, or functionality that didn't exist before.

**Examples:**
- New API endpoints
- Additional message providers
- New configuration options
- Command-line arguments

### Changed  
Modifications to existing functionality that maintain compatibility.

**Examples:**
- Improved error messages
- Performance optimizations  
- Updated dependencies
- Enhanced logging

### Deprecated
Features that still work but will be removed in future versions.

**Examples:**
- Old API endpoint formats
- Configuration options being replaced
- Legacy authentication methods

### Removed
Features that no longer work (breaking changes).

**Examples:**
- Deleted API endpoints
- Removed configuration options  
- Discontinued support for old versions

### Fixed
Bug fixes and corrections.

**Examples:**
- Crash fixes
- Validation errors
- Memory leaks
- Incorrect behavior

### Security
Security-related changes, improvements, or vulnerability fixes.

**Examples:**
- Patched security vulnerabilities
- Improved input validation
- Enhanced authentication
- Updated dependencies with security fixes

---

## Development Releases

### Pre-release Versions

We sometimes release pre-release versions for testing:

- **Alpha** (`1.1.0-alpha.1`): Early testing, might be unstable
- **Beta** (`1.1.0-beta.1`): Feature-complete, final testing
- **Release Candidate** (`1.1.0-rc.1`): Ready for release, final validation

**Using Pre-releases:**
```bash
# Install specific pre-release version
dotnet add package MmsRelay --version 1.1.0-alpha.1

# Get latest pre-release
dotnet add package MmsRelay --prerelease
```

### Nightly Builds

Development versions are automatically built from the main branch:

- **Version**: `1.1.0-dev.20240115.1` (includes date and build number)
- **Use Case**: Testing latest changes, not for production
- **Availability**: GitHub Packages or development NuGet feed

---

## Getting Help with Versions

### Which Version Should I Use?

**For Production:**
- ✅ Latest stable release (no `-alpha`, `-beta` suffixes)
- ✅ LTS (Long Term Support) versions when available
- ❌ Pre-release or development versions

**For Development/Testing:**
- ✅ Latest stable or pre-release versions
- ✅ Specific versions to match your production environment
- ⚠️ Development versions (expect breaking changes)

### Version Compatibility

**Backward Compatibility Promise:**
- **Patch releases**: Always backward compatible
- **Minor releases**: Backward compatible, may add new features
- **Major releases**: May contain breaking changes

**API Version Support:**
- We maintain API compatibility within major versions
- Deprecation warnings provided at least one minor version before removal
- Critical security fixes backported to previous major version

### Troubleshooting Version Issues

**Common Problems:**

1. **"Method not found" errors**
   - You're using a newer API with an older version
   - Check the changelog for when the feature was added
   - Upgrade to the required version

2. **"Configuration option not recognized"**
   - Configuration format changed between versions
   - Check migration guide for your version upgrade
   - Use version-specific configuration format

3. **Breaking changes after upgrade**
   - Check if you crossed a major version boundary (1.x → 2.x)
   - Review "Changed" and "Removed" sections in changelog
   - Follow migration guide for breaking changes

**Getting Version Info:**
```bash
# Check your current version
dotnet --list-packages | grep MmsRelay

# Check service version at runtime
curl http://localhost:5000/health/live
# Returns version info in headers or response

# Check console client version  
dotnet run -- --version
```

---

## Contributing to Changelog

### When Adding Entries

**For Contributors:**
- Add entries to "Unreleased" section when submitting PRs
- Use clear, user-focused descriptions
- Include issue numbers when applicable
- Follow the existing format and categories

**Entry Format:**
```markdown
### Added
- New phone number validation for international formats [#123]
- Support for MMS attachments in console client [#145]

### Fixed  
- Resolved memory leak in Twilio client connection [#134]
- Fixed phone number parsing for UK numbers [#156]
```

**Writing Good Entries:**
- ✅ "Added support for international phone number formats"
- ❌ "Updated PhoneNumberValidator.cs"
- ✅ "Fixed memory leak in long-running services"  
- ❌ "Fixed bug in TwilioMmsSender"

The changelog should help users understand what changed and whether they need to take action, not document internal code changes.