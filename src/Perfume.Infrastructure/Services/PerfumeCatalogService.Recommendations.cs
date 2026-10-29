using Microsoft.EntityFrameworkCore;
using Perfume.Application.DTOs;
using Perfume.Domain.Entities;
using Perfume.Domain.Enums;

namespace Perfume.Infrastructure.Services;

/// <summary>
/// Service de xuat mui huong thong minh (AI Scent Quiz Recommendation Engine).
/// </summary>
public partial class PerfumeCatalogService
{
    // ==========================================
    // 1. DE XUAT MUI HUONG THEO KET QUA KHAO SAT (QUIZ)
    // ==========================================
    public async Task<QuizRecommendationResultDto> GetQuizRecommendationsAsync(QuizAnswerRequest request)
    {
        var allProducts = await _context.Products.AsNoTracking().AsSplitQuery()
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

        // Buoc 1: Cham diem ma tran da chieu cho tung san pham
        var scoredList = allProducts.Select(p =>
        {
            var rawScore = CalculateRawMatchScore(p, request);
            return new { Product = p, RawScore = rawScore };
        })
        .OrderByDescending(x => x.RawScore)
        .ToList();

        // Buoc 2: Lay top 4 san pham diem cao nhat va chuan hoa Match Score % (88% - 99%)
        var topScored = scoredList.Take(4).ToList();
        var maxRaw = topScored.FirstOrDefault()?.RawScore ?? 100;
        if (maxRaw <= 0) maxRaw = 100;

        var items = new List<QuizProductRecommendationDto>();
        for (int i = 0; i < topScored.Count; i++)
        {
            var item = topScored[i];
            var p = item.Product;

            // Tinh toan Match Score % theo thang tiem can chuan luxury: 98% -> 94% -> 91% -> 88%
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

        // Buoc 3: Sinh ho so Scent Archetype doc ban
        var archetype = GenerateArchetype(request);
        archetype.Items = items;

        return archetype;
    }

    // ==========================================
    // 2. TINH DIEM MA TRAN PHU HOP (MATCH SCORE)
    // ==========================================
    private static double CalculateRawMatchScore(Product p, QuizAnswerRequest req)
    {
        double score = 0;

        // Tieu chi 1: Phu hop Gioi tinh (Toi da 25 diem)
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
        else // Unisex
        {
            if (p.Gender == GenderTarget.Unisex) score += 25;
            else score += 18;
        }

        // Tieu chi 2: Phong thai / Vibe & Nhom huong Scent Family (Toi da 30 diem)
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

        // Tieu chi 3: Hoan canh su dung / Occasion (Toi da 15 diem)
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
        else // Dung trong tat ca moi hoan canh
        {
            if (p.SeasonRecommendation?.Contains("Bốn mùa", StringComparison.OrdinalIgnoreCase) == true || scentType == ScentFamily.Woody || scentType == ScentFamily.Fresh) score += 15;
            else score += 10;
        }

        // Tieu chi 4: Cuong do / Do luu toa (Toi da 15 diem)
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
        else // Vua phai (moderate)
        {
            if (p.Concentration <= Concentration.EDT || scentType == ScentFamily.Fresh || scentType == ScentFamily.Citrus) score += 15;
            else if (p.Concentration == Concentration.EDP) score += 10;
            else score += 6;
        }

        // Tieu chi 5: Ngan sach & Dinh dang gia (Toi da 10 diem)
        if (req.BudgetPreference == "high")
        {
            if (p.BasePrice >= 5000000 || p.Brand?.Name.Contains("Creed", StringComparison.OrdinalIgnoreCase) == true || p.Brand?.Name.Contains("Kilian", StringComparison.OrdinalIgnoreCase) == true || p.Brand?.Name.Contains("Tom Ford", StringComparison.OrdinalIgnoreCase) == true) score += 10;
            else score += 6;
        }
        else if (req.BudgetPreference == "sample")
        {
            // Chai co muc gia hop ly hoac co the thu chiet
            if (p.BasePrice <= 4000000) score += 10;
            else score += 7;
        }
        else // Muc trung binh (mid)
        {
            if (p.BasePrice >= 3000000 && p.BasePrice <= 6000000) score += 10;
            else score += 6;
        }

        // Tieu chi 6: Danh gia nguoi dung va san pham tieu bieu (Toi da 5 diem)
        if (p.RatingAverage >= 4.8) score += 5;
        else if (p.RatingAverage >= 4.5) score += 3;

        if (p.IsBestSeller || p.IsFeatured) score += 2;

        return score;
    }

    // ==========================================
    // 3. SINH LY DO DE XUAT PHU HOP
    // ==========================================
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

    // ==========================================
    // 4. SINH DANH HIEU SCENT ARCHETYPE DOC BAN
    // ==========================================
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

        // Mac dinh hoac Unisex Luxury
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
}
