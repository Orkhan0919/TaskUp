using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskUp.Models;
using TaskUp.Utilities.Enums;

namespace TaskUp.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Tablolar (DbSets)
        public DbSet<Board> Boards { get; set; }
        public DbSet<BoardColumn> BoardColumns { get; set; }
        public DbSet<BoardTask> BoardTasks { get; set; }
        public DbSet<BoardMember> BoardMembers { get; set; }
        public DbSet<TaskAssignee> TaskAssignees { get; set; }
        public DbSet<TaskComment> TaskComments { get; set; }
        public DbSet<TaskAttachment> TaskAttachments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Identity ayarlarını yüklemek için ZORUNLU
            base.OnModelCreating(modelBuilder);

            // ==========================================
            // 1. BOARD CONFIGURATION
            // ==========================================
            modelBuilder.Entity<Board>(entity =>
            {
                entity.HasKey(b => b.Id);
                entity.Property(b => b.Name).IsRequired().HasMaxLength(100);
                entity.Property(b => b.Description).HasMaxLength(500);
                entity.Property(b => b.JoinCode).IsRequired().HasMaxLength(6);
                entity.Property(b => b.CreatedAt).HasDefaultValueSql("GETDATE()");

                // Board - Owner (AppUser)
                entity.HasOne(b => b.Owner)
                      .WithMany(u => u.OwnedBoards)
                      .HasForeignKey(b => b.OwnerId)
                      .OnDelete(DeleteBehavior.Restrict); 
                
                // Index
                entity.HasIndex(b => b.JoinCode).IsUnique();
            });


            modelBuilder.Entity<BoardColumn>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(50);
                entity.Property(c => c.Order).HasDefaultValue(0);

                // Column - Board
                entity.HasOne(c => c.Board)
                      .WithMany(b => b.Columns)
                      .HasForeignKey(c => c.BoardId)
                      .OnDelete(DeleteBehavior.Cascade); 

                // Index
                entity.HasIndex(c => new { c.BoardId, c.Order });
            });


            modelBuilder.Entity<BoardTask>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
                entity.Property(t => t.Description).HasMaxLength(1000);
                entity.Property(t => t.Priority)
                    .HasConversion<string>()
                    .HasDefaultValue(TaskPriority.Medium);                
                entity.Property(t => t.Order).HasDefaultValue(0);
                entity.Property(t => t.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(t => t.Column)
                      .WithMany(c => c.Tasks)
                      .HasForeignKey(t => t.ColumnId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasIndex(t => new { t.ColumnId, t.Order });
                entity.HasIndex(t => t.DueDate);
                entity.HasIndex(t => t.Priority);
            });


            modelBuilder.Entity<BoardMember>(entity =>
            {
                entity.HasKey(bm => new { bm.BoardId, bm.UserId }); 

                entity.HasOne(bm => bm.Board)
                      .WithMany(b => b.Members)
                      .HasForeignKey(bm => bm.BoardId)
                      .OnDelete(DeleteBehavior.Cascade); 

                entity.HasOne(bm => bm.User)
                      .WithMany(u => u.JoinedBoards)
                      .HasForeignKey(bm => bm.UserId)
                      .OnDelete(DeleteBehavior.Restrict); 
                entity.Property(bm => bm.Role).HasMaxLength(50).HasDefaultValue("Member");
                entity.Property(bm => bm.JoinedAt).HasDefaultValueSql("GETDATE()");
            });


            modelBuilder.Entity<TaskAssignee>(entity =>
            {
                entity.HasKey(ta => new { ta.TaskId, ta.UserId }); // Composite Key

                entity.HasOne(ta => ta.Task)
                      .WithMany(t => t.Assignees)
                      .HasForeignKey(ta => ta.TaskId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ta => ta.User)
                      .WithMany(u => u.AssignedTasks)
                      .HasForeignKey(ta => ta.UserId)
                      .OnDelete(DeleteBehavior.Restrict); 

                entity.Property(ta => ta.AssignedAt).HasDefaultValueSql("GETDATE()");
            });


            modelBuilder.Entity<TaskComment>(entity =>
            {
                entity.HasKey(tc => tc.Id);
                entity.Property(tc => tc.Content).IsRequired().HasMaxLength(1000);
                entity.Property(tc => tc.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(tc => tc.Task)
                      .WithMany(t => t.Comments)
                      .HasForeignKey(tc => tc.TaskId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(tc => tc.User)
                      .WithMany(u => u.Comments)
                      .HasForeignKey(tc => tc.UserId)
                      .OnDelete(DeleteBehavior.Restrict); 
            });

 
            modelBuilder.Entity<TaskAttachment>(entity =>
            {
                entity.HasKey(ta => ta.Id);
                entity.Property(ta => ta.FileName).IsRequired().HasMaxLength(255);
                entity.Property(ta => ta.FileType).HasMaxLength(50);
                entity.Property(ta => ta.UploadedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(ta => ta.Task)
                      .WithMany(t => t.Attachments)
                      .HasForeignKey(ta => ta.TaskId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ta => ta.User)
                      .WithMany(u => u.Attachments)
                      .HasForeignKey(ta => ta.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}