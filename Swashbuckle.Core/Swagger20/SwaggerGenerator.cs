﻿using System.Linq;
using System.Collections.Generic;
using System.Web.Http.Description;
using System;

namespace Swashbuckle.Swagger20
{
    public class SwaggerGenerator : ISwaggerProvider
    {
        private readonly IApiExplorer _apiExplorer;
        private readonly SwaggerGeneratorSettings _settings;

        public SwaggerGenerator(
            IApiExplorer apiExplorer,
            SwaggerGeneratorSettings settings)
        {
            _apiExplorer = apiExplorer;
            _settings = settings;
        }

        public SwaggerDocument GetSwaggerFor(string apiVersion)
        {
            var schemaRegistry = new SchemaRegistry(_settings.SchemaFilters);

            Info info;
            _settings.ApiVersions.TryGetValue(apiVersion, out info);
            if (info == null)
                throw new UnknownApiVersion(apiVersion);

            var paths = GetApiDescriptionsFor(apiVersion)
                .GroupBy(apiDesc => apiDesc.RelativePathSansQueryString())
                .ToDictionary(group => "/" + group.Key, group => CreatePathItem(group, schemaRegistry));

            var swaggerDoc = new SwaggerDocument
            {
                info = info,
                host = _settings.Host,
                basePath = _settings.VirtualPathRoot,
                schemes = _settings.Schemes.ToList(),
                paths = paths,
                definitions = schemaRegistry.Definitions
            };

            foreach(var filter in _settings.DocumentFilters)
            {
                filter.Apply(swaggerDoc, schemaRegistry, _apiExplorer);
            }

            return swaggerDoc;
        }

        private IEnumerable<ApiDescription> GetApiDescriptionsFor(string apiVersion)
        {
            return _apiExplorer.ApiDescriptions
                .Where(apiDesc => _settings.VersionSupportResolver(apiDesc, apiVersion));
        }

        private PathItem CreatePathItem(IEnumerable<ApiDescription> apiDescriptions, SchemaRegistry schemaRegistry)
        {
            var pathItem = new PathItem();

            // Group further by http method
            var perMethodGrouping = apiDescriptions
                .GroupBy(apiDesc => apiDesc.HttpMethod.Method.ToLower());

            foreach (var group in perMethodGrouping)
            {
                var httpMethod = group.Key;

                if (group.Count() > 1)
                    throw new NotSupportedException(String.Format(
                        "Not supported by Swagger 2.0: Multiple operations with path '{0}' and method '{1}'",
                        "/" + group.First().RelativePathSansQueryString(),
                        httpMethod));

                var apiDescription = group.Single();
                switch (httpMethod)
                {
                    case "get":
                        pathItem.get = CreateOperation(apiDescription, schemaRegistry);
                        break;
                    case "put":
                        pathItem.put = CreateOperation(apiDescription, schemaRegistry);
                        break;
                    case "post":
                        pathItem.post = CreateOperation(apiDescription, schemaRegistry);
                        break;
                    case "delete":
                        pathItem.delete = CreateOperation(apiDescription, schemaRegistry);
                        break;
                    case "options":
                        pathItem.options = CreateOperation(apiDescription, schemaRegistry);
                        break;
                    case "head":
                        pathItem.head = CreateOperation(apiDescription, schemaRegistry);
                        break;
                    case "patch":
                        pathItem.patch = CreateOperation(apiDescription, schemaRegistry);
                        break;
                }
            }

            return pathItem;
        }

        private Operation CreateOperation(ApiDescription apiDescription, SchemaRegistry schemaRegistry)
        {
            var parameters = apiDescription.ParameterDescriptions
                .Select(paramDesc =>
                    {
                        var inPath = apiDescription.RelativePathSansQueryString().Contains("{" + paramDesc.Name + "}");
                        return CreateParameter(paramDesc, inPath, schemaRegistry);
                    })
                 .ToList();

            var responses = new Dictionary<string, Response>{
                { "200", CreateResponse(apiDescription.SuccessResponseType(), schemaRegistry) }
            };

            var operation = new Operation
            { 
                operationId = apiDescription.OperationId(),
                produces = apiDescription.Produces().ToList(),
                consumes = apiDescription.Consumes().ToList(),
                parameters = parameters,
                responses = responses,
                deprecated = apiDescription.IsObsolete()
            };

            foreach (var filter in _settings.OperationFilters)
            {
                filter.Apply(operation, schemaRegistry, apiDescription);
            }

            return operation;
        }

        private Parameter CreateParameter(ApiParameterDescription paramDesc, bool inPath, SchemaRegistry schemaRegistry)
        {
            var @in = (inPath)
                ? "path"
                : (paramDesc.Source == ApiParameterSource.FromUri) ? "query" : "body";

            var parameter = new Parameter
            {
                name = paramDesc.Name,
                @in = @in
            };

            if (paramDesc.ParameterDescriptor == null)
            {
                parameter.type = "string";
                parameter.required = true;
                return parameter; 
            }

            parameter.required = !paramDesc.ParameterDescriptor.IsOptional;

            var schema = schemaRegistry.FindOrRegister(paramDesc.ParameterDescriptor.ParameterType);
            if (parameter.@in == "body")
            {
                parameter.schema = schema;
            }
            else
            {
                parameter.format = schema.format;
                parameter.type = schema.type;
            }

            return parameter;
        }

        private Response CreateResponse(Type returnType, SchemaRegistry schemaRegistry)
        {
            var schema = (returnType != null)
                ? schemaRegistry.FindOrRegister(returnType)
                : null;

            return new Response { schema = schema };
        }
    }
}