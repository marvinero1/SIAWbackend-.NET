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
        private TipoCambio tipocambio= new TipoCambio();
        private Funciones funciones= new Funciones();
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
                    resultado = result ?? 0;
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
        public async Task<bool> emp_permitir_facturas_sin_nombre(DBContext _context, string codempresa)
        {
            var result = await _context.adparametros
                    .Where(v => v.codempresa == codempresa)
                    .Select(parametro => parametro.permitir_facturas_sn)
                    .FirstOrDefaultAsync() ?? false;

            return result;
        }
        public async Task<int> codempaque_permite_item_repetido(DBContext _context, string codempresa)
        {
            try
            {
                int resultado = 0;
                //using (_context )
                //{
                var result = await _context.adparametros
                    .Where(v => v.codempresa == codempresa)
                    .Select(parametro => parametro.codempaque_permite_item_repetido)
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
        public async Task<DateTime> Depositos_Nuevos_Desde_Fecha(DBContext _context)
        {
            //esta fecha es la que se empezo con los descuentos por deposito
            var result = await _context.adparametros
                    .Select(parametro => parametro.nuevos_depositos_desde)
                    .FirstOrDefaultAsync() ?? new DateTime(2015, 5, 13);

            return result;
        }

        public async Task<int> Dias_Revision_Desctos_Deposito_No_Facturados(DBContext _context)
        {
            //esta fecha es la que se empezo con los descuentos por deposito
            int result = await _context.adparametros
                    .Select(parametro => parametro.dias_revision_depositos_no_facturados)
                    .FirstOrDefaultAsync() ?? 0;

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

        public async Task<int> dias_mora_limite(DBContext _context, string codempresa)
        {
            try
            {
                int resultado = 0;
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var result = await _context.adparametros
                    .Where(v => v.codempresa == codempresa)
                    .Select(parametro => parametro.dias_mora_limite)
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
                return 3;
            }
        }

        public async Task<double> monto_maximo_facturas_sin_nombre(DBContext _context, string codempresa)
        {
            try
            {
                int resultado = 0;
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var result = await _context.adparametros
                    .Where(v => v.codempresa == codempresa)
                    .Select(parametro => parametro.monto_maximo_facturas_sin_nombre)
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
                return 9999;
            }
        }

        public async Task<decimal> monto_maximo_mora_clientes(DBContext _context, string codempresa, string moneda, int codalmacen)
        {
            decimal resultado = 3;
            try
            {
                var parametros = _context.adparametros.FirstOrDefault(p => p.codempresa == codempresa);
                if (parametros != null)
                {
                    if (string.IsNullOrWhiteSpace(parametros.codmoneda_monto_max_mora))
                    {
                        //MessageBox.Show("La moneda del monto máximo de mora permitido por cuota no está definida en los parámetros. Corrija esta situación.", "Validar Datos", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        resultado = 100;
                    }
                    else
                    {
                        if (parametros.codmoneda_monto_max_mora == moneda)
                        {
                            resultado = (decimal)parametros.monto_maximo_mora;
                        }
                        else
                        {
                            resultado = await tipocambio._conversion_alm(_context, moneda, parametros.codmoneda_monto_max_mora, await funciones.FechaDelServidor(_context), (decimal)parametros.monto_maximo_mora, codalmacen);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                resultado = 1; // Indicar un error
            }
            return resultado;
        }

        public async Task<string> moneda_monto_minimo_venta_urgente_provincia(DBContext _context, string codempresa)
        {
            try
            {
                string resultado = "";
                var result = await _context.adparametros
                    .Where(v => v.codempresa == codempresa)
                    .Select(parametro => parametro.moneda_monto_min_urg_provincia)
                   .FirstOrDefaultAsync();
                if (result != null)
                {
                    resultado = result;
                }
                return resultado;
            }
            catch (Exception)
            {
                return "";
            }
        }

        public async Task<string> moneda_monto_minimo_venta_cliente_en_oficina(DBContext _context, string codempresa)
        {
            try
            {
                string resultado = "";
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var result = await _context.adparametros
                    .Where(v => v.codempresa == codempresa)
                    .Select(parametro => parametro.moneda_monto_min_vta_cliente_oficina)
                   .FirstOrDefaultAsync();
                if (result != null)
                {
                    resultado = result;
                }
                return resultado;
                //}
            }
            catch (Exception)
            {
                return "";
            }
        }

        public async Task<double> emp_monto_rnd100011(DBContext _context, string codempresa)
        {
            double resultado = 0;
            try
            {
                var result = await _context.adparametros
                    .Where(v => v.codempresa == codempresa)
                    .Select(parametro => parametro.monto_rnd100011)
                   .FirstOrDefaultAsync();
                if (result != null)
                {
                    resultado = (double)result;
                }
            }
            catch (Exception)
            {
                resultado = 0;
            }
            return resultado;
        }

        public async Task<int> empaque_venta_cliente_en_oficina(DBContext _context, string codempresa)
        {
            try
            {
                int resultado = 0;
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var result = await _context.adparametros
                    .Where(v => v.codempresa == codempresa)
                    .Select(parametro => parametro.codempaque_venta_cliente_oficina)
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
                return 3;
            }
        }

        public async Task<int> Nro_Empaques_Minimo_Vta_Oficina(DBContext _context, string codempresa)
        {
            try
            {
                int resultado = 0;
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var result = await _context.adparametros
                    .Where(v => v.codempresa == codempresa)
                    .Select(parametro => parametro.cantidad_empaques_venta_cliente_oficina)
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
                return 3;
            }
        }
        public async Task<int> Nro_Items_Maximo_Conteo_Vta_Oficina(DBContext _context, string codempresa)
        {
            try
            {
                int resultado = 0;
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var result = await _context.adparametros
                    .Where(v => v.codempresa == codempresa)
                    .Select(parametro => parametro.maximo_items_conteo_vta_cliente_oficina)
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
                return 3;
            }
        }

        public async Task<double> monto_minimo_venta_cliente_en_oficina(DBContext _context, string codempresa)
        {
            try
            {
                double resultado = 0;
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var result = await _context.adparametros
                    .Where(v => v.codempresa == codempresa)
                    .Select(parametro => parametro.monto_min_vta_cliente_oficina)
                   .FirstOrDefaultAsync();
                if (result != null)
                {
                    resultado = (double)result;
                }
                return resultado;
                //}
            }
            catch (Exception)
            {
                return 9999;
            }
        }

        public async Task<double> monto_minimo_venta_urgente_provincia(DBContext _context, string codempresa)
        {
            try
            {
                double resultado = 0;
                var result = await _context.adparametros
                    .Where(v => v.codempresa == codempresa)
                    .Select(parametro => parametro.monto_min_urg_provincia)
                   .FirstOrDefaultAsync();
                if (result != null)
                {
                    resultado = (double)result;
                }
                return resultado;
            }
            catch (Exception)
            {
                return 9999;
            }
        }

        public async Task<double> porcentaje_sugerencia_empaque(DBContext _context, string codempresa)
        {
            double resultado = 9999;
            try
            {

                var result = await _context.adparametros
                    .Where(v => v.codempresa == codempresa)
                    .Select(parametro => parametro.porcentaje_sugerencia_empaque)
                   .FirstOrDefaultAsync();
                if (result != null)
                {
                    resultado = (double)result;
                }
                return resultado;
            }
            catch (Exception)
            {
                return 9999;
            }
        }

        public async Task<int> getemp_numeracion_clientes_desde(DBContext _context)
        {
            var result = await _context.adparametros
                    .Select(parametro => parametro.numeracion_clientes_desde)
                    .FirstOrDefaultAsync();

            return (int)result;
        }
        public async Task<int> getemp_numeracion_clientes_hasta(DBContext _context)
        {
            var result = await _context.adparametros
                    .Select(parametro => parametro.numeracion_clientes_hasta)
                    .FirstOrDefaultAsync();

            return (int)result;
        }
        public async Task<bool> emp_proforma_reserva(DBContext _context, string empresa)
        {
            var result = await _context.adparametros
                .Where(i => i.codempresa == empresa)
                .Select(parametro => parametro.proforma_reserva)
                .FirstOrDefaultAsync() ?? false;

            return result;
        }
        public async Task<bool> Permitir_Añadir_Creditos_Temporales_Automatico(DBContext _context, string codempresa)
        {
            var result = await _context.adparametros
                .Where(i => i.codempresa == codempresa)
                .Select(parametro => parametro.añadir_creditos_temporales_automaticos)
                .FirstOrDefaultAsync() ?? false;

            return result;
        }
        public async Task<bool> emp_aplica_ajustes_en_descto_deposito(DBContext _context, string adparametros)
        {
            bool result = await _context.adparametros
                .Where(i => i.codempresa == adparametros)
                    .Select(parametro => parametro.aplicar_ajuste_descdeposito)
                    .FirstOrDefaultAsync() ?? false;

            return result;
        }

        public async Task<double> DuracionHabilAsync(DBContext _context, int almacen, DateTime desde, DateTime hasta)
        {
            DateTime fechaAux;

            if (almacen.ToString().Trim() == "")
            {
                return 1;
            }
            else
            {
                // Verificar si la fecha desde es menor a hasta y si no invertir fechas
                if (desde < hasta)
                {
                    // desde = desde;
                    // hasta = hasta;
                }
                else
                {
                    fechaAux = desde;
                    desde = hasta;
                    hasta = fechaAux;
                }

                double duracion = hasta.Date.Subtract(desde.Date).TotalDays + 1;
                // Descontar los días que no eran días hábiles
                int noHabiles;
                noHabiles = await _context.penohabil
                    .CountAsync(ph => ph.codalmacen == almacen &&
                                      ph.fecha >= desde.Date &&
                                      ph.fecha <= hasta.Date);
                duracion -= noHabiles;
                if (duracion < 0)
                {
                    duracion = 0;
                }

                return duracion;
            }
        }

        public async Task<string> usr_idremision(DBContext _context, string usuario)
        {
            try
            {
                var result = await _context.adusparametros
                    .Where(parametro => parametro.usuario == usuario)
                    .Select(i => new
                    {
                        i.idremision
                    })
                    .FirstOrDefaultAsync();
                if (result == null)
                {
                    return "";
                }
                return result.idremision;
            }
            catch (Exception)
            {
                return "";
            }
        }
        public async Task<int> usr_codvendedor(DBContext _context, string usuario)
        {
            try
            {
                var result = await _context.adusparametros
                    .Where(parametro => parametro.usuario == usuario)
                    .Select(i => new
                    {
                        i.codvendedor
                    })
                    .FirstOrDefaultAsync();
                if (result == null)
                {
                    return 0;
                }
                return result.codvendedor ?? 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }
        public async Task<string> usr_codmoneda(DBContext _context, string usuario)
        {
            try
            {
                var result = await _context.adusparametros
                    .Where(parametro => parametro.usuario == usuario)
                    .Select(i => new
                    {
                        i.codmoneda
                    })
                    .FirstOrDefaultAsync();
                if (result == null)
                {
                    return "";
                }
                return result.codmoneda;
            }
            catch (Exception)
            {
                return "";
            }
        }
        public async Task<int> usr_codalmacen(DBContext _context, string usuario)
        {
            try
            {
                var result = await _context.adusparametros
                    .Where(parametro => parametro.usuario == usuario)
                    .Select(i => new
                    {
                        i.codalmacen
                    })
                    .FirstOrDefaultAsync();
                if (result == null)
                {
                    return 0;
                }
                return result.codalmacen ?? 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }
        public async Task<int> usr_codtarifa(DBContext _context, string usuario)
        {
            try
            {
                var result = await _context.adusparametros
                    .Where(parametro => parametro.usuario == usuario)
                    .Select(i => new
                    {
                        i.codtarifa
                    })
                    .FirstOrDefaultAsync();
                if (result == null)
                {
                    return 0;
                }
                return result.codtarifa ?? 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }
        public async Task<int> usr_coddescuento(DBContext _context, string usuario)
        {
            try
            {
                var result = await _context.adusparametros
                    .Where(parametro => parametro.usuario == usuario)
                    .Select(i => new
                    {
                        i.coddescuento
                    })
                    .FirstOrDefaultAsync();
                if (result == null)
                {
                    return 0;
                }
                return result.coddescuento ?? 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }
        public async Task<int> emp_codplanpago_reversion(DBContext _context, string codempresa)
        {
            var result = await _context.adparametros
                    .Where(v => v.codempresa == codempresa)
                    .Select(parametro => parametro.codplanpago_reversion)
                    .FirstOrDefaultAsync();
            if (result == null)
            {
                return 0;
            }
            return (int)result;
        }

        public async Task<bool> usr_ver_columna_empaques(DBContext _context, string usuario)
        {
            try
            {
                var result = await _context.adusparametros
                    .Where(v => v.usuario == usuario)
                    .Select(i => new { i.ver_columna_empaques })
                    .FirstOrDefaultAsync();
                if (result == null)
                {
                    return false;
                }
                if (result.ver_columna_empaques == true)
                {
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public async Task<bool> distribuir_descuentos_en_facturacion(DBContext _context, string empresa)
        {
            // se implemento esto para la version del sia del 10-03-2019
            // si este parametro es TRUE quiere decir que los descuentos extras se proratean en la factura cuando se genera es la forma como 
            bool resultado = true;
            try
            {
                resultado = await _context.adparametros.Where(i => i.codempresa == empresa).Select(i => i.distribuir_desc_extra_en_factura).FirstOrDefaultAsync() ?? false;
            }
            catch (Exception)
            {
                resultado = true;
            }
            return resultado;
        }
        public async Task<bool> usr_usar_bd_opcional(DBContext _context, string usuario)
        {
            try
            {
                bool resultado = await _context.adusparametros.Where(i => i.usuario == usuario).Select(i => i.usar_bd_opcional).FirstOrDefaultAsync();
                return resultado;
            }
            catch (Exception)
            {
                return false;
            }
        }


    }
}
 