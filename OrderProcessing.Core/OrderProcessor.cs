using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderProcessing.Core
{
    /// <summary>
    /// Обработчик заказов с улучшенной читаемостью и обработкой ошибок
    /// </summary>
    public class OrderProcessor
    {
        private readonly IDatabase _database;
        private readonly IEmailService _emailService;
        private const decimal EMAIL_THRESHOLD_AMOUNT = 100m;

        public OrderProcessor(IDatabase database, IEmailService emailService)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        public bool ProcessOrder(Order order)
        {
            ValidateOrder(order);

            if (!IsValidOrderAmount(order))
                return false;

            EnsureDatabaseConnected();

            return ProcessOrderInternal(order);
        }

        private void ValidateOrder(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));
        }

        private bool IsValidOrderAmount(Order order)
        {
            return order.TotalAmount > 0;
        }

        private void EnsureDatabaseConnected()
        {
            if (!_database.IsConnected)
            {
                _database.Connect();
            }
        }

        private bool ProcessOrderInternal(Order order)
        {
            try
            {
                SaveOrderToDatabase(order);
                SendConfirmationEmailIfNeeded(order);
                MarkOrderAsProcessed(order);

                return true;
            }
            catch (Exception) when (IsDatabaseOperationException())
            {
                //логируем исключение базы данных
                return false;
            }
        }

        private void SaveOrderToDatabase(Order order)
        {
            _database.Save(order);
        }

        private void SendConfirmationEmailIfNeeded(Order order)
        {
            if (order.TotalAmount > EMAIL_THRESHOLD_AMOUNT)
            {
                try
                {
                    _emailService.SendOrderConfirmation(order.CustomerEmail, order.Id);
                }
                catch (Exception)
                {
                    //логируем ошибку отправки email, но не прерываем основной процесс
                }
            }
        }

        private void MarkOrderAsProcessed(Order order)
        {
            order.IsProcessed = true;
        }

        private bool IsDatabaseOperationException()
        {
            //пока возвращаем true для сохранения обратной совместимости
            return true;
        }
    }
}