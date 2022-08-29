
namespace LegacyApp
{
    public class UserService
    {
        private readonly ICreditLimitFactory _creditLimitFactory;
        private readonly IClientRepository _clientRepository;
        private readonly IUserDataAccess _userDataAccess;

        public UserService(ICreditLimitFactory creditLimitFactory, IClientRepository clientRepository, IUserDataAccess userDataAccess)
        {
            _creditLimitFactory = creditLimitFactory;
            _clientRepository = clientRepository;
            _userDataAccess = userDataAccess;
        }

        public UserService()
        {
            var userCreditService = new UserCreditServiceClient();
            _creditLimitFactory = new CreditLimitFactory(userCreditService);
            _clientRepository = new ClientRepository();
            _userDataAccess = new UserDataAccessProxy();
        }        

        public bool AddUser(string firname, string surname, string email, DateTime dateOfBirth, int clientId)
        {
            if (string.IsNullOrEmpty(firname) || string.IsNullOrEmpty(surname))
            {
                return false;
            }

            if (!email.Contains("@") && !email.Contains("."))
            {
                return false;
            }

            var now = DateTime.Now;
            int age = now.Year - dateOfBirth.Year;

            if (now.Month < dateOfBirth.Month || (now.Month == dateOfBirth.Month && now.Day < dateOfBirth.Day)) age--;

            if (age < 21)
            {
                return false;
            }

            var client = _clientRepository.GetById(clientId);
            var user = new User
            {
                Client = client,
                DateOfBirth = dateOfBirth,
                EmailAddress = email,
                Firstname = firname,
                Surname = surname
            };

            var creditRule = _creditLimitFactory.GetRule(client.Name);
            var creditLimit = creditRule.GetCreditLimit(user);
            user.HasCreditLimit = creditLimit.HasLimit;
            user.CreditLimit = creditLimit.Limit;

            if (user.IsUserEligible)
            {
                return false;
            }

            _userDataAccess.AddUser(user);
            return true;
        }
    }
}