# DynamicPredicateBuilder �ƭ������䴩���n

## ���z

DynamicPredicateBuilder v1.0.83+ ���ѹ�Ҧ� .NET �ƭ�����������䴩�A�]�A nullable ����C�����ɸԲӻ����U�ؼƭ��������ϥΤ覡�M�̨ι�ȡC

## �䴩���ƭ�����

### �������

| ���� | �d�� | �d�ҭ� | �γ~ |
|------|------|---------|------|
| `int`, `int?` | -2,147,483,648 �� 2,147,483,647 | `Age = 25` | �@���� |
| `long`, `long?` | -9,223,372,036,854,775,808 �� 9,223,372,036,854,775,807 | `Id = 1234567890L` | �j��ơBID |

### �B�I������

| ���� | ��� | �d�ҭ� | �γ~ |
|------|------|---------|------|
| `float`, `float?` | 7 ��� | `Rate = 3.14f` | �C��ׯB�I�� |
| `double`, `double?` | 15-17 ��� | `Score = 95.5678901234567` | ����ׯB�I�� |

### �Q�i������

| ���� | ��� | �d�ҭ� | �γ~ |
|------|------|---------|------|
| `decimal`, `decimal?` | 28-29 ��� | `Price = 123456.789012345m` | ���ĭp��B����� |

## Nullable �����S��欰

### Null �Ȥ��

```csharp
// �d�� null ��
new FilterRule { Property = "Salary", Operator = FilterOperator.Equal, Value = null }

// �d�߫D null ��
new FilterRule { Property = "Salary", Operator = FilterOperator.NotEqual, Value = null }
```

### �ƭȤ������ Null

�b�ƭȤ���B�⤤�Anull �Ȫ��欰�p�U�G

```csharp
// null > 50000 �� false
// null < 50000 �� false  
// null >= 50000 �� false
// null <= 50000 �� false
// null == null �� true
// null != null �� false
```

## �B��l�䴩�x�}

| �B��l | int/int? | long/long? | decimal/decimal? | double/double? | float/float? |
|--------|----------|------------|------------------|----------------|--------------|
| Equal | ? | ? | ? | ? | ? |
| NotEqual | ? | ? | ? | ? | ? |
| GreaterThan | ? | ? | ? | ? | ? |
| GreaterThanOrEqual | ? | ? | ? | ? | ? |
| LessThan | ? | ? | ? | ? | ? |
| LessThanOrEqual | ? | ? | ? | ? | ? |
| Between | ? | ? | ? | ? | ? |
| NotBetween | ? | ? | ? | ? | ? |
| In | ? | ? | ? | ? | ? |
| NotIn | ? | ? | ? | ? | ? |

## �ϥνd��

### �򥻼ƭȬd��

```csharp
// ��Ƭd��
new FilterRule { Property = "Age", Operator = FilterOperator.GreaterThan, Value = 18 }

// ����Ƭd��
new FilterRule { Property = "UserId", Operator = FilterOperator.Equal, Value = 1234567890L }

// Decimal �d��
new FilterRule { Property = "Price", Operator = FilterOperator.LessThanOrEqual, Value = 999.99m }

// Double �d��
new FilterRule { Property = "Score", Operator = FilterOperator.GreaterThanOrEqual, Value = 85.5 }

// Float �d��
new FilterRule { Property = "Rate", Operator = FilterOperator.Between, Value = new[] { 0.1f, 0.9f } }
```

### Nullable �����d��

```csharp
// �d�ߦ��~�ꪺ���u
new FilterRule { Property = "Salary", Operator = FilterOperator.GreaterThan, Value = 0m }

// �d�ߨS���~���ƪ����u
new FilterRule { Property = "Salary", Operator = FilterOperator.Equal, Value = null }

// �d���~��b�d�򤺪����u�]�ư� null�^
new FilterRule { Property = "Salary", Operator = FilterOperator.Between, Value = new[] { 30000m, 80000m } }

// �d���~��b���w�Ȥ������u�]�]�t null�^
new FilterRule { Property = "Salary", Operator = FilterOperator.In, Value = new object[] { 50000m, 60000m, null } }
```

### �����d�߽d��

