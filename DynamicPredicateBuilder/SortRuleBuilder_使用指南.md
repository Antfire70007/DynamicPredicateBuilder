# SortRuleBuilder 陣列導覽屬性排序 - 使用指南

## 問題說明

**陣列導覽屬性無法在 Entity Framework Core 查詢中直接排序**，因為 EF Core 無法將這種 LINQ 表達式翻譯為 SQL。

## 解決方案

我們提供了三種方法來處理包含陣列導覽屬性的排序需求：

### 方法 1：自動跳過陣列導覽屬性（推薦）

```csharp
// 建立混合排序規則
var sortRules = SortRuleBuilder.Create<Department>()
    .Ascending(d => d.Company.Name)  // 一般導覽屬性 ✓ 可在 EF 中執行
    .ArrayThenByDescending(d => d.Employees, e => e.Salary)  // 陣列導覽 ✗ 無法在 EF 中執行
    .ThenBy(d => d.Name)  // 一般屬性 ✓ 可在 EF 中執行
    .Build();

// 使用 skipArrayNavigation: true 自動跳過陣列導覽屬性
var results = context.Departments
    .Include(d => d.Company)
    .Include(d => d.Employees)
    .ApplySort(sortRules, skipArrayNavigation: true)  // 只使用一般屬性排序
    .ToList();

// 如需要，可在記憶體中進行陣列導覽屬性的排序
results = results
    .OrderByDescending(d => d.Employees.Any() ? d.Employees.Max(e => e.Salary) : 0)
    .ToList();
```

### 方法 2：手動分離排序規則

```csharp
// 建立混合排序規則
var allSortRules = SortRuleBuilder.Create<Department>()
    .Ascending(d => d.Company.Name)
    .ArrayThenByDescending(d => d.Employees, e => e.Salary)
    .ThenBy(d => d.Name)
    .Build();

// 使用輔助方法分離規則
var dbSortRules = allSortRules.GetNonArrayNavigationRules();  // 可在資料庫執行
var arraySortRules = allSortRules.GetArrayNavigationRules();   // 需在記憶體執行

// 步驟 1: 在資料庫端排序
var departments = context.Departments
    .Include(d => d.Company)
    .Include(d => d.Employees)
    .ApplySort(dbSortRules)  // 只用非陣列規則
    .ToList();

// 步驟 2: 在記憶體中應用陣列導覽屬性排序
if (arraySortRules.Any())
{
    departments = departments
        .OrderByDescending(d => d.Employees.Any() ? d.Employees.Max(e => e.Salary) : 0)
        .ToList();
}
```

### 方法 3：只使用一般屬性排序

```csharp
// 不使用陣列導覽屬性，只用一般屬性和導覽屬性
var sortRules = SortRuleBuilder.Create<Employee>()
    .Ascending(e => e.Department.Company.Name)
    .ThenBy(e => e.Department.Name)
    .ThenByDescending(e => e.Salary)
    .Build();

// 可以直接在 EF Core 查詢中執行
var results = context.Employees
    .Include(e => e.Department)
        .ThenInclude(d => d.Company)
    .ApplySort(sortRules)  // 不需要 skipArrayNavigation
    .ToList();
```

## 新增的輔助方法

### ApplySort 擴充方法

```csharp
// 原有方法（遇到陣列導覽屬性會拋出異常）
query.ApplySort(sortRules);

// 新增 skipArrayNavigation 參數
query.ApplySort(sortRules, skipArrayNavigation: true);  // 自動跳過陣列導覽屬性
```

### GetNonArrayNavigationRules

```csharp
// 取得非陣列導覽屬性的排序規則（可在資料庫執行的規則）
var dbRules = sortRules.GetNonArrayNavigationRules();
```

### GetArrayNavigationRules

```csharp
// 取得陣列導覽屬性的排序規則（需在記憶體執行的規則）
var memoryRules = sortRules.GetArrayNavigationRules();
```

## 錯誤處理

如果不使用 `skipArrayNavigation: true` 且排序規則中包含陣列導覽屬性，會拋出清楚的錯誤訊息：

```
InvalidOperationException: 陣列導覽屬性 'Employees[].Salary' 無法在資料庫查詢中排序。
請先使用 ToList() 或 AsEnumerable() 將資料載入記憶體，然後再進行陣列導覽屬性的排序。
或者使用 ApplySort(sortRules, skipArrayNavigation: true) 跳過陣列導覽屬性。
範例：var data = query.ApplySort(sortRules, skipArrayNavigation: true).ToList();
```

## 最佳實踐

### 混合排序策略

```csharp
// 步驟 1: 建立完整的排序需求
var allSortRules = SortRuleBuilder.Create<Department>()
    .Ascending(d => d.Company.Name)  // 資料庫排序
    .ThenBy(d => d.Name)  // 資料庫排序
    .ArrayThenByDescending(d => d.Employees, e => e.Salary)  // 記憶體排序
    .Build();

// 步驟 2: 在資料庫端先過濾和預排序（減少資料量）
var query = context.Departments
    .Include(d => d.Company)
    .Include(d => d.Employees)
    .Where(d => d.Employees.Any())  // 先過濾
    .ApplySort(allSortRules, skipArrayNavigation: true);  // 只用非陣列規則排序

// 步驟 3: 載入到記憶體
var departments = query.ToList();

// 步驟 4: 在記憶體中應用陣列導覽屬性排序
var arraySortRules = allSortRules.GetArrayNavigationRules();
if (arraySortRules.Any())
{
    departments = departments
        .OrderByDescending(d => d.Employees.Max(e => e.Salary))
        .ToList();
}
```

## 支援的排序規則語法

### 一般屬性（可在 EF Core 中執行）
- `"Name"` - 簡單屬性
- `"Department.Name"` - 導覽屬性
- `"Department.Company.Name"` - 多層導覽屬性

### 陣列導覽屬性（需在記憶體中執行）
- `"Employees[].Salary"` - 陣列中的簡單屬性
- `"Employees[].Department.Name"` - 陣列中的導覽屬性
- `"ProjectAssignments[].Project.Name"` - 陣列中的多層導覽屬性

## 識別規則

系統透過檢查屬性路徑中是否包含 `"[]"` 來識別陣列導覽屬性：

```csharp
bool isArrayNavigation = rule.Property.Contains("[]");
```

## 範例總結

| 情境 | 建議方法 | 說明 |
|------|---------|------|
| 只有一般屬性 | 直接使用 `ApplySort()` | 可完全在資料庫端執行 |
| 包含陣列導覽屬性 | 使用 `ApplySort(rules, skipArrayNavigation: true)` | 自動跳過陣列規則 |
| 需要完整排序 | 手動分離規則 | 資料庫 + 記憶體混合排序 |
| API 接收排序參數 | 分離規則方法 | 靈活處理各種排序需求 |

## 效能考量

1. **優先在資料庫端過濾和排序** - 減少載入到記憶體的資料量
2. **避免不必要的陣列導覽排序** - 評估是否真的需要根據集合屬性排序
3. **使用分頁** - 載入大量資料到記憶體前考慮分頁
4. **快取排序結果** - 如果排序結果會重複使用

## 總結

- ✅ **一般屬性和導覽屬性** - 完全支援，可在 EF Core 查詢中執行
- ⚠️ **陣列導覽屬性** - 支援語法生成，但需在記憶體中排序
- 🔧 **新增的工具方法** - 讓混合排序策略更容易實作
- 📝 **清楚的錯誤訊息** - 幫助開發者了解限制和解決方法
