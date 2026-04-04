using System.Collections.ObjectModel;

namespace L4.Hashing;

public sealed class RussianTwoLetterHashCalculator : IHashCalculator
{
    private const int AlphabetBase = 33;
    private const char PaddingLetter = 'А';
    private const string Alphabet = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";
    private static readonly ReadOnlyDictionary<char, int> LetterIndexes = BuildLetterIndexes();

    public int ComputeNumericValue(string key)
    {
        var letters = ExtractLeadingLetters(key);
        var firstLetter = letters[0];
        var secondLetter = letters.Count > 1 ? letters[1] : PaddingLetter;

        return GetLetterIndex(firstLetter) * AlphabetBase + GetLetterIndex(secondLetter);
    }

    public int ComputeAddress(string key, int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        return ComputeNumericValue(key) % capacity;
    }

    private static List<char> ExtractLeadingLetters(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Ключ не может быть пустым.", nameof(key));
        }

        var letters = new List<char>(capacity: 2);

        foreach (var symbol in key.Trim().ToUpperInvariant())
        {
            if (TryGetLetterIndex(symbol, out _))
            {
                letters.Add(symbol);
                if (letters.Count == 2)
                {
                    break;
                }

                continue;
            }

            if (char.IsLetter(symbol))
            {
                throw new ArgumentException(
                    $"Ключ содержит неподдерживаемую букву '{symbol}'. Допустимы только русские буквы.",
                    nameof(key));
            }
        }

        return letters.Count == 0
            ? throw new ArgumentException("Ключ должен содержать хотя бы одну русскую букву.", nameof(key))
            : letters;
    }

    private static int GetLetterIndex(char symbol)
    {
        if (TryGetLetterIndex(symbol, out var index))
        {
            return index;
        }

        throw new ArgumentException(
            $"Ключ содержит неподдерживаемую букву '{symbol}'. Допустимы только русские буквы.");
    }

    private static bool TryGetLetterIndex(char symbol, out int index)
    {
        return LetterIndexes.TryGetValue(symbol, out index);
    }

    private static ReadOnlyDictionary<char, int> BuildLetterIndexes()
    {
        var indexes = new Dictionary<char, int>(Alphabet.Length);

        for (var index = 0; index < Alphabet.Length; index++)
        {
            indexes[Alphabet[index]] = index;
        }

        return new ReadOnlyDictionary<char, int>(indexes);
    }
}