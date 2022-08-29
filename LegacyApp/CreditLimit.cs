
namespace LegacyApp
{
    public class CreditLimit
    {
        public bool HasLimit { get; }
        public int Limit { get; }
        public CreditLimit(bool hasLimit, int limit)
        {
            HasLimit = hasLimit;
            Limit = limit;
        }
    }
}
