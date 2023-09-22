using SIAW.Data;

namespace SIAW.Funciones
{
    public class Cobranzas
    {
        public async Task<bool> Registrar_Descuento_Por_Deposito_de_Cbza(DBContext _context, string codcobranza, string codcliente, string codcliente_real, string nit, string codproforma, string cod_empresa, string usuarioreg)
        {
            DateTime Depositos_Desde_Fecha = new DateTime(2015, 5, 13);

            return true;
        }
    }
}
