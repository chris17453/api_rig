# OpenAPI/Swagger Import Feature

This document describes the OpenAPI and Swagger import capabilities added to PostmanClone.

## Overview

PostmanClone now supports importing API specifications in the following formats:

- **OpenAPI 3.0.x** (including 3.1.x)
- **Swagger 2.0**
- **Postman Collections v2.0**
- **Postman Collections v2.1**

The import system automatically detects the format and uses the appropriate parser.

## Supported Features

### OpenAPI 3.0 Support

- ✅ Basic info (title, description, version)
- ✅ Server URLs (converted to base_url variable)
- ✅ Path parameters (e.g., `/users/{userId}`)
- ✅ Query parameters with defaults
- ✅ Header parameters
- ✅ Request bodies (JSON, form-data, url-encoded)
- ✅ Authentication schemes:
  - HTTP Basic
  - HTTP Bearer
  - API Key (header or query)
  - OAuth2 Client Credentials
- ✅ Tags (converted to folders)
- ✅ Operation IDs and summaries
- ✅ Schema examples for request bodies

### Swagger 2.0 Support

- ✅ Basic info (title, description, version)
- ✅ Host, basePath, and schemes (converted to base_url)
- ✅ Path parameters
- ✅ Query parameters with defaults
- ✅ Header parameters
- ✅ Body parameters (JSON)
- ✅ Form data parameters (including file uploads)
- ✅ Authentication schemes:
  - Basic
  - API Key (header or query)
  - OAuth2
- ✅ Tags (converted to folders)
- ✅ Operation IDs and summaries
- ✅ Schema examples and x-example extension

## How to Use

### Importing via UI

1. Open PostmanClone application
2. Click "Import" button
3. Select an OpenAPI or Swagger JSON file
4. The system will automatically detect the format
5. Review the imported collection
6. Click "Import" to add it to your workspace

### Importing via Code

```csharp
using PostmanClone.Core.Interfaces;
using PostmanClone.Data.Repositories;

// The repository automatically detects the format
var repository = new collection_repository(dbContext);
var collection = await repository.import_from_file_async(
    "path/to/openapi.json", 
    CancellationToken.None
);
```

### Using Parsers Directly

```csharp
using PostmanClone.Data.Parsers;

// OpenAPI 3.0
var openapi_parser = new openapi_v3_parser();
if (openapi_parser.can_parse(json_content))
{
    var collection = openapi_parser.parse(json_content);
}

// Swagger 2.0
var swagger_parser = new swagger_v2_parser();
if (swagger_parser.can_parse(json_content))
{
    var collection = swagger_parser.parse(json_content);
}
```

## Sample Files

Sample API specifications are provided in the `samples/` directory:

- `petstore_openapi_v3.json` - OpenAPI 3.0 Pet Store example
- `simple_swagger_v2.json` - Swagger 2.0 Simple API example
- `sample_collection.json` - Postman Collection v2.1 example

## Variable Substitution

The parsers create environment variables for:

- **base_url**: Extracted from servers (OpenAPI 3.0) or host/basePath (Swagger 2.0)
- **Authentication placeholders**: 
  - `{{username}}` and `{{password}}` for Basic auth
  - `{{bearer_token}}` for Bearer auth
  - `{{api_key}}` for API Key auth
  - `{{token_url}}`, `{{client_id}}`, `{{client_secret}}` for OAuth2

Path parameters are converted to PostmanClone variable syntax:
- OpenAPI: `/users/{userId}` → `/users/{{userId}}`

## Folder Organization

Requests are automatically organized into folders based on tags.

## Testing

The parsers are thoroughly tested with 28 unit tests:

```bash
# Run OpenAPI 3.0 parser tests
dotnet test --filter "openapi_v3_parser_tests"

# Run Swagger 2.0 parser tests
dotnet test --filter "swagger_v2_parser_tests"

# Run all parser tests
dotnet test tests/PostmanClone.Data.Tests
```

## License

This feature is part of PostmanClone and follows the same MIT license.
