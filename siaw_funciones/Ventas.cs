using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace siaw_funciones
{
    public class Ventas
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
        private Configuracion configuracion = new Configuracion();
        public async Task<string> monedabasetarifa(string userConnectionString, int codtarifa)
        {
            string resultado = "";

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var result = await _context.intarifa
                    .Where(v => v.codigo == codtarifa)
                    .Select(parametro => new
                    {
                        parametro.monedabase
                    })
                    .FirstOrDefaultAsync();
                if (result != null)
                {
                    resultado = result.monedabase;
                }

            }
            return resultado;
        }


        public async Task<bool> Grabar_Descuento_Por_deposito_Pendiente(DBContext _context, string codempresa)
        {
            int coddesextra_depositos = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);
            double PORCEN_DESCTO = await DescuentoExtra_Porcentaje(_context, coddesextra_depositos);

            return true;
        }

        public async Task<double> DescuentoExtra_Porcentaje(DBContext _context, int coddesextra)
        {
            var result = await _context.vedesextra
                    .Where(v => v.codigo == coddesextra)
                    .Select(parametro => parametro.porcentaje)
                    .FirstOrDefaultAsync();
            if (result == null)
            {
                return 0;
            }
            return (int)result;
        }
    }
}
