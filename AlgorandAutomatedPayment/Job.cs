using AlgorandAutomatedPayment.Services;
using System.Threading.Tasks;

namespace AlgorandAutomatedPayment
{
    public class Job
    {
        #region Property
        private readonly IPayments _payments;
        #endregion

        #region Constructor
        public Job(IPayments payments)
        {
            _payments = payments;
        }
        #endregion
        #region Job Scheduler
        public Task OneTimePaymentJobAsync()
        {
            var result = _payments.OneTimePayment();
            return result;
        }
        #endregion

        #region Atomic Transaction Job Scheduler
        public Task GroupedPayments()
        {
            var result = _payments.GroupedPayments();
            return result;
        }
        #endregion
    }
}
