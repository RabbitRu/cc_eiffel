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

    }
}