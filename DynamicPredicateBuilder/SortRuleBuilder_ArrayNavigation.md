# SortRuleBuilder 陣列導覽屬性排序功能

## 概述

`SortRuleBuilder` 現在支援陣列導覽屬性排序，可以透過 Fluent API 建立針對集合屬性中元素的排序規則。

## 功能特性

### 1. 一般導覽屬性排序

支援多層級的導覽屬性排序，例如：
- `Department.Company.Name`
- `Department.Manager.Bonus`
- `ProjectAssignments.Project.Name`

### 2. 陣列導覽屬性排序

支援針對集合屬性中元素的排序，語法為：
- `CollectionName[].PropertyName`
- `CollectionName[].NavigationProperty.PropertyName`

例如：
- `Employees[].Salary`
- `ProjectAssignments[].HoursPerWeek`
- `ProjectAssignments[].Project.Name`

## 使用方式

### 一般導覽屬性排序

```csharp
// 方法 1: 使用字串語法
var sortRules = new List<SortRule>
{
    new SortRule { Property = "Department.Company.Name", Descending = false }
};

// 方法 2: 使用 Expression 語法（推薦）
var sortRules = SortRuleBuilder.Create<Employee>()
    .Ascending(e => e.Department.Company.Name)
    .ThenByDescending(e => e.Salary)
    .Build();

// 應用排序
var results = context.Employees
    .Include(e => e.Department)
        .ThenInclude(d => d.Company)
    .ApplySort(sortRules)
    .ToList();
```

### 陣列導覽屬性排序

```csharp
// 方法 1: 使用字串語法
var sortRules = new List<SortRule>
{
    new SortRule { Property = "Employees[].Salary", Descending = true }
};

// 方法 2: 使用 Expression 語法（推薦）
var sortRules = SortRuleBuilder.Create<Department>()
    .ArrayDescending(d => d.Employees, e => e.Salary)
    .ArrayThenBy(d => d.Employees, e => e.Name)
    .Build();
```

#### ⚠️ 重要限制

**陣列導覽屬性排序有 Entity Framework Core 的限制：**

EF Core 無法將陣列導覽屬性的排序直接轉譯為 SQL。這意味著：

1. **無法在資料庫端排序**：使用陣列導覽屬性時，查詢會失敗並拋出 `InvalidOperationException`。

2. **需要客戶端評估**：必須先載入資料到記憶體，再進行排序。

3. **建議做法**：
   - 先使用一般屬性過濾和排序以減少資料量
   - 使用 `ToList()` 載入資料到記憶體
   - 在記憶體中進行陣列導覽屬性的排序

```csharp
// ❌ 錯誤：直接在資料庫查詢中使用陣列導覽屬性排序
var sortRules = SortRuleBuilder.Create<Department>()
    .ArrayDescending(d => d.Employees, e => e.Salary)
    .Build();

var results = context.Departments
    .Include(d => d.Employees)
    .ApplySort(sortRules)  // 會拋出 InvalidOperationException
    .ToList();

// ✅ 正確：先載入資料，再在記憶體中排序
var departments = context.Departments
    .Include(d => d.Employees)
    .ToList();  // 先載入到記憶體

// 在記憶體中手動排序
var sorted = departments
    .OrderByDescending(d => d.Employees.Any() ? d.Employees.Max(e => e.Salary) : 0)
    .ToList();
```

## API 方法

### 基本方法

- `Add(string property, bool descending = false)` - 添加排序規則（字串語法）
- `Add<TProperty>(Expression<Func<T, TProperty>> propertyExpression, bool descending = false)` - 添加排序規則（Expression 語法）

### 一般導覽屬性方法

- `Ascending<TProperty>(Expression<Func<T, TProperty>>)` - 升序排序
- `Descending<TProperty>(Expression<Func<T, TProperty>>)` - 降序排序
- `ThenBy<TProperty>(Expression<Func<T, TProperty>>)` - 次要升序排序
- `ThenByDescending<TProperty>(Expression<Func<T, TProperty>>)` - 次要降序排序

### 陣列導覽屬性方法

- `AddArrayNavigation<TCollection, TProperty>(...)` - 添加陣列導覽排序規則（通用方法）
- `ArrayAscending<TCollection, TProperty>(...)` - 陣列導覽升序排序
- `ArrayDescending<TCollection, TProperty>(...)` - 陣列導覽降序排序
- `ArrayThenBy<TCollection, TProperty>(...)` - 陣列導覽次要升序排序
- `ArrayThenByDescending<TCollection, TProperty>(...)` - 陣列導覽次要降序排序

## 完整範例

### 範例 1: 多層級導覽屬性排序

```csharp
// 依公司名稱、部門名稱排序員工
var sortRules = SortRuleBuilder.Create<Employee>()
    .Ascending(e => e.Department.Company.Name)
    .ThenBy(e => e.Department.Name)
    .ThenByDescending(e => e.Salary)
    .Build();

var results = context.Employees
    .Include(e => e.Department)
        .ThenInclude(d => d.Company)
    .ApplySort(sortRules)
    .ToList();
```

### 範例 2: 陣列導覽屬性語法生成

```csharp
// 生成陣列導覽屬性語法（用於記錄或序列化）
var sortRules = SortRuleBuilder.Create<Department>()
    .ArrayDescending(d => d.Employees, e => e.Salary)
    .ArrayThenBy(d => d.Employees, e => e.Name)
    .Build();

// 產生的規則：
// sortRules[0].Property = "Employees[].Salary"
// sortRules[0].Descending = true
// sortRules[1].Property = "Employees[].Name"
// sortRules[1].Descending = false

// 這些規則可以被序列化、傳輸、儲存
var json = JsonSerializer.Serialize(sortRules);
```

