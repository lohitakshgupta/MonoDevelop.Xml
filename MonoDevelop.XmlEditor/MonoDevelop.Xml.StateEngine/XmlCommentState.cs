// 
// XmlCommentState.cs
// 
// Author:
//   Mikayla Hutchinson <m.j.hutchinson@gmail.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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

using System;
using System.Collections.Generic;
using System.Text;

namespace MonoDevelop.Xml.StateEngine
{
	public class XmlCommentState : State
	{
		char backOne;
		char backTwo;

		public XmlCommentState (State parent, int position)
			: base (parent, position)
		{
		}
		
		protected XmlCommentState (XmlCommentState copyFrom, bool copyParents)
			: base (copyFrom, copyParents)
		{
			backOne = copyFrom.backOne;
			backTwo = copyFrom.backTwo;
		}

		public override State PushChar (char c, int position)
		{
			if (c == '>' && backOne == '-' && backTwo == '-') {
				Close (position);
				return Parent;
			}
			backTwo = backOne;
			backOne = c;
			return null;
		}

		public override string ToString ()
		{
			return "[XmlComment]";
		}
		
		public override State DeepCopy (bool copyParents)
		{
			return new XmlCommentState (this, copyParents);
		}
	}
}