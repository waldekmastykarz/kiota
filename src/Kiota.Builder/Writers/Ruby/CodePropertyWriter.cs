using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Ruby {
    public class CodePropertyWriter : BaseElementWriter<CodeProperty, RubyConventionService>
    {
        public CodePropertyWriter(RubyConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
        {
            conventions.WriteShortDescription(codeElement.Description, writer);
            var returnType = conventions.GetTypeString(codeElement.Type);
            var currentPathProperty = codeElement.Parent.GetChildElements(true).OfType<CodeProperty>().FirstOrDefault(x => x.IsOfKind(CodePropertyKind.CurrentPath));
            switch(codeElement.PropertyKind) {
                case CodePropertyKind.RequestBuilder:
                    writer.WriteLine($"def {codeElement.Name.ToSnakeCase()}()");
                    writer.IncreaseIndent();
                    var prefix = conventions.GetNormalizedNamespacePrefixForType(codeElement.Type);
                    conventions.AddRequestBuilderBody(currentPathProperty != null, returnType, writer, null, $"return {prefix}");
                    writer.DecreaseIndent();
                    writer.WriteLine("end");
                break;
                default:
                    writer.WriteLine($"@{codeElement.Name.ToSnakeCase()}");
                break;
            }
        }
    }
}
