using System.Reflection;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Domain.Catalogue;
using IdealWeightNutrition.Domain.Checkout;
using IdealWeightNutrition.Domain.Constants;
using IdealWeightNutrition.Domain.Identity;
using IdealWeightNutrition.Domain.Promotions;
using IdealWeightNutrition.Infrastructure.Options;
using IdealWeightNutrition.Infrastructure.Persistence;
using IdealWeightNutrition.Infrastructure.Services;
using IdealWeightNutrition.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace IdealWeightNutrition.Application.Tests.Payments;

/// <summary>
/// Creates a throwaway database on SQL Server LocalDB for the run and drops it afterwards.
/// The server is hard-coded to LocalDB on purpose: these tests must never reach a real database.
/// </summary>
public sealed class LocalDbFixture : IAsyncLifetime
{
    public string ConnectionString { get; } =
        $@"Server=(localdb)\MSSQLLocalDB;Database=IwnPaymentClaimTests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

    public AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        await using var db = CreateContext();

        // The production schema comes from the legacy database, and the EF model alone
        // produces cascade paths SQL Server refuses on a fresh database. Cascades are
        // irrelevant to these tests, so the generated script uses NO ACTION instead.
        var script = db.Database.GenerateCreateScript()
            .Replace("ON DELETE CASCADE", "ON DELETE NO ACTION", StringComparison.Ordinal)
            .Replace("ON DELETE SET NULL", "ON DELETE NO ACTION", StringComparison.Ordinal);

        var creator = db.GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator>();
        await creator.CreateAsync();

        var batches = System.Text.RegularExpressions.Regex.Split(
            script,
            @"^\s*GO\s*$",
            System.Text.RegularExpressions.RegexOptions.Multiline);
        foreach (var batch in batches.Where(b => !string.IsNullOrWhiteSpace(b)))
            await db.Database.ExecuteSqlRawAsync(batch);
    }

    public async Task DisposeAsync()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
    }
}

[Trait("Category", "Database")]
public sealed class PaymentClaimDatabaseTests : IClassFixture<LocalDbFixture>
{
    private const int InitialStock = 10;
    private const int LineQuantity = 2;

    private readonly LocalDbFixture _fixture;

    public PaymentClaimDatabaseTests(LocalDbFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Concurrent_confirmations_deduct_stock_and_record_promo_once()
    {
        var seed = await SeedAsync(PaymentStatuses.Pending, OrderStatuses.Pending);

        // Eight "requests" (return page, webhook, background job...) confirm at once,
        // each with its own DbContext like real scoped requests.
        var attempts = Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
        {
            await using var db = _fixture.CreateContext();
            var order = await db.OrderHeaders.SingleAsync(o => o.Id == seed.OrderId);
            await CreatePaymentService(db).MarkPaidAndFulfillAsync(order, null, null, CancellationToken.None);
        }));
        await Task.WhenAll(attempts);

