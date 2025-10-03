# FilterDictionaryBuilder 使用說明

`FilterDictionaryBuilder` 是一個支援 Fluent API 的過濾條件建構器，可以讓你用更直觀的方式建立複雜的查詢條件。現在支援強型別的 Expression 語法！

## 快速開始

### 基本使用方式 (字串版本)

```csharp
// 簡單查詢
var query = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
    .WithLogicalOperator(LogicalOperator.Or)
    .Like(nameof(LandDTO.LandNo), "A123")
    .Like(nameof(LandDTO.CityCode), "TPE")
    .Build();
```

### 基本使用方式 (Expression 版本) ? 新功能

```csharp
// 強型別查詢 - 有編譯時期檢查
var query = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
    .WithLogicalOperator(LogicalOperator.Or)
    .Like(x => x.LandNo, "A123")
    .Like(x => x.CityCode, "TPE")
    .Build();
```

### 巢狀查詢 (Expression 版本)

```csharp
// 複雜巢狀查詢
var query = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
    .WithLogicalOperator(LogicalOperator.Or)
    .Like(x => x.LandNo, landNo)
    .Like(x => x.CityCode, city)
    .Compare(LogicalOperator.And, rules => rules
        .Equal(x => x.CaseOwner, "John Doe")
        .GreaterThan(x => x.Price, 1000000)
    )
    .Build();
```

## API 參考

### 建立 Builder

```csharp
// 方法1: 靜態工廠方法
var builder = FilterDictionaryBuilder.QueryBuilder<MyEntity>();

// 方法2: 另一個靜態工廠方法
var builder = FilterDictionaryBuilder.Create<MyEntity>();

// 方法3: 泛型類別的靜態方法
var builder = FilterDictionaryBuilder<MyEntity>.Create();
```

### 設定群組屬性

```csharp
builder
    .WithLogicalOperator(LogicalOperator.And)    // 設定群組內邏輯運算子
    .WithInterOperator(LogicalOperator.Or)       // 設定與下一群組的連接運算子
    .Negate(true);                               // 否定整個群組
```

### 基本條件方法

#### 字串版本
```csharp
builder
    .Add(property, FilterOperator.Equal, value, isNegated)  // 通用方法
    .Equal(property, value, isNegated)                      // 等於
    .Like(property, value, isNegated)                       // 模糊比對
    .Contains(property, value, isNegated)                   // 包含
    .In(property, values, isNegated)                        // 在集合中
    .Between(property, min, max, isNegated)                 // 在範圍內
    .GreaterThan(property, value, isNegated)                // 大於
    .LessThan(property, value, isNegated);                  // 小於
```

#### Expression 版本 ? 新功能
```csharp
builder
    .Add(x => x.Property, FilterOperator.Equal, value, isNegated)  // 通用方法
    .Equal(x => x.Property, value, isNegated)                      // 等於
    .Like(x => x.Property, value, isNegated)                       // 模糊比對
    .Contains(x => x.Property, value, isNegated)                   // 包含
    .In(x => x.Property, values, isNegated)                        // 在集合中
    .Between(x => x.Property, min, max, isNegated)                 // 在範圍內
    .GreaterThan(x => x.Property, value, isNegated)                // 大於
    .LessThan(x => x.Property, value, isNegated)                   // 小於
    .GreaterThanOrEqual(x => x.Property, value, isNegated)         // 大於等於
    .LessThanOrEqual(x => x.Property, value, isNegated)            // 小於等於
    .StartsWith(x => x.Property, value, isNegated)                 // 開頭包含
    .EndsWith(x => x.Property, value, isNegated);                  // 結尾包含
```

### 屬性比較

#### 字串版本
```csharp
builder.AddPropertyComparison(
    property, 
    FilterOperator.Equal, 
    compareToProperty, 
    isNegated
);
```

#### Expression 版本 ? 新功能
```csharp
builder.AddPropertyComparison(
    x => x.Property1, 
    FilterOperator.Equal, 
    x => x.Property2, 
    isNegated
);
```

### 巢狀群組

```csharp
builder.Compare(LogicalOperator.And, subRules => subRules
    .Equal("Property1", "Value1")
    .GreaterThan("Property2", 100)
    .Compare(LogicalOperator.Or, deeperRules => deeperRules
        .Like("Property3", "test")
        .Contains("Property4", "keyword")
    )
, isNegated);
```

### 結果輸出

```csharp
// 輸出為字典
Dictionary<string, object> dict = builder.Build();

// 輸出為 FilterGroup
FilterGroup group = builder.ToFilterGroup();

// 隱式轉換
Dictionary<string, object> dict = builder;  // 自動轉換為字典
FilterGroup group = builder;                // 自動轉換為 FilterGroup
```

## 實用範例

### 範例1: 符合原始需求的簡化語法

**原本的寫法:**
```csharp
var dict = new List<object>();
dict.Add(new Dictionary<string, object>
{
    { "Property", nameof(LandDTO.LandNo) },
    { "Operator", FilterOperator.Like },
    { "Value", landNo }
});

dict.Add(new Dictionary<string, object>
{
    { "Property", nameof(LandDTO.CityCode) },
    { "Operator", FilterOperator.Like },
    { "Value", city }
});

var rawDict = new Dictionary<string, object>
{
    { "LogicalOperator", LogicalOperator.Or},
    { "Rules", dict }
};
```

