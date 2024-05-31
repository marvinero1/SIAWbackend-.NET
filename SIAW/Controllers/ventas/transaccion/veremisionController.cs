using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public veremisionController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }



        [HttpGet]
        [Route("transfDatosProforma/{userConn}/{idProforma}/{nroidProforma}/{codProforma}/{codempresa}")]
        public async Task<object> transfDatosProforma(string userConn, string idProforma, int nroidProforma, int codProforma, string codempresa)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    //verificar si la proforma esta vinculada a una solicitud urgente
                    var doc_solurgente = await ventas.Solicitud_Urgente_IdNroid_de_Proforma(_context, idProforma, nroidProforma);
                    string msgAlert1 = "";
                    string id_solurgente = "";
                    int nroid_solurgente = 0;

                    if (doc_solurgente.id !="")
                    {
                        msgAlert1 = "La proforma es una solicitud urgente!!!";
                        id_solurgente = doc_solurgente.id;
                        nroid_solurgente = doc_solurgente.nroId;
                    }




                    // obtener cabecera.
                    var cabecera = await _context.veproforma
                        .Where(i => i.id == idProforma && i.numeroid == nroidProforma)
                        .FirstOrDefaultAsync();

                    if (cabecera == null)
                    {
                        return BadRequest(new { resp = "No se encontró una proforma con los datos proporcionados, revise los datos" });
                    }


                    return Ok(cabecera);

                    /*
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
                            cantidad = (double)i.p.cantidad,
                            udm = i.p.udm,
                            precioneto = (double)i.p.precioneto,
                            preciodesc = (double)(i.p.preciodesc ?? 0),
                            niveldesc = i.p.niveldesc,
                            preciolista = (double)i.p.preciolista,
                            codtarifa = i.p.codtarifa,
                            coddescuento = i.p.coddescuento,
                            total = (double)i.p.total,
                            // cantaut = i.p.cantaut,
                            // totalaut = i.p.totalaut,
                            // obs = i.p.obs,
                            porceniva = (double)(i.p.porceniva ?? 0),
                            cantidad_pedida = (double)(i.p.cantidad_pedida ?? 0),
                            // peso = i.p.peso,
                            nroitem = i.p.nroitem ?? 0,
                            // id = i.p.id,
                            porcen_mercaderia = 0,
                            porcendesc = 0
                        })
                        .ToListAsync();
                    // obtener cod descuentos x deposito
                    var codDesextraxDeposito = await configuracion.emp_coddesextra_x_deposito(_context, codempresa);
                    var codDesextraxDepositoContado = await configuracion.emp_coddesextra_x_deposito_contado(_context, codempresa);
                    // obtener descuentos
                    var descuentosExtra = await _context.vedesextraprof
                        .Join(_context.vedesextra,
                        p => p.coddesextra,
                        e => e.codigo,
                        (p, e) => new { p, e })
                        .Where(i => i.p.codproforma == codProforma && i.p.coddesextra != codDesextraxDeposito && i.p.coddesextra != codDesextraxDepositoContado)
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
                    return Ok(new
                    {
                        cabecera = cabecera,
                        detalle = detalle,
                        descuentos = descuentosExtra
                    });



                    */
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }
        }




        private async Task<object?> transferirdoc(DBContext _context, int codigo, string tipo)
        {

            if (tipo == "proforma")
            {

            }
            else if (tipo == "pedido")
            {
                /*
                 transferirdatospedido(CStr(codigo))
                tproforma.Text = "0"
                tpedido.Text = CStr(codigo)
                 */
            }
            else if(tipo =="remision")
            {
                /*
                 transferirdatosremision(CStr(codigo))
                tproforma.Text = "0"
                tpedido.Text = "0"
                 */
            }
            return null;
        }

        private async Task<object?> transferirdatosproforma(DBContext _context, int codigodoc)
        {
            // cabecera
            var cabecera = await _context.veproforma.Where(i => i.codigo == codigodoc).FirstOrDefaultAsync();
            // revisar 2 campos de nombres para cliente real: razon social y nombre comercial


            return null;

        }




    }
}
