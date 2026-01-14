# UserService Refactoring Plan

**Date:** January 14, 2026  
**Objective:** Transform UserService from tightly-coupled 73-line method into testable, SOLID-compliant code

## Executive Summary

Refactor [LegacyApp/UserService.cs](../LegacyApp/UserService.cs) using interfaces, dependency injection, and strategy pattern while maintaining backward compatibility. This addresses:

- 2 critical bugs (email validation logic, parameter name typo)
- All 5 SOLID principle violations
- Code duplication (credit service instantiation)
- Zero testability (currently requires database + WCF)

## Critical Constraints

### Immutable Code (DO NOT MODIFY)

1. [LegacyApp.Consumer/Program.cs](../LegacyApp.Consumer/Program.cs) - Including using statements
2. [LegacyApp/UserDataAccess.cs](../LegacyApp/UserDataAccess.cs) - Class and `AddUser` method must remain static

### Backward Compatibility Requirements

- `UserService.AddUser` public signature must remain: `bool AddUser(string, string, string, DateTime, int)`
- Can fix `firname` typo internally, but parameter order must match
- Return type must remain `bool`

## Current State Analysis

### Code Smells Identified

1. **Parameter Typo**: `firname` should be `firstname` (line 7)
2. **Email Validation Bug**: Uses AND (`&&`) instead of OR (`||`) on line 14
   - Current: `!email.Contains("@") && !email.Contains(".")`
   - Should be: `!email.Contains("@") || !email.Contains(".")`
3. **Comment Typo**: "chek" instead of "check" (line 43)
4. **Direct Instantiation**: Creates `ClientRepository` directly (line 30)
5. **Direct Instantiation**: Creates `UserCreditServiceClient` directly (lines 50, 60)
6. **Magic Strings**: "VeryImportantClient" and "ImportantClient" (lines 42, 47)
7. **Duplicate Code**: Credit service calls duplicated (lines 48-54 and 58-64)
8. **Multiple Responsibilities**: 6 different concerns in one method
9. **No Abstraction**: Static `UserDataAccess.AddUser` call (line 73)

### SOLID Violations

- **SRP**: Method handles validation, age calculation, client lookup, credit checking, and persistence
- **OCP**: Adding new client types requires modifying the method
- **LSP**: Lack of interfaces prevents polymorphic behavior
- **ISP**: No interfaces for ClientRepository or UserCreditServiceClient
- **DIP**: Depends on 3 concrete implementations (ClientRepository, UserCreditServiceClient, UserDataAccess)

## Implementation Steps

### Step 1: Create Abstraction Interfaces

**Files to Create:**

- `LegacyApp/IClientRepository.cs`
- `LegacyApp/IUserDataAccess.cs`

**IClientRepository Interface:**

```csharp
namespace LegacyApp
{
    public interface IClientRepository
    {
        Client GetById(int id);
    }
}
```

**IUserDataAccess Interface:**

```csharp
namespace LegacyApp
{
    public interface IUserDataAccess
    {
        void AddUser(User user);
    }
}
```

**Rationale:**

- `IClientRepository`: Enables DI and mocking for `ClientRepository`
- `IUserDataAccess`: Wraps static `UserDataAccess` (which is immutable) for testability

---

### Step 2: Implement Strategy Pattern for Credit Limits

**Files to Create:**

- `LegacyApp/ICreditLimitStrategy.cs`
- `LegacyApp/Strategies/VeryImportantClientStrategy.cs`
- `LegacyApp/Strategies/ImportantClientStrategy.cs`
- `LegacyApp/Strategies/DefaultClientStrategy.cs`
- `LegacyApp/ICreditLimitStrategyFactory.cs`
- `LegacyApp/CreditLimitStrategyFactory.cs`

**ICreditLimitStrategy Interface:**

```csharp
namespace LegacyApp
{
    public interface ICreditLimitStrategy
    {
        void ApplyCreditLimit(User user, IUserCreditService creditService);
    }
}
```

**Strategy Implementations:**

1. **VeryImportantClientStrategy** - No credit limit check
2. **ImportantClientStrategy** - Credit limit doubled
3. **DefaultClientStrategy** - Standard credit limit

