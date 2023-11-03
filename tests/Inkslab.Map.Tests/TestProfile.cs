using System;
using System.Collections.Generic;
using System.Text;

namespace Inkslab.Map.Tests
{
    /// <inheritdoc/>
    public class TestProfile : Profile
    {
        private readonly IConfiguration configuration;

        /// <inheritdoc/>
        public TestProfile(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
    }
}
