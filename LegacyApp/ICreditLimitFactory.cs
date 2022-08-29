
namespace LegacyApp
{
    public interface ICreditLimitFactory
    {
        public ICreditRule GetRule(string clientType);
    }
}
