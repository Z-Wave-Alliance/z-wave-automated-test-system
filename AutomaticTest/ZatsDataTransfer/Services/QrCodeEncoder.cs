// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Zats.Services
{
    public class QRCodeEncoder
    {
        const int SIZE_OF_TYPE = 2;
        const int SIZE_OF_LENGTH = 2;
        const int SIZE_OF_QR_HEADER = 52;

        // All magic numbers you can find in file QRCodeEncoder-2 Document "REL14108-1" on hi-stage
        private QrHeader ExtractSmartStartQrHeader(string qrHeader)
        {
            QrHeader QrHeader = new QrHeader();
            try
            {
                if (qrHeader.Length >= 2)
                {
                    QrHeader.LeadIn = qrHeader.Substring(0, 2);
                    if (qrHeader.Length >= 4)
                    {
                        QrHeader.Version = qrHeader.Substring(2, 2);
                        if (qrHeader.Length >= 5)
                        {
                            QrHeader.Checksum = qrHeader.Substring(4, 5);
                            if (qrHeader.Length >= 12)
                            {
                                QrHeader.RequestedKeys = qrHeader.Substring(9, 3);
                                if (qrHeader.Length >= 52)
                                {
                                    QrHeader.DSK = qrHeader.Substring(12, 40);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ScanDSK corrupted ExtractSmartStartQrHeader: ");
                Console.WriteLine(ex.Message.ToString());
            }
            return QrHeader;
        }

        private List<Tlv> ExtractTlvExtensions(string tlvExtensions)
        {
            bool canNotRecognize = false;
            List<Tlv> result = new List<Tlv>();
            try
            {
                while (tlvExtensions != string.Empty && !canNotRecognize)
                {
                    int type = int.Parse(tlvExtensions.Substring(0, SIZE_OF_TYPE));
                    switch (type)
                    {
                        case 0:
                            Tlv0 Tlv0 = ParseTlv0(tlvExtensions);
                            tlvExtensions = tlvExtensions.Remove(0, Tlv0.Length + SIZE_OF_LENGTH + SIZE_OF_TYPE);
                            result.Add(Tlv0);
                            break;
                        case 2:
                            Tlv2 Tlv2 = ParseTlv2(tlvExtensions);
                            tlvExtensions = tlvExtensions.Remove(0, Tlv2.Length + SIZE_OF_LENGTH + SIZE_OF_TYPE);
                            result.Add(Tlv2);
                            break;
                        case 4:
                            Tlv4 Tlv4 = ParseTlv4(tlvExtensions);
                            tlvExtensions = tlvExtensions.Remove(0, Tlv4.Length + SIZE_OF_LENGTH + SIZE_OF_TYPE);
                            result.Add(Tlv4);
                            break;
                        case 6:
                            Tlv6 Tlv6 = ParseTlv6(tlvExtensions);
                            tlvExtensions = tlvExtensions.Remove(0, Tlv6.Length + SIZE_OF_LENGTH + SIZE_OF_TYPE);
                            result.Add(Tlv6);
                            break;
                        default:
                            canNotRecognize = true;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ScanDSK corrupted ExtractTlvExtensions: ");
                Console.WriteLine(ex.Message.ToString());
            }
            return result;
        }
        public QrCodeOptions ParseQrCode(string value)
        {
            QrCodeOptions qrCodeOptions = new QrCodeOptions
            {
                QrHeader = ExtractSmartStartQrHeader(value)
            };

            if (value.Length >= SIZE_OF_QR_HEADER)
            {
                string Extensions = value.Substring(SIZE_OF_QR_HEADER, value.Length - SIZE_OF_QR_HEADER);

                qrCodeOptions.TypesCollection = ExtractTlvExtensions(Extensions);
            }
            return qrCodeOptions;
        }

        public string CalculateCheksum(string input)
        {
            byte[] myByte = System.Text.ASCIIEncoding.Default.GetBytes(input);
            SHA1 sha = new SHA1CryptoServiceProvider();
            var hash = sha.ComputeHash(myByte);
            string Checksum = int.Parse((hash[0].ToString("X2") + hash[1].ToString("X2")), System.Globalization.NumberStyles.HexNumber).ToString().PadLeft(5, '0');
            return Checksum;
        }

        private Tlv0 ParseTlv0(string input)
        {
            Tlv0 result = new Tlv0();
            try
            {
                result.Type = input.Substring(0, SIZE_OF_TYPE);
                result.Length = int.Parse(input.Substring(SIZE_OF_TYPE, SIZE_OF_LENGTH));
                if (result.Length >= 5)
                {
                    result.Tlv0ValueDeviceType = input.Substring(4, 5);
                    if (result.Length >= 10)
                    {
                        result.Tlv0ValueInstallerIconType = input.Substring(9, 5);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ScanDSK corrupted ParseTlv0: ");
                Console.WriteLine(ex.Message.ToString());
            }
            return result;
        }
        private Tlv2 ParseTlv2(string input)
        {
            Tlv2 result = new Tlv2();
            try
            {
                result.Type = input.Substring(0, SIZE_OF_TYPE);
                result.Length = int.Parse(input.Substring(SIZE_OF_TYPE, SIZE_OF_LENGTH));
                if (result.Length >= 5)
                {
                    result.Tlv2ValueManufacturerId = input.Substring(4, 5);
                    if (result.Length >= 10)
                    {
                        result.Tlv2ValueProductType = input.Substring(9, 5);
                        if (result.Length >= 15)
                        {
                            result.Tlv2ValueProductId = input.Substring(14, 5);
                            if (result.Length >= 20)
                            {
                                result.Tlv2ValueApplicationVersion = input.Substring(19, 5);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ScanDSK corrupted ParseTlv2: ");
                Console.WriteLine(ex.Message.ToString());
            }
            return result;
        }
        private Tlv4 ParseTlv4(string input)
        {
            Tlv4 result = new Tlv4();
            try
            {
                result.Type = input.Substring(0, SIZE_OF_TYPE);
                result.Length = int.Parse(input.Substring(SIZE_OF_TYPE, SIZE_OF_LENGTH));
                if (input.Length > 4)
                {
                    result.Tlv4Value = input.Substring(4, result.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ScanDSK corrupted ParseTlv4: ");
                Console.WriteLine(ex.Message.ToString());
            }
            return result;
        }
        private Tlv6 ParseTlv6(string input)
        {
            Tlv6 result = new Tlv6();
            try
            {
                result.Type = input.Substring(0, SIZE_OF_TYPE);
                result.Length = int.Parse(input.Substring(SIZE_OF_TYPE, SIZE_OF_LENGTH));
                if (result.Length >= 2)
                {
                    result.Tlv6UUIDPressFormat = input.Substring(4, 2);
                    if (input.Length > 6)
                    {
                        result.Tlv6UUIDData = input.Substring(6, result.Length - 2);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ScanDSK corrupted ParseTlv6: ");
                Console.WriteLine(ex.Message.ToString());
            }
            return result;
        }
    }
}
