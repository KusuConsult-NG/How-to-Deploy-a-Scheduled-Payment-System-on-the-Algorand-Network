using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlgorandAutomatedPayment.Services
{
    public interface IPayments
    {
        Task OneTimePayment();
        Task GroupedPayments();
    }
}
