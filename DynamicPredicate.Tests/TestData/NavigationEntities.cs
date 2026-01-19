using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DynamicPredicate.Tests.TestData
{
    /// <summary>
    /// 公司實體
    /// </summary>
    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public List<Department> Departments { get; set; } = new();
        public List<Project> Projects { get; set; } = new();
    }

    /// <summary>
    /// 部門實體
    /// </summary>
    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;
        public List<Employee> Employees { get; set; } = new();
        public Manager Manager { get; set; } = null!;
    }

    /// <summary>
    /// 員工實體
    /// </summary>
    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public DateTime HireDate { get; set; }
        public int DepartmentId { get; set; }
        public Department Department { get; set; } = null!;
        public List<ProjectAssignment> ProjectAssignments { get; set; } = new();
        public EmployeeProfile Profile { get; set; } = null!;
    }

    /// <summary>
    /// 管理者實體
    /// </summary>
    public class Manager
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public decimal Bonus { get; set; }
        public int DepartmentId { get; set; }
        public Department Department { get; set; } = null!;
    }

    /// <summary>
    /// 專案實體
    /// </summary>
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal Budget { get; set; }
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;
        public List<ProjectAssignment> ProjectAssignments { get; set; } = new();
        public ProjectDetail Detail { get; set; } = null!;
    }

    /// <summary>
    /// 專案分配實體
    /// </summary>
    public class ProjectAssignment
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;
        public string Role { get; set; } = string.Empty;
        public int HoursPerWeek { get; set; }
        public DateTime AssignedDate { get; set; }
    }

    /// <summary>
    /// 員工檔案實體
    /// </summary>
    public class EmployeeProfile
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;
        public string Skills { get; set; } = string.Empty;
        public int YearsOfExperience { get; set; }
        public string Education { get; set; } = string.Empty;
    }

    /// <summary>
    /// 專案詳細資料實體
    /// </summary>
    public class ProjectDetail
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;
        public string TechnicalStack { get; set; } = string.Empty;
        public int EstimatedHours { get; set; }
        public string Priority { get; set; } = string.Empty;
    }
}