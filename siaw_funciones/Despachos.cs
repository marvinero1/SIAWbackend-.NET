using siaw_DBContext.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using siaw_DBContext.Models;

namespace siaw_funciones
{
    public class Despachos
    {
        private readonly siaw_funciones.datosProforma datos_proforma = new siaw_funciones.datosProforma();
        private readonly siaw_funciones.Funciones funciones = new Funciones();

        public async Task<bool> eliminar_prof_de_despachos(DBContext _context, string id, int nroid)
        {
			try
			{
                // elimina la proforma de la tabla de despachos
                var result = await _context.vedespacho.Where(i => i.id == id && i.nroid == nroid).FirstOrDefaultAsync();
                if (result != null)
                {
                    _context.vedespacho.Remove(result);
                    await _context.SaveChangesAsync();
                }
                return true;
            }
			catch (Exception)
			{
                return false;
			}
        }
        public async Task<bool> proforma_en_despachos(DBContext _context, string id, int nroid)
        {
            var tbl = await _context.vedespacho.Where(i => i.id == id && i.nroid == nroid).CountAsync();
            if (tbl > 0)
            {
                return true;
            }
            return false;
        }
        public async Task<bool> cadena_insertar_log_estado_pedido(DBContext _context, string idprof, int nroidprof, string estado, string usuario)
        {
            // la funcion genera una cadena de insercion en el velog_estado_pedido
            velog_estado_pedido newRegistro = new velog_estado_pedido();
            newRegistro.idproforma = idprof;
            newRegistro.nroidproforma = nroidprof;
            newRegistro.estado = estado;
            newRegistro.horareg = datos_proforma.getHoraActual();
            newRegistro.fechareg = await funciones.FechaDelServidor(_context);
            newRegistro.usuarioreg = usuario;
            _context.velog_estado_pedido.Add(newRegistro);
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        }

    }
}
