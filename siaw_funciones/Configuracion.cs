using siaw_DBContext.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace siaw_funciones
{
    public class Configuracion
    {
        public async Task<int> emp_coddesextra_x_deposito(DBContext _context, string codempresa)
        {
            var result = await _context.adparametros
                    .Where(v => v.codempresa == codempresa)
                    .Select(parametro => parametro.coddesextra_x_deposito)
                    .FirstOrDefaultAsync();
            if (result == null)
            {
                return 0;
            }
            return (int)result;
        }
    }
}
