// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>

namespace Zats.Services
{
    public static class Assert
    {
        /// <summary>
        ///     Asserts that a condition is true. If the condition is false the method 
        ///     stops the TC.
        /// </summary>
        /// <param name="condition">The evaluated condition</param>
        /// <param name="message">The message to display in case of failure</param>
        /// <param name="args">Array of objects to be used in formatting the message</param>
        public static void IsTrue(bool condition, string message, params object[] args)
        {
            if (!condition)
                AssertionException.Throw(message, args);
        }

        /// <summary>
        ///     Asserts that a condition is true. If the condition is false the method 
        ///     stops the TC.
        /// </summary>
        /// <param name="condition">The evaluated condition</param>
        /// <param name="message">The message to display in case of failure</param>
        public static void IsTrue(bool condition, string message)
        {
            IsTrue(condition, message, null);
        }

        /// <summary>
        ///     Asserts that a condition is true. If the condition is false the method 
        ///     stops the TC.
        /// </summary>
        /// <param name="condition">The evaluated condition</param>
        public static void IsTrue(bool condition)
        {
            IsTrue(condition, "", null);
        }

        /// <summary>
        ///     Asserts that a condition is false. If the condition is true the method 
        ///     stops the TC.
        /// </summary>
        /// <param name="condition">The evaluated condition</param>
        /// <param name="message">The message to display in case of failure</param>
        /// <param name="args">Array of objects to be used in formatting the message</param>
        public static void IsFalse(bool condition, string message, params object[] args)
        {
            if (condition)
                AssertionException.Throw(message, args);
        }

        /// <summary>
        ///     Asserts that a condition is false. If the condition is true the method 
        ///     stops the TC.
        /// </summary>
        /// <param name="condition">The evaluated condition</param>
        /// <param name="message">The message to display in case of failure</param>
        public static void IsFalse(bool condition, string message)
        {
            IsFalse(condition, message, null);
        }

        /// <summary>
        ///     Asserts that a condition is false. If the condition is true the method 
        ///     stops the TC.
        /// </summary>
        /// <param name="condition">The evaluated condition</param>
        public static void IsFalse(bool condition)
        {
            IsFalse(condition, "", null);
        }

        /// <summary>
        ///     Verifies that two decimals are equal. If they are not, then 
        ///     the TC stops.
        /// </summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The actual value</param>
        /// <param name="message">The message to display in case of failure</param>
        /// <param name="args">Array of objects to be used in formatting the message</param>
        public static void AreEqual(decimal expected, decimal actual, string message, params object[] args)
        {
            if (actual != expected)
                AssertionException.Throw(string.Format("expected: '{0}', but received '{1}'; {2}", expected, actual, message), args);
        }

        /// <summary>
        ///     Verifies that two decimals are equal. If they are not, then 
        ///     the TC stops.
        /// </summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The actual value</param>
        /// <param name="message">The message to display in case of failure</param>
        public static void AreEqual(decimal expected, decimal actual, string message)
        {
            AreEqual(expected, actual, message, null);
        }

        /// <summary>
        ///     Verifies that two decimals are equal. If they are not, then 
        ///     the TC stops.
        /// </summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The actual value</param>
        public static void AreEqual(decimal expected, decimal actual)
        {
            AreEqual(expected, actual, "", null);
        }

        /// <summary>
        ///     Verifies that two ints are equal. If they are not, then 
        ///     the TC stops.
        /// </summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The actual value</param>
        /// <param name="message">The message to display in case of failure</param>
        /// <param name="args">Array of objects to be used in formatting the message</param>
        public static void AreEqual(int expected, int actual, string message, params object[] args)
        {
            if (actual != expected)
                AssertionException.Throw(string.Format("expected: '{0}', but received '{1}'; {2}", expected, actual, message), args);
        }

        /// <summary>
        ///     Verifies that two ints are equal. If they are not, then 
        ///     the TC stops.
        /// </summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The actual value</param>
        /// <param name="message">The message to display in case of failure</param>
        public static void AreEqual(int expected, int actual, string message)
        {
            AreEqual(expected, actual, message, null);
        }

        /// <summary>
        ///     Verifies that two ints are equal. If they are not, then 
        ///     the TC stops.
        /// </summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The actual value</param>
        public static void AreEqual(int expected, int actual)
        {
            AreEqual(expected, actual, "", null);
        }

        /// <summary>
        ///     Verifies that two longs are equal. If they are not, then 
        ///     the TC stops.
        /// </summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The actual value</param>
        /// <param name="message">The message to display in case of failure</param>
        /// <param name="args">Array of objects to be used in formatting the message</param>
        public static void AreEqual(long expected, long actual, string message, params object[] args)
        {
            if (actual != expected)
                AssertionException.Throw(string.Format("expected: '{0}', but received '{1}'; {2}", expected, actual, message), args);
        }

