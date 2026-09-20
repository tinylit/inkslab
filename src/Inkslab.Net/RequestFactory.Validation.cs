using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Inkslab.Net.Validation;

namespace Inkslab.Net
{
    public partial class RequestFactory
    {
        private abstract partial class Requestable<T>
        {
            public IRequestable<T> Validation(ValidationOptions options = default)
                => new ValidatedRequestable<T>(this, ValidationOptions.Capture(options));
        }

        private sealed class ValidatedRequestable<T> : Requestable<T>
        {
            private readonly Requestable<T> _request;
            private readonly ValidationOptions _options;

            public ValidatedRequestable(Requestable<T> request, ValidationOptions options)
            {
                _request = request;
                _options = options;
            }

            protected override ValidationOptions? ExplicitValidationOptions => _options;

            public override Task<T> SendCoreAsync(HttpMethod method, double timeout = 1000D,
                CancellationToken cancellationToken = default)
                => _request.SendCoreAsync(method, timeout, cancellationToken);
        }
    }
}
