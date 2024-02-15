namespace siaw_DBContext.Models_Extra
{
    public class itemDataMatriz
    {
        public string coditem { get; set; }
        public string descripcion { get; set; }
        public string medida { get; set; }
        public string ud { get; set; }
        public float porceniva { get; set; }
        public float cantidad_pedida { get; set; }
        public float cantidad { get; set; }
        public float porcen_mercaderia { get; set; }
        public int codtarifa { get; set; }
        public int coddescuento { get; set; }
        public float preciolista { get; set; }
        public string niveldesc { get; set; }
        public float porcendesc { get; set; }
        public float preciodesc { get; set; }
        public float precioneto { get; set; }
        public float total { get; set; }
        public bool cumple { get; set;} = true;
        public int nroitem { get; set; } = 0;

    }
}
