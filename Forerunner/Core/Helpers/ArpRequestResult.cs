/*
==============================================================================
Copyright � Jason Drawdy

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
using System.Net.NetworkInformation;
using System.Text;

#endregion Imports

namespace Forerunner {
    // TODO: rethink the whole exception thing

    /// <summary>
    /// Contains the return values of the ArpRequest.Send function.
    /// </summary>
    public class ArpRequestResult {

        /// <summary>If errors occur in the log request, they are stored in this property. Otherwise zero.</summary>
        public Exception Exception { get; }

        /// <summary>The resolved physical address.</summary>
        public PhysicalAddress Address { get; }

        /// <summary>Creates a new ArpRequestResult instance.</summary>
        /// <param name="address">The physical address.</param>
        public ArpRequestResult(PhysicalAddress address) {
            Exception = null;
            Address = address;
        }

        /// <summary>Creates a new ArpRequestResult instance.</summary>
        /// <param name="exception">The error which occurred.</param>
        public ArpRequestResult(Exception exception) {
            Exception = exception;
            Address = null;
        }

        /// <summary>Converts ARP return values to a string.</summary>
        public override string ToString() {
            var sb = new StringBuilder();
            if (Address == null)
                sb.Append("no address");
            else {
                sb.Append("address: ");
                sb.Append(Address);
            }
            sb.Append(", ");
            if (Exception == null)
                sb.Append("no exception");
            else {
                sb.Append("exception: ");
                sb.Append(Exception.Message);
            }
            return sb.ToString();
        }
    }
}