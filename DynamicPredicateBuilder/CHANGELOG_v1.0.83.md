# DynamicPredicateBuilder v1.0.83 更新說明

## ?? 主要功能更新

### ? 完整支援 Nullable Decimal 類型

新版本對 `decimal?` 類型提供完整支援，包括：

- ? **等值比較**：`Equal`, `NotEqual` with null values
- ? **數值比較**：`GreaterThan`, `LessThan`, `GreaterThanOrEqual`, `LessThanOrEqual`
- ? **範圍查詢**：`Between`, `NotBetween`
- ? **集合查詢**：`In`, `NotIn` (支援 null 值)
- ? **複雜邏輯**：多重否定、巢狀條件

### ?? 強化的類型轉換機制

重構了 `ChangeType` 方法，新增：

- **安全轉換**：所有數值類型使用 `TryParse` 方法
- **明確處理**：`decimal`, `double`, `float`, `int`, `long`, `DateTime`
- **精度保持**：`decimal` 類型保持完整精度
- **錯誤預防**：避免 `InvalidCastException`

### ?? 重要錯誤修復

#### 1. In/NotIn 操作符修復
- **問題**：陣列參數無法正確轉換類型
- **解決**：在 `BuildIn` 方法中逐一轉換元素類型
- **影響**：所有集合查詢現在都能正常工作

#### 2. Between/NotBetween 否定邏輯修復
- **問題**：`IsNegated=true` 與 `NotBetween` 同時使用時出現雙重否定
- **解決**：統一否定邏輯處理
- **影響**：複雜否定條件現在行為正確

## ?? 測試覆蓋率提升

新增 **14 個專門的 Nullable Decimal 測試案例**：

```csharp
? BuildPredicate_NullableDecimal_EqualOperator_WithValue_ShouldWork
? BuildPredicate_NullableDecimal_EqualOperator_WithNull_ShouldWork  
? BuildPredicate_NullableDecimal_NotEqualOperator_WithValue_ShouldWork
? BuildPredicate_NullableDecimal_NotEqualOperator_WithNull_ShouldWork
? BuildPredicate_NullableDecimal_GreaterThanOperator_ShouldWork
? BuildPredicate_NullableDecimal_LessThanOperator_ShouldWork
? BuildPredicate_NullableDecimal_GreaterThanOrEqualOperator_ShouldWork
? BuildPredicate_NullableDecimal_LessThanOrEqualOperator_ShouldWork
? BuildPredicate_NullableDecimal_BetweenOperator_ShouldWork
? BuildPredicate_NullableDecimal_InOperator_ShouldWork
? BuildPredicate_NullableDecimal_InOperator_WithNull_ShouldWork
? BuildPredicate_NullableDecimal_ComplexConditions_ShouldWork
? BuildPredicate_NullableDecimal_WithNegation_ShouldWork
? BuildPredicate_NullableDecimal_MultipleGroups_ShouldWork
```

**總測試案例**：從 26 個增加到 **40 個**，涵蓋所有主要使用場景。

## ?? 使用範例

### 基本 Nullable Decimal 查詢

```csharp
// 查詢有薪資的員工
new FilterRule { Property = "Salary", Operator = FilterOperator.GreaterThan, Value = 0 }

// 查詢沒有薪資資料的員工  
new FilterRule { Property = "Salary", Operator = FilterOperator.Equal, Value = null }

// 查詢薪資在範圍內的員工
new FilterRule { Property = "Salary", Operator = FilterOperator.Between, Value = new[] { 30000m, 80000m } }
```

### 複雜查詢範例

```csharp
// 查詢高薪或無薪資記錄的員工
var filterGroup = new FilterGroup
{
    LogicalOperator = LogicalOperator.Or,
    Rules = new List<object>
    {
        new FilterRule { Property = "Salary", Operator = FilterOperator.Equal, Value = null },
        new FilterRule { Property = "Salary", Operator = FilterOperator.GreaterThan, Value = 80000m }
    }
};
```

### FilterDictionaryBuilder 範例

```csharp
// 使用 Expression 語法
var query = FilterDictionaryBuilder.QueryBuilder<Employee>()
    .WithLogicalOperator(LogicalOperator.And)
    .GreaterThan(x => x.Salary, 50000m)
    .Between(x => x.Bonus, 1000m, 5000m)
    .Equal(x => x.Commission, null)
    .Build();
```

## ?? 效能改善

- **記憶體使用**：優化類型轉換過程，減少不必要的物件建立
- **執行速度**：使用 `TryParse` 避免例外處理的效能損耗
- **編譯時間**：Expression Tree 建構更加高效

## ?? 文檔更新

### 新增文檔
- **`NUMERIC_TYPES.md`**：詳細的數值類型支援指南
- **README.md 第 5 節**：支援的資料類型說明
- **測試範例**：完整的 nullable decimal 使用案例

### 更新內容
- 運算子支援矩陣
- Nullable 類型特殊行為說明
- 最佳實務指南
- 常見問題解答

## ?? 遷移指南

### 從 v1.0.82 升級

**? 無破壞性變更**：現有程式碼無需修改

**?? 建議更新**：

1. **移除手動類型檢查**：
```csharp
// ? 舊版本需要的額外檢查
if (value is decimal decimalValue) 
{
    // 手動處理...
}

// ? 新版本自動處理
new FilterRule { Property = "Salary", Operator = FilterOperator.Equal, Value = "50000" }
```

2. **使用新的 Nullable 功能**：
```csharp
// ? 現在完全支援
new FilterRule { Property = "Salary", Operator = FilterOperator.In, Value = new object[] { 50000m, null } }
```

## ?? 已知問題修復

| 問題 | 狀態 | 說明 |
|------|------|------|
| Decimal 類型轉換異常 | ? 已修復 | 使用 TryParse 安全轉換 |
| In 操作符陣列類型錯誤 | ? 已修復 | 正確處理元素類型轉換 |
| NotBetween 雙重否定 | ? 已修復 | 統一否定邏輯處理 |
| Nullable 比較行為不一致 | ? 已修復 | 標準化 null 值處理 |

## ?? 下一步計畫

### v1.0.84 預計功能
- ?? 支援 `DateOnly` 和 `TimeOnly` (.NET 6+)
- ?? 增強 `Guid` 類型查詢功能
- ?? 新增自訂運算子擴展機制
- ?? 效能基準測試工具

### 長期規劃
- ?? 查詢快取機制
- ?? 非同步查詢支援
- ?? 多語言錯誤訊息
- ?? 查詢分析工具

## ?? 感謝

特別感謝社群回饋，讓我們能夠識別並修復這些重要問題。

---

## ?? 聯絡資訊

- **GitHub**: [DynamicPredicateBuilder](https://github.com/Antfire70007/DynamicPredicateBuilder)
- **Issues**: [回報問題](https://github.com/Antfire70007/DynamicPredicateBuilder/issues)
- **NuGet**: [套件頁面](https://www.nuget.org/packages/DynamicPredicateBuilder/)

---

**版本**: v1.0.83  
**發布日期**: 2024-12-19  
**相容性**: .NET 7.0, .NET 8.0, .NET 9.0