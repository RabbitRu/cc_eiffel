using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Syntaxer.Nodes
{
    public class BinaryOperatorNode : BaseNode
    {
        public string Op;
        public BaseNode LHS, RHS;

        public BinaryOperatorNode(string op, BaseNode lhs, BaseNode rhs)
        {
            Op = op;
            LHS = lhs;
            RHS = rhs;
        }

    }
}
