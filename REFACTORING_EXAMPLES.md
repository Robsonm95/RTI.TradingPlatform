# RTI.TradingPlatform - Code Refactoring Examples

This document provides concrete code examples to address the issues identified in the architecture analysis.

---

## 1. Critical Fix: Race Condition Solution

### Current Problematic Code
```csharp
// ExposureService.cs - UNSAFE
public async Task ApplyOrderAsync(string clOrdId, string symbol, char side, 
    int quantity, decimal price, string status, bool accepted)
{
    var tradeDate = DateOnly.FromDateTime(DateTime.UtcNow.Date);
    
    if (accepted)
    {
        var orderValue = quantity * price;
        var currentExposure = await _exposureRepository.GetExposureAsync(symbol, tradeDate);
        var updatedExposure = side == Side.BUY
            ? currentExposure + orderValue
            : currentExposure - orderValue;
        
        // BUG: Another thread could have modified exposure between read and write
        await _exposureRepository.UpdateExposureAsync(symbol, tradeDate, updatedExposure);
    }
    
    var orderEntity = new OrderEntity { ... };
    await _orderRepository.AddOrderAsync(orderEntity);
}
```

### Fixed Version with Transaction
```csharp
public class OrderProcessor
{
    private readonly IDbContextFactory<TradingDbContext> _dbContextFactory;
    private readonly ILogger<OrderProcessor> _logger;
    private readonly IOrderValidator _validator;
    
    public OrderProcessor(
        IDbContextFactory<TradingDbContext> dbContextFactory,
        ILogger<OrderProcessor> logger,
        IOrderValidator validator)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _validator = validator;
    }
    
    public async Task ProcessAsync(NewOrderSingle order, SessionID sessionID)
    {
        using var db = _dbContextFactory.CreateDbContext();
        using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        
        try
        {
            var clOrdId = order.ClOrdID.Value;
            var symbol = order.Symbol.Value;
            var quantity = (int)order.OrderQty.Value;
            var price = order.Price.Value;
            var sideValue = order.Side.Value;
            var side = sideValue == Side.BUY ? OrderSide.Buy : OrderSide.Sell;
            
            _logger.LogInformation("Processing order {ClOrdId} for {Symbol}", clOrdId, symbol);
            
            // Validate order
            var validation = _validator.Validate(order);
            if (!validation.IsValid)
            {
                _logger.LogWarning("Order {ClOrdId} failed validation: {Errors}", 
                    clOrdId, string.Join("; ", validation.Errors));
                
                var rejectionReport = ExecutionReportFactory.CreateRejected(
                    clOrdId, symbol, sideValue, quantity, price, 
                    string.Join("; ", validation.Errors));
                
                Session.SendToTarget(rejectionReport, sessionID);
                await transaction.RollbackAsync();
                return;
            }
            
            var tradeDate = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var orderValue = quantity * price;
            
            // ATOMIC OPERATIONS WITHIN TRANSACTION
            var exposureEntity = await db.Exposures
                .FirstOrDefaultAsync(e => e.Symbol == symbol && e.TradeDate == tradeDate);
            
            var newExposure = exposureEntity?.Exposure ?? 0m;
            newExposure = side == OrderSide.Buy 
                ? newExposure + orderValue 
                : newExposure - orderValue;
            
            var canAccept = Math.Abs(newExposure) <= ExposureLimits.MaxExposure;
            
            if (canAccept)
            {
                // Update exposure
                if (exposureEntity != null)
                {
                    exposureEntity.Exposure = newExposure;
                    exposureEntity.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    db.Exposures.Add(new ExposureEntity
                    {
                        Symbol = symbol,
                        TradeDate = tradeDate,
                        Exposure = newExposure,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                
                _logger.LogInformation("Order {ClOrdId} accepted. Exposure updated to {Exposure:N2}", 
                    clOrdId, newExposure);
            }
            else
            {
                _logger.LogWarning("Order {ClOrdId} rejected. Would exceed limit. " +
                    "Current: {Current:N2}, Order value: {OrderValue:N2}, Limit: {Limit:N2}",
                    clOrdId, exposureEntity?.Exposure ?? 0, orderValue, ExposureLimits.MaxExposure);
            }
            
            // Add order (always recorded, regardless of acceptance)
            db.Orders.Add(new OrderEntity
            {
                ClOrdId = clOrdId,
                Symbol = symbol,
                Side = side.ToString(),
                Quantity = quantity,
                Price = price,
                Status = canAccept ? "ACCEPTED" : "REJECTED",
                TradeDate = tradeDate,
                CreatedAt = DateTime.UtcNow
            });
            
            // Save all changes atomically
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            
            // Send response (after successful save)
            var report = canAccept
                ? ExecutionReportFactory.CreateAccepted(clOrdId, symbol, sideValue, quantity, price)
                : ExecutionReportFactory.CreateRejected(clOrdId, symbol, sideValue, quantity, price,
                    "Exposure limit exceeded");
            
            Session.SendToTarget(report, sessionID);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order");
            await transaction.RollbackAsync();
            throw;
        }
    }
}
```

