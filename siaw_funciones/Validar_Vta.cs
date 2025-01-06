using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using siaw_funciones;
using NuGet.Configuration;
using System.Collections;
using System.Reflection;
using static siaw_funciones.Validar_Vta;
using System.Globalization;
using MessagePack;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Drawing;
using System.Security.Cryptography;
using Humanizer;
//using static siaw_funciones.Validar_Vta;

namespace siaw_funciones
{
    public class Validar_Vta
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
        string Sql = "";
        public enum Acciones_Validar
        {

            Ninguna = 0,

            Pedir_Servicio = 1,

            Confirmar_SiNo = 2,

            Solo_Ok = 3,
        }
        public class ResultadoValidacion
        {
            public bool resultado { get; set; }
            public string observacion { get; set; } = "";
            public string obsdetalle { get; set; } = "";
            public string datoA { get; set; } = "";
            public string datoB { get; set; } = "";
            public Acciones_Validar accion { get; set; }
        }

        public void EliminarControlValidado(string codControl, DataTable dtValidar)
        {
            // Esta función se utiliza para poner el campo "valido" como "NO" para cualquier control
            // que ya ha sido previamente validado y por alguna modificación en la proforma,
            // su validación como "SI" se ve alterada y es necesario volver a validar.

            foreach (DataRow row in dtValidar.Rows)
            {
                if (row["codcontrol"].ToString() == codControl)
                {
                    row["valido"] = "NO";
                }
            }
        }
        public ResultadoValidacion InicializarResultado(ResultadoValidacion objres)
        {
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            return objres;
        }

        public class JsonResultadoValidacion
        {
            public string codservicio { get; set; } = "";
            public string valido { get; set; } = "";
            public string observacion { get; set; } = "";
            public string obsdetalle { get; set; } = "";
            public string descservicio { get; set; } = "";
            public string dataA { get; set; } = "";
            public string dataB { get; set; } = "";
            public string clave_servicio { get; set; } = "";
            public string accion { get; set; } = "";
        }
        //public class Dtnocumplen
        //{
        //    public string codigo { get; set; }
        //    public string descripcion { get; set; }
        //    public decimal cantidad { get; set; }
        //    public decimal cantidad_pf_anterior { get; set; }
        //    public decimal cantidad_pf_total { get; set; }
        //    public decimal porcen_venta { get; set; }
        //    public int coddescuento { get; set; }
        //    public int codtarifa { get; set; }
        //    public decimal saldo { get; set; }
        //    public decimal porcen_maximo { get; set; }
        //    public decimal porcen_mercaderia { get; set; }
        //    public decimal cantidad_permitida_seg_porcen { get; set; }
        //    public int empaque_precio { get; set; }
        //    public string obs { get; set; }
        //}
        //public class Dtnegativos
        //{
        //    public string kit { get; set; }
        //    public int nro_partes { get; set; }
        //    public string coditem_cjto { get; set; }
        //    public string coditem_suelto { get; set; }
        //    public string codigo { get; set; }
        //    public string descitem { get; set; }
        //    public decimal cantidad { get; set; }
        //    public decimal cantidad_conjunto { get; set; }
        //    public decimal cantidad_suelta { get; set; }
        //    public decimal saldo_descontando_reservas { get; set; }
        //    public decimal saldo_sin_descontar_reservas { get; set; }
        //    public decimal cantidad_reservada_para_cjtos { get; set; }
        //    public string obs { get; set; }
        //}
        public class Dt_desglosado
        {
            public string kit { get; set; }
            public int nro_partes { get; set; }
            public string coditem_cjto { get; set; }
            public string coditem_suelto { get; set; }
            public string codigo { get; set; }
            public double cantidad { get; set; }
            public double cantidad_conjunto { get; set; }
            public double cantidad_suelta { get; set; }
        }

        //public class Controles
        //{
        //    public int Codigo { get; set; }
        //    public int Orden { get; set; }
        //    public string CodControl { get; set; }
        //    public bool? Grabar { get; set; }
        //    public string GrabarAprobar { get; set; }
        //    public bool? HabilitadoPf { get; set; }
        //    public bool? HabilitadoNr { get; set; }
        //    public bool? HabilitadoFc { get; set; }
        //    public string Descripcion { get; set; }
        //    public string? CodServicio { get; set; }
        //    // ... otras propiedades ...
        //    public string NroItems { get; set; }
        //    public string Descuentos { get; set; }
        //    public string Recargos { get; set; }
        //    public string Nit { get; set; }
        //    public string Subtotal { get; set; }
        //    public double Total { get; set; }
        //    public string Preparacion { get; set; }
        //    // ... otras propiedades ...
        //    public string DescGrabar { get; set; }
        //    public string DescGrabarAprobar { get; set; }
        //    public string Valido { get; set; }
        //    public string Observacion { get; set; }
        //    public string ObsDetalle { get; set; }
        //    public string DescServicio { get; set; }
        //    public string DatoA { get; set; }
        //    public string DatoB { get; set; }
        //    public string ClaveServicio { get; set; }
        //    public string Accion { get; set; }
        //    // Nuevo atributo de la clase Dtnegativos
        //    public List<Dtnegativos> Dtnegativos { get; set; }
        //    public List<Dtnocumplen> Dtnocumplen { get; set; }
        //}
        public class intarifaMonMinMay
        {
            public int codtarifa { get; set; }
            public decimal montomin { get; set; }
            public string moneda { get; set; }
        }
        private Nombres nombres = new Nombres();
        private Cliente cliente = new Cliente();
        private Configuracion configuracion = new Configuracion();
        private Ventas ventas = new Ventas();
        private Items items = new Items();
        private Cobranzas cobranzas = new Cobranzas();
        private Creditos creditos = new Creditos();
        private Depositos_Cliente depositos_cliente = new Depositos_Cliente();
        private TipoCambio tipocambio = new TipoCambio();
        private Almacen almacen = new Almacen();
        private Funciones funciones = new Funciones();
        private Restricciones restricciones = new Restricciones();
        private Empresa empresa = new Empresa();
        private Saldos saldos = new Saldos();
        private HardCoded hardcoded = new HardCoded();
        private Seguridad seguridad = new Seguridad();
        private Anticipos_Vta_Contado anticipos_vta_contado = new Anticipos_Vta_Contado();
        private SIAT siat = new SIAT();
        //Task<ActionResult<itemDataMatriz>>
        //Task<Controles>
        public async Task<List<Controles>> DocumentoValido(string userConnectionString, string cadena_control, string tipodoc, string opcion_validar, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, List<vedetalleEtiqueta> dt_etiqueta, List<vedetalleanticipoProforma> dt_anticipo_pf, List<verecargosDatos> tablarecargos, List<Controles>? tabla_controles_recibido, string codempresa, string usuario)
        {
            List<Controles> resultados = new List<Controles>();
            try
            {
                
                List<Controles> controles_final = new List<Controles>();
                string validando = "";
                if (cadena_control == "vacio") { cadena_control = ""; }

                //var _context = DbContextFactory.Create(userConnectionString);
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.vetipo_control_vtas.AsQueryable();
                    if (tipodoc == "proforma")
                    {
                        if (opcion_validar == "grabar")
                        {
                            query = query.Where(vc => vc.habilitado_pf == true && vc.grabar == true);
                        }
                        else if (opcion_validar == "grabar_aprobar")
                        {
                            query = query.Where(vc => vc.habilitado_pf == true && vc.grabar_aprobar == "1");
                        }
                        else if (opcion_validar == "validar")
                        {
                            query = query.Where(vc => vc.habilitado_pf == true);
                        }
                    }
                    else if (tipodoc == "remision")
                    {
                        query = query.Where(vc => vc.habilitado_nr == true);
                    }
                    else if (tipodoc == "factura")
                    {
                        query = query.Where(vc => vc.habilitado_fc == true);
                    }
                    else
                    {
                        query = query.Where(vc => vc.habilitado_pf == true);
                    }

                    if (cadena_control.Length > 0)
                    {
                        var controlesEspecificos = cadena_control.Split(',').ToList();
                        query = query.Where(vc => controlesEspecificos.Contains(vc.codcontrol));
                    }

                    var controles = query.OrderBy(vc => vc.orden)
                                     .Select(vc => new Controles
                                     {
                                         Codigo = vc.codigo,
                                         Orden = (int)vc.orden,
                                         CodControl = vc.codcontrol,
                                         Grabar = vc.grabar,
                                         GrabarAprobar = vc.grabar_aprobar,
                                         HabilitadoPf = vc.habilitado_pf,
                                         HabilitadoNr = vc.habilitado_nr,
                                         HabilitadoFc = vc.habilitado_fc,
                                         Descripcion = vc.descripcion,
                                         CodServicio = vc.codservicio,
                                         // ... otras propiedades ...
                                         NroItems = DVTA.nroitems,
                                         Descuentos = DVTA.totdesctos_extras,
                                         Recargos = DVTA.totrecargos,
                                         Nit = DVTA.nitfactura,
                                         Subtotal = DVTA.subtotaldoc,
                                         Total = DVTA.totaldoc,
                                         Preparacion = DVTA.preparacion,
                                         TipoVenta = DVTA.tipo_vta,
                                         Contra_Entrega = DVTA.contra_entrega,
                                         // ... otras propiedades ...
                                         DescGrabar = "",
                                         DescGrabarAprobar = "",
                                         Valido = "",
                                         Observacion = "",
                                         ObsDetalle = "",
                                         DescServicio = "",
                                         DatoA = "",
                                         DatoB = "",
                                         ClaveServicio = "",
                                         Accion = "",
                                         Dtnegativos = new List<Dtnegativos>(),
                                         Dtnocumplen = new List<Dtnocumplen>()

                                     })
                                     .ToList();
                    controles_final = controles;
                    foreach (var control in controles_final)
                    {
                        control.CodServicio = control.CodServicio?.Trim();
                        validando = control.CodControl + "-" + control.Descripcion;
                        control.Valido = "SI";
                        control.Observacion = "Sin Observacion";
                        control.ObsDetalle = "";
                        if (control.CodServicio?.Trim().Length > 0)
                        {
                            control.DescServicio = await nombres.NombreConOpcionDeCodigoAsync(userConnectionString, "adservicio", control.CodServicio, "", "");
                        }
                        control.DatoA = "";
                        control.DatoB = "";
                        control.ClaveServicio = "";
                        control.Accion = Acciones_Validar.Ninguna.ToString();
                        //Dtnocumplen dtnocumplen = new Dtnocumplen();
                        List<Dtnocumplen> dtnocumplen = new List<Dtnocumplen>();
                        List<Dtnegativos> dtnegativos = new List<Dtnegativos>();
                        control.Dtnocumplen = dtnocumplen;
                        control.Dtnegativos = dtnegativos;
                        _ = await Control_Valido(_context, control, DVTA, tabladetalle, tabladescuentos, dt_etiqueta, dt_anticipo_pf, tablarecargos, dtnocumplen, dtnegativos, codempresa, usuario);
                        //Control_Valido(userConnectionString, control, DVTA, tabladetalle, tabladescuentos, dt_etiqueta, dt_anticipo_pf, tablarecargos, dtnocumplen, dtnegativos, codempresa);
                    }
                    var a = 1;
                    resultados = controles_final;
                }
                
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            try
            {
                //Validar que los controles_again si recibimos por primera vez en teoria es la primera vez que valida, entonces no debe comparar resultados ya validados
                if (tabla_controles_recibido != null)
                {
                    if (tabla_controles_recibido.Count > 0)
                    {
                        //Comparar con lo nuevo validado
                        foreach (var reg_1 in resultados)
                        {
                            // Si el control no es válido, verificar si se recibio el dato como valido
                            if (reg_1.Valido == "NO")
                            {
                                // Buscar si en el primer listado está el control, si no está añadirlo, si está verifica si ya se validó
                                foreach (var reg_2 in tabla_controles_recibido)
                                {
                                    if (reg_2.CodControl == reg_1.CodControl)
                                    {
                                        // Si se validó ya antes, se copiará la validación
                                        if (reg_2.Valido == "SI")
                                        {
                                            // Verificar si los principales parámetros no cambiaron
                                            // Si no cambiaron y antes ya estaba válido, la validación o autorización se replica
                                            if (reg_2.NroItems == reg_1.NroItems &&
                                                reg_2.Descuentos == reg_1.Descuentos &&
                                                reg_2.Recargos == reg_1.Recargos &&
                                                reg_2.Nit == reg_1.Nit &&
                                                reg_2.Subtotal == reg_1.Subtotal &&
                                                reg_2.Preparacion == reg_1.Preparacion &&
                                                reg_2.TipoVenta == reg_1.TipoVenta &&
                                                reg_2.Contra_Entrega == reg_1.Contra_Entrega &&
                                                reg_2.ClaveServicio == "AUTORIZADO" &&
                                                reg_2.Total == reg_1.Total)
                                            {
                                                reg_1.Valido = "SI";
                                                reg_1.ClaveServicio = reg_2.ClaveServicio;
                                            }
                                        }
                                        break; // Salir del bucle una vez encontrado
                                    }
                                }
                                // Fin de buscar si está en el primer listado
                            }
                        }

                        // Copiar a la primera lista
                        // resultados.Clear();
                        // resultados = new List<Controles>(tabla_controles_recibido);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            
            return resultados;
        }
        private ResultadoValidacion InicializarResultado(ref ResultadoValidacion objres)
        {
            objres.resultado = true;
            objres.observacion = "";
            objres.obsdetalle = "";
            objres.datoA = "";
            objres.datoB = "";
            objres.accion = Acciones_Validar.Ninguna;
            return objres;
        }
        public async Task<bool> Control_Valido(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, List<vedetalleEtiqueta> dt_etiqueta, List<vedetalleanticipoProforma> dt_anticipo_pf, List<verecargosDatos> tablarecargos, List<Dtnocumplen> dtnocumplen, List<Dtnegativos> dtnegativos,string codempresa, string usuario)
            //public void Control_Valido(string userConnectionString, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, List<vedetalleEtiqueta> dt_etiqueta, List<vedetalleanticipoProforma> dt_anticipo_pf, List<verecargosDatos> tablarecargos, Dtnocumplen dtnocumplenm, Dtnegativos dtnegativos, string codempresa)
        {
            string _codcontrol = regcontrol.CodControl;
            string _desccontrol = regcontrol.Descripcion;
            switch (_codcontrol)
            {
                case "00001":
                    //**ES CLIENTE COMPETENCIA (NIT)
                    _ = await Control_Valido_C00001Async(_context, regcontrol, DVTA);
                    break;
                case "00002":
                    //**VENTA CON VB PRESIDENCIA
                    _ = await Control_Valido_C00002Async(_context, regcontrol, DVTA);
                    break;
                case "00003":
                    //**CLIENTE COMPETENCIA PERMITIR DESCSTOS DE LINEA
                    _ = await Control_Valido_C00003Async(_context, regcontrol, DVTA, tabladetalle);
                    break;
                case "00004":
                    //**CLIENTE COMPETENCIA PERMITIR DESCTO PROVEEDOR
                    _ = await Control_Valido_C00004Async(_context, regcontrol, DVTA, tabladetalle);
                    break;
                case "00005":
                    //**CLIENTE COMPETENCIA PERMITIR DESCTO VOLUMEN
                    _ = await Control_Valido_C00005Async(_context, regcontrol, DVTA, tabladetalle);
                    break;
                case "00006":
                    //**CLIENTE COMPETENCIA PERMITIR DESCTOS PROMOCION
                    _ = await Control_Valido_C00006Async(_context, regcontrol, DVTA, tabladescuentos,codempresa);
                    break;
                case "00007":
                    //**CLIENTE COMPETENCIA PERMITIR DESCTOS EXTRAS
                    _ = await Control_Valido_C00007Async(_context, regcontrol, DVTA, tabladescuentos);
                    break;
                case "00008":
                    //VERIFICAR MONTO IGUAL A CERO DE PROFORMA
                    _ =  Control_Valido_C00008(_context, regcontrol, DVTA);
                    break;
                case "00009":
                    //VERIFICAR DESCUENTO DE NIVEL SEGÚN SOLICITUD
                    _ = await Control_Valido_C00009Async(_context, regcontrol, DVTA);
                    break;
                case "00010":
                    //VERIFICAR SI VTA ES CON DESCUENTO DE NIVEL SEGÚN SOLICITUD Y COD CLIENTE SN
                    _ = await Control_Valido_C00010Async(_context, regcontrol, DVTA);
                    break;
                case "00011":
                    //si el cliente de la proforma es un CASUAL  o un SN debe pedir clave
                    _ = await Control_Valido_C00011Async(_context, regcontrol, DVTA);
                    break;
                case "00012":
                    //12	VERIFICAR TIPO DE PREPARACION DE CLIENTE FINAL
                    _ = await Control_Valido_C00012Async(_context, regcontrol, DVTA);
                    break;
                case "00013":
                    // VERIFICA VENTA CON CODIGO DE CLIENTE SN Y CLIENTE REAL QUE NO ES SN y CASUAL
                    _ = await Control_Valido_C00013Async(_context, regcontrol, DVTA);
                    break;
                case "00014":
                    //VERIFICAR SI LOS DESCUENTOS EXTRAS ESTAN VIGENTES
                    _ = await Control_Valido_C00014Async(_context, regcontrol,tabladescuentos);
                    break;
                case "00015":
                    //VERIFICAR FECHA VIGENCIA DESCTOS. EXTRAS VERSUS FECHA INICIAL PROFORMA
                    _ = await Control_Valido_C00015Async(_context, regcontrol, DVTA, tabladescuentos);
                    break;
                case "00016":
                    //VERIFICAR SI HAY PROMOCIONES PENDIENTES DE APLICAR
                    _ = await Control_Valido_C00016Async(_context, regcontrol, DVTA, tabladescuentos, tabladetalle, codempresa);
                    break;
                case "00017":
                    //VALIDAR DESCUENTO POR DEPOSITO (CBZAS VALIDAS)
                    // Control_Valido_C00017(regcontrol, DVTA, tabladescuentos);
                    _ = await Control_Valido_C00017Async(_context, regcontrol, DVTA, tabladescuentos, codempresa);
                    break;
                case "00018":
                    //VALIDAR DESCUENTO EXTRA REQUIERE CREDITO VALIDO
                    //  Control_Valido_C00018(regcontrol, DVTA, tabladescuentos);
                    _ = await Control_Valido_C00018Async(_context, regcontrol, DVTA, tabladescuentos, codempresa);
                    break;
                case "00019":
                    //VALIDAR DESCUENTO EXTRA SEGÚN TIPO DE VENTA (CONTADO CE, CONTADO CA, CREDITO)
                    //  Control_Valido_C00019(regcontrol, DVTA, tabladescuentos);
                    _ = await Control_Valido_C00019Async(_context, regcontrol, DVTA, tabladescuentos,dt_anticipo_pf, codempresa);
                    break;
                case "00020":
                    //VALIDAR MONTO MINIMO SEGÚN LISTA DE PRECIOS
                    // Control_Valido_C00020(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00020Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00021":
                    //VALIDAR MONTO MINIMO PARA ENTREGAR EL PEDIDO AL CLIENTE
                    //  Control_Valido_C00021(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00021Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00022":
                    //VALIDAR MONTO MINIMO PARA APLICAR DESCUENTOS ESPECIALES
                    // Control_Valido_C00022(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00022Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00023":
                    //VALIDAR CUMPLIMIENTO DE EMPAQUE MINIMO SEGUN LISTA DE PRECIOS
                    // Control_Valido_C00023(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00023Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00024":
                    //VALIDAR CREDITO DISPONIBLE
                    // Control_Valido_C00024(regcontrol, DVTA);
                    _ = await Control_Valido_C00024Async(_context, regcontrol, DVTA, codempresa, usuario);
                    break;
                case "00025":
                    //VALIDAR ETIQUETA DE LA PROFORMA
                    //  Control_Valido_C00025(regcontrol, DVTA, dt_etiqueta);
                    _ = await Control_Valido_C00025Async(_context, regcontrol, DVTA, dt_etiqueta, codempresa);
                    break;
                case "00026":
                    //VALIDAR TIPO DE PAGO PERMITIDO AL CLIENTE
                    //  Control_Valido_C00026(regcontrol, DVTA, dt_etiqueta);
                    _ = await Control_Valido_C00026Async(_context, regcontrol, DVTA, dt_etiqueta, codempresa);
                    break;
                case "00027":
                    //VERIFICAR SI EL CLIENTE TIENE NOTAS DE REMISION PENDIENTES DE PAGO
                    // Control_Valido_C00027(regcontrol, DVTA, tabladescuentos);
                    _ = await Control_Valido_C00027Async(_context, regcontrol, DVTA, tabladescuentos, codempresa);
                    break;
                case "00028":
                    //VERIFICAR QUE EL CLIENTE Y CLIENTE REAL SEA ATENDIDO POR EL VENDEDOR
                    // Control_Valido_C00028(regcontrol, DVTA);
                    _ = await Control_Valido_C00028Async(_context, regcontrol, DVTA, codempresa, usuario);
                    break;
                case "00029":
                    //VERIFICAR QUE EL PERIODO ESTE ABIERTO
                    // Control_Valido_C00029(regcontrol, DVTA);
                    _ = await Control_Valido_C00029Async(_context, regcontrol, DVTA, codempresa, usuario);
                    break;
                case "00030":
                    //VERIFICAR NIT VALIDO
                    //  Control_Valido_C00030(regcontrol, DVTA);
                    _ = await Control_Valido_C00030Async(_context, regcontrol, DVTA, codempresa, usuario);
                    break;
                case "00031":
                    //VERIFICAR VENTA A CREDITO NIT NO PUEDE SER CERO
                    // Control_Valido_C00031(regcontrol, DVTA);
                    _ = await Control_Valido_C00031Async(_context, regcontrol, DVTA, codempresa, usuario);
                    break;
                case "00032":
                    //VERIFICAR MONTO LIMITE FACTURA SIN NOMBRE
                    // Control_Valido_C00032(regcontrol, DVTA);
                    _ = await Control_Valido_C00032Async(_context, regcontrol, DVTA, codempresa, usuario);
                    break;
                case "00033":
                    //VERIFICAR DESCUENTOS EXTRAS APLICADOS
                    //  Control_Valido_C00033(regcontrol, DVTA, tabladetalle, tabladescuentos);
                    _ = await Control_Valido_C00033Async(_context, regcontrol, DVTA, tabladetalle, tabladescuentos, codempresa);
                    break;
                case "00034":
                    //VALIDAR EL TIPO DE TRANSPORTE ELEGIDO PARA ENTREGA DE MERCADERIA
                    // Control_Valido_C00034(regcontrol, DVTA, tabladetalle, tabladescuentos);
                    _ = await Control_Valido_C00034Async(_context, regcontrol, DVTA, tabladetalle, tabladescuentos, codempresa);
                    break;
                case "00035":
                    //VERIFICAR NOMBRE DE TRANSPORTE VALIDO
                    //  Control_Valido_C00035(regcontrol, DVTA, tabladetalle, tabladescuentos);
                    _ = await Control_Valido_C00035Async(_context, regcontrol, DVTA, tabladetalle, tabladescuentos, codempresa);
                    break;
                case "00036":
                    //VALIDAR QUIEN DEBE CANCELAR EL FLETE(CLIENTE O PERTEC)
                    //  Control_Valido_C00036(regcontrol, DVTA, tabladetalle, tabladescuentos);
                    _ = await Control_Valido_C00036Async(_context, regcontrol, DVTA, tabladetalle, tabladescuentos, codempresa);
                    break;
                case "00037":
                    //VALIDAR OTRO MEDIO DE TRANSPORTE
                    //  Control_Valido_C00037(regcontrol, DVTA);
                    _ = await Control_Valido_C00037Async(_context, regcontrol, DVTA, codempresa);
                    break;
                case "00038":
                    //VERIFICAR ANTICIPO ASIGNADO EN VENTA AL CONTADO
                    // Control_Valido_C00038(regcontrol, DVTA, dt_anticipo_pf);
                    _ = await Control_Valido_C00038Async(_context, regcontrol, DVTA, dt_anticipo_pf, codempresa);
                    break;
                case "00039":
                    //VALIDAR VENTA CONTADO CONTRA ENTREGA CLIENTE DEL INTERIOR
                    // Control_Valido_C00039(regcontrol, DVTA);
                    _ = await Control_Valido_C00039Async(_context, regcontrol, DVTA, codempresa);
                    break;
                case "00040":
                    //VALIDAR SI ES VENTA CONTADO CONTRA ENTREGA NO TENGA ANTICIPO
                    //  Control_Valido_C00040(regcontrol, DVTA);
                    _ = await Control_Valido_C00040Async(_context, regcontrol, DVTA, codempresa);
                    break;
                case "00041":
                    //VALIDAR DIRECCION DE ENTREGA (CLIENTE CON VARIOS PTOS. DE VENTA)
                    //  Control_Valido_C00041(regcontrol, DVTA);
                    _ = await Control_Valido_C00041Async(_context, regcontrol, DVTA, codempresa);
                    break;
                case "00042":
                    //VERIFICAR DESCUENTOS EXTRAS REPETIDOS
                    //  control_valido_c00042(regcontrol, DVTA, tabladescuentos);
                    _ = await Control_Valido_C00042Async(_context, regcontrol, DVTA, tabladescuentos, codempresa);
                    break;
                case "00043":
                    //VALIDAR GEO-REFERENCIA DE ENTREGA VALIDA
                    // control_valido_c00043(regcontrol, DVTA);
                    _ = await Control_Valido_C00043Async(_context, regcontrol, DVTA, codempresa);
                    break;
                case "00044":
                    //VERIFICAR CLIENTE TIENE CUENTAS POR PAGAR EN MORA
                    // control_valido_c00044(regcontrol, DVTA);
                    _ = await Control_Valido_C00044Async(_context, regcontrol, DVTA, codempresa);
                    break;
                case "00045":
                    //VERIFICAR QUE EL DESCUENTO DE PROMOCION NO SOBRE PASE EL DISPONIBLE
                    // control_valido_c00045(regcontrol, DVTA);
                    _ = await Control_Valido_C00045Async(_context, regcontrol, DVTA, codempresa);
                    break;
                case "00046":
                    //VALIDAR PRECIO DE VENTA PERMITIDO AL CLIENTE
                    // control_valido_c00046(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00046Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00047":
                    //VALIDAR QUE LA CANTIDAD DE VENTA DEL ITEM AL CLIENTE NO SEA MAYOR AL PERMITIDO EN LOS DIAS LIMITE
                    // control_valido_c00047(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00047Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00048":
                    //VALIDAR VENTA A CLIENTE SIN CUENTA EN OFICINA
                    // control_valido_c00048(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00048Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00049":
                    //VALIDAR VENTA A CLIENTE CON CUENTA EN OFICINA
                    // control_valido_c00049(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00049Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00050":
                    //VALIDAR PEDIDO URGENTE
                    // Control_Valido_C00050(regcontrol, DVTA, tabladetalle, tablarecargos);
                    _ = await Control_Valido_C00050Async(_context, regcontrol, DVTA, tabladetalle, tablarecargos, codempresa);
                    break;
                case "00051":
                    //VALIDAR MONTO MINIMO REQUERIDO PARA LOS DESCUENTOS EXTRAS
                    //  Control_Valido_C00051(regcontrol, DVTA, tabladescuentos);
                    _ = await Control_Valido_C00051Async(_context, regcontrol, DVTA, tabladescuentos, codempresa);
                    break;
                case "00052":
                    //VALIDAR PESO MINIMO REQUERIDO PARA APLICAR DESCUENTOS EXTRAS
                    // Control_Valido_C00052(regcontrol, DVTA, tabladetalle, tabladescuentos);
                    _ = await Control_Valido_C00052Async(_context, regcontrol, DVTA, tabladetalle, tabladescuentos, codempresa);
                    break;
                case "00053":
                    //VALIDAR MONTO PARA BANCARIZACION
                    // Control_Valido_C00053(regcontrol, DVTA, tabladescuentos);
                    _ = await Control_Valido_C00053Async(_context, regcontrol, DVTA, tabladescuentos, codempresa);
                    break;
                case "00054":
                    //VALIDAR VENTA A CLIENTE QUE SE VENDE SOLO CONTRA ENTREGA
                    //  Control_Valido_C00054(regcontrol, DVTA, tabladescuentos);
                    _ = await Control_Valido_C00054Async(_context, regcontrol, DVTA, tabladescuentos, codempresa);
                    break;
                case "00055":
                    //VALIDAR QUE NO SE VENDA A CREDITO A CLIENTE SIN NOMBRE
                    //  Control_Valido_C00055(regcontrol, DVTA, tabladescuentos);
                    _ = await Control_Valido_C00055Async(_context, regcontrol, DVTA, tabladescuentos, codempresa);
                    break;
                case "00056":
                    //VALIDAR DIRECCION DE LA EQIQUETA CORRECTA
                    //  Control_Valido_C00056(regcontrol, DVTA, dt_etiqueta);
                    _ = await Control_Valido_C00056Async(_context, regcontrol, DVTA, dt_etiqueta, codempresa);
                    break;
                case "00057":
                    //VALIDAR QUE NO SE OTORGUE DESCTO PRONTO PAGO NORMAL A VENTA CONTRA ENTREGA Y VICEVERSA
                    //  Control_Valido_C00057(regcontrol, DVTA, tabladescuentos);
                    _ = await Control_Valido_C00057Async(_context, regcontrol, DVTA, tabladescuentos, codempresa);
                    break;
                case "00058":
                    //VALIDAR LIMITE MAXIMO DE VENTA DEFINIDO EN PORCENTAJE
                    // Control_Valido_C00058(regcontrol, DVTA, tabladetalle, dtnocumplen, dgvmaximos_vta);
                    _ = await Control_Valido_C00058Async(_context, regcontrol, DVTA, tabladetalle, dtnocumplen, codempresa, usuario);
                    break;
                case "00059":
                    //PERMITIR DESCUENTOS DE NIVEL ANTERIORES
                    // Control_Valido_C00059(regcontrol, DVTA);
                    _ = await Control_Valido_C00059Async(_context, regcontrol, DVTA, codempresa);
                    break;
                case "00060":
                    //VALIDAR SALDOS NEGATIVOS
                    //  Control_Valido_C00060(regcontrol, DVTA, tabladetalle, dtnegativos, dgvnegativos);
                    _ = await Control_Valido_C00060Async(_context, regcontrol, DVTA, tabladetalle, dtnegativos, codempresa, usuario);
                    break;
                case "00061":
                    //VALIDAR NRO. DE PEDIDOS URGENTES DEL CLIENTE POR DIA
                    //  Control_Valido_C00061(true, regcontrol, DVTA, tabladetalle, tablarecargos);
                    _ = await Control_Valido_C00061Async(_context, regcontrol,true, DVTA, tabladetalle, tablarecargos, codempresa);
                    break;
                case "00062":
                    //VALIDAR NRO. DE PEDIDOS URGENTES DEL CLIENTE POR SEMANA
                    //  Control_Valido_C00061(false, regcontrol, DVTA, tabladetalle, tablarecargos);
                    _ = await Control_Valido_C00061Async(_context, regcontrol, false, DVTA, tabladetalle, tablarecargos, codempresa);
                    break;
                case "00063":
                    //VALIDAR MONTO MINIMO PEDIDO URGENTE A PROVINCIA
                    // Control_Valido_C00063(false, regcontrol, DVTA, tabladetalle, tablarecargos);
                    _ = await Control_Valido_C00063Async(_context, regcontrol, false, DVTA, tabladetalle, tablarecargos, codempresa);
                    break;
                case "00064":
                    //VALIDAR ANTICIPOS ASIGNADOS EN VENTA AL CONTADO
                    // Control_Valido_C00064(regcontrol, DVTA, dt_anticipo_pf);
                    _ = await Control_Valido_C00064Async(_context, regcontrol, DVTA, dt_anticipo_pf, codempresa);
                    break;
                case "00065":
                    //VALIDAR MONTO VENTA CLIENTE FINAL
                    //Control_Valido_C00065(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00065Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00066":
                    //VALIDAR ITEMS REPETIDOS
                    // Control_Valido_C00066(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00066Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00067":
                    //VALIDAR EMPAQUES CERRADOS SEGUN LISTA DE PRECIOS
                    //Control_Valido_C00067(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00067Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00068":
                    //VALIDAR NO MEZCLAR DESCUENTOS ESPECIALES (PROV-VOL)
                    // Control_Valido_C00068(regcontrol, tabladetalle);
                    _ = await Control_Valido_C00068Async(_context, regcontrol, tabladetalle, codempresa);
                    break;
                case "00069":
                    //VALIDAR VENTA MINIMA NRO. DE ITEMS G2 EN KG PAG-41
                    //Control_Valido_C00069(regcontrol, tabladetalle);
                    _ = await Control_Valido_C00069Async(_context, regcontrol, tabladetalle, codempresa);
                    break;
                case "00070":
                    //VALIDAR DESC. PRONTO PAGO ITEMS GRADO 2 EN KG PAG-41
                    // Control_Valido_C00070(regcontrol, tabladetalle, tabladescuentos);
                    _ = await Control_Valido_C00070Async(_context, regcontrol, DVTA, tabladetalle, tabladescuentos, codempresa);
                    break;
                case "00071":
                    //VALIDAR ENLACE PROFORMA MAYORISTA-DIMEDIADO
                    // Control_Valido_C00071(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00071Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00072":
                    //VALIDAR FACTURA COMPLEMENTARIA EN TIENDA
                    //Control_Valido_C00071(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00072Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00073":
                    //VALIDAR MONTO MAXIMO DE VENTA CLIENTE SIN NOMBRE EN TIENDA
                    //  Control_Valido_C00073(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00073Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00074":
                    //VALIDAR NOMBRE FACTURA  EN TIENDA
                    //  Control_Valido_C00074(regcontrol, DVTA);
                    _ = await Control_Valido_C00074Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00075":
                    //VALIDAR NRO ITEMS POR CAJA
                    //  Control_Valido_C00075(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00075Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00076":
                    //VALIDAR FECHA LIMITE DE DOSIFICACION
                    // Control_Valido_C00076(regcontrol, DVTA);
                    _ = await Control_Valido_C00076Async(_context, regcontrol, DVTA, codempresa);
                    break;
                case "00077":
                    //VALIDAR TIPO DE CAJA
                    // Control_Valido_C00077(regcontrol, DVTA);
                    _ = await Control_Valido_C00077Async(_context, regcontrol, DVTA, codempresa);
                    break;
                case "00078":
                    //VALIDAR QUE NO SE VENDA AL CREDITO EN TIENDA
                    //  Control_Valido_C00078(regcontrol, DVTA);
                    _ = await Control_Valido_C00078Async(_context, regcontrol, DVTA, codempresa);
                    break;
                case "00079":
                    //VALIDAR ID CUENTA VENTA CONTADO TIENDA
                    // Control_Valido_C00079(regcontrol, DVTA);
                    _ = await Control_Valido_C00079Async(_context, regcontrol, DVTA, codempresa);
                    break;
                case "00080":
                    //VALIDAR NIT EXISTENTE FACTURADO
                    //  Control_Valido_C00080(regcontrol, DVTA);
                    _ = await Control_Valido_C00080Async(_context, regcontrol, DVTA, codempresa);
                    break;
                case "00081":
                    //VALIDAR MAXIMO DE VENTA DE ITEM
                    //  Control_Valido_C00081(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00081Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00082":
                    //VALIDAR DEUDA PENDIENTE DEL CLIENTE TIENDA
                    // Control_Valido_C00082(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00082Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00083":
                    //VALIDAR ANTICIPO APLICADO FACTURA TIENDA
                    // Control_Valido_C00083(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00083Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00084":
                    //VALIDAR QUE LOS ITEMS DE VENTA DE UN CLIENTE SUMADOS CON VENTAS DE N DIAS ATRAS DE VENTA DEL ITEM NO SEA MAYOR AL MAXIMO DE VENTANA SUMADO ESAS CANTIDADES
                    //control_valido_c00084(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00084Async(_context, regcontrol, DVTA, tabladetalle, dtnocumplen, codempresa, usuario);
                    break;
                case "00085":
                    //VALIDAR QUE UNA SOLICITU DE DESCUENTOS SE UTILICE CON UNA SOLA PROFORMA
                    // control_valido_c00085(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00085Async(_context, regcontrol, DVTA, tabladetalle, dtnocumplen, codempresa, usuario);
                    break;
                case "00086":
                    //VALIDAR PRECIO DE VENTA PERMITIDO A DESCUENTO ESPECIAL
                    //control_valido_c00086(regcontrol, tabladetalle);
                    _ = await Control_Valido_C00086Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00087":
                    //VALIDAR PRECIO DE VENTA PERMITIDO A DESCUENTO ESPECIAL
                    // Control_Valido_c00087(regcontrol, tabladetalle);
                    _ = await Control_Valido_C00087Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00088":
                    //VALIDAR DESCUENTO ESPECIAL HABILITADO
                    // Control_Valido_C00088(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00088Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00089":
                    //VALIDAR DESCUENTO DE LINEA-NIVEL HABILITADO
                    //Control_Valido_C00089(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00089Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00090":
                    //VALIDAR ENLACE PROFORMA CASUAL-REFERENCIAL
                    //Control_Valido_C00090(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00090Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00091":
                    //VALIDAR NRO MAXIMO DE ITEMS PROFORMAS-NOTAS DE REMISION - FACTURA SEGUN SIAT-SIN
                    //EL NRO MAXIMO DE ITEMS EN DETALLE DEBE SER 500
                    // Control_Valido_C00091(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00091Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00092":
                    //VALIDAR ITEMS REPETIDOS QUE TENGAN DESCUENTOS DE LINEA
                    // Control_Valido_C00092(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00092Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00093":
                    //VALIDAR ASIGNACION DESCUENTO ESPECIAL EMPAQUES
                    //Control_Valido_C00093(regcontrol, DVTA, tabladetalle);
                    _ = await Control_Valido_C00093Async(_context, regcontrol, DVTA, tabladetalle, codempresa);
                    break;
                case "00094":
                    //VERIFICAR SI HAY PROMOCIONES PENDIENTES DE APLICAR
                    //Control_Valido_C00094(regcontrol, DVTA, tabladescuentos, tabladetalle);
                    _ = await Control_Valido_C00094Async(_context, regcontrol, DVTA, tabladetalle, tabladescuentos, codempresa);
                    break;
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00001Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA)
        {
            // //PERMITIR GRABAR PARA APROBAR PROFORMA CLIENTE COMPETENCIA
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_NIT_Es_Cliente_CompetenciaAsync(_context, DVTA);
            if ((objres.resultado == false))
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion= objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00002Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA)
        {
            // '//VENTA CON VB PRESIDENCIA
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Tipo_ClienteAsync(_context, DVTA.codcliente_real);
            if ((objres.resultado == false))
            {
                regcontrol.Valido = "NO";
                regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00003Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle)
        {
            //CLIENTE COMPETENCIA PERMITIR DESCSTOS DE LINEA
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Cliente_Competencia_Permite_Desctos_De_LineaAsync(_context, DVTA, tabladetalle);
            if ((objres.resultado == false))
            {
                regcontrol.Valido = "NO";
               // regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00004Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle)
        {
            //CLIENTE COMPETENCIA PERMITIR DESCTO PROVEEDOR
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Cliente_Competencia_Permite_Desctos_ProveedorAsync(_context, DVTA, tabladetalle);
            if ((objres.resultado == false))
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00005Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle)
        {
            //CLIENTE COMPETENCIA PERMITIR DESCTO VOLUMEN
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Cliente_Competencia_Permite_Desctos_VolumnenAsync(_context, DVTA, tabladetalle);
            if ((objres.resultado == false))
            {
                regcontrol.Valido = "NO";
               // regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00006Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            //**CLIENTE COMPETENCIA PERMITIR DESCTOS PROMOCION
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Cliente_Competencia_Permite_Desctos_PromocionAsync(_context, DVTA, tabladescuentos,codempresa);
            if ((objres.resultado == false))
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00007Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos)
        {
            //**CLIENTE COMPETENCIA PERMITIR DESCTOS EXTRAS
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Cliente_Competencia_Permite_Desctos_ExtraAsync(_context, DVTA, tabladescuentos);
            if ((objres.resultado == false))
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private bool Control_Valido_C00008(DBContext _context, Controles regcontrol, DatosDocVta DVTA)
        {
            //**VERIFICAR MONTO IGUAL A CERO DE PROFORMA
            if ((Math.Round(DVTA.totaldoc, 2) < 0) || (Math.Round(DVTA.totaldoc, 2) == 0))
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = "El monto total final de la proforma es igual a cero, lo cual no esta permitido, verificar esta situacion!!!";
                regcontrol.ObsDetalle = "";
                regcontrol.DatoA = "";
                regcontrol.DatoB = "";
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = Acciones_Validar.Ninguna.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00009Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA)
        {
            //**VENTA CON DESCUENTOS DE NIVEL SEGUN SOLICITUD
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Descuentos_De_Nivel_Segun_SolicitudAsync(_context, DVTA);
            if ((objres.resultado == false))
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00010Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA)
        {
            //**107 - PERMITIR VENTA A CLIENTE SIN NOMBRE SIN ENLAZAR A CLIENTE REAL
            //verificar que si se utiliza el codigo SIN NOMBRE codcliente.text en el codcliente real, no puede estar vacio o ser el 
            //mismo codcliente, debe ser un codigo de cliente real
            //implementado en fecha: 11-10-2016 solicitado por JRA
            bool resultado = true;
            if (await cliente.EsClienteSinNombre(_context, DVTA.codcliente) == true || await cliente.EsClienteCasual(_context, DVTA.codcliente) == true)
            {
                if (DVTA.codcliente == DVTA.codcliente_real || await cliente.EsClienteSinNombre(_context, DVTA.codcliente_real) || await cliente.EsClienteCasual(_context, DVTA.codcliente_real))
                {
                    resultado= false;
                    regcontrol.Valido = "NO";
                    //regcontrol.DescServicio = "";
                    regcontrol.Observacion = "Toda venta al codigo de cliente SIN NOMBRE o CASUAL requiere que el codigo de cliente real-referencia sea un codigo de cliente diferente de un codigo de Cliente Sin Nombre/Casual. Ingrese el permiso especial!!!";
                    if (DVTA.estado_doc_vta.ToUpper() =="NUEVO")
                    {
                        regcontrol.DatoA = DVTA.codcliente +"-"+DVTA.nitfactura;
                    }
                    else
                    {
                        regcontrol.DatoA = DVTA.id + "-"+DVTA.numeroid+" "+DVTA.codcliente;
                    }
                    regcontrol.DatoB = "Total: " + DVTA.totaldoc + " (" + DVTA.codmoneda + ")";
                    regcontrol.ClaveServicio = "";
                    regcontrol.Accion = Acciones_Validar.Pedir_Servicio.ToString();
                }
                
            }
            return resultado;
        }
        private async Task<bool> Control_Valido_C00011Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA)
        {
            //99 - PROFORMA SIN NOMBRE CON DESCUENTOS DE NIVEL OTRO CLIENTE
            //si el cliente de la proforma es un CASUAL  o un SN debe pedir clave
            bool resultado = true;
            if (await cliente.EsClienteSinNombre(_context, DVTA.codcliente) || await cliente.EsClienteCasual(_context, DVTA.codcliente) && !await cliente.EsClienteSinNombre(_context, DVTA.codcliente_real) )
            {
                resultado = false;
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = "Las Ventas al cliente Sin Nombre o Casual con enlace a cliente Cliente Real-Referencia, requieren permiso especial!!!";
                regcontrol.ObsDetalle= "";
                regcontrol.DatoA = DVTA.id + "-" + DVTA.numeroid + " " + DVTA.codcliente + "-" + DVTA.codcliente_real;
                regcontrol.DatoB = "Total: " + DVTA.totaldoc + " (" + DVTA.codmoneda + ")";
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = Acciones_Validar.Pedir_Servicio.ToString();
            }
            return resultado;
        }
        private async Task<bool> Control_Valido_C00012Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA)
        {
            //12 VERIFICAR TIPO DE PREPARACION DE CLIENTE FINAL
            bool resultado = true;
            regcontrol.Valido = "SI";
            regcontrol.Observacion = "";
            regcontrol.ObsDetalle = "";
            regcontrol.DatoA = "";
            regcontrol.DatoB = "";
            regcontrol.ClaveServicio = "";
            regcontrol.Accion = Acciones_Validar.Ninguna.ToString();

            if (!await cliente.EsClienteSinNombre(_context, DVTA.codcliente_real) || !await cliente.EsClienteCasual(_context, DVTA.codcliente_real))
            {
                if (await cliente.EsClienteFinal(_context, DVTA.codcliente_real))
                {
                    if(DVTA.preparacion !="FINAL" || DVTA.preparacion != "URGENTE")
                    {
                        resultado = false;
                        regcontrol.Valido = "NO";
                        //regcontrol.DescServicio = "";
                        regcontrol.Observacion = "El cliente: " + DVTA.codcliente_real + " es un cliente final por tanto el tipo de preparacion de un cliente final puede ser: FINAL o URGENTE. Corrija esta situacion!!!";
                        regcontrol.ObsDetalle = "";
                        regcontrol.DatoA = "";
                        regcontrol.DatoB = "";
                        regcontrol.ClaveServicio = "";
                        regcontrol.Accion = Acciones_Validar.Ninguna.ToString();
                    }

                }
                
            }
            return resultado;
        }
        private async Task<bool> Control_Valido_C00013Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA)
        {
            //DESCTOS NIVEL DE CLIENTE COMPETENCIA EN PROFORMA SIN NOMBRE
            bool resultado = true;
            regcontrol.Valido = "SI";
            regcontrol.Observacion = "";
            regcontrol.ObsDetalle = "";
            regcontrol.DatoA = "";
            regcontrol.DatoB = "";
            regcontrol.ClaveServicio = "";
            regcontrol.Accion = Acciones_Validar.Ninguna.ToString();

            if (await cliente.EsClienteSinNombre(_context, DVTA.codcliente) || await cliente.EsClienteCasual(_context, DVTA.codcliente) && !await cliente.EsClienteSinNombre(_context, DVTA.codcliente_real))
            {
                if (await cliente.EsClienteCompetencia(_context, await cliente.NIT(_context, DVTA.codcliente_real)))
                {
                    resultado = false;
                    regcontrol.Valido = "NO";
                        //regcontrol.DescServicio = "";
                    regcontrol.Observacion = "El cliente real-referencia: " + DVTA.codcliente_real + " esta clasificado como competencia por lo cual para realizar este cliente compras a otro nombre debe ingresar permiso especial!!!";
                    regcontrol.ObsDetalle = "";
                    if (DVTA.estado_doc_vta.ToUpper() == "NUEVO")
                    {
                        regcontrol.DatoA = DVTA.codcliente + "-" + DVTA.codcliente_real + " " + DVTA.nitfactura;
                    }
                    else
                    {
                        regcontrol.DatoA = DVTA.id + "-" + DVTA.numeroid + " " + DVTA.codcliente + "-" + DVTA.codcliente_real;
                    }
                    regcontrol.DatoB = "Total: " + DVTA.totaldoc + " (" + DVTA.codmoneda + ")";
                    regcontrol.ClaveServicio = "";
                    regcontrol.Accion = Acciones_Validar.Pedir_Servicio.ToString();
                }

            }
            return resultado;
        }
        private async Task<bool> Control_Valido_C00014Async(DBContext _context, Controles regcontrol, List<vedesextraDatos> tabladescuentos)
        {
            //VERIFICAR SI LOS DESCUENTOS EXTRAS ESTAN VIGENTES
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Verificar_Descuentos_Extras_Habilitados(_context, tabladescuentos);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00015Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos)
        {
            //VERIFICAR FECHA VIGENCIA DESCTOS. EXTRAS VERSUS FECHA INICIAL PROFORMA
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Vigencia_DesctosExtras_Segun_FechasAsync(_context, DVTA, tabladescuentos);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00016Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            //VERIFICAR SI HAY PROMOCIONES PENDIENTES DE APLICAR
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Promociones_Por_Aplicar(_context, DVTA, tabladescuentos,tabladetalle, false, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }

        private async Task<bool> Control_Valido_C00017Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            //##VALIDAR DESCUENTO POR DEPOSITO
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Descuento_Por_Deposito(_context, DVTA, tabladescuentos, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DescServicio = "";
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00018Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            //##VALIDAR DESCUENTO EXTRA REQUIERE CREDITO VALIDO
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Descuento_Extra_Requiere_Credito_Valido(_context, DVTA.codcliente_real, tabladescuentos, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DescServicio = "";
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00019Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, List<vedetalleanticipoProforma> tablaanticipos, string codempresa)
        {
            //VALIDAR DESCUENTO EXTRA SEGÚN TIPO DE VENTA (CONTADO CE, CONTADO CA, CREDITO)
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Descuentos_Extra_Para_TipoVenta(_context, DVTA, tabladescuentos, tablaanticipos, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DescServicio = "";
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00020Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            //VALIDAR MONTO MINIMO SEGÚN LISTA DE PRECIOS
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Monto_Minimos_Segun_Lista_Precio(_context, DVTA, tabladetalle, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                if (DVTA.estado_doc_vta.ToUpper() == "NUEVO")
                {
                    regcontrol.DatoA = DVTA.codcliente + "-" + DVTA.codcliente_real + "-" + DVTA.nitfactura;
                }
                else
                {
                    regcontrol.DatoA = DVTA.id + "-" + DVTA.numeroid + "-" + DVTA.codcliente_real;
                }
                regcontrol.DatoB = "SubTotal: " + DVTA.subtotaldoc + " (" + DVTA.codmoneda + ")";
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00021Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            //VALIDAR MONTO MINIMO PARA ENTREGAR AL CLIENTE
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Monto_Minimo_Para_Entrega_Pedido(_context, DVTA, tabladetalle, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = "";
                regcontrol.DatoB = "";
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00022Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            //VALIDAR MONTO MINIMO PARA APLICAR DESCUENTOS ESPECIALES
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Monto_Minimo_Para_Aplicar_Descuentos_Especiales(_context, DVTA.codcliente_real, tabladetalle, DVTA.codmoneda, DVTA.fechadoc.Date, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                if (DVTA.estado_doc_vta.ToUpper() == "NUEVO")
                {
                    regcontrol.DatoA = DVTA.codcliente_real + "-" + DVTA.nitfactura;
                }
                else
                {
                    regcontrol.DatoA = DVTA.id + "-" + DVTA.numeroid ;
                }
                regcontrol.DatoB = "SubTotal: " + DVTA.subtotaldoc + " (" + DVTA.codmoneda + ")";
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00023Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            //##VALIDAR CUMPLIMIENTO DE EMPAQUE MINIMO SEGUN LISTA DE PRECIOS
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Empaque_Minimo_Segun_Lista_Precios(_context,tabladetalle, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00024Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA,  string codempresa, string usuario)
        {
            //VALIDAR CREDITO DISPONIBLE
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Credito_Disponible_Para_Vta(_context, DVTA, codempresa, usuario);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00025Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedetalleEtiqueta> dt_etiqueta, string codempresa)
        {
            //VALIDAR ETIQUETA DE LA PROFORMA
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Etiqueta_Proforma(_context, dt_etiqueta, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00026Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedetalleEtiqueta> dt_etiqueta, string codempresa)
        {
            //VALIDAR CUMPLIMIENTO DE EMPAQUE MINIMO SEGUN LISTA DE PRECIOS
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Etiqueta_Proforma(_context, dt_etiqueta, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00027Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            //VERIFICAR SI EL CLIENTE TIENE NOTAS DE REMISION (reversiones pp) PENDIENTES DE PAGO
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Cliente_Tiene_Reversiones_PP_Pendientes_de_Pago(_context, tabladescuentos,DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00028Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa, string usuario)
        {
            //VERIFICAR QUE EL CLIENTE Y CLIENTE REAL SEA ATENDIDO POR EL VENDEDOR
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Cliente_Es_Atendido_Por_El_Vendedor(_context, DVTA, codempresa, usuario);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00029Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa, string usuario)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Periodo_Abierto_Para_Venta(_context, DVTA, codempresa, usuario);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00030Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa, string usuario)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_NIT_Valido(_context, DVTA, codempresa, usuario);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00031Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa, string usuario)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Venta_Credito_NIT_Dfte_Cero(_context, DVTA, codempresa, usuario);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00032Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa, string usuario)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Monto_Limite_Factura_Sin_Nombre(_context, DVTA, codempresa, usuario);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00033Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Verificar_Descuentos_Extras_Aplicados_Validos(_context, DVTA, tabladetalle, tabladescuentos, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00034Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Verificar_Tipo_Transporte_Valido_Para_Entregar_Pedido(_context, DVTA, tabladetalle, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00035Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Verificar_Nombre_Transporte_Valido_Para_Entregar_Pedido(_context, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00036Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Quien_Cancelara_Flete(_context, DVTA.fletepor, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00037Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Otro_Medio_de_Transporte(_context, DVTA.transporte, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                if (DVTA.estado_doc_vta == "NUEVO")
                {
                    regcontrol.DatoA = DVTA.nitfactura;
                }
                else
                {
                    regcontrol.DatoA = DVTA.id + "-" + DVTA.numeroid;
                }
                regcontrol.DatoB = "Total: " + DVTA.totaldoc.ToString("####,##0.000", new CultureInfo("en-US")) + "(" + DVTA.codmoneda + ")";
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00038Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedetalleanticipoProforma> tablaanticipos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Venta_Contado_NOCE_Tenga_Anticipo(_context, DVTA, tablaanticipos, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DescServicio = "";
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00039Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Venta_Contado_Contra_Entrega_Cliente_Del_Interior(_context, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                if (objres.accion.ToString() != Acciones_Validar.Pedir_Servicio.ToString())
                {
                    //si la validacion devolvio NO PEDIR SERVICIO, que se quite el codservicio y el desc servicio
                    regcontrol.CodServicio = "0";
                    regcontrol.DescServicio = "";
                }
                regcontrol.ClaveServicio = objres.accion.ToString();
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00040Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Venta_Contado_Contra_Entrega_Con_Anticipo(_context, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = objres.accion.ToString();
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00041Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Direccion_Entrega(_context, DVTA.codcliente_real, DVTA.direccion, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = objres.accion.ToString();
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00042Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Verificar_Si_Hay_Descuentos_Extras_Repetidos(_context, tabladescuentos, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00043Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Georeferencia_Entrega_Valida(_context, DVTA.latitud, DVTA.longitud, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = objres.accion.ToString();
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00044Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Cliente_Tiene_Cuentas_Por_Pagar_En_Mora(_context, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = objres.accion.ToString();
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00045Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            //objres = await Validar_Cliente_Tiene_Cuentas_Por_Pagar_En_Mora(_context, DVTA, codempresa);
            //if (objres.resultado == false)
            //{
            //    regcontrol.Valido = "NO";
            //    regcontrol.DescServicio = "";
            //    regcontrol.Observacion = objres.observacion;
            //    regcontrol.ObsDetalle = objres.obsdetalle;
            //    regcontrol.DatoA = objres.datoA;
            //    regcontrol.DatoB = objres.datoB;
            //    regcontrol.ClaveServicio = objres.accion.ToString();
            //    regcontrol.Accion = objres.accion.ToString();
            //}
            return true;
        }
        private async Task<bool> Control_Valido_C00046Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Precio_de_Venta_Permitido_Cliente(_context, tabladetalle, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00047Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Verificar_Monto_Maximo_De_Venta_Cliente(_context,DVTA.codcliente_real,DVTA.codalmacen ,DVTA.fechadoc , tabladetalle);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00048Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Venta_Cliente_SinCuenta_En_Oficina(_context, DVTA , tabladetalle, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00049Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Venta_Cliente_ConCuenta_En_Oficina(_context, DVTA, tabladetalle, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00050Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<verecargosDatos> tablarecargos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Pedido_Urgente_Cliente(_context, DVTA, tabladetalle, tablarecargos, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00051Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Montos_Minimos_Para_Desctos_Extras(_context, tabladescuentos, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00052Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Peso_Minimo_Requerido_Para_Alicar_Descuentos_Extras(_context, tabladetalle, tabladescuentos, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00053Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Monto_Venta_Para_Bancarizacion(_context, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00054Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Venta_Cliente_Solo_Contra_Entrega(_context, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00055Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Verificar_No_Se_Venda_A_Credito_A_Cliente_SinNombre(_context, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00056Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedetalleEtiqueta> dt_etiqueta, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Direccion_Etiqueta_Correcta(_context, DVTA,dt_etiqueta, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00057Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Descuento_Extra_Aplicado_Segun_Tipo_Vta(_context,tabladescuentos, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00058Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<Dtnocumplen> dtnocumplen, string codempresa, string usuario)
        {
            //VALIDAR PORCENTAJE MAXIMO DE VENTA
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            //objres = await Validar_Saldos_Negativos_Doc(_context, tabladetalle, DVTA, dtnegativos, codempresa, usuario);
            (objres, dtnocumplen) = await Validar_Limite_Maximo_de_Venta(_context, tabladetalle, DVTA, dtnocumplen, codempresa, usuario);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
                regcontrol.Dtnocumplen = dtnocumplen;
            }
            regcontrol.Dtnocumplen = dtnocumplen;
            return true;
        }
        private async Task<bool> Control_Valido_C00059Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Venta_Con_Desctos_Nivel_anterior(_context, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = objres.accion.ToString();
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00060Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<Dtnegativos> dtnegativos, string codempresa, string usuario)
        {
            //VALIDAR SALDOS NEGATIVOS
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            //objres = await Validar_Saldos_Negativos_Doc(_context, tabladetalle, DVTA, dtnegativos, codempresa, usuario);
            (objres, dtnegativos) = await Validar_Saldos_Negativos_Doc(_context, tabladetalle, DVTA, dtnegativos, codempresa, usuario);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
                regcontrol.Dtnegativos = dtnegativos;
            }
            regcontrol.Dtnegativos = dtnegativos;
            return true;
        }
        private async Task<bool> Control_Valido_C00061Async(DBContext _context, Controles regcontrol, bool pedido_por_dia, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<verecargosDatos> tablarecargos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Nro_Pedido_Urgentes_Cliente(_context, pedido_por_dia, DVTA, tabladetalle, tablarecargos, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00063Async(DBContext _context, Controles regcontrol, bool pedido_por_dia, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<verecargosDatos> tablarecargos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Monto_Minimo_Pedido_Urgente_Provincia(_context, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00064Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<vedetalleanticipoProforma> tablaanticipos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Anticipos_Asignados_A_Proforma(_context, DVTA, tablaanticipos, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DescServicio = "";
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00065Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Monto_Venta_Cliente_Final(_context, DVTA, tabladetalle, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                if (DVTA.estado_doc_vta == "NUEVO")
                {
                    regcontrol.DatoA =DVTA.codcliente + "-" + DVTA.codcliente_real + "-" + DVTA.nitfactura;
                }
                else
                {
                    regcontrol.DatoA = DVTA.id + "-" + DVTA.numeroid + "-" + DVTA.codcliente_real;
                }
                regcontrol.DatoB = "SubTotal: " + DVTA.subtotaldoc.ToString("####,##0.000", new CultureInfo("en-US")) + " (" + DVTA.codmoneda + ")";
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        public async Task<ResultadoValidacion> Validar_Monto_Venta_Cliente_Final(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena = "";
            DataTable tabla_unidos = new DataTable();
            int i, k;
            DataRow dr;
            bool es_cliente_final = await cliente.EsClienteFinal(_context, DVTA.codcliente_real);

            InicializarResultado(objres);

            //si no es cliente final no debe controlar esto y se sale con resultado TRUE
            if (es_cliente_final == false)
            {
                return objres;
            }
            tabla_unidos.Columns.Add("codtarifa", typeof(int));
            tabla_unidos.Columns.Add("total", typeof(double));

            // Elaborar una tabla con el tipo precio y el total
            foreach (var detalle in tabladetalle)
            {
                dr = tabla_unidos.NewRow();
                dr["codtarifa"] = detalle.codtarifa;
                dr["total"] = detalle.total;
                tabla_unidos.Rows.Add(dr);
            }

            if (tabla_unidos.Rows.Count > 0)
            {
                List<int> precios = new List<int>();
                ArrayList totales = new ArrayList();
                decimal monto_desde, monto_hasta, dif;

                //sacar precios
                foreach (DataRow row in tabla_unidos.Rows)
                {
                    if (!precios.Contains(int.Parse(row["codtarifa"].ToString())))
                    {
                        precios.Add(int.Parse(row["codtarifa"].ToString()));
                    }
                }

                //sacartotales
                for (i = 0; i < precios.Count; i++)
                {
                    totales.Add(0.0);
                    for (k = 0; k < tabla_unidos.Rows.Count; k++)
                    {
                        if (Convert.ToInt32(tabla_unidos.Rows[k]["codtarifa"]) == Convert.ToInt32(precios[i]))
                        {
                            totales[i] = Convert.ToDouble(totales[i]) + Convert.ToDouble(tabla_unidos.Rows[k]["total"]);
                        }
                    }
                }

                // Comparar y mostrar
                DataTable tabla = new DataTable();
                DataRow[] registro;

                var query = from t in _context.intarifa
                            orderby t.codigo
                            select new { t.codigo, t.vta_cte_final_desde, t.vta_cte_final_hasta, t.moneda, t.tipo };
                var result = query.Distinct().ToList();
                tabla = funciones.ToDataTable(result);

                i = 0;
                foreach (int precio in precios)
                {

                    registro = tabla.Select("codigo=" + precio + " ");

                    if (registro.Length == 0)
                    {
                        //si no hay precios
                        cadena += Environment.NewLine + "No se econtro en la tabla de precios, los parametros del tipo de precio: " + precio + ", consulte con el administrador del sistema!!!";
                    }
                    else
                    {
                        //obtener el monto minimo de venta a cliente final
                        monto_desde = await tipocambio._conversion(_context, DVTA.codmoneda, registro[0]["moneda"].ToString(), DVTA.fechadoc.Date,Convert.ToDecimal(registro[0]["vta_cte_final_desde"].ToString()));
                        monto_desde = Math.Round(monto_desde, 2);
                        //obtener el monto minimo de venta a cliente final
                        monto_hasta = await tipocambio._conversion(_context, DVTA.codmoneda, registro[0]["moneda"].ToString(), DVTA.fechadoc.Date, Convert.ToDecimal(registro[0]["vta_cte_final_hasta"].ToString()));
                        monto_hasta = Math.Round(monto_hasta, 2);

                        if (registro[0]["tipo"].ToString() == "MAYORISTA")
                        {
                            if (await cliente.Controla_Monto_Minimo(_context, DVTA.codcliente_real))
                            {
                                ResultadoValidacion objres2 = new ResultadoValidacion();
                                objres2.resultado = true;
                                objres2.observacion = "";
                                objres2.obsdetalle = "";
                                objres2.datoA = "";
                                objres2.datoB = "";
                                objres2.accion = Acciones_Validar.Ninguna;
                                //validar el monto minimo de lista de precio mayorista
                                cadena = "";
                                objres2 = await Validar_Monto_Minimos_Segun_Lista_Precio(_context, DVTA, tabladetalle, codempresa);
                                if (objres2.resultado == false)
                                {
                                    cadena = objres2.observacion;
                                    cadena += Environment.NewLine + objres2.obsdetalle;
                                }
                                else { cadena = ""; }
                            }
                            else
                            { cadena = "";}
                        }
                        else
                        {
                            //verificar si cumple con el minimo
                            if (Convert.ToDecimal(totales[i].ToString()) < monto_desde)
                            {
                                cadena += Environment.NewLine + "Monto Minimo Vta Precio: " + precios + " es: " + monto_desde + "(" + DVTA.codmoneda + ") Monto actual es: " + Math.Round(Convert.ToDecimal(totales[i].ToString()), 2).ToString();
                            }
                            //verificar si cumple con el maximo
                            if (Convert.ToDecimal(totales[i].ToString()) > monto_hasta)
                            {
                                cadena += Environment.NewLine + "Monto Maximo Vta Precio: " + precios + " es: " + monto_hasta + "(" + DVTA.codmoneda + ") Monto actual es: " + Math.Round(Convert.ToDecimal(totales[i].ToString()), 2).ToString();
                            }
                        }
                    }
                    i = i + 1;
                }
                registro = null;
                tabla.Dispose();
            }

            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.datoA = "";
                objres.datoB = "";
                objres.observacion = "El cliente: " + DVTA.codcliente_real + " es cliente final, se encontraron observaciones en el monto de venta:";
                objres.obsdetalle = cadena;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Anticipos_Asignados_A_Proforma(DBContext _context, DatosDocVta DVTA, List<vedetalleanticipoProforma> dt_anticipo_pf, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);

            objres =await  anticipos_vta_contado.Validar_Anticipo_Asignado_2(_context, true, DVTA, dt_anticipo_pf, codempresa);

            return objres;
        }
        
        public async Task<ResultadoValidacion> Validar_Monto_Minimo_Pedido_Urgente_Provincia(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);

            string cadena = "";
            if (DVTA.preparacion == "URGENTE PROVINCIAS")
            {//validar monto minimo 
                double monto_min = await configuracion.monto_minimo_venta_urgente_provincia(_context, codempresa);
                double monto_min_aux = await configuracion.monto_minimo_venta_urgente_provincia(_context, codempresa);
                string moneda_monto_min = await configuracion.moneda_monto_minimo_venta_urgente_provincia(_context, codempresa);

                if (DVTA.codmoneda == moneda_monto_min)
                {
                    if (monto_min > DVTA.subtotaldoc)
                    {
                        cadena += "\n El monto minimo para pedidos urgentes a provincia es de: " + monto_min.ToString("####,##0.000", new CultureInfo("en-US")) + "(" + DVTA.codmoneda + ")" + " y la proforma actual no cumple con este requisito.";
                    }
                }
                else
                {
                    monto_min_aux = (double)await tipocambio._conversion(_context, DVTA.codmoneda, moneda_monto_min, await funciones.FechaDelServidor(_context), (decimal)monto_min);
                    if (monto_min_aux > DVTA.subtotaldoc)
                    {
                        cadena += "\n El monto minimo de pedidos urgentes a provincia es de: " + monto_min_aux.ToString("####,##0.000", new CultureInfo("en-US")) + "(" + DVTA.codmoneda + ")" + " y la proforma actual no cumple con este requisito.";
                        objres.resultado = false;
                    }
                }
            }

            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = "Se encontraron observaciones del pedido urgente: ";
                objres.obsdetalle = cadena;
                objres.datoA = DVTA.nitfactura;
                objres.datoB = "Subtotal: " + DVTA.subtotaldoc.ToString("####,##0.000", new CultureInfo("en-US")) + " (" + DVTA.codmoneda + ")";
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Nro_Pedido_Urgentes_Cliente(DBContext _context, bool verificar_pordia, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<verecargosDatos> tablarecargos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);

            string cadena = "";
            if (DVTA.preparacion == "URGENTE" || DVTA.preparacion == "URGENTE PROVINCIAS")
            {
                int nro_urg_dia = await cliente.DiaVentasUrgentes(_context, DVTA.codcliente_real, DVTA.fechadoc.Date);
                int nro_urg_dia_max = await empresa.maxurgentes_por_dia(_context, codempresa);

                int nro_urg_semana = await cliente.SemanaVentasUrgentes(_context, DVTA.codcliente_real, DVTA.fechadoc.Date);
                int nro_urg_semana_max = await empresa.maxurgentes(_context, codempresa);

                if (verificar_pordia)
                {//validar nro de pedidos urgentes por dia
                    if(nro_urg_dia >= nro_urg_dia_max)
                    {
                        cadena = "Un cliente solo puede tener " + nro_urg_dia_max + " pedidos urgentes al dia, y el cliente: " + DVTA.codcliente_real + " tiene actualmente: " + nro_urg_dia;
                    }
                }
                else
                {
                    if (nro_urg_semana >= nro_urg_semana_max)
                    {
                        cadena = "Un cliente solo puede tener " + nro_urg_semana_max + " pedidos urgentes desde inicio de semana(lunes), y el cliente: " + DVTA.codcliente_real + " actualmente tiene: " + nro_urg_semana;
                        objres.resultado = false;
                    }
                }
            }

            if (cadena.Trim().Length > 0 )
            {
                objres.resultado = false;
                objres.observacion = "Se encontraron observaciones del pedido urgente: ";
                objres.obsdetalle = cadena;
                objres.datoA = DVTA.nitfactura;
                objres.datoB = "Subtotal: " + DVTA.subtotaldoc.ToString("####,##0.000", new CultureInfo("en-US")) + " (" + DVTA.codmoneda + ")";
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Venta_Con_Desctos_Nivel_anterior(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            InicializarResultado(objres);

            if (DVTA.niveles_descuento == "ANTERIOR" )
            {
                objres.resultado = false;
                objres.observacion = "Las ventas con descuentos de nivel ANTERIOR requieren permiso especial!!!";
                objres.obsdetalle = "";
                if(DVTA.estado_doc_vta == "NUEVO")
                {
                    objres.datoA = DVTA.nitfactura;
                    objres.datoB = "Total: " + DVTA.totaldoc.ToString("####,##0.000", new CultureInfo("en-US")) + " (" + DVTA.codmoneda + ")";
                }
                else
                {
                    objres.datoA = DVTA.id;
                    objres.datoB = DVTA.numeroid;
                }
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Descuento_Extra_Aplicado_Segun_Tipo_Vta(DBContext _context, List<vedesextraDatos> tabladescuentos, DatosDocVta DVTA, string codempresa)
        {

            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);

            string cadena = "";
            string tipo_vta_descto = "";


            foreach (var descuentos in tabladescuentos)
            {
                tipo_vta_descto = await ventas.Descuento_Extra_Tipo_Venta(_context, descuentos.coddesextra);
                if (DVTA.tipo_vta == "CREDITO")
                {
                    if (tipo_vta_descto == "CREDITO" || tipo_vta_descto == "SIN RESTRICCION")
                    {
                        //OK
                    }
                    else
                    {
                        cadena += "\n " + descuentos.coddesextra + "-" + descuentos.descripcion;
                    }
                }
                else
                {
                    if (tipo_vta_descto == "CONTADO" || tipo_vta_descto == "SIN RESTRICCION")
                    {
                        //OK
                    }
                    else
                    {
                        cadena += "\n " + descuentos.coddesextra + "-" + descuentos.descripcion;
                    }
                }
            }
            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = "Los siguientes descuentos no pueden aplicarse en ventas al: " + DVTA.tipo_vta;
                objres.obsdetalle = "\n" + cadena;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Direccion_Etiqueta_Correcta(DBContext _context, DatosDocVta DVTA, List<vedetalleEtiqueta> dt_etiqueta, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);

            if (!await cliente.EsClienteSinNombre(_context, DVTA.codcliente))
            {
                if (dt_etiqueta.Count > 0)
                {
                    foreach (var etiqueta in dt_etiqueta)
                    {
                        if (await cliente.direccion_es_valida(_context, DVTA.codcliente_real, etiqueta.representante))
                        {
                            objres.resultado = true;
                            objres.observacion = "";
                            objres.obsdetalle = "";
                            objres.datoA = "";
                            objres.datoB = "";
                            objres.accion = Acciones_Validar.Ninguna;
                        }
                        else
                        {
                            if (!await cliente.direccion_con_pto_vta_es_valida(_context, DVTA.codcliente, etiqueta.representante))
                            {
                                objres.resultado = false;
                                objres.observacion = "La direccion de la etiqueta, no corresponde a una direccion valida para el cliente: " + DVTA.codcliente;
                                objres.obsdetalle = "";
                                objres.datoA = DVTA.id + "-" + DVTA.numeroid;
                                objres.datoB = DVTA.codcliente_real;
                                objres.accion = Acciones_Validar.Pedir_Servicio;
                            }

                        }
                    }
                }
                else
                {
                    objres.resultado = false;
                    objres.observacion = "No se encontro la etiqueta para la proforma!!!";
                    objres.obsdetalle = "";
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                }
            }
            else
            {
                if (!(DVTA.codcliente == DVTA.codcliente_real))
                {
                    if (await cliente.EsClienteSinNombre(_context, DVTA.codcliente) || await cliente.Es_Cliente_Casual(_context, DVTA.codcliente) && !await cliente.EsClienteSinNombre(_context, DVTA.codcliente_real))
                    {
                        if(dt_etiqueta.Count > 0)
                        {
                            foreach (var etiqueta in dt_etiqueta)
                            {
                                if (await cliente.direccion_es_valida(_context, DVTA.codcliente_real, etiqueta.representante))
                                {
                                    objres.resultado = true;
                                    objres.observacion = "";
                                    objres.obsdetalle = "";
                                    objres.datoA = "";
                                    objres.datoB = "";
                                    objres.accion = Acciones_Validar.Ninguna;
                                }
                                else
                                {
                                    if (!await cliente.direccion_con_pto_vta_es_valida(_context, DVTA.codcliente_real, etiqueta.representante))
                                    {
                                        objres.resultado = false;
                                        objres.observacion = "La direccion de la etiqueta, no corresponde a una direccion valida para el cliente: " + DVTA.codcliente_real;
                                        objres.obsdetalle = "";
                                        objres.datoA = DVTA.id + "-" + DVTA.numeroid;
                                        objres.datoB = DVTA.codcliente_real;
                                        objres.accion = Acciones_Validar.Pedir_Servicio;
                                    }

                                }

                            }
                        }
                        else
                        {
                            objres.resultado = false;
                            objres.observacion = "No se encontro la etiqueta para la proforma!!!";
                            objres.obsdetalle = "";
                            objres.datoA = "";
                            objres.datoB = "";
                            objres.accion = Acciones_Validar.Ninguna;
                        }
                        
                    }
                      
                }
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Verificar_No_Se_Venda_A_Credito_A_Cliente_SinNombre(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);

            if (DVTA.tipo_vta == "CREDITO")
            {
                if (await cliente.EsClienteSinNombre(_context, DVTA.codcliente) || await cliente.EsClienteCasual(_context, DVTA.codcliente))
                {
                    objres.resultado = false;
                    objres.observacion = "No puede hacer una venta a credito a un cliente Sin Nombre y/o Casual.";
                    objres.obsdetalle = "";
                    objres.datoA = DVTA.codcliente + "-" + DVTA.nitfactura;
                    objres.datoB = "Total: " + DVTA.totaldoc.ToString("####,##0.000", new CultureInfo("en-US")) + " (" + DVTA.codmoneda + ")";
                    objres.accion = Acciones_Validar.Pedir_Servicio;
                }
            }
            
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Venta_Cliente_Solo_Contra_Entrega(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            bool venta_es_ce = true;
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);

            venta_es_ce = DVTA.contra_entrega == "SI";

            if (await restricciones.Validar_Cliente_Contraentrega(_context, venta_es_ce, DVTA.codcliente_real))
            {
                objres.resultado = false;
                objres.observacion = "El cliente: " + DVTA.codcliente_real + " esta Registrado como 'Solo Venta Contra Entrega'. Por favor marque este documento como una venta contra-entrega antes de proseguir.";
                objres.obsdetalle = "";
                objres.datoA = DVTA.nitfactura;
                objres.datoB = "Total: " + DVTA.totaldoc.ToString("####,##0.000", new CultureInfo("en-US")) + " (" + DVTA.codmoneda + ")";
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Monto_Venta_Para_Bancarizacion(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            double MAX_RND = 0;
            double monto_convertido = 0;
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);

            MAX_RND = await configuracion.emp_monto_rnd100011(_context, codempresa);
            monto_convertido = (double)await tipocambio._conversion(_context,await Empresa.monedabase(_context, codempresa),DVTA.codmoneda, DVTA.fechadoc, (decimal)DVTA.totaldoc);

            if (monto_convertido >= MAX_RND)
            {
                objres.resultado = false;
                objres.observacion = "Como esta venta es mayor a " + MAX_RND.ToString("####,##0.000", new CultureInfo("en-US")) + " (MN) tendra que ser pagada por medio de una entidad bancaria obligatoriamente, Necesita autorizacion especial.";
                objres.obsdetalle = "";
                objres.datoA = DVTA.nitfactura;
                objres.datoB = "Total: " + DVTA.totaldoc.ToString("####,##0.000", new CultureInfo("en-US")) + " (" + DVTA.codmoneda + ")";
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Peso_Minimo_Requerido_Para_Alicar_Descuentos_Extras(DBContext _context, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, DatosDocVta DVTA, string codempresa)
        {
            string cadena = "";
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);

            if (await cliente.Controla_Monto_Minimo(_context, DVTA.codcliente_real))
            {   //calcular Peso
                double peso_doc = 0;
                foreach (var detalle in tabladetalle)
                {
                    peso_doc += detalle.cantidad * await items.itempeso(_context, detalle.coditem);
                }
                foreach (var descuentos in tabladescuentos)
                {
                    if (peso_doc < await ventas.PesoMinimoDescuentoExtra(_context, descuentos.coddesextra))
                    {
                        if (cadena.Trim().Length == 0)
                        {
                           cadena = "El descuento " + descuentos.descripcion + " no cumple el peso minimo para su aplicacion.";
                        }
                        else
                        {
                            cadena += "\n El descuento " + descuentos.descripcion + " no cumple el peso minimo para su aplicacion.";
                        }
                    }
                }
            }
            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = "Los descuentos extras aplicados no cumplen la cantidad minima en KG para su aplicacion!!!";
                objres.obsdetalle = cadena;
                objres.datoA = DVTA.nitfactura;
                objres.datoB = "Total: " + DVTA.totaldoc.ToString("####,##0.000", new CultureInfo("en-US")) + " (" + DVTA.codmoneda + ")";
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Montos_Minimos_Para_Desctos_Extras(DBContext _context, List<vedesextraDatos> tabladescuentos, DatosDocVta DVTA, string codempresa)
        {
            string cadena = "";
            double _total = 0;
            double _montoMIN = 0;
            double _dif = 0;
            //verificar el monto si cumple con el requerido
            //antes se obitene datos del complemento mayorista-Dimediado si tiene
            int _codproforma = 0;
            double _subtotal_pfcomplemento = 0;
            string _moneda_total_pfcomplemento = "";
            bool hay_enlace = false;

            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);

            if (await cliente.Controla_Monto_Minimo(_context, DVTA.codcliente_real))
            {
                try
                {
                    if (DVTA.idpf_complemento.Trim().Length > 0 && DVTA.nroidpf_complemento.Trim().Length > 0 && DVTA.tipo_complemento == "complemento_para_descto_monto_min_desextra")
                    {
                        _codproforma = await ventas.codproforma(_context, DVTA.idpf_complemento, Convert.ToInt32(DVTA.nroidpf_complemento));
                        if (Convert.ToInt32(DVTA.nroidpf_complemento) > 0)
                        {
                            _moneda_total_pfcomplemento = await ventas.MonedaPF(_context, _codproforma);
                            _subtotal_pfcomplemento = (double)await ventas.SubTotal_Proforma(_context, _codproforma);
                            _subtotal_pfcomplemento = (double)await tipocambio._conversion(_context, DVTA.codmoneda, _moneda_total_pfcomplemento, DVTA.fechadoc.Date, (decimal)_subtotal_pfcomplemento);
                            hay_enlace = true;
                        }
                    }
                }
                catch
                {
                    _moneda_total_pfcomplemento = "";
                    _subtotal_pfcomplemento = 0;
                }
                if (DVTA.tipo_vta == "CONTADO")
                {
                    //contado
                    foreach (var descuentos in tabladescuentos)
                    {
                        _total = DVTA.subtotaldoc;
                        //implementado en fecha: 07-07-2021
                        //se añade el subtotal del complemento
                        _total += _subtotal_pfcomplemento;
                        //obtener el montomin requerido por el descto extra
                        _montoMIN = (double)await ventas.MontoMinimoContadoDescuentoExtra(_context, descuentos.coddesextra, DVTA.codmoneda, DVTA.fechadoc.Date);
                        _dif = _montoMIN - _total;
                        if (_montoMIN == 0)
                        {
                            objres.resultado = true;
                        }
                        else
                        {
                            if (_total < _montoMIN)
                            {
                                if (_subtotal_pfcomplemento > 0)
                                {
                                    cadena +=  "\n Esta proforma tiene complemento";
                                }
                                cadena +=  "\n El descuento: " + descuentos.coddesextra + "-" + descuentos.descripcion + " no cumple el monto minimo para su aplicacion, el monto mínimo es: " + _montoMIN.ToString("####,##0.000", new CultureInfo("en-US")) + "(" + DVTA.codmoneda + ") y el subtotal de esta proforma: " + DVTA.subtotaldoc.ToString("####,##0.000", new CultureInfo("en-US")) + " mas su complemento de: " + _subtotal_pfcomplemento.ToString("####,##0.000", new CultureInfo("en-US")) + " es de:" + _total.ToString("####,##0.000", new CultureInfo("en-US")) + "(" + DVTA.codmoneda + ")";
                            }
                        }
                    }

                }
                else
                {
                    //credito
                    foreach (var descuentos in tabladescuentos)
                    {
                        _total = DVTA.subtotaldoc;
                        //implementado en fecha: 07-07-2021
                        //se añade el subtotal del complemento
                        _total += _subtotal_pfcomplemento;
                        //obtener el montomin requerido por el descto extra
                        _montoMIN = (double)await ventas.MontoMinimoCreditoDescuentoExtra(_context, descuentos.coddesextra, DVTA.codmoneda, DVTA.fechadoc.Date);
                        _dif = _montoMIN - _total;
                        if (_montoMIN == 0)
                        {
                            objres.resultado = true;
                        }
                        else
                        {
                            if (_total < _montoMIN)
                            {
                                if (_subtotal_pfcomplemento > 0)
                                {
                                    cadena += "\n Esta proforma tiene complemento para cumplir monto minimo de descto extra";
                                }
                                cadena += "\n El descuento: " + descuentos.coddesextra + "-" + descuentos.descripcion + " no cumple el monto minimo para su aplicacion, el monto mínimo es: " + _montoMIN.ToString("####,##0.000", new CultureInfo("en-US")) + "(" + DVTA.codmoneda + ") y el subtotal de esta proforma: " + DVTA.subtotaldoc.ToString("####,##0.000", new CultureInfo("en-US")) + " mas su complemento de: " + _subtotal_pfcomplemento.ToString("####,##0.000", new CultureInfo("en-US")) + " es de:" + _total.ToString("####,##0.000", new CultureInfo("en-US")) + "(" + DVTA.codmoneda + ")";
                            }
                        }
                    }
                }
            }
            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = "Los siguientes descuentos extras aplicados no cumplen el monto minimo requerido:";
                objres.obsdetalle = cadena;
                if(DVTA.estado_doc_vta == "NUEVO")
                {
                    objres.datoA = DVTA.nitfactura;
                }
                else
                {
                    objres.datoA = DVTA.id + "-" + DVTA.numeroid;
                }
                objres.datoB = "Subtotal: " + DVTA.subtotaldoc.ToString("####,##0.000", new CultureInfo("en-US")) + " (" + DVTA.codmoneda + ")";
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }

        public async Task<ResultadoValidacion> Validar_Pedido_Urgente_Cliente(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<verecargosDatos> tablarecargos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);

            string cadena = "";
            string cadena_msg_empaques = "";
            string cadena_no_cumple_empaque = "";
            int nro_items;
            int nro_items_no_cumple_empaque = 0;
            int nro_items_max;

            if (DVTA.preparacion == "URGENTE" || DVTA.preparacion == "URGENTE PROVINCIAS")
            {
                nro_items = tabladetalle.Count;
                nro_items_max = await empresa.maxitemurgentes(_context, codempresa);

                // Crear un diccionario para contar las ocurrencias de cada coditem
                Dictionary<string, int> codItemCount = new Dictionary<string, int>();

                // Desde 04/11/2024 validar que si hay items repetidos verificar si alguno de ellos cumple con empaque caja cerrado
                // si es así, restar ese item al total nroitem del detalle para esta validación
                // Primero, recorremos la lista para contar las ocurrencias de cada coditem
                foreach (var item in tabladetalle)
                {
                    string codItem = item.coditem;

                    if (codItemCount.ContainsKey(codItem))
                    {
                        codItemCount[codItem] += 1;
                    }
                    else
                    {
                        codItemCount[codItem] = 1;
                    }
                }

                // Luego, identificamos los duplicados y validamos las condiciones
                foreach (var codItemDuplicado in codItemCount)
                {
                    if (codItemDuplicado.Value > 1) // Si el coditem está duplicado
                    {
                        // Obtenemos los items de la lista que tienen el mismo coditem
                        var itemsDuplicados = tabladetalle.Where(x => x.coditem == codItemDuplicado.Key).ToList();

                        foreach (var item in itemsDuplicados)
                        {
                            // Condición adicional
                            if (item.coddescuento > 0)
                            {
                                nro_items = nro_items - 1;
                            }
                        }
                    }
                }

                //verificar el nro de items del pedido
                if (nro_items > nro_items_max)
                {
                    cadena = $"\nPara un pedido URGENTE el numero maximo de items es {nro_items_max}, y el pedido actual tiene: {nro_items} items.";
                    objres.resultado = false;
                }
            }

            if (DVTA.preparacion == "URGENTE PROVINCIAS")
            {//validar el recargo para pedidos urgentes provincias
                int codrecargo_pedido_urg_provincia = await configuracion.emp_codrecargo_pedido_urgente_provincia(_context, codempresa);
                List<int> lst_rec_prov = tablarecargos.AsEnumerable().Where(r => r.codrecargo == codrecargo_pedido_urg_provincia).Select(r => r.codrecargo).ToList();

                if (lst_rec_prov.Count == 0)
                {
                    cadena += $"\nTodo pedido urgente con envio a provincias, debe incluir el recargo de envio: {codrecargo_pedido_urg_provincia} por favor añada el recargo.";
                }
            }
            //verificar si cumple los empaque
            //aclaracion de marlen en fecha: 30-09-2021 (en pedidos urgentes todo debe controlar, por mas que sea final)
            //Validar empaques Cerrados
            if (DVTA.preparacion == "URGENTE" || DVTA.preparacion == "URGENTE PROVINCIAS")
            {
                int nro_items_urgentes_debe_cumplir_empaque_cerrado = await empresa.nro_items_urgentes_empaque_cerrado(_context, codempresa);

                foreach (var row in tabladetalle)
                {
                    if (await ventas.Tarifa_EmpaqueCerrado(_context, row.codtarifa))
                    {
                        if (!await ventas.CumpleEmpaqueCerrado(_context, row.coditem, row.codtarifa, row.coddescuento, (decimal)row.cantidad, DVTA.codcliente_real))
                        {
                            cadena_no_cumple_empaque += $"\n{row.coditem} {funciones.Rellenar(row.descripcion, 20, " ", false)} {funciones.Rellenar(row.medida, 14, " ", false)}  {funciones.Rellenar(row.cantidad.ToString("####,##0.000", new CultureInfo("en-US")), 12, " ")}";
                            nro_items_no_cumple_empaque++;
                        }
                    }
                }

                if (!(nro_items_no_cumple_empaque < nro_items_urgentes_debe_cumplir_empaque_cerrado))
                {
                    cadena_msg_empaques = $"En pedidos urgentes, {nro_items_urgentes_debe_cumplir_empaque_cerrado} deben cumplir empaque cerrado, y en este pedido existen: {nro_items_no_cumple_empaque} items que no cumplen con empaque cerrado:";
                }
                else
                {
                    cadena_msg_empaques = "";
                }
            }

            if (cadena.Trim().Length > 0 || cadena_msg_empaques.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = "Se encontraron observaciones al pedido urgente: ";
                objres.obsdetalle = cadena + "\n" + cadena_msg_empaques + "\n" + cadena_no_cumple_empaque;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }

        public async Task<ResultadoValidacion> Validar_Venta_Cliente_ConCuenta_En_Oficina(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            string cadena = "";

            if (DVTA.vta_cliente_en_oficina == false)
            {
                return objres;
            }
            ///////////////////////////////////////////////
            //SI ES VENTA A CLIENTE CON CODIGO
            //'///////////////////////////////////////////////

            if (!await cliente.EsClienteSinNombre(_context, DVTA.codcliente) )
            {//validar venta con codigo
                int codempaque_vta_oficina = await configuracion.empaque_venta_cliente_en_oficina(_context, codempresa);
                int nro_empaques_minimo = await configuracion.Nro_Empaques_Minimo_Vta_Oficina(_context, codempresa);
                int maximo_items_conteo = await configuracion.Nro_Items_Maximo_Conteo_Vta_Oficina(_context, codempresa);
                int nro_empaques_cerrados = 0;
                int nro_items_conteo = 0;
                List<dynamic> tbl_control = new List<dynamic>();

                foreach (var reg in tabladetalle)
                {
                    string coditem = reg.coditem.ToString();
                    double cantidad_pedido = Convert.ToDouble(reg.cantidad);
                    double cantidad_empaque_correcto = await ventas.Empaque(_context,codempaque_vta_oficina, coditem);
                    nro_empaques_cerrados = 0;
                    if (cantidad_empaque_correcto != 0)
                    {
                        // Realizar la división solo si el valor del empaque correcto es diferente de cero
                        nro_empaques_cerrados = (int)(cantidad_pedido / cantidad_empaque_correcto);
                    }   
                    double sobrante_de_empaque = cantidad_pedido % cantidad_empaque_correcto;
                    int es_conteo = sobrante_de_empaque > 0 ? 1 : 0;
                    nro_items_conteo += es_conteo;

                    tbl_control.Add(new { coditem, cantidad_pedido, cantidad_empaque_correcto, nro_empaques_cerrados, sobrante_de_empaque, es_conteo });
                }

                nro_empaques_cerrados = tbl_control.Sum(reg => reg.nro_empaques_cerrados);

                if (nro_empaques_cerrados < nro_empaques_minimo)
                {
                    cadena += $"\nLa venta en oficina a clientes(con codigo de cliente) debe contener al menos: {nro_empaques_minimo} empaques {await nombres.nombreempaque(_context, codempaque_vta_oficina)} y el pedido solo tiene solo: {nro_empaques_cerrados} items con empaque cerrado.";
                }

                if (nro_items_conteo > maximo_items_conteo)
                {
                    cadena += $"\nEl numero maximo de items de conteo para venta en oficina a cliente(con codigo de cliente) es: {maximo_items_conteo} y la proforma actualmente tiene: {nro_items_conteo} items de conteo, verifique esta situacion!!!";
                }

                if (cadena.Trim().Length > 0)
                {
                    objres.resultado = false;
                    objres.observacion = "Se tienen las siguientes observaciones a la venta en oficina a cliente con cuenta:";
                    objres.obsdetalle = cadena;
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                }

            }

            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Venta_Cliente_SinCuenta_En_Oficina(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);

            if (DVTA.vta_cliente_en_oficina == false)
            {
                return objres;
            }
            ///////////////////////////////////////////////
            //SI ES VENTA A CON CODIGO SIN NOMBRE
            //'///////////////////////////////////////////////
            
            if (await cliente.EsClienteSinNombre(_context, DVTA.codcliente) || await cliente.EsClienteCasual(_context, DVTA.codcliente))
            {//validar venta minima cliente sin nombre
                double monto_min_sinnombre = await configuracion.monto_minimo_venta_cliente_en_oficina(_context, codempresa);
                string codmoneda_monto_mins_sn = await configuracion.moneda_monto_minimo_venta_cliente_en_oficina(_context, codempresa);
                double monto_min_requerido = monto_min_sinnombre;
                
                if(DVTA.codmoneda != codmoneda_monto_mins_sn)
                {
                    monto_min_requerido = (double)await tipocambio._conversion(_context, DVTA.codmoneda, codmoneda_monto_mins_sn, await funciones.FechaDelServidor(_context), (decimal)monto_min_sinnombre);
                    monto_min_requerido = Math.Round(monto_min_requerido, 2);
                }
                if (monto_min_requerido > DVTA.subtotaldoc)
                {
                    objres.resultado = false;
                    objres.observacion = "El monto minimo para venta a cliente(Sin Nombre o Casual) en oficina es de: " + monto_min_requerido.ToString("####,##0.000", new CultureInfo("en-US")) + " (" + DVTA.codmoneda + ") y el subtotal de la proforma actual es: " + DVTA.subtotaldoc.ToString("####,##0.000", new CultureInfo("en-US")) + " (" + DVTA.codmoneda + ") y es menor a este minimo, verifique esta situacion!!!";
                    objres.obsdetalle = "";
                    objres.datoA = DVTA.nitfactura;                    
                    objres.datoB = "SubTotal: " + DVTA.subtotaldoc.ToString("####,##0.000", new CultureInfo("en-US")) + " (" + DVTA.codmoneda + ")";
                    objres.accion = Acciones_Validar.Pedir_Servicio;
                }
                else
                {
                    objres.resultado = true;
                    objres.observacion = "";
                    objres.obsdetalle = "";
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                }
            }

            return objres;
        }
        public async Task<ResultadoValidacion> Verificar_Monto_Maximo_De_Venta_Cliente(DBContext _context, string codcliente_real, string codalmacen,DateTime fecha, List<itemDataMatriz> tabladetalle)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            string cadena_items = "";
            string cadena_solo_items = "";
            double max = 0;
            int diascontrol = 0;
            double cantidad_ttl_vedida = 0;

            //esta funcion verifica si el cliente - item - alacen tiene configurado un maximo de ventas en $ cada cierto numero de dias
            if (await cliente.ClienteControlaMaximo(_context, codcliente_real))
            {
                foreach (var detalle in tabladetalle)
                {
                    max =await items.MaximoDeVenta(_context, detalle.coditem, codalmacen, detalle.codtarifa);
                    if (max > 0) //si controlar
                    {
                        diascontrol = await items.MaximoDeVenta_PeriodoDeControl(_context, detalle.coditem, codalmacen, detalle.codtarifa);
                        if(diascontrol > 0)
                        {
                            //obtiene la cantidad vendida en los ultimos X dias mas lo que se quiere vender a ahora
                            cantidad_ttl_vedida = 0;
                            cantidad_ttl_vedida = await cliente.CantidadVendida(_context, detalle.coditem,codcliente_real, fecha.Date.AddDays(-diascontrol), fecha) + detalle.cantidad;
                            if(cantidad_ttl_vedida <= max)
                            {
                                detalle.cumple = true;
                            }
                            else
                            {
                                detalle.cumple = false;
                                if(cadena_items.Trim().Length > 0)
                                {
                                    cadena_solo_items = detalle.coditem;
                                    cadena_items = "Item:" + detalle.coditem + "cantidad Vendida: " + cantidad_ttl_vedida.ToString("####,##0.000", new CultureInfo("en-US")) + " MaxVta Permitida: " + max.ToString("####,##0.000", new CultureInfo("en-US"));
                                }
                                else
                                {
                                    cadena_items += "\nItem:" + detalle.coditem + " cantidad Vendida: " + cantidad_ttl_vedida.ToString("####,##0.000", new CultureInfo("en-US")) + " MaxVta Permitida: " + max.ToString("####,##0.000", new CultureInfo("en-US"));
                                    cadena_solo_items += ", " + detalle.coditem;
                                }
                            }
                        }
                        else
                        {
                            detalle.cumple = true;
                        }
                        
                    }
                    else
                    {
                        detalle.cumple = true;
                    }

                }

                if (cadena_items.Length > 0)
                {
                    objres.resultado = false;
                    objres.observacion = "Los siguientes items, ya fueron comprados por el cliente: " + codcliente_real + " en los ultimos "+ diascontrol + " dias.";
                    objres.obsdetalle = cadena_items;
                    objres.datoA = codcliente_real;
                    objres.datoB = cadena_solo_items;
                    objres.accion = Acciones_Validar.Pedir_Servicio;
                }
                else
                {
                    objres.resultado = true;
                    objres.observacion = "";
                    objres.obsdetalle = "";
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                }
            }

            return objres;
        }
        public async Task<ResultadoValidacion> Precio_de_Venta_Permitido_Cliente(DBContext _context, List<itemDataMatriz> tabladetalle, DatosDocVta DVTA, string codempresa)
        {
            List<string> precios_doc = new List<string>();
            List<string> precios_cliente = new List<string>();
            string cadena_precios_cliente = "";

            ResultadoValidacion objres = new ResultadoValidacion();

            InicializarResultado(objres);

            // Llenar precios del documento de venta
            precios_doc.Clear();
            foreach (var detalle in tabladetalle)
            {
                if (!precios_doc.Contains(detalle.codtarifa.ToString()))
                {
                    precios_doc.Add(Convert.ToString(detalle.codtarifa));
                }
            }

            // Obtener lista de precios permitidos al cliente
            var preciosClienteQuery = await _context.veclienteprecio
                                   .Where(precio => precio.codcliente == DVTA.codcliente_real)
                                   .OrderBy(precio => precio.codtarifa)
                                   .Select(precio => precio.codtarifa)
                                   .ToListAsync();

            foreach (var codtarifa in preciosClienteQuery)
            {
                precios_cliente.Add(Convert.ToString(codtarifa));
                if (cadena_precios_cliente.Trim().Length == 0)
                {
                    cadena_precios_cliente += Convert.ToString(codtarifa);
                }
                else
                {
                    cadena_precios_cliente += ", " + Convert.ToString(codtarifa);
                }
            }

            // Verificar precios no permitidos al cliente
            string cadena_precios_no_permitidos = "";
            int nro_no_permitidos = 0;
            for (int j = 0; j < precios_doc.Count; j++)
            {
                if (!precios_cliente.Contains(precios_doc[j]))
                {
                    if (cadena_precios_no_permitidos.Trim().Length == 0)
                    {
                        cadena_precios_no_permitidos = precios_doc[j].ToString();
                        nro_no_permitidos += 1;
                    }
                    else
                    {
                        cadena_precios_no_permitidos += ", " + precios_doc[j].ToString();
                        nro_no_permitidos += 1;
                    }
                }
            }

            // Si hay uno o más precios no permitidos
            if (nro_no_permitidos > 0)
            {
                objres.resultado = false;
                objres.observacion = "El cliente no tiene habilitado el/los precio(s): " + cadena_precios_no_permitidos + " los precios habilitados para el cliente son: " + cadena_precios_cliente;
                objres.obsdetalle = "";
                objres.datoA = DVTA.codcliente_real;
                if (DVTA.estado_doc_vta == "NUEVO")
                {
                    objres.datoA = DVTA.nitfactura;
                }
                else
                {
                    objres.datoA = DVTA.id + "-" + DVTA.numeroid + "-" + DVTA.codcliente;
                }
                objres.datoB = "Total: " + DVTA.totaldoc.ToString("####,##0.000", new CultureInfo("en-US")) + " (" + DVTA.codmoneda + ")";
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Cliente_Es_Atendido_Por_El_Vendedor(DBContext _context, DatosDocVta DVTA, string codempresa, string usuario)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            InicializarResultado(objres); ;

            if (await configuracion.emp_clientevendedor(_context, codempresa))
            {
                if (!await ventas.Cliente_de_vendedor(_context, DVTA.codcliente, int.Parse(DVTA.codvendedor)))
                {
                    //verificar si es cliente casual (cliente al que se realiza la proforma) es o no casual, solo para clientes casuales se aceptara que no sea su vendedor el vendedor del documento de la proforma
                    if (await cliente.EsClienteCasual(_context, DVTA.codcliente) == false)
                    {
                        objres.resultado = false;
                        objres.observacion = "El cliente:" + DVTA.codcliente_real + " No Es Casual y no pertenece al a cartera del vendedor " + DVTA.codvendedor + ", la venta debe ser realizada con el vendedor al cual esta asignado el cliente.";
                        objres.obsdetalle = "Verifique esta situacion";
                        objres.datoA = "";
                        objres.datoB = "";
                        objres.accion = Acciones_Validar.Confirmar_SiNo;
                    }
                    else
                    {
                        objres.resultado = false;
                        objres.observacion = "El cliente:" + DVTA.codcliente_real + " Si Es Casual y no pertenece al a cartera del vendedor " + DVTA.codvendedor + ", esta seguro de continuar con la operacion?";
                        objres.obsdetalle = "Verifique esta situacion";
                        objres.datoA = "";
                        objres.datoB = "";
                        objres.accion = Acciones_Validar.Confirmar_SiNo;
                    }
                }
            }
            return objres;
        }
        
        public async Task<(ResultadoValidacion resultadoValidacion, List<Dtnocumplen> dtnocumplen)> Validar_Limite_Maximo_de_Venta(DBContext _context, List<itemDataMatriz> tabladetalle, DatosDocVta DVTA, List<Dtnocumplen> dtnocumplen, string codempresa, string usuario)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena_items = "";
            string cadena_items2 = "";

            InicializarResultado(objres);

            if (DVTA.estado_doc_vta == "NUEVO")
            {
                if (DVTA.id.Substring(0, 2) == "FC")
                {
                    DVTA.id = DVTA.idpf_solurgente;
                    DVTA.numeroid = DVTA.noridpf_solurgente;
                }
                else
                {
                    DVTA.id = "";
                    DVTA.numeroid = "0";
                }
            }

            decimal PorcenMaxVtaAg = await hardcoded.MaximoPorcentajeDeVentaPorMercaderia(_context, Convert.ToInt32(DVTA.codalmacen));
            // modificado 03-11-2021 antes de hacer la validacion de porcentaje del detalle que sume las cantidades de los items de
            // compras hechas 30 dias antes
            int diascontrol = await configuracion.Dias_Proforma_Vta_Item_Cliente(_context, codempresa);
            string valida_nr_pf = await configuracion.Valida_Maxvta_NR_PF(_context, codempresa);
            // añadir al la dt copia para validar el max vta con las cantidades sumadas
            List<itemDataMatrizMaxVta> dt_detalle_item = new List<itemDataMatrizMaxVta>();
            dt_detalle_item.Clear();

            // Utilizar ConvertAll para convertir los elementos de tabladetalle a itemDataMatrizMaxVta
            dt_detalle_item = tabladetalle.ConvertAll(item =>
                new itemDataMatrizMaxVta
                {
                    coditem = item.coditem,
                    descripcion = item.descripcion,
                    medida = item.medida,
                    udm = item.udm,
                    porceniva = item.porceniva,
                    niveldesc = item.niveldesc,
                    porcendesc = item.porcendesc,
                    porcen_mercaderia = item.porcen_mercaderia,
                    cantidad_pedida = item.cantidad_pedida,
                    cantidad = item.cantidad,
                    codtarifa = item.codtarifa,
                    coddescuento = item.coddescuento,
                    precioneto = item.precioneto,
                    preciodesc = item.preciodesc,
                    preciolista = item.preciolista,
                    total = item.total,
                    cumple = item.cumple,
                    nroitem = item.nroitem,
                    cantidad_pf_anterior = 0,
                    cantidad_pf_total = 0
                }
            );
            dt_detalle_item.Clear();
            foreach (var detalle in tabladetalle)
            {
                // obtiene los items y la cantidad en proformas en los ultimos X dias mas lo que se quiere vender a ahora
                decimal cantidad_ttl_vendida_pf = 0;
                decimal cantidad_ttl_vendida_pf_actual = 0;
                decimal cantidad_ttl_vendida_pf_total = 0;
                cantidad_ttl_vendida_pf_actual = (decimal)detalle.cantidad;
                DateTime fecha_serv = await funciones.FechaDelServidor(_context);
                if (diascontrol > 0 && detalle.cantidad > 0)
                {
                    if (valida_nr_pf == "PF")
                    {
                        cantidad_ttl_vendida_pf = await cliente.CantidadVendida_PF(_context, Convert.ToString(detalle.coditem), DVTA.codcliente_real, fecha_serv.Date.AddDays(-diascontrol), fecha_serv.Date);
                    }
                    else if (valida_nr_pf == "NR")
                    {
                        cantidad_ttl_vendida_pf = await cliente.CantidadVendida_NR(_context, Convert.ToString(detalle.coditem), DVTA.codcliente_real, fecha_serv.Date.AddDays(-diascontrol), fecha_serv.Date);
                    }
                    else
                    {
                        cantidad_ttl_vendida_pf = await cliente.CantidadVendida_PF(_context, Convert.ToString(detalle.coditem), DVTA.codcliente_real, fecha_serv.Date.AddDays(-diascontrol), fecha_serv.Date);
                    }

                    cantidad_ttl_vendida_pf_total = cantidad_ttl_vendida_pf + cantidad_ttl_vendida_pf_actual;
                    // aqui verificar si la cantidad sumada con las pf q hubieran y la pf actual sobrepasan el porcentaje o no
                    // VALIDAR PORCENTAJE MAXIMO DE VENTA
                    if (cantidad_ttl_vendida_pf_total > (decimal)detalle.cantidad)
                    {
                        // poner la cantidad del item obtenido de la suma de la cantidad actual de la pf y de las pf q hubiera
                        dt_detalle_item.Add(new itemDataMatrizMaxVta
                        {
                            coditem = detalle.coditem,
                            descripcion = detalle.descripcion,
                            medida = detalle.medida,
                            udm = detalle.udm,
                            porceniva = detalle.porceniva,
                            niveldesc = detalle.niveldesc,
                            porcendesc = detalle.porcendesc,
                            porcen_mercaderia = detalle.porcen_mercaderia,
                            cantidad_pedida = detalle.cantidad_pedida,
                            cantidad = detalle.cantidad,
                            codtarifa = detalle.codtarifa,
                            coddescuento = detalle.coddescuento,
                            precioneto = detalle.precioneto,
                            preciodesc = detalle.preciodesc,
                            preciolista = detalle.preciolista,
                            total = detalle.total,
                            cumple = detalle.cumple,
                            nroitem = detalle.nroitem,
                            // Añadir las propiedades adicionales de itemDataMatrizMaxVta que no existen en itemDataMatriz
                            cantidad_pf_anterior = (double)cantidad_ttl_vendida_pf,
                            cantidad_pf_total = (double)cantidad_ttl_vendida_pf_total
                        });
                    }
                    else
                    {
                        dt_detalle_item.Add(new itemDataMatrizMaxVta
                        {
                            coditem = detalle.coditem,
                            descripcion = detalle.descripcion,
                            medida = detalle.medida,
                            udm = detalle.udm,
                            porceniva = detalle.porceniva,
                            niveldesc = detalle.niveldesc,
                            porcendesc = detalle.porcendesc,
                            porcen_mercaderia = detalle.porcen_mercaderia,
                            cantidad_pedida = detalle.cantidad_pedida,
                            cantidad = detalle.cantidad,
                            codtarifa = detalle.codtarifa,
                            coddescuento = detalle.coddescuento,
                            precioneto = detalle.precioneto,
                            preciodesc = detalle.preciodesc,
                            preciolista = detalle.preciolista,
                            total = detalle.total,
                            cumple = detalle.cumple,
                            nroitem = detalle.nroitem,
                            // Añadir las propiedades adicionales de itemDataMatrizMaxVta que no existen en itemDataMatriz
                            cantidad_pf_anterior = (double)cantidad_ttl_vendida_pf,
                            cantidad_pf_total = (double)cantidad_ttl_vendida_pf_total
                        });
                    }
                }
                else
                {
                    cantidad_ttl_vendida_pf_total = cantidad_ttl_vendida_pf + cantidad_ttl_vendida_pf_actual;
                    dt_detalle_item.Add(new itemDataMatrizMaxVta
                    {
                        coditem = detalle.coditem,
                        descripcion = detalle.descripcion,
                        medida = detalle.medida,
                        udm = detalle.udm,
                        porceniva = detalle.porceniva,
                        niveldesc = detalle.niveldesc,
                        porcendesc = detalle.porcendesc,
                        porcen_mercaderia = detalle.porcen_mercaderia,
                        cantidad_pedida = detalle.cantidad_pedida,
                        cantidad = detalle.cantidad,
                        codtarifa = detalle.codtarifa,
                        coddescuento = detalle.coddescuento,
                        precioneto = detalle.precioneto,
                        preciodesc = detalle.preciodesc,
                        preciolista = detalle.preciolista,
                        total = detalle.total,
                        cumple = detalle.cumple,
                        nroitem = detalle.nroitem,
                        // Añadir las propiedades adicionales de itemDataMatrizMaxVta que no existen en itemDataMatriz
                        cantidad_pf_anterior = (double)cantidad_ttl_vendida_pf,
                        cantidad_pf_total = (double)cantidad_ttl_vendida_pf_total
                    });
                }
            }
            dtnocumplen.Clear();
            dtnocumplen = await restricciones.ValidarMaximoPorcentajeDeMercaderiaVenta(_context, DVTA.codcliente_real, false, dt_detalle_item, Convert.ToInt32(DVTA.codalmacen), PorcenMaxVtaAg, DVTA.id, Convert.ToInt32(DVTA.numeroid), codempresa, usuario);
            dt_detalle_item.Clear();

            // poner a todos del detalle que cumplen
            foreach (var detalle in tabladetalle)
            {
                detalle.porcen_mercaderia = 0;
            }

            // ahora indentificar los que no cumplen de la tabla detalle
            double _porcen = 0;
            foreach (var nocumplen in dtnocumplen)
            {

                if (Math.Round(Convert.ToDouble(nocumplen.saldo), 2) == 0)
                {
                    // si el el saldo es cero evitar la divicion entre cero
                    _porcen = 0;
                }
                else
                {
                    _porcen = (Convert.ToDouble(nocumplen.cantidad) * 100) / Convert.ToDouble(nocumplen.saldo);
                    _porcen = Math.Round(_porcen, 2);
                }

                if (nocumplen.obs.ToString() == "No hay Saldo!!!")
                {
                    // si no hay saldo
                    goto sgte;
                }
                else if (!nocumplen.obs.ToString().Equals("Cumple"))
                {
                    // formar cadena de los que no cumplen
                    if (cadena_items.Trim().Length == 0)
                    {
                        cadena_items = nocumplen.codigo.ToString();
                    }
                    else
                    {
                        cadena_items += " | " + nocumplen.codigo.ToString();
                    }
                }

                foreach (var detalle in tabladetalle)
                {
                    if (detalle.coditem.ToString() == nocumplen.codigo.ToString())
                    {
                        // SI NO CUMPLE ES DECIR PORCENTAJE DE VENTA MAYOR AL PERMITIDO
                        detalle.porcen_mercaderia = (double)_porcen;
                        break;
                    }
                }

                sgte:;
            }

            if (cadena_items.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = "Hay algunos items que sobrepasan el limite de venta!!!";
                objres.obsdetalle = cadena_items;
                if (DVTA.estado_doc_vta == "NUEVO")
                {
                    objres.datoA = DVTA.nitfactura + "-" + DVTA.codcliente_real;
                }
                else
                {
                    objres.datoA = DVTA.id + "-" + DVTA.numeroid + "-" + DVTA.codcliente_real;
                }
                objres.datoB = cadena_items;
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return (objres, dtnocumplen);
        }

        public async Task<(ResultadoValidacion resultadoValidacion, List<Dtnocumplen> dtnocumplen)> Validar_Limite_Maximo_de_Venta_por_Item(DBContext _context, string id, int numeroid, string nitfactura, string estado_doc_vta, string codcliente_real, int codalmacen, List<itemDataMatrizMaxVta> tabladetalle, List<Dtnocumplen> dtnocumplen, string codempresa, string usuario)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena_items = "";
            string cadena_items2 = "";

            InicializarResultado(objres);

            if (estado_doc_vta == "NUEVO")
            {
                id = "";
                numeroid = 0;

            }

            decimal PorcenMaxVtaAg = await hardcoded.MaximoPorcentajeDeVentaPorMercaderia(_context, Convert.ToInt32(codalmacen));

            dtnocumplen.Clear();
            dtnocumplen = await restricciones.ValidarMaximoPorcentajeDeMercaderiaVenta(_context, codcliente_real, false, tabladetalle, Convert.ToInt32(codalmacen), PorcenMaxVtaAg, id, Convert.ToInt32(numeroid), codempresa, usuario);
            //ahora indentificar los que no cumplen de la tabla detalle
            double _porcen = 0;
            foreach (var nocumplen in dtnocumplen)
            {
                if (Math.Round(Convert.ToDouble(nocumplen.saldo), 2) == 0)
                {
                    // si el el saldo es cero evitar la divicion entre cero
                    _porcen = 0;
                }
                else
                {
                    _porcen = (Convert.ToDouble(nocumplen.cantidad) * 100) / Convert.ToDouble(nocumplen.saldo);
                    _porcen = Math.Round(_porcen, 2);
                }

                if (nocumplen.obs.ToString() == "No hay Saldo!!!")
                {
                    // si no hay saldo
                    goto sgte;
                }
                else if (!nocumplen.obs.ToString().Equals("Cumple"))
                {
                    // formar cadena de los que no cumplen
                    if (cadena_items.Trim().Length == 0)
                    {
                        cadena_items = nocumplen.codigo.ToString();
                    }
                    else
                    {
                        cadena_items += " | " + nocumplen.codigo.ToString();
                    }
                }

                foreach (var detalle in tabladetalle)
                {
                    if (detalle.coditem.ToString() == nocumplen.codigo.ToString())
                    {
                        // SI NO CUMPLE ES DECIR PORCENTAJE DE VENTA MAYOR AL PERMITIDO
                        detalle.porcen_mercaderia = (double)_porcen;
                        break;
                    }
                }

                sgte:;
            }
            if (cadena_items.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = "Hay algunos items que sobrepasan el limite de venta!!!";
                objres.obsdetalle = cadena_items;
                if(estado_doc_vta == "NUEVO")
                {
                    objres.datoA = nitfactura + "-" + codcliente_real;
                }
                else
                {
                    objres.datoA = id + "-" + numeroid + "-" + codcliente_real;
                }
                objres.datoB = cadena_items;
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return (objres, dtnocumplen);
        }

        public async Task<(ResultadoValidacion resultadoValidacion, List<Dtnegativos> dtnegativos)> Validar_Saldos_Negativos_Doc(DBContext _context, List<itemDataMatriz> tabladetalle, DatosDocVta DVTA, List<Dtnegativos> dtnegativos, string codempresa, string usuario)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena_items = "";
            string cadena_items2 = "";

            InicializarResultado(objres);

            if (tabladetalle.Count > 0)
            {
                List<string> msgs = new List<string>();
                List<string> negs = new List<string>();

                dtnegativos.Clear();
                dtnegativos = await saldos.ValidarNegativosDocVenta(_context, tabladetalle, Convert.ToInt32(DVTA.codalmacen), DVTA.idpf_solurgente, Convert.ToInt32(DVTA.noridpf_solurgente), msgs, negs, codempresa, usuario);

                foreach (var negativo in dtnegativos)
                {
                    if (negativo.obs.ToString() == "Genera Negativo")
                    {
                        if ((int)negativo.cantidad_conjunto > 0)
                        {
                            negs.Add(negativo.coditem_cjto.ToString());
                        }
                        if ((int)negativo.cantidad_suelta > 0)
                        {
                            negs.Add(negativo.coditem_suelto.ToString());
                        }
                    }
                }
                if (negs.Count == 0)
                {
                    objres.resultado = true;
                    objres.observacion = "Ningun item del documento genera negativos";
                    objres.accion = Acciones_Validar.Solo_Ok;

                    foreach (var detalle in tabladetalle)
                    {
                        detalle.cumple = true;
                    }
                }
                else
                {
                    foreach (var detalle in tabladetalle)
                    {
                        if (negs.Contains(detalle.coditem.ToString()))
                        {
                            // No cumple genera negativo
                            detalle.cumple = false;
                            // Genera la cadena de items en una fila
                            if (cadena_items.Trim().Length == 0)
                            {
                                cadena_items = detalle.coditem.ToString();
                            }
                            else
                            {
                                cadena_items += " - " + detalle.coditem.ToString();
                            }
                            // Genera la lista item en filas
                            if (cadena_items2.Trim().Length == 0)
                            {
                                cadena_items2 = detalle.coditem.ToString();
                            }
                            else
                            {
                                cadena_items2 += " - " + detalle.coditem.ToString();
                            }
                        }
                        else
                        {
                            // Si cumple y NO genera negativo
                            detalle.cumple = true;
                        }
                    }
                }
            }

            if (await almacen.Es_Tienda(_context, int.Parse(DVTA.codalmacen)))
            {
                //si es tienda se habilita la posibilidad de facturar con negativos
                //ESTO SERA PARA CASOS EXCEPCIONALES
                if (cadena_items.Trim().Length > 0)
                {
                    objres.resultado = false;
                    objres.observacion = "Los siguientes items generan saldos negativos!!!";
                    objres.obsdetalle = cadena_items2;
                    objres.datoA = DVTA.id + "-" + DVTA.numeroid;
                    objres.datoB = cadena_items2;
                    objres.accion = Acciones_Validar.Pedir_Servicio;
                }
            }
            else
            {
                if (cadena_items.Trim().Length > 0)
                {
                    objres.resultado = false;
                    objres.observacion = "Los siguientes items generan saldos negativos!!!";
                    objres.obsdetalle = cadena_items2;
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                }
            }
            return (objres, dtnegativos);
        }
        private async Task<bool> Control_Valido_C00066Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Items_Repetidos(_context, tabladetalle, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        public async Task<ResultadoValidacion> Validar_Items_Repetidos(DBContext _context, List<itemDataMatriz> tabladetalle, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena_repeditos = "";
            string item = "";
            int i, j;
            decimal cantidad;
            decimal cantidad_total;
            ArrayList items_iguales = new ArrayList();
            bool resultado = true;

            InicializarResultado(objres);
            //Verificar si item tiene en el detalle items repetidos
            //son los que tenian antes de la auditoria
            if (!await empresa.Permitir_Items_Repetidos(_context, codempresa))
            {
                if (!await cliente.Permite_items_repetidos(_context, DVTA.codcliente_real))
                {
                    //llamar a la funcion que devolvera los items repetidos si es que los hay
                    cadena_repeditos = "";
                    cadena_repeditos = Cadena_Items_Repetidos(tabladetalle);
                    if ((cadena_repeditos.Trim().Length > 0))
                    {
                        resultado = false;
                    }
                }
            }

            if (resultado == false)
            {
                objres.resultado = false;
                objres.observacion = "Se encontraron Items Repetidos en el documento lo cual no esta permitido: ";
                objres.obsdetalle = cadena_repeditos;
                objres.datoA = DVTA.nitfactura;
                objres.datoB = "Total: " + DVTA.totaldoc.ToString("####,##0.000", new CultureInfo("en-US")) + " (" + DVTA.codmoneda + ")";
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }

        
        private string Cadena_Items_Repetidos(List<itemDataMatriz> tabladetalle)
        {
            bool resultado = true;
            string cadena = "";

            if (tabladetalle.Count > 0)
            {
                // Comparar y mostrar
                DataTable tabla = new DataTable();
                DataRow registro;
                tabla.Columns.Add("coditem", typeof(string));
                tabla.Columns.Add("descripcion", typeof(string));
                tabla.Columns.Add("medida", typeof(string));
                tabla.Columns.Add("veces", typeof(int));

                foreach (var detalle in tabladetalle)
                {
                    // Buscar si ya está
                    bool found = false;
                    foreach (DataRow existingRow in tabla.Rows)
                    {
                        if (existingRow["coditem"].ToString() == detalle.coditem.ToString())
                        {
                            found = true;
                            existingRow["veces"] = (int)existingRow["veces"] + 1;
                            break;
                        }
                    }

                    if (!found)
                    {
                        // Si no estaba, añadir
                        registro = tabla.NewRow();
                        registro["coditem"] = detalle.coditem;
                        registro["descripcion"] = detalle.descripcion;
                        registro["medida"] = detalle.medida;
                        registro["veces"] = 1;
                        tabla.Rows.Add(registro);
                    }
                }

                // Borrar de tabla los que tengan menos de dos
                foreach (DataRow row in tabla.Rows.Cast<DataRow>().ToList())
                {
                    if ((int)row["veces"] < 2)
                    {
                        tabla.Rows.Remove(row);
                    }
                }

                if (tabla.Rows.Count > 0)
                {
                    cadena = "";
                    cadena = " ITEM     DESCRIPCION          MEDIDA      VECES\n";
                    cadena += "------------------------------------------------\n";
                    foreach (DataRow row in tabla.Rows)
                    {
                        cadena += " " + row["coditem"] + " " + funciones.Rellenar(row["descripcion"].ToString(), 20, " ", false) + " " + funciones.Rellenar(row["medida"].ToString(), 11, " ", false) + "  " + funciones.Rellenar(row["veces"].ToString(), 3, " ") + "\n";
                    }
                    cadena += "------------------------------------------------\n";
                }
                else
                {
                    cadena = "";
                }
            }
            return cadena;
        }
        private async Task<string> Cadena_Items_Repetidos_Con_DescuentosAsync(DBContext _context, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            bool resultado = true;
            string cadena = "";
            int cant_veces_repetido = 0;

            if (tabladetalle.Count > 0)
            {
                // Comparar y mostrar
                DataTable tabla = new DataTable();
                DataRow registro;
                tabla.Columns.Add("coditem", typeof(string));
                tabla.Columns.Add("descripcion", typeof(string));
                tabla.Columns.Add("medida", typeof(string));
                tabla.Columns.Add("veces", typeof(int));
                tabla.Columns.Add("coddescuento", typeof(int));
                tabla.Columns.Add("cantidad", typeof(int));

                foreach (var detalle in tabladetalle)
                {
                    // Buscar si ya está
                    bool found = false;
                    foreach (DataRow existingRow in tabla.Rows)
                    {
                        if (existingRow["coditem"].ToString() == detalle.coditem.ToString())
                        {
                            found = true;
                            existingRow["veces"] = (int)existingRow["veces"] + 1;
                            break;
                        }
                    }

                    if (!found)
                    {
                        // Si no estaba, añadir
                        registro = tabla.NewRow();
                        registro["coditem"] = detalle.coditem;
                        registro["descripcion"] = detalle.descripcion;
                        registro["medida"] = detalle.medida;
                        registro["veces"] = 1;
                        registro["coddescuento"] = detalle.coddescuento;
                        registro["cantidad"] = detalle.cantidad;
                        tabla.Rows.Add(registro);
                    }
                }

                // Borrar de tabla los que tengan menos de dos
                foreach (DataRow row in tabla.Rows.Cast<DataRow>().ToList())
                {
                    if ((int)row["veces"] < 2)
                    {
                        tabla.Rows.Remove(row);
                    }
                }
                //verificar que esos items repetidos por lo menos uno cumpla con el empaque de descuento
                int codempaque_permite_item_repetido = 0;
                int codigo_empaque_descuento_especial = 0;
                codempaque_permite_item_repetido = await configuracion.codempaque_permite_item_repetido(_context, codempresa);

                if (tabla.Rows.Count > 0)
                {
                    int hasta = 0;
                    List<string> itemsBorrar = new List<string>();
                    bool bandera = true;
                    foreach (DataRow tbl in tabla.Rows)
                    {
                        if (Convert.ToInt32(tbl["coddescuento"].ToString()) > 0 && Convert.ToInt32(tbl["veces"].ToString()) <= 2)
                        {
                            foreach (var detalle in tabladetalle)
                            {
                                codigo_empaque_descuento_especial = await ventas.Codigo_Empaque_Descuento_Especial(_context, detalle.coddescuento);
                                if (tbl["coditem"].ToString() == detalle.coditem && codempaque_permite_item_repetido == codigo_empaque_descuento_especial)
                                {
                                    if (await ventas.Cumple_Empaque_De_DesctoEspecial(_context, detalle.coditem, detalle.codtarifa, detalle.coddescuento, (decimal)detalle.cantidad, ""))
                                    {
                                        if (bandera == true)
                                        {
                                            if (detalle.cantidad == tabladetalle.FirstOrDefault(x => x.coditem == tbl["coditem"].ToString()).cantidad)
                                            {
                                                bandera = false;
                                            }
                                            else
                                            {
                                                itemsBorrar.Add(tbl["coditem"].ToString());
                                                break;
                                            }
                                        }
                                    }
                                }

                            }

                        }

                    }
                    //borrar items
                   // tabla = tabla.Where(t => !items_borrar.Contains(t.coditem)).ToList();
                    foreach (DataRow row in tabla.Rows)
                    {
                        if (itemsBorrar.Contains(row["coditem"].ToString()))
                        {
                            row.Delete();
                        }
                    }

                }
                if (tabla.Rows.Count > 0)
                {
                    cadena = "";
                    cadena = " ITEM     DESCRIPCION          MEDIDA      VECES\n";
                    cadena += "------------------------------------------------\n";
                    foreach (DataRow row in tabla.Rows)
                    {
                        cadena += " " + row["coditem"] + " " + funciones.Rellenar(row["descripcion"].ToString(), 20, " ", false) + " " + funciones.Rellenar(row["medida"].ToString(), 11, " ", false) + "  " + funciones.Rellenar(row["veces"].ToString(), 3, " ") + "\n";
                    }
                    cadena += "------------------------------------------------\n";
                }
                else
                {
                    cadena = "";
                }
            }
            return cadena;
        }
        public class ItemRepetido
        {
            public string Coditem { get; set; }
            public string Descripcion { get; set; }
            public string Medida { get; set; }
            public int Veces { get; set; }
            public int Coddescuento { get; set; }
            public double Cantidad { get; set; }
        }


        public async Task<string> Cadena_Items_Repetidos_Con_Descuentos(DBContext _context, List<itemDataMatriz> tabladetalle, string codempresa, string codcliente)
        {
            string cadena = "";
            decimal ttl_cantidad = 0;

            int coddescuento_caja_cerrada = 0;
            coddescuento_caja_cerrada = await configuracion.coddescuento_caja_cerrada(_context, codempresa);

            if (tabladetalle.Count > 0)
            {
                // Crear una tabla temporal para almacenar los items repetidos
                var tabla = new List<ItemRepetido>();

                foreach (var item in tabladetalle)
                {
                    var existingItem = tabla.FirstOrDefault(t => t.Coditem == item.coditem);
                    if (existingItem != null)
                    {
                        existingItem.Veces += 1;
                    }
                    else
                    {
                        tabla.Add(new ItemRepetido
                        {
                            Coditem = item.coditem,
                            Descripcion = item.descripcion,
                            Medida = item.medida,
                            Veces = 1,
                            Coddescuento = item.coddescuento,
                            Cantidad = item.cantidad
                        });
                    }
                }

                // Filtrar los items que se repiten menos de dos veces
                //tabla = tabla.Where(t => t.Veces >= 2).ToList();
                // Eliminar ítems que no tienen repeticiones (menos de 2 veces)
                tabla.RemoveAll(t => t.Veces < 2);
                // Validar las condiciones de los ítems repetidos
                List<string> errores = new List<string>();

                foreach (var item in tabla)
                {
                    var registros = tabladetalle.Where(t => t.coditem == item.Coditem).ToList();
                    bool tiene301 = registros.Any(r => r.coddescuento == coddescuento_caja_cerrada);
                    bool tiene0 = registros.Any(r => r.coddescuento == 0);

                    // Verificar la cantidad de repeticiones
                    if (registros.Count > 2)
                    {
                        errores.Add($"{item.Coditem} {funciones.Rellenar(item.Descripcion, 18, " ", false)} {funciones.Rellenar(item.Medida, 11, " ", false)} {funciones.Rellenar(item.Veces.ToString(), 2, " ")}");
                        continue;
                    }

                    // Validar que exista un coddescuento = 301 y otro coddescuento = 0
                    if (!(tiene301 && tiene0))
                    {
                        errores.Add($"{item.Coditem} {funciones.Rellenar(item.Descripcion, 18, " ", false)} {funciones.Rellenar(item.Medida, 11, " ", false)} {funciones.Rellenar(item.Veces.ToString(), 2, " ")}");
                    }

                    // Validar que ambos registros cumplan con el empaque
                    ttl_cantidad = 0;
                    foreach (var registro in registros)
                    {
                        ttl_cantidad += (decimal)registro.cantidad;
                        if (!await ventas.CumpleEmpaqueCerrado(_context, registro.coditem, registro.codtarifa, registro.coddescuento, (decimal)registro.cantidad, codcliente))
                        {
                            if (!await ventas.CumpleEmpaqueCerrado(_context, registro.coditem, registro.codtarifa, registro.coddescuento, ttl_cantidad, codcliente))
                            {
                                errores.Add($"{item.Coditem} {funciones.Rellenar(item.Descripcion, 18, " ", false)} {funciones.Rellenar(item.Medida, 11, " ", false)} {funciones.Rellenar(item.Veces.ToString(), 2, " ")}");
                                break;
                            }
                        }
                    }
                }

                // Construir el mensaje final
                if (errores.Count > 0)
                {
                    cadena = " ITEM     DESCRIPCION          MEDIDA      VECES" + Environment.NewLine;
                    cadena += "------------------------------------------------" + Environment.NewLine;
                    cadena += string.Join(Environment.NewLine, errores);
                    cadena += Environment.NewLine + "------------------------------------------------" + Environment.NewLine;
                }
                else
                {
                    cadena = "";
                }
            }
            return cadena;
        }


        private async Task<bool> Control_Valido_C00067Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            //##VALIDAR EMPAQUES CERRADOS SEGUN LISTA DE PRECIOS
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Empaques_Cerrados_Segun_Lista_Precio(_context, tabladetalle, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }

        private async Task<bool> Control_Valido_C00068Async(DBContext _context, Controles regcontrol, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_No_Mezclar_Descuentos_Especiales(_context, tabladetalle, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00069Async(DBContext _context, Controles regcontrol, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Venta_Minima_Nro_Items_Grado2_KG(_context, tabladetalle, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00070Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Descto_PP_Items_Grado2_KG(_context, tabladetalle, tabladescuentos, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }
        public async Task<ResultadoValidacion> Validar_Descto_PP_Items_Grado2_KG(DBContext _context, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            //Obligar a poner el descuento especial PP 6
            //a los documentos que tengas items especiales KG hoja 41
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena = "";
            bool resultado = true;
            int c = 0;

            InicializarResultado(objres);

            //verificar si hay items G" en KG pagina 41
            foreach (var detalle in tabladetalle)
            {
                if (detalle.coditem.ToString().StartsWith("41"))
                {
                    c++;
                }
            }
            //si hay items de G" en KG de la pagina 41
            if (c > 0 )
            {
                //verificar si el descuento PP para items G2 pag 41: 6 esta asignado
                bool esta_aplicado = false;
                foreach (var desc in tabladescuentos)
                {
                    if (desc.coddesextra == 6)
                    {
                        esta_aplicado = true;
                    }
                }
                if (esta_aplicado ==false)
                {
                    resultado = false;
                }
            }

            if (resultado == false)
            {
                objres.resultado = false;
                objres.observacion = "Se encontro observaciones a la venta de items PER HEX G2 en KG de la pagina-41:";
                objres.obsdetalle = "Si va a Vender en kilos esta mercaderia, Debe adicionar el Descuento 6 ZDKG(0 %) al documento para generar un plan de pagos correcto.";
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Venta_Minima_Nro_Items_Grado2_KG(DBContext _context, List<itemDataMatriz> tabladetalle, string codempresa)
        {//Valida que en una venta no se mezcle, sin descuento, con proveedor o volumen etc
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena = "";
            bool resultado = true;
            int nro = 0;

            InicializarResultado(objres);

            //Validar empaques Cerrados
            foreach (var detalle in tabladetalle)
            {
                if (detalle.coditem.ToString().StartsWith("41"))
                {
                    nro++;
                }
            }

            if (nro > 0 && nro < 10)
            {
               resultado = false;
               cadena = "Los items (PER HEX G2 GK) de la pagina 41 deben venderse minimo 10 items diferentes, y el documento actual tiene: " + nro;
            }

            if (resultado == false)
            {
                objres.resultado = false;
                objres.observacion = "Se econtraron las siguitentes observaciones en la venta de items de la Pag-41:";
                objres.obsdetalle = cadena;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_No_Mezclar_Descuentos_Especiales(DBContext _context, List<itemDataMatriz> tabladetalle, string codempresa)
        {//Valida que en una venta no se mezcle, sin descuento, con proveedor o volumen etc
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena = "";
            bool resultado = true;
            List<Int32> descuentos = new List<Int32>();

            InicializarResultado(objres);

            //Validar empaques Cerrados
            foreach (var detalle in tabladetalle)
            {
                if (!descuentos.Contains(detalle.coddescuento))
                {
                    descuentos.Add(detalle.coddescuento);
                }
            }

            if (descuentos.Count > 1)
            {
                resultado = false;
                foreach (var desc in descuentos)
                {
                    cadena += Environment.NewLine + "DescEspecial: " + desc + "-" + await nombres.nombre_descuento_especial(_context, Convert.ToInt32(desc));
                }
            }

            if (resultado == false)
            {
                objres.resultado = false;
                objres.observacion = "En el documento existen los siguientes tipos de descuento especial, lo cual no esta permitido, Solo se puede aplicar un descuento especial a la vez.";
                objres.obsdetalle = cadena;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Confirmar_SiNo;
            }
            return objres;
        }
        private async Task<bool> Control_Valido_C00071Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Enlace_Proforma_Mayorista_Dimediado(_context, tabladetalle, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00072Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Fc_Validar_Factura_Complementaria(_context, tabladetalle, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00073Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Fc_Validar_Monto_Maximo_Vta_Cliente_SN(_context, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00074Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Fc_Validar_Nombre_Factura_Valido(_context, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        
        private async Task<bool> Control_Valido_C00075Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            //##VALIDAR NRO DE ITEMS POR CAJA
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Fc_Validar_Nro_Items_Para_Facturar_Por_Caja(_context, DVTA, tabladetalle, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00076Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa)
        {
            //##VALIDAR FECHA LIMITE DE DOSIFICACION
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Fc_Validar_Fecha_Limite_Dosificacion(_context, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00077Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa)
        {
            //##VALIDAR TIPO DE DOSIFICACION
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Fc_Validar_Tipo_Dosificacion(_context, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00078Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Fc_Validar_Venta_Credito_Tienda(_context, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00079Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Fc_Validar_IdCuenta_Venta_Contado_Tienda(_context, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00080Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, string codempresa)
        {
            //##VALIDAR NIT - NOMB FACTURA EXISTENTE Y FACTURADO
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_NIT_Existente_Facturado(_context, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00081Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await FC_Validar_Maximo_De_Venta_Item(_context, DVTA, tabladetalle, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00082Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await FC_Validar_Deuda_Pendiente_Cliente_Tienda(_context, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00083Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await FC_Validar_Anticipo_Aplicado_Factura_mostrador(_context, DVTA, tabladetalle, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00084Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<Dtnocumplen> dtnocumplen, string codempresa, string usuario)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Verificar_Cantidad_Maximo_De_Venta_Cliente_Items(_context, DVTA.id,Convert.ToInt32(DVTA.numeroid), DVTA.nitfactura, DVTA.estado_doc_vta, DVTA.codcliente_real, Convert.ToInt32(DVTA.codalmacen), DVTA.fechadoc, tabladetalle, dtnocumplen, codempresa, usuario);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00085Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<Dtnocumplen> dtnocumplen, string codempresa, string usuario)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Solicitud_Descuentos_Con_Una_Sola_Proforma(_context, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }

        private async Task<bool> Control_Valido_C00086Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Precios_Del_Documento(_context, DVTA, tabladetalle, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }
        private async Task<bool> Control_Valido_C00087Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Precio_De_Venta_Habilitado_Para_Descto_Especial(_context, DVTA, tabladetalle, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }

        private async Task<bool> Control_Valido_C00088Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            List<int> lista_desc_especial = new List<int>();
            foreach (var detalle in tabladetalle)
            {
                if (detalle.coddescuento == 0 || detalle.coddescuento.ToString().Trim().Length == 0)
                {
                    //si es el descto cero quiere decir que no se puso descto
                }
                else
                {
                    if (!lista_desc_especial.Contains(detalle.coddescuento))
                    {
                        lista_desc_especial.Add(detalle.coddescuento);
                    }
                }
            }

            objres = await Verificar_Descuentos_Especiales_Habilitados(_context, DVTA, lista_desc_especial, codempresa);

            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }

        private async Task<bool> Control_Valido_C00089Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            List<string> lista_desc_nivel = new List<string>();
            List<int> lista_precios = new List<int>();
            foreach (var detalle in tabladetalle)
            {
                if (detalle.niveldesc.ToString().Trim().Length == 0)
                {
                    //si es el descto cero quiere decir que no se puso descto
                }
                else
                {
                    if (!lista_desc_nivel.Contains(detalle.niveldesc))
                    {
                        lista_desc_nivel.Add(detalle.niveldesc);
                    }
                }
            }

            foreach (var detalle in tabladetalle)
            {
                if (!lista_precios.Contains(detalle.codtarifa))
                {
                    lista_precios.Add(detalle.codtarifa);
                }
            }

            objres = await Verificar_Descuentos_LineaNivel_Habilitados(_context, DVTA, lista_desc_nivel, lista_precios, tabladetalle, codempresa);

            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                // regcontrol.DescServicio = "PERMITIR PROMOCION PARA PROFORMAS ANTERIORES";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }

        private async Task<bool> Control_Valido_C00090Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);

            objres = await Validar_Enlace_Proforma_Cliente_Referencia(_context, DVTA, codempresa);

            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }

        private async Task<bool> Control_Valido_C00091Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);

            int nro_max_items_siat = 0;
            int cantidad_pedido_items = 0;

            nro_max_items_siat = await siat.Nro_Maximo_Items_Factura_Segun_SIAT(_context, codempresa);
            cantidad_pedido_items = await Cantidad_Items_Pedido_Proforma(tabladetalle);

            if (cantidad_pedido_items > nro_max_items_siat)
            {
                objres.resultado = false;
                objres.observacion = "La cantidad de items del pedido es mayor al permitido!!!";
                objres.obsdetalle = "El numero maximo de items a facturar segun normativa del SIAT es de: " + nro_max_items_siat.ToString() + " items en el detalle, actualmente el pedido tiene: " + cantidad_pedido_items.ToString() + " items con cantidad efectiva.";
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }

            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }

        private async Task<bool> Control_Valido_C00092Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);

            objres = await Validar_Items_Repetidos_Con_Descuentos(_context, DVTA, tabladetalle, codempresa);

            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }

        private async Task<bool> Control_Valido_C00093Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Empaques_caja_cerrada_Descuento(_context, tabladetalle, DVTA, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.Accion = objres.accion.ToString();
                regcontrol.ClaveServicio = "";
            }
            return true;
        }

        private async Task<bool> Control_Valido_C00094Async(DBContext _context, Controles regcontrol, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            objres = await Validar_Items_Promociones_Por_Aplicar(_context, DVTA, tabladetalle, tabladescuentos, codempresa);
            if (objres.resultado == false)
            {
                regcontrol.Valido = "NO";
                //regcontrol.DescServicio = "";
                regcontrol.Observacion = objres.observacion;
                regcontrol.ObsDetalle = objres.obsdetalle;
                regcontrol.DatoA = objres.datoA;
                regcontrol.DatoB = objres.datoB;
                regcontrol.ClaveServicio = "";
                regcontrol.Accion = objres.accion.ToString();
            }
            return true;
        }

        public async Task<ResultadoValidacion> Validar_Items_Promociones_Por_Aplicar(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena_aplicados = "";
            string cadena_aplicable = "";
            string por_aplicar = "";
            string niveles_por_aplicar = "";
            bool resultado = true;
            int c = 0;

            InicializarResultado(objres);

            if (DVTA.codtarifadefecto.ToString().Trim().Length == 0)
            {

                objres.resultado = false;
                objres.observacion = "Debe indicar el precio para el cual se verificara que descuentos-promociones se pueden aplicar.";
                objres.obsdetalle = "";
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
                return objres;
            }

            int coddesextra_depo = await configuracion.emp_coddesextra_x_deposito_context(_context, codempresa);
            List<string> lista_promos_aplicadas = new List<string>();
            lista_promos_aplicadas = await ventas.DescuentosPromocionAplicadosAsync(_context, tabladescuentos, coddesextra_depo);
            List<string> lista_items = new List<string>();
            lista_items = ListaItemsPedido(tabladetalle);
            int i = 0;
            int j = 0;
            bool esta_habilitado = false;
            int contador = 0;
            string promo = "";
            string habilitado = "NO";
            string cadena = "";

            if (lista_promos_aplicadas.Count > 0)
            {
                foreach (var l_prom in lista_promos_aplicadas)
                {
                    contador = 0;
                    foreach (var l_items in lista_items)
                    {
                        promo = l_prom;
                        esta_habilitado = await ventas.Item_habilitado_para_DescExtra(_context,Convert.ToInt32(promo), l_items);
                        if (esta_habilitado)
                        {
                            contador ++;
                            promo = l_prom;
                            habilitado = "SI";
                            break;
                        }
                    }   
                }
                if (contador == 0)
                {
                    //No hay ningun item que este habilitado para el descuento entonces NO puede acceder al descuento
                    cadena = funciones.Rellenar("EN EL DETALLE NO EXISTEN ITEM'S HABILITADOS PARA PODER ASIGNAR EL DESC.PROMOCION APLICADO", 118, " ", false) + Environment.NewLine;
                    cadena += "--------------------------------------------------------------" + Environment.NewLine;
                    cadena += funciones.Rellenar("COD DESEXTRA       ", 96, " ", false) + Environment.NewLine;
                    cadena += funciones.Rellenar(promo + "            ", 96, " ", false) + Environment.NewLine;
                    cadena += "--------------------------------------------------------------" + Environment.NewLine;
                }
            }
            string cadena_msg= "";
            cadena_msg = cadena;
            
            if (cadena_msg.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = "Se verifico que existen Items no habilitado(s) para las Promociones seleccionadas:";
                objres.obsdetalle = cadena_msg;
                if (DVTA.estado_doc_vta == "NUEVO")
                {
                    objres.datoA = DVTA.codcliente_real;
                    objres.datoB = "DESC/PROM ITEMS NO HABILITADOS: " + promo + " " + habilitado;
                }
                else
                {
                    objres.datoA = DVTA.id + "-" + DVTA.numeroid + "-" + DVTA.codcliente_real;
                    objres.datoB = "DESC/PROM ITEMS NO HABILITADOS: " + promo + " " + habilitado;
                }
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }

        public async Task<ResultadoValidacion> Validar_Items_Repetidos_Con_Descuentos(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena_repeditos = "";
            bool resultado = true;

            InicializarResultado(objres);
            //Del detalle verificar si alguno de los items se puede dividir en 2 items repetidos para que uno cumpla con empaque de descuento y el otro no
            //Verificar si item tiene en el detalle items repetidos

            if (!await empresa.Permitir_Items_Repetidos(_context, codempresa))
            {
                if (!await cliente.Permite_items_repetidos(_context, DVTA.codcliente_real))
                {
                    //llamar a la funcion que devolvera los items repetidos si es que los hay
                    cadena_repeditos = "";
                    cadena_repeditos = await Cadena_Items_Repetidos_Con_Descuentos(_context ,tabladetalle, codempresa,DVTA.codcliente_real);
                    if ((cadena_repeditos.Trim().Length > 0))
                    {
                        resultado = false;
                    }
                }
            }

            if (resultado == false)
            {
                objres.resultado = false;
                objres.observacion = "Se encontraron Items Repetidos en el documento sin que alguno de ellos cumpla con el empaque para descuento de CAJA CERRADA y/o son mas de 2 items repetidos, lo cual no esta permitido: ";
                objres.obsdetalle = cadena_repeditos;
                objres.datoA = DVTA.nitfactura;
                objres.datoB = "Total: " + DVTA.totaldoc.ToString("####,##0.000", new CultureInfo("en-US")) + " (" + DVTA.codmoneda + ")";
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<int> Cantidad_Items_Pedido_Proforma(List<itemDataMatriz> tabladetalle)
        {
            int resultado = 0;
            foreach(var detalle in tabladetalle)
            {
                if (Convert.ToDouble(detalle.cantidad) > 0)
                {
                    resultado ++;
                }
            }
            return resultado;
        }
        public async Task<ResultadoValidacion> Validar_Enlace_Proforma_Cliente_Referencia(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            bool mismo_cliente = true;
            string cadena = "";

            ResultadoValidacion objres = new ResultadoValidacion();
            if (DVTA.codcliente == DVTA.codcliente_real)
            {
                mismo_cliente = true;
                objres.resultado = true;
                objres.observacion = "";
                objres.obsdetalle = cadena;
                objres.datoA = "";
                objres.datoB = "";
            }
            else
            {
                mismo_cliente = false;
            }
            if (mismo_cliente)
            {
                return objres;
            }
            // Si el cliente de la proforma (al cual se factura) es diferente al cliente referencia (el que realiza la compra)
            // estamos hablando de un cliente que compra con factura a nombre de otro
            // y hay que controlar ciertos aspectos
            string cliente_DOC_tipo = await cliente.Es_Cliente_Casual(_context, DVTA.codcliente) ? "CASUAL" : "NO_CASUAL";
            string cliente_REF_tipo = await cliente.Es_Cliente_Casual(_context, DVTA.codcliente_real) ? "CASUAL" : "NO_CASUAL";

            // VERIFICAR CÓMO SON LOS CLIENTES
            if (cliente_DOC_tipo == "CASUAL" && cliente_REF_tipo == "NO_CASUAL")
            {
                objres.resultado = true;
                cadena = "";
                return objres;
            }

            if (cliente_DOC_tipo == "CASUAL" && cliente_REF_tipo == "CASUAL")
            {
                objres.resultado = false;
                cadena = $"El cliente: {DVTA.codcliente} y el clienteRef.: {DVTA.codcliente_real} SON CASUALES y a la vez diferentes (códigos cliente), por lo tanto no se puede realizar esta operación!!!";
            }
            else if (cliente_DOC_tipo == "NO_CASUAL" && cliente_REF_tipo == "NO_CASUAL")
            {
                objres.resultado = false;
                cadena = $"El cliente: {DVTA.codcliente} y el clienteRef.: {DVTA.codcliente_real} ambos son NO CASUALES y a la vez diferentes (códigos cliente), por lo tanto no se puede realizar esta operación!!!";
            }
            else if (cliente_DOC_tipo == "NO_CASUAL" && cliente_REF_tipo == "CASUAL")
            {
                objres.resultado = false;
                cadena = $"El cliente: {DVTA.codcliente} es NO CASUAL y el cliente Ref.: {DVTA.codcliente_real} es CASUAL, por lo tanto no se puede realizar esta operación!!!";
            }

            if (!string.IsNullOrWhiteSpace(cadena))
            {
                objres.resultado = false;
                objres.observacion = "Se tiene la siguiente observación en el enlace del cliente de la proforma con el cliente referencia: ";
                objres.obsdetalle = cadena;
                if (DVTA.estado_doc_vta == "NUEVO")
                {
                    objres.datoA = DVTA.codcliente;
                    objres.datoB = DVTA.nitfactura;
                }
                else
                {
                    objres.datoA = DVTA.id;
                    objres.datoB = DVTA.numeroid;
                }
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }

            return objres;
        }


        public async Task<ResultadoValidacion> Verificar_Descuentos_LineaNivel_Habilitados(DBContext _context, DatosDocVta DVTA, List<string> lista_desc_nivel, List<int> lista_precios, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena = "";
            string cadena2 = "";
            bool resultado_local = true;
            string cadena_tarifa = "";
            string NIVEL_ELEGIDO = "";

            InicializarResultado(objres);

            foreach (var desc in lista_desc_nivel)
            {
                if (await ventas.Descuento_Linea_Habilitado(_context, desc) == false)
                {
                    if (cadena.Trim().Length == 0)
                    {
                        cadena = desc + "-" + await nombres.Nombre_Descuento_De_Nivel(_context, desc)+ " no puede ser aplicado, descuento deshabilitado!!!";
                    }
                    else
                    {
                        cadena += Environment.NewLine + desc + "-" + await nombres.Nombre_Descuento_De_Nivel(_context, desc) + " no puede ser aplicado, descuento deshabilitado!!!";
                    }
                }
                //Dsd 25 - 11 - 2022 se implemento el control de verificar la fecha de inicio y fin de validez de un vedesitem en vedesitem_parametros
                if(desc != null)
                {
                    NIVEL_ELEGIDO = desc.Trim();
                    if (await funciones.FechaDelServidor(_context) < await ventas.Descuento_Linea_Fecha_Desde(_context, desc))
                    {
                        if (cadena.Trim().Length == 0)
                        {
                            cadena = desc + "-" + await nombres.Nombre_Descuento_De_Nivel(_context, desc)+ " no puede ser aplicado, la proforma no debe ser anterior a la fecha inicial de la promocion!!!";
                        }
                        else
                        {
                            cadena += Environment.NewLine + desc + "-" + await nombres.Nombre_Descuento_De_Nivel(_context, desc) + " no puede ser aplicado, la proforma no debe ser anterior a la fecha inicial de la promocion!!!";
                        }
                    }
                }
                if (desc != null)
                {
                    NIVEL_ELEGIDO = desc.Trim();
                    if (await funciones.FechaDelServidor(_context) > await ventas.Descuento_Linea_Fecha_Hasta(_context, desc))
                    {
                        if (cadena.Trim().Length == 0)
                        {
                            cadena = desc + "-" + await nombres.Nombre_Descuento_De_Nivel(_context, desc) + " no puede ser aplicado, la proforma no debe ser despues a la fecha final de la promocion!!!";
                        }
                        else
                        {
                            cadena += Environment.NewLine + desc + "-" + await nombres.Nombre_Descuento_De_Nivel(_context, desc) + " no puede ser aplicado, la proforma no debe ser despues a la fecha final de la promocion!!!";
                        }
                    }
                }

                if (desc != null)
                {
                    NIVEL_ELEGIDO = desc.Trim();
                    if (DVTA.fechadoc < await ventas.Descuento_Linea_Fecha_Desde(_context, desc))
                    {
                        if (cadena.Trim().Length == 0)
                        {
                            cadena = desc + "-" + await nombres.Nombre_Descuento_De_Nivel(_context, desc) + " no puede ser aplicado, la fecha de la proforma no debe ser anterior a la fecha inicial de la promocion!!!";
                        }
                        else
                        {
                            cadena += Environment.NewLine + desc + "-" + await nombres.Nombre_Descuento_De_Nivel(_context, desc) + " no puede ser aplicado, la fecha de la proforma no debe ser anterior a la fecha inicial de la promocion!!!";
                        }
                    }
                }
                //Desde 11 / 03 / 2024 Validar que el descuento de linea este valido segun los precio de la proforma
                if (desc != null)
                {
                    NIVEL_ELEGIDO = desc.Trim();
                    int CODTARIFA_PARA_VALIDAR = await Tarifa_Monto_Min_Mayor(_context, DVTA, lista_precios);
                    List<int> lista_precios_para_validar = new List<int>();
                    lista_precios_para_validar.Add(CODTARIFA_PARA_VALIDAR);
                    foreach (var detalle in tabladetalle)
                    {
                        // verificar si el nivel - tiene habilitado el precio(vedesitem_tarifa)
                        if (await ventas.TarifaValidaNivel(_context, detalle.codtarifa, detalle.niveldesc) == false)
                        {
                            if (cadena2.Trim().Length == 0)
                            {
                                cadena2 = "DesExtra no tiene habilitados precios(vedesitem_tarifa):";
                            }
                            else
                            {
                                cadena2 += Environment.NewLine + "Nivel: " + detalle.niveldesc + " --> Precio: " + detalle.codtarifa;
                            }
                        }
                    }
                    
                }
            }
            cadena = cadena + cadena2;

            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = "Los siguientes descuentos de promoción no estan habilitados y/o la fecha de la proforma invalida: ";
                objres.obsdetalle = cadena;
                if (DVTA.estado_doc_vta == "NUEVO")
                {
                    objres.datoA = DVTA.codcliente;
                    objres.datoB = DVTA.nitfactura;
                }
                else
                {
                    objres.datoA = DVTA.id;
                    objres.datoB = DVTA.numeroid;
                }
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Verificar_Descuentos_Especiales_Habilitados(DBContext _context, DatosDocVta DVTA, List<int> lista_desc_especial, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena = "";
            bool resultado_local = true;
            string cadena_tarifa = "";

            InicializarResultado(objres);

            foreach (var desc in lista_desc_especial)
            {
                if (await ventas.Descuento_Especial_Habilitado(_context, desc) ==false )
                {
                    if (cadena.Trim().Length == 0)
                    {
                        cadena = desc + "-" + await nombres.nombre_descuento_especial(_context, desc);
                    }
                    else
                    {
                        cadena += Environment.NewLine + desc + "-" + await nombres.nombre_descuento_especial(_context, desc);
                    }
                }
                
            }

            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = "Los siguientes descuentos especiales estan actualmente deshabilitados: ";
                objres.obsdetalle = cadena;
                if(DVTA.estado_doc_vta == "NUEVO")
                {
                    objres.datoA = DVTA.codcliente;
                    objres.datoB = DVTA.nitfactura;
                }
                else
                {
                    objres.datoA = DVTA.id;
                    objres.datoB = DVTA.numeroid;
                }
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }

        public async Task<ResultadoValidacion> Validar_Precio_De_Venta_Habilitado_Para_Descto_Especial(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena = "";
            bool resultado_local = true;
            string cadena_tarifa = "";

            InicializarResultado(objres);
            List<int> lista_precios = new List<int>();

            foreach (var detalle in tabladetalle)
            {
                if (!lista_precios.Contains(detalle.codtarifa))
                {
                    lista_precios.Add(detalle.codtarifa);
                }
            }

            foreach (var detalle in tabladetalle)
            {
                if (!await ventas.Precio_Permite_Descto_Especial(_context, detalle.coddescuento, detalle.codtarifa))
                {
                    resultado_local = false;
                    if (cadena.Trim().Length == 0)
                    {
                        cadena = "Configurar en vedescuento_tarifa";
                        cadena += Environment.NewLine + "Item: " + detalle.coditem + " PrecioVta: " + detalle.codtarifa + " No habilitado para DescEspecial: " + detalle.coddescuento;
                    }
                    else
                    {
                        cadena += Environment.NewLine + "Item: " + detalle.coditem + " PrecioVta: " + detalle.codtarifa + " No habilitado para DescEspecial: " + detalle.coddescuento;
                    }
                }
            }

            if (resultado_local == false)
            {
                objres.resultado = false;
                objres.observacion = "Se tienen observaciones en los Descuento especiales y los precios...";
                objres.obsdetalle = cadena;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Precios_Del_Documento(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena = "";
            bool resultado = true;
            string cadena_tarifa = "";

            InicializarResultado(objres);
            List<int> lista_precios = await Lista_Precios_En_El_Documento(tabladetalle);
            foreach (var precios in lista_precios)
            {
                if (cadena_tarifa.Trim().Length == 0)
                {
                    cadena_tarifa = precios.ToString();
                }
                else
                {
                    cadena_tarifa +=", " + precios.ToString();
                }
            }
            if (lista_precios.Count > 1)
            {
                objres.resultado = false;
                objres.observacion = "Se tienen observaciones en los tipos de precio!!!";
                objres.obsdetalle = "El detalle del documento contiene precios: " + cadena_tarifa + ", lo cual no esta permitido.";
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Confirmar_SiNo;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Solicitud_Descuentos_Con_Una_Sola_Proforma(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena = "";
            InicializarResultado(objres);

            if (DVTA.idsol_nivel.Trim().Length > 0 && DVTA.nroidsol_nivel.Trim().Length > 0)
            {
                //primero verificar si la solicitu de descto de nivel existe, si existe buscar si esta vinculada con alguna proforma
                if (await ventas.Existe_Solicitud_Descuento_Nivel(_context, DVTA.idsol_nivel.Trim(),Convert.ToInt32(DVTA.nroidsol_nivel.Trim())))
                {
                     var dt = await _context.veproforma
                       .Where(p =>
                           p.idsoldesctos == DVTA.idsol_nivel &&
                           p.nroidsoldesctos == Convert.ToInt32(DVTA.nroidsol_nivel.Trim()) &&
                           p.anulada == false)
                       .Select(p => new
                       {
                           p.codigo,
                           p.id,
                           p.numeroid,
                           p.fecha,
                           p.codcliente,
                           p.anulada,
                           p.total,
                           p.idsoldesctos,
                           p.nroidsoldesctos,
                           p.aprobada,
                           p.transferida,
                       }).ToListAsync();

                    if (dt.Count() > 0)
                    {
                        var firstRecord = dt.First();
                        if (DVTA.estado_doc_vta == "NUEVO")
                        {
                            objres.resultado = false;

                            cadena = "La solicitud de descuentos de nivel: " + DVTA.idsol_nivel + "-" + DVTA.nroidsol_nivel + " ya esta asignada a la proforma: " + firstRecord.id + ")" + "-" + firstRecord.numeroid + ")";
                            cadena += Environment.NewLine + "por tanto no puede volver a asignarla!!!";
                            objres.observacion = "Se tiene observaciones en la asignacion de la solicitud de descuentos de nivel!!!";
                            objres.obsdetalle = cadena;
                            objres.datoA = "";
                            objres.datoB = "";
                            objres.accion = Acciones_Validar.Ninguna;
                        }
                        else
                        {
                            // si esta modificando una proforma y los descur
                            if (DVTA.id == firstRecord.id && Convert.ToInt32(DVTA.numeroid) == firstRecord.numeroid)
                            {
                                //si es la misma esta bien
                            }
                            else
                            {//si es dfte quiere decir que esta cambiando entonces esta bien tb
                            }
                        }
                    }
                }
            }
            return objres;
        }

        public async Task<ResultadoValidacion> Verificar_Cantidad_Maximo_De_Venta_Cliente_Items(DBContext _context, string id, int numeroid, string nitfactura, string estado_doc_vta, string codcliente_real, int codalmacen, DateTime fecha, List<itemDataMatriz> tabladetalle, List<Dtnocumplen> dtnocumplen, string codempresa, string usuario)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena_items = "";
            string cadena_solo_items = "";
            decimal max = 0;
            bool resultado = true;
            int diascontrol = 0;
            decimal cantidad_ttl_vendida_pf = 0;
            decimal cantidad_ttl_vendida_pf_actual = 0;
            decimal cantidad_ttl_vendida_pf_total = 0;
            decimal PorcenMaxVtaAg = await hardcoded.MaximoPorcentajeDeVentaPorMercaderia(_context, codalmacen);
            string valido = "";
            string valida_nr_pf = "";

            InicializarResultado(objres);

            //esta funcion verifica si el cliente - item - alacen tiene configurado un maximo de ventas en $ cada cierto numero de dias
            try
            {
                diascontrol = await configuracion.Dias_Proforma_Vta_Item_Cliente(_context, codempresa);
                valida_nr_pf = await configuracion.Valida_Maxvta_NR_PF(_context, codempresa);

                foreach (var detalle in tabladetalle)
                {
                    if (diascontrol > 0 && Convert.ToDouble(detalle.cantidad) > 0)
                    {//obtiene los items y la cantidad en proformas en los ultimos X dias mas lo que se quiere vender a ahora
                        cantidad_ttl_vendida_pf = 0;
                        cantidad_ttl_vendida_pf_actual = 0;
                        cantidad_ttl_vendida_pf_total = 0;
                        cantidad_ttl_vendida_pf_actual = Convert.ToDecimal(detalle.cantidad);

                        if (valida_nr_pf == "PF")
                        {
                            cantidad_ttl_vendida_pf = await cliente.CantidadVendida_PF(_context, detalle.coditem, codcliente_real, fecha.AddDays(-diascontrol), fecha.Date);
                        }
                        else if (valida_nr_pf == "NR")
                        {
                            cantidad_ttl_vendida_pf = await cliente.CantidadVendida_NR(_context, detalle.coditem, codcliente_real, fecha.AddDays(-diascontrol), fecha.Date);
                        }
                        else
                        {
                            cantidad_ttl_vendida_pf = await cliente.CantidadVendida_PF(_context, detalle.coditem, codcliente_real, fecha.AddDays(-diascontrol), fecha.Date);
                        }

                        cantidad_ttl_vendida_pf_total = cantidad_ttl_vendida_pf + cantidad_ttl_vendida_pf_actual;
                        //aqui verificar si la cantidad sumada con las pf q hubieran y la pf actual sobrepasan el porcentaje o no
                        //VALIDAR PORCENTAJE MAXIMO DE VENTA
                        if (cantidad_ttl_vendida_pf_total > Convert.ToDecimal(detalle.cantidad))
                        {
                            //poner la cantidad del item obtenido de la suma de la cantidad actual de la pf y de las pf q hubiera
                            detalle.cantidad = (double)cantidad_ttl_vendida_pf_total;
                            // añadir al la dt copia para validar el max vta con las cantidades sumadas
                            List<itemDataMatrizMaxVta> dt_detalle_item = new List<itemDataMatrizMaxVta>();
                            dt_detalle_item.Clear();

                            itemDataMatrizMaxVta nuevoItem = new itemDataMatrizMaxVta
                            {
                                coditem = detalle.coditem,
                                descripcion = detalle.descripcion,
                                medida = detalle.medida,
                                udm = detalle.udm,
                                porceniva = detalle.porceniva,
                                niveldesc = detalle.niveldesc,
                                porcendesc = detalle.porcendesc,
                                porcen_mercaderia = detalle.porcen_mercaderia,
                                cantidad_pedida = detalle.cantidad_pedida,
                                cantidad = (double)cantidad_ttl_vendida_pf_actual,
                                codtarifa = detalle.codtarifa,
                                coddescuento = detalle.coddescuento,
                                precioneto = detalle.precioneto,
                                preciodesc = detalle.preciodesc,
                                preciolista = detalle.preciolista,
                                total = detalle.total,
                                cumple = detalle.cumple,
                                nroitem = detalle.nroitem,
                                cantidad_pf_anterior = (double)cantidad_ttl_vendida_pf,
                                cantidad_pf_total = (double)cantidad_ttl_vendida_pf_total
                            };

                            // Añadir el nuevo objeto a la lista dt_detalle_item
                            dt_detalle_item.Add(nuevoItem);

                            //poner la cantidad original del item de la proforma actual
                            detalle.cantidad = (double)cantidad_ttl_vendida_pf_actual;

                            (objres, dtnocumplen) = await Validar_Limite_Maximo_de_Venta_por_Item(_context, id, numeroid, nitfactura, estado_doc_vta, codcliente_real, codalmacen, dt_detalle_item, dtnocumplen, codempresa, usuario);
                            if (!objres.resultado)
                            {
                                valido = "NO";
                            }

                            if (objres.resultado)
                            {
                                detalle.cumple = true;
                            }
                            else
                            {
                                detalle.cumple = false;
                                if (cadena_items.Length > 0)
                                {
                                    cadena_solo_items += ", "+ detalle.coditem;
                                    cadena_items +=Environment.NewLine + "Item: "+ detalle.coditem +" cantidad Vendida en PF(s) anterior(es) grabada(s) del mismo cliente es de: "+ cantidad_ttl_vendida_pf.ToString("####,##0.000", new CultureInfo("en-US")) +"hace: "+ diascontrol +" días atrás, más la cantidad de PF actual de: "+ cantidad_ttl_vendida_pf_actual.ToString("####,##0.000", new CultureInfo("en-US")) +", suman: " + cantidad_ttl_vendida_pf_total.ToString("####,##0.000", new CultureInfo("en-US")) + ", que sobrepasan el máximo porcentaje de venta permitido.";
                                }
                                else
                                {
                                    cadena_items = Environment.NewLine +"Item: "+ detalle.coditem +" cantidad Vendida en PF(s) anterior(es) grabada(s) del mismo cliente es de: "+ cantidad_ttl_vendida_pf.ToString("####,##0.000", new CultureInfo("en-US")) + " hace: "+ diascontrol + " días atrás, más la cantidad de PF actual de: "+ cantidad_ttl_vendida_pf_actual.ToString("####,##0.000", new CultureInfo("en-US")) + ", suman: "+ cantidad_ttl_vendida_pf_total.ToString("####,##0.000", new CultureInfo("en-US")) +", que sobrepasan el máximo porcentaje de venta permitido.";
                                    cadena_solo_items += detalle.coditem;
                                }
                            }
                        }
                        else
                        {
                            detalle.cumple = true;
                        }
                    }
                    else
                    {
                        detalle.cumple = true;
                    }
                }

                if (cadena_items.Length > 0)
                {
                    objres.resultado = false;
                    objres.observacion = "Los siguientes items, ya fueron comprados por el cliente: "+ codcliente_real +" en los últimos"+ diascontrol +" días y sumados sobrepasan el máximo de venta permitido.";
                    objres.obsdetalle = cadena_items.ToString();
                    objres.datoA = estado_doc_vta == "NUEVO" ?  nitfactura + "-"+ codcliente_real : id +"-"+ numeroid+"-"+ codcliente_real;
                    objres.datoB = cadena_solo_items.ToString();
                    objres.accion = Acciones_Validar.Pedir_Servicio;
                }
                else
                {
                    objres.resultado = true;
                    objres.observacion = "";
                    objres.obsdetalle = "";
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                }
            }
            catch (Exception)
            {
                objres.resultado = false;
                objres.observacion = "Error en la validación";
            }
            return objres;
        }
        public async Task<ResultadoValidacion> FC_Validar_Anticipo_Aplicado_Factura_mostrador(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena_obs = "";
            bool resultado = true;
            int diascontrol = 0;
            decimal ttl_cantidad_vendida = 0;
            string cliente_de_anticipo = "";

            InicializarResultado(objres);

            if (DVTA.idanticipo.Trim().Length > 0)
            {
                if (await cobranzas.Anticipo_Esta_Anulado(_context, DVTA.idanticipo, DVTA.noridanticipo))
                {
                    cadena_obs = "El anticipo elegido: "+ DVTA.idanticipo +"-" + DVTA.noridanticipo +" esta ANULADO, por tanto no lo puede revertir!!!";
                    resultado = false;

                    objres.resultado = false;
                    objres.observacion = "Se tiene observaciones en la aplicacion del anticipo!!!";
                    objres.obsdetalle = cadena_obs;
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                    return objres;
                }
                //si no esta anulado revisar los demas controles
                if (await cobranzas.Existe_Anticipo(_context, DVTA.idanticipo,Convert.ToInt32(DVTA.noridanticipo)))
                {
                    string clienteDeAnticipo = await cobranzas.Cliente_De_Anticipo(_context, DVTA.idanticipo, Convert.ToInt32(DVTA.noridanticipo));
                    //verificar si el anticipo elegido es del mismo cliente
                    if (!clienteDeAnticipo.Equals(DVTA.codcliente, StringComparison.OrdinalIgnoreCase))
                    {
                        resultado = false;
                        cadena_obs = "El cliente del anticipo: " + DVTA.idanticipo + "-" + DVTA.noridanticipo + " es: " + cliente_de_anticipo + " no es el mismo cliente de la factura, el cliente de la factura es: " + DVTA.codcliente; ;
                    }
                    //VERIFICAR QUE INDIQUE EL MONTO ASIGNADO DE ANTICIPO
                    if (resultado && DVTA.monto_anticipo <= 0)
                    {
                        resultado = false;
                        cadena_obs = "Debe indicar el monto del anticipo. Por favor corrija el numero de anticipo o modifique los datos del anticipo.";
                    }
                    //VERIFICAR QUE NO SE ASIGNE UN MONTO MAYOR AL MONTO DEL TOTAL DE LA FACTURA
                    if (resultado && DVTA.monto_anticipo > DVTA.totaldoc)
                    {
                        //en fecha_ 18-12-2021 Mariela indico que  el monto de anticipo asignado puede ser mayor pero como
                        //maximo hasta 0.09, si es la diferencia es mayor a o
                        double difX = Math.Round(DVTA.monto_anticipo - DVTA.totaldoc, 2);
                        if (difX >= 0.1)
                        {
                            resultado = false;
                            cadena_obs = "El monto asignado de anticipo: "+ DVTA.monto_anticipo.ToString("####,##0.000", new CultureInfo("en-US")) +" es mayor a monto total de la factura:"+ DVTA.totaldoc.ToString("####,##0.000", new CultureInfo("en-US")) +" ("+ DVTA.codmoneda +")";
                        }
                    }
                    //VERIFICAR QUE EL MONTO DEL ANTICIPO MAS ESTE NO PASE EL TOTAL DEL ANTICIPO
                    if (resultado)
                    {
                        //obtener el monto del anticipo en la moneda de la factura
                        decimal totalAnticipo = Math.Round(await cobranzas.AnticipoMonto(_context, DVTA.idanticipo, Convert.ToInt32(DVTA.noridanticipo), DVTA.codmoneda), 2);
                        //los movimientos con el anticipo
                        decimal totalReversionCbzaContado = Math.Round(await cobranzas.Anticipo_Monto_Total_Revertido_A_Cobranza_Contado(_context, DVTA.idanticipo, Convert.ToInt32(DVTA.noridanticipo), DVTA.codmoneda), 2);
                        decimal totalReversionCbza = Math.Round(await cobranzas.Anticipo_Monto_Total_Revertido_A_Cobranza(_context, DVTA.idanticipo, Convert.ToInt32(DVTA.noridanticipo), DVTA.codmoneda), 2);
                        decimal totalDevolucion = Math.Round(await cobranzas.Anticipo_Monto_Total_Devolucion(_context, DVTA.idanticipo, Convert.ToInt32(DVTA.noridanticipo), DVTA.codmoneda), 2);
                        decimal totalAsignadoFacturaMostrador = Math.Round(await cobranzas.Anticipo_Monto_Total_Asignado_En_Factura_Mostrador(_context, DVTA.idanticipo, Convert.ToInt32(DVTA.noridanticipo), DVTA.codmoneda), 2);
                        decimal totalAsignadoEnProforma = Math.Round(await cobranzas.Anticipo_Monto_Total_Asignado_En_Proforma(_context, DVTA.idanticipo, Convert.ToInt32(DVTA.noridanticipo), DVTA.codmoneda), 2);

                        decimal totalAsignado = Math.Round(totalReversionCbzaContado + totalReversionCbza + totalDevolucion + totalAsignadoFacturaMostrador + totalAsignadoEnProforma + (decimal)DVTA.monto_anticipo, 2);

                        if (totalAsignado > totalAnticipo)
                        {
                            cadena_obs = "El anticipo: " + DVTA.idanticipo + "-" + DVTA.noridanticipo + " tiene asignados: " + totalAsignado.ToString("####,##0.000", new CultureInfo("en-US")) + " (" + DVTA.codmoneda + ") en total (incluye esta asignacion), con lo que sobrepasa el monto total del anticipo, el monto total del anticipo es de: " + totalAnticipo.ToString("####,##0.000", new CultureInfo("en-US"));
                            objres.resultado = false;
                            objres.observacion = "Se tiene observaciones en la aplicacion del anticipo!!!";
                            objres.obsdetalle = cadena_obs;
                            objres.datoA = "";
                            objres.datoB = "";
                            objres.accion = Acciones_Validar.Ninguna;
                        }
                    }
                    //si ocurrio algo no valido generar el resultado como que no valido
                    if (resultado == false)
                    {
                        objres.resultado = false;
                        objres.observacion = "Se tiene observaciones en la aplicacion del anticipo!!!";
                        objres.obsdetalle = cadena_obs;
                        objres.datoA = "";
                        objres.datoB = "";
                        objres.accion = Acciones_Validar.Ninguna;
                    }
                }
                else
                {
                    cadena_obs = "El anticipo elegido: " + DVTA.idanticipo + "-" + DVTA.noridanticipo + " no existe!!!";
                    objres.resultado = false;
                    objres.observacion = "Se tiene observaciones en la aplicacion del anticipo!!!";
                    objres.obsdetalle = cadena_obs;
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                }
            }
            return objres;
        }

        public async Task<ResultadoValidacion> FC_Validar_Deuda_Pendiente_Cliente_Tienda(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena_obs = "";
            bool resultado = true;
            int diascontrol = 0;
            decimal ttl_cantidad_vendida = 0;

            InicializarResultado(objres);
            decimal debe = 0;
            debe = await cliente.cliente_debe_vencido(_context, DVTA.codcliente, DVTA.codmoneda, DVTA.fechadoc.Date);
            if (debe > 0)
            {
                resultado = false;
                cadena_obs = "Este Cliente debe: " + debe.ToString("###,##0.00") + " (" + DVTA.codmoneda + ") , Para ver el detalle de su deuda utilice la barra de herramientas ubicada en la parte superior derecha de la ventana, Desea Continuar a pesar de esta cuenta pendiente? ";
            }

            if (resultado == false)
            {
                objres.resultado = false;
                objres.observacion = "Se tiene observaciones en los siguientes items que se quiere facturar puesto que sobrepasan el maximo de venta permitido en: " + diascontrol + " (dias):";
                objres.obsdetalle = cadena_obs;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Confirmar_SiNo;
            }
            return objres;
        }

        public async Task<ResultadoValidacion> FC_Validar_Maximo_De_Venta_Item(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena_obs = "";
            bool resultado = true;
            int diascontrol = 0;
            decimal ttl_cantidad_vendida = 0;

            InicializarResultado(objres);

            if (await cliente.ClienteControlaMaximo(_context, DVTA.codcliente))
            {
                decimal max = 0;
                foreach (var detalle in tabladetalle)
                {
                    max = (decimal)await items.MaximoDeVenta(_context, detalle.coditem, DVTA.codalmacen, detalle.codtarifa);
                    if (max > 0)//si controlar
                    {
                        diascontrol = await items.MaximoDeVenta_PeriodoDeControl(_context, detalle.coditem, DVTA.codalmacen, detalle.codtarifa);
                        if (diascontrol > 0)//si controlar
                        {
                            //obtener la cantidad vendida al cliente en los N dias ultimos
                            ttl_cantidad_vendida = 0;
                            ttl_cantidad_vendida = (decimal)(await cliente.CantidadVendida(_context, detalle.coditem, DVTA.codcliente, DVTA.fechadoc.Date.AddDays(-diascontrol), DVTA.fechadoc.Date) + detalle.cantidad);
                            if (ttl_cantidad_vendida <= max)
                            {
                                detalle.cumpleMin = true;
                            }
                            else
                            {
                                detalle.cumpleMin = false;
                                resultado = false;
                                cadena_obs += Environment.NewLine + detalle.coditem + " Maximo vta: " + max.ToString("####,##0.000", new CultureInfo("en-US")) + " Vta Actual+Anteriores: " + ttl_cantidad_vendida.ToString("####,##0.000", new CultureInfo("en-US"));
                            }
                        }
                        else
                        {
                            detalle.cumple = true;
                        }

                    }
                    else
                    {
                        detalle.cumple = true;
                    }
                }

            }

            if (resultado == false)
            {
                objres.resultado = false;
                objres.observacion = "Se tiene observaciones en los siguientes items que se quiere facturar puesto que sobrepasan el maximo de venta permitido en:" + diascontrol + " (dias):";
                objres.obsdetalle = cadena_obs;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_NIT_Existente_Facturado(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena_obs = "";
            bool resultado = true;

            InicializarResultado(objres);
            if (DVTA.nitfactura.Trim() != "0")
            {
                string cliente_ = "";
                string cliente_nit_facturado = "";
                cliente_ = await cliente.Cliente_Segun_Nit(_context, DVTA.nitfactura);

                if (cliente_.Trim() == "")
                {
                    resultado = true;
                }
                else
                {
                    if (await cliente.clientehabilitado(_context, cliente_))
                    {
                        if (cliente_ != DVTA.codcliente.Trim())
                        {
                            cadena_obs = "Este NIT/CI pertenece al cliente: " + cliente_ + ", Por favor utilice ese codigo para realizar la venta.";
                            resultado = false;
                            objres.resultado = false;
                            objres.observacion = "Se encontro observaciones con el NIT al cual desea facturar!!!";
                            objres.obsdetalle = cadena_obs;
                            objres.datoA = "";
                            objres.datoB = "";
                            objres.accion = Acciones_Validar.Confirmar_SiNo;

                        }
                    }
                }
                if (resultado == true)
                {
                    cliente_nit_facturado = await cliente.Cliente_Nit_facturado(_context, DVTA.nitfactura, DVTA.nombcliente);
                    if (cliente_nit_facturado.Trim() == "")
                    {
                        resultado = true;
                    }
                    else
                    {
                        cadena_obs = "Con ese NIT se facturo previamente a nombre de: " + cliente_nit_facturado + ", esta seguro de continuar???";
                        objres.resultado = false;
                        objres.observacion = "Se encontro observaciones con el NIT al cual desea facturar!!!";
                        objres.obsdetalle = cadena_obs;
                        objres.datoA = "";
                        objres.datoB = "";
                        objres.accion = Acciones_Validar.Confirmar_SiNo;
                    }
                }
            }
            if (resultado == true)
            {
                objres.resultado = true;
                objres.observacion = "";
                objres.obsdetalle = "";
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Fc_Validar_IdCuenta_Venta_Contado_Tienda(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena_obs = "";
            bool resultado = true;

            InicializarResultado(objres);

            try
            {
                if (DVTA.tipo_vta.Trim() == "CONTADO")
                {
                    if (DVTA.codalmacen.Trim().Length > 0)
                    {
                        objres.observacion = hardcoded.CuentaDeEfectivoVtaContado(DVTA.codalmacen.Trim());
                        objres.obsdetalle = await nombres.nombrecuenta_fondos(_context, objres.observacion);
                        resultado = true;
                    }
                    else
                    {
                        objres.observacion = hardcoded.CuentaDeEfectivoVtaContado(Convert.ToString(await empresa.AlmacenLocalEmpresa(_context, codempresa)));
                        objres.obsdetalle = await nombres.nombrecuenta_fondos(_context, objres.observacion);
                        resultado = true;
                    }
                }
                else
                {
                    objres.observacion = "Ocurrio un error al validar la el ID de Cuente Venta al Contado";
                    objres.obsdetalle = "";
                    resultado = false;
                }
            }
            catch
            {
                objres.observacion = "Ocurrio un error al validar la el ID de Cuente Venta al Contado";
                objres.obsdetalle = "";
                resultado = false;
            }
            if (resultado == false)
            {
                objres.resultado = false;
                objres.observacion = objres.observacion;
                objres.obsdetalle = objres.obsdetalle;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Fc_Validar_Venta_Credito_Tienda(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena_obs = "";
            bool resultado = true;

            InicializarResultado(objres);

            try
            {
                if (DVTA.tipo_vta == "CREDITO")
                {
                    cadena_obs = "Las ventas al Credito en tiendas no estan permitidas!!!";
                    resultado = false;
                }
            }
            catch
            {
                cadena_obs = "Ocurrio un error al validar la venta al credito!!!";
                resultado = false;
            }
            if (resultado == false)
            {
                objres.resultado = false;
                objres.observacion = "Se encontraron observaciones en tipo de venta:";
                objres.obsdetalle = cadena_obs;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }

        public async Task<ResultadoValidacion> Fc_Validar_Tipo_Dosificacion(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena_obs = "";
            bool resultado = true;

            InicializarResultado(objres);

            try
            {
                if (DVTA.tipo_caja.Trim() == "")
                {
                    cadena_obs = "La dosificacion no tiene tipo de documento.";
                    resultado = false;
                }
                else
                {
                    if (await cliente.TipoDeFactura(_context, DVTA.codcliente_real) == DVTA.tipo_caja)
                    {}
                    else
                    {
                        cadena_obs = "El tipo de documento no es el correcto para el cliente, deberia ser tipo: " + cliente.TipoDeFactura(_context, DVTA.codcliente_real) + ".";
                        resultado = false;
                    }
                }
            }
            catch
            {
                cadena_obs = "Ocurrio un error al validar el tipo de dosificacion";
                resultado = false;
            }
            if (resultado == false)
            {
                objres.resultado = false;
                objres.observacion = "Se encontraron observaciones en tipo de dosificacion!!!";
                objres.obsdetalle = cadena_obs;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Fc_Validar_Fecha_Limite_Dosificacion(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena_obs = "";
            bool resultado = true;

            InicializarResultado(objres);

            try
            {
                if (DVTA.fechadoc.Date > DVTA.fechalimite_dosificacion.Date)
                {
                    cadena_obs = "La fecha del documento excede la fecha limite de la dosificacion: " + DVTA.fechalimite_dosificacion.Date.ToString();
                    resultado = false;
                }
            }
            catch
            {
                cadena_obs = "Ocurrio un error al validar la fecha limite de la dosificacion";
                resultado = false;
            }
            if (resultado == false)
            {
                objres.resultado = false;
                objres.observacion = "Se encontraron observaciones en la fecha limite de dosificacion!!!";
                objres.obsdetalle = cadena_obs;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Fc_Validar_Nro_Items_Para_Facturar_Por_Caja(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena_obs = "";
            bool resultado = true;
            int nro_items = await ventas.numitemscaja(_context,Convert.ToInt32(DVTA.nrocaja));

            InicializarResultado(objres);

            try
            {
                if (tabladetalle.Count > nro_items)
                {
                     cadena_obs = "El numero de items permitidos para facturar de esta caja es: " + nro_items + " y la factura actual es tiene: " + tabladetalle.Count + " items.";
                     resultado = false;
                }
            }
            catch
            {
                cadena_obs = "Ocurrio un error al validar el nro. de items a factura por caja";
                resultado = false;
            }
            if (resultado == false)
            {
                objres.resultado = false;
                objres.observacion = "Se encontraron observaciones en el nro.de items a facturar!!!";
                objres.obsdetalle = cadena_obs;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Fc_Validar_Nombre_Factura_Valido(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena_obs = "";
            bool resultado = true;

            InicializarResultado(objres);

            try
            {
                if (DVTA.nombcliente.Trim().Length > 0)
                {
                    if (!char.IsLetterOrDigit(DVTA.nombcliente.Trim()[0]))
                    {
                        cadena_obs = "El nombre a facturar debe empezar con una letra del alfabeto o con un numero, verifique esta situacion.";
                        resultado = false;
                    }
                }
                else
                {
                    cadena_obs = "Debe indicar nombre Valido para la factura.";
                    resultado = false;
                }

            }
            catch
            {
                resultado = false;
            }
            if (resultado == false)
            {
                objres.resultado = false;
                objres.observacion = cadena_obs;
                objres.obsdetalle = "";
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Fc_Validar_Monto_Maximo_Vta_Cliente_SN(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            //VALIDAR EL MONTO MAXIMO DE VENTA AL CLIENTE
            //CON EL FIN DE QUE CLIENTE SIN NOMBRE NO SE 
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena_obs = "";
            bool resultado = true;

            InicializarResultado(objres);

            try
            {
                decimal monto_max_vta_snomb = await cliente.Maximo_Vta(_context, DVTA.codcliente);
                string codmoneda_monto_max_vta_snomb = await cliente.Maximo_Vta_Moneda(_context, DVTA.codcliente);
                if (codmoneda_monto_max_vta_snomb.Trim().Length == 0)
                {
                    codmoneda_monto_max_vta_snomb = await Empresa.monedabase(_context, codempresa);
                }
                if (Convert.ToDecimal(DVTA.totaldoc) > monto_max_vta_snomb)
                {
                    cadena_obs = "No Se puede vender este monto a este Cliente, ya que sobrepasa el monto maximo de venta. El maximo de venta es: " + monto_max_vta_snomb.ToString("####,##0.000", new CultureInfo("en-US")) + "(" + codmoneda_monto_max_vta_snomb + ")";
                    resultado = false;
                }
            }
            catch
            {
                resultado = false;
            }
            if (resultado == false)
            {
                objres.resultado = false;
                objres.observacion = cadena_obs;
                objres.obsdetalle = "";
                objres.datoA = DVTA.codcliente + "-" + DVTA.nombcliente + "-" + DVTA.nitfactura;
                objres.datoB = "Total: " + DVTA.totaldoc.ToString("####,##0.000", new CultureInfo("en-US")) + " (" + DVTA.codmoneda + ")";
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Fc_Validar_Factura_Complementaria(DBContext _context, List<itemDataMatriz> tabladetalle, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena_obs = "";
            bool resultado = true;

            InicializarResultado(objres);

            try
            {
                if (DVTA.idFC_complementaria.Trim() != "" && DVTA.nroidFC_complementaria.Trim() != "")
                {
                    if (await ventas.Existe_Factura(_context, DVTA.idFC_complementaria, Convert.ToInt32(DVTA.nroidFC_complementaria)) == false)
                    {
                        resultado = false;
                        cadena_obs += Environment.NewLine + "La factura de la cual este documento es complementaria no existe.";
                    }
                    else
                    {
                        //cliente valido
                        if(DVTA.codcliente != await ventas.Cliente_De_Factura(_context, DVTA.idFC_complementaria, Convert.ToInt32(DVTA.nroidFC_complementaria)))
                        {
                            resultado = false;
                            cadena_obs += Environment.NewLine + "La factura de la cual este documento es complementario debe ser del mismo cliente.";
                        }
                        else
                        {
                            //dias validos
                            if ((await configuracion.DuracionHabilAsync(_context,Convert.ToInt32(DVTA.codalmacen), await Ventas.Fecha_de_Factura(_context, DVTA.idFC_complementaria, Convert.ToInt32(DVTA.nroidFC_complementaria)), DVTA.fechadoc.Date) - 1) > await empresa.diascompleempresa(_context, codempresa))
                            {
                                resultado = false;
                                cadena_obs += Environment.NewLine + "La factura de la cual este documento es complementario sobrepasa el limite de dias de diferencia.";
                            }
                            else
                            {
                                //Verificar montos minimos validos
                                if (FacturaConDescuentosEspeciales(tabladetalle))
                                {
                                    if (await FacturaComplementariaCumpleMinimo(_context, DVTA, false, tabladetalle, codempresa))
                                    {
                                        ///no hace nada
                                    } else {
                                        resultado = false;
                                        cadena_obs += Environment.NewLine + "La factura no cumple el monto minimo para ser complementaria.";
                                    }
                                }
                                else
                                {
                                    if (await FacturaComplementariaCumpleMinimo(_context, DVTA, true, tabladetalle, codempresa))
                                    {
                                        ///no hace nada
                                    }
                                    else
                                    {
                                        resultado = false;
                                        cadena_obs += Environment.NewLine + "La factura no cumple el monto minimo para ser complementaria.";
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                resultado = false;
            }
            //si el codproforma es 0 quiere decir que no hay enlace entonces este control es valido
            if (resultado == false)
            {
                objres.resultado = false;
                objres.observacion = "Se encontro observaciones en la factura complementaria:";
                objres.obsdetalle = "Si va a Vender en kilos esta mercaderia, Debe adicionar el Descuento 6 ZDKG (0%) al documento para generar un plan de pagos correcto." + cadena_obs;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }        
            return objres;
        }
        public bool FacturaConDescuentosEspeciales(List<itemDataMatriz> tabladetalle)
        {
            bool resultado = false;
            foreach (var detalle in tabladetalle)
            {
                if (Convert.ToInt32(detalle.coddescuento) > 0)
                {
                    resultado = true;
                    break;
                }
            }
            return resultado;
        }
        public async Task<bool> FacturaComplementariaCumpleMinimo(DBContext _context, DatosDocVta DVTA, bool sindesc, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            bool resultado = true;
            try
            {
                if (DVTA.totaldoc >= (double)await empresa.MinimoComplementarAsync(_context, sindesc, FacturaPrimeraTarifa(tabladetalle), codempresa, DVTA.codmoneda, DVTA.fechadoc))
                {

                }
                else { resultado = false; }

            }
            catch
            {
                resultado = false;
            }
            return resultado;
        }
        public int FacturaPrimeraTarifa(List<itemDataMatriz> tabladetalle)
        {
            int min_tarifa = 999999;
            foreach (var detalle in tabladetalle)
            {
                if (Convert.ToInt32(detalle.codtarifa) < min_tarifa)
                {
                    min_tarifa = detalle.codtarifa;
                }
            }
            if (min_tarifa == 999999)
            {
                min_tarifa = 0;
            }
            return min_tarifa;
        }
        public async Task<ResultadoValidacion> Validar_Enlace_Proforma_Mayorista_Dimediado(DBContext _context, List<itemDataMatriz> tabladetalle, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena = "";
            string resultado_cadena = "";
            int codproforma = 0;
            bool hay_enlace = false;
            bool resultado = true;

            InicializarResultado(objres);

            //verificar su hay enlacen con proforma
            try
            {
                if (DVTA.idpf_complemento.Trim().Length > 0 && DVTA.nroidpf_complemento.Trim().Length > 0 )
                {
                    if (Convert.ToInt32(DVTA.nroidpf_complemento) > 0)
                    {
                        if (await ventas.Existe_Proforma(_context, DVTA.idpf_complemento, Convert.ToInt32(DVTA.nroidpf_complemento)) == false)
                        {
                            objres.resultado = false;
                            objres.observacion = "";
                            objres.obsdetalle = "No existe la proforma: " + DVTA.idpf_complemento + "-" + DVTA.nroidpf_complemento;
                            objres.datoA = "";
                            objres.datoB = "";
                            objres.accion = Acciones_Validar.Ninguna;
                            return objres;
                        }
                        else
                        {
                            codproforma = await ventas.codproforma(_context, DVTA.idpf_complemento, Convert.ToInt32(DVTA.nroidpf_complemento));
                            hay_enlace = true;
                        }
                    }
                    else
                    {
                        codproforma = 0;
                        hay_enlace = false;
                    }
                }
                else
                {
                    codproforma = 0;
                    hay_enlace = false;
                }
            }
            catch
            {
                hay_enlace = false;
                codproforma = 0;
            }
            //si el codproforma es 0 quiere decir que no hay enlace entonces este control es valido
            if (codproforma == 0)
            {
                objres.resultado = true;
                objres.observacion = "";
                objres.obsdetalle = "";
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
                return objres;
            }
            //verificar si la proforma esta aprobada
            if (await ventas.proforma_aprobada(_context, codproforma) == false) 
            {
                cadena += Environment.NewLine + "La proforma: " + DVTA.idpf_complemento + "-" + DVTA.nroidpf_complemento + " no esta aprobada, se requiere una proforma aprobada para complementar Precio Mayorista Con Dimediado";
                resultado = false;
            }
            //verificar si la proforma con la cual quiere enlazar es del mismo cliente
            if (await ventas.Cliente_De_Proforma(_context, codproforma) != DVTA.codcliente.Trim())
            {
                cadena += Environment.NewLine + "La proforma: " + DVTA.idpf_complemento + "-" + DVTA.nroidpf_complemento + " no es del mismo cliente respecto de la proforma actual!!!";
                resultado = false;
            }

            //verifica las fechas de las dos proformas
            DateTime fechaPf1 = await ventas.Fecha_Autoriza_de_Proforma(_context, DVTA.idpf_complemento,Convert.ToInt32(DVTA.nroidpf_complemento));
            int dias_dif = 0;
            TimeSpan diferencia = DVTA.fechadoc - fechaPf1;
            dias_dif = diferencia.Days;

            if (dias_dif > 3)
            {
                cadena += Environment.NewLine + "La diferencia entre la proforma: " + DVTA.idpf_complemento + "-" + DVTA.nroidpf_complemento + " y la proforma actual es mayor 3 dias, no se puede complementar!!!";
                resultado = false;
            }
            //verificar si la proforma con la cual se quiere enlazar esta aprecio mayorista
            string tipo = "";
            List<int> lstprecios = new List<int>();
            lstprecios = await ventas.Proforma_Lista_Tipo_Precio(_context, codproforma);
            //verificar si la proforma con la cual quiere enlazar es mayorista, solo se enlazan a prodformas mayoristas
            foreach (int precio in lstprecios)
            {
                tipo = await ventas.Tarifa_tipo(_context, precio);
                if (tipo != "MAYORISTA")
                {
                    cadena += Environment.NewLine + "La proforma elegida " + DVTA.idpf_complemento + "-" + DVTA.nroidpf_complemento + " no está aprobada con precio Mayorista, por lo tanto no se puede realizar el complemento!!!";
                    resultado = false;
                }
            }
            //obtener los precios del detalle de la proforma
            string cadena_items = "";
            //verificar si los items del pedido 2 son de los que tienen empaque dimediado
            DataTable dtitem = new DataTable();
           // string qry = "";
            string cadena_no_dim = "";

            foreach (var detalle in tabladetalle)
            {
                if (cadena_items.Trim().Length == 0)
                {
                    cadena_items = "'" + detalle.coditem + "'";
                }
                else
                {
                    cadena_items += ",'" + detalle.coditem + "'";
                }
            }
            dtitem.Clear();

            var codigos = tabladetalle.Select(item => item.coditem).Distinct().ToList();

                var qry = from p1 in _context.initem
                          where codigos.Contains(p1.codigo)
                                && !_context.veempaque1.Any(v => v.item == p1.codigo && new[] { 36, 46, 86 }.Contains(v.codempaque))
                          select new
                          {
                              p1.codigo,
                              p1.descripabr,
                              p1.medida
                          };

                var result = qry.Distinct().ToList();
                dtitem = funciones.ToDataTable(result);

                foreach (var row in qry)
                {
                    if (cadena_no_dim.Trim().Length == 0)
                    {
                        cadena_no_dim = "Los sgtes. Items no tienen empaque dimediado:" + Environment.NewLine
                                        + "(" + row.codigo + ") " + row.descripabr + "  " + row.medida;
                    }
                    else
                    {
                        cadena_no_dim += Environment.NewLine
                                        + "(" + row.codigo + ") " + row.descripabr + "  " + row.medida;
                    }
                    resultado = false;
                }
            cadena += Environment.NewLine + cadena_no_dim;
            //verificar si el precio es dimediado
            lstprecios.Clear();
            foreach (var detalle in tabladetalle)
            {
                if (!lstprecios.Contains((int)detalle.codtarifa))
                {
                    lstprecios.Add((int)detalle.codtarifa);
                }
            }

            foreach (var lista in lstprecios)
            {
                var qry1 = await _context.intarifa_dimediado
                                    .Where(v => v.codtarifa == lista)
                                    .CountAsync();
                
                if (qry1 == 0)
                {
                    cadena += Environment.NewLine + "La proforma actual esta a precio: " + lista + " el cual no es precio Dimediado, por lo tanto no se puede realizar el complemento!!!";
                    resultado = false;
                }
            }
            //verificar si la proforma con la cual quiere complementar ya esta siendo complementadaa en otra
            DataTable dt = new DataTable();
            string idpf_comp = "";
            int nroidpf_comp = 0;
               var proforma = (from p in _context.veproforma
                                where p.idpf_complemento == DVTA.idpf_complemento && p.nroidpf_complemento == Convert.ToInt32(DVTA.nroidpf_complemento)
                                select new
                                {
                                    p.id,
                                    p.numeroid
                                }).FirstOrDefault();

                if (proforma != null)
                {
                    idpf_comp = proforma.id.Trim();
                    nroidpf_comp = proforma.numeroid;

                    if (!string.IsNullOrEmpty(idpf_comp) && !string.IsNullOrEmpty(Convert.ToString(nroidpf_comp)))
                    {
                        if (DVTA.estado_doc_vta == "EDITAR")
                        {
                            if (idpf_comp == DVTA.id && nroidpf_comp == Convert.ToInt32(DVTA.numeroid))
                            {
                                // No hace nada porque es la misma a la que se está complementando
                            }
                            else
                            {
                                cadena += Environment.NewLine + "La proforma: " + DVTA.idpf_complemento + "-" + DVTA.nroidpf_complemento + " ya está enlazada con otro proforma: " + idpf_comp + "-" + nroidpf_comp + ", por tanto no se puede enlazar!!!";
                                resultado = false;
                            }
                        }
                        else
                        {
                            cadena += Environment.NewLine + "La proforma: " + DVTA.idpf_complemento + "-" + DVTA.nroidpf_complemento + " ya está enlazada con otro proforma: " + idpf_comp + "-" + nroidpf_comp + ", por tanto no se puede enlazar!!!";
                            resultado = false;
                        }
                    }
                }


            if (resultado == false)
            {
                objres.resultado = false;
                objres.observacion = "Se encontro observaciones en el enlace de esta proforma con Precio Dimediados con la Proforma Mayorista!!!";
                objres.obsdetalle = cadena;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            else
            {
                objres.resultado = true;
                objres.observacion = "";
                objres.obsdetalle = "";
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }


        public async Task<ResultadoValidacion> Validar_Complementar_Proforma_Para_Descto_Extra(DBContext _context, List<itemDataMatriz> tabladetalle, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena = "";
            string resultado_cadena = "";
            int codproforma_complemento = 0;
            bool hay_enlace = false;
            bool resultado = true;
            InicializarResultado(objres);

            //verificar su hay enlacen con proforma
            try
            {
                if (DVTA.idpf_complemento.Trim().Length > 0 && DVTA.nroidpf_complemento.Trim().Length > 0 && DVTA.tipo_complemento == "complemento_para_descto_monto_min_desextra")
                {
                    if (Convert.ToInt32(DVTA.nroidpf_complemento) > 0)
                    {
                        if (await ventas.Existe_Proforma(_context, DVTA.idpf_complemento, Convert.ToInt32(DVTA.nroidpf_complemento)) == false)
                        {
                            objres.resultado = false;
                            objres.observacion = "";
                            objres.obsdetalle = "No existe la proforma: " + DVTA.idpf_complemento + "-" + DVTA.nroidpf_complemento;
                            objres.datoA = "";
                            objres.datoB = "";
                            objres.accion = Acciones_Validar.Ninguna;
                            return objres;
                        }
                        else
                        {
                            codproforma_complemento = await ventas.codproforma(_context, DVTA.idpf_complemento, Convert.ToInt32(DVTA.nroidpf_complemento));
                            hay_enlace = true;
                        }
                    }
                    else
                    {
                        codproforma_complemento = 0;
                        hay_enlace = false;
                    }
                }
                else
                {
                    codproforma_complemento = 0;
                    hay_enlace = false;
                }
            }
            catch
            {
                hay_enlace = false;
                codproforma_complemento = 0;
            }
            //si el codproforma es 0 quiere decir que no hay enlace entonces este control es valido
            if (codproforma_complemento == 0)
            {
                objres.resultado = true;
                objres.observacion = "";
                objres.obsdetalle = "";
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
                return objres;
            }
            //verificar si la proforma esta aprobada
            if (await ventas.proforma_aprobada(_context, codproforma_complemento) == false)
            {
                cadena += Environment.NewLine + "La proforma: " + DVTA.idpf_complemento + "-" + DVTA.nroidpf_complemento + " con la que intenta complementar no esta aprobada, se requiere una proforma aprobada para complementar proforma.";
                resultado = false;
            }

            //verificar si la proforma esta transferida, deberia estar transferida
            if (await ventas.proforma_transferida(_context, codproforma_complemento) == false)
            {
                cadena += Environment.NewLine + "La proforma: " + DVTA.idpf_complemento + "-" + DVTA.nroidpf_complemento + " con la que intenta complementar no esta Transferida-Facturada, se requiere una proforma facturada.";
                resultado = false;
            }

            //verificar si la proforma con la cual quiere enlazar es del mismo cliente
            if (await ventas.Cliente_De_Proforma(_context, codproforma_complemento) != DVTA.codcliente.Trim())
            {
                //cadena += Environment.NewLine + "La proforma: " + DVTA.idpf_complemento + "-" + DVTA.nroidpf_complemento + " no es del mismo cliente respecto de la proforma actual!!!";
                // si las proformas no son del mismo cliente verificar si son sucursales entre si, entre los clientes de ambas proformas asy mismo entre clientes referencia
                //obtener el grupo de clientes del codcliente al cual se realiza la proforma
                List<string> lista_clientes_mismo_nit = new List<string>();
                string codcliente_proforma_complemento = await ventas.Cliente_De_Proforma(_context, codproforma_complemento);
                lista_clientes_mismo_nit = await cliente.CodigosIgualesMismoNIT_List(_context, DVTA.codcliente);
                //verificar los codclientes de ambas proformas
                if (!lista_clientes_mismo_nit.Contains(codcliente_proforma_complemento))
                {
                    cadena += Environment.NewLine + "La proforma con la cual quiere complementar: " + DVTA.idpf_complemento + "-" + DVTA.nroidpf_complemento + " no es del mismo cliente o sucursal(mismo NIT) respecto del cliente de la proforma actual!!!";
                    resultado = false;
                }
                //obtener el grupo de clientes del codcliente referencia-real o referencia
                List<string> lista_clientes_mismo_nit_cliente_referencia = new List<string>();
                string codclienteReferencia_proforma_complemento = await ventas.Cliente_Referencia_Proforma_Etiqueta_Segun_CodProforma(_context, codproforma_complemento);
                string codclienteReferencia_proforma_actual = "";
                lista_clientes_mismo_nit_cliente_referencia = await cliente.CodigosIgualesMismoNIT_List(_context, DVTA.codcliente_real);

            }

            //verifica las fechas de las dos proformas
            DateTime fechaPf1 = await ventas.Fecha_Autoriza_de_Proforma(_context, DVTA.idpf_complemento, Convert.ToInt32(DVTA.nroidpf_complemento));
            int dias_dif = 0;
            TimeSpan diferencia = DVTA.fechadoc - fechaPf1;
            dias_dif = diferencia.Days;

            if (fechaPf1.Year == DVTA.fechadoc.Year && fechaPf1.Month == DVTA.fechadoc.Month)
            {
                //si la proforma actual y la complemento son del mismo mes y ano esta bien, sino no se puede complementar
            }
            else
            {
                cadena += Environment.NewLine + "La proforma complemento: " + DVTA.idpf_complemento + "-" + DVTA.nroidpf_complemento + " y la proforma actual no son del mismo Año-Mes, por lo tanto no puede complementar!!";
                resultado = false;
            }
            //verifica si la fecha de la proforma a complementar si su fecha de autorizacion es a partir del inicio de la promocion >= vedesitem_parametros.desde_fecha
            if (fechaPf1 >= await ventas.Descuento_Linea_Fecha_Desde(_context, "A"))
            {
                //si fecha de autorizacion del complemento es mayor a la fecha de inicio de la promocion, entonces esta valida
            }
            else
            {
                cadena += Environment.NewLine + "La proforma complemento: " + DVTA.idpf_complemento + "-" + DVTA.nroidpf_complemento + " no esta habilitada para complementar, debido a que su fecha de aprobacion fue anterior a la fecha de inicio de la promocion!!!";
                resultado = false;
            }
            DateTime mifecha_actual = await funciones.FechaDelServidor(_context);
            DateTime mifecha1 = new DateTime(mifecha_actual.Year, mifecha_actual.Month, 1);
            int dias_mes = DateTime.DaysInMonth(mifecha_actual.Year, mifecha_actual.Month);
            DateTime mifecha2 = new DateTime(mifecha_actual.Year, mifecha_actual.Month, dias_mes);

            var Sql = await _context.veproforma
                        .Where(p =>
                            p.fecha >= mifecha1.Date &&
                            p.fecha <= mifecha2.Date &&
                            p.codcliente == DVTA.codcliente.Trim() &&
                            p.anulada == false &&
                            p.aprobada &&
                            //!string.IsNullOrEmpty(p.idpf_complemento))
                            p.idpf_complemento != "")
                        .Select(p => new
                        {
                            p.codigo,
                            p.id,
                            p.numeroid,
                            p.fecha,
                            p.codcliente,
                            p.nomcliente,
                            p.total,
                            p.anulada,
                            p.aprobada,
                            p.transferida,
                            p.idpf_complemento,
                            p.nroidpf_complemento
                        }).ToListAsync();

            if (Sql.Count() > 0)
            {
                //cadena += Environment.NewLine + "El cliente: " + DVTA.codcliente + " ya realizo el/los sgte(s) complemento(s) en el mes actual, y no puede realizar mas complementos: ";
                //foreach (var lista in Sql)
                //{
                //     cadena += Environment.NewLine + "Proforma: " + lista.id +"-"+ lista.numeroid + " de Fecha: " + lista.fecha + " Complementada con: " + lista.idpf_complemento + "-" + lista.nroidpf_complemento;
                //     resultado = false;

                //}
                //Desde 25/09/2024 se debe modificar el control, por instruccion de JRA en realidad un cliente puede realizar mas de 1 complemento en el mismo mes pero de distintas proformas pero no entre si;
                //'es decir por ejemplo se complemento la proforma PF-1 con la PF-2, y ahora quiere complementar la PF-3 con la PF-4 eso debe permitir el sistema; 
                //'pero no debe permitir complementar entre esas proformas ya complementadas, no debe permitir complementar la PF-5 con la PF-1 o PF-2 o PF-3 o PF-4
                foreach (var lista in Sql)
                {
                    if (lista.id == DVTA.idpf_complemento && lista.numeroid.ToString() == DVTA.nroidpf_complemento)
                    {
                        cadena += Environment.NewLine + "El cliente: " + DVTA.codcliente + " ya realizo el/los sgte(s) complemento(s) en el mes actual, y no puede realizar mas complementos: ";
                        cadena += Environment.NewLine + "Proforma: " + lista.id + "-" + lista.numeroid + " de Fecha: " + lista.fecha + " Complementada con: " + lista.idpf_complemento + "-" + lista.nroidpf_complemento;
                        resultado = false;
                    }
                    else
                    {
                        if (resultado)
                        {
                            if (lista.idpf_complemento == DVTA.idpf_complemento && lista.nroidpf_complemento.ToString() == DVTA.nroidpf_complemento)
                            {
                                cadena += Environment.NewLine + "El cliente: " + DVTA.codcliente + " ya realizo el/los sgte(s) complemento(s) en el mes actual, y no puede realizar mas complementos: ";
                                cadena += Environment.NewLine + "Proforma: " + lista.id + "-" + lista.numeroid + " de Fecha: " + lista.fecha + " Complementada con: " + lista.idpf_complemento + "-" + lista.nroidpf_complemento;
                                resultado = false;
                            }
                        }
                    }
                }

            }
            //VERIFICAR QUE LA PROFORMA ACTUAL Y LA COMPLEMENTO SEAN AL MISMO TIPO DE PRECIO
            //se puede complementar con proformas de distinto precio segun indicado por JRA en fecha 30-08-2022
            //Obtener los precios de la proforma actual
            List<int> lstprecios_pf_actual = new List<int>();
            foreach (var detalle in tabladetalle)
            {
                if (!lstprecios_pf_actual.Contains((int)detalle.codtarifa))
                {
                    lstprecios_pf_actual.Add((int)detalle.codtarifa);
                }
            }
            int CODTARIFA_PROF_ACTUAL = await Tarifa_Monto_Min_Mayor(_context, DVTA, lstprecios_pf_actual);
            //Obtener los precios de la proforma complemento
            List<int> lstprecios_pf_complemento = new List<int>();
            lstprecios_pf_complemento = await ventas.Proforma_Lista_Tipo_Precio(_context, codproforma_complemento);
            int CODTARIFA_PROF_COMPLEMENTO = await Tarifa_Monto_Min_Mayor(_context, DVTA, lstprecios_pf_complemento);

            if (CODTARIFA_PROF_COMPLEMENTO != CODTARIFA_PROF_ACTUAL)
            {
                cadena += Environment.NewLine + "Proforma Actual: " + DVTA.id + "-" + DVTA.numeroid + " es a precio: " + CODTARIFA_PROF_ACTUAL + " La Proforma complemento es a precio: " + CODTARIFA_PROF_COMPLEMENTO.ToString();
                resultado = true;
            }
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ///

            if (resultado == false)
            {
                objres.resultado = false;
                objres.observacion = "Se encontro observaciones en el enlace de esta proforma con Precio Dimediados con la Proforma Mayorista!!!";
                objres.obsdetalle = cadena;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            else
            {
                objres.resultado = true;
                objres.observacion = "";
                objres.obsdetalle = "";
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }


        public async Task<ResultadoValidacion> Validar_Empaques_caja_cerrada_Descuento(DBContext _context, List<itemDataMatriz> tabladetalle, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena = "";
            int i, j;
            ArrayList items_iguales = new ArrayList();
            bool resultado = true;

            InicializarResultado(objres);

            if (tabladetalle.Count > 0)
            {
               var resp = await Validar_Resaltar_Empaques_Caja_Cerrada_DesctoEspecial_detalle(_context, tabladetalle,Convert.ToInt32(DVTA.codalmacen), false, DVTA.codcliente_real);
                cadena = resp.cadena;
                resultado = cadena.Length == 0;
            }
            if (resultado == false)
            {
                objres.resultado = false;
                objres.observacion = "Hay algunos items que no cumplen el empaque minimo o multiplos del descuento que se aplico.";
                objres.obsdetalle = cadena;
                objres.datoA = DVTA.codcliente;
                objres.datoB = DVTA.nitfactura;
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Empaques_Cerrados_Segun_Lista_Precio(DBContext _context, List<itemDataMatriz> tabladetalle, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena = "";
            string cadena_titulos = "";
            string item = "";
            int i, j;
            decimal cantidad;
            decimal cantidad_total;
            ArrayList items_iguales = new ArrayList();
            bool resultado = true;

            InicializarResultado(objres);

            cadena_titulos = "";
            cadena_titulos = " ITEM     DESCRIPCION         MEDIDA             CANTIDAD    " + '\r';
            cadena_titulos += "------------------------------------------------------------------------" + '\r';

            if (await cliente.Controla_empaque_cerrado(_context, DVTA.codcliente_real))
            {
                //Validar empaques Cerrados
                foreach (var detalle in tabladetalle)
                {
                    if (await ventas.Tarifa_EmpaqueCerrado(_context, detalle.codtarifa))
                    {
                        //desde 23-11-2022 implementar que de un item sume la cantidad total de los items repetidos para asi validar si el empaque del item sin descuento cumpla o no el empaque minimo
                        //ya que al realizar la division de las cantidad de un item para cumplir empaque caja cerrada puede haber items q no cumplan el empaque minimo despues de la division'
                        //para esto el item sin descuento empaque cerrada para validar debe sumar su propia cantidad mas la cantidad de caja cerrada y validar el empaque minimo con esa cantidad
                        if (detalle.coddescuento == 0)
                        {
                            //'Dsde 08-12-2022
                            //'1ro verificar si el item que no tiene descuento sin sumar cumple o no el empaquecerrado segun precio
                            if (!await ventas.CumpleEmpaqueCerrado(_context, detalle.coditem, detalle.codtarifa, detalle.coddescuento, (decimal)detalle.cantidad, DVTA.codcliente_real))
                            {
                                //2do si no tiene descuento sumar las cantidad de todo el detalle con el mismo item y validar el empaque con ese total
                                cantidad_total = 0;
                                item = detalle.coditem;
                                //sacar items iguales
                                foreach (var detalle2 in tabladetalle)
                                {
                                    if (item == detalle2.coditem)
                                    {
                                        //si es igual sumar la cantidad
                                        cantidad_total = cantidad_total + (decimal)detalle2.cantidad;
                                    }
                                    else
                                    {
                                        cantidad_total = cantidad_total;
                                    }
                                }
                                if (!await ventas.CumpleEmpaqueCerrado(_context, detalle.coditem, detalle.codtarifa, detalle.coddescuento, cantidad_total, DVTA.codcliente_real))
                                {
                                    resultado = false;
                                    cadena += "\r\n" + detalle.coditem + " " + funciones.Rellenar(detalle.descripcion, 20, " ", false) + " " + funciones.Rellenar(detalle.medida, 14, " ", false) + "  " + funciones.Rellenar(detalle.cantidad.ToString("####,##0.000", new CultureInfo("en-US")), 12, " ");
                                }
                            }
                        }
                        else
                        {
                            if (!await ventas.CumpleEmpaqueCerrado(_context, detalle.coditem, detalle.codtarifa, detalle.coddescuento, (decimal)detalle.cantidad, DVTA.codcliente_real))
                            {
                                resultado = false;
                                cadena += "\r\n" + detalle.coditem + " " + funciones.Rellenar(detalle.descripcion, 20, " ", false) + " " + funciones.Rellenar(detalle.medida, 14, " ", false) + "  " + funciones.Rellenar(detalle.cantidad.ToString("####,##0.000", new CultureInfo("en-US")), 12, " ");
                            }
                        }
                    }
                }
                cadena += "\r\n" + "------------------------------------------------------------------------" + "\r\n";
            }

            if (resultado == false)
            {
                objres.resultado = false;
                objres.observacion = "Los siguientes items no cumplen el empaque cerrado que el precio de venta elegido requiere:";
                objres.obsdetalle = cadena_titulos;
                objres.obsdetalle += "\r\n" + cadena;
                objres.datoA = DVTA.nitfactura;
                objres.datoB = "Total: " + DVTA.totaldoc.ToString("####,##0.000", new CultureInfo("en-US")) + " (" + DVTA.codmoneda + ")";
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
       
        public async Task<ResultadoValidacion> Validar_Cliente_Tiene_Cuentas_Por_Pagar_En_Mora(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            string cadena = "";
            string cadena_mora = "";
            ResultadoValidacion objres = new ResultadoValidacion();

            InicializarResultado(objres);

            int dias_limite_mora = await configuracion.dias_mora_limite(_context, codempresa);
            //valida solo si es para venta al credito
            if (DVTA.tipo_vta == "CREDITO")
            {
                if (await creditos.ClienteEnMora(_context, DVTA.codcliente_real, codempresa))
                {
                    cadena = "El cliente:" + DVTA.codcliente_real + " tiene cuentas en mora, aun tomando en cuenta sus extensiones, por favor corrija esta situacion antes de grabar para aprobar la proforma(verifique el reporte morosidad por cuotas con extension). El maximo de dias con mora permitido es de: (" + dias_limite_mora + ") dia(s)";
                    cadena_mora = await creditos.Cadena_Notas_De_Remision_En_Mora(_context, DVTA.codcliente_real, codempresa);
                }
            }
            else
            {
                if (await cliente.EsClienteSinNombre(_context, DVTA.codcliente) == false)
                {
                    if (await creditos.ClienteEnMora(_context, DVTA.codcliente_real, codempresa))
                    {
                        cadena = "El cliente:" + DVTA.codcliente_real + " tiene cuentas en mora, aun tomando en cuenta sus extensiones, por favor corrija esta situacion antes de grabar para aprobar la proforma(verifique el reporte morosidad por cuotas con extension). El maximo de dias con mora permitido es de: (" + dias_limite_mora + ") dia(s)";
                        cadena_mora = await creditos.Cadena_Notas_De_Remision_En_Mora(_context, DVTA.codcliente_real, codempresa);
                    }
                }
            }

            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = cadena;
                objres.obsdetalle = cadena_mora;
                objres.datoA = DVTA.estado_doc_vta == "NUEVO" ? DVTA.nitfactura : DVTA.id + "-" + DVTA.numeroid + " " + DVTA.codcliente_real;
                objres.datoB = "Total: " + DVTA.totaldoc.ToString("####,##0.000", new CultureInfo("en-US")) + " (" + DVTA.codmoneda + ")";
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }


        public async Task<ResultadoValidacion> Validar_Georeferencia_Entrega_Valida(DBContext _context, string latitud, string longitud, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena = "";
            InicializarResultado(objres);

            if (latitud.Trim().Length == 0 || longitud.Trim().Length == 0)
            {
                cadena += "\n" + (latitud.Trim().Length == 0 ? "La Latitud" : "") + (longitud.Trim().Length == 0 ? "La Longitud" : "") + " de la dirección de entrega es incorrecta!!!";
            }
            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = "Las georeferencias tienen observaciones:";
                objres.obsdetalle = cadena;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Verificar_Si_Hay_Descuentos_Extras_Repetidos(DBContext _context, List<vedesextraDatos> tabladescuentos, DatosDocVta DVTA, string codempresa)
        {
            List<string> listado = new List<string>();
            int coddesextra_depositos = await configuracion.emp_coddesextra_x_deposito_context(_context, codempresa);
            string cadena_repetidos = "";

            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);

            //el descuento por depositos si puede estar repetido
            //por eso se excluye y no se controla
            foreach (var descuentos in tabladescuentos)
            {
                if (descuentos.coddesextra != coddesextra_depositos)
                {
                    if (listado.Contains(descuentos.coddesextra.ToString()))
                    {
                        cadena_repetidos += "\n" + descuentos.coddesextra + "-" + descuentos.descripcion;
                    }
                    else { listado.Add(descuentos.coddesextra.ToString()); }
                }
            }
            if (cadena_repetidos.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = "Los siguientes descuentos extras estan repetidos, lo cual no esta permitido: ";
                objres.obsdetalle = cadena_repetidos;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Direccion_Entrega(DBContext _context, string codcliente_real, string direccion, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            InicializarResultado(objres);

            if (await cliente.NroDeTiendas(_context, codcliente_real) > 1)
            {
                objres.resultado = false;
                objres.observacion = "Este cliente tiene mas de una tienda. Por favor verifique que ha indicado la direccion correcta, Desea continuar?";
                objres.obsdetalle = "La direccion que eligio es: " + direccion.Trim();
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Confirmar_SiNo;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Venta_Contado_Contra_Entrega_Con_Anticipo(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            InicializarResultado(objres);

            if (DVTA.tipo_vta == "CONTADO" && DVTA.contra_entrega == "SI" && DVTA.pago_con_anticipo == true)
            {

                objres.resultado = false;
                objres.observacion = "Si esta realizando una venta al CONTADO - CONTRA ENTREGA; no debe asignar anticipos, pues existe una contradiccion; la ventas sera contado contra entrega o sera contado con anticipo???";
                objres.obsdetalle = "";
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Venta_Contado_Contra_Entrega_Cliente_Del_Interior(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            InicializarResultado(objres);

            if (DVTA.tipo_vta == "CONTADO")
            {
                if (DVTA.contra_entrega.ToUpper() == "SI")
                {
                    // si es un cliente del interior, no se le puede vender contado contra entrega
                    // implementado desde 29-10-2019
                    if (DVTA.ubicacion == "INTERIOR")
                    {
                        if (string.IsNullOrWhiteSpace(DVTA.estado_contra_entrega))
                        {
                            objres.resultado = false;
                            objres.observacion = "Si está realizando una venta al CONTADO-CONTRA ENTREGA, debe definir el estado de contra entrega!!!";
                            objres.obsdetalle = "";
                            objres.datoA = "";
                            objres.datoB = "";
                            objres.accion = Acciones_Validar.Ninguna;
                        }
                        else if (DVTA.estado_contra_entrega == "POR CANCELAR")
                        {
                            objres.resultado = false;
                            objres.observacion = "Las ventas al CONTADO - CONTRA ENTREGA - POR CANCELAR; a clientes con dirección de entrega al INTERIOR; no están permitidas. Verifique esta situación o ingrese un permiso especial.";
                            objres.obsdetalle = "";
                            objres.datoA = DVTA.estado_doc_vta == "NUEVO" ? DVTA.nitfactura : DVTA.id + "-" + DVTA.numeroid + "-" + DVTA.codcliente;
                            objres.datoB = "Total: " + DVTA.totaldoc + "(" + DVTA.codmoneda + ")";
                            objres.accion = Acciones_Validar.Pedir_Servicio;
                        }
                        else
                        {
                            objres.resultado = false;
                            objres.observacion = "¿Está seguro de realizar esta venta al CONTADO - CONTRA ENTREGA - YA CANCELÓ a un cliente del interior?";
                            objres.obsdetalle = "";
                            objres.datoA = "";
                            objres.datoB = "";
                            objres.accion = Acciones_Validar.Confirmar_SiNo;
                        }
                    }
                    else if (DVTA.ubicacion == "LOCAL")
                    {
                        objres.resultado = false;
                        objres.observacion = "¿Está seguro de realizar esta venta al CONTADO - CONTRA ENTREGA?";
                        objres.obsdetalle = "";
                        objres.datoA = "";
                        objres.datoB = "";
                        objres.accion = Acciones_Validar.Confirmar_SiNo;
                    }
                    else
                    {
                        objres.resultado = false;
                        objres.observacion = "La ubicación de la dirección de entrega del pedido no es válida. Verifique esta situación!!!";
                        objres.obsdetalle = "";
                        objres.datoA = "";
                        objres.datoB = "";
                        objres.accion = Acciones_Validar.Ninguna;
                    }
                }
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Venta_Contado_NOCE_Tenga_Anticipo(DBContext _context, DatosDocVta DVTA, List<vedetalleanticipoProforma> dt_anticipo_pf, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);

            if (DVTA.tipo_vta == "CONTADO")
            {
                if (DVTA.contra_entrega.ToUpper() == "NO")
                {
                    // SI SE HABILITO LA OPCION DE PAGO ANTICIPADO
                    if (DVTA.pago_con_anticipo)
                    {
                        if (dt_anticipo_pf.Count == 0)
                        {
                            objres.resultado = false;
                            objres.observacion = "Toda venta al contado debería tener asignado uno o más anticipos que cubran el monto total de la proforma, y esta proforma no tiene ningún anticipo, por lo tanto no podrá grabar esta proforma!!!";
                            objres.obsdetalle = "";
                            objres.datoA = "";
                            objres.datoB = "";
                            objres.accion = Acciones_Validar.Ninguna;
                        }
                    }
                    else
                    {
                        // alertar si es contado y no se asignó ningún anticipo
                        objres.resultado = false;
                        objres.observacion = "Toda venta al contado debería tener asignado uno o más anticipos que cubran el monto total de la proforma, y esta proforma no tiene ningún anticipo, por lo tanto no podrá grabar esta proforma!!!";
                        objres.obsdetalle = "";
                        objres.datoA = "";
                        objres.datoB = "";
                        objres.accion = Acciones_Validar.Ninguna;
                    }
                }
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Otro_Medio_de_Transporte(DBContext _context, string transporte, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            InicializarResultado(objres);
            //si el medio de transporte elegido es: OTRO, pedir clave
            if (transporte == "OTRO")
            {
                objres.resultado = false;
                objres.observacion = "El medio de transporte que eligio es: OTROS, lo cual requiere permiso especial para aprobar con este medio de transporte!!!";
                objres.obsdetalle = "";
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Quien_Cancelara_Flete(DBContext _context, string fletepor, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            InicializarResultado(objres);

            if (fletepor.Trim().Length == 0)
            {
                objres.resultado = false;
                objres.observacion = "Debe elegir quien cancelara el flete!!!";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Verificar_Nombre_Transporte_Valido_Para_Entregar_Pedido(DBContext _context, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            InicializarResultado(objres);

            if (DVTA.transporte.Trim().Length > 0 && DVTA.nombre_transporte.Trim().Length == 0)
            {
                objres.resultado = false;
                objres.observacion = "Ha elegido un medio de transporte, pero no ha definido el nombre del transporte, desea continuar de todas formas?";
                objres.accion = Acciones_Validar.Confirmar_SiNo;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Verificar_Tipo_Transporte_Valido_Para_Entregar_Pedido(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            InicializarResultado(objres);

            //Si no cumple el monto para:ENTREGAR, entonces no puede elegir estos medios de transporte (FLOTA, TRANSPORTADORA,CAMION PERTEC)
            if ((DVTA.transporte == "FLOTA" || DVTA.transporte == "TRANSPORTADORA" || DVTA.transporte == "CAMION PERTEC") && DVTA.tipoentrega == "ENTREGAR")
            {
                ResultadoValidacion objres1 = new ResultadoValidacion();

                objres1.resultado = true;
                objres1.observacion = "";
                objres1.obsdetalle = "";
                objres1.datoA = "";
                objres1.datoB = "";
                objres1.accion = Acciones_Validar.Ninguna;

                objres1 = await Validar_Monto_Minimo_Para_Entrega_Pedido(_context, DVTA, tabladetalle, codempresa);
                if (objres1.resultado == false)
                {
                    objres.resultado = false;
                    objres.observacion = "Ha elegido el medio de transporte: " + DVTA.transporte + " y este pedido no cumple con el monto minimo para entregar, por tanto el medio de transporte elegido no es correcto!!!";
                    objres.obsdetalle = objres1.obsdetalle;
                }
            }
            return objres;
        }


        public async Task<ResultadoValidacion> Verificar_Descuentos_Extras_Aplicados_Validos(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            bool resultado = true;
            bool _contra_entrega = DVTA.contra_entrega == "SI" ? true : false;
            string _tipo_vta = DVTA.tipo_vta;
            string _tipo_precio = await ventas.Tarifa_tipo(_context, DVTA.codtarifadefecto);
            string es_venta_casual = DVTA.codcliente == DVTA.codcliente_real ? "NO" : "SI";
            int i, j = 0;
            string cadena = "";
            string cadena0 = "";
            string cadena1 = "";
            string cadena2 = "";
            string cadena3 = "";
            string cadena4 = "";
            string cadena5 = "";// para validar si no tiene descuentos asignados
            string cadena6 = "";//para validar si no tiene descuentos especiales o de linea asignados
            string cadena7 = "";//para validar si hay descuentos deshabilitados
            string cadena8 = "";//para validar si hay descuentos excluentes
            string cadena9 = "";//para validar si la proforma segun el tipo de venta le corresponde un descuento que no le asignaron

            InicializarResultado(objres);

            // Verifica si es una venta CONTADO contra entrega, no se asigne los decuentos que son de VENTA CREDITO (1,8,24,25)
            foreach (var descuentos in tabladescuentos)
            {
                if (!await restricciones.Validar_Contraentrega_Descuento(_context, _contra_entrega, descuentos.coddesextra))
                {
                    cadena0 += "\nNo se puede aplicar el descuento:" + descuentos.coddesextra + " -" + descuentos.descripcion + " a una venta Contra Entrega.";
                    resultado = false;
                }
            }

            if (cadena0.Trim().Length > 0)
            {
                cadena0 += "\n----------------------------------------------";
            }

            // Verificar si el cliente tiene asignado el descuento extra (vecliente_desextra)
            foreach (var descuentos in tabladescuentos)
            {
                // Cliente_Tiene_Descto_Extra_Asignado
                if (!await cliente.Cliente_Tiene_Descto_Extra_Asignado(_context, descuentos.coddesextra, DVTA.codcliente_real))
                {
                    if (cadena1.Trim().Length == 0)
                    {
                        cadena1 = "El cliente: " + DVTA.codcliente_real + " no tiene asignado DescExtra: ";
                    }
                    cadena1 += "\n" + descuentos.coddesextra.ToString() + " - " + descuentos.descripcion;
                    resultado = false;
                }
            }

            if (cadena1.Trim().Length > 0)
            {
                cadena1 += "\n----------------------------------------------";
            }

            // Verificar si: DVTA.codcliente es dfte de: DVTA.codcliente_real, si dfte entonces validar del otro cliente
            // implementado en fecha: 30-11-2021
            if (!DVTA.codcliente.Equals(DVTA.codcliente_real))
            {
                foreach (var descuentos in tabladescuentos)
                {
                    // Cliente_Tiene_Descto_Extra_Asignado
                    if (!await cliente.Cliente_Tiene_Descto_Extra_Asignado(_context, descuentos.coddesextra, DVTA.codcliente))
                    {
                        if (cadena1.Trim().Length == 0)
                        {
                            cadena1 = "El cliente Sin Nombre: " + DVTA.codcliente + " no tiene asignado DescExtra: ";
                        }
                        cadena1 += "\n" + descuentos.coddesextra.ToString() + " - " + descuentos.descripcion;
                        resultado = false;
                    }
                }

                if (cadena1.Trim().Length > 0)
                {
                    cadena1 += "\n----------------------------------------------";
                }
            }

            // Verificar el detalle de los items
            List<int> lista_precios = new List<int>();

            foreach (var descuentos in tabladetalle)
            {
                if (!lista_precios.Contains(descuentos.codtarifa))
                {
                    lista_precios.Add(descuentos.codtarifa);
                }
            }

            int CODTARIFA_PARA_VALIDAR = await Tarifa_Monto_Min_Mayor(_context, DVTA, lista_precios);
            List<int> lista_precios_para_validar = new List<int>();

            lista_precios_para_validar.Clear();
            lista_precios_para_validar.Add(CODTARIFA_PARA_VALIDAR);

            foreach (var descuentos in tabladescuentos)
            {
                foreach (int precio in lista_precios_para_validar)
                {
                    // Verificar si el precio - tiene habilitado el DESCTO (vedesextra_tarifa)
                    if (!await ventas.TarifaValidaDescuento(_context, precio, descuentos.coddesextra))
                    {
                        if (cadena2.Trim().Length == 0)
                        {
                            cadena2 = "DesExtra no tiene habilitados precios(vedesextra_tarifa):";
                        }
                        cadena2 += "\nDesExtra: " + descuentos.coddesextra + "  --> Precio: " + precio;
                        resultado = false;
                    }
                }
            }

            if (cadena2.Trim().Length > 0)
            {
                cadena2 += "\n----------------------------------------------";
            }

            // Obtener los descuentos especiales que se están aplicando
            List<int> lista_descesp = new List<int>();

            foreach (var descuentos in tabladetalle)
            {
                if (!lista_descesp.Contains(descuentos.coddescuento))
                {
                    lista_descesp.Add(descuentos.coddescuento);
                }
            }

            // Verificar el detalle de los items
            foreach (var descuentos in tabladescuentos)
            {
                foreach (int descesp in lista_descesp)
                {
                    // Verificar si el DESCTO ESPECIAL - tiene habilitado el DESCTO (vedesextra_descuento)
                    if (!await ventas.DescuentoEspecialValidoDescuento(_context, descesp, descuentos.coddesextra))
                    {
                        if (cadena3.Trim().Length == 0)
                        {
                            cadena3 = "DescExtra no tienen habilitado el DescEspecial(vedesextra_descuento):";
                        }
                        cadena3 += "\nDesExtra: " + descuentos.coddesextra + " --> DesEsp: " + descesp;
                        resultado = false;
                    }
                }
            }

            if (cadena3.Trim().Length > 0)
            {
                cadena3 += "\n----------------------------------------------";
            }

            // Obtener los descuentos especiales que se están aplicando
            List<int> lista_desextra = new List<int>();

            foreach (var descuentos in tabladescuentos)
            {
                if (!lista_desextra.Contains(descuentos.coddesextra))
                {
                    lista_desextra.Add(descuentos.coddesextra);
                }
            }

            foreach (int desextra in lista_desextra)
            {
                foreach (var descuentos in tabladetalle)
                {
                    // Verificar si el item tiene configurado el descuento extra (vedesextra_item)
                    if (!await ventas.DescuentoExtra_ItemValido(_context, descuentos.coditem, desextra))
                    {
                        if (cadena4.Trim().Length == 0)
                        {
                            cadena4 = "Item no tiene habilitado el DesExtra(vedesextra_item):";
                        }
                        cadena4 += "\n- " + descuentos.coditem.ToString() + " DescExtra:" + desextra + "-" + await nombres.nombredesextra(_context, desextra);
                        resultado = false;
                    }
                }
            }

            if (cadena4.Trim().Length > 0)
            {
                cadena4 += "\n----------------------------------------------";
            }
            //Desde 04/11/2024 se añadio todos estos nuevos controles al realizar esta validacion a solicitud de servicio al clieente
            //*******************************************************************************************************************
            //para validar si no tiene descuentos especiales o de linea asignados
            //*******************************************************************************************************************
            foreach (var item in lista_descesp)
            {
                // Verificar si el DESCTO ESPECIAL es cero y alertar
                if (Convert.ToInt32(item) == 0)
                {
                    if (cadena6.Trim().Length == 0)
                    {
                        cadena6 = "DescEspecial(igual a cero):";
                    }
                    cadena6 += Environment.NewLine + "Hay items con DescEspecial --> " + item + ", ¿Desea continuar?";
                    resultado = false;
                }
            }

            if (cadena6.Trim().Length > 0)
            {
                cadena6 += Environment.NewLine + "----------------------------------------------";
            }
            //Desde 04/11/2024 se añadio todos estos nuevos controles al realizar esta validacion a solicitud de servicio al clieente
            //*******************************************************************************************************************
            //para validar si hay descuentos deshabilitados
            //*******************************************************************************************************************
            foreach (var descuentos in tabladescuentos)
            {
                if (!await ventas.Descuento_Extra_Habilitado(_context, descuentos.coddesextra))
                {
                    if (cadena7.Trim().Length == 0)
                    {
                        cadena7 = "El descuento está deshabilitado: ";
                    }
                    cadena7 += Environment.NewLine + Convert.ToString(descuentos.coddesextra) + " - " + descuentos.descripcion;
                    resultado = false;
                }
            }

            if (cadena7.Trim().Length > 0)
            {
                cadena7 += Environment.NewLine + "----------------------------------------------";
            }
            //Desde 04/11/2024 se añadio todos estos nuevos controles al realizar esta validacion a solicitud de servicio al clieente
            //*******************************************************************************************************************
            //para validar si hay descuentos excluentes
            //*******************************************************************************************************************
            string Sql;
            int nro_excluyentes = 0;

            foreach (var reg in tabladescuentos)
            {
                foreach (var reg1 in tabladescuentos)
                {
                    var excluyentes = await _context.vedesextra_excluyentes
                        .Where(ex =>
                            (ex.coddesextra1 == reg.coddesextra && ex.coddesextra2 == reg1.coddesextra) ||
                            (ex.coddesextra1 == reg1.coddesextra && ex.coddesextra2 == reg.coddesextra)
                        )
                        .ToListAsync();

                    if (excluyentes.Any())
                    {
                        nro_excluyentes++;
                        if (cadena8.Trim().Length == 0)
                        {
                            cadena8 = "Descuentos Excluyentes: ";
                        }
                        cadena8 += Environment.NewLine + "El descuento: " + reg.coddesextra + " y el descuento: " + reg1.coddesextra + " no pueden ser aplicados de manera simultánea en una misma proforma.";
                        resultado = false;
                    }
                }
            }

            if (cadena8.Trim().Length > 0)
            {
                cadena8 += Environment.NewLine + "----------------------------------------------";
            }
            //Desde 04/11/2024 se añadio todos estos nuevos controles al realizar esta validacion a solicitud de servicio al clieente
            //*******************************************************************************************************************
            //para validar segun el tipo de venta de la proforma si no le asignaron un descuento que le corresponde
            //*******************************************************************************************************************
            bool existe_descuento = false;

            var dt_condiciones = await _context.vedesextra_condiciones
                .Where(cond => cond.tipo_vta == _tipo_vta && (cond.tipo_precio == _tipo_precio || cond.tipo_precio == "SIN RESTRICCION")
                    && (cond.aplica_para_casual == es_venta_casual || cond.aplica_para_casual == "SIN RESTRICCION"))
                .OrderBy(cond => cond.coddesextra)
                .ToListAsync();
            if (_contra_entrega)
            {
                dt_condiciones = await _context.vedesextra_condiciones
                    .Where(cond => cond.tipo_vta == _tipo_vta &&
                                  (cond.tipo_precio == _tipo_precio || cond.tipo_precio == "SIN RESTRICCION") &&
                                  (cond.aplica_para_casual == es_venta_casual || cond.aplica_para_casual == "SIN RESTRICCION") &&
                                  cond.aplica_para_solo_contado == false)
                    .OrderBy(cond => cond.coddesextra)
                    .ToListAsync();
            }
            else
            {
                dt_condiciones = await _context.vedesextra_condiciones
                    .Where(cond => cond.tipo_vta == _tipo_vta &&
                                  (cond.tipo_precio == _tipo_precio || cond.tipo_precio == "SIN RESTRICCION") &&
                                  (cond.aplica_para_casual == es_venta_casual || cond.aplica_para_casual == "SIN RESTRICCION"))
                    .OrderBy(cond => cond.coddesextra)
                    .ToListAsync();
            }

            if (dt_condiciones.Any())
            {
                foreach (var reg0 in dt_condiciones)
                {
                    string tipoPrecio = reg0.tipo_precio;
                    string aplicaParaCasual = reg0.aplica_para_casual;
                    int codDescuento = reg0.coddesextra ?? 0;

                    // Validación de condiciones
                    bool condicionesCumplidas = true;
                    // Buscar el descuento en tabladescuentos si las condiciones se cumplen
                    if (condicionesCumplidas)
                    {
                        existe_descuento = tabladescuentos.AsEnumerable().Any(reg2 => Convert.ToInt32(reg2.coddesextra) == codDescuento);

                        // Si el descuento no existe, agregar mensaje de advertencia
                        if (!existe_descuento)
                        {
                            if (string.IsNullOrEmpty(cadena9))
                            {
                                cadena9 = "Descuentos Sin Asignar por Tipo de Venta - Precio: ";
                            }
                            cadena9 += Environment.NewLine + $"El descuento: {codDescuento} no está agregado en el detalle de descuentos de la proforma. ¿Desea continuar?";
                            resultado = false;
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(cadena9))
            {
                cadena9 += Environment.NewLine + "----------------------------------------------";
            }

            if (resultado == false)
            {
                cadena = cadena0;
                cadena += "\n\n" + cadena1;
                cadena += "\n\n" + cadena2;
                cadena += "\n\n" + cadena3;
                cadena += "\n\n" + cadena4;
                cadena += "\n\n" + cadena5;
                cadena += "\n\n" + cadena6;
                cadena += "\n\n" + cadena7;
                cadena += "\n\n" + cadena8;
                cadena += "\n\n" + cadena9;
                objres.resultado = false;
                objres.observacion = "Se encontraron las siguientes observaciones en la aplicacion de los descuentos extras:";
                objres.obsdetalle = cadena;
                objres.datoA = "";
                objres.datoB = "";
                if ((cadena5.Trim().Length > 0 || cadena6.Trim().Length > 0 || cadena9.Trim().Length > 0) &&
                     (cadena0.Trim().Length == 0 && cadena1.Trim().Length == 0 && cadena2.Trim().Length == 0 &&
                      cadena3.Trim().Length == 0 && cadena4.Trim().Length == 0 && cadena7.Trim().Length == 0 &&
                      cadena8.Trim().Length == 0))
                {
                    objres.accion = Acciones_Validar.Confirmar_SiNo;
                }
                else
                {
                    objres.accion = Acciones_Validar.Ninguna;
                }
            }
            return objres;
        }

        public async Task<ResultadoValidacion> Validar_NIT_Valido(DBContext _context, DatosDocVta DVTA, string codempresa, string usuario)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string minomcliente = "";

            InicializarResultado(objres);

            minomcliente = DVTA.nombcliente.Trim().Replace(" ", "");
            if (minomcliente == "SINNOMBRE")
            {
                DVTA.nitfactura = "0";
            }
            else
            {
                (bool esValido, string mensaje) = await ventas.Validar_NIT_Correcto(_context, DVTA.nitfactura, DVTA.tipo_doc_id);
                if (!esValido)
                {
                    objres.resultado = false;
                    objres.observacion = "Debe indicar un N.I.T. o Nro de C.I. Valido.";
                    objres.obsdetalle = mensaje;
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                }
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Venta_Credito_NIT_Dfte_Cero(DBContext _context, DatosDocVta DVTA, string codempresa, string usuario)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            InicializarResultado(objres);

            DVTA.nitfactura = DVTA.nitfactura.Trim();
            if (DVTA.tipo_vta == "CREDITO")
            {
                //VENTA CREDITO
                if (funciones.EsNumero(DVTA.nitfactura))
                {
                    if (double.Parse(DVTA.nitfactura) == 0)
                    {
                        objres.resultado = false;
                        objres.observacion = "No se puede realizar una venta al credito con N.I.T. o Nro de C.I. igual a cero.";
                        objres.obsdetalle = "";
                        objres.datoA = "";
                        objres.datoB = "";
                        objres.accion = Acciones_Validar.Ninguna;
                    }
                }
                else
                {
                    objres.resultado = false;
                    objres.observacion = "Para realizar una venta al credito, debe indicar un N.I.T. o Nro de C.I. Valido.";
                    objres.obsdetalle = "";
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                }

            }
            else
            {
                (bool esValido, string mensaje) = await ventas.Validar_NIT_Correcto(_context, DVTA.nitfactura, DVTA.tipo_doc_id);
                if (!esValido)
                {
                    objres.resultado = false;
                    objres.observacion = "Debe indicar un N.I.T. o Nro de C.I. Valido.";
                    objres.obsdetalle = mensaje;
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                }
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Monto_Limite_Factura_Sin_Nombre(DBContext _context, DatosDocVta DVTA, string codempresa, string usuario)
        {
            string minomcliente = "";
            string mon_base = await Empresa.monedabase(_context, codempresa);
            double monto_conversion = (double)await tipocambio._conversion(_context, mon_base, DVTA.codmoneda, DVTA.fechadoc.Date, (decimal)DVTA.totaldoc);
            string codmoneda_monto_max_vta_snomb = await cliente.Maximo_Vta_Moneda(_context, DVTA.codcliente);
            double montomax_facturas_sinnombre = (double)await configuracion.monto_maximo_facturas_sin_nombre(_context, codempresa);
            string cadena_msg = "";
            string cadena = "";
            cadena = " ";
            ResultadoValidacion objres = new ResultadoValidacion();

            InicializarResultado(objres);

            //si la venta es mayor al permitido para facturas sin nombre alertar como debe ser
            //consignado el nombre de la factura y el nit

            if (monto_conversion >= montomax_facturas_sinnombre && DVTA.nitfactura == "0" && DVTA.nombcliente == "SIN NOMBRE")
            {
                cadena_msg = "Como esta venta es mayor o igual a: " + montomax_facturas_sinnombre.ToString("####,##0.000", new CultureInfo("en-US")) + " (" + codmoneda_monto_max_vta_snomb + ") " + Environment.NewLine;
                cadena_msg += " deben llenarse obligatoriamente los datos del comprador: " + Environment.NewLine;
                cadena_msg += " -Para sociedades o entidades, colocar el nombre comercial " + Environment.NewLine;
                cadena_msg += "  registrado en Fundempresa y luego el NIT." + Environment.NewLine;
                cadena_msg += " -Para empresas unipersonales que usan el nombre del propietario, " + Environment.NewLine;
                cadena_msg += "  o personas naturales se debe colocar el nombre completo  " + Environment.NewLine;
                cadena_msg += "  o por lo menos el primer apellido. Luego el NIT o CI. " + Environment.NewLine;
                cadena_msg += " -Para personas no obligadas a tener NIT o personas extranjeras:  " + Environment.NewLine;
                cadena_msg += "  En el lugar de Señores: colocar la entidad o persona," + Environment.NewLine;
                cadena_msg += "  y en la parte NIT/CI: colocar el numero: 99001 " + Environment.NewLine;
            }

            if (monto_conversion >= montomax_facturas_sinnombre)
            {
                if (await cliente.EsClienteSinNombre(_context, DVTA.codcliente))
                {
                    if (!funciones.EsNumero(DVTA.nitfactura))
                    {
                        cadena += Environment.NewLine + " Como esta venta es mayor a " + montomax_facturas_sinnombre.ToString("####,##0.000", new CultureInfo("en-US")) + " (MN) debe ser facturada a un NIT válido.";
                    }

                    if (DVTA.nombcliente.Replace(" ", "") == "SINNOMBRE")
                    {
                        cadena += Environment.NewLine + " Como esta venta es mayor a " + montomax_facturas_sinnombre.ToString("####,##0.000", new CultureInfo("en-US")) + " (MN) debe ser facturada a un nombre válido.";
                    }

                    if (DVTA.nitfactura == "0")
                    {
                        cadena += Environment.NewLine + " Como esta venta es mayor a " + montomax_facturas_sinnombre.ToString("####,##0.000", new CultureInfo("en-US")) + " (MN) debe ser facturada a un NIT válido.";
                    }
                }
                else
                {
                    if (DVTA.nombcliente.Replace(" ", "") == "SINNOMBRE")
                    {
                        cadena += Environment.NewLine + " Como esta venta es mayor a " + montomax_facturas_sinnombre.ToString("####,##0.000", new CultureInfo("en-US")) + " (MN) debe ser facturada a un nombre válido.";
                    }

                    if (DVTA.nitfactura.Trim() == "0")
                    {
                        cadena += Environment.NewLine + " Como esta venta es mayor a " + montomax_facturas_sinnombre.ToString("####,##0.000", new CultureInfo("en-US")) + " (MN) debe ser facturada a un NIT válido.";
                    }
                }
            }
            if (cadena_msg.Trim().Length > 0 || cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = cadena_msg;
                objres.obsdetalle = cadena;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }


        public async Task<ResultadoValidacion> Validar_NIT_Es_Cliente_CompetenciaAsync(DBContext _context, DatosDocVta DVTA)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            //var es_cliente_competencia = await Cliente.EsClienteCompetencia(userConnectionString, DVTA.nitfactura);
            if (await cliente.EsClienteCompetencia(_context, DVTA.nitfactura))
            {
                objres.resultado = false;
                objres.observacion = ("El NIT: " + (DVTA.nitfactura + " corresponde a un NIT clasificado como cliente competencia, necesita permiso especial para dar continuar!!!"));
                objres.obsdetalle = "";
                if (DVTA.estado_doc_vta.ToUpper() == "NUEVO")
                {
                    objres.datoA = DVTA.codcliente + "-" + DVTA.codcliente_real + "-" + DVTA.nitfactura;
                }
                else
                {
                    objres.datoB = DVTA.id + "-" + DVTA.numeroid + " " + DVTA.codcliente_real + "-" + DVTA.nitfactura;
                }

                objres.accion = Acciones_Validar.Pedir_Servicio;
            }

            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Tipo_ClienteAsync(DBContext _context, string codcliente)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string _tipo = "";
            bool _esgrupo = false;
            string _cadena = "";
            string _obs = "";

            InicializarResultado(objres);
            List<string> liguales = new List<string>();
            liguales = await cliente.CodigosIgualesListAsync(_context, codcliente);

            if ((liguales.Count > 1))
            {
                _esgrupo = true;
                _obs = "El cliente pertenece a un grupo de clientes con observacion:";
            }
            else
            {
                _esgrupo = false;
                _obs = "El cliente tiene observacion:";
            }
            for (int i = 0; (i <= (liguales.Count - 1)); i++)
            {
                _tipo = await cliente.Tipo_Cliente(_context, liguales[i], false);
                if ((_tipo == "NORMAL"))
                { }
                else
                {
                    _cadena += "\r\n" + "El cliente: " + liguales[i] + " es tipo: " + _tipo;
                }

            }
            if (_cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = _obs;
                objres.obsdetalle = _cadena;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Confirmar_SiNo;
            }

            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Cliente_Competencia_Permite_Desctos_De_LineaAsync(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            InicializarResultado(objres);

            if (await cliente.EsClienteCompetencia(_context, DVTA.nitfactura))
            {
                bool Cliente_Competencia_Permite_Descto_Linea = await cliente.ClienteCompetenciaPermiteDesctoLinea(_context, DVTA.nitfactura);
                if (ProformaTieneDescuentoLinea(tabladetalle) && !Cliente_Competencia_Permite_Descto_Linea)
                {
                    objres.resultado = false;
                    objres.observacion = "El NIT: " + DVTA.nitfactura + " corresponde a un NIT clasificado como cliente competencia!!!";
                    objres.obsdetalle = "El cliente-nit pertenece al grupo de clientes competencia que no se puede otorgar descuentos de linea.";
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                }
            }

            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Cliente_Competencia_Permite_Desctos_ProveedorAsync(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            InicializarResultado(objres);

            if (await cliente.EsClienteCompetencia(_context, DVTA.nitfactura))
            {
                bool Cliente_Competencia_Permite_Descto_Proveedor = await cliente.ClienteCompetenciaPermiteDesctoProveedor(_context, DVTA.nitfactura);
                if (ProformaTieneDescuentoEspeciales(tabladetalle) && !Cliente_Competencia_Permite_Descto_Proveedor)
                {
                    objres.resultado = false;
                    objres.observacion = "El NIT: " + DVTA.nitfactura + " corresponde a un NIT clasificado como cliente competencia!!!";
                    objres.obsdetalle = "El cliente-nit pertenece al grupo de clientes competencia que no se puede otorgar descuentos especiales Proveedor.";
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                }
            }

            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Cliente_Competencia_Permite_Desctos_VolumnenAsync(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            InicializarResultado(objres);

            if (await cliente.EsClienteCompetencia(_context, DVTA.nitfactura))
            {
                bool Cliente_Competencia_Permite_Descto_Volumen = await cliente.ClienteCompetenciaPermiteDesctoVolumen(_context, DVTA.nitfactura);
                if (ProformaTieneDescuentoEspeciales(tabladetalle) && !Cliente_Competencia_Permite_Descto_Volumen)
                {
                    objres.resultado = false;
                    objres.observacion = "El NIT: " + DVTA.nitfactura + " corresponde a un NIT clasificado como cliente competencia!!!";
                    objres.obsdetalle = "El cliente-nit pertenece al grupo de clientes competencia que no se puede otorgar descuentos especiales Volumen.";
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                }
            }

            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Cliente_Competencia_Permite_Desctos_PromocionAsync(DBContext _context, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            InicializarResultado(objres);

            if (await cliente.EsClienteCompetencia(_context, DVTA.nitfactura))
            {
                List<string> lista_promos_aplicadas = new List<string>();
                int coddesextra_depo = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);
                lista_promos_aplicadas = await ventas.DescuentosPromocionAplicadosAsync(_context, tabladescuentos, coddesextra_depo);
                if (lista_promos_aplicadas.Count > 0)
                {
                    bool Cliente_Competencia_Permite_Descto_Promocion = await cliente.ClienteCompetenciaPermiteDesctoPromocion(_context, DVTA.nitfactura);
                    if (!Cliente_Competencia_Permite_Descto_Promocion)
                    {
                        objres.resultado = false;
                        objres.observacion = "El NIT: " + DVTA.nitfactura + " corresponde a un NIT clasificado como cliente competencia!!!";
                        objres.obsdetalle = "El cliente-nit pertenece al grupo de clientes competencia que no se puede otorgar descuentos de Promocion.";
                        objres.datoA = "";
                        objres.datoB = "";
                        objres.accion = Acciones_Validar.Ninguna;
                    }
                }
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Cliente_Competencia_Permite_Desctos_ExtraAsync(DBContext _context, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            InicializarResultado(objres);

            if (await cliente.EsClienteCompetencia(_context, DVTA.nitfactura))
            {
                if (await Proforma_Tiene_Descuentos_ExtrasAsync(_context, tabladescuentos))
                {
                    bool Cliente_Competencia_Permite_Descto_Extra = await cliente.ClienteCompetenciaPermiteDesctoExtra(_context, DVTA.nitfactura);
                    if (!Cliente_Competencia_Permite_Descto_Extra)
                    {
                        objres.resultado = false;
                        objres.observacion = "El NIT: " + DVTA.nitfactura + " corresponde a un NIT clasificado como cliente competencia!!!";
                        objres.obsdetalle = "El cliente-nit pertenece al grupo de clientes competencia que no se puede otorgar descuentos extras adicionales.";
                        objres.datoA = "";
                        objres.datoB = "";
                        objres.accion = Acciones_Validar.Ninguna;
                    }
                }
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Verificar_Descuentos_Extras_Habilitados(DBContext _context, List<vedesextraDatos> tabladescuentos)
        {
            string cadena = "";
            ResultadoValidacion objres = new ResultadoValidacion();

            InicializarResultado(objres);

            foreach (var dato in tabladescuentos)
            {
                if (await ventas.Descuento_Extra_Habilitado(_context, dato.coddesextra) == false)
                {
                    if (cadena.Trim().Length == 0)
                    {
                        cadena = dato.coddesextra + " - " + await nombres.nombredesextra(_context, dato.coddesextra);
                    }
                    else
                    {
                        cadena += Environment.NewLine + " - " + dato.coddesextra + await nombres.nombredesextra(_context, dato.coddesextra);
                    }

                }
            }
            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = "Los siguientes descuentos extras estan actualmente deshabilitados:";
                objres.obsdetalle = cadena;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Descuentos_De_Nivel_Segun_SolicitudAsync(DBContext _context, DatosDocVta DVTA)
        {
            ResultadoValidacion objres = new ResultadoValidacion();

            InicializarResultado(objres);

            //PERMITIR PROFORMA CON DESCTOS DE NIVEL SEGUN SOLICITUD
            if (DVTA.desclinea_segun_solicitud == true)
            {
                if (DVTA.idsol_nivel.Trim().Length == 0 || DVTA.nroidsol_nivel.Trim().Length == 0)
                {
                    objres.observacion = "Ha elegido otorgar descuentos de nivel segun solicitud, debe identificar correctamente el ID-Numeroid de la solicitud!!!";
                    objres.resultado = false;
                    objres.accion = Acciones_Validar.Ninguna;
                }
                //verificar si la solicitud de descuentos de linea existe
                if (objres.resultado == true)
                {
                    bool existe_sol_Descuento_Nivel = await ventas.Existe_Solicitud_Descuento_Nivel(_context, DVTA.idsol_nivel, int.Parse(DVTA.nroidsol_nivel));
                    if (!existe_sol_Descuento_Nivel)
                    {
                        objres.observacion = "Ha elegido utilizar la solicitud de descuentos de nivel: " + DVTA.idsol_nivel.Trim() + "-" + DVTA.nroidsol_nivel.Trim() + " para aplicar descuentos de linea, pero la solicitud indicada no existe!!!";
                        objres.resultado = false;
                        objres.accion = Acciones_Validar.Ninguna;
                    }
                }
                //verificar que el codigo de cliente real de la proforma sea el mismo al de la solicitud
                if (objres.resultado == true)
                {
                    string cliente_Sol_Descuento_Nivel = await ventas.Cliente_Solicitud_Descuento_Nivel(_context, DVTA.idsol_nivel, int.Parse(DVTA.nroidsol_nivel));
                    if (!(cliente_Sol_Descuento_Nivel == DVTA.codcliente))
                    {
                        objres.observacion = "La solicitud de descuentos de nivel: " + DVTA.idsol_nivel + "-" + DVTA.nroidsol_nivel + " a la que hace referencia no pertenece al mismo cliente de esta proforma!!!";
                        objres.obsdetalle = "";
                        objres.datoA = "";
                        objres.datoB = "";
                        objres.resultado = false;
                        objres.accion = Acciones_Validar.Ninguna;
                    }
                }
                //validar si la solicitud de descuentos ya fue utilizada en otra proforma
                if (objres.resultado == true)
                {
                    bool Sol_ya_Utilizada = await ventas.Solicitud_Descuento_Ya_Utilizada(_context, DVTA.idsol_nivel, DVTA.nroidsol_nivel);
                    if (Sol_ya_Utilizada)
                    {
                        //obtiene el id-nroid de la proforma en la cual se utilizo ya la solicitud
                        string doc_pf = await ventas.ProformaConSolicitudDescuentoYaUtilizada(_context, DVTA.idsol_nivel, DVTA.nroidsol_nivel);
                        //SI ES PROFORMA NUEVA
                        if (DVTA.estado_doc_vta.ToUpper() == "NUEVO")
                        {
                            objres.observacion = "La solicitud de descuentos: " + DVTA.idsol_nivel + "-" + DVTA.nroidsol_nivel + " ya fue utilizada en la proforma:" + doc_pf + " por tanto no puede utilizar en esta proforma!!!";
                            objres.obsdetalle = "";
                            objres.datoA = "";
                            objres.datoB = "";
                            objres.resultado = false;
                            objres.accion = Acciones_Validar.Ninguna;
                        }
                        else
                        {
                            //EDITAR
                            //si se esta modificando la proforma y es la misma en la cual se aplico la sol de descuentos de nivel
                            //entonces esta bien
                            if (doc_pf == DVTA.id.Trim() + "-" + DVTA.numeroid.Trim())
                            {
                                objres.observacion = "";
                                objres.obsdetalle = "";
                                objres.datoA = "";
                                objres.datoB = "";
                                objres.resultado = true;
                                objres.accion = Acciones_Validar.Ninguna;
                            }
                            else
                            {
                                objres.observacion = "La solicitud de descuentos: " + DVTA.idsol_nivel + "-" + DVTA.nroidsol_nivel + " ya fue utilizada en la proforma: " + doc_pf + " por tanto no puede utilizar en esta proforma!!!";
                                objres.obsdetalle = "";
                                objres.datoA = "";
                                objres.datoB = "";
                                objres.resultado = false;
                                objres.accion = Acciones_Validar.Ninguna;
                            }

                        }

                    }
                }
                //pedir la clave para dar curso a proforma con descuentos de nivel segun solicitud
                if (objres.resultado == true)
                {
                    objres.observacion = "Ha elegido aplicar descuentos de linea segun solicitud: " + DVTA.idsol_nivel + "-" + DVTA.nroidsol_nivel + " lo cual requiere permiso especial, ingrese el permiso especial!!!";
                    objres.obsdetalle = "";
                    objres.resultado = false;
                    if (DVTA.estado_doc_vta.ToUpper() == "NUEVO")
                    {
                        objres.datoA = DVTA.codcliente + "-" + DVTA.codcliente_real + " " + DVTA.idsol_nivel + "-" + DVTA.nroidsol_nivel;
                    }
                    else
                    {
                        objres.datoA = DVTA.id + "-" + DVTA.numeroid + " " + DVTA.idsol_nivel + "-" + DVTA.nroidsol_nivel;
                    }

                    objres.datoB = "Total: " + DVTA.totaldoc.ToString("####,##0.000", new CultureInfo("en-US")) + " (" + DVTA.codmoneda + ")";
                    objres.accion = Acciones_Validar.Pedir_Servicio;
                }


            }
            return objres;
        }
        public bool ProformaTieneDescuentoLinea(List<itemDataMatriz> tabladetalle)
        {
            if (!tabladetalle.Any(item => item.porcendesc != 0.0f))
            {
                //EN EL CASO DE LAS TIENDAS NO SE TIENE ESTA COLUMNA EN LA FACTURACION DE MOSTRADOR
                //ENTONCES SI NO TIENE ESTA COLUMNA DE HECHO NO HAY DESCTO DE LINEA
                return false;
            }
            else
            {
                //if (tabladetalle.Any(item => item.porcen > 0))
                //{
                //    return true;
                //}
                foreach (var item in tabladetalle)
                {
                    if (item.porcendesc > 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public bool ProformaTieneDescuentoEspeciales(List<itemDataMatriz> tabladetalle)
        {

            foreach (var item in tabladetalle)
            {
                if (item.coddescuento != 0)
                {
                    return true;
                }
            }
            return false;
        }
        public async Task<bool> Proforma_Tiene_Descuentos_ExtrasAsync(DBContext _context, List<vedesextraDatos> tabladescuentos)
        {

            foreach (var item in tabladescuentos)
            {
                //si no es uno de promocion es un descuento extra
                if (!await ventas.DescuentoExtra_Diferenciado_x_item(_context, item.coddesextra))
                {
                    return true;
                }
            }
            return false;
        }
        public async Task<ResultadoValidacion> Validar_Vigencia_DesctosExtras_Segun_FechasAsync(DBContext _context, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena = "";
            DateTime valido_desdeF;
            DateTime valido_hastaF;

            InicializarResultado(objres);

            foreach (var descuentos in tabladescuentos)
            {
                //obtener desde que fecha esta habilitado el descto extra
                valido_desdeF = await ventas.Descuento_Extra_Valido_Desde_Fecha(_context, descuentos.coddesextra);
                //verificar si la fecha de la proforma es posterior a la habilitacion del descto
                if (DVTA.fechadoc.Date < valido_desdeF.Date)
                {
                    cadena += Environment.NewLine + "El Desc: " + descuentos.coddesextra + "-" + descuentos.descripcion + " es válido desde fecha: " + valido_desdeF.ToShortDateString();
                }

                valido_hastaF = await ventas.Descuento_Extra_Valido_Hasta_Fecha(_context, descuentos.coddesextra);
                //verificar si la fecha de la proforma es posterior a la habilitacion del descto
                if (DVTA.fechadoc.Date > valido_hastaF.Date)
                {
                    cadena += Environment.NewLine + "El Desc: " + descuentos.coddesextra + "-" + descuentos.descripcion + " es válido hasta fecha: " + valido_hastaF.ToShortDateString();
                }
            }
            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = "Los siguientes descuentos extras aplicados tienen observaciones en sus fechas de vigencia:";
                objres.obsdetalle = cadena;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Promociones_Por_Aplicar(DBContext _context, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, List<itemDataMatriz> tabladetalle, bool notificar_sinohay, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena_aplicados = "";
            string cadena_aplicable = "";
            string por_aplicar = "";
            string niveles_por_aplicar = "";
            DataTable dt_promo = new DataTable();

            InicializarResultado(objres);

            if (DVTA.codtarifadefecto.ToString().Trim().Length == 0)
            {
                objres.resultado = false;
                objres.observacion = "Debe indicar el precio para el cual se verificara que descuentos-promociones se pueden aplicar.";
                objres.obsdetalle = "";
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
                return objres;
            }
            //using (_context)
            ////using (var _context = DbContextFactory.Create(userConnectionString))
            //{

            int coddesextra_depositos = await configuracion.emp_coddesextra_x_deposito_context(_context, codempresa);
            List<string> lista_promos_aplicadas = new List<string>();
            lista_promos_aplicadas = await ventas.DescuentosPromocionAplicadosAsync(_context, tabladescuentos, coddesextra_depositos);

            List<string> lista_items = new List<string>();
            lista_items = ListaItemsPedido(tabladetalle);

            List<string> lista_lineas = new List<string>();
            lista_lineas = await ventas.AlinearItemsAsync(_context, lista_items);


            DataTable dt_promo_aux = new DataTable();

            dt_promo.Columns.Add("codgrupo", typeof(string));
            dt_promo.Columns.Add("codlinea", typeof(string));
            dt_promo.Columns.Add("desclinea", typeof(string));
            dt_promo.Columns.Add("nivel", typeof(string));
            dt_promo.Columns.Add("promo_aplicado", typeof(string));
            dt_promo.Columns.Add("promo_aplicable", typeof(string));
            dt_promo.Columns.Add("por_aplicar", typeof(string));
            dt_promo.Columns.Add("nivel_sugerido", typeof(string));
            dt_promo.Columns.Add("nivel_por_aplicar", typeof(string));

            List<string> lista_aplicables_por_linea = new List<string>();

            foreach (string linea in lista_lineas)
            {
                lista_aplicables_por_linea.Clear();
                lista_aplicables_por_linea = await ventas.ListaPromocionesPorLineaAplicablesAsync(_context, linea, DVTA.codtarifadefecto, DVTA.codcliente_real, DVTA.fechadoc, DVTA.subtotaldoc, DVTA.codmoneda, DVTA.tipo_vta);

                cadena_aplicados = "";
                cadena_aplicable = "";
                por_aplicar = "";

                foreach (var promocion in lista_aplicables_por_linea)
                {
                    cadena_aplicable += (cadena_aplicable.Length == 0) ? promocion : ", " + promocion;

                    if (!lista_promos_aplicadas.Contains(promocion))
                    {
                        por_aplicar += (por_aplicar.Length == 0) ? promocion : ", " + promocion;
                    }

                    if (lista_promos_aplicadas.Contains(promocion))
                    {
                        cadena_aplicados += (cadena_aplicados.Length == 0) ? promocion : ", " + promocion;
                    }
                }
                DataRow nuevoreg = dt_promo.NewRow();
                List<string> milista_niveles = new List<string> { "A", "B", "C", "D", "E" };
                string nivel_actual = "";

                nuevoreg["codgrupo"] = await items.lineagrupo(_context, linea);
                nuevoreg["codlinea"] = linea;
                nuevoreg["desclinea"] = await nombres.nombrelinea(_context, linea);
                nuevoreg["nivel"] = await cliente.NivelDescclienteLinea(_context, DVTA.codcliente_real, linea, DVTA.codtarifadefecto);
                nuevoreg["nivel_sugerido"] = await cliente.NiveldescClientesugeridoSegunAuditoria(_context, DVTA.codcliente_real, linea, DVTA.codtarifadefecto);
                nuevoreg["promo_aplicado"] = cadena_aplicados;
                nuevoreg["promo_aplicable"] = cadena_aplicable;
                nuevoreg["por_aplicar"] = por_aplicar;

                if (nuevoreg["nivel"].ToString() == "Z" || nuevoreg["nivel"].ToString() == "X")
                    nivel_actual = "";
                else
                    nivel_actual = nuevoreg["nivel"].ToString();

                if ((nuevoreg["nivel_sugerido"].ToString() != "Z" || nuevoreg["nivel_sugerido"].ToString() != "X") && (nivel_actual.CompareTo(nuevoreg["nivel_sugerido"]) < 0) && milista_niveles.Contains(nuevoreg["nivel_sugerido"].ToString()))
                    nuevoreg["nivel_por_aplicar"] = nuevoreg["nivel_sugerido"];
                else
                    nuevoreg["nivel_por_aplicar"] = "";

                dt_promo.Rows.Add(nuevoreg);
            }
            DataRow reg;

            dt_promo_aux = dt_promo.Copy();
            dt_promo_aux.Clear();
            niveles_por_aplicar = "";
            foreach (DataRow reg_loopVariable in dt_promo.Rows)
            {
                reg = reg_loopVariable;

                // Si se quiere obligar a dar descuento de línea con los niveles sugeridos, esta es la línea de código a habilitar
                // La cual se deshabilita por instrucción de JRA en fecha 19-04-2022
                // If (CStr(reg["por_aplicar"]).Trim().Length > 0) Or CStr(reg["nivel_por_aplicar"]).Trim().Length > 0 Then

                // Solo debe obligar a dar descuentos extras por instrucción de JRA en fecha tb 19-04-2022
                if (Convert.ToString(reg["por_aplicar"]).Trim().Length > 0)
                {
                    dt_promo_aux.ImportRow(reg);
                    //if (niveles_por_aplicar.Trim().Length == 0)
                    //{
                    //    niveles_por_aplicar = Convert.ToString(reg["nivel_por_aplicar"]);
                    //}
                    //else
                    //{
                    //    niveles_por_aplicar = ", " + Convert.ToString(reg["nivel_por_aplicar"]);
                    //}
                }
            }

            dt_promo.Clear();
            dt_promo = dt_promo_aux.Copy();
            dt_promo_aux.Clear();
            //}
            string cadena_msg = "";
            cadena_msg = ventas.MostrarMensajesPromocionesAplicar(dt_promo);
            if (cadena_msg.Trim().Length > 0)
            {
                objres.resultado = false;
                if (DVTA.estado_doc_vta == "NUEVO")
                {
                    objres.datoA = DVTA.codcliente_real;
                    objres.datoB = "DESC/NIV NO APLICADOS: " + por_aplicar + " " + niveles_por_aplicar;
                }
                else
                {
                    objres.datoA = DVTA.id + "-" + DVTA.numeroid + "-" + DVTA.codcliente_real;
                    objres.datoB = "DESC/NIV NO APLICADOS: " + por_aplicar + " " + niveles_por_aplicar;
                }

                objres.accion = Acciones_Validar.Pedir_Servicio;
                objres.observacion = "Se verifico que existen Promociones Por Aplicar y/o Descuentos de Linea Por Aplicar:";
                objres.obsdetalle = cadena_msg;
            }
            else
            {
                objres.resultado = true;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
                objres.observacion = "";
                objres.obsdetalle = "";
            }
            return objres;
        }

        public async Task<ResultadoValidacion> Validar_Recargo_Aplicado_Por_Desc_Deposito_Excedente(DBContext _context, DatosDocVta DVTA, List<verecargosDatos> tablarecargos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);
            //validar que no se recargue(demas) por descuento por deposito excendente
            string cadena = "";
            decimal ttl_recargo_aplicado = 0;
            decimal ttl_descdeposito_aplicado = 0;
            decimal ttl_limite_descto_original = 0;
            decimal ttl_limite_descto = 0;
            decimal ttl_limite_recargo = 0;
            decimal ttl_saldo_recargo = 0;
            int codrecargo_deposito = await configuracion.emp_codrecargo_x_deposito(_context, codempresa);

            foreach (var recargos in tablarecargos)
            {
                //solo validar el recargo por deposito excedente, otros recargos no validar aqui
                if (recargos.codrecargo == codrecargo_deposito)
                {
                    //buscar la cobranza que genero el descto por deposito y verificar si genero recargo, el monto del recargo y el saldo de recargo
                    var dt1 = await cobranzas.Recargos_Por_Descto_Deposito_Excedente(_context, "CBZA", recargos.codcobranza.ToString(), DVTA.codcliente, DVTA.nitfactura, DVTA.codcliente_real, false, "APLICAR_DESCTO", DVTA.coddocumento, "Proforma_Nueva", codempresa);
                    //si hay registro verificar
                    if (dt1 != null)
                    {
                        foreach (var dt in dt1)
                        {
                            ttl_limite_descto_original = (decimal)dt.monto_limite_descto;
                            ttl_limite_descto = (decimal)dt.monto_limite_descto;
                            ttl_descdeposito_aplicado = (decimal)dt.monto_descto_aplicado;
                            ttl_limite_recargo = (decimal)dt.monto_recargo;
                            //obtener cuanto ya se aplico de recargo
                            ttl_recargo_aplicado = (decimal)await depositos_cliente.Total_Recargos_Por_Deposito_Aplicado_De_Cobranza_En_Proforma(_context, recargos.codcobranza, DVTA.coddocumento, DVTA.codmoneda);
                            //total recargo por aplicar
                            ttl_saldo_recargo = ttl_limite_recargo - ttl_recargo_aplicado;
                            //si el monto del recargo es mayor al saldo entonces genera error
                            if (ttl_saldo_recargo < recargos.montodoc)
                            {
                                objres.resultado = false;
                                cadena = Environment.NewLine + "- Limite de recargo de la Cbza: " + dt.idcbza.ToString() + "-" + dt.nroidcbza.ToString() + " es:" + ttl_limite_recargo.ToString("####,##0.000", new CultureInfo("en-US")) + " Aplicado: " + ttl_recargo_aplicado.ToString("####,##0.000", new CultureInfo("en-US")) + " Por Aplicar: " + ttl_saldo_recargo.ToString("####,##0.000", new CultureInfo("en-US")) + " Aplicando Ahora: " + recargos.montodoc.ToString("####,##0.000", new CultureInfo("en-US"));
                            }
                        }

                    }
                    else
                    {
                        objres.resultado = false;
                        cadena = Environment.NewLine + "- La Cbza no tiene descuento por deposito disponible, por lo tanto tampoco podria generar recargo!!!";
                    }
                }
            }

            if (objres.resultado == false)
            {
                objres.resultado = false;
                objres.observacion = "El monto del recargo por descuento de deposito excedente que intenta aplicar es mayor al recargo disponible!!!";
                objres.obsdetalle = cadena;
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }

        public async Task<ResultadoValidacion> Validar_Descuento_Por_Deposito(DBContext _context, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string resultado_cadena = "";
            string cadena = "";
            int codesextradeposito = await configuracion.emp_coddesextra_x_deposito_context(_context, codempresa);
            string[] doc_cbza = new string[2];
            DateTime fecha_cbza;

            InicializarResultado(objres);

            //esta es la fecha en la que JRA Y GVR enviaron corrreo indicando que todo deposito desde esta fecha
            //es aplicable solo si el cliente tiene linea de credito regularizada, vigente y no revertida
            DateTime mifecha = new DateTime(2015, 6, 3);
            foreach (var descuentos in tabladescuentos)
            {
                if (descuentos.coddesextra == codesextradeposito)
                {
                    // Primero verificar si el depósito existe
                    doc_cbza[0] = "";
                    doc_cbza[1] = "";

                    // Si es cobza credito
                    if (descuentos.codcobranza != 0)
                        doc_cbza = await cobranzas.Id_Nroid_Cobranza(_context, descuentos.codcobranza);
                    else if (descuentos.codcobranza_contado != 0)
                        doc_cbza = await cobranzas.Id_Nroid_Cobranza_Contado(_context, descuentos.codcobranza_contado);
                    else if (descuentos.codanticipo != 0)
                        doc_cbza = await cobranzas.Id_Nroid_Anticipo(_context, descuentos.codanticipo);

                    if (doc_cbza == null)
                    {
                        resultado_cadena += "\nLa cobranza-deposito: " + doc_cbza[0] + "-" + doc_cbza[1] + " No existe por tanto no se puede aplicar el descuento por deposito.";
                    }

                    // Si es cobza CREDITO
                    if (descuentos.codcobranza != 0)
                    {
                        if (await cobranzas.Existe_Cobranza(_context, doc_cbza[0], doc_cbza[1]) == true)
                        {
                            fecha_cbza = await cobranzas.Fecha_De_Cobranza(_context, doc_cbza[0], doc_cbza[1]);
                            // validar_descuento por deposito, si el desposito es posterior al 03 - 06 - 2105
                            //debe controlarse si el cliente tiene linea de credito valida
                            //si el cliente deberia tener linea de credito valida
                            if (await ventas.Descuento_Extra_Valida_Linea_Credito(_context, descuentos.coddesextra) == true)
                            {
                                //validar que el cliente tenga linea de credito, vigente no revertida
                                if (!await creditos.Cliente_Tiene_Linea_De_Credito_Valida(_context, DVTA.codcliente_real))
                                {
                                    resultado_cadena += "\nEl descuento por deposito que corresponde a la cobranza-deposito:" + doc_cbza[0] + "-" + doc_cbza[1] + " no puede ser añadido, porque todo deposito de cliente posterior al 03-06-2015 que se quiera aplicar debe tener linea de credito Fija regularizada.";
                                }
                            }
                        }
                        else
                        {
                            resultado_cadena += "\nLa cobranza-deposito: " + doc_cbza[0] + "-" + doc_cbza[1] + " No existe por tanto no se puede aplicar el descuento por deposito.";
                            break;
                        }
                    }

                    // Si es cobza CONTADO
                    if (descuentos.codcobranza_contado != 0)
                    {
                        if (await cobranzas.Existe_Cobranza_Contado(_context, doc_cbza[0], Convert.ToInt32(doc_cbza[1])) == true)
                        {
                            fecha_cbza = await cobranzas.Fecha_De_Cobranza_Contado(_context, doc_cbza[0], doc_cbza[1]);
                            //validar_descuento por deposito, si el desposito es posterior al 03-06-2105
                            //debe controlarse si el cliente tiene linea de credito valida
                            if (fecha_cbza > mifecha)
                            {
                                if (await ventas.Descuento_Extra_Valida_Linea_Credito(_context, descuentos.coddesextra) == true)
                                {
                                    if (!await creditos.Cliente_Tiene_Linea_De_Credito_Valida(_context, DVTA.codcliente_real))
                                    {
                                        resultado_cadena += "\nEl descuento por deposito que corresponde a la Cobranza Contado-Deposito:" + doc_cbza[0] + "-" + doc_cbza[1] + " no puede ser añadido, porque todo deposito de cliente posterior al 03-06-2015 que se quiera aplicar debe tener linea de credito Fija regularizada.";
                                    }
                                }
                            }
                        }
                        else
                        {
                            resultado_cadena += "\nLa Cobranza Contado-Deposito: " + doc_cbza[0] + "-" + doc_cbza[1] + " No existe por tanto no se puede aplicar el descuento por deposito.";
                            break;
                        }
                    }
                }
            }
            if (resultado_cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.datoA = "";
                objres.datoB = "";
                objres.observacion = "";
                objres.obsdetalle = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Descuento_Extra_Requiere_Credito_Valido(DBContext _context, string codcliente_real, List<vedesextraDatos> tabladescuentos, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string resultado_cadena = "";
            string cadena = "";
            bool requiere_credito = false;
            bool es_cliente_pertec = false;
            bool cliente_tiene_credito = false;
            bool tiene_casa_matriz = false;
            bool tiene_casa_matriz_nal = false;
            //esta es la fecha en la que JRA Y GVR enviaron corrreo indicando que todo deposito desde esta fecha
            //es aplicable solo si el cliente tiene linea de credito regularizada, vigente y no revertida
            DateTime mifecha = new DateTime(2015, 6, 3);
            string codigoPrincipal = "";
            string codigoPrincipal_nal = "";

            codigoPrincipal = await cliente.CodigoPrincipal(_context, codcliente_real);
            codigoPrincipal_nal = await cliente.CodigoPrincipal_Nacional(_context, codcliente_real);

            if (!(codcliente_real == codigoPrincipal))
            {
                tiene_casa_matriz = true;
            }

            if (!(codcliente_real == codigoPrincipal_nal))
            {
                tiene_casa_matriz = true;
            }

            //Solo considerarlo el principal si Tiene el mismo NIT, caso contrario usar solo el codigo individual de cliente
            if ((cliente.NIT(_context, codigoPrincipal) == cliente.NIT(_context, codcliente_real)))
            { }
            else
            {
                codigoPrincipal = codcliente_real;
            }
            es_cliente_pertec = await cliente.EsClientePertec(_context, codigoPrincipal);
            cliente_tiene_credito = await creditos.Cliente_Tiene_Linea_De_Credito_Valida(_context, codcliente_real);

            foreach (var descuentos in tabladescuentos)
            {
                requiere_credito = await ventas.Descuento_Extra_Valida_Linea_Credito(_context, descuentos.coddesextra);
                if (requiere_credito)
                {
                    if (es_cliente_pertec || cliente_tiene_credito)
                    { }
                    else
                    {
                        cadena = "El descuento: " + descuentos.coddesextra + "-" + descuentos.descripcion + " requiere Credito Fijo valido o ser cliente pertec!!!";
                    }
                }
            }
            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.datoA = "";
                objres.datoB = "";
                objres.observacion = "Los siguientes descuentos extras, requieren que el cliente: " + codcliente_real + " o su casa matriz local:" + codigoPrincipal + " o su casa matriz nacional: " + codigoPrincipal_nal + " tenga linea de credito valida o sea cliente Pertec";
                objres.obsdetalle = cadena;
                objres.accion = Acciones_Validar.Ninguna;
            }
            else
            {
                objres.resultado = true;
                objres.datoA = "";
                objres.datoB = "";
                objres.observacion = "";
                objres.obsdetalle = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }

        public async Task<ResultadoValidacion> Validar_Descuentos_Extra_Para_TipoVenta(DBContext _context, DatosDocVta DVTA, List<vedesextraDatos> tabladescuentos, List<vedetalleanticipoProforma> dt_anticipo_pf, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string tipovta = "";
            string cadena = "";
            string cadena_aux = "";
            string cadenaA = "";
            string cadenaB = "";
            int cant_anticipos = 0;
            int cant_anticipos_usados = 0;
            int ttl_anticipos = 0;
            bool pedir_clave = true;

            InicializarResultado(objres);

            int cod_desextra_contado = await configuracion.emp_coddesextra_x_deposito_contado(_context, codempresa);
            //esta rutina valida si el(los) descuentos extras aplicados a la proforma son validos para el tipo de venta de proforma(contado-credito-contra entrega)

            foreach (var descuentos in tabladescuentos)
            {
                tipovta = await ventas.Descuento_Extra_Tipo_Venta(_context, descuentos.coddesextra);
                if ((tipovta == "SIN RESTRICCION"))
                { }
                else
                {
                    if (!(tipovta == DVTA.tipo_vta))
                    {
                        cadena += "\r\n" + "La venta que esta realizando es tipo: " + DVTA.tipo_vta + " y el descuento aplicado: " + descuentos.coddesextra + "-" + descuentos.descripcion + " es para venta tipo: " + tipovta;
                    }

                    if ((tipovta == DVTA.tipo_vta) && (DVTA.contra_entrega == "SI") && (descuentos.coddesextra == cod_desextra_contado))
                    {
                        cadena += "\r\n" + "La venta que esta realizando es tipo: " + DVTA.tipo_vta + " - CONTRA ENTREGA y el descuento aplicado: " + descuentos.coddesextra + "-" + descuentos.coddesextra + " es para venta tipo SOLO: " + tipovta;
                    }

                    if (("CONTADO" == DVTA.tipo_vta) && (DVTA.contra_entrega == "NO") && (descuentos.coddesextra == cod_desextra_contado))
                    {
                        cant_anticipos = 0;
                        cant_anticipos_usados = 0;
                        ttl_anticipos = dt_anticipo_pf.Count;
                        if (dt_anticipo_pf.Count > 0)
                        {
                            foreach (var anticipos in dt_anticipo_pf)
                            {
                                string[] id_nroid_deposito = new string[2];
                                id_nroid_deposito = await depositos_cliente.IdNroid_Deposito_Asignado_Anticipo(_context, anticipos.id_anticipo, anticipos.nroid_anticipo);
                                cant_anticipos = cant_anticipos + 1;
                                if (id_nroid_deposito[0] == "NSE")
                                {
                                    cadena += "\r\n" + "El anticipo: " + anticipos.id_anticipo + "-" + anticipos.nroid_anticipo + " asignado a la proforma no esta enlazado a algun deposito de cliente!!!";
                                    cadenaA = anticipos.id_anticipo + "-" + anticipos.nroid_anticipo;
                                    cadenaB = DVTA.codcliente;
                                }
                                else
                                {
                                    // Desde 07/12/2023 controlar q un anticipo ya aplicado a una proforma CONTADO con el descuento 74 no pueda volver aplicarse solicitado por JRA
                                    string doc_anticipo_ya_aplicado;
                                    doc_anticipo_ya_aplicado = await depositos_cliente.Anticipo_Asignado_A_Deposito_a_Proforma(_context, id_nroid_deposito[0], id_nroid_deposito[1], true);
                                    if (doc_anticipo_ya_aplicado.Contains("->"))
                                    {
                                        cadena_aux += "\r\n" + "El anticipo: " + anticipos.id_anticipo + "-" + anticipos.nroid_anticipo + " ya fue aplicado al documento: " + doc_anticipo_ya_aplicado + ", no puede utilizar este anticipo para el descuento: " + descuentos.coddesextra + " !!!";
                                        cant_anticipos_usados = cant_anticipos_usados + 1;
                                    }
                                    if (cant_anticipos_usados == ttl_anticipos)
                                    {
                                        cadena = cadena_aux;
                                        cadenaA = anticipos.id_anticipo + "-" + anticipos.nroid_anticipo;
                                        cadenaB = DVTA.codcliente;
                                        pedir_clave = false;
                                    }

                                }

                            }
                        }
                        else
                        {
                            cadena += "\r\n" + "No existen anticipos asignados para validar si ya fueron utilizados!!!";
                            cadenaA = "NSE";
                            cadenaB = DVTA.codcliente;
                        }


                    }

                    // If reg("coddesextra") = cod_desextra_contado Then
                    //     If DVTA.contra_entrega = True And DVTA.tipo_vta = "CONTADO" Then
                    //         cadena &= vbCrLf & "La venta que esta realizando es tipo: " & DVTA.tipo_vta & " - CONTRA ENTREGA y el descuento aplicado: " & reg("coddesextra") & "-" & reg("descrip") & " es para venta tipo SOLO: " & tipovta
                    //     End If
                    // End If
                }
            }
            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.observacion = "Los descuentos extras aplicados no corresponden con el tipo de venta: " + DVTA.tipo_vta;
                objres.obsdetalle = cadena;
                objres.datoA = cadenaA;
                objres.datoB = cadenaB;
                //Desde 27/02/2024 segun autorizacion JRA solicitado por servicio al cliente que un anticipo ya utilizado para una proforma si esta tiene saldo, ese saldo puede utilizarce
                //para otra proforma y permitir el descuento respectivo.
                if (pedir_clave == true)
                {
                    objres.accion = Acciones_Validar.Pedir_Servicio;
                }
                else
                {
                    objres.accion = Acciones_Validar.Confirmar_SiNo;
                }

            }
            return objres;
        }

        public async Task<ResultadoValidacion> Validar_Monto_Minimos_Segun_Lista_Precio(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena = "";
            DataTable tabla_unidos = new DataTable();
            int i, k;
            DataRow dr;
            bool cliente_nuevo = await cliente.EsClienteNuevo(_context, DVTA.codcliente_real);
            decimal SUBTTL_GRAL_PEDIDO = 0;

            InicializarResultado(objres);

            if (await cliente.Controla_Monto_Minimo(_context, DVTA.codcliente_real) == false)
            {
                return objres;
            }
            tabla_unidos.Columns.Add("codtarifa", typeof(int));
            tabla_unidos.Columns.Add("total", typeof(double));

            // Elaborar una tabla con el tipo precio y el total
            foreach (var detalle in tabladetalle)
            {
                dr = tabla_unidos.NewRow();
                dr["codtarifa"] = detalle.codtarifa;
                dr["total"] = detalle.total;
                SUBTTL_GRAL_PEDIDO += Convert.ToDecimal(dr["total"]);
                tabla_unidos.Rows.Add(dr);
            }

            if (tabla_unidos.Rows.Count > 0)
            {
                List<int> precios = new List<int>();
                ArrayList totales = new ArrayList();
                decimal montomin, dif;

                // Obtener los diferentes precios que hay en el detalle
                foreach (DataRow row in tabla_unidos.Rows)
                {
                    if (!precios.Contains(int.Parse(row["codtarifa"].ToString())))
                    {
                        precios.Add(int.Parse(row["codtarifa"].ToString()));
                    }
                }

                // Totalizar el monto por tipo de precio
                for (i = 0; i < precios.Count; i++)
                {
                    totales.Add(0.0);
                    for (k = 0; k < tabla_unidos.Rows.Count; k++)
                    {
                        if (Convert.ToInt32(tabla_unidos.Rows[k]["codtarifa"]) == Convert.ToInt32(precios[i]))
                        {
                            totales[i] = Convert.ToDouble(totales[i]) + Convert.ToDouble(tabla_unidos.Rows[k]["total"]);
                        }
                    }
                }

                // Comparar y mostrar
                DataTable tabla = new DataTable();
                DataRow[] registro;

                // Ahora es así desde mayo-2021
                if (DVTA.tipo_vta == "CONTADO")
                {
                    // Contado
                    if (cliente_nuevo)
                    {
                        var query = from t in _context.intarifa
                                    orderby t.codigo
                                    select new { t.codigo, montomin = t.min_nuevo_contado, moneda = t.codmoneda_min_nuevo_contado };
                        var result = query.Distinct().ToList();
                        tabla = funciones.ToDataTable(result);
                    }
                    else
                    {
                        var query = from t in _context.intarifa
                                    orderby t.codigo
                                    select new { t.codigo, montomin = t.min_contado, moneda = t.codmoneda_min_contado };
                        var result = query.Distinct().ToList();
                        tabla = funciones.ToDataTable(result);
                    }
                }
                else
                {
                    // Crédito
                    if (cliente_nuevo)
                    {
                        var query = from t in _context.intarifa
                                    orderby t.codigo
                                    select new { t.codigo, montomin = t.min_nuevo_credito, moneda = t.codmoneda_min_nuevo_credito };
                        var result = query.Distinct().ToList();
                        tabla = funciones.ToDataTable(result);
                    }
                    else
                    {
                        var query = from t in _context.intarifa
                                    orderby t.codigo
                                    select new { t.codigo, montomin = t.min_credito, moneda = t.codmoneda_min_credito };
                        var result = query.Distinct().ToList();
                        tabla = funciones.ToDataTable(result);
                    }
                }

                // Verificar el monto si cumple con el requerido
                // Antes se obtiene datos del complemento mayorista-Dimediado si tiene
                int _codproforma = 0;
                decimal _subtotal_pfcomplemento = 0;
                string _moneda_total_pfcomplemento = "";
                bool hay_enlace = false;

                try
                {
                    if (!string.IsNullOrEmpty(DVTA.idpf_complemento) && !string.IsNullOrEmpty(DVTA.nroidpf_complemento) && DVTA.tipo_complemento == "complemento_mayorista_dimediado")
                    {
                        _codproforma = await ventas.codproforma(_context, DVTA.idpf_complemento, int.Parse(DVTA.nroidpf_complemento));
                        if (Convert.ToInt32(DVTA.nroidpf_complemento) > 0)
                        {
                            _moneda_total_pfcomplemento = await ventas.MonedaPF(_context, _codproforma);
                            _subtotal_pfcomplemento = await ventas.SubTotal_Proforma(_context, _codproforma);
                            _subtotal_pfcomplemento = await tipocambio._conversion(_context, DVTA.codmoneda, _moneda_total_pfcomplemento, DVTA.fechadoc.Date, (decimal)_subtotal_pfcomplemento);
                            hay_enlace = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    _moneda_total_pfcomplemento = "";
                    _subtotal_pfcomplemento = 0;
                }

                // Obtener el precio que tiene monto min mayor para validar con ese
                int CODTARIFA_PARA_VALIDAR = await Tarifa_Monto_Min_Mayor(_context, DVTA, precios);
                List<int> precios_para_validar = new List<int>();
                precios_para_validar.Add(CODTARIFA_PARA_VALIDAR);

                i = 0;
                foreach (int precio in precios_para_validar)
                {

                    registro = tabla.Select("codigo=" + precio + " ");

                    // Verificar si recupero los datos del tipo de precio
                    if (registro.Length > 0)
                    {
                        montomin = await tipocambio._conversion(_context, DVTA.codmoneda, (string)registro[0]["moneda"], DVTA.fechadoc.Date, Convert.ToDecimal(registro[0]["montomin"]));

                        // Implementado en fecha: 07-07-2021
                        // Se añade el subtotal del complemento
                        dif = (Convert.ToDecimal(totales[i]) + _subtotal_pfcomplemento);
                        dif = SUBTTL_GRAL_PEDIDO + _subtotal_pfcomplemento;
                        dif -= montomin;
                        if (dif < 0)
                        {
                            cadena += "\nMonto mínimo de venta a precio: " + precio.ToString() + " es de: " + montomin.ToString("####,##0.000", new CultureInfo("en-US")) + "(" + DVTA.codmoneda + ") el monto actual es de: " + Math.Round(SUBTTL_GRAL_PEDIDO, 2).ToString("####,##0.000", new CultureInfo("en-US"));
                            cadena += "\nSi el pedido tiene diferentes precios, se valida con el monto minimo mayor de listas de precios.";
                        }
                    }
                    else
                    {
                        montomin = 9999999999;
                        cadena += "\nNo se encontró en la tabla de precios, los parámetros del tipo de precio: " + precio.ToString() + ", consulte con el administrador del sistema!!!";
                    }
                    i = i + 1;
                }
                registro = null;
                tabla.Dispose();
            }

            if (cadena.Trim().Length > 0)
            {
                string tipoCliente = cliente_nuevo ? "NUEVO" : "HABITUAL";
                objres.resultado = false;
                objres.datoA = "";
                objres.datoB = "";
                objres.observacion = "El cliente " + DVTA.codcliente_real + " es: " + tipoCliente + " y el documento no cumple el Monto Minimo de venta: " + DVTA.tipo_vta + " requerido de la lista de precios: ";
                objres.obsdetalle = cadena;
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<string> Validar_Precios_Permitidos_Usuario(DBContext _context, string usuario, List<itemDataMatriz> tabladetalle)
        {
            string cadena_precios_no_autorizados_al_us = "";
            List<int> lista_precios = new List<int>();
            foreach (var reg in tabladetalle)
            {
                if (!await ventas.UsuarioTarifa_Permitido(_context, usuario, reg.codtarifa))
                {
                    //arma cadena de precios no permitidos
                    if (!lista_precios.Contains(reg.codtarifa))
                    {
                        lista_precios.Add(reg.codtarifa);
                    }
                    cadena_precios_no_autorizados_al_us = cadena_precios_no_autorizados_al_us + reg.codtarifa + ", ";
                }
            }
            return cadena_precios_no_autorizados_al_us;
        }
        public async Task<List<int>> Lista_Precios_En_El_Documento(List<itemDataMatriz> tabladetalle)
        {
            var elementosUnicos = tabladetalle
                .Select(objeto => objeto.codtarifa)
                .Distinct()
                .ToList();
            return elementosUnicos;
        }
        public async Task<ResultadoValidacion> Validar_Monto_Minimo_Para_Entrega_Pedido(DBContext _context, DatosDocVta DVTA, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            bool resultado = true;
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena = "";
            DataTable tabla_unidos = new DataTable();
            int i, k;
            DataRow dr;
            bool cliente_nuevo = await cliente.EsClienteNuevo(_context, DVTA.codcliente_real);
            decimal SUBTTL_GRAL_PEDIDO = 0;

            InicializarResultado(objres);
            //si el codalmacen es tienda, directo se validar como verdadero porque siginifica que es tienda y las ventas en mostrador
            //se entregan al momento de la venta en mostrador
            if (await almacen.Es_Tienda(_context, int.Parse(DVTA.codalmacen)))
            {
                return objres;
            }
            tabla_unidos.Columns.Add("codtarifa", typeof(int));
            tabla_unidos.Columns.Add("total", typeof(double));

            // Elaborar una tabla con el tipo precio y el total
            foreach (var detalle in tabladetalle)
            {
                dr = tabla_unidos.NewRow();
                dr["codtarifa"] = detalle.codtarifa;
                dr["total"] = detalle.total;
                SUBTTL_GRAL_PEDIDO += Convert.ToDecimal(dr["total"]);
                tabla_unidos.Rows.Add(dr);
            }

            if (tabla_unidos.Rows.Count > 0)
            {
                List<int> precios = new List<int>();
                ArrayList totales = new ArrayList();
                decimal montomin, dif;

                // Obtener los diferentes precios que hay en el detalle
                foreach (DataRow row in tabla_unidos.Rows)
                {
                    if (!precios.Contains(int.Parse(row["codtarifa"].ToString())))
                    {
                        precios.Add(int.Parse(row["codtarifa"].ToString()));
                    }
                }

                // Totalizar el monto por tipo de precio
                for (i = 0; i < precios.Count; i++)
                {
                    totales.Add(0.0);
                    for (k = 0; k < tabla_unidos.Rows.Count; k++)
                    {
                        if (Convert.ToInt32(tabla_unidos.Rows[k]["codtarifa"]) == Convert.ToInt32(precios[i]))
                        {
                            totales[i] = Convert.ToDouble(totales[i]) + Convert.ToDouble(tabla_unidos.Rows[k]["total"]);
                        }
                    }
                }

                // Comparar y mostrar
                DataTable tabla = new DataTable();
                DataRow[] registro;

                //DESDE EL 20-02-2023
                if (DVTA.tipo_vta == "CONTADO")
                {
                    // Contado
                    var query = from t in _context.intarifa
                                orderby t.codigo
                                select new { t.codigo, montomin = t.min_entrega_contado, moneda = t.codmoneda_min_entrega_contado };
                    var result = query.Distinct().ToList();
                    tabla = funciones.ToDataTable(result);
                }
                else
                {
                    // Crédito
                    var query = from t in _context.intarifa
                                orderby t.codigo
                                select new { t.codigo, montomin = t.min_entrega_credito, moneda = t.codmoneda_min_entrega_credito };
                    var result = query.Distinct().ToList();
                    tabla = funciones.ToDataTable(result);
                }

                // Obtener el precio que tiene monto min mayor para validar con ese
                int CODTARIFA_PARA_VALIDAR = await Tarifa_Monto_Min_Mayor(_context, DVTA, precios);
                List<int> precios_para_validar = new List<int>();
                precios_para_validar.Add(CODTARIFA_PARA_VALIDAR);

                foreach (int precio in precios_para_validar)
                {
                    registro = tabla.Select("codigo=" + precio + " ");

                    // Verificar si recupero los datos del tipo de precio
                    if (registro.Length == 0)
                    {
                        // verificar si hay precio
                        montomin = 9999999999;
                        cadena += "\nNo se encontró en la tabla de precios, los parámetros del tipo de precio: " + precio.ToString() + ", consulte con el administrador del sistema!!!";
                    }
                    else
                    {
                        //obtiene el monto minimo requerido de la lista de precio
                        montomin = await tipocambio._conversion(_context, DVTA.codmoneda, (string)registro[0]["moneda"], DVTA.fechadoc.Date, Convert.ToDecimal(registro[0]["montomin"]));
                        dif = Math.Round(SUBTTL_GRAL_PEDIDO, 2) - montomin;
                        if (dif < 0)
                        {
                            resultado = false;
                            cadena += "\nMonto Min a precio: " + precio.ToString() + " Para Entrega es: " + montomin.ToString("####,##0.000", new CultureInfo("en-US")) + "(" + DVTA.codmoneda + ") el monto actual es de: " + Math.Round(SUBTTL_GRAL_PEDIDO, 2).ToString("####,##0.000", new CultureInfo("en-US")) + "(" + DVTA.codmoneda + ")";
                            cadena += "\nSi el pedido tiene diferentes precios, se valida con el monto minimo mayor de listas de precios.";
                        }
                    }
                }
                registro = null;
                tabla.Dispose();
            }

            if (resultado == false)
            {
                objres.resultado = false;
                objres.datoA = "";
                objres.datoB = "";
                objres.observacion = "El pedido no puede ser entregado al cliente por Pertec!!!";
                objres.obsdetalle = cadena;
                objres.accion = Acciones_Validar.Confirmar_SiNo;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Monto_Minimo_Para_Aplicar_Descuentos_Especiales(DBContext _context, string codcliente_real, List<itemDataMatriz> tabladetalle, string codmoneda, DateTime fecha, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena = "";
            DataTable tabla_unidos = new DataTable();
            int i, k;
            DataRow dr;

            InicializarResultado(objres);

            //a todos los clientes se les debe validar si cumple el monto minimo de los desctos especiales
            //no importa si es cliente final o no, esta aclaracion la hizo JRA fines de julio 2022
            //siempre debe controlar que cumpla el monto para descto especial

            tabla_unidos.Columns.Add("coddescuento", typeof(int));
            tabla_unidos.Columns.Add("total", typeof(double));

            // Elaborar una tabla con el tipo precio y el total
            foreach (var detalle in tabladetalle)
            {
                dr = tabla_unidos.NewRow();
                dr["coddescuento"] = detalle.coddescuento;
                dr["total"] = detalle.total;
                tabla_unidos.Rows.Add(dr);
            }

            if (tabla_unidos.Rows.Count > 0)
            {
                List<string> descuentos = new List<string>();
                ArrayList totales = new ArrayList();
                decimal montomin, dif;

                // Obtener los diferentes precios que hay en el detalle
                foreach (DataRow row in tabla_unidos.Rows)
                {
                    if (int.Parse(row["coddescuento"].ToString()) > 0)
                    {
                        if (!descuentos.Contains(row["coddescuento"].ToString()))
                        {
                            descuentos.Add(row["coddescuento"].ToString());
                        }
                    }

                }

                // Totalizar el monto por tipo de precio
                for (i = 0; i < descuentos.Count; i++)
                {
                    totales.Add(0.0);
                    for (k = 0; k < tabla_unidos.Rows.Count; k++)
                    {
                        if (Convert.ToInt32(tabla_unidos.Rows[k]["coddescuento"]) == Convert.ToInt32(descuentos[i]))
                        {
                            totales[i] = Convert.ToDouble(totales[i]) + Convert.ToDouble(tabla_unidos.Rows[k]["total"]);
                        }
                    }
                }

                // Comparar y mostrar
                DataTable tabla = new DataTable();
                DataRow[] registro;

                var query = from t in _context.vedescuento
                            orderby t.codigo
                            select new { t.codigo, t.monto, t.moneda };
                var result = query.Distinct().ToList();
                tabla = funciones.ToDataTable(result);

                i = 0;
                foreach (string desc in descuentos)
                {

                    registro = tabla.Select("codigo=" + desc + " ");

                    // Verificar si recupero los datos del tipo de precio
                    if (registro.Length == 0)
                    {
                        // verificar si hay precio
                        cadena += "\nNo se encontro en la tabla(vedescuento) de descuentos especiales, los parametros del descuento: " + desc.ToString() + ", consulte con el administrador del sistema!!!";
                    }
                    else
                    {
                        montomin = await tipocambio._conversion(_context, codmoneda, (string)registro[0]["moneda"], fecha, Convert.ToDecimal(registro[0]["monto"]));
                        dif = Convert.ToDecimal(totales[i]) - montomin;
                        if (dif < 0)
                        {
                            cadena += "\nEl monto minimo para aplicar el descuento especial: " + desc.ToString() + " es de: " + montomin.ToString("####,##0.000", new CultureInfo("en-US")) + "(" + codmoneda + ") el monto actual es de: " + Math.Round(Convert.ToDecimal(totales[i]), 2).ToString("####,##0.000", new CultureInfo("en-US"));
                        }
                    }
                    i = i + 1;
                }
                registro = null;
                tabla.Dispose();
            }

            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                objres.datoA = "";
                objres.datoB = "";
                objres.observacion = "Se encontraron observaciones para la aplicacion del descuento especial: ";
                objres.obsdetalle = cadena;
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Empaque_Minimo_Segun_Lista_Precios(DBContext _context, List<itemDataMatriz> tabladetalle, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena = "";
            string item = "";
            int i, j;
            decimal cantidad_total;

            InicializarResultado(objres);

            if (!await cliente.Controla_empaque_minimo(_context, DVTA.codcliente_real))
            {
                return objres;
            }

            foreach (var detalle in tabladetalle)
            {
                //desde 23-11-2022 implementar que de un item sume la cantidad total de los items repetidos para asi validar si el empaque del item sin descuento cumpla o no el empaque minimo
                //ya que al realizar la division de las cantidad de un item para cumplir empaque caja cerrada puede haber items q no cumplan el empaque minimo despues de la division'
                //para esto el item sin descuento empaque cerrada para validar debe sumar su propia cantidad mas la cantidad de caja cerrada y validar el empaque minimo con esa cantidad
                if (detalle.coddescuento == 0)
                {
                    //si no tiene descuento sumar las cantidad de todo el detalle con el mismo item y validar el empaque con ese total
                    cantidad_total = 0;
                    item = detalle.coditem;
                    //sacar items iguales
                    foreach (var detalle2 in tabladetalle)
                    {
                        if (item == detalle2.coditem)
                        {
                            //si es igual sumar la cantidad
                            cantidad_total = cantidad_total + (decimal)detalle2.cantidad;
                        }
                        else
                        {
                            cantidad_total = cantidad_total;
                        }
                    }
                    if (await restricciones.cumpleempaque(_context, detalle.coditem, detalle.codtarifa, detalle.coddescuento, cantidad_total, int.Parse(DVTA.codalmacen), DVTA.codcliente_real))
                    {
                        detalle.cumple = true;
                    }
                    else
                    {
                        cadena += "\n" + detalle.coditem + " precio: " + detalle.codtarifa;
                        detalle.cumple = false;
                    }
                }
                else
                {
                    if (await restricciones.cumpleempaque(_context, detalle.coditem, detalle.codtarifa, detalle.coddescuento, (decimal)detalle.cantidad, int.Parse(DVTA.codalmacen), DVTA.codcliente_real))
                    {
                        detalle.cumple = true;
                    }
                    else
                    {
                        cadena += "\n" + detalle.coditem + " precio: " + detalle.codtarifa;
                        detalle.cumple = false;
                    }
                }
            }

            if (cadena.Trim().Length > 0)
            {
                objres.resultado = false;
                if (DVTA.estado_doc_vta == "NUEVO")
                {
                    objres.datoA = DVTA.nitfactura;
                }
                else
                {
                    objres.datoA = DVTA.id + "-" + DVTA.numeroid + "-" + DVTA.nitfactura;
                }
                objres.datoB = "Total: " + DVTA.totaldoc.ToString("####,##0.000", new CultureInfo("en-US")) + " (" + DVTA.codmoneda + ")";
                objres.observacion = "Los siguientes items no cumplen el empaque minimo de la lista de precios: ";
                objres.obsdetalle = cadena;
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Credito_Disponible_Para_Vta(DBContext _context, DatosDocVta DVTA, string codempresa, string usuario)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            bool tiene_credito_suficiente = true;
            string moneda_cliente = await cliente.monedacliente(_context, DVTA.codcliente_real, codempresa, usuario);
            decimal monto = 0;
            string monedae = await empresa.monedaext(_context, codempresa);
            string monedabase = await Empresa.monedabase(_context, codempresa);


            InicializarResultado(objres);

            if (!(DVTA.tipo_vta.Trim().ToUpper() == "CREDITO"))
            {
                return objres;
            }

            if ((DVTA.tipo_vta.Trim().ToUpper() == "CREDITO"))
            {
                if ((DVTA.codmoneda == monedae))
                {
                    // If DVTA.codmoneda = moneda_cliente Then
                    // tiene_credito_suficiente = sia_funciones.Creditos.Instancia.ValidarCreditoDisponible(False, DVTA.codcliente_real, True, CDbl(DVTA.totaldoc), sia_compartidos.temporales.Instancia.codempresa, sia_compartidos.temporales.Instancia.usuario, False)
                    var result = await creditos.ValidarCreditoDisponible_en_Bs(_context, false, DVTA.codcliente_real, true, DVTA.totaldoc, codempresa, usuario, monedae, DVTA.codmoneda);
                    tiene_credito_suficiente = result.resultado_func;
                }
                else
                {
                    // monto = sia_funciones.TipoCambio.Instancia.conversion(moneda_cliente, DVTA.codmoneda, DVTA.fechadoc.Date, DVTA.totaldoc)
                    var result1 = await creditos.ValidarCreditoDisponible_en_Bs(_context, false, DVTA.codcliente_real, true, (double)await tipocambio._conversion(_context, monedabase, DVTA.codmoneda, DVTA.fechadoc, (decimal)DVTA.totaldoc), codempresa, usuario, monedae, DVTA.codmoneda);
                    tiene_credito_suficiente = result1.resultado_func;
                }

            }

            if (!tiene_credito_suficiente)
            {
                objres.resultado = false;
                objres.observacion = ("El cliente:" + (DVTA.codcliente_real + " no tiene credito suficiente, verifique esta situacion!!!"));
                objres.obsdetalle = "";
                if ((DVTA.estado_doc_vta == "NUEVO"))
                {
                    objres.datoA = DVTA.nitfactura;
                }
                else
                {
                    objres.datoA = (DVTA.id + ("-" + DVTA.numeroid));
                }

                objres.datoB = "Total: " + DVTA.totaldoc.ToString("####,##0.000", new CultureInfo("en-US")) + " (" + DVTA.codmoneda + ")";
                objres.accion = Acciones_Validar.Pedir_Servicio;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Etiqueta_Proforma(DBContext _context, List<vedetalleEtiqueta> dt_etiqueta, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);

            if (await empresa.Forzar_Etiquetas(_context, codempresa))
            {
                if (dt_etiqueta.Count <= 0)
                {
                    objres.resultado = false;
                    objres.observacion = "Debe indicar los datos de la etiqueta del pedido!!!";
                    objres.obsdetalle = "";
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                }
                else
                {
                    objres.resultado = true;
                    objres.obsdetalle = "";
                    objres.datoA = "";
                    objres.datoB = "";
                    //direccion 
                    objres.observacion = dt_etiqueta[0].representante;
                }
            }
            else
            {
                objres.resultado = true;
                objres.observacion = "";
                objres.obsdetalle = "";
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }
        public async Task<ResultadoValidacion> Validar_Cliente_Tiene_Reversiones_PP_Pendientes_de_Pago(DBContext _context, List<vedesextraDatos> tabladescuentos, DatosDocVta DVTA, string codempresa)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            InicializarResultado(objres);

            if (await Venta_Tiene_Descuento_ProntoPago(_context, tabladescuentos))
            {
                string cadena_msg = "";
                int nro_reversiones_pendiente_permitido = 0;
                int nro_reversiones_pendiente_cliente = 0;
                nro_reversiones_pendiente_cliente = await ventas.Nro_Reversiones_Pendientes_de_Pago_Cliente(_context, DVTA.codcliente_real);
                nro_reversiones_pendiente_permitido = await ventas.Nro_Reversiones_Pendientes_Pago_Permitido(_context, codempresa);
                if ((nro_reversiones_pendiente_cliente > nro_reversiones_pendiente_permitido))
                {
                    cadena_msg = await ventas.Notas_de_Reversion_Pendientes_de_Pago(_context, DVTA.codcliente_real);
                    objres.resultado = false;
                    objres.observacion = "El cliente tiene: " + nro_reversiones_pendiente_cliente + " reversiones pronto pago pendientes de pago, y el maximo permitido es: " + nro_reversiones_pendiente_permitido + ", por lo cual no se le puede otorgar mas descuentos pronto pago.";
                    objres.obsdetalle = ("\r\n" + cadena_msg);
                    if ((DVTA.estado_doc_vta == "NUEVO"))
                    {
                        objres.datoA = DVTA.nitfactura;
                    }
                    else
                    {
                        objres.datoA = DVTA.id + "-" + DVTA.numeroid + "-" + DVTA.codcliente;
                    }
                    objres.datoB = "Total: " + DVTA.totaldoc.ToString("####,##0.000", new CultureInfo("en-US")) + " (" + DVTA.codmoneda + ")";
                    objres.accion = Acciones_Validar.Pedir_Servicio;
                }
            }
            return objres;
        }
        public async Task<bool> Venta_Tiene_Descuento_ProntoPago(DBContext _context, List<vedesextraDatos> tabladescuentos)
        {
            bool resultado = false;

            foreach (var row in tabladescuentos)
            {
                if (await ventas.Descuento_Extra_Es_Pronto_Pago(_context, row.coddesextra))
                {
                    resultado = true;
                    break;
                }
                else
                {
                    resultado = false;
                }
            }

            return resultado;
        }
        public async Task<int> Tarifa_Monto_Min_Mayor_Rodrigo(DBContext _context, List<int> milista_precios, veproforma DVTA)
        {
            bool cliente_nuevo = await cliente.EsClienteNuevo(_context, DVTA.codcliente_real);
            double MONTO_MAYOR = 0;
            int resultado = 0;
            if (milista_precios.Count() == 1)
            {
                return milista_precios[0];
            }
            foreach (var reg in milista_precios)
            {
                intarifaMonMinMay tabla = new intarifaMonMinMay();
                if (DVTA.tipopago == 0)   // "CONTADO"
                {
                    // contado
                    if (cliente_nuevo)
                    {

                        tabla = await _context.intarifa
                            .Where(i => i.codigo == reg)
                            .Select(i => new intarifaMonMinMay
                            {
                                codtarifa = i.codigo,
                                montomin = i.min_nuevo_contado ?? 0,
                                moneda = i.codmoneda_min_nuevo_contado
                            })
                            .Distinct()
                            .FirstOrDefaultAsync() ?? tabla;
                    }
                    else
                    {
                        tabla = await _context.intarifa
                            .Where(i => i.codigo == reg)
                            .Select(i => new intarifaMonMinMay
                            {
                                codtarifa = i.codigo,
                                montomin = i.min_contado ?? 0,
                                moneda = i.codmoneda_min_contado
                            })
                            .Distinct()
                            .FirstOrDefaultAsync() ?? tabla;
                    }
                }
                else  //  CREDITO  == 1
                {
                    // credito
                    if (cliente_nuevo)
                    {

                        tabla = await _context.intarifa
                            .Where(i => i.codigo == reg)
                            .Select(i => new intarifaMonMinMay
                            {
                                codtarifa = i.codigo,
                                montomin = i.min_nuevo_credito ?? 0,
                                moneda = i.codmoneda_min_nuevo_credito
                            })
                            .Distinct()
                            .FirstOrDefaultAsync() ?? tabla;
                    }
                    else
                    {
                        tabla = await _context.intarifa
                            .Where(i => i.codigo == reg)
                            .Select(i => new intarifaMonMinMay
                            {
                                codtarifa = i.codigo,
                                montomin = i.min_credito ?? 0,
                                moneda = i.codmoneda_min_credito
                            })
                            .Distinct()
                            .FirstOrDefaultAsync() ?? tabla;
                    }
                    
                }
                // convertir a la moneda en la cual se esta haciendo la proforma
                double Monto_Min_Tarifa = (double)await tipocambio._conversion(_context, DVTA.codmoneda, tabla.moneda, DVTA.fecha, tabla.montomin);

                // verificar si el monto min del tipo de precio es mayor, si es mayo se obtiene su codtarifa
                if (MONTO_MAYOR == 0)
                {
                    MONTO_MAYOR = Monto_Min_Tarifa;
                    resultado = tabla.codtarifa;
                }
                else
                {
                    if (Monto_Min_Tarifa > MONTO_MAYOR)
                    {
                        MONTO_MAYOR = Monto_Min_Tarifa;
                        resultado = tabla.codtarifa;
                    }
                }
            }
            return resultado;
        }
        public async Task<int> Tarifa_Monto_Min_Mayor(DBContext _context, DatosDocVta DVTA, List<int> milista_precios)
        {
            int resultado = 0;
            DataTable tabla = new DataTable();
            bool cliente_nuevo = await cliente.EsClienteNuevo(_context, DVTA.codcliente_real);
            decimal Monto_Min_Tarifa;
            decimal MONTO_MAYOR = 0;

            if (milista_precios.Count == 1)
            {
                resultado = milista_precios[0];
            }
            else
            {
                foreach (int precio in milista_precios)
                {
                    if (DVTA.tipo_vta == "CONTADO")
                    {
                        if (cliente_nuevo)
                        {
                            var query = from t in _context.intarifa
                                        orderby t.codigo
                                        select new { codtarifa = t.codigo, montomin = t.min_nuevo_contado, moneda = t.codmoneda_min_nuevo_contado };
                            var result = query.Distinct().ToList();
                            tabla = funciones.ToDataTable(result);
                            //tabla = ObtenerDataTable("SELECT DISTINCT codigo AS codtarifa, min_nuevo_contado AS montomin, codmoneda_min_nuevo_contado AS moneda FROM intarifa WHERE codigo='" + precio + "' ORDER BY codigo");
                        }
                        else
                        {
                            var query = from t in _context.intarifa
                                        orderby t.codigo
                                        select new { codtarifa = t.codigo, montomin = t.min_contado, moneda = t.codmoneda_min_contado };
                            var result = query.Distinct().ToList();
                            tabla = funciones.ToDataTable(result);
                            //tabla = ObtenerDataTable("SELECT DISTINCT codigo AS codtarifa, min_contado AS montomin, codmoneda_min_contado AS moneda FROM intarifa WHERE codigo='" + precio + "' ORDER BY codigo");
                        }
                    }
                    else
                    {
                        if (cliente_nuevo)
                        {
                            var query = from t in _context.intarifa
                                        orderby t.codigo
                                        select new { codtarifa = t.codigo, montomin = t.min_nuevo_credito, moneda = t.codmoneda_min_nuevo_credito };
                            var result = query.Distinct().ToList();
                            tabla = funciones.ToDataTable(result);
                            //tabla = ObtenerDataTable("SELECT DISTINCT codigo AS codtarifa, min_nuevo_credito AS montomin, codmoneda_min_nuevo_credito AS moneda FROM intarifa WHERE codigo='" + precio + "' ORDER BY codigo");
                        }
                        else
                        {
                            var query = from t in _context.intarifa
                                        orderby t.codigo
                                        select new { codtarifa = t.codigo, montomin = t.min_credito, moneda = t.codmoneda_min_credito };
                            var result = query.Distinct().ToList();
                            tabla = funciones.ToDataTable(result);
                            //tabla = ObtenerDataTable("SELECT DISTINCT codigo AS codtarifa, min_credito AS montomin, codmoneda_min_credito AS moneda FROM intarifa WHERE codigo='" + precio + "' ORDER BY codigo");
                        }
                    }

                    Monto_Min_Tarifa = 0;
                    Monto_Min_Tarifa = await tipocambio._conversion(_context, DVTA.codmoneda, (string)tabla.Rows[0]["moneda"], DVTA.fechadoc.Date, (decimal)tabla.Rows[0]["montomin"]);

                    if (MONTO_MAYOR == 0)
                    {
                        MONTO_MAYOR = Monto_Min_Tarifa;
                        resultado = int.Parse(tabla.Rows[0]["codtarifa"].ToString());
                    }
                    else
                    {
                        if (Monto_Min_Tarifa > MONTO_MAYOR)
                        {
                            MONTO_MAYOR = Monto_Min_Tarifa;
                            resultado = int.Parse(tabla.Rows[0]["codtarifa"].ToString());
                        }
                        else
                        {
                            MONTO_MAYOR = MONTO_MAYOR;
                        }
                    }
                }
            }

            return resultado;
        }

        public async Task<ResultadoValidacion> Validar_Periodo_Abierto_Para_Venta(DBContext _context, DatosDocVta DVTA, string codempresa, string usuario)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string periodo = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(DVTA.fechadoc.Date.Month).ToUpper() + "-" + DVTA.fechadoc.Year.ToString();
            InicializarResultado(objres);

            if (!await seguridad.periodo_fechaabierta_context(_context, DVTA.fechadoc.Date, 3))
            {
                objres.resultado = false;
                objres.observacion = "El periodo " + periodo + " esta cerrado, no se pueden realizar ventas con esa fecha!!!";
                objres.obsdetalle = "";
                objres.datoA = "";
                objres.datoB = "";
                objres.accion = Acciones_Validar.Ninguna;
            }
            return objres;
        }

        //public async Task<bool> Validar_Resaltar_Empaques_Minimos_Segun_Lista_Precios(DBContext _context, List<itemDataMatriz> tabladetalle, int codalmacen, string codcliente)
        //{
        //    bool resultado = true;

        //    foreach (var detalle in tabladetalle)
        //    {
        //        if (await restricciones.cumpleempaque(_context, detalle.coditem, detalle.codtarifa, detalle.coddescuento, (decimal)detalle.cantidad, codalmacen, codcliente))
        //        {
        //            detalle.cumple = true;
        //        }
        //        else
        //        {
        //            resultado = false;
        //            detalle.cumple = false;
        //        }
        //    }

        //    return resultado;
        //}
        public async Task<(bool cumple, List<itemDataMatriz> tabladetalle)> Validar_Resaltar_Empaques_Minimos_Segun_Lista_Precios(DBContext _context, List<itemDataMatriz> tabladetalle, int codalmacen, string codcliente)
        {
            bool resultado = true;

            foreach (var detalle in tabladetalle)
            {
                if (await restricciones.cumpleempaque(_context, detalle.coditem, detalle.codtarifa, detalle.coddescuento, (decimal)detalle.cantidad, codalmacen, codcliente))
                {
                    detalle.cumple = true;
                }
                else
                {
                    resultado = false;
                    detalle.cumple = false;
                }
            }

            return (resultado, tabladetalle);
        }
        public async Task<(string cadena, List<itemDataMatriz> tabladetalle)> Validar_Resaltar_Empaques_Caja_Cerrada_DesctoEspecial_detalle(DBContext _context, List<itemDataMatriz> tabladetalle, int codalmacen, bool quitar_descuento, string codcliente)
        {
            bool resultado = true;
            string cadena = "";

            foreach (var detalle in tabladetalle)
            {//validar el empaque del precio
                if (await ventas.Cumple_Empaque_De_DesctoEspecial(_context, detalle.coditem, detalle.codtarifa, detalle.coddescuento, (decimal)detalle.cantidad, codcliente))
                {
                    detalle.cumple = true;
                }
                else
                {
                    if (quitar_descuento)
                    {
                        resultado = false;
                        detalle.cumple = false;
                        detalle.coddescuento = 0;
                    }
                    else
                    {
                        resultado = false;
                    }
                    if (cadena.Length == 0)
                    {
                        cadena = "";
                        cadena = " ITEM     DESCRIPCION          MEDIDA      CANTIDAD" + "\r\n";
                        cadena += "------------------------------------------------" + "\r\n";
                        cadena += " " + detalle.coditem + " " + funciones.Rellenar(detalle.descripcion, 20, " ", false) + " " + funciones.Rellenar(detalle.medida, 11, " ", false) + "  " + funciones.Rellenar(detalle.cantidad.ToString("####,##0.000", new CultureInfo("en-US")), 3, " ") + "\r\n";
                    }
                    else
                    {
                        cadena += " " + detalle.coditem + " " + funciones.Rellenar(detalle.descripcion, 20, " ", false) + " " + funciones.Rellenar(detalle.medida, 11, " ", false) + "  " + funciones.Rellenar(detalle.cantidad.ToString("####,##0.000", new CultureInfo("en-US")), 3, " ") + "\r\n";
                    }

                }
            }

            return (cadena, tabladetalle);
        }
        public async Task<(bool result, List<itemDataMatriz> tabladetalle)> Validar_Resaltar_Empaques_Caja_Cerrada_DesctoEspecial(DBContext _context, List<itemDataMatriz> tabladetalle, int codalmacen, bool quitar_descuento, string codcliente)
        {
            bool resultado = true;

            foreach (var detalle in tabladetalle)
            {//validar el empaque del precio
                if (await ventas.Cumple_Empaque_De_DesctoEspecial(_context, detalle.coditem, detalle.codtarifa, detalle.coddescuento, (decimal)detalle.cantidad, codcliente))
                {
                    detalle.cumple = true;
                }
                else
                {
                    if (quitar_descuento)
                    {
                        resultado = false;
                        detalle.cumple = false;
                        detalle.coddescuento = 0;
                    }
                    else
                    {
                        resultado = false;
                    }
                }
            }

            return (resultado, tabladetalle);
        }

        public async Task<(List<itemDataSugerencia> tabla_sugerencia, List<itemDataMatriz> tabladetalle)> Sugerir_Cantidades_Empaques_Caja_Cerrada_DesctoEspecial(DBContext _context, List<itemDataMatriz> tabladetalle, int codalmacen, int coddescuento, string codempresa)
        {
            bool resultado = true;
            List<itemDataSugerencia> dt = new List<itemDataSugerencia>();
            int cantidad = 0;
            int cantidad_mas = 0;
            int cantidad_menos = 0;
            string sugerencia = "0";
            string obs = "Sin Obs.";
            int i;
            double porcentaje_empaque = 0;
            double cantidad_porcentaje = 0;
            double empaque_caja_cerrada = 0;
            int cant_empaques = 0;
            double diferencia = 0;
            double empaque_aux = 0;

            porcentaje_empaque = await configuracion.porcentaje_sugerencia_empaque(_context, codempresa);
            foreach (var detalle in tabladetalle)
            {//validar el empaque del precio
                cantidad = 0;
                empaque_caja_cerrada = await ventas.Empaque(_context, await ventas.Codigo_Empaque_Descuento_Especial(_context, coddescuento), detalle.coditem);
                //validar el empaque del descto especial
                if (await ventas.Cumple_Empaque_De_DesctoEspecial(_context, detalle.coditem, detalle.codtarifa, coddescuento, (decimal)detalle.cantidad, ""))
                {
                    detalle.cumple = true;
                    itemDataSugerencia nuevoItem = new itemDataSugerencia();
                    nuevoItem.coditem = detalle.coditem;
                    nuevoItem.descripcion = detalle.descripcion;
                    nuevoItem.medida = detalle.medida;
                    nuevoItem.cantidad = detalle.cantidad;
                    if (detalle.cantidad == 0 || empaque_caja_cerrada == 0)
                    {
                        nuevoItem.cantidad_sugerida = "0";
                        detalle.cumple = false;
                        detalle.coddescuento = 0;
                    }
                    else
                    {
                        nuevoItem.cantidad_sugerida = "Cumple empaque.";
                    }
                    nuevoItem.cantidad_sugerida_aplicable = cantidad;
                    nuevoItem.empaque_caja_cerrada = empaque_caja_cerrada;
                    nuevoItem.porcentaje = porcentaje_empaque;
                    cantidad_porcentaje = (empaque_caja_cerrada * porcentaje_empaque) / 100;
                    nuevoItem.cantidad_porcentaje = cantidad_porcentaje;
                    nuevoItem.diferencia = 0;
                    if (detalle.cantidad == 0 || empaque_caja_cerrada == 0)
                    {
                        nuevoItem.obs = "Cantidad 0 / Empaque Cerrado 0";
                    }
                    else
                    {
                        nuevoItem.obs = "Cumple Empaque Cerrado.";
                    }
                    dt.Add(nuevoItem);
                }
                else
                {
                    sugerencia = await ventas.Sugerir_Empaque_De_DesctoEspecial(_context, detalle.coditem, detalle.codtarifa, coddescuento, detalle.cantidad, "", codempresa);
                    string[] resultado_list = sugerencia.Split('/');
                    cantidad_mas = Convert.ToInt32(resultado_list[0]);
                    cantidad_menos = Convert.ToInt32(resultado_list[1]);

                    if (cantidad_mas > 0)
                    {//si la cantidad se va aumentar ingresa aca
                        detalle.cumple = false;
                        detalle.coddescuento = 0;
                    }
                    itemDataSugerencia otroItem = new itemDataSugerencia();
                    otroItem.coditem = detalle.coditem;
                    otroItem.descripcion = detalle.descripcion;
                    otroItem.medida = detalle.medida;
                    otroItem.cantidad = detalle.cantidad;
                    otroItem.cantidad_sugerida = sugerencia;
                    cant_empaques = (int)(detalle.cantidad / empaque_caja_cerrada);
                    empaque_aux = cant_empaques * empaque_caja_cerrada;
                    cantidad_porcentaje = (empaque_caja_cerrada * porcentaje_empaque) / 100;
                    if (Math.Abs(cantidad_mas) <= Math.Abs(cantidad_porcentaje))
                    {
                        cantidad = cantidad_mas;
                    }
                    if (Math.Abs(cantidad_menos) <= Math.Abs(cantidad_porcentaje))
                    {
                        if (empaque_caja_cerrada - (cantidad_mas + detalle.cantidad) == 0)
                        {
                            cantidad = cantidad_mas;
                        }
                        else
                        {
                            cantidad = cantidad_menos * -1;
                        }
                    }
                    if (cantidad == 0)
                    {
                        obs = "No Cumple.";
                        if (cantidad_mas > cantidad_menos)
                        {
                            if (detalle.cantidad > empaque_caja_cerrada)
                            {
                                cantidad = cantidad_menos;
                            }
                            else
                            {
                                cantidad = cantidad_mas;
                            }

                        }
                        else
                        {
                            cantidad = cantidad_mas;
                        }
                        diferencia = Math.Abs(cantidad_porcentaje) - Math.Abs(cantidad);
                    }
                    else
                    {
                        if (cantidad > 0)
                        {
                            if (cantidad <= cantidad_porcentaje)
                            {
                                obs = "Cumple porcentaje para aumentar cantidades.";
                            }
                            else
                            {
                                obs = "No Cumple.";
                            }

                        }
                        else
                        {
                            if (detalle.cantidad < empaque_caja_cerrada)
                            {
                                obs = "No Cumple.";
                            }
                            else
                            {
                                obs = "Cumple porcentaje para reducir cantidades.";
                            }
                        }
                        diferencia = Math.Abs(cantidad) - Math.Abs(cantidad_porcentaje);
                    }
                    otroItem.cantidad_sugerida_aplicable = cantidad;
                    otroItem.empaque_caja_cerrada = empaque_caja_cerrada;
                    otroItem.porcentaje = porcentaje_empaque;
                    otroItem.cantidad_porcentaje = cantidad_porcentaje;
                    otroItem.diferencia = Math.Abs(diferencia);
                    otroItem.obs = obs;
                    dt.Add(otroItem);

                }
            }

            return (dt, tabladetalle);
        }
        public async Task<(ResultadoValidacion resultadoValidacion, List<Dtnegativos> dtnegativos)> Validar_Saldos_Negativos_Doc_Remision(DBContext _context, List<itemDataMatriz> tabladetalle, DatosDocVta DVTA, List<Dtnegativos> dtnegativos, string codempresa, string usuario, string idproforma, int numeroidproforma)
        {
            ResultadoValidacion objres = new ResultadoValidacion();
            string cadena_items = "";
            string cadena_items2 = "";

            InicializarResultado(objres);

            if (tabladetalle.Count > 0)
            {
                List<string> msgs = new List<string>();
                List<string> negs = new List<string>();

                dtnegativos.Clear();
                dtnegativos = await saldos.ValidarNegativosDocVenta(_context, tabladetalle, Convert.ToInt32(DVTA.codalmacen), idproforma, numeroidproforma, msgs, negs, codempresa, usuario);

                foreach (var negativo in dtnegativos)
                {
                    if (negativo.obs.ToString() == "Genera Negativo")
                    {
                        if ((int)negativo.cantidad_conjunto > 0)
                        {
                            negs.Add(negativo.coditem_cjto.ToString());
                        }
                        if ((int)negativo.cantidad_suelta > 0)
                        {
                            negs.Add(negativo.coditem_suelto.ToString());
                        }
                    }
                }
                if (negs.Count == 0)
                {
                    objres.resultado = true;
                    objres.observacion = "Ningun item del documento genera negativos";
                    objres.accion = Acciones_Validar.Solo_Ok;

                    foreach (var detalle in tabladetalle)
                    {
                        detalle.cumple = true;
                    }
                }
                else
                {
                    foreach (var detalle in tabladetalle)
                    {
                        if (negs.Contains(detalle.coditem.ToString()))
                        {
                            // No cumple genera negativo
                            detalle.cumple = false;
                            // Genera la cadena de items en una fila
                            if (cadena_items.Trim().Length == 0)
                            {
                                cadena_items = detalle.coditem.ToString();
                            }
                            else
                            {
                                cadena_items += " - " + detalle.coditem.ToString();
                            }
                            // Genera la lista item en filas
                            if (cadena_items2.Trim().Length == 0)
                            {
                                cadena_items2 = detalle.coditem.ToString();
                            }
                            else
                            {
                                cadena_items2 += " - " + detalle.coditem.ToString();
                            }
                        }
                        else
                        {
                            // Si cumple y NO genera negativo
                            detalle.cumple = true;
                        }
                    }
                }
            }

            if (await almacen.Es_Tienda(_context, int.Parse(DVTA.codalmacen)))
            {
                //si es tienda se habilita la posibilidad de facturar con negativos
                //ESTO SERA PARA CASOS EXCEPCIONALES
                if (cadena_items.Trim().Length > 0)
                {
                    objres.resultado = false;
                    objres.observacion = "Los siguientes items generan saldos negativos!!!";
                    objres.obsdetalle = cadena_items2;
                    objres.datoA = DVTA.id + "-" + DVTA.numeroid;
                    objres.datoB = cadena_items2;
                    objres.accion = Acciones_Validar.Pedir_Servicio;
                }
            }
            else
            {
                if (cadena_items.Trim().Length > 0)
                {
                    objres.resultado = false;
                    objres.observacion = "Los siguientes items generan saldos negativos!!!";
                    objres.obsdetalle = cadena_items2;
                    objres.datoA = "";
                    objres.datoB = "";
                    objres.accion = Acciones_Validar.Ninguna;
                }
            }
            return (objres, dtnegativos);
        }

        public List<string> ListaItemsPedido(List<itemDataMatriz> tabladetalle)
        {
            List<string> resultado = new List<string>();

            resultado.Clear();
            foreach (var detalle in tabladetalle)
            {
                resultado.Add(detalle.coditem);
            }
            return resultado;
        }

        public async Task<int> Precio_Unico_Del_Documento(DBContext _context, List<itemDataMatriz> tabladetalle, string codempresa)
        {
            int resultado = -1;
            var lista_precios = await Lista_Precios_En_El_Documento(tabladetalle);
            if (lista_precios.Count == 1)
            {
                resultado = lista_precios[0];
            }
            else
            {
                resultado = -1;
            }
            return resultado;

        }

    }
}
