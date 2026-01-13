# Copilot Instructions for Legacy App Refactoring

## Project Overview
This is a C# refactoring exercise focused on applying clean code principles (SOLID, KISS, DRY, YAGNI) to a legacy user registration system. The codebase demonstrates a typical legacy scenario: tightly coupled business logic in [LegacyApp/UserService.cs](LegacyApp/UserService.cs) that needs refactoring while maintaining backward compatibility.

## Critical Constraints

## **Non-Negotiable Rules**
**NEVER make changes to existing files or create new files without explicit permission from the user. Always show proposed changes and ask for verification before implementing them.**

**IMMUTABLE CODE (DO NOT MODIFY):**
- [LegacyApp.Consumer/Program.cs](LegacyApp.Consumer/Program.cs) - Including using statements
- [LegacyApp/UserDataAccess.cs](LegacyApp/UserDataAccess.cs) class and its `AddUser` method (must remain static)

**Why:** This simulates a production system where consumers depend on the existing API surface. Any non-backward compatible change breaks integration points.

## Architecture & Key Components

### Core Business Flow
1. **UserService.AddUser** - Main entry point with validation, credit check logic
2. **ClientRepository** - SQL database access via stored procedure `uspGetClientById`
3. **UserCreditServiceClient** - WCF service client for external credit limit checks
4. **UserDataAccess** - Static data persistence layer calling `uspAddUser` stored procedure

### Client Classification Logic
The system applies different credit rules based on client tier:
- **VeryImportantClient**: No credit limit check (`HasCreditLimit = false`)
- **ImportantClient**: Credit limit doubled after external check
- **Default clients**: Standard credit limit from external service

Minimum credit threshold: 500

## Refactoring Targets

### Known Code Smells in UserService.AddUser
- **Typo**: Parameter name `firname` should be `firstname`
- **Email validation bug**: Uses AND instead of OR (`!email.Contains("@") && !email.Contains(".")`)
- **Duplicate credit service logic**: Credit check code repeated for ImportantClient and default paths
- **Direct instantiation**: Creates `ClientRepository` and `UserCreditServiceClient` directly (violates DIP)
- **String-based client type checking**: Magic strings like "VeryImportantClient"
- **Multiple responsibilities**: Validation, age calculation, client lookup, credit checking in one method

### Refactoring Approaches

#### 1. Strategy Pattern for Credit Calculation
Replace string-based client type checking with a strategy pattern:

```csharp
public interface ICreditLimitStrategy
{
    void ApplyCreditLimit(User user, IUserCreditService creditService);
}

public class VeryImportantClientStrategy : ICreditLimitStrategy
{
    public void ApplyCreditLimit(User user, IUserCreditService creditService)
    {
        user.HasCreditLimit = false;
    }
}

public class ImportantClientStrategy : ICreditLimitStrategy
{
    public void ApplyCreditLimit(User user, IUserCreditService creditService)
    {
        user.HasCreditLimit = true;
        var creditLimit = creditService.GetCreditLimit(user.Firstname, user.Surname, user.DateOfBirth);
        user.CreditLimit = creditLimit * 2;
    }
}
```

#### 2. Dependency Injection
Inject dependencies via constructor to enable testability and decouple components:

```csharp
public class UserService
{
    private readonly IClientRepository _clientRepository;
    private readonly IUserCreditService _userCreditService;
    private readonly ICreditLimitStrategyFactory _creditLimitStrategyFactory;

    public UserService(
        IClientRepository clientRepository,
        IUserCreditService userCreditService,
        ICreditLimitStrategyFactory creditLimitStrategyFactory)
    {
        _clientRepository = clientRepository;
        _userCreditService = userCreditService;
        _creditLimitStrategyFactory = creditLimitStrategyFactory;
    }
}
```

#### 3. Extract Validation Methods
Use private methods with descriptive names following Single Responsibility Principle:

```csharp
private bool IsValidName(string firstname, string surname)
{
    return !string.IsNullOrEmpty(firstname) && !string.IsNullOrEmpty(surname);
}

private bool IsValidEmail(string email)
{
    return email.Contains("@") || email.Contains(".");  // Fix: OR not AND
}

private bool IsAgeValid(DateTime dateOfBirth)
{
    int age = CalculateAge(dateOfBirth);
    return age >= 21;
}

private int CalculateAge(DateTime dateOfBirth)
{
    var now = DateTime.Now;
    int age = now.Year - dateOfBirth.Year;
    if (now.Month < dateOfBirth.Month || 
        (now.Month == dateOfBirth.Month && now.Day < dateOfBirth.Day)) 
        age--;
    return age;
}
```

#### 4. Replace Magic Strings with Enums
Create enum for client types to prevent typos and enable compile-time checking:

```csharp
public enum ClientType
{
    Normal = 0,
    ImportantClient = 1,
    VeryImportantClient = 2
}
```

**Key Constraint**: Maintain the same public API signature: `bool AddUser(string, string, string, DateTime, int)`

## Testing Strategy

### xUnit Best Practices

- Unit tests use **xUnit** (see [RefactoringTest.UnitTests/UnitTest1.cs](RefactoringTest.UnitTests/UnitTest1.cs))
- Test framework: xUnit 2.4.1 with Visual Studio test runner
- Follow the **Arrange-Act-Assert (AAA)** pattern
- Use descriptive test method names: `AddUser_Should{Expected}_When{Condition}`

### Mocking Dependencies

Use **Moq** (recommended) or **NSubstitute** for mocking external dependencies:

