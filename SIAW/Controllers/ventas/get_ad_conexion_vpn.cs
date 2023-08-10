using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Models;

namespace SIAW.Controllers.ventas
{
    public class get_ad_conexion_vpn
    {

        public string Getad_conexion_vpnFromDatabase(string userConnectionString, string agencia)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                if (_context.ad_conexion_vpn == null)
                {
                    return null;
                }
                var ad_conexion_vpn = _context.ad_conexion_vpn
                    .Where(a => a.agencia == agencia)
                    .Select(a => new
                    {
                        agencia = a.agencia,
                        servidor_sql = a.servidor_sql,
                        usuario_sql = a.usuario_sql,
                        contrasena_sql = a.contrasena_sql,
                        bd_sql = a.bd_sql
                    })
                    .FirstOrDefault();
                if (ad_conexion_vpn==null)
                {
                    return null;
                }
                string cadConection = "Data Source=" + ad_conexion_vpn.servidor_sql +
                    ";User ID=" + ad_conexion_vpn.usuario_sql +
                    ";Password=" + ad_conexion_vpn.contrasena_sql +
                    ";Connect Timeout=30;Initial Catalog=" + ad_conexion_vpn.bd_sql + ";";

                return cadConection;
            }
        }

    }
}
