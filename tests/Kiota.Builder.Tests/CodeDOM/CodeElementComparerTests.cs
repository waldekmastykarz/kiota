using System;
using System.Collections.Generic;
using Xunit;

namespace Kiota.Builder.Tests {
    public class CodeElementComparerTests {
        [Fact]
        public void OrdersWithMethodWithinClass() {
            var root = CodeNamespace.InitRootNamespace();
            var comparer = new CodeElementOrderComparer();
            var codeClass = new CodeClass(root) {
                Name = "Class"
            };
            var method = new CodeMethod(codeClass);
            method.Parameters.Add(new CodeParameter(method) {
                Name = "param"
            });
            var dataSet = new List<Tuple<CodeElement, CodeElement, int>> {
                new(null, null, 0),
                new(null, new CodeClass(root), -1),
                new(new CodeClass(root), null, 1),
                new(new CodeUsing(root), new CodeProperty(root), -100),
                new(new CodeIndexer(root), new CodeProperty(root), 100),
                new(method, new CodeProperty(root), 101),
                new(method, codeClass, -99)
                
            };
            foreach(var dataEntry in dataSet) {
                Assert.Equal(dataEntry.Item3, comparer.Compare(dataEntry.Item1, dataEntry.Item2));
            }
        }
        [Fact]
        public void OrdersWithMethodsOutsideOfClass() {
            var root = CodeNamespace.InitRootNamespace();
            var comparer = new CodeElementOrderComparerWithExternalMethods();
            var codeClass = new CodeClass(root) {
                Name = "Class"
            };
            var method = new CodeMethod(codeClass);
            method.Parameters.Add(new CodeParameter(method) {
                Name = "param"
            });
            var dataSet = new List<Tuple<CodeElement, CodeElement, int>> {
                new(null, null, 0),
                new(null, new CodeClass(root), -1),
                new(new CodeClass(root), null, 1),
                new(new CodeUsing(root), new CodeProperty(root), -100),
                new(new CodeIndexer(root), new CodeProperty(root), 100),
                new(method, new CodeProperty(root), 101),
                new(method, codeClass, 101)
                
            };
            foreach(var dataEntry in dataSet) {
                Assert.Equal(dataEntry.Item3, comparer.Compare(dataEntry.Item1, dataEntry.Item2));
            }
        }
    }
}