**ICreditLimitStrategyFactory Interface:**

```csharp
namespace LegacyApp
{
    public interface ICreditLimitStrategyFactory
    {
        ICreditLimitStrategy GetStrategy(string clientName);
    }
}
```

**Rationale:**

- Eliminates magic strings "VeryImportantClient" and "ImportantClient"
- Removes duplicate credit service instantiation code
- Follows Open/Closed Principle (new client types = new strategy class)
- Consolidates credit calculation logic in one place per client type

---

### Step 3: Extract Validation Methods in UserService

**File to Modify:**

- `LegacyApp/UserService.cs`

**Add Private Methods:**

```csharp
private bool IsValidName(string firstname, string surname)
{
    return !string.IsNullOrEmpty(firstname) && !string.IsNullOrEmpty(surname);
}

private bool IsValidEmail(string email)
{
    // FIX: Changed && to || to correctly validate email
    return email.Contains("@") || email.Contains(".");
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

**Rationale:**

- **SRP**: Each method has one responsibility
- **Fixes email validation bug**: Changes `&&` to `||`
- **Improves readability**: Descriptive method names replace inline logic
- **Enables reuse**: Validation methods can be called from multiple places

---

### Step 4: Refactor UserService Constructor and AddUser

**File to Modify:**

- `LegacyApp/UserService.cs`

**Add Constructor with Dependency Injection:**

```csharp
private readonly IClientRepository _clientRepository;
private readonly IUserCreditService _userCreditService;
private readonly IUserDataAccess _userDataAccess;
private readonly ICreditLimitStrategyFactory _creditLimitStrategyFactory;

