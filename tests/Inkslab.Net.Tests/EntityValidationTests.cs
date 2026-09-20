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
    /// <summary>Request entities validate before serialization; final response results validate after business handling.</summary>
    public class EntityValidationTests
    {
        static EntityValidationTests() => SingletonPools.TryAdd<IJsonHelper, DefaultJsonHelper>();

        /// <summary>Invalid request objects fail locally for all supported formats.</summary>
        [Theory]
        [InlineData("json")]
        [InlineData("xml")]
        [InlineData("form")]
        [InlineData("query")]
        public void InvalidRequestNeverSends(string format)
        {
            var factory = new ValidationTestFactory("{}");
            var request = factory.CreateRequestable("https://unit.test/");
            var invalid = new Order();
            var error = Assert.Throws<HttpEntityValidationException>(() =>
            {
                switch (format)
                {
                    case "json": request.Json(invalid); break;
                    case "xml": request.Xml(invalid); break;
                    case "query": request.AppendQueryString(invalid, NamingType.Normal); break;
                    default: request.Form(invalid, NamingType.Normal); break;
                }
            });
            Assert.Equal(ValidationStage.Request, error.Stage);
            Assert.Equal(2, error.ValidationResults.Count);
            Assert.Contains(error.ValidationResults, x => x.MemberNames.Contains(nameof(Order.Name)));
            Assert.Equal(0, factory.Sends);
        }

        /// <summary>Valid entities retain the native JSON, XML, form and query serialization behavior.</summary>
        [Theory]
        [InlineData("json")]
        [InlineData("xml")]
        [InlineData("form")]
        [InlineData("query")]
        public async Task ValidRequestEntitiesAreSerializedAsync(string format)
        {
            var factory = new ValidationTestFactory("ok");
            var request = factory.CreateRequestable("https://unit.test/");
            var entity = new Order { Name = "valid", Quantity = 2 };
            switch (format)
            {
                case "json": await request.Json(entity).PostAsync(); break;
                case "xml": await request.Xml(entity).PostAsync(); break;
                case "query": await request.AppendQueryString(entity, NamingType.Normal).GetAsync(); break;
                default: await request.Form(entity, NamingType.Normal).PostAsync(); break;
            }

            switch (format)
            {
                case "json":
                    Assert.Equal("{\"Name\":\"valid\",\"Quantity\":2}", factory.RequestBody);
                    break;
                case "xml":
                    Assert.Contains("<Name>valid</Name>", factory.RequestBody);
                    Assert.Contains("<Quantity>2</Quantity>", factory.RequestBody);
                    break;
                case "query":
                    Assert.Equal("https://unit.test/?Name=valid&Quantity=2", factory.RequestUri);
                    break;
                default:
                    Assert.Equal("Name=valid&Quantity=2", factory.RequestBody);
                    break;
            }
            Assert.Equal(1, factory.Sends);
        }

        /// <summary>Null marked DTOs are rejected when the generic boundary preserves their type.</summary>
        [Theory]
        [InlineData("json")]
        [InlineData("xml")]
        [InlineData("query")]
        public void NullRequestEntitiesAreRejected(string format)
        {
            var factory = new ValidationTestFactory("ok");
            var request = factory.CreateRequestable("https://unit.test/");
            var error = Assert.Throws<HttpEntityValidationException>(() =>
            {
                switch (format)
                {
                    case "json": request.Json<Order>(null); break;
                    case "xml": request.Xml<Order>(null); break;
                    default: request.AppendQueryString<Order>(null, NamingType.Normal); break;
                }
            });

            Assert.Equal(ValidationStage.Request, error.Stage);
            Assert.Single(error.ValidationResults);
            Assert.Equal(0, factory.Sends);
        }

        /// <summary>Raw text inputs remain raw and do not run entity validation.</summary>
        [Theory]
        [InlineData("json")]
        [InlineData("xml")]
        [InlineData("query")]
        public async Task RawRequestTextRemainsUnchangedAsync(string format)
        {
            var factory = new ValidationTestFactory("ok");
            var request = factory.CreateRequestable("https://unit.test/");
            switch (format)
            {
                case "json": await request.Json("raw-json").PostAsync(); break;
                case "xml": await request.Xml("raw-xml").PostAsync(); break;
                default: await request.AppendQueryString("Name=raw").GetAsync(); break;
            }

            if (format == "query")
            {
                Assert.Equal("https://unit.test/?Name=raw", factory.RequestUri);
            }
            else
            {
                Assert.Equal("raw-" + format, factory.RequestBody);
            }
            Assert.Equal(1, factory.Sends);
        }

        /// <summary>Business predicates run before final response validation.</summary>
        [Fact]
        public async Task ResponseIsValidatedAfterBusinessPredicateAsync()
        {
            var factory = new ValidationTestFactory("ignored");
            int predicates = 0;
            var request = factory.CreateRequestable("https://unit.test/")
                .CustomCast(_ => new Order())
                .DataVerify(_ => { predicates++; return true; })
                .Fail(_ => new InvalidOperationException())
                .Validation();
            var error = await Assert.ThrowsAsync<HttpEntityValidationException>(() => request.PostAsync());
            Assert.Equal(ValidationStage.Response, error.Stage);
            Assert.Equal(1, factory.Sends);
            Assert.Equal(1, predicates);
        }

        /// <summary>Validation terminates the configurable business chain.</summary>
        [Fact]
        public void ValidationReturnsOnlyExecutionCapability()
        {
            var request = new ValidationTestFactory("ignored").CreateRequestable("https://unit.test/")
                .CustomCast(_ => new Order()).Validation();

            Assert.False(request is IRequestableExtend<Order>);
        }

        /// <summary>Only the mapped result is validated, after the predicate and success mapping.</summary>
        [Fact]
        public async Task SuccessResultIsValidatedAfterMappingAsync()
        {
            var events = new List<string>();
            var factory = new ValidationTestFactory("ignored");
            var final = new TrackedResult(events);
            var request = factory.CreateRequestable("https://unit.test/")
                .CustomCast(_ => { events.Add("parse"); return new Order(); })
                .DataVerify(_ => { events.Add("predicate"); return true; })
                .Success(_ => { events.Add("success"); return final; })
                .Fail(_ => new InvalidOperationException())
                .Validation();

            Assert.Same(final, await request.GetAsync());
            Assert.Equal(new[] { "parse", "predicate", "success", "validation" }, events);
            Assert.Equal(1, factory.Sends);
        }

        /// <summary>A failure fallback is the final result and must satisfy entity rules.</summary>
        [Fact]
        public async Task FailureFallbackIsValidatedAsync()
        {
            var events = new List<string>();
            var factory = new ValidationTestFactory("ignored");
            var request = factory.CreateRequestable("https://unit.test/")
                .CustomCast(_ => new Order { Name = "valid", Quantity = 1 })
                .DataVerify(_ => { events.Add("predicate"); return false; })
                .Success(_ => { events.Add("success"); return new Order { Name = "valid", Quantity = 1 }; })
                .Fail(_ => { events.Add("fallback"); return new Order(); })
                .Validation();

            var error = await Assert.ThrowsAsync<HttpEntityValidationException>(() => request.GetAsync());
            Assert.Equal(ValidationStage.Response, error.Stage);
            Assert.Equal(new[] { "predicate", "fallback" }, events);
            Assert.Equal(1, factory.Sends);
        }

        /// <summary>A business failure exception escapes without validating the rejected input.</summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task BusinessFailureIsNotReplacedByValidationAsync(bool mapped)
        {
            var expected = new InvalidOperationException("business rejection");
            var factory = new ValidationTestFactory("ignored");
            var source = factory.CreateRequestable("https://unit.test/")
                .CustomCast(_ => new Order()).DataVerify(_ => false);
            var request = mapped
                ? source.Success(_ => new Order()).Fail(_ => expected).Validation()
                : source.Fail(_ => expected).Validation();

            var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => request.GetAsync());
            Assert.Same(expected, actual);
            Assert.Equal(1, factory.Sends);
        }

        /// <summary>Mapped struct values use DataAnnotations without a reference-type restriction.</summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task MappedValueTypesAreValidatedAsync(bool success)
        {
            var source = new ValidationTestFactory("ignored").CreateRequestable("https://unit.test/")
                .CustomCast(_ => new Order()).DataVerify(_ => success)
                .Success(_ => new QuantityResult { Quantity = 2 })
                .Fail(_ => new QuantityResult { Quantity = 0 })
                .Validation();

            if (success)
            {
                Assert.Equal(2, (await source.GetAsync()).Quantity);
            }
            else
            {
                var error = await Assert.ThrowsAsync<HttpEntityValidationException>(() => source.GetAsync());
                Assert.Contains(nameof(QuantityResult.Quantity), error.ValidationResults[0].MemberNames);
            }
        }

        /// <summary>Nullable mapped values apply the same configurable null policy as reference types.</summary>
        [Fact]
        public async Task NullableMappedResultsRespectNullPolicyAsync()
        {
            var source = new ValidationTestFactory("ignored").CreateRequestable("https://unit.test/")
                .CustomCast(_ => new Order()).DataVerify(_ => true)
                .Success<int?>(_ => null).Fail(_ => new InvalidOperationException());

            await Assert.ThrowsAsync<HttpEntityValidationException>(() => source.Validation().GetAsync());
            Assert.Null(await source.Validation(new ValidationOptions { AllowNull = true }).GetAsync());
        }

        /// <summary>Parsing fallback precedes business evaluation and final validation.</summary>
        [Fact]
        public async Task CatchPrecedesBusinessFallbackAndValidationAsync()
        {
            var events = new List<string>();
            var factory = new ValidationTestFactory("ignored");
            var final = new TrackedResult(events);
            var request = factory.CreateRequestable("https://unit.test/")
                .CustomCast<Order>(_ => { events.Add("parse"); throw new InvalidOperationException(); })
                .Catch(_ => { events.Add("catch"); return new Order(); })
                .DataVerify(_ => { events.Add("predicate"); return false; })
                .Success(_ => { events.Add("success"); return final; })
                .Fail(_ => { events.Add("fallback"); return final; })
                .Validation();

            Assert.Same(final, await request.GetAsync());
            Assert.Equal(new[] { "parse", "catch", "predicate", "fallback", "validation" }, events);
        }

        /// <summary>Direct JSON responses can be validated without a business predicate.</summary>
        [Fact]
        public async Task JsonCastSupportsDirectValidationAsync()
        {
            var factory = new ValidationTestFactory("{\"Name\":\"valid\",\"Quantity\":0}");
            var error = await Assert.ThrowsAsync<HttpEntityValidationException>(() => factory
                .CreateRequestable("https://unit.test/").JsonCast<Order>().Validation().GetAsync());

            Assert.Equal(ValidationStage.Response, error.Stage);
            Assert.Single(error.ValidationResults);
            Assert.Contains(nameof(Order.Quantity), error.ValidationResults[0].MemberNames);
        }

        /// <summary>Final validation errors are outside the parsing catch boundary.</summary>
        [Fact]
        public async Task ParsingCatchDoesNotCatchFinalValidationAsync()
        {
            int catches = 0;
            var request = new ValidationTestFactory("{}").CreateRequestable("https://unit.test/")
                .JsonCast<Order>()
                .JsonCatch(_ => { catches++; return new Order { Name = "valid", Quantity = 1 }; })
                .Validation();

            await Assert.ThrowsAsync<HttpEntityValidationException>(() => request.GetAsync());
            Assert.Equal(0, catches);
        }

        /// <summary>Validation preserves caller cancellation and does not invoke parsing or predicates.</summary>
        [Fact]
        public async Task CancellationPassesThroughValidationAsync()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var events = new List<string>();
            var factory = new ValidationTestFactory("ignored");
            var request = factory.CreateRequestable("https://unit.test/")
                .CustomCast(_ => { events.Add("parse"); return new TrackedResult(events); })
                .DataVerify(_ => { events.Add("predicate"); return true; })
                .Fail(_ => new InvalidOperationException())
                .Validation();

            var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request.GetAsync(cancellationToken: cancellation.Token));
            Assert.Equal(cancellation.Token, error.CancellationToken);
            Assert.Empty(events);
            Assert.Equal(0, factory.Sends);
        }

        /// <summary>Rules can use context services and options are captured when wrapped.</summary>
        [Fact]
        public async Task ContextAndOptionsAreCapturedAsync()
        {
            var factory = new ValidationTestFactory("ignored");
            var options = new ValidationOptions
            {
                Items = new Dictionary<object, object> { ["allowed"] = "first" },
                ServiceProvider = new RuleServices()
            };
            var request = factory.CreateRequestable("https://unit.test/")
                .CustomCast(_ => new ContextOrder { Name = "first" })
                .Validation(options);
            options.Items["allowed"] = "changed";
            options = new ValidationOptions { Items = options.Items };
            Assert.Equal("first", (await request.GetAsync()).Name);
        }

        /// <summary>The null policy is captured when validation is configured.</summary>
        [Fact]
        public async Task NullPolicyIsCapturedAsync()
        {
            var options = ValidationOptions.Default;
            var request = new ValidationTestFactory("ignored").CreateRequestable("https://unit.test/")
                .CustomCast<Order>(_ => null).Validation(options);
            options = new ValidationOptions { AllowNull = true };

            await Assert.ThrowsAsync<HttpEntityValidationException>(() => request.GetAsync());
        }

        /// <summary>Null is rejected unless explicitly allowed; traversal is not recursive.</summary>
        [Fact]
        public async Task NullPolicyAndNonRecursiveValidationAsync()
        {
            var factory = new ValidationTestFactory("ignored");
            var source = factory.CreateRequestable("https://unit.test/");
            await Assert.ThrowsAsync<HttpEntityValidationException>(() => source.CustomCast<Order>(_ => null).Validation().GetAsync());
            Assert.Null(await source.CustomCast<Order>(_ => null).Validation(new ValidationOptions { AllowNull = true }).GetAsync());
            var parent = new Parent { Child = new Order() };
            Assert.Same(parent, await source.CustomCast(_ => parent).Validation().GetAsync());
        }

        /// <summary>Fallback results still require validation; cancellation is never retried.</summary>
        [Fact]
        public async Task CatchResultsAreValidatedAsync()
        {
            var factory = new ValidationTestFactory("ignored");
            var request = factory.CreateRequestable("https://unit.test/")
                .CustomCast<Order>(_ => throw new InvalidOperationException())
                .Catch(_ => new Order()).Validation();
            await Assert.ThrowsAsync<HttpEntityValidationException>(() => request.GetAsync());
            Assert.Equal(1, factory.Sends);
        }

        /// <summary>Standard Validator skips object validation when property rules fail.</summary>
        [Fact]
        public async Task StandardRuleStagesArePreservedAsync()
        {
            var factory = new ValidationTestFactory("ignored");
            var dto = new ContextOrder();
            await Assert.ThrowsAsync<HttpEntityValidationException>(() => factory.CreateRequestable("https://unit.test/")
                .CustomCast(_ => dto).Validation().GetAsync());
            Assert.Equal(0, dto.ObjectValidations);
        }

        private sealed class ValidationTestFactory : RequestFactory
        {
            private readonly string _body;
            public int Sends { get; private set; }
            public string RequestBody { get; private set; }
            public string RequestUri { get; private set; }
            public ValidationTestFactory(string body) => _body = body;
            protected override async Task<HttpResponseMessage> SendAsync(RequestOptions options, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Sends++;
                RequestUri = options.RequestUri;
                using var content = options.Content;
                RequestBody = content is null ? null : await content.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_body) };
            }
        }

        /// <summary>Public entity usable by the XML serializer.</summary>
        [ValidateInput]
        public sealed class Order
        {
            /// <summary>Required order name.</summary>
            [Required]
            public string Name { get; set; }
            /// <summary>Positive order quantity.</summary>
            [Range(1, 100)]
            public int Quantity { get; set; }
        }

        private sealed class Parent { public Order Child { get; set; } }
        private struct QuantityResult
        {
            [Range(1, 100)]
            public int Quantity { get; set; }
        }
        private sealed class TrackedResult : IValidatableObject
        {
            private readonly List<string> _events;
            public TrackedResult(List<string> events) => _events = events;
            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                _events.Add("validation");
                return Array.Empty<ValidationResult>();
            }
        }
        private sealed class RuleServices : IServiceProvider
        {
            public object GetService(Type serviceType) => serviceType == typeof(string) ? "service" : null;
        }
        private sealed class ContextOrder : IValidatableObject
        {
            [Required]
            public string Name { get; set; }
            public int ObjectValidations { get; private set; }
            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                ObjectValidations++;
                if (!validationContext.Items.TryGetValue("allowed", out var allowed) || !Equals(allowed, Name)
                    || !Equals(validationContext.GetService(typeof(string)), "service"))
                {
                    yield return new ValidationResult("Context missing", new[] { nameof(Name) });
                }
            }
        }
    }
}
