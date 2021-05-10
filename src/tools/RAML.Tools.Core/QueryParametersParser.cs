using System.Collections.Generic;
using System.Linq;
using RAML.Parser.Model;
using AMF.Tools.Core.ClientGenerator;
using RAML.Api.Core;

namespace AMF.Tools.Core
{
    public class QueryParametersParser
    {
        private readonly IDictionary<string, ApiObject> schemaObjects;

        public QueryParametersParser(IDictionary<string, ApiObject> schemaObjects)
        {
            this.schemaObjects = schemaObjects;
        }

        public ApiObject GetQueryObject(ClientGeneratorMethod generatedMethod, Operation method, string objectName,
            IDictionary<string, ApiEnum> existingEnums = null)
        {
            var queryObject = new ApiObject { Name = generatedMethod.Name + objectName + "Query" };
            queryObject.Properties = ParseParameters(method, existingEnums);


            return queryObject;
        }

        public IList<Property> ParseParameters(Operation method, IDictionary<string, ApiEnum> existingEnums = null)
        {
            return ConvertParametersToProperties(method.Request.QueryParameters, existingEnums);
        }

        public IList<Property> ConvertParametersToProperties(IEnumerable<Parameter> parameters, 
            IDictionary<string, ApiEnum> existingEnums = null)
        {
            var properties = new List<Property>();
            foreach (var parameter in parameters.Where(parameter => parameter != null && parameter.Schema != null))
            {
                var description = ParserHelpers.RemoveNewLines(parameter.Description);

				properties.Add(new Property
				               {
					               Type = NewNetTypeMapper.GetNetType(parameter.Schema, existingEnums: existingEnums),
					               Name = NetNamingMapper.GetPropertyName(parameter.Name),
                                   OriginalName = parameter.Name,
					               Description = description,
					               Example = ObjectParser.MapExample(parameter.Schema),
					               Required = parameter.Required
				               });
			}
			return properties;
		}

	    private string GetType(Parameter param)
	    {
	        if (param.Schema == null)
                return "string";
	        
            if(param.Schema is ScalarShape scalar)
	            return NewNetTypeMapper.GetNetType(scalar) +
	                   (NewNetTypeMapper.GetNetType(scalar) == "string" || NewNetTypeMapper.GetNetType(scalar) == "object" || param.Required ? "" : "?");

            var pureType = param.Schema.Name;

            if (schemaObjects.ContainsKey(pureType))
            {
                var apiObject = schemaObjects[pureType];
                return RamlTypesHelper.GetTypeFromApiObject(apiObject);
            }

	        return pureType;
	    }

    }
}