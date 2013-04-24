﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using RazorEngine.Text;

namespace FormFactory.RazorEngine
{
    public class RazorTemplateHtmlHelper : FfHtmlHelper<IDictionary<string, object>> 
    {
        static RazorTemplateHtmlHelper()
        {
            var templateConfig = new TemplateServiceConfiguration
                {
                    Resolver = new DelegateTemplateResolver(name =>
                        {
                            string resourcePath = EmbeddedResourceRegistry.ResolveResourcePath(name);
                            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcePath);
                            using (var reader = new StreamReader(stream))
                            {
                                var sb = new StringBuilder();
                                while (reader.Peek()> 0)
                                {
                                    var readLine = reader.ReadLine();
                                    if (readLine.StartsWith("@model ")) continue;
                                    if (readLine == "@using FormFactory.AspMvc")
                                    {
                                        sb.AppendLine("@using FormFactory.RazorEngine");
                                        continue;
                                    }
                                    sb.AppendLine(readLine);
                                }
                                return sb.ToString();
                            }
                        })
                };
            templateConfig.BaseTemplateType = typeof (RazorTemplateFormFactoryTemplate<>);
            Razor.SetTemplateService(new TemplateService(templateConfig));
        }


        public IEnumerable<PropertyVm> PropertiesFor(object model, Type type)
        {
            return VmHelper.GetPropertyVmsUsingReflection(this, model, type);
        }
       
      
        public UrlHelper Url()
        {
            throw new NotImplementedException("Url not implemented in FormFactory.RazorTemplate");
        }

        public string WriteTypeToString(Type type)
        {
            return type.AssemblyQualifiedName;
        }

        public ViewData ViewData { get { return new RazorTemplateViewData(this); } }
        public FfContext FfContext
        {
            get { return new RazorEngineContext(this); }
        }

        public object Model { get; set; }

        public IHtmlString BestProperty(PropertyVm vm)
        {
            try
            {
                var viewname = this.FfContext.BestViewName(vm.Type, "FormFactory/Property.");
                viewname = viewname ??
                           FfContext.BestViewName(vm.Type.GetEnumerableType(), "FormFactory/Property.IEnumerable.");
                viewname = viewname ?? "FormFactory/Property.System.Object";
                //must be some unknown object exposed as an interface
                return Partial(viewname, vm);
            }
            catch(Exception ex)
            {
                return new HtmlString(ex.Message);
            }
        }
        public bool HasErrors(string modelName)
        {
            return false;
        }
        public string AllValidationMessages(string x)
        {
            return string.Empty;
        }
        public PropertyVm PropertyVm(Type type, string name, object value)
        {
            return new PropertyVm(this, type, name) { Value = value };
        }

        public IHtmlString Partial(string partialName, object model)
        {
            return Partial(partialName, model, null);
        }
        public IHtmlString Partial(string partialName, object model, IDictionary<string, object> viewData)
        {
            var template = Razor.Resolve(partialName, viewData);
            var dyn = (dynamic) template;
            dyn.Html = this;
            dyn.Model = model;
            if (viewData != null) dyn.ViewData = viewData;
            try
            {
                string result = template.Run(new ExecuteContext());
                return new HtmlString(result);
            }
            catch (Exception ex)
            {
                return new HtmlString(ex.Message);
            }
        }
        public IHtmlString UnobtrusiveValidation(object model)
        {
            return new HtmlString("");
        }
        public IHtmlString UnobtrusiveValidation(PropertyVm model)
        {
            return new HtmlString(""); //not implemented
        }
        public IHtmlString Raw(string s)
        {
            return new HtmlString(s);;
        }
    }

 
}