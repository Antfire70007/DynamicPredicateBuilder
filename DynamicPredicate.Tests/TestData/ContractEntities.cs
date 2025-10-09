using System;
using System.Collections.Generic;

namespace DynamicPredicate.Tests.TestData
{
    /// <summary>
    /// 合約實體
    /// </summary>
    public class Contract
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public List<BuildContract> BuildContracts { get; set; } = new();
    }

    /// <summary>
    /// 建案合約實體
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
    /// 建案實體
    /// </summary>
    public class Build
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public long? AptId { get; set; }  // nullable long 類型的 AptId
        public string Location { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public List<BuildContract> BuildContracts { get; set; } = new();
    }
}