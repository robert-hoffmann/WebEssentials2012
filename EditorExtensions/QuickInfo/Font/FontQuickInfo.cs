﻿using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class FontQuickInfo : IQuickInfoSource
    {
        private FontQuickInfoSourceProvider _provider;
        private readonly ITextBuffer _buffer;
        private static readonly InstalledFontCollection fonts = new InstalledFontCollection();
        private CssTree _tree;
        private readonly List<string> _allowed = new List<string> { "FONT", "FONT-FAMILY" };
        private bool _isDisposed;

        public FontQuickInfo(FontQuickInfoSourceProvider provider, ITextBuffer subjectBuffer)
        {
            _provider = provider;
            _buffer = subjectBuffer;
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> qiContent, out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;

            if (!EnsureTreeInitialized() || session == null || qiContent == null)
                return;

            // Map the trigger point down to our buffer.
            SnapshotPoint? point = session.GetTriggerPoint(_buffer.CurrentSnapshot);
            if (!point.HasValue)
                return;

            ParseItem item = _tree.StyleSheet.ItemBeforePosition(point.Value.Position);
            if (item == null || !item.IsValid)
                return;

            Declaration dec = item.FindType<Declaration>();
            if (dec == null || !dec.IsValid || !_allowed.Contains(dec.PropertyName.Text.ToUpperInvariant()))
                return;

            string fontName = item.Text.Trim('\'', '"');

            if (fonts.Families.SingleOrDefault(f => f.Name.Equals(fontName, StringComparison.OrdinalIgnoreCase)) != null)
            {
                FontFamily font = new FontFamily(fontName);

                applicableToSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(item.Start, item.Length, SpanTrackingMode.EdgeNegative);
                qiContent.Add(CreateFontPreview(font, 10));
                qiContent.Add(CreateFontPreview(font, 11));
                qiContent.Add(CreateFontPreview(font, 12));
                qiContent.Add(CreateFontPreview(font, 14));
                qiContent.Add(CreateFontPreview(font, 25));
                qiContent.Add(CreateFontPreview(font, 40));
            }
        }

        /// <summary>
        /// This must be delayed so that the TextViewConnectionListener
        /// has a chance to initialize the WebEditor host.
        /// </summary>
        public bool EnsureTreeInitialized()
        {
            if (_tree == null)
            {
                try
                {
                    CssEditorDocument document = CssEditorDocument.FromTextBuffer(_buffer);
                    _tree = document.Tree;
                }
                catch (Exception)
                {
                }
            }

            return _tree != null;
        }

        private static UIElement CreateFontPreview(FontFamily font, double size)
        {
            return new TextBlock()
            {
                Text = font.Source + " (" + size + "px)",
                FontFamily = font,
                FontSize = size,
            };
        }


        public void Dispose()
        {
            if (!_isDisposed)
            {
                GC.SuppressFinalize(this);
                _isDisposed = true;
            }
        }
    }
}
