using System;
using System.Linq;
using System.Collections.Generic;

namespace Microsoft.Kiota.Abstractions {
    public abstract class QueryParametersBase {
        /// <summary>
        /// Vanity method to add the query parameters to the request query parameters dictionary.
        /// </summary>
        public void AddQueryParameters(IDictionary<string, object> target) {
            if (target == null) throw new ArgumentNullException(nameof(target));
            foreach(var property in this.GetType()
                                        .GetProperties()
                                        .Where(x => !target.ContainsKey(x.Name))) {
                target.Add(property.Name, property.GetValue(this));
            }
        }
    }
}
