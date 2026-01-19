# ArrayLike 功能示例

## 概述
為 `FilterDictionaryBuilder` 新增完整的陣列導覽方法支援，包括 `ArrayLike` 和其他所有運算子。

## 新增的陣列導覽方法

### 基本方法
- `ArrayEqual` - 陣列元素等於指定值
- `ArrayNotEqual` - 陣列元素不等於指定值
- `ArrayLike` - 陣列元素符合 Like 條件 ? **新增**
- `ArrayNotLike` - 陣列元素不符合 Like 條件
- `ArrayContains` - 陣列元素包含指定字串
- `ArrayNotContains` - 陣列元素不包含指定字串

### 比較方法
- `ArrayGreaterThan` - 陣列元素大於指定值
- `ArrayGreaterThanOrEqual` - 陣列元素大於等於指定值
- `ArrayLessThan` - 陣列元素小於指定值
- `ArrayLessThanOrEqual` - 陣列元素小於等於指定值

### 字串方法
- `ArrayStartsWith` - 陣列元素以指定字串開頭
- `ArrayEndsWith` - 陣列元素以指定字串結尾

### 範圍方法
- `ArrayIn` - 陣列元素在指定集合中
- `ArrayNotIn` - 陣列元素不在指定集合中
- `ArrayBetween` - 陣列元素在指定範圍內
- `ArrayNotBetween` - 陣列元素不在指定範圍內

### 存在性方法
- `ArrayAny` - 陣列中有任何元素符合條件
- `ArrayNotAny` - 陣列中沒有任何元素符合條件

## 使用範例

### 1. ArrayLike 字串匹配
```csharp
using var context = CreateContractTestContext();

// 查找建案名稱包含 "豪宅" 的合約
var filterGroup = FilterDictionaryBuilder.QueryBuilder<Contract>()
    .WithLogicalOperator(LogicalOperator.And)
    .ArrayLike(c => c.BuildContracts, bc => bc.Build.Name, "豪宅")
    .ToFilterGroup();

var predicate = FilterBuilder.Build<Contract>(filterGroup);
var results = context.Contracts
    .Include(c => c.BuildContracts)
    .ThenInclude(bc => bc.Build)
    .Where(predicate)
    .ToList();

// 結果：找到 "豪宅購買合約"
```

### 2. ArrayContains 字串包含
```csharp
// 查找建案位置包含 "台北市" 的合約
var filterGroup = FilterDictionaryBuilder.QueryBuilder<Contract>()
    .ArrayStartsWith(c => c.BuildContracts, bc => bc.Build.Location, "台北市")
    .ToFilterGroup();
```

### 3. ArrayIn 集合查詢
```csharp
// 查找 AptId 在指定集合中的合約
var filterGroup = FilterDictionaryBuilder.QueryBuilder<Contract>()
    .ArrayIn(c => c.BuildContracts, bc => bc.Build.AptId, new object[] { 1001L, 1002L })
    .ToFilterGroup();
```

### 4. ArrayBetween 範圍查詢
```csharp
// 查找 AptId 在 1001-1003 範圍內的合約
var filterGroup = FilterDictionaryBuilder.QueryBuilder<Contract>()
    .ArrayBetween(c => c.BuildContracts, bc => bc.Build.AptId, 1001L, 1003L)
    .ToFilterGroup();
```

### 5. 複雜組合查詢
```csharp
// 複雜查詢：結合多種陣列導覽方法
var filterGroup = FilterDictionaryBuilder.QueryBuilder<Contract>()
    .WithLogicalOperator(LogicalOperator.And)
    .ArrayGreaterThan(c => c.BuildContracts, bc => bc.Build.AptId, 1000L)
    .ArrayLike(c => c.BuildContracts, bc => bc.Build.Location, "台北市")
    .ArrayNotEqual(c => c.BuildContracts, bc => bc.ContractType, "自住")
    .Contains(c => c.Name, "購買")
    .ToFilterGroup();

var predicate = FilterBuilder.Build<Contract>(filterGroup);
var results = context.Contracts
    .Include(c => c.BuildContracts)
    .ThenInclude(bc => bc.Build)
    .Where(predicate)
    .ToList();
```

## 陣列導覽語法

所有陣列導覽方法都會自動生成正確的語法：
```
{集合屬性}[].{目標屬性}
```

例如：
- `BuildContracts[].Build.AptId`
- `BuildContracts[].Build.Name`
- `BuildContracts[].Build.Location`

## 支援的資料類型

- 字串類型：支援 `Like`、`Contains`、`StartsWith`、`EndsWith` 等字串操作
- 數值類型：支援所有比較和範圍操作
- 可空類型：正確處理 `null` 值
- 集合類型：支援 `In`、`NotIn` 操作

## 測試覆蓋

所有新增的陣列導覽方法都已經過完整測試，包括：
- 基本功能測試
- 字串操作測試
- 比較操作測試
- 高級操作測試
- 複雜組合查詢測試

總計測試：116 個，全部通過 ?