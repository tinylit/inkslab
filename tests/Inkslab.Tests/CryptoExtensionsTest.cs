using System;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Inkslab.Tests
{
    /// <summary>
    /// <see cref="CryptoExtensions"/> 测试。
    /// </summary>
    public class CryptoExtensionsTest
    {
        private const string TestData = "Hello World! 测试数据 123456 !@#$%^&*()";
        private const string ShortData = "Test";
        private const string LongData = "This is a very long test string that contains multiple sentences. It includes various characters like numbers 1234567890, special symbols !@#$%^&*()_+, and Chinese characters 这是一个很长的测试字符串，包含多种字符类型。";

        #region DES 加密测试

        /// <summary>
        /// DES 加密/解密测试（默认算法）。
        /// </summary>
        [Fact]
        public void DES_EncryptDecrypt_ShouldWork()
        {
            string key = "Test@*$!"; // 8字符键
            string data = TestData;

            var encryptedData = data.Encrypt(key, CryptoKind.DES);
            var decryptedData = encryptedData.Decrypt(key, CryptoKind.DES);

            Assert.NotEqual(data, encryptedData);
            Assert.Equal(data, decryptedData);
        }

        /// <summary>
        /// DES 使用默认参数测试（应该使用DES）。
        /// </summary>
        [Fact]
        public void DES_DefaultCrypto_ShouldUseDES()
        {
            string key = "Test@*$!";
            string data = TestData;

            var encrypted1 = data.Encrypt(key); // 默认使用DES
            var encrypted2 = data.Encrypt(key, CryptoKind.DES);

            var decrypted1 = encrypted1.Decrypt(key);
            var decrypted2 = encrypted2.Decrypt(key, CryptoKind.DES);

            Assert.Equal(data, decrypted1);
            Assert.Equal(data, decrypted2);
        }

        /// <summary>
        /// DES 无效键长度测试。
        /// </summary>
        [Theory]
        [InlineData("short")] // 5字符，太短
        [InlineData("toolongkey")] // 10字符，太长
        public void DES_InvalidKeyLength_ShouldThrowException(string invalidKey)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => 
                TestData.Encrypt(invalidKey, CryptoKind.DES));
        }

        #endregion

        #region TripleDES 加密测试

        /// <summary>
        /// TripleDES 加密/解密测试。
        /// </summary>
        [Theory]
        [InlineData("1234567890123456")] // 16字符
        [InlineData("123456789012345678901234")] // 24字符
        public void TripleDES_EncryptDecrypt_ShouldWork(string key)
        {
            string data = TestData;

            var encryptedData = data.Encrypt(key, CryptoKind.TripleDES);
            var decryptedData = encryptedData.Decrypt(key, CryptoKind.TripleDES);

            Assert.NotEqual(data, encryptedData);
            Assert.Equal(data, decryptedData);
        }

        /// <summary>
        /// TripleDES 无效键长度测试。
        /// </summary>
        [Theory]
        [InlineData("short")] // 太短
        [InlineData("toolongkeytoolongkey")] // 20字符，无效长度
        public void TripleDES_InvalidKeyLength_ShouldThrowException(string invalidKey)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => 
                TestData.Encrypt(invalidKey, CryptoKind.TripleDES));
        }

        #endregion

        #region RC2 加密测试

        /// <summary>
        /// RC2 加密/解密测试。
        /// </summary>
        [Theory]
        [InlineData("12345")] // 5字符
        [InlineData("1234567890")] // 10字符
        [InlineData("1234567890123456")] // 16字符
        public void RC2_EncryptDecrypt_ShouldWork(string key)
        {
            string data = TestData;

            var encryptedData = data.Encrypt(key, CryptoKind.RC2);
            var decryptedData = encryptedData.Decrypt(key, CryptoKind.RC2);

            Assert.NotEqual(data, encryptedData);
            Assert.Equal(data, decryptedData);
        }

        /// <summary>
        /// RC2 无效键长度测试。
        /// </summary>
        [Theory]
        [InlineData("1234")] // 4字符，太短
        public void RC2_InvalidKeyLength_ShouldThrowException(string invalidKey)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => 
                TestData.Encrypt(invalidKey, CryptoKind.RC2));
        }

        #endregion

        #region AES 加密测试

        /// <summary>
        /// AES 加密/解密测试。
        /// </summary>
        [Theory]
        [InlineData("1234567890123456")] // 16字符 (128-bit)
        [InlineData("123456789012345678901234")] // 24字符 (192-bit)
        [InlineData("12345678901234567890123456789012")] // 32字符 (256-bit)
        public void AES_EncryptDecrypt_ShouldWork(string key)
        {
            string data = TestData;

            var encryptedData = data.Encrypt(key, CryptoKind.AES);
            var decryptedData = encryptedData.Decrypt(key, CryptoKind.AES);

            Assert.NotEqual(data, encryptedData);
            Assert.Equal(data, decryptedData);
        }

        /// <summary>
        /// AES 无效键长度测试。
        /// </summary>
        [Theory]
        [InlineData("short")] // 太短
        [InlineData("1234567890123456789")] // 19字符，无效长度
        public void AES_InvalidKeyLength_ShouldThrowException(string invalidKey)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => 
                TestData.Encrypt(invalidKey, CryptoKind.AES));
        }

        #endregion

        #region RSA 加密测试

        /// <summary>
        /// RSA 加密/解密测试。
        /// </summary>
        [Fact]
        public void RSA_EncryptDecrypt_ShouldWork()
        {
            string publicXml;
            string privateXml;

            using (var rsa = RSA.Create())
            {
                publicXml = rsa.ToXmlString(false);
                privateXml = rsa.ToXmlString(true);
            }

            string data = TestData;

            var encryptedData = data.Encrypt(publicXml, CryptoKind.RSA);
            var decryptedData = encryptedData.Decrypt(privateXml, CryptoKind.RSA);

            Assert.NotEqual(data, encryptedData);
            Assert.Equal(data, decryptedData);
        }

        /// <summary>
        /// RSA 使用私钥加密，私钥解密测试。
        /// </summary>
        [Fact]
        public void RSA_PrivateKeyEncryptDecrypt_ShouldWork()
        {
            string publicXml;
            string privateXml;

            using (var rsa = RSA.Create())
            {
                publicXml = rsa.ToXmlString(false);
                privateXml = rsa.ToXmlString(true);
            }

            string data = ShortData; // RSA对数据长度有限制

            var encryptedData = data.Encrypt(privateXml, CryptoKind.RSA);
            var decryptedData = encryptedData.Decrypt(privateXml, CryptoKind.RSA);

            Assert.Equal(data, decryptedData);
        }

        /// <summary>
        /// RSA 不同密钥长度测试。
        /// </summary>
        [Theory]
        [InlineData(1024)]
        [InlineData(2048)]
        public void RSA_DifferentKeySizes_ShouldWork(int keySize)
        {
            string publicXml;
            string privateXml;

            using (var rsa = RSA.Create(keySize))
            {
                publicXml = rsa.ToXmlString(false);
                privateXml = rsa.ToXmlString(true);
            }

            string data = ShortData;

            var encryptedData = data.Encrypt(publicXml, CryptoKind.RSA);
            var decryptedData = encryptedData.Decrypt(privateXml, CryptoKind.RSA);

            Assert.Equal(data, decryptedData);
        }

        #endregion

        #region MD5 哈希测试

        /// <summary>
        /// MD5 大写哈希测试。
        /// </summary>
        [Fact]
        public void MD5_UpperCase_ShouldReturnCorrectHash()
        {
            var expectedMd5 = "3A7E9DE6BA28B738B6141658D72E9CFB"; // 数据源于在线MD5加密，32位【大】
            string data = "加密/解密测试。";

            var result = data.Md5();

            Assert.Equal(expectedMd5, result);
        }

        /// <summary>
        /// MD5 小写哈希测试。
        /// </summary>
        [Fact]
        public void MD5_LowerCase_ShouldReturnCorrectHash()
        {
            var expectedMd5 = "3a7e9de6ba28b738b6141658d72e9cfb"; // 小写版本
            string data = "加密/解密测试。";

            var result = data.Md5(toUpperCase: false);

            Assert.Equal(expectedMd5, result);
        }

        /// <summary>
        /// MD5 不同编码测试。
        /// </summary>
        [Theory]
        [InlineData("Hello", "8B1A9953C4611296A827ABF8C47804D7")] // UTF8 编码
        public void MD5_DifferentEncodings_ShouldReturnCorrectHash(string input, string expectedHash)
        {
            var result1 = input.Md5(Encoding.UTF8);
            var result2 = input.Md5(Encoding.ASCII);

            Assert.Equal(expectedHash, result1);
            // ASCII 和 UTF8 对于纯英文字符应该相同
            Assert.Equal(result1, result2);
        }

        /// <summary>
        /// MD5 空字符串测试。
        /// </summary>
        [Fact]
        public void MD5_EmptyString_ShouldReturnKnownHash()
        {
            var expectedMd5 = "D41D8CD98F00B204E9800998ECF8427E"; // 空字符串的MD5
            string data = "";

            var result = data.Md5();

            Assert.Equal(expectedMd5, result);
        }

        /// <summary>
        /// MD5 长字符串测试。
        /// </summary>
        [Fact]
        public void MD5_LongString_ShouldWork()
        {
            var result = LongData.Md5();

            Assert.NotNull(result);
            Assert.Equal(32, result.Length); // MD5 总是32个字符
            Assert.Matches("^[A-F0-9]{32}$", result); // 应该是大写十六进制
        }

        #endregion

        #region 异常处理测试

        /// <summary>
        /// 空数据参数测试。
        /// </summary>
        [Theory]
        [InlineData(CryptoKind.DES)]
        [InlineData(CryptoKind.AES)]
        [InlineData(CryptoKind.RSA)]
        public void Encrypt_NullData_ShouldThrowArgumentNullException(CryptoKind kind)
        {
            string key = kind == CryptoKind.RSA ? GetValidRSAKey() : "12345678";
            
            Assert.Throws<ArgumentNullException>(() => 
                ((string)null).Encrypt(key, kind));
        }

        /// <summary>
        /// 空键参数测试。
        /// </summary>
        [Theory]
        [InlineData(CryptoKind.DES)]
        [InlineData(CryptoKind.AES)]
        [InlineData(CryptoKind.RSA)]
        public void Encrypt_NullKey_ShouldThrowArgumentNullException(CryptoKind kind)
        {
            Assert.Throws<ArgumentNullException>(() => 
                TestData.Encrypt(null, kind));
        }

        /// <summary>
        /// 解密空数据参数测试。
        /// </summary>
        [Fact]
        public void Decrypt_NullData_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => 
                ((string)null).Decrypt("12345678"));
        }

        /// <summary>
        /// 解密空键参数测试。
        /// </summary>
        [Fact]
        public void Decrypt_NullKey_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => 
                "somedata".Decrypt(null));
        }

        /// <summary>
        /// MD5 空数据参数测试。
        /// </summary>
        [Fact]
        public void MD5_NullData_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => 
                ((string)null).Md5());
        }

        /// <summary>
        /// 不支持的加密类型测试。
        /// </summary>
        [Fact]
        public void GetSymmetricAlgorithm_UnsupportedKind_ShouldThrowNotSupportedException()
        {
            // 使用无效的枚举值
            var invalidKind = (CryptoKind)999;
            
            Assert.Throws<NotSupportedException>(() => 
                TestData.Encrypt("12345678", invalidKind));
        }

        /// <summary>
        /// RSA 无效密钥格式测试。
        /// </summary>
        [Fact]
        public void RSA_InvalidKeyFormat_ShouldThrowException()
        {
            string invalidKey = "This is not a valid RSA key";
            
            Assert.ThrowsAny<Exception>(() => 
                TestData.Encrypt(invalidKey, CryptoKind.RSA));
        }

        /// <summary>
        /// 解密无效的Base64数据测试。
        /// </summary>
        [Fact]
        public void Decrypt_InvalidBase64_ShouldThrowException()
        {
            string invalidData = "This is not base64!@#$%";
            
            Assert.ThrowsAny<Exception>(() => 
                invalidData.Decrypt("12345678"));
        }

        #endregion

        #region 跨算法兼容性测试

        /// <summary>
        /// 不同算法间不应该兼容。
        /// </summary>
        [Fact]
        public void DifferentAlgorithms_ShouldNotBeCompatible()
        {
            string key16 = "1234567890123456"; // 16字符，适用于AES和TripleDES
            string data = TestData;

            var aesEncrypted = data.Encrypt(key16, CryptoKind.AES);
            var tripleDesEncrypted = data.Encrypt(key16, CryptoKind.TripleDES);

            // 相同数据和密钥，不同算法应该产生不同的加密结果
            Assert.NotEqual(aesEncrypted, tripleDesEncrypted);

            // 交叉解密应该失败或产生不同结果
            Assert.ThrowsAny<Exception>(() => 
                aesEncrypted.Decrypt(key16, CryptoKind.TripleDES));
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取有效的RSA密钥。
        /// </summary>
        private string GetValidRSAKey()
        {
            using (var rsa = RSA.Create())
            {
                return rsa.ToXmlString(true);
            }
        }

        #endregion

        #region 原有测试保持兼容性

        /// <summary>
        /// 加密/解密测试（保持原有测试）。
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
        /// MD5（保持原有测试）。
        /// </summary>
        [Fact]
        public void Md5Test()
        {
            var md5 = "3A7E9DE6BA28B738B6141658D72E9CFB"; //数据源于在线MD5加密，32位【大】。https://www.sojson.com/encrypt_md5.html

            string data = "加密/解密测试。";

            var r = data.Md5();

            Assert.Equal(md5, r);
        }

        /// <summary>
        /// RSA（保持原有测试）
        /// </summary>
        [Fact]
        public void RSATest()
        {
            string publicXml;
            string privateXml;

            using (var rsa = RSA.Create())
            {
                publicXml = rsa.ToXmlString(false);
                privateXml = rsa.ToXmlString(true);
            }

            string data = "加密/解密测试。";

            var encrypt = data.Encrypt(publicXml, CryptoKind.RSA);

            var value = encrypt.Decrypt(privateXml, CryptoKind.RSA);

            Assert.Equal(data, value);
        }

        #endregion
    }
}
