using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Syntaxer.Nodes
{
    public class ConstantValueNode : BaseNode
    {
        public VariableType Type;
        public string Value;

        public ConstantValueNode(string value)
        {
            Value = value;
        }

        public object GetValue()
        {
            switch (Type)
            {
                case VariableType.Integer:
                    return int.Parse(Value);
                    break;
                case VariableType.Real:
                    return double.Parse(Value);
                    break;
                case VariableType.String:
                    return Value;
                    break;
            }

            return null;
        }

    }

    public enum VariableType
    {
        Integer,
        Real,
        String
    }
}
