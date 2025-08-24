using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Weighbridge.Models;
using Weighbridge.Services;
using Weighbridge.ViewModels;
using FluentValidation;
using FluentValidation.Results;
using static NUnit.Framework.Assert;
using BCrypt.Net;

namespace Weighbridge.Tests
{
    [TestFixture]
    public class UserManagementViewModelTests
    {
        private Mock<IDatabaseService> _mockDatabaseService;
        private Mock<IValidator<User>> _mockUserValidator;
        private UserManagementViewModel _viewModel;
        private List<User> _users;

        [SetUp]
        public void Setup()
        {
            _mockDatabaseService = new Mock<IDatabaseService>();
            _mockUserValidator = new Mock<IValidator<User>>();
            _users = new List<User>
            {
                new User { Id = 1, Username = "User1", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"), Role = "Admin", CanEditDockets = true, CanDeleteDockets = true, IsAdmin = true },
                new User { Id = 2, Username = "User2", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"), Role = "Operator", CanEditDockets = false, CanDeleteDockets = false, IsAdmin = false }
            };

            _mockDatabaseService.Setup(db => db.GetItemsAsync<User>()).ReturnsAsync(_users);
            _mockUserValidator.Setup(v => v.ValidateAsync(It.IsAny<User>(), default)).ReturnsAsync(new FluentValidation.Results.ValidationResult());
            
            _viewModel = new UserManagementViewModel(_mockDatabaseService.Object, _mockUserValidator.Object);
        }

        [Test]
        public void Constructor_LoadsUsers()
        {
            // Assert
            _mockDatabaseService.Verify(db => db.GetItemsAsync<User>(), Times.Once); // Only once in constructor
            That(_users.Count, Is.EqualTo(_viewModel.Users.Count));
        }

        [Test]
        public void SelectedUser_Setter_UpdatesProperties()
        {
            // Arrange
            var user = _users.First();

            // Act
            _viewModel.SelectedUser = user;

            // Assert
            That(user.Username, Is.EqualTo(_viewModel.NewUsername));
            That(user.CanEditDockets, Is.EqualTo(_viewModel.CanEditDockets));
            That(user.CanDeleteDockets, Is.EqualTo(_viewModel.CanDeleteDockets));
            That(user.IsAdmin, Is.EqualTo(_viewModel.IsAdmin));
        }

        [Test]
        public async Task AddUser_WithValidData_SavesUser()
        {
            // Arrange
            _viewModel.NewUsername = "NewUser";
            _viewModel.NewPassword = "newpassword";
            _viewModel.IsAdmin = false;

            // Act
            await _viewModel.AddUserCommand.ExecuteAsync(null);

            // Assert
            _mockUserValidator.Verify(v => v.ValidateAsync(It.Is<User>(u => u.Username == "NewUser"), default), Times.Once);
            _mockDatabaseService.Verify(db => db.SaveItemAsync(It.Is<User>(u => u.Username == "NewUser")), Times.Once);
            _mockDatabaseService.Verify(db => db.GetItemsAsync<User>(), Times.Exactly(2)); // Once in constructor, once after adding
        }

        [Test]
        public async Task UpdateUser_WithValidData_UpdatesUser()
        {
            // Arrange
            var user = _users.First();
            _viewModel.SelectedUser = user;
            _viewModel.NewUsername = "UpdatedUser";
            _viewModel.NewPassword = "updatedpassword";
            _viewModel.IsAdmin = true;

            // Act
            await _viewModel.UpdateUserCommand.ExecuteAsync(null);

            // Assert
            _mockUserValidator.Verify(v => v.ValidateAsync(It.Is<User>(u => u.Username == "UpdatedUser" && u.Id == user.Id), default), Times.Once);
            _mockDatabaseService.Verify(db => db.SaveItemAsync(It.Is<User>(u => u.Username == "UpdatedUser" && u.Id == user.Id)), Times.Once);
        }

        [Test]
        public async Task DeleteUser_DeletesUser()
        {
            // Arrange
            var user = _users.First();
            _viewModel.SelectedUser = user;

            // Act
            await _viewModel.DeleteUserCommand.ExecuteAsync(null);

            // Assert
            _mockDatabaseService.Verify(db => db.DeleteItemAsync(user), Times.Once);
        }

        [Test]
        public void ClearForm_ClearsProperties()
        {
            // Arrange
            _viewModel.SelectedUser = _users.First();
            _viewModel.NewUsername = "Test";
            _viewModel.NewPassword = "Test";
            _viewModel.CanEditDockets = true;
            _viewModel.CanDeleteDockets = true;
            _viewModel.IsAdmin = true;

            // Act
            _viewModel.ClearFormCommand.Execute(null);

            // Assert
            That(_viewModel.SelectedUser, Is.Null);
            That(_viewModel.NewUsername, Is.EqualTo(string.Empty));
            That(_viewModel.NewPassword, Is.EqualTo(string.Empty));
            That(_viewModel.CanEditDockets, Is.False);
            That(_viewModel.CanDeleteDockets, Is.False);
            That(_viewModel.IsAdmin, Is.False);
            That(_viewModel.ValidationErrors, Is.Null);
        }
    }
}
