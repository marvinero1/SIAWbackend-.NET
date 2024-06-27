using siaw_DBContext.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using siaw_DBContext.Models;

namespace siaw_funciones
{
    public class Despachos
    {
        public async Task<bool> eliminar_prof_de_despachos(DBContext _context, string id, int nroid)
        {
			try
			{
                // elimina la proforma de la tabla de despachos
                var result = await _context.vedespacho.Where(i => i.id == id && i.nroid == nroid).FirstOrDefaultAsync();
                if (result != null)
                {
                    _context.vedespacho.Remove(result);
                    await _context.SaveChangesAsync();
                }
                return true;
            }
			catch (Exception)
			{
                return false;
			}
        }
    }
}
