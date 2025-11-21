using Moq;
using NUnit.Framework;
using OrderProcessing.Core;

namespace OrderProcessing.Tests
{
    [TestFixture]
    public class OrderProcessorTests
    {
        private Mock<IDatabase> _mockDatabase;
        private Mock<IEmailService> _mockEmailService;
        private OrderProcessor _processor;

        [SetUp]
        public void Setup()
        {
            _mockDatabase = new Mock<IDatabase>();
            _mockEmailService = new Mock<IEmailService>();
            _processor = new OrderProcessor(_mockDatabase.Object, _mockEmailService.Object);
        }

        [Test]
        public void ProcessOrder_OrderIsNull_ThrowsArgumentNullException()
        {
            //Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => _processor.ProcessOrder(null));
        }

        [Test]
        public void ProcessOrder_TotalAmountZero_ReturnsFalse()
        {
            //Arrange
            var order = new Order { TotalAmount = 0 };

            //Act
            bool result = _processor.ProcessOrder(order);

            //Assert
            Assert.IsFalse(result);
            _mockDatabase.Verify(db => db.Save(It.IsAny<Order>()), Times.Never);
            _mockEmailService.Verify(es => es.SendOrderConfirmation(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void ProcessOrder_TotalAmountNegative_ReturnsFalse()
        {
            //Arrange
            var order = new Order { TotalAmount = -50 };

            //Act
            bool result = _processor.ProcessOrder(order);

            //Assert
            Assert.IsFalse(result);
            _mockDatabase.Verify(db => db.Save(It.IsAny<Order>()), Times.Never);
            _mockEmailService.Verify(es => es.SendOrderConfirmation(It.IsAny<string>(),
                It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void ProcessOrder_ValidOrderOver100_SavesOrderAndSendsEmail()
        {
            //Arrange
            _mockDatabase.Setup(db => db.IsConnected).Returns(true);
            var order = new Order { Id = 1, CustomerEmail = "test@mail.com", TotalAmount = 150 };

            //Act
            bool result = _processor.ProcessOrder(order);

            //Assert
            Assert.IsTrue(result);
            Assert.IsTrue(order.IsProcessed);
            _mockDatabase.Verify(db => db.Save(order), Times.Once);
            _mockEmailService.Verify(es => es.SendOrderConfirmation("test@mail.com", 1), Times.Once);
        }

        [Test]
        public void ProcessOrder_ValidOrderExactly100_DoesNotSendEmail()
        {
            //Arrange
            _mockDatabase.Setup(db => db.IsConnected).Returns(true);
            var order = new Order { Id = 2, CustomerEmail = "customer@mail.com", TotalAmount = 100 };

            //Act
            bool result = _processor.ProcessOrder(order);

            //Assert
            Assert.IsTrue(result);
            Assert.IsTrue(order.IsProcessed);
            _mockDatabase.Verify(db => db.Save(order), Times.Once);
            _mockEmailService.Verify(es => es.SendOrderConfirmation(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void ProcessOrder_ValidOrderUnder100_DoesNotSendEmail()
        {
            //Arrange
            _mockDatabase.Setup(db => db.IsConnected).Returns(true);
            var order = new Order { Id = 3, CustomerEmail = "customer@mail.com", TotalAmount = 99 };

            //Act
            bool result = _processor.ProcessOrder(order);

            //Assert
            Assert.IsTrue(result);
            Assert.IsTrue(order.IsProcessed);
            _mockDatabase.Verify(db => db.Save(order), Times.Once);
            _mockEmailService.Verify(es => es.SendOrderConfirmation(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void ProcessOrder_DatabaseNotConnected_ConnectsBeforeSaving()
        {
            //Arrange
            _mockDatabase.Setup(db => db.IsConnected).Returns(false);
            var order = new Order { TotalAmount = 50 };

            //Act
            bool result = _processor.ProcessOrder(order);

            //Assert
            Assert.IsTrue(result);
            _mockDatabase.Verify(db => db.Connect(), Times.Once);
            _mockDatabase.Verify(db => db.Save(order), Times.Once);
        }

        [Test]
        public void ProcessOrder_DatabaseAlreadyConnected_DoesNotConnectAgain()
        {
            //Arrange
            _mockDatabase.Setup(db => db.IsConnected).Returns(true);
            var order = new Order { TotalAmount = 50 };

            //Act
            bool result = _processor.ProcessOrder(order);

            //Assert
            Assert.IsTrue(result);
            _mockDatabase.Verify(db => db.Connect(), Times.Never);
            _mockDatabase.Verify(db => db.Save(order), Times.Once);
        }

        [Test]
        public void ProcessOrder_DatabaseSaveThrowsException_ReturnsFalse()
        {
            //Arrange
            _mockDatabase.Setup(db => db.IsConnected).Returns(true);
            _mockDatabase.Setup(db => db.Save(It.IsAny<Order>())).Throws<Exception>();
            var order = new Order { TotalAmount = 200 };

            //Act
            bool result = _processor.ProcessOrder(order);

            //Assert
            Assert.IsFalse(result);
            _mockEmailService.Verify(es => es.SendOrderConfirmation(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void ProcessOrder_EmailServiceThrowsException_StillReturnsTrueAndMarksProcessed()
        {
            // Arrange
            _mockDatabase.Setup(db => db.IsConnected).Returns(true);
            _mockEmailService.Setup(es => es.SendOrderConfirmation(It.IsAny<string>(), It.IsAny<int>()))
                           .Throws<Exception>();
            var order = new Order { Id = 4, CustomerEmail = "test@mail.com", TotalAmount = 150 };

            // Act
            bool result = _processor.ProcessOrder(order);

            // Assert
            Assert.IsTrue(result); // Теперь должно возвращать True!
            Assert.IsTrue(order.IsProcessed);
            _mockDatabase.Verify(db => db.Save(order), Times.Once);
            // Проверяем, что email сервис всё равно вызывался (несмотря на исключение)
            _mockEmailService.Verify(es => es.SendOrderConfirmation("test@mail.com", 4), Times.Once);
        }
    }
}