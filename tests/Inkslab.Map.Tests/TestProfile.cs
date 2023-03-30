using System;
using System.Collections.Generic;
using System.Text;

namespace Inkslab.Map.Tests
{
    public class TestProfile : Profile
    {
        private readonly IConfiguration configuration;

        public TestProfile(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
    }
}
