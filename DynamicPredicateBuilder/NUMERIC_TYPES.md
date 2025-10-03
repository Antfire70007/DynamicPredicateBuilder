# DynamicPredicateBuilder 數值類型支援指南

## 概述

DynamicPredicateBuilder v1.0.83+ 提供對所有 .NET 數值類型的完整支援，包括 nullable 變體。本文檔詳細說明各種數值類型的使用方式和最佳實務。

## 支援的數值類型

### 整數類型

| 類型 | 範圍 | 範例值 | 用途 |
|------|------|---------|------|
| `int`, `int?` | -2,147,483,648 到 2,147,483,647 | `Age = 25` | 一般整數 |
| `long`, `long?` | -9,223,372,036,854,775,808 到 9,223,372,036,854,775,807 | `Id = 1234567890L` | 大整數、ID |

### 浮點數類型

| 類型 | 精度 | 範例值 | 用途 |
|------|------|---------|------|
| `float`, `float?` | 7 位數 | `Rate = 3.14f` | 低精度浮點數 |
| `double`, `double?` | 15-17 位數 | `Score = 95.5678901234567` | 高精度浮點數 |

### 十進位類型

| 類型 | 精度 | 範例值 | 用途 |
|------|------|---------|------|
| `decimal`, `decimal?` | 28-29 位數 | `Price = 123456.789012345m` | 金融計算、高精度 |

## Nullable 類型特殊行為

### Null 值比較

```csharp
// 查詢 null 值
new FilterRule { Property = "Salary", Operator = FilterOperator.Equal, Value = null }

// 查詢非 null 值
new FilterRule { Property = "Salary", Operator = FilterOperator.NotEqual, Value = null }
```

### 數值比較中的 Null

在數值比較運算中，null 值的行為如下：

```csharp
// null > 50000 → false
// null < 50000 → false  
// null >= 50000 → false
// null <= 50000 → false
// null == null → true
// null != null → false
```

## 運算子支援矩陣

| 運算子 | int/int? | long/long? | decimal/decimal? | double/double? | float/float? |
|--------|----------|------------|------------------|----------------|--------------|
| Equal | ? | ? | ? | ? | ? |
| NotEqual | ? | ? | ? | ? | ? |
| GreaterThan | ? | ? | ? | ? | ? |
| GreaterThanOrEqual | ? | ? | ? | ? | ? |
| LessThan | ? | ? | ? | ? | ? |
| LessThanOrEqual | ? | ? | ? | ? | ? |
| Between | ? | ? | ? | ? | ? |
| NotBetween | ? | ? | ? | ? | ? |
| In | ? | ? | ? | ? | ? |
| NotIn | ? | ? | ? | ? | ? |

## 使用範例

### 基本數值查詢

```csharp
// 整數查詢
new FilterRule { Property = "Age", Operator = FilterOperator.GreaterThan, Value = 18 }

// 長整數查詢
new FilterRule { Property = "UserId", Operator = FilterOperator.Equal, Value = 1234567890L }

// Decimal 查詢
new FilterRule { Property = "Price", Operator = FilterOperator.LessThanOrEqual, Value = 999.99m }

// Double 查詢
new FilterRule { Property = "Score", Operator = FilterOperator.GreaterThanOrEqual, Value = 85.5 }

// Float 查詢
new FilterRule { Property = "Rate", Operator = FilterOperator.Between, Value = new[] { 0.1f, 0.9f } }
```

### Nullable 類型查詢

```csharp
// 查詢有薪資的員工
new FilterRule { Property = "Salary", Operator = FilterOperator.GreaterThan, Value = 0m }

// 查詢沒有薪資資料的員工
new FilterRule { Property = "Salary", Operator = FilterOperator.Equal, Value = null }

// 查詢薪資在範圍內的員工（排除 null）
new FilterRule { Property = "Salary", Operator = FilterOperator.Between, Value = new[] { 30000m, 80000m } }

// 查詢薪資在指定值中的員工（包含 null）
new FilterRule { Property = "Salary", Operator = FilterOperator.In, Value = new object[] { 50000m, 60000m, null } }
```

