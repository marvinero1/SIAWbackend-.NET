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
    public class Log
    {
        public async Task<bool> RegistrarEvento(DBContext _context, string usuario, string entidad, string codigo, string id_doc, int numeroid_doc, string ventana, string detalle, string tipo)
        {

        }
    }
}
