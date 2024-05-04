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
        public enum Entidades
        {
            Nota_Movimiento = 0,
            Proforma,
            Nota_Remision,
            Factura,
            Comprobante,
            Cobranza,
            Cobranza_Contado,
            Anticipo,
            Ventana,
            Ruta,
            Flete,
            Flete_Devolucion,
            Pedido,
            Solicitud_Creacion_Cliente,
            Deposito_Cliente,
            Facturacion_Eletronica_Linea
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
    }

    
}
