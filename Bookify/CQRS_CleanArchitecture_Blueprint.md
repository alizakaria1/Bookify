# CQRS + Clean Architecture Blueprint
## Using Apartments Feature as Example

This document outlines the step-by-step blueprint for implementing CQRS (Command Query Responsibility Segregation) with Clean Architecture, based on the Bookify solution structure.

---

## Architecture Layers Overview

```
┌─────────────────────────────────────┐
│   Bookify.Api (Presentation)        │  ← Controllers, HTTP endpoints
├─────────────────────────────────────┤
│   Bookify.Application (Use Cases)   │  ← Commands, Queries, Handlers
├─────────────────────────────────────┤
│   Bookify.Domain (Business Logic)   │  ← Entities, Value Objects, Domain Events
├─────────────────────────────────────┤
│   Bookify.Infrastructure (I/O)      │  ← Repositories, EF Core, External Services
└─────────────────────────────────────┘
```

**Dependency Flow**: Outer layers depend on inner layers. Domain has no dependencies.

---

## Step-by-Step Implementation Blueprint

### **STEP 1: Domain Layer - Define the Core Business Entity**

**Location**: `Bookify.Domain/Apartments/`

#### 1.1 Create the Entity Class
```csharp
// Apartment.cs
public sealed class Apartment : Entity
{
    public Apartment(Guid id, Name name, Description description, 
                     Address address, Money price, Money cleaningFee, 
                     List<Amenity> amenities) : base(id)
    {
        Name = name;
        Description = description;
        Address = address;
        Price = price;
        CleaningFee = cleaningFee;
        Amenities = amenities;
    }

    public Name Name { get; private set; }
    public Description Description { get; private set; }
    public Address Address { get; private set; }
    public Money Price { get; private set; }
    public Money CleaningFee { get; private set; }
    public List<Amenity> Amenities { get; private set; }
}
```

**Key Points**:
- Inherits from `Entity` (base class with `Id` and domain events)
- Uses **Value Objects** (`Name`, `Description`, `Address`, `Money`) instead of primitives
- Properties have `private set` to enforce encapsulation
- No public setters - changes happen through methods

#### 1.2 Create Value Objects
```csharp
// Name.cs
public record Name(string Value);

// Description.cs
public record Description(string Value);

// Address.cs
public record Address(string Country, string State, string ZipCode, string City, string Street);
```

**Why Value Objects?**
- Encapsulate validation logic
- Prevent primitive obsession
- Make code more expressive and type-safe

#### 1.3 Create Domain Errors
```csharp
// ApartmentErrors.cs
public static class ApartmentErrors
{
    public static readonly Error NotFound = new("Apartment.NotFound", "The apartment was not found");
}
```

#### 1.4 Define Repository Interface
```csharp
// IApartmentRepository.cs
public interface IApartmentRepository
{
    Task<Apartment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
```

**Key Points**:
- Interface is in **Domain** layer (dependency inversion)
- Implementation will be in **Infrastructure** layer
- Only define methods needed by domain logic

---

### **STEP 2: Application Layer - Define Commands/Queries**

**Location**: `Bookify.Application/Apartments/`

#### 2.1 For WRITE Operations (Commands)

**Example: CreateApartment (if it existed)**

```csharp
// CreateApartment/CreateApartmentCommand.cs
public sealed record CreateApartmentCommand(
    string Name,
    string Description,
    string Country,
    string State,
    string ZipCode,
    string City,
    string Street,
    decimal PriceAmount,
    string PriceCurrency,
    decimal CleaningFeeAmount,
    string CleaningFeeCurrency,
    List<string> Amenities
) : ICommand<Guid>;
```

**Key Points**:
- Uses `record` for immutability
- Implements `ICommand<TResponse>` where `TResponse` is the return type (e.g., `Guid` for the created ID)
- Contains only primitive types (DTOs from API layer)

#### 2.2 For READ Operations (Queries)

```csharp
// SearchApartments/SearchApartmentsQuery.cs
public sealed record SearchApartmentsQuery(
    DateOnly StartDate, 
    DateOnly EndDate
) : IQuery<IReadOnlyList<ApartmentResponse>>;
```

**Key Points**:
- Implements `IQuery<TResponse>` where `TResponse` is the response DTO
- Queries are read-only, no side effects

#### 2.3 Create Response DTOs (for Queries)

