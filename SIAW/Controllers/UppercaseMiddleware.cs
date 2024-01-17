namespace SIAW.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    public class UppercaseMiddleware
    {
        private readonly RequestDelegate _next;

        public UppercaseMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Method == "POST" || context.Request.Method == "PUT")
            {
                if (!context.Request.Path.Value.Contains("/refreshToken"))
                {
                    using (var reader = new StreamReader(context.Request.Body))
                    {
                        var body = await reader.ReadToEndAsync();
                        var transformedBody = TransformToUppercase(body);
                        var stream = new MemoryStream(Encoding.UTF8.GetBytes(transformedBody));
                        context.Request.Body = stream;
                    }
                }
            }

            await _next(context);
        }

        private string TransformToUppercase(string input)
        {
            // Intentar convertir a lista
            JArray jsonArray;
            try
            {
                jsonArray = JArray.Parse(input);
            }
            catch (JsonReaderException)
            {
                // Si falla, intentar convertir a objeto
                JObject jsonObject;
                try
                {
                    jsonObject = JObject.Parse(input);
                }
                catch (JsonReaderException)
                {
                    // Si falla también, devolver la entrada original
                    return input;
                }

                // Convertir propiedades de cadena a mayúsculas
                foreach (var property in jsonObject.Properties())
                {
                    if (property.Value.Type == JTokenType.String)
                    {
                        property.Value = ((string)property.Value).ToUpper();
                    }
                }

                return jsonObject.ToString();
            }

            // Convertir cada objeto en la lista
            foreach (var item in jsonArray)
            {
                if (item.Type == JTokenType.Object)
                {
                    // Convertir propiedades de cadena a mayúsculas
                    foreach (var property in ((JObject)item).Properties())
                    {
                        if (property.Value.Type == JTokenType.String)
                        {
                            property.Value = ((string)property.Value).ToUpper();
                        }
                    }
                }
            }

            return jsonArray.ToString();
        }


    }
}
