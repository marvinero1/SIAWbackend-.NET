using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using siaw_funciones;
using System.IO;
using System.Web.Http.Results;

namespace SIAW.Controllers.z_pruebas
{
    [Route("api/[controller]")]
    [ApiController]
    public class z_pruebaController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly Saldos saldos = new Saldos();
        // private readonly Cobranzas cobranzas = new Cobranzas();
        private readonly siaw_funciones.Funciones funciones = new Funciones();


        public z_pruebaController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        // POST: api/acaseguradora
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //[Authorize]
        [HttpPost]
        [Route("pruebaEnvioCorreo/{userConn}/{usuario}/{codvendedor}/{codproforma}")]
        public async Task<ActionResult> pruebaEnvioCorreo(string userConn, string usuario, int codvendedor, int codproforma, [FromForm] IFormFile pdfFile)
        {
            if (pdfFile == null || pdfFile.Length == 0)
            {
                return BadRequest("No se ha proporcionado un archivo PDF válido.");
            }

            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                
                try
                {
                    byte[] pdfBytes;
                    using (var memoryStream = new MemoryStream())
                    {
                        await pdfFile.CopyToAsync(memoryStream);
                        pdfBytes = memoryStream.ToArray();
                    }
                   

                    string direcc_mail_cliente = "analista.nal.informatica2@pertec.com.bo";
                    string titulo = "Solicitud Recepción de Pedido";

                    var credenciales = await _context.adusuario.Where(i => i.login == usuario)
                        .Select(i => new
                        {
                            i.correo,
                            i.passwordcorreo,
                            i.celcorporativo,
                            i.persona
                        }).FirstOrDefaultAsync();

                    var nombreVendedor = await _context.pepersona.Where(i => i.codigo == (credenciales.persona))
                        .Select(i => i.nombre1 + " " + i.nombre2 + " " + i.apellido1 + " " + i.apellido2).FirstOrDefaultAsync();

                    var emailsCc = await _context.adusuario_destinatarios.Where(i => i.codvendedor == codvendedor)
                        .Select(i => i.destinatarios).ToListAsync();

                    var dataProf = await _context.veproforma.Where(i => i.codigo == codproforma)
                        .Select(i => new
                        {
                            i.id,
                            i.numeroid,
                            i.fecha,
                            i.codcliente,
                            i.nomcliente,
                            i.codvendedor,
                            i.subtotal,
                            i.descuentos,
                            i.total
                        }).FirstOrDefaultAsync();

                    string detalle = @"
                        


                            <!DOCTYPE html>
                            <html lang=""en"" xmlns=""http://www.w3.org/1999/xhtml"" xmlns:v=""urn:schemas-microsoft-com:vml""
                                xmlns:o=""urn:schemas-microsoft-com:office:office"">

                            <head>
                                <meta charset=""utf-8""> <!-- utf-8 works for most cases -->
                                <meta name=""viewport"" content=""width=device-width""> <!-- Forcing initial-scale shouldn't be necessary -->
                                <meta http-equiv=""X-UA-Compatible"" content=""IE=edge""> <!-- Use the latest (edge) version of IE rendering engine -->
                                <meta name=""x-apple-disable-message-reformatting""> <!-- Disable auto-scale in iOS 10 Mail entirely -->
                                <meta name=""format-detection"" content=""telephone=no,address=no,email=no,date=no,url=no"">
                                <!-- Tell iOS not to automatically link certain text strings. -->
                                <meta name=""color-scheme"" content=""light"">
                                <meta name=""supported-color-schemes"" content=""light"">
                                <title></title> <!--   The title tag shows in email notifications, like Android 4.4. -->
                                <!-- CSS Reset : BEGIN -->
                                <style>
                                    /* What it does: Tells the email client that only light styles are provided but the client can transform them to dark. A duplicate of meta color-scheme meta tag above. */
                                    :root {
                                        color-scheme: light;
                                        supported-color-schemes: light;
                                    }

                                    /* What it does: Remove spaces around the email design added by some email clients. */
                                    /* Beware: It can remove the padding / margin and add a background color to the compose a reply window. */
                                    html,
                                    body {
                                        margin: 0 auto !important;
                                        padding: 0 !important;
                                        height: 100% !important;
                                        width: 100% !important;
                                    }

                                    /* What it does: Stops email clients resizing small text. */
                                    * {
                                        -ms-text-size-adjust: 100%;
                                        -webkit-text-size-adjust: 100%;
                                    }

                                    /* What it does: Centers email on Android 4.4 */
                                    div[style*=""margin: 16px 0""] {
                                        margin: 0 !important;
                                    }

                                    /* What it does: forces Samsung Android mail clients to use the entire viewport */
                                    #MessageViewBody,
                                    #MessageWebViewDiv {
                                        width: 100% !important;
                                    }

                                    /* What it does: Stops Outlook from adding extra spacing to tables. */
                                    table,
                                    td {
                                        mso-table-lspace: 0pt !important;
                                        mso-table-rspace: 0pt !important;
                                    }

                                    /* What it does: Fixes webkit padding issue. */
                                    table {
                                        border-spacing: 0 !important;
                                        border-collapse: collapse !important;
                                        table-layout: fixed !important;
                                        margin: 0 auto !important;
                                    }

                                    /* What it does: Uses a better rendering method when resizing images in IE. */
                                    img {
                                        -ms-interpolation-mode: bicubic;
                                    }

                                    /* What it does: Prevents Windows 10 Mail from underlining links despite inline CSS. Styles for underlined links should be inline. */
                                    a {
                                        text-decoration: none;
                                    }

                                    /* What it does: A work-around for email clients meddling in triggered links. */
                                    a[x-apple-data-detectors],
                                    /* iOS */
                                    .unstyle-auto-detected-links a,
                                    .aBn {
                                        border-bottom: 0 !important;
                                        cursor: default !important;
                                        color: inherit !important;
                                        text-decoration: none !important;
                                        font-size: inherit !important;
                                        font-family: inherit !important;
                                        font-weight: inherit !important;
                                        line-height: inherit !important;
                                    }

                                    /* What it does: Prevents Gmail from displaying a download button on large, non-linked images. */
                                    .a6S {
                                        display: none !important;
                                        opacity: 0.01 !important;
                                    }

                                    /* What it does: Prevents Gmail from changing the text color in conversation threads. */
                                    .im {
                                        color: inherit !important;
                                    }

                                    /* If the above doesn't work, add a .g-img class to any image in question. */
                                    img.g-img+div {
                                        display: none !important;
                                    }

                                    /* What it does: Removes right gutter in Gmail iOS app: https://github.com/TedGoas/Cerberus/issues/89  */
                                    /* Create one of these media queries for each additional viewport size you'd like to fix */

                                    /* iPhone 4, 4S, 5, 5S, 5C, and 5SE */
                                    @media only screen and (min-device-width: 320px) and (max-device-width: 374px) {
                                        u~div .email-container {
                                            min-width: 320px !important;
                                        }
                                    }

                                    /* iPhone 6, 6S, 7, 8, and X */
                                    @media only screen and (min-device-width: 375px) and (max-device-width: 413px) {
                                        u~div .email-container {
                                            min-width: 375px !important;
                                        }
                                    }

                                    /* iPhone 6+, 7+, and 8+ */
                                    @media only screen and (min-device-width: 414px) {
                                        u~div .email-container {
                                            min-width: 414px !important;
                                        }
                                    }
                                </style>
                                <!-- CSS Reset : END -->

                                <!-- Progressive Enhancements : BEGIN -->
                                <style>
                                    /* What it does: Hover styles for buttons */
                                    .button-td,
                                    .button-a {
                                        transition: all 100ms ease-in;
                                    }

                                    .button-td-primary:hover,
                                    .button-a-primary:hover {
                                        background: #555555 !important;
                                        border-color: #555555 !important;
                                    }

                                    /* Media Queries */
                                    @media screen and (max-width: 600px) {

                                        /* What it does: Adjust typography on small screens to improve readability */
                                        .email-container p {
                                            font-size: 17px !important;
                                        }

                                    }
                                </style>
                                <!-- Progressive Enhancements : END -->

                            </head>

                            <body width=""100%"" style=""margin: 0; padding: 0 !important; mso-line-height-rule: exactly; background-color: #093070;"">
                                <center role=""article"" aria-roledescription=""email"" lang=""en"" style=""width: 100%; background-color: #093070;"">

                                    <!-- Visually Hidden Preheader Text : BEGIN -->
                                    <div style=""max-height:0; overflow:hidden; mso-hide:all;"" aria-hidden=""true"">
                                        ¡Bienvenido a Pertec!
                                    </div>
                                    <div
                                        style=""display: none; font-size: 1px; line-height: 1px; max-height: 0px; max-width: 0px; opacity: 0; overflow: hidden; mso-hide: all; font-family: sans-serif;"">
                                        &zwnj;&nbsp;&zwnj;&nbsp;&zwnj;&nbsp;&zwnj;&nbsp;&zwnj;&nbsp;&zwnj;&nbsp;&zwnj;&nbsp;&zwnj;&nbsp;&zwnj;&nbsp;&zwnj;&nbsp;&zwnj;&nbsp;&zwnj;&nbsp;&zwnj;&nbsp;&zwnj;&nbsp;&zwnj;&nbsp;&zwnj;&nbsp;&zwnj;&nbsp;&zwnj;&nbsp;
                                    </div>

                                    <div style=""max-width: 600px; margin: 0 auto;"" class=""email-container"">

                                        <!-- Email Body : BEGIN -->
                                        <table align=""center"" role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%""
                                            style=""margin: auto;"">
                                            <!-- Email Header : BEGIN -->
                                            <tr>
                                                <td style=""padding: 20px 0; text-align: center"">
                                                    <img src=""https://pertec.com.bo/assets/img/pertec_moving.gif"" width=""125"" height=""50"" alt=""logo""
                                                        border=""0""
                                                        style=""height: auto; background: #dddddd; font-family: sans-serif; font-size: 15px; line-height: 15px; color: #555555;"">
                                                </td>
                                            </tr>
                                            <!-- Email Header : END -->

                                            <!-- Hero Image, Flush : BEGIN -->
                                            <tr>
                                                <td style=""background-color: #ffffff;"">
                                                    <img src=""https://pertec.com.bo/assets/img/bg.jpg"" width=""450"" height="""" alt=""alt_text""
                                                        border=""0""
                                                        style=""width: 100%; max-width: 600px; height: auto; background: #dddddd; font-family: sans-serif; font-size: 15px; line-height: 15px; color: #555555; margin: auto; display: block;""
                                                        class=""g-img"">
                                                </td>
                                            </tr>
                                            <!-- Hero Image, Flush : END -->

                                            <!-- 1 Column Text + Button : BEGIN -->
                                            <tr>
                                                <td style=""background-color: #ffffff;"">
                                                    <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"">
                                                        <tr>
                                                            <td
                                                                style=""padding: 20px; font-family: 'Franklin Gothic Medium', 'Arial Narrow', Arial, sans-serif; font-size: 15px; line-height: 20px; color: #555555;"">
                                                                <h1
                                                                    style=""text-align: center;margin: 0 0 10px 0; font-family: Franklin Gothic Medium; font-size: 25px; line-height: 30px; color: #333333; font-weight: normal;"">
                                                                    Solicitud Recepción de Pedido</h1>
                                                                <p style=""margin: 0;"">Servicio al Cliente: <br></p>
                                                            </td>
                                                        </tr>

                                                        <tr>
                                                            <td
                                                                style=""padding: 0px 45px 30px 45px; font-family: Franklin Gothic Medium; font-size: 15px; line-height: 20px; color: #555555;"">
                                                                <h2 style=""margin: 0 0 10px 0; font-family: Franklin Gothic Medium; font-size: 18px; line-height: 22px; color: #333333; font-weight: bold;"">
                                                                </h2>

                                                                <p style=""margin: 0;"">
                                                                    La presente es para informarles que se ha generado una nueva proforma con la siguiente información:
                                                                    <br><br>
                                                                    Detalles de la Proforma:
                                                                    <br><br>
                                                                    •	ID y Número ID de Proforma: " + dataProf.id + "-" + dataProf.numeroid +
                                                                    @"<br>
                                                                    •	Fecha: " + dataProf.fecha +
                                                                    @"<br>
                                                                    •	Cliente: " + dataProf.codcliente + " - " + dataProf.nomcliente +
                                                                    @"<br>
                                                                    •	Vendedor: " + dataProf.codvendedor +
                                                                    @"<br>
                                                                    •	Subtotal: " + dataProf.subtotal +
                                                                    @"< br>
                                                                    •	Descuentos: " + dataProf.descuentos +
                                                                    @"< br>
                                                                    •	Total: " + dataProf.total +
                                                                    @"< br><br>

                                                                    Se adjunta en PDF la proforma generada para su aprobación.
                                                                    <br><br>
                                                                    Por favor, procedan con la aprobación y seguimiento necesario para esta proforma. Si necesitan información adicional o hay alguna duda, 
                                                                    no duden en ponerse en contacto conmigo.
                                                                    <br><br>
                                                                    PostData: [Notas de Vendedor con alguna observacion].
                                                                    <br><br>
                                                                    Gracias por su atención y apoyo.
                                                                    <br><br>
                                                                    Saludos cordiales,
                                                                    <br><br>" +

                                                                    nombreVendedor +
                                                                    @"<br>" +
                                                                    credenciales.correo +
                                                                    @"<br>" +
                                                                    credenciales.celcorporativo +
                                                                    @"<br>

                                                                </p>

                                                            </td>
                                                        </tr>

                                                        <tr>
                                                            <td style=""padding: 0 20px;"">
                                                                <!-- Button : BEGIN -->
                                                                <table align=""center"" role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0""
                                                                    style=""margin: auto;"">
                                                                    <tr>
                                                                        <td class=""button-td button-td-primary""
                                                                            style=""border-radius: 4px; background: #093070;"">
                                                                            <a class=""button-a button-a-primary"" href=""https://pertec.com.bo/""
                                                                                style=""background: #093070; border: 1px solid #000000; font-family: Franklin Gothic Medium; font-size: 15px; 
													                            line-height: 15px;text-decoration: none; padding: 13px 17px; color: #ffffff; display: block; border-radius: 4px;"">
                                                    	                            Ir a Pertec</a>
                                                                        </td>
                                                                    </tr>
                                                                </table>
                                                                <!-- Button : END -->
                                                            </td>
                                                        </tr>
                                                    </table><br><br>
                                                </td>
                                            </tr>
                                            <!-- 1 Column Text + Button : END -->

                                            <!-- Clear Spacer : BEGIN -->
                                            <tr>
                                                <td aria-hidden=""true"" height=""40"" style=""font-size: 0px; line-height: 0px;"">
                                                    &nbsp;
                                                </td>
                                            </tr>
                                            <!-- Clear Spacer : END -->
                                        </table>
                                        <!-- Email Body : END -->

                                        <!-- Email Footer : BEGIN -->
                                        <table align=""center"" role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%""
                                            style=""margin: auto;"">
                                            <tr>
                                                <td
                                                    style=""padding: 20px; font-family: Franklin Gothic Medium; font-size: 12px; line-height: 15px; text-align: center; color: #ffffff;"">
                                                    <br><br>
                                                    Pertec S.R.L © | Maestros en Pernos<br><span class=""unstyle-auto-detected-links"">
                                                        Dirección: # 4581 Calle Innominada, Arocagua, Cochabamba-Bolivia,
                                                        <br>Telf: (+591) 471-6000</span> <span>Whatsapp: 72221031</span> <span>Celular:
                                                        72221031</span>
                                                    <br><br>
                                                    <unsubscribe style=""color: #ffffff; text-decoration: underline;"">unsubscribe</unsubscribe>
                                                </td>
                                            </tr>
                                        </table>
                                        <!-- Email Footer : END -->
                                    </div>

                                    <!-- Full Bleed Background Section : BEGIN -->
                                    <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%""
                                        style=""background-color: #fbd800;"">
                                        <tr>
                                            <td>
                                                <div align=""center"" style=""max-width: 600px; margin: auto;"" class=""email-container"">
                                                    <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"">
                                                        <tr>
                                                            <td style=""padding: 20px; text-align: left; font-family: Franklin Gothic Medium; font-size: 15px; 
									                            line-height: 20px; color: #000;text-align: center;"">
                                                                <p style=""margin: 0;"">Derechos Reservados Pertec S.R.L © | Maestros en Pernos
                                                                    {{ date('Y') }}</p>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </div>
                                            </td>
                                        </tr>
                                    </table>
                                </center>
                            </body>

                            </html>



                        "
                    ;


                    bool envio = funciones.EnviarEmail(credenciales.correo, direcc_mail_cliente, emailsCc, credenciales.correo, credenciales.passwordcorreo, titulo, detalle, pdfBytes, pdfFile.FileName);
                    if (envio)
                    {
                        return Ok("Correo enviado con éxito.");
                    }
                    else
                    {
                        return StatusCode(500, "Error al enviar el correo.");
                    }
                }
                catch (DbUpdateException)
                {
                    return Problem("Error en el servidor");
                }




            }
        }





        // POST: api/acaseguradora
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //[Authorize]
        [HttpPost("{userConn}")]
        public async Task<ActionResult<acaseguradora>> Postacaseguradora(string userConn, veptoventa veptoventa)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.veptoventa == null)
                {
                    return BadRequest(new { resp = "Entidad veptoventa es null." });
                }
                return Ok(veptoventa);
                _context.veptoventa.Add(veptoventa);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return Problem("Error en el servidor");
                }

                return Ok(new { resp = "204" });   // creado con exito

            }
        }

        // POST: api/veptoventa
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        // [Authorize]
        [HttpPost]
        [Route("aaa/{userConn}")]
        public async Task<ActionResult<adunidad>> Postadunidaefgqwegrvwd(string userConn, adunidad adunidad)
        {
            // Obtener el contexto de base de datos correspondiente al usuario
            string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.adunidad == null)
                {
                    return BadRequest(new { resp = "Entidad adunidad es null." });
                }
                _context.adunidad.Add(adunidad);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return Problem("Error en el servidor");
                }

                return Ok(new { resp = "204" });   // creado con exito

            }

        }

        // POST: api/acaseguradora
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //[Authorize]
        [HttpPost]
        [Route("ppppp/{userConn}")]
        public async Task<ActionResult<acaseguradora>> prueba (string userConn, RequestValidacion RequestValidacion)
        {
            return Ok(RequestValidacion);
           
        }
    }
}
