using System;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace Core
{
    public class LogMiddleware : OwinMiddleware
    {
        public LogMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        public override async Task Invoke(IOwinContext context)
        {
            Console.WriteLine("Request begins: {0} {1}", context.Request.Method, context.Request.Uri);
            await Next.Invoke(context);
            Console.WriteLine("Request ends  : {0} {1} {2}", context.Request.Method, context.Request.Uri, context.Response.StatusCode);
        }
    }
}