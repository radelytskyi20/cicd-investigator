---
description: "This file provides guidelines for writing clean, maintainable, and idiomatic C# code with a focus on functional patterns and proper abstraction."
applyTo: "**/*.cs"
---

## Project Type Decision Rules

- Admin-based projects (legacy .NET Framework), for example `Shaw.EDS.Admin.Features.Products.Library`:
    - Keep code paths synchronous.
    - Do not introduce `async`/`await`, `Task`-returning public APIs, or `CancellationToken`-based public signatures unless the existing path is already asynchronous and requires parity.
- Multi-target libraries (for example net48 + net10):
    - `async`/`await` is allowed when it is required by the implementation or consuming APIs.
    - Keep legacy .NET Framework-compatible styling for shared source files (for example block-scoped namespaces).
- .NET 10-only projects:
    - Follow modern styling rules in this guide, including file-scoped namespaces.

## Type Definitions:

- Define only one type per file. Each class, record, struct, interface, enum, and delegate must have its own file named after the type. Do not place helper, marker, request, response, or nested types in another type's file.

- Prefer records for data types:
    ```csharp
    // Good: Immutable data type with value semantics
    public sealed record CustomerDto(string Name, Email Email);

    // Avoid: Class with mutable properties
    public class Customer
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }
    ```
- Make classes sealed by default:
    ```csharp
    // Good: Make classes sealed by default
    public sealed class OrderProcessor
    {
        // Implementation
    }

    // Only unsealed when inheritance is specifically designed for
    public abstract class Repository<T>
    {
        // Base implementation
    }
    ```
- Prefer primary constructors:
    ```csharp
    // Good: Use a primary constructor when dependencies are simple constructor-injected fields
    public sealed class OrderProcessor(IOrderRepository orderRepository, ILogger<OrderProcessor> logger)
    {
        private readonly IOrderRepository _orderRepository = orderRepository;
        private readonly ILogger<OrderProcessor> _logger = logger;
    }

    // Avoid: Verbose constructor boilerplate when a primary constructor is sufficient
    public sealed class OrderProcessor
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<OrderProcessor> _logger;

        public OrderProcessor(IOrderRepository orderRepository, ILogger<OrderProcessor> logger)
        {
            _orderRepository = orderRepository;
            _logger = logger;
        }
    }
    ```
- Avoid large parameter lists in methods and constructors:
    ```csharp
    // Good: Keep signatures readable when many dependencies are required
    public class ArticleItemModelService(
        IProductDataService productDataService,
        IRelationshipService relationshipService,
        IButtonGroupWidgetService buttonGroupWidgetService,
        ISiteSettingsService settingsService,
        IHttpContextAccessor httpContextAccessor,
        IFilterService filterService,
        IUserModelService userModelService) : IArticleItemModelService

    // Better: Refactor when parameter count grows too large
    // (for example use a cohesive facade service, parameter object, or options model).

    // Avoid: Long dense one-line signatures that are hard to scan and maintain
    public class ArticleItemModelService(IProductDataService productDataService, IRelationshipService relationshipService, IButtonGroupWidgetService buttonGroupWidgetService, ISiteSettingsService settingsService, IHttpContextAccessor httpContextAccessor, IFilterService filterService, IUserModelService userModelService) : IArticleItemModelService
    ```

## Variable Declarations

- Use explicit types for built-in/primitive types (e.g., `string`, `int`, `bool`).
- Use `var` for custom types and class object instantiations to reduce redundancy.

```csharp
// Good: Explicit types for primitives
string fruit = "Apple";
int number = 42;

// Good: Using var for custom types and object creation
var order = new Order(fruit, number);
```

## Variable Naming Conventions
- **Do not shortcut variable names.** Always use highly descriptive, unabbreviated names (e.g., use `userAccount` instead of `usrAcct`).
- Use `camelCase` for local variables and method parameters.
- Use `PascalCase` for class names, method names, records, and public properties.
- Use `_camelCase` for private fields.

## Control Flow:

- Prefer collection initializers:
    ```csharp
    // Good: Using collection initializers
    string[] fruits = ["Apple", "Banana", "Cherry"];

    // Avoid: Using explicit initialization when type is clear
    var fruits = new List<int>() {
        "Apple",
        "Banana",
        "Cherry"
    };
    ```
