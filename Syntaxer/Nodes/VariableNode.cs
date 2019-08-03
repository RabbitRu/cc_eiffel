namespace Syntaxer.Nodes
{
    public class VariableNode : BaseNode
    {
        public string VariableName;
        public string VariableType;

        public VariableNode(string variableName, string variableType = "unknown")
        {
            VariableName = variableName;
            VariableType = variableType;
        }
    }
}