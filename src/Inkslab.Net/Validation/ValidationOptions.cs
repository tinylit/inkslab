using System;
using System.Collections.Generic;

namespace Inkslab.Net.Validation
{
    /// <summary>Immutable value options for explicit final HTTP response entity validation.</summary>
    public readonly struct ValidationOptions
    {
        /// <summary>Default options: reject null entities, without context services or items.</summary>
        public static ValidationOptions Default => default;

        /// <summary>Whether a null entity is accepted. Defaults to false.</summary>
        public bool AllowNull { get; init; }
        /// <summary>Services available to validation attributes and object rules.</summary>
        public IServiceProvider ServiceProvider { get; init; }
        /// <summary>Context values shallow-copied when validation is configured; referenced values are not cloned.</summary>
        public IDictionary<object, object> Items { get; init; }

        internal static ValidationOptions Capture(ValidationOptions options)
        {
            if (options.Items is null) { return options; }

            return new ValidationOptions
            {
                AllowNull = options.AllowNull,
                ServiceProvider = options.ServiceProvider,
                Items = options.Items.Count == 0 ? null : new Dictionary<object, object>(options.Items)
            };
        }
    }
}