---

## 2. Thread Safety Fix: Session Manager

### Current Unsafe Code
```csharp
public static class SessionManager
{
    public static SessionID? CurrentSession { get; set; }  // NOT THREAD-SAFE
}

// Usage
if (SessionManager.CurrentSession is null)
    throw new Exception("Not connected");

Session.SendToTarget(order, SessionManager.CurrentSession);  // RACE CONDITION
```

### Thread-Safe Version
```csharp
public class SessionManager
{
    private readonly object _lock = new();
    private SessionID? _currentSession;
    
    public bool IsConnected
    {
        get
        {
            lock (_lock)
            {
                return _currentSession != null;
            }
        }
    }
    
    public SessionID GetCurrentSession()
    {
        lock (_lock)
        {
            return _currentSession ?? throw new InvalidOperationException(
                "FIX session not connected. Please check connection status.");
        }
    }
    
    public void SetCurrentSession(SessionID sessionID)
    {
        lock (_lock)
        {
            _currentSession = sessionID;
        }
    }
    
    public void Clear()
    {
        lock (_lock)
        {
            _currentSession = null;
        }
    }
}

// Register as singleton in DI
builder.Services.AddSingleton<SessionManager>();

// Usage in FixApplication
public class FixApplication : MessageCracker, IApplication
{
    private readonly SessionManager _sessionManager;
    private readonly ILogger<FixApplication> _logger;
    
    public FixApplication(SessionManager sessionManager, ILogger<FixApplication> logger)
    {
        _sessionManager = sessionManager;
        _logger = logger;
    }
    
    public void OnLogon(SessionID sessionID)
    {
        _sessionManager.SetCurrentSession(sessionID);
        _logger.LogInformation("FIX session established: {SessionID}", sessionID);
    }
    
    public void OnLogout(SessionID sessionID)
    {
        _sessionManager.Clear();
        _logger.LogInformation("FIX session closed: {SessionID}", sessionID);
    }
}

// Usage in OrderFixService
public class OrderFixService
{
    private readonly SessionManager _sessionManager;
    private readonly ExecutionReportTracker _tracker;
    
    public async Task<FixExecutionResult> SendOrder(CreateOrderRequest request)
    {
        var session = _sessionManager.GetCurrentSession();  // Throws if not connected
        
        var clOrdId = Guid.NewGuid().ToString();
        var side = request.Side == OrderSide.Buy ? Side.BUY : Side.SELL;
        
        var order = new NewOrderSingle(
            new ClOrdID(clOrdId),
            new Symbol(request.Symbol),
            new Side(side),
            new TransactTime(DateTime.UtcNow),
            new OrdType(OrdType.LIMIT));
        
        order.Set(new OrderQty(request.Quantity));
        order.Set(new Price(request.Price));
        
        var waitTask = _tracker.WaitForExecution(clOrdId);
        
        Session.SendToTarget(order, session);
        
        return await waitTask;
    }
}
```

---

## 3. Fix Sync-Over-Async Anti-Pattern

