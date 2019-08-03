using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Syntaxer.Nodes
{
    public class PrototypeNode : BaseNode
    {
        public String Name;
        public List<BaseNode> Args;
        public PrototypeNode(string name, List<BaseNode> args)
        {
            Name = name;
            Args = args;
        }
    }

    public class FunctionNode : BaseNode
    {
        public PrototypeNode Prototype;
        public BaseNode Body;

        public FunctionNode(PrototypeNode prototypeNode, BaseNode body)
        {
            Prototype = prototypeNode;
            Body = body;
        }
    }
}
