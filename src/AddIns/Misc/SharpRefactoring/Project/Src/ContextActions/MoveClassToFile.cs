﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Martin Konicek" email="martin.konicek@gmail.com"/>
//     <version>$Revision: $</version>
// </file>
using System;
using System.IO;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.Refactoring;

namespace SharpRefactoring.ContextActions
{
	/// <summary>
	/// Description of MoveClassToFile.
	/// </summary>
	public class MoveClassToFile : ContextAction
	{
		public CacheClassAtCaret ClassAtCaret
		{
			get { return this.Context.GetCached<CacheClassAtCaret>(); }
		}
		
		public override string Title {
			get {
				var fileName = Path.GetFileName(ClassAtCaret.CorrectClassFileName);
				return StringParser.Parse("${res:SharpDevelop.Refactoring.MoveClassToFile}", new StringTagPair("FileName", fileName));
			}
		}
		
		public override bool IsAvailable(EditorContext context)
		{
			if (ClassAtCaret.Class == null) return false;
			return (ClassAtCaret.IsFixClassFileNameAvailable && ClassAtCaret.Class.CompilationUnit.Classes.Count != 1);
		}
		
		public override void Execute(EditorContext context)
		{
			FindReferencesAndRenameHelper.MoveClassToFile(ClassAtCaret.Class, ClassAtCaret.CorrectClassFileName);
		}
	}
}