### Current Code
```csharp
// ExposureService - BLOCKING
public decimal GetExposure(string symbol)
{
    var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
    return _exposureRepository.GetExposureAsync(symbol, today).Result;  // DEADLOCK RISK
}

// Controller - BLOCKING
public IActionResult Get([FromQuery] string? tradeDate)
{
    var date = string.IsNullOrEmpty(tradeDate)
        ? DateOnly.FromDateTime(DateTime.UtcNow.Date)
        : DateOnly.Parse(tradeDate);
    
    var exposures = _exposureService.GetExposureAll(date);  // Calls .Result internally
    return Ok(...);
}

// FixApplication - BLOCKING FIX THREAD
public void OnMessage(NewOrderSingle order, SessionID sessionID)
{
    _processor.ProcessAsync(order, sessionID).Wait();  // BLOCKS PROTOCOL THREAD
}
```

### Fixed Code
```csharp
// ExposureService - ASYNC THROUGHOUT
public async Task<decimal> GetExposureAsync(string symbol)
{
    var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
    return await _exposureRepository.GetExposureAsync(symbol, today);
}

public async Task<Dictionary<string, decimal>> GetExposureAllAsync(DateOnly? tradeDate = null)
{
    var date = tradeDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
    return await _exposureRepository.GetAllExposuresAsync(date);
}

// Controller - ASYNC ENDPOINT
[HttpGet]
public async Task<IActionResult> Get([FromQuery] string? tradeDate)
{
    try
    {
        var date = string.IsNullOrEmpty(tradeDate)
            ? DateOnly.FromDateTime(DateTime.UtcNow.Date)
            : DateOnly.Parse(tradeDate);
        
        var exposures = await _exposureService.GetExposureAllAsync(date);
        
        return Ok(new ExposureResponse
        {
            Exposures = exposures,
            MaxLimit = ExposureLimits.MaxExposure
        });
    }
    catch (FormatException ex)
    {
        return BadRequest(new { error = "Invalid tradeDate format. Use YYYY-MM-DD." });
    }
}

// FixApplication - PROPER ASYNC HANDLING
public class FixApplication : MessageCracker, IApplication
{
    private readonly OrderProcessor _processor;
    private readonly ILogger<FixApplication> _logger;
    
    public FixApplication(OrderProcessor processor, ILogger<FixApplication> logger)
    {
        _processor = processor;
        _logger = logger;
    }
    
    public void FromApp(Message message, SessionID sessionID)
    {
        // Don't block FIX thread - fire and forget with error logging
        _ = ProcessOrderAsync(message, sessionID);
    }
    
    private async Task ProcessOrderAsync(Message message, SessionID sessionID)
    {
        try
        {
            // This runs on thread pool, not FIX protocol thread
            if (message is NewOrderSingle order)
            {
                await _processor.ProcessAsync(order, sessionID);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing FIX message");
        }
    }
    
    public void OnMessage(NewOrderSingle order, SessionID sessionID)
    {
        Crack(order, sessionID);
    }
}
```

---

## 4. Structured Logging Implementation

### Current Console-Only Logging
```csharp
Console.WriteLine($"ORDER ACCEPTED [{clOrdId}]");
Console.WriteLine($"EXPOSURE [{symbol}] = {exposureValue:N2}");
```

