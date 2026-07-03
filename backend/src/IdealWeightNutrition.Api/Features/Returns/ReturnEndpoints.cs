using FastEndpoints;
using IdealWeightNutrition.Api.Http;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Returns;
using AuthPolicies = IdealWeightNutrition.Domain.Constants.Policies;

namespace IdealWeightNutrition.Api.Features.Returns;

public sealed class CreateReturnEndpoint : Endpoint<CreateReturnRequest, ReturnRequestDto>
{
    private readonly IReturnService _returns;

    public CreateReturnEndpoint(IReturnService returns) => _returns = returns;

    public override void Configure()
    {
        Post("returns");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateReturnRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _returns.CreateReturnAsync(req, CartHttp.GetUserId(User), ct);
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class ListMyReturnsEndpoint : EndpointWithoutRequest<IReadOnlyList<ReturnListItemDto>>
{
    private readonly IReturnService _returns;

    public ListMyReturnsEndpoint(IReturnService returns) => _returns = returns;

    public override void Configure()
    {
        Get("returns");
        Policies(AuthPolicies.Customer);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = CartHttp.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var items = await _returns.ListUserReturnsAsync(userId, ct);
        await Send.OkAsync(items, ct);
    }
}

public sealed class GetReturnEndpoint : EndpointWithoutRequest<ReturnRequestDto>
{
    private readonly IReturnService _returns;

    public GetReturnEndpoint(IReturnService returns) => _returns = returns;

    public override void Configure()
    {
        Get("returns/{returnId}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("returnId"), out var returnId) || returnId <= 0)
        {
            ThrowError("Invalid return id.", StatusCodes.Status400BadRequest);
            return;
        }

        var email = Query<string>("email");
        var result = await _returns.GetReturnAsync(returnId, CartHttp.GetUserId(User), email, ct);
        if (result is null)
            ThrowError("Return request not found.", StatusCodes.Status404NotFound);
        else
            await Send.OkAsync(result, ct);
    }
}
