// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <author name="Daniel Grunwald"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.SharpDevelop.Dom.Refactoring;
using System.Collections.Generic;

namespace ICSharpCode.SharpDevelop
{
	/// <summary>
	/// Allows creating a <see cref="ICompletionDataList"/> from code-completion information.
	/// </summary>
	public class CodeCompletionItemProvider
	{
		/// <summary>
		/// Shows code completion for the specified editor.
		/// </summary>
		public virtual void ShowCompletion(ITextEditor editor)
		{
			ICompletionItemList itemList = GenerateCompletionList(editor);
			if (itemList != null)
				editor.ShowCompletionWindow(itemList);
		}
		
		public virtual ICompletionItemList GenerateCompletionList(ITextEditor editor)
		{
			if (editor == null)
				throw new ArgumentNullException("textEditor");
			ExpressionResult expression = GetExpression(editor);
			return GenerateCompletionListForExpression(editor, expression);
		}
		
		public virtual ExpressionResult GetExpression(ITextEditor editor)
		{
			return GetExpressionFromOffset(editor, editor.Caret.Offset);
		}
		
		protected ExpressionResult GetExpressionFromOffset(ITextEditor editor, int offset)
		{
			if (editor == null)
				throw new ArgumentNullException("editor");
			IDocument document = editor.Document;
			IExpressionFinder expressionFinder = ParserService.GetExpressionFinder(editor.FileName);
			if (expressionFinder == null) {
				return ExpressionResult.Empty;
			} else {
				return expressionFinder.FindExpression(document.GetText(0, offset), offset);
			}
		}
		
		public virtual ICompletionItemList GenerateCompletionListForExpression(ITextEditor editor, ExpressionResult expressionResult)
		{
			if (expressionResult.Expression == null) {
				return null;
			}
			if (LoggingService.IsDebugEnabled) {
				if (expressionResult.Context == ExpressionContext.Default)
					LoggingService.DebugFormatted("GenerateCompletionData for >>{0}<<", expressionResult.Expression);
				else
					LoggingService.DebugFormatted("GenerateCompletionData for >>{0}<<, context={1}", expressionResult.Expression, expressionResult.Context);
			}
			ResolveResult rr = Resolve(editor, expressionResult);
			return GenerateCompletionListForResolveResult(rr, expressionResult.Context);
		}
		
		public virtual ResolveResult Resolve(ITextEditor editor, ExpressionResult expressionResult)
		{
			if (editor == null)
				throw new ArgumentNullException("editor");
			return ParserService.Resolve(expressionResult, editor.Caret.Line, editor.Caret.Column, editor.FileName, editor.Document.Text);
		}
		
		public virtual ICompletionItemList GenerateCompletionListForResolveResult(ResolveResult rr, ExpressionContext context)
		{
			if (rr == null)
				return null;
			IProjectContent callingContent = rr.CallingClass != null ? rr.CallingClass.ProjectContent : null;
			ArrayList arr = rr.GetCompletionData(callingContent ?? ParserService.CurrentProjectContent);
			return GenerateCompletionListForCompletionData(arr, context);
		}
		
		public virtual ICompletionItemList GenerateCompletionListForCompletionData(ArrayList arr, ExpressionContext context)
		{
			if (arr == null)
				return null;
			
			List<ICompletionItem> resultItems = new List<ICompletionItem>();
			DefaultCompletionItemList result = new DefaultCompletionItemList(resultItems);
			Dictionary<string, CodeCompletionItem> methodItems = new Dictionary<string, CodeCompletionItem>();
			foreach (object o in arr) {
				if (context != null && !context.ShowEntry(o))
					continue;
				
				IMethod method = o as IMethod;
				if (method != null) {
					CodeCompletionItem codeItem;
					if (methodItems.TryGetValue(method.Name, out codeItem)) {
						codeItem.Overloads++;
						continue;
					}
				}
				
				ICompletionItem item = CreateCompletionItem(o, context);
				if (item != null) {
					resultItems.Add(item);
					CodeCompletionItem codeItem = item as CodeCompletionItem;
					if (method != null && codeItem != null) {
						methodItems[method.Name] = codeItem;
					}
					if (o.Equals(context.SuggestedItem))
						result.SuggestedItem = item;
				}
			}
			resultItems.Sort((a,b) => string.Compare(a.Text, b.Text, StringComparison.OrdinalIgnoreCase));
			
			if (context.SuggestedItem != null) {
				if (result.SuggestedItem == null) {
					result.SuggestedItem = CreateCompletionItem(context.SuggestedItem, context);
					if (result.SuggestedItem != null) {
						resultItems.Insert(0, result.SuggestedItem);
					}
				}
			}
			return result;
		}
		
		public virtual ICompletionItem CreateCompletionItem(object o, ExpressionContext context)
		{
			IEntity entity = o as IEntity;
			if (entity != null)
				return new CodeCompletionItem(entity);
			else
				return new DefaultCompletionItem(o.ToString());
		}
	}
	
	public class DotCodeCompletionItemProvider : CodeCompletionItemProvider
	{
		
	}
	
	public class CodeCompletionItem : ICompletionItem
	{
		readonly IEntity entity;
		
		public CodeCompletionItem(IEntity entity)
		{
			if (entity == null)
				throw new ArgumentNullException("entity");
			this.entity = entity;
			
			IAmbience ambience = AmbienceService.GetCurrentAmbience();
			ambience.ConversionFlags = entity is IClass ? ConversionFlags.ShowTypeParameterList : ConversionFlags.None;
			this.Text = ambience.Convert(entity);
			ambience.ConversionFlags = ConversionFlags.StandardConversionFlags;
			description = ambience.Convert(entity);
			this.Overloads = 1;
		}
		
		public string Text { get; private set; }
		
		public int Overloads { get; set; }
		
		#region Description
		string description;
		bool descriptionCreated;
		
		public string Description {
			get {
				lock (this) {
					if (!descriptionCreated) {
						descriptionCreated = true;
						if (Overloads > 1) {
							description += Environment.NewLine +
								StringParser.Parse("${res:ICSharpCode.SharpDevelop.DefaultEditor.Gui.Editor.CodeCompletionData.OverloadsCounter}", new string[,] {{"NumOverloads", this.Overloads.ToString()}});
						}
						if (!string.IsNullOrEmpty(entity.Documentation)) {
							string documentation = ICSharpCode.SharpDevelop.DefaultEditor.Gui.Editor.CodeCompletionData.ConvertDocumentation(entity.Documentation);
							if (!string.IsNullOrEmpty(documentation)) {
								description += Environment.NewLine + documentation;
							}
						}
					}
					return description;
				}
			}
		}
		#endregion
	}
}