using NUnit.Framework;
using Moq;
using Weighbridge.Services;
using System.Threading.Tasks;

namespace TestProject1
{
    [TestFixture]
    public class AlertServiceTests
    {
        private Mock<IMainPageProvider> _mockMainPageProvider;
        private AlertService _alertService;

        [SetUp]
        public void Setup()
        {
            _mockMainPageProvider = new Mock<IMainPageProvider>();
            _alertService = new AlertService(_mockMainPageProvider.Object);
        }

        [Test]
        public async Task DisplayAlert_CallsMainPageDisplayAlert()
        {
            // Arrange
            string title = "Test Title";
            string message = "Test Message";
            string cancel = "OK";

            _mockMainPageProvider.Setup(mpp => mpp.DisplayAlert(title, message, cancel))
                                 .Returns(Task.CompletedTask);

            // Act
            await _alertService.DisplayAlert(title, message, cancel);

            // Assert
            _mockMainPageProvider.Verify(mpp => mpp.DisplayAlert(title, message, cancel), Times.Once);
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

            _mockMainPageProvider.Setup(mpp => mpp.DisplayAlert(title, message, accept, cancel))
                                 .ReturnsAsync(expectedResult);

            // Act
            bool actualResult = await _alertService.DisplayConfirmation(title, message, accept, cancel);

            // Assert
            _mockMainPageProvider.Verify(mpp => mpp.DisplayAlert(title, message, accept, cancel), Times.Once);
            Assert.That(actualResult, Is.EqualTo(expectedResult));
        }
    }
}