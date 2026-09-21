#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    public class InputValidationMarkerTests
    {
        static InputValidationMarkerTests() => SingletonPools.TryAdd<IJsonHelper, DefaultJsonHelper>();

        [Theory]
        [InlineData("json")]
        [InlineData("xml")]
        [InlineData("form")]
        [InlineData("query")]
        public async Task UnmarkedRulesRemainInactiveAsync(string format)
        {
            var factory = new InputFactory();
            var entity = new UnmarkedDto();
            await Build(factory, format, entity).PostAsync();
            entity.Name = "valid";
            await Build(factory, format, entity).PostAsync();
            Assert.Equal(0, entity.GetValidationCalls());
            Assert.Equal(2, factory.Sends);
        }

        [Theory]
        [InlineData("json")]
        [InlineData("xml")]
        [InlineData("form")]
        [InlineData("query")]
        public void MarkedRulesFailBeforeSending(string format)
        {
            var factory = new InputFactory();
            var error = Assert.Throws<HttpEntityValidationException>(() => Build(factory, format, new MarkedDto()));
            Assert.Equal(ValidationStage.Request, error.Stage);
            Assert.Contains(nameof(UnmarkedDto.Name), Assert.Single(error.ValidationResults).MemberNames);
            Assert.Equal(0, factory.Sends);
        }

        [Theory]
        [InlineData("json")]
        [InlineData("xml")]
        [InlineData("form")]
        [InlineData("query")]
        public void RuntimeMarkerAppliesThroughObjectAndBaseTypes(string format)
        {
            var factory = new InputFactory();
            object boxed = new MarkedDto();
            UnmarkedDto declaredBase = new MarkedDto();
            Assert.Throws<HttpEntityValidationException>(() => Build(factory, format, boxed));
            Assert.Throws<HttpEntityValidationException>(() => Build(factory, format, declaredBase));
            Assert.Equal(0, factory.Sends);
        }

        [Theory]
        [InlineData("json")]
        [InlineData("xml")]
        [InlineData("form")]
        [InlineData("query")]
        public async Task BaseMarkerDoesNotEnableUnmarkedDerivedTypeAsync(string format)
        {
            var factory = new InputFactory();
            MarkedDto entity = new UnmarkedDerivedDto();
            await Build(factory, format, entity).PostAsync();
            Assert.Equal(1, factory.Sends);
            Assert.Equal(0, entity.GetValidationCalls());
        }

        [Theory]
        [InlineData("json")]
        [InlineData("xml")]
        [InlineData("form")]
        [InlineData("query")]
        public async Task OutputMarkerDoesNotEnableInputValidationAsync(string format)
        {
            var factory = new InputFactory();
            await Build(factory, format, new OutputOnlyDto()).PostAsync();
            Assert.Equal(1, factory.Sends);
        }

        [Theory]
        [InlineData("json")]
        [InlineData("xml")]
        [InlineData("query")]
        public void NullMarkedGenericDtoUsesDeclaredType(string format)
        {
            var factory = new InputFactory();
            var error = Assert.Throws<HttpEntityValidationException>(() => Build<MarkedDto>(factory, format, null));
            Assert.Equal(ValidationStage.Request, error.Stage);
            Assert.Single(error.ValidationResults);
            Assert.Equal(0, factory.Sends);
        }

        [Fact]
        public async Task UnmarkedAndUntypedNullRetainNativeBehaviorAsync()
        {
            var factory = new InputFactory();
            var request = factory.CreateRequestable("https://unit.test/");
            MarkedDto typedNull = null;
            Assert.Same(request, request.Form(null, NamingType.Normal));
            Assert.Same(request, request.Form(typedNull, NamingType.Normal));
            Assert.Same(request, request.AppendQueryString<UnmarkedDto>(null, NamingType.Normal));
            Assert.Same(request, request.AppendQueryString<object>(null, NamingType.Normal));
            await request.Json<UnmarkedDto>(null).PostAsync();
            Assert.Equal(JsonHelper.ToJson<UnmarkedDto>(null), factory.Bodies[0]);
            await request.Json<object>(null).PostAsync();
            Assert.Equal(JsonHelper.ToJson<object>(null), factory.Bodies[1]);
            var xml = request.Xml<UnmarkedDto>(null);
            await Assert.ThrowsAsync<ArgumentNullException>(() => xml.PostAsync());
            var untypedXml = request.Xml<object>(null);
            await Assert.ThrowsAsync<ArgumentNullException>(() => untypedXml.PostAsync());
        }

        [Fact]
        public async Task FormValidatesBoxedStructDtoAndSkipsNullWithoutATypeAsync()
        {
            var factory = new InputFactory();
            var request = factory.CreateRequestable("https://unit.test/");
            object invalid = new MarkedValueDto();
            var error = Assert.Throws<HttpEntityValidationException>(() => request.Form(invalid, NamingType.Normal));
            Assert.Equal(ValidationStage.Request, error.Stage);
            MarkedValueDto? nullable = new MarkedValueDto();
            Assert.Throws<HttpEntityValidationException>(() => request.Form(nullable, NamingType.Normal));
            nullable = null;
            Assert.Same(request, request.Form(nullable, NamingType.Normal));
            await request.Form(new MarkedValueDto { Quantity = 1 }, NamingType.Normal).PostAsync();
            Assert.Equal("Quantity=1", Assert.Single(factory.Bodies));
        }

        [Theory]
        [InlineData("json")]
        [InlineData("xml")]
        [InlineData("form")]
        [InlineData("query")]
        public async Task NestedMarkedDtoIsNotValidatedRecursivelyAsync(string format)
        {
            var factory = new InputFactory();
            await Build(factory, format, new MarkedParent { Child = new MarkedDto() }).PostAsync();
            Assert.Equal(1, factory.Sends);
        }

        [Fact]
        public void MarkedDictionaryUsesMarkerValidation()
        {
            var factory = new InputFactory();
            var dictionary = new MarkedDictionary { ["item"] = new MarkedDto() };
            var error = Assert.Throws<HttpEntityValidationException>(() =>
                factory.CreateRequestable("https://unit.test/").Json((object)dictionary));
            Assert.Equal(ValidationStage.Request, error.Stage);
            Assert.Equal(1, dictionary.GetValidationCalls());
            Assert.Equal(0, factory.Sends);
        }

        [Theory]
        [InlineData("json")]
        [InlineData("xml")]
        [InlineData("form")]
        [InlineData("query")]
        public async Task RequestRetryUsesOneValidatedSnapshotAsync(string format)
        {
            var factory = new InputFactory { RetryOnce = true };
            var entity = new CountedDto { Name = "first" };
            var body = Build(factory, format, entity);
            Assert.Equal(1, entity.GetValidationCalls());
            await body.When(status => status == HttpStatusCode.Unauthorized).ThenAsync(_ =>
            {
                entity.Name = null;
                return Task.CompletedTask;
            }).PostAsync();
            Assert.Equal(1, entity.GetValidationCalls());
            Assert.Equal(2, factory.Sends);
            Assert.Equal(factory.Bodies[0], factory.Bodies[1]);
            Assert.Equal(factory.Uris[0], factory.Uris[1]);
        }

        private static IRequestableContent Build<T>(InputFactory factory, string format, T entity) where T : class
        {
            var request = factory.CreateRequestable("https://unit.test/");
            return format switch
            {
                "json" => request.Json(entity),
                "xml" => request.Xml(entity),
                "form" => request.Form(entity, NamingType.Normal),
                _ => request.AppendQueryString(entity, NamingType.Normal)
            };
        }

        private sealed class InputFactory : RequestFactory
        {
            public int Sends { get; private set; }
            public bool RetryOnce { get; set; }
            public List<string> Bodies { get; } = new List<string>();
            public List<string> Uris { get; } = new List<string>();

            protected override async Task<HttpResponseMessage> SendAsync(RequestOptions options, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var content = options.Content;
                Bodies.Add(content is null ? null : await content.ReadAsStringAsync());
                Uris.Add(options.RequestUri);
                Sends++;
                return new HttpResponseMessage(RetryOnce && Sends == 1 ? HttpStatusCode.Unauthorized : HttpStatusCode.OK)
                {
                    Content = new StringContent("ok")
                };
            }
        }

        public class UnmarkedDto : IValidatableObject
        {
            private int _validationCalls;
            [Required]
            public string Name { get; set; }
            public int GetValidationCalls() => _validationCalls;
            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                _validationCalls++;
                return new[] { new ValidationResult("Object rule failed.") };
            }
        }

        [ValidateInput]
        public class MarkedDto : UnmarkedDto { }
        public sealed class UnmarkedDerivedDto : MarkedDto { }
        [ValidateOutput]
        public sealed class OutputOnlyDto : UnmarkedDto { }

        [ValidateInput]
        public struct MarkedValueDto
        {
            [Range(1, 10)]
            public int Quantity { get; set; }
        }

        [ValidateInput]
        public sealed class MarkedParent
        {
            public MarkedDto Child { get; set; }
        }

        [ValidateInput]
        public sealed class MarkedDictionary : Dictionary<string, MarkedDto>, IValidatableObject
        {
            private int _validationCalls;
            public int GetValidationCalls() => _validationCalls;
            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                _validationCalls++;
                return new[] { new ValidationResult("Container must not be validated.") };
            }
        }

        [ValidateInput]
        public sealed class CountedDto : IValidatableObject
        {
            private int _validationCalls;
            [Required]
            public string Name { get; set; }
            public int GetValidationCalls() => _validationCalls;
            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                _validationCalls++;
                return Array.Empty<ValidationResult>();
            }
        }
    }
}
