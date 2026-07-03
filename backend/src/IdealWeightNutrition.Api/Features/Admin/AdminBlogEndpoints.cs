using FastEndpoints;
using IdealWeightNutrition.Api.Http;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using AuthPolicies = IdealWeightNutrition.Domain.Constants.Policies;

namespace IdealWeightNutrition.Api.Features.Admin;

public sealed class ListAdminBlogPostsRequest
{
    public bool IncludeDeleted { get; init; }
}

public sealed class ListAdminBlogPostsEndpoint : Endpoint<ListAdminBlogPostsRequest, IReadOnlyList<AdminBlogPostListItemDto>>
{
    private readonly IAdminBlogService _blog;

    public ListAdminBlogPostsEndpoint(IAdminBlogService blog) => _blog = blog;

    public override void Configure()
    {
        Get("admin/blog-posts");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(ListAdminBlogPostsRequest req, CancellationToken ct) =>
        await Send.OkAsync(await _blog.ListAsync(req.IncludeDeleted, ct), ct);
}

public sealed class GetAdminBlogPostEndpoint : EndpointWithoutRequest<AdminBlogPostDetailDto>
{
    private readonly IAdminBlogService _blog;

    public GetAdminBlogPostEndpoint(IAdminBlogService blog) => _blog = blog;

    public override void Configure()
    {
        Get("admin/blog-posts/{postId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("postId"), out var id) || id <= 0)
        {
            ThrowError("Invalid blog post id.", StatusCodes.Status400BadRequest);
            return;
        }

        var post = await _blog.GetAsync(id, ct);
        if (post is null)
            ThrowError("Blog post not found.", StatusCodes.Status404NotFound);
        else
            await Send.OkAsync(post, ct);
    }
}

public sealed class CreateAdminBlogPostEndpoint : Endpoint<UpsertAdminBlogPostRequest, AdminBlogPostDetailDto>
{
    private readonly IAdminBlogService _blog;

    public CreateAdminBlogPostEndpoint(IAdminBlogService blog) => _blog = blog;

    public override void Configure()
    {
        Post("admin/blog-posts");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(UpsertAdminBlogPostRequest req, CancellationToken ct)
    {
        try
        {
            var userId = CartHttp.GetUserId(User);
            var created = await _blog.CreateAsync(req, userId, ct);
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            await Send.OkAsync(created, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class UpdateAdminBlogPostEndpoint : Endpoint<UpsertAdminBlogPostRequest, AdminBlogPostDetailDto>
{
    private readonly IAdminBlogService _blog;

    public UpdateAdminBlogPostEndpoint(IAdminBlogService blog) => _blog = blog;

    public override void Configure()
    {
        Put("admin/blog-posts/{postId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(UpsertAdminBlogPostRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("postId"), out var id) || id <= 0)
        {
            ThrowError("Invalid blog post id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            var userId = CartHttp.GetUserId(User);
            await Send.OkAsync(await _blog.UpdateAsync(id, req, userId, ct), ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class DeleteAdminBlogPostEndpoint : EndpointWithoutRequest
{
    private readonly IAdminBlogService _blog;

    public DeleteAdminBlogPostEndpoint(IAdminBlogService blog) => _blog = blog;

    public override void Configure()
    {
        Delete("admin/blog-posts/{postId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("postId"), out var id) || id <= 0)
        {
            ThrowError("Invalid blog post id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _blog.SoftDeleteAsync(id, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status404NotFound);
        }
    }
}

public sealed class RestoreAdminBlogPostEndpoint : EndpointWithoutRequest
{
    private readonly IAdminBlogService _blog;

    public RestoreAdminBlogPostEndpoint(IAdminBlogService blog) => _blog = blog;

    public override void Configure()
    {
        Post("admin/blog-posts/{postId}/restore");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("postId"), out var id) || id <= 0)
        {
            ThrowError("Invalid blog post id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _blog.RestoreAsync(id, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status404NotFound);
        }
    }
}
