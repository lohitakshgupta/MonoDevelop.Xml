// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Editor.Completion;
using MonoDevelop.Xml.Parser;

namespace MonoDevelop.MSBuild.Editor.SmartIndent
{
	public class XmlSmartIndent<TParser, TResult> : ISmartIndent
		where TParser : XmlBackgroundParser<TResult>, new()
		where TResult : XmlParseResult
	{
		readonly ITextView textView;
		readonly IEditorOptions options;

		public XmlSmartIndent (ITextView textView) : this (textView, textView.Options)
		{
		}

		public XmlSmartIndent (ITextView textView, IEditorOptions options)
		{
			this.textView = textView;
			this.options = options;
		}
		public virtual void Dispose ()
		{
		}

		protected TParser GetParser () => BackgroundParser<TResult>.GetParser<TParser> ((ITextBuffer2)textView.TextBuffer);

		//FIXME: make this smarter, it's very simple right now
		public virtual int? GetDesiredIndentation (ITextSnapshotLine line)
		{
			var parser = GetParser ();

			var indentSize = options.GetIndentSize ();
			var tabsToSpaces = options.IsConvertTabsToSpacesEnabled ();

			//calculate the delta between the previous line's expected and actual indent
			int? previousIndentDelta = null;

			//FIXME: make this work with tabs
			if (tabsToSpaces) {
				// find a preceding non-empty line so we don't get confused by blank lines with virtual indents
				var previousLine = GetPreviousNonEmptyLine (line);
				if (previousLine != null) {
					var previousExpectedIndent = GetLineExpectedIndent (previousLine, parser, indentSize);
					var previousActualIndent = GetLineActualIndent (previousLine);
					previousIndentDelta = previousActualIndent - previousExpectedIndent;
				}
			}

			var indent = GetLineExpectedIndent (line, parser, indentSize);

			// if the previous non-blank line was in the same state and had a different indent than
			// expected, the user has manually corrected it, so re-apply the same delta to this line.
			if (previousIndentDelta.HasValue) {
				indent = Math.Max (0, indent + previousIndentDelta.Value);
			}

			return indent;
		}

		static ITextSnapshotLine GetPreviousNonEmptyLine (ITextSnapshotLine line)
		{
			const int MAX_ITERATIONS = 20;
			int end = Math.Max (0, line.LineNumber - MAX_ITERATIONS);

			for (int i = line.LineNumber - 1; i >= end; i--) {
				var prev = line.Snapshot.GetLineFromLineNumber (i);
				if (prev.Length > 0) {
					return prev;
				}
			}

			return null;
		}

		protected virtual int GetLineActualIndent (ITextSnapshotLine line)
		{
			int actualIndent = 0;
			int start = line.Start.Position;
			int length = line.Length;
			var snapshot = line.Snapshot;
			for (int i = start; i < start + length; i++) {
				if (snapshot[i] == ' ') {
					actualIndent++;
				} else {
					break;
				}
			}
			return actualIndent;
		}

		protected virtual int GetLineExpectedIndent (ITextSnapshotLine line, TParser parser, int indentSize)
		{
			//create a lightweight tree parser, which will actually close nodes
			var startParser = parser.GetSpineParser (line.Start);
			var startNodes = startParser.Nodes.ToList ();
			var startState = startParser.CurrentState;
			startNodes.Reverse ();

			var endNodes = parser.GetSpineParser (line.End).Nodes.ToList ();
			endNodes.Reverse ();

			//count the number of elements in the stack at the start of the line
			//which were not closed by the end of the line
			int depth = 0;

			//first node is the xdocument, skip it
			for (int i = 1; i < startNodes.Count; i++) {
				if (i == endNodes.Count || !(startNodes[i] is XElement) || startNodes[i] != endNodes[i]) {
					break;
				}
				depth++;
			}

			//if inside a tag state, indent a level further
			while (startState != null) {
				if (startState is XmlTagState) {
					depth ++;
				}
				startState = startState.Parent;
			}

			int indent = indentSize * depth;
			return indent;
		}
	}
}