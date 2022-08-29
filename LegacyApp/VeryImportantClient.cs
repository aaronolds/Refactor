
namespace LegacyApp
{
    public class VeryImportantClient : ICreditRule
    {
        public CreditLimit GetCreditLimit(User user)
        {                   
            return new CreditLimit(false, 0);
        }
    }
}