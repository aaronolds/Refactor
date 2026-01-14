# Refactor Plan — `LegacyApp/UserService` (2026-01-14)

## Goal

Refactor `LegacyApp/UserService.cs` to better follow clean code principles (SOLID/KISS/DRY), reduce duplication, and improve testability — while maintaining backward compatibility.

## Hard constraints (non-negotiable)

- **Do NOT modify:** `LegacyApp.Consumer/Program.cs` (including `using` statements).
- **Do NOT modify:** `LegacyApp/UserDataAccess.cs` (class and its static `AddUser` method must remain static).
- **Must preserve public API:** `UserService.AddUser(string firstname, string surname, string email, DateTime dateOfBirth, int clientId)` returns `bool` and remains callable by the existing consumer.
- **Must preserve construction:** `new UserService()` must continue to work (consumer dependency).

## Current behavior to preserve (unless explicitly approved to change)

- Return `false` when:
  - first name or surname is null/empty
  - email is invalid (note: current implementation has a bug; see decision points)
  - user is under 21
  - user has a credit limit AND `CreditLimit < 500`
- Client credit tier rules based on `Client.Name`:
  - `"VeryImportantClient"`: no credit limit check (`HasCreditLimit = false`)
  - `"ImportantClient"`: credit limit fetched and **doubled**
  - default: standard credit limit fetched
- Persist successful user via `UserDataAccess.AddUser(user)`

## Decision points (need explicit approval)

- [ ] **Email validation bug fix:** change the invalid-email condition from AND to OR (tightens validation; may reject some previously-accepted invalid emails).
- [ ] **Client not found behavior:** if `ClientRepository.GetById(clientId)` returns `null`, should `AddUser` return `false` (recommended) or throw (current code can null-ref later)?
- [ ] **WCF disposal hardening:** keep current simple usage vs. implement a safer Close/Abort pattern (more code; likely unnecessary for this exercise).

## Planned changes (tracked)

### 1) Baseline + safety net

- [ ] Read `README.md` and confirm all expectations/constraints.
- [ ] Inspect current `LegacyApp/UserService.cs` and identify the minimal set of behavioral changes (if any) beyond readability/testability.
- [ ] Ensure unit test project is runnable and establish a baseline `dotnet test` run (expect minimal/empty coverage initially).

### 2) Refactor `UserService` internals (no public API changes)

- [ ] In `LegacyApp/UserService.cs`, extract intent-revealing private methods:
  - [ ] `IsValidName(firstname, surname)`
  - [ ] `IsValidEmail(email)` (apply approved fix)
  - [ ] `IsOldEnough(dateOfBirth, now)` / `CalculateAge(dateOfBirth, now)`
- [ ] Replace inline logic with guard clauses using the extracted methods.
- [ ] Keep `AddUser` readable and narrowly focused on orchestration.

### 3) Introduce dependency seams (DIP) while preserving `new UserService()`

- [ ] Add abstractions (interfaces) in `LegacyApp/` to allow unit testing without DB/WCF:
  - [ ] `IClientRepository`
  - [ ] `IUserDataAccess` (adapter around static `UserDataAccess`)
  - [ ] `IClock` (optional but recommended for deterministic age tests)
- [ ] Add default implementations/adapters:
  - [ ] `UserDataAccessAdapter` calling `UserDataAccess.AddUser(user)`
  - [ ] `SystemClock` returning `DateTime.Now`
- [ ] Update `UserService`:
  - [ ] Add an overload/constructor that accepts dependencies (for tests)
  - [ ] Keep parameterless constructor that wires up production defaults

### 4) Remove duplicated credit logic + reduce magic-string branching

- [ ] Introduce credit limit strategies in `LegacyApp/Strategies/`:
  - [ ] `ICreditLimitStrategy`
  - [ ] `VeryImportantClientCreditLimitStrategy`
  - [ ] `ImportantClientCreditLimitStrategy`
  - [ ] `DefaultClientCreditLimitStrategy`
  - [ ] `CreditLimitStrategyFactory` (central place for mapping `Client.Name` → strategy)
- [ ] Update `UserService.AddUser` to:
  - [ ] Fetch client (and handle `null` per approved decision)
  - [ ] Create user model
  - [ ] Apply exactly one strategy (no duplicated WCF calls)
  - [ ] Enforce `CreditLimit < 500` consistently

### 5) Unit tests (xUnit)

- [ ] Replace placeholder tests with coverage for key scenarios:
  - [ ] Invalid names (null/empty)
  - [ ] Email validation cases (missing `@`, missing `.`, missing both)
  - [ ] Age boundary (20 fails, 21 passes)
  - [ ] Credit tier rules: VeryImportant bypasses credit, Important doubles credit
  - [ ] Credit threshold boundary (499 fails, 500 passes)
  - [ ] Client not found behavior (per approved decision)
- [ ] Run tests and ensure everything passes.

### 6) Final cleanup + verification

- [ ] Remove any unused code/constructors not needed after refactor.
- [ ] Re-run `dotnet build` and `dotnet test`.
- [ ] Quick sanity check that consumer still compiles (without modifying consumer code).

## Expected files to change

### Will modify

- `LegacyApp/UserService.cs` — internal refactor + dependency injection + strategy orchestration.
- `RefactoringTest.UnitTests/UnitTest1.cs` — replace placeholder test(s) with real coverage (or split into more test files if preferred).

### Will add

- `LegacyApp/IClientRepository.cs`
- `LegacyApp/IUserDataAccess.cs`
- `LegacyApp/UserDataAccessAdapter.cs`
- `LegacyApp/IClock.cs` (optional)
- `LegacyApp/SystemClock.cs` (optional)
- `LegacyApp/Strategies/ICreditLimitStrategy.cs`
- `LegacyApp/Strategies/VeryImportantClientCreditLimitStrategy.cs`
- `LegacyApp/Strategies/ImportantClientCreditLimitStrategy.cs`
- `LegacyApp/Strategies/DefaultClientCreditLimitStrategy.cs`
- `LegacyApp/Strategies/CreditLimitStrategyFactory.cs`

### Must NOT modify

- `LegacyApp.Consumer/Program.cs`
- `LegacyApp/UserDataAccess.cs`
