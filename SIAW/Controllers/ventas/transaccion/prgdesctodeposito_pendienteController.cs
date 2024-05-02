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


        private async Task<int> Buscar_Desctos_Por_Depositos_Pendientes_Por_aplicar(DBContext _context, string codcliente, string codcliente_real, string nit, string codempresa)
        {
            try
            {
                DateTime Depositos_Desde_Fecha = await configuracion.Depositos_Nuevos_Desde_Fecha(_context);
                bool buscar_por_nit = false;
                if (await cliente.EsClienteSinNombre(_context,codcliente))
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
                List < consultCocobranza > dt_depositos_pendientes = new List<consultCocobranza> ();
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


            }
            catch (Exception)
            {
                return 0;
            }
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
