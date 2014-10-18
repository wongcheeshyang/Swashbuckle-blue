﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Http.Controllers;
using System.Web.Http.Description;
using System.Xml.XPath;
using Swashbuckle.Swagger20;

namespace Swashbuckle.SwaggerFilters
{
    public class ApplyXmlActionComments : IOperationFilter
    {
        private const string MethodExpression = "/doc/members/member[@name='M:{0}.{1}{2}']";
        private const string SummaryExpression = "summary";
        private const string RemarksExpression = "remarks";
        private const string ParameterExpression = "param[@name=\"{0}\"]";
        private const string ResponseExpression = "response";

        private readonly XPathNavigator _navigator;

        public ApplyXmlActionComments(string xmlCommentsPath)
        {
            _navigator = new XPathDocument(xmlCommentsPath).CreateNavigator();
        }

        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            var methodNode = _navigator.SelectSingleNode(GetXPathFor(apiDescription.ActionDescriptor));
            if (methodNode == null) return;

            var summary = methodNode.SelectSingleNode(SummaryExpression);
            if (summary != null)
                operation.summary = summary.Value.Trim();

            var remarks = methodNode.SelectSingleNode(RemarksExpression);
            if (remarks != null)
                operation.description = remarks.Value.Trim();

        //    foreach (var paramDesc in apiDescription.ParameterDescriptions)
        //    {
        //        if (paramDesc.ParameterDescriptor == null) continue; // not in action signature (e.g. route parameter)

        //        var parameter = operation.Parameters.SingleOrDefault(p => p.Name == paramDesc.Name);
        //        if (parameter == null) continue;

        //        parameter.Description = GetChildValueOrDefault(
        //            methodNode,
        //            String.Format(ParameterExpression, paramDesc.ParameterDescriptor.ParameterName));
        //    }

        //    if (methodNode == null) return;

        //    foreach (var responseMessage in GetResponseMessages(methodNode))
        //    {
        //        operation.ResponseMessages.Add(responseMessage);
        //    }
        }

        private static string GetXPathFor(HttpActionDescriptor actionDescriptor)
        {
            var controllerName = actionDescriptor.ControllerDescriptor.ControllerType.FullName;
            var actionName = actionDescriptor.ActionName;

            var paramTypeNames = actionDescriptor.GetParameters()
                .Select(paramDesc => TypeNameFor(paramDesc.ParameterType))
                .ToArray();

            var parameters = (paramTypeNames.Any())
                ? String.Format("({0})", String.Join(",", paramTypeNames))
                : String.Empty;

            return String.Format(MethodExpression, controllerName, actionName, parameters);
        }

        private static string TypeNameFor(Type type)
        {
            if (type.IsGenericType)
            {
                var genericArguments = type.GetGenericArguments()
                    .Select(TypeNameFor)
                    .ToArray();

                var builder = new StringBuilder(type.Namespace + "." + type.Name);

                return builder
                    .Replace(String.Format("`{0}", genericArguments.Count()), String.Empty)
                    .Append(String.Format("{{{0}}}", String.Join(",", genericArguments).TrimEnd(',')))
                    .ToString();
            }

            return type.Namespace + "." + type.Name;
        }

        //private static IEnumerable<ResponseMessage> GetResponseMessages(XPathNavigator node)
        //{
        //    var iterator = node.Select(ResponseExpression);
        //    while (iterator.MoveNext())
        //    {
        //        yield return new ResponseMessage
        //        {
        //            Code = Int32.Parse(iterator.Current.GetAttribute("code", String.Empty)),
        //            Message = iterator.Current.Value
        //        };
        //    }
        //}
    }
}