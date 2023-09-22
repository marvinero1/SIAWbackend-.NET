using SIAW.Data;
using SIAW.Models;

namespace SIAW.Models_Extra
{
    public class SaveProformaCompleta
    {
        public veproforma veproforma { get; set; }
        public List<veproforma1> veproforma1 { get; set; }
        public List<veproforma_valida> veproforma_valida { get; set; }
        public veproforma_anticipo veproforma_anticipo { get; set; }
        public List<vedesextraprof> vedesextraprof { get; set; }
        public List<verecargoprof> verecargoprof { get; set; }
        public veproforma_iva veproforma_iva { get; set; }
    }

    public class Requestgrabardesextra
    {
        public DBContext context { get; set; }
        public List<vedesextraprof> vedesextraprofList { get; set; }
    }

    public class Requestgrabarrecargo
    {
        public DBContext context { get; set; }
        public List<verecargoprof> verecargoprof { get; set; }
    }

    public class Requestgrabariva
    {
        public DBContext context { get; set; }
        public veproforma_iva veproforma_iva { get; set; }
    }
}
