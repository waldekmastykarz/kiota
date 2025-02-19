﻿using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners {
    public class JavaRefiner : CommonLanguageRefiner, ILanguageRefiner
    {
        public JavaRefiner(GenerationConfiguration configuration) : base(configuration) {}
        public override void Refine(CodeNamespace generatedCode)
        {
            AddInnerClasses(generatedCode, false);
            AndInsertOverrideMethodForRequestExecutorsAndBuilders(generatedCode);
            ReplaceIndexersByMethodsWithParameter(generatedCode, generatedCode);
            ConvertUnionTypesToWrapper(generatedCode);
            AddRequireNonNullImports(generatedCode);
            FixReferencesToEntityType(generatedCode);
            AddPropertiesAndMethodTypesImports(generatedCode, true, false, true);
            AddDefaultImports(generatedCode, defaultNamespaces, defaultNamespacesForModels, defaultNamespacesForRequestBuilders);
            CorrectCoreType(generatedCode, CorrectMethodType, CorrectPropertyType);
            PatchHeaderParametersType(generatedCode, "Map<String, String>");
            AddListImport(generatedCode);
            AddParsableInheritanceForModelClasses(generatedCode);
            ReplaceBinaryByNativeType(generatedCode, "InputStream", "java.io", true);
            AddEnumSetImport(generatedCode);
            ReplaceReservedNames(generatedCode, new JavaReservedNamesProvider(), x => $"{x}_escaped");
            AddGetterAndSetterMethods(generatedCode, new() {
                                                    CodePropertyKind.Custom,
                                                    CodePropertyKind.AdditionalData,
                                                    CodePropertyKind.BackingStore,
                                                }, _configuration.UsesBackingStore, true);
            AddConstructorsForDefaultValues(generatedCode, true);
            CorrectCoreTypesForBackingStore(generatedCode, "com.microsoft.kiota.store", "BackingStoreFactorySingleton.instance.createBackingStore()");
            ReplaceDefaultSerializationModules(generatedCode, "com.microsoft.kiota.serialization.JsonSerializationWriterFactory");
            ReplaceDefaultDeserializationModules(generatedCode, "com.microsoft.kiota.serialization.JsonParseNodeFactory");
            AddSerializationModulesImport(generatedCode,
                                        new [] { "com.microsoft.kiota.ApiClientBuilder",
                                                "com.microsoft.kiota.serialization.SerializationWriterFactoryRegistry" },
                                        new [] { "com.microsoft.kiota.serialization.ParseNodeFactoryRegistry" });
        }
        private static void AddEnumSetImport(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model) &&
                currentClass.GetChildElements(true).OfType<CodeProperty>().Any(x => x.Type is CodeType xType && xType.TypeDefinition is CodeEnum xEnumType && xEnumType.Flags)) {
                    var nUsing = new CodeUsing(currentClass) {
                        Name = "EnumSet",
                    };
                    nUsing.Declaration = new CodeType(nUsing) { Name = "java.util", IsExternal = true };
                    currentClass.AddUsing(nUsing);
                }

            CrawlTree(currentElement, AddEnumSetImport);
        }
        private static void AddParsableInheritanceForModelClasses(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass && currentClass.IsOfKind(CodeClassKind.Model)) {
                var declaration = currentClass.StartBlock as CodeClass.Declaration;
                declaration.Implements.Add(new CodeType(currentClass) {
                    IsExternal = true,
                    Name = $"Parsable",
                });
            }
            CrawlTree(currentElement, AddParsableInheritanceForModelClasses);
        }
        private static void AddListImport(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass) {
                var childElements = currentClass.GetChildElements(true);
                if(childElements.OfType<CodeProperty>().Any(x => x.Type?.CollectionKind == CodeType.CodeTypeCollectionKind.Complex) ||
                    childElements.OfType<CodeMethod>().Any(x => x.ReturnType?.CollectionKind == CodeType.CodeTypeCollectionKind.Complex) ||
                    childElements.OfType<CodeMethod>().Any(x => x.Parameters.Any(y => y.Type.CollectionKind == CodeType.CodeTypeCollectionKind.Complex))) {
                        var nUsing = new CodeUsing(currentClass) {
                            Name = "List"
                        };
                        nUsing.Declaration = new CodeType(nUsing) { Name = "java.util", IsExternal = true };
                        currentClass.AddUsing(nUsing);
                }
            }
            CrawlTree(currentElement, AddListImport);
        }
        private static readonly Tuple<string, string>[] defaultNamespacesForRequestBuilders = new Tuple<string, string>[] { 
            new ("HttpCore", "com.microsoft.kiota"),
            new ("HttpMethod", "com.microsoft.kiota"),
            new ("RequestInfo", "com.microsoft.kiota"),
            new ("ResponseHandler", "com.microsoft.kiota"),
            new ("QueryParametersBase", "com.microsoft.kiota"),
            new ("MiddlewareOption", "com.microsoft.kiota"),
            new ("Map", "java.util"),
            new ("URI", "java.net"),
            new ("URISyntaxException", "java.net"),
            new ("InputStream", "java.io"),
            new ("Function", "java.util.function"),
            new ("Collection", "java.util"),
        };
        private static readonly Tuple<string, string>[] defaultNamespaces = new Tuple<string, string>[] { 
            new ("SerializationWriter", "com.microsoft.kiota.serialization"),
        };
        private static readonly Tuple<string, string>[] defaultNamespacesForModels = new Tuple<string, string>[] { 
            new ("ParseNode", "com.microsoft.kiota.serialization"),
            new ("Parsable", "com.microsoft.kiota.serialization"),
            new ("BiConsumer", "java.util.function"),
            new ("Map", "java.util"),
            new ("HashMap", "java.util"),
        };
        private static void CorrectPropertyType(CodeProperty currentProperty) {
            if(currentProperty.IsOfKind(CodePropertyKind.HttpCore))
                    currentProperty.Type.Name = "HttpCore";
            else if(currentProperty.IsOfKind(CodePropertyKind.BackingStore))
                currentProperty.Type.Name = currentProperty.Type.Name[1..]; // removing the "I"
            else if("DateTimeOffset".Equals(currentProperty.Type.Name, StringComparison.OrdinalIgnoreCase)) {
                currentProperty.Type.Name = $"OffsetDateTime";
                var nUsing = new CodeUsing(currentProperty.Parent) {
                    Name = "OffsetDateTime",
                };
                nUsing.Declaration = new CodeType(nUsing) {
                    Name = "java.time",
                    IsExternal = true,
                };
                (currentProperty.Parent as CodeClass).AddUsing(nUsing);
            } else if(currentProperty.IsOfKind(CodePropertyKind.AdditionalData)) {
                currentProperty.Type.Name = "Map<String, Object>";
                currentProperty.DefaultValue = "new HashMap<>()";
            }
        }
        private static void CorrectMethodType(CodeMethod currentMethod) {
            if(currentMethod.IsOfKind(CodeMethodKind.RequestExecutor, CodeMethodKind.RequestGenerator)) {
                if(currentMethod.IsOfKind(CodeMethodKind.RequestExecutor))
                    currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.ResponseHandler) && x.Type.Name.StartsWith("i", StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Type.Name = x.Type.Name[1..]);
                currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Options)).ToList().ForEach(x => x.Type.Name = "Collection<MiddlewareOption>");
            }
            else if(currentMethod.IsOfKind(CodeMethodKind.Serializer))
                currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.Serializer) && x.Type.Name.StartsWith("i", StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => x.Type.Name = x.Type.Name[1..]);
            else if(currentMethod.IsOfKind(CodeMethodKind.Deserializer)) {
                currentMethod.ReturnType.Name = $"Map<String, BiConsumer<T, ParseNode>>";
                currentMethod.Name = "getFieldDeserializers";
            }
            else if(currentMethod.IsOfKind(CodeMethodKind.ClientConstructor))
                currentMethod.Parameters.Where(x => x.IsOfKind(CodeParameterKind.HttpCore, CodeParameterKind.BackingStore))
                    .Where(x => x.Type.Name.StartsWith("I", StringComparison.OrdinalIgnoreCase))
                    .ToList()
                    .ForEach(x => x.Type.Name = x.Type.Name[1..]); // removing the "I"
        }
        private static void AddRequireNonNullImports(CodeElement currentElement) {
            if(currentElement is CodeMethod currentMethod && currentMethod.Parameters.Any(x => !x.Optional)) {
                var parentClass = currentMethod.Parent as CodeClass;
                var newUsing = new CodeUsing(parentClass) {
                    Name = "Objects",
                };
                newUsing.Declaration = new CodeType(newUsing) {
                    Name = "java.util",
                    IsExternal = true,
                };
                parentClass?.AddUsing(newUsing);
            }
            CrawlTree(currentElement, AddRequireNonNullImports);
        }
        private static void AndInsertOverrideMethodForRequestExecutorsAndBuilders(CodeElement currentElement) {
            if(currentElement is CodeClass currentClass) {
                var codeMethods = currentClass.GetChildElements(true).OfType<CodeMethod>();
                if(codeMethods.Any()) {
                    var originalExecutorMethods = codeMethods.Where(x => x.IsOfKind(CodeMethodKind.RequestExecutor));
                    var executorMethodsToAdd = originalExecutorMethods
                                        .Select(x => GetMethodClone(x, CodeParameterKind.ResponseHandler))
                                        .Union(originalExecutorMethods
                                                .Select(x => GetMethodClone(x, CodeParameterKind.Options, CodeParameterKind.ResponseHandler)))
                                        .Union(originalExecutorMethods
                                                .Select(x => GetMethodClone(x, CodeParameterKind.Headers, CodeParameterKind.Options, CodeParameterKind.ResponseHandler)))
                                        .Union(originalExecutorMethods
                                                .Select(x => GetMethodClone(x, CodeParameterKind.QueryParameter, CodeParameterKind.Headers, CodeParameterKind.Options, CodeParameterKind.ResponseHandler)))
                                        .Where(x => x != null);
                    var originalGeneratorMethods = codeMethods.Where(x => x.IsOfKind(CodeMethodKind.RequestGenerator));
                    var generatorMethodsToAdd = originalGeneratorMethods
                                        .Select(x => GetMethodClone(x, CodeParameterKind.Options))
                                        .Union(originalGeneratorMethods
                                                .Select(x => GetMethodClone(x, CodeParameterKind.Headers, CodeParameterKind.Options)))
                                        .Union(originalGeneratorMethods
                                                .Select(x => GetMethodClone(x, CodeParameterKind.QueryParameter, CodeParameterKind.Headers, CodeParameterKind.Options)))
                                        .Where(x => x != null);
                    if(executorMethodsToAdd.Any() || generatorMethodsToAdd.Any())
                        currentClass.AddMethod(executorMethodsToAdd.Union(generatorMethodsToAdd).ToArray());
                }
            }
            
            CrawlTree(currentElement, AndInsertOverrideMethodForRequestExecutorsAndBuilders);
        }
        private static CodeMethod GetMethodClone(CodeMethod currentMethod, params CodeParameterKind[] parameterTypesToExclude) {
            if(currentMethod.Parameters.Any(x => parameterTypesToExclude.Contains(x.ParameterKind))) {
                var cloneMethod = currentMethod.Clone() as CodeMethod;
                cloneMethod.Parameters.RemoveAll(x => parameterTypesToExclude.Contains(x.ParameterKind));
                cloneMethod.OriginalMethod = currentMethod;
                return cloneMethod;
            }
            else return null;
        }
    }
}
