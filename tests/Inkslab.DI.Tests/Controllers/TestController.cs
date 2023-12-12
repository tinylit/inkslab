using System;
using System.Diagnostics;
using Inkslab.DI.Tests.DependencyInjections;
using Inkslab.DI.Tests.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Inkslab.DI.Tests.Controllers
{
    /// <summary>
    /// 接口测试。
    /// </summary>
    [ApiController]
    [Route("api/di-test")]
    public class TestController : ControllerBase
    {
        private readonly TestControllerCtor controllerCtor;
        private readonly SingletonTest singletonTest;
        private readonly ITestImplementElectionsByOne implementElectionsByOne;
        private readonly ILogger<TestController> logger;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public TestController(TestControllerCtor controllerCtor, SingletonTest singletonTest, ITestImplementElectionsByOne implementElectionsByOne, ILogger<TestController> logger, IServiceProvider serviceProvider)
        {
            this.controllerCtor = controllerCtor;
            this.singletonTest = singletonTest;
            this.implementElectionsByOne = implementElectionsByOne;
            this.logger = logger;

            var a = serviceProvider.GetRequiredService<ITypeDefinition<int>>();

            Debug.Assert(a is TypeDefinitionInt);

            var b = serviceProvider.GetRequiredService<ITypeDefinition<DateTime>>();
            
            Debug.Assert(b is TypeDefinition<DateTime>);
        }

        /// <summary>
        /// 单列测试。
        /// </summary>
        /// <returns></returns>
        [HttpGet("singleton")]
        public bool SingletonTest() => singletonTest.Test();

        /// <summary>
        /// 构造函数测试。
        /// </summary>
        /// <returns></returns>
        [HttpGet("constructor")]
        public bool ConstructorTest() => controllerCtor.Test();

        /// <summary>
        /// 标记注入测试。
        /// </summary>
        /// <returns></returns>
        [HttpGet("from-services")]
        public bool FromServicesTest([FromServices] ITestFromServices fromServices) => fromServices.Test();

        /// <summary>
        /// 实现选举测试。
        /// </summary>
        /// <returns></returns>
        [HttpGet("implement-elections")]
        public bool FromServicesTest([FromServices] ITestImplementElectionsByTwo implementElectionsByTwo) => implementElectionsByOne.Test() == 1 && implementElectionsByTwo.Test() == 2;

        /// <summary>
        /// 泛型注入测试。
        /// </summary>
        /// <returns></returns>
        [HttpGet("generic")]
        public bool GenericTest([FromServices] ITestGeneric<int> testGeneric) => testGeneric.CreateNew() == 0;

        [HttpPost]
        public IActionResult Post([FromBody] TestModel model)
        {
            logger.LogInformation(Request.Headers["Authorization"]);

            return Ok(model);
        }
    }
}