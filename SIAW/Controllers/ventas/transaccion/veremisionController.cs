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
        private readonly Log log = new Log();

        public veremisionController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
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












        private async Task<(string resp, int codprof, int numeroId)> Grabar_Documento(DBContext _context, string idProf, string codempresa, SaveProformaCompleta datosProforma)
        {
            veproforma veproforma = datosProforma.veproforma;
            List<veproforma1> veproforma1 = datosProforma.veproforma1;
            var veproforma_valida = datosProforma.veproforma_valida;
            var dt_anticipo_pf = datosProforma.dt_anticipo_pf;
            var vedesextraprof = datosProforma.vedesextraprof;
            var verecargoprof = datosProforma.verecargoprof;
            var veproforma_iva = datosProforma.veproforma_iva;

            ////////////////////   GRABAR DOCUMENTO

            int _ag = await empresa.AlmacenLocalEmpresa(_context, codempresa);
            // verificar si valido el documento, si es tienda no es necesario que valide primero
            if (!await almacen.Es_Tienda(_context, _ag))
            {
                if (veproforma_valida.Count() < 1 || veproforma_valida == null)
                {
                    return ("Antes de grabar el documento debe previamente validar el mismo!!!", 0, 0);
                }
            }


            //obtenemos numero actual de proforma de nuevo
            int idnroactual = await datos_proforma.getNumActProd(_context, idProf);

            if (idnroactual == 0)
            {
                return ("Error al obtener los datos de numero de proforma", 0, 0);
            }

            // valida si existe ya la proforma
            if (await datos_proforma.existeProforma(_context, idProf, idnroactual))
            {
                return ("Ese numero de documento, ya existe, por favor consulte con el administrador del sistema.", 0, 0);
            }
            veproforma.numeroid = idnroactual;

            //fin de obtener id actual

            // obtener hora y fecha actual si es que la proforma no se importo
            if (veproforma.hora_inicial == "")
            {
                veproforma.fecha_inicial = DateTime.Parse(datos_proforma.getFechaActual());
                veproforma.hora_inicial = datos_proforma.getHoraActual();
            }


            // accion de guardar

            // guarda cabecera (veproforma)
            _context.veproforma.Add(veproforma);
            await _context.SaveChangesAsync();

            var codProforma = veproforma.codigo;

            // actualiza numero id
            var numeracion = _context.venumeracion.FirstOrDefault(n => n.id == idProf);
            numeracion.nroactual += 1;
            await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos


            int validaCantProf = await _context.veproforma.Where(i => i.id == veproforma.id && i.numeroid == veproforma.numeroid).CountAsync();
            if (validaCantProf > 1)
            {
                return ("Se detecto más de un número del mismo documento, por favor consulte con el administrador del sistema.", 0, 0);
            }


            // guarda detalle (veproforma1)
            // actualizar codigoproforma para agregar
            veproforma1 = veproforma1.Select(p => { p.codproforma = codProforma; return p; }).ToList();
            // colocar obs como vacio no nulo
            veproforma1 = veproforma1.Select(o => { o.obs = ""; return o; }).ToList();
            // actualizar peso del detalle.
            veproforma1 = await ventas.Actualizar_Peso_Detalle_Proforma(_context, veproforma1);

            _context.veproforma1.AddRange(veproforma1);
            await _context.SaveChangesAsync();





            //======================================================================================
            // grabar detalle de validacion
            //======================================================================================

            veproforma_valida = veproforma_valida.Select(p => { p.codproforma = codProforma; return p; }).ToList();
            _context.veproforma_valida.AddRange(veproforma_valida);
            await _context.SaveChangesAsync();

            //======================================================================================
            //grabar anticipos aplicados
            //======================================================================================
            try
            {
                if (dt_anticipo_pf.Count() > 0 && dt_anticipo_pf != null)
                {
                    var anticiposprevios = await _context.veproforma_anticipo.Where(i => i.codproforma == codProforma).ToListAsync();
                    if (anticiposprevios.Count() > 0)
                    {
                        _context.veproforma_anticipo.RemoveRange(anticiposprevios);
                        await _context.SaveChangesAsync();
                    }
                    var newData = dt_anticipo_pf
                        .Select(i => new veproforma_anticipo
                        {
                            codproforma = codProforma,
                            codanticipo = i.codanticipo,
                            monto = (decimal?)i.monto,
                            tdc = (decimal?)i.tdc,

                            fechareg = i.fechareg,
                            usuarioreg = veproforma.usuarioreg,
                            horareg = datos_proforma.getHoraActual()
                        }).ToList();
                    _context.veproforma_anticipo.AddRange(newData);
                    await _context.SaveChangesAsync();

                }
            }
            catch (Exception)
            {

                throw;
            }



            // grabar descto por deposito si hay descuentos

            if (vedesextraprof.Count() > 0)
            {
                await grabardesextra(_context, codProforma, vedesextraprof);
            }

            // grabar recargo si hay recargos
            if (verecargoprof.Count > 0)
            {
                await grabarrecargo(_context, codProforma, verecargoprof);
            }

            // grabar iva

            if (veproforma_iva.Count > 0)
            {
                await grabariva(_context, codProforma, veproforma_iva);
            }

            bool resultado = new bool();
            // grabar descto por deposito
            if (await ventas.Grabar_Descuento_Por_deposito_Pendiente(_context, codProforma, codempresa, veproforma.usuarioreg, vedesextraprof))
            {
                resultado = true;
            }
            else
            {
                resultado = false;
            }

            // ======================================================================================
            // actualizar saldo restante de anticipos aplicados
            // ======================================================================================
            if (resultado)
            {
                foreach (var reg in dt_anticipo_pf)
                {
                    if (!await anticipos_vta_contado.ActualizarMontoRestAnticipo(_context, reg.id_anticipo, reg.nroid_anticipo, reg.codproforma ?? 0, reg.codanticipo ?? 0, reg.monto, codempresa))
                    {
                        resultado = false;
                    }
                }
            }



            return ("ok", codProforma, veproforma.numeroid);


        }





    }
}
