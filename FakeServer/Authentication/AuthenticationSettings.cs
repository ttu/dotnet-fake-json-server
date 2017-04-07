using System.Collections.Generic;

namespace FakeServer.Authentication
{
    public class AuthenticationSettings
    {
        public bool Enabled { get; set; }

        public IEnumerable<User> Users { get; set; }
    }

    public class User
    {
        public string Username { get; set; }

        public string Password { get; set; }
    }
}