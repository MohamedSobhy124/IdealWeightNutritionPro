using IdealWeightNutrition.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdealWeightNutrition.Infrastructure.Persistence.Configurations;

internal sealed class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        builder.ToTable("BlogPosts");
        builder.HasKey(b => b.Id);
        builder.HasIndex(b => b.Slug).IsUnique();
        builder.HasIndex(b => new { b.IsDeleted, b.PublishedDate });
    }
}
