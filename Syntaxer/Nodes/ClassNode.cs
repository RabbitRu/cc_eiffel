using System.Collections.Generic;

namespace Syntaxer.Nodes
{
    public class ClassNode : BaseNode
    {
        public string Name;
        public string Obsolete;
        public List<string> Inheritance;
        public List<string> Creators;
        public List<string> Converters;
        public List<string> Features;
        public NoteNode Notes;
        public List<string> Invariant;

        public ClassNode()
        {
            Inheritance = new List<string>();
            Creators = new List<string>();
            Converters = new List<string>();
            Features = new List<string>();
            Notes = new NoteNode();
            Invariant = new List<string>();
        }

        public class NoteNode :BaseNode
        {
            public List<string> NoteNames;
            public List<List<string>> NoteContents;

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

    }
}