namespace Perfume.Infrastructure;

internal static class DatabaseSchemaMigrationV5
{
    public const string Version = "20261015_05_seed_ai_radar_seo_blog_posts";
    public const string Sql = @"
DECLARE @CatXuHuong int = (SELECT TOP 1 Id FROM BlogCategories WHERE Slug = 'xu-huong');
IF @CatXuHuong IS NULL
BEGIN
    INSERT INTO BlogCategories (Name, Slug, Description, CreatedAt, IsDeleted)
    VALUES (N'Xu Hướng Mùi Hương', 'xu-huong', N'Cập nhật xu hướng mùi hương độc bản và phong cách nước hoa dẫn đầu', SYSUTCDATETIME(), 0);
    SET @CatXuHuong = SCOPE_IDENTITY();
END;

DECLARE @CatKienThuc int = (SELECT TOP 1 Id FROM BlogCategories WHERE Slug = 'kien-thuc');
IF @CatKienThuc IS NULL
BEGIN
    INSERT INTO BlogCategories (Name, Slug, Description, CreatedAt, IsDeleted)
    VALUES (N'Kiến Thức Nước Hoa', 'kien-thuc', N'Nghệ thuật điều chế, phân biệt và cẩm nang sử dụng nước hoa cao cấp', SYSUTCDATETIME(), 0);
    SET @CatKienThuc = SCOPE_IDENTITY();
END;

DECLARE @CatDanhGia int = (SELECT TOP 1 Id FROM BlogCategories WHERE Slug = 'danh-gia');
IF @CatDanhGia IS NULL
BEGIN
    INSERT INTO BlogCategories (Name, Slug, Description, CreatedAt, IsDeleted)
    VALUES (N'Đánh Giá & So Sánh', 'danh-gia', N'Review chi tiết và so sánh các kiệt tác mùi hương huyền thoại', SYSUTCDATETIME(), 0);
    SET @CatDanhGia = SCOPE_IDENTITY();
END;

DECLARE @CatBoSuuTap int = (SELECT TOP 1 Id FROM BlogCategories WHERE Slug = 'bo-suu-tap');
IF @CatBoSuuTap IS NULL
BEGIN
    INSERT INTO BlogCategories (Name, Slug, Description, CreatedAt, IsDeleted)
    VALUES (N'Bộ Sưu Tập Tuyển Chọn', 'bo-suu-tap', N'Gợi ý các dòng nước hoa biểu tượng theo mùa và phong cách', SYSUTCDATETIME(), 0);
    SET @CatBoSuuTap = SCOPE_IDENTITY();
END;

IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'xu-huong-nuoc-hoa-niche-2026')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Summary, Content, ThumbnailUrl, AuthorName, ViewCount, IsPublished, CategoryId, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Top 5 Xu Hướng Nước Hoa Niche Bùng Nổ Năm 2026 Bạn Cần Biết',
        'xu-huong-nuoc-hoa-niche-2026',
        N'Khám phá 5 xu hướng nước hoa niche 2026 dẫn đầu phong cách sống thượng lưu: nốt hương bền vững, layering độc bản, nguyên liệu sinh thái và tinh dầu nồng độ cao.',
        N'<div class=""blog-content-body"">
