# Unit Testing Rules (xUnit + Moq + AwesomeAssertions)

- Frameworks: xUnit, Moq, AwesomeAssertions (FluentAssertions syntax, different namespace).
- Do not add comments to test code (especially not Arrange/Act/Assert).
- Braces must never be omitted.
- Expression-bodied members are not permitted in tests.

## Naming

- Test class name: `<ClassName>Tests`
- Test namespace mirrors the product namespace with `.Test` inserted before the final segment.  
  Example:  
  - Product: `SlimmingWorld.Platform.Integration.Services`  
  - Tests: `SlimmingWorld.Platform.Integration.Test.Services`
- Test method names use Given-When-Then:  
  `GIVEN_StateOfItem_WHEN_PerformingOperation_THEN_ShouldBeExpectedState`

## Test Class Structure

- The system under test is a readonly field named `_target`.
- `_target` is constructed in the test class constructor.
- Mocks used across tests are private readonly fields created with `Mock.Of<T>()`.
- Mocks that are local to a single test method should use `new Mock<T>()`.

## Test Data Conventions

- Strings use the property name as the value (no `nameof`), e.g. `user.FirstName = "FirstName"`.
- Dates use a fixed point in time: `2000-01-01 00:00` with the correct `DateTimeKind`.  
  Adjust earlier/later than this when ranges or ordering are required.
- Numeric values must be contextually appropriate.

## Coverage and Access

- Tests must cover 100% of the lines and branches of the implementation.
- Never use reflection to invoke implementation code. Cover private or protected methods via normal execution flow only.
- If code cannot be reached via public methods then consider asking to refactor.

## Clarification Policy

- Do not make assumptions. If any referenced code or behavior is unclear, ask for clarification before writing tests.

## Blazor Components

- Use `bUnit` for testing Blazor components.
- Follow the same naming and structuring conventions as above.
- Use `TestContext` for component rendering and dependency injection.
- Mock services using `Mock.Of<T>()` and register them in the `TestContext.Services`.
- Use `IRenderedComponent<T>` to interact with and assert against rendered components.
- Ensure component tests also achieve 100% line coverage.
- Raise any uncertainties for clarification before proceeding with component tests.
- Additions to the component under test to use a data attribute (data-test-id) is permitted to aid in element selection during testing.
- `RazorComponentTestBase<T>` can be used as a base class for component tests to encapsulate common setup logic with helper methods for selecting components using `data-test-id`.
- Do not use test harness components that inherit from the component under test to access protected members or invoke protected methods. Drive all behavior via UI interactions and public APIs only; if behavior cannot be reached through the UI, ask for a refactor or clarification.
- CSS selectors MUST not be used to locate/assert elements. FindComponent MUST be used. The only exception is to locate a DOM element to trigger an event.

## Pre-Flight Checklist (must confirm all before generating tests)

- [ ] I am using xUnit, Moq, and AwesomeAssertions.
- [ ] No comments will be included in the test code.
- [ ] Class name is `<ClassName>Tests`.
- [ ] Namespace mirrors the product namespace with `.Test` inserted appropriately.
- [ ] Methods follow `GIVEN_..._WHEN_..._THEN_...` naming.
- [ ] `_target` exists as a readonly field and is constructed in the test class constructor.
- [ ] Class-level mocks are `Mock.Of<T>()`; method-local mocks use `new Mock<T>()`.
- [ ] Strings use property names as values; dates use `2000-01-01 00:00` with correct `DateTimeKind`; numbers are sensible.
- [ ] No expression-bodied members; braces are always present.
- [ ] No reflection is used; private logic is covered through normal flows.
- [ ] Planned tests achieve 100% line coverage.
- [ ] Any uncertainties have been raised and clarified.
