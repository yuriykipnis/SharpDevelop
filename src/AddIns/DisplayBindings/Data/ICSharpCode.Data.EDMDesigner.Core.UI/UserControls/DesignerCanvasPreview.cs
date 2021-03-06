﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

#region Usings

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.Data.EDMDesigner.Core.UI.UserControls.CSDLType;
using ICSharpCode.Data.EDMDesigner.Core.UI.UserControls.Common;
using ICSharpCode.Data.EDMDesigner.Core.EDMObjects.Designer;
using ICSharpCode.Data.EDMDesigner.Core.EDMObjects.Designer.CSDL.Type;
using ICSharpCode.Data.EDMDesigner.Core.UI.DisplayBinding;

#endregion

namespace ICSharpCode.Data.EDMDesigner.Core.UI.UserControls
{
    public class DesignerCanvasPreview : ContentControl
    {
        private EDMDesignerViewContent _container = null;
        private DesignerCanvas _content;
        private ListView _typebaseDesignerListView;
        private int _zoom;

        public DesignerCanvasPreview()
        {
            if (DesignerCanvasPreviewCreated != null)
                DesignerCanvasPreviewCreated(this);        
        }

        public DesignerCanvasPreview(EDMDesignerViewContent container) : this()
        {
            _container = container;
        }

        internal static EDMView EDMView { get; set; }
        internal static IUIType UIType { get; set; }

        public DesignerView DesignerView
        {
            get { return (DesignerView)GetValue(DesignerViewProperty); }
            set { SetValue(DesignerViewProperty, value); }
        }
        public static readonly DependencyProperty DesignerViewProperty =
            DependencyProperty.Register("DesignerView", typeof(DesignerView), typeof(DesignerCanvasPreview), new UIPropertyMetadata(null, (sender, e) =>
                {
                    var designerCanvasPreview = (DesignerCanvasPreview)sender;
                    designerCanvasPreview.Content = DesignerCanvas.GetDesignerCanvas(designerCanvasPreview._container, EDMView, (DesignerView)e.NewValue);
                }));

        internal new DesignerCanvas Content
        {
            get 
            {
                if (_content == null)
                    _content = (DesignerCanvas)base.Content;
                return _content; 
            }
            set 
            {
                if (value == null)
                {
                    Content.Zoom = _zoom;
                    Content.OnSelectionChanged();
                    TypebaseDesignerListView.Background = Brushes.White;
                }
                else
                {
                    value.UnselectAllTypes();
                    value.TypesVisibles.First(t => t.UIType == UIType).IsSelected = true;
                    RoutedEventHandler loadHandler = null;
                    loadHandler = delegate
                    {
                        TypebaseDesignerListView.Background = Brushes.Yellow;
                        _zoom = Content.Zoom;
                        var widthZoom = (int)(Width * 100.0 / _content.WidthNeed);
                        var heightZoom = (int)(Height * 100.0 / _content.HeightNeed);
                        _content.Zoom = Math.Min(widthZoom, heightZoom);
                        value.Loaded -= loadHandler;
                    };
                    value.Loaded += loadHandler;
                }
                _content = value;
                base.Content = value;
            }
        }

        private ListView TypebaseDesignerListView
        {
            get
            {
                if (_typebaseDesignerListView == null)
                    _typebaseDesignerListView = VisualTreeHelperUtil.GetControlsDecendant<ListView>(Content.Children.OfType<TypeBaseDesigner>().Where(tbd => tbd.UIType.BusinessInstance == UIType.BusinessInstance).First()).First();
                return _typebaseDesignerListView;
            }
        }

        public static event Action<DesignerCanvasPreview> DesignerCanvasPreviewCreated;
    }
}
