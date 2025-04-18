using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Models
{
    public class User
    {
        public string Email { get; set; }
        public string Password { get; set; } // plaintext for now, but could be hashed later
        public string Role { get; set; } // "runner" or "crew"
        public string Name { get; set; } // this will be the name of the runner
    }

}