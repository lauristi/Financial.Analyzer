using Core.HttpHandleResults.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Server.Api.Controllers;
using Server.Api.Models;
using Server.Api.Orchestration.Interface;
using Server.Api.Services.Interfaces;
using System.Text;

namespace Server.Api.Tests
{
    public class StatementControllerTests
    {
        private readonly Mock<IStatementOrchestratorService> _orchestratorMock;
        private readonly Mock<IExpenseService> _expenseServiceMock;
        private readonly Mock<IStatementXlsService> _xlsServiceMock;
        private readonly StatementController _controller;

        public StatementControllerTests()
        {
            _orchestratorMock = new Mock<IStatementOrchestratorService>();
            _expenseServiceMock = new Mock<IExpenseService>();
            _xlsServiceMock = new Mock<IStatementXlsService>();

            // Resolvendo o erro CS7036: Passando os 3 parâmetros requeridos
            _controller = new StatementController(
                _orchestratorMock.Object,
                _expenseServiceMock.Object,
                _xlsServiceMock.Object);
        }

        [Fact]
        public async Task ProcessCsv_ShouldReturnBadRequest_WhenNoFilesSent()
        {
            // Act
            var result = await _controller.ProcessCsv(new List<IFormFile>());

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var envelope = Assert.IsType<GenericResponseEnvelope<StatementResponse>>(badRequestResult.Value);
            Assert.False(envelope.IsSuccess);
            Assert.Equal("NO_FILES", envelope.ErrorCode);
        }

        [Fact]
        public async Task UploadExpenses_ShouldReturnOk_WhenFileIsValid()
        {
            // Arrange
            var content = "date,description,amount\n2026-03-20,Test,100";
            var fileName = "expenses.csv";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            // Criando o FormFile corretamente para evitar erros de parâmetro
            var file = new FormFile(stream, 0, stream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/csv"
            };

            _expenseServiceMock.Setup(s => s.SaveFileAsync(It.IsAny<IFormFile>()))
                               .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UploadExpenses(file);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var envelope = Assert.IsType<GenericResponseEnvelope<string>>(okResult.Value);
            Assert.True(envelope.IsSuccess);
            _expenseServiceMock.Verify(s => s.SaveFileAsync(It.IsAny<IFormFile>()), Times.Once);
        }

        [Fact]
        public async Task ProcessXls_ShouldReturnOk_WhenFileIsValid()
        {
            // Arrange
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake excel content"));
            var file = new FormFile(stream, 0, stream.Length, "file", "test.xlsx");

            var expectedResponse = new StatementResponse(); // Ajuste se houver construtor específico

            _xlsServiceMock.Setup(s => s.CreateFinalExcelAsync(It.IsAny<IFormFile>()))
                           .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.ProcessXls(file);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var envelope = Assert.IsType<GenericResponseEnvelope<StatementResponse>>(okResult.Value);
            Assert.True(envelope.IsSuccess);
            Assert.Equal("Extratos processados com sucesso.", envelope.Message);
        }
    }
}