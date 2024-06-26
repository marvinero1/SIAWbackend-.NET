using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace siaw_DBContext.Models_Extra
{
    public class ProformasWF
    {
        public bool Aprobada { get; set; }
        public bool Transferida { get; set; }
        public int Codproforma { get; set; }
        public string Codpf { get; set; } //es id en veproforma
        public int Numeroid { get; set; }
        public string Codcliente { get; set; }
        public string Nomcliente { get; set; }
        public int Codvendedor { get; set; } // es codvendedor de veproforma
        public DateTime Fecha_inicial { get; set; }
        public string Hora_inicial { get; set; }
        public DateTime Fecha { get; set; }
        public DateTime Fechareg { get; set; }
        public string Horareg { get; set; }
        public DateTime Fechaaut { get; set; }
        public string Horaaut { get; set; }
        public string Usuarioreg { get; set; }
        public decimal Total { get; set; }
        public decimal Peso { get; set; }
        public int Nroitems { get; set; }
        public string Id_nr { get; set; }
        public int Nroid_nr { get; set; }
        public DateTime Fechareg_nr { get; set; }
        public string Horareg_nr { get; set; }
        public int DiasHrs_tomo { get; set; }
        public int Hrs_tomo { get; set; }
        public byte Tipopago { get; set; } //es tipopago en veremision que es tynint
        public bool Contra_entrega { get; set; }
        public string Estado_contra_entrega { get; set; }
        public int Ttl_hrs { get; set; }
    }
}
