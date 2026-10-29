using Microsoft.EntityFrameworkCore;
using WebsiteBanDienThoai.Models;

namespace WebsiteBanDienThoai.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            // Tự động áp dụng các Migration mới nhất
            context.Database.Migrate();

            // 1. Đảm bảo bảng Users tồn tại trên Database
            context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
                BEGIN
                    CREATE TABLE [Users] (
                        [Id] int NOT NULL IDENTITY,
                        [FullName] nvarchar(100) NOT NULL,
                        [Email] nvarchar(100) NOT NULL,
                        [PhoneNumber] nvarchar(20) NOT NULL,
                        [PasswordHash] nvarchar(255) NOT NULL,
                        [Role] nvarchar(50) NOT NULL DEFAULT 'Customer',
                        [SmemberPoints] int NOT NULL DEFAULT 0,
                        [SmemberLevel] nvarchar(50) NOT NULL DEFAULT 'S-New',
                        [Address] nvarchar(255) NULL,
                        [Province] nvarchar(100) NULL DEFAULT 'Hồ Chí Minh',
                        [Gender] nvarchar(20) NULL DEFAULT 'Nam',
                        [Birthday] datetime2 NULL,
                        [IsActive] bit NOT NULL DEFAULT 1,
                        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
                    );
                    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
                    CREATE UNIQUE INDEX [IX_Users_PhoneNumber] ON [Users] ([PhoneNumber]);
                END
                ELSE
                BEGIN
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'Address')
                    BEGIN
                        ALTER TABLE [Users] ADD [Address] nvarchar(255) NULL;
                        ALTER TABLE [Users] ADD [Province] nvarchar(100) NULL DEFAULT 'Hồ Chí Minh';
                        ALTER TABLE [Users] ADD [Gender] nvarchar(20) NULL DEFAULT 'Nam';
                        ALTER TABLE [Users] ADD [Birthday] datetime2 NULL;
                    END
                END
            ");

            // 2. Đảm bảo bảng ProductVariants tồn tại và có cột ImageUrl
            context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductVariants')
                BEGIN
                    CREATE TABLE [ProductVariants] (
                        [Id] int NOT NULL IDENTITY,
                        [ProductId] int NOT NULL,
                        [ColorName] nvarchar(100) NOT NULL,
                        [ColorHex] nvarchar(20) NOT NULL DEFAULT '#000000',
                        [Storage] nvarchar(50) NOT NULL DEFAULT '',
                        [Price] decimal(18,2) NOT NULL,
                        [StockQuantity] int NOT NULL DEFAULT 50,
                        [Sku] nvarchar(50) NULL,
                        [ImageUrl] nvarchar(500) NULL,
                        CONSTRAINT [PK_ProductVariants] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_ProductVariants_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_ProductVariants_ProductId] ON [ProductVariants] ([ProductId]);
                END
                ELSE
                BEGIN
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ProductVariants') AND name = 'ImageUrl')
                    BEGIN
                        ALTER TABLE [ProductVariants] ADD [ImageUrl] nvarchar(500) NULL;
                    END
                END
            ");

            // 3. Đảm bảo bảng ProductReviews tồn tại
            context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductReviews')
                BEGIN
                    CREATE TABLE [ProductReviews] (
                        [Id] int NOT NULL IDENTITY,
                        [ProductId] int NOT NULL,
                        [UserId] int NULL,
                        [CustomerName] nvarchar(100) NOT NULL,
                        [PhoneNumber] nvarchar(20) NULL,
                        [Rating] int NOT NULL DEFAULT 5,
                        [Content] nvarchar(1000) NOT NULL,
                        [VariantInfo] nvarchar(100) NULL,
                        [IsVerifiedBuyer] bit NOT NULL DEFAULT 1,
                        [LikesCount] int NOT NULL DEFAULT 0,
                        [AdminReply] nvarchar(1000) NULL,
                        [AdminReplyAt] datetime2 NULL,
                        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                        CONSTRAINT [PK_ProductReviews] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_ProductReviews_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_ProductReviews_ProductId] ON [ProductReviews] ([ProductId]);
                END
            ");

            // 4. Đảm bảo bảng FavoriteProducts tồn tại
            context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FavoriteProducts')
                BEGIN
                    CREATE TABLE [FavoriteProducts] (
                        [Id] int NOT NULL IDENTITY,
                        [UserId] int NULL,
                        [ProductId] int NOT NULL,
                        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                        CONSTRAINT [PK_FavoriteProducts] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_FavoriteProducts_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_FavoriteProducts_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_FavoriteProducts_ProductId] ON [FavoriteProducts] ([ProductId]);
                    CREATE INDEX [IX_FavoriteProducts_UserId] ON [FavoriteProducts] ([UserId]);
                END
            ");

            // 5. Đảm bảo bảng TradeInDevices tồn tại & có cột DeviceType
            context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TradeInDevices')
                BEGIN
                    CREATE TABLE [TradeInDevices] (
                        [Id] int NOT NULL IDENTITY,
                        [Brand] nvarchar(50) NOT NULL,
                        [DeviceType] nvarchar(50) NOT NULL DEFAULT N'Điện thoại',
                        [ModelName] nvarchar(100) NOT NULL,
                        [Storage] nvarchar(50) NOT NULL,
                        [BasePrice] decimal(18,2) NOT NULL,
                        [ImageUrl] nvarchar(500) NULL,
                        [IsActive] bit NOT NULL DEFAULT 1,
                        [DisplayOrder] int NOT NULL DEFAULT 0,
                        CONSTRAINT [PK_TradeInDevices] PRIMARY KEY ([Id])
                    );
                END
                ELSE
                BEGIN
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('TradeInDevices') AND name = 'DeviceType')
                    BEGIN
                        ALTER TABLE [TradeInDevices] ADD [DeviceType] nvarchar(50) NOT NULL DEFAULT N'Điện thoại';
                    END
                    UPDATE [TradeInDevices] SET [DeviceType] = N'Điện thoại' WHERE [DeviceType] IS NULL OR LTRIM(RTRIM([DeviceType])) = '';
                END
            ");

            // 6. Đảm bảo bảng TradeInRequests tồn tại & có các cột ActionType, SmemberTier, SmemberBonus
            context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TradeInRequests')
                BEGIN
                    CREATE TABLE [TradeInRequests] (
                        [Id] int NOT NULL IDENTITY,
                        [RequestCode] nvarchar(50) NOT NULL,
                        [UserId] int NULL,
                        [CustomerName] nvarchar(100) NOT NULL,
                        [PhoneNumber] nvarchar(20) NOT NULL,
                        [Email] nvarchar(100) NULL,
                        [OldDeviceName] nvarchar(100) NOT NULL,
                        [OldDeviceStorage] nvarchar(50) NOT NULL DEFAULT '',
                        [ConditionGrade] int NOT NULL DEFAULT 1,
                        [ActionType] nvarchar(20) NOT NULL DEFAULT 'Upgrade',
                        [SmemberTier] nvarchar(20) NOT NULL DEFAULT 'None',
                        [SmemberBonus] decimal(18,2) NOT NULL DEFAULT 0,
                        [BaseEstimatePrice] decimal(18,2) NOT NULL,
                        [SubsidyAmount] decimal(18,2) NOT NULL DEFAULT 0,
                        [TotalBenefit] decimal(18,2) NOT NULL DEFAULT 0,
                        [TargetProductId] int NULL,
                        [TargetProductName] nvarchar(150) NULL,
                        [TargetProductStorage] nvarchar(50) NULL,
                        [TargetProductPrice] decimal(18,2) NOT NULL DEFAULT 0,
                        [PayDifference] decimal(18,2) NOT NULL DEFAULT 0,
                        [BranchStore] nvarchar(255) NOT NULL,
                        [AppointmentDate] datetime2 NOT NULL,
                        [CustomerNote] nvarchar(500) NULL,
                        [Status] int NOT NULL DEFAULT 1,
                        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                        CONSTRAINT [PK_TradeInRequests] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_TradeInRequests_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL,
                        CONSTRAINT [FK_TradeInRequests_Products_TargetProductId] FOREIGN KEY ([TargetProductId]) REFERENCES [Products] ([Id]) ON DELETE SET NULL
                    );
                    CREATE INDEX [IX_TradeInRequests_UserId] ON [TradeInRequests] ([UserId]);
                    CREATE INDEX [IX_TradeInRequests_TargetProductId] ON [TradeInRequests] ([TargetProductId]);
                END
                ELSE
                BEGIN
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('TradeInRequests') AND name = 'ActionType')
                    BEGIN
                        ALTER TABLE [TradeInRequests] ADD [ActionType] nvarchar(20) NOT NULL DEFAULT 'Upgrade';
                    END
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('TradeInRequests') AND name = 'SmemberTier')
                    BEGIN
                        ALTER TABLE [TradeInRequests] ADD [SmemberTier] nvarchar(20) NOT NULL DEFAULT 'None';
                    END
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('TradeInRequests') AND name = 'SmemberBonus')
                    BEGIN
                        ALTER TABLE [TradeInRequests] ADD [SmemberBonus] decimal(18,2) NOT NULL DEFAULT 0;
                    END
                END
            ");

            // 6.5. Đảm bảo bảng Coupons và các cột giảm giá trong Orders tồn tại
            context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Coupons')
                BEGIN
                    CREATE TABLE [Coupons] (
                        [Id] int NOT NULL IDENTITY,
                        [Code] nvarchar(50) NOT NULL,
                        [Title] nvarchar(150) NOT NULL,
                        [Description] nvarchar(500) NOT NULL DEFAULT '',
                        [Type] int NOT NULL DEFAULT 0,
                        [DiscountValue] decimal(18,2) NOT NULL DEFAULT 0,
                        [IsPercentage] bit NOT NULL DEFAULT 0,
                        [MinOrderAmount] decimal(18,2) NOT NULL DEFAULT 0,
                        [MaxDiscountAmount] decimal(18,2) NULL,
                        [UsageLimit] int NOT NULL DEFAULT 1000,
                        [UsedCount] int NOT NULL DEFAULT 0,
                        [ExpiryDate] datetime2 NOT NULL,
                        [IsActive] bit NOT NULL DEFAULT 1,
                        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                        CONSTRAINT [PK_Coupons] PRIMARY KEY ([Id])
                    );
                    CREATE UNIQUE INDEX [IX_Coupons_Code] ON [Coupons] ([Code]);
                END

                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Orders')
                BEGIN
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'SubtotalAmount')
                    BEGIN
                        ALTER TABLE [Orders] ADD [SubtotalAmount] decimal(18,2) NOT NULL DEFAULT 0;
                        ALTER TABLE [Orders] ADD [ShippingFee] decimal(18,2) NOT NULL DEFAULT 30000;
                        ALTER TABLE [Orders] ADD [ShippingDiscountCode] nvarchar(50) NULL;
                        ALTER TABLE [Orders] ADD [ShippingDiscountAmount] decimal(18,2) NOT NULL DEFAULT 0;
                        ALTER TABLE [Orders] ADD [OrderDiscountCode] nvarchar(50) NULL;
                        ALTER TABLE [Orders] ADD [OrderDiscountAmount] decimal(18,2) NOT NULL DEFAULT 0;
                    END
                END
            ");

            // 6.6. Đảm bảo bảng RepairBookings (Đặt lịch sửa chữa Điện Thoại Vui / CellphoneS) tồn tại
            context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RepairBookings')
                BEGIN
                    CREATE TABLE [RepairBookings] (
                        [Id] int NOT NULL IDENTITY,
                        [BookingCode] nvarchar(50) NOT NULL,
                        [UserId] int NULL,
                        [CustomerName] nvarchar(100) NOT NULL,
                        [PhoneNumber] nvarchar(20) NOT NULL,
                        [Email] nvarchar(100) NULL,
                        [DeviceType] nvarchar(50) NOT NULL DEFAULT N'Điện thoại',
                        [DeviceName] nvarchar(150) NOT NULL,
                        [SerialNumber] nvarchar(50) NULL,
                        [ServiceType] nvarchar(100) NOT NULL,
                        [IssueDescription] nvarchar(1000) NULL,
                        [HasWarranty] bit NOT NULL DEFAULT 0,
                        [OrderCode] nvarchar(50) NULL,
                        [City] nvarchar(50) NOT NULL DEFAULT N'Hồ Chí Minh',
                        [StoreBranch] nvarchar(255) NOT NULL,
                        [BookingDate] datetime2 NOT NULL,
                        [TimeSlot] nvarchar(50) NOT NULL DEFAULT '09:00 - 10:30',
                        [EstimatedCost] decimal(18,2) NOT NULL DEFAULT 0,
                        [Status] int NOT NULL DEFAULT 0,
                        [TechnicianNote] nvarchar(500) NULL,
                        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                        CONSTRAINT [PK_RepairBookings] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_RepairBookings_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
                    );
                    CREATE INDEX [IX_RepairBookings_UserId] ON [RepairBookings] ([UserId]);
                    CREATE UNIQUE INDEX [IX_RepairBookings_BookingCode] ON [RepairBookings] ([BookingCode]);
                END
            ");

            // 6.7. Đảm bảo bảng WarrantyCards (Quản lý Phiếu bảo hành điện tử khách hàng) tồn tại
            context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WarrantyCards')
                BEGIN
                    CREATE TABLE [WarrantyCards] (
                        [Id] int NOT NULL IDENTITY,
                        [WarrantyCode] nvarchar(50) NOT NULL,
                        [OrderId] int NULL,
                        [OrderCode] nvarchar(50) NULL,
                        [OrderDetailId] int NULL,
                        [ProductId] int NULL,
                        [ProductVariantId] int NULL,
                        [UserId] int NULL,
                        [CustomerName] nvarchar(100) NOT NULL,
                        [PhoneNumber] nvarchar(20) NOT NULL,
                        [Email] nvarchar(100) NULL,
                        [Address] nvarchar(255) NULL,
                        [ProductName] nvarchar(255) NOT NULL,
                        [VariantInfo] nvarchar(100) NULL,
                        [ProductImageUrl] nvarchar(500) NULL,
                        [SerialNumber] nvarchar(100) NOT NULL,
                        [WarrantyPackage] nvarchar(100) NOT NULL DEFAULT N'12 tháng chính hãng CellphoneS',
                        [WarrantyPeriodMonths] int NOT NULL DEFAULT 12,
                        [PurchaseDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                        [StartDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                        [EndDate] datetime2 NOT NULL,
                        [ServiceCenter] nvarchar(255) NOT NULL DEFAULT N'Điện Thoại Vui AASP - 136 Nguyễn Thái Học, Quận 1, TP.HCM (Hotline: 1800.2064)',
                        [WarrantyTerms] nvarchar(2000) NULL,
                        [Status] int NOT NULL DEFAULT 0,
                        [AdminNote] nvarchar(1000) NULL,
                        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                        CONSTRAINT [PK_WarrantyCards] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_WarrantyCards_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE SET NULL,
                        CONSTRAINT [FK_WarrantyCards_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE SET NULL,
                        CONSTRAINT [FK_WarrantyCards_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
                    );
                    CREATE INDEX [IX_WarrantyCards_UserId] ON [WarrantyCards] ([UserId]);
                    CREATE INDEX [IX_WarrantyCards_OrderId] ON [WarrantyCards] ([OrderId]);
                    CREATE INDEX [IX_WarrantyCards_ProductId] ON [WarrantyCards] ([ProductId]);
                    CREATE UNIQUE INDEX [IX_WarrantyCards_WarrantyCode] ON [WarrantyCards] ([WarrantyCode]);
                    CREATE INDEX [IX_WarrantyCards_SerialNumber] ON [WarrantyCards] ([SerialNumber]);
                END
            ");

            // 6.8. Đảm bảo bảng Notifications tồn tại
            context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Notifications')
                BEGIN
                    CREATE TABLE [Notifications] (
                        [Id] int NOT NULL IDENTITY,
                        [Type] int NOT NULL DEFAULT 0,
                        [Title] nvarchar(200) NOT NULL,
                        [Message] nvarchar(1000) NOT NULL,
                        [TargetUrl] nvarchar(255) NULL,
                        [OrderId] int NULL,
                        [OrderCode] nvarchar(50) NULL,
                        [UserId] int NULL,
                        [RecipientRole] nvarchar(50) NULL,
                        [IsRead] bit NOT NULL DEFAULT 0,
                        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id])
                    );
                    CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);
                    CREATE INDEX [IX_Notifications_RecipientRole] ON [Notifications] ([RecipientRole]);
                    CREATE INDEX [IX_Notifications_CreatedAt] ON [Notifications] ([CreatedAt]);
                END
            ");

            // 7. Đảm bảo bảng NewsArticles tồn tại và seed bài viết công nghệ từ CellphoneS
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'NewsArticles')
                    BEGIN
                        CREATE TABLE [NewsArticles] (
                            [Id] int NOT NULL IDENTITY,
                            [Title] nvarchar(255) NOT NULL,
                            [Slug] nvarchar(255) NOT NULL,
                            [Summary] nvarchar(1000) NOT NULL,
                            [Content] nvarchar(max) NULL,
                            [ThumbnailUrl] nvarchar(500) NOT NULL,
                            [SourceUrl] nvarchar(500) NULL,
                            [Author] nvarchar(100) NOT NULL DEFAULT N'Sforum Editor',
                            [Category] nvarchar(100) NOT NULL DEFAULT N'Tin công nghệ',
                            [ViewCount] int NOT NULL DEFAULT 1200,
                            [IsFeatured] bit NOT NULL DEFAULT 0,
                            [IsActive] bit NOT NULL DEFAULT 1,
                            [PublishedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                            [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                            CONSTRAINT [PK_NewsArticles] PRIMARY KEY ([Id])
                        );
                        CREATE INDEX [IX_NewsArticles_Slug] ON [NewsArticles] ([Slug]);
                        CREATE INDEX [IX_NewsArticles_Category] ON [NewsArticles] ([Category]);
                    END
                ");

                if (!context.NewsArticles.Any())
                {
                    var sampleNews = new List<NewsArticle>
                    {
                        new NewsArticle
                        {
                            Title = "Đánh giá chi tiết iPhone 16 Pro Max: Nâng cấp đỉnh cao về camera và thời lượng pin",
                            Slug = "danh-gia-chi-tiet-iphone-16-pro-max",
                            Summary = "iPhone 16 Pro Max mang đến nút điều khiển Camera Control hoàn toàn mới, chip Apple A18 Pro tiến trình 3nm thế hệ thứ 2 cùng thời lượng pin đạt kỷ lục tốt nhất từ trước đến nay trên dòng iPhone.",
                            Content = "<p>iPhone 16 Pro Max là chiếc smartphone cao cấp nhất vừa được Apple ra mắt với nhiều cải tiến đáng giá. Thiết kế viền titan cấp 5 siêu mỏng giúp máy có màn hình 6.9 inch lớn nhất từ trước đến nay nhưng kích thước cầm nắm vẫn rất thoải mái.</p><p>Hệ thống camera tele tiềm vọng 5x tetraprism hiện diện trên cả hai phiên bản Pro, kết hợp cùng nút Camera Control cảm ứng lực giúp người dùng zoom, lấy nét và chụp ảnh chuyên nghiệp chỉ trong tích tắc.</p>",
                            ThumbnailUrl = "https://cdn-media.sforum.vn/storage/app/media/Tag-image/apple-iphone-18-pro-18-pro-max-tag.jpg",
                            SourceUrl = "https://cellphones.com.vn/sforum/danh-gia-iphone-16-pro-max",
                            Author = "Hải Nam",
                            Category = "Đánh giá",
                            ViewCount = 15800,
                            IsFeatured = true,
                            IsActive = true,
                            PublishedAt = DateTime.UtcNow.AddHours(-2)
                        },
                        new NewsArticle
                        {
                            Title = "So sánh Galaxy Z Fold6 và iPhone 16 Pro Max: Siêu phẩm nào đáng tiền hơn?",
                            Slug = "so-sanh-galaxy-z-fold-6-va-iphone-16-pro-max",
                            Summary = "Hai đại diện xuất sắc nhất của thế giới công nghệ: Màn hình gập đa nhiệm Galaxy AI đỉnh cao từ Samsung đối đầu cùng hiệu năng mượt mà, camera xuất sắc của iPhone 16 Pro Max.",
                            Content = "<p>Cả Galaxy Z Fold6 và iPhone 16 Pro Max đều là những thiết bị công nghệ đỉnh cao hướng đến phân khúc khách hàng thượng lưu. Galaxy Z Fold6 mang lại không gian làm việc đa nhiệm tuyệt vời như một chiếc máy tính bảng mini, trong khi iPhone 16 Pro Max giữ vững sự ổn định, quay video điện ảnh và hệ sinh thái Apple bền bỉ.</p>",
                            ThumbnailUrl = "https://cdn-media.sforum.vn/storage/app/media/Tag-image/apple-iphone-duo-tag.jpg",
                            SourceUrl = "https://cellphones.com.vn/sforum/so-sanh-galaxy-z-fold-6-va-iphone-16-pro-max",
                            Author = "Jay Nguyen",
                            Category = "So sánh",
                            ViewCount = 9400,
                            IsFeatured = false,
                            IsActive = true,
                            PublishedAt = DateTime.UtcNow.AddHours(-5)
                        },
                        new NewsArticle
                        {
                            Title = "Trên tay OPPO Find X10 Pro Max chính hãng: Hệ thống camera đỉnh 3 ống kính 200MP",
                            Slug = "tren-tay-oppo-find-x10-pro-max",
                            Summary = "OPPO Find X10 Pro Max, flagship đỉnh cao đầu tiên sở hữu bộ ba camera 200MP, chip Dimensity 9600 Pro cùng viên pin siêu khủng 8,000 mAh và sạc nhanh 120W SuperVOOC.",
                            Content = "<p>Hôm nay Sforum CellphoneS đã có cơ hội trên tay chiếc flagship mới nhất mang tên OPPO Find X10 Pro Max. Điểm nổi bật nhất chính là cụm camera Hasselblad với cả 3 cảm biến đều đạt độ phân giải 200MP mang lại độ chi tiết vô song ngay cả trong điều kiện thiếu sáng phức tạp.</p>",
                            ThumbnailUrl = "https://cdn-media.sforum.vn/storage/app/media/thanhnam/2026_10/tren-tay-oppo-find-x10-pro-max/tren-tay-oppo-find-x10-pro-max-thumb.jpg",
                            SourceUrl = "https://cellphones.com.vn/sforum/tren-tay-oppo-find-x10-pro-max",
                            Author = "Jay Nguyen",
                            Category = "Trên tay",
                            ViewCount = 12300,
                            IsFeatured = false,
                            IsActive = true,
                            PublishedAt = DateTime.UtcNow.AddHours(-8)
                        },
                        new NewsArticle
                        {
                            Title = "Top 5 smartphone có camera zoom quang học xa nhất và tốt nhất hiện nay",
                            Slug = "top-5-smartphone-co-camera-zoom-quang-hoc-xa-nhat",
                            Summary = "Khám phá danh sách những mẫu điện thoại chụp ảnh phóng to từ xa sắc nét nhất: Galaxy S24 Ultra zoom 100x, iPhone 16 Pro Max zoom 25x, Xiaomi 14 Ultra.",
                            Content = "<p>Nếu bạn là người đam mê chụp ảnh phong cảnh, sân khấu ca nhạc hay chụp chim muông từ khoảng cách xa, thì một chiếc smartphone có ống kính tiềm vọng zoom quang học từ 5x đến 10x là trang bị không thể thiếu.</p>",
                            ThumbnailUrl = "https://cdn-media.sforum.vn/storage/app/media/dinhnhan/Danh-gia-camera-OPPO-A7-Pro/Cover.jpg",
                            SourceUrl = "https://cellphones.com.vn/sforum/top-dien-thoai-chup-anh-dep-2024",
                            Author = "Thuý Huyên",
                            Category = "Tư vấn",
                            ViewCount = 7600,
                            IsFeatured = false,
                            IsActive = true,
                            PublishedAt = DateTime.UtcNow.AddDays(-1)
                        },
                        new NewsArticle
                        {
                            Title = "10 tính năng ẩn trên iOS 18 cực hay mà người dùng iPhone nhất định phải biết",
                            Slug = "10-tinh-nang-an-tren-ios-18-cuc-hay",
                            Summary = "Tự do sắp xếp và đổi màu icon ứng dụng, bảo mật khóa app bằng Face ID, máy tính đa năng Math Notes và giao diện Control Center tùy biến không giới hạn.",
                            Content = "<p>iOS 18 là một trong những bản nâng cấp giao diện lớn nhất của Apple. Không chỉ mang đến khả năng cá nhân hóa tự do trên màn hình chính, iOS 18 còn tích hợp hàng loạt tính năng bảo mật mới giúp bảo vệ dữ liệu người dùng an toàn tuyệt đối.</p>",
                            ThumbnailUrl = "https://cdn-media.sforum.vn/storage/app/media/Tag-image/ios-27-tag.jpg",
                            SourceUrl = "https://cellphones.com.vn/sforum/thu-thuat-ios-18-hay-nhat",
                            Author = "Khánh Minh",
                            Category = "Thủ thuật",
                            ViewCount = 10800,
                            IsFeatured = false,
                            IsActive = true,
                            PublishedAt = DateTime.UtcNow.AddDays(-2)
                        },
                        new NewsArticle
                        {
                            Title = "HONOR Win 2 ra mắt: Chip Snapdragon 8EE Gen 6, pin 10100mAh, tản nhiệt cực xịn",
                            Slug = "honor-win-2-ra-mat",
                            Summary = "HONOR chính thức trình làng dòng điện thoại gaming Win 2 trang bị quạt tản nhiệt chủ động 15000 vòng/phút, sạc siêu nhanh 120W cùng mức giá cực kỳ hấp dẫn.",
                            Content = "<p>Dòng smartphone gaming Win 2 mới của HONOR hướng trực tiếp đến các game thủ chuyên nghiệp. Với dung lượng pin lên tới 10,100 mAh, máy cho phép chơi game liên tục suốt 14 giờ mà không cần cắm sạc.</p>",
                            ThumbnailUrl = "/images/banners/banner-s24ultra.jpg",
                            SourceUrl = "https://cellphones.com.vn/sforum/honor-win-2-ra-mat",
                            Author = "Hải Nam",
                            Category = "Di động",
                            ViewCount = 6200,
                            IsFeatured = false,
                            IsActive = true,
                            PublishedAt = DateTime.UtcNow.AddHours(-14)
                        },
                        new NewsArticle
                        {
                            Title = "NVIDIA RTX Spark lộ diện hiệu năng: GPU out trình M5 Pro, cạnh tranh quyết liệt",
                            Slug = "hieu-nang-thuc-te-nvidia-rtx-spark",
                            Summary = "NVIDIA RTX Spark đã có những bài đánh giá thực tế đầu tiên, cạnh tranh trực tiếp Apple M5 Pro và M5 Max trên thị trường đồ họa cao cấp.",
                            Content = "<p>Kiến trúc nhân đồ họa mới của NVIDIA tối ưu hóa cho AI tạo sinh và render 3D chuyên sâu, đạt số điểm ấn tượng trên Blender và Premiere Pro.</p>",
                            ThumbnailUrl = "/images/banners/banner-laptop.jpg",
                            SourceUrl = "https://cellphones.com.vn/sforum/hieu-nang-thuc-te-nvidia-rtx-spark",
                            Author = "Hải Trần",
                            Category = "Máy tính",
                            ViewCount = 8900,
                            IsFeatured = false,
                            IsActive = true,
                            PublishedAt = DateTime.UtcNow.AddHours(-18)
                        },
                        new NewsArticle
                        {
                            Title = "So sánh iPhone 18 Pro vs 16 Pro Max: Được gì và mất gì khi lên đời?",
                            Slug = "so-sanh-iphone-18-pro-vs-16-pro-max",
                            Summary = "Phân tích chi tiết những nâng cấp về thiết kế, vi xử lý và hệ thống camera thế hệ mới giữa iPhone 18 Pro và người tiền nhiệm 16 Pro Max.",
                            Content = "<p>Mỗi thế hệ iPhone mới ra mắt đều khiến người dùng đắn đo về việc có nên lên đời hay không. Cùng Sforum phân tích những điểm hơn thiệt chi tiết nhất.</p>",
                            ThumbnailUrl = "https://cdn-media.sforum.vn/storage/app/media/thanhnam/2026_10/so-sanh-iphone-18-pro-vs-16-pro-max/so-sanh-iphone-18-pro-vs-16-pro-max-thumb.jpg",
                            SourceUrl = "https://cellphones.com.vn/sforum/so-sanh-iphone-18-pro-vs-16-pro-max",
                            Author = "Tiz",
                            Category = "So sánh",
                            ViewCount = 13500,
                            IsFeatured = false,
                            IsActive = true,
                            PublishedAt = DateTime.UtcNow.AddDays(-3)
                        },
                        new NewsArticle
                        {
                            Title = "Cập nhật giá iPhone dịp 20/10: Mẫu iPhone nào đang có giá tốt phù hợp mua làm quà?",
                            Slug = "cap-nhat-gia-iphone-dip-20-10-2026",
                            Summary = "Hệ thống CellphoneS giảm giá cực sốc loạt sản phẩm Apple chính hãng, trợ giá thu cũ lên đời tới 4 triệu đồng nhân dịp đại lễ tri ân.",
                            Content = "<p>Dịp lễ 20/10 là thời điểm vàng để lựa chọn những món quà công nghệ ý nghĩa. CellphoneS triển khai chương trình ưu đãi đặc quyền với mức giảm trực tiếp lên tới hàng triệu đồng.</p>",
                            ThumbnailUrl = "https://cdn-media.sforum.vn/storage/app/media/maithuong/cap-nhat-gia-iphone-dip-20-10-cover.jpg",
                            SourceUrl = "https://cellphones.com.vn/sforum/cap-nhat-gia-iphone-dip-20-10-2026",
                            Author = "Mai Thương",
                            Category = "Tư vấn",
                            ViewCount = 9100,
                            IsFeatured = false,
                            IsActive = true,
                            PublishedAt = DateTime.UtcNow.AddDays(-4)
                        }
                    };

                    context.NewsArticles.AddRange(sampleNews);
                    context.SaveChanges();
                }
                else
                {
                    var existingNews = context.NewsArticles.ToList();
                    bool newsUpdated = false;
                    foreach (var n in existingNews)
                    {
                        if (n.Slug == "danh-gia-chi-tiet-iphone-16-pro-max" && n.ThumbnailUrl.Contains("wp-content/uploads"))
                        {
                            n.ThumbnailUrl = "https://cdn-media.sforum.vn/storage/app/media/Tag-image/apple-iphone-18-pro-18-pro-max-tag.jpg";
                            newsUpdated = true;
                        }
                        else if (n.Slug == "so-sanh-galaxy-z-fold-6-va-iphone-16-pro-max" && n.ThumbnailUrl.Contains("wp-content/uploads"))
                        {
                            n.ThumbnailUrl = "https://cdn-media.sforum.vn/storage/app/media/Tag-image/apple-iphone-duo-tag.jpg";
                            newsUpdated = true;
                        }
                        else if (n.Slug == "top-5-smartphone-co-camera-zoom-quang-hoc-xa-nhat" && n.ThumbnailUrl.Contains("wp-content/uploads"))
                        {
                            n.ThumbnailUrl = "https://cdn-media.sforum.vn/storage/app/media/dinhnhan/Danh-gia-camera-OPPO-A7-Pro/Cover.jpg";
                            newsUpdated = true;
                        }
                        else if (n.Slug == "10-tinh-nang-an-tren-ios-18-cuc-hay" && n.ThumbnailUrl.Contains("wp-content/uploads"))
                        {
                            n.ThumbnailUrl = "https://cdn-media.sforum.vn/storage/app/media/Tag-image/ios-27-tag.jpg";
                            newsUpdated = true;
                        }
                    }
                    if (newsUpdated)
                    {
                        context.SaveChanges();
                    }
                }
            }
            catch { /* Bỏ qua nếu bảng NewsArticles chưa hoàn tất */ }

            // 7.1 Đảm bảo bảng WarehouseReceipts, WarehouseReceiptDetails, InventoryLogs tồn tại
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WarehouseReceipts')
                    BEGIN
                        CREATE TABLE [WarehouseReceipts] (
                            [Id] int NOT NULL IDENTITY,
                            [ReceiptCode] nvarchar(50) NOT NULL,
                            [SupplierName] nvarchar(150) NOT NULL,
                            [SupplierPhone] nvarchar(20) NULL,
                            [SupplierAddress] nvarchar(255) NULL,
                            [CreatedBy] nvarchar(100) NULL,
                            [ImportDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                            [Note] nvarchar(500) NULL,
                            [TotalAmount] decimal(18,2) NOT NULL DEFAULT 0,
                            [Status] int NOT NULL DEFAULT 1,
                            [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                            CONSTRAINT [PK_WarehouseReceipts] PRIMARY KEY ([Id])
                        );
                        CREATE UNIQUE INDEX [IX_WarehouseReceipts_ReceiptCode] ON [WarehouseReceipts] ([ReceiptCode]);
                    END
                ");
            }
            catch { }

            try
            {
                context.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WarehouseReceiptDetails')
                    BEGIN
                        CREATE TABLE [WarehouseReceiptDetails] (
                            [Id] int NOT NULL IDENTITY,
                            [WarehouseReceiptId] int NOT NULL,
                            [ProductId] int NOT NULL,
                            [ProductVariantId] int NULL,
                            [VariantInfo] nvarchar(150) NULL,
                            [Quantity] int NOT NULL DEFAULT 1,
                            [ImportPrice] decimal(18,2) NOT NULL DEFAULT 0,
                            [TotalPrice] decimal(18,2) NOT NULL DEFAULT 0,
                            [Note] nvarchar(255) NULL,
                            CONSTRAINT [PK_WarehouseReceiptDetails] PRIMARY KEY ([Id]),
                            CONSTRAINT [FK_WarehouseReceiptDetails_WarehouseReceipts] FOREIGN KEY ([WarehouseReceiptId]) REFERENCES [WarehouseReceipts] ([Id]) ON DELETE CASCADE,
                            CONSTRAINT [FK_WarehouseReceiptDetails_Products] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
                            CONSTRAINT [FK_WarehouseReceiptDetails_ProductVariants] FOREIGN KEY ([ProductVariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE NO ACTION
                        );
                        CREATE INDEX [IX_WarehouseReceiptDetails_WarehouseReceiptId] ON [WarehouseReceiptDetails] ([WarehouseReceiptId]);
                        CREATE INDEX [IX_WarehouseReceiptDetails_ProductId] ON [WarehouseReceiptDetails] ([ProductId]);
                    END
                ");
            }
            catch { }

            try
            {
                context.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InventoryLogs')
                    BEGIN
                        CREATE TABLE [InventoryLogs] (
                            [Id] int NOT NULL IDENTITY,
                            [ProductId] int NOT NULL,
                            [ProductVariantId] int NULL,
                            [ChangeType] nvarchar(50) NOT NULL,
                            [QuantityChange] int NOT NULL,
                            [StockBefore] int NOT NULL,
                            [StockAfter] int NOT NULL,
                            [ReferenceCode] nvarchar(50) NULL,
                            [Note] nvarchar(500) NULL,
                            [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                            CONSTRAINT [PK_InventoryLogs] PRIMARY KEY ([Id]),
                            CONSTRAINT [FK_InventoryLogs_Products] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
                            CONSTRAINT [FK_InventoryLogs_ProductVariants] FOREIGN KEY ([ProductVariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE NO ACTION
                        );
                        CREATE INDEX [IX_InventoryLogs_ProductId] ON [InventoryLogs] ([ProductId]);
                    END
                ");
            }
            catch { }

            // 8. Chuẩn hóa tên sản phẩm trong DB (chỉ để tên dòng máy, đưa dung lượng/RAM vào biến thể)
            try
            {
                var existingProducts = context.Products.ToList();
                if (existingProducts.Any())
                {
                    bool nameUpdated = false;
                    foreach (var p in existingProducts)
                    {
                        string oldName = p.Name;
                        if (p.Name.Contains("vivo V40 Lite", StringComparison.OrdinalIgnoreCase))
                            p.Name = "vivo V40 Lite";
                        else if (p.Name.Contains("iPhone 16 Pro Max", StringComparison.OrdinalIgnoreCase))
                            p.Name = "iPhone 16 Pro Max";
                        else if (p.Name.Contains("Galaxy S24 Ultra", StringComparison.OrdinalIgnoreCase))
                            p.Name = "Samsung Galaxy S24 Ultra";
                        else if (p.Name.Contains("iPhone 15", StringComparison.OrdinalIgnoreCase))
                            p.Name = "iPhone 15";
                        else if (p.Name.Contains("Xiaomi 14", StringComparison.OrdinalIgnoreCase))
                            p.Name = "Xiaomi 14";
                        else if (p.Name.Contains("Find N3 Flip", StringComparison.OrdinalIgnoreCase))
                            p.Name = "OPPO Find N3 Flip";
                        else if (p.Name.Contains("Galaxy Z Flip6", StringComparison.OrdinalIgnoreCase) || p.Name.Contains("Z Flip 6", StringComparison.OrdinalIgnoreCase))
                            p.Name = "Samsung Galaxy Z Flip6";
                        else if (p.Name.Contains("Redmi Note 13", StringComparison.OrdinalIgnoreCase))
                            p.Name = "Xiaomi Redmi Note 13";
                        else if (p.Name.Contains("MacBook Air M2", StringComparison.OrdinalIgnoreCase))
                            p.Name = "MacBook Air M2 13 inch";
                        else if (p.Name.Contains("TUF Gaming F15", StringComparison.OrdinalIgnoreCase))
                            p.Name = "ASUS TUF Gaming F15 FX507ZC4";
                        else if (p.Name.Contains("iPad Air 6", StringComparison.OrdinalIgnoreCase))
                            p.Name = "iPad Air 6 M2 11 inch";

                        if (oldName != p.Name)
                        {
                            nameUpdated = true;
                        }
                    }
                    if (nameUpdated)
                    {
                        context.SaveChanges();
                    }
                }
            }
            catch { /* Bỏ qua nếu bảng Products chưa khởi tạo xong */ }

            // 8. Tự động kiểm tra và gộp/xóa các sản phẩm trùng lặp trong DB
            try
            {
                var allDbProducts = context.Products
                    .Include(p => p.Variants)
                    .Include(p => p.Images)
                    .Include(p => p.Reviews)
                    .ToList();

                var productGroups = allDbProducts
                    .GroupBy(p => p.Name.Trim().ToLower())
                    .Where(g => g.Count() > 1)
                    .ToList();

                bool hasDuplicatesRemoved = false;

                foreach (var group in productGroups)
                {
                    // Chọn sản phẩm chính: ưu tiên có nhiều biến thể nhất, nhiều đánh giá nhất, hoặc ID nhỏ nhất
                    var primary = group.OrderByDescending(p => p.Variants.Count)
                                       .ThenByDescending(p => p.Reviews.Count)
                                       .ThenBy(p => p.Id)
                                       .First();

                    var duplicates = group.Where(p => p.Id != primary.Id).ToList();

                    foreach (var dup in duplicates)
                    {
                        // 1. Cập nhật OrderDetails trỏ về primary
                        var orderDetails = context.OrderDetails.Where(od => od.ProductId == dup.Id).ToList();
                        foreach (var od in orderDetails)
                        {
                            od.ProductId = primary.Id;
                        }

                        // 2. Cập nhật TradeInRequests trỏ về primary
                        var tradeIns = context.TradeInRequests.Where(t => t.TargetProductId == dup.Id).ToList();
                        foreach (var t in tradeIns)
                        {
                            t.TargetProductId = primary.Id;
                        }

                        // 3. Cập nhật FavoriteProducts trỏ về primary (xóa bản ghi trùng nếu user đã thích primary)
                        var favs = context.FavoriteProducts.Where(f => f.ProductId == dup.Id).ToList();
                        foreach (var f in favs)
                        {
                            bool alreadyFav = context.FavoriteProducts.Any(existing => existing.UserId == f.UserId && existing.ProductId == primary.Id);
                            if (alreadyFav)
                            {
                                context.FavoriteProducts.Remove(f);
                            }
                            else
                            {
                                f.ProductId = primary.Id;
                            }
                        }

                        // 4. Chuyển đánh giá (Reviews) về primary
                        var reviews = context.ProductReviews.Where(r => r.ProductId == dup.Id).ToList();
                        foreach (var r in reviews)
                        {
                            r.ProductId = primary.Id;
                        }

                        // 5. Gộp các biến thể của duplicate vào primary nếu primary chưa có
                        foreach (var v in dup.Variants.ToList())
                        {
                            bool variantExists = primary.Variants.Any(pv => 
                                pv.ColorName.Trim().Equals(v.ColorName.Trim(), StringComparison.OrdinalIgnoreCase) && 
                                pv.Storage.Trim().Equals(v.Storage.Trim(), StringComparison.OrdinalIgnoreCase));

                            if (!variantExists)
                            {
                                v.ProductId = primary.Id;
                                primary.Variants.Add(v);
                            }
                            else
                            {
                                context.ProductVariants.Remove(v);
                            }
                        }

                        // 6. Xóa ảnh của sản phẩm trùng lặp
                        foreach (var img in dup.Images.ToList())
                        {
                            context.ProductImages.Remove(img);
                        }

                        // 7. Xóa sản phẩm trùng lặp
                        context.Products.Remove(dup);
                        hasDuplicatesRemoved = true;
                    }
                }

                if (hasDuplicatesRemoved)
                {
                    context.SaveChanges();
                }
            }
            catch { /* Bỏ qua nếu có ngoại lệ */ }

            // 1. Seed Danh mục nếu chưa có
            if (!context.Categories.Any())
            {
                var catPhone = new Category { Name = "Điện thoại", Slug = "dien-thoai", IconClass = "bi bi-phone", DisplayOrder = 1 };
                var catLaptop = new Category { Name = "Laptop", Slug = "laptop", IconClass = "bi bi-laptop", DisplayOrder = 2 };
                var catTablet = new Category { Name = "Tablet", Slug = "tablet", IconClass = "bi bi-tablet", DisplayOrder = 3 };
                var catAudio = new Category { Name = "Âm thanh", Slug = "am-thanh", IconClass = "bi bi-headphones", DisplayOrder = 4 };
                var catWatch = new Category { Name = "Đồng hồ", Slug = "dong-ho", IconClass = "bi bi-smartwatch", DisplayOrder = 5 };
                var catAccessories = new Category { Name = "Phụ kiện", Slug = "phu-kien", IconClass = "bi bi-plug", DisplayOrder = 6 };
                var catHome = new Category { Name = "Đồ gia dụng", Slug = "gia-dung", IconClass = "bi bi-house-gear", DisplayOrder = 7 };
                var catTradeIn = new Category { Name = "Thu cũ đổi mới", Slug = "thu-cu-doi-moi", IconClass = "bi bi-arrow-repeat", DisplayOrder = 8 };

                context.Categories.AddRange(catPhone, catLaptop, catTablet, catAudio, catWatch, catAccessories, catHome, catTradeIn);
                context.SaveChanges();
            }

            // 2. Seed Thương hiệu nếu chưa có
            if (!context.Brands.Any())
            {
                var brandApple = new Brand { Name = "Apple", Slug = "apple", LogoUrl = "https://cdn.tgdd.vn/Brand/1/logo-iphone-220x48.png" };
                var brandSamsung = new Brand { Name = "Samsung", Slug = "samsung", LogoUrl = "https://cdn.tgdd.vn/Brand/1/samsungnew-220x48-1.png" };
                var brandXiaomi = new Brand { Name = "Xiaomi", Slug = "xiaomi", LogoUrl = "https://cdn.tgdd.vn/Brand/1/logo-xiaomi-220x48-5.png" };
                var brandOppo = new Brand { Name = "OPPO", Slug = "oppo", LogoUrl = "https://cdn.tgdd.vn/Brand/1/OPPO42-b_5.jpg" };
                var brandVivo = new Brand { Name = "vivo", Slug = "vivo", LogoUrl = "https://cdn.tgdd.vn/Brand/1/vivo-logo-220x48-3.png" };
                var brandAsus = new Brand { Name = "ASUS", Slug = "asus", LogoUrl = "https://cdn.tgdd.vn/Brand/1/logo-asus-149x40.png" };
                var brandSony = new Brand { Name = "Sony", Slug = "sony", LogoUrl = "https://cdn.tgdd.vn/Brand/1/Sony42-b_39.png" };

                context.Brands.AddRange(brandApple, brandSamsung, brandXiaomi, brandOppo, brandVivo, brandAsus, brandSony);
                context.SaveChanges();
            }

            var cats = context.Categories.ToDictionary(c => c.Slug, c => c.Id);
            var brs = context.Brands.ToDictionary(b => b.Slug, b => b.Id);

            // 3. Seed hoặc Cập nhật Banners
            if (!context.Banners.Any())
            {
                var banners = new List<Banner>
                {
                    new Banner
                    {
                        Title = "IPHONE 16 PRO MAX",
                        Subtitle = "Đỉnh cao Titanium - Quyền năng Apple Intelligence",
                        ImageUrl = "/images/banners/banner-iphone16.jpg",
                        LinkUrl = "/Product/Details/1",
                        Position = "MainSlider",
                        DisplayOrder = 1
                    },
                    new Banner
                    {
                        Title = "GALAXY S24 ULTRA",
                        Subtitle = "Quyền năng Galaxy AI - Giảm đến 6 triệu",
                        ImageUrl = "/images/banners/banner-s24ultra.jpg",
                        LinkUrl = "/Product/Details/2",
                        Position = "MainSlider",
                        DisplayOrder = 2
                    },
                    new Banner
                    {
                        Title = "MỪNG KHAI TRƯƠNG",
                        Subtitle = "Ưu đãi cực sốc - Săn deal giá hời",
                        ImageUrl = "/images/banners/banner-khaitruong.jpg",
                        LinkUrl = "#",
                        Position = "MainSlider",
                        DisplayOrder = 3
                    },
                    new Banner
                    {
                        Title = "LAPTOP BACK TO SCHOOL",
                        Subtitle = "Đặc quyền HSSV - Giảm thêm 10%",
                        ImageUrl = "/images/banners/banner-laptop.jpg",
                        LinkUrl = "#",
                        Position = "MainSlider",
                        DisplayOrder = 4
                    },
                    // Right Widget Banners
                    new Banner
                    {
                        Title = "Thu cũ đổi mới",
                        Subtitle = "Trợ giá lên đời đến 4 triệu",
                        ImageUrl = "/images/banners/banner-tradein.jpg",
                        LinkUrl = "/TradeIn",
                        Position = "RightWidget",
                        DisplayOrder = 1
                    },
                    new Banner
                    {
                        Title = "Ưu đãi Smember",
                        Subtitle = "Tích điểm & giảm thêm đến 5%",
                        ImageUrl = "/images/banners/banner-smember.jpg",
                        LinkUrl = "/Account/Profile",
                        Position = "RightWidget",
                        DisplayOrder = 2
                    },
                    // Big Promotional Banners trang chủ
                    new Banner
                    {
                        Title = "ĐẠI TIỆC CÔNG NGHỆ - DEAL KHỦNG ĐẾN 50%",
                        Subtitle = "CellphoneS Siêu Hội Mua Sắm - Trợ giá lên đời đến 4 triệu, trả góp 0%",
                        ImageUrl = "/images/banners/banner-megasale.jpg",
                        LinkUrl = "/TradeIn",
                        Position = "BigBanner",
                        DisplayOrder = 1,
                        IsActive = true
                    },
                    new Banner
                    {
                        Title = "MỪNG KHAI TRƯƠNG CHI NHÁNH MỚI",
                        Subtitle = "Bùng nổ quà tặng & giảm giá sốc phụ kiện, âm thanh, điện thoại chính hãng",
                        ImageUrl = "/images/banners/banner-khaitruong.jpg",
                        LinkUrl = "/Product/Category/dien-thoai",
                        Position = "BigBanner",
                        DisplayOrder = 2,
                        IsActive = true
                    },
                    // Popup Banner quảng cáo khi vào trang chủ
                    new Banner
                    {
                        Title = "ĐẠI TIỆC CÔNG NGHỆ - DEAL KHỦNG 50%",
                        Subtitle = "Săn deal iPhone 16 Pro Max, Galaxy S24 Ultra & Laptop giảm đến 50%",
                        ImageUrl = "/images/banners/banner-megasale.jpg",
                        LinkUrl = "/TradeIn",
                        Position = "Popup",
                        DisplayOrder = 1,
                        IsActive = true
                    }
                };
                context.Banners.AddRange(banners);
                context.SaveChanges();
            }
            else
            {
                // Nếu chưa có Popup nào, tự động thêm Popup quảng cáo mẫu
                if (!context.Banners.Any(b => b.Position == "Popup" || b.Position == "PopupBanner"))
                {
                    context.Banners.Add(
                        new Banner
                        {
                            Title = "ĐẠI TIỆC CÔNG NGHỆ - DEAL KHỦNG 50%",
                            Subtitle = "Săn deal iPhone 16 Pro Max, Galaxy S24 Ultra & Laptop giảm đến 50%",
                            ImageUrl = "/images/banners/banner-megasale.jpg",
                            LinkUrl = "/TradeIn",
                            Position = "Popup",
                            DisplayOrder = 1,
                            IsActive = true
                        }
                    );
                    context.SaveChanges();
                }

                // Nếu chưa có BigBanner nào, tự động thêm BigBanner mẫu
                if (!context.Banners.Any(b => b.Position == "BigBanner" || b.Position == "MiddleBanner"))
                {
                    context.Banners.AddRange(
                        new Banner
                        {
                            Title = "ĐẠI TIỆC CÔNG NGHỆ - DEAL KHỦNG ĐẾN 50%",
                            Subtitle = "CellphoneS Siêu Hội Mua Sắm - Trợ giá lên đời đến 4 triệu, trả góp 0%",
                            ImageUrl = "/images/banners/banner-megasale-wide.jpg",
                            LinkUrl = "/TradeIn",
                            Position = "BigBanner",
                            DisplayOrder = 1,
                            IsActive = true
                        },
                        new Banner
                        {
                            Title = "MỪNG KHAI TRƯƠNG CHI NHÁNH MỚI",
                            Subtitle = "Bùng nổ quà tặng & giảm giá sốc phụ kiện, âm thanh, điện thoại chính hãng",
                            ImageUrl = "/images/banners/banner-khaitruong-wide.jpg",
                            LinkUrl = "/Product/Category/dien-thoai",
                            Position = "BigBanner",
                            DisplayOrder = 2,
                            IsActive = true
                        }
                    );
                    context.SaveChanges();
                }

                // Cập nhật lại banners nếu còn dùng link CDN hỏng cũ hoặc ảnh chưa tối ưu
                var existingBanners = context.Banners.ToList();
                bool bannerUpdated = false;
                foreach (var b in existingBanners)
                {
                    if ((b.Position == "BigBanner" || b.Position == "MiddleBanner") && b.ImageUrl.Contains("banner-megasale.jpg"))
                    {
                        b.ImageUrl = "/images/banners/banner-megasale-wide.jpg";
                        bannerUpdated = true;
                    }
                    else if ((b.Position == "BigBanner" || b.Position == "MiddleBanner") && b.ImageUrl.Contains("banner-khaitruong.jpg"))
                    {
                        b.ImageUrl = "/images/banners/banner-khaitruong-wide.jpg";
                        bannerUpdated = true;
                    }
                    else if (b.Title.Contains("IPHONE 16", StringComparison.OrdinalIgnoreCase))
                    {
                        b.ImageUrl = "/images/banners/banner-iphone16.jpg";
                        bannerUpdated = true;
                    }
                    else if (b.Title.Contains("GALAXY S24", StringComparison.OrdinalIgnoreCase))
                    {
                        b.ImageUrl = "/images/banners/banner-s24ultra.jpg";
                        bannerUpdated = true;
                    }
                    else if ((b.Position == "MainSlider") && (b.Title.Contains("KHAI TRƯƠNG", StringComparison.OrdinalIgnoreCase) || b.Title.Contains("KHAI TRUONG", StringComparison.OrdinalIgnoreCase)))
                    {
                        b.ImageUrl = "/images/banners/banner-khaitruong.jpg";
                        bannerUpdated = true;
                    }
                    else if (b.Title.Contains("LAPTOP", StringComparison.OrdinalIgnoreCase))
                    {
                        b.ImageUrl = "/images/banners/banner-laptop.jpg";
                        bannerUpdated = true;
                    }
                    else if (b.Title.Contains("Thu c", StringComparison.OrdinalIgnoreCase) || b.Title.Contains("Thu cũ", StringComparison.OrdinalIgnoreCase))
                    {
                        b.ImageUrl = "/images/banners/banner-tradein.jpg";
                        b.LinkUrl = "/TradeIn";
                        bannerUpdated = true;
                    }
                    else if (b.Title.Contains("Smember", StringComparison.OrdinalIgnoreCase) || b.Title.Contains("Phụ kiện", StringComparison.OrdinalIgnoreCase) || b.Title.Contains("Phu kien", StringComparison.OrdinalIgnoreCase))
                    {
                        b.Title = "Ưu đãi Smember";
                        b.Subtitle = "Tích điểm & giảm thêm đến 5%";
                        b.ImageUrl = "/images/banners/banner-smember.jpg";
                        b.LinkUrl = "/Account/Profile";
                        bannerUpdated = true;
                    }
                }
                if (bannerUpdated)
                {
                    context.SaveChanges();
                }
            }

            int idPhone = cats["dien-thoai"];
            int idLaptop = cats["laptop"];
            int idTablet = cats["tablet"];
            int idAudio = cats["am-thanh"];
            int idWatch = cats["dong-ho"];
            int idAccessories = cats["phu-kien"];
            int idHome = cats["gia-dung"];
            int idApple = brs["apple"];
            int idSamsung = brs["samsung"];
            int idXiaomi = brs["xiaomi"];
            int idOppo = brs["oppo"];
            int idVivo = brs["vivo"];
            int idAsus = brs["asus"];
            int idSony = brs["sony"];

            // 4. Seed Sản phẩm mẫu thực tế
            var products = new List<Product>
            {
                // Product 1: iPhone 16 Pro Max
                new Product
                {
                    Name = "iPhone 16 Pro Max",
                    Slug = "iphone-16-pro-max",
                    CategoryId = idPhone,
                    BrandId = idApple,
                    OriginalPrice = 34990000,
                    SalePrice = 30990000,
                    ImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/iphone-16-pro-max.png",
                    ShortDescription = "Màn hình 6.9 inch Super Retina XDR, chip A18 Pro, camera 48MP điều khiển chụp ảnh mới.",
                    Description = "iPhone 16 Pro Max sở hữu thiết kế titan cấp độ 5 tuyệt đẹp và bền nhẹ, với viền mỏng nhất từng có trên các sản phẩm Apple. Màn hình Super Retina XDR 6.9 inch lớn hơn. Chip A18 Pro mang đến hiệu năng chưa từng có với đồ họa ray-tracing mạnh mẽ.",
                    TechnicalSpecs = "Màn hình: 6.9 inch Super Retina XDR 120Hz | Chip: Apple A18 Pro | RAM: 8GB | Bộ nhớ trong: 256GB | Camera sau: 48MP + 48MP + 12MP | Pin: 4685 mAh",
                    IsHot = true,
                    IsFeatured = true,
                    IsFlashSale = true,
                    FlashSalePrice = 29990000,
                    FlashSaleQuantity = 50,
                    FlashSaleSold = 42,
                    Rating = 5.0,
                    ReviewCount = 89,
                    StockQuantity = 100,
                    SmemberDiscount = 300000,
                    PromotionInfo = "Thu cũ trợ giá đến 4 triệu | Tặng gói iCloud 50GB 3 tháng",
                    HasInstallment0Percent = true,
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { ColorName = "Titan Sa Mạc", ColorHex = "#C8B19E", Storage = "256GB", Price = 30990000, StockQuantity = 30, ImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/iphone-16-pro-max.png" },
                        new ProductVariant { ColorName = "Titan Tự Nhiên", ColorHex = "#9F9E99", Storage = "256GB", Price = 30990000, StockQuantity = 25, ImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/iphone-16-pro-max-natural.png" },
                        new ProductVariant { ColorName = "Titan Trắng", ColorHex = "#F2F3F4", Storage = "256GB", Price = 30990000, StockQuantity = 20, ImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/iphone-16-pro-max-white.png" },
                        new ProductVariant { ColorName = "Titan Đen", ColorHex = "#3C3B3D", Storage = "256GB", Price = 30990000, StockQuantity = 25, ImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/iphone-16-pro-max-black.png" },
                        new ProductVariant { ColorName = "Titan Sa Mạc", ColorHex = "#C8B19E", Storage = "512GB", Price = 36990000, StockQuantity = 15, ImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/iphone-16-pro-max.png" }
                    }
                },

                // Product 2: Samsung Galaxy S24 Ultra
                new Product
                {
                    Name = "Samsung Galaxy S24 Ultra",
                    Slug = "samsung-galaxy-s24-ultra",
                    CategoryId = idPhone,
                    BrandId = idSamsung,
                    OriginalPrice = 33990000,
                    SalePrice = 25490000,
                    ImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/s/a/samsung-galaxy-s24-ultra_1.png",
                    ShortDescription = "Quyền năng Galaxy AI, bút S-Pen tích hợp, khung viền Titanium, camera 200MP zoom quang 100x.",
                    Description = "Galaxy S24 Ultra mở ra kỷ nguyên mới với công nghệ Galaxy AI tiên tiến: Khoanh tròn để tìm kiếm, Trợ lý phiên dịch trực tiếp và Trợ lý Note. Khung viền titan cùng kính Corning Gorilla Armor chống phản chiếu vượt trội.",
                    TechnicalSpecs = "Màn hình: 6.8 inch Dynamic AMOLED 2X 120Hz 2600 nits | Chip: Snapdragon 8 Gen 3 for Galaxy | RAM: 12GB | Bộ nhớ trong: 256GB | Camera sau: 200MP + 50MP + 12MP + 10MP | Pin: 5000 mAh, sạc nhanh 45W",
                    IsHot = true,
                    IsFeatured = true,
                    IsFlashSale = true,
                    FlashSalePrice = 24890000,
                    FlashSaleQuantity = 30,
                    FlashSaleSold = 27,
                    Rating = 4.9,
                    ReviewCount = 65,
                    StockQuantity = 80,
                    SmemberDiscount = 250000,
                    PromotionInfo = "Thu cũ trợ giá đến 5 triệu | Tặng sạc nhanh Samsung 45W",
                    HasInstallment0Percent = true,
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { ColorName = "Xám Titan", ColorHex = "#797672", Storage = "12GB - 256GB", Price = 25490000, StockQuantity = 20, ImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/s/a/samsung-galaxy-s24-ultra_1.png" },
                        new ProductVariant { ColorName = "Đen Titan", ColorHex = "#2B2A29", Storage = "12GB - 256GB", Price = 25490000, StockQuantity = 20, ImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/s/2/s24-ultra-den.png" },
                        new ProductVariant { ColorName = "Tím Titan", ColorHex = "#605668", Storage = "12GB - 256GB", Price = 25490000, StockQuantity = 15, ImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/s/2/s24-ultra-tim.png" }
                    }
                },

                // Product 3: iPhone 15
                new Product
                {
                    Name = "iPhone 15",
                    Slug = "iphone-15",
                    CategoryId = idPhone,
                    BrandId = idApple,
                    OriginalPrice = 22990000,
                    SalePrice = 18490000,
                    ImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/iphone-15-plus_1_.png",
                    ShortDescription = "Dynamic Island kỳ diệu, Camera 48MP sắc nét, Cổng sạc USB-C tiện dụng.",
                    Description = "iPhone 15 mang Dynamic Island tới phiên bản tiêu chuẩn, mặt lưng kính pha màu độ bền cao, camera chính 48MP hỗ trợ chụp chân dung thế hệ mới.",
                    TechnicalSpecs = "Màn hình: 6.1 inch OLED Super Retina XDR | Chip: Apple A16 Bionic | RAM: 6GB | Bộ nhớ trong: 128GB | Camera sau: 48MP + 12MP | Pin: 3349 mAh",
                    IsHot = false,
                    IsFeatured = true,
                    IsFlashSale = true,
                    FlashSalePrice = 17990000,
                    FlashSaleQuantity = 40,
                    FlashSaleSold = 36,
                    Rating = 4.8,
                    ReviewCount = 52,
                    StockQuantity = 60,
                    SmemberDiscount = 180000,
                    PromotionInfo = "Tặng củ sạc nhanh 20W chính hãng",
                    HasInstallment0Percent = true
                },

                // Product 4: Xiaomi 14
                new Product
                {
                    Name = "Xiaomi 14",
                    Slug = "xiaomi-14",
                    CategoryId = idPhone,
                    BrandId = idXiaomi,
                    OriginalPrice = 22990000,
                    SalePrice = 19490000,
                    ImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/x/i/xiaomi-14_2__2.png",
                    ShortDescription = "Hệ thống ống kính Leica quang học đỉnh cao, Snapdragon 8 Gen 3, màn hình 1.5K 120Hz mượt mà.",
                    Description = "Xiaomi 14 là chiếc flagship nhỏ gọn hoàn hảo với cụm 3 camera Leica Summilux đỉnh cao, chip Snapdragon 8 Gen 3 mạnh mẽ và hệ điều hành Xiaomi HyperOS mượt mà.",
                    TechnicalSpecs = "Màn hình: 6.36 inch AMOLED 1.5K 120Hz 3000 nits | Chip: Snapdragon 8 Gen 3 | RAM: 12GB | Bộ nhớ trong: 256GB | Pin: 4610 mAh, Sạc nhanh 90W",
                    IsHot = true,
                    IsFeatured = true,
                    IsFlashSale = false,
                    Rating = 4.9,
                    ReviewCount = 38,
                    StockQuantity = 45,
                    SmemberDiscount = 200000,
                    PromotionInfo = "Bảo hành 24 tháng chính hãng | 1 đổi 1 trong 30 ngày",
                    HasInstallment0Percent = true
                },

                // Product 5: OPPO Find N3 Flip
                new Product
                {
                    Name = "OPPO Find N3 Flip",
                    Slug = "oppo-find-n3-flip",
                    CategoryId = idPhone,
                    BrandId = idOppo,
                    OriginalPrice = 22990000,
                    SalePrice = 16990000,
                    ImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/o/p/oppo-find-n3-flip_1.png",
                    ShortDescription = "Điện thoại gập dọc phong cách, 3 camera Hasselblad chuyên nghiệp, màn hình phụ tiện lợi.",
                    Description = "OPPO Find N3 Flip mang đến bản lề tàng hình bền bỉ, màn hình phụ dọc tương thích nhiều ứng dụng, cụm 3 camera đỉnh cao hợp tác cùng hãng máy ảnh Hasselblad.",
                    TechnicalSpecs = "Màn hình chính: 6.8 inch AMOLED 120Hz | Màn hình phụ: 3.26 inch | Chip: Dimensity 9200 | RAM: 12GB | Pin: 4300 mAh, Sạc 44W",
                    IsHot = false,
                    IsFeatured = true,
                    IsFlashSale = true,
                    FlashSalePrice = 16490000,
                    FlashSaleQuantity = 20,
                    FlashSaleSold = 15,
                    Rating = 4.7,
                    ReviewCount = 29,
                    StockQuantity = 30,
                    SmemberDiscount = 150000,
                    PromotionInfo = "Trợ giá thu cũ 2 triệu | Tặng ốp lưng thời trang",
                    HasInstallment0Percent = true
                },

                // Product 6: Samsung Galaxy Z Flip6
                new Product
                {
                    Name = "Samsung Galaxy Z Flip6",
                    Slug = "samsung-galaxy-z-flip6",
                    CategoryId = idPhone,
                    BrandId = idSamsung,
                    OriginalPrice = 28990000,
                    SalePrice = 23990000,
                    ImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/s/a/samsung-galaxy-z-flip-6-xanh-blue.png",
                    ShortDescription = "Điện thoại gập AI cao cấp, FlexCam 50MP, Snapdragon 8 Gen 3 for Galaxy, thiết kế sắc sảo.",
                    Description = "Galaxy Z Flip6 nhỏ gọn và thời thượng, tích hợp Galaxy AI với tính năng Phiên dịch cuộc trò chuyện trên màn hình kép FlexWindow.",
                    TechnicalSpecs = "Màn hình: 6.7 inch Dynamic AMOLED 2X 120Hz | Màn hình phụ: 3.4 inch Super AMOLED | RAM: 12GB | ROM: 256GB | Pin: 4000 mAh",
                    IsHot = true,
                    IsFeatured = true,
                    IsFlashSale = false,
                    Rating = 4.9,
                    ReviewCount = 41,
                    StockQuantity = 40,
                    SmemberDiscount = 250000,
                    PromotionInfo = "Tặng 1 năm Samsung Care+ bảo hành rơi vỡ",
                    HasInstallment0Percent = true
                },

                // Product 7: vivo V40 Lite
                new Product
                {
                    Name = "vivo V40 Lite",
                    Slug = "vivo-v40-lite",
                    CategoryId = idPhone,
                    BrandId = idVivo,
                    OriginalPrice = 8490000,
                    SalePrice = 7990000,
                    ImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/d/i/dien-thoai-vivo-v40-lite-8gb-256gb.png",
                    ShortDescription = "Đèn Aura AI chân dung thông minh, sạc siêu tốc 80W, thiết kế kim cương sang trọng.",
                    Description = "vivo V40 Lite dẫn đầu phân khúc tầm trung với camera chân dung Aura Light AI, pin khủng 5000mAh cùng sạc FlashCharge 80W bền bỉ suốt 4 năm.",
                    TechnicalSpecs = "Màn hình: 6.67 inch AMOLED 120Hz | Chip: Snapdragon 685 | RAM: 8GB (+8GB mở rộng) | ROM: 256GB | Pin: 5000 mAh, sạc 80W",
                    IsHot = false,
                    IsFeatured = true,
                    IsFlashSale = true,
                    FlashSalePrice = 7590000,
                    FlashSaleQuantity = 30,
                    FlashSaleSold = 22,
                    Rating = 4.8,
                    ReviewCount = 18,
                    StockQuantity = 50,
                    SmemberDiscount = 80000,
                    PromotionInfo = "Tặng tai nghe không dây vivo TWS Air",
                    HasInstallment0Percent = true,
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { ColorName = "Bạc Kim Cương", ColorHex = "#E8ECEF", Storage = "8GB - 256GB", Price = 7990000, StockQuantity = 25 },
                        new ProductVariant { ColorName = "Đen Titan", ColorHex = "#2B2B2B", Storage = "8GB - 256GB", Price = 7990000, StockQuantity = 25 },
                        new ProductVariant { ColorName = "Tím Mộng Mơ", ColorHex = "#B7A4D7", Storage = "8GB - 256GB", Price = 7990000, StockQuantity = 20 },
                        new ProductVariant { ColorName = "Bạc Kim Cương", ColorHex = "#E8ECEF", Storage = "12GB - 512GB", Price = 8990000, StockQuantity = 15 },
                        new ProductVariant { ColorName = "Đen Titan", ColorHex = "#2B2B2B", Storage = "12GB - 512GB", Price = 8990000, StockQuantity = 15 }
                    }
                },

                // Product 8: Xiaomi Redmi Note 13
                new Product
                {
                    Name = "Xiaomi Redmi Note 13",
                    Slug = "xiaomi-redmi-note-13",
                    CategoryId = idPhone,
                    BrandId = idXiaomi,
                    OriginalPrice = 4890000,
                    SalePrice = 4190000,
                    ImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/x/i/xiaomi-redmi-note-13_1_.png",
                    ShortDescription = "Camera 108MP siêu nét, màn hình AMOLED 120Hz viền mỏng, loa kép sống động.",
                    Description = "Redmi Note 13 là smartphone quốc dân với thiết kế vuông vắn thời trang, camera 108MP đỉnh cao, màn hình AMOLED sắc nét bảo vệ mắt.",
                    TechnicalSpecs = "Màn hình: 6.67 inch FHD+ AMOLED 120Hz | Chip: Snapdragon 685 | RAM: 6GB | ROM: 128GB | Pin: 5000 mAh, sạc nhanh 33W",
                    IsHot = false,
                    IsFeatured = true,
                    IsFlashSale = true,
                    FlashSalePrice = 3890000,
                    FlashSaleQuantity = 50,
                    FlashSaleSold = 45,
                    Rating = 4.7,
                    ReviewCount = 112,
                    StockQuantity = 120,
                    SmemberDiscount = 50000,
                    PromotionInfo = "Bảo hành 18 tháng chính hãng",
                    HasInstallment0Percent = true,
                    Variants = new List<ProductVariant>
                    {
                        new ProductVariant { ColorName = "Đen Bán Dạ", ColorHex = "#1F1F1F", Storage = "6GB - 128GB", Price = 4190000, StockQuantity = 30 },
                        new ProductVariant { ColorName = "Xanh Băng Tuyết", ColorHex = "#A2C2E2", Storage = "6GB - 128GB", Price = 4190000, StockQuantity = 25 },
                        new ProductVariant { ColorName = "Vàng Hoàng Hôn", ColorHex = "#F0D8A8", Storage = "6GB - 128GB", Price = 4190000, StockQuantity = 20 },
                        new ProductVariant { ColorName = "Đen Bán Dạ", ColorHex = "#1F1F1F", Storage = "8GB - 256GB", Price = 4890000, StockQuantity = 20 },
                        new ProductVariant { ColorName = "Xanh Băng Tuyết", ColorHex = "#A2C2E2", Storage = "8GB - 256GB", Price = 4890000, StockQuantity = 20 }
                    }
                },

                // Product 9: MacBook Air M2
                new Product
                {
                    Name = "MacBook Air M2 13 inch",
                    Slug = "macbook-air-m2-13-inch",
                    CategoryId = idLaptop,
                    BrandId = idApple,
                    OriginalPrice = 27990000,
                    SalePrice = 23690000,
                    ImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/m/a/macbook-air-m2-8gb-256gb.png",
                    ShortDescription = "Thiết kế siêu mỏng nhẹ thời thượng, chip Apple M2 vượt trội, thời lượng pin lên đến 18 giờ.",
                    Description = "MacBook Air M2 kết hợp hoàn hảo giữa độ mỏng kinh ngạc và hiệu năng vượt bậc. Màn hình Liquid Retina 13.6 inch rực rỡ cùng sạc MagSafe 3 tiện lợi.",
                    TechnicalSpecs = "CPU: Apple M2 8-core | GPU: 8-core | RAM: 8GB Unified | SSD: 256GB | Màn hình: 13.6 inch Liquid Retina 2560x1664 | Pin: 18 giờ",
                    IsHot = true,
                    IsFeatured = true,
                    IsFlashSale = false,
                    Rating = 4.9,
                    ReviewCount = 74,
                    StockQuantity = 35,
                    SmemberDiscount = 300000,
                    PromotionInfo = "Tặng túi chống sốc cao cấp & Chuột không dây",
                    HasInstallment0Percent = true
                },

                // Product 10: ASUS TUF Gaming F15
                new Product
                {
                    Name = "ASUS TUF Gaming F15 FX507ZC4",
                    Slug = "asus-tuf-gaming-f15",
                    CategoryId = idLaptop,
                    BrandId = idAsus,
                    OriginalPrice = 21990000,
                    SalePrice = 18490000,
                    ImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/t/e/text_ng_n_1__1_72.png",
                    ShortDescription = "Laptop gaming chuẩn độ bền quân đội, Core i5 12500H, card đồ họa RTX 3050 4GB, màn hình 144Hz.",
                    Description = "ASUS TUF Gaming F15 sẵn sàng cho mọi tựa game eSports với bộ vi xử lý Intel thế hệ 12, tản nhiệt Arc Flow Fans 84 cánh và bàn phím RGB phong cách cơ học.",
                    TechnicalSpecs = "CPU: Intel Core i5-12500H | RAM: 16GB DDR4 3200MHz | SSD: 512GB NVMe PCIe | VGA: NVIDIA GeForce RTX 3050 4GB | Màn hình: 15.6 inch FHD 144Hz",
                    IsHot = true,
                    IsFeatured = true,
                    IsFlashSale = false,
                    Rating = 4.8,
                    ReviewCount = 33,
                    StockQuantity = 25,
                    SmemberDiscount = 200000,
                    PromotionInfo = "Tặng Balo Gaming ASUS TUF & Chuột Gaming",
                    HasInstallment0Percent = true
                },

                // Product 11: Apple AirPods Pro 2 USB-C
                new Product
                {
                    Name = "Tai nghe Bluetooth Apple AirPods Pro 2 2023 Type-C",
                    Slug = "apple-airpods-pro-2-type-c",
                    CategoryId = idAudio,
                    BrandId = idApple,
                    OriginalPrice = 6190000,
                    SalePrice = 5190000,
                    ImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/a/p/apple-airpods-pro-2-usb-c_1.png",
                    ShortDescription = "Chống ồn chủ động ANC gấp 2 lần, Âm Thanh Thích Ứng, Hộp sạc MagSafe USB-C chống bụi IP54.",
                    Description = "AirPods Pro 2 trang bị chip Apple H2 mang đến khả năng khử ồn xuất sắc gấp đôi, chế độ Xuyên Âm thích ứng và âm thanh không gian cá nhân hóa Spatial Audio sống động.",
                    TechnicalSpecs = "Chip: Apple H2 | Chống ồn: Active Noise Cancellation | Thời lượng pin: 6 giờ (30 giờ với hộp) | Cổng sạc: USB-C & MagSafe",
                    IsHot = true,
                    IsFeatured = true,
                    IsFlashSale = true,
                    FlashSalePrice = 4890000,
                    FlashSaleQuantity = 40,
                    FlashSaleSold = 38,
                    Rating = 5.0,
                    ReviewCount = 105,
                    StockQuantity = 50,
                    SmemberDiscount = 100000,
                    PromotionInfo = "Tặng case silicon bảo vệ tai nghe",
                    HasInstallment0Percent = true
                },

                // Product 12: iPad Air 6 M2 11 inch
                new Product
                {
                    Name = "iPad Air 6 M2 11 inch",
                    Slug = "ipad-air-6-m2-11-inch",
                    CategoryId = idTablet,
                    BrandId = idApple,
                    OriginalPrice = 16990000,
                    SalePrice = 15390000,
                    ImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/ipad-air-6-11-inch.png",
                    ShortDescription = "Chip Apple M2 cực mạnh, hỗ trợ Apple Pencil Pro, màn hình Liquid Retina chống chói tuyệt đẹp.",
                    Description = "iPad Air thế hệ mới mang sức mạnh đột phá từ chip Apple M2, thiết kế nhôm nguyên khối sang trọng, tương thích bút Apple Pencil Pro và Magic Keyboard chuyên nghiệp.",
                    TechnicalSpecs = "Màn hình: 11 inch Liquid Retina IPS LCD | Chip: Apple M2 8-core | Bộ nhớ: 128GB | Camera: 12MP trước & sau | Trọng lượng: 462g",
                    IsHot = false,
                    IsFeatured = true,
                    IsFlashSale = false,
                    Rating = 4.9,
                    ReviewCount = 28,
                    StockQuantity = 30,
                    SmemberDiscount = 200000,
                    PromotionInfo = "Giảm 10% khi mua kèm bút Apple Pencil",
                    HasInstallment0Percent = true
                },

                // Product 13: Apple Watch Series 10
                new Product
                {
                    Name = "Apple Watch Series 10 Nhôm GPS 42mm",
                    Slug = "apple-watch-series-10-42mm",
                    CategoryId = idWatch,
                    BrandId = idApple,
                    OriginalPrice = 10990000,
                    SalePrice = 10290000,
                    ImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/a/p/apple-watch-series-10-nhom.png",
                    ShortDescription = "Màn hình OLED góc nhìn rộng nhất, thiết kế mỏng nhẹ kỷ lục, sạc nhanh 80% trong 30 phút.",
                    Description = "Apple Watch Series 10 sở hữu màn hình OLED góc nhìn rộng sáng hơn 40%, vỏ nhôm mỏng hơn 10% cùng chip S10 SiP thông minh với tính năng phát hiện ngưng thở khi ngủ.",
                    TechnicalSpecs = "Màn hình: OLED Retina Always-On | Kích thước: 42mm | Chống nước: 50m | Tính năng: ECG, SpO2, Đo nhiệt độ, Phát hiện ngưng thở khi ngủ",
                    IsHot = true,
                    IsFeatured = true,
                    IsFlashSale = true,
                    FlashSalePrice = 9890000,
                    FlashSaleQuantity = 25,
                    FlashSaleSold = 22,
                    Rating = 5.0,
                    ReviewCount = 47,
                    StockQuantity = 40,
                    SmemberDiscount = 100000,
                    PromotionInfo = "Tặng dây đeo thể thao phụ trợ",
                    HasInstallment0Percent = true
                },

                // Product 14: Samsung Galaxy Watch Ultra
                new Product
                {
                    Name = "Samsung Galaxy Watch Ultra LTE 47mm",
                    Slug = "samsung-galaxy-watch-ultra-lte",
                    CategoryId = idWatch,
                    BrandId = idSamsung,
                    OriginalPrice = 16990000,
                    SalePrice = 14490000,
                    ImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/s/a/samsung-galaxy-watch-ultra.png",
                    ShortDescription = "Khung viền Titanium chuẩn quân đội, chống nước 10ATM, GPS tần số kép L1+L5, pin lên đến 100 giờ.",
                    Description = "Galaxy Watch Ultra là chiếc smartwatch siêu bền bỉ đỉnh cao cho thể thao ngoài trời với chuẩn lặn biển 10ATM, phân tích chỉ số sức khỏe bằng Galaxy AI và thời lượng pin cực khủng.",
                    TechnicalSpecs = "Màn hình: 1.5 inch Super AMOLED 3000 nits | Kích thước: 47mm | Chất liệu: Titanium Grade 4 | Kháng nước: 10ATM / IP68 | Pin: 590 mAh",
                    IsHot = true,
                    IsFeatured = true,
                    IsFlashSale = false,
                    Rating = 4.9,
                    ReviewCount = 31,
                    StockQuantity = 20,
                    SmemberDiscount = 200000,
                    PromotionInfo = "Trợ giá thu cũ 2 triệu | Tặng dây đeo Trail Band",
                    HasInstallment0Percent = true
                },

                // Product 15: Loa Bluetooth Marshall Emberton II
                new Product
                {
                    Name = "Loa Bluetooth Marshall Emberton II Chính Hãng",
                    Slug = "loa-marshall-emberton-ii",
                    CategoryId = idAudio,
                    BrandId = idSony,
                    OriginalPrice = 4990000,
                    SalePrice = 3890000,
                    ImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/m/a/marshall-emberton-ii.png",
                    ShortDescription = "Âm thanh đa hướng True Stereophonic 360 độ, pin hơn 30 giờ, chuẩn kháng bụi nước IP67.",
                    Description = "Marshall Emberton II là mẫu loa Bluetooth di động mang âm thanh phong cách rock cổ điển với công nghệ True Stereophonic đa hướng sống động và thời lượng pin ấn tượng trên 30 giờ.",
                    TechnicalSpecs = "Công suất: 20W Class D | Chuẩn Bluetooth: 5.1 | Thời lượng pin: 30+ giờ | Kháng nước: IP67 | Trọng lượng: 700g",
                    IsHot = true,
                    IsFeatured = true,
                    IsFlashSale = true,
                    FlashSalePrice = 3590000,
                    FlashSaleQuantity = 30,
                    FlashSaleSold = 28,
                    Rating = 5.0,
                    ReviewCount = 82,
                    StockQuantity = 35,
                    SmemberDiscount = 100000,
                    PromotionInfo = "Bảo hành 12 tháng 1 đổi 1",
                    HasInstallment0Percent = true
                },

                // Product 16: Tai nghe chụp tai Sony WH-1000XM5
                new Product
                {
                    Name = "Tai nghe chụp tai Sony WH-1000XM5 Chống Ồn",
                    Slug = "tai-nghe-sony-wh-1000xm5",
                    CategoryId = idAudio,
                    BrandId = idSony,
                    OriginalPrice = 8990000,
                    SalePrice = 6990000,
                    ImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/s/o/sony-wh-1000xm5.png",
                    ShortDescription = "Công nghệ chống ồn hàng đầu thế giới với 2 bộ xử lý và 8 micro, âm thanh Hi-Res Audio chuẩn mực.",
                    Description = "Sony WH-1000XM5 định nghĩa lại trải nghiệm chống ồn với bộ xử lý tích hợp V1 kết hợp QN1, màng loa 30mm sợi carbon siêu nhẹ và thời lượng pin 30 giờ sạc siêu nhanh 3 phút được 3 giờ.",
                    TechnicalSpecs = "Driver: 30mm Carbon Fiber | Chống ồn: Auto NC Optimizer | Codec: LDAC, AAC, SBC | Pin: 30 giờ (bật ANC) | Trọng lượng: 250g",
                    IsHot = true,
                    IsFeatured = true,
                    IsFlashSale = false,
                    Rating = 4.9,
                    ReviewCount = 64,
                    StockQuantity = 25,
                    SmemberDiscount = 150000,
                    PromotionInfo = "Tặng giá treo tai nghe bằng nhôm sang trọng",
                    HasInstallment0Percent = true
                },

                // Product 17: Củ sạc nhanh Anker 511 Nano 3 30W
                new Product
                {
                    Name = "Củ sạc nhanh Anker 511 Nano 3 30W USB-C GaN",
                    Slug = "cu-sac-anker-511-nano-3-30w",
                    CategoryId = idAccessories,
                    BrandId = idApple,
                    OriginalPrice = 450000,
                    SalePrice = 320000,
                    ImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/a/n/anker-511-nano-3-30w.png",
                    ShortDescription = "Công nghệ GaN siêu nhỏ gọn, công suất 30W sạc nhanh iPhone 16 / iPad, công nghệ ActiveShield 2.0 an toàn.",
                    Description = "Anker 511 Nano 3 công suất 30W nhỏ hơn 70% so với củ sạc thông thường, sạc iPhone lên 50% chỉ trong 25 phút.",
                    TechnicalSpecs = "Cổng ra: Type-C | Công suất: 30W Max | Công nghệ: GaN, PowerIQ 3.0, ActiveShield 2.0 | Chân gập tiện lợi",
                    IsHot = false,
                    IsFeatured = true,
                    IsFlashSale = true,
                    FlashSalePrice = 289000,
                    FlashSaleQuantity = 100,
                    FlashSaleSold = 94,
                    Rating = 4.9,
                    ReviewCount = 210,
                    StockQuantity = 200,
                    SmemberDiscount = 20000,
                    PromotionInfo = "Bảo hành 18 tháng chính hãng",
                    HasInstallment0Percent = false
                },

                // Product 18: Pin sạc dự phòng Baseus Blade 100W 20000mAh
                new Product
                {
                    Name = "Pin dự phòng Baseus Blade 100W 20000mAh Mỏng Nhẹ",
                    Slug = "pin-du-phong-baseus-blade-100w-20000mah",
                    CategoryId = idAccessories,
                    BrandId = idXiaomi,
                    OriginalPrice = 1490000,
                    SalePrice = 1090000,
                    ImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/b/a/baseus-blade-100w.png",
                    ShortDescription = "Thiết kế siêu mỏng 18mm sạc được cho cả Laptop/MacBook, công suất 100W, màn hình LED kỹ thuật số.",
                    Description = "Pin sạc dự phòng Baseus Blade sở hữu công suất khủng 100W Power Delivery trong thiết kế mỏng ấn tượng, hỗ trợ sạc nhanh đồng thời 4 thiết bị cùng lúc.",
                    TechnicalSpecs = "Dung lượng: 20000mAh (74Wh) | Cổng ra: 2 Type-C (100W), 2 USB-A (30W) | Màn hình: LED hiển thị công suất thời gian thực",
                    IsHot = true,
                    IsFeatured = true,
                    IsFlashSale = false,
                    Rating = 4.8,
                    ReviewCount = 76,
                    StockQuantity = 50,
                    SmemberDiscount = 50000,
                    PromotionInfo = "Tặng cáp C to C 100W đi kèm",
                    HasInstallment0Percent = true
                },

                // Product 19: Robot hút bụi Xiaomi Robot Vacuum X20+
                new Product
                {
                    Name = "Robot hút bụi lau nhà Xiaomi Robot Vacuum X20+",
                    Slug = "xiaomi-robot-vacuum-x20-plus",
                    CategoryId = idHome,
                    BrandId = idXiaomi,
                    OriginalPrice = 12990000,
                    SalePrice = 9990000,
                    ImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/x/i/xiaomi-vacuum-x20-plus.png",
                    ShortDescription = "Trạm sạc đa năng tự giặt sấy giẻ lau và gom bụi tự động, lực hút siêu mạnh 6000Pa, điều hướng LDS laser.",
                    Description = "Xiaomi X20+ là siêu phẩm robot dọn nhà all-in-one giúp bạn rảnh tay tới 75 ngày nhờ trạm sạc tự động hút bụi và giặt sấy giẻ lau bằng luồng khí nóng thông minh.",
                    TechnicalSpecs = "Lực hút: 6000Pa | Dung lượng pin: 5200 mAh | Trạm sạc: Tự gom rác, tự giặt & sấy khô giẻ lau | Điều hướng: LDS Laser 360",
                    IsHot = true,
                    IsFeatured = true,
                    IsFlashSale = false,
                    Rating = 4.9,
                    ReviewCount = 39,
                    StockQuantity = 20,
                    SmemberDiscount = 200000,
                    PromotionInfo = "Tặng bộ phụ kiện giẻ lau và màng lọc HEPA",
                    HasInstallment0Percent = true
                }
            };

            foreach (var p in products)
            {
                if (!context.Products.Any(existing => existing.Slug == p.Slug || existing.Name.ToLower() == p.Name.ToLower()))
                {
                    context.Products.Add(p);
                }
            }
            context.SaveChanges();

            // 5. Seed Đơn hàng mẫu cho Admin Dashboard
            if (!context.Orders.Any())
            {
                var pIphone = context.Products.FirstOrDefault(p => p.Slug.Contains("iphone-16-pro-max"));
                var pSamsung = context.Products.FirstOrDefault(p => p.Slug.Contains("galaxy-s24-ultra"));
                var pMarshall = context.Products.FirstOrDefault(p => p.Slug.Contains("marshall-emberton-ii"));
                var pAnker = context.Products.FirstOrDefault(p => p.Slug.Contains("anker-30w"));
                var pWatch = context.Products.FirstOrDefault(p => p.Slug.Contains("apple-watch-series-10"));
                var pSony = context.Products.FirstOrDefault(p => p.Slug.Contains("sony-wh-1000xm5"));

                var sampleOrders = new List<Order>();

                if (pIphone != null)
                {
                    sampleOrders.Add(new Order
                    {
                        OrderCode = "CPS-" + new Random().Next(100000, 999999),
                        CustomerName = "Nguyễn Văn An",
                        PhoneNumber = "0987123456",
                        Email = "nguyenvanan@gmail.com",
                        Address = "123 Nguyễn Trãi, Phường 2, Quận 5",
                        Province = "Hồ Chí Minh",
                        PaymentMethod = "VNPay",
                        Status = OrderStatus.Completed,
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        TotalAmount = pIphone.SalePrice,
                        OrderDetails = new List<OrderDetail>
                        {
                            new OrderDetail
                            {
                                ProductId = pIphone.Id,
                                UnitPrice = pIphone.SalePrice,
                                Quantity = 1
                            }
                        }
                    });
                }

                if (pSamsung != null)
                {
                    sampleOrders.Add(new Order
                    {
                        OrderCode = "CPS-" + new Random().Next(100000, 999999),
                        CustomerName = "Trần Thị Mai",
                        PhoneNumber = "0912345678",
                        Email = "tranmai@outlook.com",
                        Address = "456 Cầu Giấy, Dịch Vọng",
                        Province = "Hà Nội",
                        PaymentMethod = "COD",
                        Status = OrderStatus.Shipping,
                        CreatedAt = DateTime.UtcNow.AddHours(-18),
                        TotalAmount = pSamsung.SalePrice,
                        OrderDetails = new List<OrderDetail>
                        {
                            new OrderDetail
                            {
                                ProductId = pSamsung.Id,
                                UnitPrice = pSamsung.SalePrice,
                                Quantity = 1
                            }
                        }
                    });
                }

                if (pMarshall != null && pAnker != null)
                {
                    sampleOrders.Add(new Order
                    {
                        OrderCode = "CPS-" + new Random().Next(100000, 999999),
                        CustomerName = "Lê Hoàng Long",
                        PhoneNumber = "0903456789",
                        Email = "hoanglong.dev@gmail.com",
                        Address = "78 Lê Duẩn, Hải Châu",
                        Province = "Đà Nẵng",
                        PaymentMethod = "COD",
                        Status = OrderStatus.Confirmed,
                        CreatedAt = DateTime.UtcNow.AddHours(-6),
                        TotalAmount = pMarshall.SalePrice + pAnker.SalePrice,
                        OrderDetails = new List<OrderDetail>
                        {
                            new OrderDetail
                            {
                                ProductId = pMarshall.Id,
                                UnitPrice = pMarshall.SalePrice,
                                Quantity = 1
                            },
                            new OrderDetail
                            {
                                ProductId = pAnker.Id,
                                UnitPrice = pAnker.SalePrice,
                                Quantity = 1
                            }
                        }
                    });
                }

                if (pWatch != null)
                {
                    sampleOrders.Add(new Order
                    {
                        OrderCode = "CPS-" + new Random().Next(100000, 999999),
                        CustomerName = "Phạm Thu Hà",
                        PhoneNumber = "0978901234",
                        Email = "phamthuha@gmail.com",
                        Address = "12 Trần Hưng Đạo",
                        Province = "Hồ Chí Minh",
                        PaymentMethod = "BankTransfer",
                        Status = OrderStatus.Pending,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-45),
                        TotalAmount = pWatch.SalePrice,
                        OrderDetails = new List<OrderDetail>
                        {
                            new OrderDetail
                            {
                                ProductId = pWatch.Id,
                                UnitPrice = pWatch.SalePrice,
                                Quantity = 1
                            }
                        }
                    });
                }

                if (pSony != null)
                {
                    sampleOrders.Add(new Order
                    {
                        OrderCode = "CPS-" + new Random().Next(100000, 999999),
                        CustomerName = "Đặng Quốc Bảo",
                        PhoneNumber = "0945678901",
                        Address = "89 Quang Trung, Gò Vấp",
                        Province = "Hồ Chí Minh",
                        PaymentMethod = "COD",
                        Status = OrderStatus.Pending,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                        TotalAmount = pSony.SalePrice,
                        OrderDetails = new List<OrderDetail>
                        {
                            new OrderDetail
                            {
                                ProductId = pSony.Id,
                                UnitPrice = pSony.SalePrice,
                                Quantity = 1
                            }
                        }
                    });
                }

                context.Orders.AddRange(sampleOrders);
                context.SaveChanges();
            }

            // Đảm bảo dữ liệu đơn hàng phong phú thuộc mọi trạng thái cho ngày hôm nay và các kỳ báo cáo
            try
            {
                var now = DateTime.UtcNow;
                var allOrders = context.Orders.Include(o => o.OrderDetails).OrderByDescending(o => o.Id).ToList();

                // Cập nhật SubtotalAmount nếu bị 0
                foreach (var o in allOrders)
                {
                    if (o.SubtotalAmount <= 0)
                    {
                        var detailSum = o.OrderDetails.Sum(d => d.UnitPrice * d.Quantity);
                        o.SubtotalAmount = detailSum > 0 ? detailSum : o.TotalAmount;
                    }
                }

                // Đảm bảo trong ngày hôm nay có đầy đủ các trạng thái đơn hàng (Hoàn thành, Đang giao, Xác nhận, Chờ xử lý, Đã hủy)
                var todayOrders = allOrders.Where(o => o.CreatedAt.Date == now.Date).ToList();
                if (todayOrders.Count < 5 && allOrders.Count >= 5)
                {
                    var statuses = new[] {
                        OrderStatus.Completed,
                        OrderStatus.Shipping,
                        OrderStatus.Confirmed,
                        OrderStatus.Pending,
                        OrderStatus.Cancelled
                    };

                    for (int i = 0; i < Math.Min(5, allOrders.Count); i++)
                    {
                        allOrders[i].CreatedAt = now.Date.AddHours(1 + i * 2).AddMinutes(15);
                        allOrders[i].Status = statuses[i];
                    }
                }
                context.SaveChanges();
            }
            catch { /* Bỏ qua nếu chưa sẵn sàng */ }

            // 6. Seed Tài khoản Admin & Customer mẫu
            if (!context.Users.Any())
            {
                var adminUser = new AppUser
                {
                    FullName = "Quản Trị Viên CellphoneS",
                    Email = "admin@cellphones.com.vn",
                    PhoneNumber = "0988888888",
                    PasswordHash = Helpers.PasswordHelper.HashPassword("Admin@123"),
                    Role = "Admin",
                    SmemberLevel = "S-VIP",
                    SmemberPoints = 9999,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var customerUser = new AppUser
                {
                    FullName = "Nguyễn Văn Khách",
                    Email = "customer@gmail.com",
                    PhoneNumber = "0901234567",
                    PasswordHash = Helpers.PasswordHelper.HashPassword("User@123"),
                    Role = "Customer",
                    SmemberLevel = "S-Mem",
                    SmemberPoints = 350,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                context.Users.AddRange(adminUser, customerUser);
                context.SaveChanges();
            }

            // 6.1 Bổ sung thêm khách hàng mẫu cho trang Quản lý khách hàng nếu < 5
            if (context.Users.Count(u => u.Role == "Customer") < 5)
            {
                var additionalCustomers = new List<AppUser>
                {
                    new AppUser
                    {
                        FullName = "Trần Thị Mai Anh",
                        Email = "maianh.tran@gmail.com",
                        PhoneNumber = "0982345678",
                        PasswordHash = Helpers.PasswordHelper.HashPassword("User@123"),
                        Role = "Customer",
                        SmemberLevel = "S-VIP",
                        SmemberPoints = 1250,
                        Gender = "Nữ",
                        Birthday = new DateTime(1996, 5, 14),
                        Province = "Hà Nội",
                        Address = "Số 45 Tràng Thi, Hoàn Kiếm, Hà Nội",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddMonths(-3)
                    },
                    new AppUser
                    {
                        FullName = "Lê Hoàng Nam",
                        Email = "hoangnam.le@gmail.com",
                        PhoneNumber = "0918765432",
                        PasswordHash = Helpers.PasswordHelper.HashPassword("User@123"),
                        Role = "Customer",
                        SmemberLevel = "S-Mem",
                        SmemberPoints = 680,
                        Gender = "Nam",
                        Birthday = new DateTime(1992, 11, 20),
                        Province = "Đà Nẵng",
                        Address = "120 Nguyễn Văn Linh, Hải Châu, Đà Nẵng",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddMonths(-1)
                    },
                    new AppUser
                    {
                        FullName = "Phạm Minh Tuấn",
                        Email = "minhtuan.pham@gmail.com",
                        PhoneNumber = "0933456789",
                        PasswordHash = Helpers.PasswordHelper.HashPassword("User@123"),
                        Role = "Customer",
                        SmemberLevel = "S-New",
                        SmemberPoints = 100,
                        Gender = "Nam",
                        Birthday = new DateTime(2000, 8, 9),
                        Province = "Hồ Chí Minh",
                        Address = "58 Cống Quỳnh, P. Phạm Ngũ Lão, Q.1, TP.HCM",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-5)
                    },
                    new AppUser
                    {
                        FullName = "Hoàng Thu Thảo",
                        Email = "thuthao.hoang@gmail.com",
                        PhoneNumber = "0977889900",
                        PasswordHash = Helpers.PasswordHelper.HashPassword("User@123"),
                        Role = "Customer",
                        SmemberLevel = "S-VIP",
                        SmemberPoints = 2100,
                        Gender = "Nữ",
                        Birthday = new DateTime(1995, 3, 28),
                        Province = "Cần Thơ",
                        Address = "24 Đường 30/4, Ninh Kiều, Cần Thơ",
                        IsActive = false,
                        CreatedAt = DateTime.UtcNow.AddMonths(-6)
                    }
                };

                foreach (var c in additionalCustomers)
                {
                    if (!context.Users.Any(u => u.Email.ToLower() == c.Email.ToLower() || u.PhoneNumber == c.PhoneNumber))
                    {
                        context.Users.Add(c);
                    }
                }
                context.SaveChanges();
            }

            // 7. Seed / Chuẩn hóa Biến thể & Hình ảnh chính hãng CellphoneS cho toàn bộ sản phẩm
            var allProds = context.Products
                .Include(p => p.Variants)
                .Include(p => p.Images)
                .ToList();
            var variantsToAdd = new List<ProductVariant>();
            var imagesToAdd = new List<ProductImage>();
            bool variantsUpdated = false;

            foreach (var prod in allProds)
            {
                // 7.1. Cập nhật ảnh đại diện sản phẩm chuẩn CDN CellphoneS
                string verifiedMainImg = GetCellphonesDefaultProductImage(prod.Name, prod.Slug, prod.ImageUrl);
                if (!string.IsNullOrEmpty(verifiedMainImg) && prod.ImageUrl != verifiedMainImg)
                {
                    prod.ImageUrl = verifiedMainImg;
                    variantsUpdated = true;
                }

                // 7.2. Chuẩn hóa tên Storage của biến thể cũ nếu có
                if (prod.Name.Contains("Galaxy S24 Ultra", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var v in prod.Variants)
                    {
                        if (v.Storage == "256GB") { v.Storage = "12GB - 256GB"; variantsUpdated = true; }
                        else if (v.Storage == "512GB") { v.Storage = "12GB - 512GB"; variantsUpdated = true; }
                    }
                }
                else if (prod.Name.Contains("Xiaomi 14", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var v in prod.Variants)
                    {
                        if (v.Storage == "256GB") { v.Storage = "12GB - 256GB"; variantsUpdated = true; }
                        else if (v.Storage == "512GB") { v.Storage = "16GB - 512GB"; variantsUpdated = true; }
                    }
                }
                else if (prod.Name.Contains("vivo V40 Lite", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var v in prod.Variants)
                    {
                        if (v.Storage == "256GB") { v.Storage = "8GB - 256GB"; variantsUpdated = true; }
                    }
                }

                // 7.3. Bổ sung đầy đủ các biến thể màu sắc & phiên bản còn thiếu từ danh mục chuẩn CellphoneS
                var standardVariants = GetStandardProductVariants(prod);
                foreach (var sv in standardVariants)
                {
                    bool exists = prod.Variants.Any(existing =>
                        existing.ColorName.Trim().Equals(sv.ColorName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                        existing.Storage.Trim().Equals(sv.Storage.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (!exists)
                    {
                        sv.ProductId = prod.Id;
                        sv.ImageUrl = GetCellphonesVariantImageUrl(prod.Name, sv.ColorName, prod.ImageUrl);
                        prod.Variants.Add(sv);
                        variantsToAdd.Add(sv);
                    }
                }

                // 7.4. Cập nhật ImageUrl chuẩn theo đúng màu sắc trên CellphoneS cho toàn bộ biến thể hiện có
                foreach (var v in prod.Variants)
                {
                    string colorImg = GetCellphonesVariantImageUrl(prod.Name, v.ColorName, prod.ImageUrl);
                    if (!string.IsNullOrEmpty(colorImg) && v.ImageUrl != colorImg)
                    {
                        v.ImageUrl = colorImg;
                        variantsUpdated = true;
                    }
                }

                // 7.5. Đồng bộ bộ sưu tập ảnh (ProductImages) theo từng màu của biến thể
                var distinctColorVariants = prod.Variants
                    .Where(v => !string.IsNullOrEmpty(v.ImageUrl))
                    .GroupBy(v => v.ColorName.Trim().ToLower())
                    .Select(g => g.First())
                    .ToList();

                foreach (var cv in distinctColorVariants)
                {
                    bool imgExists = prod.Images.Any(img =>
                        img.ImageUrl == cv.ImageUrl ||
                        (!string.IsNullOrEmpty(img.ColorKey) && img.ColorKey.Equals(cv.ColorName, StringComparison.OrdinalIgnoreCase)));

                    if (!imgExists && !string.IsNullOrEmpty(cv.ImageUrl))
                    {
                        imagesToAdd.Add(new ProductImage
                        {
                            ProductId = prod.Id,
                            ImageUrl = cv.ImageUrl,
                            ColorKey = cv.ColorName,
                            IsThumbnail = cv.ImageUrl == prod.ImageUrl
                        });
                    }
                }
            }

            if (variantsToAdd.Any())
            {
                context.ProductVariants.AddRange(variantsToAdd);
                context.SaveChanges();
            }
            if (imagesToAdd.Any())
            {
                context.ProductImages.AddRange(imagesToAdd);
                context.SaveChanges();
            }
            if (variantsUpdated)
            {
                context.SaveChanges();
            }

            // 8. Seed Đánh giá mẫu chân thực cho các sản phẩm
            if (!context.ProductReviews.Any())
            {
                var pIphone = context.Products.FirstOrDefault(p => p.Slug.Contains("iphone-16-pro-max"));
                var pSamsung = context.Products.FirstOrDefault(p => p.Slug.Contains("galaxy-s24-ultra"));
                var pXiaomi = context.Products.FirstOrDefault(p => p.Slug.Contains("xiaomi-14"));

                var sampleReviews = new List<ProductReview>();

                if (pIphone != null)
                {
                    sampleReviews.Add(new ProductReview
                    {
                        ProductId = pIphone.Id,
                        CustomerName = "Nguyễn Tuấn Kiệt",
                        PhoneNumber = "0983****12",
                        Rating = 5,
                        Content = "Máy xuất sắc! Màu Titan Sa Mạc bên ngoài nhìn rất sang trọng, viền màn hình siêu mỏng. Nút Camera Control dùng rất tiện khi chụp ảnh nhanh. Giao hàng CellphoneS 1 tiếng là có, bạn nhân viên còn dán cường lực rất kỹ.",
                        VariantInfo = "Titan Sa Mạc - 256GB",
                        IsVerifiedBuyer = true,
                        LikesCount = 18,
                        AdminReply = "CellphoneS xin chào anh Kiệt! Rất vui vì anh đã hài lòng với trải nghiệm sản phẩm và dịch vụ giao hàng tại CellphoneS. Chúc anh có những bức ảnh tuyệt đẹp cùng iPhone 16 Pro Max ạ!",
                        AdminReplyAt = DateTime.UtcNow.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-3)
                    });

                    sampleReviews.Add(new ProductReview
                    {
                        ProductId = pIphone.Id,
                        CustomerName = "Trần Minh Quân",
                        PhoneNumber = "0912****88",
                        Rating = 5,
                        Content = "Pin trâu hơn hẳn con 14 Pro Max cũ của mình, onscreen liên tục hơn 9 tiếng vẫn còn 25%. Màn hình 6.9 inch xem phim và chơi game quá đã. Smember được giảm thêm 300k và tặng gói bảo hành 1 đổi 1 30 ngày an tâm tuyệt đối.",
                        VariantInfo = "Titan Tự Nhiên - 256GB",
                        IsVerifiedBuyer = true,
                        LikesCount = 9,
                        AdminReply = "CellphoneS cảm ơn anh Quân đã đánh giá sản phẩm. Chúc anh một ngày ngập tràn niềm vui ạ!",
                        AdminReplyAt = DateTime.UtcNow.AddDays(-4),
                        CreatedAt = DateTime.UtcNow.AddDays(-5)
                    });

                    sampleReviews.Add(new ProductReview
                    {
                        ProductId = pIphone.Id,
                        CustomerName = "Lê Thị Mai Phương",
                        PhoneNumber = "0977****65",
                        Rating = 4,
                        Content = "Máy hơi to so với tay nữ một chút nhưng quen dần thì rất thích. Loa to, chụp ảnh đêm không bị noise. Màu Titan Trắng rất tinh tế và sạch sẽ.",
                        VariantInfo = "Titan Trắng - 256GB",
                        IsVerifiedBuyer = true,
                        LikesCount = 5,
                        AdminReply = "Dạ chào chị Phương, phiên bản 16 Pro Max màn hình lớn 6.9 inch hiển thị cực đã nhưng nếu chị muốn máy gọn hơn thì dòng 16 Pro 6.3 inch cũng rất tuyệt ạ. Cảm ơn chị đã tin tưởng CellphoneS!",
                        AdminReplyAt = DateTime.UtcNow.AddDays(-6),
                        CreatedAt = DateTime.UtcNow.AddDays(-7)
                    });
                }

                if (pSamsung != null)
                {
                    sampleReviews.Add(new ProductReview
                    {
                        ProductId = pSamsung.Id,
                        CustomerName = "Phạm Đình Bảo",
                        PhoneNumber = "0908****43",
                        Rating = 5,
                        Content = "Tính năng Galaxy AI thực sự hữu ích cho công việc, dịch thuật trực tiếp và tóm tắt ghi chú cực chuẩn. Màn hình phẳng chống chói ra nắng nhìn rõ mồn một. Bút S-Pen thì khỏi phải bàn rồi.",
                        VariantInfo = "Xám Titan - 256GB",
                        IsVerifiedBuyer = true,
                        LikesCount = 14,
                        AdminReply = "CellphoneS xin chào anh Bảo! Cảm ơn anh đã chia sẻ trải nghiệm thực tế với Galaxy S24 Ultra. Nếu cần hỗ trợ thêm về kỹ thuật anh cứ liên hệ hotline 1800.2097 anh nhé!",
                        AdminReplyAt = DateTime.UtcNow.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-2)
                    });

                    sampleReviews.Add(new ProductReview
                    {
                        ProductId = pSamsung.Id,
                        CustomerName = "Hoàng Kim Ngân",
                        PhoneNumber = "0934****79",
                        Rating = 5,
                        Content = "Zoom 100x đỉnh chóp, chụp sân khấu ca nhạc hay phong cảnh nét căng. Cửa hàng 213 Trần Quang Khải nhân viên phục vụ tận tình, hỗ trợ chuyển toàn bộ dữ liệu máy cũ sang máy mới trong 20 phút.",
                        VariantInfo = "Tím Titan - 256GB",
                        IsVerifiedBuyer = true,
                        LikesCount = 7,
                        AdminReply = "CellphoneS cảm ơn chị Ngân rất nhiều vì lời khen dành cho đội ngũ nhân viên chi nhánh 213 Trần Quang Khải ạ!",
                        AdminReplyAt = DateTime.UtcNow.AddDays(-3),
                        CreatedAt = DateTime.UtcNow.AddDays(-4)
                    });
                }

                if (pXiaomi != null)
                {
                    sampleReviews.Add(new ProductReview
                    {
                        ProductId = pXiaomi.Id,
                        CustomerName = "Đỗ Cao Thắng",
                        PhoneNumber = "0965****99",
                        Rating = 5,
                        Content = "Camera Leica màu sắc đậm chất nghệ thuật, máy nhỏ gọn cầm rất vừa tay. Sạc nhanh 90W cắm 30 phút là đầy pin dùng cả ngày. Quá hài lòng với tầm giá này.",
                        VariantInfo = "Đen Nhám - 256GB",
                        IsVerifiedBuyer = true,
                        LikesCount = 11,
                        AdminReply = "CellphoneS cảm ơn anh Thắng đã tin tưởng chọn mua Xiaomi 14 tại CellphoneS ạ!",
                        AdminReplyAt = DateTime.UtcNow.AddDays(-5),
                        CreatedAt = DateTime.UtcNow.AddDays(-6)
                    });
                }

                if (sampleReviews.Any())
                {
                    context.ProductReviews.AddRange(sampleReviews);
                    context.SaveChanges();
                }
            }

            // 11. Khởi tạo danh mục máy cũ phục vụ định giá thu cũ đổi mới (TradeInDevices)
            if (!context.TradeInDevices.Any())
            {
                var tradeInList = new List<TradeInDevice>
                {
                    // Apple
                    new TradeInDevice { Brand = "Apple", ModelName = "iPhone 15 Pro Max", Storage = "256GB", BasePrice = 23500000, DisplayOrder = 1 },
                    new TradeInDevice { Brand = "Apple", ModelName = "iPhone 15 Pro Max", Storage = "512GB", BasePrice = 25500000, DisplayOrder = 2 },
                    new TradeInDevice { Brand = "Apple", ModelName = "iPhone 15 Pro", Storage = "128GB", BasePrice = 19000000, DisplayOrder = 3 },
                    new TradeInDevice { Brand = "Apple", ModelName = "iPhone 15 Pro", Storage = "256GB", BasePrice = 21000000, DisplayOrder = 4 },
                    new TradeInDevice { Brand = "Apple", ModelName = "iPhone 15 Plus", Storage = "128GB", BasePrice = 15500000, DisplayOrder = 5 },
                    new TradeInDevice { Brand = "Apple", ModelName = "iPhone 15", Storage = "128GB", BasePrice = 14000000, DisplayOrder = 6 },
                    new TradeInDevice { Brand = "Apple", ModelName = "iPhone 14 Pro Max", Storage = "128GB", BasePrice = 17000000, DisplayOrder = 7 },
                    new TradeInDevice { Brand = "Apple", ModelName = "iPhone 14 Pro Max", Storage = "256GB", BasePrice = 18500000, DisplayOrder = 8 },
                    new TradeInDevice { Brand = "Apple", ModelName = "iPhone 14 Pro", Storage = "128GB", BasePrice = 14500000, DisplayOrder = 9 },
                    new TradeInDevice { Brand = "Apple", ModelName = "iPhone 14", Storage = "128GB", BasePrice = 11500000, DisplayOrder = 10 },
                    new TradeInDevice { Brand = "Apple", ModelName = "iPhone 13 Pro Max", Storage = "128GB", BasePrice = 13500000, DisplayOrder = 11 },
                    new TradeInDevice { Brand = "Apple", ModelName = "iPhone 13 Pro Max", Storage = "256GB", BasePrice = 14800000, DisplayOrder = 12 },
                    new TradeInDevice { Brand = "Apple", ModelName = "iPhone 13 Pro", Storage = "128GB", BasePrice = 11000000, DisplayOrder = 13 },
                    new TradeInDevice { Brand = "Apple", ModelName = "iPhone 13", Storage = "128GB", BasePrice = 9500000, DisplayOrder = 14 },
                    new TradeInDevice { Brand = "Apple", ModelName = "iPhone 12 Pro Max", Storage = "128GB", BasePrice = 10000000, DisplayOrder = 15 },
                    new TradeInDevice { Brand = "Apple", ModelName = "iPhone 12", Storage = "128GB", BasePrice = 6800000, DisplayOrder = 16 },
                    new TradeInDevice { Brand = "Apple", ModelName = "iPhone 11", Storage = "64GB", BasePrice = 4800000, DisplayOrder = 17 },
                    new TradeInDevice { Brand = "Apple", ModelName = "iPhone 11", Storage = "128GB", BasePrice = 5500000, DisplayOrder = 18 },

                    // Samsung
                    new TradeInDevice { Brand = "Samsung", ModelName = "Galaxy S23 Ultra", Storage = "256GB", BasePrice = 14000000, DisplayOrder = 19 },
                    new TradeInDevice { Brand = "Samsung", ModelName = "Galaxy S23 Ultra", Storage = "512GB", BasePrice = 15500000, DisplayOrder = 20 },
                    new TradeInDevice { Brand = "Samsung", ModelName = "Galaxy S23+", Storage = "256GB", BasePrice = 11000000, DisplayOrder = 21 },
                    new TradeInDevice { Brand = "Samsung", ModelName = "Galaxy S23", Storage = "128GB", BasePrice = 9000000, DisplayOrder = 22 },
                    new TradeInDevice { Brand = "Samsung", ModelName = "Galaxy S22 Ultra", Storage = "128GB", BasePrice = 10000000, DisplayOrder = 23 },
                    new TradeInDevice { Brand = "Samsung", ModelName = "Galaxy S22 Ultra", Storage = "256GB", BasePrice = 11000000, DisplayOrder = 24 },
                    new TradeInDevice { Brand = "Samsung", ModelName = "Galaxy Z Fold5", Storage = "256GB", BasePrice = 16500000, DisplayOrder = 25 },
                    new TradeInDevice { Brand = "Samsung", ModelName = "Galaxy Z Flip5", Storage = "256GB", BasePrice = 9500000, DisplayOrder = 26 },
                    new TradeInDevice { Brand = "Samsung", ModelName = "Galaxy S21 Ultra", Storage = "128GB", BasePrice = 7500000, DisplayOrder = 27 },

                    // Xiaomi
                    new TradeInDevice { Brand = "Xiaomi", ModelName = "Xiaomi 13 Pro", Storage = "256GB", BasePrice = 9500000, DisplayOrder = 28 },
                    new TradeInDevice { Brand = "Xiaomi", ModelName = "Xiaomi 13", Storage = "256GB", BasePrice = 8000000, DisplayOrder = 29 },
                    new TradeInDevice { Brand = "Xiaomi", ModelName = "Xiaomi 12 Pro", Storage = "256GB", BasePrice = 6500000, DisplayOrder = 30 },
                    new TradeInDevice { Brand = "Xiaomi", ModelName = "Redmi Note 12 Pro", Storage = "128GB", BasePrice = 3200000, DisplayOrder = 31 },

                    // OPPO
                    new TradeInDevice { Brand = "OPPO", ModelName = "OPPO Find N2 Flip", Storage = "256GB", BasePrice = 8500000, DisplayOrder = 32 },
                    new TradeInDevice { Brand = "OPPO", ModelName = "OPPO Reno10 Pro+", Storage = "256GB", BasePrice = 7500000, DisplayOrder = 33 },
                    new TradeInDevice { Brand = "OPPO", ModelName = "OPPO Reno8 Pro", Storage = "256GB", BasePrice = 5000000, DisplayOrder = 34 }
                };

                context.TradeInDevices.AddRange(tradeInList);
                context.SaveChanges();
            }

            // Bổ sung thiết bị cho các nhóm Laptop, Tablet, Đồng hồ, Tai nghe nếu chưa có
            if (!context.TradeInDevices.Any(d => d.DeviceType == "Laptop" || d.DeviceType == "Tablet" || d.DeviceType == "Đồng hồ" || d.DeviceType == "Tai nghe"))
            {
                var additionalDevices = new List<TradeInDevice>
                {
                    // LAPTOP
                    new TradeInDevice { Brand = "Apple", DeviceType = "Laptop", ModelName = "MacBook Air M1 13 inch", Storage = "256GB", BasePrice = 12000000, DisplayOrder = 35 },
                    new TradeInDevice { Brand = "Apple", DeviceType = "Laptop", ModelName = "MacBook Air M2 13 inch", Storage = "256GB", BasePrice = 15500000, DisplayOrder = 36 },
                    new TradeInDevice { Brand = "Apple", DeviceType = "Laptop", ModelName = "MacBook Pro 14 inch M2 Pro", Storage = "512GB", BasePrice = 28000000, DisplayOrder = 37 },
                    new TradeInDevice { Brand = "ASUS", DeviceType = "Laptop", ModelName = "ASUS Zenbook 14 OLED", Storage = "512GB", BasePrice = 13500000, DisplayOrder = 38 },
                    new TradeInDevice { Brand = "Dell", DeviceType = "Laptop", ModelName = "Dell XPS 13 Plus 9320", Storage = "512GB", BasePrice = 19500000, DisplayOrder = 39 },

                    // TABLET
                    new TradeInDevice { Brand = "Apple", DeviceType = "Tablet", ModelName = "iPad Pro 11 M2 Wifi", Storage = "128GB", BasePrice = 15000000, DisplayOrder = 40 },
                    new TradeInDevice { Brand = "Apple", DeviceType = "Tablet", ModelName = "iPad Air 5 M1 Wifi", Storage = "64GB", BasePrice = 10500000, DisplayOrder = 41 },
                    new TradeInDevice { Brand = "Apple", DeviceType = "Tablet", ModelName = "iPad Gen 10 10.9 inch", Storage = "64GB", BasePrice = 7200000, DisplayOrder = 42 },
                    new TradeInDevice { Brand = "Samsung", DeviceType = "Tablet", ModelName = "Galaxy Tab S9 Ultra", Storage = "256GB", BasePrice = 16000000, DisplayOrder = 43 },
                    new TradeInDevice { Brand = "Samsung", DeviceType = "Tablet", ModelName = "Galaxy Tab S9 FE", Storage = "128GB", BasePrice = 7000000, DisplayOrder = 44 },

                    // ĐỒNG HỒ
                    new TradeInDevice { Brand = "Apple", DeviceType = "Đồng hồ", ModelName = "Apple Watch Ultra 2 49mm", Storage = "Tiêu chuẩn", BasePrice = 14500000, DisplayOrder = 45 },
                    new TradeInDevice { Brand = "Apple", DeviceType = "Đồng hồ", ModelName = "Apple Watch Series 9 41mm", Storage = "Tiêu chuẩn", BasePrice = 6800000, DisplayOrder = 46 },
                    new TradeInDevice { Brand = "Apple", DeviceType = "Đồng hồ", ModelName = "Apple Watch Series 8 45mm", Storage = "Tiêu chuẩn", BasePrice = 5500000, DisplayOrder = 47 },
                    new TradeInDevice { Brand = "Samsung", DeviceType = "Đồng hồ", ModelName = "Galaxy Watch 6 Classic 43mm", Storage = "Tiêu chuẩn", BasePrice = 4500000, DisplayOrder = 48 },
                    new TradeInDevice { Brand = "Samsung", DeviceType = "Đồng hồ", ModelName = "Galaxy Watch 5 Pro", Storage = "Tiêu chuẩn", BasePrice = 3800000, DisplayOrder = 49 },

                    // TAI NGHE
                    new TradeInDevice { Brand = "Apple", DeviceType = "Tai nghe", ModelName = "AirPods Pro 2 Type-C", Storage = "Tiêu chuẩn", BasePrice = 3600000, DisplayOrder = 50 },
                    new TradeInDevice { Brand = "Apple", DeviceType = "Tai nghe", ModelName = "AirPods 3 Lightning", Storage = "Tiêu chuẩn", BasePrice = 2200000, DisplayOrder = 51 },
                    new TradeInDevice { Brand = "Apple", DeviceType = "Tai nghe", ModelName = "AirPods Max", Storage = "Tiêu chuẩn", BasePrice = 7500000, DisplayOrder = 52 },
                    new TradeInDevice { Brand = "Sony", DeviceType = "Tai nghe", ModelName = "Sony WH-1000XM5", Storage = "Tiêu chuẩn", BasePrice = 4800000, DisplayOrder = 53 },
                    new TradeInDevice { Brand = "Samsung", DeviceType = "Tai nghe", ModelName = "Galaxy Buds2 Pro", Storage = "Tiêu chuẩn", BasePrice = 1800000, DisplayOrder = 54 }
                };

                context.TradeInDevices.AddRange(additionalDevices);
                context.SaveChanges();
            }

            // 12. Khởi tạo phiếu mẫu Thu Cũ Lên Đời (TradeInRequests)
            if (!context.TradeInRequests.Any())
            {
                var demoCustomer = context.Users.FirstOrDefault(u => u.Email == "customer@cellphones.com.vn");
                var ip16 = context.Products.FirstOrDefault(p => p.Name.Contains("iPhone 16 Pro Max"));

                var sampleRequest = new TradeInRequest
                {
                    RequestCode = "CPS-TC-2410-0088",
                    UserId = demoCustomer?.Id,
                    CustomerName = demoCustomer != null ? demoCustomer.FullName : "Nguyễn Văn Khách",
                    PhoneNumber = demoCustomer != null ? demoCustomer.PhoneNumber : "0901234567",
                    Email = demoCustomer?.Email,
                    OldDeviceName = "iPhone 14 Pro Max",
                    OldDeviceStorage = "128GB",
                    ConditionGrade = 1,
                    BaseEstimatePrice = 17000000,
                    SubsidyAmount = 2000000,
                    TotalBenefit = 19000000,
                    TargetProductId = ip16?.Id,
                    TargetProductName = "iPhone 16 Pro Max",
                    TargetProductStorage = "256GB",
                    TargetProductPrice = 31990000,
                    PayDifference = 12990000,
                    BranchStore = "CellphoneS 213 Trần Quang Khải, Q.1, TP.HCM",
                    AppointmentDate = DateTime.UtcNow.AddDays(2),
                    CustomerNote = "Máy em dùng giữ kỹ ốp lưng dán màn hình từ đầu, mong shop thẩm định nhanh.",
                    Status = 1, // Đã xác nhận lịch hẹn
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                };

                context.TradeInRequests.Add(sampleRequest);
                context.SaveChanges();
            }

            // 13. Khởi tạo phiếu nhập hàng mẫu (WarehouseReceipts) nếu chưa có
            try
            {
                if (!context.WarehouseReceipts.Any())
                {
                    var ip16 = context.Products.Include(p => p.Variants).FirstOrDefault(p => p.Name.Contains("iPhone 16"));
                    var s24 = context.Products.Include(p => p.Variants).FirstOrDefault(p => p.Name.Contains("S24"));

                    if (ip16 != null || s24 != null)
                    {
                        var sampleReceipt = new WarehouseReceipt
                        {
                            ReceiptCode = "PNK" + DateTime.UtcNow.ToString("yyyyMMdd") + "-0001",
                            SupplierName = "Công ty TNHH Apple Việt Nam (FPT Synnex)",
                            SupplierPhone = "02838248888",
                            SupplierAddress = "Tầng 9, Toà nhà MPlaza Saigon, 39 Lê Duẩn, Q.1, TP.HCM",
                            CreatedBy = "Quản Trị Viên CellphoneS",
                            ImportDate = DateTime.UtcNow.AddDays(-5),
                            Note = "Nhập lô hàng chính hãng đợt 1 phục vụ mùa mở bán",
                            Status = ReceiptStatus.Completed,
                            CreatedAt = DateTime.UtcNow.AddDays(-5)
                        };

                        decimal total = 0;
                        if (ip16 != null)
                        {
                            var v = ip16.Variants.FirstOrDefault();
                            var detail1 = new WarehouseReceiptDetail
                            {
                                ProductId = ip16.Id,
                                ProductVariantId = v?.Id,
                                VariantInfo = v != null ? $"{v.ColorName} - {v.Storage}" : null,
                                Quantity = 50,
                                ImportPrice = ip16.SalePrice * 0.85m,
                                TotalPrice = 50 * (ip16.SalePrice * 0.85m),
                                Note = "Hàng mới 100% nguyên seal"
                            };
                            sampleReceipt.Details.Add(detail1);
                            total += detail1.TotalPrice;

                            context.InventoryLogs.Add(new InventoryLog
                            {
                                ProductId = ip16.Id,
                                ProductVariantId = v?.Id,
                                ChangeType = "Import",
                                QuantityChange = 50,
                                StockBefore = 0,
                                StockAfter = ip16.StockQuantity,
                                ReferenceCode = sampleReceipt.ReceiptCode,
                                Note = "Nhập kho đợt 1 từ FPT Synnex",
                                CreatedAt = DateTime.UtcNow.AddDays(-5)
                            });
                        }

                        if (s24 != null)
                        {
                            var v = s24.Variants.FirstOrDefault();
                            var detail2 = new WarehouseReceiptDetail
                            {
                                ProductId = s24.Id,
                                ProductVariantId = v?.Id,
                                VariantInfo = v != null ? $"{v.ColorName} - {v.Storage}" : null,
                                Quantity = 30,
                                ImportPrice = s24.SalePrice * 0.82m,
                                TotalPrice = 30 * (s24.SalePrice * 0.82m),
                                Note = "Samsung Vina phân phối chính thức"
                            };
                            sampleReceipt.Details.Add(detail2);
                            total += detail2.TotalPrice;

                            context.InventoryLogs.Add(new InventoryLog
                            {
                                ProductId = s24.Id,
                                ProductVariantId = v?.Id,
                                ChangeType = "Import",
                                QuantityChange = 30,
                                StockBefore = 0,
                                StockAfter = s24.StockQuantity,
                                ReferenceCode = sampleReceipt.ReceiptCode,
                                Note = "Nhập kho đợt 1 từ Samsung Vina",
                                CreatedAt = DateTime.UtcNow.AddDays(-5)
                            });
                        }

                        sampleReceipt.TotalAmount = total;
                        context.WarehouseReceipts.Add(sampleReceipt);
                        context.SaveChanges();
                    }
                }

                // Khởi tạo các mã giảm giá mẫu (Đơn hàng & Phí vận chuyển)
                if (!context.Coupons.Any())
                {
                    var coupons = new List<Coupon>
                    {
                        // Mã giảm giá cho đơn hàng (Order Discount)
                        new Coupon
                        {
                            Code = "CPSVIP500K",
                            Title = "Voucher Hội Viên Smember VIP",
                            Description = "Giảm trực tiếp 500.000₫ cho đơn hàng từ 5.000.000₫",
                            Type = CouponType.OrderDiscount,
                            DiscountValue = 500000,
                            IsPercentage = false,
                            MinOrderAmount = 5000000,
                            ExpiryDate = DateTime.UtcNow.AddDays(90),
                            IsActive = true
                        },
                        new Coupon
                        {
                            Code = "GIAM100K",
                            Title = "Ưu đãi Khách hàng thân thiết",
                            Description = "Giảm ngay 100.000₫ cho đơn hàng từ 1.000.000₫",
                            Type = CouponType.OrderDiscount,
                            DiscountValue = 100000,
                            IsPercentage = false,
                            MinOrderAmount = 1000000,
                            ExpiryDate = DateTime.UtcNow.AddDays(90),
                            IsActive = true
                        },
                        new Coupon
                        {
                            Code = "GIAM10PT",
                            Title = "Giảm 10% Siêu Sale Thiết Bị & Phụ Kiện",
                            Description = "Giảm 10% (tối đa 200.000₫) cho đơn từ 500.000₫",
                            Type = CouponType.OrderDiscount,
                            DiscountValue = 10,
                            IsPercentage = true,
                            MaxDiscountAmount = 200000,
                            MinOrderAmount = 500000,
                            ExpiryDate = DateTime.UtcNow.AddDays(90),
                            IsActive = true
                        },
                        new Coupon
                        {
                            Code = "PHUKIEN50K",
                            Title = "Voucher Phụ kiện công nghệ",
                            Description = "Giảm ngay 50.000₫ cho đơn hàng từ 250.000₫",
                            Type = CouponType.OrderDiscount,
                            DiscountValue = 50000,
                            IsPercentage = false,
                            MinOrderAmount = 250000,
                            ExpiryDate = DateTime.UtcNow.AddDays(90),
                            IsActive = true
                        },

                        // Mã giảm giá phí vận chuyển (Shipping Discount / Freeship)
                        new Coupon
                        {
                            Code = "FREESHIP30K",
                            Title = "Miễn phí vận chuyển tiêu chuẩn",
                            Description = "Giảm 30.000₫ phí giao hàng cho mọi đơn từ 200.000₫",
                            Type = CouponType.ShippingDiscount,
                            DiscountValue = 30000,
                            IsPercentage = false,
                            MinOrderAmount = 200000,
                            ExpiryDate = DateTime.UtcNow.AddDays(90),
                            IsActive = true
                        },
                        new Coupon
                        {
                            Code = "FREESHIP50K",
                            Title = "Trợ giá Giao hàng hỏa tốc",
                            Description = "Giảm 50.000₫ phí vận chuyển hỏa tốc cho đơn từ 1.000.000₫",
                            Type = CouponType.ShippingDiscount,
                            DiscountValue = 50000,
                            IsPercentage = false,
                            MinOrderAmount = 1000000,
                            ExpiryDate = DateTime.UtcNow.AddDays(90),
                            IsActive = true
                        },
                        new Coupon
                        {
                            Code = "SHIP0D",
                            Title = "Freeship Toàn Quốc 0 Đồng",
                            Description = "Miễn 100% phí giao hàng toàn quốc cho đơn từ 500.000₫",
                            Type = CouponType.ShippingDiscount,
                            DiscountValue = 30000,
                            IsPercentage = false,
                            MinOrderAmount = 500000,
                            ExpiryDate = DateTime.UtcNow.AddDays(90),
                            IsActive = true
                        }
                    };
                    context.Coupons.AddRange(coupons);
                    context.SaveChanges();
                }
            }
            catch { /* Bỏ qua nếu lỗi */ }

            // 11. Seed Lịch hẹn sửa chữa mẫu nếu chưa có
            try
            {
                if (!context.RepairBookings.Any())
                {
                    var customer = context.Users.FirstOrDefault(u => u.Role == "Customer");
                    var sampleBookings = new List<RepairBooking>
                    {
                        new RepairBooking
                        {
                            BookingCode = "DTV-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-8921",
                            UserId = customer?.Id,
                            CustomerName = customer?.FullName ?? "Nguyễn Văn Khách",
                            PhoneNumber = customer?.PhoneNumber ?? "0901234567",
                            Email = customer?.Email ?? "customer@gmail.com",
                            DeviceType = "Điện thoại",
                            DeviceName = "iPhone 15 Pro Max",
                            SerialNumber = "IMEI-358492019482103",
                            ServiceType = "Thay Pin dung lượng cao chính hãng",
                            IssueDescription = "Pin bị chai còn 78%, máy dùng nhanh nóng và tụt pin khi mở 4G.",
                            HasWarranty = true,
                            OrderCode = "CPS-849201",
                            City = "Hồ Chí Minh",
                            StoreBranch = "Điện Thoại Vui ASP - 136 Nguyễn Thái Học, P. Phạm Ngũ Lão, Quận 1, TP.HCM",
                            BookingDate = DateTime.UtcNow.Date.AddDays(1),
                            TimeSlot = "09:30 - 11:00",
                            EstimatedCost = 850000,
                            Status = RepairStatus.Received,
                            TechnicianNote = "Đã chuẩn bị sẵn linh kiện Pin chính hãng tại chi nhánh 136 Nguyễn Thái Học.",
                            CreatedAt = DateTime.UtcNow.AddHours(-3)
                        },
                        new RepairBooking
                        {
                            BookingCode = "DTV-" + DateTime.UtcNow.AddDays(-2).ToString("yyyyMMdd") + "-4102",
                            UserId = customer?.Id,
                            CustomerName = customer?.FullName ?? "Nguyễn Văn Khách",
                            PhoneNumber = customer?.PhoneNumber ?? "0901234567",
                            Email = customer?.Email ?? "customer@gmail.com",
                            DeviceType = "Laptop",
                            DeviceName = "MacBook Pro M2 14 inch",
                            SerialNumber = "SN-C02GF92P0GHK",
                            ServiceType = "Vệ sinh máy & Tra keo tản nhiệt Thermal Grizzly",
                            IssueDescription = "Máy quạt kêu to khi render video, cần vệ sinh bụi bên trong.",
                            HasWarranty = false,
                            City = "Hà Nội",
                            StoreBranch = "Điện Thoại Vui - 19 Thái Hà, P. Trung Liệt, Q. Đống Đa, Hà Nội",
                            BookingDate = DateTime.UtcNow.Date.AddDays(-1),
                            TimeSlot = "14:00 - 15:30",
                            EstimatedCost = 250000,
                            Status = RepairStatus.Completed,
                            TechnicianNote = "Đã vệ sinh sạch sẽ, nhiệt độ CPU giảm 12 độ C, máy chạy êm.",
                            CreatedAt = DateTime.UtcNow.AddDays(-2)
                        }
                    };
                    context.RepairBookings.AddRange(sampleBookings);
                    context.SaveChanges();
                }
            }
            catch { /* Bỏ qua nếu lỗi */ }

            // 12. Seed Phiếu bảo hành mẫu nếu chưa có
            try
            {
                if (!context.WarrantyCards.Any())
                {
                    var customer = context.Users.FirstOrDefault(u => u.Email == "customer@gmail.com");
                    var user2 = context.Users.FirstOrDefault(u => u.Email == "maianh.tran@gmail.com");
                    var user3 = context.Users.FirstOrDefault(u => u.Email == "hoangnam.le@gmail.com");

                    var sampleWarranties = new List<WarrantyCard>
                    {
                        new WarrantyCard
                        {
                            WarrantyCode = "BH-20241015-0012",
                            UserId = customer?.Id,
                            CustomerName = customer?.FullName ?? "Nguyễn Văn Khách",
                            PhoneNumber = customer?.PhoneNumber ?? "0901234567",
                            Email = customer?.Email ?? "customer@gmail.com",
                            Address = customer?.Address ?? "123 Nguyễn Trãi, Quận 1, TP.HCM",
                            ProductName = "iPhone 15 Pro Max 256GB Titan Tự Nhiên",
                            VariantInfo = "Titan Tự Nhiên / 256GB",
                            ProductImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/iphone-15-pro-max_3.png",
                            SerialNumber = "IMEI-358492019482103",
                            WarrantyPackage = "12 tháng Apple Care chính hãng CellphoneS",
                            WarrantyPeriodMonths = 12,
                            PurchaseDate = DateTime.UtcNow.AddMonths(-3),
                            StartDate = DateTime.UtcNow.AddMonths(-3),
                            EndDate = DateTime.UtcNow.AddMonths(9),
                            ServiceCenter = "Điện Thoại Vui AASP - 136 Nguyễn Thái Học, Quận 1, TP.HCM (Hotline: 1800.2064)",
                            WarrantyTerms = "Bảo hành 1 đổi 1 trong 30 ngày nếu lỗi nhà sản xuất. Sửa chữa miễn phí phần cứng chính hãng tại Điện Thoại Vui ASP.",
                            Status = WarrantyCardStatus.Active,
                            AdminNote = "Khách hàng mua kèm gói dán màn hình cường lực Kingbull.",
                            CreatedAt = DateTime.UtcNow.AddMonths(-3)
                        },
                        new WarrantyCard
                        {
                            WarrantyCode = "BH-20241105-0034",
                            UserId = customer?.Id,
                            CustomerName = customer?.FullName ?? "Nguyễn Văn Khách",
                            PhoneNumber = customer?.PhoneNumber ?? "0901234567",
                            Email = customer?.Email ?? "customer@gmail.com",
                            Address = customer?.Address ?? "123 Nguyễn Trãi, Quận 1, TP.HCM",
                            ProductName = "Samsung Galaxy S24 Ultra 512GB Xám Titan",
                            VariantInfo = "Xám Titan / 512GB",
                            ProductImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/s/a/samsung-s24-ultra_4.png",
                            SerialNumber = "IMEI-359102938172635",
                            WarrantyPackage = "24 tháng bảo hành VIP 1 đổi 1 CellphoneS Care",
                            WarrantyPeriodMonths = 24,
                            PurchaseDate = DateTime.UtcNow.AddMonths(-1),
                            StartDate = DateTime.UtcNow.AddMonths(-1),
                            EndDate = DateTime.UtcNow.AddMonths(23),
                            ServiceCenter = "Trung tâm CellphoneS Care & Samsung Plaza - 213 Trần Quang Khải, Q.1, TP.HCM",
                            WarrantyTerms = "Đặc quyền VIP: 1 đổi 1 trong 12 tháng đầu, bảo hành rơi vỡ ngấm nước hỗ trợ 90% chi phí sửa chữa.",
                            Status = WarrantyCardStatus.Active,
                            AdminNote = "Gói CellphoneS Care VIP kích hoạt thành công.",
                            CreatedAt = DateTime.UtcNow.AddMonths(-1)
                        },
                        new WarrantyCard
                        {
                            WarrantyCode = "BH-20231120-0089",
                            UserId = customer?.Id,
                            CustomerName = customer?.FullName ?? "Nguyễn Văn Khách",
                            PhoneNumber = customer?.PhoneNumber ?? "0901234567",
                            Email = customer?.Email ?? "customer@gmail.com",
                            Address = customer?.Address ?? "123 Nguyễn Trãi, Quận 1, TP.HCM",
                            ProductName = "MacBook Pro M2 14 inch 16GB/512GB Space Gray",
                            VariantInfo = "Space Gray / 16GB / 512GB",
                            ProductImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/m/a/macbook-pro-14-m2.png",
                            SerialNumber = "SN-C02GF92P0GHK",
                            WarrantyPackage = "12 tháng chính hãng Apple toàn cầu",
                            WarrantyPeriodMonths = 12,
                            PurchaseDate = DateTime.UtcNow.AddDays(-345),
                            StartDate = DateTime.UtcNow.AddDays(-345),
                            EndDate = DateTime.UtcNow.AddDays(20),
                            ServiceCenter = "Điện Thoại Vui ASP - 19 Thái Hà, Đống Đa, Hà Nội",
                            WarrantyTerms = "Bảo hành phần cứng theo tiêu chuẩn Apple Limited Warranty.",
                            Status = WarrantyCardStatus.ExpiringSoon,
                            AdminNote = "Sắp hết hạn bảo hành. Đã gửi tin nhắn nhắc gia hạn AppleCare+.",
                            CreatedAt = DateTime.UtcNow.AddDays(-345)
                        },
                        new WarrantyCard
                        {
                            WarrantyCode = "BH-20230510-0156",
                            UserId = user2?.Id,
                            CustomerName = user2?.FullName ?? "Trần Thị Mai Anh",
                            PhoneNumber = user2?.PhoneNumber ?? "0982345678",
                            Email = user2?.Email ?? "maianh.tran@gmail.com",
                            Address = "456 Lê Văn Sỹ, Quận 3, TP.HCM",
                            ProductName = "iPad Pro 11 inch M1 128GB Wifi Bạc",
                            VariantInfo = "Silver / 128GB",
                            ProductImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/ipad-pro-11-2021.png",
                            SerialNumber = "SN-DMPD9410Q16X",
                            WarrantyPackage = "12 tháng chính hãng Apple",
                            WarrantyPeriodMonths = 12,
                            PurchaseDate = DateTime.UtcNow.AddMonths(-16),
                            StartDate = DateTime.UtcNow.AddMonths(-16),
                            EndDate = DateTime.UtcNow.AddMonths(-4),
                            ServiceCenter = "Điện Thoại Vui ASP - 136 Nguyễn Thái Học, Quận 1, TP.HCM",
                            WarrantyTerms = "Bảo hành linh kiện chính hãng.",
                            Status = WarrantyCardStatus.Expired,
                            AdminNote = "Đã hết hạn bảo hành. Áp dụng bảng giá sửa chữa dịch vụ ưu đãi Smember.",
                            CreatedAt = DateTime.UtcNow.AddMonths(-16)
                        },
                        new WarrantyCard
                        {
                            WarrantyCode = "BH-20240901-0078",
                            UserId = user3?.Id,
                            CustomerName = user3?.FullName ?? "Lê Hoàng Nam",
                            PhoneNumber = user3?.PhoneNumber ?? "0918765432",
                            Email = user3?.Email ?? "hoangnam.le@gmail.com",
                            Address = "789 Cách Mạng Tháng 8, Tân Bình, TP.HCM",
                            ProductName = "Xiaomi 14 Ultra 512GB Đen",
                            VariantInfo = "Black / 16GB / 512GB",
                            ProductImageUrl = "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/x/i/xiaomi-14-ultra_1.png",
                            SerialNumber = "IMEI-867123049182746",
                            WarrantyPackage = "18 tháng bảo hành tiêu chuẩn Xiaomi Việt Nam",
                            WarrantyPeriodMonths = 18,
                            PurchaseDate = DateTime.UtcNow.AddMonths(-2),
                            StartDate = DateTime.UtcNow.AddMonths(-2),
                            EndDate = DateTime.UtcNow.AddMonths(16),
                            ServiceCenter = "Trung tâm bảo hành Xiaomi Digiworld - 65A Hồ Xuân Hương, Q.3, TP.HCM",
                            WarrantyTerms = "Bảo hành 18 tháng, bao gồm gói bảo hiểm rơi vỡ màn hình trong 6 tháng đầu.",
                            Status = WarrantyCardStatus.Active,
                            AdminNote = "Tặng kèm gói rơi vỡ màn hình 6 tháng.",
                            CreatedAt = DateTime.UtcNow.AddMonths(-2)
                        }
                    };

                    context.WarrantyCards.AddRange(sampleWarranties);
                    context.SaveChanges();
                }
            }
            catch { /* Bỏ qua nếu lỗi */ }
        }

        public static string GetCellphonesDefaultProductImage(string productName, string slug, string currentImg)
        {
            string p = $"{productName} {slug}".ToLowerInvariant();

            if (p.Contains("iphone 16 pro") || p.Contains("iphone-16-pro"))
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/iphone-16-pro-max.png";

            if (p.Contains("s24 ultra") || p.Contains("s24-ultra"))
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/s/s/ss-s24-ultra-xam-222.png";

            if (p.Contains("iphone 15") || p.Contains("iphone-15"))
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/iphone-15-plus_1__1.png";

            if (p.Contains("xiaomi 14") || p.Contains("xiaomi-14"))
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/x/i/xiaomi-14_2__2.png";

            if (p.Contains("find n3 flip") || p.Contains("find-n3-flip"))
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/f/i/find-n3-flip-vang_1.png";

            if (p.Contains("z flip6") || p.Contains("z flip 6") || p.Contains("z-flip6") || p.Contains("z-flip-6"))
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/f/r/frame_166_3.png";

            if (p.Contains("vivo v40 lite") || p.Contains("vivo-v40-lite"))
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/m/image_-_2025-02-10t145206.786.png";

            if (p.Contains("redmi note 13") || p.Contains("redmi-note-13"))
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/x/i/xiaomi-redmi-note-13_1__1_1.png";

            if (p.Contains("macbook air m2") || p.Contains("macbook-air-m2"))
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/m/a/macbook_1__1_8.png";

            if (p.Contains("tuf gaming") || p.Contains("tuf-gaming"))
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/g/r/group_509_4__1.png";

            if (p.Contains("airpods pro 2") || p.Contains("airpods-pro-2"))
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/a/p/apple-airpods-pro-2-usb-c_1_.png";

            if (p.Contains("ipad air 6") || p.Contains("ipad-air-6"))
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/ipad-air-6-m2-11-inch_9_.jpg";

            if (p.Contains("watch series 10") || p.Contains("watch-series-10"))
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/g/r/group_604_1.png";

            if (p.Contains("watch ultra") || p.Contains("watch-ultra"))
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/g/r/group_565_4__1.png";

            if (p.Contains("marshall emberton") || p.Contains("marshall-emberton"))
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/l/o/loa-bluetooth-marshall-emberton-ii-ksp-2.png";

            if (p.Contains("wh-1000xm5") || p.Contains("1000xm5"))
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/f/r/frame_428_2.png";

            if (p.Contains("anker") && p.Contains("30w"))
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/c/u/cu-sac-anker-zolo-a2698-1c-30w_3.png";

            if (p.Contains("baseus") && p.Contains("100w"))
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/p/i/pin-du-phong-baseus-enerfill-20000mah-100w-tich-hop-cap-doi-usb-c-man-hinh-fc41_1_.png";

            if (p.Contains("vacuum x20") || p.Contains("vacuum-x20"))
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/r/o/robot_hut_bui_kh_ng_dock.png";

            return currentImg;
        }

        public static List<ProductVariant> GetStandardProductVariants(Product prod)
        {
            string p = $"{prod.Name} {prod.Slug}".ToLowerInvariant();
            var list = new List<ProductVariant>();

            if (p.Contains("iphone 16 pro") || p.Contains("iphone-16-pro"))
            {
                list.AddRange(new[]
                {
                    new ProductVariant { ColorName = "Titan Sa Mạc", ColorHex = "#C8B19E", Storage = "256GB", Price = 30990000, StockQuantity = 30 },
                    new ProductVariant { ColorName = "Titan Tự Nhiên", ColorHex = "#9F9E99", Storage = "256GB", Price = 30990000, StockQuantity = 25 },
                    new ProductVariant { ColorName = "Titan Trắng", ColorHex = "#F2F3F4", Storage = "256GB", Price = 30990000, StockQuantity = 20 },
                    new ProductVariant { ColorName = "Titan Đen", ColorHex = "#3C3B3D", Storage = "256GB", Price = 30990000, StockQuantity = 25 },
                    new ProductVariant { ColorName = "Titan Sa Mạc", ColorHex = "#C8B19E", Storage = "512GB", Price = 36990000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Titan Tự Nhiên", ColorHex = "#9F9E99", Storage = "512GB", Price = 36990000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Titan Trắng", ColorHex = "#F2F3F4", Storage = "512GB", Price = 36990000, StockQuantity = 12 },
                    new ProductVariant { ColorName = "Titan Đen", ColorHex = "#3C3B3D", Storage = "512GB", Price = 36990000, StockQuantity = 12 },
                    new ProductVariant { ColorName = "Titan Sa Mạc", ColorHex = "#C8B19E", Storage = "1TB", Price = 43990000, StockQuantity = 10 },
                    new ProductVariant { ColorName = "Titan Tự Nhiên", ColorHex = "#9F9E99", Storage = "1TB", Price = 43990000, StockQuantity = 8 },
                    new ProductVariant { ColorName = "Titan Trắng", ColorHex = "#F2F3F4", Storage = "1TB", Price = 43990000, StockQuantity = 8 },
                    new ProductVariant { ColorName = "Titan Đen", ColorHex = "#3C3B3D", Storage = "1TB", Price = 43990000, StockQuantity = 8 }
                });
            }
            else if (p.Contains("s24 ultra") || p.Contains("s24-ultra"))
            {
                list.AddRange(new[]
                {
                    new ProductVariant { ColorName = "Xám Titan", ColorHex = "#797672", Storage = "12GB - 256GB", Price = 25490000, StockQuantity = 25 },
                    new ProductVariant { ColorName = "Đen Titan", ColorHex = "#2B2A29", Storage = "12GB - 256GB", Price = 25490000, StockQuantity = 20 },
                    new ProductVariant { ColorName = "Tím Titan", ColorHex = "#605668", Storage = "12GB - 256GB", Price = 25490000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Vàng Titan", ColorHex = "#E1D6B8", Storage = "12GB - 256GB", Price = 25490000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Xám Titan", ColorHex = "#797672", Storage = "12GB - 512GB", Price = 28990000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Đen Titan", ColorHex = "#2B2A29", Storage = "12GB - 512GB", Price = 28990000, StockQuantity = 12 },
                    new ProductVariant { ColorName = "Tím Titan", ColorHex = "#605668", Storage = "12GB - 512GB", Price = 28990000, StockQuantity = 10 },
                    new ProductVariant { ColorName = "Vàng Titan", ColorHex = "#E1D6B8", Storage = "12GB - 512GB", Price = 28990000, StockQuantity = 10 }
                });
            }
            else if (p.Contains("iphone 15") || p.Contains("iphone-15"))
            {
                list.AddRange(new[]
                {
                    new ProductVariant { ColorName = "Hồng Pastel", ColorHex = "#F4D7DB", Storage = "128GB", Price = 18490000, StockQuantity = 20 },
                    new ProductVariant { ColorName = "Xanh Mint", ColorHex = "#D7E6DF", Storage = "128GB", Price = 18490000, StockQuantity = 20 },
                    new ProductVariant { ColorName = "Đen Midnight", ColorHex = "#323539", Storage = "128GB", Price = 18490000, StockQuantity = 25 },
                    new ProductVariant { ColorName = "Xanh Dương", ColorHex = "#D5E3EB", Storage = "128GB", Price = 18490000, StockQuantity = 18 },
                    new ProductVariant { ColorName = "Vàng Chanh", ColorHex = "#F5EFA6", Storage = "128GB", Price = 18490000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Hồng Pastel", ColorHex = "#F4D7DB", Storage = "256GB", Price = 21490000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Xanh Mint", ColorHex = "#D7E6DF", Storage = "256GB", Price = 21490000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Đen Midnight", ColorHex = "#323539", Storage = "256GB", Price = 21490000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Xanh Dương", ColorHex = "#D5E3EB", Storage = "256GB", Price = 21490000, StockQuantity = 12 },
                    new ProductVariant { ColorName = "Vàng Chanh", ColorHex = "#F5EFA6", Storage = "256GB", Price = 21490000, StockQuantity = 10 }
                });
            }
            else if (p.Contains("xiaomi 14") || p.Contains("xiaomi-14"))
            {
                list.AddRange(new[]
                {
                    new ProductVariant { ColorName = "Đen Nhám", ColorHex = "#232323", Storage = "12GB - 256GB", Price = 19490000, StockQuantity = 20 },
                    new ProductVariant { ColorName = "Trắng Tuyết", ColorHex = "#F5F5F5", Storage = "12GB - 256GB", Price = 19490000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Xanh Ngọc", ColorHex = "#52756A", Storage = "12GB - 256GB", Price = 19490000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Đen Nhám", ColorHex = "#232323", Storage = "16GB - 512GB", Price = 21990000, StockQuantity = 12 },
                    new ProductVariant { ColorName = "Trắng Tuyết", ColorHex = "#F5F5F5", Storage = "16GB - 512GB", Price = 21990000, StockQuantity = 10 },
                    new ProductVariant { ColorName = "Xanh Ngọc", ColorHex = "#52756A", Storage = "16GB - 512GB", Price = 21990000, StockQuantity = 10 }
                });
            }
            else if (p.Contains("find n3 flip") || p.Contains("find-n3-flip"))
            {
                list.AddRange(new[]
                {
                    new ProductVariant { ColorName = "Vàng Thạch Anh", ColorHex = "#F0E5D0", Storage = "12GB - 256GB", Price = 16990000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Hồng Ánh Trăng", ColorHex = "#F5DDE6", Storage = "12GB - 256GB", Price = 16990000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Đen Hổ Phách", ColorHex = "#1E1E1E", Storage = "12GB - 256GB", Price = 16990000, StockQuantity = 10 }
                });
            }
            else if (p.Contains("z flip6") || p.Contains("z flip 6") || p.Contains("z-flip6") || p.Contains("z-flip-6"))
            {
                list.AddRange(new[]
                {
                    new ProductVariant { ColorName = "Xanh Maya", ColorHex = "#6FA8DC", Storage = "12GB - 256GB", Price = 23990000, StockQuantity = 20 },
                    new ProductVariant { ColorName = "Xám Metal", ColorHex = "#7F7F7F", Storage = "12GB - 256GB", Price = 23990000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Vàng Solar", ColorHex = "#FFE599", Storage = "12GB - 256GB", Price = 23990000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Xanh Mint", ColorHex = "#A8D5BA", Storage = "12GB - 256GB", Price = 23990000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Xanh Maya", ColorHex = "#6FA8DC", Storage = "12GB - 512GB", Price = 26990000, StockQuantity = 12 },
                    new ProductVariant { ColorName = "Xám Metal", ColorHex = "#7F7F7F", Storage = "12GB - 512GB", Price = 26990000, StockQuantity = 10 },
                    new ProductVariant { ColorName = "Vàng Solar", ColorHex = "#FFE599", Storage = "12GB - 512GB", Price = 26990000, StockQuantity = 10 },
                    new ProductVariant { ColorName = "Xanh Mint", ColorHex = "#A8D5BA", Storage = "12GB - 512GB", Price = 26990000, StockQuantity = 10 }
                });
            }
            else if (p.Contains("vivo v40 lite") || p.Contains("vivo-v40-lite"))
            {
                list.AddRange(new[]
                {
                    new ProductVariant { ColorName = "Bạc Kim Cương", ColorHex = "#E8ECEF", Storage = "8GB - 256GB", Price = 7990000, StockQuantity = 25 },
                    new ProductVariant { ColorName = "Đen Titan", ColorHex = "#2B2B2B", Storage = "8GB - 256GB", Price = 7990000, StockQuantity = 25 },
                    new ProductVariant { ColorName = "Tím Mộng Mơ", ColorHex = "#B7A4D7", Storage = "8GB - 256GB", Price = 7990000, StockQuantity = 20 },
                    new ProductVariant { ColorName = "Bạc Kim Cương", ColorHex = "#E8ECEF", Storage = "12GB - 512GB", Price = 8990000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Đen Titan", ColorHex = "#2B2B2B", Storage = "12GB - 512GB", Price = 8990000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Tím Mộng Mơ", ColorHex = "#B7A4D7", Storage = "12GB - 512GB", Price = 8990000, StockQuantity = 12 }
                });
            }
            else if (p.Contains("redmi note 13") || p.Contains("redmi-note-13"))
            {
                list.AddRange(new[]
                {
                    new ProductVariant { ColorName = "Đen Bán Dạ", ColorHex = "#1F1F1F", Storage = "6GB - 128GB", Price = 4190000, StockQuantity = 30 },
                    new ProductVariant { ColorName = "Xanh Băng Tuyết", ColorHex = "#A2C2E2", Storage = "6GB - 128GB", Price = 4190000, StockQuantity = 25 },
                    new ProductVariant { ColorName = "Vàng Hoàng Hôn", ColorHex = "#F0D8A8", Storage = "6GB - 128GB", Price = 4190000, StockQuantity = 20 },
                    new ProductVariant { ColorName = "Đen Bán Dạ", ColorHex = "#1F1F1F", Storage = "8GB - 256GB", Price = 4890000, StockQuantity = 20 },
                    new ProductVariant { ColorName = "Xanh Băng Tuyết", ColorHex = "#A2C2E2", Storage = "8GB - 256GB", Price = 4890000, StockQuantity = 20 },
                    new ProductVariant { ColorName = "Vàng Hoàng Hôn", ColorHex = "#F0D8A8", Storage = "8GB - 256GB", Price = 4890000, StockQuantity = 15 }
                });
            }
            else if (p.Contains("macbook air m2") || p.Contains("macbook-air-m2"))
            {
                list.AddRange(new[]
                {
                    new ProductVariant { ColorName = "Đen Midnight", ColorHex = "#1E2530", Storage = "8GB - 256GB", Price = 23690000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Bạc Silver", ColorHex = "#E2E4E5", Storage = "8GB - 256GB", Price = 23690000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Vàng Starlight", ColorHex = "#F0E5D3", Storage = "8GB - 256GB", Price = 23690000, StockQuantity = 12 },
                    new ProductVariant { ColorName = "Xám Space Gray", ColorHex = "#7D7E80", Storage = "8GB - 256GB", Price = 23690000, StockQuantity = 12 },
                    new ProductVariant { ColorName = "Đen Midnight", ColorHex = "#1E2530", Storage = "16GB - 256GB", Price = 25990000, StockQuantity = 12 },
                    new ProductVariant { ColorName = "Bạc Silver", ColorHex = "#E2E4E5", Storage = "16GB - 256GB", Price = 25990000, StockQuantity = 12 },
                    new ProductVariant { ColorName = "Vàng Starlight", ColorHex = "#F0E5D3", Storage = "16GB - 256GB", Price = 25990000, StockQuantity = 10 },
                    new ProductVariant { ColorName = "Xám Space Gray", ColorHex = "#7D7E80", Storage = "16GB - 256GB", Price = 25990000, StockQuantity = 10 },
                    new ProductVariant { ColorName = "Đen Midnight", ColorHex = "#1E2530", Storage = "16GB - 512GB", Price = 28690000, StockQuantity = 10 },
                    new ProductVariant { ColorName = "Xám Space Gray", ColorHex = "#7D7E80", Storage = "16GB - 512GB", Price = 28690000, StockQuantity = 10 }
                });
            }
            else if (p.Contains("tuf gaming") || p.Contains("tuf-gaming"))
            {
                list.AddRange(new[]
                {
                    new ProductVariant { ColorName = "Xám Jaeger Gray", ColorHex = "#4A4D52", Storage = "16GB - 512GB", Price = 18490000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Đen Mecha Gray", ColorHex = "#2E3033", Storage = "16GB - 512GB", Price = 18490000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Xám Jaeger Gray", ColorHex = "#4A4D52", Storage = "32GB - 1TB", Price = 21990000, StockQuantity = 10 },
                    new ProductVariant { ColorName = "Đen Mecha Gray", ColorHex = "#2E3033", Storage = "32GB - 1TB", Price = 21990000, StockQuantity = 10 }
                });
            }
            else if (p.Contains("airpods pro 2") || p.Contains("airpods-pro-2"))
            {
                list.AddRange(new[]
                {
                    new ProductVariant { ColorName = "Trắng", ColorHex = "#F8FAFC", Storage = "USB-C MagSafe", Price = 5190000, StockQuantity = 50 }
                });
            }
            else if (p.Contains("ipad air 6") || p.Contains("ipad-air-6"))
            {
                list.AddRange(new[]
                {
                    new ProductVariant { ColorName = "Xám Không Gian", ColorHex = "#68696E", Storage = "128GB Wi-Fi", Price = 15390000, StockQuantity = 20 },
                    new ProductVariant { ColorName = "Ánh Sao Starlight", ColorHex = "#E5DFD9", Storage = "128GB Wi-Fi", Price = 15390000, StockQuantity = 20 },
                    new ProductVariant { ColorName = "Xanh Dương Blue", ColorHex = "#C1D5E0", Storage = "128GB Wi-Fi", Price = 15390000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Tím Purple", ColorHex = "#D3C9E2", Storage = "128GB Wi-Fi", Price = 15390000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Xám Không Gian", ColorHex = "#68696E", Storage = "256GB Wi-Fi", Price = 17890000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Ánh Sao Starlight", ColorHex = "#E5DFD9", Storage = "256GB Wi-Fi", Price = 17890000, StockQuantity = 12 },
                    new ProductVariant { ColorName = "Xanh Dương Blue", ColorHex = "#C1D5E0", Storage = "256GB Wi-Fi", Price = 17890000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Tím Purple", ColorHex = "#D3C9E2", Storage = "256GB Wi-Fi", Price = 17890000, StockQuantity = 12 }
                });
            }
            else if (p.Contains("watch series 10") || p.Contains("watch-series-10"))
            {
                list.AddRange(new[]
                {
                    new ProductVariant { ColorName = "Đen Bóng Jet Black", ColorHex = "#1F2024", Storage = "42mm GPS", Price = 10290000, StockQuantity = 20 },
                    new ProductVariant { ColorName = "Vàng Hồng Rose Gold", ColorHex = "#E6C7C2", Storage = "42mm GPS", Price = 10290000, StockQuantity = 20 },
                    new ProductVariant { ColorName = "Bạc Silver", ColorHex = "#D9DADB", Storage = "42mm GPS", Price = 10290000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Đen Bóng Jet Black", ColorHex = "#1F2024", Storage = "46mm GPS", Price = 11090000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Vàng Hồng Rose Gold", ColorHex = "#E6C7C2", Storage = "46mm GPS", Price = 11090000, StockQuantity = 12 },
                    new ProductVariant { ColorName = "Bạc Silver", ColorHex = "#D9DADB", Storage = "46mm GPS", Price = 11090000, StockQuantity = 12 }
                });
            }
            else if (p.Contains("watch ultra") || p.Contains("watch-ultra"))
            {
                list.AddRange(new[]
                {
                    new ProductVariant { ColorName = "Xám Titan", ColorHex = "#5A5B5E", Storage = "47mm LTE", Price = 14490000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Bạc Titan", ColorHex = "#C8CACD", Storage = "47mm LTE", Price = 14490000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Trắng Titan", ColorHex = "#F1F2F4", Storage = "47mm LTE", Price = 14490000, StockQuantity = 12 }
                });
            }
            else if (p.Contains("marshall emberton") || p.Contains("marshall-emberton"))
            {
                list.AddRange(new[]
                {
                    new ProductVariant { ColorName = "Đen & Đồng", ColorHex = "#1E1E1E", Storage = "Tiêu chuẩn", Price = 3890000, StockQuantity = 20 },
                    new ProductVariant { ColorName = "Kem Cream", ColorHex = "#EDE6D6", Storage = "Tiêu chuẩn", Price = 3890000, StockQuantity = 15 }
                });
            }
            else if (p.Contains("wh-1000xm5") || p.Contains("1000xm5"))
            {
                list.AddRange(new[]
                {
                    new ProductVariant { ColorName = "Đen", ColorHex = "#222222", Storage = "Tiêu chuẩn", Price = 6990000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Bạc Platinum", ColorHex = "#DCD8D0", Storage = "Tiêu chuẩn", Price = 6990000, StockQuantity = 15 },
                    new ProductVariant { ColorName = "Xanh Ánh Trăng", ColorHex = "#344154", Storage = "Tiêu chuẩn", Price = 6990000, StockQuantity = 10 },
                    new ProductVariant { ColorName = "Hồng Khói", ColorHex = "#D8B7B8", Storage = "Tiêu chuẩn", Price = 6990000, StockQuantity = 10 }
                });
            }
            else if (p.Contains("anker") && p.Contains("30w"))
            {
                list.AddRange(new[]
                {
                    new ProductVariant { ColorName = "Đen", ColorHex = "#222222", Storage = "30W USB-C GaN", Price = 320000, StockQuantity = 60 },
                    new ProductVariant { ColorName = "Trắng", ColorHex = "#F5F5F7", Storage = "30W USB-C GaN", Price = 320000, StockQuantity = 60 },
                    new ProductVariant { ColorName = "Xanh Dương", ColorHex = "#A6C8E0", Storage = "30W USB-C GaN", Price = 320000, StockQuantity = 40 },
                    new ProductVariant { ColorName = "Hồng", ColorHex = "#F2C6D0", Storage = "30W USB-C GaN", Price = 320000, StockQuantity = 40 }
                });
            }
            else if (p.Contains("baseus") && p.Contains("100w"))
            {
                list.AddRange(new[]
                {
                    new ProductVariant { ColorName = "Đen", ColorHex = "#1F1F1F", Storage = "20000mAh - 100W", Price = 1090000, StockQuantity = 30 },
                    new ProductVariant { ColorName = "Trắng", ColorHex = "#F2F4F8", Storage = "20000mAh - 100W", Price = 1090000, StockQuantity = 25 }
                });
            }
            else if (p.Contains("vacuum x20") || p.Contains("vacuum-x20"))
            {
                list.AddRange(new[]
                {
                    new ProductVariant { ColorName = "Trắng", ColorHex = "#F8FAFC", Storage = "Bản Quốc Tế", Price = 9990000, StockQuantity = 20 }
                });
            }

            return list;
        }

        public static string GetCellphonesVariantImageUrl(string productName, string colorName, string fallbackImg)
        {
            string p = (productName ?? string.Empty).ToLowerInvariant();
            string c = (colorName ?? string.Empty).ToLowerInvariant();

            // 1. iPhone 16 Pro Max
            if (p.Contains("iphone 16 pro") || p.Contains("16 pro max"))
            {
                if (c.Contains("tự nhiên") || c.Contains("natural") || c.Contains("tu nhien"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/iphone-16-pro-max-titan-tu-nhien.png";
                if (c.Contains("trắng") || c.Contains("white") || c.Contains("trang"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/iphone-16-pro-max-titan-trang.png";
                if (c.Contains("đen") || c.Contains("black") || c.Contains("den"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/iphone-16-pro-max-titan-den.png";
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/iphone-16-pro-max-titan-sa-mac.png";
            }

            // 2. Samsung Galaxy S24 Ultra
            if (p.Contains("s24 ultra"))
            {
                if (c.Contains("đen") || c.Contains("black") || c.Contains("den"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/g/a/galaxy-s24-ultra-den-1_1_3.png";
                if (c.Contains("tím") || c.Contains("violet") || c.Contains("tim"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/g/a/galaxy-s24-ultra-tim_1_3.png";
                if (c.Contains("vàng") || c.Contains("yellow") || c.Contains("vang"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/g/a/galaxy-s24-ultra-vang_1_3.png";
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/g/a/galaxy-s24-ultra-xam_1_3.png";
            }

            // 3. iPhone 15
            if (p.Contains("iphone 15"))
            {
                if (c.Contains("mint") || c.Contains("xanh lá") || c.Contains("xanh la") || c.Contains("green"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/iphone-15-128gb-xanh-la.png";
                if (c.Contains("đen") || c.Contains("midnight") || c.Contains("den"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/iphone-15-128-gbden.png";
                if (c.Contains("dương") || c.Contains("duong") || c.Contains("blue") || c.Contains("lam"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/iphone-15-128gb-xanh-duong.png";
                if (c.Contains("vàng") || c.Contains("vang") || c.Contains("yellow"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/iphone-15-128gb-vang.png";
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/iphone-15-hong.png";
            }

            // 4. Xiaomi 14
            if (p.Contains("xiaomi 14"))
            {
                if (c.Contains("trắng") || c.Contains("trang") || c.Contains("white"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/x/i/xiaomi-14-pre-trang_1.png";
                if (c.Contains("xanh") || c.Contains("green") || c.Contains("ngọc"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/x/i/xiaomi-14-pre-xanh-la_1.png";
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/x/i/xiaomi-14-pre-den_1.png";
            }

            // 5. OPPO Find N3 Flip
            if (p.Contains("find n3 flip"))
            {
                if (c.Contains("hồng") || c.Contains("hong") || c.Contains("pink"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/o/p/oppo-find-n3-flip_2_.png";
                if (c.Contains("đen") || c.Contains("den") || c.Contains("black"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/n/3/n3_flip_-_combo_product_-_black.png";
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/n/3/n3_flip_-_combo_product_-_gold.png";
            }

            // 6. Samsung Galaxy Z Flip6
            if (p.Contains("z flip6") || p.Contains("z flip 6"))
            {
                if (c.Contains("xám") || c.Contains("xam") || c.Contains("gray") || c.Contains("metal") || c.Contains("đen"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/f/l/flip-den.jpg";
                if (c.Contains("vàng") || c.Contains("vang") || c.Contains("solar") || c.Contains("yellow"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/f/l/flip-vang.jpg";
                if (c.Contains("mint") || c.Contains("lá") || c.Contains("la") || c.Contains("green"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/f/l/flip-xanh-la.jpg";
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/f/l/flip-xanh.jpg";
            }

            // 7. vivo V40 Lite
            if (p.Contains("vivo v40 lite"))
            {
                if (c.Contains("đen") || c.Contains("den") || c.Contains("black"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/m/image_-_2025-02-10t145332.961.png";
                if (c.Contains("tím") || c.Contains("tim") || c.Contains("purple"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/d/i/dien-thoai-vivo-v40-lite_1_.png";
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/m/image_-_2025-02-10t145206.786.png";
            }

            // 8. Xiaomi Redmi Note 13
            if (p.Contains("redmi note 13"))
            {
                if (c.Contains("xanh") || c.Contains("blue") || c.Contains("green") || c.Contains("băng"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/d/o/downloadnote_13.png";
                if (c.Contains("vàng") || c.Contains("vang") || c.Contains("gold"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/d/o/download_3__11.png";
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/d/o/download_4__12_2.png";
            }

            // 9. MacBook Air M2
            if (p.Contains("macbook air m2"))
            {
                if (c.Contains("bạc") || c.Contains("bac") || c.Contains("silver"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/m/a/macbook_air_m2_2_3.jpg";
                if (c.Contains("vàng") || c.Contains("vang") || c.Contains("starlight"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/m/a/macbook_air_m2_3_2.jpg";
                if (c.Contains("xám") || c.Contains("xam") || c.Contains("gray") || c.Contains("space"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/m/a/macbook_air_m2_4_2.jpg";
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/m/a/macbook_air_m2_1_2.jpg";
            }

            // 10. ASUS TUF Gaming F15
            if (p.Contains("tuf gaming"))
            {
                if (c.Contains("đen") || c.Contains("den") || c.Contains("mecha"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/l/a/laptop_asus_tuf_gaming_f15_fx507zc4-hn074w.png";
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/t/e/text_ng_n_2__3_25.png";
            }

            // 11. AirPods Pro 2
            if (p.Contains("airpods pro 2"))
            {
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/a/p/apple-airpods-pro-2-usb-c_8__1.png";
            }

            // 12. iPad Air 6 M2
            if (p.Contains("ipad air 6"))
            {
                if (c.Contains("starlight") || c.Contains("ánh sao") || c.Contains("anh sao") || c.Contains("vàng"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/ipad-air-6-m2-11-inch_3_.png";
                if (c.Contains("xanh") || c.Contains("blue") || c.Contains("dương"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/ipad-air-6-m2-11-inch.png";
                if (c.Contains("tím") || c.Contains("tim") || c.Contains("purple"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/p/ipad-air-6-m2-11-inch_2_.png";
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/f/r/frame_100_5__3.png";
            }

            // 13. Apple Watch Series 10
            if (p.Contains("watch series 10"))
            {
                if (c.Contains("hồng") || c.Contains("hong") || c.Contains("rose") || c.Contains("gold"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/t/e/text_ng_n_34__7_2.png";
                if (c.Contains("bạc") || c.Contains("bac") || c.Contains("silver"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/t/e/text_ng_n_26__6_4.png";
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/t/e/text_ng_n_31__7_4.png";
            }

            // 14. Galaxy Watch Ultra
            if (p.Contains("watch ultra"))
            {
                if (c.Contains("bạc") || c.Contains("bac") || c.Contains("silver"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/s/m/sm-l705_001_front_titanium_silve_2.png";
                if (c.Contains("trắng") || c.Contains("trang") || c.Contains("white"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/s/m/sm-l705_001_front_titanium_white_2.png";
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/i/m/image_1075_1_1_1_1.png";
            }

            // 15. Marshall Emberton II
            if (p.Contains("marshall emberton"))
            {
                if (c.Contains("kem") || c.Contains("cream") || c.Contains("trắng") || c.Contains("xám"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/l/o/loa-bluetooth-marshall-emberton-ii-xam.png";
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/l/o/loa-marshall-emberton-ii_1.jpg";
            }

            // 16. Sony WH-1000XM5
            if (p.Contains("wh-1000xm5") || p.Contains("1000xm5"))
            {
                if (c.Contains("bạc") || c.Contains("bac") || c.Contains("platinum") || c.Contains("silver"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/t/a/tai-nghe-chup-tai-sony-wh-1000xm5-2-removebg-preview_1.png";
                if (c.Contains("xanh") || c.Contains("blue") || c.Contains("trăng"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/t/_/t_i_xu_ng_67__2_7.png";
                if (c.Contains("hồng") || c.Contains("hong") || c.Contains("pink"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/t/a/tai-nghe-chup-tai-sony-wh-1000xm5-hong-khoi.jpg";
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/t/a/tai-nghe-chup-tai-sony-wh-1000xm5-3_1.png";
            }

            // 17. Anker 30W
            if (p.Contains("anker") && p.Contains("30w"))
            {
                if (c.Contains("trắng") || c.Contains("trang") || c.Contains("white"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/f/r/frame_1_79__2.png";
                if (c.Contains("xanh") || c.Contains("blue") || c.Contains("dương"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/m/_/m_y_r_a_ch_n_29__1.png";
                if (c.Contains("hồng") || c.Contains("hong") || c.Contains("pink"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/m/_/m_y_r_a_ch_n_28__1.png";
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/f/r/frame_1_78__2.png";
            }

            // 18. Baseus 100W
            if (p.Contains("baseus") && p.Contains("100w"))
            {
                if (c.Contains("trắng") || c.Contains("trang") || c.Contains("white"))
                    return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/p/i/pin-du-phong-baseus-enerfill-20000mah-100w-tich-hop-cap-doi-usb-c-man-hinh-fc41_3_.png";
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/p/i/pin-du-phong-baseus-enerfill-20000mah-100w-tich-hop-cap-doi-usb-c-man-hinh-fc41_2_.png";
            }

            // 19. Xiaomi Robot Vacuum X20+
            if (p.Contains("vacuum x20") || p.Contains("vacuum-x20"))
            {
                return "https://cdn2.cellphones.com.vn/insecure/rs:fill:358:358/q:90/plain/https://cellphones.com.vn/media/catalog/product/r/o/robot-hut-bui-xiaomi-vacuum-x20-2_1.png";
            }

            return fallbackImg;
        }
    }
}
