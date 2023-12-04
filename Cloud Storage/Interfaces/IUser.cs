using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Interfaces {
    public interface IUser {
        string Id { get; }
        string Username { get; set; }
        string Email { get; set; }
        string Password { get; set; }
        string PasswordSalt { get; set; }
        bool Verified { get; set; }
    }
}
