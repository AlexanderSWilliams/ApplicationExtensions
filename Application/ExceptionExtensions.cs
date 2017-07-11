using System;

namespace Application.ExceptionExtensions
{
    public static class ExceptionExtensions
    {
        public static string FullException(this Exception e, string result = "")
        {
            var Result = e.ToString() + (result != "" ? "\r\n" + result : "");
            if (e.InnerException == null)
                return Result;
            else
                return FullException(e.InnerException, Result);
        }
    }
}