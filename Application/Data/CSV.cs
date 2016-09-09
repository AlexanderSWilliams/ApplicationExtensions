using Application.IEnumerableExtensions;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Application.Data.CSV
{
    public static class CSV
    {
        public static List<Dictionary<string, string>> CSVToDictionary(string csv, char delimiter = ',')
        {
            var ListOfListOfStrings = CSVToListOfListOfStrings(csv, delimiter);

            return ListOfListOfStrings.ListOfListOfStringToDictionary();
        }

        public static List<List<List<byte>>> CSVToListOfListOfBytes(byte[] csv, byte delimiter = 44)
        {
            var result = new List<List<List<byte>>>();
            var Cells = new List<List<byte>>();
            var cell = new List<byte>();
            var InQuotes = false;
            var Length = csv.Length;

            var SpecialBytes = new HashSet<byte> { delimiter, 13, 10 };
            for (var i = 0; i < Length; i++)
            {
                var ch = csv[i];
                switch (ch)
                {
                    case 34:
                        if (InQuotes)
                        {
                            var nextIndex = i + 1;
                            if (nextIndex >= Length)
                                break;

                            if (csv[nextIndex] == 34)
                            {
                                cell.Add(34);
                                i++;
                            }
                            else if (csv[i - 1] != 34 && !SpecialBytes.Contains(csv[nextIndex]))
                                cell.Add(34);
                            else
                                InQuotes = false;
                        }
                        else
                        {
                            InQuotes = true;
                        }
                        break;

                    case 13:
                    case 10:
                        if (InQuotes)
                            cell.Add(ch);
                        else
                        {
                            Cells.Add(cell);
                            result.Add(Cells);
                            Cells = new List<List<byte>>();
                            cell = new List<byte>();
                            var nextIndex = i + 1;
                            if (nextIndex != Length && ch == 13 && csv[nextIndex] == 10)
                                i++;
                        }
                        break;

                    default:
                        if (ch == delimiter)
                        {
                            if (InQuotes)
                                cell.Add(delimiter);
                            else
                            {
                                Cells.Add(cell);
                                cell = new List<byte>();
                            }
                        }
                        else
                            cell.Add(ch);
                        break;
                }
            }
            if (cell.Count != 0)
                Cells.Add(cell);
            if (Cells.Count != 0)
                result.Add(Cells);
            return result;
        }

        public static List<List<string>> CSVToListOfListOfStrings(string csv, char delimiter = ',')
        {
            var result = new List<List<string>>();
            var Cells = new List<string>();
            var cell = new StringBuilder();
            var InQuotes = false;
            var Length = csv.Length;

            var SpecialCharacters = new HashSet<char> { delimiter, '\r', '\n' };
            for (var i = 0; i < Length; i++)
            {
                var ch = csv[i];
                switch (ch)
                {
                    case '"':
                        if (InQuotes)
                        {
                            var nextIndex = i + 1;
                            if (nextIndex >= Length)
                                break;

                            if (csv[nextIndex] == '"')
                            {
                                cell.Append('"');
                                i++;
                            }
                            else if (csv[i - 1] != '"' && !SpecialCharacters.Contains(csv[nextIndex]))
                                cell.Append('"');
                            else
                                InQuotes = false;
                        }
                        else
                        {
                            InQuotes = true;
                        }
                        break;

                    case '\r':
                    case '\n':
                        if (InQuotes)
                            cell.Append(ch);
                        else
                        {
                            var Value = cell.ToString();
                            Cells.Add(!string.IsNullOrWhiteSpace(Value) ? Value : null);
                            result.Add(Cells);
                            Cells = new List<string>();
                            cell = new StringBuilder();
                            var nextIndex = i + 1;
                            if (nextIndex != Length && ch == '\r' && csv[nextIndex] == '\n')
                                i++;
                        }
                        break;

                    default:
                        if (ch == delimiter)
                        {
                            if (InQuotes)
                                cell.Append(delimiter);
                            else
                            {
                                var Value = cell.ToString();
                                Cells.Add(!string.IsNullOrWhiteSpace(Value) ? Value : null);
                                cell = new StringBuilder();
                            }
                        }
                        else
                            cell.Append(ch);
                        break;
                }
            }
            if (cell.Length != 0)
                Cells.Add(cell.ToString());
            if (Cells.Count != 0)
                result.Add(Cells);
            return result;
        }

        public static List<Dictionary<string, string>> CSVToTrimmedDictionary(string csv, char delimiter = ',', string[] trimeExclusionArray = null)
        {
            trimeExclusionArray = trimeExclusionArray ?? new string[] { };
            var data = CSVToDictionary(csv, delimiter);

            var trimmedData = new List<Dictionary<string, string>>();
            foreach (var row in data)
            {
                trimmedData.Add(row.ToDictionary(x => x.Key, x => trimeExclusionArray.Contains(x.Key) ? x.Value : x.Value?.Trim()));
            }

            return trimmedData;
        }

        public static List<Dictionary<string, string>> CSVToTrimmedNonDistinctDictionary(string csv, char delimiter = ',', string[] trimeExclusionArray = null)
        {
            var data = CSVToTrimmedDictionary(csv, delimiter, trimeExclusionArray);

            var NonDistinctKeys = data.First().Keys.ToList().Select(x => new
            {
                Key = x,
                IsNotDisintct = data.Select(y => y[x]).Distinct().Count() > 1
            })
            .Where(x => x.IsNotDisintct).Select(x => x.Key).ToHashSet();

            return data.Select(x => x.Where(y => NonDistinctKeys.Contains(y.Key)).ToDictionary(y => y.Key, y => y.Value)).ToList();
        }

        public static string ListOfListOfStringsToCSV(this IEnumerable<IEnumerable<string>> listOfListOfStrings, char delimiter = ',', bool minimalSize = false)
        {
            var builder = new StringBuilder();
            var SpecialCharacterArray = new char[] { '"', delimiter, '\r', '\n' };

            if (minimalSize)
            {
                foreach (var row in listOfListOfStrings)
                {
                    bool firstColumn = true;
                    foreach (string value in row)
                    {
                        // Add separator if this isn't the first value
                        if (!firstColumn)
                            builder.Append(delimiter);

                        if (value == null)
                            builder.Append("");
                        else if (value.IndexOfAny(SpecialCharacterArray) != -1)
                            builder.AppendFormat("\"{0}\"", value.Replace("\"", "\"\""));
                        else
                            builder.Append(value);
                        firstColumn = false;
                    }
                    builder.Append("\r\n");
                }
            }
            else
            {
                foreach (var row in listOfListOfStrings)
                {
                    bool firstColumn = true;
                    foreach (string value in row)
                    {
                        // Add separator if this isn't the first value
                        if (!firstColumn)
                            builder.Append(delimiter);

                        if (value == null)
                            builder.Append("");
                        else
                            builder.AppendFormat("\"{0}\"", value.Replace("\"", "\"\""));
                        firstColumn = false;
                    }
                    builder.Append("\r\n");
                }
            }

            return builder.ToString();
        }
    }
}