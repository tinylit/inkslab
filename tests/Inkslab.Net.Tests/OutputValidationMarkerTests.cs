using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Inkslab.Annotations;
using Inkslab.Json;
using Inkslab.Net.Options;
using Inkslab.Net.Validation;
using Inkslab.Serialize.Json;
using Xunit;

namespace Inkslab.Net.Tests
{
    /// <summary>Output markers and explicit validation select one final validation per invocation.</summary>
    public class OutputValidationMarkerTests
    {
        static OutputValidationMarkerTests() => SingletonPools.TryAdd<IJsonHelper, DefaultJsonHelper>();

        /// <summary>Automatic validation runs once on every invocation, without a persistent checked flag.</summary>
        [Fact]
        public async Task MarkedResultValidatesOnceOnEverySendAsync()
        {
            var output = new CountedOutput();
            var factory = new OutputFactory();
            var request = factory.CreateRequestable("https://unit.test/").CustomCast(_ => output);
            Assert.Same(output, await request.GetAsync());
            Assert.Equal(1, output.Calls);
            Assert.Same(output, await request.GetAsync());
            Assert.Equal(2, output.Calls);
            Assert.Equal(2, factory.Sends);
        }

        /// <summary>Explicit options suppress automatic validation, including automatic default options.</summary>
        [Fact]
        public async Task ExplicitOptionsOverrideMarkerWithoutDefaultValidationAsync()
        {
            var output = new CountedOutput { NeedsService = true };
            var request = new OutputFactory().CreateRequestable("https://unit.test/").CustomCast(_ => output)
                .Validation(new ValidationOptions { ServiceProvider = new RuleServices() });
            Assert.Same(output, await request.GetAsync());
            Assert.Equal(1, output.Calls);
        }

        /// <summary>An explicit default is still explicit and must not be executed twice.</summary>
        [Fact]
        public async Task ExplicitDefaultAndMarkerValidateOnlyOnceAsync()
        {
            var output = new CountedOutput();
            await new OutputFactory().CreateRequestable("https://unit.test/").CustomCast(_ => output)
                .Validation(default).GetAsync();
            Assert.Equal(1, output.Calls);
        }

        /// <summary>Unmarked results keep the old behavior until validation is explicitly requested.</summary>
        [Fact]
        public async Task UnmarkedResultOnlyValidatesWhenExplicitAsync()
        {
            var output = new UnmarkedOutput();
            var request = new OutputFactory().CreateRequestable("https://unit.test/").CustomCast(_ => output);
            Assert.Same(output, await request.GetAsync());
            await Assert.ThrowsAsync<HttpEntityValidationException>(() => request.Validation().GetAsync());
        }

        /// <summary>DataVerify and mapping complete before the marked final value is checked.</summary>
        [Fact]
        public async Task OnlyFinalMappedResultIsValidatedAsync()
        {
            var events = new List<string>();
            var envelope = new InvalidMarkedOutput();
            var final = new CountedOutput { Events = events };
            var request = new OutputFactory().CreateRequestable("https://unit.test/")
                .CustomCast(_ => { events.Add("parse"); return envelope; })
                .DataVerify(_ => { events.Add("predicate"); return true; })
                .Success(_ => { events.Add("success"); return final; })
                .Fail(_ => new InvalidOperationException());
            Assert.Same(final, await request.GetAsync());
            Assert.Equal(new[] { "parse", "predicate", "success", "validation" }, events);
            Assert.Equal(1, final.Calls);
        }

        /// <summary>An envelope marker does not enable validation of an unmarked mapped result.</summary>
        [Fact]
        public async Task SourceMarkerDoesNotLeakIntoUnmarkedResultAsync()
        {
            var final = new UnmarkedOutput();
            var request = new OutputFactory().CreateRequestable("https://unit.test/")
                .CustomCast(_ => new InvalidMarkedOutput()).DataVerify(_ => true)
                .Success(_ => final).Fail(_ => new InvalidOperationException());
            Assert.Same(final, await request.GetAsync());
        }

        /// <summary>A fallback's runtime type controls its automatic validation.</summary>
        [Fact]
        public async Task FallbackRuntimeTypeIsValidatedAsync()
        {
            var final = new CountedOutput();
            var request = new OutputFactory().CreateRequestable("https://unit.test/")
                .CustomCast(_ => new UnmarkedOutput()).DataVerify(_ => false)
                .Success<object>(_ => new object()).Fail(_ => final);
            Assert.Same(final, await request.GetAsync());
            Assert.Equal(1, final.Calls);
        }

