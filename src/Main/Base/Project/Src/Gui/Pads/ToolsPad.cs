// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Linq;
using System.Windows.Controls;
using ICSharpCode.Core;

namespace ICSharpCode.SharpDevelop.Gui
{
	/// <summary>
	/// A pad that shows a single child control determined by the document that currently has the focus.
	/// </summary>
	public class ToolsPad : AbstractPadContent
	{
		private ContentPresenter contentControl = new ContentPresenter();
		
		public override object Control
		{
			get { return contentControl; }
		}
		
		public ToolsPad()
		{
			WorkbenchSingleton.Workbench.ActiveViewContentChanged += WorkbenchActiveContentChanged;
			WorkbenchActiveContentChanged(null, null);
		}
		
		private void WorkbenchActiveContentChanged(object sender, EventArgs e)
		{
			IViewContent viewContent = WorkbenchSingleton.Workbench.ActiveViewContent;
			contentControl.SetContent(GetToolBoxContent(viewContent), viewContent);
		}
		
		public object GetToolBoxContent(IViewContent viewContent)
		{
			if (viewContent != null)
			{
				Type holderType = viewContent.GetType();
				var node = AddInTree.GetTreeNode("/SharpDevelop/Workbench/ToolBoxContent", false);
				var toolBoxCodon = node.Codons.Where(codon => codon.Properties["holder"].Equals(holderType.FullName)).FirstOrDefault();
				if (toolBoxCodon != null)
					return toolBoxCodon.BuildItem(null, null);
			}
		
			return StringParser.Parse("${res:SharpDevelop.SideBar.NoToolsAvailableForCurrentDocument}");;
		}
	}
}