<p class=""lead-text"">Thế giới nước hoa cao cấp đang bước vào giai đoạn chuyển giao mang tính lịch sử. Khi người dùng ngày càng trân trọng tính cá nhân hóa, <strong>xu hướng nước hoa niche 2026</strong> không còn dừng lại ở việc tạo ra mùi hương dễ chịu, mà trở thành bản tuyên ngôn nghệ thuật và định vị phong thái cá nhân.</p>
<h2>1. Nốt Hương Gỗ Khói & Trầm Hương Sinh Thái (Sustainable Oud & Woody)</h2>
<p>Năm 2026 đánh dấu sự trở lại mạnh mẽ của các nốt hương trầm ấm, sâu lắng nhưng được khai thác hoàn toàn bền vững. Những cái tên như <a href=""/nuoc-hoa/le-labo-santal-33"">Le Labo Santal 33</a> hay Tom Ford Oud Wood tiếp tục định hình phong cách mùi hương vững chãi, uy nghiêm của giới thành đạt.</p>
<h2>2. Triết Lý Tối Giản Nhưng Đậm Đặc (Extrait de Parfum & Elixir)</h2>
<p>Thay vì những chai EDT nhanh bay, người sành hương 2026 săn đón nồng độ tinh dầu đậm đặc từ 25% – 40%. Điển hình là sự thống trị của các phiên bản Elixir và Parfum nguyên bản, giúp hương thơm lưu luyến trên da suốt 12 đến 24 giờ mà không gây nồng gắt.</p>
<h2>3. Nghệ Thuật Phối Trộn Hương (Scent Layering Cá Nhân Hóa)</h2>
<p>Không ai muốn bị ""trùng mùi"" ở các sự kiện thượng lưu. Xu hướng layering – kết hợp 2 nốt hương đối lập như một làn khói gỗ ấm phủ lên lớp cam chanh tươi mới – đang trở thành phong cách chuẩn mực. Hãy thử trải nghiệm tính năng <a href=""/catalog/quiz"">Trắc Nghiệm Tìm Mùi Hương AI</a> của Perfume Luxe để khám phá công thức phối hương độc bản cho riêng bạn.</p>
<h2>4. Nước Hoa Unisex Chiếm Lĩnh Thị Phần</h2>
<p>Ranh giới giới tính trong mùi hương chính thức bị xóa nhòa. Cả nam và nữ giới đều yêu thích những tông hương da thuộc mềm, trà đen, hoa iris và hổ phách tinh khiết.</p>
<h2>5. Trải Nghiệm Thử Mùi Trước Khi Mua Fullbox (Discovery Box)</h2>
<p>Thay vì mua ngay chai 100ml với chi phí hàng chục triệu, người tiêu dùng thông minh năm 2026 chọn giải pháp <a href=""/discovery-box"">Discovery Box 5 ống chiết VIP</a> để trải nghiệm trọn vẹn 3 tầng hương qua các điều kiện thời tiết thực tế trước khi ra quyết định.</p>
<div class=""callout-box"" style=""padding: 20px; background: rgba(212,175,55,0.08); border-left: 4px solid var(--gold-primary); border-radius: 8px; margin: 30px 0;"">
    <h4 style=""color: var(--gold-primary); margin-top: 0;"">Khám Phá Ngay Tại PERFUME LUXE</h4>
    <p style=""margin-bottom: 12px;"">Đón đầu xu hướng mùi hương 2026 với bộ sưu tập nước hoa Niche nhập khẩu 100% chính hãng từ Paris, Grasse và London.</p>
    <a href=""/bo-suu-tap"" class=""btn-luxe"" style=""display: inline-block; padding: 10px 24px;"">Xem Bộ Sưu Tập Niche 2026</a>
</div>
</div>',
        'https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?auto=format&fit=crop&w=800&q=80',
        N'Chuyên Gia Mùi Hương PERFUME LUXE',
        520,
        1,
        @CatXuHuong,
        N'Top 5 Xu Hướng Nước Hoa Niche 2026 Bùng Nổ | PERFUME LUXE',
        N'Khám phá 5 xu hướng nước hoa niche 2026 dẫn đầu phong cách sống thượng lưu: nốt hương bền vững, layering độc bản, nguyên liệu sinh thái và tinh dầu nồng độ cao.',
        SYSUTCDATETIME(),
        0
    );
END;

IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'nuoc-hoa-ben-vung-2026')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Summary, Content, ThumbnailUrl, AuthorName, ViewCount, IsPublished, CategoryId, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Nước Hoa Bền Vững: Tương Lai Của Ngành Hương Thơm Xa Xỉ 2026',
        'nuoc-hoa-ben-vung-2026',
        N'Tìm hiểu xu hướng nước hoa bền vững 2026: quy trình chiết xuất sạch, bao bì sinh thái có thể refill và cam kết đạo đức từ các nhà chế tác hương hàng đầu thế giới.',
        N'<div class=""blog-content-body"">
<p class=""lead-text"">Khái niệm xa xỉ thực sự của năm 2026 không chỉ nằm ở mức giá đắt đỏ, mà nằm ở nguồn gốc đạo đức và sự tử tế với hành tinh. <strong>Nước hoa bền vững 2026</strong> đang trở thành thước đo mới cho đẳng cấp của các Haute Parfumerie toàn cầu.</p>
<h2>Thế Nào Là Nước Hoa Bền Vững (Sustainable Fragrance)?</h2>
<p>Một chai nước hoa đạt chuẩn bền vững cần đáp ứng 4 tiêu chuẩn khắt khe:</p>
<ul>
    <li><strong>Nguồn nguyên liệu canh tác hữu cơ:</strong> Tinh dầu hoa hồng Thổ Nhĩ Kỳ, cỏ vetiver Haiti được thu hoạch có trách nhiệm, hỗ trợ sinh kế nông dân bản địa.</li>
    <li><strong>Công nghệ chiết xuất CO2 siêu tới hạn:</strong> Loại bỏ hoàn toàn dung môi hóa học độc hại, bảo tồn 100% cấu trúc phân tử hương tự nhiên.</li>
    <li><strong>Chai thủy tinh tái chế & Cơ chế Refill:</strong> Các hãng tiên phong như Kilian, Maison Margiela cho phép người dùng nạp lại tinh chất vào chai cũ, giảm 70% lượng rác thải thủy tinh cao cấp.</li>
    <li><strong>Không thử nghiệm trên động vật (Cruelty-Free):</strong> 100% thành phần thuần chay an toàn cho làn da nhạy cảm nhất.</li>
