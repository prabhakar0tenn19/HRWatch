using HRWatch.Application.Common.Abstractions;
using HRWatch.Application.Features.Employees.Commands.SyncEmployees;
using HRWatch.Application.Features.Employees.DTOs;
using HRWatch.Domain.Entities;
using Moq;
using FluentAssertions;
using Xunit;
using Microsoft.Extensions.Logging;

namespace HRWatch.Tests.UnitTests.Application;

public class SyncEmployeesCommandHandlerTests
{
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepo = new();
    private readonly Mock<IEmployeeApiClient> _mockApiClient = new();
    private readonly Mock<ILogger<SyncEmployeesCommandHandler>> _mockLogger = new();

    [Fact]
    public async Task HandleAsync_WhenApiReturnsNewEmployees_ShouldCreateThem()
    {
        var externalEmployees = new List<ExternalEmployeeDto>
        {
            new() { ExternalId = "EXT001", FirstName = "John", LastName = "Doe",
                    Email = "john@co.com", Department = "Eng",
                    JoinDate = DateTime.Today.AddYears(-1), IsActive = true }
        };

        _mockApiClient.Setup(x => x.GetAllEmployeesAsync(default))
            .ReturnsAsync(externalEmployees);

        _mockEmployeeRepo.Setup(x => x.GetByExternalIdAsync("EXT001", default))
            .ReturnsAsync((Employee?)null);

        _mockEmployeeRepo.Setup(x => x.AddAsync(It.IsAny<Employee>(), default))
            .Returns(Task.CompletedTask);

        _mockEmployeeRepo.Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var handler = new SyncEmployeesCommandHandler(
            _mockEmployeeRepo.Object, _mockApiClient.Object, _mockLogger.Object);

        var result = await handler.HandleAsync(new SyncEmployeesCommand(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EmployeesCreated.Should().Be(1);
        result.Value.EmployeesUpdated.Should().Be(0);

        _mockEmployeeRepo.Verify(x => x.AddAsync(It.IsAny<Employee>(), default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenApiReturnsZeroEmployees_ShouldReturnSuccess_WithZeroCounts()
    {
        _mockApiClient.Setup(x => x.GetAllEmployeesAsync(default))
            .ReturnsAsync(new List<ExternalEmployeeDto>());

        var handler = new SyncEmployeesCommandHandler(
            _mockEmployeeRepo.Object, _mockApiClient.Object, _mockLogger.Object);

        var result = await handler.HandleAsync(new SyncEmployeesCommand(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EmployeesSynced.Should().Be(0);
    }
}
