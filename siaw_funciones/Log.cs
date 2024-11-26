using siaw_DBContext.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using siaw_DBContext.Models;
using System.Drawing;

namespace siaw_funciones
{
    public class Log
    {
        datosProforma datosProforma = new datosProforma();
        public async Task<bool> RegistrarEvento(DBContext _context, string usuario, Entidades entidad, string codigo, string id_doc, string numeroid_doc, string ventana, string detalle, TipoLog tipo)
        {
            string fecha = datosProforma.getFechaActual();
            string hora = datosProforma.getHoraActual();
            selog datos = new selog();
            datos.usuario = usuario;
            datos.fecha = DateTime.Parse(fecha);
            datos.hora = hora;
            datos.entidad = entidad.ToString();
            datos.codigo = codigo;
            datos.id_doc = id_doc;
            datos.numeroid_doc = numeroid_doc;
            datos.ventana = ventana;
            datos.detalle = detalle;
            datos.tipo = tipo.ToString();
            _context.selog.Add(datos);
            await _context.SaveChangesAsync();
            return true;

        }

        public async Task<bool> RegistrarEvento_Siat(DBContext _context, string usuario, Entidades entidad, string codigo, string id_doc, string numeroid_doc, string ventana, string detalle, TipoLog_Siat tipo)
        {
            string fecha = datosProforma.getFechaActual();
            string hora = datosProforma.getHoraActual();
            selog_siat datos = new selog_siat();
            datos.usuario = usuario;
            datos.fecha = DateTime.Parse(fecha);
            datos.hora = hora;
            datos.entidad = entidad.ToString();
            datos.codigo = codigo;
            datos.id_doc = id_doc;
            datos.numeroid_doc = numeroid_doc;
            datos.ventana = ventana;
            datos.detalle = detalle;
            datos.tipo = tipo.ToString();
            _context.selog_siat.Add(datos);
            await _context.SaveChangesAsync();
            return true;

        }

        public enum Entidades
        {
            SW_Nota_Movimiento = 0,
            SW_Proforma,
            SW_Nota_Remision,
            SW_Factura,
            SW_Comprobante,
            SW_Cobranza,
            SW_Cobranza_Contado,
            SW_Anticipo,
            SW_Ventana,
            SW_Ruta,
            SW_Flete,
            SW_Flete_Devolucion,
            SW_Pedido,
            SW_Solicitud_Creacion_Cliente,
            SW_Deposito_Cliente,
            SW_Facturacion_Eletronica_Linea
        }
        public enum TipoLog
        {
            Creacion = 0,
            Modificacion,
            Anulacion,
            Habilitacion,
            Eliminacion,
            Administracion,
            Apertura,
            Cierre,
            Ejecucion,
            Autorizacion,
            Edicion
        }
        public enum TipoLog_Siat
        {
            Creacion = 0,
            Envio_Factura,
            Envio_Paquete_Facturas,
            Anulacion_Facturas,
            Modificacion,
            Modificacion_Estado_En_Linea,
            Estado_Facturas,
            Validar_Nit
        }
    }

    
}
