using Application.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.ActionExtensions
{
    public static class ActionExtensions
    {
        public static void FireAndForget(this Action fn)
        {
#pragma warning disable 4014
            try
            {
                Task.Run(fn);
            }
            catch (Exception e) { }
        }
    }
}