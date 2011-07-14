﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

#region Usings

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Xml.Linq;

using ICSharpCode.Data.EDMDesigner.Core.EDMObjects.Designer;
using ICSharpCode.Data.EDMDesigner.Core.EDMObjects.Designer.CSDL.Property;
using ICSharpCode.Data.EDMDesigner.Core.EDMObjects.Designer.CSDL.Type;
using ICSharpCode.Data.EDMDesigner.Core.IO;
using ICSharpCode.Data.EDMDesigner.Core.UI.Helpers;
using ICSharpCode.Data.EDMDesigner.Core.UI.UserControls;
using ICSharpCode.Data.EDMDesigner.Core.UI.UserControls.CSDLType;
using ICSharpCode.Data.EDMDesigner.Core.Windows.EDMWizard;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.Data.EDMDesigner.Core.EDMObjects.Designer.ChangeWatcher;
using ICSharpCode.Data.EDMDesigner.Core.EDMObjects.Common;
using ICSharpCode.SharpDevelop.Project;

#endregion

namespace ICSharpCode.Data.EDMDesigner.Core.UI.DisplayBinding
{
    public class EDMDesignerViewContent : AbstractViewContent, IHasPropertyContainer, IEDMDesignerChangeWatcherObserver
    {
        #region Fields

        private ScrollViewer _scrollViewer = new ScrollViewer() { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        private DesignerCanvas _designerCanvas = null;
        private PropertyContainer _propertyContainer = new PropertyContainer();
        private EDMView _edmView = null;
        private object _selection = null;

        #endregion

        #region Properties

        public object Selection
        {
            get { return _selection; }
            set
            {
                if (_selection == null)
                    _propertyContainer.Clear();
                else
                    _propertyContainer.SelectedObject = value;

                _selection = value;
            }
        }

        public Window Window
        {
            get { return Application.Current.MainWindow; }
        }

        public EDMView EDMView
        {
            get { return _edmView; }
        }
		
		public override object Control 
       {
            get { return _scrollViewer; }
		}
		
		public DesignerCanvas DesignerCanvas 
        {
            get { return _designerCanvas; }
		}

        #endregion

        #region Constructor

		public EDMDesignerViewContent(OpenedFile primaryFile)
			: base(primaryFile)
		{
			if (primaryFile == null)
				throw new ArgumentNullException("primaryFile");
			
			primaryFile.ForceInitializeView(this); // call Load()

			EDMDesignerChangeWatcher.AddEDMDesignerViewContent(this);
		}

        #endregion

        #region Methods

        public override void Load(OpenedFile file, Stream stream)
		{
			Debug.Assert(file == this.PrimaryFile);

            EDMDesignerChangeWatcher.Init = true;

            // Load EDMX from stream
            XElement edmxElement = null;
            Action<XElement> readMoreAction = edmxElt => edmxElement = edmxElt;            
            _edmView = new EDMView(stream, readMoreAction);
            
            // If EDMX is empty run EDM Wizard
            if (_edmView.EDM.IsEmpty)
            {
                edmxElement = null;
                EDMWizardWindow wizard = RunWizard(file, ProjectService.CurrentProject.RootNamespace);

                if (wizard.DialogResult == true)
                    _edmView = new EDMView(wizard.EDMXDocument, readMoreAction);
                else
                    throw new WizardCancelledException();
            }

            // Load or generate DesignerView and EntityTypeDesigners
            EntityTypeDesigner.Init = true;

            XElement designerViewsElement = null;

            if (edmxElement == null || (designerViewsElement = EDMXIO.ReadSection(edmxElement, EDMXIO.EDMXSection.DesignerViews)) == null)
            {
                designerViewsElement = DesignerIO.GenerateNewDesignerViewsFromCSDLView(_edmView);
            }

            if (edmxElement != null && designerViewsElement != null)
                DesignerIO.Read(_edmView, designerViewsElement, entityType => new EntityTypeDesigner(entityType), complexType => new ComplexTypeDesigner(complexType));

            EntityTypeDesigner.Init = false; 

            // Call DoEvents, otherwise drawing associations can fail
            VisualHelper.DoEvents();

            // Gets the designer canvas
            _designerCanvas = DesignerCanvas.GetDesignerCanvas(this, _edmView, _edmView.DesignerViews.FirstOrDefault());
			_scrollViewer.Content = _designerCanvas;
            
            // Register CSDL of EDMX in CSDL DatabaseTreeView
            CSDLDatabaseTreeViewAdditionalNode.Instance.CSDLViews.Add(_edmView.CSDL);
            
            EDMDesignerChangeWatcher.Init = false;
		}
		
		public override void Save(OpenedFile file, Stream stream)
		{
            EDMXIO.WriteXDocument(_edmView).Save(stream);
		}
		
		private EDMWizardWindow RunWizard(OpenedFile file, string projectStandardNamespace)
		{
            EDMWizardWindow wizard = new EDMWizardWindow(file, projectStandardNamespace);
            wizard.Owner = Application.Current.MainWindow;
            wizard.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            wizard.ShowDialog();
            
            return wizard;
		}

        public override void Dispose()
        {
            if (CSDLDatabaseTreeViewAdditionalNode.Instance.CSDLViews.Contains(_edmView.CSDL))
                CSDLDatabaseTreeViewAdditionalNode.Instance.CSDLViews.Remove(_edmView.CSDL);

            EDMDesignerChangeWatcher.RemoveEDMDesignerViewContent(this);
        }

        public void ShowMappingTab(IUIType uiType)
        { }

        #endregion

        #region IHasPropertyContainer

        public PropertyContainer PropertyContainer 
        {
			get { return _propertyContainer; }
		}

		#endregion

        #region IEDMDesignerChangeWatcherObserver Member

        public bool ObjectChanged(object changedObject)
        {
            foreach (DesignerView designerView in _edmView.DesignerViews)
            {                
                foreach (ITypeDesigner uiType in designerView)
                {
                    if (uiType == changedObject || uiType.UIType.BusinessInstance == changedObject)
                    {
                        PrimaryFile.IsDirty = true;
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion
    }
}