### Proper Structured Logging
```csharp
// ServiceCollection configuration in Program.cs
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (!app.Environment.IsDevelopment())
{
    // Add production logging (e.g., Application Insights, Serilog)
    builder.Logging.AddApplicationInsights();
}

// Usage in services
public class OrderProcessor
{
    private readonly ILogger<OrderProcessor> _logger;
    
    public OrderProcessor(ILogger<OrderProcessor> logger)
    {
        _logger = logger;
    }
    
    public async Task ProcessAsync(NewOrderSingle order, SessionID sessionID)
    {
        var clOrdId = order.ClOrdID.Value;
        var symbol = order.Symbol.Value;
        var quantity = order.OrderQty.Value;
        var price = order.Price.Value;
        
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            { "ClOrdId", clOrdId },
            { "Symbol", symbol },
            { "Session", sessionID.ToString() }
        }))
        {
            _logger.LogInformation(
                "Processing order: Symbol={Symbol}, Qty={Quantity}, Price={Price:N4}",
                symbol, quantity, price);
            
            var side = order.Side.Value == Side.BUY ? OrderSide.Buy : OrderSide.Sell;
            var orderValue = quantity * price;
            
            var canAccept = _exposureService.CanAccept(symbol, side, orderValue);
            
            if (canAccept)
            {
                _logger.LogInformation(
                    "Order accepted. New exposure would be {NewExposure:N2}",
                    currentExposure + orderValue);
            }
            else
            {
                _logger.LogWarning(
                    "Order rejected. Would exceed limit. Current={Current:N2}, " +
                    "Order={Order:N2}, Limit={Limit:N2}",
                    currentExposure, orderValue, ExposureLimits.MaxExposure);
            }
        }
    }
}

// Usage in controllers
[HttpGet]
public async Task<IActionResult> Get([FromQuery] string symbol)
{
    using (_logger.BeginScope(new { Symbol = symbol }))
    {
        _logger.LogInformation("Fetching orders for symbol");
        
        try
        {
            var orders = await _exposureService.GetOrdersBySymbolAsync(symbol, today);
            _logger.LogInformation("Retrieved {Count} orders", orders.Count);
            return Ok(...);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders");
            throw;
        }
    }
}
```

---

## 5. Global Error Handling Middleware

### Implement Standard Error Responses
```csharp
// Models/ErrorResponse.cs
public record ErrorResponse(
    int StatusCode,
    string Message,
    string? TraceId = null,
    IDictionary<string, string[]>? Errors = null);

// Middleware/ExceptionHandlingMiddleware.cs
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    
    public ExceptionHandlingMiddleware(RequestDelegate next, 
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        var traceId = context.TraceIdentifier;
        
        var response = exception switch
        {
            InvalidOrderException => new ErrorResponse(
                400, 
                exception.Message,
                traceId),
            
            OrderProcessingException => new ErrorResponse(
                422, 
                "Failed to process order",
                traceId),
            
            FormatException => new ErrorResponse(
                400, 
                "Invalid format",
                traceId),
            
            OperationCanceledException => new ErrorResponse(
                408, 
                "Request timeout",
                traceId),
            
            _ => new ErrorResponse(
                500, 
                "An internal error occurred",
                traceId)
        };
        
        context.Response.StatusCode = response.StatusCode;
        return context.Response.WriteAsJsonAsync(response);
    }
}

// Program.cs
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

---

## 6. Configuration Management

### Move Hardcoded Values to Configuration
```json
// appsettings.json
{
  "Trading": {
    "MaxExposurePerSymbol": 100000000,
    "MaxOrderQuantity": 99999,
    "MaxOrderPrice": 999.99,
    "MinOrderPrice": 0.01,
    "PricePrecision": 0.01,
    "SymbolWhitelist": ["PETR4", "VALE3", "VIIA4"]
  },
  "Cors": {
    "AllowedOrigins": ["https://localhost:5002"],
    "AllowCredentials": false,
    "AllowedMethods": ["GET", "POST"],
    "AllowedHeaders": ["Content-Type"]
  }
}

// appsettings.Development.json
{
  "Trading": {
    "MaxExposurePerSymbol": 100000000
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5000",
      "http://localhost:5071",
      "http://localhost:3000"
    ]
  }
}

// Configuration class
public class TradingConfiguration
{
    public decimal MaxExposurePerSymbol { get; set; } = 100_000_000m;
    public int MaxOrderQuantity { get; set; } = 99_999;
    public decimal MaxOrderPrice { get; set; } = 999.99m;
    public decimal MinOrderPrice { get; set; } = 0.01m;
    public decimal PricePrecision { get; set; } = 0.01m;
    public string[] SymbolWhitelist { get; set; } = [];
}

public class CorsConfiguration
{
    public string[] AllowedOrigins { get; set; } = [];
    public bool AllowCredentials { get; set; }
    public string[] AllowedMethods { get; set; } = [];
    public string[] AllowedHeaders { get; set; } = [];
}

