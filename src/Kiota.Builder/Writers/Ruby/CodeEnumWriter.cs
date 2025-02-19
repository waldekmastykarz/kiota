using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Ruby {
    public class CodeEnumWriter : BaseElementWriter<CodeEnum, RubyConventionService>
    {
        public CodeEnumWriter(RubyConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer)
        {
            if(!(codeElement?.Options.Any() ?? false))
                return;
            if(codeElement.Parent is CodeNamespace ns) {
                writer.WriteLine($"module {ns.Name.NormalizeNameSpaceName("::")}");
                writer.IncreaseIndent();
            }
            conventions.WriteShortDescription(codeElement.Description, writer);
            writer.WriteLine($"{codeElement.Name.ToFirstCharacterUpperCase()} = {{");
            writer.IncreaseIndent();
            codeElement.Options.ForEach(x => writer.WriteLine($"{x.ToFirstCharacterUpperCase()}: :{x.ToFirstCharacterUpperCase()},"));
            writer.DecreaseIndent();
            writer.WriteLine("}");
            if(codeElement?.Parent?.Parent is CodeNamespace) {
                writer.DecreaseIndent();
                writer.WriteLine("end");
            }
            
        }
    }
}
