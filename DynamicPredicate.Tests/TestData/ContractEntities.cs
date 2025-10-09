using System;
using System.Collections.Generic;

namespace DynamicPredicate.Tests.TestData
{
    /// <summary>
    /// �X������
    /// </summary>
    public class Contract
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public List<BuildContract> BuildContracts { get; set; } = new();
    }

    /// <summary>
    /// �خצX������
    /// </summary>
    public class BuildContract
    {
        public int Id { get; set; }
        public int ContractId { get; set; }
        public Contract Contract { get; set; } = null!;
        public int BuildId { get; set; }
        public Build Build { get; set; } = null!;
        public string ContractType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime SignedDate { get; set; }
    }

    /// <summary>
    /// �خ׹���
    /// </summary>
    public class Build
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public long? AptId { get; set; }  // nullable long ������ AptId
        public string Location { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public List<BuildContract> BuildContracts { get; set; } = new();
    }
}