        /// <summary>
        ///     Verifies that two longs are equal. If they are not, then 
        ///     the TC stops.
        /// </summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The actual value</param>
        /// <param name="message">The message to display in case of failure</param>
        public static void AreEqual(long expected, long actual, string message)
        {
            AreEqual(expected, actual, message, null);
        }

        /// <summary>
        ///     Verifies that two longs are equal. If they are not, then 
        ///     the TC stops.
        /// </summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The actual value</param>
        public static void AreEqual(long expected, long actual)
        {
            AreEqual(expected, actual, "", null);
        }

        /// <summary>
        ///     Verifies that two objects are equal. Two objects are considered equal if
        ///     both are null, or if both have the same value. If they are not equal 
        ///     the TC stops.
        /// </summary>
        /// <param name="expected">The value that is expected</param>
        /// <param name="actual">The actual value</param>
        /// <param name="message">The message to display in case of failure</param>
        /// <param name="args">Array of objects to be used in formatting the message</param>
        public static void AreEqual(object expected, object actual, string message, params object[] args)
        {
            if (actual == null && expected != null ||
                actual != null && expected == null ||
                actual != null && expected != null && !actual.Equals(expected))
                AssertionException.Throw(string.Format("expected: '{0}', but received '{1}'; {2}", expected, actual, message), args);
        }

        /// <summary>
        ///     Verifies that two objects are equal. Two objects are considered equal if
        ///     both are null, or if both have the same value. If they are not equal 
        ///     the TC stops.
        /// </summary>
        /// <param name="expected">The value that is expected</param>
        /// <param name="actual">The actual value</param>
        /// <param name="message">The message to display in case of failure</param>
        public static void AreEqual(object expected, object actual, string message)
        {
            AreEqual(expected, actual, message, null);
        }

        /// <summary>
        ///     Verifies that two objects are equal. Two objects are considered equal if
        ///     both are null, or if both have the same value. If they are not equal 
        ///     the TC stops.
        /// </summary>
        /// <param name="expected">The value that is expected</param>
        /// <param name="actual">The actual value</param>
        public static void AreEqual(object expected, object actual)
        {
            AreEqual(expected, actual, "", null);
        }

        /// <summary>
        ///     Verifies that two unsigned ints are equal. If they are not, then 
        ///     the TC stops.
        /// </summary>
        /// <param name="expected"> The expected value</param>
        /// <param name="actual">The actual value</param>
        /// <param name="message">The message to display in case of failure</param>
        /// <param name="args">Array of objects to be used in formatting the message</param>
        public static void AreEqual(uint expected, uint actual, string message, params object[] args)
        {
            if (actual != expected)
                AssertionException.Throw(string.Format("expected: '{0}', but received '{1}'; {2}", expected, actual, message), args);
        }

        /// <summary>
        ///     Verifies that two unsigned ints are equal. If they are not, then 
        ///     the TC stops.
        /// </summary>
        /// <param name="expected"> The expected value</param>
        /// <param name="actual">The actual value</param>
        /// <param name="message">The message to display in case of failure</param>
        public static void AreEqual(uint expected, uint actual, string message)
        {
            AreEqual(expected, actual, message, null);
        }

        /// <summary>
        ///     Verifies that two unsigned ints are equal. If they are not, then 
        ///     the TC stops.
        /// </summary>
        /// <param name="expected"> The expected value</param>
        /// <param name="actual">The actual value</param>
        public static void AreEqual(uint expected, uint actual)
        {
            AreEqual(expected, actual, "", null);
        }

        /// <summary>
        ///     Verifies that two unsigned longs are equal. If they are not, then 
        ///     the TC stops.
        /// </summary>
        /// <param name="expected"> The expected value</param>
        /// <param name="actual">The actual value</param>
        /// <param name="message">The message to display in case of failure</param>
        /// <param name="args">Array of objects to be used in formatting the message</param>
        public static void AreEqual(ulong expected, ulong actual, string message, params object[] args)
        {
            if (actual != expected)
                AssertionException.Throw(string.Format("expected: '{0}', but received '{1}'; {2}", expected, actual, message), args);
        }

        /// <summary>
        ///     Verifies that two unsigned longs are equal. If they are not, then 
        ///     the TC stops.
        /// </summary>
        /// <param name="expected"> The expected value</param>
        /// <param name="actual">The actual value</param>
        /// <param name="message">The message to display in case of failure</param>
        public static void AreEqual(ulong expected, ulong actual, string message)
        {
            AreEqual(expected, actual, message, null);
        }

        /// <summary>
        ///     Verifies that two unsigned longs are equal. If they are not, then 
        ///     the TC stops.
        /// </summary>
        /// <param name="expected"> The expected value</param>
        /// <param name="actual">The actual value</param>
        public static void AreEqual(ulong expected, ulong actual)
        {
            AreEqual(expected, actual, "", null);
        }
    }
}
