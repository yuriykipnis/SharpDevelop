﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the BSD license (for details please see \src\AddIns\Debugger\Debugger.AddIn\license.txt)

using System;
using System.Collections.Generic;
using Debugger.AddIn.Visualizers.Graph.Drawing;

namespace Debugger.AddIn.Visualizers.Graph.Layout
{
	/// <summary>
	/// Node in tree-layouted <see cref="PositionedGraph"/>.
	/// This is the abstract ancestor of TreeNodeLR and TreeNodeTB.
	/// There are 2 dimensions - "main" and "lateral".
	/// Main dimension is the dimension in which the graph depth grows (vertical when TB, horizontal when LR).
	/// Lateral dimension is the other dimension. Siblings are placed next to each other in Lateral dimension.
	/// </summary>
	public abstract class TreeGraphNode : PositionedGraphNode
	{
		public static TreeGraphNode Create(LayoutDirection direction, ObjectGraphNode objectNode)
		{
			switch (direction) {
					case LayoutDirection.TopBottom:	return new TreeNodeTB(objectNode);
					case LayoutDirection.LeftRight: return new TreeNodeLR(objectNode);
					default: throw new DebuggerVisualizerException("Unsupported layout direction: " + direction.ToString());
			}
		}
		
		public double HorizontalMargin { get; set; }
		public double VerticalMargin { get; set; }
		
		protected TreeGraphNode(ObjectGraphNode objectNode) : base(objectNode)
		{
		}
		
		/// <summary>
		/// Width or height of the subtree.
		/// </summary>
		public double SubtreeSize { get; set; }
		
		public abstract double MainCoord { get; set; }
		public abstract double LateralCoord { get; set; }
		
		public abstract double MainSize { get; }
		public abstract double LateralSize { get; }
		
		public double MainSizeWithMargin { get { return MainSize + MainMargin; } }
		public double LateralSizeWithMargin { get { return LateralSize + LateralMargin; } }
		
		public abstract double MainMargin { get; }
		public abstract double LateralMargin { get; }
		
		public IEnumerable<PositionedEdge> ChildEdges 
		{
			get 
			{ 
				foreach (TreeGraphEdge childEdge in Edges)
				{
					if (childEdge.IsTreeEdge)
					{
						yield return childEdge;
					}
				}
			}
		}
		
		public IEnumerable<TreeGraphNode> Childs
		{
			get
			{
				foreach (PositionedEdge outEdge in this.ChildEdges)
					yield return (TreeGraphNode)outEdge.Target;
			}
		}
	}
}
