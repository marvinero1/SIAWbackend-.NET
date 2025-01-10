using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;

namespace siaw_funciones
{
    public class Empresa
    {
        //Clase necesaria para el uso del DBContext del proyecto siaw_Context
        private readonly TipoCambio tipocambio = new TipoCambio();
        public static class DbContextFactory
        {
            public static DBContext Create(string connectionString)
            {
                var optionsBuilder = new DbContextOptionsBuilder<DBContext>();
                optionsBuilder.UseSqlServer(connectionString);

                return new DBContext(optionsBuilder.Options);
            }
        }
        public async Task<bool> ControlarStockSeguridad(string userConnectionString, string codigoempresa)
        {
            bool resultado;
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                //precio unitario del item
                var stock_seguridad = await _context.adparametros
                    .Where(i => i.codempresa == codigoempresa)
                    .Select(i => i.stock_seguridad)
                    .FirstOrDefaultAsync() ?? false;
                resultado = stock_seguridad;
            }
            return resultado;
        }
        public async Task<bool> ControlarStockSeguridad_context(DBContext _context, string codigoempresa)
        {
            bool resultado;
            //using (var _context = DbContextFactory.Create(userConnectionString))
            //{
            //precio unitario del item
            var stock_seguridad = await _context.adparametros
                .Where(i => i.codempresa == codigoempresa)
                .Select(i => i.stock_seguridad)
                .FirstOrDefaultAsync();
            resultado = (bool)stock_seguridad;
            // }
            return resultado;
        }