```csharp
public class Employee
{
    public string Name { get; set; }
    public int Age { get; set; }
    public decimal? Salary { get; set; }
    public decimal? Bonus { get; set; }
    public double? Rating { get; set; }
}

// �d�߰��~�B�����������u
var filterGroup = new FilterGroup
{
    LogicalOperator = LogicalOperator.And,
    Rules = new List<object>
    {
        new FilterRule { Property = "Salary", Operator = FilterOperator.GreaterThan, Value = 80000m },
        new FilterRule { Property = "Rating", Operator = FilterOperator.NotEqual, Value = null },
        new FilterRule { Property = "Rating", Operator = FilterOperator.GreaterThanOrEqual, Value = 4.0 }
    }
};

// �d���~��μ������������u
var filterGroup2 = new FilterGroup
{
    LogicalOperator = LogicalOperator.Or,
    Rules = new List<object>
    {
        new FilterRule { Property = "Salary", Operator = FilterOperator.GreaterThan, Value = 100000m },
        new FilterRule { Property = "Bonus", Operator = FilterOperator.GreaterThan, Value = 20000m }
    }
};
```

## �����ഫ����

### �۰������ഫ

DynamicPredicateBuilder ���Ѵ��z�������ഫ�G

```csharp
// �r��۰��ഫ���ƭ�
new FilterRule { Property = "Age", Operator = FilterOperator.Equal, Value = "25" } // �� int 25

// ��Ʀ۰��ഫ�� decimal
new FilterRule { Property = "Price", Operator = FilterOperator.Equal, Value = 100 } // �� decimal 100m

// Double �۰��ഫ�� decimal�]�i�঳��׷l���^
new FilterRule { Property = "Amount", Operator = FilterOperator.Equal, Value = 123.45 } // �� decimal 123.45m
```

### �w���ഫ����

�Ҧ������ഫ���ϥΦw���� TryParse ��k�G

1. **���������ǰt**�G�p�G�Ȥw�g�O�ؼ������A�����ϥ�
2. **TryParse �ഫ**�G�ϥι����� TryParse ��k
3. **Convert.ChangeType �^�h**�G�̫�ϥ� .NET �����ഫ

```csharp
// �����ഫ�޿�ܨ�
if (value is decimal decimalValue)
    return decimalValue;  // �����ϥ�
if (decimal.TryParse(value.ToString(), out var parsedDecimal))
    return parsedDecimal;  // �w���ഫ
return Convert.ToDecimal(value);  // �^�h�ഫ
```

## �į�Ҷq

### Decimal vs Double

```csharp
// ? ���ˡG���ĭp��ϥ� decimal
new FilterRule { Property = "Price", Operator = FilterOperator.Equal, Value = 19.99m }

// ?? �`�N�G��ǭp��i�ϥ� double�A���n�`�N���
new FilterRule { Property = "Measurement", Operator = FilterOperator.Equal, Value = 19.99 }
```

### �j�ƭȳB�z

```csharp
// ? �䴩�G�j���
new FilterRule { Property = "BigId", Operator = FilterOperator.Equal, Value = 9223372036854775807L }

// ? �䴩�G����� decimal
new FilterRule { Property = "PreciseAmount", Operator = FilterOperator.Equal, Value = 123456789.123456789m }
```

## �̨ι��

### 1. ��ܦX�A���ƭ�����

```csharp
// �p�ơB���� �� int
public int Count { get; set; }

// �j�� ID�B�ɶ��W �� long  
public long Timestamp { get; set; }

// �����B��T�p�� �� decimal
public decimal Price { get; set; }

// ��ǭp��B�έp �� double
public double Average { get; set; }

// ��v�B�ʤ��� �� float�]�p�G��ר����^
public float Percentage { get; set; }
```

### 2. Nullable �������ϥ�

```csharp
// ? ���ˡG�i�諸�ƭ����ϥ� nullable
public decimal? OptionalFee { get; set; }
public double? Rating { get; set; }

// ? ���ˡG���T�B�z null ��
var hasRating = new FilterRule { Property = "Rating", Operator = FilterOperator.NotEqual, Value = null };
var highRating = new FilterRule { Property = "Rating", Operator = FilterOperator.GreaterThan, Value = 4.0 };
```

