using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Data;
using SIAW.Models;

namespace SIAW.Controllers.ventas
{
    public class datosProforma
    {
        public async Task<int> getNumActProd(string userConnectionString, string id)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var nroactual = await _context.venumeracion
                    .Where(item => item.id == id)
                    .Select(item => item.nroactual)
                    .FirstOrDefaultAsync();

                return (nroactual + 1);
            }
        }

        public async Task<bool> existeProforma(string userConnectionString, string id, int numId)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var count = await _context.veproforma
                    .CountAsync(p => p.id == id && p.numeroid == numId);
                if (count > 0)
                {
                    return true;
                }
                return false;
            }
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
