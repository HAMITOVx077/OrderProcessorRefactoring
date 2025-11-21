using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderProcessing.Core
{
    public class Order
    {
        public int Id { get; set; }
        public string CustomerEmail { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsProcessed { get; set; }
    }
}
