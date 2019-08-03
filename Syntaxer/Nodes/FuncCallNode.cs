using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Syntaxer.Nodes
{
    public class FuncCallNode : BaseNode
    {
        public string Callee;
        public List<BaseNode> Args;

        public FuncCallNode(string callee, List<BaseNode> args)
        {
            Callee = callee;
            Args = args;
        }
    }
}
