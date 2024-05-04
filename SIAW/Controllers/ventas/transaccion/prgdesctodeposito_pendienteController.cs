using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using siaw_funciones;
using System.Threading;
using System.Drawing;
using siaw_DBContext.Models_Extra;
using System.Collections.Generic;

namespace SIAW.Controllers.ventas.transaccion
{
    [Route("api/venta/transac/[controller]")]
    [ApiController]
    public class prgdesctodeposito_pendienteController : ControllerBase
    {
        private readonly siaw_funciones.Cliente cliente = new Cliente();
        private readonly siaw_funciones.Configuracion configuracion = new Configuracion();
        private readonly siaw_funciones.Cobranzas cobranzas = new Cobranzas();
        private readonly siaw_funciones.Ventas ventas = new Ventas();
        private readonly UserConnectionManager _userConnectionManager;
        public prgdesctodeposito_pendienteController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }

        // boton btnborrar_desctos_Click
        [HttpPost]
        [Route("deleteDescProf/{userConn}/{nomb_ventana}/{usuarioreg}")]
        public async Task<ActionResult<List<object>>> deleteDescProf(string userConn, string nomb_ventana, string usuarioreg, List<dtdesc_apli_prof_no_aprob> dt_desctos_aplicados_no_facturados)
        {
            bool valida = validar_borrar_desctos_aplicados(dt_desctos_aplicados_no_facturados);
            if (!valida)
            {
                return BadRequest(new { resp = "No ha elegido ninguna proforma para que se le quiten los descuentos por depositos aplicados!!!" });
            }
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    // If Not validar_borrar_desctos_aplicados() Then Exit Sub
                    bool resp = await cobranzas.Borrar_Desctos_Por_Deposito_Aplicados_No_Facturados(_context, dt_desctos_aplicados_no_facturados, nomb_ventana, usuarioreg);
                    if (resp)
                    {
                        return Ok(new { resp = "Se elimino exitosamente el/los descuento(s) por deposito!!!" });
                    }
                    return BadRequest(new { resp = "Ocurrio un error y no se elimino el/los descuento(s) por deposito!!!" });
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }
        }
        private bool validar_borrar_desctos_aplicados(List<dtdesc_apli_prof_no_aprob> dt_desctos_aplicados_no_facturados)
        {
            int nro_elegidos = dt_desctos_aplicados_no_facturados.Count(i => i.borrar == true);
            if (nro_elegidos == 0 )
            {
                return false;
            }
            return true;
        }


        // boton btnrefrescar_depositos_pendientes_Click
        [HttpGet]
        [Route("depPendientesRefresh/{userConn}/{codcliente}/{codcliente_real}/{nit}/{codempresa}")]
        public async Task<ActionResult<List<object>>> depPendientesRefresh(string userConn, string codcliente, string codcliente_real, string nit, string codempresa)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var desc_por_depos_pend_por_apli = await Buscar_Desctos_Por_Depositos_Pendientes_Por_aplicar(_context,codcliente,codcliente_real, nit,codempresa);
                    var desc_asig_no_fact = await Buscar_Descuentos_Asignados_No_Facturados(_context, codcliente);

                    return Ok(new
                    {
                        desc_por_depos_pend_por_apli = desc_por_depos_pend_por_apli,
                        desc_asig_no_fact = desc_asig_no_fact
                    });
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }
        }







        private async Task<object> Buscar_Descuentos_Asignados_No_Facturados(DBContext _context, string codcliente)
        {
            // busca los descuentos por deposito aplicados en proformas y que no estan facturados
            // (la busqueda es hasta la fecha actual del servidor
            List<dtdesc_apli_prof_no_aprob> dt_desctos_aplicados_no_facturados = await cobranzas.Descuentos_Por_Deposito_Aplicados_A_Proformas_No_Aprobadas(_context, "cliente", codcliente, 0, 0, 0, DateTime.Today.Date);

            double ttl_aplicado_no_facturado = dt_desctos_aplicados_no_facturados.Sum(i => i.montodoc);
            // devolver esto: dt_desctos_aplicados_no_facturados, ttl_aplicado_no_facturado
            return new
            {
                dt_desctos_aplicados_no_facturados = dt_desctos_aplicados_no_facturados,
                ttl_aplicado_no_facturado = ttl_aplicado_no_facturado.ToString("#,0.00", new System.Globalization.CultureInfo("en-US"))
            };
        }





        private async Task<object> Buscar_Desctos_Por_Depositos_Pendientes_Por_aplicar(DBContext _context, string codcliente, string codcliente_real, string nit, string codempresa)
        {
            DateTime Depositos_Desde_Fecha = await configuracion.Depositos_Nuevos_Desde_Fecha(_context);
            bool buscar_por_nit = false;
            if (await cliente.EsClienteSinNombre(_context, codcliente))
            {
                buscar_por_nit = true;
            }

            //DEPOSITOS PENDIENTES DE APLICAR DESCTO POR DEPOSITO DE CBZAS CREDITO
            List<consultCocobranza> dt_credito_depositos_pendientes = await cobranzas.Depositos_Cobranzas_Credito_Cliente_Sin_Aplicar(_context, "cliente", "", codcliente_real, nit, codcliente_real, buscar_por_nit, "APLICAR_DESCTO", 0, "Proforma_Nueva", codempresa, false, Depositos_Desde_Fecha, true);
            dt_credito_depositos_pendientes.ForEach(item => item.tipo_pago = "es_cbza_credito");

            //DEPOSITOS PENDIENTES DE APLICAR DESCTO POR DEPOSITO DE CBZAS CONTADO
            /*
             'dt_contado_depositos_pendientes.Clear()
            'dt_contado_depositos_pendientes = sia_funciones.Cobranzas.Instancia.Depositos_Cobranzas_Contado_Cliente_Sin_Aplicar(txtcodcliente.Text, 0, "Proforma_Nueva", sia_compartidos.temporales.Instancia.codempresa)
            'dt_contado_depositos_pendientes.Columns.Add("tipo_pago", System.Type.GetType("System.String"))
            'For y As Integer = 0 To dt_contado_depositos_pendientes.Rows.Count - 1
            '    dt_contado_depositos_pendientes.Rows(y)("tipo_pago") = "es_cbza_contado"
            'Next

            ''//DEPOSITOS PENDIENTES DE ANTICIPOS PARA VENTAS CONTADO APLICADOS EN PROFORMAS DIRECTAMENTE
            'dt_anticipos_depositos_pendientes.Clear()
            'dt_anticipos_depositos_pendientes = sia_funciones.Cobranzas.Instancia.Depositos_Anticipos_Contado_Cliente_Sin_Aplicar(txtcodcliente.Text, 0, "Proforma_Nueva", sia_compartidos.temporales.Instancia.codempresa)
            'dt_anticipos_depositos_pendientes.Columns.Add("tipo_pago", System.Type.GetType("System.String"))
            'For y As Integer = 0 To dt_anticipos_depositos_pendientes.Rows.Count - 1
            '    dt_anticipos_depositos_pendientes.Rows(y)("tipo_pago") = "es_anticipo_contado"
            'Next

             */

            // esta instruccion es para copiar la estructura de una de las tablas a la tabla final de resultado: dt_depositos_pendientes
            List<consultCocobranza> dt_depositos_pendientes = new List<consultCocobranza>();
            if (dt_credito_depositos_pendientes.Count() > 0)
            {
                dt_depositos_pendientes = dt_credito_depositos_pendientes;
            }


            // ***********************************************************************************************************
            double ttl_descuentos_deposito = 0;
            double ttl_dist = 0;
            double ttl_cbza = 0;
            int coddesextra = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);
            foreach (var reg in dt_depositos_pendientes)
            {
                reg.docremision = reg.idrem + "-" + reg.nroidrem.ToString();
                reg.doccbza = reg.idcbza + "-" + reg.nroidcbza.ToString();
                reg.docdeposito = reg.iddeposito + "-" + reg.numeroiddeposito.ToString();
                if (reg.fecha_cbza > reg.vencimiento && reg.deposito_en_mora_habilita_descto == false)
                {
                    reg.obs = "No aplicable por pagos de cuenta posterior a la fecha de vencimiento!!!";
                    reg.monto_doc = 0;
                    reg.porcen_descto = 0;
                }
                else
                {
                    if (reg.tipo == 0)
                    {
                        // si es tipo=0 quiere decir que es una cbza que estaba ya en la tabla de descuentos 
                        // y el monto total por aplicar es el 100%
                        reg.porcen_descto = 100;
                        reg.monto_doc = (double)reg.monto_dis;
                    }
                    else
                    {
                        reg.porcen_descto = (double)await ventas.DescuentoExtra_Porcentaje(_context, coddesextra);
                        reg.obs = "Aplicable " + reg.obs;
                        reg.monto_doc = (double)reg.monto_dis * 0.01 * reg.porcen_descto;
                        reg.monto_doc = Math.Round(reg.monto_doc, 2);
                    }
                }
                ttl_descuentos_deposito += reg.monto_doc;
                ttl_cbza += (double)reg.monto_cbza;
                ttl_dist += (double)reg.monto_dis;
            }
            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            // retornar dt_depositos_pendientes, ttl_cbza,ttl_dist, ttl_descuentos_deposito
            return new
            {
                dt_depositos_pendientes = dt_depositos_pendientes,
                ttl_cbza = ttl_cbza.ToString("#,0.00", new System.Globalization.CultureInfo("en-US")),
                ttl_dist = ttl_dist.ToString("#,0.00", new System.Globalization.CultureInfo("en-US")),
                ttl_descuentos_deposito = ttl_descuentos_deposito.ToString("#,0.00", new System.Globalization.CultureInfo("en-US"))
            };
        }




    }
    public class dtdesc_deposito_negativo
    {
        public string cliente { get; set; }
        public int codcobranza { get; set; }
        public string idcbza { get; set; }
        public int nroidcbza { get; set; }
        public DateTime fecha_cbza { get; set; }

        public string iddeposito { get; set; }
        public int numeroiddeposito { get; set; }
        public DateTime fdeposito { get; set; }

        public double monto_recargo { get; set; }
        public string codmoneda { get; set; }
    }
}
