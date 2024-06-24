using Microsoft.Data.SqlClient;
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
    public class Nombres
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
        
        public async Task<string> NombreConOpcionDeCodigoAsync(string userConnectionString, string nombreTabla, string codigo, string campo, string nomColumnaCodigo)
        {
            string resultado = "";

            if (string.IsNullOrWhiteSpace(codigo))
            {
                resultado = "";
            }
            else
            {
                try
                {
                    if (nombreTabla == "adservicio")
                    {
                        resultado = await nombre_adservicio(userConnectionString, codigo);
                    }
                }
                catch (Exception ex)
                {
                    resultado = "";
                    // Manejar la excepción si es necesario
                }
            }

            return resultado;
        }
        public async Task<string> nombre_adservicio(string userConnectionString, string codigo)
        {
            string resultado;
                //obtener el desitem del precio
                string descripcion ="";
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var result = await _context.adservicio
                        .Where(v => v.nivel == int.Parse(codigo))
                        .Select(parametro => new
                        {
                            parametro.descripcion
                        })
                        .FirstOrDefaultAsync();
                    if (result != null)
                    {
                    descripcion = result.descripcion;
                    }

                }
            resultado = descripcion;
            return resultado;
        }


        public async Task<string> nombre_almacen(string userConnectionString, int codigo)
        {
            string resultado;
            string descripcion = "";
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var result = await _context.inalmacen
                    .Where(v => v.codigo == codigo)
                    .Select(parametro => new
                    {
                        parametro.descripcion
                    })
                    .FirstOrDefaultAsync();
                if (result != null)
                {
                    descripcion = result.descripcion;
                }

            }
            resultado = descripcion;
            return resultado;
        }


        public async Task<string> nombre_persona(string userConnectionString, int codigo)
        {
            string resultado;
            string descripcion = "";
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var result = await _context.pepersona
                    .Where(v => v.codigo ==codigo)
                    .Select(parametro => new
                    {
                        descripcion = parametro.nombre1 + " " + parametro.apellido1
                    })
                    .FirstOrDefaultAsync();
                if (result != null)
                {
                    descripcion = result.descripcion;
                }

            }
            resultado = descripcion;
            return resultado;
        }
        public async Task<string> nombredesextra(DBContext _context, int codigo)
        {
            string resultado;
            string descripcion = "";
            //using (_context)
            ////using (var _context = DbContextFactory.Create(userConnectionString))
            //{
            var result = await _context.vedesextra
                .Where(v => v.codigo == codigo)
                .Select(parametro => new
                {
                    parametro.descripcion
                })
                .FirstOrDefaultAsync();
            if (result != null)
            {
                descripcion = result.descripcion;
            }

            //}
            resultado = descripcion.Trim();
            return resultado;
        }
        public async Task<string> nombrelinea(DBContext _context, string codigo)
        {
            string resultado;
            string descripcion = "";
            //using (_context)
            ////using (var _context = DbContextFactory.Create(userConnectionString))
            //{
            var result = await _context.inlinea
                .Where(v => v.codigo == codigo)
                .Select(parametro => new
                {
                    parametro.descripcion
                })
                .FirstOrDefaultAsync();
            if (result != null)
            {
                descripcion = result.descripcion;
            }

            //}
            resultado = descripcion.Trim();
            return resultado;
        }

        public async Task<string> nombreempaque(DBContext _context, int codigo)
        {
            string resultado;
            string descripcion = "";
            //using (_context)
            ////using (var _context = DbContextFactory.Create(userConnectionString))
            //{
            var result = await _context.veempaque
                .Where(v => v.codigo == codigo)
                .Select(parametro => new
                {
                    parametro.descripcion
                })
                .FirstOrDefaultAsync();
            if (result != null)
            {
                descripcion = result.descripcion;
            }

            //}
            resultado = descripcion.Trim();
            return resultado;
        }
        public async Task<string> nombre_descuento_especial(DBContext _context, int codigo)
        {
            string resultado;
            string descripcion = "";
            //using (_context)
            ////using (var _context = DbContextFactory.Create(userConnectionString))
            //{
            var result = await _context.vedescuento
                .Where(v => v.codigo == codigo)
                .Select(parametro => new
                {
                    parametro.descripcion
                })
                .FirstOrDefaultAsync();
            if (result != null)
            {
                descripcion = result.descripcion;
            }

            //}
            resultado = descripcion.Trim();
            return resultado;
        }
        public async Task<string> nombrecuenta_fondos(DBContext _context, string codigo)
        {
            string resultado;
            string descripcion = "";
            var result = await _context.fncuenta
                .Where(v => v.id == codigo)
                .Select(parametro => new
                {
                    parametro.descripcion
                })
                .FirstOrDefaultAsync();
            if (result != null)
            {
                descripcion = result.descripcion;
            }
            resultado = descripcion.Trim();
            return resultado;
        }
        public async Task<string> Nombre_Descuento_De_Nivel(DBContext _context, string nivel_desc)
        {
            string resultado;
            string descripcion = "";
            //using (_context)
            ////using (var _context = DbContextFactory.Create(userConnectionString))
            //{
            var result = await _context.vedesitem_parametros
                .Where(v => v.nivel == nivel_desc)
                .Select(parametro => new
                {
                    parametro.descripcion
                })
                .FirstOrDefaultAsync();
            if (result != null)
            {
                descripcion = result.descripcion;
            }

            //}
            resultado = descripcion.Trim();
            return resultado;
        }
        public async Task<string> nombreempresa(DBContext _context, string codigoempresa)
        {
            string resultado;
            string descripcion = "";
            var result = await _context.adempresa
                .Where(v => v.codigo == codigoempresa)
                .Select(parametro => new
                {
                    parametro.descripcion
                })
                .FirstOrDefaultAsync();
            if (result != null)
            {
                descripcion = result.descripcion;
            }
            resultado = descripcion.Trim();
            return resultado;
        }

        public async Task<string> nombredescuento(DBContext _context, int coddescuento)
        {
            string resultado;
            string descripcion = "";
            var result = await _context.vedescuento
                .Where(v => v.codigo == coddescuento)
                .Select(parametro => new
                {
                    parametro.descripcion
                })
                .FirstOrDefaultAsync();
            if (result != null)
            {
                descripcion = result.descripcion;
            }
            resultado = descripcion.Trim();
            return resultado;
        }
        public async Task<string> nombrecliente(DBContext _context, string codcliente)
        {
            string resultado;
            string descripcion = "";
            var result = await _context.vecliente
                .Where(v => v.codigo == codcliente)
                .Select(parametro => new
                {
                    parametro.razonsocial
                })
                .FirstOrDefaultAsync();
            if (result != null)
            {
                descripcion = result.razonsocial;
            }
            resultado = descripcion.Trim();
            return resultado;
        }
        public async Task<string> nombremoneda(DBContext _context, string codigomoneda)
        {
            string resultado;
            string descripcion = "";
            var result = await _context.admoneda
                .Where(v => v.codigo == codigomoneda)
                .Select(parametro => new
                {
                    parametro.descripcion
                })
                .FirstOrDefaultAsync();
            if (result != null)
            {
                descripcion = result.descripcion;
            }
            resultado = descripcion.Trim();
            return resultado;
        }
        public async Task<string> nombre_servicio(DBContext _context, int codservicio)
        {
            string resultado;
            string descripcion = "";
            var result = await _context.adservicio
                .Where(v => v.nivel == codservicio)
                .Select(parametro => new
                {
                    parametro.descripcion
                })
                .FirstOrDefaultAsync();
            if (result != null)
            {
                descripcion = result.descripcion;
            }
            resultado = descripcion.Trim();
            return resultado;
        }
    }
}