```csharp
// SearchApartments/ApartmentResponse.cs
public sealed class ApartmentResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; }
    public AddressResponse Address { get; set; }
}

// SearchApartments/AddressResponse.cs
public sealed class AddressResponse
{
    public string Country { get; init; }
    public string State { get; init; }
    public string ZipCode { get; init; }
    public string City { get; init; }
    public string Street { get; init; }
}
```

**Key Points**:
- Response DTOs are in Application layer
- They represent the data shape returned to the API
- Can be different from domain entities (CQRS separation)

---

### **STEP 3: Application Layer - Implement Handlers**

#### 3.1 Command Handler (Write Operation)

**Example Structure** (using ReserveBooking as reference):

```csharp
// CreateApartment/CreateApartmentCommandHandler.cs
internal sealed class CreateApartmentCommandHandler 
    : ICommandHandler<CreateApartmentCommand, Guid>
{
    private readonly IApartmentRepository _apartmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateApartmentCommandHandler(
        IApartmentRepository apartmentRepository,
        IUnitOfWork unitOfWork)
    {
        _apartmentRepository = apartmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        CreateApartmentCommand request, 
        CancellationToken cancellationToken)
    {
        // 1. Create value objects from primitives
        var name = new Name(request.Name);
        var description = new Description(request.Description);
        var address = new Address(
            request.Country, 
            request.State, 
            request.ZipCode, 
            request.City, 
            request.Street);
        var price = new Money(request.PriceAmount, Currency.FromCode(request.PriceCurrency));
        var cleaningFee = new Money(request.CleaningFeeAmount, Currency.FromCode(request.CleaningFeeCurrency));
        var amenities = request.Amenities.Select(a => new Amenity(a)).ToList();

        // 2. Create domain entity
        var apartment = new Apartment(
            Guid.NewGuid(),
            name,
            description,
            address,
            price,
            cleaningFee,
            amenities);

        // 3. Add to repository
        _apartmentRepository.Add(apartment);

        // 4. Save changes (Unit of Work pattern)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. Return result
        return apartment.Id;
    }
}
```

**Key Points**:
- Implements `ICommandHandler<TCommand, TResponse>`
- Uses dependency injection for repositories and services
- Returns `Result<T>` for error handling
- Uses Unit of Work pattern to persist changes
- Converts primitives to value objects

#### 3.2 Query Handler (Read Operation)

```csharp
// SearchApartments/SearchApartmentsQueryHandler.cs
internal sealed class SearchApartmentsQueryHandler
    : IQueryHandler<SearchApartmentsQuery, IReadOnlyList<ApartmentResponse>>
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    public SearchApartmentsQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
    }

    public async Task<Result<IReadOnlyList<ApartmentResponse>>> Handle(
        SearchApartmentsQuery request, 
        CancellationToken cancellationToken)
    {
        // 1. Validation
        if (request.StartDate > request.EndDate)
        {
            return new List<ApartmentResponse>();
        }

        // 2. Use raw SQL for read operations (CQRS - optimized queries)
        using IDbConnection connection = _sqlConnectionFactory.CreateConnection();

        const string sql = """
            SELECT
                a.id AS Id,
                a.name AS Name,
                a.description AS Description,
                a.price_amount AS Price,
                a.price_currency AS Currency,
                a.address_country AS Country,
                a.address_state AS State,
                a.address_zip_code AS ZipCode,
                a.address_city AS City,
                a.address_street AS Street
            FROM apartments AS a
            WHERE NOT EXISTS (...)
            """;

        // 3. Map directly to response DTOs (bypass domain entities for reads)
        IEnumerable<ApartmentResponse> apartments = await connection
            .QueryAsync<ApartmentResponse, AddressResponse, ApartmentResponse>(
                sql,
                (apartment, address) =>
                {
                    apartment.Address = address;
                    return apartment;
                },
                new { request.StartDate, request.EndDate },
                splitOn: "Country");

        return apartments.ToList();
    }
}
```

**Key Points**:
- Implements `IQueryHandler<TQuery, TResponse>`
- For **read operations**, can use raw SQL/Dapper (bypass EF Core)
- Directly maps to response DTOs (no domain entities needed)
- Optimized for performance (CQRS benefit)

#### 3.3 Command Validation (Optional but Recommended)

