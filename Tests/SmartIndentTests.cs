// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.MiniEditor;
using Microsoft.VisualStudio.Text.Editor;

using MonoDevelop.MSBuild.Editor.SmartIndent;
using MonoDevelop.Xml.Editor.Completion;
using MonoDevelop.Xml.Tests.Completion;
using MonoDevelop.Xml.Tests.EditorTestHelpers;

using NUnit.Framework;

namespace MonoDevelop.Xml.Tests
{
	class SmartIndentTests : EditorTestBase
	{
		protected override string ContentTypeName => CompletionTestContentType.Name;

		protected override (EditorEnvironment, EditorCatalog) InitializeEnvironment () => TestEnvironment.EnsureInitialized ();

		[Test]
		[TestCase ("<a>|", 4)] // single indent inside one element
		[TestCase ("<a b=''|", 8)] // double indent in element attributes
		[TestCase ("<a>\n    <b>|", 8)] // double indent when 2 elements deep
		[TestCase ("<a>\n<b>|", 4)] // respect user correction on previous line
		[TestCase ("<a>\n  <b>|", 6)] // respect user correction on previous line
		[TestCase ("<a>\n    <b>|</b>\n</a>", 4)] // tag close on current line deindents
		[TestCase ("<a>\n    <b><c>|</c></b>\n</a>", 4)] // double tag close
		public void TestSmartIndent (string doc, int expectedIndent)
		{
			var caretPos = doc.IndexOf ('|');
			if (caretPos > -1) {
				doc = doc.Replace ("|", "\n");
				caretPos++;
			} else {
				caretPos = doc.Length;
				doc += "\n";
			}

			var textView = CreateTextView (doc);
			var line = textView.TextBuffer.CurrentSnapshot.GetLineFromPosition (caretPos);

			var options = new TestEditorOptions ();
			options.SetOptionValue (DefaultOptions.ConvertTabsToSpacesOptionId, true);
			options.SetOptionValue (DefaultOptions.IndentSizeOptionId, 4);

			var smartIndent = new TestSmartIndent (textView, options);
			var indent = smartIndent.GetDesiredIndentation (line);
			Assert.AreEqual (expectedIndent, indent);
		}
	}

	class TestSmartIndent : XmlSmartIndent<XmlBackgroundParser, XmlParseResult>
	{
		public TestSmartIndent (ITextView textView, IEditorOptions options) : base (textView, options)
		{

		}
	}

	class TestEditorOptions : IEditorOptions
	{
		Dictionary<string, object> storage = new Dictionary<string, object> ();

		public IEnumerable<EditorOptionDefinition> SupportedOptions => throw new NotImplementedException ();

		public IEditorOptions GlobalOptions => throw new NotImplementedException ();

		public IEditorOptions Parent { get => throw new NotImplementedException (); set => throw new NotImplementedException (); }

		public event EventHandler<EditorOptionChangedEventArgs> OptionChanged;

		public bool ClearOptionValue (string optionId) => throw new NotImplementedException ();

		public bool ClearOptionValue<T> (EditorOptionKey<T> key) => throw new NotImplementedException ();

		public T GetOptionValue<T> (string optionId) => (T)storage[optionId];

		public T GetOptionValue<T> (EditorOptionKey<T> key) => (T)storage[key.Name];

		public object GetOptionValue (string optionId) => storage[optionId];

		public bool IsOptionDefined (string optionId, bool localScopeOnly) => throw new NotImplementedException ();

		public bool IsOptionDefined<T> (EditorOptionKey<T> key, bool localScopeOnly) => throw new NotImplementedException ();

		public void SetOptionValue (string optionId, object value) => storage[optionId] = value;

		public void SetOptionValue<T> (EditorOptionKey<T> key, T value) => storage[key.Name] = value;
	}
}