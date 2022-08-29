
namespace LegacyApp
{
    public class ClientRepository : IClientRepository
    {
        private readonly UserDataAccessProxy _userDataAccessProxy;

        public ClientRepository() : this(new UserDataAccessProxy())
        {
        }

        public ClientRepository(UserDataAccessProxy userDataAccessProxy)
        {
            _userDataAccessProxy = userDataAccessProxy;
        }

        public Client GetById(int id)
        {
            User? user = _userDataAccessProxy.GetUsers.Where(u => u.Client?.Id == id).FirstOrDefault();
            return user?.Client!;
        }
    }
}