        await using var check = _fixture.CreateContext();
        var stored = await check.OrderHeaders.AsNoTracking().SingleAsync(o => o.Id == seed.OrderId);
        Assert.Equal(PaymentStatuses.Paid, stored.PaymentStatus);
        Assert.Equal(OrderStatuses.Paid, stored.OrderStatus);
        Assert.Equal(InitialStock - LineQuantity, await StockAsync(check, seed.ProductId));
        Assert.Equal(1, await check.PromoCodeUsages.CountAsync(u => u.OrderId == seed.OrderId));
        Assert.Equal(1, (await check.PromoCodes.AsNoTracking().SingleAsync(p => p.Id == seed.PromoId)).TimesUsed);
        Assert.Equal(1, await check.OrderAuditLogs.CountAsync(a => a.OrderHeaderId == seed.OrderId && a.Action == "Payment Confirmed"));
    }

    [Fact]
    public async Task Stale_auto_cancel_does_not_cancel_an_order_that_was_paid_meanwhile()
    {
        var seed = await SeedAsync(PaymentStatuses.Pending, OrderStatuses.Pending);

        // The background job loads the order while it is still Pending...
        await using var jobDb = _fixture.CreateContext();
        var staleOrder = await jobDb.OrderHeaders.SingleAsync(o => o.Id == seed.OrderId);

        // ...then the webhook confirms the payment...
        await using (var webhookDb = _fixture.CreateContext())
        {
            var order = await webhookDb.OrderHeaders.SingleAsync(o => o.Id == seed.OrderId);
            await CreatePaymentService(webhookDb).MarkPaidAndFulfillAsync(order, null, null, CancellationToken.None);
        }

        // ...and the job, still holding its stale copy, tries to cancel it.
        await CreatePaymentService(jobDb).CancelUnpaidOrderAsync(staleOrder, CancellationToken.None, "test: expired");

        await using var check = _fixture.CreateContext();
        var stored = await check.OrderHeaders.AsNoTracking().SingleAsync(o => o.Id == seed.OrderId);
        Assert.Equal(PaymentStatuses.Paid, stored.PaymentStatus);
        Assert.Equal(OrderStatuses.Paid, stored.OrderStatus);
        Assert.Equal(InitialStock - LineQuantity, await StockAsync(check, seed.ProductId));
        Assert.Equal(PaymentStatuses.Paid, staleOrder.PaymentStatus); // the job's copy was refreshed
    }

    [Fact]
    public async Task Late_payment_on_an_auto_cancelled_order_reopens_it_as_paid()
    {
        var seed = await SeedAsync(PaymentStatuses.Rejected, OrderStatuses.Cancelled);

        await using (var db = _fixture.CreateContext())
        {
            var order = await db.OrderHeaders.SingleAsync(o => o.Id == seed.OrderId);
            await CreatePaymentService(db).MarkPaidAndFulfillAsync(order, null, null, CancellationToken.None);
        }

        await using var check = _fixture.CreateContext();
        var stored = await check.OrderHeaders.AsNoTracking().SingleAsync(o => o.Id == seed.OrderId);
        Assert.Equal(PaymentStatuses.Paid, stored.PaymentStatus);
        Assert.Equal(OrderStatuses.Paid, stored.OrderStatus);
        Assert.Equal(InitialStock - LineQuantity, await StockAsync(check, seed.ProductId));
    }

    [Theory]
    [InlineData(PaymentStatuses.Cancelled, OrderStatuses.Cancelled)]   // cancelled by an admin
    [InlineData(PaymentStatuses.Refunded, OrderStatuses.Refunded)]
    [InlineData(PaymentStatuses.PartiallyRefunded, OrderStatuses.Delivered)]
    public async Task Replayed_confirmation_never_reopens_admin_cancelled_or_refunded_orders(string paymentStatus, string orderStatus)
    {
        var seed = await SeedAsync(paymentStatus, orderStatus);

        await using (var db = _fixture.CreateContext())
        {
            var order = await db.OrderHeaders.SingleAsync(o => o.Id == seed.OrderId);
            await CreatePaymentService(db).MarkPaidAndFulfillAsync(order, null, null, CancellationToken.None);
        }

        await using var check = _fixture.CreateContext();
        var stored = await check.OrderHeaders.AsNoTracking().SingleAsync(o => o.Id == seed.OrderId);
        Assert.Equal(paymentStatus, stored.PaymentStatus);
        Assert.Equal(orderStatus, stored.OrderStatus);
        Assert.Equal(InitialStock, await StockAsync(check, seed.ProductId));
        Assert.Equal(0, await check.PromoCodeUsages.CountAsync(u => u.OrderId == seed.OrderId));
    }

    [Fact]
    public async Task Promo_usage_is_recorded_once_and_released_on_auto_cancel()
    {
        var seed = await SeedAsync(PaymentStatuses.Pending, OrderStatuses.Pending);

        await using (var db = _fixture.CreateContext())
        {
            var promos = new PromoCodeService(db, new FixedClock());
            await promos.RecordUsageAsync(seed.PromoId, seed.UserId, seed.OrderId);
            await promos.RecordUsageAsync(seed.PromoId, seed.UserId, seed.OrderId); // replay
        }

        await using (var check = _fixture.CreateContext())
        {
            Assert.Equal(1, await check.PromoCodeUsages.CountAsync(u => u.OrderId == seed.OrderId));
            Assert.Equal(1, (await check.PromoCodes.AsNoTracking().SingleAsync(p => p.Id == seed.PromoId)).TimesUsed);
        }

        await using (var db = _fixture.CreateContext())
        {
            var order = await db.OrderHeaders.SingleAsync(o => o.Id == seed.OrderId);
            await CreatePaymentService(db).CancelUnpaidOrderAsync(order, CancellationToken.None, "test: expired");
        }

        await using var after = _fixture.CreateContext();
        var stored = await after.OrderHeaders.AsNoTracking().SingleAsync(o => o.Id == seed.OrderId);
        Assert.Equal(OrderStatuses.Cancelled, stored.OrderStatus);
        Assert.Equal(PaymentStatuses.Rejected, stored.PaymentStatus);
        Assert.Equal(0, await after.PromoCodeUsages.CountAsync(u => u.OrderId == seed.OrderId));
        Assert.Equal(0, (await after.PromoCodes.AsNoTracking().SingleAsync(p => p.Id == seed.PromoId)).TimesUsed);
    }

    // ------------------------------------------------------------------ helpers

    private sealed record Seed(int OrderId, int ProductId, int PromoId, string UserId);

    private async Task<Seed> SeedAsync(string paymentStatus, string orderStatus)
    {
        await using var db = _fixture.CreateContext();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = $"claim-test-{suffix}",
            NormalizedUserName = $"CLAIM-TEST-{suffix}".ToUpperInvariant(),
            Email = $"claim-test-{suffix}@example.test",
            NormalizedEmail = $"CLAIM-TEST-{suffix}@EXAMPLE.TEST".ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString()
        };
        db.Users.Add(user);

        var category = new Category { Name = "Test", NameAr = "Test", Description = "Test", DescriptionAr = "Test" };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var product = new Product
        {
            Title = $"Claim test {suffix}",
            TitleAr = "Test",
            Description = "Test",
            DescriptionAr = "Test",
            CategryId = category.Id,
            Price = 50,
            ListPrice = 50,
            StockQuantity = InitialStock,
            MinimumStockAlert = 0,
            CreatedDate = DateTime.UtcNow
        };
        db.Products.Add(product);

        var promo = new PromoCode
        {
            Code = $"CLAIM{suffix}".ToUpperInvariant(),
            Description = "Test",
            DiscountType = DiscountType.FixedAmount,
            DiscountValue = 5,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1),
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };
        db.PromoCodes.Add(promo);
        await db.SaveChangesAsync();

        var order = new OrderHeader
        {
            ApplicationUserId = user.Id,
            Email = user.Email,
            Name = "Claim Test",
            PhoneNumber = "0500000000",
            StreetAddress = "Test",
            City = "Dubai",
            State = "Dubai",
            PostalCode = "00000",
            OrderDate = DateTime.UtcNow.AddHours(-1),
            OrderTotal = 100,
            PaymentMethod = PaymentMethods.Tabby,
            PaymentStatus = paymentStatus,
            OrderStatus = orderStatus,
            PromoCodeId = promo.Id,
            Details =
            {
                new OrderDetail { ProductId = product.Id, Count = LineQuantity, Price = 50 }
            }
        };
        db.OrderHeaders.Add(order);
        await db.SaveChangesAsync();

        return new Seed(order.Id, product.Id, promo.Id, user.Id);
    }

    private static Task<int> StockAsync(AppDbContext db, int productId) =>
        db.Products.AsNoTracking().Where(p => p.Id == productId).Select(p => p.StockQuantity).SingleAsync();

    private static PaymentService CreatePaymentService(AppDbContext db)
    {
        var clock = new FixedClock();
        return new PaymentService(
            db,
            NoOp<ICartService>.Create(),
            new InventoryService(
                db,
                clock,
                NoOp<IAdminNotificationService>.Create(),
                NoOp<IStockNotificationFulfillmentService>.Create()),
            new PromoCodeService(db, clock),
            clock,
            MsOptions.Create(new GeideaSettings()),
            MsOptions.Create(new TamaraSettings()),
            MsOptions.Create(new TappySettings()),
            MsOptions.Create(new AppUrlOptions()),
            MsOptions.Create(new PaymentVerificationOptions()),
            NoOp<IOrderNotificationService>.Create(),
            NullLogger<PaymentService>.Instance);
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTime Now => DateTime.UtcNow;
        public DateTime UtcNow => DateTime.UtcNow;
    }
}

/// <summary>Interface stand-in whose methods do nothing and return completed tasks / defaults.</summary>
public class NoOp<T> : DispatchProxy where T : class
{
    public static T Create() => DispatchProxy.Create<T, NoOp<T>>();

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var returnType = targetMethod!.ReturnType;
        if (returnType == typeof(void))
            return null;
        if (returnType == typeof(Task))
            return Task.CompletedTask;
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = returnType.GetGenericArguments()[0];
            var value = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
            return typeof(Task).GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(resultType)
                .Invoke(null, [value]);
        }

        return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
    }
}