### 範例 3: 巢狀陣列導覽屬性

```csharp
// 產生包含巢狀導覽屬性的陣列排序語法
var sortRules = SortRuleBuilder.Create<Employee>()
    .ArrayDescending(e => e.ProjectAssignments, pa => pa.Project.Budget)
    .ThenBy(e => e.Name)
    .Build();

// 產生的規則：
// sortRules[0].Property = "ProjectAssignments[].Project.Budget"
// sortRules[1].Property = "Name"
```

### 範例 4: 混合排序

```csharp
// 混合使用一般屬性、導覽屬性、陣列導覽屬性
var sortRules = SortRuleBuilder.Create<Employee>()
    .Descending(e => e.Department.Company.Name)  // 一般導覽屬性
    .ArrayThenByDescending(e => e.ProjectAssignments, pa => pa.HoursPerWeek)  // 陣列導覽（記錄用）
    .ThenBy(e => e.Salary)  // 一般屬性
    .Build();

// 只有一般屬性和導覽屬性能在 EF Core 查詢中使用
// 陣列導覽屬性規則會被記錄但無法在查詢中執行
```

## 與 FilterDictionaryBuilder 的陣列方法對應

`SortRuleBuilder` 的陣列導覽方法與 `FilterDictionaryBuilder` 的陣列過濾方法設計一致：

| FilterDictionaryBuilder | SortRuleBuilder |
|------------------------|-----------------|
| `ArrayEqual<TCollection, TProperty>()` | `ArrayAscending<TCollection, TProperty>()` |
| `ArrayLike<TCollection, TProperty>()` | `ArrayDescending<TCollection, TProperty>()` |
| `AddCustomArrayNavigation()` | `AddArrayNavigation()` |

兩者都使用相同的語法格式：`CollectionName[].PropertyName`

## 使用場景

### 適用場景

1. **定義排序規則**：建立可序列化的排序規則物件
2. **API 參數**：將排序規則作為 API 請求參數
3. **配置儲存**：將排序偏好儲存到資料庫或配置檔
4. **動態報表**：根據使用者選擇動態建立排序規則
5. **一般導覽屬性排序**：在 EF Core 查詢中排序（單一物件導覽）

### 不適用場景

1. **陣列導覽屬性的 EF Core 查詢排序**：無法在資料庫端執行
2. **需要即時排序的大型資料集**：陣列導覽需要載入到記憶體

## 最佳實踐

### 1. 使用 Expression 語法

```csharp
// ✅ 推薦：類型安全、支援重構
var sortRules = SortRuleBuilder.Create<Employee>()
    .Descending(e => e.Department.Company.Name)
    .Build();

// ❌ 避免：字串容易出錯
var sortRules = new List<SortRule>
{
    new SortRule { Property = "Department.Company.Name", Descending = true }
};
```

### 2. 確保 Include 相關導覽屬性

```csharp
var sortRules = SortRuleBuilder.Create<Employee>()
    .Ascending(e => e.Department.Manager.Name)
    .Build();

// 必須 Include 所有使用的導覽屬性
var results = context.Employees
    .Include(e => e.Department)
        .ThenInclude(d => d.Manager)  // 必須包含
    .ApplySort(sortRules)
    .ToList();
```

### 3. 陣列導覽屬性的處理

```csharp
// 陣列導覽屬性主要用於記錄排序意圖
var sortRules = SortRuleBuilder.Create<Department>()
    .ArrayDescending(d => d.Employees, e => e.Salary)
    .Build();

// 在應用層面處理排序邏輯
var departments = context.Departments
    .Include(d => d.Employees)
    .ToList();

// 根據 sortRules 在記憶體中排序
var sorted = departments
    .OrderByDescending(d => d.Employees.Any() ? d.Employees.Max(e => e.Salary) : 0)
    .ToList();
```

### 4. 混合查詢策略

```csharp
// 先用資料庫查詢過濾和預排序
var query = context.Employees
    .Include(e => e.Department)
    .Include(e => e.ProjectAssignments)
    .Where(e => e.Salary > 50000)
    .OrderBy(e => e.Department.Name);  // 資料庫端排序

// 載入到記憶體
var employees = query.ToList();

// 在記憶體中進行陣列導覽屬性的最終排序
var finalResults = employees
    .OrderByDescending(e => e.ProjectAssignments.Any() 
        ? e.ProjectAssignments.Max(pa => pa.HoursPerWeek) 
        : 0)
    .ToList();
```

## 注意事項

1. **EF Core 翻譯限制**：陣列導覽屬性無法轉譯為 SQL
2. **效能考量**：陣列導覽排序需要載入資料到記憶體
3. **Null 安全**：確保處理可能的 null 導覽屬性
4. **Include 要求**：使用導覽屬性時必須正確 Include
5. **記憶體使用**：大型資料集的陣列導覽排序會消耗較多記憶體

## 總結

`SortRuleBuilder` 的陣列導覽屬性功能主要用於：
- **語法生成**：產生標準化的排序規則語法
- **規則記錄**：序列化、傳輸、儲存排序偏好
- **類型安全**：透過 Expression API 提供編譯時期檢查
- **一致性**：與 FilterDictionaryBuilder 保持相同的陣列語法

實際的陣列導覽屬性排序執行需要在應用層面處理，無法直接在 EF Core 查詢中使用。
