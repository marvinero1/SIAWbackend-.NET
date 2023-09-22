using Microsoft.EntityFrameworkCore;
using SIAW.Models;

namespace SIAW.Funciones
{
    public class Empresa
    {
        public async Task<bool> ControlarStockSeguridad(string userConnectionString, string codigoempresa)
        {
            bool resultado;
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                //precio unitario del item
                var stock_seguridad = await _context.adparametros
                    .Where(i => i.codempresa == codigoempresa)
                    .Select(i => i.stock_seguridad)
                    .FirstOrDefaultAsync();
                resultado = (bool)stock_seguridad;
            }
            return resultado;
        }
    }
}
