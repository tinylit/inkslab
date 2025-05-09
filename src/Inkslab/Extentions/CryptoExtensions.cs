﻿using System.IO;
using System.Security.Cryptography;
using System.Text;

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace System
#pragma warning restore IDE0130 // 命名空间与文件夹结构不匹配
{
    /// <summary>
    /// 加密模式。
    /// </summary>
    public enum CryptoKind
    {
        /// <summary>
        /// 有效的 KEY 与 IV 长度，以英文字符为单位： KEY（Min:8 Max:8 Skip:0），IV（8）。
        /// </summary>
        DES,
        /// <summary>
        /// 有效的 KEY 与 IV 长度，以英文字符为单位： KEY（Min:16 Max:24 Skip:8），IV（8）。
        /// </summary>
        TripleDES,
        /// <summary>
        /// 有效的 KEY 与 IV 长度，以英文字符为单位： KEY（Min:5 Max:16 Skip:1），IV（8）。
        /// </summary>
        RC2,
        /// <summary>
        /// 有效的 KEY 与 IV 长度，以英文字符为单位： KEY（Min:16 Max:32 Skip:8），IV（16）。
        /// </summary>
        AES,
        /// <summary>
        /// 非对称加密。
        /// </summary>
        RSA
    }

    /// <summary>
    /// 加密扩展。
    /// </summary>
    public static class CryptoExtensions
    {
        private static SymmetricAlgorithm GetSymmetricAlgorithm(CryptoKind kind)
        {
            return kind switch
            {
                CryptoKind.DES => DES.Create(),
                CryptoKind.TripleDES => TripleDES.Create(),
                CryptoKind.RC2 => RC2.Create(),
                CryptoKind.AES => Aes.Create(),
                _ => throw new NotSupportedException()
            };
        }

        /// <summary>
        /// 加密（对称：<see cref="CipherMode.ECB"/>，<seealso cref="PaddingMode.PKCS7"/>；非对称：<see cref="RSAEncryptionPadding.Pkcs1"/>）。
        /// </summary>
        /// <param name="data">内容。</param>
        /// <param name="key">键。</param>
        /// <param name="kind">加密方式。</param>
        /// <returns></returns>
        public static string Encrypt(this string data, string key, CryptoKind kind = CryptoKind.DES)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (kind == CryptoKind.RSA)
            {
                using (var rsa = RSA.Create())
                {
                    rsa.FromXmlString(key);

                    return Convert.ToBase64String(
                        rsa.Encrypt(
                            Encoding.UTF8.GetBytes(data),
                            RSAEncryptionPadding.Pkcs1)
                    );
                }
            }

            ICryptoTransform crypto;

            using (var algorithm = GetSymmetricAlgorithm(kind))
            {
                var rgbKey = Encoding.UTF8.GetBytes(key);

                if (!algorithm.ValidKeySize(rgbKey.Length * 8))
                {
                    throw new ArgumentOutOfRangeException(nameof(key));
                }

                algorithm.Key = rgbKey;
                algorithm.Mode = CipherMode.ECB;
                algorithm.Padding = PaddingMode.PKCS7;

                crypto = algorithm.CreateEncryptor();
            }

            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, crypto, CryptoStreamMode.Write))
                {
                    var buffer = Encoding.UTF8.GetBytes(data);

                    cs.Write(buffer, 0, buffer.Length);

                    cs.FlushFinalBlock();

                    cs.Close();
                }

                return Convert.ToBase64String(ms.ToArray());
            }
        }

        /// <summary>
        /// 减密（对称：<see cref="CipherMode.ECB"/>，<seealso cref="PaddingMode.PKCS7"/>；非对称：<see cref="RSAEncryptionPadding.Pkcs1"/>）。
        /// </summary>
        /// <param name="data">内容。</param>
        /// <param name="key">键。</param>
        /// <param name="kind">减密方式。</param>
        /// <returns></returns>
        public static string Decrypt(this string data, string key, CryptoKind kind = CryptoKind.DES)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (kind == CryptoKind.RSA)
            {
                using (var rsa = RSA.Create())
                {
                    rsa.FromXmlString(key);

                    return Encoding.UTF8.GetString(
                        rsa.Decrypt(
                            Convert.FromBase64String(data),
                            RSAEncryptionPadding.Pkcs1)
                    );
                }
            }

            ICryptoTransform crypto;

            using (var algorithm = GetSymmetricAlgorithm(kind))
            {
                var rgbKey = Encoding.UTF8.GetBytes(key);

                if (!algorithm.ValidKeySize(rgbKey.Length * 8))
                {
                    throw new ArgumentOutOfRangeException(nameof(key));
                }

                algorithm.Key = rgbKey;
                algorithm.Mode = CipherMode.ECB;
                algorithm.Padding = PaddingMode.PKCS7;

                crypto = algorithm.CreateDecryptor();
            }

            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, crypto, CryptoStreamMode.Write))
                {
                    var buffer = Convert.FromBase64String(data);

                    cs.Write(buffer, 0, buffer.Length);

                    cs.FlushFinalBlock();

                    cs.Close();
                }

                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        /// <summary>
        /// MD5加密(32个字符)。
        /// </summary>
        /// <param name="data">数据。</param>
        /// <param name="encoding">编码，默认：UTF8。</param>
        /// <param name="toUpperCase">是否转为大小。</param>
        /// <returns></returns>
        public static string Md5(this string data, Encoding encoding = null, bool toUpperCase = true)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            byte[] buffer;

            using (var md5 = MD5.Create())
            {
                buffer = md5.ComputeHash((encoding ?? Encoding.UTF8).GetBytes(data));
            }

            var sb = new StringBuilder();

            for (int i = 0, length = buffer.Length; i < length; i++)
            {
                sb.Append(buffer[i].ToString(toUpperCase ? "X2" : "x2"));
            }

            return sb.ToString();
        }
    }
}
