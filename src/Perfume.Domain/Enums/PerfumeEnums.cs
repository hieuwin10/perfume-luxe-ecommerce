namespace Perfume.Domain.Enums;

public enum NoteLevel
{
    Top = 1,    // Hương đầu (10 - 15 phút đầu)
    Heart = 2,  // Hương giữa / Hương tim (2 - 4 giờ)
    Base = 3    // Hương cuối / Hương nền (lưu hương dài nhất 6 - 12 giờ)
}

public enum Concentration
{
    EDC = 1,      // Eau de Cologne (2 - 4%)
    EDT = 2,      // Eau de Toilette (5 - 15%)
    EDP = 3,      // Eau de Parfum (15 - 20%)
    Parfum = 4,   // Parfum / Extrait de Parfum (20 - 40%)
    Oil = 5       // Fragrance Oil / Tinh dầu
}

public enum ScentFamily
{
    Woody = 1,     // Hương gỗ trầm ấm (Tuyết tùng, Đàn hương, Trầm hương)
    Floral = 2,    // Hương hoa cỏ thanh khiết (Hoa hồng, Hoa nhài, Mẫu đơn)
    Oriental = 3,  // Hương phương đông quyến rũ (Hổ phách, Vani, Gia vị ấm)
    Fresh = 4,     // Hương tươi mát sảng khoái (Biển cả, Không khí mát)
    Citrus = 5,    // Hương cam chanh tươi mới (Bergamot, Quýt, Bưởi)
    Gourmand = 6,  // Hương ngọt ngào thực phẩm (Socola, Cà phê, Caramel)
    Leather = 7,   // Hương da thuộc cá tính, sang trọng
    Aromatic = 8   // Hương thảo mộc cay mát (Oải hương, Xô thơm, Bạc hà)
}

public enum GenderTarget
{
    Unisex = 1,
    Men = 2,
    Women = 3
}

public enum OrderStatus
{
    Pending = 1,      // Chờ duyệt
    Confirmed = 2,    // Đã duyệt & chuẩn bị hàng
    Shipping = 3,     // Đang vận chuyển
    Delivered = 4,    // Giao thành công
    Cancelled = 5     // Đã hủy
}

public enum PaymentMethod
{
    COD = 1,          // Thanh toán khi nhận hàng
    VNPAY = 2,        // Cổng VNPAY QR / Thẻ nội địa
    MoMo = 3,         // Ví điện tử MoMo
    BankTransfer = 4  // Chuyển khoản ngân hàng trực tiếp
}

public enum PaymentStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3,
    Refunded = 4
}

public enum UserTier
{
    Bronze = 1,    // Thành viên Mới / Đồng
    Silver = 2,    // Thành viên Bạc (giảm 3%)
    Gold = 3,      // Thành viên Vàng (giảm 7%)
    Diamond = 4    // Thành viên Kim Cương (giảm 12% + quà độc quyền)
}
