using System;
using System.IO;
using Xunit;

namespace Kiota.Builder.Writers.Ruby.Tests {
    public class CodeClassDeclarationWriterTests : IDisposable
    {
        private const string defaultPath = "./";
        private const string defaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeClassDeclarationWriter codeElementWriter;
        private readonly CodeClass parentClass;

        public CodeClassDeclarationWriterTests() {
            codeElementWriter = new CodeClassDeclarationWriter(new RubyConventionService());
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Ruby, defaultPath, defaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            var root = CodeNamespace.InitRootNamespace();
            parentClass = new (root) {
                Name = "parentClass"
            };
            root.AddClass(parentClass);
        }
        public void Dispose() {
            tw?.Dispose();
        }
        [Fact]
        public void WritesSimpleDeclaration() {
            codeElementWriter.WriteCodeElement(parentClass.StartBlock as CodeClass.Declaration, writer);
            var result = tw.ToString();
            Assert.Contains("class", result);
        }
        [Fact]
        public void WritesImplementation() {
            var declaration = parentClass.StartBlock as CodeClass.Declaration;
            declaration.Implements.Add(new (parentClass){
                Name = "someInterface"
            });
            codeElementWriter.WriteCodeElement(declaration, writer);
            var result = tw.ToString();
            Assert.Contains("include", result);
        }
        [Fact]
        public void WritesInheritance() {
            var declaration = parentClass.StartBlock as CodeClass.Declaration;
            declaration.Inherits = new (parentClass){
                Name = "someInterface"
            };
            codeElementWriter.WriteCodeElement(declaration, writer);
            var result = tw.ToString();
            Assert.Contains("<", result);
        }
        [Fact]
        public void WritesImports() {
            var declaration = parentClass.StartBlock as CodeClass.Declaration;
            declaration.Usings.Add(new (parentClass) {
                Name = "Objects",
                Declaration = new(parentClass) {
                    Name = "util",
                    IsExternal = true,
                }
            });
            declaration.Usings.Add(new (parentClass) {
                Name = "project-graph",
                Declaration = new(parentClass) {
                    Name = "Message",
                }
            });
            codeElementWriter.WriteCodeElement(declaration, writer);
            var result = tw.ToString();
            Assert.Contains("require_relative", result);
            Assert.Contains("message", result);
            Assert.Contains("require", result);
        }
        [Fact]
        public void WritesMixins() {
            var declaration = parentClass.StartBlock as CodeClass.Declaration;
            declaration.Implements.Add(new (parentClass) {
                Name = "test"
            });
            codeElementWriter.WriteCodeElement(declaration, writer);
            var result = tw.ToString();
            Assert.Contains("include", result);
            Assert.Contains("test", result);
        }
    }
}
