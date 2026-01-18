# CQRS Implementation Flow - Visual Guide

## Quick Reference: Implementation Order

```
┌─────────────────────────────────────────────────────────────┐
│                    IMPLEMENTATION ORDER                      │
└─────────────────────────────────────────────────────────────┘

1️⃣  DOMAIN LAYER (Start Here - No Dependencies)
    ├─ Entity Class (Apartment.cs)
    ├─ Value Objects (Name, Description, Address, etc.)
    ├─ Domain Errors (ApartmentErrors.cs)
    └─ Repository Interface (IApartmentRepository.cs)

2️⃣  APPLICATION LAYER (Use Cases)
    ├─ Command/Query Records
    │   ├─ CreateApartmentCommand.cs (Write)
    │   └─ SearchApartmentsQuery.cs (Read)
    │
    ├─ Handlers
    │   ├─ CreateApartmentCommandHandler.cs
    │   └─ SearchApartmentsQueryHandler.cs
    │
    ├─ Response DTOs (for Queries)
    │   ├─ ApartmentResponse.cs
    │   └─ AddressResponse.cs
    │
    └─ Validators (for Commands)
        └─ CreateApartmentCommandValidator.cs

3️⃣  INFRASTRUCTURE LAYER (Data Access)
    ├─ Repository Implementation
    │   └─ ApartmentRepository.cs
    │
    └─ EF Core Configuration
        └─ ApartmentConfiguration.cs

4️⃣  API LAYER (Presentation)
    └─ Controller
        └─ ApartmentsController.cs

5️⃣  DEPENDENCY INJECTION
    ├─ Application/DependencyInjection.cs
    └─ Infrastructure/DependencyInjection.cs
```

---

## Command Flow (Write Operation)

```
┌──────────────┐
│ HTTP POST    │
│ /api/...     │
└──────┬───────┘
       │
       ▼
┌─────────────────────────────────┐
│  Controller (API Layer)         │
│  - Receives HTTP request        │
│  - Creates Command              │
│  - Sends via ISender (MediatR)  │
└──────┬──────────────────────────┘
       │
       ▼
┌─────────────────────────────────┐
│  MediatR Pipeline               │
│  1. LoggingBehavior             │
│  2. ValidationBehavior          │
│     └─ Runs FluentValidation    │
└──────┬──────────────────────────┘
       │
       ▼
┌─────────────────────────────────┐
│  CommandHandler (Application)   │
│  - Loads entities via Repository │
│  - Validates business rules     │
│  - Creates/Modifies domain      │
│  - Calls UnitOfWork.Save()      │
└──────┬──────────────────────────┘
       │
       ▼
┌─────────────────────────────────┐
│  Repository (Infrastructure)    │
│  - EF Core tracks changes       │
└──────┬──────────────────────────┘
       │
       ▼
┌─────────────────────────────────┐
│  UnitOfWork (Infrastructure)    │
│  - Publishes domain events      │
│  - Saves to database            │
└──────┬──────────────────────────┘
       │
       ▼
┌──────────────┐
│ HTTP 201     │
│ Created      │
└──────────────┘
```

---

## Query Flow (Read Operation)

```
┌──────────────┐
│ HTTP GET     │
│ /api/...     │
└──────┬───────┘
       │
       ▼
┌─────────────────────────────────┐
│  Controller (API Layer)         │
│  - Receives HTTP request        │
│  - Creates Query               │
│  - Sends via ISender (MediatR)  │
└──────┬──────────────────────────┘
       │
       ▼
┌─────────────────────────────────┐
│  MediatR Pipeline               │
│  1. LoggingBehavior             │
└──────┬──────────────────────────┘
       │
       ▼
┌─────────────────────────────────┐
│  QueryHandler (Application)     │
│  - Uses ISqlConnectionFactory   │
│  - Executes raw SQL (Dapper)    │
│  - Maps directly to DTOs        │
│  - Bypasses domain entities     │
└──────┬──────────────────────────┘
       │
       ▼
┌─────────────────────────────────┐
│  Database (Direct SQL)          │
│  - Optimized query              │
│  - Returns data                 │
└──────┬──────────────────────────┘
       │
       ▼
┌──────────────┐
│ HTTP 200 OK  │
│ + DTOs       │
└──────────────┘
```

---

## File Structure Example: Apartments Feature