- Use pattern matching effectively:
    ```csharp
    // Good: Clear pattern matching
    public decimal CalculateDiscount(Customer customer) =>
        customer switch
        {
            { Tier: CustomerTier.Premium } => 0.2m,
            { OrderCount: > 10 } => 0.1m,
            _ => 0m
        };

    // Avoid: Nested if statements
    public decimal CalculateDiscount(Customer customer)
    {
        if (customer.Tier == CustomerTier.Premium)
            return 0.2m;
        if (customer.OrderCount > 10)
            return 0.1m;
        return 0m;
    }
    ```
- Do not use nested ternary operators:
    ```csharp
    // Avoid: Nested ternary is hard to read
    var label = isArchived
        ? "Archived"
        : isDraft
            ? "Draft"
            : "Published";

    // Good: Use explicit control flow or pattern matching for clarity
    string label;
    if (isArchived)
    {
        label = "Archived";
    }
    else if (isDraft)
    {
        label = "Draft";
    }
    else
    {
        label = "Published";
    }
    ```

## Nullability:

- Mark nullable fields explicitly:
    ```csharp
    // Good: Explicit nullability
    public class OrderProcessor
    {
        private readonly ILogger<OrderProcessor>? _logger;
        private string? _lastError;

        public OrderProcessor(ILogger<OrderProcessor>? logger = null)
        {
            _logger = logger;
        }
    }

    // Avoid: Implicit nullability
    public class OrderProcessor
    {
        private readonly ILogger<OrderProcessor> _logger; // Warning: Could be null
        private string _lastError; // Warning: Could be null
    }
    ```
- Use null checks only when necessary for reference types and public methods:
    ```csharp
    // Good: Proper null checking
    public void ProcessOrder(Order order)
    {
        ArgumentNullException.ThrowIfNull(order); // Appropriate for reference types

        _logger?.LogInformation("Processing order {Id}", order.Id);
    }

    // Good: Using pattern matching for null checks
    public decimal CalculateTotal(Order? order) =>
        order switch
        {
            null => throw new ArgumentNullException(nameof(order)),
            { Lines: null } => throw new ArgumentException("Order lines cannot be null", nameof(order)),
            _ => order.Lines.Sum(l => l.Total)
        };
    // BAD: Avoid null checks for value types
    public void ProcessOrder(int orderId)
    {
        ArgumentNullException.ThrowIfNull(order); // DON'T USE Null checks are unnecessary for value types
    }

    // Avoid: null checks for non-public methods
    private void ProcessOrder(Order order)
    {
        ArgumentNullException.ThrowIfNull(order); // DON'T USE, ProcessOrder is private
    }
    ```
- Use null-forgiving operator when appropriate:
    ```csharp
    public class OrderValidator
    {
        private readonly IValidator<Order> _validator;

        public OrderValidator(IValidator<Order> validator)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public ValidationResult Validate(Order order)
        {
            // We know _validator can't be null due to constructor check
            return _validator!.Validate(order);
        }
    }
    ```
- Use nullability attributes:
    ```csharp
    public class StringUtilities
    {
        // Output is non-null if input is non-null
        [return: NotNullIfNotNull(nameof(input))]
        public static string? ToUpperCase(string? input) =>
            input?.ToUpperInvariant();

        // Method never returns null
        [return: NotNull]
        public static string EnsureNotNull(string? input) =>
            input ?? string.Empty;

        // Parameter must not be null when method returns true
        public static bool TryParse(string? input, [NotNullWhen(true)] out string? result)
        {
            result = null;
            if (string.IsNullOrEmpty(input))
                return false;

            result = input;
            return true;
        }
    }
    ```
- Use init-only properties with non-null validation:
    ```csharp
    // Good: Non-null validation in constructor
    public sealed record Order
    {
        public required OrderId Id { get; init; }
        public required ImmutableList<OrderLine> Lines { get; init; }

        public Order()
        {
            Id = null!; // Will be set by required property
            Lines = null!; // Will be set by required property
        }

        private Order(OrderId id, ImmutableList<OrderLine> lines)
        {
            Id = id;
            Lines = lines;
        }

        public static Order Create(OrderId id, IEnumerable<OrderLine> lines) =>
            new(id, lines.ToImmutableList());
    }
    ```
- Document nullability in interfaces:
    ```csharp
    public interface IOrderRepository
    {
        // Explicitly shows that null is a valid return value
        Task<Order?> FindByIdAsync(OrderId id, CancellationToken ct = default);

        // Method will never return null
        [return: NotNull]
        Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default);

        // Parameter cannot be null
        Task SaveAsync([NotNull] Order order, CancellationToken ct = default);
    }
    ```

