using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Inkslab.Net
{
    public partial class RequestFactory
    {
        private class RequestableDataVerify<T> : IRequestableDataVerify<T>
        {
            private readonly Requestable<T> _requestable;
            private readonly Predicate<T> _dataVerify;

            public RequestableDataVerify(Requestable<T> requestable, Predicate<T> dataVerify)
            {
                _requestable = requestable;
                _dataVerify = dataVerify;
            }

            public IRequestableDataVerifySuccess<T, TResult> Success<TResult>(Func<T, TResult> dataVerifySuccess)
            {
                if (dataVerifySuccess is null)
                {
                    throw new ArgumentNullException(nameof(dataVerifySuccess));
                }

                return new RequestableDataVerifySuccess<T, TResult>(_requestable, _dataVerify, dataVerifySuccess);
            }

            public IRequestableDataVerifyFail<T> Fail<TError>(Func<T, TError> throwError) where TError : Exception
            {
                if (throwError is null)
                {
                    throw new ArgumentNullException(nameof(throwError));
                }

                return new RequestableDataVerifyError<T, TError>(_requestable, _dataVerify, throwError);
            }
        }

        private class RequestableDataVerifyError<T, TError> : Requestable<T>, IRequestableDataVerifyFail<T> where TError : Exception
        {
            private readonly Requestable<T> _requestable;
            private readonly Predicate<T> _dataVerify;
            private readonly Func<T, TError> _throwError;

            public RequestableDataVerifyError(Requestable<T> requestable, Predicate<T> dataVerify, Func<T, TError> throwError)
            {
                _requestable = requestable;
                _dataVerify = dataVerify;
                _throwError = throwError;
            }

            public override async Task<T> SendAsync(HttpMethod method, double timeout = 1000D, CancellationToken cancellationToken = default)
            {
                var msgData = await _requestable.SendAsync(method, timeout, cancellationToken);

                if (_dataVerify(msgData))
                {
                    return msgData;
                }

                throw _throwError.Invoke(msgData);
            }
        }

        private class RequestableDataVerifyError<T, TResult, TError> : Requestable<TResult>, IRequestableDataVerifyFail<T, TResult> where TError : Exception
        {
            private readonly Requestable<T> _requestable;
            private readonly Predicate<T> _dataVerify;
            private readonly Func<T, TResult> _dataVerifySuccess;
            private readonly Func<T, TError> _throwError;

            public RequestableDataVerifyError(Requestable<T> requestable, Predicate<T> dataVerify, Func<T, TResult> dataVerifySuccess, Func<T, TError> throwError)
            {
                _requestable = requestable;
                _dataVerify = dataVerify;
                _dataVerifySuccess = dataVerifySuccess;
                _throwError = throwError;
            }

            public override async Task<TResult> SendAsync(HttpMethod method, double timeout = 1000, CancellationToken cancellationToken = default)
            {
                var msgData = await _requestable.SendAsync(method, timeout, cancellationToken);

                if (_dataVerify(msgData))
                {
                    return _dataVerifySuccess.Invoke(msgData);
                }

                throw _throwError.Invoke(msgData);
            }
        }

        private class RequestableDataVerifyFail<T, TResult> : Requestable<TResult>, IRequestableDataVerifyFail<T, TResult>
        {
            private readonly Requestable<T> _requestable;
            private readonly Predicate<T> _dataVerify;
            private readonly Func<T, TResult> _dataVerifySuccess;
            private readonly Func<T, TResult> _dataVerifyFail;

            public RequestableDataVerifyFail(Requestable<T> requestable, Predicate<T> dataVerify, Func<T, TResult> dataVerifySuccess, Func<T, TResult> dataVerifyFail)
            {
                _requestable = requestable;
                _dataVerify = dataVerify;
                _dataVerifySuccess = dataVerifySuccess;
                _dataVerifyFail = dataVerifyFail;
            }

            public override async Task<TResult> SendAsync(HttpMethod method, double timeout = 1000, CancellationToken cancellationToken = default)
            {
                var msgData = await _requestable.SendAsync(method, timeout, cancellationToken);

                if (_dataVerify(msgData))
                {
                    return _dataVerifySuccess.Invoke(msgData);
                }

                return _dataVerifyFail.Invoke(msgData);
            }
        }

        private class RequestableDataVerifySuccess<T, TResult> : IRequestableDataVerifySuccess<T, TResult>
        {
            private readonly Requestable<T> _requestable;
            private readonly Predicate<T> _dataVerify;
            private readonly Func<T, TResult> _dataVerifySuccess;

            public RequestableDataVerifySuccess(Requestable<T> requestable, Predicate<T> dataVerify, Func<T, TResult> dataVerifySuccess)
            {
                _requestable = requestable;
                _dataVerify = dataVerify;
                _dataVerifySuccess = dataVerifySuccess;
            }

            public IRequestableDataVerifyFail<T, TResult> Fail(Func<T, TResult> dataVerifyFail)
            {
                if (dataVerifyFail is null)
                {
                    throw new ArgumentNullException(nameof(dataVerifyFail));
                }

                return new RequestableDataVerifyFail<T, TResult>(_requestable, _dataVerify, _dataVerifySuccess, dataVerifyFail);
            }

            public IRequestableDataVerifyFail<T, TResult> Fail<TError>(Func<T, TError> throwError) where TError : Exception
            {
                if (throwError is null)
                {
                    throw new ArgumentNullException(nameof(throwError));
                }

                return new RequestableDataVerifyError<T, TResult, TError>(_requestable, _dataVerify, _dataVerifySuccess, throwError);
            }
        }
    }
}
