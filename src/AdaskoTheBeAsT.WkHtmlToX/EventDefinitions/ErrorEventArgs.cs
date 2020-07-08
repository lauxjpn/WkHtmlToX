#nullable enable
using System;
using AdaskoTheBeAsT.WkHtmlToX.Abstractions;

namespace AdaskoTheBeAsT.WkHtmlToX.EventDefinitions
{
    public class ErrorEventArgs : EventArgs
    {
        public ErrorEventArgs(
            IDocument? document,
            string message)
        {
            Document = document;
            Message = message;
        }

        public IDocument? Document { get; }

        public string Message { get; }
    }
}
