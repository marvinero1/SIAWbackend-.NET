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
                using (var reader = new StreamReader(context.Request.Body))
                {
                    var body = await reader.ReadToEndAsync();
                    var transformedBody = TransformToUppercase(body);
                    var stream = new MemoryStream(Encoding.UTF8.GetBytes(transformedBody));
                    context.Request.Body = stream;
                }
            }

            await _next(context);
        }

        private string TransformToUppercase(string input)
        {
            // Convertir propiedades de cadena a mayúsculas
            var jsonObject = JObject.Parse(input);
            foreach (var property in jsonObject.Properties())
            {
                if (property.Value.Type == JTokenType.String)
                {
                    property.Value = ((string)property.Value).ToUpper();
                }
            }

            return jsonObject.ToString();
        }


    }
}
