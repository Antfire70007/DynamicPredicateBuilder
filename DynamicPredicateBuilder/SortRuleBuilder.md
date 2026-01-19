# SortRuleBuilder 使用說明

`SortRuleBuilder` 是一個支援 Fluent API 的排序規則建構器，可以讓你用更直觀的方式建立排序條件。支援強型別的 Expression 語法！

## 快速開始

### 基本使用方式 (字串版本)

```csharp
// 簡單排序
var sortRules = SortRuleBuilder.SortBuilder<UserDTO>()
    .Ascending(nameof(UserDTO.Username))
    .Descending(nameof(UserDTO.CreatedDate))
    .Build();
```

### 基本使用方式 (Expression 版本)

```csharp
// 強型別排序 - 有編譯時期檢查
var sortRules = SortRuleBuilder.SortBuilder<UserDTO>()
    .Ascending(x => x.Username)
    .Descending(x => x.CreatedDate)
    .Build();
```

### 多層級排序 (Expression 版本)

```csharp
// 主要排序條件與次要排序條件
var sortRules = SortRuleBuilder.SortBuilder<UserDTO>()
    .Ascending(x => x.Department)      // 先按部門升序排序
    .ThenBy(x => x.Username)           // 相同部門按用戶名升序排序
    .ThenByDescending(x => x.Age)      // 相同用戶名按年齡降序排序
    .Build();
```

## API 參考

### 建立 Builder

```csharp
// 方法1: 靜態工廠方法
var builder = SortRuleBuilder.SortBuilder<MyEntity>();

// 方法2: 另一個靜態工廠方法
var builder = SortRuleBuilder.Create<MyEntity>();

// 方法3: 泛型類別的靜態方法
var builder = SortRuleBuilder<MyEntity>.Create();
```

### 基本排序方法

#### 字串版本
```csharp
builder
    .Add(property, descending)          // 通用方法，可指定是否降序
    .Ascending(property)                // 升序排序
    .Descending(property)               // 降序排序
    .ThenBy(property)                   // 次要升序排序
    .ThenByDescending(property);        // 次要降序排序
```

#### Expression 版本
```csharp
builder
    .Add(x => x.Property, descending)   // 通用方法，可指定是否降序
    .Ascending(x => x.Property)         // 升序排序
    .Descending(x => x.Property)        // 降序排序
    .ThenBy(x => x.Property)            // 次要升序排序
    .ThenByDescending(x => x.Property); // 次要降序排序
```

### 結果輸出

```csharp
// 輸出為排序規則列表
List<SortRule> rules = builder.Build();

// 隱式轉換
List<SortRule> rules = builder;         // 自動轉換為列表
SortRule[] rulesArray = builder;        // 自動轉換為陣列
```

## 實用範例

### 範例1: 單一排序條件

**原本的寫法:**
```csharp
var sortRules = new List<SortRule>
{
    new SortRule
    {
        Property = nameof(UserDTO.Username),
        Descending = false
    }
};
```

**新的寫法 (字串版本):**
```csharp
var sortRules = SortRuleBuilder.SortBuilder<UserDTO>()
    .Ascending(nameof(UserDTO.Username))
    .Build();
```

**新的寫法 (Expression 版本) – 推薦:**
```csharp
var sortRules = SortRuleBuilder.SortBuilder<UserDTO>()
    .Ascending(x => x.Username)
    .Build();
```

### 範例2: 多層級排序 (Expression 版本)

```csharp
// 建立複雜的多層級排序
var sortRules = SortRuleBuilder.SortBuilder<OrderDTO>()
    .Ascending(x => x.Customer.Region)      // 先按客戶區域升序排序
    .ThenBy(x => x.OrderDate)               // 相同區域按訂單日期升序排序
    .ThenByDescending(x => x.TotalAmount)   // 相同日期按總金額降序排序
    .Build();
```

### 範例3: 與查詢請求結合使用

```csharp
// 建立排序規則
var sortRules = SortRuleBuilder.SortBuilder<ProductDTO>()
    .Descending(x => x.Price)
    .ThenBy(x => x.Name)
    .Build();

// 建立過濾條件
var filters = FilterDictionaryBuilder.QueryBuilder<ProductDTO>()
    .GreaterThan(x => x.Stock, 10)
    .Like(x => x.Category, "Electronics")
    .Build();

// 建立查詢請求
var request = new QueryRequest
{
    Filters = filters,
    SortRules = sortRules,
    PageSize = 20,
    PageIndex = 0
};

// 使用查詢請求
var result = await _productService.QueryAsync(request);
```

### 範例4: 直接與 LINQ 整合

```csharp
// 建立排序規則
var sortBuilder = SortRuleBuilder.SortBuilder<User>()
    .Ascending(x => x.LastName)
    .ThenBy(x => x.FirstName);

// 使用排序規則進行查詢
IQueryable<User> query = dbContext.Users;

// 針對每個排序規則依序套用
foreach (var rule in sortBuilder.Build())
{
    query = rule.Descending 
        ? query.AppendOrderByDescending(rule.Property) 
        : query.AppendOrderBy(rule.Property);
}

var results = query.ToList();
```

## 優勢

1. **可讀性佳**: Fluent API 讓程式碼更直觀易懂
2. **類型安全**: 泛型支援提供編譯時期的類型檢查
3. **功能完整**: 支援所有排序需求和複雜的多層級排序
4. **向後相容**: 完全相容現有的 SortRule 清單格式
5. **彈性高**: 支援隱式轉換和多種建立方式
6. **易於測試**: 清晰的 API 結構便於單元測試

## 注意事項

- 排序規則的順序很重要，會依照添加順序進行優先級排序
- 屬性名稱建議使用 `nameof()` 運算子或 Expression 確保類型安全
- `ThenBy` 和 `ThenByDescending` 在語義上是次要排序，但功能上與 `Ascending` 和 `Descending` 相同