using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsSandboxing
{
    public interface ICallable
    {
        void CallMe(string data);
        Type TestType();
    }
}