        /// <summary>A business failure is preserved instead of validating the rejected envelope.</summary>
        [Fact]
        public async Task BusinessExceptionPrecedesAutomaticValidationAsync()
        {
            var expected = new InvalidOperationException("business");
            var request = new OutputFactory().CreateRequestable("https://unit.test/")
                .CustomCast(_ => new InvalidMarkedOutput()).DataVerify(_ => false).Fail(_ => expected);
            Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(() => request.GetAsync()));
        }

        /// <summary>Parsing catch handles parsing only; final validation is outside its scope.</summary>
        [Fact]
        public async Task CatchFallbackIsValidatedOnceAndCannotCatchValidationAsync()
        {
            var final = new CountedOutput();
            var source = new OutputFactory().CreateRequestable("https://unit.test/");
            Assert.Same(final, await source.CustomCast<CountedOutput>(_ => throw new InvalidOperationException())
                .Catch(_ => final).GetAsync());
            Assert.Equal(1, final.Calls);

            int catches = 0;
            var rejected = source.CustomCast(_ => new InvalidMarkedOutput())
                .Catch(_ => { catches++; return new InvalidMarkedOutput { Name = "fallback" }; });
            await Assert.ThrowsAsync<HttpEntityValidationException>(() => rejected.GetAsync());
            Assert.Equal(0, catches);
        }

        /// <summary>The asynchronous raw-response parser shares the same final validation boundary.</summary>
        [Fact]
        public async Task AsyncResponseParserAndCatchValidateOnlyFinalResultAsync()
        {
            var source = new OutputFactory().CreateRequestable("https://unit.test/");
            var output = new CountedOutput();
            Assert.Same(output, await source.CustomCast(async (message, token) =>
            {
                Assert.Equal("ignored", await message.Content.ReadAsStringAsync(token));
                return output;
            }).GetAsync());
            Assert.Equal(1, output.Calls);

            var fallback = new CountedOutput { NeedsService = true };
            var recovered = source.CustomCast<CountedOutput>(async (message, token) =>
            {
                await message.Content.ReadAsStringAsync(token);
                throw new FormatException("parser failed");
            }).Catch(_ => fallback).Validation(new ValidationOptions { ServiceProvider = new RuleServices() });
            Assert.Same(fallback, await recovered.GetAsync());
            Assert.Equal(1, fallback.Calls);

            int catches = 0;
            var invalid = source.CustomCast((message, token) => Task.FromResult(new InvalidMarkedOutput()))
                .Catch(_ => { catches++; return new InvalidMarkedOutput { Name = "replacement" }; });
            await Assert.ThrowsAsync<HttpEntityValidationException>(() => invalid.GetAsync());
            Assert.Equal(0, catches);
        }

        /// <summary>Automatic validation treats null according to the final declared type.</summary>
        [Fact]
        public async Task MarkedNullUsesDefaultPolicyAndExplicitOptionsTakePrecedenceAsync()
        {
            var source = new OutputFactory().CreateRequestable("https://unit.test/");
            var marked = source.CustomCast<CountedOutput>(_ => null);
            await Assert.ThrowsAsync<HttpEntityValidationException>(() => marked.GetAsync());
            Assert.Null(await marked.Validation(new ValidationOptions { AllowNull = true }).GetAsync());
            Assert.Null(await source.CustomCast<UnmarkedOutput>(_ => null).GetAsync());
            Assert.Null(await source.CustomCast<object>(_ => null).GetAsync());
        }

        /// <summary>Struct markers work for mapped values and nullable declared result types.</summary>
        [Fact]
        public async Task MarkedStructAndNullableResultsAreValidatedAsync()
        {
            var source = new OutputFactory().CreateRequestable("https://unit.test/")
                .CustomCast(_ => new UnmarkedOutput()).DataVerify(_ => true);
            await Assert.ThrowsAsync<HttpEntityValidationException>(() => source.Success(_ => new MarkedValue())
                .Fail(_ => new InvalidOperationException()).GetAsync());
            var nullable = source.Success<MarkedValue?>(_ => null).Fail(_ => new InvalidOperationException());
            await Assert.ThrowsAsync<HttpEntityValidationException>(() => nullable.GetAsync());
            Assert.Null(await nullable.Validation(new ValidationOptions { AllowNull = true }).GetAsync());
        }

