using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using siaw_DBContext.Models;
using siaw_DBContext.Data;

namespace siaw_funciones
{
    public class impresoraTermica
    {
        /*
        private readonly Funciones funciones = new Funciones();
        private readonly Contabilidad contabilidad = new Contabilidad();
        private readonly Nombres nombres = new Nombres();
        private readonly Adsiat_Parametros_facturacion adsiat_Parametros_Facturacion = new Adsiat_Parametros_facturacion();

        public void ImprimirTexto(DBContext _context, string codempresa, string nombreImpresora, Font fuente, vefactura cabecera, List<vefactura1> detalle)
        {
            PrintDocument pd = new PrintDocument
            {
                PrinterSettings = { PrinterName = nombreImpresora }
            };

            if (!pd.PrinterSettings.IsValid)
            {
                throw new Exception("La impresora no está disponible o no es válida.");
            }

            pd.PrintPage += async (sender, e) =>
            {
                // Definir las coordenadas para imprimir
                float x = 10;
                float y = 4;
                int NC = 39;  // Ancho del área de impresión
                float lineOffset;

                // Imprimir el texto pasado como parámetro
                // Configuración de fuentes
                Font tituloFont = new Font("Arial", 10, FontStyle.Bold, GraphicsUnit.Point);
                Font subTituloFont = new Font("Arial", 8, FontStyle.Bold, GraphicsUnit.Point);

                // Título "FACTURA"
                lineOffset = tituloFont.GetHeight(e.Graphics);
                string titulo = funciones.CentrarCadena("FACTURA", NC, " ");
                e.Graphics.DrawString(titulo, tituloFont, Brushes.Black, x, y);
                y += lineOffset;

                // Subtítulo "CON DERECHO A CREDITO FISCAL"
                lineOffset = subTituloFont.GetHeight(e.Graphics);
                string subTitulo = funciones.CentrarCadena("CON DERECHO A CREDITO FISCAL", NC, " ");
                e.Graphics.DrawString(subTitulo, subTituloFont, Brushes.Black, x, y);
                y += lineOffset + 10; // Espacio adicional después del subtítulo



                Font printFont = new Font("Arial", 10, FontStyle.Bold, GraphicsUnit.Point);
                // Nombre de la empresa
                string cadena = funciones.CentrarCadena(await nombres.nombreempresa(_context, codempresa), NC, " ");
                e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);

                // Ajuste para la siguiente línea
                printFont = new Font("Consolas", 7, FontStyle.Regular, GraphicsUnit.Point);
                lineOffset = printFont.GetHeight(e.Graphics);
                NC = 49;
                y += lineOffset + 10;

                // Casa Matriz
                cadena = funciones.CentrarCadena("Casa Matriz", NC, " ");
                e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                y += lineOffset;

                // Dirección General Acha N°330
                cadena = funciones.CentrarCadena("Gral. Acha N°330", NC, " ");
                e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                y += lineOffset;

                // Ciudad
                cadena = funciones.CentrarCadena("Cochabamba-Bolivia", NC, " ");
                e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                y += lineOffset;

                // Teléfonos
                cadena = funciones.CentrarCadena("Telfs.: 4259660 Fax:4111282", NC, " ");
                e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                y += lineOffset * 2;

                // Sucursal
                cadena = funciones.CentrarCadena("Sucursal Nro.: " + await contabilidad.sucursalalm(_context,cabecera.codalmacen), NC, " ");
                e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                y += lineOffset;

                // Punto de Venta
                cadena = funciones.CentrarCadena("Punto de Vta.: " + sia_DAL.adsiat_parametros_facturacion.Instancia.PuntoDeVta(cabecera.codalmacen), NC, " ");
                e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                y += lineOffset;

                // Dirección del Almacén
                cadena = funciones.CentrarCadena(sia_funciones.Almacen.Instancia.direccionalmacen(cabecera.codalmacen), NC, " ");
                e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                y += lineOffset;

                // Teléfono del Almacén
                cadena = funciones.CentrarCadena("Telefono: " + sia_funciones.Almacen.Instancia.telefonoalmacen(cabecera.codalmacen), NC, " ");
                e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                y += lineOffset;

                // Fax del Almacén (solo si existe)
                string faxAg = sia_funciones.Almacen.Instancia.faxalmacen(cabecera.codalmacen);
                if (!string.IsNullOrEmpty(faxAg) && faxAg != "0")
                {
                    cadena = funciones.CentrarCadena("Fax: " + faxAg, NC, " ");
                    e.Graphics.DrawString(cadena, printFont, Brushes.Black, x, y);
                    y += lineOffset;
                }
                else
                {
                    y += lineOffset;
                }


            };

            pd.Print();
        }

        */
    }
}
