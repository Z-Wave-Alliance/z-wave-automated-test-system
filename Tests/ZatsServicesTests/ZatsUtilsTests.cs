// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using NUnit.Framework;
using ZatsTestConsole;

namespace ZatsServicesTests
{
    [TestFixture]
    public class ZatsUtilsTests
    {
        [Test]
        public void ParseArguments_EmtpyArguments_FullHelp()
        {
            string[] testArgs = new string[0];
            var actual = ZatsUtils.ParseArguments(testArgs);

            Assert.IsTrue(actual.HasHelp);
            Assert.IsNotEmpty(actual.Help);
            Assert.IsTrue(actual.HasParseError);
            Assert.IsNotEmpty(actual.ParseErrorMessage);
        }

        [Test]
        public void ParseArguments_HELP_FullHelp()
        {
            string[] testArgs1 = new string[] { CmdLineArgs.HELP.ShortName };
            string[] testArgs2 = new string[] { CmdLineArgs.HELP.LongName };

            var actual1 = ZatsUtils.ParseArguments(testArgs1);
            var actual2 = ZatsUtils.ParseArguments(testArgs2);

            Assert.IsTrue(actual1.HasHelp);
            Assert.IsNotEmpty(actual1.Help);
            Assert.IsTrue(actual2.HasHelp);
            Assert.IsNotEmpty(actual2.Help);

            Assert.AreEqual(actual1.Help, actual2.Help);
        }


        [Test]
        public void ParseArguments_JLINK_DEVICES_FILTER_InputFilterHelp()
        {
            string[] testArgs1 = new string[] { CmdLineArgs.HELP.ShortName, "jlf" };
            string[] testArgs2 = new string[] { CmdLineArgs.HELP.LongName, "jlink-filter" };

            var actual1 = ZatsUtils.ParseArguments(testArgs1);
            var actual2 = ZatsUtils.ParseArguments(testArgs2);

            Assert.IsTrue(actual1.HasHelp);
            Assert.IsNotEmpty(actual1.Help);
            Assert.IsTrue(actual2.HasHelp);
            Assert.IsNotEmpty(actual2.Help);

            Assert.AreEqual(actual1.Help, actual2.Help);
        }

        [Test]
        public void ParseArguments_ANY_PATH_AllPathOccurrenceONLYHelp()
        {
            var unExpected = "frequency";
            string[] testArgs = new string[] { "--help", "path" };

            var actual = ZatsUtils.ParseArguments(testArgs);

            Assert.IsTrue(actual.HasHelp);
            Assert.IsNotEmpty(actual.Help);
            Assert.IsFalse(actual.Help.Contains(unExpected));
        }

        [Test]
        public void ParseArguments_INPUTwithParam_NoHelp() // but AssemblyFile
        {
            var exp = "test.dll";
            string[] testArgs = new string[] { CmdLineArgs.HELP.ShortName, CmdLineArgs.INPUT.ShortName, "test.dll" };

            var actual1 = ZatsUtils.ParseArguments(testArgs);

            Assert.IsTrue(actual1.HasHelp);
            Assert.IsNotEmpty(actual1.Help);

            Assert.IsNotEmpty(actual1.AssemblyFile);
            Assert.AreEqual(exp, actual1.AssemblyFile);
        }


        [Test]
        public void ParseArguments_WrongREGION_HasParseError()
        {
            string[] testArgs = new string[] { CmdLineArgs.FREQUENCY.ShortName, "null" };
            
            var actual = ZatsUtils.ParseArguments(testArgs);

            Assert.IsTrue(actual.HasParseError);
            Assert.IsNotEmpty(actual.ParseErrorMessage);
        }


        [TestCase("US")]
        [TestCase("us")] // not case sensitive
        [TestCase("US_LR")]
        public void ParseArguments_CorrectREGION_NoError(string param)
        {
            var expected = param;
            string[] testArgs = new string[] { CmdLineArgs.FREQUENCY.ShortName, expected };

            var actual = ZatsUtils.ParseArguments(testArgs);

            Assert.IsFalse(actual.HasParseError);
            Assert.IsNull(actual.ParseErrorMessage);
            Assert.Contains(expected, actual.Frequencies);
        }

        [Test]
        public void ParseArguments_NoParameterREGION_HasParseError()
        {
            string[] testArgs = new string[] { CmdLineArgs.INPUT.ShortName, "someInputPath" }; // not specified -f at all.

            var actual = ZatsUtils.ParseArguments(testArgs);

            Assert.IsTrue(actual.HasParseError);
            Assert.IsNotEmpty(actual.ParseErrorMessage);
        }

        [Test]
        public void ParseArguments_MissingArgumentREGION_HasParseError()
        {
            string[] testArgs = new string[] { CmdLineArgs.FREQUENCY.ShortName, "", CmdLineArgs.INPUT.ShortName };

            var actual = ZatsUtils.ParseArguments(testArgs);

            Assert.IsTrue(actual.HasParseError);
            Assert.IsNotEmpty(actual.ParseErrorMessage);
        }

        [Test]
        public void ParseArguments_EmptyCOMMANDER_APP_PATH_HasParseError()
        {
            string[] testArgs = new string[] { CmdLineArgs.COMMANDER_APP_PATH.ShortName, "" };

            var actual = ZatsUtils.ParseArguments(testArgs);

            Assert.IsTrue(actual.HasParseError);
            Assert.IsNotEmpty(actual.ParseErrorMessage);
            Assert.IsNull(actual.CommanderAppPath);
        }
        
    }
}
