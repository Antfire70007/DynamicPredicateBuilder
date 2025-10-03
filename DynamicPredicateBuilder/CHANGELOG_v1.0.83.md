# DynamicPredicateBuilder v1.0.83 ��s����

## ?? �D�n�\���s

### ? ����䴩 Nullable Decimal ����

�s������ `decimal?` �������ѧ���䴩�A�]�A�G

- ? **���Ȥ��**�G`Equal`, `NotEqual` with null values
- ? **�ƭȤ��**�G`GreaterThan`, `LessThan`, `GreaterThanOrEqual`, `LessThanOrEqual`
- ? **�d��d��**�G`Between`, `NotBetween`
- ? **���X�d��**�G`In`, `NotIn` (�䴩 null ��)
- ? **�����޿�**�G�h���_�w�B�_������

### ?? �j�ƪ������ഫ����

���c�F `ChangeType` ��k�A�s�W�G

- **�w���ഫ**�G�Ҧ��ƭ������ϥ� `TryParse` ��k
- **���T�B�z**�G`decimal`, `double`, `float`, `int`, `long`, `DateTime`
- **��׫O��**�G`decimal` �����O��������
- **���~�w��**�G�קK `InvalidCastException`

### ?? ���n���~�״_

#### 1. In/NotIn �ާ@�ŭ״_
- **���D**�G�}�C�ѼƵL�k���T�ഫ����
- **�ѨM**�G�b `BuildIn` ��k���v�@�ഫ��������
- **�v�T**�G�Ҧ����X�d�߲{�b���ॿ�`�u�@

#### 2. Between/NotBetween �_�w�޿�״_
- **���D**�G`IsNegated=true` �P `NotBetween` �P�ɨϥήɥX�{�����_�w
- **�ѨM**�G�Τ@�_�w�޿�B�z
- **�v�T**�G�����_�w����{�b�欰���T

## ?? �����л\�v����

�s�W **14 �ӱM���� Nullable Decimal ���ծר�**�G

```csharp
? BuildPredicate_NullableDecimal_EqualOperator_WithValue_ShouldWork
? BuildPredicate_NullableDecimal_EqualOperator_WithNull_ShouldWork  
? BuildPredicate_NullableDecimal_NotEqualOperator_WithValue_ShouldWork
? BuildPredicate_NullableDecimal_NotEqualOperator_WithNull_ShouldWork
? BuildPredicate_NullableDecimal_GreaterThanOperator_ShouldWork
? BuildPredicate_NullableDecimal_LessThanOperator_ShouldWork
? BuildPredicate_NullableDecimal_GreaterThanOrEqualOperator_ShouldWork
? BuildPredicate_NullableDecimal_LessThanOrEqualOperator_ShouldWork
? BuildPredicate_NullableDecimal_BetweenOperator_ShouldWork
? BuildPredicate_NullableDecimal_InOperator_ShouldWork
? BuildPredicate_NullableDecimal_InOperator_WithNull_ShouldWork
? BuildPredicate_NullableDecimal_ComplexConditions_ShouldWork
? BuildPredicate_NullableDecimal_WithNegation_ShouldWork
? BuildPredicate_NullableDecimal_MultipleGroups_ShouldWork
```

**�`���ծר�**�G�q 26 �ӼW�[�� **40 ��**�A�[�\�Ҧ��D�n�ϥγ����C

## ?? �ϥνd��

### �� Nullable Decimal �d��

```csharp
// �d�ߦ��~�ꪺ���u
new FilterRule { Property = "Salary", Operator = FilterOperator.GreaterThan, Value = 0 }

// �d�ߨS���~���ƪ����u  
new FilterRule { Property = "Salary", Operator = FilterOperator.Equal, Value = null }

// �d���~��b�d�򤺪����u
new FilterRule { Property = "Salary", Operator = FilterOperator.Between, Value = new[] { 30000m, 80000m } }
```

### �����d�߽d��

```csharp
// �d�߰��~�εL�~��O�������u
var filterGroup = new FilterGroup
{
    LogicalOperator = LogicalOperator.Or,
    Rules = new List<object>
    {
        new FilterRule { Property = "Salary", Operator = FilterOperator.Equal, Value = null },
        new FilterRule { Property = "Salary", Operator = FilterOperator.GreaterThan, Value = 80000m }
    }
};
```

### FilterDictionaryBuilder �d��

```csharp
// �ϥ� Expression �y�k
var query = FilterDictionaryBuilder.QueryBuilder<Employee>()
    .WithLogicalOperator(LogicalOperator.And)
    .GreaterThan(x => x.Salary, 50000m)
    .Between(x => x.Bonus, 1000m, 5000m)
    .Equal(x => x.Commission, null)
    .Build();
```

## ?? �į�ﵽ

- **�O����ϥ�**�G�u�������ഫ�L�{�A��֤����n������إ�
- **����t��**�G�ϥ� `TryParse` �קK�ҥ~�B�z���į�l��
- **�sĶ�ɶ�**�GExpression Tree �غc��[����

## ?? ���ɧ�s

### �s�W����
- **`NUMERIC_TYPES.md`**�G�ԲӪ��ƭ������䴩���n
- **README.md �� 5 �`**�G�䴩�������������
- **���սd��**�G���㪺 nullable decimal �ϥήר�

### ��s���e
- �B��l�䴩�x�}
- Nullable �����S��欰����
- �̨ι�ȫ��n
- �`�����D�ѵ�

## ?? �E�����n

### �q v1.0.82 �ɯ�

**? �L�}�a���ܧ�**�G�{���{���X�L�ݭק�

**?? ��ĳ��s**�G

1. **������������ˬd**�G
```csharp
// ? �ª����ݭn���B�~�ˬd
if (value is decimal decimalValue) 
{
    // ��ʳB�z...
}

// ? �s�����۰ʳB�z
new FilterRule { Property = "Salary", Operator = FilterOperator.Equal, Value = "50000" }
```

2. **�ϥηs�� Nullable �\��**�G
```csharp
// ? �{�b�����䴩
new FilterRule { Property = "Salary", Operator = FilterOperator.In, Value = new object[] { 50000m, null } }
```

## ?? �w�����D�״_

| ���D | ���A | ���� |
|------|------|------|
| Decimal �����ഫ���` | ? �w�״_ | �ϥ� TryParse �w���ഫ |
| In �ާ@�Ű}�C�������~ | ? �w�״_ | ���T�B�z���������ഫ |
| NotBetween �����_�w | ? �w�״_ | �Τ@�_�w�޿�B�z |
| Nullable ����欰���@�P | ? �w�״_ | �зǤ� null �ȳB�z |

## ?? �U�@�B�p�e

### v1.0.84 �w�p�\��
- ?? �䴩 `DateOnly` �M `TimeOnly` (.NET 6+)
- ?? �W�j `Guid` �����d�ߥ\��
- ?? �s�W�ۭq�B��l�X�i����
- ?? �į��Ǵ��դu��

### �����W��
- ?? �d�ߧ֨�����
- ?? �D�P�B�d�ߤ䴩
- ?? �h�y�����~�T��
- ?? �d�ߤ��R�u��

## ?? �P��

�S�O�P�ª��s�^�X�A���ڭ̯���ѧO�í״_�o�ǭ��n���D�C

---

## ?? �p����T

- **GitHub**: [DynamicPredicateBuilder](https://github.com/Antfire70007/DynamicPredicateBuilder)
- **Issues**: [�^�����D](https://github.com/Antfire70007/DynamicPredicateBuilder/issues)
- **NuGet**: [�M�󭶭�](https://www.nuget.org/packages/DynamicPredicateBuilder/)

---

**����**: v1.0.83  
**�o�����**: 2024-12-19  
**�ۮe��**: .NET 7.0, .NET 8.0, .NET 9.0