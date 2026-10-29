using Microsoft.EntityFrameworkCore;
using WebsiteBanDienThoai.Models;

namespace WebsiteBanDienThoai.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Brand> Brands { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<ProductVariant> ProductVariants { get; set; } = null!;
        public DbSet<ProductImage> ProductImages { get; set; } = null!;
        public DbSet<Banner> Banners { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderDetail> OrderDetails { get; set; } = null!;
        public DbSet<AppUser> Users { get; set; } = null!;
        public DbSet<ProductReview> ProductReviews { get; set; } = null!;
        public DbSet<FavoriteProduct> FavoriteProducts { get; set; } = null!;
        public DbSet<TradeInDevice> TradeInDevices { get; set; } = null!;
        public DbSet<TradeInRequest> TradeInRequests { get; set; } = null!;
        public DbSet<NewsArticle> NewsArticles { get; set; } = null!;
        public DbSet<WarehouseReceipt> WarehouseReceipts { get; set; } = null!;
        public DbSet<WarehouseReceiptDetail> WarehouseReceiptDetails { get; set; } = null!;
        public DbSet<InventoryLog> InventoryLogs { get; set; } = null!;
        public DbSet<Coupon> Coupons { get; set; } = null!;
        public DbSet<RepairBooking> RepairBookings { get; set; } = null!;
        public DbSet<WarrantyCard> WarrantyCards { get; set; } = null!;
        public DbSet<AppNotification> Notifications { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình quan hệ Category
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Parent)
                .WithMany()
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình quan hệ Product -> Category
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình quan hệ Product -> Brand
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Brand)
                .WithMany(b => b.Products)
                .HasForeignKey(p => p.BrandId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình quan hệ ProductVariant -> Product
            modelBuilder.Entity<ProductVariant>()
                .HasOne(pv => pv.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(pv => pv.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình quan hệ ProductImage -> Product
            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình quan hệ ProductReview -> Product
            modelBuilder.Entity<ProductReview>()
                .HasOne(pr => pr.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(pr => pr.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình quan hệ FavoriteProduct -> Product
            modelBuilder.Entity<FavoriteProduct>()
                .HasOne(fp => fp.Product)
                .WithMany()
                .HasForeignKey(fp => fp.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình quan hệ FavoriteProduct -> User
            modelBuilder.Entity<FavoriteProduct>()
                .HasOne(fp => fp.User)
                .WithMany()
                .HasForeignKey(fp => fp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình quan hệ OrderDetail -> Order
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình quan hệ OrderDetail -> Product
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Product)
                .WithMany()
                .HasForeignKey(od => od.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình quan hệ TradeInRequest
            modelBuilder.Entity<TradeInRequest>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TradeInRequest>()
                .HasOne(t => t.TargetProduct)
                .WithMany()
                .HasForeignKey(t => t.TargetProductId)
                .OnDelete(DeleteBehavior.SetNull);

            // Cấu hình AppUser
            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.PhoneNumber)
                .IsUnique();

            // Cấu hình NewsArticle
            modelBuilder.Entity<NewsArticle>()
                .HasIndex(n => n.Slug);

            modelBuilder.Entity<NewsArticle>()
                .HasIndex(n => n.Category);

            // Cấu hình WarehouseReceipt & WarehouseReceiptDetail
            modelBuilder.Entity<WarehouseReceipt>()
                .HasIndex(w => w.ReceiptCode)
                .IsUnique();

            modelBuilder.Entity<WarehouseReceiptDetail>()
                .HasOne(d => d.WarehouseReceipt)
                .WithMany(r => r.Details)
                .HasForeignKey(d => d.WarehouseReceiptId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WarehouseReceiptDetail>()
                .HasOne(d => d.Product)
                .WithMany()
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<WarehouseReceiptDetail>()
                .HasOne(d => d.ProductVariant)
                .WithMany()
                .HasForeignKey(d => d.ProductVariantId)
                .OnDelete(DeleteBehavior.NoAction);

            // Cấu hình InventoryLog
            modelBuilder.Entity<InventoryLog>()
                .HasOne(l => l.Product)
                .WithMany()
                .HasForeignKey(l => l.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<InventoryLog>()
                .HasOne(l => l.ProductVariant)
                .WithMany()
                .HasForeignKey(l => l.ProductVariantId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
