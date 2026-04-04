namespace L4.Hashing;

public static class LiteratureHashTableFactory
{
    private static IReadOnlyList<SeedRecord> GetDefaultRecords()
    {
        return
        [
            new SeedRecord("Роман", "Крупная форма эпической прозы."),
            new SeedRecord("Романс", "Лирическое произведение с музыкальным началом."),
            new SeedRecord("Роль", "Функция или образ персонажа в произведении."),
            new SeedRecord("Рассказ", "Небольшое прозаическое повествование."),
            new SeedRecord("Рецензия", "Критический анализ литературного текста."),
            new SeedRecord("Поэма", "Стихотворное произведение большого объема."),
            new SeedRecord("Поэзия", "Искусство образного поэтического слова."),
            new SeedRecord("Пьеса", "Драматическое произведение для театра."),
            new SeedRecord("Сюжет", "Последовательность событий в произведении."),
            new SeedRecord("Сонет", "Лирическое стихотворение из 14 строк."),
            new SeedRecord("Баллада", "Сюжетное стихотворение легендарного характера."),
            new SeedRecord("Бестселлер", "Книга, имеющая высокий коммерческий успех.")
        ];
    }

    public static HashTable CreatePreFilledTable()
    {
        var table = new HashTable();
        foreach (var record in GetDefaultRecords())
        {
            table.Add(record.Key, record.Data);
        }

        return table;
    }
}