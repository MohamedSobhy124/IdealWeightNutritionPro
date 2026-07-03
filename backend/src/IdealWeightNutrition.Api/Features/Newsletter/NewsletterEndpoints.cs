using FastEndpoints;

using IdealWeightNutrition.Api.Http;

using IdealWeightNutrition.Application.Abstractions;

using IdealWeightNutrition.Contracts.Newsletter;

using AuthPolicies = IdealWeightNutrition.Domain.Constants.Policies;



namespace IdealWeightNutrition.Api.Features.Newsletter;



public sealed class SubscribeNewsletterEndpoint : Endpoint<NewsletterSubscribeRequest, NewsletterSubscribeResponse>

{

    private readonly INewsletterService _newsletter;



    public SubscribeNewsletterEndpoint(INewsletterService newsletter) => _newsletter = newsletter;



    public override void Configure()

    {

        Post("newsletter/subscribe");

        AllowAnonymous();

        Options(o => o.RequireRateLimiting(RateLimitPolicies.PublicForms));

    }



    public override async Task HandleAsync(NewsletterSubscribeRequest req, CancellationToken ct)

    {

        var email = ResolveEmail(req.Email);

        if (string.IsNullOrWhiteSpace(email))

        {

            ThrowError("Email is required.", StatusCodes.Status400BadRequest);

            return;

        }



        try

        {

            var result = await _newsletter.SubscribeAsync(email, req.Source ?? "Storefront", ct);

            await Send.OkAsync(result, ct);

        }

        catch (InvalidOperationException ex)

        {

            ThrowError(ex.Message, StatusCodes.Status400BadRequest);

        }

    }



    private string? ResolveEmail(string? requestEmail)

    {

        if (!string.IsNullOrWhiteSpace(requestEmail))

            return requestEmail.Trim();



        if (User.Identity?.IsAuthenticated != true)

            return null;



        return User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value

            ?? User.Identity?.Name;

    }

}



public sealed class GetNewsletterStatusRequest

{

    public string? Email { get; init; }

}



public sealed class GetNewsletterStatusEndpoint : Endpoint<GetNewsletterStatusRequest, NewsletterStatusResponse>

{

    private readonly INewsletterService _newsletter;



    public GetNewsletterStatusEndpoint(INewsletterService newsletter) => _newsletter = newsletter;



    public override void Configure()

    {

        Get("newsletter/status");

        AllowAnonymous();

        Options(o => o.RequireRateLimiting(RateLimitPolicies.PublicForms));

    }



    public override async Task HandleAsync(GetNewsletterStatusRequest req, CancellationToken ct)

    {

        var email = req.Email?.Trim();

        if (string.IsNullOrWhiteSpace(email) && User.Identity?.IsAuthenticated == true)

        {

            email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value

                ?? User.Identity?.Name;

        }



        if (string.IsNullOrWhiteSpace(email))

        {

            ThrowError("Email is required.", StatusCodes.Status400BadRequest);

            return;

        }



        var status = await _newsletter.GetStatusAsync(email, ct);

        await Send.OkAsync(status, ct);

    }

}



public sealed class UnsubscribeNewsletterEndpoint : Endpoint<NewsletterUnsubscribeRequest, NewsletterUnsubscribeResponse>

{

    private readonly INewsletterService _newsletter;



    public UnsubscribeNewsletterEndpoint(INewsletterService newsletter) => _newsletter = newsletter;



    public override void Configure()

    {

        Post("newsletter/unsubscribe");

        AllowAnonymous();

        Options(o => o.RequireRateLimiting(RateLimitPolicies.PublicForms));

    }



    public override async Task HandleAsync(NewsletterUnsubscribeRequest req, CancellationToken ct)

    {

        var email = req.Email?.Trim();

        if (string.IsNullOrWhiteSpace(email) && User.Identity?.IsAuthenticated == true)

        {

            email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value

                ?? User.Identity?.Name;

        }



        if (string.IsNullOrWhiteSpace(email))

        {

            ThrowError("Email is required.", StatusCodes.Status400BadRequest);

            return;

        }



        try

        {

            var result = await _newsletter.UnsubscribeAsync(email, ct);

            await Send.OkAsync(result, ct);

        }

        catch (InvalidOperationException ex)

        {

            ThrowError(ex.Message, StatusCodes.Status400BadRequest);

        }

    }

}


