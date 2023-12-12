using System;
using Xunit;

namespace Inkslab.Tests
{
    /// <summary>
    /// <see cref="CryptoExtensions"/> 测试。
    /// </summary>
    public class CryptoExtensionsTest
    {
        /// <summary>
        /// 加密/解密测试。
        /// </summary>
        [Fact]
        public void Test()
        {
            string key = "Test@*$!";
            string data = "加密/解密测试。";

            var encryptData = data.Encrypt(key);

            var decryptData = encryptData.Decrypt(key);

            Assert.Equal(data, decryptData);
        }

        /// <summary>
        /// 加密/解密测试。
        /// </summary>
        [Fact]
        public void IvTest()
        {
            string iv = "1234&^@?";
            string key = "Test@*$!";
            string data = "加密/解密测试。";

            var encryptData = data.Encrypt(key, iv);

            var decryptData = encryptData.Decrypt(key, iv);

            Assert.Equal(data, decryptData);
        }

        /// <summary>
        /// MD5。
        /// </summary>
        [Fact]
        public void Md5Test()
        {
            var md5 = "3A7E9DE6BA28B738B6141658D72E9CFB"; //数据源于在线MD5加密，32位【大】。https://www.sojson.com/encrypt_md5.html

            string data = "加密/解密测试。";

            var r = data.Md5();

            Assert.Equal(md5, r);
        }
    }
}
