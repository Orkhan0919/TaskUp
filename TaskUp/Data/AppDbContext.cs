using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskUp.Models;

namespace TaskUp.Data 
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Board> Boards { get; set; }
        public DbSet<BoardMember> BoardMembers { get; set; }
        public DbSet<BoardTask> BoardTasks { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); 
            builder.Entity<Board>()
                .HasOne(b => b.Owner)
                .WithMany(u => u.OwnedBoards)
                .HasForeignKey(b => b.OwnerId)
                .OnDelete(DeleteBehavior.Restrict); 
            builder.Entity<BoardMember>()
                .HasOne(bm => bm.Board)
                .WithMany(b => b.Members)
                .HasForeignKey(bm => bm.BoardId)
                .OnDelete(DeleteBehavior.Cascade); 

            builder.Entity<BoardMember>()
                .HasOne(bm => bm.User)
                .WithMany(u => u.JoinedBoards)
                .HasForeignKey(bm => bm.UserId)
                .OnDelete(DeleteBehavior.Restrict); 

            builder.Entity<BoardTask>()
                .HasOne(t => t.Board)
                .WithMany(b => b.Tasks)
                .HasForeignKey(t => t.BoardId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}