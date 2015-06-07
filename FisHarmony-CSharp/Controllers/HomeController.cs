using System;
using System.Configuration;
using System.Web.Http;
using Dapper;
using Npgsql;
using RestSharp;

namespace FisHarmony_CSharp.Controllers
{
  public class HomeController : ApiController
  {
    [HttpGet]
    [Route("test")]
    public IHttpActionResult Test()
    {
      decimal longitude = (decimal)-118.1968472;
      decimal latitude = (decimal)33.7611916;

      decimal minLong = longitude - (decimal)0.005;
      decimal minLat = latitude - (decimal)0.005;

      decimal maxLong = longitude + (decimal)0.005;
      decimal maxLat = latitude + (decimal)0.005;

      var key = ConfigurationManager.ConnectionStrings["AIS"].ConnectionString;

      var req =
        string.Format(
          "http://services.marinetraffic.com/api/exportvessels/{0}/MINLAT:{1}/MAXLAT:{2}/MINLON:{3}/MAXLON:{4}/timespan:60/protocol:xml/msgtype:extended",
          key,
          minLat,
          maxLat,
          minLong,
          maxLong);

      var client = new RestClient(req);
      var request = new RestRequest(Method.GET);
      var response = client.Execute(request);
      var content = response.Content; // raw content as string
      return Ok(content);
    }

    [HttpGet]
    [Route("")]
    public IHttpActionResult Home()
    {
      NpgsqlConnection conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["PostgreSQL"].ConnectionString);
      conn.Open();

      var reports = conn.Query<Report>("select * from reports");


      //("select blobinfo = @blobinfo, " +
      //                               "latitude = @latitude, " +
      //                               "longitude = @longitude, " +
      //                               "compass_direction = @compass_direction, " +
      //                               "submitter_id = @submitter_id, " +
      //                               "verified = @verified, " +
      //                               "created_at = @created_at, " +
      //                               "updated_at = @updated_at, " +
      //                               "notes = @notes, " +
      //                               "picture_taken_at = @picture_taken_at from reports");
      return Ok(reports);

      //NpgsqlCommand command = new NpgsqlCommand("select * from reports", conn);

      //StringBuilder resultBuilder = new StringBuilder();

      //try
      //{
      //  NpgsqlDataReader dr = command.ExecuteReader();
      //  while (dr.Read())
      //  {
      //    for (int i = 0; i < dr.FieldCount; i++)
      //    {
      //      resultBuilder.AppendFormat("{0} \t", dr[i]);
      //    }
      //    resultBuilder.AppendLine();
      //  }
      //}
      //finally
      //{
      //  conn.Close();
      //}

      //return Ok(resultBuilder.ToString());
    }
  }

  public class Report
  {
    public string blobinfo { get; set; }
    public decimal latitude { get; set; }
    public decimal longitude { get; set; }
    public string compass_direction { get; set; }
    public int submitter_id { get; set; }
    public bool verified { get; set; }
    public DateTime created_at { get; set; }
    public DateTime updated_at { get; set; }
    public string notes { get; set; }
    public DateTime picture_taken_at { get; set; }
  }
}