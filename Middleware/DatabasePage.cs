using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Penguin.Cms.Pages.Repositories;
using Penguin.Web.Abstractions.Interfaces;
using System.Threading.Tasks;

namespace Penguin.Cms.Modules.Pages.Middleware
{
    //http://azurecoder.net/2017/07/09/routing-middleware-custom-irouter/
    public class DatabasePage : IPenguinMiddleware
    {
        private readonly RequestDelegate _next;

        //TODO: Learn what this is
        public DatabasePage(RequestDelegate next)
        {
            this._next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context is null)
            {
                throw new System.ArgumentNullException(nameof(context));
            }

            string RequestUrl = context.Request.Path.Value.Split('?')[0];

            if (context.RequestServices.GetService<PageRepository>().TryGetPageFromCache(RequestUrl, out _))
            {
                context.Request.Path = "/Page/RenderPage";

                context.Request.QueryString = context.Request.QueryString.Add("Url", RequestUrl);
            }

            await this._next(context).ConfigureAwait(true);
        }
    }
}