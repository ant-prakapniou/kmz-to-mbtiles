using System.Text.RegularExpressions;
using NickBuhro.Translit;

namespace Kmz2Mbtiles.Core.Naming;

public static class NameHelper
{
    private static readonly Regex CyrillicRegex = new(@"\p{IsCyrillic}", RegexOptions.Compiled);

    /// <summary>
    /// Символы, которые формально допустимы в Windows, но могут быть проблемой
    /// в Google Drive, облаках, старых ФС. Заменяем на _.
    /// </summary>
    private static readonly char[] ExtraUnsafeChars =
    {
        '«', '»', '№', '“', '”', '„', '‘', '’', '–', '—', '…', '·',
    };

    /// <summary>Есть ли в строке хотя бы один кириллический символ.</summary>
    public static bool ContainsCyrillic(string s) => CyrillicRegex.IsMatch(s);

    /// <summary>
    /// Приводит мастер-имя к целевому:
    ///   1. Если есть кириллица — транслит по ГОСТ 7.79-2000 (ISO 9, система Б).
    ///   2. Всегда — санитайз (пробелы -> _, недопустимые и «опасные» символы -> _,
    ///      остатки кавычек от транслита -> удалить, схлопнуть __, обрезать концы).
    /// </summary>
    public static string Normalize(string master)
    {
        var s = master;

        if (ContainsCyrillic(s))
        {
            s = Transliteration.CyrillicToLatin(s, Language.Russian);
        }

        s = Sanitize(s);
        return s;
    }

    /// <summary>Санитайз имени файла.</summary>
    public static string Sanitize(string s)
    {
        // Пробелы ->
        s = s.Replace(' ', '_');

        // Убираем остатки ъ/ь от транслита (двойные и одинарные кавычки)
        s = s.Replace("\"", "").Replace("'", "").Replace("`", "");

        // Недопустимые в имени файла символы -> _
        var invalid = Path.GetInvalidFileNameChars();
        var unsafeSet = new HashSet<char>(invalid);
        foreach (var c in ExtraUnsafeChars)
            unsafeSet.Add(c);

        var chars = s.Select(c => unsafeSet.Contains(c) ? '_' : c).ToArray();
        s = new string(chars);

        // Схлопываем повторные _
        while (s.Contains("__"))
            s = s.Replace("__", "_");

        // Обрезаем trailing dots/spaces
        s = s.TrimEnd('.', ' ');

        // Ограничение длины (255 - запас на расширение)
        if (s.Length > 200)
            s = s.Substring(0, 200).TrimEnd('_', '.', ' ');

        return s;
    }
}