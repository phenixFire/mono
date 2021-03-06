//
// System.Globalization.TextInfo.cs
//
// Authors:
//	Dick Porter (dick@ximian.com)
// 	Duncan Mak (duncan@ximian.com)
//	Atsushi Enomoto (atsushi@ximian.com)
//	Sebastien Pouliot  <sebastien@ximian.com>
//
// (C) 2002 Ximian, Inc.
// (C) 2005 Novell, Inc.
//
// TODO:
//   Missing the various code page mappings.
//   Missing the OnDeserialization implementation.
//
// Copyright (C) 2004, 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics.Contracts;

namespace System.Globalization {

	[Serializable]
	[ComVisible (true)]
	[MonoTODO ("IDeserializationCallback isn't implemented.")]
	public class TextInfo: IDeserializationCallback, ICloneable
	{
		static TextInfo ()
		{
			unsafe {
				GetDataTablePointersLite (out to_lower_data_low, out to_lower_data_high, out to_upper_data_low, out to_upper_data_high);
			}
		}
		
		private readonly unsafe static ushort *to_lower_data_low;
		private readonly unsafe static ushort *to_lower_data_high;
		private readonly unsafe static ushort *to_upper_data_low;
		private readonly unsafe static ushort *to_upper_data_high;
		[MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
		private unsafe static extern void GetDataTablePointersLite (out ushort *to_lower_data_low, out ushort *to_lower_data_high,
			out ushort *to_upper_data_low, out ushort *to_upper_data_high);

		static char ToLowerInvariant (char c)
		{
			unsafe {
				if (c <= ((char)0x24cf))
					return (char) to_lower_data_low [c];
				if (c >= ((char)0xff21))
					return (char) to_lower_data_high[c - 0xff21];
			}
			return c;
		}

		static char ToUpperInvariant (char c)
		{
			unsafe {
				if (c <= ((char)0x24e9))
					return (char) to_upper_data_low [c];
				if (c >= ((char)0xff21))
					return (char) to_upper_data_high [c - 0xff21];
			}
			return c;
		}
		
		[StructLayout (LayoutKind.Sequential)]
		struct Data {
			public int ansi;
			public int ebcdic;
			public int mac;
			public int oem;
			public bool right_to_left;
			public byte list_sep;
		}

		string m_listSeparator;
		bool m_isReadOnly;
		string customCultureName;

#pragma warning disable 169
		[NonSerialized]
		int m_nDataItem;
		bool m_useUserOverride;
#pragma warning restore 169		

		int m_win32LangID;

		[NonSerialized]
		readonly CultureInfo ci;

		[NonSerialized]
		readonly bool handleDotI;

		[NonSerialized]
		readonly Data data;

		internal unsafe TextInfo (CultureInfo ci, int lcid, void* data, bool read_only)
		{
			this.m_isReadOnly = read_only;
			this.m_win32LangID = lcid;
			this.ci = ci;
			if (data != null)
				this.data = *(Data*) data;
			else {
				this.data = new Data ();
				this.data.list_sep = (byte) ',';
			}

			CultureInfo tmp = ci;
			while (tmp.Parent != null && tmp.Parent.LCID != 0x7F && tmp.Parent != tmp)
				tmp = tmp.Parent;

			if (tmp != null) {
				switch (tmp.LCID) {
				case 44: // Azeri (az)
				case 31: // Turkish (tr)
					handleDotI = true;
					break;
				}
			}
		}

		private TextInfo (TextInfo textInfo)
		{
			m_win32LangID = textInfo.m_win32LangID;
			m_nDataItem = textInfo.m_nDataItem;
			m_useUserOverride = textInfo.m_useUserOverride;
			m_listSeparator = textInfo.ListSeparator;
			customCultureName = textInfo.CultureName;
			ci = textInfo.ci;
			handleDotI = textInfo.handleDotI;
			data = textInfo.data;
		}

		public virtual int ANSICodePage
		{
			get {
				return data.ansi;
			}
		}

		public virtual int EBCDICCodePage
		{
			get {
				return data.ebcdic;
			}
		}

		[ComVisible (false)]
		public int LCID {
			get { return m_win32LangID; }
		}

		public virtual string ListSeparator {
			get {
				if (m_listSeparator == null)
					m_listSeparator = ((char) data.list_sep).ToString ();
				return m_listSeparator;
			}
			[ComVisible (false)]
			set { m_listSeparator = value; }
		}

		public virtual int MacCodePage
		{
			get {
				return data.mac;
			}
		}

		public virtual int OEMCodePage
		{
			get {
				return data.oem;
			}
		}

		[ComVisible (false)]
		public string CultureName {
			get {
				if (customCultureName == null)
					customCultureName = ci == null ? String.Empty : ci.Name;
				return customCultureName;
			}
		}

		[ComVisible (false)]
		public bool IsReadOnly {
			get { return m_isReadOnly; }
		}

		[ComVisible (false)]
		public bool IsRightToLeft {
			get {
				return data.right_to_left;
			}
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;
			TextInfo other = obj as TextInfo;
			if (other == null)
				return false;
			if (other.m_win32LangID != m_win32LangID)
				return false;
			if (other.ci != ci)
				return false;
			return true;
		}

		public override int GetHashCode()
		{
			return (m_win32LangID);
		}
		
		public override string ToString()
		{
			return "TextInfo - " + m_win32LangID;
		}

		public string ToTitleCase (string str)
		{
			if(str == null)
				throw new ArgumentNullException ("str");

			StringBuilder sb = null;
			int i = 0;
			int start = 0;
			while (i < str.Length) {
				if (!Char.IsLetter (str [i++]))
					continue;
				i--;
				char t = ToTitleCase (str [i]);
				bool capitalize = true;
				if (t == str [i]) {
					capitalize = false;
					bool allTitle = true;
					// if the word is all titlecase,
					// then don't capitalize it.
					int saved = i;
					while (++i < str.Length) {
						var ch = str [i];
						var category = char.GetUnicodeCategory (ch);
						if (IsSeparator (category))
							break;
						t = ToTitleCase (ch);
						if (t != ch) {
							allTitle = false;
							break;
						}
					}
					if (allTitle)
						continue;
					i = saved;

					// still check if all remaining
					// characters are lowercase,
					// where we don't have to modify
					// the source word.
					while (++i < str.Length) {
						var ch = str [i];
						var category = char.GetUnicodeCategory (ch);
						if (IsSeparator (category))
							break;
						if (ToLower (ch) != ch) {
							capitalize = true;
							i = saved;
							break;
						}
					}
				}

				if (capitalize) {
					if (sb == null)
						sb = new StringBuilder (str.Length);
					sb.Append (str, start, i - start);
					sb.Append (ToTitleCase (str [i]));
					start = i + 1;
					while (++i < str.Length) {
						var ch = str [i];
						var category = char.GetUnicodeCategory (ch);
						if (IsSeparator (category))
							break;
						sb.Append (ToLower (ch));
					}
					start = i;
				}
			}
			if (sb != null)
				sb.Append (str, start, str.Length - start);

			return sb != null ? sb.ToString () : str;
		}

		static bool IsSeparator (UnicodeCategory category)
		{
			switch (category) {
			case UnicodeCategory.SpaceSeparator:
			case UnicodeCategory.LineSeparator:
			case UnicodeCategory.ParagraphSeparator:
			case UnicodeCategory.Control:
			case UnicodeCategory.Format:
			case UnicodeCategory.ConnectorPunctuation:
			case UnicodeCategory.DashPunctuation:
			case UnicodeCategory.OpenPunctuation:
			case UnicodeCategory.ClosePunctuation:
			case UnicodeCategory.InitialQuotePunctuation:
			case UnicodeCategory.FinalQuotePunctuation:
			case UnicodeCategory.OtherPunctuation:
				return true;
			}

			return false;
		}

		// Only Azeri and Turkish have their own special cases.
		// Other than them, all languages have common special case
		// (enumerable enough).
		public virtual char ToLower (char c)
		{
			// quick ASCII range check
			if (c < 0x40 || 0x60 < c && c < 128)
				return c;
			else if ('A' <= c && c <= 'Z' && (!handleDotI || c != 'I'))
				return (char) (c + 0x20);

			if (ci == null || ci.LCID == 0x7F)
				return ToLowerInvariant (c);

			switch (c) {
			case '\u0049': // Latin uppercase I
				if (handleDotI)
					return '\u0131'; // I becomes dotless i
				break;
			case '\u0130': // I-dotted
				return '\u0069'; // i

			case '\u01c5': // LATIN CAPITAL LETTER D WITH SMALL LETTER Z WITH CARON
				return '\u01c6';
			// \u01c7 -> \u01c9 (LJ) : invariant
			case '\u01c8': // LATIN CAPITAL LETTER L WITH SMALL LETTER J
				return '\u01c9';
			// \u01ca -> \u01cc (NJ) : invariant
			case '\u01cb': // LATIN CAPITAL LETTER N WITH SMALL LETTER J
				return '\u01cc';
			// WITH CARON : invariant
			// WITH DIAERESIS AND * : invariant

			case '\u01f2': // LATIN CAPITAL LETTER D WITH SMALL LETTER Z
				return '\u01f3';
			case '\u03d2':  // ? it is not in ICU
				return '\u03c5';
			case '\u03d3':  // ? it is not in ICU
				return '\u03cd';
			case '\u03d4':  // ? it is not in ICU
				return '\u03cb';
			}
			return ToLowerInvariant (c);
		}

		public virtual char ToUpper (char c)
		{
			// quick ASCII range check
			if (c < 0x60)
				return c;
			else if ('a' <= c && c <= 'z' && (!handleDotI || c != 'i'))
				return (char) (c - 0x20);

			if (ci == null || ci.LCID == 0x7F)
				return ToUpperInvariant (c);

			switch (c) {
			case '\u0069': // Latin lowercase i
				if (handleDotI)
					return '\u0130'; // dotted capital I
				break;
			case '\u0131': // dotless i
				return '\u0049'; // I

			case '\u01c5': // see ToLower()
				return '\u01c4';
			case '\u01c8': // see ToLower()
				return '\u01c7';
			case '\u01cb': // see ToLower()
				return '\u01ca';
			case '\u01f2': // see ToLower()
				return '\u01f1';
			case '\u0390': // GREEK SMALL LETTER IOTA WITH DIALYTIKA AND TONOS
				return '\u03aa'; // it is not in ICU
			case '\u03b0': // GREEK SMALL LETTER UPSILON WITH DIALYTIKA AND TONOS
				return '\u03ab'; // it is not in ICU
			case '\u03d0': // GREEK BETA
				return '\u0392';
			case '\u03d1': // GREEK THETA
				return '\u0398';
			case '\u03d5': // GREEK PHI
				return '\u03a6';
			case '\u03d6': // GREEK PI
				return '\u03a0';
			case '\u03f0': // GREEK KAPPA
				return '\u039a';
			case '\u03f1': // GREEK RHO
				return '\u03a1';
			// am not sure why miscellaneous GREEK symbols are 
			// not handled here.
			}

			return ToUpperInvariant (c);
		}

		private char ToTitleCase (char c)
		{
			// Handle some Latin characters.
			switch (c) {
			case '\u01c4':
			case '\u01c5':
			case '\u01c6':
				return '\u01c5';
			case '\u01c7':
			case '\u01c8':
			case '\u01c9':
				return '\u01c8';
			case '\u01ca':
			case '\u01cb':
			case '\u01cc':
				return '\u01cb';
			case '\u01f1':
			case '\u01f2':
			case '\u01f3':
				return '\u01f2';
			}
			if ('\u2170' <= c && c <= '\u217f' || // Roman numbers
				'\u24d0' <= c && c <= '\u24e9')
				return c;
			return ToUpper (c);
		}

		public unsafe virtual string ToLower (string str)
		{
			// In ICU (3.2) there are a few cases that one single
			// character results in multiple characters in e.g.
			// tr-TR culture. So I tried brute force conversion
			// test with single character as a string input, but 
			// there was no such conversion. So I think it just
			// invokes ToLower(char).
			if (str == null)
				throw new ArgumentNullException ("str");

			if (str.Length == 0)
				return String.Empty;

			string tmp = String.FastAllocateString (str.Length);
			fixed (char* source = str, dest = tmp) {

				char* destPtr = (char*)dest;
				char* sourcePtr = (char*)source;

				for (int n = 0; n < str.Length; n++) {
					*destPtr = ToLower (*sourcePtr);
					sourcePtr++;
					destPtr++;
				}
			}
			return tmp;
		}

		public unsafe virtual string ToUpper (string str)
		{
			// In ICU (3.2) there is a case that string
			// is handled beyond per-character conversion, but
			// it is only lt-LT culture where MS.NET does not
			// handle any special transliteration. So I keep
			// ToUpper() just as character conversion.
			if (str == null)
				throw new ArgumentNullException ("str");

			if (str.Length == 0)
				return String.Empty;

			string tmp = String.FastAllocateString (str.Length);
			fixed (char* source = str, dest = tmp) {

				char* destPtr = (char*)dest;
				char* sourcePtr = (char*)source;

				for (int n = 0; n < str.Length; n++) {
					*destPtr = ToUpper (*sourcePtr);
					sourcePtr++;
					destPtr++;
				}
			}
			return tmp;
		}

		[ComVisible (false)]
		public static TextInfo ReadOnly (TextInfo textInfo)
		{
			if (textInfo == null)
				throw new ArgumentNullException ("textInfo");

			TextInfo ti = new TextInfo (textInfo);
			ti.m_isReadOnly = true;
			return ti;
		}

		/* IDeserialization interface */
		[MonoTODO]
		void IDeserializationCallback.OnDeserialization(object sender)
		{
			// FIXME: we need to re-create "data" in order to get most properties working
		}

		/* IClonable */
		[ComVisible (false)]
		public virtual object Clone ()
		{
			return new TextInfo (this);
		}

		internal int GetCaseInsensitiveHashCode (string str)
		{
			return StringComparer.CurrentCultureIgnoreCase.GetHashCode (str);
		}

		internal static unsafe int GetHashCodeOrdinalIgnoreCase (string s)
		{
			var length = s.Length;
			fixed (char * c = s) {
				char * cc = c;
				char * end = cc + length - 1;
				int h = 0;
				for (;cc < end; cc += 2) {
					h = (h << 5) - h + Char.ToUpperInvariant (*cc);
					h = (h << 5) - h + Char.ToUpperInvariant (cc [1]);
				}
				++end;
				if (cc < end)
					h = (h << 5) - h + Char.ToUpperInvariant (*cc);
				return h;
			}
		}

		internal static unsafe int CompareOrdinalIgnoreCase(String str1, String str2)
		{
			return CompareOrdinalIgnoreCaseEx (str1, 0, str2, 0, str1.Length, str2.Length);
		}

		internal static int CompareOrdinalIgnoreCaseEx (String strA, int indexA, String strB, int indexB, int lenA, int lenB)
		{
			return CompareOrdinalCaseInsensitiveUnchecked (strA, indexA, lenA, strB, indexB, lenB);
		}

		static unsafe int CompareOrdinalCaseInsensitiveUnchecked (String strA, int indexA, int lenA, String strB, int indexB, int lenB)
		{
			if (strA == null) {
				return strB == null ? 0 : -1;
			}
			if (strB == null) {
				return 1;
			}
			int lengthA = Math.Min (lenA, strA.Length - indexA);
			int lengthB = Math.Min (lenB, strB.Length - indexB);

			if (lengthA == lengthB && Object.ReferenceEquals (strA, strB))
				return 0;

			fixed (char* aptr = strA, bptr = strB) {
				char* ap = aptr + indexA;
				char* end = ap + Math.Min (lengthA, lengthB);
				char* bp = bptr + indexB;
				while (ap < end) {
					if (*ap != *bp) {
						char c1 = Char.ToUpperInvariant (*ap);
						char c2 = Char.ToUpperInvariant (*bp);
						if (c1 != c2)
							return c1 - c2;
					}
					ap++;
					bp++;
				}
				return lengthA - lengthB;
			}
		}

		internal static unsafe int LastIndexOfStringOrdinalIgnoreCase(String source, String value, int startIndex, int count)
		{
			int valueLen = value.Length;
			if (count < valueLen)
				return -1;

			if (valueLen == 0)
				return startIndex;

			fixed (char* thisptr = source, valueptr = value) {
				char* ap = thisptr + startIndex - valueLen + 1;
				char* thisEnd = ap - count + valueLen - 1;
				while (ap != thisEnd) {
					for (int i = 0; i < valueLen; i++) {
						if (Char.ToUpperInvariant (ap[i]) != Char.ToUpperInvariant (valueptr[i]))
							goto NextVal;
					}
					return (int)(ap - thisptr);
					NextVal:
					ap--;
				}
			}
			return -1;
		}

		internal static int IndexOfStringOrdinalIgnoreCase(String source, String value, int startIndex, int count)
		{
            Contract.Assert(source != null, "[TextInfo.IndexOfStringOrdinalIgnoreCase] Caller should've validated source != null");
            Contract.Assert(value != null, "[TextInfo.IndexOfStringOrdinalIgnoreCase] Caller should've validated value != null");
            Contract.Assert(startIndex + count <= source.Length, "[TextInfo.IndexOfStringOrdinalIgnoreCase] Caller should've validated startIndex + count <= source.Length");

            // We return 0 if both inputs are empty strings
            if (source.Length == 0 && value.Length == 0)
            {
                return 0;
            }

            // the search space within [source] starts at offset [startIndex] inclusive and includes
            // [count] characters (thus the last included character is at index [startIndex + count -1]
            // [end] is the index of the next character after the search space
            // (it points past the end of the search space)
            int end = startIndex + count;
            
            // maxStartIndex is the index beyond which we never *start* searching, inclusive; in other words;
            // a search could include characters beyond maxStartIndex, but we'd never begin a search at an 
            // index strictly greater than maxStartIndex. 
            int maxStartIndex = end - value.Length;

            for (; startIndex <= maxStartIndex; startIndex++)
            {
                // We should always have the same or more characters left to search than our actual pattern
                Contract.Assert(end - startIndex >= value.Length);
                // since this is an ordinal comparison, we can assume that the lengths must match
                if (CompareOrdinalIgnoreCaseEx(source, startIndex, value, 0, value.Length, value.Length) == 0)
                {
                    return startIndex;
                }
            }
            
            // Not found
            return -1;
		}
	}
}
