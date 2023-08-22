﻿using System.Net;

namespace VisualArt.Media.Exceptions
{
    public class HttpResponseException : Exception
    {
        public HttpResponseException(int statusCode, object? value = null) =>
            (StatusCode, Value) = (statusCode, value);
        public HttpResponseException(HttpStatusCode statusCode, object? value = null) =>
            (StatusCode, Value) = ((int)statusCode, value);
        public int StatusCode { get; }

        public object? Value { get; }
    }
}
