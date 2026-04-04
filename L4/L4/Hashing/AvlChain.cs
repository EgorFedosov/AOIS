namespace L4.Hashing;

internal enum InsertOutcome
{
    Inserted,
    Restored,
    Duplicate
}

internal readonly record struct ChainNodeSnapshot(
    HashTableEntry Entry,
    string? LeftKey,
    string? RightKey,
    bool IsLeaf);

internal sealed class AvlChain(StringComparer comparer)
{
    private Node? _root;

    public int ActiveCount { get; private set; }

    public int TotalCount { get; private set; }

    public string? RootKey => _root?.Entry.Key;

    public InsertOutcome Insert(string key, string data, int numericValue, int hashAddress)
    {
        var (newRoot, outcome) = Insert(_root, key, data, numericValue, hashAddress);
        _root = newRoot;

        switch (outcome)
        {
            case InsertOutcome.Inserted:
                ActiveCount++;
                TotalCount++;
                break;
            case InsertOutcome.Restored:
                ActiveCount++;
                break;
            case InsertOutcome.Duplicate:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return outcome;
    }

    public bool TryGetActive(string key, out HashTableEntry entry)
    {
        var node = FindNode(_root, key);
        if (node is not null && node.Entry.IsOccupied)
        {
            entry = node.Entry;
            return true;
        }

        entry = null!;
        return false;
    }

    public bool TryUpdate(string key, string data)
    {
        var node = FindNode(_root, key);
        if (node is null || !node.Entry.IsOccupied)
        {
            return false;
        }

        node.Entry.UpdateData(data);
        return true;
    }

    public bool TryDelete(string key)
    {
        var node = FindNode(_root, key);
        if (node is null)
        {
            return false;
        }

        if (!node.Entry.MarkDeleted())
        {
            return false;
        }

        ActiveCount--;
        return true;
    }

    public IReadOnlyList<ChainNodeSnapshot> GetSnapshots()
    {
        var snapshots = new List<ChainNodeSnapshot>();
        TraverseInOrder(_root, snapshots);
        return snapshots;
    }

    private (Node NewRoot, InsertOutcome Outcome) Insert(
        Node? current,
        string key,
        string data,
        int numericValue,
        int hashAddress)
    {
        if (current is null)
        {
            var entry = new HashTableEntry(key, data, numericValue, hashAddress);
            return (new Node(entry), InsertOutcome.Inserted);
        }

        var comparison = comparer.Compare(key, current.Entry.Key);

        switch (comparison)
        {
            case < 0:
            {
                var (newLeft, outcome) = Insert(current.Left, key, data, numericValue, hashAddress);
                current.Left = newLeft;
                return (Balance(current), outcome);
            }
            case > 0:
            {
                var (newRight, outcome) = Insert(current.Right, key, data, numericValue, hashAddress);
                current.Right = newRight;
                return (Balance(current), outcome);
            }
        }

        if (!current.Entry.IsDeleted) return (current, InsertOutcome.Duplicate);
        current.Entry.Restore(data);
        return (current, InsertOutcome.Restored);
    }

    private Node? FindNode(Node? current, string key)
    {
        var node = current;
        while (node is not null)
        {
            var comparison = comparer.Compare(key, node.Entry.Key);
            if (comparison == 0)
            {
                return node;
            }

            node = comparison < 0 ? node.Left : node.Right;
        }

        return null;
    }

    private static void TraverseInOrder(Node? current, ICollection<ChainNodeSnapshot> snapshots)
    {
        while (true)
        {
            if (current is null)
            {
                return;
            }

            TraverseInOrder(current.Left, snapshots);

            snapshots.Add(new ChainNodeSnapshot(current.Entry, current.Left?.Entry.Key, current.Right?.Entry.Key,
                current.Left is null && current.Right is null));

            current = current.Right;
        }
    }

    private static Node Balance(Node node)
    {
        UpdateHeight(node);
        var balanceFactor = GetBalanceFactor(node);

        switch (balanceFactor)
        {
            case < -1:
            {
                if (GetBalanceFactor(node.Left!) > 0)
                {
                    node.Left = RotateLeft(node.Left!);
                }

                return RotateRight(node);
            }
            case > 1:
            {
                if (GetBalanceFactor(node.Right!) < 0)
                {
                    node.Right = RotateRight(node.Right!);
                }

                return RotateLeft(node);
            }
            default:
                return node;
        }
    }

    private static Node RotateRight(Node node)
    {
        var pivot = node.Left!;
        node.Left = pivot.Right;
        pivot.Right = node;

        UpdateHeight(node);
        UpdateHeight(pivot);

        return pivot;
    }

    private static Node RotateLeft(Node node)
    {
        var pivot = node.Right!;
        node.Right = pivot.Left;
        pivot.Left = node;

        UpdateHeight(node);
        UpdateHeight(pivot);

        return pivot;
    }

    private static void UpdateHeight(Node node)
    {
        node.Height = Math.Max(GetHeight(node.Left), GetHeight(node.Right)) + 1;
    }

    private static int GetBalanceFactor(Node node)
    {
        return GetHeight(node.Right) - GetHeight(node.Left);
    }

    private static int GetHeight(Node? node)
    {
        return node?.Height ?? 0;
    }

    private sealed class Node(HashTableEntry entry)
    {
        public HashTableEntry Entry { get; } = entry;

        public Node? Left { get; set; }

        public Node? Right { get; set; }

        public int Height { get; set; } = 1;
    }
}