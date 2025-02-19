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
            try
            {
                if (context.Request.Method == "POST" || context.Request.Method == "PUT")
                {
                    bool BANDERA = context.Request.Path.Value.Contains("/guardarProforma");
                    bool BANDERA2 = context.Request.Path.Value.Contains("/importProf");
                    bool BANDERA3 = context.Request.Path.Value.Contains("/envioCorreoProforma");
                    bool BANDERA4 = context.Request.Path.Value.Contains("/grabarFacturasNR");
                    bool BANDERA5 = context.Request.Path.Value.Contains("/enviarFacturaEmail");
                    bool BANDERA6 = context.Request.Path.Value.Contains("/grabarFacturaTienda");
                    bool BANDERA7 = context.Request.Path.Value.Contains("/importNMinJson");
                    bool BANDERA8 = context.Request.Path.Value.Contains("/importPedidoinJson");
                    if (!context.Request.Path.Value.Contains("/refreshToken") && !BANDERA && !BANDERA2 && !BANDERA3 && !BANDERA4 && !BANDERA5 && !BANDERA6 && !BANDERA7 && !BANDERA8)
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
            }
            catch (Exception)
            {
                // Manejar la excepción aquí
                // Puedes registrar el error, devolver una respuesta de error, etc.
                // Por ejemplo:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("Ocurrió un error durante el procesamiento de la solicitud.");
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
