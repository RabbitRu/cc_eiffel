using Syntaxer.Nodes;

namespace Syntaxer
{
    public class FeatureNode : BaseNode
    {
        public FeatureNameNode FeatureName { get; }
    }

    public class FeatureNameNode : BaseNode
    {
        public string Name { get; set; }
        public string AliasName { get; set; }
        public bool Convert { get; set; } = false;
    }
}