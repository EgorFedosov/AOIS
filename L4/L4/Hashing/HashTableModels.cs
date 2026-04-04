namespace L4.Hashing;

public readonly record struct HashComputation(int NumericValue, int HashAddress);

public readonly record struct HashRecord(string Key, string Data);

public readonly record struct HashTableRow(
    int BucketIndex,
    string Key,
    int NumericValue,
    int HashAddress,
    bool Collision,
    bool Occupied,
    bool Terminal,
    bool Link,
    bool Deleted,
    string OverflowPointer,
    string Data);

public readonly record struct HashBucketRow(
    int BucketIndex,
    string RootKey,
    int ActiveCount,
    int TotalCount,
    bool Collision);

public readonly record struct SeedRecord(string Key, string Data);