namespace Inkslab.Map.Tests
{
    /// <inheritdoc/>
    public class TestProfile : Profile
    {
#pragma warning disable IDE0052 // Remove unread private members
        private readonly IConfiguration _configuration;
#pragma warning restore IDE0052 // Remove unread private members

        /// <inheritdoc/>
        public TestProfile(IConfiguration configuration)
        {
            _configuration = configuration;
        }
    }
}
