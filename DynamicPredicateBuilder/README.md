# DynamicPredicateBuilder 使用說明

> **支援環境**：.NET 9;.NET 8; .NET 7  
> **核心特色**：動態過濾、排序、分頁、欄位查詢權限、巢狀/多組條件、NOT 取反、重複條件自動去除。

---

## 1. 欄位查詢權限設定

### 1-1. 程式碼指定可查詢欄位

```csharp
[HttpPost("people")]
public IActionResult QueryPeople([FromBody] QueryRequest request)
{
    var options = new FilterOptions
    {
        AllowedFields = new HashSet<string> { "Name", "Age", "Address.City" }
    };

    var filterGroup = FilterGroupFactory.FromDictionary(request.Filter);
    var predicate   = FilterBuilder.Build<Person>(filterGroup, options);

    var data = _db.People
                 .Where(predicate)
                 .ApplySort(request.Sort)
                 .ApplyPaging(request.Page, request.PageSize, out var total);

    return Ok(new QueryResult<Person> { TotalCount = total, Items = data });
}
```

---

### 1-2. 使用 Attribute 自動擷取可查詢欄位

```csharp
public class Person
{
    [Queryable] public string Name  { get; set; } = string.Empty;
    [Queryable] public int    Age   { get; set; }
                 public string Password { get; set; } = string.Empty;   // ❌ 未標註，不可查
    [Queryable] public Address Address { get; set; } = new();
}

public class Address
{
    [Queryable] public string City { get; set; } = string.Empty;
                 public string SecretNote { get; set; } = string.Empty; // ❌
}

[HttpPost("people")]
public IActionResult QueryPeople([FromBody] QueryRequest request)
{
    var options = new FilterOptions
    {
        AllowedFields = QueryableFieldHelper.GetQueryableFields<Person>()
    };

    var filterGroup = FilterGroupFactory.FromDictionary(request.Filter);
    var predicate   = FilterBuilder.Build<Person>(filterGroup, options);

    // …其餘程式碼同上
}
```

---

## 2. 進階條件組合功能

### 2-1. 多組條件 (List<FilterGroup>)

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
            new FilterRule { Property = "Name", Operator = Equal, Value="Boss", IsNegated=true },
            new FilterRule { Property = "Age",  Operator = GreaterThan, Value=40 }
        ]
    },

    // Group 2：NOT (Status == "Retired")
    new()
    {
        IsNegated       = true,
        LogicalOperator = LogicalOperator.And,
        Rules =
        [
            new FilterRule { Property = "Status", Operator = Equal, Value="Retired" }
        ]
    }
};

