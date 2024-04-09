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
    public class Configuracion
    {

        public async Task<int> emp_coddesextra_x_deposito(DBContext _context, string codempresa)
        {
            var result = await _context.adparametros
                    .Where(v => v.codempresa == codempresa)
                    .Select(parametro => parametro.coddesextra_x_deposito)
                    .FirstOrDefaultAsync();
            if (result == null)
            {
                return 0;
            }
            return (int)result;
        }
        public async Task<int> emp_coddesextra_x_deposito_context(DBContext _context, string codempresa)
        {
            try
            {
                int resultado = 0;
                //using (_context )
                //{
                var result = await _context.adparametros
                    .Where(v => v.codempresa == codempresa)
                    .Select(parametro => parametro.coddesextra_x_deposito)
                   .FirstOrDefaultAsync();
                if (result != null)
                {
                    resultado = (int)result;
                }
                return resultado;
                //}
            }
            catch (Exception)
            {
                return 0;
            }
        }
        public async Task<int> emp_coddesextra_x_deposito_contado(DBContext _context, string codempresa)
        {
            var result = await _context.adparametros
                    .Where(v => v.codempresa == codempresa)
                    .Select(parametro => parametro.coddesextra_x_deposito_contado)
                    .FirstOrDefaultAsync();
            if (result == null)
            {
                return 0;
            }
            return (int)result;
        }
        public async Task<int> emp_codrecargo_x_deposito(DBContext _context, string codempresa)
        {
            var result = await _context.adparametros
                    .Where(v => v.codempresa == codempresa)
                    .Select(parametro => parametro.coddesextra_x_deposito)
                    .FirstOrDefaultAsync();
            if (result == null)
            {
                return 0;
            }
            return (int)result;
        }
        public async Task<int> emp_codrecargo_pedido_urgente_provincia(DBContext _context, string codempresa)
        {
            var result = await _context.adparametros
                .Where(v => v.codempresa == codempresa)
                .Select(parametro => parametro.codrecargo_pedido_urgente_provincia)
                .FirstOrDefaultAsync();
            if (result == null)
            {
                return 0;
            }
            return (int)result;
        }
        public async Task<bool> emp_hab_descto_x_deposito(DBContext _context, string codempresa)
        {
            var result = await _context.adparametros
                    .Where(v => v.codempresa == codempresa)
                    .Select(parametro => parametro.hab_descto_x_deposito)
                    .FirstOrDefaultAsync() ??true;

            return result;
        }
        public async Task<DateTime> Depositos_Nuevos_Desde_Fecha(DBContext _context)
        {
            //esta fecha es la que se empezo con los descuentos por deposito
            var result = await _context.adparametros
                    .Select(parametro => parametro.nuevos_depositos_desde)
                    .FirstOrDefaultAsync() ?? new DateTime(2015, 5, 13);

            return result;
        }
        public async Task<bool> emp_clientevendedor(DBContext _context, string codempresa)
        {
            try
            {
                bool resultado = false;
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var result = await _context.adparametros
                    .Where(v => v.codempresa == codempresa)
                    .Select(parametro => parametro.clientevendedor)
                   .FirstOrDefaultAsync();
                if (result != null)
                {
                    resultado = (bool)result;
                }
                return resultado;
                //}
            }
            catch (Exception)
            {
                return false;
            }
        }
        public async Task<int> Dias_Proforma_Vta_Item_Cliente(DBContext _context, string codempresa)
        {
            try
            {
                int resultado = 0;
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var result = await _context.adparametros
                    .Where(v => v.codempresa == codempresa)
                    .Select(parametro => parametro.dias_proforma_vta_item_cliente)
                   .FirstOrDefaultAsync();
                if (result != null)
                {
                    resultado = (int)result;
                }
                return resultado;
                //}
            }
            catch (Exception)
            {
                return 0;
            }
        }
        public async Task<string> Valida_Maxvta_NR_PF(DBContext _context, string codempresa)
        {
            try
            {
                string resultado = "";
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var result = await _context.adparametros
                    .Where(v => v.codempresa == codempresa)
                    .Select(parametro => parametro.valida_max_vta_nr_pf)
                   .FirstOrDefaultAsync();
                if (result != null)
                {
                    resultado = (string)result;
                }
                return resultado;
                //}
            }
            catch (Exception)
            {
                return "NN";
            }
        }
    }
}
