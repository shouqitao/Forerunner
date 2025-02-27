﻿/*
==============================================================================
Copyright © Jason Drawdy

All rights reserved.

The MIT License (MIT)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

Except as contained in this notice, the name of the above copyright holder
shall not be used in advertising or otherwise to promote the sale, use or
other dealings in this Software without prior written authorization.
==============================================================================
*/

#region Imports

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

#endregion Imports

namespace Forerunner {

    [Serializable]
    public class IPAddressRange : ISerializable, IEnumerable<IPAddress> {

        #region Variables

        // Pattern 1. CIDR range: "192.168.0.0/24", "fe80::/10"
        private static Regex m1_regex = new Regex(@"^(?<adr>[\da-f\.:]+)/(?<maskLen>\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Pattern 2. Uni address: "127.0.0.1", ":;1"
        private static Regex m2_regex = new Regex(@"^(?<adr>[\da-f\.:]+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Pattern 3. Begin end range: "169.258.0.0-169.258.0.255"
        private static Regex m3_regex = new Regex(@"^(?<begin>[\da-f\.:]+)[\-–](?<end>[\da-f\.:]+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Pattern 4. Bit mask range: "192.168.0.0/255.255.255.0"
        private static Regex m4_regex = new Regex(@"^(?<adr>[\da-f\.:]+)/(?<bitmask>[\da-f\.:]+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// The beginning IP address a scan will start from.
        /// </summary>
        public IPAddress Begin { get; set; }

        /// <summary>
        /// The ending IP address a scan will stop with.
        /// </summary>
        public IPAddress End { get; set; }

        #endregion Variables

        #region Initialization

        /// <summary>
        /// Creates an empty range object, equivalent to "0.0.0.0/0".
        /// </summary>
        public IPAddressRange() : this(new IPAddress(0L)) { }

        /// <summary>
        /// Creates a new range with the same start/end address (range of one).
        /// </summary>
        /// <param name="singleAddress"></param>
        public IPAddressRange(IPAddress singleAddress) {
            if (singleAddress == null)
                throw new ArgumentNullException(nameof(singleAddress));

            Begin = End = singleAddress;
        }

        /// <summary>
        /// Create a new range from a begin and end address.
        /// Throws an exception if Begin comes after End, or the
        /// addresses are not in the same family.
        /// </summary>
        public IPAddressRange(IPAddress begin, IPAddress end) {
            if (begin == null)
                throw new ArgumentNullException(nameof(begin));

            if (end == null)
                throw new ArgumentNullException(nameof(end));

            Begin = begin;
            End = end;

            if (Begin.AddressFamily != End.AddressFamily)
                throw new ArgumentException("Elements must be of the same address family", nameof(end));

            var beginBytes = Begin.GetAddressBytes();
            var endBytes = End.GetAddressBytes();
            if (!Bits.LE(endBytes, beginBytes))
                throw new ArgumentException("Begin must be smaller than the End", nameof(begin));
        }

        /// <summary>
        /// Creates a range from a base address and mask bits.
        /// This can also be used with <see cref="SubnetMaskLength"/> to create a
        /// range based on a subnet mask.
        /// </summary>
        /// <param name="baseAddress"></param>
        /// <param name="maskLength"></param>
        public IPAddressRange(IPAddress baseAddress, int maskLength) {
            if (baseAddress == null)
                throw new ArgumentNullException(nameof(baseAddress));

            var baseAdrBytes = baseAddress.GetAddressBytes();
            if (baseAdrBytes.Length * 8 < maskLength)
                throw new FormatException();
            var maskBytes = Bits.GetBitMask(baseAdrBytes.Length, maskLength);
            baseAdrBytes = Bits.And(baseAdrBytes, maskBytes);

            Begin = new IPAddress(baseAdrBytes);
            End = new IPAddress(Bits.Or(baseAdrBytes, Bits.Not(maskBytes)));
        }

        [Obsolete("Use the IPAddressRange.Parse static method instead.")]
        public IPAddressRange(string ipRangeString) {
            var parsed = Parse(ipRangeString);
            Begin = parsed.Begin;
            End = parsed.End;
        }

        protected IPAddressRange(SerializationInfo info, StreamingContext context) {
            var names = new List<string>();
            foreach (var item in info)
                names.Add(item.Name);

            Func<string, IPAddress> deserialize = (name) => names.Contains(name) ?
                 IPAddress.Parse(info.GetValue(name, typeof(object)).ToString()) :
                 new IPAddress(0L);

            this.Begin = deserialize("Begin");
            this.End = deserialize("End");
        }

        #endregion Initialization

        #region Methods

        /// <summary>
        /// Checks if an IP address is already contained within the range.
        /// </summary>
        /// <param name="ipaddress">The IP address to check.</param>
        public bool Contains(IPAddress ipaddress) {
            if (ipaddress == null)
                throw new ArgumentNullException(nameof(ipaddress));

            if (ipaddress.AddressFamily != this.Begin.AddressFamily)
                return false;
            var adrBytes = ipaddress.GetAddressBytes();
            return Bits.GE(this.Begin.GetAddressBytes(), adrBytes) && Bits.LE(this.End.GetAddressBytes(), adrBytes);
        }

        /// <summary>
        /// Checks if an IP address range is already set.
        /// </summary>
        /// <param name="range">The IP address range to check for.</param>
        public bool Contains(IPAddressRange range) {
            if (range == null)
                throw new ArgumentNullException(nameof(range));

            if (this.Begin.AddressFamily != range.Begin.AddressFamily)
                return false;

            return
                Bits.GE(this.Begin.GetAddressBytes(), range.Begin.GetAddressBytes()) &&
                Bits.LE(this.End.GetAddressBytes(), range.End.GetAddressBytes());

            throw new NotImplementedException();
        }

        /// <summary>
        /// Obtains serialization info about an <see cref="IPAddressRange"/> object.
        /// </summary>
        /// <param name="info">The info to get data for.</param>
        /// <param name="context"></param>
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            info.AddValue("Begin", this.Begin != null ? this.Begin.ToString() : "");
            info.AddValue("End", this.End != null ? this.End.ToString() : "");
        }

        /// <summary>
        /// Parses an IP address range string into an <see cref="IPAddressRange"/> object.
        /// </summary>
        /// <param name="ipRangeString">The IP range string to parse.</param>
        public static IPAddressRange Parse(string ipRangeString) {
            if (ipRangeString == null)
                throw new ArgumentNullException(nameof(ipRangeString));

            // remove all spaces.
            ipRangeString = ipRangeString.Replace(" ", String.Empty);

            // Pattern 1. CIDR range: "192.168.0.0/24", "fe80::/10"
            var m1 = m1_regex.Match(ipRangeString);
            if (m1.Success) {
                var baseAdrBytes = IPAddress.Parse(m1.Groups["adr"].Value).GetAddressBytes();
                var maskLen = int.Parse(m1.Groups["maskLen"].Value);
                if (baseAdrBytes.Length * 8 < maskLen)
                    throw new FormatException();
                var maskBytes = Bits.GetBitMask(baseAdrBytes.Length, maskLen);
                baseAdrBytes = Bits.And(baseAdrBytes, maskBytes);
                return new IPAddressRange(new IPAddress(baseAdrBytes), new IPAddress(Bits.Or(baseAdrBytes, Bits.Not(maskBytes))));
            }

            // Pattern 2. Uni address: "127.0.0.1", ":;1"
            var m2 = m2_regex.Match(ipRangeString);
            if (m2.Success) {
                return new IPAddressRange(IPAddress.Parse(ipRangeString));
            }

            // Pattern 3. Begin end range: "169.258.0.0-169.258.0.255"
            var m3 = m3_regex.Match(ipRangeString);
            if (m3.Success) {
                return new IPAddressRange(IPAddress.Parse(m3.Groups["begin"].Value), IPAddress.Parse(m3.Groups["end"].Value));
            }

            // Pattern 4. Bit mask range: "192.168.0.0/255.255.255.0"
            var m4 = m4_regex.Match(ipRangeString);
            if (m4.Success) {
                var baseAdrBytes = IPAddress.Parse(m4.Groups["adr"].Value).GetAddressBytes();
                var maskBytes = IPAddress.Parse(m4.Groups["bitmask"].Value).GetAddressBytes();
                baseAdrBytes = Bits.And(baseAdrBytes, maskBytes);
                return new IPAddressRange(new IPAddress(baseAdrBytes), new IPAddress(Bits.Or(baseAdrBytes, Bits.Not(maskBytes))));
            }

            throw new FormatException("Unknown IP range string.");
        }

        /// <summary>
        /// Parses an IP range string into an <see cref="IPAddressRange"/> object.
        /// </summary>
        /// <param name="ipRangeString">The range string to parse.</param>
        /// <param name="ipRange">The <see cref="IPAddressRange"/> object to parse to.</param>
        /// <returns>A flag determining whether or not the operation was successful.</returns>
        public static bool TryParse(string ipRangeString, out IPAddressRange ipRange) {
            try {
                ipRange = IPAddressRange.Parse(ipRangeString);
                return true;
            } catch (Exception) {
                ipRange = null;
                return false;
            }
        }

        /// <summary>
        /// Takes a subnetmask (eg, "255.255.254.0") and returns the CIDR bit length of that
        /// address. Throws an exception if the passed address is not valid as a subnet mask.
        /// </summary>
        /// <param name="subnetMask">The subnet mask to use</param>
        /// <returns></returns>
        public static int SubnetMaskLength(IPAddress subnetMask) {
            if (subnetMask == null)
                throw new ArgumentNullException(nameof(subnetMask));

            var length = Bits.GetBitMaskLength(subnetMask.GetAddressBytes());
            if (length == null)
                throw new ArgumentException("Not a valid subnet mask", "subnetMask");
            return length.Value;
        }

        public IEnumerator<IPAddress> GetEnumerator() {
            var first = Begin.GetAddressBytes();
            var last = End.GetAddressBytes();
            for (var ip = first; Bits.GE(ip, last); ip = Bits.Increment(ip))
                yield return new IPAddress(ip);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns the range in the format "begin-end", or
        /// as a single address if End is the same as Begin.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return Equals(Begin, End) ? Begin.ToString() : string.Format("{0}-{1}", Begin, End);
        }

        public int GetPrefixLength() {
            byte[] byteBegin = Begin.GetAddressBytes();
            byte[] byteEnd = End.GetAddressBytes();

            // Handle single IP
            if (Begin.Equals(End)) {
                return byteBegin.Length * 8;
            }

            int length = byteBegin.Length * 8;

            for (int i = 0; i < length; i++) {
                byte[] mask = Bits.GetBitMask(byteBegin.Length, i);
                if (new IPAddress(Bits.And(byteBegin, mask)).Equals(Begin)) {
                    if (new IPAddress(Bits.Or(byteBegin, Bits.Not(mask))).Equals(End)) {
                        return i;
                    }
                }
            }
            throw new FormatException(string.Format("{0} is not a CIDR Subnet", ToString()));
        }

        /// <summary>
        /// Returns a Cidr String if this matches exactly a Cidr subnet.
        /// </summary>
        public string ToCidrString() {
            return string.Format("{0}/{1}", Begin, GetPrefixLength());
        }

        #endregion Methods
    }
}