        public async Task<int> CodAlmacen(string userConnectionString, string codigoempresa)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                //precio unitario del item
                int codalmacen = (int)await _context.adempresa
                    .Where(i => i.codigo == codigoempresa)
                    .Select(i => i.codalmacen)
                    .FirstOrDefaultAsync();
                return codalmacen;
            }
        }

        public async Task<int> AlmacenLocalEmpresa(DBContext _context, string codigoempresa)
        {
            //precio unitario del item
            int codalmacen = (int)await _context.adempresa
                .Where(i => i.codigo == codigoempresa)
                .Select(i => i.codalmacen)
                .FirstOrDefaultAsync();
            return codalmacen;
        }
        public async Task<int> AlmacenLocalEmpresa_context(DBContext _context, string codigoempresa)
        {
            //using (var _context = DbContextFactory.Create(userConnectionString))
            //{
            //precio unitario del item
            int codalmacen = (int)await _context.adempresa
                .Where(i => i.codigo == codigoempresa)
                .Select(i => i.codalmacen)
                .FirstOrDefaultAsync();
            return codalmacen;
            //}
        }
        public static async Task<string> monedabase(DBContext _context, string codigoempresa)
        {
            //Esta funcion devuelve la moneda base de una determinada empresa
            string codmoneda = await _context.adempresa
                .Where(i => i.codigo == codigoempresa)
                .Select(i => i.moneda)
                .FirstOrDefaultAsync() ?? "";
            return codmoneda;
        }
        public async Task<string> monedaext(DBContext _context, string codigoempresa)
        {
            //Esta funcion devuelve la moneda base de una determinada empresa
            string codmoneda = await _context.adparametros
                .Where(i => i.codempresa == codigoempresa)
                .Select(i => i.monedae)
                .FirstOrDefaultAsync() ?? "";
            return codmoneda;
        }
        public async Task<string> NITempresa(DBContext _context, string codigoempresa)
        {
            //Esta funcion devuelve la moneda base de una determinada empresa
            string nit = await _context.adempresa
                .Where(i => i.codigo == codigoempresa)
                .Select(i => i.nit)
                .FirstOrDefaultAsync() ?? "";
            return nit;
        }
        public async Task<bool> Forzar_Etiquetas(DBContext _context, string codempresa)
        {
            bool resultado = false;
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var forzar_etiqueta = await _context.adparametros
                .Where(v => v.codempresa == codempresa)
                .Select(v => v.forzar_etiqueta)
                .FirstOrDefaultAsync();

                if (forzar_etiqueta != null)
                {
                    resultado = (bool)forzar_etiqueta;
                }
                //}
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
            return resultado;
        }
        public async Task<bool> Permitir_Items_Repetidos(DBContext _context, string codempresa)
        {
            bool resultado = false;
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var permitir_items_repetidos = await _context.adparametros
                .Where(v => v.codempresa == codempresa)
                .Select(v => v.permitir_items_repetidos)
                .FirstOrDefaultAsync();

                if (permitir_items_repetidos != null)
                {
                    resultado = (bool)permitir_items_repetidos;
                }
                //}
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
            return resultado;
        }

        public async Task<int> maxurgentes_por_dia(DBContext _context, string codempresa)
        {
            int resultado = 0;
            try
            {
                var maxurgentes_dia = await _context.adparametros
                .Where(v => v.codempresa == codempresa)
                .Select(v => v.maxurgentes_dia)
                .FirstOrDefaultAsync();

                if (maxurgentes_dia != null)
                {
                    resultado = (int)maxurgentes_dia;
                }
            }
            catch (Exception ex)
            {
                return 0;
            }
            return resultado;
        }
        public async Task<int> maxurgentes(DBContext _context, string codempresa)
        {
            int resultado = 0;
            try
            {
                var maxurgentes = await _context.adparametros
                .Where(v => v.codempresa == codempresa)
                .Select(v => v.maxurgentes)
                .FirstOrDefaultAsync();

                if (maxurgentes != null)
                {
                    resultado = (int)maxurgentes;
                }
            }
            catch (Exception ex)
            {
                return 0;
            }
            return resultado;
        }

        public async Task<int> maxitemurgentes(DBContext _context, string codempresa)
        {
            int resultado = 0;
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var maxitemurgentes = await _context.adparametros
                .Where(v => v.codempresa == codempresa)
                .Select(v => v.maxitemurgentes)
                .FirstOrDefaultAsync();

                if (maxitemurgentes != null)
                {
                    resultado = (int)maxitemurgentes;
                }
                //}
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error: {ex.Message}");
                return 0;
            }
            return resultado;
        }

        public async Task<int> nro_items_urgentes_empaque_cerrado(DBContext _context, string codempresa)
        {
            int resultado = 0;
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var nro_items_urgentes_empaque_cerrado = await _context.adparametros
                .Where(v => v.codempresa == codempresa)
                .Select(v => v.nro_items_urgentes_empaque_cerrado)
                .FirstOrDefaultAsync();

                if (nro_items_urgentes_empaque_cerrado != null)
                {
                    resultado = (int)nro_items_urgentes_empaque_cerrado;
                }
                //}
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error: {ex.Message}");
                return 0;
            }
            return resultado;
        }

        public async Task<decimal> MinimoComplementarAsync(DBContext _context, bool sinDescuentos, int codTarifa, string codEmpresa, string codMoneda, DateTime fecha)
        {
            decimal? resultado = 0;
            decimal? minimo = 0;
            string? monedaMin = "";

            try
            {
                if (sinDescuentos)
                {
                    minimo = await _context.adparametros_complementarias
                        .Where(ac => ac.codempresa == codEmpresa && ac.codtarifa == codTarifa && ac.sindesc == true)
                        .Select(ac => ac.monto)
                        .FirstOrDefaultAsync();

                    monedaMin = await _context.adparametros_complementarias
                        .Where(ac => ac.codempresa == codEmpresa && ac.codtarifa == codTarifa && ac.sindesc == true)
                        .Select(ac => ac.codmoneda)
                        .FirstOrDefaultAsync();
                }
                else
                {
                    minimo = await _context.adparametros_complementarias
                        .Where(ac => ac.codempresa == codEmpresa && ac.codtarifa == codTarifa && ac.sindesc == false)
                        .Select(ac => ac.monto)
                        .FirstOrDefaultAsync();

                    monedaMin = await _context.adparametros_complementarias
                        .Where(ac => ac.codempresa == codEmpresa && ac.codtarifa == codTarifa && ac.sindesc == false)
                        .Select(ac => ac.codmoneda)
                        .FirstOrDefaultAsync();
                }
                if (monedaMin == codMoneda)
                {
                    resultado = minimo;
                }
                else
                {
                    resultado = await tipocambio._conversion(_context, codMoneda, monedaMin, fecha, (decimal)minimo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                resultado = 0;
            }

            return (decimal)resultado;
        }

        public async Task<int> diascompleempresa(DBContext _context, string codempresa)
        {
            int resultado = 0;
            try
            {
                var diascomplementarias = await _context.adparametros
                .Where(v => v.codempresa == codempresa)
                .Select(v => v.diascomplementarias)
                .FirstOrDefaultAsync();

                resultado = (int)diascomplementarias;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 0;
            }
            return resultado;
        }

        public async Task<string> municipio_empresa(DBContext _context, string codigoempresa)
        {
            //Esta funcion devuelve la moneda base de una determinada empresa
            string municipio = await _context.adempresa
                .Where(i => i.codigo == codigoempresa)
                .Select(i => i.municipio)
                .FirstOrDefaultAsync() ?? "";
            return municipio;
        }
        public async Task<int> HojaReportes(DBContext _context, string codigoempresa)
        {
            int num = 0;
            try
            {
                num = _context.adparametros.Where(i => i.codempresa == codigoempresa).Select(i => i.hoja_reportes).FirstOrDefault() ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error en Hoja de Reportes: " + ex.Message);
                num = 0;
            }
            return num;
        }


    }
}
