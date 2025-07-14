// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using NUnit.Framework;
using System.Linq;
using ZatsTestConsole;

namespace ZatsServicesTests
{
    [TestFixture]
    public class CmdLineArgsTests
    {
        [Test]
        public void Find_INPUT_Exists()
        {
            var exp = CmdLineArgs.INPUT;
            foreach (var item in new[] { exp.ShortName, exp.LongName })
            {
                var cmdLineArg = CmdLineArgs.Find(item);
                Assert.AreNotEqual(CmdLineArgument.Empty, cmdLineArg);
                Assert.AreEqual(exp.ShortName, cmdLineArg.ShortName);
                Assert.AreEqual(exp.LongName, cmdLineArg.LongName);
            }
        }

        [Test]
        public void Find_Each_Exists()
        {
            var possibleArgs = new CmdLineArgs();
            var hardocedArgs = possibleArgs.GetType().GetFields();

            foreach (var item in hardocedArgs)
            {
                var expectedValue = item.GetValue(possibleArgs).ToString();
                var cmdLineArg = CmdLineArgs.Find(expectedValue);

                Assert.AreNotEqual(CmdLineArgument.Empty, cmdLineArg);
                Assert.AreEqual(expectedValue, cmdLineArg.ShortName);
            }
        }

        [Test]
        public void Find_NotExisted_NULL()
        {
            var notExistedArg1 = "notExistedArg";
            var notExistedArg2 = "-none";
            var notExistedArg3 = "--not-Existed-Arg";

            var actRes1 = CmdLineArgs.Find(notExistedArg1);
            var actRes2 = CmdLineArgs.Find(notExistedArg2);
            var actRes3 = CmdLineArgs.Find(notExistedArg3);

            Assert.AreEqual(CmdLineArgument.Empty, actRes1);
            Assert.AreEqual(CmdLineArgument.Empty, actRes2);
            Assert.AreEqual(CmdLineArgument.Empty, actRes3);
        }

        [Test]
        public void Find_EmptyAndNull_NULL()
        {
            var actRes1 = CmdLineArgs.Find(null);
            var actRes2 = CmdLineArgs.Find(string.Empty);

            Assert.AreEqual(CmdLineArgument.Empty, actRes1);
            Assert.AreEqual(CmdLineArgument.Empty, actRes2);
        }

        [Test]
        public void GetHelp_ANY_FullHelp()
        {
            var possibleArgs = new CmdLineArgs();
            var hardocedArgs = possibleArgs.GetType().GetFields();

            var actRes1 = CmdLineArgs.GetHelp();

            Assert.IsNotEmpty(actRes1);


            //PArt II verify each hardcoded value from CmdLineArgs:
            var helpLines = actRes1.Split();
            foreach (var item in hardocedArgs)
            {
                var expectedValue = item.GetValue(possibleArgs).ToString();

                Assert.Contains(expectedValue, helpLines);
            }
        }

        [Test]
        public void GetHelp_PROFILE_ExistsOnlyPROFILE()
        {
            var exp = CmdLineArgs.PROFILE;
            var possibleArgs = new CmdLineArgs();
            var hardocedArgs = possibleArgs.GetType().GetFields().Where(x => x.Name != "PROFILE").ToArray();

            var actual = CmdLineArgs.GetHelp(exp);

            Assert.AreNotEqual(CmdLineArgument.Empty, actual);

            var helpLines = actual.Split();
            foreach (var item in hardocedArgs)
            {
                var unExpectedValue = item.GetValue(possibleArgs).ToString();

                Assert.IsTrue(!helpLines.Any(x => x.Contains(unExpectedValue)));
            }
        }

        [Test]
        public void GetHelp_FullHelp()
        {
            var possibleArgs = new CmdLineArgs();
            var hardocedArgs = possibleArgs.GetType().GetFields();

            var actRes1 = CmdLineArgs.GetHelp();

            Assert.AreNotEqual(CmdLineArgument.Empty, actRes1);

            var helpLines = actRes1.Split();
            foreach (var item in hardocedArgs)
            {
                var expectedValue = item.GetValue(possibleArgs).ToString();

                Assert.Contains(expectedValue, helpLines);
            }
        }
    }
}
