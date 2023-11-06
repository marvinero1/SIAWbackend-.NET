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

    }
}
