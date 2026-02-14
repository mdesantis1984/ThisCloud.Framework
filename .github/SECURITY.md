# Security Policy

## Supported Versions

The following versions of **ThisCloud.Framework** are currently supported with security updates:

| Version | Supported          |
| ------- | ------------------ |
| 1.x     | :white_check_mark: |
| < 1.0   | :x:                |

We recommend always using the latest stable release to ensure you have the most recent security patches and features.

## Reporting a Vulnerability

**This project does not provide commercial support or security SLA.**

If you discover a security vulnerability in this project, please report it responsibly.

**Please do NOT report security vulnerabilities through public GitHub issues.**

### How to Report

Send an email to **[info@thiscloud.com.ar](mailto:info@thiscloud.com.ar)** with the following information:

- **Description of the vulnerability**: Clear explanation of the issue
- **Steps to reproduce**: Detailed reproduction steps
- **Potential impact**: Assessment of severity and scope
- **Affected versions**: Which versions are vulnerable
- **Suggested fix** (optional): If you have recommendations

### Response Process

**Best-Effort Basis**: Security reports are reviewed on a **best-effort basis** as maintainer availability permits.

- **No guaranteed response time**: There is no SLA or guaranteed timeframe for acknowledgment.
- **No guaranteed patch timeline**: If a vulnerability is confirmed, patches will be developed as resources allow.
- **Community-driven**: This is an open-source project maintained by volunteers without formal support obligations.

We appreciate responsible disclosure and will make reasonable efforts to address legitimate security concerns when possible.

## Responsible Disclosure Policy

We follow a **responsible disclosure process** on a best-effort basis:

1. **Private reporting**: Report vulnerabilities privately via email (not public issues)
2. **Reasonable timeline**: We will make reasonable efforts to respond and coordinate disclosure timelines
3. **Credit**: Security researchers will be credited in release notes (unless anonymity is requested)
4. **No legal action**: We will not pursue legal action against researchers who follow this policy

**Note**: As an open-source project without formal support commitments, response and resolution timelines are subject to maintainer availability.

## Security Updates

Security updates will be released as **patch versions** and announced through:

- **GitHub Security Advisories**: [github.com/mdesantis1984/ThisCloud.Framework/security/advisories](https://github.com/mdesantis1984/ThisCloud.Framework/security/advisories)
- **Release Notes**: Detailed changelog in GitHub Releases
- **NuGet Release Notes**: Package-specific security notes

## Best Practices for Consumers

When using ThisCloud.Framework in production:

- ✅ **Keep packages updated** to the latest stable version
- ✅ **Monitor security advisories** via GitHub Watch → Custom → Security Alerts
- ✅ **Enable Dependabot** in your consuming repositories
- ✅ **Review breaking changes** before upgrading major versions
- ✅ **Test in non-production** environments before deploying security patches

## Out of Scope

The following are **not considered security vulnerabilities**:

- Issues requiring physical access to the server
- Social engineering attacks
- Denial of Service (DoS) attacks against public endpoints (rate limiting is the consumer's responsibility)
- Issues in third-party dependencies (report to the respective maintainers)
- Theoretical vulnerabilities without proof of concept

---

**Contact**: [info@thiscloud.com.ar](mailto:info@thiscloud.com.ar)  
**Repository**: [github.com/mdesantis1984/ThisCloud.Framework](https://github.com/mdesantis1984/ThisCloud.Framework)  
**License**: ISC