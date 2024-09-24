using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Adsiat_Token
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
    public async Task<string> Obtener_Token_Delegado_Activo(DBContext _context, string codautorizacion_sistema, int codambiente)
    {
        string resultado = "";
        resultado = await _context.adsiat_token.Where(i => i.codsistema == codautorizacion_sistema && i.activo == true && i.codambiente == codambiente).OrderByDescending(i => i.valido_desde).Select(I => I.token).FirstOrDefaultAsync() ?? "NSE";
        return resultado;
    }

}

