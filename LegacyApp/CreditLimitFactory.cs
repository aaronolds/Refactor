
namespace LegacyApp
{
    public class CreditLimitFactory : ICreditLimitFactory
    {
        private readonly IUserCreditService _userCreditService;

        public CreditLimitFactory(IUserCreditService userCreditService)
        {
            _userCreditService = userCreditService;
        }

        public ICreditRule GetRule(string clientType)
        {
            switch (clientType)
            {
                case "VeryImportantClient":
                    return new VeryImportantClient();
                case "ImportantClient":
                    return new ImportantClient(_userCreditService);
                default:
                    return new DefaultCredit(_userCreditService);
            };
        }
    }
}
