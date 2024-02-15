using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace siaw_funciones
{
    public class datosProforma
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
        public async Task<int> getNumActProd(DBContext _context, string id)
        {
            var nroactual = await _context.venumeracion
                    .Where(item => item.id == id)
                    .Select(item => item.nroactual)
                    .FirstOrDefaultAsync();

            return (nroactual + 1);
        }

        public async Task<bool> existeProforma(DBContext _context, string id, int numId)
        {
            var count = await _context.veproforma
                    .CountAsync(p => p.id == id && p.numeroid == numId);
            if (count > 0)
            {
                return true;
            }
            return false;
        }

        public async Task<int> ultimoCodProf(string userConnectionString)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var codigo = await _context.veproforma
                .OrderByDescending(p => p.codigo)
                .Select(p => p.codigo)
                .FirstOrDefaultAsync();
                return codigo;
            }
        }

        public string getFechaActual()
        {
            DateTime fechaActualLocal = DateTime.Now;
            string fechaFormateada = fechaActualLocal.ToString("yyyy-MM-dd");
            return fechaFormateada;
        }

        public string getHoraActual()
        {
            DateTime horaActual = DateTime.Now;
            int hora = horaActual.Hour; // Obtiene la hora actual en formato de 24 horas
            int minutos = horaActual.Minute; // Obtiene los minutos actuales
            string horaAct = hora.ToString() + ":" + minutos.ToString();
            return horaAct;
        }


    }
}
