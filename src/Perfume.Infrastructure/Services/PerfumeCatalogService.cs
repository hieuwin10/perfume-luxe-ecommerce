using Microsoft.EntityFrameworkCore;
using Perfume.Application.DTOs;
using Perfume.Application.Interfaces;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;
using Perfume.Infrastructure.Data;

namespace Perfume.Infrastructure.Services;

public class PerfumeCatalogService : IPerfumeCatalogService
{
    private readonly PerfumeDbContext _context;

    public PerfumeCatalogService(PerfumeDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductCardDto>> GetFeaturedProductsAsync()
    {
        return await QueryProductCards(_context.Products.Where(p => p.IsFeatured).Take(8));
    }

    public async Task<List<ProductCardDto>> GetBestSellersAsync()
    {
        return await QueryProductCards(_context.Products.Where(p => p.IsBestSeller).Take(8));
    }

    public async Task<List<ProductCardDto>> GetNewArrivalsAsync()
    {
        return await QueryProductCards(_context.Products.Where(p => p.IsNewArrival).Take(8));
    }

    public async Task<(List<ProductCardDto> Items, int TotalCount)> GetProductsAsync(ProductFilterParams filter)
    {
        var query = _context.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var kw = filter.Keyword.Trim();
            query = query.Where(p => p.Name.Contains(kw) || p.Brand!.Name.Contains(kw) || p.ShortDescription!.Contains(kw));
        }

        if (!string.IsNullOrWhiteSpace(filter.BrandSlug))
        {
            query = query.Where(p => p.Brand != null && p.Brand.Slug == filter.BrandSlug);
        }

        if (!string.IsNullOrWhiteSpace(filter.CategorySlug))
        {
            query = query.Where(p => p.Category != null && p.Category.Slug == filter.CategorySlug);
        }

        if (!string.IsNullOrWhiteSpace(filter.FamilySlug))
        {
            query = query.Where(p => p.FragranceFamily != null && p.FragranceFamily.Slug == filter.FamilySlug);
        }

        if (filter.NoteId.HasValue)
        {
            query = query.Where(p => p.FragranceNotes.Any(fn => fn.FragranceNoteId == filter.NoteId.Value));
        }

        if (filter.Gender.HasValue)
        {
            query = query.Where(p => p.Gender == filter.Gender.Value || p.Gender == GenderTarget.Unisex);
        }

        if (filter.Concentration.HasValue)
        {
            query = query.Where(p => p.Concentration == filter.Concentration.Value);
        }

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(p => p.BasePrice >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(p => p.BasePrice <= filter.MaxPrice.Value);
        }

        int totalCount = await query.CountAsync();

        query = filter.SortBy?.ToLower() switch
        {
            "price_asc" => query.OrderBy(p => p.BasePrice),
            "price_desc" => query.OrderByDescending(p => p.BasePrice),
            "rating" => query.OrderByDescending(p => p.RatingAverage),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        int skip = (Math.Max(1, filter.Page) - 1) * filter.PageSize;
        var paginatedQuery = query.Skip(skip).Take(filter.PageSize);

        var items = await QueryProductCards(paginatedQuery);
        return (items, totalCount);
    }

    public async Task<ProductDetailDto?> GetProductBySlugAsync(string slug)
    {
        var product = await _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.FragranceFamily)
            .Include(p => p.Variants.OrderBy(v => v.VolumeMl))
            .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
            .Include(p => p.FragranceNotes)
                .ThenInclude(fn => fn.FragranceNote)
            .FirstOrDefaultAsync(p => p.Slug == slug);

        return product == null ? null : await MapToProductDetailDtoAsync(product);
    }

    public async Task<ProductDetailDto?> GetProductByIdAsync(int id)
    {
        var product = await _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.FragranceFamily)
            .Include(p => p.Variants.OrderBy(v => v.VolumeMl))
            .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
            .Include(p => p.FragranceNotes)
                .ThenInclude(fn => fn.FragranceNote)
            .FirstOrDefaultAsync(p => p.Id == id);

