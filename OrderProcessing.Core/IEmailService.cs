using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderProcessing.Core
{
    public interface IEmailService
    {
        void SendOrderConfirmation(string customerEmail, int orderId);
    }
}
