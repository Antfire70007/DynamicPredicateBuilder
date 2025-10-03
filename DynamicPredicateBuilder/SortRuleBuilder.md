# SortRuleBuilder �ϥλ���

`SortRuleBuilder` �O�@�Ӥ䴩 Fluent API ���ƧǳW�h�غc���A�i�H���A�Χ��[���覡�إ߱ƧǱ���C�䴩�j���O�� Expression �y�k�I

## �ֳt�}�l

### �򥻨ϥΤ覡 (�r�ꪩ��)

```csharp
// ²��Ƨ�
var sortRules = SortRuleBuilder.SortBuilder<UserDTO>()
    .Ascending(nameof(UserDTO.Username))
    .Descending(nameof(UserDTO.CreatedDate))
    .Build();
```

### �򥻨ϥΤ覡 (Expression ����)

```csharp
// �j���O�Ƨ� - ���sĶ�ɴ��ˬd
var sortRules = SortRuleBuilder.SortBuilder<UserDTO>()
    .Ascending(x => x.Username)
    .Descending(x => x.CreatedDate)
    .Build();
```

### �h�h�űƧ� (Expression ����)

```csharp
// �D�n�ƧǱ���P���n�ƧǱ���
var sortRules = SortRuleBuilder.SortBuilder<UserDTO>()
    .Ascending(x => x.Department)      // ���������ɧǱƧ�
    .ThenBy(x => x.Username)           // �ۦP�������Τ�W�ɧǱƧ�
    .ThenByDescending(x => x.Age)      // �ۦP�Τ�W���~�֭��ǱƧ�
    .Build();
```

## API �Ѧ�

### �إ� Builder

```csharp
// ��k1: �R�A�u�t��k
var builder = SortRuleBuilder.SortBuilder<MyEntity>();

// ��k2: �t�@���R�A�u�t��k
var builder = SortRuleBuilder.Create<MyEntity>();

// ��k3: �x�����O���R�A��k
var builder = SortRuleBuilder<MyEntity>.Create();
```

### �򥻱ƧǤ�k

#### �r�ꪩ��
```csharp
builder
    .Add(property, descending)          // �q�Τ�k�A�i���w�O�_����
    .Ascending(property)                // �ɧǱƧ�
    .Descending(property)               // ���ǱƧ�
    .ThenBy(property)                   // ���n�ɧǱƧ�
    .ThenByDescending(property);        // ���n���ǱƧ�
```

#### Expression ����
```csharp
builder
    .Add(x => x.Property, descending)   // �q�Τ�k�A�i���w�O�_����
    .Ascending(x => x.Property)         // �ɧǱƧ�
    .Descending(x => x.Property)        // ���ǱƧ�
    .ThenBy(x => x.Property)            // ���n�ɧǱƧ�
    .ThenByDescending(x => x.Property); // ���n���ǱƧ�
```

### ���G��X

```csharp
// ��X���ƧǳW�h�C��
List<SortRule> rules = builder.Build();

// �����ഫ
List<SortRule> rules = builder;         // �۰��ഫ���C��
SortRule[] rulesArray = builder;        // �۰��ഫ���}�C
```

## ��νd��

### �d��1: ��@�ƧǱ���

**�쥻���g�k:**
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

**�s���g�k (�r�ꪩ��):**
```csharp
var sortRules = SortRuleBuilder.SortBuilder<UserDTO>()
    .Ascending(nameof(UserDTO.Username))
    .Build();
```

**�s���g�k (Expression ����) �V ����:**
```csharp
var sortRules = SortRuleBuilder.SortBuilder<UserDTO>()
    .Ascending(x => x.Username)
    .Build();
```

### �d��2: �h�h�űƧ� (Expression ����)

```csharp
// �إ߽������h�h�űƧ�
var sortRules = SortRuleBuilder.SortBuilder<OrderDTO>()
    .Ascending(x => x.Customer.Region)      // �����Ȥ�ϰ�ɧǱƧ�
    .ThenBy(x => x.OrderDate)               // �ۦP�ϰ���q�����ɧǱƧ�
    .ThenByDescending(x => x.TotalAmount)   // �ۦP������`���B���ǱƧ�
    .Build();
```

### �d��3: �P�d�߽ШD���X�ϥ�

```csharp
// �إ߱ƧǳW�h
var sortRules = SortRuleBuilder.SortBuilder<ProductDTO>()
    .Descending(x => x.Price)
    .ThenBy(x => x.Name)
    .Build();

// �إ߹L�o����
var filters = FilterDictionaryBuilder.QueryBuilder<ProductDTO>()
    .GreaterThan(x => x.Stock, 10)
    .Like(x => x.Category, "Electronics")
    .Build();

// �إ߬d�߽ШD
var request = new QueryRequest
{
    Filters = filters,
    SortRules = sortRules,
    PageSize = 20,
    PageIndex = 0
};

// �ϥάd�߽ШD
var result = await _productService.QueryAsync(request);
```

### �d��4: �����P LINQ ��X

```csharp
// �إ߱ƧǳW�h
var sortBuilder = SortRuleBuilder.SortBuilder<User>()
    .Ascending(x => x.LastName)
    .ThenBy(x => x.FirstName);

// �ϥαƧǳW�h�i��d��
IQueryable<User> query = dbContext.Users;

// �w��C�ӱƧǳW�h�̧ǮM��
foreach (var rule in sortBuilder.Build())
{
    query = rule.Descending 
        ? query.AppendOrderByDescending(rule.Property) 
        : query.AppendOrderBy(rule.Property);
}

var results = query.ToList();
```

## �u��

1. **�iŪ�ʨ�**: Fluent API ���{���X���[����
2. **�����w��**: �x���䴩���ѽsĶ�ɴ��������ˬd
3. **�\�৹��**: �䴩�Ҧ��ƧǻݨD�M�������h�h�űƧ�
4. **�V��ۮe**: �����ۮe�{���� SortRule �M��榡
5. **�u�ʰ�**: �䴩�����ഫ�M�h�ثإߤ覡
6. **�������**: �M���� API ���c�K��椸����

## �`�N�ƶ�

- �ƧǳW�h�����ǫܭ��n�A�|�̷ӲK�[���Ƕi���u���űƧ�
- �ݩʦW�٫�ĳ�ϥ� `nameof()` �B��l�� Expression �T�O�����w��
- `ThenBy` �M `ThenByDescending` �b�y�q�W�O���n�ƧǡA���\��W�P `Ascending` �M `Descending` �ۦP