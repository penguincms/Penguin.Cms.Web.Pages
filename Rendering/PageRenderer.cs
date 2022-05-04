using Microsoft.AspNetCore.Hosting;
using Penguin.Cms.Abstractions.Interfaces;
using Penguin.Cms.Pages;
using Penguin.DependencyInjection.Abstractions.Interfaces;
using Penguin.Reflection;
using Penguin.Reflection.Extensions;
using Penguin.Templating.Abstractions;
using Penguin.Web.Templating;
using System;
using System.Collections.Generic;
using System.Text;
using TemplateParameter = Penguin.Templating.Abstractions.TemplateParameter;

namespace Penguin.Cms.Web.Pages.Rendering
{
    public class PageRenderer : ObjectRenderer, ISelfRegistering
    {
        private static readonly object ViewInjectorLock = new object();
        protected static string ViewInjectors { get; set; }
        protected IServiceProvider ServiceProvider { get; set; }

        public PageRenderer(IHostingEnvironment hostingEnvironment, IServiceProvider serviceProvider = null) : base(hostingEnvironment)
        {
            this.ServiceProvider = serviceProvider;
        }

        public (string RelativePath, object Model) GenerateRenderInformation(Page page, IEnumerable<TemplateParameter> parameters = null)
        {
            if (page is null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            parameters = parameters ?? new List<TemplateParameter>();

            string PageContent;

            lock (ViewInjectorLock)
            {
                if (ViewInjectors is null)
                {
                    StringBuilder viewInjectors = new StringBuilder();

                    _ = viewInjectors.Append(string.Empty);

                    if (this.ServiceProvider != null)
                    {
                        foreach (Type t in TypeFactory.GetAllImplementations(typeof(IMacroProvider)))
                        {
                            if (this.ServiceProvider.GetService(t) != null)
                            {
                                _ = viewInjectors.Append($"@inject {t.GetDeclaration()} {t.Name} {Environment.NewLine}");
                            }
                        }
                    }

                    ViewInjectors = viewInjectors.ToString();
                }
            }

            PageContent = ViewInjectors + Environment.NewLine;

            if (!string.IsNullOrWhiteSpace(page.Layout))
            {
                PageContent += "@{ Layout = \"" + page.Layout + "\"; }" + Environment.NewLine + Environment.NewLine;
            }
            else
            {
                PageContent += "@{ Layout = null; }" + Environment.NewLine + Environment.NewLine;
            }

            PageContent += page.Content;

            GeneratedTemplateInfo generatedTemplateInfo = GenerateTemplatePath(page, parameters, PageContent, "Content");

            return (generatedTemplateInfo.RelativePath, generatedTemplateInfo.Model);
        }
    }
}