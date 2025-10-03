# FilterDictionaryBuilder �ϥλ���

`FilterDictionaryBuilder` �O�@�Ӥ䴩 Fluent API ���L�o����غc���A�i�H���A�Χ��[���覡�إ߽������d�߱���C�{�b�䴩�j���O�� Expression �y�k�I

## �ֳt�}�l

### �򥻨ϥΤ覡 (�r�ꪩ��)

```csharp
// ²��d��
var query = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
    .WithLogicalOperator(LogicalOperator.Or)
    .Like(nameof(LandDTO.LandNo), "A123")
    .Like(nameof(LandDTO.CityCode), "TPE")
    .Build();
```

### �򥻨ϥΤ覡 (Expression ����) ? �s�\��

```csharp
// �j���O�d�� - ���sĶ�ɴ��ˬd
var query = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
    .WithLogicalOperator(LogicalOperator.Or)
    .Like(x => x.LandNo, "A123")
    .Like(x => x.CityCode, "TPE")
    .Build();
```

### �_���d�� (Expression ����)

```csharp
// �����_���d��
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

## API �Ѧ�

### �إ� Builder

```csharp
// ��k1: �R�A�u�t��k
var builder = FilterDictionaryBuilder.QueryBuilder<MyEntity>();

// ��k2: �t�@���R�A�u�t��k
var builder = FilterDictionaryBuilder.Create<MyEntity>();

// ��k3: �x�����O���R�A��k
var builder = FilterDictionaryBuilder<MyEntity>.Create();
```

### �]�w�s���ݩ�

```csharp
builder
    .WithLogicalOperator(LogicalOperator.And)    // �]�w�s�դ��޿�B��l
    .WithInterOperator(LogicalOperator.Or)       // �]�w�P�U�@�s�ժ��s���B��l
    .Negate(true);                               // �_�w��Ӹs��
```

### �򥻱����k

#### �r�ꪩ��
```csharp
builder
    .Add(property, FilterOperator.Equal, value, isNegated)  // �q�Τ�k
    .Equal(property, value, isNegated)                      // ����
    .Like(property, value, isNegated)                       // �ҽk���
    .Contains(property, value, isNegated)                   // �]�t
    .In(property, values, isNegated)                        // �b���X��
    .Between(property, min, max, isNegated)                 // �b�d��
    .GreaterThan(property, value, isNegated)                // �j��
    .LessThan(property, value, isNegated);                  // �p��
```

#### Expression ���� ? �s�\��
```csharp
builder
    .Add(x => x.Property, FilterOperator.Equal, value, isNegated)  // �q�Τ�k
    .Equal(x => x.Property, value, isNegated)                      // ����
    .Like(x => x.Property, value, isNegated)                       // �ҽk���
    .Contains(x => x.Property, value, isNegated)                   // �]�t
    .In(x => x.Property, values, isNegated)                        // �b���X��
    .Between(x => x.Property, min, max, isNegated)                 // �b�d��
    .GreaterThan(x => x.Property, value, isNegated)                // �j��
    .LessThan(x => x.Property, value, isNegated)                   // �p��
    .GreaterThanOrEqual(x => x.Property, value, isNegated)         // �j�󵥩�
    .LessThanOrEqual(x => x.Property, value, isNegated)            // �p�󵥩�
    .StartsWith(x => x.Property, value, isNegated)                 // �}�Y�]�t
    .EndsWith(x => x.Property, value, isNegated);                  // �����]�t
```

### �ݩʤ��

#### �r�ꪩ��
```csharp
builder.AddPropertyComparison(
    property, 
    FilterOperator.Equal, 
    compareToProperty, 
    isNegated
);
```

#### Expression ���� ? �s�\��
```csharp
builder.AddPropertyComparison(
    x => x.Property1, 
    FilterOperator.Equal, 
    x => x.Property2, 
    isNegated
);
```

### �_���s��

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

### ���G��X

```csharp
// ��X���r��
Dictionary<string, object> dict = builder.Build();

// ��X�� FilterGroup
FilterGroup group = builder.ToFilterGroup();

// �����ഫ
Dictionary<string, object> dict = builder;  // �۰��ഫ���r��
FilterGroup group = builder;                // �۰��ഫ�� FilterGroup
```

## ��νd��

### �d��1: �ŦX��l�ݨD��²�ƻy�k

**�쥻���g�k:**
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

**�s���g�k (�r�ꪩ��):**
```csharp
var query = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
    .WithLogicalOperator(LogicalOperator.Or)
    .Like(nameof(LandDTO.LandNo), landNo)
    .Like(nameof(LandDTO.CityCode), city)
    .Build();
```

**�s���g�k (Expression ����) ? ����:**
```csharp
var query = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
    .WithLogicalOperator(LogicalOperator.Or)
    .Like(x => x.LandNo, landNo)
    .Like(x => x.CityCode, city)
    .Build();
```

### �d��2: �������h�h�_���d�� (Expression ����)

```csharp
var query = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
    .WithLogicalOperator(LogicalOperator.And)
    .Compare(LogicalOperator.Or, mainConditions => mainConditions
        // �D�n�����
        .Like(x => x.LandNo, "A123")
        .In(x => x.CityCode, new[] { "TPE", "KHH", "TCH" })
        
        // �S�����l��
        .Compare(LogicalOperator.And, specialConditions => specialConditions
            .Equal(x => x.CaseOwner, "VIP Owner")
            .Between(x => x.Price, 5000000, 50000000)
        )
    )
    // �ɶ��d�򭭨�
    .Compare(LogicalOperator.And, timeConditions => timeConditions
        .GreaterThan(x => x.CreateDate, DateTime.Now.AddMonths(-6))
        .LessThan(x => x.CreateDate, DateTime.Now)
    )
    .Build();
```

### �d��3: �_�w���� (Expression ����)

```csharp
var query = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
    .Equal(x => x.CaseOwner, "Blacklisted Owner", isNegated: true)  // ���O�S�w�~�D
    .Compare(LogicalOperator.Or, priceRules => priceRules
        .LessThan(x => x.Price, 500000)
        .GreaterThan(x => x.Price, 10000000),
        isNegated: true)  // ���b����d��
    .Build();
```

### �d��4: �V�X�ϥ� Expression �M�r�ꪩ��

```csharp
var query = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
    .WithLogicalOperator(LogicalOperator.And)
    // Expression ���� - �A�Ω�w�����j���O�ݩ�
    .Like(x => x.LandNo, "A123")
    .GreaterThan(x => x.Price, 1000000)
    // �r�ꪩ�� - �A�Ω�ʺA�ݩʦW��
    .Equal(nameof(LandDTO.CaseOwner), "John Doe")
    .Contains("DynamicProperty", "value")  // ���]���ʺA�ݩ�
    .Build();
```

### �d��5: �ݩʹ��ݩʤ�� (Expression ����)

```csharp
var query = FilterDictionaryBuilder.QueryBuilder<LandDTO>()
    .WithLogicalOperator(LogicalOperator.And)
    // �ݩʹ��ݩʤ��
    .AddPropertyComparison(x => x.LandNo, FilterOperator.Equal, x => x.CityCode)
    // ��L����
    .GreaterThan(x => x.Price, 1000000)
    .Build();
```

## �P FilterBuilder ��X

�إߪ��d�߱���i�H�����P�{���� `FilterBuilder` ��X�ϥΡG

```csharp
// �إ߬d�߱���
FilterGroup filterGroup = FilterDictionaryBuilder.QueryBuilder<MyEntity>()
    .Like("Name", "test")
    .GreaterThan("Age", 18)
    .ToFilterGroup();

// �ϥ� FilterBuilder �إ� Expression
var expression = FilterBuilder.Build<MyEntity>(filterGroup);

// ���Ψ� LINQ �d��
var results = dbContext.MyEntities.Where(expression).ToList();
```

## �u��

1. **�iŪ�ʨ�**: Fluent API ���{���X���[����
2. **�����w��**: �x���䴩�M Expression �y�k���ѽsĶ�ɴ��������ˬd ?
3. **IntelliSense �䴩**: Expression �������ѧ��㪺 IDE �۰ʧ��� ?
4. **���c�͵�**: �ϥ� Expression ���ݩʭ��s�R�W�|�۰ʧ�s ?
5. **�\�৹��**: �䴩�Ҧ��{�����L�o�B��l�M�_�����c
6. **�V��ۮe**: �����ۮe�{�����r��榡�M FilterGroup
7. **�u�ʰ�**: �䴩�����ഫ�M�h�ثإߤ覡
8. **���y�k�䴩**: �P�ɤ䴩�j���O Expression �M�ʺA�r��y�k ?
9. **�������**: �M���� API ���c�K��椸����

## ��ɨϥέ��ػy�k

### Expression ���� (����) ?
- ? �ݩʦW�٦b�sĶ�ɴ��w��
- ? �ݭn IntelliSense �䴩
- ? �ݭn���c�w����
- ? �ζ����n�j���O�{���X

### �r�ꪩ��
- ? �ʺA�ݩʦW�� (�q�]�w��Ū����)
- ? �ݭn�V��ۮe�µ{���X
- ? �ݩʦW�٨Ӧۥ~���ӷ� (�p API)

## �`�N�ƶ�

- **Expression ����**: �䴩²���ݩʩM�_���ݩ� (�p `x => x.User.Name`)
- **�r�ꪩ��**: �ݩʦW�٫�ĳ�ϥ� `nameof()` �B��l�T�O�����w��
- �_���h�ŨS������A����ĳ���n�L�`�H�����iŪ��
- �Ҧ���k���䴩 `isNegated` �ѼƨӤ������
- Expression �M�r�ꪩ���i�H�b�P�@�Ӭd�ߤ��V�X�ϥ