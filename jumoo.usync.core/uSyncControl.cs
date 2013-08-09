using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jumoo.usync.core
{
    public interface IUSyncControl
    {
         void Import();
         void Export();
         void Attach(); 
    }
}
