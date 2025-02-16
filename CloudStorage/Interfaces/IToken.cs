using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Interfaces
{
    public interface IToken
    {
        string TokenId { get; set; }
        string WebToken { get; set; }
        string RefreshToken { get; set; }
        DateTime Expires { get; set; }
    }
}
