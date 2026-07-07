using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PolicyAPI.Data
{
    public class PolicyDbContext : DbContext
    {
        public PolicyDbContext(DbContextOptions<PolicyDbContext> options) : base(options) { }

        public DbSet<Role> Roles { get; set; }
        public DbSet<Registration> Registrations { get; set; }
        public DbSet<Policy> Policies { get; set; }
        public DbSet<Status> Statuses { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<LastLogin> LastLogins { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Designation> Designations { get; set; }
        public DbSet<RegistrationCodeMap> RegistrationCodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "Admin" },
                new Role { RoleId = 2, RoleName = "Faculty" },
                new Role { RoleId = 3, RoleName = "Head" },
                new Role { RoleId = 4, RoleName = "Dean - Academics" },
                new Role { RoleId = 5, RoleName = "Dean - Research" },
                new Role { RoleId = 6, RoleName = "Accountant" },
                new Role { RoleId = 7, RoleName = "Lab Instructor" },
                new Role { RoleId = 8, RoleName = "System Admin" },
                new Role { RoleId = 9, RoleName = "IT Admin" },
                new Role { RoleId = 10, RoleName = "Principal" },
                new Role { RoleId = 11, RoleName = "Administrative Officer" },
                new Role { RoleId = 12, RoleName = "Assistant Administrative Officer" }
            );

            modelBuilder.Entity<Role>()
                .HasIndex(r => r.RoleName)
                .IsUnique();

            modelBuilder.Entity<Registration>()
                .HasIndex(u => u.EmployeeId)
                .IsUnique();

            modelBuilder.Entity<Registration>()
                .HasIndex(u => u.EmailId)
                .IsUnique();

            modelBuilder.Entity<Policy>()
                .HasIndex(p => p.PolicyTitle)
                .IsUnique();

            modelBuilder.Entity<Status>()
                .HasIndex(s => new { s.EmployeeId, s.PolicyTitle })
                .IsUnique();

            modelBuilder.Entity<Quiz>()
                .HasIndex(q => new { q.EmployeeId, q.QuizTitle })
                .IsUnique();
        }
    }

    [Table("Roles")]
    public class Role
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public byte RoleId { get; set; }

        [Required]
        [MaxLength(150)]
        public string RoleName { get; set; } = string.Empty;
    }

    [Table("Registrations")]
    public class Registration
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Required]
        [MaxLength(30)]
        public string EmployeeId { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Department { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Designation { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string EmailId { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string ContactNo { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string RegistrationCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public byte RoleId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
    }

    [Table("Policies")]
    public class Policy
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PolicyId { get; set; }

        [Required]
        [MaxLength(500)]
        public string PolicyTitle { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Version { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Overview { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string PolicyDocumentLink { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? FaqFileId { get; set; }

        [MaxLength(100)]
        public string? QuizFileId { get; set; }

        public DateTime LastUploaded { get; set; } = DateTime.UtcNow;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
    }

    [Table("Status")]
    public class Status
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long StatusId { get; set; }

        [Required]
        [MaxLength(30)]
        public string EmployeeId { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Department { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string PolicyTitle { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string Acknowledged { get; set; } = "Yes";

        public DateTime DateAcknowledged { get; set; } = DateTime.UtcNow;
    }

    [Table("Quiz")]
    public class Quiz
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long QuizAttemptId { get; set; }

        [Required]
        [MaxLength(30)]
        public string EmployeeId { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string QuizTitle { get; set; } = string.Empty;

        public decimal? Attempt1 { get; set; }
        public decimal? Attempt2 { get; set; }
        public decimal? Attempt3 { get; set; }
        public decimal? Attempt4 { get; set; }
        public decimal? Attempt5 { get; set; }
        public decimal? Attempt6 { get; set; }
        public decimal? Attempt7 { get; set; }
        public decimal? Attempt8 { get; set; }
        public decimal? Attempt9 { get; set; }
        public decimal? Attempt10 { get; set; }

        public decimal BestScore { get; set; } = 0.00m;

        public DateTime DateAttempted { get; set; } = DateTime.UtcNow;
    }

    [Table("LastLogin")]
    public class LastLogin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long LoginSessionId { get; set; }

        [Required]
        [MaxLength(30)]
        public string EmployeeId { get; set; } = string.Empty;

        public DateTime LoginTime { get; set; } = DateTime.UtcNow;
    }

    [Table("Notifications")]
    public class Notification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int NotificationId { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("Designations")]
    public class Designation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DesignationId { get; set; }

        [Required]
        [MaxLength(150)]
        public string DesignationName { get; set; } = string.Empty;
    }

    [Table("RegistrationCodes")]
    public class RegistrationCodeMap
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(250)]
        public string Department { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = string.Empty;
    }
}
