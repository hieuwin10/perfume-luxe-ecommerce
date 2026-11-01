namespace Perfume.Application.Common;

public static class CommercePolicyDefaults
{
    public const decimal FreeShippingThreshold = 2_000_000m;
    public const decimal DefaultShippingFee = 35_000m;
    public const decimal GiftWrappingFee = 50_000m;

    public static decimal CalculateShippingFee(decimal subTotal)
    {
        if (subTotal <= 0m)
        {
            return 0m;
        }

        return subTotal >= FreeShippingThreshold ? 0m : DefaultShippingFee;
    }

    public static decimal CalculateGiftWrappingFee(bool isGiftWrapping)
    {
        return isGiftWrapping ? GiftWrappingFee : 0m;
    }
}
