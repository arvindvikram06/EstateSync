using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Services.Storage;

namespace EstateSync.Tests.Services;

[TestFixture]
public class LocalFileStorageServiceTests
{
    private Mock<IWebHostEnvironment> _envMock;
    private Mock<IConfiguration> _configMock;
    private LocalFileStorageService _service;
    private string _tempPath;

    [SetUp]
    public void Setup()
    {
        _envMock = new Mock<IWebHostEnvironment>();
        _configMock = new Mock<IConfiguration>();
        _tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        _envMock.Setup(x => x.ContentRootPath).Returns(_tempPath);
        _configMock.Setup(x => x["FileStorage:LocalUploadPath"]).Returns("uploads");

        _service = new LocalFileStorageService(_envMock.Object, _configMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempPath))
        {
            Directory.Delete(_tempPath, true);
        }
    }

    [Test]
    public async Task SaveFileAsync_ShouldThrowBadRequest_WhenFileIsNull()
    {
        Func<Task> act = () => _service.SaveFileAsync(null!, "subfolder", CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Test]
    public async Task SaveFileAsync_ShouldSaveFileAndReturnRelativePath()
    {
        var fileMock = new Mock<IFormFile>();
        var content = "test file content";
        var fileName = "test.txt";
        var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
        
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(ms.Length);

        var result = await _service.SaveFileAsync(fileMock.Object, "subfolder", CancellationToken.None);

        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("subfolder");
        result.Should().EndWith(".txt");

        var fullPath = Path.Combine(_tempPath, "uploads", result);
        File.Exists(fullPath).Should().BeTrue();
    }

    [Test]
    public async Task DeleteFileAsync_ShouldDeleteFile_WhenExists()
    {
        var uploadsPath = Path.Combine(_tempPath, "uploads", "testdir");
        Directory.CreateDirectory(uploadsPath);
        var fullPath = Path.Combine(uploadsPath, "file.txt");
        await File.WriteAllTextAsync(fullPath, "content");

        await _service.DeleteFileAsync("testdir/file.txt");

        File.Exists(fullPath).Should().BeFalse();
    }
}