        /// <summary>Markers are not inherited and runtime type wins over the declared base type.</summary>
        [Fact]
        public async Task MarkerChecksRuntimeTypeWithoutInheritanceAsync()
        {
            var source = new OutputFactory().CreateRequestable("https://unit.test/");
            var derived = new UnmarkedDerived();
            Assert.Same(derived, await source.CustomCast<InvalidMarkedOutput>(_ => derived).GetAsync());
            await Assert.ThrowsAsync<HttpEntityValidationException>(() => source.CustomCast<object>(_ => new InvalidMarkedOutput()).GetAsync());
            Assert.Same(derived, await source.CustomCast<object>(_ => derived).GetAsync());
        }

        /// <summary>Even marked containers keep the entity-only validation contract.</summary>
        [Fact]
        public async Task MarkedContainersAreNotValidatedOrTraversedAsync()
        {
            var dictionary = new MarkedDictionary();
            Assert.Same(dictionary, await new OutputFactory().CreateRequestable("https://unit.test/")
                .CustomCast(_ => dictionary).GetAsync());
        }

        /// <summary>Automatic policy runs for every public HTTP execution method.</summary>
        [Theory]
        [InlineData("GET")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("DELETE")]
        [InlineData("HEAD")]
        [InlineData("PATCH")]
        [InlineData("CUSTOM")]
        public async Task EveryExecutionMethodValidatesOnceAsync(string method)
        {
            var result = new CountedOutput();
            var request = new OutputFactory().CreateRequestable("https://unit.test/").CustomCast(_ => result);
            switch (method)
            {
                case "GET": await request.GetAsync(); break;
                case "POST": await request.PostAsync(); break;
                case "PUT": await request.PutAsync(); break;
                case "DELETE": await request.DeleteAsync(); break;
                case "HEAD": await request.HeadAsync(); break;
                case "PATCH": await request.PatchAsync(); break;
                default: await request.SendAsync(method); break;
            }
            Assert.Equal(1, result.Calls);
        }

        /// <summary>Explicit validation returns an execution-only public surface.</summary>
        [Fact]
        public void ValidationCannotBeChainedAgain()
        {
            var method = typeof(IRequestableValidation<CountedOutput>).GetMethod("Validation");
            Assert.Equal(typeof(IRequestable<CountedOutput>), method.ReturnType);
            Assert.Null(method.ReturnType.GetMethod("Validation"));
            var request = new OutputFactory().CreateRequestable("https://unit.test/").CustomCast(_ => new CountedOutput()).Validation();
            Assert.False(request is IRequestableValidation<CountedOutput>);
            Assert.False(request is IRequestableExtend<CountedOutput>);
        }

        /// <summary>JSON parsing and automatic validation remain separate phases.</summary>
        [Fact]
        public async Task JsonResultUsesOutputMarkerAsync()
        {
            var request = new OutputFactory("{}").CreateRequestable("https://unit.test/")
                .JsonCast<InvalidMarkedOutput>();
            await Assert.ThrowsAsync<HttpEntityValidationException>(() => request.GetAsync());
        }

        /// <summary>A real JSON parse failure can produce a marked fallback, validated once afterward.</summary>
        [Fact]
        public async Task JsonParsingFailureValidatesMarkedFallbackOnceAsync()
        {
            int catches = 0;
            var fallback = new CountedOutput();
            var request = new OutputFactory("not-json").CreateRequestable("https://unit.test/")
                .JsonCast<CountedOutput>().JsonCatch(_ => { catches++; return fallback; });
            Assert.Same(fallback, await request.GetAsync());
            Assert.Equal(1, catches);
            Assert.Equal(1, fallback.Calls);
        }

        /// <summary>XML output validation happens after parsing, outside the parsing fallback.</summary>
        [Fact]
        public async Task XmlResultUsesOutputMarkerOutsideCatchAsync()
        {
            int catches = 0;
            var request = new OutputFactory("<InvalidMarkedOutput />").CreateRequestable("https://unit.test/")
                .XmlCast<InvalidMarkedOutput>()
                .XmlCatch(_ => { catches++; return new InvalidMarkedOutput { Name = "fallback" }; });
            await Assert.ThrowsAsync<HttpEntityValidationException>(() => request.GetAsync());
            Assert.Equal(0, catches);
        }

