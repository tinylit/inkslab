using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Inkslab.DI.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

#pragma warning disable CS1591

namespace Inkslab.DI.UnitTests
{
    public class RegistrationMutationTests
    {
        [Fact]
        public void RegisteredServicesUsesHashSet()
        {
            var services = new ServiceCollection();
            using var dependencyInjection = services.DependencyInjection(new DependencyInjectionOptions());
            var registeredServices = dependencyInjection.GetType().GetField(
                "_registeredServices", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(registeredServices);
            Assert.Equal(typeof(HashSet<Type>), registeredServices.FieldType);
        }

        [Fact]
        public void RemovedDescriptorDoesNotLeaveServiceMarkedAsRegistered()
        {
            var services = Configure(collection =>
            {
                var descriptor = ServiceDescriptor.Transient<IRemovedService, RemovedService>();
                collection.Add(descriptor);
                Assert.True(collection.Remove(descriptor));
            }, out var dependencyInjection);

            dependencyInjection.Add<IRemovedService>();

            Assert.Single(services, x => x.ServiceType == typeof(IRemovedService));
        }

        [Fact]
        public void RemoveAllDoesNotLeaveServiceMarkedAsRegistered()
        {
            var services = Configure(collection =>
            {
                collection.AddTransient<IRemoveAllService, RemoveAllService>();
                collection.AddSingleton<IRemoveAllService, RemoveAllService>();
                collection.RemoveAll<IRemoveAllService>();
            }, out var dependencyInjection);

            dependencyInjection.Add<IRemoveAllService>();

            Assert.Single(services, x => x.ServiceType == typeof(IRemoveAllService));
        }

        [Fact]
        public void ClearRemovesAllServiceTypeRegistrationsFromIndex()
        {
            var services = Configure(collection =>
            {
                collection.AddTransient<IClearedService, ClearedService>();
                collection.Clear();
            }, out var dependencyInjection);

            dependencyInjection.Add<IClearedService>();

            Assert.Single(services, x => x.ServiceType == typeof(IClearedService));
        }

        [Fact]
        public void IndexReplacementRemovesOldTypeAndTracksNewType()
        {
            var services = Configure(collection =>
            {
                collection.AddTransient<IReplacedService, ReplacedService>();
                collection[0] = ServiceDescriptor.Transient<IReplacementService, ReplacementService>();
            }, out var dependencyInjection);

            dependencyInjection.Add<IReplacedService>();
            dependencyInjection.Add<IReplacementService>();

            Assert.Single(services, x => x.ServiceType == typeof(IReplacedService));
            Assert.Single(services, x => x.ServiceType == typeof(IReplacementService));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RemovingOneDuplicateKeepsServiceMarkedAsRegistered(bool removeAt)
        {
            var services = Configure(collection =>
            {
                collection.AddTransient<IDuplicateService, DuplicateService>();
                collection.AddSingleton<IDuplicateService, DuplicateService>();
                if (removeAt)
                {
                    collection.RemoveAt(0);
                }
                else
                {
                    Assert.True(collection.Remove(collection[0]));
                }
            }, out var dependencyInjection);

            dependencyInjection.Add<IDuplicateService>();

            Assert.Single(services, x => x.ServiceType == typeof(IDuplicateService));
            Assert.Equal(ServiceLifetime.Singleton, services.Single(x => x.ServiceType == typeof(IDuplicateService)).Lifetime);
        }

        [Fact]
        public void ReplacingOneDuplicateKeepsRemainingServiceMarkedAsRegistered()
        {
            var services = Configure(collection =>
            {
                collection.AddTransient<IDuplicateService, DuplicateService>();
                collection.AddSingleton<IDuplicateService, DuplicateService>();
                collection[0] = ServiceDescriptor.Transient<IReplacementService, ReplacementService>();
            }, out var dependencyInjection);

            dependencyInjection.Add<IDuplicateService>();
            dependencyInjection.Add<IReplacementService>();

            Assert.Single(services, x => x.ServiceType == typeof(IReplacementService));
            Assert.Equal(ServiceLifetime.Singleton, Assert.Single(services, x => x.ServiceType == typeof(IDuplicateService)).Lifetime);
        }

        [Fact]
        public void DisposeCanBeCalledMoreThanOnce()
        {
            var services = new ServiceCollection();
            var dependencyInjection = services.DependencyInjection(new DependencyInjectionOptions());

            dependencyInjection.Dispose();
            var exception = Record.Exception(dependencyInjection.Dispose);

            Assert.Null(exception);
        }

#if NET8_0_OR_GREATER
        [Theory]
        [InlineData("Remove")]
        [InlineData("RemoveAt")]
        [InlineData("Replace")]
        public void RemovingUnkeyedDescriptorDoesNotCountRemainingKeyedDescriptor(string operation)
        {
            var services = Configure(collection =>
            {
                collection.AddTransient<IKeyedService, KeyedService>();
                collection.AddKeyedSingleton<IKeyedService, KeyedService>("key");
                MutateFirstDescriptor(collection, operation);
            }, out var dependencyInjection);

            dependencyInjection.Add<IKeyedService>();

            Assert.Single(services, x => x.ServiceType == typeof(IKeyedService) && x.IsKeyedService);
            Assert.Single(services, x => x.ServiceType == typeof(IKeyedService) && !x.IsKeyedService);
        }

        [Theory]
        [InlineData("Remove")]
        [InlineData("RemoveAt")]
        [InlineData("Replace")]
        public void RemovingKeyedDescriptorKeepsUnkeyedServiceMarkedAsRegistered(string operation)
        {
            var services = Configure(collection =>
            {
                collection.AddKeyedTransient<IKeyedService, KeyedService>("key");
                collection.AddSingleton<IKeyedService, KeyedService>();
                MutateFirstDescriptor(collection, operation);
            }, out var dependencyInjection);

            dependencyInjection.Add<IKeyedService>();

            var descriptor = Assert.Single(services, x => x.ServiceType == typeof(IKeyedService));
            Assert.False(descriptor.IsKeyedService);
            Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        }

        private static void MutateFirstDescriptor(IServiceCollection collection, string operation)
        {
            switch (operation)
            {
                case "Remove":
                    Assert.True(collection.Remove(collection[0]));
                    break;
                case "RemoveAt":
                    collection.RemoveAt(0);
                    break;
                default:
                    collection[0] = ServiceDescriptor.Transient<IReplacementService, ReplacementService>();
                    break;
            }
        }

        [Fact]
        public void KeyedDescriptorsAreSkippedAndDoNotCountAsUnkeyedRegistrations()
        {
            var services = new ServiceCollection();
            services.AddKeyedTransient<IKeyedService, KeyedService>("key");
            services.AddKeyedSingleton<IKeyedInstanceService>("instance", new KeyedInstanceService());
            services.AddKeyedTransient<IKeyedFactoryService>("factory", (_, _) => new KeyedFactoryService());
            using var dependencyInjection = services.DependencyInjection(new DependencyInjectionOptions());
            dependencyInjection.AddAssembly(typeof(RegistrationMutationTests).Assembly);

            var exception = Record.Exception(() => dependencyInjection.ConfigureByExamine(x =>
                x == typeof(IKeyedService)
                || x == typeof(IKeyedInstanceService)
                || x == typeof(IKeyedFactoryService)));
            dependencyInjection.Add<IKeyedService>();
            dependencyInjection.Add<IKeyedInstanceService>();
            dependencyInjection.Add<IKeyedFactoryService>();

            Assert.Null(exception);
            Assert.Single(services, x => x.ServiceType == typeof(IKeyedService) && x.IsKeyedService);
            Assert.Single(services, x => x.ServiceType == typeof(IKeyedService) && !x.IsKeyedService);
            Assert.Single(services, x => x.ServiceType == typeof(IKeyedInstanceService) && x.IsKeyedService);
            Assert.Single(services, x => x.ServiceType == typeof(IKeyedInstanceService) && !x.IsKeyedService);
            Assert.Single(services, x => x.ServiceType == typeof(IKeyedFactoryService) && x.IsKeyedService);
            Assert.Single(services, x => x.ServiceType == typeof(IKeyedFactoryService) && !x.IsKeyedService);
        }
#endif

        private static ServiceCollection Configure(Action<IServiceCollection> mutation, out IDependencyInjectionServices dependencyInjection)
        {
            MutationConfigureServices.Mutation = mutation;

            try
            {
                var services = new ServiceCollection();
                dependencyInjection = services.DependencyInjection(new DependencyInjectionOptions());
                dependencyInjection.AddAssembly(typeof(RegistrationMutationTests).Assembly);
                dependencyInjection.ConfigureByDefined();
                return services;
            }
            finally
            {
                MutationConfigureServices.Mutation = null;
            }
        }
    }

    public sealed class MutationConfigureServices : IConfigureServices
    {
        public static Action<IServiceCollection> Mutation { get; set; }

        public void ConfigureServices(IServiceCollection services) => Mutation?.Invoke(services);
    }

    public interface IRemovedService { }
    public sealed class RemovedService : IRemovedService { }
    public interface IRemoveAllService { }
    public sealed class RemoveAllService : IRemoveAllService { }
    public interface IClearedService { }
    public sealed class ClearedService : IClearedService { }
    public interface IReplacedService { }
    public sealed class ReplacedService : IReplacedService { }
    public interface IReplacementService { }
    public sealed class ReplacementService : IReplacementService { }
    public interface IDuplicateService { }
    public sealed class DuplicateService : IDuplicateService { }
#if NET8_0_OR_GREATER
    public interface IKeyedService { }
    public sealed class KeyedService : IKeyedService { }
    public interface IKeyedInstanceService { }
    public sealed class KeyedInstanceService : IKeyedInstanceService { }
    public interface IKeyedFactoryService { }
    public sealed class KeyedFactoryService : IKeyedFactoryService { }
#endif
}
