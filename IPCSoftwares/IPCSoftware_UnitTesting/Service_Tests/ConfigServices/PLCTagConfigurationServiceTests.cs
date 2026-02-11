using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using IPCSoftware.Services.ConfigServices;
using IPCSoftware.Services;
using IPCSoftware.Shared.Models;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models.ConfigModels;

namespace IPCSoftware_UnitTesting.Service_Tests.ConfigServices
{
    public class PLCTagConfigurationServiceTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _fileName = "PLCTags.csv";
        private readonly Mock<IAppLogger> _loggerMock;

        public PLCTagConfigurationServiceTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "PLCTagTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _loggerMock = new Mock<IAppLogger>();
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                    Directory.Delete(_tempDir, true);
            }
            catch { /* best-effort cleanup */ }
        }

        private PLCTagConfigurationService CreateService()
        {
            var config = new ConfigSettings
            {
                DataFolder = _tempDir,
                PlcTagsFileName = _fileName
            };

            var options = Options.Create(config);

            // Use the real TagConfigLoader implementation from the product code.
            var tagLoader = new TagConfigLoader(_loggerMock.Object);

            return new PLCTagConfigurationService(options, tagLoader, _loggerMock.Object);
        }

        private string CsvHeader =>
            "Id,TagNo,Name,PLCNo,ModbusAddress,Length,AlgoNo,DataType,BitNo,Offset,Span,Description,Remark,CanWrite,IOType";

        private void WriteCsv(params string[] rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine(CsvHeader);
            foreach (var r in rows) sb.AppendLine(r);
            File.WriteAllText(Path.Combine(_tempDir, _fileName), sb.ToString(), Encoding.UTF8);
        }

        [Fact]
        public async Task InitializeAsync_CreatesFileWhenMissing()
        {
            // Arrange
            var svc = CreateService();
            var csvPath = Path.Combine(_tempDir, _fileName);
            if (File.Exists(csvPath)) File.Delete(csvPath);

            // Act
            await svc.InitializeAsync();

            // Assert
            Assert.True(File.Exists(csvPath));
            var contents = await File.ReadAllTextAsync(csvPath);
            Assert.Contains("Id,TagNo,Name", contents); // header present
        }

        [Fact]
        public async Task InitializeAsync_LoadsTagsFromCsv()
        {


            // Arrange: create a CSV with one row
            // Row values: Id,TagNo,Name,PLCNo,ModbusAddress,Length,AlgoNo,DataType,BitNo,Offset,Span,Description,Remark,CanWrite,IOType
                         //Id,TagNo,Name,PLCNo,ModbusAddress,Length,AlgoNo,DataType,BitNo,Offset,Span,Description,Remark,CanWrite,IOType
            var row = "1,100,TempTag,1,40001,1,0,1,0,0,100,SampleDesc,SampleRemark,1,Input";
            WriteCsv(row);

            var svc = CreateService();

            // Act
            await svc.InitializeAsync();
            var tags = await svc.GetAllTagsAsync();

            // Assert
            Assert.NotNull(tags);
            Assert.Single(tags);
            var tag = tags.First();
            Assert.Equal(1, tag.Id);
            Assert.Equal(100, tag.TagNo);
            Assert.Equal("TempTag", tag.Name);
            Assert.Equal(1, tag.PLCNo);
            Assert.Equal(40001, tag.ModbusAddress);
            Assert.Equal(1, tag.Length);
            Assert.Equal(1, tag.DataType); // depends on TagConfigLoader mapping but this is the CSV value
            Assert.Equal("SampleDesc", tag.Description);
            Assert.Equal("SampleRemark", tag.Remark);
            Assert.True(tag.CanWrite);
        }

        [Fact]
        public async Task AddTagAsync_AddsAndPersists()
        {
            // Arrange
            var svc = CreateService();

            var newTag = new PLCTagConfigurationModel
            {
                TagNo = 101,
                Name = "NewTag",
                PLCNo = 2,
                ModbusAddress = 40010,
                Length = 1,
                AlgNo = 0,
                DataType = 1,
                BitNo = 0,
                Offset = 0,
                Span = 1,
                Description = "desc",
                Remark = "remark",
                CanWrite = true,
                IOType = "Output"
            };

            // Act
            var added = await svc.AddTagAsync(newTag);

            // Assert
            Assert.NotNull(added);
            Assert.Equal(1, added.Id); // first added => id=1

            var all = await svc.GetAllTagsAsync();
            Assert.Single(all);
            Assert.Equal("NewTag", all[0].Name);

            // File persisted
            var csv = await File.ReadAllTextAsync(Path.Combine(_tempDir, _fileName));
            Assert.Contains("NewTag", csv);
            Assert.Contains("Output", csv);
        }

        [Fact]
        public async Task UpdateTagAsync_UpdatesExistingReturnTrue()
        {
            // Arrange
            var svc = CreateService();

            var tag = new PLCTagConfigurationModel { TagNo = 1, Name = "A", PLCNo = 1, ModbusAddress = 1, Length = 1, AlgNo = 0, DataType = 1, BitNo = 0, Offset = 0, Span = 1, Description = "", Remark = "", CanWrite = false, IOType = "I" };
            var added = await svc.AddTagAsync(tag);

            // Act
            added.Name = "UpdatedName";
            var result = await svc.UpdateTagAsync(added);

            // Assert
            Assert.True(result);
            var fetched = await svc.GetTagByIdAsync(added.Id);
            Assert.Equal("UpdatedName", fetched.Name);

            var csv = await File.ReadAllTextAsync(Path.Combine(_tempDir, _fileName));
            Assert.Contains("UpdatedName", csv);
        }

        [Fact]
        public async Task UpdateTagAsync_NonExisting_ReturnsFalse()
        {
            // Arrange
            var svc = CreateService();
            var t = new PLCTagConfigurationModel { Id = 999, Name = "Missing" };

            // Act
            var result = await svc.UpdateTagAsync(t);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteTagAsync_RemovesTagAndPersists()
        {
            // Arrange
            var svc = CreateService();
            var t1 = await svc.AddTagAsync(new PLCTagConfigurationModel { TagNo = 1, Name = "toRemove", PLCNo = 1, ModbusAddress = 1, Length = 1, AlgNo = 0, DataType = 1, BitNo = 0, Offset = 0, Span = 1, Description = "", Remark = "", CanWrite = false, IOType = "I" });
            var t2 = await svc.AddTagAsync(new PLCTagConfigurationModel { TagNo = 2, Name = "keep", PLCNo = 1, ModbusAddress = 2, Length = 1, AlgNo = 0, DataType = 1, BitNo = 0, Offset = 0, Span = 1, Description = "", Remark = "", CanWrite = false, IOType = "I" });

            // Act
            var deleted = await svc.DeleteTagAsync(t1.Id);

            // Assert
            Assert.True(deleted);
            var all = await svc.GetAllTagsAsync();
            Assert.Single(all);
            Assert.Equal("keep", all[0].Name);

            var csv = await File.ReadAllTextAsync(Path.Combine(_tempDir, _fileName));
            Assert.DoesNotContain("toRemove", csv);
        }

        [Fact]
        public async Task DeleteTagAsync_NonExisting_ReturnsFalse()
        {
            // Arrange
            var svc = CreateService();

            // Act
            var result = await svc.DeleteTagAsync(12345);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ReloadTagsAsync_ReflectsExternalFileChanges()
        {
            // Arrange
            // initial file with one row
            var row1 = "1,10,TagA,1,40001,1,0,1,0,0,1,DescA,RemarkA,1,Input";
            WriteCsv(row1);
            var svc = CreateService();

            await svc.InitializeAsync();
            var initial = await svc.GetAllTagsAsync();
            Assert.Single(initial);
            Assert.Equal("TagA", initial[0].Name);

            // Now externally modify the CSV (simulate external process)
            var row2 = "2,11,TagB,1,40002,1,0,1,0,0,1,DescB,RemarkB,1,Input";
            WriteCsv(row1, row2);

            // Act
            var reloaded = await svc.ReloadTagsAsync();

            // Assert
            Assert.NotNull(reloaded);
            Assert.Equal(2, reloaded.Count);
            Assert.Contains(reloaded, t => t.Name == "TagB");
        }
    }
}