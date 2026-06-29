using EventTicketing.BusinessLogic.Exceptions;
using EventTicketing.BusinessLogic.Services;
using EventTicketing.DataAccess;
using EventTicketing.DataAccess.Repositories;
using EventTicketing.Models.Dtos;
using EventTicketing.Models.Entities;
using EventTicketing.Tests.TestSupport;
using FluentAssertions;
using Moq;

namespace EventTicketing.Tests.Unit;

public class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly TestClock _clock = new(new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));

    private CustomerService CreateSut() => new(_customers.Object, _uow.Object, _clock);

    [Fact]
    public async Task Create_with_duplicate_email_throws_validation()
    {
        _customers.Setup(c => c.EmailExistsAsync("dupe@example.com", null)).ReturnsAsync(true);
        var sut = CreateSut();

        await sut.Invoking(s => s.CreateAsync(new CreateCustomerRequest("Dupe", "dupe@example.com", null)))
            .Should().ThrowAsync<ValidationException>();
        _customers.Verify(c => c.Add(It.IsAny<Customer>()), Times.Never);
    }

    [Fact]
    public async Task Create_with_new_email_persists_customer()
    {
        _customers.Setup(c => c.EmailExistsAsync(It.IsAny<string>(), null)).ReturnsAsync(false);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var sut = CreateSut();

        var result = await sut.CreateAsync(new CreateCustomerRequest("New User", "new@example.com", "+123"));

        result.Email.Should().Be("new@example.com");
        _customers.Verify(c => c.Add(It.IsAny<Customer>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
