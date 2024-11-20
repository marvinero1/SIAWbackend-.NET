using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Adsiat_Mensaje_Servicio
 {
    //#region SINGLETON PATTERN
    //private adsiat_token() { }

    //public static readonly adsiat_token Instancia = new adsiat_token();
    //#endregion
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
    public async Task<string> Descripcion_Codigo(DBContext _context, int codigoClasificador)
    {
        string resultado = "NSE";
        resultado = await _context.adsiat_mensaje_servicio.Where(i => i.codigoclasificador == codigoClasificador).Select(I => I.descripcion).FirstOrDefaultAsync() ?? "NSE";
        return resultado;
    }

}

