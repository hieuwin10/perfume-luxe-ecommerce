using Perfume.Application.Interfaces;
using Perfume.Infrastructure.Data;

namespace Perfume.Infrastructure.Services;

/// <summary>Compatibility adapter for existing consumers of ILoyaltyTierService.</summary>
public sealed class LoyaltyTierService(PerfumeDbContext db) : LoyaltyService(db), ILoyaltyTierService
{
}
