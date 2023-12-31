﻿using System.Net;

namespace Opswat.Challenge.Exceptions
{
    internal class HttpResponseException : Exception
    {
        public HttpStatusCode StatusCode { get; private set; }
        public string Content { get; private set; }

        public HttpResponseException(HttpStatusCode statusCode, string reasonPhrase, string content) : base(reasonPhrase)
        {
            StatusCode = statusCode;
            Content = content;
        }
    }
}
