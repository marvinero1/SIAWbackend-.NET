using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace siaw_funciones
{
    public class TipoCambio
    {
        //Clase necesaria para el uso del DBContext del proyecto siaw_Context
        public static class DbContextFactory
        {
            public static DBContext Create(string connectionString)
            {
                var optionsBuilder = new DbContextOptionsBuilder<DBContext>();
                optionsBuilder.UseSqlServer(connectionString);

                return new DBContext(optionsBuilder.Options);
            }
        }
        public async Task<decimal> conversion(string userConnectionString, string moneda_hasta, string moneda_desde, DateTime fecha, decimal monto)
        {
            try
            {
                decimal resultado = 0;
                using (var context = DbContextFactory.Create(userConnectionString))
                {
                    resultado = await tipocambio(userConnectionString, moneda_hasta, moneda_desde, fecha);
                }
                resultado = (resultado * monto);
                return resultado;
            }
            catch (Exception)
            {
                return 0;
                //return BadRequest("Error en el servidor");
            }
        }
        public async Task<decimal> tipocambio(string userConnectionString, string monedabase, string moneda, DateTime fecha)
        {
            decimal resultado = 0;
            monedabase = monedabase.Trim();
            moneda = moneda.Trim();
            if ((monedabase == moneda))
            {
                resultado = 1;
            }
            else
            {
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var factor = await _context.adtipocambio
                            .Where(v => v.monedabase == monedabase && v.moneda == moneda && v.fecha == fecha)
                            .OrderBy(v => v.codalmacen)
                            .Select(item => item.factor)
                            .FirstOrDefaultAsync();
                    if (factor == null)
                    {
                        resultado = 0;
                        //'si no encuentra buscar la ultima fecha en que exista
                        var factor2 = await _context.adtipocambio
                                    .Where(v => v.monedabase == monedabase && v.moneda == moneda && v.fecha <= fecha)
                                    .OrderByDescending(v => v.fecha)
                                    .Select(item => item.factor)
                                    .FirstOrDefaultAsync();
                        if (factor2 == null)
                        {
                            resultado = 0;
                            //'si no encuentra buscar la ultima fecha en que exista
                        }
                        else
                        {
                            resultado = (decimal)factor2;
                        }

                    }
                    else
                    {
                        resultado = (decimal)factor;
                    }

                    if (resultado == 0)
                    {
                        //'tratar ala inversa y devolver el factor 1/factor
                        var factor3 = await _context.adtipocambio
                            .Where(v => v.monedabase == moneda && v.moneda == monedabase && v.fecha == fecha)
                            .OrderBy(v => v.codalmacen)
                            .Select(item => item.factor)
                            .FirstOrDefaultAsync();
                        if (factor3 == null)
                        {
                            resultado = 0;
                            //si no encuentra buscar la ultima fecha que haya
                            var factor4 = await _context.adtipocambio
                                        .Where(v => v.monedabase == moneda && v.moneda == monedabase && v.fecha <= fecha)
                                        .OrderByDescending(v => v.fecha)
                                        .Select(item => item.factor)
                                        .FirstOrDefaultAsync();
                            if (factor4 == null)
                            {
                                resultado = 1;
                                //'si no encuentra buscar la ultima fecha en que exista
                            }
                            else
                            {
                                resultado = (decimal)factor4;
                            }
                        }
                        else
                        {
                            resultado = 1 / (decimal)factor3;
                        }

                    }
                }
            }

            return resultado;
        }

    }
}