```csharp
// Example: Mock IClientRepository
var mockClientRepo = new Mock<IClientRepository>();
mockClientRepo.Setup(repo => repo.GetById(It.IsAny<int>()))
    .Returns(new Client 
    { 
        Id = 1, 
        Name = "VeryImportantClient",
        ClientStatus = ClientStatus.none 
    });

// Example: Mock IUserCreditService
var mockCreditService = new Mock<IUserCreditService>();
mockCreditService.Setup(service => service.GetCreditLimit(
    It.IsAny<string>(), 
    It.IsAny<string>(), 
    It.IsAny<DateTime>()))
    .Returns(600);

// Example test structure
[Fact]
public void AddUser_ShouldReturnTrue_WhenVeryImportantClientWithValidData()
{
    // Arrange
    var mockClientRepo = CreateMockClientRepository(clientType: "VeryImportantClient");
    var mockCreditService = CreateMockCreditService();
    var userService = new UserService(mockClientRepo.Object, mockCreditService.Object);
    
    // Act
    var result = userService.AddUser("John", "Doe", "john@test.com", 
        new DateTime(1990, 1, 1), 1);
    
    // Assert
    Assert.True(result);
}

[Theory]
[InlineData("", "Doe")]
[InlineData("John", "")]
[InlineData(null, "Doe")]
public void AddUser_ShouldReturnFalse_WhenNameIsInvalid(string firstname, string surname)
{
    // Arrange, Act, Assert
}
```

### WCF Service Testing

Mock the `IUserCreditService` interface rather than the concrete `UserCreditServiceClient`:
- Extract interface from WCF client
- Inject `IUserCreditService` instead of instantiating `UserCreditServiceClient` directly
- This allows easy mocking without requiring WCF infrastructure

### Consumer Validation

**Note**: The consumer requires a SQL database connection and WCF service endpoint to run successfully. Without these dependencies, it will throw a `NullReferenceException` at runtime. For refactoring validation, focus on:
- Unit tests with mocked dependencies pass successfully
- Code compiles without errors
- Public API signature remains unchanged

```powershell
# Build and test (recommended validation approach)
dotnet build Refactoring.sln
dotnet test RefactoringTest.UnitTests/RefactoringTest.UnitTests.csproj

# Consumer (requires database/WCF infrastructure)
dotnet run --project LegacyApp.Consumer/LegacyApp.Consumer.csproj
```

## Build & Run

```powershell
# Build solution
dotnet build Refactoring.sln

# Add Moq for testing (if not already added)
dotnet add RefactoringTest.UnitTests/RefactoringTest.UnitTests.csproj package Moq

# Run tests
dotnet test RefactoringTest.UnitTests/RefactoringTest.UnitTests.csproj

# Run consumer (validates backward compatibility)
dotnet run --project LegacyApp.Consumer/LegacyApp.Consumer.csproj
```

## Technology Stack

- **.NET 10.0** with nullable reference types enabled
- **Microsoft.Data.SqlClient** for database access (modern replacement for System.Data.SqlClient)
- **System.ServiceModel** for WCF service client (legacy SOAP service)
- **System.Configuration.ConfigurationManager** for connection strings

### Centralized Build Configuration

This project uses **Central Package Management (CPM)** with:
- **[Directory.Build.props](Directory.Build.props)** - Defines `TargetFramework` (net10.0), `Nullable`, `ImplicitUsings`, and shared compiler settings
- **[Directory.Packages.props](Directory.Packages.props)** - Centrally manages all NuGet package versions with `ManagePackageVersionsCentrally`

All project files (.csproj) inherit these settings automatically. Package versions are defined once in Directory.Packages.props and referenced without versions in individual projects.

## C# Coding Conventions

Follow Microsoft's C# coding conventions and naming guidelines:

### Naming
- **PascalCase**: Classes, methods, properties, public fields (`UserService`, `AddUser`, `CreditLimit`)
- **camelCase**: Private fields with underscore prefix (`_clientRepository`, `_userCreditService`)
- **camelCase**: Parameters and local variables (`firstname`, `clientId`, `creditLimit`)
- **Interfaces**: Prefix with `I` (`IClientRepository`, `IUserCreditService`)

### File Organization
- **One class per file**: Match filename to class name (`UserService.cs` contains `UserService`)
- **Interfaces in separate files**: `IClientRepository.cs` for the interface
- **Group related classes**: Strategies in `Strategies/` folder, validators in `Validators/`

### Code Style
- **Use expression-bodied members** for simple properties/methods:
  ```csharp
  public string FullName => $"{Firstname} {Surname}";
  ```
- **Prefer `var`** when type is obvious from right side:
  ```csharp
  var client = clientRepository.GetById(clientId);  // Type is clear
  ```
- **Guard clauses first**: Return early for invalid conditions
  ```csharp
  if (!IsValidName(firstname, surname)) return false;
  if (!IsValidEmail(email)) return false;
  if (!IsAgeValid(dateOfBirth)) return false;
  ```
- **Use nullable reference types**: Leverage `?` annotations for nullability contracts

### SOLID Principles Applied
- **S**ingle Responsibility: Separate validation, credit calculation, persistence concerns
- **O**pen/Closed: Use strategy pattern - open for extension (new client types), closed for modification
- **L**iskov Substitution: All `ICreditLimitStrategy` implementations are interchangeable
- **I**nterface Segregation: Create focused interfaces (`IClientRepository`, `IUserCreditService`)
- **D**ependency Inversion: Depend on abstractions (`IClientRepository`) not concretions (`ClientRepository`)

## Key Dependencies

External systems (simulated/mocked):
- SQL Server stored procedures: `uspGetClientById`, `uspAddUser`
- WCF service: `http://totally-real-service.com/IUserCreditService/GetCreditLimit`
