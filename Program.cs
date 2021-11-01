namespace GS1ApplicationIdentifierScrapper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Curiosity.Resources;

    using HtmlAgilityPack;

    internal class Program
    {
        private static async Task Main(string[] args)
        {
            //var format = new Format("N4 + N6..12", "n", "c");
            var fileNameFormat = "AppIdentifiers.{0}.resx";
            var tasks = new List<Task>();
            tasks.Add(CollectData("en", fileNameFormat, "numeric characters", "characters"));
            tasks.Add(CollectData("es", fileNameFormat, "caracteres numéricos", "caracteres"));
            await Task.WhenAll(tasks);
        }

        private static async Task CollectData(
            string languageString,
            string resourceName,
            string numeric,
            string characters)
        {
            var web = new HtmlWeb();
            using var writer = new ResXResourceWriter("../../../" + string.Format(resourceName, languageString));

            var document =
                await web.LoadFromWebAsync(
                    $"https://www.gs1.org/standards/barcodes/application-identifiers?lang={languageString}");

            var tableRows = document.DocumentNode.SelectNodes("//tr");

            foreach (var row in tableRows.Skip(1))
            {
                var dataRow = new DataRow(row.ChildNodes, numeric, characters);

                writer.AddResource($"Description{dataRow.AiCode}", dataRow.Description);
                writer.AddResource($"Format{dataRow.AiCode}", dataRow.Format.ToString());
            }
        }
    }

    internal class DataRow
    {
        public DataRow(HtmlNodeCollection collection, string numeric, string characters)
        {
            if (collection.Count < 3)
            {
                throw new ArgumentException("Not enough data");
            }

            AiCode = collection[1].InnerText.Trim();
            Description = collection[3].InnerText.Trim();

            try
            {
                Format = new Format(collection[5].InnerText.Trim(), numeric, characters);
            }
            catch
            {
                Console.WriteLine($"Failed to format {AiCode} with format {collection[5].InnerText.Trim()}");
            }
        }

        public string AiCode { get; set; }
        public string Description { get; set; }
        public Format Format { get; set; }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        /// <footer>
        ///     <a href="https://docs.microsoft.com/en-us/dotnet/api/System.Object.ToString?view=netcore-5.0">
        ///         `Object.ToString` on
        ///         docs.microsoft.com
        ///     </a>
        /// </footer>
        public override string ToString()
        {
            return $"AI Code {AiCode} : Description {Description} : Format {Format}";
        }
    }

    internal class Format
    {
        private readonly string output;

        public Format(string format, string numeric, string characters)
        {
            var builder = new StringBuilder();
            var split = format.Split('+');

            var count = 0;
            var isNumeric = true;

            foreach (var section in split.Skip(1))
            {
                var trimmed = section.Trim();

                if (trimmed[0] == 'N')
                {
                    if (!isNumeric)
                    {
                        // Handle swap between numeric and non
                        count = AddCountToBuilder(characters, count, builder);
                    }

                    isNumeric = true;

                    count = ProcessSection(numeric, trimmed, builder, count);
                }
                else
                {
                    if (isNumeric)
                    {
                        // Handle swap between numeric and non
                        count = AddCountToBuilder(numeric, count, builder);
                    }

                    isNumeric = false;

                    count = ProcessSection(characters, trimmed, builder, count);
                }
            }

            AddCountToBuilder(isNumeric ? numeric : characters, count, builder);

            output = builder.ToString();
        }

        private static int AddCountToBuilder(string text, int count, StringBuilder builder)
        {
            if (count != 0)
            {
                builder.Append($"{count} {text} ");
                count = 0;
            }

            return count;
        }

        private static int ProcessSection(string numeric, string section, StringBuilder builder, int count)
        {
            var splits = section.Split("..");

            if (splits.Length == 1)
            {
                return int.Parse(string.Concat(section.Skip(1)));
            }

            if (splits.Length > 2)
            {
                throw new Exception($"Unable to parse section with more than 1 .. {section}");
            }

            // Handle NX..
            if (char.IsDigit(section[1]))
            {
                count += int.Parse(string.Concat(section.Skip(1).TakeWhile(char.IsDigit)));
            }

            // Handle ..X
            var ending = section.Substring(section.LastIndexOf('.') + 1);
            builder.Append($"{count} to {ending} {numeric} ");

            return 0;
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return output;
        }
    }
}