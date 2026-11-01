namespace Perfume.Application.DTOs;

public class CreateProductDto : BaseProductDto
{
    public CreateProductDto()
    {
        ReleaseYear = DateTime.UtcNow.Year;
        SeasonRecommendation = "Bốn mùa, Ngày và Đêm";
        IsNewArrival = true;
    }

    // Biến thể mặc định đầu tiên
    public int InitialVolumeMl { get; set; } = 100;
    public string InitialVolumeLabel { get; set; } = "Chai Full 100ml";
    public int InitialStockQuantity { get; set; } = 20;
}
