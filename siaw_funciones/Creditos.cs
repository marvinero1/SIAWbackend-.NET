using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace siaw_funciones
{
    public class Creditos
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
        private Funciones funciones = new Funciones();
        private Cliente cliente = new Cliente();
        private Empresa empresa = new Empresa();
        public async Task<bool> Cliente_Tiene_Linea_De_Credito_Valida(DBContext _context, string codcliente)
        {
            try
            {
                bool resultado = false;
                DateTime fecha_hoy = await funciones.FechaDelServidor(_context);
                DateTime fechaAux = new DateTime(fecha_hoy.Year, fecha_hoy.Month, fecha_hoy.Day);

                string cliente_principal = "";
                string codigoPrincipal = "";
                string CodigosIguales = "";
                
                 codigoPrincipal = await cliente.CodigoPrincipal(_context, codcliente);

                if (await cliente.NIT(_context, codigoPrincipal) ==  await cliente.NIT(_context, codcliente))
                {
                    cliente_principal = codigoPrincipal;
                    CodigosIguales = await cliente.CodigosIgualesMismoNIT(_context, codcliente);
                }
                else
                {
                    cliente_principal = codcliente;
                    CodigosIguales = "'" + codcliente + "'";
                }

                //using (_context)
                ////using (var _context = DbContextFactory.Create(_context))
                //{
                    var creditoCliente = (from credito in _context.vehcredito
                                          join cliente in _context.vecliente on credito.codcliente equals cliente.codigo
                                          where CodigosIguales.Contains(credito.codcliente) &&
                                                credito.codtipocredito == "FIJO" &&
                                                credito.credito > 0 &&
                                                !(credito.revertido??false) &&
                                                (fechaAux <= credito.fechavenc && fechaAux <= credito.fecha_vence_garantia)
                                          select credito).FirstOrDefault();

                    if (creditoCliente != null)
                    {
                        if (fechaAux > creditoCliente.fechavenc || fechaAux > creditoCliente.fecha_vence_garantia)
                            return false;
                        else
                            return true;
                    }
                    else
                    {
                        return false;
                    }
               //}
            }
            catch (Exception)
            {
                return false;
            }

        }

        public async Task<bool> ValidarCreditoDisponible_en_Bs(DBContext _context, bool mostrar_detalle, string codcliente, bool incluir_proformas, double totaldoc, string codempresa, string usuario, string monedae, string moneda_pf)
        {
            string codigoPrincipal = await cliente.CodigoPrincipal(_context, codcliente);
            // Solo considerarlo el principal si Tiene el mismo NIT, caso contrario usar solo el codigo individual de cliente
            string CodigosIguales = "";

            if (await cliente.NIT(_context,codigoPrincipal) == await cliente.NIT(_context,codcliente))
            {
                codcliente = codigoPrincipal;
                CodigosIguales = await cliente.CodigosIgualesMismoNIT(_context, codcliente);
            }
            else
            {
                CodigosIguales = "'" + codcliente + "'";
            }
            //actualizar el credito (falta implementar)
            //await Actualizar_Credito_2023(codcliente, usuario, codempresa, false);

            //sacar su credito disponible
            decimal cred_actual = await _context.vecliente.Where(i=> i.codigo == codcliente).Select(i=> i.credito).FirstOrDefaultAsync()??0;


            //saacr moneda actual
            string moneda_actual = "";
            string moneda_base = await empresa.monedabase(_context, codempresa);
            try
            {
                moneda_actual = await Credito_Fijo_Asignado_Vigente_Moneda(_context, codcliente);
            }
            catch (Exception)
            {
                moneda_actual = await empresa.monedabase(_context, codempresa);
            }
            //sacar cuanto debe localmente
            decimal deuda_actual_bs = 0;
            decimal deuda_actual_us = 0;

            // Desde 17-04-2023 la moneda debe obtenerse segun el credito del cliente
            try
            {
                var codigos = await _context.veremision
                    .Join(_context.coplancuotas,
                          p1 => p1.codigo,
                          p2 => p2.coddocumento,
                          (p1, p2) => new { Veremision = p1, Coplancuotas = p2 })
                    .Where(joinResult =>
                           CodigosIguales.Contains(joinResult.Veremision.codcliente)
                           && joinResult.Veremision.tipopago == 1
                           && joinResult.Veremision.contra_entrega == false
                           && joinResult.Veremision.anulada == false)
                    .Select(joinResult => joinResult.Veremision.codigo)
                    .ToListAsync();

                deuda_actual_bs = await _context.coplancuotas
                    .Where(c => CodigosIguales.Contains(c.cliente) 
                    && codigos.Contains(c.coddocumento)
                    && c.monto > c.montopagado
                    && c.moneda == moneda_base
                    )
                    .SumAsync(c => c.monto - c.montopagado) ?? 0;
            }
            catch (Exception)
            {
                deuda_actual_bs = 0;
            }


            try
            {
                var codigos = await _context.veremision
                    .Join(_context.coplancuotas,
                          p1 => p1.codigo,
                          p2 => p2.coddocumento,
                          (p1, p2) => new { Veremision = p1, Coplancuotas = p2 })
                    .Where(joinResult =>
                           CodigosIguales.Contains(joinResult.Veremision.codcliente)
                           && joinResult.Veremision.tipopago == 1
                           && joinResult.Veremision.contra_entrega == false
                           && joinResult.Veremision.anulada == false)
                    .Select(joinResult => joinResult.Veremision.codigo)
                    .ToListAsync();

                deuda_actual_us = await _context.coplancuotas
                    .Where(c => CodigosIguales.Contains(c.cliente)
                    && codigos.Contains(c.coddocumento)
                    && c.monto > c.montopagado
                    && c.moneda == monedae
                    )
                    .SumAsync(c => c.monto - c.montopagado) ?? 0;
            }
            catch (Exception)
            {
                deuda_actual_us = 0;
            }

            //busca el SALDO NACIONAL si tiene sucursales en otras agencia
            //implementado el 09-05-2020

            double saldo_x_pagar_demas_ags_us = 0;

            var resul1 = await cliente.Cliente_Saldo_Pendiente_Nacional(_context, codcliente, monedae);
            if (resul1.message != "")
            {
                return false;  // devolver error de mensaje REVISAR
            }
            saldo_x_pagar_demas_ags_us = resul1.resp;

            double saldo_x_pagar_demas_ags_bs = 0;
            var resul2 = await cliente.Cliente_Saldo_Pendiente_Nacional(_context, codcliente, moneda_base);
            if (resul2.message != "")
            {
                return false;  // devolver error de mensaje REVISAR
            }
            saldo_x_pagar_demas_ags_bs = resul2.resp;


            //busca el SALDO NACIONAL si tiene sucursales en otras agencia
            //implementado el 09-05-2020
            double ttl_proformas_aprobadas_demas_ags_us = 0;
            var resul3 = await cliente.Cliente_Proformas_Aprobadas_Nacional(_context, codcliente, monedae);
            if (resul3.message != "")
            {
                return false;  // devolver error de mensaje REVISAR
            }
            ttl_proformas_aprobadas_demas_ags_us = resul3.resp;

            double ttl_proformas_aprobadas_demas_ags_bs = 0;
            var resul4 = await cliente.Cliente_Proformas_Aprobadas_Nacional(_context, codcliente, moneda_base);
            if (resul4.message != "")
            {
                return false;  // devolver error de mensaje REVISAR
            }
            ttl_proformas_aprobadas_demas_ags_bs = resul4.resp;

            //si es necesario sacar de proformas aprobadas



            return true;

        }



        public async Task<bool> Actualizar_Credito_2023(string codcliente, string usuario, string codempresa, bool arreglar)
        {
            if (arreglar)
            {
                //arreglar datos posiblemente arroneos

            }
            return true;
        }

        public async Task<string> Credito_Fijo_Asignado_Vigente_Moneda(DBContext _context, string codcliente)
        {
            var resultado = await _context.vehcredito
                .Where(v => _context.vetipocredito
                    .Any(t => t.codigo == v.codtipocredito)
                    && v.revertido == false
                    && v.codcliente == codcliente)
                .OrderByDescending(v => v.fecha)
                .ThenByDescending(v => v.horareg)
                .FirstOrDefaultAsync();
            if (resultado!= null)
            {
                return resultado.moneda;
            }
            return "BS";
        }
    }
}