</ul>
<h2>Sự Chuyển Mình Của Các Thương Hiệu Xa Xỉ</h2>
<p>Tại Perfume Luxe, chúng tôi chọn lọc kỹ lưỡng những thương hiệu cam kết trung hòa carbon và bảo vệ sinh thái rừng già. Sự tinh tế nằm ở chỗ mùi hương vẫn giữ trọn vẹn độ sâu thẳm, vương vấn đến 12 giờ mà hoàn toàn không ảnh hưởng đến môi trường.</p>
</div>',
        'https://images.unsplash.com/photo-1615397349754-cfa2066a298e?auto=format&fit=crop&w=800&q=80',
        N'Chuyên Gia Mùi Hương PERFUME LUXE',
        380,
        1,
        @CatKienThuc,
        N'Nước Hoa Bền Vững 2026: Đẳng Cấp Xanh Của Giới Thượng Lưu | PERFUME LUXE',
        N'Tìm hiểu xu hướng nước hoa bền vững 2026: quy trình chiết xuất sạch, bao bì sinh thái có thể refill và cam kết đạo đức từ các nhà chế tác hương hàng đầu thế giới.',
        SYSUTCDATETIME(),
        0
    );
END;

IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'nuoc-hoa-unisex-cho-mua-he-2026')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Summary, Content, ThumbnailUrl, AuthorName, ViewCount, IsPublished, CategoryId, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Bộ Sưu Tập Nước Hoa Unisex Tươi Mát Dẫn Đầu Xu Hướng Hè 2026',
        'nuoc-hoa-unisex-cho-mua-he-2026',
        N'Khám phá các dòng nước hoa unisex cho mùa hè 2026 với nốt hương citrus mát lạnh, muối biển Địa Trung Hải và trà trắng thanh tao giúp bạn tự tin tỏa sáng suốt ngày dài.',
        N'<div class=""blog-content-body"">
<p class=""lead-text"">Mùa hè nhiệt đới tại Việt Nam đòi hỏi những sáng tạo hương thơm vừa sảng khoái đánh thức khứu giác, vừa êm dịu không gây ngột ngạt. Cùng điểm danh các kiệt tác <strong>nước hoa unisex cho mùa hè 2026</strong> được giới mộ điệu săn lùng nhiều nhất.</p>
<h2>1. Tông Hương Citrus & Cam Bergamot Calabria Mát Lạnh</h2>
<p>Mở đầu ngày hè rực rỡ với cam Bergamot mọng nước, quýt hồng Sicily hòa quyện cùng lá xô thơm. Những nốt hương này mang đến nguồn năng lượng tràn đầy cho cả phái nam và phái nữ trong các buổi gặp gỡ công việc hay dạo phố cuối tuần.</p>
<h2>2. Hương Muối Biển & Gió Đại Dương Địa Trung Hải</h2>
<p>Một làn gió mặn mà thoảng qua da thịt, mang vị khoáng đạt của đá biển và rong tảo xanh thẳm. Cảm giác như bạn đang thả mình trên du thuyền ngoài khơi bờ biển Amalfi đầy nắng gió.</p>
<h2>3. Nốt Trà Trắng & Xạ Hương Tinh Khiết Dịu Mát</h2>
<p>Điểm độc đáo của dòng hương hè 2026 là sự kết hợp của trà ô long, hoa nhài sương mai và xạ hương trắng mịn màng. Độ lưu hương đạt từ 6 - 8 tiếng dù bạn hoạt động dưới thời tiết nắng nóng.</p>
<div class=""product-recommendation"" style=""background: var(--bg-surface-elevated); padding: 24px; border: 1px solid var(--border-subtle); border-radius: 12px; margin: 30px 0;"">
    <h3 style=""color: var(--gold-primary); margin-top: 0;"">Trải Nghiệm Dòng Nước Hoa Hè Tại Perfume Luxe</h3>
    <p>Tất cả các dòng nước hoa mùa hè đều có sẵn phiên bản chiết 10ml tiện lợi mang theo khi đi du lịch hoặc công tác.</p>
    <a href=""/bo-suu-tap"" class=""btn-luxe"">Khám Phá Bộ Sưu Tập Mùa Hè</a>
</div>
</div>',
        'https://images.unsplash.com/photo-1523293182086-7651a899d37f?auto=format&fit=crop&w=800&q=80',
        N'Chuyên Gia Mùi Hương PERFUME LUXE',
        490,
        1,
        @CatBoSuuTap,
        N'Top Nước Hoa Unisex Mùa Hè 2026 Tươi Mát Bất Tận | PERFUME LUXE',
        N'Khám phá các dòng nước hoa unisex cho mùa hè 2026 với nốt hương citrus mát lạnh, muối biển Địa Trung Hải và trà trắng thanh tao giúp bạn tự tin tỏa sáng suốt ngày dài.',
        SYSUTCDATETIME(),
        0
    );
