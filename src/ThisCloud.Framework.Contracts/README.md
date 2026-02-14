# ThisCloud.Framework.Contracts

**Standardized HTTP contracts and models for ASP.NET Core Minimal APIs.**

## What is this?

`ThisCloud.Framework.Contracts` provides common HTTP response contracts, error models, and API standards to ensure consistency across all Minimal API endpoints in your application.

## Installation

```bash
dotnet add package ThisCloud.Framework.Contracts
```

## Quick Start

```csharp
using ThisCloud.Framework.Contracts;

// Use standardized response models
var response = new ApiResponse<CustomerDto>
{
    Success = true,
    Data = new CustomerDto { Id = 1, Name = "Acme Corp" }
};

// Use standardized error contracts
var errorResponse = new ErrorResponse
{
    Error = "VALIDATION_FAILED",
    Message = "Invalid customer ID"
};
```

## Features

- ✅ **Standardized contracts** - Consistent response models across all APIs
- ✅ **Error models** - Unified error handling with structured error responses
- ✅ **Correlation support** - Built-in correlation ID contracts for distributed tracing
- ✅ **Minimal API optimized** - Designed for .NET Minimal APIs
- ✅ **NuGet ready** - Published and versioned for easy dependency management

## Documentation

For full documentation, architecture, and usage examples:
- [Repository](https://github.com/mdesantis1984/ThisCloud.Framework)
- [Web Framework Docs](https://github.com/mdesantis1984/ThisCloud.Framework/tree/main/docs/web)

## License

ISC License - see [LICENSE](https://github.com/mdesantis1984/ThisCloud.Framework/blob/main/LICENSE) for details.

## Support

This is an open-source project maintained on a best-effort basis. For issues or contributions, visit the [GitHub repository](https://github.com/mdesantis1984/ThisCloud.Framework).
