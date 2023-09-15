using Microsoft.EntityFrameworkCore;
using PostAPI.Models;

namespace PostAPI
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            // Empty
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupsRelations> GroupsRelations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ReaderTier> ReaderTiers { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasKey(u => u.User_Id);

            modelBuilder.Entity<User>()
                .HasOne<ReaderTier>()
                .WithMany()
                .HasForeignKey(u => u.Tier_Id);

            modelBuilder.Entity<ReaderTier>()
                .HasKey(t => t.Tier_Id);
        }
    }
}
