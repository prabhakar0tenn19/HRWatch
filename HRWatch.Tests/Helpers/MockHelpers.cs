using HRWatch.Application.Common;
using HRWatch.Application.Common.Abstractions;
using Moq;

namespace HRWatch.Tests.Helpers;

public static class MockHelpers
{
    public static Mock<ICommandMediator> CreateSuccessCommandMediator<TResult>(TResult returnValue)
    {
        var mock = new Mock<ICommandMediator>();
        mock.Setup(x => x.SendAsync(It.IsAny<ICommand<TResult>>(), default))
            .ReturnsAsync(Result<TResult>.Success(returnValue));
        return mock;
    }

    public static Mock<IQueryMediator> CreateSuccessQueryMediator<TResult>(TResult returnValue)
    {
        var mock = new Mock<IQueryMediator>();
        mock.Setup(x => x.SendAsync(It.IsAny<IQuery<TResult>>(), default))
            .ReturnsAsync(Result<TResult>.Success(returnValue));
        return mock;
    }

    public static Mock<ICommandMediator> CreateFailureCommandMediator<TResult>(string code, string message)
    {
        var mock = new Mock<ICommandMediator>();
        mock.Setup(x => x.SendAsync(It.IsAny<ICommand<TResult>>(), default))
            .ReturnsAsync(Result<TResult>.Failure(code, message));
        return mock;
    }
}
