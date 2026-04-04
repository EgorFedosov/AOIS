namespace L4.Hashing;

internal sealed class HashTableEntry(string key, string data, int numericValue, int hashAddress)
{
    public string Key { get; } = key;

    public string Data { get; private set; } = data;

    public int NumericValue { get; } = numericValue;

    public int HashAddress { get; } = hashAddress;

    public bool IsDeleted { get; private set; }

    public bool IsOccupied => !IsDeleted;

    public void UpdateData(string data)
    {
        Data = data;
    }

    public bool MarkDeleted()
    {
        if (IsDeleted)
        {
            return false;
        }

        IsDeleted = true;
        return true;
    }

    public void Restore(string data)
    {
        Data = data;
        IsDeleted = false;
    }
}