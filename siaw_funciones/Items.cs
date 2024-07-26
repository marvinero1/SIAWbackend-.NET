using Microsoft.EntityFrameworkCore;
using NuGet.Configuration;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace siaw_funciones
{
    public class Items
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

        public async Task<bool> itemventa(string userConnectionString, string coditem)
        {
            try
            {
                bool resultado = false;
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    string estado = "4";
                    var query = await _context.initem
                        .Where(i => i.codigo == coditem)
                            .Select(i => new
                            {
                                i.estadocv
                            })
                        .FirstOrDefaultAsync();
                    if (query != null)
                    {
                        estado = query.estadocv;
                    }
                    if (estado == "1" || estado == "2")
                    {
                        resultado= true;
                    }
                    else
                    {
                        resultado= false;   
                    }                    
                }
                return resultado;
            }
            catch (Exception)
            {
                return false;
            }

        }

        public async Task<bool> disminuiritem(DBContext _context, string codigo)
        {
            var query = await _context.instoactual
                .Where(i => i.coditem == codigo)
                 .ToListAsync();
            if (query.Count() > 0)   // elimina
            {
                _context.instoactual.RemoveRange(query);
                await _context.SaveChangesAsync();
            }
            return true;

        }
        public async Task<bool> itemesconjunto(DBContext _context, string coditem)
        {
            try
            {
                bool resultado = true;
                //using (
                //   var _context = DbContextFactory.Create(userConnectionString))
                //{
                var query = await _context.initem
                    .Where(i => i.codigo == coditem)
                        .Select(i => new
                        {
                            i.kit
                        })
                    .FirstOrDefaultAsync();
                if (query != null)
                {
                    resultado = query.kit;
                }
                else
                {
                    resultado = false;
                }
                //}
                return resultado;
            }
            catch (Exception)
            {
                return false;
            }

        }
        public async Task<bool> EsKit(DBContext _context, string coditem)
        {
            try
            {
                bool resultado = false;
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var result = await _context.initem
                    .Where(v => v.codigo == coditem)
                    .Select(parametro => parametro.kit)
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
        public async Task<bool> controla_negativo(DBContext _context, string coditem)
        {
            try
            {
                bool resultado = false;
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var result = await _context.initem
                    .Where(v => v.codigo == coditem)
                    .Select(parametro => parametro.controla_negativo)
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
        public async Task<bool> existeitem(DBContext _context, string codigo)
        {
            var consulta = await _context.initem
                .Where(i => i.codigo == codigo)
                 .CountAsync();
            if (consulta > 0)   // elimina
            {
                return true;
            }
            return false;

        }
        public async Task<double> itempeso(DBContext _context, string codigo)
        {
            var resultado = await _context.initem.Where(i => i.codigo == codigo).Select(i => i.peso).FirstOrDefaultAsync() ?? 0;
            return (double)resultado;

        }
        public async Task<int> lineagrupo(DBContext _context, string codtarifa)
        {
            int resultado = 0;

            //using (_context)
            ////using (var _context = DbContextFactory.Create(userConnectionString))
            //{
            var result = await _context.inlinea
                .Where(v => v.codigo == codtarifa)
                .Select(parametro => new
                {
                    parametro.codgrupo
                })
                .FirstOrDefaultAsync();
            if (result != null)
            {
                resultado = (int)result.codgrupo;
            }

            //}
            return resultado;
        }
        public async Task<decimal> cantidad_empaque_item(DBContext _context, string item, string codtarifa)
        {
            decimal resultado = 0;

            //using (_context)
            ////using (var _context = DbContextFactory.Create(userConnectionString))
            //{
            var result = await _context.inlinea
                .Where(v => v.codigo == codtarifa)
                .Select(parametro => new
                {
                    parametro.codgrupo
                })
                .FirstOrDefaultAsync();
            if (result != null)
            {
                resultado = (int)result.codgrupo;
            }

            //}
            return resultado;
        }
        public async Task<string> itemdescripcion(DBContext _context, string codigo)
        {
            string resultado = "";

            //using (_context)
            ////using (var _context = DbContextFactory.Create(userConnectionString))
            //{
            var result = await _context.initem
                .Where(v => v.codigo == codigo)
                .Select(parametro => new
                {
                    parametro.descripcion
                })
                .FirstOrDefaultAsync();
            if (result != null)
            {
                resultado = result.descripcion;
            }

            //}
            return resultado;
        }
        public async Task<string> itemmedida(DBContext _context, string codigo)
        {
            string resultado = "";

            //using (_context)
            ////using (var _context = DbContextFactory.Create(userConnectionString))
            //{
            var result = await _context.initem
                .Where(v => v.codigo == codigo)
                .Select(parametro => new
                {
                    parametro.medida
                })
                .FirstOrDefaultAsync();
            if (result != null)
            {
                resultado = result.medida;
            }

            //}
            return resultado;
        }
        public async Task<string> item_tipo(DBContext _context, string codigo)
        {
            string resultado = "";

            //using (_context)
            ////using (var _context = DbContextFactory.Create(userConnectionString))
            //{
            var result = await _context.initem
                .Where(v => v.codigo == codigo)
                .Select(parametro => new
                {
                    parametro.tipo
                })
                .FirstOrDefaultAsync();
            if (result != null)
            {
                resultado = result.tipo;
            }

            //}
            return resultado;
        }

        public async Task<double> MaximoDeVenta(DBContext _context, string coditem, string codalmacen, int codtarifa)
        {
            double resultado = 0;
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var res = await _context.initem_max
                .Where(v => v.coditem == coditem && v.codalmacen == Convert.ToInt32(codalmacen) && v.codtarifa == codtarifa)
                .Select(v => v.maximo)
                .FirstOrDefaultAsync();

                if (res != null)
                {
                    resultado = (double)res;
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

        public async Task<int> MaximoDeVenta_PeriodoDeControl(DBContext _context, string coditem, string codalmacen, int codtarifa)
        {
            int resultado = 0;
            try
            {
                //using (_context)
                ////using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                var res = await _context.initem_max
                .Where(v => v.coditem == coditem && v.codalmacen == Convert.ToInt32(codalmacen) && v.codtarifa == codtarifa)
                .Select(v => v.dias)
                .FirstOrDefaultAsync();

                if (res != null)
                {
                    resultado = res;
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

        ///////////////////////////////////////////////////////////////////////////////
        // esta funcion devuelve el grupo al que pertence un item
        ///////////////////////////////////////////////////////////////////////////////
        public async Task<int> itemgrupo(DBContext _context, string codigo)
        {
            int resultado = await _context.initem
                .Join(_context.inlinea,
                    i => i.codlinea,
                    l => l.codigo,
                    (i, l) => new { Item = i, Linea = l })
                .Where(joined => joined.Item.codigo == codigo)
                .Select(joined => joined.Linea.codgrupo).FirstOrDefaultAsync() ?? 0;
            return resultado;
        }

        public async Task<List<inlinea_tuercas>?> inlinea_tuercas(DBContext _context, bool habilitado)
        {
            try
            {
                var resultado = await _context.inlinea_tuercas
                .Where(i => i.habilitado == habilitado)
                .OrderBy(i => i.codigo)
                .ToListAsync();

                return resultado;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<string> itemlinea(DBContext _context, string codigo)
        {
            try
            {
                var resultado = await _context.initem
                .Where(i => i.codigo == codigo)
                .Select(i => i.codlinea)
                .FirstOrDefaultAsync() ?? "";

                return resultado;
            }
            catch (Exception)
            {
                return "";
            }
        }
        public async Task<string> itemudm(DBContext _context, string codigo)
        {
            try
            {
                var resultado = await _context.initem
                .Where(i => i.codigo == codigo)
                .Select(i => i.unidad)
                .FirstOrDefaultAsync() ?? "";

                return resultado;
            }
            catch (Exception)
            {
                return "";
            }
        }
        public async Task<List<string>> Partes_De_Conjunto(DBContext _context, string kit)
        {
            try
            {
                var resultado = await _context.inkit
                .Where(i => i.codigo == kit)
                .Select(i => i.item)
                .ToListAsync();

                return resultado;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> itemventa_context(DBContext _context, string coditem)
        {
            try
            {
                bool resultado = false;
                //using (var _context = DbContextFactory.Create(userConnectionString))
                //{
                string estado = "4";
                var query = await _context.initem
                    .Where(i => i.codigo == coditem)
                        .Select(i => new
                        {
                            i.estadocv
                        })
                    .FirstOrDefaultAsync();
                if (query != null)
                {
                    estado = query.estadocv;
                }
                if (estado == "1" || estado == "2")
                {
                    resultado = true;
                }
                else
                {
                    resultado = false;
                }
                //}
                return resultado;
            }
            catch (Exception)
            {
                return false;
            }

        }
    }
}
