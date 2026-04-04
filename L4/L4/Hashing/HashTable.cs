namespace L4.Hashing;

public sealed class HashTable
{
    private const int MinimumCapacity = 20;
    public const string EmptyPointer = "-";

    private readonly AvlChain[] _buckets;
    private readonly IHashCalculator _hashCalculator;

    public HashTable(
        int capacity = MinimumCapacity,
        IHashCalculator? hashCalculator = null,
        StringComparer? keyComparer = null)
    {
        if (capacity < MinimumCapacity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                $"Размер таблицы должен быть не меньше {MinimumCapacity}.");
        }

        _hashCalculator = hashCalculator ?? new RussianTwoLetterHashCalculator();

        var comparer = keyComparer ?? StringComparer.OrdinalIgnoreCase;
        _buckets = new AvlChain[capacity];
        for (var index = 0; index < capacity; index++)
        {
            _buckets[index] = new AvlChain(comparer);
        }
    }

    public int Capacity => _buckets.Length;

    public int Count { get; private set; }

    public HashComputation Compute(string key)
    {
        var normalizedKey = NormalizeKey(key);
        return ComputeInternal(normalizedKey);
    }

    public void Add(string key, string data)
    {
        var normalizedKey = NormalizeKey(key);
        var normalizedData = NormalizeData(data);
        var computation = ComputeInternal(normalizedKey);

        var outcome = _buckets[computation.HashAddress].Insert(
            normalizedKey,
            normalizedData,
            computation.NumericValue,
            computation.HashAddress);

        if (outcome == InsertOutcome.Duplicate)
        {
            throw new InvalidOperationException(
                $"Запись с ключом \"{normalizedKey}\" уже существует.");
        }

        Count++;
    }

    public bool TryGet(string key, out HashRecord record)
    {
        var normalizedKey = NormalizeKey(key);
        var computation = ComputeInternal(normalizedKey);

        if (_buckets[computation.HashAddress].TryGetActive(normalizedKey, out var entry))
        {
            record = new HashRecord(entry.Key, entry.Data);
            return true;
        }

        record = default;
        return false;
    }

    public void Update(string key, string data)
    {
        var normalizedKey = NormalizeKey(key);
        var normalizedData = NormalizeData(data);
        var computation = ComputeInternal(normalizedKey);

        if (!_buckets[computation.HashAddress].TryUpdate(normalizedKey, normalizedData))
        {
            throw new KeyNotFoundException(
                $"Нельзя обновить ключ \"{normalizedKey}\", потому что запись не найдена.");
        }
    }

    public bool Delete(string key)
    {
        var normalizedKey = NormalizeKey(key);
        var computation = ComputeInternal(normalizedKey);
        var isDeleted = _buckets[computation.HashAddress].TryDelete(normalizedKey);

        if (isDeleted)
        {
            Count--;
        }

        return isDeleted;
    }

    public bool Contains(string key)
    {
        return TryGet(key, out _);
    }

    public double GetLoadFactor()
    {
        return Count / (double)Capacity;
    }

    public IReadOnlyList<HashBucketRow> GetBucketRows()
    {
        var rows = new List<HashBucketRow>(Capacity);

        for (var bucketIndex = 0; bucketIndex < Capacity; bucketIndex++)
        {
            var bucket = _buckets[bucketIndex];
            rows.Add(
                new HashBucketRow(
                    bucketIndex,
                    bucket.RootKey ?? EmptyPointer,
                    bucket.ActiveCount,
                    bucket.TotalCount,
                    bucket.ActiveCount > 1));
        }

        return rows;
    }

    public IReadOnlyList<HashTableRow> GetRows()
    {
        var rows = new List<HashTableRow>();

        for (var bucketIndex = 0; bucketIndex < Capacity; bucketIndex++)
        {
            var bucket = _buckets[bucketIndex];
            var hasCollision = bucket.ActiveCount > 1;

            rows.AddRange(from snapshot in bucket.GetSnapshots()
                let entry = snapshot.Entry
                select new HashTableRow(BucketIndex: bucketIndex, Key: entry.Key, NumericValue: entry.NumericValue,
                    HashAddress: entry.HashAddress, Collision: hasCollision, Occupied: entry.IsOccupied,
                    Terminal: snapshot.IsLeaf, Link: false, Deleted: entry.IsDeleted,
                    OverflowPointer: FormatPointer(snapshot.LeftKey, snapshot.RightKey), Data: entry.Data));
        }

        return rows;
    }

    private HashComputation ComputeInternal(string normalizedKey)
    {
        var numericValue = _hashCalculator.ComputeNumericValue(normalizedKey);
        var hashAddress = numericValue % Capacity;
        return new HashComputation(numericValue, hashAddress);
    }

    private static string NormalizeKey(string key)
    {
        return string.IsNullOrWhiteSpace(key)
            ? throw new ArgumentException("Ключ не может быть пустым.", nameof(key))
            : key.Trim();
    }

    private static string NormalizeData(string data)
    {
        return string.IsNullOrWhiteSpace(data)
            ? throw new ArgumentException("Данные не могут быть пустыми.", nameof(data))
            : data.Trim();
    }

    private static string FormatPointer(string? leftKey, string? rightKey)
    {
        if (leftKey is null && rightKey is null)
        {
            return EmptyPointer;
        }

        if (leftKey is not null && rightKey is not null)
        {
            return $"L:{leftKey},R:{rightKey}";
        }

        return leftKey is not null ? $"L:{leftKey}" : $"R:{rightKey}";
    }
}