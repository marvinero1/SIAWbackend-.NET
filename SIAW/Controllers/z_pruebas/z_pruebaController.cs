using LibSIAVB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using siaw_funciones;

using System;
using System.Drawing;
using System.Drawing.Printing;
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
        [Route("envioCorreoProforma/{userConn}/{usuario}/{codvendedor}/{codproforma}")]
        public async Task<ActionResult> envioCorreoProforma(string userConn, string usuario, int codvendedor, int codproforma, [FromForm] IFormFile pdfFile)
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

                    var emailsCc = await _context.vevendedor_destinatarios.Where(i => i.codvendedor == codvendedor)
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



        [HttpGet]
        [Route("imprimirLX350/{userConn}")]
        public async Task<ActionResult<acaseguradora>> imprimirLX350(string userConn)
        {
            /*
             
            Try
                If sia_funciones.Funciones.Instancia.GetOSVersion = "WinXP" Then
                    Dim impresoradestino As String

                    '//verificar si hay impresoras instaladas en el equipo
                    Dim prtdoc As New System.Drawing.Printing.PrintDocument
                    If Drawing.Printing.PrinterSettings.InstalledPrinters.Count = 0 Then
                        Throw New System.Exception("No hay impresoras instaladas.")
                    End If
                    prtdoc.Dispose()

                    If impresora = "" Then
                        '//Mostrar dialogo para elegir impresora
                        Dim prgelijeimpresora As New sia_funciones.prgelijeimpresora
                        prgelijeimpresora.ShowDialog()

                        If prgelijeimpresora.eligio Then
                            impresoradestino = Trim(prgelijeimpresora.impresora)
                        Else
                            impresoradestino = ""
                        End If
                        prgelijeimpresora.Dispose()
                        '//fin mostrar dialogo
                    Else
                        impresoradestino = impresora
                    End If

                    If impresoradestino = "" Then
                    Else
                        Dim config As New System.Drawing.Printing.PrinterSettings
                        config.PrinterName = impresoradestino
                        'config.DefaultPageSettings.PaperSize = New System.Drawing.Printing.PaperSize("banda", 300, (21 * 12.59) + (12.59 * 2 * tabladetalle.Rows.Count) + 25)
                        'config.DefaultPageSettings.Margins = New System.Drawing.Printing.Margins(5, 5, 5, 5)
                        RawPrinterHelper.SendFileToPrinter(config.PrinterName, path & nombarch)
                        config = Nothing
                    End If

                Else
                    If System.IO.Directory.Exists("c:\puente\temp") Then
                    Else
                        System.IO.Directory.CreateDirectory("c:\puente\temp")
                    End If
                    System.IO.File.Copy(path & nombarch, "c:\puente\temp\" & nombarch, True)
                    Shell("c:\command.com /c copy c:\puente\temp\" & nombarch & " lpt1 > NULL")
                End If
            Catch ex As Exception
                System.Windows.Forms.MessageBox.Show("Ocurrio un error al imprimir " & ex.Message, "Impresion", Windows.Forms.MessageBoxButtons.OK, Windows.Forms.MessageBoxIcon.Exclamation)
            End Try

             */
            
            try
            {
                string path_file = "C:\\laragon\\www\\siawbackend-.net\\SIAW\\bin\\Debug\\net6.0\\OutputFiles\\temp\\r31446.txt";
                string impresoradestino = "EPSON LX-350";
                // ImprimirArchivo(path_file, "");

                // Crear un nuevo objeto PrinterSettings
                System.Drawing.Printing.PrinterSettings config = new System.Drawing.Printing.PrinterSettings();

                // Asignar el nombre de la impresora
                config.PrinterName = impresoradestino;

                // Comprobar si la impresora está instalada
                if (config.IsValid)
                {
                    // Configurar e iniciar el trabajo de impresión
                    // Aquí iría el código para configurar el documento a imprimir y lanzar la impresión
                    RawPrinterHelper.SendFileToPrinter(config.PrinterName, path_file);
                    //imprimirDatos.SendFileToPrinter(config.PrinterName, path_file);
                }
                else
                {
                    return BadRequest("La impresora no está disponible.");
                }

                //imprimirDatos.SendFileToPrinter("EPSON LX-350", path_file);
            }
            catch (Exception)
            {

                throw;
            }

            return Ok("funciona");

        }
        private string filePath;


        private void ImprimirArchivo(string path, string impresora = "")
        {
            try
            {
                string impresoradestino;

                // Verificar si hay impresoras instaladas en el equipo
                PrintDocument prtdoc = new PrintDocument();
                if (PrinterSettings.InstalledPrinters.Count == 0)
                {
                    throw new Exception("No hay impresoras instaladas.");
                }
                prtdoc.Dispose();

                if (string.IsNullOrEmpty(impresora))
                {
                    impresoradestino = "EPSON LX-350";
                    // Mostrar dialogo para elegir impresora (Implementar tu lógica para elegir impresora)
                    // impresoradestino = ElegirImpresora();
                }
                else
                {
                    impresoradestino = impresora;
                }

                if (!string.IsNullOrEmpty(impresoradestino))
                {
                    this.filePath = path;
                    PrintDocument printDoc = new PrintDocument();
                    printDoc.PrinterSettings.PrinterName = impresoradestino;
                    printDoc.PrintPage += new PrintPageEventHandler(PrintPage);

                    // Configurar el tamaño de la hoja y márgenes en 0
                    printDoc.DefaultPageSettings.PaperSize = new PaperSize("Custom", 816, 1056); // Carta en píxeles, ajusta según necesidad
                    printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

                    try
                    {
                        printDoc.Print();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error al imprimir: " + ex.Message);
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine( ex.Message);
            }
        }

        private int currentLine = 0; // Keeps track of the current line to print
        private void PrintPage(object sender, PrintPageEventArgs ev)
        {
            float linesPerPage = 0;
            float yPos = 0;
            int count = 0;
            float leftMargin = ev.MarginBounds.Left;
            float topMargin = ev.MarginBounds.Top;
            string line = null;
            Font printFont = new Font("Draft", 10);
            SolidBrush printBrush = new SolidBrush(Color.Black);

            // Calculate the number of lines per page.
            linesPerPage = ev.MarginBounds.Height / printFont.GetHeight(ev.Graphics);

            // Open the file and create a StreamReader.
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (StreamReader sr = new StreamReader(fs))
            {
                // Skip to the current line to print (for subsequent pages).
                for (int i = 0; i < currentLine && sr.ReadLine() != null; i++) { }

                while ((line = sr.ReadLine()) != null)
                {
                    yPos = topMargin + (count * printFont.GetHeight(ev.Graphics));
                    ev.Graphics.DrawString(line, printFont, printBrush, leftMargin, yPos, new StringFormat());
                    count++;

                    // Check if we've reached the bottom of the page.
                    if (count >= linesPerPage)
                    {
                        // More lines to print, so set HasMorePages to true
                        ev.HasMorePages = true;
                        currentLine += count; // Update the line counter for the next page
                        return;
                    }
                }
            }

            // No more lines to print
            ev.HasMorePages = false;
            currentLine = 0; // Reset for future print jobs
        }


        private void PrintPage_1(object sender, PrintPageEventArgs ev)
        {
            float linesPerPage = 0;
            float yPos = 0;
            int count = 0;
            float leftMargin = 0;
            float topMargin = 0;
            string line = null;
            Font printFont = new Font("Draft", 10);

            // Calculate the number of lines per page.
            linesPerPage = ev.MarginBounds.Height / printFont.GetHeight(ev.Graphics);

            // Read the file and print it line by line.
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (StreamReader sr = new StreamReader(fs))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    yPos = topMargin + (count * printFont.GetHeight(ev.Graphics));
                    ev.Graphics.DrawString(line, printFont, Brushes.Black, leftMargin, yPos, new StringFormat());
                    count++;

                    if (count >= linesPerPage)
                    {
                        ev.HasMorePages = true;
                        return;
                    }
                }
            }

            ev.HasMorePages = false;
        }  



        private string ElegirImpresora()
        {
            Console.WriteLine("Impresoras instaladas:");

            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                Console.WriteLine(printer);
            }

            Console.WriteLine("Presiona cualquier tecla para salir.");
            Console.ReadKey();
            return "";
        }

        /*
        private void RegistrarError(string mensaje)
        {
            // Implementa tu lógica para registrar errores aquí
            // Por ejemplo, podrías escribir el error en un archivo de log
            File.AppendAllText("log_errores.txt", $"{DateTime.Now}: {mensaje}{Environment.NewLine}");
        }
        */
    }
}
