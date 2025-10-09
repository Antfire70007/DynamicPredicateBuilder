# ArrayLike �\��ܨ�

## ���z
�� `FilterDictionaryBuilder` �s�W���㪺�}�C������k�䴩�A�]�A `ArrayLike` �M��L�Ҧ��B��l�C

## �s�W���}�C������k

### �򥻤�k
- `ArrayEqual` - �}�C����������w��
- `ArrayNotEqual` - �}�C������������w��
- `ArrayLike` - �}�C�����ŦX Like ���� ? **�s�W**
- `ArrayNotLike` - �}�C�������ŦX Like ����
- `ArrayContains` - �}�C�����]�t���w�r��
- `ArrayNotContains` - �}�C�������]�t���w�r��

### �����k
- `ArrayGreaterThan` - �}�C�����j����w��
- `ArrayGreaterThanOrEqual` - �}�C�����j�󵥩���w��
- `ArrayLessThan` - �}�C�����p����w��
- `ArrayLessThanOrEqual` - �}�C�����p�󵥩���w��

### �r���k
- `ArrayStartsWith` - �}�C�����H���w�r��}�Y
- `ArrayEndsWith` - �}�C�����H���w�r�굲��

### �d���k
- `ArrayIn` - �}�C�����b���w���X��
- `ArrayNotIn` - �}�C�������b���w���X��
- `ArrayBetween` - �}�C�����b���w�d��
- `ArrayNotBetween` - �}�C�������b���w�d��

### �s�b�ʤ�k
- `ArrayAny` - �}�C�������󤸯��ŦX����
- `ArrayNotAny` - �}�C���S�����󤸯��ŦX����

## �ϥνd��

### 1. ArrayLike �r��ǰt
```csharp
using var context = CreateContractTestContext();

// �d��خצW�٥]�t "���v" ���X��
var filterGroup = FilterDictionaryBuilder.QueryBuilder<Contract>()
    .WithLogicalOperator(LogicalOperator.And)
    .ArrayLike(c => c.BuildContracts, bc => bc.Build.Name, "���v")
    .ToFilterGroup();

var predicate = FilterBuilder.Build<Contract>(filterGroup);
var results = context.Contracts
    .Include(c => c.BuildContracts)
    .ThenInclude(bc => bc.Build)
    .Where(predicate)
    .ToList();

// ���G�G��� "���v�ʶR�X��"
```

### 2. ArrayContains �r��]�t
```csharp
// �d��خצ�m�]�t "�x�_��" ���X��
var filterGroup = FilterDictionaryBuilder.QueryBuilder<Contract>()
    .ArrayStartsWith(c => c.BuildContracts, bc => bc.Build.Location, "�x�_��")
    .ToFilterGroup();
```

### 3. ArrayIn ���X�d��
```csharp
// �d�� AptId �b���w���X�����X��
var filterGroup = FilterDictionaryBuilder.QueryBuilder<Contract>()
    .ArrayIn(c => c.BuildContracts, bc => bc.Build.AptId, new object[] { 1001L, 1002L })
    .ToFilterGroup();
```

### 4. ArrayBetween �d��d��
```csharp
// �d�� AptId �b 1001-1003 �d�򤺪��X��
var filterGroup = FilterDictionaryBuilder.QueryBuilder<Contract>()
    .ArrayBetween(c => c.BuildContracts, bc => bc.Build.AptId, 1001L, 1003L)
    .ToFilterGroup();
```

### 5. �����զX�d��
```csharp
// �����d�ߡG���X�h�ذ}�C������k
var filterGroup = FilterDictionaryBuilder.QueryBuilder<Contract>()
    .WithLogicalOperator(LogicalOperator.And)
    .ArrayGreaterThan(c => c.BuildContracts, bc => bc.Build.AptId, 1000L)
    .ArrayLike(c => c.BuildContracts, bc => bc.Build.Location, "�x�_��")
    .ArrayNotEqual(c => c.BuildContracts, bc => bc.ContractType, "�ۦ�")
    .Contains(c => c.Name, "�ʶR")
    .ToFilterGroup();

var predicate = FilterBuilder.Build<Contract>(filterGroup);
var results = context.Contracts
    .Include(c => c.BuildContracts)
    .ThenInclude(bc => bc.Build)
    .Where(predicate)
    .ToList();
```

## �}�C�����y�k

�Ҧ��}�C������k���|�۰ʥͦ����T���y�k�G
```
{���X�ݩ�}[].{�ؼ��ݩ�}
```

�Ҧp�G
- `BuildContracts[].Build.AptId`
- `BuildContracts[].Build.Name`
- `BuildContracts[].Build.Location`

## �䴩���������

- �r�������G�䴩 `Like`�B`Contains`�B`StartsWith`�B`EndsWith` ���r��ާ@
- �ƭ������G�䴩�Ҧ�����M�d��ާ@
- �i�������G���T�B�z `null` ��
- ���X�����G�䴩 `In`�B`NotIn` �ާ@

## �����л\

�Ҧ��s�W���}�C������k���w�g�L������աA�]�A�G
- �򥻥\�����
- �r��ާ@����
- ����ާ@����
- ���žާ@����
- �����զX�d�ߴ���

�`�p���աG116 �ӡA�����q�L ?