using FlatRedBall.Glue.Plugins.CodeGenerators;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArticyDraftPlugin.CodeGenerators
{
    class GameDialogCodeGenerator : FullFileCodeGenerator
    {
        public override string RelativeFile => "ArticyDraft/GameDialog.Generated.cs";

        protected override string GenerateFileContents()
        {
            var toReturn =
                $@"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ArticyDraft
{{
    [XmlRoot(""Content"")]
    public class GameDialogue
    {{
        [XmlElement(""Dialogue"")]
        public List<Dialogue> DialogueList {{ get; set; }}

        [XmlElement(""Connection"")]
        public List<DialogueConnection> ConnectionList {{ get; set; }}

        public List<Dialogue> GetDialogueOptionsFor(Dialogue dialogue)
        {{
            var connections = ConnectionList.Where(c => c.Source.IdRef == dialogue.Id).Select(l => l.Target.IdRef).ToList();
            var dialogues = DialogueList.Where(d => connections.Contains(d.Id)).ToList();
            return dialogues;
        }}

        public Dialogue GetResponseFor(Dialogue dialogue)
        {{
            var connection = ConnectionList.FirstOrDefault(c => c.Source.IdRef == dialogue.Id);
            if (connection == null) return null;

            var response = DialogueList.FirstOrDefault(d => connection.Target.IdRef == d.Id);

            return response;
        }}

        public Dialogue GetResponseFor(string dialogueId)
        {{
            var connection = ConnectionList.FirstOrDefault(c => c.Source.IdRef == dialogueId);
            if (connection == null) return null;

            var response = DialogueList.FirstOrDefault(d => connection.Target.IdRef == d.Id);

            return response;
        }}

        public string GetDialogueText(string dialogueId)
        {{
            return DialogueList.FirstOrDefault(d => d.Id == dialogueId)?.DisplayText;
        }}
    }}

    public class DialogueConnection
    {{
        [XmlElement(""Source"")]
        public ConnectionLink Source;

        [XmlElement(""Target"")]
        public ConnectionLink Target;
    }}

    public class ConnectionLink
    {{
        [XmlAttribute(""IdRef"")]
        public string IdRef {{ get; set; }}

        [XmlAttribute(""PinRef"")]
        public string PinRef {{ get; set; }}
    }}

    public class Dialogue
    {{
        [XmlAttribute(""Id"")]
        public string Id {{ get; set; }}

        [XmlIgnore]
        public string DisplayName => DisplayNameClass.Text;

        [XmlIgnore]
        public string DisplayText => DisplayTextClass.Text;

        [XmlIgnore] private string inputId;

        [XmlIgnore]
        public string InputId
        {{
            get
            {{
                if (string.IsNullOrEmpty(inputId)) inputId = Pins.FirstOrDefault(p => p.Semantic == ""Input"")?.Id;
                return inputId;
            }}
        }}

        [XmlIgnore] private List<string> outputIds;

        [XmlIgnore]
        public List<string> OutputIds
        {{
            get
            {{
                return outputIds ?? (outputIds = Pins.Where(p => p.Semantic == ""Output"").Select(p => p.Id).ToList());
            }}
        }}

        [XmlElement(""DisplayName"")]
        public DialogueText DisplayNameClass {{ get; set; }}

        [XmlElement(""Text"")]
        public DialogueText DisplayTextClass {{ get; set; }}

        [XmlArray(""Pins""), XmlArrayItem(""Pin"")]
        public DialoguePin[] Pins {{ get; set; }}
    }}

    public class DialogueText
    {{
        [XmlAttribute(""Count"")]
        public int Count {{ get; set; }}

        [XmlElement(""LocalizedString"")]
        public string Text {{ get; set; }}
    }}

    public class DialoguePin
    {{
        [XmlAttribute(""Id"")]
        public string Id {{ get; set; }}

        [XmlAttribute(""Index"")]
        public int Index {{ get; set; }}

        [XmlAttribute(""Semantic"")]
        public string Semantic {{ get; set; }}
    }}
}}
";

            return toReturn;
        }
    }
}
