﻿using System;
using System.Collections.Generic;
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
        private readonly TestControllerCtor _controllerCtor;
        private readonly SingletonTest _singletonTest;
        private readonly ITestImplementElectionsByOne _implementElectionsByOne;
        private readonly ILogger<TestController> _logger;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public TestController(TestControllerCtor controllerCtor, SingletonTest singletonTest, ITestImplementElectionsByOne implementElectionsByOne, ILogger<TestController> logger, IEnumerable<ITestEnumerable> testEnumerables, ITypeDefinition<int> typeDefinitionInt, ITypeDefinition<DateTime> typeDefinitionDateTime, ITestSeekArguments testSeekArguments)
        {
            _controllerCtor = controllerCtor;
            _singletonTest = singletonTest;
            _implementElectionsByOne = implementElectionsByOne;
            _logger = logger;

            Debug.Assert(typeDefinitionInt is TypeDefinitionInt);

            Debug.Assert(typeDefinitionDateTime is TypeDefinition<DateTime>);
        }

        /// <summary>
        /// 单列测试。
        /// </summary>
        /// <returns></returns>
        [HttpGet("singleton")]
        public bool SingletonTest() => _singletonTest.Test();

        /// <summary>
        /// 构造函数测试。
        /// </summary>
        /// <returns></returns>
        [HttpGet("constructor")]
        public bool ConstructorTest() => _controllerCtor.Test();

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
        public bool FromServicesTest([FromServices] ITestImplementElectionsByTwo implementElectionsByTwo) => _implementElectionsByOne.Test() == 1 && implementElectionsByTwo.Test() == 2;

        /// <summary>
        /// 泛型注入测试。
        /// </summary>
        /// <returns></returns>
        [HttpGet("generic")]
        public bool GenericTest([FromServices] ITestGeneric<int> testGeneric) => testGeneric.CreateNew() == 0;

        /// <summary>
        /// 测试。
        /// </summary>
        /// <param name="model">数据模型。</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Post([FromBody] TestModel model)
        {
            _logger.LogInformation(Request.Headers["Authorization"]);

            return Ok(model);
        }
    }
}