public UserService(
    IClientRepository clientRepository,
    IUserCreditService userCreditService,
    IUserDataAccess userDataAccess,
    ICreditLimitStrategyFactory creditLimitStrategyFactory)
{
    _clientRepository = clientRepository;
    _userCreditService = userCreditService;
    _userDataAccess = userDataAccess;
    _creditLimitStrategyFactory = creditLimitStrategyFactory;
}
```

**Refactor AddUser Method:**

```csharp
public bool AddUser(string firname, string surname, string email, DateTime dateOfBirth, int clientId)
{
    // Fix typo: map parameter to correct internal name
    string firstname = firname;
    
    // Validation using extracted methods
    if (!IsValidName(firstname, surname)) return false;
    if (!IsValidEmail(email)) return false;
    if (!IsAgeValid(dateOfBirth)) return false;
    
    // Use injected client repository
    var client = _clientRepository.GetById(clientId);
    
    var user = new User
    {
        Client = client,
        DateOfBirth = dateOfBirth,
        EmailAddress = email,
        Firstname = firstname,
        Surname = surname
    };
    
    // Use strategy pattern for credit limit logic
    var strategy = _creditLimitStrategyFactory.GetStrategy(client.Name);
    strategy.ApplyCreditLimit(user, _userCreditService);
    
    // Validate credit limit if applicable
    if (user.HasCreditLimit && user.CreditLimit < 500)
    {
        return false;
    }
    
    // Use injected user data access
    _userDataAccess.AddUser(user);
    
    return true;
}
```

**Key Changes:**

- **Fixes parameter typo**: Maps `firname` → `firstname` internally (maintains API compatibility)
- **Uses guard clauses**: Early returns for validation failures
- **Injects dependencies**: No more `new ClientRepository()` or `new UserCreditServiceClient()`
- **Uses strategy pattern**: Replaces string-based if/else with factory-selected strategy
- **Simplified logic**: Credit limit logic delegated to strategies

---

### Step 5: Update ClientRepository and Create Adapter Classes

**Files to Modify:**

- `LegacyApp/ClientRepository.cs`

**Files to Create:**

- `LegacyApp/UserDataAccessAdapter.cs`

**Update ClientRepository:**

```csharp
// Add interface implementation
public class ClientRepository : IClientRepository
{
    // Existing implementation unchanged
    public Client GetById(int id)
    {
        // ... existing code ...
    }
}
```

**Create UserDataAccessAdapter:**

```csharp
namespace LegacyApp
{
    public class UserDataAccessAdapter : IUserDataAccess
    {
        public void AddUser(User user)
        {
            // Delegate to immutable static method
            UserDataAccess.AddUser(user);
        }
    }
}
```

**Create CreditLimitStrategyFactory:**

```csharp
namespace LegacyApp
{
    public class CreditLimitStrategyFactory : ICreditLimitStrategyFactory
    {
        public ICreditLimitStrategy GetStrategy(string clientName)
        {
            return clientName switch
            {
                "VeryImportantClient" => new VeryImportantClientStrategy(),
                "ImportantClient" => new ImportantClientStrategy(),
                _ => new DefaultClientStrategy()
            };
        }
    }
}
```

**Rationale:**

- **Adapter pattern**: Wraps immutable static `UserDataAccess` for DI
- **Factory pattern**: Centralizes strategy selection logic
- **Maintains compatibility**: Static method still works, now wrapped

---

### Step 6: Create Comprehensive Unit Tests

**Files to Modify:**

- `RefactoringTest.UnitTests/RefactoringTest.UnitTests.csproj` (add Moq package)
- `RefactoringTest.UnitTests/UnitTest1.cs` (replace with real tests)

**Add Moq Package:**

```xml
<PackageReference Include="Moq" />
```

**Test Categories to Implement:**

1. **Validation Tests**
   - `AddUser_ShouldReturnFalse_WhenFirstnameIsEmpty`
   - `AddUser_ShouldReturnFalse_WhenSurnameIsEmpty`
   - `AddUser_ShouldReturnFalse_WhenEmailIsInvalid`
   - `AddUser_ShouldReturnFalse_WhenAgeIsUnder21`

2. **Client Type Tests**
   - `AddUser_ShouldReturnTrue_WhenVeryImportantClient`
   - `AddUser_ShouldReturnTrue_WhenImportantClientWithSufficientCredit`
   - `AddUser_ShouldReturnFalse_WhenImportantClientWithInsufficientCredit`
   - `AddUser_ShouldReturnTrue_WhenDefaultClientWithSufficientCredit`
   - `AddUser_ShouldReturnFalse_WhenDefaultClientWithInsufficientCredit`

3. **Credit Limit Threshold Tests**
   - `AddUser_ShouldReturnFalse_WhenCreditLimitIs499`
   - `AddUser_ShouldReturnTrue_WhenCreditLimitIs500`
   - `AddUser_ShouldReturnTrue_WhenCreditLimitIs501`

4. **Edge Cases**
   - `AddUser_ShouldCalculateAgeCorrectly_ForBirthdayToday`
   - `AddUser_ShouldCalculateAgeCorrectly_ForBirthdayTomorrow`

**Example Test Structure:**

```csharp
[Fact]
public void AddUser_ShouldReturnTrue_WhenVeryImportantClientWithValidData()
{
    // Arrange
    var mockClientRepo = new Mock<IClientRepository>();
    mockClientRepo.Setup(repo => repo.GetById(1))
        .Returns(new Client { Id = 1, Name = "VeryImportantClient" });
    
    var mockCreditService = new Mock<IUserCreditService>();
    var mockUserDataAccess = new Mock<IUserDataAccess>();
    
    var strategyFactory = new CreditLimitStrategyFactory();
    
    var userService = new UserService(
        mockClientRepo.Object,
        mockCreditService.Object,
        mockUserDataAccess.Object,
        strategyFactory);
    
    // Act
    var result = userService.AddUser("John", "Doe", "john@test.com", 
        new DateTime(1990, 1, 1), 1);
    
    // Assert
    Assert.True(result);
    mockUserDataAccess.Verify(u => u.AddUser(It.IsAny<User>()), Times.Once);
}
```

---

## Files Summary

### Files to Create (11 new files)

1. `LegacyApp/IClientRepository.cs` - Interface for client data access
2. `LegacyApp/IUserDataAccess.cs` - Interface wrapping static UserDataAccess
3. `LegacyApp/ICreditLimitStrategy.cs` - Strategy interface
4. `LegacyApp/Strategies/VeryImportantClientStrategy.cs` - No credit limit
5. `LegacyApp/Strategies/ImportantClientStrategy.cs` - Doubled credit limit
6. `LegacyApp/Strategies/DefaultClientStrategy.cs` - Standard credit limit
7. `LegacyApp/ICreditLimitStrategyFactory.cs` - Factory interface
8. `LegacyApp/CreditLimitStrategyFactory.cs` - Factory implementation
9. `LegacyApp/UserDataAccessAdapter.cs` - Adapter for static method

### Files to Modify (3 files)

1. `LegacyApp/UserService.cs`
   - Add constructor with DI
   - Add private validation methods
   - Refactor AddUser method
   - Fix parameter typo (firname → firstname)
   - Fix email validation bug (&& → ||)
   - Fix comment typo (chek → check)

2. `LegacyApp/ClientRepository.cs`
   - Add `IClientRepository` interface implementation

3. `RefactoringTest.UnitTests/UnitTest1.cs`
   - Replace empty test with comprehensive test suite
   - Add Moq for mocking dependencies
   - Cover all validation scenarios
   - Cover all client types
   - Cover credit limit thresholds

### Files NOT to Modify (Immutable)

1. `LegacyApp.Consumer/Program.cs` - Consumer must work unchanged
2. `LegacyApp/UserDataAccess.cs` - Static method is immutable constraint

---

## Validation Checklist

### Before Implementation

- [ ] Review plan with stakeholders
- [ ] Confirm ClientStatus enum usage strategy
- [ ] Verify test database availability (if needed for integration tests)

### During Implementation

- [ ] Create interfaces and strategies first (foundation)
- [ ] Update existing classes to implement interfaces
- [ ] Refactor UserService with DI
- [ ] Write unit tests for each scenario
- [ ] Run tests after each step

### After Implementation

- [ ] All unit tests pass (target: 100% coverage for UserService.AddUser)
- [ ] Solution builds without errors
- [ ] Consumer program compiles (runtime requires DB/WCF)
- [ ] Backward compatibility maintained (public API unchanged)
- [ ] Code review: Verify SOLID principles applied
- [ ] Documentation: Update README if needed

---

## Open Questions

1. **ClientStatus enum**: Currently unused with only `none` value. Should we:
   - Option A: Repurpose it for client type classification (replace magic strings)?
   - Option B: Keep strategy pattern independent of enum?
   - **Recommendation**: Keep strategy pattern as-is. Client types may come from external data, and strategy pattern is more flexible.

2. **Consumer validation**: [Program.cs](../LegacyApp.Consumer/Program.cs) requires database/WCF infrastructure. Should we:
   - Option A: Validate only via unit tests?
   - Option B: Set up integration test with test database?
   - **Recommendation**: Unit tests are sufficient. Consumer immutability ensures compatibility.

3. **Additional interfaces**: Should we extract an `IUser` or `IClient` interface?
   - **Recommendation**: Not necessary. These are data entities (POCOs), not behavior abstractions.

---

## Benefits After Refactoring

### Code Quality

- **Bug fixes**: Email validation and parameter naming corrected
- **SOLID compliant**: All 5 principles now followed
- **DRY**: No duplicate credit service code
- **Testable**: 100% unit test coverage possible via mocking

### Maintainability

- **Extensible**: New client types = new strategy class (no UserService changes)
- **Readable**: Validation methods have descriptive names
- **Separated concerns**: Each class has single responsibility
- **Documented**: Strategy pattern makes business rules explicit

### Developer Experience

- **Faster iteration**: Unit tests run in milliseconds (no database/WCF required)
- **Easier debugging**: Smaller methods, clear responsibilities
- **Confident refactoring**: Comprehensive test suite catches regressions
- **Onboarding**: Clean code principles make codebase approachable

---

## References

- [.github/copilot-instructions.md](../.github/copilot-instructions.md) - Project constraints and conventions
- [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
- [Strategy Pattern](https://refactoring.guru/design-patterns/strategy)
- [Dependency Injection in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