var predicate = FilterBuilder.Build<Person>(groups).Compile();
```

邏輯相當於  
`( !(Name == "Boss") && Age > 40 ) OR !(Status == "Retired")`

### 2-2. 巢狀群組 (Nested Group)

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

### 2-3. NOT 取反

* **單條件**：`FilterRule.IsNegated = true`  
* **整組**：`FilterGroup.IsNegated = true`

### 2-4. 新增的 Operator 使用方式

以下是新增的 `FilterOperator` 及其使用範例：

- **`NotLike`**：用於檢查屬性值不符合指定的模式。

  ```csharp
  new FilterRule { Property = "Name", Operator = FilterOperator.NotLike, Value = "%Admin%" }
  ```

  等同於 SQL：`Name NOT LIKE '%Admin%'`

- **`Between`**：用於檢查屬性值是否在指定範圍內。

  ```csharp
  new FilterRule { Property = "Age", Operator = FilterOperator.Between, Value = new[] { 20, 30 } }
  ```

  等同於 SQL：`Age BETWEEN 20 AND 30`

- **`NotBetween`**：用於檢查屬性值是否不在指定範圍內。

  ```csharp
  new FilterRule { Property = "Age", Operator = FilterOperator.NotBetween, Value = new[] { 20, 30 } }
  ```

  等同於 SQL：`Age NOT BETWEEN 20 AND 30`

- **`Any`**：用於檢查屬性值是否存在於集合中。

  ```csharp
  new FilterRule { Property = "Tags", Operator = FilterOperator.Any, Value = "VIP" }
  ```

  等同於 SQL：`'VIP' = ANY(Tags)`

- **`NotAny`**：用於檢查屬性值是否不存在於集合中。

  ```csharp
  new FilterRule { Property = "Tags", Operator = FilterOperator.NotAny, Value = "VIP" }
  ```

  等同於 SQL：`'VIP' NOT IN (Tags)`

---

## 3. API 使用範例

### 3-1. Request 範例（單組簡易）

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

### 3-2. Request 範例（多組 + NOT + 巢狀）

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

### 3-3. Response 範例

```jsonc
{
  "totalCount": 45,
  "items": [
    { "name": "Alice", "age": 30, "address": { "city": "Taipei" } }
  ]
}
```

---

## 4. 常用 Extension

| 方法 | 說明 |
|---|---|
| `ApplySort(this IQueryable<T>, IEnumerable<SortRule>)` | 依多欄位排序 (Linq.Dynamic)。 |
| `ApplyPaging(this IQueryable<T>, int page, int size, out int total)` | 取得總筆數並套用 Skip/Take。 |
| `FilterGroupFactory.FromDictionary(IDictionary<string, object>)` | 把前端 JSON 轉成 `FilterGroup` 物件。 |
| `QueryableFieldHelper.GetQueryableFields<T>()` | 解析 `[Queryable]` 標籤產生欄位白名單。 |

---

## 5. 單元測試

`DynamicPredicate.Tests` 專案示範：

* **FilterBuilderTests**：Equal、GreaterThan、NOT、巢狀、多組 AND/OR。  
* **DeduplicationTests**：重複條件自動去除。  

```bash
dotnet test
```

即可看到覆蓋率與測試結果。

---

持續優化中，歡迎 Issue／PR！

## 6. 與 jQuery DataTables Server‑Side 搭配

以下示範 **DataTables 1.13+** 於前端傳送分頁、排序、全域搜尋與欄位搜尋，後端再組成 `FilterGroup`：

### 6‑1. 前端 JavaScript

```html
<table id="peopleTable" class="display" style="width:100%">
    <thead>
        <tr>
            <th>Name</th>
            <th>Age</th>
            <th>City</th>
        </tr>
        <tr><!-- 欄位篩選列 -->
            <th><input type="text" placeholder="Name"  class="col-search" data-col="0"></th>
            <th><input type="number" placeholder=">= Age" class="col-search" data-col="1"></th>
            <th><input type="text" placeholder="City"  class="col-search" data-col="2"></th>
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
                    draw:        d.draw,
                    page:        Math.floor(d.start / d.length) + 1,
                    pageSize:    d.length,
                    sort:        d.order.map(o => ({
                                    property: d.columns[o.column].data,
                                    descending: o.dir === 'desc'
                                 })),
                    filterGroups: buildFilterGroups(d)   // 🔑 自訂函式
                });
            }
        },
        columns: [
            { data: 'name'    },
            { data: 'age'     },
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
                        property:  c.data,
                        operator:  'Like',
                        value:     dt.search.value
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
                        value:    c.search.value
                    }]
                });
            }
        });

        return groups;
    }
});
</script>
```

### 6‑2. 後端 Controller

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
        recordsTotal    = _db.People.Count(),
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

### 6‑3. 小技巧

1. **searchDelay**：適度延遲可減少後端 QPS。  
2. **欄位搜尋列**：自行在 `<thead>` 多加一列 `<tr>` 搭配 `.column(index).search()`，體驗更友善。  
3. **複合條件**：前端把條件組成 `filterGroups`，後端不需再解析 DataTables 參數，專心跑 `FilterBuilder` 即可。  
4. **大量資料**：搭配 `IQueryable` 與 `EF Core` 的 `AsNoTracking()`，並在關鍵欄位加索引。  
5. **權限控管**：仍建議在 `FilterOptions.AllowedFields` 留白名單以防打到敏感資料。  

*如需更細的 DataTables 伺服端協定，可參考官方文件 <https://datatables.net/manual/server-side>*

---

## 7. 核心類別與功能

### 7-1. FilterBuilder
`FilterBuilder` 是專案的核心類別，負責生成查詢條件的表達式。以下是其主要方法：

- **`Build<T>(FilterGroup group, FilterOptions?)`**：生成單組條件的查詢表達式。
- **`Build<T>(IEnumerable<FilterGroup> groups, FilterOptions?)`**：生成多組條件的查詢表達式。

### 7-2. QueryableFieldHelper
`QueryableFieldHelper` 提供了欄位白名單的功能，確保查詢僅限於允許的欄位。

- **`GetQueryableFields<T>()`**：解析 `[Queryable]` 標籤產生欄位白名單。
- **`GetAllowedFields<T>()`**：取得允許查詢的欄位集合。

### 7-3. PagedResult
`PagedResult` 是用於分頁結果的類別，包含以下屬性：

- **`TotalCount`**：總筆數。
- **`Items`**：分頁後的資料項目。
- **`Page`**：目前頁碼。
- **`PageSize`**：每頁筆數。
- **`TotalPages`**：總頁數。

### 7-4. FilterOperator
`FilterOperator` 定義了多種查詢運算符，例如 `Equal`、`GreaterThan`、`NotLike` 等，並可擴充自訂運算符。

---

## 8. 測試範圍與重點

### 8-1. 測試檔案
- **`FilterBuilderTests`**：測試 `FilterBuilder` 的功能，包括 Equal、GreaterThan、NOT、巢狀條件、多組 AND/OR。
- **`TestData/User.cs`**：提供測試用的資料模型。

### 8-2. 執行測試
執行以下指令以執行測試並檢視覆蓋率：

```bash
dotnet test
```

---

## 9. 安裝與使用

### 9-1. 安裝
使用以下指令安裝 NuGet 套件：

```bash
dotnet add package DynamicPredicateBuilder
```

### 9-2. 使用
參考上述的使用範例，將 `DynamicPredicateBuilder` 整合到您的專案中。

---

## 10. 貢獻指南

### 10-1. 提交 Issue
- 描述問題的詳細資訊。
- 提供重現問題的步驟。

### 10-2. 提交 PR
- 請遵循專案的程式碼風格。
- 提交前請確保所有測試通過。
- 分支命名建議使用 `feature/` 或 `bugfix/` 前綴。

---

## 11. 授權條款

本專案採用 MIT 授權條款，詳見 [LICENSE](./LICENSE)。

---

## 12. 未來規劃與已知問題

### 12-1. 未來規劃
- 支援更多的查詢運算符。
- 提供更詳細的錯誤訊息。

### 12-2. 已知問題
- 尚未支援某些複雜的巢狀條件組合。
- 測試覆蓋率仍有提升空間。

