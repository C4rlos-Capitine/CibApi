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

        // ✅ POST: api/artigo/sync
        [HttpPost("sync")]
        public IActionResult Sync([FromBody] List<Artigo> artigos)
        {
            try { 
                if (artigos == null || artigos.Count == 0)
                    return BadRequest(new { message = "Nenhum artigo recebido para sincronização" });

                int inseridos = 0, atualizados = 0;

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    foreach (var artigo in artigos)
                    {
                        // 🔹 Verifica se já existe pelo código de barras
                        var checkQuery = "SELECT COUNT(*) FROM Artigo WHERE codigo_barra = @codigo_barra";

                        using (var checkCmd = new SqlCommand(checkQuery, connection))
                        {
                            checkCmd.Parameters.AddWithValue("@codigo_barra", artigo.Codigo_Barra);

                            int existe = (int)checkCmd.ExecuteScalar();

                            if (existe > 0)
                            {
                                // 🔹 Atualizar
                                var updateQuery = @"UPDATE Artigo
                                                    SET id_sala = @id_sala,
                                                        num_artigo = @num_artigo,
                                                        nome_artigo = @nome_artigo,
                                                        data_registo = @data_registo,
                                                        data_update = @data_update,
                                                        isSynced = 1,
                                                        lastUpdated = @lastUpdated
                                                    WHERE codigo_barra = @codigo_barra";

                                using (var updateCmd = new SqlCommand(updateQuery, connection))
                                {
                                    updateCmd.Parameters.AddWithValue("@id_sala", artigo.Id_Sala);
                                    updateCmd.Parameters.AddWithValue("@num_artigo", artigo.Num_Artigo);
                                    updateCmd.Parameters.AddWithValue("@nome_artigo", artigo.Nome_Artigo);
                                    updateCmd.Parameters.AddWithValue("@data_registo", artigo.Data_Registo);
                                    updateCmd.Parameters.AddWithValue("@data_update", artigo.Data_Update);
                                    updateCmd.Parameters.AddWithValue("@lastUpdated", DateTime.Now);
                                    updateCmd.Parameters.AddWithValue("@codigo_barra", artigo.Codigo_Barra);

                                    updateCmd.ExecuteNonQuery();
                                    atualizados++;
                                }
                            }
                            else
                            {
                                // 🔹 Inserir
                                var insertQuery = @"INSERT INTO Artigo
                                                    (id_sala, codigo_barra, num_artigo, nome_artigo, data_registo, data_update, isSynced, lastUpdated)
                                                    VALUES (@id_sala, @codigo_barra, @num_artigo, @nome_artigo, @data_registo, @data_update, 1, @lastUpdated)";

                                using (var insertCmd = new SqlCommand(insertQuery, connection))
                                {
                                    insertCmd.Parameters.AddWithValue("@id_sala", artigo.Id_Sala);
                                    insertCmd.Parameters.AddWithValue("@codigo_barra", artigo.Codigo_Barra);
                                    insertCmd.Parameters.AddWithValue("@num_artigo", artigo.Num_Artigo);
                                    insertCmd.Parameters.AddWithValue("@nome_artigo", artigo.Nome_Artigo);
                                    insertCmd.Parameters.AddWithValue("@data_registo", artigo.Data_Registo);
                                    insertCmd.Parameters.AddWithValue("@data_update", artigo.Data_Update);
                                    insertCmd.Parameters.AddWithValue("@lastUpdated", DateTime.Now);

                                    insertCmd.ExecuteNonQuery();
                                    inseridos++;
                                }
                            }
                        }
                    }
                }

                return Ok(new
                {
                    message = "Sincronização concluída",
                    inseridos,
                    atualizados,
                    total = artigos.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogInformation("\n\nExcepção geral " + ex.Message);
                return StatusCode(500, new { message = "Erro interno no servidor", erro = ex.Message });
            }
        }
    }
}
