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

    }
}