## Safe Operations:

- Use Try methods for safer operations:
    ```csharp
    // Good: Using TryGetValue for dictionary access
    if (dictionary.TryGetValue(key, out var value))
    {
        // Use value safely here
    }
    else
    {
        // Handle missing key case
    }
    ```
    ```csharp
    // Avoid: Direct indexing which can throw
    var value = dictionary[key];  // Throws if key doesn't exist

    // Good: Using Uri.TryCreate for URL parsing
    if (Uri.TryCreate(urlString, UriKind.Absolute, out var uri))
    {
        // Use uri safely here
    }
    else
    {
        // Handle invalid URL case
    }
    ```
    ```csharp
    // Avoid: Direct Uri creation which can throw
    var uri = new Uri(urlString);  // Throws on invalid URL

    // Good: Using int.TryParse for number parsing
    if (int.TryParse(input, out var number))
    {
        // Use number safely here
    }
    else
    {
        // Handle invalid number case
    }
    ```
    ```csharp
    // Good: Combining Try methods with null coalescing
    var value = dictionary.TryGetValue(key, out var result)
        ? result
        : defaultValue;

    // Good: Using Try methods in LINQ with pattern matching
    var validNumbers = strings
        .Select(s => (Success: int.TryParse(s, out var num), Value: num))
        .Where(x => x.Success)
        .Select(x => x.Value);
    ```

- Prefer Try methods over exception handling:
    ```csharp
    // Good: Using Try method
    if (decimal.TryParse(priceString, out var price))
    {
        // Process price
    }

    // Avoid: Exception handling for expected cases
    try
    {
        var price = decimal.Parse(priceString);
        // Process price
    }
    catch (FormatException)
    {
        // Handle invalid format
    }
    ```

## Asynchronous Programming:

- Exclusion for admin-based projects (legacy .NET Framework), for example `Shaw.EDS.Admin.Features.Products.Library`:
    ```csharp
    // Legacy Admin project code paths must stay synchronous.
    // Do not introduce async/await, Task-returning public APIs, or CancellationToken-based public signatures
    // unless the existing code path is already asynchronous and requires parity.
    ```
- Multi-target libraries (net48 + net10) may use async/await where needed.
- The async rules below apply to projects and code paths where asynchronous execution is supported.

- Use Task.FromResult for pre-computed values:
    ```csharp
    // Good: Return pre-computed value
    public Task<int> GetDefaultQuantityAsync() =>
        Task.FromResult(1);

    // Better: Use ValueTask for zero allocations
    public ValueTask<int> GetDefaultQuantityAsync() =>
        new ValueTask<int>(1);

    // Avoid: Unnecessary thread pool usage
    public Task<int> GetDefaultQuantityAsync() =>
        Task.Run(() => 1);
    ```
- Always flow CancellationToken:
    ```csharp
    // Good: Propagate cancellation
    public async Task<Order> ProcessOrderAsync(
        OrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await _repository.GetAsync(
            request.OrderId,
            cancellationToken);

        await _processor.ProcessAsync(
            order,
            cancellationToken);

        return order;
    }
    ```
- Prefer await:
    ```csharp
    // Good: Using await
    public async Task<Order> ProcessOrderAsync(OrderId id)
    {
        var order = await _repository.GetAsync(id);
        await _validator.ValidateAsync(order);
        return order;
    }
    ```
- Never use Task.Result or Task.Wait:
    ```csharp
    // Good: Async all the way
    public async Task<Order> GetOrderAsync(OrderId id)
    {
        return await _repository.GetAsync(id);
    }

    // Avoid: Blocking on async code
    public Order GetOrder(OrderId id)
    {
        return _repository.GetAsync(id).Result; // Can deadlock
    }
    ```
- Use TaskCompletionSource correctly:
    ```csharp
    // Good: Using RunContinuationsAsynchronously
    private readonly TaskCompletionSource<Order> _tcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    // Avoid: Default TaskCompletionSource can cause deadlocks
    private readonly TaskCompletionSource<Order> _tcs = new();
    ```
- Always dispose CancellationTokenSources:
    ```csharp
    // Good: Proper disposal of CancellationTokenSource
    public async Task<Order> GetOrderWithTimeout(OrderId id)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        return await _repository.GetAsync(id, cts.Token);
    }
    ```