        return product == null ? null : await MapToProductDetailDtoAsync(product);
    }

    private async Task<ProductDetailDto> MapToProductDetailDtoAsync(Product product)
    {
        var dto = new ProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Sku = product.Sku,
            ShortDescription = product.ShortDescription,
            FullDescription = product.FullDescription,
            BrandName = product.Brand?.Name ?? string.Empty,
            BrandSlug = product.Brand?.Slug ?? string.Empty,
            BrandCountry = product.Brand?.Country,
            CategoryName = product.Category?.Name ?? string.Empty,
            FamilyName = product.FragranceFamily?.Name,
            FamilyColor = product.FragranceFamily?.ColorHex,
            Concentration = product.Concentration,
            Gender = product.Gender,
            Longevity = product.Longevity,
            Sillage = product.Sillage,
            ReleaseYear = product.ReleaseYear,
            SeasonRecommendation = product.SeasonRecommendation,
            BasePrice = product.BasePrice,
            MainImageUrl = product.MainImageUrl,
            RatingAverage = product.RatingAverage,
            RatingCount = product.RatingCount,
            IsInStock = product.IsInStock,
            MetaTitle = product.MetaTitle,
            MetaDescription = product.MetaDescription,
            TopNotes = product.FragranceNotes
                .Where(fn => fn.NoteLevel == NoteLevel.Top && fn.FragranceNote != null)
                .Select(fn => new FragranceNoteDto { Id = fn.FragranceNote!.Id, Name = fn.FragranceNote.Name, Description = fn.FragranceNote.Description })
                .ToList(),
            HeartNotes = product.FragranceNotes
                .Where(fn => fn.NoteLevel == NoteLevel.Heart && fn.FragranceNote != null)
                .Select(fn => new FragranceNoteDto { Id = fn.FragranceNote!.Id, Name = fn.FragranceNote.Name, Description = fn.FragranceNote.Description })
                .ToList(),
            BaseNotes = product.FragranceNotes
                .Where(fn => fn.NoteLevel == NoteLevel.Base && fn.FragranceNote != null)
                .Select(fn => new FragranceNoteDto { Id = fn.FragranceNote!.Id, Name = fn.FragranceNote.Name, Description = fn.FragranceNote.Description })
                .ToList(),
            Variants = product.Variants.OrderBy(v => v.VolumeMl).Select(v => new ProductVariantDto
            {
                Id = v.Id,
                VolumeMl = v.VolumeMl,
                VolumeLabel = v.VolumeLabel,
                VariantSku = v.VariantSku,
                OriginalPrice = v.OriginalPrice,
                SalePrice = v.SalePrice,
                StockQuantity = v.StockQuantity,
                IsDefault = v.IsDefault
            }).ToList(),
            GalleryImages = product.Images.OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).ToList()
        };

        var now = DateTime.UtcNow;
        var activeFlashItem = await _context.FlashSaleItems
            .Include(i => i.FlashSale)
            .Where(i => i.ProductId == product.Id
                && i.FlashSale != null
                && i.FlashSale.IsActive
                && i.FlashSale.StartTime <= now
                && i.FlashSale.EndTime > now
                && i.SoldQuantity < i.StockLimit)
            .OrderBy(i => i.FlashSalePrice)
            .FirstOrDefaultAsync();

        if (activeFlashItem != null)
        {
            dto.IsFlashSale = true;
            dto.FlashSalePrice = activeFlashItem.FlashSalePrice;
            dto.FlashSaleDiscountPercent = dto.BasePrice > 0
                ? (int)Math.Round((1m - (activeFlashItem.FlashSalePrice / dto.BasePrice)) * 100m)
                : 0;
            dto.FlashSaleEndTime = activeFlashItem.FlashSale?.EndTime;
            dto.FlashSaleStockLimit = activeFlashItem.StockLimit;
            dto.FlashSaleSoldQuantity = activeFlashItem.SoldQuantity;
        }

        return dto;
    }

    public async Task<List<Brand>> GetBrandsAsync()
    {
        return await _context.Brands.OrderBy(b => b.Name).ToListAsync();
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        return await _context.Categories.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<List<FragranceFamily>> GetFragranceFamiliesAsync()
    {
        return await _context.FragranceFamilies.OrderBy(f => f.Name).ToListAsync();
    }

    public async Task<List<FragranceNote>> GetFragranceNotesAsync()
    {
        return await _context.FragranceNotes.OrderBy(n => n.Name).ToListAsync();
    }

    public async Task<QuizRecommendationResultDto> GetQuizRecommendationsAsync(QuizAnswerRequest request)
    {
        var allProducts = await _context.Products
            .Include(p => p.Brand)
            .Include(p => p.FragranceFamily)
            .Include(p => p.FragranceNotes).ThenInclude(fn => fn.FragranceNote)
            .Where(p => !p.IsDeleted)
            .ToListAsync();

        if (!allProducts.Any())
        {
            return new QuizRecommendationResultDto
            {
                ArchetypeTitle = "The Ethereal Luminary",
                ArchetypeSubtitle = "Ánh Sáng Tinh Khôi",
                ArchetypeDescription = "Hồ sơ mùi hương cá nhân hóa đang được cập nhật."
            };
        }

        // 1. Chấm điểm ma trận đa chiều cho từng sản phẩm
        var scoredList = allProducts.Select(p =>
        {
            var rawScore = CalculateRawMatchScore(p, request);
            return new { Product = p, RawScore = rawScore };
        })
        .OrderByDescending(x => x.RawScore)
        .ToList();

        // 2. Lấy top 4 sản phẩm điểm cao nhất và chuẩn hóa Match Score % (88% - 99%)
        var topScored = scoredList.Take(4).ToList();
        var maxRaw = topScored.FirstOrDefault()?.RawScore ?? 100;
        if (maxRaw <= 0) maxRaw = 100;

        var items = new List<QuizProductRecommendationDto>();
        for (int i = 0; i < topScored.Count; i++)
        {
            var item = topScored[i];
            var p = item.Product;
            
            // Tính toán Match Score % theo thang tiệm cận chuẩn luxury: 98% -> 94% -> 91% -> 88%
            int percentageScore;
            if (i == 0) percentageScore = Math.Min(99, Math.Max(95, 90 + (int)(item.RawScore * 9.0 / maxRaw)));
            else if (i == 1) percentageScore = Math.Min(95, Math.Max(92, 88 + (int)(item.RawScore * 7.0 / maxRaw)));
            else if (i == 2) percentageScore = Math.Min(92, Math.Max(89, 85 + (int)(item.RawScore * 6.0 / maxRaw)));
            else percentageScore = Math.Min(89, Math.Max(85, 82 + (int)(item.RawScore * 6.0 / maxRaw)));

            var matchReason = GenerateMatchReason(p, request);

            items.Add(new QuizProductRecommendationDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                Sku = p.Sku,
                BrandName = p.Brand?.Name ?? "Haute Parfumerie",
                BrandCountry = p.Brand?.Country,
                MainImageUrl = p.MainImageUrl,
                BasePrice = p.BasePrice,
                FamilyName = p.FragranceFamily?.Name,
                FamilyColor = p.FragranceFamily?.ColorHex,
                Concentration = p.Concentration,
                Gender = p.Gender,
                RatingAverage = p.RatingAverage,
                RatingCount = p.RatingCount,
                Longevity = p.Longevity,
                Sillage = p.Sillage,
                MatchScore = percentageScore,
                MatchReason = matchReason,
                TopNotes = p.FragranceNotes
                    .Where(fn => fn.NoteLevel == NoteLevel.Top && fn.FragranceNote != null)
                    .Select(fn => fn.FragranceNote!.Name)
                    .Take(3)
                    .ToList()
            });
        }

        // 3. Sinh hồ sơ Scent Archetype độc bản
        var archetype = GenerateArchetype(request);
        archetype.Items = items;

        return archetype;
    }

    private static double CalculateRawMatchScore(Product p, QuizAnswerRequest req)
    {
        double score = 0;

        // 1. Phù hợp Giới tính (Max 25đ)
        if (req.GenderPreference == "men")
        {
            if (p.Gender == GenderTarget.Men) score += 25;
            else if (p.Gender == GenderTarget.Unisex) score += 20;
            else score += 5;
        }
        else if (req.GenderPreference == "women")
        {
            if (p.Gender == GenderTarget.Women) score += 25;
            else if (p.Gender == GenderTarget.Unisex) score += 20;
            else score += 5;
        }
        else // unisex
        {
            if (p.Gender == GenderTarget.Unisex) score += 25;
            else score += 18;
        }

        // 2. Phong thái / Vibe & Scent Family (Max 30đ)
        var scentType = p.FragranceFamily?.ScentType;
        if (req.Vibe == "luxury")
        {
            if (scentType == ScentFamily.Leather || scentType == ScentFamily.Woody || scentType == ScentFamily.Oriental) score += 30;
            else if (scentType == ScentFamily.Gourmand || scentType == ScentFamily.Citrus) score += 20;
            else score += 12;
        }
        else if (req.Vibe == "cozy")
        {
            if (scentType == ScentFamily.Woody || scentType == ScentFamily.Oriental) score += 30;
            else if (scentType == ScentFamily.Gourmand || scentType == ScentFamily.Leather) score += 22;
            else score += 10;
        }
        else if (req.Vibe == "fresh")
        {
            if (scentType == ScentFamily.Fresh || scentType == ScentFamily.Citrus || scentType == ScentFamily.Aromatic) score += 30;
            else if (scentType == ScentFamily.Floral) score += 18;
            else score += 10;
        }
        else if (req.Vibe == "romantic")
        {
            if (scentType == ScentFamily.Floral || scentType == ScentFamily.Gourmand) score += 30;
            else if (scentType == ScentFamily.Oriental || scentType == ScentFamily.Woody) score += 20;
            else score += 10;
        }
        else if (req.Vibe == "mysterious")
        {
            if (scentType == ScentFamily.Oriental || scentType == ScentFamily.Leather || scentType == ScentFamily.Woody) score += 30;
            else score += 15;
        }
        else
        {
            score += 20;
        }

        // 3. Hoàn cảnh / Occasion (Max 15đ)
        if (req.SeasonOrOccasion == "evening" || req.SeasonOrOccasion == "special")
        {
            if (scentType == ScentFamily.Oriental || scentType == ScentFamily.Woody || scentType == ScentFamily.Leather || p.Concentration >= Concentration.EDP) score += 15;
            else score += 8;
        }
        else if (req.SeasonOrOccasion == "daily")
        {
            if (scentType == ScentFamily.Fresh || scentType == ScentFamily.Citrus || scentType == ScentFamily.Aromatic || scentType == ScentFamily.Floral) score += 15;
            else score += 8;
        }
        else // all
        {
            if (p.SeasonRecommendation?.Contains("Bốn mùa", StringComparison.OrdinalIgnoreCase) == true || scentType == ScentFamily.Woody || scentType == ScentFamily.Fresh) score += 15;
            else score += 10;
        }

        // 4. Cường độ / Độ lưu tỏa (Max 15đ)
        if (req.IntensityPreference == "intense")
        {
            if (p.Concentration >= Concentration.Parfum || p.Longevity?.Contains("12", StringComparison.OrdinalIgnoreCase) == true) score += 15;
            else if (p.Concentration == Concentration.EDP) score += 11;
            else score += 5;
        }
        else if (req.IntensityPreference == "strong")
        {
            if (p.Concentration >= Concentration.EDP) score += 15;
            else score += 9;
        }
        else // moderate
        {
            if (p.Concentration <= Concentration.EDT || scentType == ScentFamily.Fresh || scentType == ScentFamily.Citrus) score += 15;
            else if (p.Concentration == Concentration.EDP) score += 10;
            else score += 6;
        }

        // 5. Ngân sách & Định dạng (Max 10đ)
        if (req.BudgetPreference == "high")
        {
            if (p.BasePrice >= 5000000 || p.Brand?.Name.Contains("Creed", StringComparison.OrdinalIgnoreCase) == true || p.Brand?.Name.Contains("Kilian", StringComparison.OrdinalIgnoreCase) == true || p.Brand?.Name.Contains("Tom Ford", StringComparison.OrdinalIgnoreCase) == true) score += 10;
            else score += 6;
        }
        else if (req.BudgetPreference == "sample")
        {
            // Chai có giá gốc hợp lý hoặc có thể chiết
            if (p.BasePrice <= 4000000) score += 10;
            else score += 7;
        }
        else // mid
        {
            if (p.BasePrice >= 3000000 && p.BasePrice <= 6000000) score += 10;
            else score += 6;
        }

        // 6. Điểm chất lượng & Đánh giá (Max 5đ)
        if (p.RatingAverage >= 4.8) score += 5;
        else if (p.RatingAverage >= 4.5) score += 3;

        if (p.IsBestSeller || p.IsFeatured) score += 2;

        return score;
    }

    private static string GenerateMatchReason(Product p, QuizAnswerRequest req)
    {
        var brandName = p.Brand?.Name ?? "Thương hiệu";
        var familyName = p.FragranceFamily?.Name ?? "Hương Thơm";
        var topNotes = p.FragranceNotes
            .Where(fn => fn.NoteLevel == NoteLevel.Top && fn.FragranceNote != null)
            .Select(fn => fn.FragranceNote!.Name)
            .Take(2)
            .ToList();

        var notesText = topNotes.Any() ? string.Join(" & ", topNotes) : "các nốt hương tinh tuyển";

        if (req.Vibe == "luxury")
        {
            return $"Kiệt tác từ {brandName} mang nốt {notesText} sang trọng, phối cùng nhóm {familyName}. Độ lưu hương {p.Longevity ?? "bền bỉ"} định hình khí chất vương giả hoàn hảo.";
        }
        if (req.Vibe == "cozy")
        {
            return $"Sở hữu nốt {notesText} trầm ấm và nhóm {familyName} mộc mạc, tạo nên cảm giác ấm áp, tinh tế và đáng tin cậy trong mọi khoảnh khắc.";
        }
        if (req.Vibe == "fresh")
        {
            return $"Bừng sáng nét thanh lịch với nốt {notesText} rạng rỡ, hòa quyện nhóm {familyName} mát lành, mang lại sự sảng khoái và tự tin suốt ngày dài.";
        }
        if (req.Vibe == "romantic")
        {
            return $"Sự hòa quyện ngọt ngào của nốt {notesText} cùng nhóm {familyName} nồng thắm, mang đến sức quyến rũ khó cưỡng cho những khoảnh khắc rung động.";
        }
        return $"Tuyệt phẩm cân bằng từ {brandName} với nhóm {familyName} và nốt {notesText}, tương thích trọn vẹn với gu thưởng thức mùi hương độc bản của bạn.";
    }

    private static QuizRecommendationResultDto GenerateArchetype(QuizAnswerRequest req)
    {
        if (req.GenderPreference == "men" && (req.Vibe == "luxury" || req.Vibe == "mysterious" || req.SeasonOrOccasion == "evening" || req.SeasonOrOccasion == "special"))
        {
            return new QuizRecommendationResultDto
            {
                ArchetypeTitle = "The Sovereign Aristocrat",
                ArchetypeSubtitle = "Bậc Đế Vương Quyền Uy & Đẳng Cấp",
                ArchetypeDescription = "Bạn sở hữu khí chất thủ lĩnh bẩm sinh, đĩnh đạc và mang sức hút vương giả khó cưỡng. Hương thơm sinh ra cho bạn là sự kết hợp thượng thừa giữa trầm hương huyền bí, gỗ tuyết tùng trầm ấm và gia vị cay ấm nồng nàn, để lại dư vị quyền lực bất tận trong mọi không gian dạ tiệc thượng lưu.",
                DominantAccords = new List<string> { "Gỗ Trầm Hương Quý (Oud)", "Hổ Phách & Gia Vị Cay Ấm", "Da Thuộc Hoàng Gia" },
                KeyNotes = new List<string> { "Trầm Hương", "Gỗ Tuyết Tùng", "Tiêu Đen Calabre", "Nhục Đậu Khấu" },
                BestOccasions = "Dạ tiệc thượng lưu, gặp gỡ đối tác VIP & những sự kiện trọng đại"
            };
        }

        if (req.GenderPreference == "women" && (req.Vibe == "romantic" || req.Vibe == "luxury" || req.SeasonOrOccasion == "evening"))
        {
            return new QuizRecommendationResultDto
            {
                ArchetypeTitle = "The Velvet Enchantress",
                ArchetypeSubtitle = "Nàng Thơ Mê Hoặc & Kiêu Kỳ",
                ArchetypeDescription = "Bạn toát lên vẻ đẹp quyến rũ kiêu sa, đầy nữ tính và khiến lòng người xao xuyến. Mùi hương hoàn hảo cho bạn là sự giao hòa giữa hoa hồng Damask ngút ngàn, vani bourbon ngọt ngào say đắm và xạ hương trắng mịn màng như dải lụa nhung mềm mại lướt nhẹ trên da.",
                DominantAccords = new List<string> { "Hoa Hồng Damask Xa Xỉ", "Vani Bourbon Béo Ngậy", "Xạ Hương Trắng Nữ Tính" },
                KeyNotes = new List<string> { "Hoa Hồng Grasse", "Vani Bourbon", "Xạ Hương Trắng", "Nhài Sambac" },
                BestOccasions = "Hẹn hò lãng mạn, tiệc tối xa hoa & những đêm huyền diệu"
            };
        }

        if (req.Vibe == "fresh" || req.SeasonOrOccasion == "daily")
        {
            return new QuizRecommendationResultDto
            {
                ArchetypeTitle = "The Ethereal Luminary",
                ArchetypeSubtitle = "Ánh Sáng Tinh Khôi & Thanh Lịch",
                ArchetypeDescription = "Bạn mang năng lượng tươi sáng, tự do và phóng khoáng như làn gió Địa Trung Hải sớm mai. Bản giao hưởng mùi hương cam chanh Bergamot rạng rỡ, thảo mộc tươi mát và hổ phách thanh sạch giúp bạn luôn tự tin, bừng sáng vẻ thanh tao trong từng bước đi.",
                DominantAccords = new List<string> { "Cam Chanh Bergamot Tươi Mới", "Thảo Mộc Sảng Khoái", "Hổ Phách Thanh Sạch" },
                KeyNotes = new List<string> { "Cam Bergamot Calabre", "Gió Biển Mặn Mòi", "Gỗ Tuyết Tùng Tươi", "Hoa Cam Neroli" },
                BestOccasions = "Môi trường công sở đẳng cấp, gặp gỡ hằng ngày & những ngày nắng rạng rỡ"
            };
        }

        if (req.Vibe == "cozy" || req.Vibe == "mysterious")
        {
            return new QuizRecommendationResultDto
            {
                ArchetypeTitle = "The Mystic Alchemist",
                ArchetypeSubtitle = "Nhà Giả Kim Bí Ẩn & Sâu Sắc",
                ArchetypeDescription = "Bạn là người có chiều sâu nội tâm, phong thái điềm tĩnh và khó đoán định. Hương thơm của gỗ đàn hương quý hiếm, rượu cognac ủ lâu năm và nốt khói huyền ảo sẽ tôn vinh trọn vẹn nét cuốn hút bí ẩn, khiến người đối diện ấn tượng sâu sắc không thể nào lãng quên.",
                DominantAccords = new List<string> { "Gỗ Đàn Hương Ấm Áp", "Rượu Cognac Lên Men", "Khói Thuốc Lá & Vani" },
                KeyNotes = new List<string> { "Gỗ Đàn Hương Mysore", "Đậu Tonka", "Rượu Rum & Quế", "Hoắc Hương" },
                BestOccasions = "Tiệc tối thân mật, không gian lounge thư giãn & những đêm se lạnh"
            };
        }

        // Mặc định hoặc Unisex Luxury
        return new QuizRecommendationResultDto
        {
            ArchetypeTitle = "The Avant-Garde Connoisseur",
            ArchetypeSubtitle = "Nhà Giám Tuyển Độc Bản Thời Thượng",
            ArchetypeDescription = "Bạn không hòa lẫn vào đám đông; bạn định hình phong cách bằng gu thưởng thức độc bản, vượt qua mọi giới hạn định kiến. Mùi hương dành cho bạn là kiệt tác Niche phá cách, táo bạo và để lại chữ ký mùi hương sang trọng khó nhầm lẫn ở bất cứ nơi đâu bạn xuất hiện.",
            DominantAccords = new List<string> { "Da Thuộc & Trái Cây Hoàng Gia", "Hổ Phách Vàng Lấp Lánh", "Gỗ Quý Hiếm" },
            KeyNotes = new List<string> { "Dứa Khói Hoàng Gia", "Bạch Dương & Hoắc Hương", "Long Diên Hương", "Xạ Hương" },
            BestOccasions = "Mọi khoảnh khắc bạn muốn khẳng định dấu ấn cá nhân độc nhất vô nhị"
        };
    }

    private async Task<List<ProductCardDto>> QueryProductCards(IQueryable<Product> query)
    {
        var cards = await query
            .Select(p => new ProductCardDto
            {
                Id = p.Id,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                Name = p.Name,
                Slug = p.Slug,
                Sku = p.Sku,
                BrandName = p.Brand != null ? p.Brand.Name : string.Empty,
                BrandCountry = p.Brand != null ? p.Brand.Country : null,
                MainImageUrl = p.MainImageUrl,
                BasePrice = p.BasePrice,
                FamilyName = p.FragranceFamily != null ? p.FragranceFamily.Name : null,
                FamilyColor = p.FragranceFamily != null ? p.FragranceFamily.ColorHex : null,
                Concentration = p.Concentration,
                Gender = p.Gender,
                RatingAverage = p.RatingAverage,
                RatingCount = p.RatingCount,
                IsNewArrival = p.IsNewArrival,
                IsBestSeller = p.IsBestSeller,
                TopNotes = p.FragranceNotes
                    .Where(fn => fn.NoteLevel == NoteLevel.Top && fn.FragranceNote != null)
                    .Select(fn => fn.FragranceNote!.Name)
                    .Take(3)
                    .ToList()
            })
            .ToListAsync();

        if (cards.Any())
        {
            var now = DateTime.UtcNow;
            var productIds = cards.Select(c => c.Id).ToList();
            var activeItems = await _context.FlashSaleItems
                .Include(i => i.FlashSale)
                .Where(i => productIds.Contains(i.ProductId)
                    && i.FlashSale != null
                    && i.FlashSale.IsActive
                    && i.FlashSale.StartTime <= now
                    && i.FlashSale.EndTime > now
                    && i.SoldQuantity < i.StockLimit)
                .ToListAsync();

            foreach (var card in cards)
            {
                var flashItem = activeItems.FirstOrDefault(i => i.ProductId == card.Id);
                if (flashItem != null)
                {
                    card.IsFlashSale = true;
                    card.FlashSalePrice = flashItem.FlashSalePrice;
                    card.FlashSaleDiscountPercent = card.BasePrice > 0
                        ? (int)Math.Round((1m - (flashItem.FlashSalePrice / card.BasePrice)) * 100m)
                        : 0;
                }
            }
        }

        return cards;
    }
}
