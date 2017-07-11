using System;
using System.Threading.Tasks;

namespace Application.FuncTaskExtensions
{
    public static class FuncTaskExtensions
    {
        public static void FireAndForget(this Func<Task> fn)
        {
#pragma warning disable 4014
            try
            {
                Task.Run(fn).ConfigureAwait(false);
            }
            catch (Exception e) { }
        }
    }
}