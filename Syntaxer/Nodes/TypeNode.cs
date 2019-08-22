using System.Collections.Generic;

namespace Syntaxer.Nodes
{
    public class TypeNode : BaseNode
    {
        public FeatureNameNode Anchor { get; set; }
        public string AttachmentMark { get; set; }
        public string ClassName { get; set; }
    }

    public class TupleNode : BaseNode
    {
        public object TupleParameters;
    }

    public class ClassTypeNode : BaseNode
    {
        public string AttachmentMark { get; set; }
        public string ClassName { get; set; }
        public List<TypeNode> ActualGenerics { get; set; }

    }
}