```csharp
// CreateApartment/CreateApartmentCommandValidator.cs
internal sealed class CreateApartmentCommandValidator 
    : AbstractValidator<CreateApartmentCommand>
{
    public CreateApartmentCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).NotEmpty().MaximumLength(2000);
        RuleFor(c => c.PriceAmount).GreaterThan(0);
        RuleFor(c => c.PriceCurrency).NotEmpty();
        // ... more rules
    }
}
```

**Key Points**:
- Uses FluentValidation
- Automatically executed by `ValidationBehavior` pipeline
- Validates input before handler executes

---

### **STEP 4: Infrastructure Layer - Implement Repository**

**Location**: `Bookify.Infrastructure/Repositories/`

```csharp
// ApartmentRepository.cs
internal sealed class ApartmentRepository : Repository<Apartment>, IApartmentRepository
{
    public ApartmentRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
    }
}
```

**Key Points**:
- Inherits from base `Repository<T>` class
- Implements `IApartmentRepository` interface from Domain
- Uses EF Core `DbContext` for persistence
- Can add custom query methods here if needed

#### 4.1 EF Core Configuration

```csharp
// Configurations/ApartmentConfiguration.cs
internal sealed class ApartmentConfiguration : IEntityTypeConfiguration<Apartment>
{
    public void Configure(EntityTypeBuilder<Apartment> builder)
    {
        builder.ToTable("apartments");
        builder.HasKey(apartment => apartment.Id);

        // Value objects are owned entities
        builder.OwnsOne(apartment => apartment.Address);
        
        // Value objects with conversions
        builder.Property(apartment => apartment.Name)
            .HasMaxLength(200)
            .HasConversion(name => name.Value, value => new Name(value));

        builder.OwnsOne(apartment => apartment.Price, priceBuilder =>
        {
            priceBuilder.Property(money => money.Currency)
                .HasConversion(currency => currency.Code, code => Currency.FromCode(code));
        });
    }
}
```

**Key Points**:
- Maps domain entities to database tables
- Handles value object conversions
- Uses `OwnsOne` for value objects stored in same table

---

### **STEP 5: API Layer - Create Controller**

**Location**: `Bookify.Api/Controllers/Apartments/`

```csharp
// ApartmentsController.cs
[Authorize]
[Route("api/apartments")]
[ApiController]
public class ApartmentsController : ControllerBase
{
    private readonly ISender _sender;

    public ApartmentsController(ISender sender)
    {
        _sender = sender;
    }

    // READ operation (Query)
    [HttpGet]
    public async Task<IActionResult> SearchApartments(
        DateOnly startDate, 
        DateOnly endDate, 
        CancellationToken cancellationToken)
    {
        var query = new SearchApartmentsQuery(startDate, endDate);
        var result = await _sender.Send(query, cancellationToken);
        
        return Ok(result.Value);
    }

    // WRITE operation (Command) - Example
    [HttpPost]
    public async Task<IActionResult> CreateApartment(
        CreateApartmentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateApartmentCommand(
            request.Name,
            request.Description,
            request.Country,
            request.State,
            request.ZipCode,
            request.City,
            request.Street,
            request.PriceAmount,
            request.PriceCurrency,
            request.CleaningFeeAmount,
            request.CleaningFeeCurrency,
            request.Amenities);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction(
            nameof(GetApartment), 
            new { id = result.Value }, 
            result.Value);
    }
}
```

**Key Points**:
- Uses `ISender` (MediatR) to send commands/queries
- Controllers are thin - just map HTTP to commands/queries
- No business logic in controllers
- Returns appropriate HTTP status codes

---

### **STEP 6: Dependency Injection Setup**

#### 6.1 Application Layer DI

```csharp
// Bookify.Application/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register MediatR and handlers
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblies(typeof(DependencyInjection).Assembly);
            
            // Add pipeline behaviors
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
```

#### 6.2 Infrastructure Layer DI

```csharp
// Bookify.Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register repositories
        services.AddScoped<IApartmentRepository, ApartmentRepository>();
        
        // Register Unit of Work
        services.AddScoped<IUnitOfWork>(sp => 
            sp.GetRequiredService<ApplicationDbContext>());
        
        // Register SQL connection factory (for Dapper queries)
        services.AddSingleton<ISqlConnectionFactory>(_ =>
            new SqlConnectionFactory(connectionString));

        return services;
    }
}
```

---

## Complete Flow Example: Search Apartments (Query)

