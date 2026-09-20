using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Inkslab.Json;
using Inkslab.Net.Options;
using Inkslab.Net.Validation;
using Inkslab.Serialize.Json;
using Xunit;

namespace Inkslab.Net.Tests
{
    /// <summary>Validation treats containers and scalar values differently from entity DTOs.</summary>
    public class EntityValidationScopeTests
    {
        static EntityValidationScopeTests() => SingletonPools.TryAdd<IJsonHelper, DefaultJsonHelper>();

        /// <summary>Dictionary subclasses are not validated as entity objects.</summary>
        [Fact]
        public async Task DictionaryRequestSkipsEntityRulesAsync()
        {
            var value = new DictionaryWithRules { ["key"] = "value" };
            var factory = new ScopeFactory();
            await factory.CreateRequestable("https://unit.test/").Json((object)value).PostAsync();
            Assert.Equal("{\"key\":\"value\"}", factory.Body);
            Assert.Equal(0, value.ValidationCalls);
        }

        /// <summary>Container attributes and object rules are ignored without enumerating the contents.</summary>
        [Fact]
        public async Task ResponseContainersSkipRulesAndEnumerationAsync()
        {
            var dictionary = new DictionaryWithRules();
            var sequence = new SequenceWithRules();
            var readOnly = new ReadOnlyDictionaryWithRules();
            foreach (object container in new object[] { dictionary, sequence, readOnly, new Hashtable(), new object[] { new InvalidEntity() } })
            {
                var result = await new ScopeFactory().CreateRequestable("https://unit.test/")
                    .CustomCast(_ => container).Validation().GetAsync();
                Assert.Same(container, result);
            }
            Assert.Equal(0, dictionary.ValidationCalls);
            Assert.Equal(0, readOnly.ValidationCalls);
            Assert.Equal(0, sequence.ValidationCalls);
            Assert.Equal(0, sequence.EnumerationCalls);
        }

        /// <summary>Ordinary values can pass through final response validation unchanged.</summary>
        [Fact]
        public async Task ScalarResultsPassThroughAsync()
        {
            object[] values =
            {
                42, true, 'a', 1.25m, 1.0d, "text", DayOfWeek.Monday,
                Guid.Empty, DateTime.UnixEpoch, DateTimeOffset.UnixEpoch, TimeSpan.Zero,
                DateOnly.MinValue, TimeOnly.MinValue, new Uri("https://unit.test/"), new Version(1, 0),
                new KeyValuePair<string, int>("one", 1), (Half)1
#if NET8_0_OR_GREATER
                , Int128.MaxValue, UInt128.MaxValue
#endif
            };
            foreach (object value in values)
            {
                var result = await new ScopeFactory().CreateRequestable("https://unit.test/")
                    .CustomCast(_ => value).Validation().GetAsync();
                Assert.Same(value, result);
            }
        }

        /// <summary>A non-container entity still enforces its property constraints.</summary>
        [Fact]
        public async Task EntityRulesRemainRequiredAsync()
        {
            await Assert.ThrowsAsync<HttpEntityValidationException>(() => new ScopeFactory()
                .CreateRequestable("https://unit.test/").CustomCast(_ => new InvalidEntity()).Validation().GetAsync());
        }

        /// <summary>Options are value-based and may only be configured during initialization.</summary>
        [Fact]
        public void OptionsAreReadonlyValueWithInitProperties()
        {
            Assert.True(typeof(ValidationOptions).IsValueType);
            Assert.Contains(typeof(ValidationOptions).CustomAttributes,
                attribute => attribute.AttributeType.FullName == "System.Runtime.CompilerServices.IsReadOnlyAttribute");
            foreach (string name in new[] { "AllowNull", "ServiceProvider", "Items" })
            {
                var setter = typeof(ValidationOptions).GetProperty(name).SetMethod;
                Assert.Contains(setter.ReturnParameter.GetRequiredCustomModifiers(),
                    type => type.FullName == "System.Runtime.CompilerServices.IsExternalInit");
            }
        }

        /// <summary>Default and explicit default values retain the previous null policy.</summary>
        [Fact]
        public void DefaultOptionsRequireNoInitialization()
        {
            var options = ValidationOptions.Default;
            Assert.False(options.AllowNull);
            Assert.Null(options.ServiceProvider);
            Assert.Null(options.Items);
            Assert.Equal(default, options);
        }

        /// <summary>An empty Items dictionary is captured even when no dictionary copy is needed.</summary>
        [Fact]
        public async Task EmptyItemsAreIsolatedFromLaterMutationsAsync()
        {
            var items = new Dictionary<object, object>();
            var request = new ScopeFactory().CreateRequestable("https://unit.test/")
                .CustomCast(_ => new ContextIsolationEntity())
                .Validation(new ValidationOptions { Items = items });
            items["changed"] = true;
            await request.GetAsync();
            await request.GetAsync();
            Assert.Single(items);
        }

        /// <summary>Rule mutations affect only that invocation's context, not a later invocation.</summary>
        [Fact]
        public async Task NonEmptyItemsHaveIndependentValidationContextsAsync()
        {
            var items = new Dictionary<object, object> { ["initial"] = "value" };
            var request = new ScopeFactory().CreateRequestable("https://unit.test/")
                .CustomCast(_ => new ContextIsolationEntity())
                .Validation(new ValidationOptions { Items = items });
            items["changed"] = true;
            await request.GetAsync();
            await request.GetAsync();
            Assert.Equal(2, items.Count);
            Assert.False(items.ContainsKey("visited"));
        }

        private sealed class ScopeFactory : RequestFactory
        {
            public string Body { get; private set; }
            protected override async Task<HttpResponseMessage> SendAsync(RequestOptions options, CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                using var content = options.Content;
                Body = content is null ? null : await content.ReadAsStringAsync(token);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") };
            }
        }

        private sealed class InvalidEntity
        {
            [Required]
            public string Name { get; set; }
        }

        private sealed class ContextIsolationEntity : IValidatableObject
        {
            public IEnumerable<ValidationResult> Validate(ValidationContext context)
            {
                if (context.Items.ContainsKey("visited") || context.Items.ContainsKey("changed"))
                {
                    return new[] { new ValidationResult("The context was shared or changed after capture.") };
                }
                context.Items["visited"] = true;
                return Array.Empty<ValidationResult>();
            }
        }

        private sealed class DictionaryWithRules : Dictionary<string, string>, IValidatableObject
        {
            [Required]
            public string Name { get; set; }
            public int ValidationCalls { get; private set; }
            public IEnumerable<ValidationResult> Validate(ValidationContext context)
            {
                ValidationCalls++;
                return new[] { new ValidationResult("Not an entity") };
            }
        }

        private sealed class ReadOnlyDictionaryWithRules : ReadOnlyDictionary<string, object>, IValidatableObject
        {
            public ReadOnlyDictionaryWithRules() : base(new Dictionary<string, object>()) { }
            public int ValidationCalls { get; private set; }
            public IEnumerable<ValidationResult> Validate(ValidationContext context)
            {
                ValidationCalls++;
                return new[] { new ValidationResult("Not an entity") };
            }
        }

        private sealed class SequenceWithRules : IEnumerable, IValidatableObject
        {
            public int EnumerationCalls { get; private set; }
            public int ValidationCalls { get; private set; }
            public IEnumerator GetEnumerator()
            {
                EnumerationCalls++;
                throw new InvalidOperationException("Validation must not enumerate a sequence.");
            }
            public IEnumerable<ValidationResult> Validate(ValidationContext context)
            {
                ValidationCalls++;
                return new[] { new ValidationResult("Not an entity") };
            }
        }
    }
}