### 3. �d��d�̨߳Τ�

```csharp
// ? ���ˡG�ϥ� Between �i��d��d��
new FilterRule { Property = "Price", Operator = FilterOperator.Between, Value = new[] { 100m, 1000m } }

// ? �קK�G�ϥΦh�Ӥ������
// new FilterRule { Property = "Price", Operator = FilterOperator.GreaterThanOrEqual, Value = 100m }
// new FilterRule { Property = "Price", Operator = FilterOperator.LessThanOrEqual, Value = 1000m }
```

### 4. ���X�d��

```csharp
// ? ���ˡG�ϥ� In �i��h�Ȭd��
new FilterRule { Property = "CategoryId", Operator = FilterOperator.In, Value = new[] { 1, 2, 3 } }

// ? �䴩�G�V�X null ��
new FilterRule { Property = "Score", Operator = FilterOperator.In, Value = new object[] { 85, 90, 95, null } }
```

## ���~�B�z

### �`�����~�P�ѨM���

```csharp
try
{
    var predicate = FilterBuilder.Build<Employee>(filterGroup);
}
catch (FormatException ex)
{
    // �ƭȮ榡���~
    Console.WriteLine($"Invalid number format: {ex.Message}");
}
catch (OverflowException ex)
{
    // �ƭȷ��X
    Console.WriteLine($"Number overflow: {ex.Message}");
}
catch (ArgumentException ex)
{
    // �Ѽƿ��~�]�p Between �ʤְѼơ^
    Console.WriteLine($"Invalid argument: {ex.Message}");
}
```

### ���ҿ�J���

```csharp
public bool ValidateNumericFilter(FilterRule rule)
{
    switch (rule.Operator)
    {
        case FilterOperator.Between:
        case FilterOperator.NotBetween:
            return rule.Value is IEnumerable enumerable && 
                   enumerable.Cast<object>().Count() == 2;
                   
        case FilterOperator.In:
        case FilterOperator.NotIn:
            return rule.Value is IEnumerable;
            
        default:
            return rule.Value != null || rule.Operator == FilterOperator.Equal || rule.Operator == FilterOperator.NotEqual;
    }
}
```

## �椸���սd��

```csharp
[Fact]
public void Should_Handle_Nullable_Decimal_Correctly()
{
    var employee1 = new Employee { Salary = 50000m };
    var employee2 = new Employee { Salary = null };
    
    var rule = new FilterRule 
    { 
        Property = "Salary", 
        Operator = FilterOperator.GreaterThan, 
        Value = 40000m 
    };
    
    var predicate = FilterBuilder.Build<Employee>(new FilterGroup 
    { 
        Rules = new List<object> { rule } 
    }).Compile();
    
    Assert.True(predicate(employee1));   // 50000 > 40000
    Assert.False(predicate(employee2));  // null ������ > ����
}

[Fact]
public void Should_Handle_Decimal_Precision()
{
    var product = new Product { Price = 19.99m };
    
    var rule = new FilterRule 
    { 
        Property = "Price", 
        Operator = FilterOperator.Equal, 
        Value = 19.99m 
    };
    
    var predicate = FilterBuilder.Build<Product>(new FilterGroup 
    { 
        Rules = new List<object> { rule } 
    }).Compile();
    
    Assert.True(predicate(product));
}
```

## �������v

### v1.0.83
- ? �s�W���㪺 nullable �ƭ������䴩
- ? �ﵽ�����ഫ�w���ʡ]TryParse ����^
- ? �״_ Between/NotBetween �_�w�޿�
- ? �״_ In/NotIn �}�C�����ഫ
- ? �s�W decimal? ������ծר�

### v1.0.82
- ? �򥻼ƭ������䴩
- ? ��¦ nullable �䴩

---

## �����귽

- [�D�n����](README.md)
- [API �Ѧ�](API_REFERENCE.md)
- [���ծר�](../DynamicPredicate.Tests/Builders/FilterBuilderTests.cs)
- [�d�ҵ{���X](../DynamicPredicate.Tests/Examples/)

---

�����i���A�w�ﴣ�Ѧ^�X�I