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
    public class Contabilidad
    {
        public async Task<bool> AsignarCuentasCliente(DBContext _context, string codcliente)
        {
            var result = await _context.vecliente_conta
                    .Where(v => v.codcliente == codcliente)
                    .ToListAsync();
            // si existen datos se eliminan
            if (result.Count() > 0)
            {
                _context.vecliente_conta.RemoveRange(result);
                await _context.SaveChangesAsync();
            }

            var codigoCliente = _context.vecliente.OrderBy(c => c.codigo).Select(c => c.codigo).FirstOrDefault();

            var nuevaFila = _context.vecliente_conta
                .Where(vc => vc.codcliente == codigoCliente)
                .Select(vc => new vecliente_conta
                {
                    codcliente = codcliente,
                    codunidad = vc.codunidad,
                    cta_vtacontado = vc.cta_vtacontado,
                    cta_vtacontado_aux = vc.cta_vtacontado_aux,
                    cta_vtacontado_cc = vc.cta_vtacontado_cc,

                    cta_vtacredito = vc.cta_vtacredito,
                    cta_vtacredito_aux = vc.cta_vtacredito_aux,
                    cta_vtacredito_cc = vc.cta_vtacredito_cc,
                    cta_porcobrar = vc.cta_porcobrar,
                    cta_porcobrar_aux = vc.cta_porcobrar_aux,

                    cta_porcobrar_cc = vc.cta_porcobrar_cc,
                    cta_iva = vc.cta_iva,
                    cta_iva_aux = vc.cta_iva_aux,
                    cta_iva_cc = vc.cta_iva_cc,
                    codalmacen = vc.codalmacen,

                    cta_ivadebito = vc.cta_ivadebito,
                    cta_ivadebito_aux = vc.cta_ivadebito_aux,
                    cta_ivadebito_cc = vc.cta_ivadebito_cc,
                    cta_anticipo = vc.cta_anticipo,
                    cta_anticipo_aux = vc.cta_anticipo_aux,

                    cta_anticipo_cc = vc.cta_anticipo_cc,
                    cta_anticipo_contado = vc.cta_anticipo_contado,
                    cta_anticipo_contado_aux = vc.cta_anticipo_contado_aux,
                    cta_anticipo_contado_cc = vc.cta_anticipo_contado_cc,
                    cta_diftdc = vc.cta_diftdc,

                    cta_diftdc_aux = vc.cta_diftdc_aux,
                    cta_diftdc_cc = vc.cta_diftdc_cc
                })
                .FirstOrDefault();

            _context.vecliente_conta.Add(nuevaFila);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
