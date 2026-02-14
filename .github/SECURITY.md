# Security Policy

## Supported Versions

The following versions of **ThisCloud.Framework** are currently supported with security updates:

| Version | Supported          |
| ------- | ------------------ |
| 1.x     | :white_check_mark: |
| < 1.0   | :x:                |

We recommend always using the latest stable release to ensure you have the most recent security patches and features.

## Reporting a Vulnerability

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

- **Acknowledgment**: You will receive a response within **48 hours** confirming receipt
- **Assessment**: We will investigate and validate the reported vulnerability
- **Resolution**: If confirmed, we will develop and release a patch as soon as possible
- **Disclosure**: Coordinated disclosure timeline will be agreed upon with the reporter

## Responsible Disclosure Policy

We follow a **responsible disclosure process**:

1. **Private reporting**: Report vulnerabilities privately via email (not public issues)
2. **Coordinated timeline**: We will work with you on a reasonable disclosure timeline
3. **Credit**: Security researchers will be credited in release notes (unless anonymity is requested)
4. **No legal action**: We will not pursue legal action against researchers who follow this policy

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