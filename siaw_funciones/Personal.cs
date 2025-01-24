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
    public class Personal
    {
        public async Task<int> codalmacen(DBContext _context, int codpersona)
        {
            try
            {
                int result = await _context.pepersona.Where(i => i.codigo == codpersona).Select(i => i.codalmacen).FirstOrDefaultAsync() ?? 0;
                return result;
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
}
