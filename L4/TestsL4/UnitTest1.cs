using L4.Hashing;

namespace TestsL4;

public class UnitTest1
{
    [Fact]
    public void HashCalculator_ComputesExpectedNumericValue_AndAddress()
    {
        var calculator = new RussianTwoLetterHashCalculator();

        var numericValue = calculator.ComputeNumericValue("Роман");
        var hashAddress = calculator.ComputeAddress("Роман", 20);

        Assert.Equal(576, numericValue);
        Assert.Equal(16, hashAddress);
    }

    [Fact]
    public void HashCalculator_UsesPaddingLetter_WhenKeyHasOneLetter()
    {
        var calculator = new RussianTwoLetterHashCalculator();

        var value = calculator.ComputeNumericValue("Я");

        Assert.Equal(1056, value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("A")]
    public void HashCalculator_Throws_ForInvalidKey(string key)
    {
        var calculator = new RussianTwoLetterHashCalculator();

        Assert.Throws<ArgumentException>(() => calculator.ComputeNumericValue(key));
    }

    [Fact]
    public void HashCalculator_Throws_ForInvalidCapacity()
    {
        var calculator = new RussianTwoLetterHashCalculator();

        Assert.Throws<ArgumentOutOfRangeException>(() => calculator.ComputeAddress("Роман", 0));
    }

    [Fact]
    public void HashTable_Throws_WhenCapacityIsTooSmall()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HashTable(19));
    }

    [Fact]
    public void Add_AndTryGet_Work_WithTrimmedAndCaseInsensitiveKeys()
    {
        var table = new HashTable();

        table.Add("  Роман  ", "  Эпическая проза  ");

        var found = table.TryGet("роман", out var record);

        Assert.True(found);
        Assert.Equal("Роман", record.Key);
        Assert.Equal("Эпическая проза", record.Data);
    }

    [Fact]
    public void Add_Throws_WhenDuplicateKeyExists()
    {
        var table = new HashTable();
        table.Add("Роман", "Первое значение");

        var action = () => table.Add("РОМАН", "Второе значение");

        Assert.Throws<InvalidOperationException>(action);
    }

    [Fact]
    public void Add_Throws_ForInvalidData()
    {
        var table = new HashTable();

        Assert.Throws<ArgumentException>(() => table.Add("Роман", " "));
    }

    [Fact]
    public void Update_ChangesExistingRecord()
    {
        var table = new HashTable();
        table.Add("Роман", "Старое значение");

        table.Update("РОМАН", "Новое значение");
        var found = table.TryGet("роман", out var record);

        Assert.True(found);
        Assert.Equal("Новое значение", record.Data);
    }

    [Fact]
    public void Update_Throws_WhenRecordDoesNotExist()
    {
        var table = new HashTable();

        var action = () => table.Update("Роман", "Текст");

        Assert.Throws<KeyNotFoundException>(action);
    }

    [Fact]
    public void Update_Throws_ForInvalidData()
    {
        var table = new HashTable();
        table.Add("Роман", "Текст");

        Assert.Throws<ArgumentException>(() => table.Update("Роман", " "));
    }

    [Fact]
    public void Delete_MarksRecordDeleted_AndAllowsRestore()
    {
        var table = new HashTable();
        table.Add("Роман", "Старые данные");

        var firstDelete = table.Delete("Роман");
        var secondDelete = table.Delete("Роман");
        var missingAfterDelete = table.TryGet("Роман", out _);

        table.Add("Роман", "Новые данные");
        var restored = table.TryGet("Роман", out var restoredRecord);

        Assert.True(firstDelete);
        Assert.False(secondDelete);
        Assert.False(missingAfterDelete);
        Assert.True(restored);
        Assert.Equal("Новые данные", restoredRecord.Data);
        Assert.Equal(1, table.Count);
    }

    [Fact]
    public void Delete_ReturnsFalse_WhenRecordDoesNotExist()
    {
        var table = new HashTable();

        var deleted = table.Delete("Роман");

        Assert.False(deleted);
    }

    [Fact]
    public void Contains_ReturnsExpectedResult()
    {
        var table = new HashTable();
        table.Add("Роман", "Текст");

        Assert.True(table.Contains("роман"));
        Assert.False(table.Contains("поэма"));
    }

    [Fact]
    public void LoadFactor_IsCalculatedByActiveRowsOnly()
    {
        var table = new HashTable();
        table.Add("Роман", "Текст");
        table.Add("Поэма", "Текст");
        table.Add("Сюжет", "Текст");
        table.Delete("Поэма");

        var loadFactor = table.GetLoadFactor();

        Assert.Equal(2d / table.Capacity, loadFactor, 8);
    }

    [Fact]
    public void GetRows_SetsCollisionFlags_AndDeletionFlags()
    {
        var table = new HashTable();
        table.Add("Роман", "Текст");
        table.Add("Романс", "Текст");
        table.Add("Роль", "Текст");
        table.Delete("Роль");

        var rows = table.GetRows();
        var collisionBucket = table.Compute("Роман").HashAddress;
        var bucketRows = rows.Where(row => row.BucketIndex == collisionBucket).ToList();
        var deletedRow = bucketRows.Single(row => row.Key == "Роль");

        Assert.Equal(3, bucketRows.Count);
        Assert.All(bucketRows, row => Assert.True(row.Collision));
        Assert.True(deletedRow.Deleted);
        Assert.False(deletedRow.Occupied);
    }

    [Fact]
    public void GetRows_FormatsPointers_ForRightLeftAndBothChildren()
    {
        var rightOnlyTable = new HashTable();
        rightOnlyTable.Add("Роа", "1");
        rightOnlyTable.Add("Роб", "2");
        var rightRoot = rightOnlyTable.GetRows().Single(row => row.Key == "Роа");

        var leftOnlyTable = new HashTable();
        leftOnlyTable.Add("Роб", "1");
        leftOnlyTable.Add("Роа", "2");
        var leftRoot = leftOnlyTable.GetRows().Single(row => row.Key == "Роб");

        var twoChildrenTable = new HashTable();
        twoChildrenTable.Add("Роа", "1");
        twoChildrenTable.Add("Роб", "2");
        twoChildrenTable.Add("Ров", "3");
        var rootWithTwoChildren = twoChildrenTable.GetRows().Single(row => row.Key == "Роб");
        var leaf = twoChildrenTable.GetRows().Single(row => row.Key == "Роа");

        Assert.StartsWith("R:", rightRoot.OverflowPointer);
        Assert.StartsWith("L:", leftRoot.OverflowPointer);
        Assert.Contains("L:", rootWithTwoChildren.OverflowPointer);
        Assert.Contains("R:", rootWithTwoChildren.OverflowPointer);
        Assert.Equal(HashTable.EmptyPointer, leaf.OverflowPointer);
    }

    [Theory]
    [MemberData(nameof(GetAvlScenarios))]
    public void AvlBalancing_ProducesStableRoot_ForAllRotationTypes(string[] keysInInsertOrder, string expectedRoot)
    {
        var table = new HashTable();

        foreach (var key in keysInInsertOrder)
        {
            table.Add(key, key);
        }

        var address = table.Compute("Роа").HashAddress;
        var bucket = table.GetBucketRows().Single(row => row.BucketIndex == address);

        Assert.Equal(expectedRoot, bucket.RootKey);
    }

    [Fact]
    public void Compute_TrimsInputKey()
    {
        var table = new HashTable();

        var first = table.Compute("  Роман");
        var second = table.Compute("Роман");

        Assert.Equal(first.NumericValue, second.NumericValue);
        Assert.Equal(first.HashAddress, second.HashAddress);
    }

    [Fact]
    public void Factory_CreatesTable_WithTaskConstraints()
    {
        var table = LiteratureHashTableFactory.CreatePreFilledTable();
        var activeRows = table.GetRows().Where(row => row.Occupied).ToList();
        var collisionCount = table.GetBucketRows().Sum(row => Math.Max(0, row.ActiveCount - 1));
        var hasChainOfThree = table.GetBucketRows().Any(row => row.ActiveCount >= 3);

        Assert.True(table.Capacity >= 20);
        Assert.True(table.Count >= 10);
        Assert.True(activeRows.Count >= 10);
        Assert.True(collisionCount >= 2);
        Assert.True(hasChainOfThree);
    }

    [Fact]
    public void ReportBuilder_BuildsReadableReport()
    {
        var table = LiteratureHashTableFactory.CreatePreFilledTable();

        var report = HashTableReportBuilder.Build(table);

        Assert.Contains("ХЕШ-ТАБЛИЦА", report);
        Assert.Contains("Коэффициент заполнения", report);
        Assert.Contains("№ | Root | Active | Total | C", report);
        Assert.Contains("h | ID | V | C | U | T | L | D | Po | Pi", report);
    }

    [Fact]
    public void ReportBuilder_Throws_WhenTableIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => HashTableReportBuilder.Build(null!));
    }

    public static IEnumerable<object[]> GetAvlScenarios()
    {
        yield return [new[] { "Роа", "Роб", "Ров" }, "Роб"]; // RR
        yield return [new[] { "Ров", "Роб", "Роа" }, "Роб"]; // LL
        yield return [new[] { "Ров", "Роа", "Роб" }, "Роб"]; // LR
        yield return [new[] { "Роа", "Ров", "Роб" }, "Роб"]; // RL
    }
}