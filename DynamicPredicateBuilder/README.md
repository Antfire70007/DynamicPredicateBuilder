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
