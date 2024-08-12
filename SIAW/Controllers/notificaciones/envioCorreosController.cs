using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_funciones;

namespace SIAW.Controllers.notificaciones
{
    [Route("api/notif/[controller]")]
    [ApiController]
    public class envioCorreosController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly Funciones funciones = new Funciones();

        public envioCorreosController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //[Authorize]
        [HttpPost]
        [Route("envioCorreoProforma/{userConn}/{usuario}/{codvendedor}/{codproforma}")]
        public async Task<ActionResult> envioCorreoProforma(string userConn, string usuario, int codvendedor, int codproforma, [FromForm] IFormFile pdfFile)
        {
            if (pdfFile == null || pdfFile.Length == 0)
            {
                return BadRequest(new { resp = "No se ha proporcionado un archivo PDF válido." });
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
                    var credenciales = await _context.adusuario.Where(i => i.login == usuario)
                         .Select(i => new
                         {
                             i.correo,
                             i.passwordcorreo,
                             i.celcorporativo,
                             i.persona
                         }).FirstOrDefaultAsync();
                    if (credenciales == null)
                    {
                        return BadRequest(new { resp = "No se encontraron datos con el usuario proporcionado. No se envio Correo." });
                    }
                    if (credenciales.correo == null || credenciales.passwordcorreo == null)
                    {
                        return BadRequest(new { resp = "No se encontraron las credenciales del correo del usuario proporcionado. No se envio Correo." });
                    }
                    var nombreVendedor = await _context.pepersona.Where(i => i.codigo == (credenciales.persona))
                        .Select(i => i.nombre1 + " " + i.nombre2 + " " + i.apellido1 + " " + i.apellido2).FirstOrDefaultAsync();

                    var emailsCc = await _context.vevendedor_destinatarios.Where(i => i.codvendedor == codvendedor)
                        .Select(i => i.destinatarios).ToListAsync();
                    if (emailsCc.Count() == 0)
                    {
                        return BadRequest(new { resp = "No se encontraron destinatarios relacionados a su codigo de vendedor. No se envio Correo." });
                    }

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
                    if (dataProf == null)
                    {
                        return BadRequest(new { resp = "No se encontraron datos con el código de proforma." });
                    }
                    //string direcc_mail_cliente = "analista.nal.informatica2@pertec.com.bo";
                    string titulo = "Solicitud Recepción de Proforma " + dataProf.id + "-" + dataProf.numeroid;
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

                                    <div style=""max-width: 75%; margin: 0 auto;"" class=""email-container"">

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

                                            <!-- 1 Column Text + Button : BEGIN -->
                                            <tr>
                                                <td style=""background-color: #ffffff;"">
                                                    <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"">
                                                        <tr>
                                                            <td
                                                                style=""padding: 15px; font-family: 'Franklin Gothic Medium', 'Arial Narrow', Arial, sans-serif; font-size: 15px; line-height: 20px; color: #555555;"">
                                                                <h1
                                                                    style=""text-align: center;margin: 0 0 10px 0; font-family: Franklin Gothic Medium; font-size: 25px; line-height: 30px; color: #333333; font-weight: normal;"">
                                                                    Solicitud Recepción de Proforma</h1>
                                                            </td>
                                                        </tr>

                                                        <tr>
                                                            <td
                                                                style=""padding: 0px 45px 30px 45px; font-family: Franklin Gothic; font-size: 15px; color: #555555;"">

                                                                <p style=""margin: 0;"">
                                                                    Servicio al Cliente:
                                                                    <br><br>
                                                                    Tomar nota de la generación de una nueva proforma con la siguiente información:
                                                                    <br><br>
                                                                    <strong>Detalles de la Proforma:</strong>
                                                                    <br><br>
                                                                    <strong>•	ID y Número ID de Proforma: </strong>" + dataProf.id + "-" + dataProf.numeroid +
                                                                    @"<br>
                                                                    <strong>•	Fecha: </strong>" + dataProf.fecha.ToShortDateString() +
                                                                    @"<br>
                                                                    <strong>•	Cliente: </strong>" + dataProf.codcliente + " - " + dataProf.nomcliente +
                                                                    @"<br>
                                                                    <strong>•	Vendedor </strong>" + dataProf.codvendedor +
                                                                    @"<br>
                                                                    <strong>•	Subtotal: </strong>" + dataProf.subtotal +
                                                                    @"<br>
                                                                    <strong>•	Descuentos: </strong>" + dataProf.descuentos +
                                                                    @"<br>
                                                                    <strong>•	Total: </strong>" + dataProf.total +
                                                                    @"<br><br>

                                                                    Se adjunta en PDF la proforma generada para su aprobación.
                                                                    <br><br>
                                                                    Por favor, proceder con la aprobación y seguimiento.
                                                                    <br><br>
                                                                    En caso de dudas o de requerir mayor información ponerse en contacto con mi persona.
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

                                                    </table>
                                                </td>
                                            </tr>
                                            <!-- 1 Column Text + Button : END -->

                                        </table>
                                        <!-- Email Body : END -->

                                        <!-- Email Footer : BEGIN -->
                                        <table align=""center"" role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%""
                                            style=""margin: auto;"">
                                            <tr>
                                                <td
                                                    style=""padding: 20px; font-family: Franklin Gothic Medium; font-size: 12px; line-height: 15px; text-align: center; color: #ffffff;"">
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
                                                                <p style=""margin: 0;"">Derechos Reservados Pertec S.R.L © | Maestros en Pernos " +
                                                                    DateTime.Today.ToString("d-M-yyyy") +
                                                                @"</p>
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


                    bool envio = funciones.EnviarEmail(credenciales.correo, "", emailsCc, credenciales.correo, credenciales.passwordcorreo, titulo, detalle, pdfBytes, pdfFile.FileName);
                    if (envio)
                    {
                        return Ok(new { resp = "Correo enviado con éxito." });
                    }
                    else
                    {
                        return BadRequest(new { resp = "Error al enviar el correo." });
                    }
                }
                catch (DbUpdateException)
                {
                    return Problem("Error en el servidor");
                }
            }
        }





    }
}
