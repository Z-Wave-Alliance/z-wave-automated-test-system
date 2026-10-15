// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Security V2 Test case")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    public class appbasic_SecurityV2 : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Secondary = new Alias(2);
            Alias[] ALL = new Alias[] { Primary, Secondary };

            //ParallelService.Reset(ALL);

            //Service.AddNode(Primary, Secondary);
            Service.MemoryGetId(Primary);
            Service.MemoryGetId(Secondary);

            //#region Tests cmac execution
            //TestCmacXor128();
            //TestCmacLeftShiftOneBit();
            //TestCmacPadding();
            //TestSubkeyGeneration();
            //TestCmacIetfRfc4493TestVectorsExample1();
            //TestCmacIetfRfc4493TestVectorsExample4Verify();
            //#endregion

            //#region Tests CCM execution
            //test_verify_example_3();
            ////test_verify_all_payload_lengths();
            //#endregion

          

            ////Kex1-3
            //var kex1Cmd = new COMMAND_CLASS_SECURITY_2.SECURITY_KEX_1();
            //kex1Cmd.dskNonceA = new byte[8] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };
            //kex1Cmd.supportedEcdhProfiles = 0x01;

            //var kex2Cmd = new COMMAND_CLASS_SECURITY_2.SECURITY_KEX_2();
            //kex2Cmd.dskNonceB = new byte[8] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };
            //kex2Cmd.chosenEcdhProfile = 0x01;
            //kex2Cmd.messageAuthenticationCode = new byte[8] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };
            //kex2Cmd.deviceEcdhPublicKey = new byte[32] { 
            //                                            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            //                                            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            //                                            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            //                                            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };

            //var kex3Cmd = new COMMAND_CLASS_SECURITY_2.SECURITY_KEX_3();
            //kex3Cmd.controllerEcdhPublicKey = new byte[32] { 
            //                                            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            //                                            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            //                                            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            //                                            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 }; //place another

            //kex3Cmd.messageAuthenticationCode = new byte[8] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };


            //ApplicationService.AddResponse(Secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_KEX_2(), //BUT WHY?
            //    kex1Cmd);
            //ApplicationService.AddResponse(Primary, kex3Cmd,
            //    kex2Cmd);

            //Service.RequestData(Primary, Secondary, kex1Cmd,
            //    kex2Cmd);


            //var encryptedText = File.ReadAllBytes(@"D:\work\Examples\CryptoApp\Crypted.txt");
            //var key = File.ReadAllBytes(@"D:\work\Examples\CryptoApp\key.kf");
            //var iVector = File.ReadAllBytes(@"D:\work\Examples\CryptoApp\iv.kf");
            //Aes aes = new AesCryptoServiceProvider();
            //aes.KeySize = 128;
            //aes.GenerateKey();
            //aes.Mode = CipherMode.ECB;
            //byte[] K1 = new byte[aes.Key.Length];
            //byte[] K2 = new byte[aes.Key.Length];
            //generateSubkey(aes.Key, ref K1, ref K2);
            //var helloEnc = EncryptStringToBytes_Aes("This is plaint text we have to work with!", aes.Key, aes.IV, aes.Mode);
            //var helloDecr = DecryptStringFromBytes_Aes(encryptedText, key, iVector, aes.Mode);

        }
        #region Tests aes_cmac
        private void TestSubkeyGeneration()
        {
            byte[] key = new byte[] { 0x2b, 0x7e, 0x15, 0x16, 0x28, 0xae, 0xd2, 0xa6, 0xab, 0xf7, 0x15, 0x88, 0x09, 0xcf, 0x4f, 0x3c };
            byte[] subKey1 = new byte[] { 0xfb, 0xee, 0xd6, 0x18, 0x35, 0x71, 0x33, 0x66, 0x7c, 0x85, 0xe0, 0x8f, 0x72, 0x36, 0xa8, 0xde };
            byte[] subKey2 = new byte[] { 0xf7, 0xdd, 0xac, 0x30, 0x6a, 0xe2, 0x66, 0xcc, 0xf9, 0x0b, 0xc1, 0x1e, 0xe4, 0x6d, 0x51, 0x3b };

            byte[] out1 = new byte[16];
            byte[] out2 = new byte[16];


            GenerateSubkey(key, ref out1, ref out2);

            AssertIsTrue(subKey1.SequenceEqual(out1), "Pass K1",
                "Fail K1");
            AssertIsTrue(subKey2.SequenceEqual(out2), "Pass K2",
                "Fail K2");
        }

        private void TestCmacIetfRfc4493TestVectorsExample1()
        {
            byte[] key = new byte[] { 0x2b, 0x7e, 0x15, 0x16, 0x28, 0xae, 0xd2, 0xa6, 0xab, 0xf7, 0x15, 0x88, 0x09, 0xcf, 0x4f, 0x3c }; // Valid
            byte[] expected_mac = new byte[] { 0xbb, 0x1d, 0x69, 0x29, 0xe9, 0x59, 0x37, 0x28, 0x7f, 0xa3, 0x7d, 0x12, 0x9b, 0x75, 0x67, 0x46 };

            byte[] calculated_mac = new byte[16];

            calculated_mac = AesCmacCalculate(key, new byte[] { 0x33, 0x33 }, 0);

            AssertIsTrue(expected_mac.SequenceEqual(calculated_mac), "Pass calculated_mac",
                "Fail calculated_mac");
        }

        private void TestCmacIetfRfc4493TestVectorsExample4Verify()
        {
            byte[] key = new byte[] { 0x2b, 0x7e, 0x15, 0x16, 0x28, 0xae, 0xd2, 0xa6, 0xab, 0xf7, 0x15, 0x88, 0x09, 0xcf, 0x4f, 0x3c }; // Valid
            byte[] input = new byte[] {0x6b, 0xc1, 0xbe, 0xe2, 0x2e, 0x40, 0x9f, 0x96, 0xe9, 0x3d, 0x7e, 0x11, 0x73, 0x93, 0x17, 0x2a,
                  0xae, 0x2d, 0x8a, 0x57, 0x1e, 0x03, 0xac, 0x9c, 0x9e, 0xb7, 0x6f, 0xac, 0x45, 0xaf, 0x8e, 0x51,
                  0x30, 0xc8, 0x1c, 0x46, 0xa3, 0x5c, 0xe4, 0x11, 0xe5, 0xfb, 0xc1, 0x19, 0x1a, 0x0a, 0x52, 0xef,
                  0xf6, 0x9f, 0x24, 0x45, 0xdf, 0x4f, 0x9b, 0x17, 0xad, 0x2b, 0x41, 0x7b, 0xe6, 0x6c, 0x37, 0x10};
            byte[] mac = new byte[] { 0x51, 0xf0, 0xbe, 0xbf, 0x7e, 0x3b, 0x9d, 0x92, 0xfc, 0x49, 0x74, 0x17, 0x79, 0x36, 0x3c, 0xfe };

            bool verifyResult = false;

            verifyResult = AesCmacVerify(key, input, input.Length, mac);

            AssertIsTrue(verifyResult, "Pass AesCmacVerify",
                "Fail AesCmacVerify");
        }

        private void TestCmacLeftShiftOneBit()
        {
            byte[] value = { 0xaa, 0x51, 0x58, 0xb2, 0x58, 0x99, 0x80, 0x0b, 0x61, 0xaa, 0xa0, 0x42, 0x98, 0xf8, 0x38, 0xd0 };
            byte[] expected = new byte[] { 0x54, 0xa2, 0xb1, 0x64, 0xb1, 0x33, 0x0, 0x16, 0xc3, 0x55, 0x40, 0x85, 0x31, 0xf0, 0x71, 0xa0 };

            byte[] output = new byte[16];

            LeftshiftOnebit(ref value, ref output);

            AssertIsTrue(expected.SequenceEqual(output), "Pass LeftShiftOneBit",
                "Fail LeftShiftOneBit");
        }

        private void TestCmacXor128()
        {
            byte[] value1 = new byte[] { 0xaa, 0x51, 0x58, 0xb2, 0x58, 0x99, 0x80, 0x0b, 0x61, 0xaa, 0xa0, 0x42, 0x98, 0xf8, 0x38, 0xd0 };
            byte[] value2 = new byte[] { 0x0f, 0xe7, 0x09, 0x20, 0x26, 0x45, 0x76, 0xa4, 0xc7, 0xc7, 0xc2, 0xd2, 0x9b, 0x7a, 0x46, 0xb8 };
            byte[] expected = new byte[] { 0xa5, 0xb6, 0x51, 0x92, 0x7e, 0xdc, 0xf6, 0xaf, 0xa6, 0x6d, 0x62, 0x90, 0x3, 0x82, 0x7e, 0x68 };

            byte[] output = new byte[16];

            Xor128(value1, 0, value2, ref output);

            AssertIsTrue(expected.SequenceEqual(output), "Pass Xor128",
                "Fail Xor128");
        }

        private void TestCmacPadding()
        {
            byte[] value = new byte[] { 0x38, 0xd0 };
            byte[] expected = new byte[] { 0x38, 0xd0, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            byte[] output = new byte[16];

            Padding(value, 0, ref output, value.Length);

            AssertIsTrue(expected.SequenceEqual(output), "Pass Padding",
                "Fail Padding");
        }

        #endregion

        #region Tests ccm

        private void test_verify_all_payload_lengths()
        {
            byte[] key = new byte[16] { 0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4a, 0x4b, 0x4c, 0x4d, 0x4e, 0x4f };
            byte[] nonce = new byte[15 - 7] { 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17 };
            int aad_len = 16;
            byte[] aad = new byte[16] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f };
            byte[] plaintext = new byte[16];
            int ciphertext_len;
            byte[] text_to_encrypt = new byte[] { 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c, 0x2d, 0x2e, 0x2f,
            0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c, 0x2d, 0x2e, 0x2f,
            0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c, 0x2d, 0x2e, 0x2f,
            0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c, 0x2d, 0x2e };
            int text_to_encrypt_len = text_to_encrypt.Length;
            byte[] nist_cipher_and_auth_tag = new byte[16 + 6] { 0xd2, 0xa1, 0xf0, 0xe0, 0x51, 0xea, 0x5f, 0x62, 0x08, 0x1a, 0x77, 0x92, 0x07, 0x3d, 0x59, 0x3d, 0x1f, 0xc6, 0x4f, 0xbf, 0xac, 0xcd };
            int ret;

            q = 7;
            t = 6;
            n = 8;
            ciphertext_len = text_to_encrypt_len + t;
            byte[] ciphertext = new byte[ciphertext_len];

            ret = CCM_encrypt_and_auth(key, nonce, aad, aad_len, text_to_encrypt, text_to_encrypt_len, ciphertext);
            if (ret == 0)
            { }

            ret = CCM_decrypt_and_auth(key, nonce, aad, aad_len, ciphertext, ciphertext_len, plaintext);
            if (ret == 0)
            { }

            //for (i = 1; i < 0xffff; i++)
            //{
            //    plaintext_len = text_to_encrypt_len = i;
            //    ciphertext_len = text_to_encrypt_len + t;
            //    //ciphertext = malloc(ciphertext_len);
            //    //if (ciphertext == NULL)
            //    //{
            //    //    printf("malloc failed for ciphertext\n");
            //    //    ret = 1;
            //    //    goto out3;
            //    //}
            //    //text_to_encrypt = malloc(text_to_encrypt_len);
            //    //if (ciphertext == NULL)
            //    //{
            //    //    printf("malloc failed for ciphertext\n");
            //    //    ret = 1;
            //    //    goto out2;
            //    //}
            //    //plaintext = malloc(plaintext_len);
            //    //if (ciphertext == NULL)
            //    //{
            //    //    printf("malloc failed for ciphertext\n");
            //    //    ret = 1;
            //    //    goto out1;
            //    //}
            //    for (j = 0; j < i; j++)
            //    {
            //        text_to_encrypt[j] = (byte)j;
            //    }
            //    ret = CCM_encrypt_and_auth(key, nonce, aad, aad_len, text_to_encrypt, text_to_encrypt_len, ciphertext);
            //    if (ret == 0)
            //    {
            //        //printf("EXAMPLE2 TEST FAILED\n");
            //    }
            //}
        }

        private void test_verify_example_3()
        {
            byte[] key = new byte[16] { 0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4a, 0x4b, 0x4c, 0x4d, 0x4e, 0x4f };
            byte[] nonce = new byte[15 - 7] { 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17 };
            byte[] aad = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13 };
            int aad_len = aad.Length;
            byte[] plaintext = new byte[16];
            int ciphertext_len;
            byte[] text_to_encrypt = new byte[] { 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c, 0x2d, 0x2e, 0x2f, 
                                                  0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37 };
            int text_to_encrypt_len = text_to_encrypt.Length;
            byte[] nist_cipher_and_auth_tag = new byte[] { 0xe3,  0xb2, 0x01, 0xa9, 
                0xf5, 0xb7, 0x1a, 0x7a, 
                0x9b, 0x1c, 0xea, 0xec,
                0xcd, 0x97, 0xe7, 0x0b, 
                0x61, 0x76, 0xaa, 0xd9, 
                0xa4, 0x42, 0x8a, 0xa5, 
                0x48, 0x43, 0x92, 0xfb, 
                0xc1, 0xb0, 0x99, 0x51};
            int ret;

            q = 7;
            t = 6;
            n = 8;
            ciphertext_len = text_to_encrypt_len + t;
            byte[] ciphertext = new byte[ciphertext_len];

            ret = CCM_encrypt_and_auth(key, nonce, aad, aad_len, text_to_encrypt, text_to_encrypt_len, ciphertext);
            if (ret == 0)
            { }

            ret = CCM_decrypt_and_auth(key, nonce, aad, aad_len, ciphertext, ciphertext_len, plaintext);
            if (ret == 0)
            { }

            //for (i = 1; i < 0xffff; i++)
            //{
            //    plaintext_len = text_to_encrypt_len = i;
            //    ciphertext_len = text_to_encrypt_len + t;
            //    //ciphertext = malloc(ciphertext_len);
            //    //if (ciphertext == NULL)
            //    //{
            //    //    printf("malloc failed for ciphertext\n");
            //    //    ret = 1;
            //    //    goto out3;
            //    //}
            //    //text_to_encrypt = malloc(text_to_encrypt_len);
            //    //if (ciphertext == NULL)
            //    //{
            //    //    printf("malloc failed for ciphertext\n");
            //    //    ret = 1;
            //    //    goto out2;
            //    //}
            //    //plaintext = malloc(plaintext_len);
            //    //if (ciphertext == NULL)
            //    //{
            //    //    printf("malloc failed for ciphertext\n");
            //    //    ret = 1;
            //    //    goto out1;
            //    //}
            //    for (j = 0; j < i; j++)
            //    {
            //        text_to_encrypt[j] = (byte)j;
            //    }
            //    ret = CCM_encrypt_and_auth(key, nonce, aad, aad_len, text_to_encrypt, text_to_encrypt_len, ciphertext);
            //    if (ret == 0)
            //    {
            //        //printf("EXAMPLE2 TEST FAILED\n");
            //    }
            //}
        }

        #endregion

        #region AES methods
        private string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV, CipherMode mode)
        {
            // Check arguments. 
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            // Declare the string used to hold 
            // the decrypted text. 
            string plaintext = null;

            // Create an AesCryptoServiceProvider object 
            // with the specified key and IV. 
            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.Padding = PaddingMode.Zeros;
                aesAlg.Mode = mode;
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption. 
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream 
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }

            return plaintext;

        }

        private byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV, CipherMode mode)
        {
            // Check arguments. 
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");
            byte[] encrypted;
            // Create an AesCryptoServiceProvider object 
            // with the specified key and IV. 
            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                aesAlg.Padding = PaddingMode.Zeros;
                aesAlg.Mode = mode;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption. 
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }
            // Return the encrypted bytes from the memory stream. 
            return encrypted;

        }

        private byte[] EncryptBytes_Aes(byte[] dataToEncrypt, byte[] Key, byte[] IV, CipherMode mode)
        {
            // Check arguments. 
            if (dataToEncrypt == null || dataToEncrypt.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");
            byte[] encrypted;
            // Create an AesCryptoServiceProvider object 
            // with the specified key and IV. 
            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                aesAlg.Padding = PaddingMode.Zeros;
                aesAlg.Mode = mode;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption. 
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(dataToEncrypt, 0, dataToEncrypt.Length);
                    }
                    encrypted = msEncrypt.ToArray();
                }
            }
            // Return the encrypted bytes from the memory stream. 
            return encrypted;

        }
        #endregion

        #region AES CMAC algo
        private void GenerateSubkey(byte[] key, ref byte[] K1, ref byte[] K2)
        {
            byte[] L = new byte[16];
            byte[] tmp = new byte[16];

            byte[] const_Zero = new byte[16] {
              0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
              0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };

            byte[] const_Rb = new byte[16] {
              0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
              0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x87
            };

            /*
             * L := AES-128(Key, const_Zero);
             */
            Aes aes = new AesCryptoServiceProvider();
            aes.Mode = CipherMode.ECB;
            aes.KeySize = 128;
            L = EncryptBytes_Aes(const_Zero, key, aes.IV, aes.Mode);
            //AES128_ECB_encrypt((uint8_t*)const_Zero, key, L);

            if (0 == (L[0] & 0x80)) // if MSB(L) is equal to 0 then K1 := L << 1;
            {
                LeftshiftOnebit(ref L, ref K1);
            }
            else
            { // else K1 := (L << 1) XOR const_Rb;
                LeftshiftOnebit(ref L, ref tmp);
                Xor128(tmp, 0, const_Rb, ref K1);
            }

            if (0 == (K1[0] & 0x80)) // if MSB(K1) is equal to 0 then K2 := K1 << 1;
            {
                LeftshiftOnebit(ref K1, ref K2);
            }
            else // else K2 := (K1 << 1) XOR const_Rb;
            {
                LeftshiftOnebit(ref K1, ref tmp);
                Xor128(tmp, 0, const_Rb, ref K2);
            }
        }

        private void LeftshiftOnebit(ref byte[] input, ref byte[] output)
        {
            sbyte count;
            byte overflow = 0;

            for (count = 15; count >= 0; count--)
            {
                output[count] = (byte)(input[count] << 1);
                output[count] |= overflow;
                overflow = (input[count] & 0x80) == 0x80 ? (byte)1 : (byte)0;
            }
        }

        private void Xor128(byte[] a, int aStartIndex, byte[] b, ref byte[] output)
        {
            Xor128(a, aStartIndex, b, 0, ref output);
        }

        private void Xor128(byte[] a, int aStartIndex, byte[] b, int bStartIndex, ref byte[] output)
        {
            int count;
            for (count = 0; count < 16; count++)
            {
                output[count] = (byte)(a[count + aStartIndex] ^ b[count + bStartIndex]);
            }
        }

        //void padding(unsigned char *lastb, unsigned char *pad, int length)
        private void Padding(byte[] lastb, int aStartIndex, ref byte[] pad, int length)
        {
            int j;

            /* original last block */
            for (j = 0; j < 16; j++)
            {
                if (j < length)
                {
                    pad[j] = lastb[j + aStartIndex];
                }
                else if (j == length)
                {
                    pad[j] = 0x80;
                }
                else
                {
                    pad[j] = 0x00;
                }
            }
        }

        private byte[] AesCmacCalculate(byte[] key, byte[] message, int message_length)
        {
            byte[] mac = new byte[16];
            byte[] X = new byte[16];
            byte[] Y = new byte[16];
            byte[] M_last = new byte[16];
            byte[] padded = new byte[16];
            byte[] K1 = new byte[16];
            byte[] K2 = new byte[16];
            bool flag;
            int n, i;

            Aes aes = new AesCryptoServiceProvider();
            aes.KeySize = 128;
            aes.GenerateKey();
            aes.Mode = CipherMode.ECB;

            GenerateSubkey(key, ref K1, ref K2);

            n = (message_length + 15) / 16; // n is number of rounds

            if (0 == n)
            {
                n = 1;
                flag = false;
            }
            else
            {
                if (0 == (message_length % 16))  /* last block is a complete block */
                {
                    flag = true;
                }
                else
                { /* last block is not complete block */
                    flag = false;
                }
            }

            if (flag) /* last block is complete block */
            {
                Xor128(message, 16 * (n - 1), K1, ref M_last);
            }
            else
            {
                Padding(message, 16 * (n - 1), ref padded, message_length % 16);
                Xor128(padded, 0, K2, ref M_last);
            }

            for (i = 0; i < 16; i++)
            {
                X[i] = 0;
            }

            for (i = 0; i < (n - 1); i++)
            {
                Xor128(X, 0, message, 16 * i, ref Y); /* Y := Mi (+) X  */
                X = EncryptBytes_Aes(Y, key, aes.IV, aes.Mode);
                //AES128_ECB_encrypt(Y, key, X); // X := AES-128(key, Y);
            }

            Xor128(X, 0, M_last, ref Y);
            X = EncryptBytes_Aes(Y, key, aes.IV, aes.Mode);
            //AES128_ECB_encrypt(Y, key, X); // X := AES-128(key, Y);

            for (i = 0; i < 16; i++)
            {
                mac[i] = X[i];
            }
            return mac;
        }

        private bool AesCmacVerify(byte[] key, byte[] message, int message_length, byte[] mac)
        {
            bool ret = true;
            byte[] calculated_mac = new byte[16];
            int count;

            calculated_mac = AesCmacCalculate(key, message, message_length);

            for (count = 0; count < 16; count++)
            {
                if (mac[count] != calculated_mac[count])
                {
                    ret = false;
                }
            }
            return ret;
        }
        #endregion

        #region CCM algo

        /* Converts @number to octet string @oct_str of octet length oct_len
         * e.g. if text_to_encrypt_len is 512 and q is 3 then Q is 00000000 00000010 00000000
         * P.S. octet strings are stored in array of unsinged 8 bit integer
         * See section A.1 and A.2.1 in the document mentioned in header */
        static void convert_to_octet_string(int text_to_encrypt_len, byte[] Q, int q)
        {
            int i;
            int bitmask = 0xff00;

            /* As the text_to_encrypt_len wont be more than 0xffff. There can be max 2 octet strings (Q)
             * so initialize the Q octet string to all zeroes except last two bytes. which is going to be p(text_to_encrypt_len)*/
            for (i = 0; i < (q - 2); i++)
                Q[i] = 0;

            /* Fill second last byte with 8 MSBs of text_to_encrypt_len.
                for e.g. text_to_encrypt_len is 512 (00000010 00000000) then Q[i] is 00000010 */
            Q[i] = (byte)((text_to_encrypt_len & bitmask) >> 8);
            bitmask = bitmask >> 8;
            /* Fill last byte with remaining 8 LSBs of text_to_encrypt_len.
                for e.g. text_to_encrypt_len is 512 (00000010 00000000) then Q[i+1] is 00000000 */
            Q[i + 1] = (byte)(text_to_encrypt_len & bitmask);
        }

        #region global vars
        public readonly byte B0_AAD = 0x40;
        public int q = 2;
        public int n = 13; // n is the octet length of nonce. And (n = 15 - q)
        public int t = 8; /* Length of auth_tag in bytes*/
        public int blocks_used = 0;
        public readonly int BLOCK_SIZE = 16;
        #endregion

        /* See section A.2.1 in the document mentioned in header */
        private int format_b0(int t, int text_to_encrypt_len, byte[] first_block, byte[] nonce)
        {
            int i;
            byte[] Q = new byte[q];

            first_block[0] = 0;

            /* do not set resreved bit */

            first_block[0] = (byte)(first_block[0] | B0_AAD); /* set Adata bit. See A.2.1 */
            first_block[0] = (byte)(first_block[0] | ((((t - 2) / 2) & 0x7) << 3));
            first_block[0] = (byte)(first_block[0] | ((q - 1) & 0x7));

            /* Copy the nonce from 1st byte (not 0th). See Table 2 in A.2.1 */
            for (i = 1; i < n + 1; i++)
            {
                first_block[i] = nonce[i - 1];
            }

            convert_to_octet_string(text_to_encrypt_len, Q, q);

            /* Copy the Q after nonce. See Table 2 in A.2.1 */
            Array.Copy(Q, 0, first_block, i, q);
            blocks_used++;
            return 1;
        }

        private void ciph_block(ref byte[] blocks, byte[] key)
        {
            Aes aes = new AesCryptoServiceProvider();
            aes.Mode = CipherMode.ECB;
            aes.KeySize = 128;
            blocks = EncryptBytes_Aes(blocks, key, aes.IV, aes.Mode);
        }

        /*See section A.2.2 in NIST pdf */
        private int format_aad(byte[][] blocks, byte[] aad, int aad_len)
        {
            int i;
            int offset;
            int new_aad_len;
            int no_of_blocks_for_aad;
            int aad_bytes_in_first_block;
            int aad_start = 2;

            /* See more about this lenghth ranges in beginning of page 14 in the document mentioned in header */
            if (aad_len < 65280)
            {
                blocks[1][0] = 0x0;
                blocks[1][1] = (byte)aad_len;
                aad_bytes_in_first_block = 14;
            }
            else if ((aad_len >= 65280))
            { /*TODO Check the limits */
                blocks[1][0] = 0xff;
                blocks[1][1] = 0xfe;
                blocks[1][2] = 0;
                blocks[1][3] = 1;
                blocks[1][4] = 0;
                blocks[1][5] = 0;
                aad_bytes_in_first_block = 10;
                aad_start = 6;
            }
            else
            {
                //printf("AAD length more than 2pow(16) - 2pow(8) is not supported ");
                return 0;
            }
            //memcpy(&blocks[1][aad_start], &aad[0], aad_bytes_in_first_block);/* Fill the first block's 14 octets with first 14 octets of aad*/
            Array.Copy(aad, 0, blocks[1], aad_start, aad_bytes_in_first_block); /* Fill the first block's 14 octets with first 14 octets of aad*/

            blocks_used++;
            /* Copy first 14 octets of AAD to blocks[1] */
            if (aad_len < aad_bytes_in_first_block)
            { /* need to pad with zeroes if aad_len is less than 14*/
                for (int padCounter = 0; padCounter < aad_bytes_in_first_block - aad_len; padCounter++)
                {
                    blocks[1][aad_start + aad_len + padCounter] = 0;
                }
                //memset(&blocks[1][aad_start] + aad_len, 0, aad_bytes_in_first_block - aad_len);
            }
            else
            { /* If aad_len is more than 14 we need more blocks */
                new_aad_len = aad_len - aad_bytes_in_first_block;
                no_of_blocks_for_aad = new_aad_len / BLOCK_SIZE; /* Find the number of more blocks needed */
                if (new_aad_len % BLOCK_SIZE >= 1)
                    no_of_blocks_for_aad++;

                offset = aad_bytes_in_first_block; /* Start copying the octets from aad_bytes_in_first_block'th offset*/
                for (i = 0; i < no_of_blocks_for_aad; i++)
                {

                    /* if its the last block and not aligned to BLOCK_SIZE bytes */
                    if ((i == (no_of_blocks_for_aad - 1)) && ((new_aad_len % BLOCK_SIZE) >= 1))
                    {
                        //memcpy(&blocks[i + 2][0], &aad[offset], (new_aad_len % BLOCK_SIZE));
                        Array.Copy(aad, offset, blocks[i + 2], 0, (new_aad_len % BLOCK_SIZE));
                        /* pad with zeroes */
                        for (int padCounter = 0; padCounter < BLOCK_SIZE - (new_aad_len % BLOCK_SIZE); padCounter++)
                        {
                            blocks[i + 2][0 + (new_aad_len % BLOCK_SIZE) + padCounter] = 0;
                        }
                        //memset(&blocks[i + 2][0] + (new_aad_len % BLOCK_SIZE), 0, BLOCK_SIZE - (new_aad_len % BLOCK_SIZE));
                        blocks_used++;
                        break;
                    }

                    //memcpy(&blocks[i + 2][0], &aad[offset], BLOCK_SIZE);
                    Array.Copy(aad, offset, blocks[i + 2], 0, BLOCK_SIZE);
                    //   ccm_print("blocks_used: %d, offset: %d, i=%d\n", blocks_used, offset, i);
                    offset += BLOCK_SIZE;
                    blocks_used++;
                    //    print_block(blocks[i+2], BLOCK_SIZE);
                }
            }
            return 1;
        }


        private void format_payload_block(byte[][] blocks, byte[] P,
                                 int text_to_encrypt_len)
        {
            int i;
            int no_blocks_payload = (text_to_encrypt_len / BLOCK_SIZE);
            int offset = 0;

            if ((text_to_encrypt_len % BLOCK_SIZE) >= 1)
                no_blocks_payload++;

            no_blocks_payload += blocks_used;

            for (i = blocks_used; i < no_blocks_payload; i++)
            {
                if ((i == (no_blocks_payload - 1)) && (text_to_encrypt_len % BLOCK_SIZE) >= 1)
                { /* If its a last block and not aligned to BLOCK_SIZE */
                    //memcpy(&blocks[i][0], &P[offset], (text_to_encrypt_len % BLOCK_SIZE));
                    Array.Copy(P, offset, blocks[i], 0, (text_to_encrypt_len % BLOCK_SIZE));
                    /* pad with zeroes */
                    //memset(&blocks[i][0] + (text_to_encrypt_len % BLOCK_SIZE), 0, BLOCK_SIZE - (text_to_encrypt_len % BLOCK_SIZE));
                    for (int padCounter = 0; padCounter < BLOCK_SIZE - (text_to_encrypt_len % BLOCK_SIZE); padCounter++)
                    {
                        blocks[i][0 + (text_to_encrypt_len % BLOCK_SIZE) + padCounter] = 0;
                    }
                    blocks_used++;
                    break;
                }

                //memcpy(&blocks[i][0], &P[offset], BLOCK_SIZE);
                Array.Copy(P, offset, blocks[i], 0, BLOCK_SIZE);
                offset += BLOCK_SIZE;
                blocks_used++;

            }
        }

        private void bit_xor(byte[] block, int bOffset, byte[] next_block, int size)
        {
            int i;
            for (i = 0; i < size; i++)
                next_block[i] = (byte)(block[i + bOffset] ^ next_block[i]);
        }

        private int format(byte[] nonce, byte[] aad, int aad_len,
                  byte[] key, byte[][] blocks,
                  byte[] text_to_encrypt, int text_to_encrypt_len,
                  int mac_len)
        {
            format_b0(mac_len, text_to_encrypt_len, blocks[0], nonce);
            if (!(format_aad(blocks, aad, aad_len) >= 1))
                return 0;

            format_payload_block(blocks, text_to_encrypt, text_to_encrypt_len);
            return 1;
        }

        private void set_mac(byte[] mac, byte[] block, int mac_len)
        {
            Array.Copy(block, mac, mac_len);
            //memcpy(mac, block, mac_len);
        }

        private void format_counter_blocks(int no, byte[][] ctr, byte[] nonce)
        {
            int i;

            for (i = 0; i < no; i++)
            {
                //memset(ctr[i], 0, BLOCK_SIZE);
                for (int padCounter = 0; padCounter < n; padCounter++)
                {
                    ctr[i][1 + padCounter] = 0;
                }
                ctr[i][0] = 0;
                ctr[i][0] |= (byte)((q - 1) & 0x7); /* Add flag see table 4 in A.3 */
                //memcpy(&ctr[i][1], nonce, n); /* Add N see table 2 in A.3  */
                Array.Copy(nonce, 0, ctr[i], 1, n);

                /* FIXME:as max payload length is 2 pow(16). Last two octets of counters is sufficient */
                ctr[i][14] = (byte)((i & 0xff00) >> 8); /*Add counter see table 2 in A.3  */
                ctr[i][15] = (byte)(i & 0xff);
                //ccm_print("ctr[%d]:", i);
                //print_block(ctr[i], BLOCK_SIZE);
            }
        }

        private int find_blocks_needed(int text_to_encrypt_len, int aad_len)
        {
            int blocks_needed = 2; /* one for flags and one for aad. P.S. We will always have AAD */

            if ((aad_len >= 65280))
            { /*TODO Check the limits */
                blocks_needed += (aad_len - 9) / 16;
                if (((aad_len - 9) % 16) >= 1)
                    blocks_needed++;
            }
            else if (aad_len < 65280)
            {
                if (aad_len > 14)
                { /* 14 is number of aad bytes which fit in the 1th block */
                    blocks_needed = blocks_needed + ((aad_len - 14) / BLOCK_SIZE);
                    if (((aad_len - 14) % BLOCK_SIZE) >= 1)
                        blocks_needed++;
                }
            }
            else
            {
                //printf("AAD length is more than 0xffffffff");
                return 0;
            }

            blocks_needed += text_to_encrypt_len / BLOCK_SIZE;
            if ((text_to_encrypt_len % BLOCK_SIZE) >= 1)
                blocks_needed++;

            return blocks_needed;
        }

        private int CCM_encrypt_and_auth(byte[] key, byte[] nonce, byte[] aad,
                          int aad_len, byte[] text_to_encrypt,
                         int text_to_encrypt_len, byte[] ciphertext)
        {
            int mac_len = t;
            byte[] mac = new byte[mac_len]; /* see mac_len */
            int i;
            int counter_blocks = (text_to_encrypt_len / BLOCK_SIZE) + 1;
            int max_blocks_needed = find_blocks_needed(text_to_encrypt_len, aad_len);
            byte[][] blocks = new byte[max_blocks_needed][];
            byte[] first_block = new byte[BLOCK_SIZE * max_blocks_needed];/*allocate 16byte (128 bit) blocks */
            int ret;

            if (max_blocks_needed == 0) /*To check if find_blocks_needed() returned 0*/
                return 0;

            blocks_used = 0;
            if ((text_to_encrypt_len % BLOCK_SIZE) >= 1)
                counter_blocks++;

            //mac = malloc(mac_len);
            //if (mac == NULL)
            //{
            //    printf("malloc failed in allocating mac/auth_tag\n");
            //    return 0;
            //}

            //blocks = malloc(max_blocks_needed * sizeof(uint8_t*));
            //if (blocks == NULL)
            //{
            //    printf("malloc failed in allocating first level block pointers\n");
            //    ret = 0;
            //    goto free_mac;
            //}

            //first_block = malloc(BLOCK_SIZE * max_blocks_needed); /*allocate 16byte (128 bit) blocks */
            //if (first_block == NULL)
            //{
            //    printf("malloc failed in allocating block\n");
            //    ret = 0;
            //    goto free_blocks_mac;
            //}
            for (i = 0; i < max_blocks_needed; i++)
            {
                blocks[i] = new byte[BLOCK_SIZE];
            }

            if (!(format(nonce, aad, aad_len, key, blocks, text_to_encrypt, text_to_encrypt_len, mac_len) >= 1))
            {
                ret = 0;
                //goto free_all;
            }

            //ccm_print("blocks[0]: ");
            //print_block(blocks[0], BLOCK_SIZE);
            ciph_block(ref blocks[0], key);
            for (i = 1; i < max_blocks_needed; i++)
            {
                //ccm_print("blocks[%d]; ",i);
                //print_block(blocks[i], BLOCK_SIZE);
                bit_xor(blocks[i - 1], 0, blocks[i], BLOCK_SIZE);
                ciph_block(ref blocks[i], key);
            }
            set_mac(mac, blocks[i - 1], mac_len);
            //ccm_print("MAC/T/auth tag: ");
            //print_block(mac, mac_len);
            format_counter_blocks(counter_blocks, blocks, nonce);

            for (i = 0; i < max_blocks_needed; i++)
            {
                ciph_block(ref blocks[i], key);
            }

            int xorOffset = 0;
            for (int xorCounter = 1; xorCounter < blocks.Length; xorCounter++)
            {
                int xorCleanLength = text_to_encrypt_len - xorOffset - 1;
                int xorLength = xorCleanLength > 16 ? 16 : xorCleanLength;
                bit_xor(text_to_encrypt, xorOffset, blocks[xorCounter], xorLength);/* step 7 in NIST pdf*/
                if (xorOffset < text_to_encrypt_len && xorCleanLength > 16)
                {
                    xorOffset += BLOCK_SIZE - 1;
                }
                else
                {
                    break;
                }
            }
            bit_xor(mac, 0, blocks[0], mac_len); /* step 8 in NIST pdf*/
            //ccm_print("ciphertext: ");
            //print_block(blocks[1], text_to_encrypt_len);
            //print_block(blocks[0], mac_len);
            //memcpy(ciphertext, blocks[1], text_to_encrypt_len);
            Array.Copy(blocks[1], ciphertext, text_to_encrypt_len);
            //memcpy(ciphertext + text_to_encrypt_len, blocks[0], mac_len);
            Array.Copy(blocks[0], 0, ciphertext, text_to_encrypt_len, mac_len);

            ret = text_to_encrypt_len + mac_len;
            //free_all:
            //    free(first_block);
            //free_blocks_mac:
            //    free(blocks);
            //free_mac:
            //    free(mac);

            return ret;
        }

        private int CCM_decrypt_and_auth(byte[] key, byte[] nonce, byte[] aad,
                  int aad_len, byte[] ciphertext,
                 int ciphertext_len, byte[] plaintext)
        {
            int mac_len = t;
            byte[] mac = new byte[mac_len]; /* see mac_len */
            int i, j;
            int num_counter_blocks = (ciphertext_len - t) / BLOCK_SIZE + 1;
            int max_blocks_needed = find_blocks_needed(ciphertext_len - t, aad_len);
            byte[][] blocks = new byte[max_blocks_needed][];
            byte[] first_block = new byte[BLOCK_SIZE * max_blocks_needed];/*allocate 16byte (128 bit) blocks */
            int ret;

            if (max_blocks_needed == 0) /*To check if find_blocks_needed() returned 0*/
                return 0;

            blocks_used = 0;
            if ((ciphertext_len % BLOCK_SIZE) >= 1)
                num_counter_blocks++;

            //mac = malloc(mac_len);
            //if (mac == NULL)
            //{
            //    printf("malloc failed in allocating mac/auth_tag\n");
            //    return 0;
            //}

            //blocks = malloc(max_blocks_needed * sizeof(uint8_t*));
            //if (blocks == NULL)
            //{
            //    printf("malloc failed in allocating first level block pointers\n");
            //    ret = 0;
            //    goto free_mac;
            //}

            //first_block = malloc(BLOCK_SIZE * max_blocks_needed); /*allocate 16byte (128 bit) blocks */
            //if (first_block == NULL)
            //{
            //    printf("malloc failed in allocating block\n");
            //    ret = 0;
            //    goto free_blocks_mac;
            //}
            for (i = 0; i < max_blocks_needed; i++)
            {
                blocks[i] = new byte[BLOCK_SIZE];
            }

            format_counter_blocks(num_counter_blocks, blocks, nonce);

            for (i = 0; i < num_counter_blocks; i++)
            {
                ciph_block(ref blocks[i], key);
            }
            bit_xor(ciphertext, 0, blocks[1], ciphertext_len - t);
            //memcpy(plaintext, blocks[1], ciphertext_len - t);
            Array.Copy(blocks[1], plaintext, ciphertext_len - t);
            bit_xor(ciphertext.Skip(ciphertext.Length - ciphertext_len - t).Take(ciphertext_len - t).ToArray(),
                0, blocks[0], t);
            //memcpy(mac, blocks[0], t);
            Array.Copy(blocks[0], mac, t);

            if (!(format(nonce, aad, aad_len, key, blocks, plaintext, ciphertext_len - t, mac_len) >= 1))
            {
                ret = 0;
            }

            //print_block(blocks[0], BLOCK_SIZE);
            ciph_block(ref blocks[0], key);
            for (i = 1; i < blocks_used; i++)
            {
                //print_block(blocks[i], BLOCK_SIZE);
                bit_xor(blocks[i - 1], 0, blocks[i], BLOCK_SIZE);
                ciph_block(ref blocks[i], key);
            }
            for (j = 0; j < t; j++)
            {
                if (blocks[i - 1][j] != mac[j])
                {
                    //printf("INVALID See step 10 in NIST pdf\n"); /* TODO: Come up with nice error message here */
                    //for (int padCounter = 0; padCounter < ciphertext_len - t; padCounter++)
                    //{
                    //    plaintext[padCounter] = 0;
                    //}
                    //memset(plaintext, 0, ciphertext_len - t);
                    ret = 0;
                }
            }

            ret = ciphertext_len - t;
            return ret;
        }


        #endregion
    }
}