using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Inkslab.Net.Validation
{
    /// <summary>Entity validation failed at an HTTP boundary.</summary>
    public sealed class HttpEntityValidationException : ValidationException
    {
        /// <summary>Creates an exception containing validation results without the entity body.</summary>
        /// <param name="stage">The validation boundary.</param>
        /// <param name="results">Results from standard DataAnnotations validation.</param>
        public HttpEntityValidationException(ValidationStage stage, IEnumerable<ValidationResult> results)
            : base("HTTP " + stage + " entity validation failed.")
        {
            if (results is null) { throw new ArgumentNullException(nameof(results)); }
            Stage = stage;
            ValidationResults = Array.AsReadOnly(results.Select(result =>
                new ValidationResult(result.ErrorMessage, result.MemberNames.ToArray())).ToArray());
        }

        /// <summary>The validation boundary.</summary>
        public ValidationStage Stage { get; }
        /// <summary>A read-only snapshot of the results produced by the validator.</summary>
        public IReadOnlyList<ValidationResult> ValidationResults { get; }
    }
}
