using System.Collections.Generic;

namespace Syntaxer.Nodes
{
    public class FormalArgumentNode :BaseNode
    {
        public EntityDeclarationList EntityDeclarationList = new EntityDeclarationList();

    }

    public class EntityDeclarationList :BaseNode
    {
        public List<(List<string> IdentifierList, TypeNode Type)> EntityDeclarationGroup = new List<(List<string> IdentifierList, TypeNode Type)>();
    }
}