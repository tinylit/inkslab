using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Inkslab.Annotations;

namespace Inkslab.Net.Validation
{
    internal static class EntityValidator
    {
        internal static void ValidateInput(object entity, Type declaredType = null)
        {
            if (HasMarker(entity?.GetType() ?? declaredType, typeof(ValidateInputAttribute)))
            {
                Validate(entity, ValidationStage.Request);
            }
        }

        internal static void ValidateOutput<T>(T entity, ValidationOptions? explicitOptions = null)
        {
            if (explicitOptions.HasValue)
            {
                ValidateExplicit(entity, ValidationStage.Response, explicitOptions.Value);
            }
            else if (HasMarker(entity is null ? typeof(T) : entity.GetType(), typeof(ValidateOutputAttribute)))
            {
                Validate(entity, ValidationStage.Response);
            }
        }

        private static bool HasMarker(Type type, Type markerType)
        {
            if (type is null) { return false; }
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type.IsDefined(markerType, inherit: false);
        }

        // Explicit validation excludes containers; an automatic marker opts its type in directly.
        private static bool IsExplicitValidationCandidate(Type type)
        {
            if (type.IsSimple() || type == typeof(object) || type.IsKeyValuePair()
                || typeof(IEnumerable).IsAssignableFrom(type) || typeof(Uri).IsAssignableFrom(type)
                || typeof(Version).IsAssignableFrom(type))
            {
                return false;
            }
#if NET6_0_OR_GREATER
            if (type == typeof(DateOnly) || type == typeof(TimeOnly) || type == typeof(Half))
            {
                return false;
            }
#endif
#if NET8_0_OR_GREATER
            if (type == typeof(Int128) || type == typeof(UInt128))
            {
                return false;
            }
#endif
            return true;
        }

        internal static void Validate(object entity, ValidationStage stage, ValidationOptions options = default)
        {
            if (entity is null)
            {
                if (options.AllowNull) { return; }
                throw new HttpEntityValidationException(stage,
                    new[] { new ValidationResult("The HTTP entity cannot be null.") });
            }

            var results = new List<ValidationResult>();
            var context = new ValidationContext(entity, options.ServiceProvider, options.Items);
            if (!Validator.TryValidateObject(entity, context, results, validateAllProperties: true))
            {
                throw new HttpEntityValidationException(stage, results);
            }
        }

        private static void ValidateExplicit(object entity, ValidationStage stage, ValidationOptions options)
        {
            if (entity is not null && !IsExplicitValidationCandidate(entity.GetType()))
            {
                return;
            }

            Validate(entity, stage, options);
        }
    }
}
