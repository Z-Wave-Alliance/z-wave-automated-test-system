// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Linq;
using System.Text;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("Use project args  -m o | a to reprog devices according to mapping and get dsk to temp.txt file on Desktop")]
    [DeviceMapping(1, LibTypes.Controller)]
    [DeviceMapping(2, LibTypes.EndDeviceAny)]
    //
    [TestInterfaceDeviceMapping(101, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(102, LibTypes.DoorLockKeyPad)]
    public class appbasic_ReprogAndGetDSK : ZatsCodeInteractiveTestCase
    {
        private string GetDecimal(byte[] array)
        {
            //Tools.GetDskStringFromBytes

            var sb = new StringBuilder();
            for (int i = 0; i < 16; i += 2)
            {
                if (i < array.Length)
                {
                    sb.Append(array.Skip(i).Take(2).ToArray().ToNumber().ToString("00000") + "-");
                }
                else
                {
                    sb.Append("00000");
                }
            }
            return sb.ToString();
        }

        protected override void ExecuteTest()
        {
            var tct = this.GetType();
            var ti_attributes = tct.GetCustomAttributes(typeof(TestInterfaceDeviceMappingAttribute), false);

            foreach (TestInterfaceDeviceMappingAttribute item in ti_attributes)
            {
                Alias alias = new Alias(item.Number);
                TestInterfaceService.GetLibrary(alias);
                var dsk = TestInterfaceService.GetDSK(alias, 16);
                ReportService.LogTitle($"{item.Number} DSK = {GetDecimal(dsk)}");
            }
            Alias controllerMain = new Alias(1);
            Alias[] all =
            {
                controllerMain,
            };


            //var sb = new StringBuilder();
            ////for comparison
            ////sb.AppendLine("Filepaths for any kind of lib in storage");
            ////foreach (LibTypes lt in Enum.GetValues(typeof(LibTypes)))
            ////{
            ////    sb.AppendLine(lt.ToString() + " USB path: " + ApplicationService.GetFile(lt, true));
            ////    sb.AppendLine(lt.ToString() + " UART path: " + ApplicationService.GetFile(lt, false));
            ////}

            ////sb.AppendLine("Deafault values (not used any more)");
            ////sb.AppendLine("Pr:  5DAB087E624A8A4B79E17F8B83800EE66F3BB1292618B6FD1C2F8B27FF88E0EB");
            ////sb.AppendLine("Pu:  DE9EDB7D7B7DC1B4D35B61C2ECE435373F8343C85B78674DADFC7E146F882B4F");
            ////sb.AppendLine("DSK:	56990-56189-31613-49588-54107-25026-60644-13623");

            ////sb.AppendLine("DSK for each connected device:");
            //foreach (var item in tis)
            //{
            //    //string line = string.Format("{0} {1}", item.Value, TestInterfaceService.GetLibrary(item).ToString());
            //    //sb.AppendLine(line);
            //    //var dsk = TestInterfaceService.GetDSK(item, 16);
            //    //sb.AppendLine(GetDecimal(dsk));
            //    TestInterfaceService.ExcludeNode(dut, item);
            //    TestInterfaceService.IncludeNode(dut, item);
            //}

            //Service.IncludeNode(dut, sec);
            ////var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\temp.txt";
            ////File.WriteAllText(path, sb.ToString());
        }

    }
}