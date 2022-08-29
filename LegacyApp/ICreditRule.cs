
namespace LegacyApp
{
    public interface ICreditRule
    {
        CreditLimit GetCreditLimit(User user);
    }
}