**新的寫法 (字串版本):**
```csharp
var query = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
    .WithLogicalOperator(LogicalOperator.Or)
    .Like(nameof(LandDTO.LandNo), landNo)
    .Like(nameof(LandDTO.CityCode), city)
    .Build();
```

**新的寫法 (Expression 版本) ? 推薦:**
```csharp
var query = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
    .WithLogicalOperator(LogicalOperator.Or)
    .Like(x => x.LandNo, landNo)
    .Like(x => x.CityCode, city)
    .Build();
```

### 範例2: 複雜的多層巢狀查詢 (Expression 版本)

```csharp
var query = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
    .WithLogicalOperator(LogicalOperator.And)
    .Compare(LogicalOperator.Or, mainConditions => mainConditions
        // 主要條件組
        .Like(x => x.LandNo, "A123")
        .In(x => x.CityCode, new[] { "TPE", "KHH", "TCH" })
        
        // 特殊條件子組
        .Compare(LogicalOperator.And, specialConditions => specialConditions
            .Equal(x => x.CaseOwner, "VIP Owner")
            .Between(x => x.Price, 5000000, 50000000)
        )
    )
    // 時間範圍限制
    .Compare(LogicalOperator.And, timeConditions => timeConditions
        .GreaterThan(x => x.CreateDate, DateTime.Now.AddMonths(-6))
        .LessThan(x => x.CreateDate, DateTime.Now)
    )
    .Build();
```

### 範例3: 否定條件 (Expression 版本)

```csharp
var query = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
    .Equal(x => x.CaseOwner, "Blacklisted Owner", isNegated: true)  // 不是特定業主
    .Compare(LogicalOperator.Or, priceRules => priceRules
        .LessThan(x => x.Price, 500000)
        .GreaterThan(x => x.Price, 10000000),
        isNegated: true)  // 不在價格範圍內
    .Build();
```

### 範例4: 混合使用 Expression 和字串版本

```csharp
var query = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
    .WithLogicalOperator(LogicalOperator.And)
    // Expression 版本 - 適用於已知的強型別屬性
    .Like(x => x.LandNo, "A123")
    .GreaterThan(x => x.Price, 1000000)
    // 字串版本 - 適用於動態屬性名稱
    .Equal(nameof(LandDTO.CaseOwner), "John Doe")
    .Contains("DynamicProperty", "value")  // 假設有動態屬性
    .Build();
```

### 範例5: 屬性對屬性比較 (Expression 版本)

```csharp
var query = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
    .WithLogicalOperator(LogicalOperator.And)
    // 屬性對屬性比較
    .AddPropertyComparison(x => x.LandNo, FilterOperator.Equal, x => x.CityCode)
    // 其他條件
    .GreaterThan(x => x.Price, 1000000)
    .Build();
```

## 與 FilterBuilder 整合

建立的查詢條件可以直接與現有的 `FilterBuilder` 整合使用：

```csharp
// 建立查詢條件
FilterGroup filterGroup = FilterDictionaryBuilder.QueryBuilder<MyEntity>()
    .Like("Name", "test")
    .GreaterThan("Age", 18)
    .ToFilterGroup();

// 使用 FilterBuilder 建立 Expression
var expression = FilterBuilder.Build<MyEntity>(filterGroup);

// 應用到 LINQ 查詢
var results = dbContext.MyEntities.Where(expression).ToList();
```

## 優勢

1. **可讀性佳**: Fluent API 讓程式碼更直觀易懂
2. **類型安全**: 泛型支援和 Expression 語法提供編譯時期的類型檢查 ?
3. **IntelliSense 支援**: Expression 版本提供完整的 IDE 自動完成 ?
4. **重構友善**: 使用 Expression 時屬性重新命名會自動更新 ?
5. **功能完整**: 支援所有現有的過濾運算子和巢狀結構
6. **向後相容**: 完全相容現有的字典格式和 FilterGroup
7. **彈性高**: 支援隱式轉換和多種建立方式
8. **雙語法支援**: 同時支援強型別 Expression 和動態字串語法 ?
9. **易於測試**: 清晰的 API 結構便於單元測試

## 何時使用哪種語法

### Expression 版本 (推薦) ?
- ? 屬性名稱在編譯時期已知
- ? 需要 IntelliSense 支援
- ? 需要重構安全性
- ? 團隊偏好強型別程式碼

### 字串版本
- ? 動態屬性名稱 (從設定檔讀取等)
- ? 需要向後相容舊程式碼
- ? 屬性名稱來自外部來源 (如 API)

## 注意事項

- **Expression 版本**: 支援簡單屬性和巢狀屬性 (如 `x => x.User.Name`)
- **字串版本**: 屬性名稱建議使用 `nameof()` 運算子確保類型安全
- 巢狀層級沒有限制，但建議不要過深以維持可讀性
- 所有方法都支援 `isNegated` 參數來反轉條件
- Expression 和字串版本可以在同一個查詢中混合使用