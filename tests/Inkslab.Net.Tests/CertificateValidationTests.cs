using System.Net.Security;
using Xunit;

namespace Inkslab.Net.Tests
{
    /// <summary>
    /// TLS 证书校验开关测试。
    /// </summary>
    public class CertificateValidationTests
    {
        /// <summary>
        /// 默认放行任意证书（向后兼容）。
        /// </summary>
        [Fact]
        public void Default_AcceptsAnyCertificate()
        {
            RequestFactory.DangerousAcceptAnyServerCertificate = true;

            Assert.True(RequestFactory.ServerCertificateIsAcceptable(SslPolicyErrors.RemoteCertificateChainErrors));
        }

        /// <summary>
        /// 关闭放行后：无效证书被拒绝，有效证书通过。
        /// </summary>
        [Fact]
        public void WhenDisabled_RejectsInvalid_AcceptsValid()
        {
            RequestFactory.DangerousAcceptAnyServerCertificate = false;

            try
            {
                Assert.False(RequestFactory.ServerCertificateIsAcceptable(SslPolicyErrors.RemoteCertificateChainErrors));
                Assert.True(RequestFactory.ServerCertificateIsAcceptable(SslPolicyErrors.None));
            }
            finally
            {
                RequestFactory.DangerousAcceptAnyServerCertificate = true;
            }
        }
    }
}
