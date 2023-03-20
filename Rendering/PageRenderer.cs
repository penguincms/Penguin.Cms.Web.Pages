using Loxifi;
using Microsoft.AspNetCore.Hosting;
using Penguin.Cms.Abstractions.Interfaces;
using Penguin.Cms.Pages;
using Penguin.Cms.Web.Pages.Extensions;
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
        static TypeFactory TypeFactory { get; set; } = new TypeFactory(new TypeFactorySettings());

        private static readonly object ViewInjectorLock = new();

        protected static string ViewInjectors { get; set; }

        protected IServiceProvider ServiceProvider { get; set; }

        public PageRenderer(IHostingEnvironment hostingEnvironment, IServiceProvider serviceProvider = null) : base(hostingEnvironment)
        {
            ServiceProvider = serviceProvider;
        }

        public (string RelativePath, object Model) GenerateRenderInformation(Page page, IEnumerable<TemplateParameter> parameters = null)
        {
            if (page is null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            parameters ??= new List<TemplateParameter>();

            string PageContent;

            lock (ViewInjectorLock)
            {
                if (ViewInjectors is null)
                {
                    StringBuilder viewInjectors = new();

                    _ = viewInjectors.Append(string.Empty);

                    if (ServiceProvider != null)
                    {
                        foreach (Type t in TypeFactory.GetAllImplementations(typeof(IMacroProvider)))
                        {
                            if (ServiceProvider.GetService(t) != null)
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