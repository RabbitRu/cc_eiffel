using System.Collections.Generic;
using Syntaxer.Nodes;

namespace Syntaxer
{
    public class FeatureNode : BaseNode
    {
        public List<FeatureDeclarationNode> FeatureParts { get; set; }
        public TypeNode QueryMarkType { get; set; }
        public FeatureNameNode QueryMarkAssigner { get; set; }
    }

    public class FeatureNameNode : BaseNode
    {
        public string Name { get; set; }
        public string AliasName { get; set; }
        public bool Convert { get; set; } = false;
    }

    public class FeatureDeclarationNode : BaseNode
    {
        public List<(bool frozen,FeatureNameNode FeatureName)> NewFeatureList { get; set; }
        public TypeNode QueryMarkType { get; set; }
        public FeatureNameNode QueryMarkAssigner { get; set; }
        public string ExplicitValue { get; set; }
        public string Obsolete { get; set; }
    }

    public class FeatureCallNode : BaseNode
    {
        public BaseNode FirstPart { get; set; }
        public UnqualifiedCallNode UnqualifiedCall { get; set; }
    }

    public class UnqualifiedCallNode : BaseNode
    {
        public FeatureNameNode FeatureName { get; set; }
        public List<ExpressionNode> ActualArguments { get; set; }
    }
}