### 複雜查詢範例

```csharp
public class Employee
{
    public string Name { get; set; }
    public int Age { get; set; }
    public decimal? Salary { get; set; }
    public decimal? Bonus { get; set; }
    public double? Rating { get; set; }
}

// 查詢高薪且有評分的員工
var filterGroup = new FilterGroup
{
    LogicalOperator = LogicalOperator.And,
    Rules = new List<object>
    {
        new FilterRule { Property = "Salary", Operator = FilterOperator.GreaterThan, Value = 80000m },
        new FilterRule { Property = "Rating", Operator = FilterOperator.NotEqual, Value = null },
        new FilterRule { Property = "Rating", Operator = FilterOperator.GreaterThanOrEqual, Value = 4.0 }
    }
};

// 查詢薪資或獎金較高的員工
var filterGroup2 = new FilterGroup
{
    LogicalOperator = LogicalOperator.Or,
    Rules = new List<object>
    {
        new FilterRule { Property = "Salary", Operator = FilterOperator.GreaterThan, Value = 100000m },
        new FilterRule { Property = "Bonus", Operator = FilterOperator.GreaterThan, Value = 20000m }
    }
};
```

## 類型轉換機制

### 自動類型轉換

DynamicPredicateBuilder 提供智慧型類型轉換：

```csharp
// 字串自動轉換為數值
new FilterRule { Property = "Age", Operator = FilterOperator.Equal, Value = "25" } // → int 25

// 整數自動轉換為 decimal
new FilterRule { Property = "Price", Operator = FilterOperator.Equal, Value = 100 } // → decimal 100m

// Double 自動轉換為 decimal（可能有精度損失）
new FilterRule { Property = "Amount", Operator = FilterOperator.Equal, Value = 123.45 } // → decimal 123.45m
```

### 安全轉換機制

所有類型轉換都使用安全的 TryParse 方法：

1. **直接類型匹配**：如果值已經是目標類型，直接使用
2. **TryParse 轉換**：使用對應的 TryParse 方法
3. **Convert.ChangeType 回退**：最後使用 .NET 內建轉換

```csharp
// 內部轉換邏輯示例
if (value is decimal decimalValue)
    return decimalValue;  // 直接使用
if (decimal.TryParse(value.ToString(), out var parsedDecimal))
    return parsedDecimal;  // 安全轉換
return Convert.ToDecimal(value);  // 回退轉換
```

## 效能考量

### Decimal vs Double

```csharp
// ? 推薦：金融計算使用 decimal
new FilterRule { Property = "Price", Operator = FilterOperator.Equal, Value = 19.99m }

// ?? 注意：科學計算可使用 double，但要注意精度
new FilterRule { Property = "Measurement", Operator = FilterOperator.Equal, Value = 19.99 }
```

### 大數值處理

```csharp
// ? 支援：大整數
new FilterRule { Property = "BigId", Operator = FilterOperator.Equal, Value = 9223372036854775807L }

// ? 支援：高精度 decimal
new FilterRule { Property = "PreciseAmount", Operator = FilterOperator.Equal, Value = 123456789.123456789m }
```

## 最佳實務

### 1. 選擇合適的數值類型

```csharp
// 計數、索引 → int
public int Count { get; set; }

// 大型 ID、時間戳 → long  
public long Timestamp { get; set; }

// 金錢、精確計算 → decimal
public decimal Price { get; set; }

// 科學計算、統計 → double
public double Average { get; set; }

// 比率、百分比 → float（如果精度足夠）
public float Percentage { get; set; }
```

### 2. Nullable 類型的使用

```csharp
// ? 推薦：可選的數值欄位使用 nullable
public decimal? OptionalFee { get; set; }
public double? Rating { get; set; }

// ? 推薦：明確處理 null 值
var hasRating = new FilterRule { Property = "Rating", Operator = FilterOperator.NotEqual, Value = null };
var highRating = new FilterRule { Property = "Rating", Operator = FilterOperator.GreaterThan, Value = 4.0 };
```