```
1. HTTP Request → GET /api/apartments?startDate=2024-01-01&endDate=2024-01-10

2. Controller (API Layer)
   ├─ Receives request
   ├─ Creates SearchApartmentsQuery
   └─ Sends via ISender (MediatR)

3. MediatR Pipeline
   ├─ LoggingBehavior (logs request)
   ├─ ValidationBehavior (validates query - if needed)
   └─ Routes to handler

4. QueryHandler (Application Layer)
   ├─ Validates business rules
   ├─ Uses ISqlConnectionFactory (Dapper)
   ├─ Executes raw SQL query
   ├─ Maps to ApartmentResponse DTOs
   └─ Returns Result<IReadOnlyList<ApartmentResponse>>

5. Controller
   └─ Returns HTTP 200 OK with response DTOs
```

---

## Complete Flow Example: Reserve Booking (Command)

```
1. HTTP Request → POST /api/bookings/reserve

2. Controller (API Layer)
   ├─ Receives ReserveBookingRequest
   ├─ Creates ReserveBookingCommand
   └─ Sends via ISender

3. MediatR Pipeline
   ├─ LoggingBehavior
   ├─ ValidationBehavior (runs FluentValidation)
   └─ Routes to handler

4. CommandHandler (Application Layer)
   ├─ Loads User from IUserRepository
   ├─ Loads Apartment from IApartmentRepository
   ├─ Validates business rules (overlapping bookings)
   ├─ Creates Booking domain entity (using factory method)
   ├─ Adds to IBookingRepository
   └─ Calls IUnitOfWork.SaveChangesAsync()

5. Infrastructure (Unit of Work)
   ├─ EF Core tracks changes
   ├─ Publishes domain events (outbox pattern)
   └─ Saves to database

6. Controller
   └─ Returns HTTP 201 Created with booking ID
```

---

## Key Patterns Used

### 1. **CQRS Separation**
- **Commands** (Write): Use domain entities, repositories, Unit of Work
- **Queries** (Read): Use raw SQL/Dapper, direct DTO mapping, bypass domain

### 2. **Clean Architecture Layers**
- **Domain**: Pure business logic, no dependencies
- **Application**: Use cases, commands/queries, handlers
- **Infrastructure**: Data access, external services
- **API**: Controllers, HTTP concerns

### 3. **MediatR Pattern**
- Decouples controllers from handlers
- Enables pipeline behaviors (logging, validation)
- Single responsibility per handler

### 4. **Result Pattern**
- Explicit error handling
- No exceptions for business logic failures
- Type-safe error returns

### 5. **Value Objects**
- Encapsulate validation
- Prevent primitive obsession
- Make code expressive

### 6. **Repository Pattern**
- Abstracts data access
- Interface in Domain, implementation in Infrastructure
- Dependency inversion principle

### 7. **Unit of Work Pattern**
- Manages transactions
- Publishes domain events
- Ensures consistency

---

## Implementation Checklist

When implementing a new feature (e.g., Apartments):

- [ ] **Domain Layer**
  - [ ] Create entity class (inherits from `Entity`)
  - [ ] Create value objects (if needed)
  - [ ] Create domain errors
  - [ ] Define repository interface
  - [ ] Add domain events (if needed)

- [ ] **Application Layer**
  - [ ] Create command/query record
  - [ ] Create command/query handler
  - [ ] Create response DTOs (for queries)
  - [ ] Create validator (for commands)
  - [ ] Add any application services (if needed)

- [ ] **Infrastructure Layer**
  - [ ] Implement repository class
  - [ ] Create EF Core configuration
  - [ ] Register in DI container

- [ ] **API Layer**
  - [ ] Create controller
  - [ ] Create request DTOs (if needed)
  - [ ] Map HTTP to commands/queries

- [ ] **Testing**
  - [ ] Test domain logic
  - [ ] Test handlers
  - [ ] Test API endpoints

---

## Summary: Order of Implementation

1. **Start with Domain** (innermost layer)
   - Entity, Value Objects, Repository Interface

2. **Move to Application** (use cases)
   - Commands/Queries, Handlers, DTOs, Validators

3. **Implement Infrastructure** (data access)
   - Repository implementation, EF Core config

4. **Create API** (presentation)
   - Controller, Request/Response mapping

5. **Wire Up DI** (dependency injection)
   - Register all services in DI containers

This ensures dependencies flow correctly: outer layers depend on inner layers, never the reverse.


