namespace IdealWeightNutrition.Application.Abstractions;

public interface ISeoService
{
    Task<string> GenerateSitemapXmlAsync(CancellationToken cancellationToken = default);

    string GenerateRobotsTxt();
}
