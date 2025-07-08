# DynamicPredicateBuilder 使用說明

## 簡介
DynamicPredicateBuilder 提供一套靈活的查詢條件組合機制，支援欄位查詢權限控管、動態過濾、排序與分頁，適用於 .NET 8 專案。

---

## 1. 欄位查詢權限設定

### 方式一：程式碼指定可查詢欄位
於 Controller 設定 `AllowedFields`，僅允許指定欄位查詢：
[HttpPost("people")]
public IActionResult QueryPeople([FromBody] QueryRequest request)
{
    var options = new FilterOptions
    {
        AllowedFields = new HashSet<string> { "Name", "Age", "Address.City" }  // 允許查的欄位
    };

    var filterGroup = FilterGroupFactory.FromDictionary(request.Filter);
    var predicate = FilterBuilder.Build<Person>(filterGroup, options);

    var query = _db.People.Where(predicate)
        .ApplySort(request.Sort);

    var totalCount = query.Count();

    var result = query.Skip((request.Page - 1) * request.PageSize)
                      .Take(request.PageSize)
                      .ToList();

    return Ok(new QueryResult<Person>
    {
        TotalCount = totalCount,
        Items = result
    });
}
### 方式二：使用 Attribute 標註可查詢欄位
於 Model 屬性加上 `[Queryable]`，自動取得可查詢欄位：

#### Model 使用方式public class Person
{
    [Queryable]
    public string Name { get; set; }

    [Queryable]
    public int Age { get; set; }

    public string Password { get; set; } // 沒標，不能查

    [Queryable]
    public Address Address { get; set; }
}

public class Address
{
    [Queryable]
    public string City { get; set; }

    public string SecretNote { get; set; } // 沒標，不能查
}
#### Controller 使用方式[HttpPost("people")]
public IActionResult QueryPeople([FromBody] QueryRequest request)
{
    var allowedFields = QueryableFieldHelper.GetQueryableFields<Person>();

    var options = new FilterOptions
    {
        AllowedFields = allowedFields
    };

    var filterGroup = FilterGroupFactory.FromDictionary(request.Filter);
    var predicate = FilterBuilder.Build<Person>(filterGroup, options);

    var query = _db.People.Where(predicate)
        .ApplySort(request.Sort);

    var totalCount = query.Count();

    var result = query.Skip((request.Page - 1) * request.PageSize)
                      .Take(request.PageSize)
                      .ToList();

    return Ok(new QueryResult<Person>
    {
        TotalCount = totalCount,
        Items = result
    });
}
#### 取得可查詢欄位QueryableFieldHelper.GetQueryableFields<Person>();
---

## 2. API 使用方式

### Request Example{
  "Filter": {
    "LogicalOperator": "And",
    "Rules": [
      { "Property": "Age", "Operator": "GreaterThanOrEqual", "Value": 25 },
      { "Property": "Address.City", "Operator": "Equal", "Value": "Taipei" }
    ]
  },
  "Sort": [
    { "Property": "Name", "Descending": false },
    { "Property": "Age", "Descending": true }
  ],
  "Page": 1,
  "PageSize": 5
}

### Response Example{
  "totalCount": 45,
  "items": [
    { "name": "Alice", "age": 30, "address": { "city": "Taipei" } },
    ...
  ]
}

---

## 3. API 範例

### Request 範例

### Response 範例