// Program.cs
builder.Services.Configure<TradingConfiguration>(
    builder.Configuration.GetSection("Trading"));

builder.Services.Configure<CorsConfiguration>(
    builder.Configuration.GetSection("Cors"));

var corsConfig = builder.Configuration.GetSection("Cors").Get<CorsConfiguration>()
    ?? throw new InvalidOperationException("CORS configuration missing");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        policy.WithOrigins(corsConfig.AllowedOrigins)
              .WithMethods(corsConfig.AllowedMethods)
              .WithHeaders(corsConfig.AllowedHeaders);
        
        if (corsConfig.AllowCredentials)
            policy.AllowCredentials();
    });
});

// Usage in services
public class OrderProcessor
{
    private readonly IOptions<TradingConfiguration> _config;
    
    public OrderProcessor(IOptions<TradingConfiguration> config)
    {
        _config = config.Value;
    }
    
    public bool CanAcceptOrder(OrderSide side, decimal orderValue, decimal currentExposure)
    {
        var newExposure = side == OrderSide.Buy 
            ? currentExposure + orderValue 
            : currentExposure - orderValue;
        
        return Math.Abs(newExposure) <= _config.MaxExposurePerSymbol;
    }
}
```

---

## 7. Input Validation with Data Annotations

### Enhanced Request DTOs with Validation
```csharp
public class CreateOrderRequest
{
    [Required(ErrorMessage = "Symbol is required")]
    [RegularExpression("^(PETR4|VALE3|VIIA4)$", 
        ErrorMessage = "Symbol must be PETR4, VALE3, or VIIA4")]
    public string Symbol { get; set; } = string.Empty;
    
    [Range(1, 99999, ErrorMessage = "Quantity must be between 1 and 99,999")]
    public int Quantity { get; set; }
    
    [Range(0.01, 999.99, ErrorMessage = "Price must be between 0.01 and 999.99")]
    [DecimalPrecision(2, ErrorMessage = "Price must have at most 2 decimal places")]
    public decimal Price { get; set; }
    
    public OrderSide Side { get; set; }
}

// Custom validation attribute
[AttributeUsage(AttributeTargets.Property)]
public class DecimalPrecisionAttribute : ValidationAttribute
{
    private readonly int _decimals;
    
    public DecimalPrecisionAttribute(int decimals)
    {
        _decimals = decimals;
    }
    
    public override bool IsValid(object? value)
    {
        if (value == null)
            return true;
        
        if (value is not decimal decimalValue)
            return false;
        
        var precisionError = decimal.Parse(decimalValue.ToString("G")) 
            != decimalValue;
        
        return !precisionError && 
            BitConverter.GetBytes(decimal.GetBits(decimalValue)[3])[2] <= _decimals;
    }
}

// Controller with automatic validation
[ApiController]
[Route("api/v1/orders")]
public class OrdersController : ControllerBase
{
    private readonly OrderFixService _orderFixService;
    private readonly ILogger<OrdersController> _logger;
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        // ModelState.IsValid automatically checked by framework
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            
            return BadRequest(new ErrorResponse(
                400, 
                "Validation failed",
                errors: new Dictionary<string, string[]>
                {
                    { "validationErrors", errors.ToArray() }
                }));
        }
        
        try
        {
            _logger.LogInformation("Creating order: {Symbol} {Side} {Qty} @ {Price}",
                request.Symbol, request.Side, request.Quantity, request.Price);
            
            var result = await _orderFixService.SendOrder(request);
            
            return Ok(new OrderResponse
            {
                ClOrdId = result.ClOrdId,
                Success = result.Success,
                Status = result.Status,
                Message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            throw;
        }
    }
}
```

---

## 8. Test Examples

### Unit Tests
```csharp
[TestFixture]
public class ExposureServiceTests
{
    private ExposureService _service;
    private Mock<IDbContextFactory<TradingDbContext>> _mockDbFactory;
    private Mock<TradingDbContext> _mockDb;
    private Mock<DbSet<ExposureEntity>> _mockExposures;
    
