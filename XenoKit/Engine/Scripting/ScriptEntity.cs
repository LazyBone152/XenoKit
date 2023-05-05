using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XenoKit.Engine.Scripting
{
    /// <summary>
    /// Base class for all scripting instances.
    /// </summary>
    public class ScriptEntity
    {
        //With this value we can know whether this script is still active or not
        public bool InScope { get; protected set; } = true;

    }
}
