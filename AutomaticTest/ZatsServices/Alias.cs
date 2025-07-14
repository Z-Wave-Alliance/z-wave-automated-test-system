// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections;
using System.Collections.Generic;
using Utils;

namespace Zats.Services
{
    public struct Alias : IEquatable<Alias>, IComparable<Alias>, IComparable
    {
        public ushort Value { get; private set; }
        public byte Group { get; private set; }
        public byte EndPointId { get; private set; }
        public bool IsBitAddress { get; private set; }
        public string Text { get; }

        public Alias(ushort value)
            : this(0, value, 0, false, null)
        { }

        public Alias(ushort value, string text)
            : this(0, value, 0, false, text)
        { }

        public Alias(byte group, ushort value)
           : this(group, value, 0, false, null)
        { }

        private Alias(byte group, ushort value, byte endPointId, bool isBitAddress, string text)
        {
            Group = group;
            Value = value;
            EndPointId = endPointId;
            Text = text;
            IsBitAddress = isBitAddress;
        }

        public Alias EndPoint(byte endPointId)
        {
            return new Alias(0, Value, endPointId, false, Text);
        }

        public Alias EndPoint(byte endPointId, bool isBitAddress)
        {
            return new Alias(0, Value, endPointId, isBitAddress, Text);
        }

        public override int GetHashCode()
        {
            return (Group << 24) + (Value << 8) + EndPointId;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Alias))
                return false;

            Alias alias = (Alias)obj;
            return Equals(alias);
        }

        public bool Equals(Alias other)
        {
            return Group == other.Group && Value == other.Value && EndPointId == other.EndPointId;
        }

        public static bool operator !=(Alias alias1, Alias alias2)
        {
            return !alias1.Equals(alias2);
        }

        public static bool operator ==(Alias alias1, Alias alias2)
        {
            return alias1.Equals(alias2);
        }

        public int CompareTo(Alias other)
        {
            if (Group < other.Group)
                return -1;
            else if (Group > other.Group)
                return 1;
            else if (Value < other.Value)
                return -1;
            else if (Value > other.Value)
                return 1;
            else if (EndPointId < other.EndPointId)
                return -1;
            else if (EndPointId > other.EndPointId)
                return 1;
            else
                return 0;
        }

        public int CompareTo(object obj)
        {
            if (!(obj is Alias))
                return 0;

            Alias alias = (Alias)obj;
            return CompareTo(alias);
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Text))
            {
                if (Group > 0)
                {
                    return $"{Group}.{Value}";
                }
                else
                {
                    return Value.ToString();
                }
            }
            else
            {
                if (Group > 0)
                {
                    return Tools.FormatStr($"{Group}.{Value} ({Text})");
                }
                else
                {
                    return Tools.FormatStr($"{Value} ({Text})");
                }
            }
        }

        public static implicit operator string(Alias alias)
        {
            return alias.ToString();
        }
    }

    public class AliasGroup : IEnumerable<Alias>
    {
        public byte Key { get; private set; }
        public string Text { get; }
        public IEnumerable<Alias> Aliases { get; }
        public AliasGroup(byte value, Alias[] aliases)
           : this(value, null, aliases)
        { }

        private AliasGroup(byte key, string text, IEnumerable<Alias> aliases)
        {
            Key = key;
            Text = text;
            Aliases = aliases;
        }

        public static implicit operator string(AliasGroup aliasGroup)
        {
            return aliasGroup.ToString();
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Text))
                return Key.ToString();
            else
                return Tools.FormatStr(" [module{0}: {1}] ", Key, Text);
        }

        public IEnumerator<Alias> GetEnumerator()
        {
            return Aliases.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
