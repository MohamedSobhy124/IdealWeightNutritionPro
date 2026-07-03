using FastEndpoints;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using AuthPolicies = IdealWeightNutrition.Domain.Constants.Policies;

namespace IdealWeightNutrition.Api.Features.Admin;

public sealed class ListAdminCompaniesEndpoint : EndpointWithoutRequest<IReadOnlyList<AdminCompanyDto>>
{
    private readonly IAdminCompanyService _companies;
    public ListAdminCompaniesEndpoint(IAdminCompanyService companies) => _companies = companies;
    public override void Configure() { Get("admin/companies"); Policies(AuthPolicies.Admin); }
    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(await _companies.ListAsync(ct), ct);
}

public sealed class GetAdminCompanyEndpoint : EndpointWithoutRequest<AdminCompanyDto>
{
    private readonly IAdminCompanyService _companies;
    public GetAdminCompanyEndpoint(IAdminCompanyService companies) => _companies = companies;
    public override void Configure() { Get("admin/companies/{id}"); Policies(AuthPolicies.Admin); }
    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("id"), out var id) || id <= 0)
            ThrowError("Invalid company id.", StatusCodes.Status400BadRequest);
        var company = await _companies.GetAsync(id, ct);
        if (company is null) ThrowError("Company not found.", StatusCodes.Status404NotFound);
        else await Send.OkAsync(company, ct);
    }
}

public sealed class CreateAdminCompanyEndpoint : Endpoint<UpsertAdminCompanyRequest, AdminCompanyDto>
{
    private readonly IAdminCompanyService _companies;
    public CreateAdminCompanyEndpoint(IAdminCompanyService companies) => _companies = companies;
    public override void Configure() { Post("admin/companies"); Policies(AuthPolicies.Admin); }
    public override async Task HandleAsync(UpsertAdminCompanyRequest req, CancellationToken ct)
    {
        try
        {
            var company = await _companies.CreateAsync(req, ct);
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            await Send.OkAsync(company, ct);
        }
        catch (InvalidOperationException ex) { ThrowError(ex.Message, StatusCodes.Status400BadRequest); }
    }
}

public sealed class UpdateAdminCompanyEndpoint : Endpoint<UpsertAdminCompanyRequest, AdminCompanyDto>
{
    private readonly IAdminCompanyService _companies;
    public UpdateAdminCompanyEndpoint(IAdminCompanyService companies) => _companies = companies;
    public override void Configure() { Put("admin/companies/{id}"); Policies(AuthPolicies.Admin); }
    public override async Task HandleAsync(UpsertAdminCompanyRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("id"), out var id) || id <= 0)
            ThrowError("Invalid company id.", StatusCodes.Status400BadRequest);
        try { await Send.OkAsync(await _companies.UpdateAsync(id, req, ct), ct); }
        catch (InvalidOperationException ex) { ThrowError(ex.Message, StatusCodes.Status400BadRequest); }
    }
}

public sealed class DeleteAdminCompanyEndpoint : EndpointWithoutRequest
{
    private readonly IAdminCompanyService _companies;
    public DeleteAdminCompanyEndpoint(IAdminCompanyService companies) => _companies = companies;
    public override void Configure() { Delete("admin/companies/{id}"); Policies(AuthPolicies.Admin); }
    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("id"), out var id) || id <= 0)
            ThrowError("Invalid company id.", StatusCodes.Status400BadRequest);
        try { await _companies.DeleteAsync(id, ct); await Send.NoContentAsync(ct); }
        catch (InvalidOperationException ex) { ThrowError(ex.Message, StatusCodes.Status400BadRequest); }
    }
}

public sealed class ListAdminUsersEndpoint : EndpointWithoutRequest<IReadOnlyList<AdminUserListItemDto>>
{
    private readonly IAdminUserService _users;
    public ListAdminUsersEndpoint(IAdminUserService users) => _users = users;
    public override void Configure() { Get("admin/users"); Policies(AuthPolicies.Admin); }
    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(await _users.ListAsync(ct), ct);
}

public sealed class CreateAdminUserEndpoint : Endpoint<CreateAdminUserRequest, CreateAdminUserResponse>
{
    private readonly IAdminUserService _users;
    public CreateAdminUserEndpoint(IAdminUserService users) => _users = users;
    public override void Configure() { Post("admin/users"); Policies(AuthPolicies.Admin); }
    public override async Task HandleAsync(CreateAdminUserRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _users.CreateAsync(req, ct);
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex) { ThrowError(ex.Message, StatusCodes.Status400BadRequest); }
    }
}