END;

IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'cach-phan-biet-nuoc-hoa-niche-va-designer')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Summary, Content, ThumbnailUrl, AuthorName, ViewCount, IsPublished, CategoryId, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Nước Hoa Niche vs Designer: Đâu Là Sự Khác Biệt Đẳng Cấp?',
        'cach-phan-biet-nuoc-hoa-niche-va-designer',
        N'Hướng dẫn cách phân biệt nước hoa niche và designer chuẩn xác từ triết lý sáng tạo, độ hiếm của nguyên liệu, nồng độ tinh dầu đến trải nghiệm định danh cá nhân.',
        N'<div class=""blog-content-body"">
<p class=""lead-text"">Rất nhiều người yêu hương thường băn khoăn: <em>Tại sao cùng là nước hoa cao cấp, nhưng những chai nước hoa Niche lại có giá gấp 3, thậm chí gấp 5 lần dòng Designer?</em> Hãy cùng giải mã <strong>cách phân biệt nước hoa niche và designer</strong> một cách chuẩn xác nhất.</p>
<h2>Bảng So Sánh Chi Tiết Niche vs Designer</h2>
<div style=""overflow-x: auto; margin: 24px 0;"">
<table style=""width: 100%; border-collapse: collapse; text-align: left; font-size: 0.9rem;"">
    <thead>
        <tr style=""background: rgba(212,175,55,0.15); border-bottom: 2px solid var(--gold-primary);"">
            <th style=""padding: 12px;"">Tiêu Chí</th>
            <th style=""padding: 12px;"">Nước Hoa Designer (Chanel, Dior...)</th>
            <th style=""padding: 12px;"">Nước Hoa Niche (Creed, Le Labo...)</th>
        </tr>
    </thead>
    <tbody>
        <tr style=""border-bottom: 1px solid var(--border-card);"">
            <td style=""padding: 12px; font-weight: 600;"">Mục Tiêu Sản Xuất</td>
            <td style=""padding: 12px;"">Thương mại hóa diện rộng, phục vụ số đông.</td>
            <td style=""padding: 12px;"">Bảo tồn nghệ thuật mùi hương độc bản, phục vụ giới sành hương.</td>
        </tr>
        <tr style=""border-bottom: 1px solid var(--border-card);"">
            <td style=""padding: 12px; font-weight: 600;"">Nguyên Liệu</td>
            <td style=""padding: 12px;"">Kết hợp hương liệu tổng hợp cao cấp và tự nhiên.</td>
            <td style=""padding: 12px;"">Chiết xuất tinh dầu tự nhiên quý hiếm (trầm hương, hoa hồng Grasse).</td>
        </tr>
        <tr style=""border-bottom: 1px solid var(--border-card);"">
            <td style=""padding: 12px; font-weight: 600;"">Độ Biến Chuyển Tầng Hương</td>
            <td style=""padding: 12px;"">Tuyến tính, dễ đoán, tươi tắn ngay từ lần xịt đầu.</td>
            <td style=""padding: 12px;"">Biến chuyển sâu sắc theo thời gian và thân nhiệt riêng biệt.</td>
        </tr>
    </tbody>
</table>
</div>
<h2>Khi Nào Bạn Nên Chọn Nước Hoa Niche?</h2>
<p>Nếu bạn muốn một mùi hương an toàn, dễ khen ngợi tại văn phòng thì Designer là sự lựa chọn hợp lý. Nhưng nếu bạn khao khát một mùi hương đại diện cho cái tôi cá nhân, khiến người khác phải ngoái nhìn và nhớ mãi không quên, thì thế giới Niche chính là chân trời đích thực.</p>
</div>',
        'https://images.unsplash.com/photo-1588405748880-12d1d2a59f75?auto=format&fit=crop&w=800&q=80',
        N'Chuyên Gia Mùi Hương PERFUME LUXE',
        610,
        1,
        @CatKienThuc,
        N'Cách Phân Biệt Nước Hoa Niche và Designer Chi Tiết | PERFUME LUXE',
        N'Hướng dẫn cách phân biệt nước hoa niche và designer chuẩn xác từ triết lý sáng tạo, độ hiếm của nguyên liệu, nồng độ tinh dầu đến trải nghiệm định danh cá nhân.',
        SYSUTCDATETIME(),
        0
    );
END;

IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'huong-dan-bao-quan-nuoc-hoa-trong-thoi-tiet-nong-am')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Summary, Content, ThumbnailUrl, AuthorName, ViewCount, IsPublished, CategoryId, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Bí Quyết Bảo Quản Nước Hoa Chính Hãng Bền Mùi Tại Khí Hậu Việt Nam',
        'huong-dan-bao-quan-nuoc-hoa-trong-thoi-tiet-nong-am',
        N'Tổng hợp bí quyết bảo quản nước hoa trong thời tiết nóng ẩm tại Việt Nam: tránh ánh nắng, nhiệt độ bảo quản lý tưởng, có nên để tủ lạnh không và cách chống oxy hóa.',
        N'<div class=""blog-content-body"">
<p class=""lead-text"">Một chai nước hoa đắt giá có thể suy giảm chất lượng, đổi màu hoặc biến mùi chua gắt chỉ sau vài tháng nếu không được cất giữ đúng cách. Dưới đây là <strong>hướng dẫn bảo quản nước hoa trong thời tiết nóng ẩm</strong> nhiệt đới đặc thù tại Việt Nam.</p>
<h2>3 ""Kẻ Thù"" Số 1 Của Nước Hoa Cao Cấp</h2>
<ol>
    <li><strong>Ánh sáng mặt trời trực tiếp (Tia UV):</strong> Phá vỡ liên kết phân tử tinh dầu, khiến tầng hương đầu bay hơi hoàn toàn.</li>
    <li><strong>Nhiệt độ cao & dao động liên tục:</strong> Làm oxy hóa cồn và làm biến chất nốt hương trái cây mọng nước.</li>
    <li><strong>Độ ẩm không khí cao:</strong> Thẩm thấu qua nắp xịt nếu đóng không kín, làm đục màu nước hoa.</li>
</ol>
<h2>Sai Lầm Phổ Biến: Có Nên Để Nước Hoa Trong Tủ Lạnh?</h2>
<p>Nhiều người có thói quen để nước hoa vào ngăn mát tủ lạnh thực phẩm. Đây là <strong>sai lầm nguy hiểm</strong>! Sự chênh lệch nhiệt độ đột ngột mỗi khi bạn lấy chai ra xịt sẽ làm ngưng tụ hơi nước bên trong chai, phá hủy cấu trúc tinh dầu. Chỉ sử dụng tủ lạnh chuyên dụng bảo quản mỹ phẩm với nhiệt độ cố định từ 15°C - 18°C.</p>
<h2>Vị Trí Lý Tưởng Nhất Để Đặt Nước Hoa</h2>
<ul>
    <li>Bên trong tủ quần áo gỗ hoặc ngăn kéo kín, tối và khô ráo.</li>
    <li>Giữ lại hộp giấy nguyên bản (box) của nhà sản xuất để che chắn ánh sáng.</li>
    <li>Tuyệt đối tránh để trong nhà tắm – nơi có độ ẩm và hơi nước nóng cao nhất trong nhà.</li>
</ul>
</div>',
        'https://images.unsplash.com/photo-1547887537-6158d64c35b3?auto=format&fit=crop&w=800&q=80',
        N'Chuyên Gia Mùi Hương PERFUME LUXE',
        450,
        1,
        @CatKienThuc,
        N'Hướng Dẫn Bảo Quản Nước Hoa Trong Thời Tiết Nóng Ẩm | PERFUME LUXE',
        N'Tổng hợp bí quyết bảo quản nước hoa trong thời tiết nóng ẩm tại Việt Nam: tránh ánh nắng, nhiệt độ bảo quản lý tưởng, có nên để tủ lạnh không và cách chống oxy hóa.',
        SYSUTCDATETIME(),
        0
    );
END;

IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'nuoc-hoa-le-labo-mui-nao-thom-nhat')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Summary, Content, ThumbnailUrl, AuthorName, ViewCount, IsPublished, CategoryId, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Review Chi Tiết: Top 5 Mùi Hương Le Labo Được Yêu Thích Nhất',
        'nuoc-hoa-le-labo-mui-nao-thom-nhat',
        N'Đánh giá chi tiết nước hoa Le Labo mùi nào thơm nhất: Santal 33, Another 13, Rose 31, Thé Noir 29, Bergamote 22. Hướng dẫn chọn mùi chuẩn phong cách và cá tính.',
        N'<div class=""blog-content-body"">