    [SetUp]
    public void Setup()
    {
        _mockExposures = new Mock<DbSet<ExposureEntity>>();
        _mockDb = new Mock<TradingDbContext>();
        _mockDb.Setup(x => x.Exposures).Returns(_mockExposures.Object);
        
        _mockDbFactory = new Mock<IDbContextFactory<TradingDbContext>>();
        _mockDbFactory.Setup(x => x.CreateDbContext()).Returns(_mockDb.Object);
        
        _service = new ExposureService(
            new ExposureRepository(_mockDbFactory.Object),
            new OrderRepository(_mockDbFactory.Object));
    }
    
    [Test]
    public async Task ApplyOrderAsync_WhenAccepted_UpdatesExposure()
    {
        // Arrange
        const string symbol = "PETR4";
        const int quantity = 1000;
        const decimal price = 25.50m;
        var tradeDate = DateOnly.FromDateTime(DateTime.Now.Date);
        
        var existingExposure = new ExposureEntity
        {
            Symbol = symbol,
            TradeDate = tradeDate,
            Exposure = 50_000_000m
        };
        
        _mockExposures.Setup(x => 
                x.FirstOrDefaultAsync(It.IsAny<Expression<Func<ExposureEntity, bool>>>(), 
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingExposure);
        
        // Act
        await _service.ApplyOrderAsync(
            "ORD123",
            symbol,
            (char)Side.BUY,
            quantity,
            price,
            "ACCEPTED",
            accepted: true);
        
        // Assert
        _mockDb.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}
```

---

## 9. Enum Instead of Char/String

### Refactored Domain Model
```csharp
public enum OrderSide : byte
{
    Buy = 1,
    Sell = 2
}

public class OrderEntity
{
    public int Id { get; set; }
    public string ClOrdId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public OrderSide Side { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly TradeDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Database mapping
public void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<OrderEntity>(builder =>
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ClOrdId).IsRequired();
        builder.Property(x => x.Symbol).IsRequired();
        builder.Property(x => x.Side)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.HasIndex(x => x.ClOrdId);
    });
}

// DTO with enum
public class OrderDto
{
    public string ClOrdId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public OrderSide Side { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// Service
public async Task ApplyOrderAsync(
    string clOrdId,
    string symbol,
    OrderSide side,  // Type-safe enum
    int quantity,
    decimal price,
    string status,
    bool accepted)
{
    var orderEntity = new OrderEntity
    {
        ClOrdId = clOrdId,
        Symbol = symbol,
        Side = side,  // Direct assignment, no conversion
        Quantity = quantity,
        Price = price,
        Status = status,
        TradeDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
        CreatedAt = DateTime.UtcNow
    };
    
    await _orderRepository.AddOrderAsync(orderEntity);
}
```

---

## 10. Health Checks Implementation

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TradingDbContext>(
        name: "Trading Database",
        failureStatus: HealthStatus.Unhealthy)
    .AddCheck<FIXConnectionHealthCheck>(
        name: "FIX Connection",
        failureStatus: HealthStatus.Unhealthy);

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteHealthCheckResponse
});

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// Custom health check
public class FIXConnectionHealthCheck : IHealthCheck
{
    private readonly SessionManager _sessionManager;
    
    public FIXConnectionHealthCheck(SessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }
    
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_sessionManager.IsConnected)
        {
            return Task.FromResult(HealthCheckResult.Healthy("FIX connection is active"));
        }
        
        return Task.FromResult(HealthCheckResult.Unhealthy("FIX connection is not established"));
    }
}

// Response writer
private static Task WriteHealthCheckResponse(HttpContext context, 
    HealthReport healthReport)
{
    var response = new
    {
        status = healthReport.Status.ToString(),
        timestamp = DateTime.UtcNow,
        checks = healthReport.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            duration = e.Value.Duration,
            description = e.Value.Description,
            data = e.Value.Data
        })
    };
    
    context.Response.ContentType = "application/json";
    return context.Response.WriteAsJsonAsync(response);
}
```