        /// <summary>Retries do not validate intermediate responses or revalidate the final entity.</summary>
        [Fact]
        public async Task HttpRetryOnlyValidatesFinalOutputAsync()
        {
            int sends = 0;
            var output = new CountedOutput();
            using var handler = new TestHttpMessageHandler((request, token) =>
            {
                sends++;
                return Task.FromResult(TestHttpMessageHandler.Response("ok",
                    sends == 1 ? HttpStatusCode.Unauthorized : HttpStatusCode.OK));
            });
            using var client = handler.CreateClient();
            var request = new TestRequestFactory(client).CreateRequestable("https://unit.test/")
                .When(status => status == HttpStatusCode.Unauthorized).ThenAsync(_ => Task.CompletedTask)
                .CustomCast(_ => output).Validation();
            Assert.Same(output, await request.GetAsync());
            Assert.Equal(2, sends);
            Assert.Equal(1, output.Calls);
        }

        /// <summary>Input and output markers are independent even on the same object.</summary>
        [Fact]
        public async Task EachDirectionHasOneIndependentValidationAsync()
        {
            var entity = new BothDirections();
            var request = new OutputFactory().CreateRequestable("https://unit.test/")
                .Json(entity).CustomCast(_ => entity).Validation();
            Assert.Equal(1, entity.Calls);
            Assert.Same(entity, await request.PostAsync());
            Assert.Equal(2, entity.Calls);
        }

        /// <summary>Automatic markers never turn a canceled call into a validation failure.</summary>
        [Fact]
        public async Task CanceledCallDoesNotValidateOutputAsync()
        {
            var entity = new CountedOutput();
            var source = new OutputFactory().CreateRequestable("https://unit.test/").CustomCast(_ => entity);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => source.GetAsync(cancellationToken: new CancellationToken(true)));
            Assert.Equal(0, entity.Calls);
        }

        /// <summary>Independent results do not share any invocation-level validated flag.</summary>
        [Fact]
        public async Task ConcurrentCallsHaveIndependentValidationAsync()
        {
            var source = new OutputFactory().CreateRequestable("https://unit.test/").CustomCast(_ => new CountedOutput()).Validation();
            var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(() => source.GetAsync())));
            Assert.All(results, entity => Assert.Equal(1, entity.Calls));
        }

        private sealed class OutputFactory : RequestFactory
        {
            private readonly string _body;
            public int Sends { get; private set; }
            public OutputFactory(string body = "ignored") => _body = body;
            protected override Task<HttpResponseMessage> SendAsync(RequestOptions options, CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                Sends++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_body) });
            }
        }

        [ValidateOutput]
        private sealed class CountedOutput : IValidatableObject
        {
            public int Calls { get; private set; }
            public bool NeedsService { get; set; }
            public List<string> Events { get; set; }
            public IEnumerable<ValidationResult> Validate(ValidationContext context)
            {
                Calls++;
                Events?.Add("validation");
                if (NeedsService && !Equals(context.GetService(typeof(string)), "allowed"))
                {
                    return new[] { new ValidationResult("Explicit options were not applied.") };
                }
                return Array.Empty<ValidationResult>();
            }
        }

        /// <summary>Public output entity usable by serializers.</summary>
        [ValidateOutput]
        public class InvalidMarkedOutput
        {
            /// <summary>Required response value.</summary>
            [Required]
            public string Name { get; set; }
        }

        private sealed class UnmarkedDerived : InvalidMarkedOutput { }
        private sealed class UnmarkedOutput
        {
            [Required]
            public string Name { get; set; }
        }

        [ValidateOutput]
        private struct MarkedValue
        {
            [Range(1, 10)]
            public int Value { get; set; }
        }

        [ValidateOutput]
        private sealed class MarkedDictionary : Dictionary<string, object>, IValidatableObject
        {
            public IEnumerable<ValidationResult> Validate(ValidationContext context)
                => throw new InvalidOperationException("Container validation must not run.");
        }

        private sealed class RuleServices : IServiceProvider
        {
            public object GetService(Type serviceType) => serviceType == typeof(string) ? "allowed" : null;
        }

        [ValidateInput, ValidateOutput]
        private sealed class BothDirections : IValidatableObject
        {
            public int Calls { get; private set; }
            public IEnumerable<ValidationResult> Validate(ValidationContext context)
            {
                Calls++;
                return Array.Empty<ValidationResult>();
            }
        }
    }
}
