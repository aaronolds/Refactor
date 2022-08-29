
namespace LegacyApp
{
    public interface IUserDataAccess
    {
        void AddUser(User user);
        void CreateClient(int id, string clientType);
    }

    public class UserDataAccessProxy : IUserDataAccess
    {
        private static List<Client> fakeClients = new();
        private static List<User> fakeUserRepo = new();
        public List<User> GetUsers { get { return fakeUserRepo; } }

        public UserDataAccessProxy()
        {            
        }
        public void AddUser(User user)
        {
            fakeUserRepo.Add(user);
        }

        public void CreateClient(int Id, string clientType)
        { 
            Client? client = null;

            if (!fakeClients.Any(c => c.Id == Id))
            { 
                client = new() { Id = Id, Name = clientType, ClientStatus = ClientStatus.none };
                fakeClients.Add(client);
            }
        }
    }
}