```
Bookify.Domain/
└── Apartments/
    ├── Apartment.cs                    ← Entity
    ├── IApartmentRepository.cs         ← Repository Interface
    ├── ApartmentErrors.cs              ← Domain Errors
    ├── Name.cs                         ← Value Object
    ├── Description.cs                 ← Value Object
    ├── Address.cs                      ← Value Object
    └── Amenity.cs                      ← Value Object

Bookify.Application/
└── Apartments/
    ├── SearchApartments/               ← Query (Read)
    │   ├── SearchApartmentsQuery.cs
    │   ├── SearchApartmentsQueryHandler.cs
    │   ├── ApartmentResponse.cs
    │   └── AddressResponse.cs
    │
    └── CreateApartment/               ← Command (Write) - Example
        ├── CreateApartmentCommand.cs
        ├── CreateApartmentCommandHandler.cs
        └── CreateApartmentCommandValidator.cs

Bookify.Infrastructure/
├── Repositories/
│   └── ApartmentRepository.cs         ← Repository Implementation
│
└── Configurations/
    └── ApartmentConfiguration.cs      ← EF Core Mapping

Bookify.Api/
└── Controllers/
    └── Apartments/
        └── ApartmentsController.cs    ← HTTP Endpoints
```

---

## Key Differences: Commands vs Queries

| Aspect | Command (Write) | Query (Read) |
|--------|----------------|--------------|
| **Purpose** | Modify state | Retrieve data |
| **Returns** | `ICommand<TResponse>` | `IQuery<TResponse>` |
| **Handler** | `ICommandHandler<TCommand, TResponse>` | `IQueryHandler<TQuery, TResponse>` |
| **Data Access** | Repository + EF Core | Raw SQL + Dapper |
| **Uses Domain** | ✅ Yes (entities) | ❌ No (DTOs only) |
| **Validation** | ✅ FluentValidation | ⚠️ Optional |
| **Unit of Work** | ✅ Yes | ❌ No |
| **Performance** | Standard | Optimized |

---

## Dependency Flow (Clean Architecture)

```
API Layer
    ↓ depends on
Application Layer
    ↓ depends on
Domain Layer ← (No dependencies, pure business logic)
    ↑ implemented by
Infrastructure Layer
```

**Rule**: Dependencies point inward. Domain has zero dependencies.

---

## MediatR Pipeline Behaviors

```
Request (Command/Query)
    │
    ▼
┌──────────────────┐
│ LoggingBehavior  │ ← Logs request/response
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│ ValidationBehavior│ ← Validates commands (FluentValidation)
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│ Handler          │ ← Your business logic
└────────┬─────────┘
         │
         ▼
Response (Result<T>)
```

---

## Result Pattern Usage

```csharp
// Success case
return Result.Success(apartmentId);
return apartmentId; // implicit conversion

// Failure case
return Result.Failure<Guid>(ApartmentErrors.NotFound);

// Usage in handler
var result = await _repository.GetByIdAsync(id);
if (result.IsFailure)
{
    return Result.Failure<Guid>(result.Error);
}
return result.Value;
```

---

## Value Object Pattern

```csharp
// Instead of this (primitive obsession):
public string Name { get; set; }

// Use this (value object):
public Name Name { get; private set; }

// Value object definition:
public record Name(string Value)
{
    // Can add validation here
    public Name
    {
        if (string.IsNullOrWhiteSpace(Value))
            throw new ArgumentException("Name cannot be empty");
    }
}
```

---

## Repository Pattern

```csharp
// Domain Layer (Interface)
public interface IApartmentRepository
{
    Task<Apartment?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

// Infrastructure Layer (Implementation)
internal sealed class ApartmentRepository 
    : Repository<Apartment>, IApartmentRepository
{
    public ApartmentRepository(ApplicationDbContext dbContext)
        : base(dbContext) { }
}
```

---

## Quick Decision Tree

```
Need to implement a feature?
│
├─ Is it a READ operation?
│  └─ YES → Create Query + QueryHandler
│     └─ Use raw SQL/Dapper for performance
│
└─ Is it a WRITE operation?
   └─ YES → Create Command + CommandHandler
      ├─ Add Validator (FluentValidation)
      ├─ Use Repository for data access
      └─ Use UnitOfWork to save changes
```

---

## Testing Strategy

```
Domain Layer
├─ Unit tests for entities
├─ Unit tests for value objects
└─ Unit tests for domain logic

Application Layer
├─ Unit tests for handlers (mock repositories)
├─ Integration tests for handlers (real repositories)
└─ Unit tests for validators

Infrastructure Layer
└─ Integration tests for repositories

API Layer
└─ Integration tests for controllers
```

---

This visual guide complements the detailed blueprint document.

