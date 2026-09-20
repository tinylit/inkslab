using System;
using System.Reflection;
using System.Reflection.Emit;
using Inkslab.DI.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

#pragma warning disable CS1591

namespace Inkslab.DI.UnitTests
{
    public class AssemblyCacheTests
    {
        [Fact]
        public void AddingAssemblyInvalidatesCachedImplementationCandidates()
        {
            var abstractImplementation = CreateAbstractImplementationAssembly();
            var concreteImplementation = CreateConcreteImplementationAssembly(abstractImplementation);
            var services = new ServiceCollection();
            using var dependencyInjection = services.DependencyInjection(new DependencyInjectionOptions());
            dependencyInjection.AddAssembly(abstractImplementation.Assembly);

            Assert.Throws<TypeLoadException>(() => dependencyInjection.Add<IAssemblyCacheService>());

            dependencyInjection.AddAssembly(concreteImplementation.Assembly);
            dependencyInjection.Add<IAssemblyCacheService>();

            var descriptor = Assert.Single(services, x => x.ServiceType == typeof(IAssemblyCacheService));
            Assert.Equal(concreteImplementation, descriptor.ImplementationType);
        }

        private static Type CreateAbstractImplementationAssembly()
        {
            var assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName($"Inkslab.DI.Cache.Abstract.{Guid.NewGuid():N}"),
                AssemblyBuilderAccess.Run);
            var module = assembly.DefineDynamicModule("main");
            var type = module.DefineType(
                "AbstractAssemblyCacheService",
                TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Class);
            type.AddInterfaceImplementation(typeof(IAssemblyCacheService));
            return type.CreateType();
        }

        private static Type CreateConcreteImplementationAssembly(Type baseType)
        {
            var assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName($"Inkslab.DI.Cache.Concrete.{Guid.NewGuid():N}"),
                AssemblyBuilderAccess.Run);
            var module = assembly.DefineDynamicModule("main");
            var type = module.DefineType(
                "ConcreteAssemblyCacheService",
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class,
                baseType);
            type.DefineDefaultConstructor(MethodAttributes.Public);
            return type.CreateType();
        }
    }

    public interface IAssemblyCacheService { }
}
