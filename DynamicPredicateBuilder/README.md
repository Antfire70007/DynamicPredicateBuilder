# DynamicPredicateBuilder 使用說明

> **支援環境**：.NET 9; .NET 8; .NET 7  
> **核心特色**：動態過濾、排序、分頁、欄位查詢權限、巢狀/多組條件、NOT 取反、重複條件自動去除。

---

## 目錄

1. [快速開始](#1-快速開始)
2. [欄位查詢權限設定](#2-欄位查詢權限設定)
3. [進階條件組合功能](#3-進階條件組合功能)
4. [支援的運算子](#4-支援的運算子)
5. [支援的資料類型](#5-支援的資料類型)
6. [集合型別欄位查詢支援](#6-集合型別欄位查詢支援)
7. [API 使用範例](#7-api-使用範例)
8. [核心類別與 API 參考](#8-核心類別與-api-參考)
9. [與 jQuery DataTables Server-Side 搭配](#9-與-jquery-datatables-server-side-搭配)
10. [常用 Extension](#10-常用-extension)
11. [FilterDictionaryBuilder - Fluent API 建構器](#11-filterdictionarybuilder---fluent-api-建構器)
12. [SortRuleBuilder - 排序建構器](#12-sortrulebuilder---排序建構器)
13. [單元測試](#13-單元測試)
14. [安裝與使用](#14-安裝與使用)
15. [進階功能與最佳實務](#15-進階功能與-best-practices)
16. [貢獻指南](#16-貢獻指南)
17. [授權條款](#17-授權條款)
18. [版本歷史與更新日誌](#18-版本歷史與更新日誌)
19. [常見問題 (FAQ)](#19-常見問題-faq)
20. [導覽屬性查詢支援](#20-導覽屬性查詢支援)
21. [陣列導覽屬性查詢](#21-陣列導覽屬性查詢支援)

---

## 1. 快速開始

### 1-1. 安裝套件

```bash
dotnet add package DynamicPredicateBuilder
```

### 1-2. 基本使用

```csharp
using DynamicPredicateBuilder;
using DynamicPredicateBuilder.Models;
using DynamicPredicateBuilder.Core;

[HttpPost("people")]
public IActionResult QueryPeople([FromBody] QueryRequest request)
{
    // 從 JSON 解析 FilterGroup
    var filterGroup = FilterGroupFactory.FromJsonElement(request.Filter);
    
    // 建立查詢條件
    var predicate = FilterBuilder.Build<Person>(filterGroup);
    
    // 執行查詢
    var data = _db.People
                 .Where(predicate)
                 .ApplySort(request.Sort)
                 .Skip((request.Page - 1) * request.PageSize)
                 .Take(request.PageSize)
                 .ToList();

    var totalCount = _db.People.Where(predicate).Count();

    return Ok(new QueryResult<Person> 
    { 
        TotalCount = totalCount, 
        Items = data 
    });
}
```

### 1-3. 使用 FilterEngine 簡化操作

```csharp
[HttpPost("people/simple")]
public IActionResult QueryPeopleSimple([FromBody] QueryRequest request)
{
    var result = _db.People.ApplyQuery(request);
    return Ok(result);
}
```

### 1-4. 使用 FilterDictionaryBuilder 程式化建構

```csharp
[HttpPost("people/fluent")]
public IActionResult QueryPeopleWithBuilder()
{
    // 使用 Fluent API 建構過濾條件
    var filterGroup = FilterDictionaryBuilder.QueryBuilder<Person>()
        .WithLogicalOperator(LogicalOperator.And)
        .GreaterThan(x => x.Age, 25)
        .Like(x => x.Name, "John")
        .Compare(LogicalOperator.Or, subRules => subRules
            .Equal(x => x.Address.City, "Taipei")
            .Equal(x => x.Address.City, "Kaohsiung")
        )
        .ToFilterGroup();
    
    var predicate = FilterBuilder.Build<Person>(filterGroup);
    var result = _db.People.Where(predicate).ToList();
    return Ok(result);
}
```

### 1-5. 導覽屬性查詢範例

```csharp
[HttpPost("employees/search")]
public IActionResult SearchEmployees()
{
    // 查詢導覽屬性：員工部門和技能
    var filterGroup = FilterDictionaryBuilder.QueryBuilder<Employee>()
        .WithLogicalOperator(LogicalOperator.And)
        .Equal(x => x.Department.Name, "Engineering")      // 部門名稱
        .Contains(x => x.Profile.Skills, "C#")             // 員工技能
        .GreaterThan(x => x.Salary, 50000)                 // 薪資條件
        .ToFilterGroup();
    
    var predicate = FilterBuilder.Build<Employee>(filterGroup);
    
    // 重要：必須 Include 導覽屬性
    var employees = _db.Employees
        .Include(e => e.Department)
        .Include(e => e.Profile)
        .Where(predicate)
        .ToList();
    
    return Ok(employees);
}
```

---

## 2. 欄位查詢權限設定

### 2-1. 程式碼指定可查詢欄位

```csharp
[HttpPost("people")]
public IActionResult QueryPeople([FromBody] QueryRequest request)
{
    var options = new FilterOptions
    {
        AllowedFields = new HashSet<string> { "Name", "Age", "Address.City" }
    };

    var filterGroup = FilterGroupFactory.FromJsonElement(request.Filter);
    var predicate = FilterBuilder.Build<Person>(filterGroup, options);

    var data = _db.People
                 .Where(predicate)
                 .ApplySort(request.Sort)
                 .ApplyPaging(request.Page, request.PageSize, out var total);

    return Ok(new QueryResult<Person> { TotalCount = total, Items = data });
}
```

### 2-2. 使用 Attribute 自動擷取可查詢欄位

```csharp
public class Person
{
    [Queryable] public string Name { get; set; } = string.Empty;
    [Queryable] public int Age { get; set; }
    public string Password { get; set; } = string.Empty;   // ❌ 未標註，不可查詢
    [Queryable] public Address Address { get; set; } = new();
}

public class Address
{
    [Queryable] public string City { get; set; } = string.Empty;
    public string SecretNote { get; set; } = string.Empty; // ❌ 不可查詢
}

[HttpPost("people")]
public IActionResult QueryPeople([FromBody] QueryRequest request)
{
    var options = new FilterOptions
    {
        AllowedFields = QueryableFieldHelper.GetQueryableFields<Person>()
    };

    var filterGroup = FilterGroupFactory.FromJsonElement(request.Filter);
    var predicate = FilterBuilder.Build<Person>(filterGroup, options);

    // 其餘程式碼同上...
}
```

---

## 3. 進階條件組合功能

### 3-1. 多組條件 (List<FilterGroup>)

`FilterBuilder.Build<T>(IEnumerable<FilterGroup> groups, FilterOptions?)` 允許在「群組與群組」之間再指定 AND / OR （`InterOperator`），例如：

```csharp
var groups = new List<FilterGroup>
{
    // Group 1： !(Name == "Boss") AND Age > 40
    new()
    {
        LogicalOperator = LogicalOperator.And,
        InterOperator   = LogicalOperator.Or,
        Rules =
        [
            new FilterRule { Property = "Name", Operator = FilterOperator.Equal, Value="Boss", IsNegated=true },
            new FilterRule { Property = "Age",  Operator = FilterOperator.GreaterThan, Value=40 }
        ]
    },

    // Group 2：NOT (Status == "Retired")
    new()
    {
        IsNegated       = true,
        LogicalOperator = LogicalOperator.And,
        Rules =
        [
            new FilterRule { Property = "Status", Operator = FilterOperator.Equal, Value="Retired" }
        ]
    }
};

var predicate = FilterBuilder.Build<Person>(groups).Compile();
```

邏輯相當於：`( !(Name == "Boss") && Age > 40 ) OR !(Status == "Retired")`

### 3-2. 巢狀群組 (Nested Group)

`FilterGroup.Rules` 可再放子 `FilterGroup`，自然形成括號優先：

```jsonc
{
  "LogicalOperator": "And",
  "Rules": [
    { "Property": "Age", "Operator": "GreaterThan", "Value": 25 },
    {
      "LogicalOperator": "Or",
      "IsNegated": true,                   
      "Rules": [
        { "Property": "Status", "Operator": "Equal", "Value": "Retired" },
        { "Property": "Status", "Operator": "Equal", "Value": "Fired"   }
      ]
    }
  ]
}
```

等同 SQL：`Age > 25 AND NOT (Status = 'Retired' OR Status = 'Fired')`

### 3-3. NOT 取反

* **單條件**：`FilterRule.IsNegated = true`  
* **整組**：`FilterGroup.IsNegated = true`

---

## 4. 支援的運算子

### 4-1. 基本運算子

| 運算子 | 說明 | 範例 |
|-------|------|------|
| `Equal` | 等於 | `Name == "John"` |
| `NotEqual` | 不等於 | `Name != "John"` |
| `GreaterThan` | 大於 | `Age > 30` |
| `GreaterThanOrEqual` | 大於等於 | `Age >= 30` |
| `LessThan` | 小於 | `Age < 30` |
| `LessThanOrEqual` | 小於等於 | `Age <= 30` |

### 4-2. 字串運算子

| 運算子 | 說明 | 範例 |
|-------|------|------|
| `Contains` | 包含 | `Name.Contains("John")` |
| `NotContains` | 不包含 | `!Name.Contains("John")` |
| `StartsWith` | 開始於 | `Name.StartsWith("J")` |
| `EndsWith` | 結束於 | `Name.EndsWith("n")` |
| `Like` | SQL LIKE 模式 | `Name LIKE '%John%'` |
| `NotLike` | SQL NOT LIKE 模式 | `Name NOT LIKE '%Admin%'` |

### 4-3. 集合運算子

| 運算子 | 說明 | 範例 |
|-------|------|------|
| `In` | 值在集合中 | `Status IN ('Active', 'Pending')` |
| `NotIn` | 值不在集合中 | `Status NOT IN ('Disabled', 'Banned')` |
| `Any` | 集合中任一元素符合條件或集合非空 | `Tags.Any()` 或 `Tags.Any(t => t == "VIP")` |
| `NotAny` | 集合中沒有元素符合條件或集合為空 | `!Tags.Any()` 或 `!Tags.Any(t => t == "VIP")` |

### 4-4. 範圍運算子

| 運算子 | 說明 | 範例 |
|-------|------|------|
| `Between` | 在範圍內 | `Age BETWEEN 20 AND 30` |
| `NotBetween` | 不在範圍內 | `Age NOT BETWEEN 20 AND 30` |

### 4-5. 新增運算子使用範例

```csharp
// NotLike：檢查屬性值不符合指定的模式
new FilterRule { Property = "Name", Operator = FilterOperator.NotLike, Value = "%Admin%" }

// Between：檢查屬性值是否在指定範圍內
new FilterRule { Property = "Age", Operator = FilterOperator.Between, Value = new[] { 20, 30 } }

// NotBetween：檢查屬性值是否不在指定範圍內
new FilterRule { Property = "Age", Operator = FilterOperator.NotBetween, Value = new[] { 20, 30 } }

// Any：檢查集合屬性是否有任何元素（不提供值時）
new FilterRule { Property = "Tags", Operator = FilterOperator.Any, Value = null }

// Any：檢查集合屬性是否包含指定值（提供值時）
new FilterRule { Property = "Tags", Operator = FilterOperator.Any, Value = "VIP" }

// NotAny：檢查集合屬性是否沒有任何元素（不提供值時）
new FilterRule { Property = "Tags", Operator = FilterOperator.NotAny, Value = null }

// NotAny：檢查集合屬性是否不包含指定值（提供值時）
new FilterRule { Property = "Tags", Operator = FilterOperator.NotAny, Value = "Banned" }
```

### 4-6. Any 和 NotAny 操作符詳細說明

`Any` 和 `NotAny` 操作符針對集合屬性提供了靈活的查詢功能：

**Any 操作符：**
- 當 `Value` 為 `null` 時：檢查集合是否有任何元素（相當於 `collection.Any()`）
- 當 `Value` 有值時：檢查集合是否包含該值（相當於 `collection.Any(x => x == value)`）

**NotAny 操作符：**
- 當 `Value` 為 `null` 時：檢查集合是否沒有任何元素（相當於 `!collection.Any()`）
- 當 `Value` 有值時：檢查集合是否不包含該值（相當於 `!collection.Any(x => x == value)`）

**使用範例：**
```csharp
// 使用 FilterDictionaryBuilder
var filterGroup = FilterDictionaryBuilder.QueryBuilder<User>()
    .Any(x => x.Tags)                    // 檢查用戶是否有任何標籤
    .Any(x => x.Categories, "Premium")   // 檢查用戶是否有 "Premium" 分類
    .NotAny(x => x.Flags)                // 檢查用戶是否沒有任何旗標
    .NotAny(x => x.Labels, "Banned")     // 檢查用戶是否沒有 "Banned" 標籤
    .ToFilterGroup();

// 直接使用 FilterRule
var rules = new []
{
    new FilterRule { Property = "Tags", Operator = FilterOperator.Any, Value = null },      // 有任何標籤
    new FilterRule { Property = "Categories", Operator = FilterOperator.Any, Value = "VIP" }, // 包含 VIP 分類
    new FilterRule { Property = "Blacklist", Operator = FilterOperator.NotAny, Value = null }, // 黑名單為空
    new FilterRule { Property = "Restrictions", Operator = FilterOperator.NotAny, Value = "Active" } // 不包含活躍限制
};
```

---

## 5. 支援的資料類型

### 5-1. 基本數值類型

| 類型 | 描述 | 範例 |
|------|------|------|
| `int`, `int?` | 32位整數 | `Age = 25` |
| `long`, `long?` | 64位整數 | `Id = 1234567890L` |
| `decimal`, `decimal?` | 高精度十進位 | `Salary = 50000.50m` |
| `double`, `double?` | 雙精度浮點數 | `Score = 95.5` |
| `float`, `float?` | 單精度浮點數 | `Rate = 3.14f` |

### 5-2. 日期時間類型

| 類型 | 描述 | 範例 |
|------|------|------|
| `DateTime`, `DateTime?` | 日期時間 | `CreatedDate = DateTime.Now` |
| `DateOnly`, `DateOnly?` | 僅日期 (.NET 6+) | `BirthDate = DateOnly.FromDateTime(DateTime.Now)` |
| `TimeOnly`, `TimeOnly?` | 僅時間 (.NET 6+) | `WorkTime = TimeOnly.FromDateTime(DateTime.Now)` |

### 5-3. 字串與字元類型

| 類型 | 描述 | 範例 |
|------|------|------|
| `string` | 字串（可為 null） | `Name = "John"` |
| `char`, `char?` | 單一字元 | `Gender = 'M'` |

### 5-4. 布林類型

| 類型 | 描述 | 範例 |
|------|------|------|
| `bool`, `bool?` | 布林值 | `IsActive = true` |

### 5-5. 其他特殊類型

| 類型 | 描述 | 範例 |
|------|------|------|
| `Guid`, `Guid?` | 全域唯一識別碼 | `UserId = Guid.NewGuid()` |
| `Enum` | 列舉類型 | `Status = UserStatus.Active` |

### 5-6. Nullable 類型特殊處理

DynamicPredicateBuilder 對所有 nullable 類型提供完整支援：

```csharp
// Nullable decimal 查詢範例
public class Product
{
    public string Name { get; set; }
    public decimal? Price { get; set; }  // 可為 null 的價格
    public decimal? Discount { get; set; } // 可為 null 的折扣
}

// 查詢有價格的商品
new FilterRule { Property = "Price", Operator = FilterOperator.GreaterThan, Value = 0 }

// 查詢沒有設定價格的商品
new FilterRule { Property = "Price", Operator = FilterOperator.Equal, Value = null }

// 查詢價格在範圍內的商品
new FilterRule { Property = "Price", Operator = FilterOperator.Between, Value = new[] { 100m, 1000m } }

// 查詢價格在指定清單中的商品
new FilterRule { Property = "Price", Operator = FilterOperator.In, Value = new[] { 99.9m, 199.9m, 299.9m } }
```

### 5-7. 類型轉換與安全性

FilterBuilder 內建智慧型類型轉換機制：

- **安全轉換**：使用 `TryParse` 方法避免轉換例外
- **Nullable 支援**：正確處理 null 值比較
- **精度保持**：decimal 類型保持完整精度
- **自動推導**：根據屬性類型自動選擇最佳轉換策略

```csharp
// 即使前端傳送字串，也會自動轉換為正確的數值類型
new FilterRule { Property = "Salary", Operator = FilterOperator.GreaterThan, Value = "50000" }
// 內部會自動轉換為 decimal 50000

// 支援科學記號
new FilterRule { Property = "BigNumber", Operator = FilterOperator.Equal, Value = "1.5E+10" }
```

---

## 6. 集合型別欄位查詢支援

### 6-1. 欄位路徑格式
- 支援巢狀集合屬性查詢，例如：`Orders[].Items[].Name`
- 欄位白名單自動展開所有集合層級，格式為 `集合屬性名[].屬性名`，可多層巢狀

### 6-2. FilterRule 實例
```csharp
new FilterRule
{
    Property = "Orders[].Items[].Name",
    Operator = FilterOperator.In,
    Value = new[] { "ItemA", "ItemB" }
}
```
這會產生：`Orders.SelectMany(o => o.Items).Select(i => i.Name).Any(name => new[] { "ItemA", "ItemB" }.Contains(name))`

### 6-3. 運算子支援
- **In**：查詢集合屬性是否包含指定值（多值）
- **Any**：查詢集合屬性是否有任一元素符合條件
- **Contains**：查詢集合屬性是否包含單一值
- **Equal**：僅用於非集合屬性
- **Like/NotLike**：可用於字串型別欄位

> **注意**：查詢集合屬性時，請使用 `In`、`Any`、`Contains`，不要用 `Equal` 比較集合本身。

### 6-4. 範例：查詢集合屬性底下的欄位
```csharp
// 查詢 User 的 Orders 集合底下的 OrderId 是否包含 123
new FilterRule
{
    Property = "Orders[].OrderId",
    Operator = FilterOperator.In,
    Value = new[] { 123 }
}

// 查詢 User 的 Orders 集合底下的 Items 集合底下的 Name 是否包含 "VIP"
new FilterRule
{
    Property = "Orders[].Items[].Name",
    Operator = FilterOperator.In,
    Value = new[] { "VIP" }
}
```

---

## 7. API 使用範例

### 7-1. Request 範例（單組簡易）

```jsonc
{
  "Filter": {
    "LogicalOperator": "And",
    "Rules": [
      { "Property": "Age",          "Operator": "GreaterThanOrEqual", "Value": 25        },
      { "Property": "Address.City", "Operator": "Equal",              "Value": "Taipei" }
    ]
  },
  "Sort": [
    { "Property": "Name", "Descending": false },
    { "Property": "Age",  "Descending": true  }
  ],
  "Page": 1,
  "PageSize": 5
}
```

### 7-2. Request 範例（多組 + NOT + 巢狀）

```jsonc
{
  "FilterGroups": [
    {
      "LogicalOperator": "And",
      "InterOperator":   "Or",
      "Rules": [
        { "Property": "Name", "Operator": "Equal", "Value": "Boss", "IsNegated": true },
        { "Property": "Age",  "Operator": "GreaterThan", "Value": 40 }
      ]
    },
    {
      "IsNegated": true,
      "LogicalOperator": "And",
      "Rules": [
        { "Property": "Status", "Operator": "Equal", "Value": "Retired" }
      ]
    }
  ],
  "Sort": [],
  "Page": 1,
  "PageSize": 20
}
```

> Controller 收到 `FilterGroups` 時，呼叫 `FilterBuilder.Build<Person>(request.FilterGroups, options)`。

### 7-3. Request 範例（Nullable Decimal 查詢）

```jsonc
{
  "Filter": {
    "LogicalOperator": "And",
    "Rules": [
      { "Property": "Salary", "Operator": "GreaterThan", "Value": 50000.00 },
      { "Property": "Bonus",  "Operator": "Between",     "Value": [1000, 5000] },
      { "Property": "Commission", "Operator": "Equal",   "Value": null }
    ]
  }
}
```

### 7-4. Response 範例

```jsonc
{
  "totalCount": 45,
  "items": [
    { 
      "name": "Alice", 
      "age": 30, 
      "salary": 75000.50,
      "bonus": 3000.00,
      "commission": null,
      "address": { "city": "Taipei" } 
    }
  ]
}
```

---

## 8. 核心類別與 API 參考

### 8-1. FilterBuilder
`FilterBuilder` 是專案的核心類別，負責生成查詢條件的表達式。

#### 主要方法
- **`Build<T>(FilterGroup group, FilterOptions?)`**：生成單組條件的查詢表達式
- **`Build<T>(IEnumerable<FilterGroup> groups, FilterOptions?)`**：生成多組條件的查詢表達式

```csharp
// 單組條件
var predicate = FilterBuilder.Build<Person>(filterGroup);

// 多組條件
var predicate = FilterBuilder.Build<Person>(filterGroups, options);
```

### 8-2. FilterEngine
`FilterEngine` 提供便利的靜態方法用於快速建立查詢條件。

#### 主要方法
- **`FromJson<T>(string json)`**：從 JSON 字串建立查詢表達式
- **`FromDictionary<T>(Dictionary<string, object> dict)`**：從字典建立查詢表達式

```csharp
// 從 JSON 建立
var predicate = FilterEngine.FromJson<Person>(jsonString);

// 從字典建立
var predicate = FilterEngine.FromDictionary<Person>(dictionary);
```

### 8-3. FilterEngineExtensions
提供 IQueryable 的擴展方法。

#### 主要方法
- **`ApplyFilterJson<T>(JsonElement filterJson, List<SortRule> sortRules)`**：套用 JSON 過濾條件
- **`ApplySort<T>(List<SortRule> sortRules)`**：套用排序規則
- **`ApplyQuery<T>(QueryRequest request)`**：套用完整查詢請求

```csharp
// 套用完整查詢
var result = _db.People.ApplyQuery(queryRequest);

// 僅套用過濾條件
var query = _db.People.ApplyFilterJson(filterJson, sortRules);
```

### 8-4. QueryableFieldHelper
提供欄位白名單的功能，確保查詢僅限於允許的欄位。

#### 主要方法
- **`GetQueryableFields<T>()`**：解析 `[Queryable]` 標籤產生欄位白名單
- **`GetAllowedFields<T>()`**：取得允許查詢的欄位集合

```csharp
var allowedFields = QueryableFieldHelper.GetQueryableFields<Person>();
var options = new FilterOptions { AllowedFields = allowedFields };
```

### 8-5. FilterGroupFactory
用於從不同來源建立 FilterGroup 物件。

#### 主要方法
- **`FromDictionary(Dictionary<string, object> dict)`**：從字典建立 FilterGroup
- **`FromJsonElement(JsonElement json)`**：從 JsonElement 建立 FilterGroup

```csharp
var filterGroup = FilterGroupFactory.FromDictionary(dictionary);
var filterGroup = FilterGroupFactory.FromJsonElement(jsonElement);
```

### 8-6. 核心資料模型

#### QueryRequest
```csharp
public class QueryRequest
{
    public JsonElement Filter { get; set; } 
    public List<SortRule> Sort { get; set; } = new List<SortRule>();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
```

#### QueryResult
```csharp
public class QueryResult<T>
{
    public int TotalCount { get; set; }
    public List<T> Items { get; set; } = new List<T>();
}
```

#### FilterGroup
```csharp
public class FilterGroup
{
    public LogicalOperator LogicalOperator { get; set; } = LogicalOperator.And;
    public LogicalOperator InterOperator { get; set; } = LogicalOperator.And;
    public bool IsNegated { get; set; } = false;
    public List<object> Rules { get; set; } = new List<object>();
}
```

#### FilterRule
```csharp
public class FilterRule
{
    public string Property { get; set; } = string.Empty;
    public FilterOperator Operator { get; set; } = FilterOperator.Equal;
    public object Value { get; set; } = null!;
    public bool IsNegated { get; set; } = false;
}
```

---

## 9. 與 jQuery DataTables Server-Side 搭配

以下示範 **DataTables 1.13+** 於前端傳送分頁、排序、全域搜尋與欄位搜尋，後端再組成 `FilterGroup`：

### 9-1. 前端 JavaScript

```html
<table id="peopleTable" class="display" style="width:100%">
    <thead>
        <tr>
            <th>Name</th>
            <th>Age</th>
            <th>City</th>
        </tr>
        <tr><!-- 欄位篩選列 -->
            <th><input type="text" placeholder="Name" class="col-search" data-col="0"></th>
            <th><input type="number" placeholder=">= Age" class="col-search" data-col="1"></th>
            <th><input type="text" placeholder="City" class="col-search" data-col="2"></th>
        </tr>
    </thead>
</table>

<script>
$(function () {
    const table = $('#peopleTable').DataTable({
        serverSide: true,
        processing: true,
        searchDelay: 600,     // 降低後端壓力
        ajax: {
            url: '/api/people/datatable',
            type: 'POST',
            contentType: 'application/json',
            data: function (d) {
                // 將 DataTables 內建物件轉成您 API 需要的格式
                return JSON.stringify({
                    draw: d.draw,
                    page: Math.floor(d.start / d.length) + 1,
                    pageSize: d.length,
                    sort: d.order.map(o => ({
                        property: d.columns[o.column].data,
                        descending: o.dir === 'desc'
                    })),
                    filterGroups: buildFilterGroups(d)   // 🔑 自訂函式
                });
            }
        },
        columns: [
            { data: 'name' },
            { data: 'age' },
            { data: 'address.city', render: data => data } // 巢狀欄位
        ]
    });

    // 個別欄位即時搜尋
    $('.col-search').on('keyup change', function () {
        table.column($(this).data('col')).search(this.value).draw();
    });

    // 把 DataTables 搜尋條件轉成 FilterGroup
    function buildFilterGroups(dt) {
        const groups = [];

        // 全域搜尋（LIKE %keyword%）
        if (dt.search && dt.search.value) {
            groups.push({
                logicalOperator: 'Or',
                rules: dt.columns
                    .filter(c => c.searchable)
                    .map(c => ({
                        property: c.data,
                        operator: 'Like',
                        value: dt.search.value
                    }))
            });
        }

        // 欄位個別搜尋
        dt.columns.forEach(c => {
            if (c.search && c.search.value) {
                groups.push({
                    logicalOperator: 'And',
                    rules: [{
                        property: c.data,
                        operator: 'Like',
                        value: c.search.value
                    }]
                });
            }
        });

        return groups;
    }
});
</script>
```

### 9-2. 後端 Controller

```csharp
[HttpPost("datatable")]
public IActionResult DataTableQuery([FromBody] DataTableRequest<QueryRequest> req)
{
    // req.Payload 即前端轉好的 QueryRequest
    var options = new FilterOptions
    {
        AllowedFields = QueryableFieldHelper.GetQueryableFields<Person>()
    };

    // 多組群組 Build
    var predicate = FilterBuilder.Build<Person>(req.Payload.FilterGroups, options);

    var query = _db.People.Where(predicate);

    // 排序、分頁
    query = query.ApplySort(req.Payload.Sort);
    var items = query
                .Skip((req.Payload.Page - 1) * req.Payload.PageSize)
                .Take(req.Payload.PageSize)
                .ToList();

    return Ok(new
    {
        draw = req.Payload.Draw,
        recordsTotal = _db.People.Count(),
        recordsFiltered = query.Count(),
        data = items
    });
}

// 用來包 DataTables 固定參數 + 您自定義的 QueryRequest
public class DataTableRequest<T>
{
    public int Draw { get; set; }
    public T Payload { get; set; } = default!;
}
```

---

## 10. 常用 Extension

| 方法 | 說明 |
|---|---|
| `ApplySort(this IQueryable<T>, IEnumerable<SortRule>)` | 依多欄位排序 (動態排序)。 |
| `ApplyPaging(this IQueryable<T>, int page, int size, out int total)` | 取得總筆數並套用 Skip/Take。 |
| `ApplyQuery(this IQueryable<T>, QueryRequest request)` | 套用完整查詢請求（過濾、排序、分頁）。 |
| `ApplyFilterJson(this IQueryable<T>, JsonElement, List<SortRule>?)` | 套用 JSON 格式過濾條件。 |
| `FilterGroupFactory.FromDictionary(IDictionary<string, object>)` | 把前端 JSON 轉成 `FilterGroup` 物件。 |
| `QueryableFieldHelper.GetQueryableFields<T>()` | 解析 `[Queryable]` 標籤產生欄位白名單。 |

---

## 11. FilterDictionaryBuilder - Fluent API 建構器

`FilterDictionaryBuilder` 提供 Fluent API 方式來程式化建構過濾條件，支援 Expression 語法和類型安全的屬性參考。

### 11-1. 基本使用

```csharp
// 基本條件組合
var filterGroup = FilterDictionaryBuilder.QueryBuilder<Person>()
    .WithLogicalOperator(LogicalOperator.And)
    .Equal(x => x.Name, "John")
    .GreaterThan(x => x.Age, 25)
    .ToFilterGroup();
```

### 11-2. 支援的快捷方法

#### 所有 FilterOperator 的快捷方法

```csharp
var builder = FilterDictionaryBuilder.QueryBuilder<Person>()
    // 基本比較
    .Equal(x => x.Name, "John")
    .NotEqual(x => x.Status, "Disabled")
    .GreaterThan(x => x.Age, 18)
    .GreaterThanOrEqual(x => x.Salary, 30000)
    .LessThan(x => x.Age, 65)
    .LessThanOrEqual(x => x.Experience, 10)
    
    // 字串操作
    .Contains(x => x.Description, "keyword")
    .NotContains(x => x.Description, "spam")
    .StartsWith(x => x.Phone, "02")
    .EndsWith(x => x.Email, "@company.com")
    .Like(x => x.Name, "%John%")
    .NotLike(x => x.Name, "%Admin%")
    
    // 集合操作
    .In(x => x.Department, new[] { "IT", "HR", "Finance" })
    .NotIn(x => x.Status, new[] { "Disabled", "Suspended" })
    .Any(x => x.Tags, "VIP")
    .NotAny(x => x.Tags, "Blacklisted")
    
    // 範圍操作
    .Between(x => x.Salary, 30000, 80000)
    .NotBetween(x => x.Age, 16, 18);
```

### 11-3. 巢狀條件群組

```csharp
var filterGroup = FilterDictionaryBuilder.QueryBuilder<Person>()
    .WithLogicalOperator(LogicalOperator.And)
    .GreaterThan(x => x.Age, 25)
    
    // 巢狀 OR 群組
    .Compare(LogicalOperator.Or, subRules => subRules
        .Equal(x => x.Department, "IT")
        .Equal(x => x.Department, "Engineering")
        .Equal(x => x.Department, "Research")
    )
    
    // 巢狀 AND 群組（可加否定）
    .Compare(LogicalOperator.And, statusRules => statusRules
        .NotEqual(x => x.Status, "Retired")
        .NotEqual(x => x.Status, "Terminated"),
        isNegated: false)
    
    .ToFilterGroup();
```

### 11-4. 屬性對屬性比較

```csharp
var filterGroup = FilterDictionaryBuilder.QueryBuilder<Employee>()
    // 比較兩個屬性：開始日期 < 結束日期
    .AddPropertyComparison(x => x.StartDate, FilterOperator.LessThan, x => x.EndDate)
    
    // 薪資大於獎金
    .AddPropertyComparison(x => x.Salary, FilterOperator.GreaterThan, x => x.Bonus)
    
    .ToFilterGroup();
```

### 11-5. 混合 Expression 和字串語法

```csharp
var filterGroup = FilterDictionaryBuilder.QueryBuilder<Person>()
    // Expression 語法 - 類型安全
    .Equal(x => x.Name, "John")
    .GreaterThan(x => x.Age, 25)
    
    // 字串語法 - 適用於動態屬性
    .Equal("DynamicProperty", "value")
    .Contains(nameof(Person.Description), "keyword")
    
    .ToFilterGroup();
```

### 11-6. 多種建立方式

```csharp
// 方式 1: 靜態工廠方法
var builder1 = FilterDictionaryBuilder.QueryBuilder<Person>();

// 方式 2: 泛型靜態方法
var builder2 = FilterDictionaryBuilder.Create<Person>();

// 方式 3: 直接實例化
var builder3 = FilterDictionaryBuilder<Person>.Create();
```

### 11-7. 輸出格式

```csharp
var builder = FilterDictionaryBuilder.QueryBuilder<Person>()
    .Equal(x => x.Name, "John")
    .GreaterThan(x => x.Age, 25);

// 輸出為字典
Dictionary<string, object> dict = builder.Build();

// 輸出為 FilterGroup
FilterGroup group = builder.ToFilterGroup();

// 隱式轉換
Dictionary<string, object> dictImplicit = builder;
FilterGroup groupImplicit = builder;
```

---

## 12. SortRuleBuilder - 排序建構器

`SortRuleBuilder` 提供 Fluent API 方式來建構排序規則，支援多層排序和 Expression 語法。

### 12-1. 基本排序

```csharp
// 簡單排序
var sortRules = SortRuleBuilder.SortBuilder<Person>()
    .Ascending(x => x.Name)
    .Descending(x => x.CreatedDate)
    .Build();
```

### 12-2. 多層排序

```csharp
// 主要排序和次要排序
var sortRules = SortRuleBuilder.SortBuilder<Employee>()
    .Ascending(x => x.Department)      // 先按部門升序
    .ThenBy(x => x.Position)           // 同部門按職位升序
    .ThenByDescending(x => x.Salary)   // 同職位按薪資降序
    .ThenBy(x => x.Name)               // 最後按姓名升序
    .Build();
```

### 12-3. 支援的排序方法

```csharp
var builder = SortRuleBuilder.SortBuilder<Person>()
    // 基本排序方法
    .Add(x => x.Property, descending: false)     // 通用方法
    .Ascending(x => x.Name)                      // 升序
    .Descending(x => x.Age)                      // 降序
    
    // 次要排序方法
    .ThenBy(x => x.Email)                        // 次要升序
    .ThenByDescending(x => x.CreatedDate);       // 次要降序
```

### 12-4. 字串語法支援

```csharp
var sortRules = SortRuleBuilder.SortBuilder<Person>()
    // Expression 語法
    .Ascending(x => x.Name)
    
    // 字串語法
    .Descending(nameof(Person.CreatedDate))
    .Add("DynamicProperty", descending: true)
    
    .Build();
```

### 12-5. 與查詢系統整合

```csharp
// 建構過濾和排序條件
var filterGroup = FilterDictionaryBuilder.QueryBuilder<Employee>()
    .GreaterThan(x => x.Salary, 50000)
    .Equal(x => x.Department, "IT")
    .ToFilterGroup();

var sortRules = SortRuleBuilder.SortBuilder<Employee>()
    .Descending(x => x.Salary)
    .ThenBy(x => x.Name)
    .Build();

// 建立完整查詢請求
var request = new QueryRequest
{
    Filter = JsonSerializer.SerializeToElement(filterGroup),
    Sort = sortRules,
    Page = 1,
    PageSize = 20
};

// 執行查詢
var result = _context.Employees.ApplyQuery(request);
```

### 12-6. 輸出格式

```csharp
var builder = SortRuleBuilder.SortBuilder<Person>()
    .Ascending(x => x.Name)
    .Descending(x => x.Age);

// 輸出為列表
List<SortRule> rules = builder.Build();

// 隱式轉換
List<SortRule> rulesList = builder;
SortRule[] rulesArray = builder;
```

---

## 13. 單元測試

`DynamicPredicate.Tests` 專案示範：

* **FilterBuilderTests**：Equal、GreaterThan、NOT、巢狀、多組 AND/OR 等核心功能測試
* **FilterDictionaryBuilderTests**：Fluent API 建構器測試
* **SortRuleBuilderTests**：排序建構器測試
* **Nullable Decimal Tests**：完整的 decimal? 類型測試案例
* **測試資料模型**：`User.cs` 提供測試用的資料結構

### 執行測試

```bash
dotnet test
```

### 測試範例

```csharp
[Fact]
public void BuildPredicate_WithMultipleGroups_ShouldCombineCorrectly()
{
    var groups = new List<FilterGroup>
    {
        // Group 1： !(Name == "Boss") AND Age > 40
        new()
        {
            LogicalOperator = LogicalOperator.And,
            InterOperator = LogicalOperator.Or,
            Rules =
            [
                new FilterRule { Property = "Name", Operator = FilterOperator.Equal, Value = "Boss", IsNegated = true },
                new FilterRule { Property = "Age", Operator = FilterOperator.GreaterThan, Value = 40 }
            ]
        },

        // Group 2：NOT (Status == "Retired")
        new()
        {
            IsNegated = true,
            LogicalOperator = LogicalOperator.And,
            Rules =
            [
                new FilterRule { Property = "Status", Operator = FilterOperator.Equal, Value = "Retired" }
            ]
        }
    };

    var predicate = FilterBuilder.Build<User>(groups).Compile();

    predicate(new User { Name = "Snake", Age = 20 }).Should().BeTrue();   // Group1 滿足
    predicate(new User { Name = "Otacon", Age = 50 }).Should().BeTrue(); // Group2 滿足
    predicate(new User { Name = "Otacon", Age = 30 }).Should().BeFalse();
}

[Fact]
public void FilterDictionaryBuilder_WithExpressionSyntax_ShouldWork()
{
    var filterGroup = FilterDictionaryBuilder.QueryBuilder<User>()
        .WithLogicalOperator(LogicalOperator.And)
        .Equal(x => x.Name, "John")
        .GreaterThan(x => x.Age, 25)
        .Compare(LogicalOperator.Or, subRules => subRules
            .Equal(x => x.Department, "IT")
            .Equal(x => x.Department, "HR")
            .Equal(x => x.Department, "Research")
        )
        .ToFilterGroup();

    var predicate = FilterBuilder.Build<User>(filterGroup).Compile();
    
    // 測試條件
    predicate(new User { Name = "John", Age = 30, Department = "IT" }).Should().BeTrue();
    predicate(new User { Name = "John", Age = 30, Department = "Finance" }).Should().BeFalse();
}
```

---

## 14. 安裝與使用

### 14-1. 系統需求
- .NET 7.0 或更高版本
- .NET 8.0 或更高版本  
- .NET 9.0 或更高版本

### 14-2. NuGet 安裝

```bash
dotnet add package DynamicPredicateBuilder
```

### 14-3. 基本設定

```csharp
using DynamicPredicateBuilder;
using DynamicPredicateBuilder.Models;
using DynamicPredicateBuilder.Core;

// 在 Program.cs 或 Startup.cs 中註冊服務（可選）
services.AddScoped<FilterOptions>();
```

### 14-4. 在控制器中使用

```csharp
[ApiController]
[Route("api/[controller]")]
public class PeopleController : ControllerBase
{
    private readonly IDbContext _context;

    public PeopleController(IDbContext context)
    {
        _context = context;
    }

    [HttpPost("query")]
    public IActionResult Query([FromBody] QueryRequest request)
    {
        var result = _context.People.ApplyQuery(request);
        return Ok(result);
    }

    [HttpPost("query-builder")]
    public IActionResult QueryWithBuilder()
    {
        // 使用 FilterDictionaryBuilder
        var filterGroup = FilterDictionaryBuilder.QueryBuilder<Person>()
            .WithLogicalOperator(LogicalOperator.And)
            .GreaterThan(x => x.Age, 18)
            .Like(x => x.Name, "John")
            .ToFilterGroup();

        // 使用 SortRuleBuilder
        var sortRules = SortRuleBuilder.SortBuilder<Person>()
            .Descending(x => x.CreatedDate)
            .ThenBy(x => x.Name)
            .Build();

        var predicate = FilterBuilder.Build<Person>(filterGroup);
        var result = _context.People
            .Where(predicate)
            .ApplySort(sortRules)
            .ToList();

        return Ok(result);
    }
}
```

---

## 15. 進階功能與最佳實務

### 15-1. 效能最佳化
- 使用 `AsNoTracking()` 提升查詢效能
- 在經常查詢的欄位加上索引
- 適當使用 `IQueryable` 延遲執行特性

```csharp
var result = _context.People
    .AsNoTracking()
    .ApplyQuery(request);
```

### 15-2. 安全性考量
- 始終使用 `FilterOptions.AllowedFields` 限制可查詢欄位
- 驗證輸入資料的型別和範圍
- 避免暴露敏感資料欄位

### 15-3. 錯誤處理
```csharp
try
{
    var filterGroup = FilterGroupFactory.FromJsonElement(request.Filter);
    var predicate = FilterBuilder.Build<Person>(filterGroup, options);
    // ... 執行查詢
}
catch (ArgumentException ex)
{
    return BadRequest($"Invalid filter: {ex.Message}");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Query execution failed");
    return StatusCode(500, "Internal server error");
}
```

### 15-4. 陣列導覽屬性效能最佳化
```csharp
// 避免在迴圈中執行陣列導覽查詢
// ❌ 錯誤做法
foreach (var contractId in contractIds)
{
    var contract = _context.Contracts
        .Include(c => c.BuildContracts)
        .ThenInclude(bc => bc.Build)
        .FirstOrDefault(c => c.Id == contractId);
}

// ✅ 正確做法：批次查詢
var contracts = _context.Contracts
    .Include(c => c.BuildContracts)
    .ThenInclude(bc => bc.Build)
    .Where(c => contractIds.Contains(c.Id))
    .ToList();
```

### 15-5. 記憶體使用優化
```csharp
// 大量資料處理時使用串流處理
await foreach (var contract in _context.Contracts
    .Where(predicate)
    .AsAsyncEnumerable())
{
    // 逐筆處理，減少記憶體使用
    await ProcessContractAsync(contract);
}
```

### 15-6. 查詢快取策略
```csharp
// 使用 IMemoryCache 快取經常查詢的結果
public async Task<List<Contract>> GetCachedContractsAsync(FilterGroup filterGroup)
{
    var cacheKey = $"contracts_{JsonConvert.SerializeObject(filterGroup)}";
    
    if (!_cache.TryGetValue(cacheKey, out List<Contract> contracts))
    {
        var predicate = FilterBuilder.Build<Contract>(filterGroup);
        contracts = await _context.Contracts
            .Include(c => c.BuildContracts)
            .ThenInclude(bc => bc.Build)
            .Where(predicate)
            .ToListAsync();
            
        _cache.Set(cacheKey, contracts, TimeSpan.FromMinutes(10));
    }
    
    return contracts;
}



```

### 15-7. 陣列導覽屬性安全性
```csharp
// 限制陣列導覽屬性的查詢權限
var options = new FilterOptions
{
    AllowedFields = new HashSet<string> 
    { 
        "Name", 
        "CreatedDate",
        "BuildContracts[].Amount",          // 允許查詢合約金額
        "BuildContracts[].Build.Name",      // 允許查詢建案名稱
        "BuildContracts[].Build.Location"   // 允許查詢建案位置
        // "BuildContracts[].Build.SecretInfo" // 敏感資訊不允許查詢
    }
};

// 驗證查詢深度，防止過深的陣列導覽
public static bool ValidateNavigationDepth(string property, int maxDepth = 3)
{
    var depth = property.Count(c => c == '[');
    return depth <= maxDepth;
}
```

### 15-8. 輸入驗證與清理
```csharp
// 驗證和清理陣列導覽屬性輸入
public static string SanitizeNavigationProperty(string property)
{
    // 移除不安全的字元
    var sanitized = Regex.Replace(property, @"[^\w\[\]\.]", "");
    
    // 驗證格式
    var pattern = @"^[a-zA-Z_][a-zA-Z0-9_]*(\[[]\])?(\.[a-zA-Z_][a-zA-Z0-9_]*(\[[]\])?)*$";
    if (!Regex.IsMatch(sanitized, pattern))
        throw new ArgumentException("Invalid navigation property format");
        
    return sanitized;
}

```

---

## 16. 貢獻指南

我們歡迎所有的貢獻！請依照以下步驟進行貢獻：

1. Fork 此專案
2. 提交您的改動
3. 發送 Pull Request

我們會儘快檢視您的變更！

---

## 17. 授權條款

本專案採用 MIT 授權條款。詳情請參閱 [LICENSE](LICENSE) 檔案。

---

## 18. 版本歷史與更新日誌

### v1.0.0 - 2023-10-10
- 初始版本發佈

---

## 19. 常見問題 (FAQ)

**Q1：如何處理日期時間的時區問題？**  
A1：建議在應用程式層級統一處理時區，例如將所有時間轉為 UTC 儲存，再於取出時轉回當地時間。

**Q2：支援的資料庫有哪些？**  
A2：目前測試環境包含 Microsoft SQL Server、SQLite，其他 EF Core 支援的資料庫應亦可正常運作。

**Q3：能否處理複雜物件的查詢？**  
A3：設計上是支援的，但建議查詢條件儘量簡化，以維持查詢效能。

更多問題，歡迎提出 Issue 或 PR！

---

## 20. 導覽屬性查詢支援

DynamicPredicateBuilder 完全支援導覽屬性查詢，讓您能輕鬆查詢關聯資料。以下說明如何使用導覽屬性進行查詢。

### 20-1. 基本用法

當查詢資料表的同時，想要過濾或搜尋其關聯資料表的欄位時，可使用導覽屬性查詢。

#### 範例：查詢員工的部門名稱

假設 `Employee` 有一個導覽屬性 `Department`，我們可以這樣查詢：

```csharp
var filterGroup = FilterDictionaryBuilder.QueryBuilder<Employee>()
    .WithLogicalOperator(LogicalOperator.And)
    .Equal(x => x.Department.Name, "Sales")     // 直接查詢部門名稱
    .ToFilterGroup();

var predicate = FilterBuilder.Build<Employee>(filterGroup);
var result = _db.Employees.Include(e => e.Department).Where(predicate).ToList();
```

### 20-2. 多層導覽屬性

支援多層級的導覽屬性查詢，例如：`Employee.Profile.Skills`。但要注意效能與可能的 `null` 參考問題。

#### 範例：查詢員工擁有 C# 技能

假設 `Employee` 有一個導覽屬性 `Profile`，而 `Profile` 又有一個集合型別的導覽屬性 `Skills`：

```csharp
var filterGroup = FilterDictionaryBuilder.QueryBuilder<Employee>()
    .WithLogicalOperator(LogicalOperator.And)
    .Contains(x => x.Profile.Skills, "C#")       // 查詢技能集合中是否包含 "C#"
    .ToFilterGroup();

var predicate = FilterBuilder.Build<Employee>(filterGroup);
var result = _db.Employees.Include(e => e.Profile).ThenInclude(p => p.Skills).Where(predicate).ToList();
```

### 20-3. 注意事項

- 使用導覽屬性查詢時，必須搭配 `Include` 方法，將關聯的資料表一併載入。
- 導覽屬性的查詢條件會轉換為 JOIN 條件，因此可能影響效能。
- 建議對常用的導覽屬性建立索引，以提升查詢效能。

---

## 21. 陣列導覽屬性查詢支援

### 21-1. 概述
DynamicPredicateBuilder 提供完整的陣列導覽屬性查詢功能，支援對集合中的子屬性進行各種查詢操作。透過 `FilterDictionaryBuilder` 的陣列導覽方法，您可以輕鬆查詢巢狀集合中的資料。

### 21-2. 陣列導覽語法
所有陣列導覽方法都會自動生成正確的語法格式：```
{集合屬性}[].{目標屬性}
```

範例：
- `BuildContracts[].Build.AptId`
- `BuildContracts[].Build.Name` 
- `BuildContracts[].Build.Location`
- `Orders[].Items[].ProductName`

### 21-3. 完整的陣列導覽方法

#### 基本比較方法
```csharp
var builder = FilterDictionaryBuilder.QueryBuilder<Contract>()
    // 基本等值比較
    .ArrayEqual(c => c.BuildContracts, bc => bc.Build.AptId, 1001L)
    .ArrayNotEqual(c => c.BuildContracts, bc => bc.ContractType, "自住")
    
    // 數值比較
    .ArrayGreaterThan(c => c.BuildContracts, bc => bc.Amount, 50000m)
    .ArrayGreaterThanOrEqual(c => c.BuildContracts, bc => bc.Amount, 30000m)
    .ArrayLessThan(c => c.BuildContracts, bc => bc.Build.AptId, 2000L)
    .ArrayLessThanOrEqual(c => c.BuildContracts, bc => bc.Amount, 100000m);
```

#### 字串操作方法
```csharp
var builder = FilterDictionaryBuilder.QueryBuilder<Contract>()
    // 字串匹配操作
    .ArrayLike(c => c.BuildContracts, bc => bc.Build.Name, "豪宅")        // ✨ 新增 Like 匹配
    .ArrayNotLike(c => c.BuildContracts, bc => bc.Build.Name, "舊屋")     // Not Like 匹配
    .ArrayContains(c => c.BuildContracts, bc => bc.Build.Location, "台北") // 包含字串
    .ArrayNotContains(c => c.BuildContracts, bc => bc.Build.Name, "違建") // 不包含字串
    .ArrayStartsWith(c => c.BuildContracts, bc => bc.Build.Location, "台北市") // 開頭匹配
    .ArrayEndsWith(c => c.BuildContracts, bc => bc.Build.Name, "大樓");    // 結尾匹配
```

#### 集合與範圍方法
```csharp
var builder = FilterDictionaryBuilder.QueryBuilder<Contract>()
    // 集合成員查詢
    .ArrayIn(c => c.BuildContracts, bc => bc.Build.AptId, new object[] { 1001L, 1002L, 1003L })
    .ArrayNotIn(c => c.BuildContracts, bc => bc.ContractType, new object[] { "已取消", "已終止" })
    
    // 範圍查詢
    .ArrayBetween(c => c.BuildContracts, bc => bc.Amount, 50000m, 200000m)
    .ArrayNotBetween(c => c.BuildContracts, bc => bc.Build.AptId, 1000L, 1500L)
    
    // 存在性查詢
    .ArrayAny(c => c.BuildContracts, bc => bc.Build.AptId)      // 集合中有任何元素
    .ArrayNotAny(c => c.BuildContracts, bc => bc.IsDeleted);    // 集合中沒有元素符合條件
```

### 21-4. 實際使用範例

#### 範例 1：基本陣列導覽查詢
```csharp
[HttpPost("contracts/search")]
public IActionResult SearchContracts()
{
    // 查找建案名稱包含 "豪宅" 的合約
    var filterGroup = FilterDictionaryBuilder.QueryBuilder<Contract>()
        .WithLogicalOperator(LogicalOperator.And)
        .ArrayLike(c => c.BuildContracts, bc => bc.Build.Name, "豪宅")
        .ToFilterGroup();

    var predicate = FilterBuilder.Build<Contract>(filterGroup);
    var results = _context.Contracts
        .Include(c => c.BuildContracts)
        .ThenInclude(bc => bc.Build)
        .Where(predicate)
        .ToList();

    return Ok(results);
}
```

#### 範例 2：複雜組合查詢
```csharp
[HttpPost("contracts/advanced-search")]
public IActionResult AdvancedSearchContracts()
{
    // 複雜查詢：結合多種陣列導覽方法
    var filterGroup = FilterDictionaryBuilder.QueryBuilder<Contract>()
        .WithLogicalOperator(LogicalOperator.And)
        .ArrayGreaterThan(c => c.BuildContracts, bc => bc.Build.AptId, 1000L)    // AptId > 1000
        .ArrayLike(c => c.BuildContracts, bc => bc.Build.Location, "台北市")      // 位置包含台北市
        .ArrayNotEqual(c => c.BuildContracts, bc => bc.ContractType, "自住")     // 非自住合約
        .ArrayBetween(c => c.BuildContracts, bc => bc.Amount, 30000m, 100000m)   // 金額範圍
        .Contains(c => c.Name, "購買")                                           // 合約名稱包含購買
        .ToFilterGroup();

    var predicate = FilterBuilder.Build<Contract>(filterGroup);
    var results = _context.Contracts
        .Include(c => c.BuildContracts)
        .ThenInclude(bc => bc.Build)
        .Where(predicate)
        .ToList();

    return Ok(new { count = results.Count, contracts = results });
}
```

#### 範例 3：電商訂單查詢
```csharp
[HttpPost("orders/search")]
public IActionResult SearchOrders()
{
    // 查找包含特定商品的訂單
    var filterGroup = FilterDictionaryBuilder.QueryBuilder<Order>()
        .WithLogicalOperator(LogicalOperator.And)
        .ArrayIn(c => c.OrderItems, oi => oi.Product.Category, new object[] { "3C", "家電" })
        .ArrayGreaterThan(c => c.OrderItems, oi => oi.Quantity, 1)
        .ArrayBetween(c => c.OrderItems, oi => oi.UnitPrice, 1000m, 50000m)
        .GreaterThan(c => c.TotalAmount, 5000m)
        .ToFilterGroup();

    var predicate = FilterBuilder.Build<Order>(filterGroup);
    var results = _db.Orders
        .Include(o => o.OrderItems)
        .ThenInclude(oi => oi.Product)
        .Where(predicate)
        .ToList();

    return Ok(results);
}
```

### 21-5. JSON 格式範例

#### 基本陣列導覽查詢 JSON
```jsonc
{
  "Filter": {
    "LogicalOperator": "And",
    "Rules": [
      {
        "Property": "BuildContracts[].Build.AptId",
        "Operator": "Equal",
        "Value": 1001
      },
      {
        "Property": "BuildContracts[].Build.Name",
        "Operator": "Like",
        "Value": "豪宅"
      }
    ]
  }
}
```

#### 複雜陣列導覽查詢 JSON
```jsonc
{
  "Filter": {
    "LogicalOperator": "And",
    "Rules": [
      {
        "Property": "BuildContracts[].Build.Location",
        "Operator": "StartsWith",
        "Value": "台北市"
      },
      {
        "Property": "BuildContracts[].Amount",
        "Operator": "Between",
        "Value": [30000, 100000]
      },
      {
        "Property": "BuildContracts[].ContractType",
        "Operator": "In",
        "Value": ["購買", "投資"]
      }
    ]
  }
}
```

### 21-6. 支援的資料類型

| 資料類型 | 支援的操作 | 說明 |
|---------|-----------|------|
| **字串類型** | `Like`, `NotLike`, `Contains`, `NotContains`, `StartsWith`, `EndsWith`, `Equal`, `NotEqual` | 完整支援所有字串操作 |
| **數值類型** | `Equal`, `NotEqual`, `GreaterThan`, `LessThan`, `Between`, `NotBetween`, `In`, `NotIn` | 支援所有比較和範圍操作 |
| **可空類型** | 所有對應類型的操作 | 正確處理 `null` 值比較 |
| **集合類型** | `In`, `NotIn`, `Any`, `NotAny` | 支援集合成員和存在性查詢 |
| **日期時間** | `Equal`, `GreaterThan`, `LessThan`, `Between` 等 | 完整支援日期時間比較 |

### 21-7. 陣列導覽方法完整列表

| 方法名稱 | 對應運算子 | 說明 | 使用範例 |
|---------|-----------|------|---------|
| `ArrayEqual` | `Equal` | 陣列元素等於指定值 | `ArrayEqual(c => c.Items, i => i.Id, 123)` |
| `ArrayNotEqual` | `NotEqual` | 陣列元素不等於指定值 | `ArrayNotEqual(c => c.Items, i => i.Status, "Deleted")` |
| `ArrayLike` | `Like` | 陣列元素符合 Like 條件 | `ArrayLike(c => c.Items, i => i.Name, "產品")` |
| `ArrayNotLike` | `NotLike` | 陣列元素不符合 Like 條件 | `ArrayNotLike(c => c.Items, i => i.Name, "測試")` |
| `ArrayContains` | `Contains` | 陣列元素包含指定字串 | `ArrayContains(c => c.Items, i => i.Description, "特價")` |
| `ArrayNotContains` | `NotContains` | 陣列元素不包含指定字串 | `ArrayNotContains(c => c.Items, i => i.Name, "停產")` |
| `ArrayStartsWith` | `StartsWith` | 陣列元素以指定字串開頭 | `ArrayStartsWith(c => c.Items, i => i.Code, "PRD")` |
| `ArrayEndsWith` | `EndsWith` | 陣列元素以指定字串結尾 | `ArrayEndsWith(c => c.Items, i => i.Code, "001")` |
| `ArrayGreaterThan` | `GreaterThan` | 陣列元素大於指定值 | `ArrayGreaterThan(c => c.Items, i => i.Price, 1000m)` |
| `ArrayGreaterThanOrEqual` | `GreaterThanOrEqual` | 陣列元素大於等於指定值 | `ArrayGreaterThanOrEqual(c => c.Items, i => i.Stock, 10)` |
| `ArrayLessThan` | `LessThan` | 陣列元素小於指定值 | `ArrayLessThan(c => c.Items, i => i.Weight, 5.0)` |
| `ArrayLessThanOrEqual` | `LessThanOrEqual` | 陣列元素小於等於指定值 | `ArrayLessThanOrEqual(c => c.Items, i => i.Discount, 0.3)` |
| `ArrayIn` | `In` | 陣列元素在指定集合中 | `ArrayIn(c => c.Items, i => i.Category, new[] {"A", "B"})` |
| `ArrayNotIn` | `NotIn` | 陣列元素不在指定集合中 | `ArrayNotIn(c => c.Items, i => i.Status, new[] {"Banned"})` |
| `ArrayBetween` | `Between` | 陣列元素在指定範圍內 | `ArrayBetween(c => c.Items, i => i.Price, 100m, 1000m)` |
| `ArrayNotBetween` | `NotBetween` | 陣列元素不在指定範圍內 | `ArrayNotBetween(c => c.Items, i => i.Score, 60, 80)` |
| `ArrayAny` | `Any` | 陣列中有任何元素符合條件 | `ArrayAny(c => c.Items, i => i.IsActive)` |
| `ArrayNotAny` | `NotAny` | 陣列中沒有元素符合條件 | `ArrayNotAny(c => c.Items, i => i.IsDeleted)` |

### 21-8. 性能最佳化建議

#### Include 策略
```csharp
// ✅ 正確：預先載入所需的導覽屬性
var results = _context.Contracts
    .Include(c => c.BuildContracts)
    .ThenInclude(bc => bc.Build)
    .Where(predicate)
    .ToList();

// ❌ 錯誤：未載入導覽屬性會導致 N+1 查詢問題
var results = _context.Contracts
    .Where(predicate)  // 這會觸發多次額外查詢
    .ToList();
```

#### 查詢最佳化
```csharp
// 使用 AsNoTracking() 提升只讀查詢效能
var results = _context.Contracts
    .AsNoTracking()
    .Include(c => c.BuildContracts)
    .ThenInclude(bc => bc.Build)
    .Where(predicate)
    .ToList();

// 使用投影減少資料傳輸
var results = _context.Contracts
    .Include(c => c.BuildContracts)
    .ThenInclude(bc => bc.Build)
    .Where(predicate)
    .Select(c => new ContractDto
    {
        Id = c.Id,
        Name = c.Name,
        BuildNames = c.BuildContracts.Select(bc => bc.Build.Name).ToList()
    })
    .ToList();
```

#### 避免深層巢狀
```csharp
// ❌ 避免過深的陣列導覽
.ArrayEqual(c => c.Level1, l1 => l1.Level2, l2 => l2.Level3, l3 => l3.Level4.Value, "value")

// ✅ 建議：限制在 2-3 層以內
.ArrayEqual(c => c.BuildContracts, bc => bc.Build.AptId, 1001L)
```

### 21-9. 錯誤處理與驗證

#### 屬性路徑驗證
```csharp
public static bool ValidateNavigationPath(string propertyPath)
{
    // 驗證陣列導覽語法格式
    var pattern = @"^[a-zA-Z_][a-zA-Z0-9_]*(\[\](\.[a-zA-Z_][a-zA-Z0-9_]*)+)*$";
    return Regex.IsMatch(propertyPath, pattern);
}

// 使用範例
if (!ValidateNavigationPath("BuildContracts[].Build.AptId"))
{
    throw new ArgumentException("Invalid navigation property path");
}
```

#### 安全性控制
```csharp
// 限制可查詢的陣列導覽屬性
var options = new FilterOptions
{
    AllowedFields = new HashSet<string>
    {
        "BuildContracts[].Amount",
        "BuildContracts[].Build.Name",
        "BuildContracts[].Build.Location",
        // 不包含敏感資訊如 "BuildContracts[].Build.SecretInfo"
    }
};

var predicate = FilterBuilder.Build<Contract>(filterGroup, options);
```

### 21-10. 實務應用場景

#### 電商平台
```csharp
// 查找包含特定品牌商品的訂單
.ArrayEqual(c => c.OrderItems, oi => oi.Product.Brand, "Apple")
.ArrayGreaterThan(c => c.OrderItems, oi => oi.Quantity, 1)
```

#### 專案管理
```csharp
// 查找有高優先級任務的專案
.ArrayEqual(c => c.Tasks, t => t.Priority, "High")
.ArrayLessThanOrEqual(c => c.Tasks, t => t.CompletionRate, 0.8)
```

#### 客戶關係管理
```csharp
// 查找有活躍聯絡人的客戶
.ArrayEqual(c => c.Contacts, ct => ct.Status, "Active")
.ArrayContains(c => c.Contacts, ct => ct.Email, "@company.com")
```

#### 學校管理系統
```csharp
// 查找有特定課程的學生
.ArrayIn(c => c.Enrollments, e => e.Course.Code, new[] {"CS101", "MATH201"})
.ArrayGreaterThan(c => c.Enrollments, e => e.Grade, 80)
```

### 21-11. 除錯與監控

#### SQL 查詢監控
```csharp
// 啟用 SQL 日誌以監控生成的查詢
public void ConfigureServices(IServiceCollection services)
{
    services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseSqlServer(connectionString)
               .EnableSensitiveDataLogging()
               .LogTo(Console.WriteLine, LogLevel.Information);
    });
}
```

#### 查詢效能分析
```csharp
// 使用 Stopwatch 測量查詢時間
var stopwatch = Stopwatch.StartNew();
var results = _context.Contracts
    .Include(c => c.BuildContracts)
    .ThenInclude(bc => bc.Build)
    .Where(predicate)
    .ToList();
stopwatch.Stop();

_logger.LogInformation($"Array navigation query took {stopwatch.ElapsedMilliseconds}ms");
```

### 21-12. 單元測試範例

```csharp
[Fact]
public void ArrayLike_Should_FindMatchingContracts()
{
    // Arrange
    using var context = CreateTestContext();
    
    var filterGroup = FilterDictionaryBuilder.QueryBuilder<Contract>()
        .ArrayLike(c => c.BuildContracts, bc => bc.Build.Name, "豪宅")
        .ToFilterGroup();

    // Act
    var predicate = FilterBuilder.Build<Contract>(filterGroup);
    var results = context.Contracts
        .Include(c => c.BuildContracts)
        .ThenInclude(bc => bc.Build)
        .Where(predicate)
        .ToList();

    // Assert
    results.Should().HaveCount(1);
    results.Should().Contain(c => c.Name == "豪宅購買合約");
}

[Fact]
public void ArrayBetween_Should_FilterByRange()
{
    // Arrange
    using var context = CreateTestContext();
    
    var filterGroup = FilterDictionaryBuilder.QueryBuilder<Contract>()
        .ArrayBetween(c => c.BuildContracts, bc => bc.Amount, 30000m, 60000m)
        .ToFilterGroup();

    // Act
    var predicate = FilterBuilder.Build<Contract>(filterGroup);
    var results = context.Contracts
        .Include(c => c.BuildContracts)
        .Where(predicate)
        .ToList();

    // Assert
    results.Should().NotBeEmpty();
    results.SelectMany(c => c.BuildContracts)
           .Should().OnlyContain(bc => bc.Amount >= 30000m && bc.Amount <= 60000m);
}
```

---


