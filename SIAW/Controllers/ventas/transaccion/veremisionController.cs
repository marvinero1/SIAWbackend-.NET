using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIAW.Controllers.ventas.modificacion;
using siaw_DBContext.Data;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using siaw_funciones;

namespace SIAW.Controllers.ventas.transaccion
{
    [Route("api/venta/transac/[controller]")]
    [ApiController]
    public class veremisionController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        private readonly siaw_funciones.Ventas ventas = new siaw_funciones.Ventas();
        private readonly siaw_funciones.Cliente cliente = new siaw_funciones.Cliente();
        private readonly Anticipos_Vta_Contado anticipos_vta_contado = new Anticipos_Vta_Contado();
        private readonly siaw_funciones.Nombres nombres = new siaw_funciones.Nombres();
        private readonly siaw_funciones.TipoCambio tipocambio = new siaw_funciones.TipoCambio();
        private readonly siaw_funciones.Configuracion configuracion = new siaw_funciones.Configuracion();
        private readonly siaw_funciones.Saldos saldos = new siaw_funciones.Saldos();

        private readonly Documento documento = new Documento();
        private readonly Log log = new Log();

        public veremisionController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }
        [HttpGet]
        [Route("getParametrosIniciales/{userConn}/{usuario}/{codempresa}")]
        public async Task<object> getParametrosIniciales(string userConn, string usuario, string codempresa)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    // obtener ID
                    string id = await configuracion.usr_idremision(_context,usuario);
                    // obtener nroID
                    int numeroid = new int();
                    if (id == "")
                    {
                        numeroid = 0;
                    }
                    else
                    {
                        numeroid = await documento.ventasnumeroid(_context, id) + 1;
                    }

                    //obtener cod vendedor
                    int codvendedor = await configuracion.usr_codvendedor(_context,usuario);
                    // obtener codmoneda
                    var codmoneda = await configuracion.usr_codmoneda(_context,usuario);
                    // obtener tdc
                    var tdc = await tipocambio._tipocambio(_context, await Empresa.monedabase(_context,codempresa),codmoneda,DateTime.Now.Date);
                    // obtener almacen actual
                    var codalmacen = await configuracion.usr_codalmacen(_context,usuario);
                    // obtener codigo tarifa defecto
                    var codtarifadefect = await configuracion.usr_codtarifa(_context,usuario);
                    // obtener codigo decuento defecto
                    var coddescuentodefect = await configuracion.usr_coddescuento(_context, usuario);

                    return Ok(new
                    {
                        id,
                        numeroid,
                        codvendedor,
                        codmoneda,
                        tdc,
                        codalmacen,
                        codtarifadefect,
                        coddescuentodefect
                    });
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }
        }

            [HttpGet]
        [Route("validaTranferencia/{userConn}/{idProforma}/{nroidProforma}")]
        public async Task<object> validaTranferencia(string userConn, string idProforma, int nroidProforma)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var codProforma = await _context.veproforma.Where(i=> i.id== idProforma && i.numeroid == nroidProforma && i.aprobada == true)
                        .Select(i=> new
                        {
                            i.codigo
                        }).FirstOrDefaultAsync();
                    if (codProforma == null)
                    {
                        return BadRequest(new { resp = "Esta proforma no fue encontrada. No se puede Transferir." });
                    }
                    if(await yahayproforma(_context, codProforma.codigo))
                    {
                        return BadRequest(new { resp = "Esta proforma ya fue transferida a otra Nota de Remision. No se puede Transferir." });
                    }
                    return Ok(new
                    {
                        resp = "Transfiriendo Proforma",
                        codProforma = codProforma.codigo
                    });
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }
        }

        private async Task<bool> yahayproforma(DBContext _context, int codproforma)
        {
            bool yahayproforma = false;
            // ver si no fue transferida a una nota de remision
            var transferida = await _context.veremision.Where(i=>i.anulada==false && i.codproforma==codproforma)
                .Select(i => new
                {
                    i.codigo
                }).FirstOrDefaultAsync();
            if (transferida != null)
            {
                yahayproforma = true;
            }
            return yahayproforma;

        }


        [HttpGet]
        [Route("transferirProforma/{userConn}/{idProforma}/{nroidProforma}/{codproforma}")]
        public async Task<object> transferirProforma(string userConn, string idProforma, int nroidProforma, int codproforma)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    //verificar si la proforma esta vinculada a una solicitud urgente
                    var doc_solurgente = await ventas.Solicitud_Urgente_IdNroid_de_Proforma(_context, idProforma, nroidProforma);
                    string msgAlert = "";
                    string txtid_solurgente = "";
                    int txtnroid_solurgente = 0;
                    if (doc_solurgente.id != "")
                    {
                        msgAlert = "La proforma es una solicitud urgente!!!";
                        txtid_solurgente = doc_solurgente.id;
                        txtnroid_solurgente = doc_solurgente.nroId;
                    }

                    // transferirdoc(trans.codigo_elegido, trans.tipo_documento)
                    var data = await transferirdatosproforma(_context, codproforma);
                    return Ok(new
                    {
                        msgAlert = msgAlert,
                        txtid_solurgente,
                        txtnroid_solurgente,

                        data
                    });
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }
        }







        private async Task<object> transferirdatosproforma(DBContext _context, int codproforma)
        {
            // obtener cabecera.
            var cabecera = await _context.veproforma
                .Where(i => i.codigo == codproforma)
                .FirstOrDefaultAsync();

            string _codcliente_real = cabecera.codcliente_real;


            // obtener razon social de cliente
            var codclientedescripcion = await cliente.Razonsocial(_context, cabecera.codcliente);


            // _codcliente_real = tabla.Rows(0)("codcliente_real")
            if (await cliente.Es_Cliente_Casual(_context,cabecera.codcliente))
            {
                _codcliente_real = await ventas.Cliente_Referencia_Proforma_Etiqueta(_context, cabecera.id,cabecera.numeroid);
            }

            // obtener tipo cliente
            var tipo_cliente = await cliente.Tipo_Cliente(_context, cabecera.codcliente_real);

            // obtener cliente habilitado
            string lblhabilitado = "";
            if (await cliente.clientehabilitado(_context,cabecera.codcliente_real))
            {
                lblhabilitado = "SI";
            }
            else
            {
                lblhabilitado = "NO";
            }


            // establecer ubicacion
            if (cabecera.ubicacion == null || cabecera.ubicacion == "")
            {
                cabecera.ubicacion = "NSE";
            }

            //////////////////////////////////////////////////////////////////////////////


            // obtener recargos
            var recargos = await _context.verecargoprof.Where(i => i.codproforma == codproforma).ToListAsync();

            // obtener descuentos
            var descuentosExtra = await _context.vedesextraprof
                .Join(_context.vedesextra,
                p => p.coddesextra,
                e => e.codigo,
                (p, e) => new { p, e })
                .Where(i => i.p.codproforma == codproforma)
                .Select(i => new
                {
                    i.p.codproforma,
                    i.p.coddesextra,
                    descripcion = i.e.descripcion,
                    i.p.porcen,
                    i.p.montodoc,
                    i.p.codcobranza,
                    i.p.codcobranza_contado,
                    i.p.codanticipo,
                    i.p.id
                })
                .ToListAsync();

            // obtener iva
            var iva = await _context.veproforma_iva.Where(i => i.codproforma == codproforma).ToListAsync();




            // obtener detalles.
            var codProforma = cabecera.codigo;
            var detalle = await _context.veproforma1
                .Where(i => i.codproforma == codProforma)
                .Join(_context.initem,
                p => p.coditem,
                i => i.codigo,
                (p, i) => new { p, i })
                .Select(i => new itemDataMatriz
                {
                    //codproforma = i.p.codproforma,
                    coditem = i.p.coditem,
                    descripcion = i.i.descripcion,
                    medida = i.i.medida,
                    udm = i.p.udm,
                    porceniva = (double)(i.p.porceniva ?? 0),
                    niveldesc = i.p.niveldesc,
                    cantidad = (double)i.p.cantidad,
                    codtarifa = i.p.codtarifa,
                    coddescuento = i.p.coddescuento,
                    precioneto = (double)i.p.precioneto,
                    preciodesc = (double)(i.p.preciodesc ?? 0),
                    preciolista = (double)i.p.preciolista,
                    total = (double)i.p.total,



                    // cantaut = i.p.cantaut,
                    // totalaut = i.p.totalaut,
                    // obs = i.p.obs,
                    // cantidad_pedida = (double)(i.p.cantidad_pedida ?? 0),
                    // peso = i.p.peso,
                    // nroitem = i.p.nroitem ?? 0,
                    // id = i.p.id,
                    // porcen_mercaderia = 0,
                    // porcendesc = 0
                })
                .ToListAsync();


            return (new
            {
                _codcliente_real,
                codclientedescripcion,
                tipo_cliente,
                lblhabilitado,

                cabecera = cabecera,
                detalle = detalle,
                descuentos = descuentosExtra,
                recargos = recargos,
                iva = iva,
            });
        }









        


        private async Task<(string resp, int codNRemision, int numeroId)> Grabar_Documento(DBContext _context, string id, string usuario, bool desclinea_segun_solicitud, int codProforma, string codempresa, SaveNRemisionCompleta datosRemision)
        {
            veremision veremision = datosRemision.veremision;
            List<veremision1> veremision1 = datosRemision.veremision1;
            var vedesextraremi = datosRemision.vedesextraremi;
            var verecargoremi = datosRemision.verecargoremi;
            var veremision_iva = datosRemision.veremision_iva;
            var veremision_chequerechazado = datosRemision.veremision_chequerechazado;



            ////////////////////   GRABAR DOCUMENTO
            //obtener id actual

            int idnroactual = await documento.ventasnumeroid(_context, id);
            if (await documento.existe_notaremision(_context,id,idnroactual + 1))
            {
                return ("Ese numero de documento, ya existe, por favor consulte con el administrador del sistema.", 0, 0);
            }
            veremision.numeroid = idnroactual + 1;
            // fin de obtener id actual


            // verificacion para ver si el documento descarga mercaderia
            bool descarga = await ventas.iddescarga(_context, id);


            if (desclinea_segun_solicitud == false)
            {
                veremision.idsoldesctos = "0";
                veremision.nroidsoldesctos = 0;
            }


            // accion de guardar

            // guarda cabecera (veremision)
            _context.veremision.Add(veremision);
            await _context.SaveChangesAsync();

            var codNRemision = veremision.codigo;

            // actualiza numero id
            var numeracion = _context.venumeracion.FirstOrDefault(n => n.id == id);
            numeracion.nroactual += 1;
            await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos



            // guarda detalle (veremision1)
            // actualizar codigoproforma para agregar
            veremision1 = veremision1.Select(p => { p.codremision = codNRemision; return p; }).ToList();
            // actualiza grupomer antes de guardado
            veremision1 = await ventas.Remision_Cargar_Grupomer(_context, veremision1);
            // actualizar peso del detalle.
            veremision1 = await ventas.Actualizar_Peso_Detalle_Remision(_context, veremision1);

            _context.veremision1.AddRange(veremision1);
            await _context.SaveChangesAsync();



            // actualizar proforma a transferida
            if (codProforma!=0)
            {
                try
                {
                    var proforma = await _context.veproforma.Where(i => i.codigo == codProforma).FirstOrDefaultAsync();

                    if (proforma != null)
                    {
                        proforma.transferida = true;
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        return ("No se pudo encontrar la proforma para transferirla, por favor consulte con el administrador del sistema.", 0, 0);
                    }
                }
                catch (Exception)
                {
                    return ("Error al transferir la proforma, por favor consulte con el administrador del sistema.", 0, 0);
                }
            }

            try
            {
                // grabar descto si hay descuentos
                if (vedesextraremi.Count() > 0)
                {
                    await grabardesextra(_context, codProforma, vedesextraremi);
                }
            }
            catch (Exception)
            {
                return ("Error al Guardar descuentos extras de Nota de Remision, por favor consulte con el administrador del sistema.", 0, 0);
            }
            try
            {
                // grabar recargo si hay recargos
                if (verecargoremi.Count > 0)
                {
                    await grabarrecargo(_context, codProforma, verecargoremi);
                }
            }
            catch (Exception)
            {
                return ("Error al Guardar recargos de Nota de Remision, por favor consulte con el administrador del sistema.", 0, 0);
            }
            try
            {
                // grabar iva
                if (veremision_iva.Count > 0)
                {
                    await grabariva(_context, codProforma, veremision_iva);
                }
            }
            catch (Exception)
            {
                return ("Error al Guardar iva de Nota de Remision, por favor consulte con el administrador del sistema.", 0, 0);
            }


            // actualizar stock actual si es que descarga mercaderia
            try
            {
                if (descarga)
                {
                    // Desde 15/11/2023 registrar en el log si por alguna razon no actualiza en instoactual correctamente al disminuir el saldo de cantidad y la reserva en proforma
                    if (await saldos.Veremision_ActualizarSaldo(_context,usuario,codNRemision, Saldos.ModoActualizacion.Crear)==false)
                    {
                        await log.RegistrarEvento(_context, usuario, Log.Entidades.Nota_Remision, codNRemision.ToString(), veremision.id, veremision.numeroid.ToString(), "veremisionController", "No actualizo stock al restar cantidad en NR.", Log.TipoLog.Creacion);
                    }
                    else
                    {
                        if (await ventas.revertirstocksproforma(_context,codProforma,codempresa) == false)
                        {
                            await log.RegistrarEvento(_context, usuario, Log.Entidades.Nota_Remision, codNRemision.ToString(), veremision.id, veremision.numeroid.ToString(), "veremisionController", "No actualizo stock al restar reserva en PF.", Log.TipoLog.Creacion);
                        }
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }


            // ####################################
            // generar plan de pagos si es que es a credito
            // #################################
            if (veremision.tipopago == 1)  // si el tipo pago es a credito == 1
            {
                // SI LA PROFORMA ES DE UNA NOTA ANTERIOR ANULADA SE DEBE HACER EL PLAN DE PAGOS SEGUN LA FECHA
                // DE LA PRIMERA NOTA DE REMISION
                if (await ventas.ProformaTieneNRAnulada(_context, codProforma))
                {
                    // con fecha de ntoa anulada
                    DateTime fecha_antigua = await ventas.ProformaFechaMasAntiguaNR(_context, codProforma);

                    // #si es PP genrarar con su monto y su fecha no importan las complementarias ni influye
                    if (await ventas.remision_es_PP(_context,codNRemision))
                    {
                        /*
                        if (await ventas.generarcuotaspago())
                        {

                        }
                        */
                    }
                }
                else
                {
                    // procedimiento normal
                    // #si es PP genrarar con su monto y su fecha no importan las complementarias ni influye




                }
            }






            return ("ok", codProforma, 0);


        }


        private async Task grabardesextra(DBContext _context, int codRemi, List<vedesextraremi> vedesextraremi)
        {
            var descExtraAnt = await _context.vedesextraremi.Where(i => i.codremision == codRemi).ToListAsync();
            if (descExtraAnt.Count() > 0)
            {
                _context.vedesextraremi.RemoveRange(descExtraAnt);
                await _context.SaveChangesAsync();
            }
            vedesextraremi = vedesextraremi.Select(p => { p.codremision = codRemi; return p; }).ToList();
            _context.vedesextraremi.AddRange(vedesextraremi);
            await _context.SaveChangesAsync();
        }


        private async Task grabarrecargo(DBContext _context, int codRemi, List<verecargoremi> verecargoremi)
        {
            var recargosAnt = await _context.verecargoremi.Where(i => i.codremision == codRemi).ToListAsync();
            if (recargosAnt.Count() > 0)
            {
                _context.verecargoremi.RemoveRange(recargosAnt);
                await _context.SaveChangesAsync();
            }
            verecargoremi = verecargoremi.Select(p => { p.codremision = codRemi; return p; }).ToList();
            _context.verecargoremi.AddRange(verecargoremi);
            await _context.SaveChangesAsync();
        }

        private async Task grabariva(DBContext _context, int codRemi, List<veremision_iva> veremision_iva)
        {
            var ivaAnt = await _context.veremision_iva.Where(i => i.codremision == codRemi).ToListAsync();
            if (ivaAnt.Count() > 0)
            {
                _context.veremision_iva.RemoveRange(ivaAnt);
                await _context.SaveChangesAsync();
            }
            veremision_iva = veremision_iva.Select(p => { p.codremision = codRemi; return p; }).ToList();
            _context.veremision_iva.AddRange(veremision_iva);
            await _context.SaveChangesAsync();
        }


    }
}