<p class=""lead-text"">Le Labo – thương hiệu nước hoa đến từ New York với phong cách phòng thí nghiệm tối giản – đã tạo nên cơn sốt toàn cầu. Vậy giữa hàng chục con số bí ẩn, <strong>nước hoa Le Labo mùi nào thơm nhất</strong> và đáng đầu tư nhất?</p>
<h2>1. Le Labo Santal 33 – Tuyên Ngôn Gỗ Đàn Hương Bất Hủ</h2>
<p>Chiếm vị trí số 1 không thể bàn cãi. <a href=""/nuoc-hoa/le-labo-santal-33"">Le Labo Santal 33</a> mang mùi hương của da thuộc, gỗ đàn hương Úc, bạch đậu khấu và hoa violet. Mùi hương đậm chất phong trần, bụi bặm nhưng vô cùng sang trọng, lưu hương trên 12 tiếng.</p>
<h2>2. Le Labo Another 13 – Xạ Hương Sạch Sẽ Tinh Tế</h2>
<p>Một mùi hương ma mị biến đổi kỳ ảo theo làn da của từng người. Another 13 sở hữu hợp chất Ambroxan kết hợp quả lê mọng, tạo nên cảm giác da thịt sạch sẽ, mê hoặc ở cự ly gần.</p>
<h2>3. Le Labo Thé Noir 29 – Nốt Trà Đen Bí Ẩn Đầy Quyến Rũ</h2>
<p>Sự pha trộn tinh tế giữa búp trà đen, lá nguyệt quế và quả sung ngọt dịu. Đây là mùi hương dành cho những buổi hẹn hò lãng mạn dưới ánh nến.</p>
<h2>4. Le Labo Rose 31 – Hoa Hồng Gai Góc Phi Giới Tính</h2>
<p>Không ngọt ngào ủy mị, Rose 31 là đóa hồng gai góc được nướng trên than củi cùng thì là Ba Tư và gỗ tuyết tùng. Cả nam và nữ đều có thể diện mùi hương này với phong thái tự tin.</p>
<h2>5. Le Labo Bergamote 22 – Bùng Nổ Sảng Khoái Mùa Hè</h2>
<p>Chai nước hoa mùa hè hoàn hảo với cam Bergamot, tinh dầu hoa cam Petitgrain và xạ hương trong veo, xua tan cái oi bức của những ngày nắng nóng.</p>
<div class=""cta-box"" style=""padding: 20px; background: rgba(212,175,55,0.08); border-radius: 8px; margin: 30px 0;"">
    <h4 style=""color: var(--gold-primary);"">Trải Nghiệm Nước Hoa Le Labo Tại PERFUME LUXE</h4>
    <p>Chúng tôi cung cấp đầy đủ phiên bản chiết 10ml chuẩn gốc và Fullbox 100ml nguyên seal với cam kết hoàn tiền 200% nếu phát hiện hàng giả.</p>
    <a href=""/nuoc-hoa/le-labo-santal-33"" class=""btn-luxe"">Xem Chi Tiết Santal 33</a>
</div>
</div>',
        'https://images.unsplash.com/photo-1594035910387-fea47794261f?auto=format&fit=crop&w=800&q=80',
        N'Chuyên Gia Mùi Hương PERFUME LUXE',
        750,
        1,
        @CatDanhGia,
        N'Nước Hoa Le Labo Mùi Nào Thơm Nhất? Top 5 Mùi Bán Chạy | PERFUME LUXE',
        N'Đánh giá chi tiết nước hoa Le Labo mùi nào thơm nhất: Santal 33, Another 13, Rose 31, Thé Noir 29, Bergamote 22. Hướng dẫn chọn mùi chuẩn phong cách và cá tính.',
        SYSUTCDATETIME(),
        0
    );
END;

IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'gia-nuoc-hoa-creed-aventus-chinh-hang')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Summary, Content, ThumbnailUrl, AuthorName, ViewCount, IsPublished, CategoryId, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Bảng Giá Nước Hoa Creed Aventus Chính Hãng Mới Nhất Tại Perfume Luxe',
        'gia-nuoc-hoa-creed-aventus-chinh-hang',
        N'Cập nhật bảng giá nước hoa Creed Aventus chính hãng mới nhất 2026: chiết 10ml, fullbox 50ml, 100ml. Cam kết 100% nhập khẩu chính ngạch, bồi hoàn 200% nếu phát hiện giả.',
        N'<div class=""blog-content-body"">
