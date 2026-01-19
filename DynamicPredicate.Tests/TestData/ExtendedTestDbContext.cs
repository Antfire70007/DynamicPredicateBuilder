using Microsoft.EntityFrameworkCore;

namespace DynamicPredicate.Tests.TestData
{
    /// <summary>
    /// 擴展的測試 DbContext，用於複雜的導覽屬性
    /// </summary>
    public class ExtendedTestDbContext : DbContext
    {
        public ExtendedTestDbContext(DbContextOptions<ExtendedTestDbContext> options) : base(options) { }

        // 原有的 DbSet
        public DbSet<Company> Companies { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<Manager> Managers { get; set; } = null!;
        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<ProjectAssignment> ProjectAssignments { get; set; } = null!;
        public DbSet<EmployeeProfile> EmployeeProfiles { get; set; } = null!;
        public DbSet<ProjectDetail> ProjectDetails { get; set; } = null!;

        // 新增的合約相關 DbSet
        public DbSet<Contract> Contracts { get; set; } = null!;
        public DbSet<BuildContract> BuildContracts { get; set; } = null!;
        public DbSet<Build> Builds { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 公司 - 部門關聯
            modelBuilder.Entity<Department>()
                .HasOne(d => d.Company)
                .WithMany(c => c.Departments)
                .HasForeignKey(d => d.CompanyId);

            // 部門 - 員工關聯
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId);

            // 部門 - 管理者關聯 (一對一)
            modelBuilder.Entity<Manager>()
                .HasOne(m => m.Department)
                .WithOne(d => d.Manager)
                .HasForeignKey<Manager>(m => m.DepartmentId);

            // 員工 - 員工檔案關聯 (一對一)
            modelBuilder.Entity<EmployeeProfile>()
                .HasOne(p => p.Employee)
                .WithOne(e => e.Profile)
                .HasForeignKey<EmployeeProfile>(p => p.EmployeeId);

            // 公司 - 專案關聯
            modelBuilder.Entity<Project>()
                .HasOne(p => p.Company)
                .WithMany(c => c.Projects)
                .HasForeignKey(p => p.CompanyId);

            // 專案 - 專案詳細資料關聯 (一對一)
            modelBuilder.Entity<ProjectDetail>()
                .HasOne(pd => pd.Project)
                .WithOne(p => p.Detail)
                .HasForeignKey<ProjectDetail>(pd => pd.ProjectId);

            // 專案分配關聯 (多對多)
            modelBuilder.Entity<ProjectAssignment>()
                .HasOne(pa => pa.Employee)
                .WithMany(e => e.ProjectAssignments)
                .HasForeignKey(pa => pa.EmployeeId);

            modelBuilder.Entity<ProjectAssignment>()
                .HasOne(pa => pa.Project)
                .WithMany(p => p.ProjectAssignments)
                .HasForeignKey(pa => pa.ProjectId);

            // 合約相關的關聯設定
            // 合約 - 建案合約關聯 (一對多)
            modelBuilder.Entity<BuildContract>()
                .HasOne(bc => bc.Contract)
                .WithMany(c => c.BuildContracts)
                .HasForeignKey(bc => bc.ContractId);

            // 建案 - 建案合約關聯 (一對多)
            modelBuilder.Entity<BuildContract>()
                .HasOne(bc => bc.Build)
                .WithMany(b => b.BuildContracts)
                .HasForeignKey(bc => bc.BuildId);

            // 設定 decimal 精度
            modelBuilder.Entity<Employee>()
                .Property(e => e.Salary)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Manager>()
                .Property(m => m.Bonus)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Project>()
                .Property(p => p.Budget)
                .HasPrecision(18, 2);

            modelBuilder.Entity<BuildContract>()
                .Property(bc => bc.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Build>()
                .Property(b => b.Price)
                .HasPrecision(18, 2);
        }
    }
}