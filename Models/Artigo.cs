namespace CibApi.Models
{
    public class Artigo
    {
        public int Id_Artigo { get; set; }
        public int Id_Sala { get; set; }
        public string Codigo_Barra { get; set; } = string.Empty;
        public string Num_Artigo { get; set; } = string.Empty;
        public string Nome_Artigo { get; set; } = string.Empty;
        public DateTime Data_Registo { get; set; }
        public DateTime Data_Update { get; set; }
        public bool IsSynced { get; set; } = false;
        public DateTime LastUpdated { get; set; }
    }

}
