using FastEndpoints;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Content;

namespace IdealWeightNutrition.Api.Features.Content;

public sealed class ListBlogPostsEndpoint : EndpointWithoutRequest<IReadOnlyList<BlogPostSummaryDto>>
{
    private readonly IBlogService _blog;

    public ListBlogPostsEndpoint(IBlogService blog) => _blog = blog;

    public override void Configure()
    {
        Get("blog");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var posts = await _blog.ListPublishedAsync(ct);
        await Send.OkAsync(posts, ct);
    }
}

public sealed class GetBlogPostEndpoint : EndpointWithoutRequest<BlogPostDetailDto>
{
    private readonly IBlogService _blog;

    public GetBlogPostEndpoint(IBlogService blog) => _blog = blog;

    public override void Configure()
    {
        Get("blog/{slug}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var slug = Route<string>("slug");
        if (string.IsNullOrWhiteSpace(slug))
        {
            ThrowError("Invalid blog slug.", StatusCodes.Status400BadRequest);
            return;
        }

        var post = await _blog.GetBySlugAsync(slug, ct);
        if (post is null)
            ThrowError("Blog post not found.", StatusCodes.Status404NotFound);
        else
            await Send.OkAsync(post, ct);
    }
}
