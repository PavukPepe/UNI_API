using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace UNI.Models;

public partial class UniContext : DbContext
{
    public UniContext()
    {
    }

    public UniContext(DbContextOptions<UniContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Block> Blocks { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Certificate> Certificates { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Step> Steps { get; set; }

    public virtual DbSet<Token> Tokens { get; set; }

    public virtual DbSet<Topic> Topics { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Userprogress> Userprogresses { get; set; }

    public virtual DbSet<Wishlist> Wishlists { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=UNI;Username=postgres;Password=20332035");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Block>(entity =>
        {
            entity.HasKey(e => e.BlockId).HasName("blocks_pkey");

            entity.ToTable("blocks");

            entity.HasIndex(e => e.CourseId, "blocks_course_index");

            entity.Property(e => e.BlockId).HasColumnName("block_id");
            entity.Property(e => e.BlockTitle)
                .HasMaxLength(255)
                .HasColumnName("block_title");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.DisplayOrder).HasColumnName("display_order");

            entity.HasOne(d => d.Course).WithMany(p => p.Blocks)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("blocks_course_id_fkey");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("categories_pkey");

            entity.ToTable("categories");

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(100)
                .HasColumnName("category_name");
            entity.Property(e => e.CreatedByUser).HasColumnName("created_by_user");

            entity.HasOne(d => d.CreatedByUserNavigation).WithMany(p => p.Categories)
                .HasForeignKey(d => d.CreatedByUser)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("categories_created_by_user_fkey");
        });

        modelBuilder.Entity<Certificate>(entity =>
        {
            entity.HasKey(e => e.CertificateId).HasName("certificates_pkey");

            entity.ToTable("certificates");

            entity.HasIndex(e => e.CertificateCode, "certificates_certificate_code_key").IsUnique();

            entity.HasIndex(e => e.CourseId, "certificates_course_index");

            entity.HasIndex(e => e.UserId, "certificates_user_index");

            entity.Property(e => e.CertificateId).HasColumnName("certificate_id");
            entity.Property(e => e.CertificateCode)
                .HasMaxLength(50)
                .HasColumnName("certificate_code");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.IssueDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("issue_date");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Course).WithMany(p => p.Certificates)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("certificates_course_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Certificates)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("certificates_user_id_fkey");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseId).HasName("courses_pkey");

            entity.ToTable("courses");

            entity.HasIndex(e => e.AuthorId, "courses_author_index");

            entity.HasIndex(e => e.CategoryId, "courses_category_index");

            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.AuthorId).HasColumnName("author_id");
            entity.Property(e => e.AverageRating)
                .HasPrecision(2, 1)
                .HasDefaultValueSql("0.0")
                .HasColumnName("average_rating");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CourseDescription).HasColumnName("course_description");
            entity.Property(e => e.CourseLanguage)
                .HasMaxLength(10)
                .HasColumnName("course_language");
            entity.Property(e => e.CourseLogo)
                .HasMaxLength(255)
                .HasColumnName("course_logo");
            entity.Property(e => e.CoursePrice)
                .HasPrecision(10, 2)
                .HasDefaultValueSql("0.00")
                .HasColumnName("course_price");
            entity.Property(e => e.CourseTitle)
                .HasMaxLength(255)
                .HasColumnName("course_title");
            entity.Property(e => e.CreationDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("creation_date");
            entity.Property(e => e.DifficultyLevel)
                .HasMaxLength(20)
                .HasColumnName("difficulty_level");
            entity.Property(e => e.DurationHours).HasColumnName("duration_hours");
            entity.Property(e => e.IsApproved)
                .HasDefaultValue(false)
                .HasColumnName("is_approved");

            entity.HasOne(d => d.Author).WithMany(p => p.Courses)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("courses_author_id_fkey");

            entity.HasOne(d => d.Category).WithMany(p => p.Courses)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("courses_category_id_fkey");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("payments_pkey");

            entity.ToTable("payments");

            entity.HasIndex(e => e.CourseId, "payments_course_index");

            entity.HasIndex(e => e.UserId, "payments_user_index");

            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.PaymentAmount)
                .HasPrecision(10, 2)
                .HasColumnName("payment_amount");
            entity.Property(e => e.PaymentDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("payment_date");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .HasColumnName("payment_status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Course).WithMany(p => p.Payments)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("payments_course_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Payments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("payments_user_id_fkey");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("reviews_pkey");

            entity.ToTable("reviews");

            entity.HasIndex(e => e.CourseId, "reviews_course_index");

            entity.HasIndex(e => e.UserId, "reviews_user_index");

            entity.Property(e => e.ReviewId).HasColumnName("review_id");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.ReviewText).HasColumnName("review_text");
            entity.Property(e => e.SubmissionDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("submission_date");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.UserRating).HasColumnName("user_rating");

            entity.HasOne(d => d.Course).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("reviews_course_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("reviews_user_id_fkey");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("roles_pkey");

            entity.ToTable("roles");

            entity.HasIndex(e => e.RoleName, "roles_role_name_key").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<Step>(entity =>
        {
            entity.HasKey(e => e.StepId).HasName("steps_pkey");

            entity.ToTable("steps");

            entity.HasIndex(e => e.TopicId, "steps_topic_index");

            entity.Property(e => e.StepId).HasColumnName("step_id");
            entity.Property(e => e.ContentType)
                .HasMaxLength(20)
                .HasColumnName("content_type");
            entity.Property(e => e.DisplayOrder).HasColumnName("display_order");
            entity.Property(e => e.StepContent).HasColumnName("step_content");
            entity.Property(e => e.StepTitle)
                .HasMaxLength(255)
                .HasColumnName("step_title");
            entity.Property(e => e.TopicId).HasColumnName("topic_id");

            entity.HasOne(d => d.Topic).WithMany(p => p.Steps)
                .HasForeignKey(d => d.TopicId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("steps_topic_id_fkey");
        });

        modelBuilder.Entity<Token>(entity =>
        {
            entity.HasKey(e => e.TokenId).HasName("tokens_pkey");

            entity.ToTable("tokens");

            entity.HasIndex(e => e.UserId, "tokens_user_index");

            entity.Property(e => e.TokenId).HasColumnName("token_id");
            entity.Property(e => e.AccessToken)
                .HasMaxLength(512)
                .HasColumnName("access_token");
            entity.Property(e => e.ExpirationTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("expiration_time");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Tokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("tokens_user_id_fkey");
        });

        modelBuilder.Entity<Topic>(entity =>
        {
            entity.HasKey(e => e.TopicId).HasName("topics_pkey");

            entity.ToTable("topics");

            entity.HasIndex(e => e.BlockId, "topics_block_index");

            entity.Property(e => e.TopicId).HasColumnName("topic_id");
            entity.Property(e => e.BlockId).HasColumnName("block_id");
            entity.Property(e => e.DisplayOrder).HasColumnName("display_order");
            entity.Property(e => e.TopicTitle)
                .HasMaxLength(255)
                .HasColumnName("topic_title");

            entity.HasOne(d => d.Block).WithMany(p => p.Topics)
                .HasForeignKey(d => d.BlockId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("topics_block_id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "users_email_index");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.IsBlocked)
                .HasDefaultValue(false)
                .HasColumnName("is_blocked");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.ProfilePicture)
                .HasMaxLength(255)
                .HasColumnName("profile_picture");
            entity.Property(e => e.RegistrationDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("registration_date");

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "Userrole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .HasConstraintName("userroles_role_id_fkey"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("userroles_user_id_fkey"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId").HasName("userroles_pkey");
                        j.ToTable("userroles");
                        j.IndexerProperty<int>("UserId").HasColumnName("user_id");
                        j.IndexerProperty<int>("RoleId").HasColumnName("role_id");
                    });
        });

        modelBuilder.Entity<Userprogress>(entity =>
        {
            entity.HasKey(e => e.ProgressId).HasName("userprogress_pkey");

            entity.ToTable("userprogress");

            entity.HasIndex(e => e.StepId, "progress_step_index");

            entity.HasIndex(e => e.UserId, "progress_user_index");

            entity.Property(e => e.ProgressId).HasColumnName("progress_id");
            entity.Property(e => e.CompletionDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("completion_date");
            entity.Property(e => e.IsCompleted)
                .HasDefaultValue(false)
                .HasColumnName("is_completed");
            entity.Property(e => e.StepId).HasColumnName("step_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Step).WithMany(p => p.Userprogresses)
                .HasForeignKey(d => d.StepId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("userprogress_step_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Userprogresses)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("userprogress_user_id_fkey");
        });

        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasKey(e => e.WishlistId).HasName("wishlist_pkey");

            entity.ToTable("wishlist");

            entity.HasIndex(e => e.CourseId, "wishlist_course_index");

            entity.HasIndex(e => e.UserId, "wishlist_user_index");

            entity.Property(e => e.WishlistId).HasColumnName("wishlist_id");
            entity.Property(e => e.AddedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("added_date");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Course).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("wishlist_course_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("wishlist_user_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
