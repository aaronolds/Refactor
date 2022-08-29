
using LegacyApp;

AddUser(args);

static void AddUser(string[] args) 
{
    var userService = new UserService();
    var addResult = userService.AddUser("John", "Doe", "John.doe@gmail.com", new DateTime(1993, 1, 1), 4);
    Console.WriteLine("Adding John Doe was " + (addResult ? "successful" : "unsuccessful"));
}