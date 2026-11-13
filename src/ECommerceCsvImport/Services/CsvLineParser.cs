namespace ECommerceCsvImport.Services;

/// <summary>
/// Разбор одной строки CSV с поддержкой кавычек, запятых внутри полей
/// и экранированных двойных кавычек ("").
/// </summary>
public static class CsvLineParser
{
    public static IReadOnlyList<string> Parse(string line)
    {
        if (line is null)
            throw new ArgumentNullException(nameof(line));

        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // Экранированная кавычка внутри поля: "" -> ".
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }

                continue;
            }

            if (c == '"' && string.IsNullOrWhiteSpace(current.ToString()))
            {
                // Открывающая кавычка в начале поля (пробелы перед ней игнорируются).
                current.Clear();
                inQuotes = true;
            }
            else if (c == ',')
            {
                fields.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString().Trim());
        return fields;
    }
}
