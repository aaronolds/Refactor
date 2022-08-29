using LegacyApp;

namespace LegacyApp
{
    public class DefaultCredit : ICreditRule
    {
        private readonly IUserCreditService _userCreditService;

        public DefaultCredit(IUserCreditService userCreditService)
        {
            _userCreditService = userCreditService;
        }

        public CreditLimit GetCreditLimit(User user)
        {
            var creditLimit = _userCreditService.GetCreditLimit(user.Firstname, user.Surname, user.DateOfBirth);
            return new CreditLimit(true, creditLimit);
        }
    }
}