- Prefer async/await over direct Task return:
    ```csharp
    // Good: Using async/await
    public async Task<Order> ProcessOrderAsync(OrderRequest request)
    {
        await _validator.ValidateAsync(request);
        var order = await _factory.CreateAsync(request);
        return order;
    }

    // Avoid: Manual task composition
    public Task<Order> ProcessOrderAsync(OrderRequest request)
    {
        return _validator.ValidateAsync(request)
            .ContinueWith(t => _factory.CreateAsync(request))
            .Unwrap();
    }
    ```

## Symbol References:

- Always use nameof operator:
    ```csharp
    // Good: Using nameof in attributes
    public class OrderProcessor
    {
        [Required(ErrorMessage = "The {0} field is required")]
        [Display(Name = nameof(OrderId))]
        public string OrderId { get; init; }

        [MemberNotNull(nameof(_repository))]
        private void InitializeRepository()
        {
            _repository = new OrderRepository();
        }

        [NotifyPropertyChangedFor(nameof(FullName))]
        public string FirstName
        {
            get => _firstName;
            set => SetProperty(ref _firstName, value);
        }
    }
    ```
- Use nameof with exceptions:
    ```csharp
    public class OrderService
    {
        public async Task<Order> GetOrderAsync(OrderId id, CancellationToken ct)
        {
            var order = await _repository.FindAsync(id, ct);

            if (order is null)
                throw new OrderNotFoundException(
                    $"Order with {nameof(id)} '{id}' not found");

            if (!order.Lines.Any())
                throw new InvalidOperationException(
                    $"{nameof(order.Lines)} cannot be empty");

            return order;
        }

        public void ValidateOrder(Order order)
        {
            if (order.Lines.Count == 0)
                throw new ArgumentException(
                    "Order must have at least one line",
                    nameof(order));
        }
    }
    ```
- Use nameof in logging:
    ```csharp
    public class OrderProcessor
    {
        private readonly ILogger<OrderProcessor> _logger;

        public async Task ProcessAsync(Order order)
        {
            _logger.LogInformation(
                "Starting {Method} for order {OrderId}",
                nameof(ProcessAsync),
                order.Id);

            try
            {
                await ProcessInternalAsync(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error in {Method} for {Property} {Value}",
                    nameof(ProcessAsync),
                    nameof(order.Id),
                    order.Id);
                throw;
            }
        }
    }
    ```

### Usings and Namespaces:

- Use implicit usings:
    ```csharp
    // Good: Implicit
    namespace MyNamespace
    {
        public class MyClass
        {
            // Implementation
        }
    }
    // Avoid:
    using System; // DON'T USE
    using System.Collections.Generic; // DON'T USE
    using System.IO; // DON'T USE
    using System.Linq; // DON'T USE
    using System.Net.Http; // DON'T USE
    using System.Threading; // DON'T USE
    using System.Threading.Tasks;// DON'T USE
    using System.Net.Http.Json; // DON'T USE
    using Microsoft.AspNetCore.Builder; // DON'T USE
    using Microsoft.AspNetCore.Hosting; // DON'T USE
    using Microsoft.AspNetCore.Http; // DON'T USE
    using Microsoft.AspNetCore.Routing; // DON'T USE
    using Microsoft.Extensions.Configuration; // DON'T USE
    using Microsoft.Extensions.DependencyInjection; // DON'T USE
    using Microsoft.Extensions.Hosting; // DON'T USE
    using Microsoft.Extensions.Logging; // DON'T USE
    using Good: Explicit usings; // DON'T USE

    namespace MyNamespace
    {
        public class MyClass
        {
            // Implementation
        }
    }
    ```
- Use file-scoped namespaces for .NET 10-only projects:
    ```csharp
    // Good: File-scoped namespace
    namespace MyNamespace;

    public class MyClass
    {
        // Implementation
    }

    // Avoid: Block-scoped namespace
    namespace MyNamespace
    {
        public class MyClass
        {
            // Implementation
        }
    }
    ```

- Exclusion for multi-target libraries (net48 + net10): keep legacy .NET Framework style for shared source files.
    ```csharp
    // Good for multi-target shared source: block-scoped namespace
    namespace MyNamespace
    {
        public class MyClass
        {
            // Implementation
        }
    }

    // Avoid in multi-target shared source: file-scoped namespace
    namespace MyNamespace;
    ```
