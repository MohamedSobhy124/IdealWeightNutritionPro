using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Domain.Time;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime Now => UaeDateTime.Now;

    public DateTime UtcNow => DateTime.UtcNow;
}
