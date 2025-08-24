using NUnit.Framework;
using Moq;
using Weighbridge.Services;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace TestProject1
{
    [TestFixture]
    public class AlertServiceTests
    {
        private Mock<Page> _mockMainPage;
        private AlertService _alertService;

        [SetUp]
        public void Setup()
        {
            _mockMainPage = new Mock<Page>();
            // Mock Application.Current.MainPage
            var mockApplication = new Mock<Application>();
            mockApplication.SetupGet(app => app.MainPage).Returns(_mockMainPage.Object);
            Application.SetCurrentApplication(mockApplication.Object);

            _alertService = new AlertService();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up the mocked application
            Application.SetCurrentApplication(null);
        }

        [Test]
        public async Task DisplayAlert_CallsMainPageDisplayAlert()
        {
            // Arrange
            string title = "Test Title";
            string message = "Test Message";
            string cancel = "OK";

            _mockMainPage.Setup(mp => mp.DisplayAlert(title, message, cancel))
                         .Returns(Task.CompletedTask);

            // Act
            await _alertService.DisplayAlert(title, message, cancel);

            // Assert
            _mockMainPage.Verify(mp => mp.DisplayAlert(title, message, cancel), Times.Once);
        }

        [Test]
        public async Task DisplayConfirmation_CallsMainPageDisplayAlertAndReturnsResult()
        {
            // Arrange
            string title = "Confirm Title";
            string message = "Confirm Message";
            string accept = "Yes";
            string cancel = "No";
            bool expectedResult = true;

            _mockMainPage.Setup(mp => mp.DisplayAlert(title, message, accept, cancel))
                         .ReturnsAsync(expectedResult);

            // Act
            bool actualResult = await _alertService.DisplayConfirmation(title, message, accept, cancel);

            // Assert
            _mockMainPage.Verify(mp => mp.DisplayAlert(title, message, accept, cancel), Times.Once);
            Assert.That(actualResult, Is.EqualTo(expectedResult));
        }
    }
}