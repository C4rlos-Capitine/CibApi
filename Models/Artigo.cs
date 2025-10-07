namespace CibApi.Models
{
    public class Artigo
    {
        public Guid id_artigo { get; set; }
        public Guid id_sala { get; set; }
        public string codigo_barra { get; set; } = string.Empty;
        public string num_artigo { get; set; } = string.Empty;
        public string nome_artigo { get; set; } = string.Empty;
        public DateTime data_registo { get; set; }
        public DateTime data_update { get; set; }
        public int IsSynced { get; set; }
        public DateTime LastUpdated { get; set; }
    }

}
