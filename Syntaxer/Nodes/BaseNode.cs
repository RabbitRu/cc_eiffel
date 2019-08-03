using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Syntaxer.Nodes
{
    public abstract class BaseNode
    {
        public BaseNode(){}
    }

    public class RootNode : BaseNode
    {
        public List<BaseNode> Children;

        public RootNode()
        {
            Children = new List<BaseNode>();
        }
    }
}
