// 
// XmlTagState.cs
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

using System.Diagnostics;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Parser
{
	public class XmlTagState : XmlParserState
	{
		internal const int ATTEMPT_RECOVERY = 1;
		internal const int RECOVERY_FOUND_WHITESPACE = 2;
		internal const int MAYBE_SELF_CLOSING = 2;
		internal const int FREE = 0;

		readonly XmlAttributeState AttributeState;
		readonly XmlNameState NameState;
		
		public XmlTagState () : this (new XmlAttributeState ()) {}
		
		public XmlTagState  (XmlAttributeState attributeState)
			: this (attributeState, new XmlNameState ()) {}
		
		public XmlTagState (XmlAttributeState attributeState, XmlNameState nameState)
		{
			AttributeState = attributeState;
			NameState = nameState;
			
			Adopt (AttributeState);
			Adopt (NameState);
		}
		
		public override XmlParserState PushChar (char c, IXmlParserContext context, ref string rollback)
		{
			var element = context.Nodes.Peek () as XElement;
			
			if (element == null || element.IsEnded) {
				var parent = element;
				element = new XElement (context.Position - 2) { Parent = parent }; // 2 == < + current char
				context.Nodes.Push (element);
				if (context.BuildTree) {
					var parentContainer = (XContainer)context.Nodes.Peek (element.IsClosed ? 0 : 1);
					parentContainer.AddChildNode (element);
				}
			}
			
			if (c == '<') {
				if (element.IsNamed) {
					context.LogError ("Unexpected '<' in tag '" + element.Name.FullName + "'.");
					Close (element, context, context.Position - 1);
				} else {
					context.LogError ("Tag has no name.", element.Span.Start);
				}
				
				rollback = string.Empty;
				return Parent;
			}
			
			Debug.Assert (!element.IsEnded);
			
			if (element.IsClosed && c != '>') {
				if (char.IsWhiteSpace (c)) {
					context.LogWarning ("Unexpected whitespace after '/' in self-closing tag.");
					return null;
				}
				context.LogError ("Unexpected character '" + c + "' after '/' in self-closing tag.");
				context.Nodes.Pop ();
				return Parent;
			}
			
			//if tag closed
			if (c == '>') {
				element.HasEndBracket = true;
				if (context.StateTag == MAYBE_SELF_CLOSING) {
					element.Close (element);
				}
				if (!element.IsNamed) {
					context.LogError ("Tag closed prematurely.");
				} else {
					Close (element, context, context.Position);
				}
				return Parent;
			}

			if (c == '/') {
				context.StateTag = MAYBE_SELF_CLOSING;
				return null;
			}

			if (context.StateTag == ATTEMPT_RECOVERY) {
				if (XmlChar.IsWhitespace (c)) {
					context.StateTag = RECOVERY_FOUND_WHITESPACE;
				}
				return null;
			}

			if (context.StateTag == RECOVERY_FOUND_WHITESPACE) {
				if (!XmlChar.IsFirstNameChar (c))
					return null;
			}

			context.StateTag = FREE;

			if (context.CurrentStateLength > 1 && XmlChar.IsFirstNameChar (c)) {
				rollback = string.Empty;
				return AttributeState;
			}

			if (!element.IsNamed && XmlChar.IsFirstNameChar (c)) {
				rollback = string.Empty;
				return NameState;
			}

			if (XmlChar.IsWhitespace (c))
				return null;

			context.LogError ("Unexpected character '" + c + "' in tag.", context.Position - 1);
			context.StateTag = ATTEMPT_RECOVERY;
			return null;
		}
		
		protected virtual void Close (XElement element, IXmlParserContext context, int endOffset)
		{
			//have already checked that element is not null, i.e. top of stack is our element
			if (element.IsClosed)
				context.Nodes.Pop ();

			element.End (endOffset);
		}
	}
}