<p class=""lead-text"">Được mệnh danh là ""Ông Hoàng của thế giới mùi hương"", Creed Aventus luôn là khát khao của mọi quý ông quyền lực. Dưới đây là thông tin chi tiết về <strong>giá nước hoa Creed Aventus chính hãng</strong> được cập nhật mới nhất tại hệ thống PERFUME LUXE.</p>
<h2>Bảng Giá Creed Aventus Millesime EDP 2026</h2>
<div style=""overflow-x: auto; margin: 24px 0;"">
<table style=""width: 100%; border-collapse: collapse; text-align: left; font-size: 0.95rem;"">
    <thead>
        <tr style=""background: rgba(212,175,55,0.15); border-bottom: 2px solid var(--gold-primary);"">
            <th style=""padding: 12px;"">Quy Cách Đóng Gói</th>
            <th style=""padding: 12px;"">Giá Niêm Yết</th>
            <th style=""padding: 12px;"">Ưu Đãi Hôm Nay</th>
            <th style=""padding: 12px;"">Tình Trạng</th>
        </tr>
    </thead>
    <tbody>
        <tr style=""border-bottom: 1px solid var(--border-card);"">
            <td style=""padding: 12px; font-weight: 600;"">Ống Chiết VIP 10ml</td>
            <td style=""padding: 12px; text-decoration: line-through; color: var(--text-muted);"">850,000₫</td>
            <td style=""padding: 12px; color: var(--gold-primary); font-weight: 700;"">720,000₫</td>
            <td style=""padding: 12px; color: #22c55e;"">Còn hàng</td>
        </tr>
        <tr style=""border-bottom: 1px solid var(--border-card);"">
            <td style=""padding: 12px; font-weight: 600;"">Fullbox Nguyên Seal 50ml</td>
            <td style=""padding: 12px; text-decoration: line-through; color: var(--text-muted);"">6,200,000₫</td>
            <td style=""padding: 12px; color: var(--gold-primary); font-weight: 700;"">5,500,000₫</td>
            <td style=""padding: 12px; color: #22c55e;"">Còn hàng</td>
        </tr>
        <tr style=""border-bottom: 1px solid var(--border-card);"">
            <td style=""padding: 12px; font-weight: 600;"">Fullbox Nguyên Seal 100ml</td>
            <td style=""padding: 12px; text-decoration: line-through; color: var(--text-muted);"">8,800,000₫</td>
            <td style=""padding: 12px; color: var(--gold-primary); font-weight: 700;"">7,800,000₫</td>
            <td style=""padding: 12px; color: #22c55e;"">Sẵn sàng giao ngay</td>
        </tr>
    </tbody>
</table>
</div>
<h2>Chính Sách Bảo Hành & Cam Kết Từ PERFUME LUXE</h2>
<ul>
    <li><strong>100% Chính Hãng:</strong> Nhập khẩu trực tiếp từ Pháp, đầy đủ tem niêm phong và mã vạch truy xuất.</li>
    <li><strong>Bồi hoàn 200%:</strong> Đền gấp đôi giá trị đơn hàng nếu khách hàng phát hiện hàng kém chất lượng.</li>
    <li><strong>Miễn phí giao hàng VIP:</strong> Kiểm tra hàng, xịt thử trước khi thanh toán (COD).</li>
</ul>
<div style=""text-align: center; margin: 36px 0;"">
    <a href=""/nuoc-hoa/creed-aventus-millesime-edp"" class=""btn-luxe"" style=""font-size: 1.05rem; padding: 14px 36px;"">
        <i class=""fa-solid fa-bag-shopping"" style=""margin-right: 8px;""></i> Đặt Mua Creed Aventus Chính Hãng
    </a>
</div>
</div>',
        'https://images.unsplash.com/photo-1595425970377-c9703cf48b6d?auto=format&fit=crop&w=800&q=80',
        N'Chuyên Gia Mùi Hương PERFUME LUXE',
        890,
        1,
        @CatBoSuuTap,
        N'Bảng Giá Nước Hoa Creed Aventus Chính Hãng 2026 | PERFUME LUXE',
        N'Cập nhật bảng giá nước hoa Creed Aventus chính hãng mới nhất 2026: chiết 10ml, fullbox 50ml, 100ml. Cam kết 100% nhập khẩu chính ngạch, bồi hoàn 200% nếu phát hiện giả.',
        SYSUTCDATETIME(),
        0
    );
END;

IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'so-sanh-dior-sauvage-va-bleu-de-chanel')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Summary, Content, ThumbnailUrl, AuthorName, ViewCount, IsPublished, CategoryId, MetaTitle, MetaDescription, CreatedAt, IsDeleted)
    VALUES (
        N'Dior Sauvage vs Bleu de Chanel: Cuộc Chiến Của Những Huyền Thoại Mùi Hương Nam',
        'so-sanh-dior-sauvage-va-bleu-de-chanel',
        N'So sánh chi tiết Dior Sauvage và Bleu de Chanel về tầng hương, độ lưu hương, độ tỏa hương và phong cách phù hợp. Đâu là sự lựa chọn số 1 cho quý ông hiện đại?',
        N'<div class=""blog-content-body"">
<p class=""lead-text"">Trong phân khúc nước hoa nam cao cấp, cuộc đối đầu giữa <strong>Dior Sauvage</strong> và <strong>Bleu de Chanel</strong> luôn là tâm điểm tranh luận không có hồi kết. Cùng đặt lên bàn cân <strong>so sánh Dior Sauvage và Bleu de Chanel</strong> để tìm ra mảnh ghép hoàn hảo cho phong cách của bạn.</p>
<h2>Bảng So Sánh Toàn Diện</h2>
<div style=""overflow-x: auto; margin: 24px 0;"">
<table style=""width: 100%; border-collapse: collapse; text-align: left; font-size: 0.95rem;"">
    <thead>
        <tr style=""background: rgba(212,175,55,0.15); border-bottom: 2px solid var(--gold-primary);"">
            <th style=""padding: 12px;"">Đặc Điểm</th>
            <th style=""padding: 12px;""><a href=""/nuoc-hoa/dior-sauvage-elixir-parfum"" style=""color: var(--gold-primary);"">Dior Sauvage</a></th>
            <th style=""padding: 12px;""><a href=""/nuoc-hoa/bleu-de-chanel-parfum"" style=""color: var(--gold-primary);"">Bleu de Chanel</a></th>
        </tr>
    </thead>
    <tbody>
        <tr style=""border-bottom: 1px solid var(--border-card);"">
            <td style=""padding: 12px; font-weight: 600;"">Phong Cách Chủ Đạo</td>
            <td style=""padding: 12px;"">Hoang dã, phóng khoáng, nam tính mạnh mẽ.</td>
            <td style=""padding: 12px;"">Lịch lãm, tinh tế, quý ông thành đạt chuẩn mực.</td>
        </tr>
        <tr style=""border-bottom: 1px solid var(--border-card);"">
            <td style=""padding: 12px; font-weight: 600;"">Tầng Hương Chính</td>
            <td style=""padding: 12px;"">Cam Bergamot, Tiêu Tứ Xuyên, Ambroxan cay nồng.</td>
            <td style=""padding: 12px;"">Bưởi chùm, Hương tuyết tùng, Bạc hà, Đàn hương êm dịu.</td>
        </tr>
        <tr style=""border-bottom: 1px solid var(--border-card);"">
            <td style=""padding: 12px; font-weight: 600;"">Độ Tỏa Hương</td>
            <td style=""padding: 12px;"">Rất xa (bán kính 2 mét), thu hút sự chú ý tức thì.</td>
            <td style=""padding: 12px;"">Vừa phải (khoảng 1 cánh tay), kín đáo, thanh nhã.</td>
        </tr>
        <tr style=""border-bottom: 1px solid var(--border-card);"">
            <td style=""padding: 12px; font-weight: 600;"">Độ Lưu Hương</td>
            <td style=""padding: 12px;"">8 - 12 giờ (bản Elixir / Parfum).</td>
            <td style=""padding: 12px;"">7 - 10 giờ (bản Parfum).</td>
        </tr>
        <tr style=""border-bottom: 1px solid var(--border-card);"">
            <td style=""padding: 12px; font-weight: 600;"">Dịp Sử Dụng Tốt Nhất</td>
            <td style=""padding: 12px;"">Tiệc tùng, bar club, buổi hẹn hò cần gây ấn tượng mạnh.</td>
            <td style=""padding: 12px;"">Môi trường công sở, gặp đối tác, sự kiện trang trọng.</td>
        </tr>
    </tbody>
</table>
</div>
<h2>Lời Khuyên Từ Chuyên Gia Perfume Luxe</h2>
<p>Nếu bạn trẻ trung, năng động và muốn một mùi hương tạo ấn tượng choáng ngợp ngay khi bước vào phòng, hãy chọn <strong>Dior Sauvage</strong>. Ngược lại, nếu bạn là một quý ông điềm đạm, yêu thích sự tinh tế sang trọng khó nắm bắt, <strong>Bleu de Chanel</strong> chính là tấm danh thiếp hoàn mỹ nhất.</p>
<div style=""display: flex; gap: 16px; justify-content: center; flex-wrap: wrap; margin: 30px 0;"">
    <a href=""/nuoc-hoa/dior-sauvage-elixir-parfum"" class=""btn-luxe"">Xem Dior Sauvage Elixir</a>
    <a href=""/nuoc-hoa/bleu-de-chanel-parfum"" class=""btn-luxe-outline"">Xem Bleu de Chanel Parfum</a>
</div>
</div>',
        'https://images.unsplash.com/photo-1541643600914-78b084683601?auto=format&fit=crop&w=800&q=80',
        N'Chuyên Gia Mùi Hương PERFUME LUXE',
        980,
        1,
        @CatDanhGia,
        N'So Sánh Dior Sauvage và Bleu de Chanel: Nên Mua Chai Nào? | PERFUME LUXE',
        N'So sánh chi tiết Dior Sauvage và Bleu de Chanel về tầng hương, độ lưu hương, độ tỏa hương và phong cách phù hợp. Đâu là sự lựa chọn số 1 cho quý ông hiện đại?',
        SYSUTCDATETIME(),
        0
    );
END;

INSERT INTO dbo.__PerfumeSchemaVersions(Version) VALUES (@version);";
}
