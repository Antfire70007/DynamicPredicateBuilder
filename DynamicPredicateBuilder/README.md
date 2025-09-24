# DynamicPredicateBuilder 使用說明

> **支援環境**：.NET 9; .NET 8; .NET 7  
> **核心特色**：動態過濾、排序、分頁、欄位查詢權限、巢狀/多組條件、NOT 取反、重複條件自動去除。

---

## 目錄

1. [快速開始](#1-快速開始)
2. [欄位查詢權限設定](#2-欄位查詢權限設定)
3. [進階條件組合功能](#3-進階條件組合功能)
4. [支援的運算子](#4-支援的運算子)
5. [集合型別欄位查詢支援](#5-集合型別欄位查詢支援)
6. [API 使用範例](#6-api-使用範例)
7. [核心類別與 API 參考](#7-核心類別與-api-參考)
8. [與 jQuery DataTables Server-Side 搭配](#8-與-jquery-datatables-server-side-搭配)
9. [常用 Extension](#9-常用-extension)
10. [單元測試](#10-單元測試)
11. [安裝與使用](#11-安裝與使用)

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

`FilterBuilder.Build<T>(IEnumerable<FilterGroup> groups, FilterOptions?)` 允許在「群組與群組」之間再指定 AND / OR（`InterOperator`），例如：

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
| `Any` | 集合中任一元素符合條件 | `Tags.Any(t => t == "VIP")` |
| `NotAny` | 集合中沒有元素符合條件 | `!Tags.Any(t => t == "VIP")` |

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

// Any：檢查屬性值是否存在於集合中
new FilterRule { Property = "Tags", Operator = FilterOperator.Any, Value = "VIP" }

// NotAny：檢查屬性值是否不存在於集合中
new FilterRule { Property = "Tags", Operator = FilterOperator.NotAny, Value = "VIP" }
```

---

## 5. 集合型別欄位查詢支援

### 5-1. 欄位路徑格式
- 支援巢狀集合屬性查詢，例如：`Orders[].Items[].Name`
- 欄位白名單自動展開所有集合層級，格式為 `集合屬性名[].屬性名`，可多層巢狀

### 5-2. FilterRule 實例
```csharp
new FilterRule
{
    Property = "Orders[].Items[].Name",
    Operator = FilterOperator.In,
    Value = new[] { "ItemA", "ItemB" }
}
```
這會產生：`Orders.SelectMany(o => o.Items).Select(i => i.Name).Any(name => new[] { "ItemA", "ItemB" }.Contains(name))`

### 5-3. 運算子支援
- **In**：查詢集合屬性是否包含指定值（多值）
- **Any**：查詢集合屬性是否有任一元素符合條件
- **Contains**：查詢集合屬性是否包含單一值
- **Equal**：僅用於非集合屬性
- **Like/NotLike**：可用於字串型別欄位

> **注意**：查詢集合屬性時，請使用 `In`、`Any`、`Contains`，不要用 `Equal` 比較集合本身。

### 5-4. 範例：查詢集合屬性底下的欄位
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

## 6. API 使用範例

### 6-1. Request 範例（單組簡易）

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

### 6-2. Request 範例（多組 + NOT + 巢狀）

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

### 6-3. Response 範例

```jsonc
{
  "totalCount": 45,
  "items": [
    { "name": "Alice", "age": 30, "address": { "city": "Taipei" } }
  ]
}
```

---

## 7. 核心類別與 API 參考

### 7-1. FilterBuilder
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

### 7-2. FilterEngine
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

### 7-3. FilterEngineExtensions
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

### 7-4. QueryableFieldHelper
提供欄位白名單的功能，確保查詢僅限於允許的欄位。

#### 主要方法
- **`GetQueryableFields<T>()`**：解析 `[Queryable]` 標籤產生欄位白名單
- **`GetAllowedFields<T>()`**：取得允許查詢的欄位集合

```csharp
var allowedFields = QueryableFieldHelper.GetQueryableFields<Person>();
var options = new FilterOptions { AllowedFields = allowedFields };
```

### 7-5. FilterGroupFactory
用於從不同來源建立 FilterGroup 物件。

#### 主要方法
- **`FromDictionary(Dictionary<string, object> dict)`**：從字典建立 FilterGroup
- **`FromJsonElement(JsonElement json)`**：從 JsonElement 建立 FilterGroup

```csharp
var filterGroup = FilterGroupFactory.FromDictionary(dictionary);
var filterGroup = FilterGroupFactory.FromJsonElement(jsonElement);
```

### 7-6. 核心資料模型

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

## 8. 與 jQuery DataTables Server-Side 搭配

以下示範 **DataTables 1.13+** 於前端傳送分頁、排序、全域搜尋與欄位搜尋，後端再組成 `FilterGroup`：

### 8-1. 前端 JavaScript

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

### 8-2. 後端 Controller

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

## 9. 常用 Extension

| 方法 | 說明 |
|---|---|
| `ApplySort(this IQueryable<T>, IEnumerable<SortRule>)` | 依多欄位排序 (動態排序)。 |
| `ApplyPaging(this IQueryable<T>, int page, int size, out int total)` | 取得總筆數並套用 Skip/Take。 |
| `ApplyQuery(this IQueryable<T>, QueryRequest request)` | 套用完整查詢請求（過濾、排序、分頁）。 |
| `ApplyFilterJson(this IQueryable<T>, JsonElement, List<SortRule>?)` | 套用 JSON 格式過濾條件。 |
| `FilterGroupFactory.FromDictionary(IDictionary<string, object>)` | 把前端 JSON 轉成 `FilterGroup` 物件。 |
| `QueryableFieldHelper.GetQueryableFields<T>()` | 解析 `[Queryable]` 標籤產生欄位白名單。 |

---

## 10. 單元測試

`DynamicPredicate.Tests` 專案示範：

* **FilterBuilderTests**：Equal、GreaterThan、NOT、巢狀、多組 AND/OR 等核心功能測試
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
        new FilterGroup
        {
            LogicalOperator = LogicalOperator.And,
            InterOperator = LogicalOperator.Or,
            Rules =
            [
                new FilterRule { Property = "Name", Operator = FilterOperator.Equal, Value = "Snake" }
            ]
        },
        new FilterGroup
        {
            LogicalOperator = LogicalOperator.And,
            Rules =
            [
                new FilterRule { Property = "Age", Operator = FilterOperator.GreaterThan, Value = 40 }
            ]
        }
    };

    var predicate = FilterBuilder.Build<User>(groups).Compile();

    predicate(new User { Name = "Snake", Age = 20 }).Should().BeTrue();   // Group1 滿足
    predicate(new User { Name = "Otacon", Age = 50 }).Should().BeTrue(); // Group2 滿足
    predicate(new User { Name = "Otacon", Age = 30 }).Should().BeFalse();
}
```

---

## 11. 安裝與使用

### 11-1. 系統需求
- .NET 7.0 或更高版本
- .NET 8.0 或更高版本  
- .NET 9.0 或更高版本

### 11-2. NuGet 安裝

```bash
dotnet add package DynamicPredicateBuilder
```

### 11-3. 基本設定

```csharp
using DynamicPredicateBuilder;
using DynamicPredicateBuilder.Models;
using DynamicPredicateBuilder.Core;

// 在 Program.cs 或 Startup.cs 中註冊服務（可選）
services.AddScoped<FilterOptions>();
```

### 11-4. 在控制器中使用

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
}
```

---

## 12. 進階功能與最佳實務

### 12-1. 效能最佳化
- 使用 `AsNoTracking()` 提升查詢效能
- 在經常查詢的欄位加上索引
- 適當使用 `IQueryable` 延遲執行特性

```csharp
var result = _context.People
    .AsNoTracking()
    .ApplyQuery(request);
```

### 12-2. 安全性考量
- 始終使用 `FilterOptions.AllowedFields` 限制可查詢欄位
- 驗證輸入資料的型別和範圍
- 避免暴露敏感資料欄位

### 12-3. 錯誤處理
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

---

## 13. 貢獻指南

### 13-1. 開發環境設定
1. 安裝 .NET 7.0 或更高版本 SDK
2. Clone 專案：`git clone https://github.com/Antfire70007/DynamicPredicateBuilder.git`
3. 建置專案：`dotnet build`
4. 執行測試：`dotnet test`

### 13-2. 提交 Issue
- 描述問題的詳細資訊
- 提供重現問題的步驟
- 包含相關的程式碼範例

### 13-3. 提交 PR
- 請遵循專案的程式碼風格
- 提交前請確保所有測試通過
- 分支命名建議使用 `feature/` 或 `bugfix/` 前綴
- 加入對應的單元測試

---

## 14. 授權條款

本專案採用 MIT 授權條款，詳見 [LICENSE](./LICENSE)。

---

## 15. 版本歷史與更新日誌

### v1.0.7
- 支援 .NET 9.0
- 新增更多查詢運算子
- 改善集合查詢效能
- 強化錯誤處理機制

### 未來規劃
- 支援更複雜的空間查詢
- 加入快取機制
- 提供 GraphQL 整合
- 支援非同步查詢

---

## 16. 常見問題 (FAQ)

### Q: 如何處理日期時間查詢？
A: 使用標準的 DateTime 比較運算子：
```csharp
new FilterRule { Property = "CreatedDate", Operator = FilterOperator.GreaterThan, Value = DateTime.Today }
```

### Q: 支援模糊搜尋嗎？
A: 支援，使用 `Like` 或 `Contains` 運算子：
```csharp
new FilterRule { Property = "Name", Operator = FilterOperator.Like, Value = "%John%" }
```

### Q: 如何處理 Null 值查詢？
A: 直接使用 `Equal` 或 `NotEqual` 搭配 null 值：
```csharp
new FilterRule { Property = "MiddleName", Operator = FilterOperator.Equal, Value = null }
```

---

持續優化中，歡迎 Issue／PR！

