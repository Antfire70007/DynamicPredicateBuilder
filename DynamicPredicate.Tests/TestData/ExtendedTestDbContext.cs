using Microsoft.EntityFrameworkCore;

namespace DynamicPredicate.Tests.TestData
{
    /// <summary>
    /// �X�i������ DbContext�A�Ω�����������ݩ�
    /// </summary>
    public class ExtendedTestDbContext : DbContext
    {
        public ExtendedTestDbContext(DbContextOptions<ExtendedTestDbContext> options) : base(options) { }

        // �즳�� DbSet
        public DbSet<Company> Companies { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<Manager> Managers { get; set; } = null!;
        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<ProjectAssignment> ProjectAssignments { get; set; } = null!;
        public DbSet<EmployeeProfile> EmployeeProfiles { get; set; } = null!;
        public DbSet<ProjectDetail> ProjectDetails { get; set; } = null!;

        // �s�W���X������ DbSet
        public DbSet<Contract> Contracts { get; set; } = null!;
        public DbSet<BuildContract> BuildContracts { get; set; } = null!;
        public DbSet<Build> Builds { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ���q - �������p
            modelBuilder.Entity<Department>()
                .HasOne(d => d.Company)
                .WithMany(c => c.Departments)
                .HasForeignKey(d => d.CompanyId);

            // ���� - ���u���p
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId);

            // ���� - �޲z�����p (�@��@)
            modelBuilder.Entity<Manager>()
                .HasOne(m => m.Department)
                .WithOne(d => d.Manager)
                .HasForeignKey<Manager>(m => m.DepartmentId);

            // ���u - ���u�ɮ����p (�@��@)
            modelBuilder.Entity<EmployeeProfile>()
                .HasOne(p => p.Employee)
                .WithOne(e => e.Profile)
                .HasForeignKey<EmployeeProfile>(p => p.EmployeeId);

            // ���q - �M�����p
            modelBuilder.Entity<Project>()
                .HasOne(p => p.Company)
                .WithMany(c => c.Projects)
                .HasForeignKey(p => p.CompanyId);

            // �M�� - �M�׸ԲӸ�����p (�@��@)
            modelBuilder.Entity<ProjectDetail>()
                .HasOne(pd => pd.Project)
                .WithOne(p => p.Detail)
                .HasForeignKey<ProjectDetail>(pd => pd.ProjectId);

            // �M�פ��t���p (�h��h)
            modelBuilder.Entity<ProjectAssignment>()
                .HasOne(pa => pa.Employee)
                .WithMany(e => e.ProjectAssignments)
                .HasForeignKey(pa => pa.EmployeeId);

            modelBuilder.Entity<ProjectAssignment>()
                .HasOne(pa => pa.Project)
                .WithMany(p => p.ProjectAssignments)
                .HasForeignKey(pa => pa.ProjectId);

            // �X�����������p�]�w
            // �X�� - �خצX�����p (�@��h)
            modelBuilder.Entity<BuildContract>()
                .HasOne(bc => bc.Contract)
                .WithMany(c => c.BuildContracts)
                .HasForeignKey(bc => bc.ContractId);

            // �خ� - �خצX�����p (�@��h)
            modelBuilder.Entity<BuildContract>()
                .HasOne(bc => bc.Build)
                .WithMany(b => b.BuildContracts)
                .HasForeignKey(bc => bc.BuildId);

            // �]�w decimal ���
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