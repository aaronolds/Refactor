using Moq;
using LegacyApp;
using System;
using Xunit;

namespace RefactoringTest.UnitTests;

public class UnitTestForCreditRules {

    private readonly Mock<IUserDataAccess> _mockUserDataAccess = new();
    private readonly Mock<IClientRepository> _mockClientRepository = new();
    private readonly Mock<ICreditLimitFactory> _mockCreditLimitFactory = new();
    private readonly UserDataAccessProxy userDataAccessProxy = new();    
    private readonly UserService userService;

    public UnitTestForCreditRules()
    {
        userService = new UserService(_mockCreditLimitFactory.Object, _mockClientRepository.Object, _mockUserDataAccess.Object);
    }

    [Fact]
    public void Should_CreateUser_WithAllParametersPresent() 
    {
        User user = new()
        {
            Firstname = "Elton",
            Surname = "John",
            EmailAddress = "elton_john@aol.com",
            DateOfBirth = new DateTime(1968, 7, 24),
            Client = new Client { Id = 27, Name = "VeryImportantClient" }
        };
        
        _mockUserDataAccess.Setup(m => m.AddUser(It.IsAny<User>()));
        _mockUserDataAccess.Setup(m => m.CreateClient(It.IsAny<int>(), It.IsAny<string>()));
        _mockClientRepository.Setup(m => m.GetById(It.IsAny<int>())).Returns(user.Client);

        _mockCreditLimitFactory.Setup(m => m.GetRule(It.IsAny<string>())).Returns(new VeryImportantClient());

        userDataAccessProxy.CreateClient(user.Client.Id, user.Client.Name);
        var result = userService.AddUser(user.Firstname, user.Surname, user.EmailAddress, user.DateOfBirth, 27);  
        Assert.True(result);

        _mockUserDataAccess.Verify(u => u.AddUser(It.IsAny<User>()), Times.Once());
    }
}