using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Latency
{
    public interface IUserTracker<out THub>
    {
        void AddUser();
        void RemoveUser();
        long Users();
    }
}
