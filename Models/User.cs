using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppTeste.Models
{
    public class User
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string AccessToken { get; set; }
        public DateTime AuthenticatedAt { get; set; }

        public User()
        {
            AuthenticatedAt = DateTime.UtcNow;
        }

        public bool IsTokenValid(int expirationHours = 24)
        {
            return (DateTime.UtcNow - AuthenticatedAt).TotalHours < expirationHours;
        }
    }
}
