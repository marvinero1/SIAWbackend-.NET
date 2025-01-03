﻿using siaw_DBContext.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using siaw_DBContext.Models;


namespace siaw_funciones
{
    public class Documento
    {
        public async Task<int> ventasnumeroid(DBContext _context, string id)
        {
            try
            {
                int result = await _context.venumeracion.Where(i => i.id == id).Select(i => i.nroactual).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<bool> existe_notaremision(DBContext _context, string id, int numeroid)
        {
            try
            {
                int result = await _context.veremision.Where(i => i.id == id && i.numeroid == numeroid).CountAsync();
                if (result > 0)
                {
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> existe_factura(DBContext _context, string id, int numeroid)
        {
            try
            {
                int result = await _context.vefactura.Where(i => i.id == id && i.numeroid == numeroid).CountAsync();
                if (result > 0)
                {
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

    }
}
