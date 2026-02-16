using System;
using System.IO;
using System.Reflection;
using Xunit;
using IPCSoftware.Services;

namespace IPCSoftware_UnitTesting.Service_Tests
{
    public class CsvReaderTests : IDisposable
    {
        private readonly string _tempFile;

        public CsvReaderTests()
        {
            _tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".csv");
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_tempFile)) File.Delete(_tempFile);
            }
            catch { /* best-effort cleanup for tests */ }
        }

        [Fact]
        public void Read_NonExistingFile_ReturnsEmptyList()
        {
            // Ensure file does not exist
            if (File.Exists(_tempFile)) File.Delete(_tempFile);

            var rows = CsvReader.Read(_tempFile);

            Assert.NotNull(rows);
            Assert.Empty(rows);
        }

        [Fact]
        public void Read_HeaderOnlyFile_ReturnsEmptyList()
        {
            File.WriteAllText(_tempFile, "Col1,Col2,Col3\r\n");

            var rows = CsvReader.Read(_tempFile);

            Assert.NotNull(rows);
            Assert.Empty(rows);
        }

        [Fact]
        public void Read_SimpleRow_ParsesColumns()
        {
            File.WriteAllText(_tempFile, "C1,C2,C3\r\none,two,three\r\n");

            var rows = CsvReader.Read(_tempFile);

            Assert.Single(rows);
            Assert.Equal(new[] { "one", "two", "three" }, rows[0]);
        }

        [Fact]
        public void Read_QuotedComma_FieldContainsComma()
        {
            File.WriteAllText(_tempFile,
                "C1,C2,C3\r\n\"a,1\",b,c\r\n");

            var rows = CsvReader.Read(_tempFile);

            Assert.Single(rows);
            Assert.Equal(3, rows[0].Length);
            Assert.Equal("a,1", rows[0][0]);
            Assert.Equal("b", rows[0][1]);
            Assert.Equal("c", rows[0][2]);
        }

        [Fact]
        public void Read_EscapedQuotes_UnescapesQuotes()
        {
            File.WriteAllText(_tempFile,
                "C1,C2\r\n\"He said \"\"Hello\"\"\",Value\r\n");

            var rows = CsvReader.Read(_tempFile);

            Assert.Single(rows);
            Assert.Equal("He said \"Hello\"", rows[0][0]);
            Assert.Equal("Value", rows[0][1]);
        }

        [Fact]
        public void Read_BlankLines_AreIgnored()
        {
            File.WriteAllText(_tempFile,
                "C1,C2\r\n\r\none,two\r\n\r\n");

            var rows = CsvReader.Read(_tempFile);

            Assert.Single(rows);
            Assert.Equal(new[] { "one", "two" }, rows[0]);
        }

        [Fact]
        public void Read_FieldsAreTrimmed()
        {
            File.WriteAllText(_tempFile,
                "C1,C2\r\n  a  ,  b  \r\n");

            var rows = CsvReader.Read(_tempFile);

            Assert.Single(rows);
            Assert.Equal(new[] { "a", "b" }, rows[0]);
        }

        [Fact]
        public void SplitCsvLine_PrivateMethod_WorksForVariousCases()
        {
            var method = typeof(CsvReader).GetMethod("SplitCsvLine", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method);

            string[] result;

            // simple
            result = (string[])method.Invoke(null, new object[] { "a,b,c" });
            Assert.Equal(new[] { "a", "b", "c" }, result);

            // quoted comma
            result = (string[])method.Invoke(null, new object[] { "\"a,1\",b" });
            Assert.Equal(new[] { "a,1", "b" }, result);

            // escaped quotes
            result = (string[])method.Invoke(null, new object[] { "\"X \"\"inner\"\" Y\",Z" });
            Assert.Equal(new[] { "X \"inner\" Y", "Z" }, result);

            // trimming and surrounding spaces
            result = (string[])method.Invoke(null, new object[] { "  \"x\"  ,  y  " });
            Assert.Equal(new[] { "x", "y" }, result);
        }
    }
}