﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace EFCore.TextTemplating.Design
{
    /// <summary>
    /// Base class for transformations. The default one generated by T4 isn't compatible with .NET Standard.
    /// </summary>
    abstract class MyCodeGeneratorBase
    {
        bool _endsWithNewline;
        readonly List<int> _indentLengths = new List<int>();
        string _currentIndent = "";

        public virtual IDictionary<string, object> Session { get; set; }

        protected StringBuilder GenerationEnvironment { get; set; } = new StringBuilder();

        public abstract string TransformText();

        protected string CurrentIndent
            => _currentIndent;

        protected void Write(string textToAppend)
        {
            if (string.IsNullOrEmpty(textToAppend))
                return;

            // If we're starting off, or if the previous text ended with a newline,
            // we have to append the current indent first.
            if (GenerationEnvironment.Length == 0 || _endsWithNewline)
            {
                GenerationEnvironment.Append(_currentIndent);
                _endsWithNewline = false;
            }

            // Check if the current text ends with a newline
            if (textToAppend.EndsWith(Environment.NewLine, StringComparison.CurrentCulture))
            {
                _endsWithNewline = true;
            }

            // This is an optimization. If the current indent is "", then we don't have to do any
            // of the more complex stuff further down.
            if (_currentIndent.Length == 0)
            {
                GenerationEnvironment.Append(textToAppend);

                return;
            }

            // Everywhere there is a newline in the text, add an indent after it
            textToAppend = textToAppend.Replace(Environment.NewLine, Environment.NewLine + _currentIndent);

            // If the text ends with a newline, then we should strip off the indent added at the very end
            // because the appropriate indent will be added when the next time Write() is called
            if (_endsWithNewline)
            {
                GenerationEnvironment.Append(textToAppend, 0, textToAppend.Length - _currentIndent.Length);
            }
            else
            {
                GenerationEnvironment.Append(textToAppend);
            }
        }

        protected void WriteLine(string textToAppend)
        {
            Write(textToAppend);
            GenerationEnvironment.AppendLine();
            _endsWithNewline = true;
        }

        protected void Write(string format, params object[] args)
            => Write(string.Format(CultureInfo.CurrentCulture, format, args));

        protected void WriteLine(string format, params object[] args)
            => WriteLine(string.Format(CultureInfo.CurrentCulture, format, args));

        protected void PushIndent(string indent)
        {
            if (indent == null)
            {
                throw new ArgumentNullException(nameof(indent));
            }

            _currentIndent += indent;
            _indentLengths.Add(indent.Length);
        }

        protected string PopIndent()
        {
            var returnValue = string.Empty;

            if (_indentLengths.Count != 0)
            {
                var indentLength = _indentLengths[_indentLengths.Count - 1];
                _indentLengths.RemoveAt(_indentLengths.Count - 1);

                if (indentLength != 0)
                {
                    returnValue = _currentIndent.Substring(_currentIndent.Length - indentLength);
                    _currentIndent = _currentIndent.Remove(_currentIndent.Length - indentLength);
                }
            }

            return returnValue;
        }

        protected void ClearIndent()
        {
            _indentLengths.Clear();
            _currentIndent = string.Empty;
        }

        protected class ToStringInstanceHelper
        {
            IFormatProvider _formatProvider = CultureInfo.InvariantCulture;

            public IFormatProvider FormatProvider
            {
                get => _formatProvider;
                set
                {
                    if (value != null)
                    {
                        _formatProvider = value;
                    }
                }
            }

            public string ToStringWithCulture(object objectToConvert)
            {
                if (objectToConvert == null)
                    throw new ArgumentNullException(nameof(objectToConvert));

                var method = objectToConvert.GetType().GetMethod("ToString", new[] { typeof(IFormatProvider) });
                if (method == null)
                    return objectToConvert.ToString();

                return (string)method.Invoke(objectToConvert, new[] { _formatProvider });
            }
        }

        protected ToStringInstanceHelper ToStringHelper { get; } = new ToStringInstanceHelper();
    }
}
