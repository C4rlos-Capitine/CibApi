using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CibApi.Models;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;

namespace CibApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArtigoController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<ArtigoController> _logger;

        public ArtigoController(IConfiguration configuration, ILogger<ArtigoController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
            _logger = logger;
        }

        // ✅ GET: api/artigo
        [HttpGet]
        public IActionResult GetAll()
        {
            var artigos = new List<Artigo>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT * FROM artigo";
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            artigos.Add(new Artigo
                            {
                                Id_Artigo = reader.GetInt32(reader.GetOrdinal("id_artigo")),
                                Id_Sala = reader.GetInt32(reader.GetOrdinal("id_sala")),
                                Codigo_Barra = reader.GetString(reader.GetOrdinal("codigo_barra")),
                                Num_Artigo = reader.GetString(reader.GetOrdinal("num_artigo")),
                                Nome_Artigo = reader.GetString(reader.GetOrdinal("nome_artigo")),
                                Data_Registo = reader.GetDateTime(reader.GetOrdinal("data_registo")),
                                Data_Update = reader.GetDateTime(reader.GetOrdinal("data_update")),
                                IsSynced = reader.GetBoolean(reader.GetOrdinal("isSynced")),
                                LastUpdated = reader.GetDateTime(reader.GetOrdinal("lastUpdated"))
                            });
                        }
                    }
                }
            }

            return Ok(artigos);
        }

        // ✅ POST: api/artigo (um artigo)
        [HttpPost]
        public IActionResult Create([FromBody] Artigo artigo)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"INSERT INTO artigo 
                                (id_sala, codigo_barra, num_artigo, nome_artigo, data_registo, data_update, isSynced, lastUpdated)
                              VALUES 
                                (@id_sala, @codigo_barra, @num_artigo, @nome_artigo, @data_registo, @data_update, @isSynced, @lastUpdated)";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id_sala", artigo.Id_Sala);
                    command.Parameters.AddWithValue("@codigo_barra", artigo.Codigo_Barra);
                    command.Parameters.AddWithValue("@num_artigo", artigo.Num_Artigo);
                    command.Parameters.AddWithValue("@nome_artigo", artigo.Nome_Artigo);
                    command.Parameters.AddWithValue("@data_registo", artigo.Data_Registo);
                    command.Parameters.AddWithValue("@data_update", artigo.Data_Update);
                    command.Parameters.AddWithValue("@isSynced", artigo.IsSynced);
                    command.Parameters.AddWithValue("@lastUpdated", artigo.LastUpdated);

                    command.ExecuteNonQuery();
                }
            }

            return Ok(new { message = "Artigo inserido com sucesso" });
        }

        // ✅ POST: api/artigo/lote (vários artigos de uma só vez)
        [HttpPost("lote")]
        public IActionResult CreateBatch([FromBody] List<Artigo> artigos)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                foreach (var artigo in artigos)
                {
                    var query = @"INSERT INTO artigo 
                                    (id_sala, codigo_barra, num_artigo, nome_artigo, data_registo, data_update, isSynced, lastUpdated)
                                  VALUES 
                                    (@id_sala, @codigo_barra, @num_artigo, @nome_artigo, @data_registo, @data_update, @isSynced, @lastUpdated)";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id_sala", artigo.Id_Sala);
                        command.Parameters.AddWithValue("@codigo_barra", artigo.Codigo_Barra);
                        command.Parameters.AddWithValue("@num_artigo", artigo.Num_Artigo);
                        command.Parameters.AddWithValue("@nome_artigo", artigo.Nome_Artigo);
                        command.Parameters.AddWithValue("@data_registo", artigo.Data_Registo);
                        command.Parameters.AddWithValue("@data_update", artigo.Data_Update);
                        command.Parameters.AddWithValue("@isSynced", artigo.IsSynced);
                        command.Parameters.AddWithValue("@lastUpdated", artigo.LastUpdated);

                        command.ExecuteNonQuery();
                    }
                }
            }

            return Ok(new { message = $"{artigos.Count} artigos inseridos com sucesso" });
        }
    }
}
