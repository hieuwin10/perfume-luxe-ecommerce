-- =========================================================================================
-- PERFUME LUXE - MORE REVIEWS FOR JADORE, BLACK OPIUM, SMW, ANGELS SHARE
-- =========================================================================================
SET NOCOUNT ON;

DECLARE @P_Jadore INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'dior-jadore-edp');
DECLARE @P_BlackOpium INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'ysl-black-opium-edp');
DECLARE @P_SMW INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'creed-silver-mountain-water');
DECLARE @P_Angels INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'kilian-angels-share');
DECLARE @P_Jazz INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'replica-jazz-club');
DECLARE @P_TV INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'tom-ford-tobacco-vanille');
DECLARE @P_Santal INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'le-labo-santal-33');
DECLARE @P_Libre INT = (SELECT TOP 1 Id FROM Products WHERE Slug = 'ysl-libre-edp');

INSERT INTO Reviews (ProductId, UserName, Rating, Comment, LongevityRating, IsApproved, CreatedAt, IsDeleted)
VALUES
(@P_Jadore, N'Kim Ngân', 5, N'Mùi hoa vàng quý phái rực rỡ, mẹ mình rất thích món quà này. Cảm ơn shop đóng gói rất đẹp.', N'Tốt (8-10h)', 1, DATEADD(DAY, -7, GETUTCDATE()), 0),
(@P_BlackOpium, N'Khánh Linh', 5, N'Mùi cà phê vani quá đỉnh cho mùa thu đông. Xịt đi hẹn hò người yêu khen suốt.', N'Tốt (8-10h)', 1, DATEADD(DAY, -4, GETUTCDATE()), 0),
(@P_BlackOpium, N'Phương Uyên', 5, N'Chai nước hoa quyến rũ nhất tủ đồ của mình. Mùi hương đậm đặc và giữ mùi qua đêm.', N'Rất lâu (trên 10h)', 1, DATEADD(DAY, -1, GETUTCDATE()), 0),
(@P_SMW, N'Tuấn Kiệt', 5, N'Hương suối băng dãy Alps mát lạnh cực kỳ thư giãn. Đúng chất Niche đẳng cấp Creed.', N'Khá (6-8h)', 1, DATEADD(DAY, -9, GETUTCDATE()), 0),
(@P_Angels, N'Hoàng Anh', 5, N'Mùi hương cognac ngọt ấm hoàn hảo cho những ngày mưa lạnh. Thiết kế vỏ chai pha lê tuyệt tác.', N'Rất lâu (trên 12h)', 1, DATEADD(DAY, -12, GETUTCDATE()), 0),
(@P_Jazz, N'Huy Cường', 5, N'Tông mùi ấm áp của quán bar jazz New York, xịt vào là cảm giác chill và thư thái ngay.', N'Khá (6-8h)', 1, DATEADD(DAY, -5, GETUTCDATE()), 0),
(@P_TV, N'Minh Đức', 5, N'Bản Tobacco Vanille này quá kinh điển. Mùi lá thuốc lá quyện vani béo ngậy lưu hương đến tận ngày hôm sau.', N'Rất lâu (trên 12h)', 1, DATEADD(DAY, -14, GETUTCDATE()), 0),
(@P_Santal, N'Thanh Hằng', 5, N'Chai chiết 10ml dùng thử quá mê, chắc chắn tháng sau sẽ nâng cấp lên chai Fullbox 100ml.', N'Rất lâu (trên 10h)', 1, DATEADD(DAY, -2, GETUTCDATE()), 0);

PRINT N'>>> REVIEWS EXTRA XONG! <<<';