### 3. 範圍查詢最佳化

```csharp
// ? 推薦：使用 Between 進行範圍查詢
new FilterRule { Property = "Price", Operator = FilterOperator.Between, Value = new[] { 100m, 1000m } }

// ? 避免：使用多個比較條件
// new FilterRule { Property = "Price", Operator = FilterOperator.GreaterThanOrEqual, Value = 100m }
// new FilterRule { Property = "Price", Operator = FilterOperator.LessThanOrEqual, Value = 1000m }
```

### 4. 集合查詢

```csharp
// ? 推薦：使用 In 進行多值查詢
new FilterRule { Property = "CategoryId", Operator = FilterOperator.In, Value = new[] { 1, 2, 3 } }

// ? 支援：混合 null 值
new FilterRule { Property = "Score", Operator = FilterOperator.In, Value = new object[] { 85, 90, 95, null } }
```

## 錯誤處理

### 常見錯誤與解決方案

```csharp
try
{
    var predicate = FilterBuilder.Build<Employee>(filterGroup);
}
catch (FormatException ex)
{
    // 數值格式錯誤
    Console.WriteLine($"Invalid number format: {ex.Message}");
}
catch (OverflowException ex)
{
    // 數值溢出
    Console.WriteLine($"Number overflow: {ex.Message}");
}
catch (ArgumentException ex)
{
    // 參數錯誤（如 Between 缺少參數）
    Console.WriteLine($"Invalid argument: {ex.Message}");
}
```

### 驗證輸入資料

```csharp
public bool ValidateNumericFilter(FilterRule rule)
{
    switch (rule.Operator)
    {
        case FilterOperator.Between:
        case FilterOperator.NotBetween:
            return rule.Value is IEnumerable enumerable && 
                   enumerable.Cast<object>().Count() == 2;
                   
        case FilterOperator.In:
        case FilterOperator.NotIn:
            return rule.Value is IEnumerable;
            
        default:
            return rule.Value != null || rule.Operator == FilterOperator.Equal || rule.Operator == FilterOperator.NotEqual;
    }
}
```

## 單元測試範例

```csharp
[Fact]
public void Should_Handle_Nullable_Decimal_Correctly()
{
    var employee1 = new Employee { Salary = 50000m };
    var employee2 = new Employee { Salary = null };
    
    var rule = new FilterRule 
    { 
        Property = "Salary", 
        Operator = FilterOperator.GreaterThan, 
        Value = 40000m 
    };
    
    var predicate = FilterBuilder.Build<Employee>(new FilterGroup 
    { 
        Rules = new List<object> { rule } 
    }).Compile();
    
    Assert.True(predicate(employee1));   // 50000 > 40000
    Assert.False(predicate(employee2));  // null 不滿足 > 條件
}

[Fact]
public void Should_Handle_Decimal_Precision()
{
    var product = new Product { Price = 19.99m };
    
    var rule = new FilterRule 
    { 
        Property = "Price", 
        Operator = FilterOperator.Equal, 
        Value = 19.99m 
    };
    
    var predicate = FilterBuilder.Build<Product>(new FilterGroup 
    { 
        Rules = new List<object> { rule } 
    }).Compile();
    
    Assert.True(predicate(product));
}
```

## 版本歷史

### v1.0.83
- ? 新增完整的 nullable 數值類型支援
- ? 改善類型轉換安全性（TryParse 機制）
- ? 修復 Between/NotBetween 否定邏輯
- ? 修復 In/NotIn 陣列類型轉換
- ? 新增 decimal? 完整測試案例

### v1.0.82
- ? 基本數值類型支援
- ? 基礎 nullable 支援

---

## 相關資源

- [主要文檔](README.md)
- [API 參考](API_REFERENCE.md)
- [測試案例](../DynamicPredicate.Tests/Builders/FilterBuilderTests.cs)
- [範例程式碼](../DynamicPredicate.Tests/Examples/)

---

持續改進中，歡迎提供回饋！