using System.Collections.Generic;

namespace Syntaxer.Nodes
{
    public class ClassNode : BaseNode
    {
        public string Name { get; set; }
        public string Obsolete { get; set; }
        public InheritanceNode Inheritance { get; }
        public List<FeatureNameNode> Creators { get; }
        public List<(FeatureNameNode, List<TypeNode>)> Converters { get; }
        public List<FeatureDeclarationNode> Features { get; }
        public List<string> Headers { get; }
        public NoteNode Notes { get; }
        public List<string> Invariant { get; }

        public ClassNode()
        {
            Inheritance = new InheritanceNode();
            Headers = new List<string>();
            Creators = new List<FeatureNameNode>();
            Converters = new List<(FeatureNameNode, List<TypeNode>)>();
            Features = new List<FeatureDeclarationNode>();
            Notes = new NoteNode();
            Invariant = new List<string>();
        }

        public class NoteNode : BaseNode
        {
            public List<string> NoteNames { get; }
            public List<List<string>> NoteContents { get; }

            public NoteNode()
            {
                NoteNames = new List<string>();
                NoteContents = new List<List<string>>();
            }

            public void Add(string name, List<string> content)
            {
                NoteNames.Add(name);
                NoteContents.Add(content);
            }
        }


        public class InheritanceNode : BaseNode
        {
            public bool NonConformance { get; set; } = false;
            public List<ClassTypeNode> ParentList { get; set; }
        }

    }
}