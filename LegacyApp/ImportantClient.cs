namespace LegacyApp
{
    public class ImportantClient : ICreditRule
    {
        private readonly IUserCreditService _userCreditService;        

        public ImportantClient(IUserCreditService userCreditService)
        {
            _userCreditService = userCreditService;  
        }

        public CreditLimit GetCreditLimit(User user)
        {
            var creditLimit = _userCreditService.GetCreditLimit(user.Firstname, user.Surname, user.DateOfBirth);
            return new CreditLimit(true, creditLimit * 2);
        }
    }
}