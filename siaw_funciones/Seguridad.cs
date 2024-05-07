using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Core.Types;
using siaw_DBContext.Data;
using siaw_DBContext.Models;

namespace siaw_funciones
{
    public class Seguridad
    {
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
        public async Task<bool> periodo_fechaabierta_context(DBContext _context, DateTime fecha, int modulo)
        {
            /*
            'detalle de modulos
            '1 : administracion
            '2 : inventario
            '3 : ventas
            '4 : cuentas por cobrar
            '5 : contabilidad
            '6 : activos fijos
            '7 : costo y costeo
            '8 : compras y ctas por pagar
            '9 : personal y planillas
            '10: seguridad
            '11: compras menores
            '12: fondos
            */

            bool resultado = await periodo_abierto_context(_context, fecha.Year, fecha.Month, modulo);

            return resultado;
        }

        public async Task<bool> periodo_abierto_context(DBContext _context, int anio, int mes, int modulo)
        {
            /*
            'detalle de modulos
            '1 : administracion
            '2 : inventario
            '3 : ventas
            '4 : cuentas por cobrar
            '5 : contabilidad
            '6 : activos fijos
            '7 : costo y costeo
            '8 : compras y ctas por pagar
            '9 : personal y planillas
            '10: seguridad
            '11: compras menores
            '12: fondos
            */

            var count = await _context.adapertura1
                    .Join(
                        _context.adapertura,
                        a1 => a1.codigo,
                        a => a.codigo,
                        (a1, a) => new { a1, a }
                    )
                    .Where(joined => joined.a.mes == mes && joined.a.ano == anio && joined.a1.sistema == modulo)
                    .CountAsync();

            if (count > 0)
            {
                return false;   // si hay registro quiere decir que esta cerrado
            }
            return true;   // esta abierto no hay registro
        }

        public async Task<bool> periodo_fechaabierta(string userConnectionString, DateTime fecha, int modulo)
        {
            /*
            'detalle de modulos
            '1 : administracion
            '2 : inventario
            '3 : ventas
            '4 : cuentas por cobrar
            '5 : contabilidad
            '6 : activos fijos
            '7 : costo y costeo
            '8 : compras y ctas por pagar
            '9 : personal y planillas
            '10: seguridad
            '11: compras menores
            '12: fondos
            */

            bool resultado = await periodo_abierto(userConnectionString, fecha.Year, fecha.Month, modulo);

            return resultado;
        }

        public async Task<bool> periodo_abierto(string userConnectionString, int anio, int mes, int modulo)
        {
            /*
            'detalle de modulos
            '1 : administracion
            '2 : inventario
            '3 : ventas
            '4 : cuentas por cobrar
            '5 : contabilidad
            '6 : activos fijos
            '7 : costo y costeo
            '8 : compras y ctas por pagar
            '9 : personal y planillas
            '10: seguridad
            '11: compras menores
            '12: fondos
            */

            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var count = await _context.adapertura1
                    .Join(
                        _context.adapertura,
                        a1 => a1.codigo,
                        a => a.codigo,
                        (a1, a) => new { a1, a }
                    )
                    .Where(joined => joined.a.mes == mes && joined.a.ano == anio && joined.a1.sistema == modulo)
                    .CountAsync();

                if (count > 0)
                {
                    return false;   // si hay registro quiere decir que esta cerrado
                }
                return true;   // esta abierto no hay registro
            }
        }

        public async Task<bool> AutorizacionEstaHabilitada(string userConnectionString, int nivel)
        {
            using (var _context = DbContextFactory.Create(userConnectionString))
            {
                var autorizacion_deshabilitado = await _context.adautorizacion_deshabilitadas
                    .Where(i => i.nivel == nivel)
                    .FirstOrDefaultAsync();
                if (autorizacion_deshabilitado != null)
                {
                    return false;   /// la autorizacion esta en la tabla de deshabilitados
                }
                return true;
            }
                
        }


        public string Getad_conexion_vpnFromDatabase(string contrasena_sql, string servidor_sql, string usuario_sql, string bd_sql)
        {
            var passDesencript = XorString(contrasena_sql, "vpn");
            string cadConection = "Data Source=" + servidor_sql +
                ";User ID=" + usuario_sql +
                ";Password=" + passDesencript +
                ";Connect Timeout=30;Initial Catalog=" + bd_sql + ";";

            return cadConection;
        }

        static string XorString(string targetString, string maskValue)
        {
            int index = 0;
            StringBuilder returnValue = new StringBuilder();

            foreach (char charValue in targetString.ToCharArray())
            {
                int maskCharCode = maskValue[index % maskValue.Length];
                int xorResult = charValue ^ maskCharCode;
                returnValue.Append((char)xorResult);

                index = (index + 1) % maskValue.Length;
            }

            return returnValue.ToString();
        }

        public async Task<bool> grabar_log_permisos(DBContext _context, string permiso, string obs, string datosdoc, string usuario, DateTime fecha, string hora)
        {
            // fecha,hora,codpermiso,obs,usuarioactual,datosdoc
            if (obs.Length >= 100)
            {
                obs = obs.Substring(0, 100);
            }
            if (datosdoc.Length >= 100)
            {
                datosdoc = datosdoc.Substring(0, 100);
            }
            adautorizacion_log data = new adautorizacion_log();
            data.fecha = fecha;
            data.hora = hora;
            data.permiso = permiso;
            data.responsable = obs;
            data.usuario = usuario;
            data.datosdoc = datosdoc;
            _context.adautorizacion_log.Add(data);
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        }

        public async Task<bool> autorizado_vendedores(DBContext _context, string usuario, int codvendedor_desde, int codvendedor_hasta)
        {
            bool resultado = false;
            int codpersona = await _context.adusuario.Where(u => u.login == usuario).Select(u => u.persona).FirstOrDefaultAsync();
            var tabla = await _context.vevendedor
            .Where(v => v.comisionista == true && v.codpersona == codpersona)
            .ToListAsync();
            if (tabla.Count > 0)
            {
                foreach (var reg in tabla)
                {
                    var vendedor = reg.codigo;
                    if (vendedor == codvendedor_desde && vendedor == codvendedor_hasta)
                    {
                        resultado = true; break;
                    }
                }
            }
            else
            {
                resultado = true;
            }
            return resultado;
        }
    }
}
