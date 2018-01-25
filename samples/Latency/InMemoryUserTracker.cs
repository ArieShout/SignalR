using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Latency
{
    public class InMemoryUserTracker<THub> : IUserTracker<THub>
    {
        private long _onLineNo;
        public long Users() => Interlocked.Read(ref _onLineNo);

	public InMemoryUserTracker()
	{
	    _onLineNo = 0;
	}

        void IUserTracker<THub>.AddUser()
        {
            Interlocked.Increment(ref _onLineNo);
        }
        
        void IUserTracker<THub>.RemoveUser()
        {
            Interlocked.Decrement(ref _onLineNo);
        }
    }
}
