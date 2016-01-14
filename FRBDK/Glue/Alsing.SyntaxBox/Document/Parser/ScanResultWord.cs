namespace Alsing.SourceCode.SyntaxDocumentParsers
{

    public class ScanResultWord
    {
        public bool HasContent;
        public PatternList ParentList;
        public Pattern Pattern;
        public int Position;
        public string Token = "";
    }
}
