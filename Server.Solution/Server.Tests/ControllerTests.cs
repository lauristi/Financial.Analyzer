using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Api.Controllers;
using Server.Api.Domain.Model;
using Server.Api.Domain.Service.InfrastrutureService.Interface;


namespace Server.Api.Tests
{
    public class ControllerTests
    {
        private readonly Mock<IEncryptionService> _encryptionServiceMock;
        private readonly Mock<ILogger<ClipboardController>> _loggerMock;
        private readonly ClipboardController _controller;

        public ControllerTests()
        {
            _encryptionServiceMock = new Mock<IEncryptionService>();
            _loggerMock = new Mock<ILogger<ClipboardController>>();
            _controller = new ClipboardController(_encryptionServiceMock.Object, _loggerMock.Object);
        }

        //---------------------------------------------------------------------------
        //Basic tests
        //---------------------------------------------------------------------------

        [Fact]
        public async Task PostClipBoard_ShouldEncryptContent()
        {
            // Arrange
            var transmition = new Transmition { Content = "test content" };
            _encryptionServiceMock.Setup(c => c.Encrypt(It.IsAny<string>())).Returns("encrypted content");

            // Act
            var result = await _controller.PostClipBoard(transmition);

            // Assert
            var okResult = Assert.IsType<OkResult>(result);
            _encryptionServiceMock.Verify(c => c.Encrypt("test content"), Times.Once);
        }

        [Fact]
        public async Task GetClipBoard_ShouldReturnDecryptedContent()
        {
            // Arrange
            _encryptionServiceMock.Setup(c => c.Decrypt(It.IsAny<string>())).Returns("decrypted content");
            await _controller.PostClipBoard(new Transmition { Content = "test content" });

            // Act
            var result = await _controller.GetClipBoard();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var clipboardResponse = Assert.IsType<ClipboardResponse>(okResult.Value);
            Assert.Equal("decrypted content", clipboardResponse.Clipboard);
            _encryptionServiceMock.Verify(c => c.Decrypt(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetClipBoard_ShouldReturnEmptyClipboardResponseIfNoData()
        {
            // Act
            var result = await _controller.GetClipBoard();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var clipboardResponse = Assert.IsType<ClipboardResponse>(okResult.Value);
            Assert.Null(clipboardResponse.Clipboard);
        }
    }
}